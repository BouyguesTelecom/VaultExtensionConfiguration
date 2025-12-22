using Vault.Enum;
using VaultSharp.V1.AuthMethods;

namespace Vault.Options;

/// <summary>
/// Options de configuration pour le service Vault.
/// Permet de configurer différents types d'authentification (Local, AWS_IAM ou Custom).
/// </summary>
public class VaultOptions
{
    /// <summary>
    /// Type d'authentification (Local, AWS_IAM ou Custom).
    /// </summary>
    public VaultAuthenticationType AuthenticationType { get; set; } = VaultAuthenticationType.Local;

    /// <summary>
    /// Configuration pour l'accès à Vault.
    /// - VaultLocalConfiguration si AuthenticationType = Local.
    /// - VaultAwsConfiguration si AuthenticationType = AWS_IAM.
    /// - VaultDefaultConfiguration si AuthenticationType = Custom.
    /// </summary>
    public VaultDefaultConfiguration? Configuration { get; set; } = new VaultLocalConfiguration();

    /// <summary>
    /// Méthode d'authentification personnalisée.
    /// Utilisé uniquement si AuthenticationType = Custom.
    /// Permet de fournir une implémentation personnalisée de IAuthMethodInfo pour les méthodes
    /// d'authentification non prises en charge nativement (AppRole, LDAP, UserPass, etc.).
    /// </summary>
    public IAuthMethodInfo? CustomAuthMethodInfo { get; set; }
}
