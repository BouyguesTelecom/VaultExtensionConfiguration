namespace Vault.Enum;

/// <summary>
/// Authentication type for Vault.
/// </summary>
public enum VaultAuthenticationType
{
    /// <summary>
    /// Indicates that no options are set.
    /// </summary>
    None = 0,

    /// <summary>
    /// Local authentication via token (.vault-token file).
    /// </summary>
    Local = 1,

    /// <summary>
    /// Automatic authentication via AWS IAM
    /// Uses available AWS credentials (environment variables, EC2 instance profile, ECS task role, etc.)
    /// Requires Vault role configuration with auth_type=iam and an appropriate bound_iam_principal_arn.
    /// </summary>
    AWS_IAM = 2,

    /// <summary>
    /// Automatic authentication via Kubernetes
    /// Uses the JWT token of the pod's service account (projected or default token file) to authenticate
    /// against a Kubernetes auth backend mounted in Vault.
    /// Requires Vault role configuration bound to the appropriate service account name/namespace.
    /// </summary>
    Kubernetes = 3,

    /// <summary>
    /// Custom authentication via a custom IAuthMethodInfo implementation
    /// Allows providing your own authentication strategy.
    /// Assigned a high value to leave room (3-99) for future built-in authentication modes
    /// without shifting this value and breaking binary/serialization compatibility.
    /// </summary>
    Custom = 100,
}
