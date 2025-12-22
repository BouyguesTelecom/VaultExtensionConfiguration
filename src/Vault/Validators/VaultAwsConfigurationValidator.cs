using FluentValidation;
using Vault.Options;

namespace Vault.Validators;

/// <summary>
/// Provides validation logic for <see cref="VaultAwsConfiguration"/> instances, ensuring required AWS Vault
/// configuration properties are set correctly.
/// </summary>
/// <remarks>This validator enforces that the <c>Environment</c> property is not empty and includes default
/// validation rules from <see cref="VaultDefaultConfigurationValidator"/>. Use this class with validation frameworks
/// such as FluentValidation to verify configuration objects before use.</remarks>
public class VaultAwsConfigurationValidator
    : AbstractValidator<VaultAwsConfiguration>
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
