using Vault.Enum;
using Vault.Options;
using Vault.Options.Configuration;

namespace Vault.Validators;

/// <summary>
/// Validator for VaultOptions configuration
/// Ensures all required settings are properly configured based on the authentication type
/// </summary>
public static class VaultOptionsValidator
{
    /// <summary>
    /// Validates VaultOptions and throws InvalidOperationException if validation fails
    /// </summary>
    /// <param name="vaultOptions">The VaultOptions to validate</param>
    /// <exception cref="ArgumentNullException">Thrown when vaultOptions is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when validation fails</exception>
    public static void Validate(VaultOptions vaultOptions)
    {
        if (vaultOptions == null)
        {
            throw new ArgumentNullException(nameof(vaultOptions));
        }

        // If Vault is not activated, skip validation
        if (!vaultOptions.IsActivated)
        {
            return;
        }

        // Validate authentication type is set
        if (vaultOptions.AuthenticationType == VaultAuthenticationType.None)
        {
            throw new InvalidOperationException(
                "Vault:AuthenticationType configuration cannot be 'None' when Vault is activated");
        }

        var config = vaultOptions.Configuration;

        // Validate common configuration
        ValidateCommonConfiguration(config);

        // Validate specific authentication configuration
        switch (vaultOptions.AuthenticationType)
        {
            case VaultAuthenticationType.Local:
                ValidateLocalConfiguration(config);
                break;

            case VaultAuthenticationType.AWS_IAM:
                ValidateAwsIamConfiguration(config);
                break;

            case VaultAuthenticationType.Custom:
                ValidateCustomConfiguration(config);
                break;
        }
    }

    /// <summary>
    /// Validates common configuration properties required by all authentication types
    /// </summary>
    private static void ValidateCommonConfiguration(VaultDefaultConfiguration config)
    {
        if (config == null)
        {
            throw new InvalidOperationException(
                "Vault:Configuration is missing. Ensure VaultOptions.Configuration is properly set.");
        }

        if (string.IsNullOrWhiteSpace(config.VaultUrl))
        {
            throw new InvalidOperationException(
                "Vault:Configuration:VaultUrl configuration is missing");
        }

        if (string.IsNullOrWhiteSpace(config.MountPoint))
        {
            throw new InvalidOperationException(
                "Vault:Configuration:MountPoint configuration is missing");
        }
    }

    /// <summary>
    /// Validates configuration for Local authentication
    /// </summary>
    private static void ValidateLocalConfiguration(VaultDefaultConfiguration config)
    {
        if (config is not VaultLocalConfiguration localConfig)
        {
            throw new InvalidOperationException(
                "Configuration must be of type VaultLocalConfiguration for Local authentication. " +
                "Create a VaultLocalConfiguration instance and assign it to VaultOptions.Configuration.");
        }

        if (string.IsNullOrWhiteSpace(localConfig.TokenFilePath))
        {
            throw new InvalidOperationException(
                "Vault:Configuration:TokenFilePath configuration is missing for Local authentication. " +
                "Default convention: %USERPROFILE%\\.vault-token");
        }
    }

    /// <summary>
    /// Validates configuration for AWS IAM authentication
    /// </summary>
    private static void ValidateAwsIamConfiguration(VaultDefaultConfiguration config)
    {
        if (config is not VaultAwsIAMConfiguration awsConfig)
        {
            throw new InvalidOperationException(
                "Configuration must be of type VaultAwsIAMConfiguration for AWS_IAM authentication. " +
                "Create a VaultAwsIAMConfiguration instance and assign it to VaultOptions.Configuration.");
        }

        // Optional: Add specific AWS IAM validation if needed
        // For example, validate that either AwsIamRoleName is set OR both MountPoint and Environment are set
    }

    /// <summary>
    /// Validates configuration for Custom authentication
    /// </summary>
    private static void ValidateCustomConfiguration(VaultDefaultConfiguration config)
    {
        if (config is not VaultCustomConfiguration customConfig)
        {
            throw new InvalidOperationException(
                "Configuration must be of type VaultCustomConfiguration for Custom authentication. " +
                "Create a VaultCustomConfiguration instance and assign it to VaultOptions.Configuration.");
        }

        if (customConfig.AuthMethodFactory == null)
        {
            throw new InvalidOperationException(
                "AuthMethodFactory factory must be provided for Custom authentication. " +
                "Define VaultOptions.Configuration.AuthMethodFactory with a function that returns your custom IAuthMethodInfo.");
        }
    }
}
