using Vault.Options;
using FluentValidation;

namespace Vault.Validators;

/// <summary>
/// Validateur pour VaultAwsConfiguration.
/// Vérifie la configuration spécifique à l'authentification AWS IAM.
/// </summary>
public class VaultAwsConfigurationValidator : AbstractValidator<VaultAwsConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VaultAwsConfigurationValidator"/> class.
    /// </summary>
    public VaultAwsConfigurationValidator()
    {
        Include(new VaultDefaultConfigurationValidator());

        RuleFor(x => x.Environment)
            .NotEmpty()
            .WithMessage(VaultOptionsResources.Environment_Not_Empty);
    }
}
