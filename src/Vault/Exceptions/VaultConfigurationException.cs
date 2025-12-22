namespace Vault.Exceptions;

/// <summary>
/// Exception levée lors d'une erreur de configuration de Vault.
/// </summary>
public class VaultConfigurationException
    : VaultException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VaultConfigurationException"/> class.
    /// </summary>
    public VaultConfigurationException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VaultConfigurationException"/> class.
    /// </summary>
    /// <param name="message">Le message d'erreur.</param>
    public VaultConfigurationException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VaultConfigurationException"/> class.
    /// </summary>
    /// <param name="message">Le message d'erreur.</param>
    /// <param name="innerException">L'exception interne.</param>
    public VaultConfigurationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
