using Vault.Options;
using FluentValidation;

namespace Vault.Validators;

/// <summary>
/// Validateur pour VaultLocalConfiguration.
/// Vérifie la configuration spécifique à l'authentification locale par token.
/// </summary>
public class VaultLocalConfigurationValidator : AbstractValidator<VaultLocalConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VaultLocalConfigurationValidator"/> class.
    /// </summary>
    public VaultLocalConfigurationValidator()
    {
        // Inclure les validations de base
        Include(new VaultDefaultConfigurationValidator());
    }
}
