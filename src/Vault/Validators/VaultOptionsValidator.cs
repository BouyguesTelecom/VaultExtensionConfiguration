using FluentValidation;
using Vault.Enum;
using Vault.Options;

namespace Vault.Validators;

/// <summary>
/// Provides validation logic for <see cref="VaultOptions"/> instances to ensure that configuration and authentication
/// settings are valid before use.
/// </summary>
/// <remarks>This validator enforces required fields and checks that the configuration type matches the selected
/// authentication method. It should be used to validate <see cref="VaultOptions"/> objects prior to initializing
/// Vault-related services. Validation failures will provide descriptive error messages for each invalid
/// property.</remarks>
public class VaultOptionsValidator
    : AbstractValidator<VaultOptions>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VaultOptionsValidator"/> class.
    /// </summary>
    public VaultOptionsValidator()
    {
        RuleFor(x => x.AuthenticationType)
            .NotEqual(VaultAuthenticationType.None)
            .WithMessage(VaultOptionsResources.VaultAuthenticationType_Not_In_Range);

        RuleFor(x => x.Configuration)
            .NotNull()
            .WithMessage(VaultOptionsResources.Vault_Configuration_Undefined);

        When(x => x.AuthenticationType == VaultAuthenticationType.Local, () =>
        {
            RuleFor(x => x.Configuration)
                .Must(config => config is VaultLocalConfiguration)
                .WithMessage(VaultOptionsResources.Configuration_Local_CheckType);

            When(x => x.Configuration is VaultLocalConfiguration, () =>
            {
                RuleFor(x => (VaultLocalConfiguration)x.Configuration!)
                    .SetValidator(new VaultLocalConfigurationValidator());
            });
        });

        When(x => x.AuthenticationType == VaultAuthenticationType.AWS_IAM, () =>
        {
            RuleFor(x => x.Configuration)
                .Must(config => config is VaultAwsConfiguration)
                .WithMessage(VaultOptionsResources.Configuration_AWS_IAM_CheckType);

            When(x => x.Configuration is VaultAwsConfiguration, () =>
            {
                RuleFor(x => (VaultAwsConfiguration)x.Configuration!)
                    .SetValidator(new VaultAwsConfigurationValidator());
            });
        });

        When(x => x.AuthenticationType == VaultAuthenticationType.Custom, () =>
        {
            RuleFor(x => x.CustomAuthMethodInfo)
                .NotNull()
                .WithMessage(VaultOptionsResources.Vault_CustomAuthMethodInfo_Undefined);

            RuleFor(x => x.Configuration)
                .Must(config => config != null && config.GetType() == typeof(VaultDefaultConfiguration))
                .WithMessage(VaultOptionsResources.Configuration_CUSTOM_CheckType);

            When(x => x.Configuration != null && x.Configuration.GetType() == typeof(VaultDefaultConfiguration), () =>
            {
                RuleFor(x => x.Configuration)
                    .SetValidator(new VaultDefaultConfigurationValidator()!);
            });
        });
    }
}
