namespace Vault.Exceptions;

/// <summary>
/// Exception de base pour toutes les erreurs liées à Vault.
/// </summary>
public class VaultException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VaultException"/> class.
    /// </summary>
    public VaultException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VaultException"/> class.
    /// </summary>
    /// <param name="message">Le message d'erreur.</param>
    public VaultException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VaultException"/> class.
    /// </summary>
    /// <param name="message">Le message d'erreur.</param>
    /// <param name="innerException">L'exception interne.</param>
    public VaultException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
