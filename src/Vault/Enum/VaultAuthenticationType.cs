namespace Vault.Enum;

/// <summary>
/// Specifies the authentication method to use when connecting to a Vault server.
/// </summary>
/// <remarks>Use this enumeration to select the appropriate authentication strategy for your Vault client. The
/// available options include no authentication, local token-based authentication, automatic AWS IAM authentication, or
/// a custom authentication method. Choose 'Custom' to provide your own implementation for authentication scenarios not
/// covered by the built-in options.</remarks>
public enum VaultAuthenticationType
{
    /// <summary>
    /// Indicates that no options are set.
    /// </summary>
    None = 0,

    /// <summary>
    /// Indicates that the resource or operation is local to the current machine or environment.
    /// </summary>
    Local = 10,

    /// <summary>
    /// Specifies the Amazon Web Services Identity and Access Management (IAM) authentication provider.
    /// </summary>
    AWS_IAM = 20,

    /// <summary>
    /// Specifies a custom log level with a user-defined value.
    /// </summary>
    /// <remarks>Use this value to represent a log level that does not correspond to any of the predefined levels. The
    /// numeric value can be used to distinguish custom log levels from standard ones.</remarks>
    Custom = 100
}
