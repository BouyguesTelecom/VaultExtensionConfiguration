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
    private readonly IVaultService? _vaultService;
    private Timer? _reloadTimer;
    private readonly ILogger? _logger;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="VaultConfigurationProvider"/> class.
    /// Main constructor used when VaultService is already available.
    /// </summary>
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
    /// Initializes a new instance of the <see cref="VaultConfigurationProvider"/> class.
    /// Constructor for compatibility with IConfigurationSource.Build
    /// VaultService will be injected later via SetVaultService.
    /// </summary>
    internal VaultConfigurationProvider(VaultConfigurationSource source)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));

        if (string.IsNullOrWhiteSpace(_source.Environment))
        {
            throw new InvalidOperationException(
                "Vault environment must be specified (e.g., DEV, PROD)");
        }
    }

    /// <summary>
    /// Inject VaultService after creation (used by extension method).
    /// </summary>
    internal void SetVaultService(IVaultService vaultService, ILogger<VaultConfigurationProvider>? logger = null)
    {
        if (_vaultService != null)
        {
            throw new InvalidOperationException("VaultService already set");
        }

        // Use reflection to assign the readonly field
        var field = typeof(VaultConfigurationProvider).GetField(
            "_vaultService",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(this, vaultService);

        var loggerField = typeof(VaultConfigurationProvider).GetField(
            "_logger",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        loggerField?.SetValue(this, logger);
    }

    /// <summary>
    /// Load secrets from Vault.
    /// </summary>
    public override void Load()
    {
        LoadAsync().GetAwaiter().GetResult();
    }

    private async Task LoadAsync()
    {
        if (_vaultService == null)
        {
            var errorMessage = "VaultService is not initialized. " +
                "Make sure to call AddVault() before AddVaultConfiguration()";

            if (_source.Optional)
            {
                _logger?.LogWarning(errorMessage);
                return;
            }

            throw new InvalidOperationException(errorMessage);
        }

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
                if (IsJsonValue(kvp.Value))
                {
                    FlattenJsonValue(kvp.Key, kvp.Value, data);
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
    /// Check if a value is JSON.
    /// </summary>
    private static bool IsJsonValue(object? value)
    {
        if (value is not string str)
        {
            return false;
        }

        str = str.Trim();
        return (str.StartsWith("{") && str.EndsWith("}")) || (str.StartsWith("[") && str.EndsWith("]"));
    }

    /// <summary>
    /// Flatten a JSON value into dotted keys with dot notation.
    /// </summary>
    private void FlattenJsonValue(string parentKey, object? value, Dictionary<string, string?> data)
    {
        if (value is not string jsonString)
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
}
