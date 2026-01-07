using System.Text.Json;
using Microsoft.Extensions.Logging;
using Vault.Abstractions;
using Vault.Helpers;
using Vault.Options;
using VaultSharp;
using VaultSharp.V1.SecretsEngines;

namespace Vault.Services;

public class VaultService
    : IVaultService
{
    private readonly IVaultClient _vaultClient;
    private readonly VaultOptions _options;
    private readonly ILogger _logger;

    public VaultService(
        VaultOptions options,
        ILogger<VaultService> logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (!options.IsActivated)
        {
            throw new InvalidOperationException("Vault service is not activated. Check Vault:IsActivated configuration.");
        }

        try
        {
            var authMethod = options.CreateAuthMethod();
            var config = options.GetConfiguration();

            HttpClient? httpClient = null;
            if (config.IgnoreSslErrors)
            {
                var httpClientHandler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
                };
                httpClient = new HttpClient(httpClientHandler);
            }

            var vaultClientSettings = new VaultClientSettings(config.VaultUrl, authMethod)
            {
                SecretsEngineMountPoints = new SecretsEngineMountPoints
                {
                    KeyValueV2 = config.MountPoint
                }
            };

            if (httpClient is not null)
            {
                vaultClientSettings.MyHttpClientProviderFunc = handler => httpClient;
            }

            _vaultClient = new VaultClient(vaultClientSettings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing VaultService");
            throw;
        }
    }

    public async Task<IEnumerable<string>> ListEnvironmentsAsync()
    {
        var listResponse = await _vaultClient.V1.Secrets.KeyValue.V2.ReadSecretPathsAsync(path: string.Empty);
        return listResponse.Data.Keys.ToList();
    }

    public async Task<Dictionary<string, object>> GetSecretsAsync(string environment)
    {
        if (string.IsNullOrWhiteSpace(environment))
        {
            throw new ArgumentException("Environment cannot be empty", nameof(environment));
        }

        var secrets = await _vaultClient.V1.Secrets.KeyValue.V2.ReadSecretAsync(path: environment);

        if (secrets?.Data?.Data == null)
        {
            return new Dictionary<string, object>();
        }

        return new Dictionary<string, object>(secrets.Data.Data);
    }

    public async Task<object?> GetSecretValueAsync(string environment, string key)
    {
        if (string.IsNullOrWhiteSpace(environment))
        {
            throw new ArgumentException("Environment cannot be empty", nameof(environment));
        }

        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Key cannot be empty", nameof(key));
        }

        var secrets = await GetSecretsAsync(environment);
        secrets.TryGetValue(key, out var value);

        return value;
    }

    public async Task<object?> GetNestedSecretValueAsync(string environment, string path)
    {
        if (string.IsNullOrWhiteSpace(environment))
        {
            throw new ArgumentException("Environment cannot be empty", nameof(environment));
        }

        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path cannot be empty", nameof(path));
        }

        var secrets = await GetSecretsAsync(environment);

        // Split the path by dots: "level1.level2.level3" -> ["level1", "level2", "level3"]
        var pathParts = path.Split('.');

        object? currentValue = secrets;

        foreach (var part in pathParts)
        {
            // If the current value is not a dictionary, we cannot continue
            if (currentValue is not Dictionary<string, object> dict)
            {
                // Try to convert from JsonElement if necessary
                if (currentValue is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Object)
                {
                    if (!jsonElement.TryGetProperty(part, out var jsonProperty))
                    {
                        _logger.LogDebug("Property '{Part}' not found in path '{Path}'", part, path);
                        return null;
                    }

                    currentValue = ConvertJsonElement(jsonProperty);
                }
                else
                {
                    _logger.LogDebug("Value at '{Part}' is not a navigable object in path '{Path}'", part, path);
                    return null;
                }
            }
            else
            {
                // Navigate through the dictionary
                if (!dict.TryGetValue(part, out currentValue))
                {
                    _logger.LogDebug("Key '{Part}' not found in path '{Path}'", part, path);
                    return null;
                }
            }
        }

        return currentValue;
    }

    /// <summary>
    /// Convert a JsonElement to a native object.
    /// </summary>
    private static object? ConvertJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt64(out var l) ? l : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Object => JsonSerializer.Deserialize<Dictionary<string, object>>(element.GetRawText()),
            JsonValueKind.Array => JsonSerializer.Deserialize<object[]>(element.GetRawText()),
            _ => element.ToString()
        };
    }
}
