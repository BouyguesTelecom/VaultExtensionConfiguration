using VaultSharp.V1.AuthMethods;

namespace Vault.Options.Configuration;

/// <summary>
/// Configuration for custom authentication
/// Allows providing your own IAuthMethodInfo implementation.
/// </summary>
public class VaultCustomConfiguration : VaultDefaultConfiguration
{
    /// <summary>
    /// Factory to create the custom IAuthMethodInfo implementation
    /// Must be provided by the user during configuration.
    /// </summary>
    public Func<IAuthMethodInfo>? AuthMethodFactory { get; set; }
}
