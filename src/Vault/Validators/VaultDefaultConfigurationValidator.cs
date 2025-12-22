using FluentValidation;
using Vault.Options;

namespace Vault.Validators;

/// <summary>
/// Provides validation logic for instances of <see cref="VaultDefaultConfiguration"/> to ensure required configuration
/// properties are set.
/// </summary>
/// <remarks>This validator enforces that the <c>VaultUrl</c> and <c>MountPoint</c> properties of a <see
/// cref="VaultDefaultConfiguration"/> are not empty. Use this class to validate configuration objects before using them
/// in Vault-related operations.</remarks>
public class VaultDefaultConfigurationValidator
    : AbstractValidator<VaultDefaultConfiguration>
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
