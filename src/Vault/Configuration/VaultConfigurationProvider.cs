using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Vault.Abstractions;

namespace Vault.Configuration;

/// <summary>
/// HashiCorp Vault configuration provider for ASP.NET Core.
/// Loads secrets from Vault and makes them available via IConfiguration.
/// </summary>
public class VaultConfigurationProvider
    : ConfigurationProvider, IDisposable
{
    private readonly VaultConfigurationSource _source;
    private readonly IVaultService _vaultService;
    private readonly ILogger? _logger;
    private Timer? _reloadTimer;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="VaultConfigurationProvider"/> class.
    /// </summary>
    /// <param name="source">The configuration source.</param>
    /// <param name="vaultService">The Vault service instance.</param>
    /// <param name="logger">Optional logger for the provider.</param>
    public VaultConfigurationProvider(
        VaultConfigurationSource source,
        IVaultService vaultService,
        ILogger<VaultConfigurationProvider>? logger = null)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        _vaultService = vaultService ?? throw new ArgumentNullException(nameof(vaultService));
        _logger = logger;

        if (string.IsNullOrWhiteSpace(_source.Environment))
        {
            throw new InvalidOperationException(
                "Vault environment must be specified (e.g., DEV, PROD)");
        }
    }

    /// <summary>
    /// Load secrets from Vault.
    /// </summary>
    public override void Load()
    {
        LoadAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Release resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _reloadTimer?.Dispose();
        _reloadTimer = null;
        _disposed = true;

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Convert an object value to string.
    /// </summary>
    private static string? ConvertValueToString(object? value)
    {
        if (value == null)
        {
            return null;
        }

        return value switch
        {
            string s => s,
            bool b => b.ToString().ToLowerInvariant(),
            _ => value.ToString()
        };
    }

    /// <summary>
    /// Check if a value is JSON (object or array).
    /// Handles both string values and JsonElement values.
    /// </summary>
    private static bool IsJsonValue(object? value, out string? jsonString)
    {
        jsonString = null;

        if (value == null)
        {
            return false;
        }

        // Handle JsonElement from VaultSharp
        if (value is JsonElement jsonElement)
        {
            if (jsonElement.ValueKind == JsonValueKind.Object ||
                jsonElement.ValueKind == JsonValueKind.Array)
            {
                jsonString = jsonElement.GetRawText();
                return true;
            }

            if (jsonElement.ValueKind == JsonValueKind.String)
            {
                var str = jsonElement.GetString()?.Trim();
                if (!string.IsNullOrEmpty(str) &&
                    ((str.StartsWith("{") && str.EndsWith("}")) ||
                     (str.StartsWith("[") && str.EndsWith("]"))))
                {
                    jsonString = str;
                    return true;
                }
            }

            return false;
        }

        // Handle string values
        if (value is string strValue)
        {
            var trimmed = strValue.Trim();
            if ((trimmed.StartsWith("{") && trimmed.EndsWith("}")) ||
                (trimmed.StartsWith("[") && trimmed.EndsWith("]")))
            {
                jsonString = trimmed;
                return true;
            }
        }

        return false;
    }

    private async Task LoadAsync()
    {
        try
        {
            _logger?.LogInformation(
                "Loading Vault secrets for environment: {Environment}",
                _source.Environment);

            var secrets = await _vaultService.GetSecretsAsync(_source.Environment);

            var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

            foreach (var kvp in secrets)
            {
                // If the value is JSON, flatten it
                if (IsJsonValue(kvp.Value, out var jsonString))
                {
                    FlattenJsonValue(kvp.Key, jsonString, data);
                }
                else
                {
                    var value = ConvertValueToString(kvp.Value);
                    data[kvp.Key] = value;
                    _logger?.LogDebug("Secret loaded: {Key}", kvp.Key);
                }
            }

            Data = data;

            _logger?.LogInformation(
                "Loading complete: {Count} secrets loaded from Vault",
                secrets.Count);

            // Configure automatic reload if requested
            if (_source.ReloadOnChange && _reloadTimer == null)
            {
                var interval = TimeSpan.FromSeconds(_source.ReloadIntervalSeconds);
                _reloadTimer = new Timer(
                    _ => LoadAndNotifyChange(),
                    null,
                    interval,
                    interval);

                _logger?.LogInformation(
                    "Automatic reload configured: every {Seconds} seconds",
                    _source.ReloadIntervalSeconds);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(
                ex,
                "Error loading Vault secrets for environment: {Environment}",
                _source.Environment);

            if (!_source.Optional)
            {
                throw;
            }

            _logger?.LogWarning(
                "Optional loading failed, Vault configuration ignored");
        }
    }

    /// <summary>
    /// Load data and notify changes.
    /// </summary>
    private void LoadAndNotifyChange()
    {
        try
        {
            Load();
            OnReload();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error reloading Vault secrets");
        }
    }

    /// <summary>
    /// Flatten a JSON value into configuration keys.
    /// </summary>
    private void FlattenJsonValue(string parentKey, string? jsonString, Dictionary<string, string?> data)
    {
        if (string.IsNullOrEmpty(jsonString))
        {
            return;
        }

        try
        {
            using var doc = JsonDocument.Parse(jsonString);
            FlattenJsonElement(parentKey, doc.RootElement, data);
        }
        catch (JsonException ex)
        {
            // If JSON parsing fails, store the raw value
            _logger?.LogWarning(ex, "Unable to parse JSON for key {Key}, storing raw value", parentKey);
            data[parentKey] = jsonString;
        }
    }

    /// <summary>
    /// Recursively flatten a JSON element.
    /// </summary>
    private void FlattenJsonElement(string parentKey, JsonElement element, Dictionary<string, string?> data)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    var key = string.IsNullOrEmpty(parentKey)
                        ? property.Name
                        : $"{parentKey}:{property.Name}";

                    FlattenJsonElement(key, property.Value, data);
                }

                break;

            case JsonValueKind.Array:
                int index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    var key = $"{parentKey}:{index}";
                    FlattenJsonElement(key, item, data);
                    index++;
                }

                break;

            default:
                data[parentKey] = element.ToString();
                _logger?.LogDebug("Flattened JSON secret: {Key} = {Value}", parentKey, element.ToString());
                break;
        }
    }
}
