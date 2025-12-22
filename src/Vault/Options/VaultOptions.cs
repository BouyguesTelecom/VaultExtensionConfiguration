using Vault.Enum;
using VaultSharp.V1.AuthMethods;

namespace Vault.Options;

/// <summary>
/// Configuration options for the Vault service.
/// Allows configuring different authentication types (Local, AWS_IAM, or Custom).
/// </summary>
public class VaultOptions
{
    /// <summary>
    /// Gets or sets the authentication type (Local, AWS_IAM, or Custom).
    /// </summary>
    public VaultAuthenticationType AuthenticationType { get; set; } = VaultAuthenticationType.Local;

    /// <summary>
    /// Gets or sets the configuration for Vault access.
    /// Use VaultLocalConfiguration when AuthenticationType = Local.
    /// Use VaultAwsConfiguration when AuthenticationType = AWS_IAM.
    /// Use VaultDefaultConfiguration when AuthenticationType = Custom.
    /// </summary>
    public VaultDefaultConfiguration? Configuration { get; set; } = new VaultLocalConfiguration();

    /// <summary>
    /// Gets or sets the custom authentication method.
    /// Used only when AuthenticationType = Custom.
    /// Allows providing a custom IAuthMethodInfo implementation for authentication methods
    /// not natively supported (AppRole, LDAP, UserPass, etc.).
    /// </summary>
    public IAuthMethodInfo? CustomAuthMethodInfo { get; set; }
}
