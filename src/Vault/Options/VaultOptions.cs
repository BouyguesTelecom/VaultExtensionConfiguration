using System.Text.Json.Serialization;
using Vault.Enum;
using Vault.Options.Configuration;

namespace Vault.Options;

/// <summary>
/// Configuration options for the Vault service
/// Allows configuring different authentication types (Local, AWS_IAM or Custom).
/// </summary>
public class VaultOptions
{
    /// <summary>
    /// Indicates whether Vault is activated.
    /// </summary>
    public bool IsActivated { get; set; } = true;

    /// <summary>
    /// Authentication type (Local, AWS_IAM or Custom).
    /// </summary>
    public VaultAuthenticationType AuthenticationType { get; set; } = VaultAuthenticationType.None;

    /// <summary>
    /// Configuration specific to the authentication type
    /// The actual instance can be:
    /// - VaultLocalConfiguration if AuthenticationType = Local
    /// - VaultAwsIAMConfiguration if AuthenticationType = AWS_IAM
    /// - VaultCustomConfiguration if AuthenticationType = Custom
    /// - VaultDefaultConfiguration by default.
    /// </summary>
    public VaultDefaultConfiguration Configuration { get; set; } = new();
}
