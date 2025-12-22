using FluentValidation;
using Vault.Options;

namespace Vault.Validators;

/// <summary>
/// Provides validation logic for instances of the VaultLocalConfiguration class using predefined validation rules.
/// </summary>
/// <remarks>This validator includes the base validation rules defined in VaultDefaultConfigurationValidator. Use
/// this class to ensure that VaultLocalConfiguration objects meet required criteria before use.</remarks>
public class VaultLocalConfigurationValidator
    : AbstractValidator<VaultLocalConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VaultLocalConfigurationValidator"/> class.
    /// </summary>
    public VaultLocalConfigurationValidator()
    {
        // Include base validations
        Include(new VaultDefaultConfigurationValidator());
    }
}
