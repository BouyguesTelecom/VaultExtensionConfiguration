namespace Vault;

/// <summary>
/// Type d'authentification pour Vault.
/// </summary>
public enum VaultAuthenticationType
{
    /// <summary>
    /// Aucune authentification.
    /// </summary>
    None = 0,

    /// <summary>
    /// Authentification locale via token (fichier .vault-token).
    /// </summary>
    Local = 10,

    /// <summary>
    /// Authentification automatique via AWS IAM.
    /// Utilise les credentials AWS disponibles (variables d'environnement, instance profile EC2, ECS task role, etc.).
    /// Requiert la configuration d'un rôle Vault avec auth_type=iam et un bound_iam_principal_arn approprié.
    /// </summary>
    AWS_IAM = 20,

    /// <summary>
    /// Authentification personnalisée - l'utilisateur fournit sa propre instance de IAuthMethodInfo.
    /// Utilisez cette option pour les méthodes d'authentification non prises en charge nativement.
    /// </summary>
    Custom = 100
}
