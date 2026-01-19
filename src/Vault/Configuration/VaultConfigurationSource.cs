using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Vault.Abstractions;

namespace Vault.Configuration;

/// <summary>
/// Configuration source for HashiCorp Vault.
/// Implements IConfigurationSource to integrate Vault into the ASP.NET Core configuration system.
/// </summary>
public class VaultConfigurationSource
    : IConfigurationSource
{
    /// <summary>
    /// Name of the Vault environment to load (e.g., DEV, PROD).
    /// </summary>
    public string Environment { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether loading errors should be considered optional.
    /// </summary>
    public bool Optional { get; set; }

    /// <summary>
    /// Indicates whether the source should be reloadable.
    /// </summary>
    public bool ReloadOnChange { get; set; }

    /// <summary>
    /// Reload interval in seconds (if ReloadOnChange = true).
    /// </summary>
    public int ReloadIntervalSeconds { get; set; } = 300; // 5 minutes by default

    /// <summary>
    /// Gets or sets the Vault service instance to use for loading secrets.
    /// </summary>
    internal IVaultService? VaultService { get; set; }

    /// <summary>
    /// Gets or sets the logger for the configuration provider.
    /// </summary>
    internal ILogger<VaultConfigurationProvider>? Logger { get; set; }

    /// <summary>
    /// Build the configuration provider.
    /// Secrets are loaded immediately to make them available for subsequent
    /// configuration (e.g., connection strings for Entity Framework).
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when VaultService is not set.</exception>
    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        if (VaultService == null)
        {
            throw new InvalidOperationException(
                "VaultService must be set. Use AddVault() or AddVaultConfiguration() with a VaultService instance.");
        }

        // Create provider with VaultService and load immediately
        var provider = new VaultConfigurationProvider(this, VaultService, Logger);
        provider.Load();

        return provider;
    }
}
