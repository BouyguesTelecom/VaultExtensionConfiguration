using Microsoft.Extensions.Configuration;

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
    /// Build the configuration provider.
    /// </summary>
    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new VaultConfigurationProvider(this);
    }
}
