namespace Vault.Options.Configuration;

/// <summary>
/// Base configuration for Vault access
/// Contains parameters common to all authentication types.
/// </summary>
public class VaultDefaultConfiguration
{
    /// <summary>
    /// Vault server URL.
    /// </summary>
    public string VaultUrl { get; set; } = string.Empty;

    /// <summary>
    /// KV v2 mount point in Vault.
    /// </summary>
    public string MountPoint { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether SSL errors should be ignored
    /// Should be disabled in production for security reasons.
    /// </summary>
    public bool IgnoreSslErrors { get; set; } = true;
}
