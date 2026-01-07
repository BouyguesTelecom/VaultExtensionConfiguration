namespace Vault.Options.Configuration;

/// <summary>
/// Configuration for local authentication via token
/// </summary>
public class VaultLocalConfiguration : VaultDefaultConfiguration
{
    /// <summary>
    /// Token file path
    /// Default convention: %USERPROFILE%\.vault-token
    /// You can use environment variables in the path
    /// </summary>
    public string TokenFilePath { get; set; } = "%USERPROFILE%\\.vault-token";
}
