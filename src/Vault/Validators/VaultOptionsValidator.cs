using Vault.Options;
using FluentValidation;

namespace Vault.Validators;

/// <summary>
/// Validateur pour VaultOptions.
/// Vérifie la cohérence de la configuration selon le type d'authentification.
/// </summary>
public class VaultOptionsValidator : AbstractValidator<VaultOptions>
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
