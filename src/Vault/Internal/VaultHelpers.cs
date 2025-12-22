using Vault.Enum;
using Vault.Exceptions;
using Vault.Options;
using VaultSharp;
using VaultSharp.V1.AuthMethods;
using VaultSharp.V1.AuthMethods.AWS;
using VaultSharp.V1.AuthMethods.Token;

namespace Vault.Internal;

/// <summary>
/// Méthodes d'extension pour les options de Vault.
/// </summary>
internal static class VaultHelpers
{
    /// <summary>
    /// Récupère la configuration appropriée selon le type d'authentification.
    /// </summary>
    public static VaultDefaultConfiguration GetConfiguration(this VaultOptions options)
    {
        return options.Configuration
            ?? throw new VaultConfigurationException("Configuration doit être défini");
    }

    /// <summary>
    /// Crée la méthode d'authentification appropriée selon le type configuré.
    /// </summary>
    /// <exception cref="VaultConfigurationException">Si le type d'authentification n'est pas supporté.</exception>
    /// <exception cref="VaultAuthenticationException">Si l'authentification échoue.</exception>
    public static IAuthMethodInfo CreateAuthMethod(this VaultOptions options)
    {
        return options.AuthenticationType switch
        {
            VaultAuthenticationType.Local => (options.Configuration as VaultLocalConfiguration ??
                throw new VaultConfigurationException("Configuration doit être de type VaultLocalConfiguration pour l'authentification Local"))
                .CreateLocalAuthMethod(),
            VaultAuthenticationType.AWS_IAM => (options.Configuration as VaultAwsConfiguration ??
                throw new VaultConfigurationException("Configuration doit être de type VaultAwsConfiguration pour l'authentification AWS_IAM"))
                .CreateAwsIamAuthMethod(),
            VaultAuthenticationType.Custom => options.CustomAuthMethodInfo
                ?? throw new VaultConfigurationException("CustomAuthMethodInfo doit être défini lorsque AuthenticationType = Custom"),
            _ => throw new VaultConfigurationException($"Le type d'authentification '{options.AuthenticationType}' n'est pas supporté")
        };
    }

    /// <summary>
    /// Crée la méthode d'authentification locale via token file.
    /// </summary>
    private static IAuthMethodInfo CreateLocalAuthMethod(this VaultLocalConfiguration config)
    {
        try
        {
            var token = ReadTokenFromFile(config.TokenFilePath);

            const string bearerPrefix = "Bearer ";
            if (token.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase))
            {
                token = token.Substring(bearerPrefix.Length).Trim();
            }

            return new TokenAuthMethodInfo(token);
        }
        catch (FileNotFoundException ex)
        {
            throw new VaultAuthenticationException(
                $"Le fichier de token Vault n'existe pas: {config.TokenFilePath}", ex);
        }
        catch (Exception ex) when (ex is not VaultException)
        {
            throw new VaultAuthenticationException(
                "Erreur lors de la lecture du token Vault", ex);
        }
    }

    /// <summary>
    /// Crée la méthode d'authentification AWS IAM.
    /// </summary>
    private static IAuthMethodInfo CreateAwsIamAuthMethod(this VaultAwsConfiguration config)
    {
        var awsAuthMountPoint = config.AwsAuthMountPoint;

        // Déterminer le nom du rôle
        string roleName = !string.IsNullOrWhiteSpace(config.AwsIamRoleName)
            ? config.AwsIamRoleName
            // Pattern standard: {MountPoint}-{Environment}-role si non fourni
            : $"{config.MountPoint}-{config.Environment}-role";

        try
        {
            // Récupérer les credentials AWS
            var credentials = new Amazon.Runtime.InstanceProfileAWSCredentials();
            var immutableCredentials = credentials.GetCredentials();

            // Configuration STS - Endpoint global (us-east-1)
            var region = "us-east-1";
            var stsHost = "sts.amazonaws.com";
            var requestBody = "Action=GetCallerIdentity&Version=2011-06-15";

            var headers = new Dictionary<string, string>
            {
                ["Content-Type"] = "application/x-www-form-urlencoded"
            };

            // Signer la requête avec AWS SigV4
            var signedHeaders = AwsSigV4Helper.SignRequest(
                accessKey: immutableCredentials.AccessKey,
                secretKey: immutableCredentials.SecretKey,
                sessionToken: immutableCredentials.Token,
                region: region,
                service: "sts",
                method: "POST",
                host: stsHost,
                path: "/",
                queryString: "",
                headers: headers,
                body: requestBody);

            var headersForVault = new Dictionary<string, string>(signedHeaders);
            var headersJson = System.Text.Json.JsonSerializer.Serialize(headersForVault);
            var base64EncodedIamRequestHeaders = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(headersJson));
            var base64EncodedIamRequestBody = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(requestBody));

            // Créer IAMAWSAuthMethodInfo avec le nom de rôle déterminé
            var authMethodInfo = new IAMAWSAuthMethodInfo(
                mountPoint: awsAuthMountPoint,
                requestHeaders: base64EncodedIamRequestHeaders,
                requestBody: base64EncodedIamRequestBody,
                roleName: roleName);

            // Valider l'authentification
            ValidateAwsAuthentication(config, authMethodInfo);

            return authMethodInfo;
        }
        catch (Exception ex) when (ex is not VaultException)
        {
            throw new VaultAuthenticationException(
                $"Impossible de s'authentifier auprès de Vault avec le rôle '{roleName}'.\n" +
                $"Erreur: {ex.Message}\n\n" +
                "Vérifiez que:\n" +
                "- Le rôle existe dans Vault: vault list auth/aws/role\n" +
                $"- Le rôle '{roleName}' est configuré avec auth_type=iam\n" +
                "- Le bound_iam_principal_arn correspond à votre rôle EC2/ECS\n" +
                "- Les credentials AWS sont disponibles (instance profile, task role, etc.)",
                ex);
        }
    }

    /// <summary>
    /// Valide que l'authentification AWS fonctionne.
    /// </summary>
    private static void ValidateAwsAuthentication(VaultAwsConfiguration config, IAuthMethodInfo authMethodInfo)
    {
        try
        {
            var testClientSettings = new VaultClientSettings(config.VaultUrl, authMethodInfo);

            if (config.IgnoreSslErrors)
            {
                var httpClientHandler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
                };
                var httpClient = new HttpClient(httpClientHandler);
                testClientSettings.MyHttpClientProviderFunc = handler => httpClient;
            }

            var testClient = new VaultClient(testClientSettings);

            // Validation synchrone - acceptable ici car appelé une seule fois au démarrage
            var testToken = testClient.V1.Auth.Token.LookupSelfAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            throw new VaultAuthenticationException(
                "Échec de validation de l'authentification AWS IAM avec Vault", ex);
        }
    }

    /// <summary>
    /// Lit le token depuis un fichier.
    /// </summary>
    private static string ReadTokenFromFile(string tokenFilePath)
    {
        var expandedPath = Environment.ExpandEnvironmentVariables(tokenFilePath);

        if (!File.Exists(expandedPath))
        {
            throw new FileNotFoundException($"Le fichier token n'existe pas : {expandedPath}");
        }

        return File.ReadAllText(expandedPath).Trim();
    }
}
