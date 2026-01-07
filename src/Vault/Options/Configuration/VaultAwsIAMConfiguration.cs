namespace Vault.Options.Configuration;

/// <summary>
/// Configuration for automatic authentication via AWS IAM
/// Uses available AWS credentials (environment variables, EC2 instance profile, ECS task role, etc.)
/// </summary>
public class VaultAwsIAMConfiguration
    : VaultDefaultConfiguration
{
    /// <summary>
    /// Vault role name for AWS IAM authentication (optional)
    /// If not provided, the role will be automatically deduced according to the standard pattern:
    /// {MountPoint}-{Environment}-role
    /// Example: MountPoint="HELLOWORLD-FORMATION", Environment="thomas" -> "HELLOWORLD-FORMATION-thomas-role"
    /// This pattern corresponds to the naming convention used by your organization
    /// If you want to use a different role name, explicitly define this property.
    /// </summary>
    public string? AwsIamRoleName { get; set; }

    /// <summary>
    /// Deployment environment (dev, test, prod, thomas, etc.)
    /// Used to automatically build the Vault role name if AwsIamRoleName is not defined
    /// Pattern: {MountPoint}-{Environment}-role.
    /// </summary>
    public string? Environment { get; set; }

    /// <summary>
    /// AWS auth method mount point in Vault
    /// Default: "aws".
    /// </summary>
    public string AwsAuthMountPoint { get; set; } = "aws";
}
