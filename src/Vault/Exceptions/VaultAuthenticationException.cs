namespace Vault.Exceptions;

/// <summary>
/// Exception levée lors d'une erreur d'authentification avec Vault.
/// </summary>
public class VaultAuthenticationException : VaultException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VaultAuthenticationException"/> class.
    /// </summary>
    public VaultAuthenticationException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VaultAuthenticationException"/> class.
    /// </summary>
    /// <param name="message">Le message d'erreur.</param>
    public VaultAuthenticationException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VaultAuthenticationException"/> class.
    /// </summary>
    /// <param name="message">Le message d'erreur.</param>
    /// <param name="innerException">L'exception interne.</param>
    public VaultAuthenticationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
