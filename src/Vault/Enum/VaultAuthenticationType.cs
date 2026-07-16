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
    Local,

    /// <summary>
    /// Automatic authentication via AWS IAM
    /// Uses available AWS credentials (environment variables, EC2 instance profile, ECS task role, etc.)
    /// Requires Vault role configuration with auth_type=iam and an appropriate bound_iam_principal_arn.
    /// </summary>
    AWS_IAM,

    /// <summary>
    /// Automatic authentication via Kubernetes
    /// Uses the JWT token of the pod's service account (projected or default token file) to authenticate
    /// against a Kubernetes auth backend mounted in Vault.
    /// Requires Vault role configuration bound to the appropriate service account name/namespace.
    /// </summary>
    Kubernetes,

    /// <summary>
    /// Custom authentication via a custom IAuthMethodInfo implementation
    /// Allows providing your own authentication strategy.
    /// </summary>
    Custom
}
