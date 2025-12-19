using Vault.Options;
using FluentValidation;

namespace Vault.Validators;

/// <summary>
/// Validateur pour VaultDefaultConfiguration.
/// Vérifie les propriétés communes à toutes les configurations Vault.
/// </summary>
public class VaultDefaultConfigurationValidator : AbstractValidator<VaultDefaultConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VaultDefaultConfigurationValidator"/> class.
    /// </summary>
    public VaultDefaultConfigurationValidator()
    {
        RuleFor(x => x.VaultUrl)
            .NotEmpty()
            .WithMessage(VaultOptionsResources.VaultUrl_Not_Empty);

        RuleFor(x => x.MountPoint)
            .NotEmpty()
            .WithMessage(VaultOptionsResources.MountPoint_Not_Empty);
    }
}
