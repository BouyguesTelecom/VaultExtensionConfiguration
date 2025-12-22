using Vault.Enum;
using Vault.Exceptions;
using Vault.Options;
using VaultSharp;
using VaultSharp.V1.AuthMethods;
using VaultSharp.V1.AuthMethods.AWS;
using VaultSharp.V1.AuthMethods.Token;

namespace Vault.Internal;

/// <summary>
/// Extension methods for Vault options.
/// </summary>
internal static class VaultHelpers
{
    /// <summary>
    /// Gets the appropriate configuration based on the authentication type.
    /// </summary>
    public static VaultDefaultConfiguration GetConfiguration(this VaultOptions options)
    {
        return options.Configuration
            ?? throw new VaultConfigurationException("Configuration must be defined");
    }

    /// <summary>
    /// Creates the appropriate authentication method based on the configured type.
    /// </summary>
    /// <exception cref="VaultConfigurationException">If the authentication type is not supported.</exception>
    /// <exception cref="VaultAuthenticationException">If authentication fails.</exception>
    public static IAuthMethodInfo CreateAuthMethod(this VaultOptions options)
    {
        return options.AuthenticationType switch
        {
            VaultAuthenticationType.Local => (options.Configuration as VaultLocalConfiguration ??
                throw new VaultConfigurationException("Configuration must be of type VaultLocalConfiguration for Local authentication"))
                .CreateLocalAuthMethod(),
            VaultAuthenticationType.AWS_IAM => (options.Configuration as VaultAwsConfiguration ??
                throw new VaultConfigurationException("Configuration must be of type VaultAwsConfiguration for AWS_IAM authentication"))
                .CreateAwsIamAuthMethod(),
            VaultAuthenticationType.Custom => options.CustomAuthMethodInfo
                ?? throw new VaultConfigurationException("CustomAuthMethodInfo must be defined when AuthenticationType = Custom"),
            _ => throw new VaultConfigurationException($"Authentication type '{options.AuthenticationType}' is not supported")
        };
    }

    /// <summary>
    /// Creates the local authentication method via token file.
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
                $"Vault token file does not exist: {config.TokenFilePath}", ex);
        }
        catch (Exception ex) when (ex is not VaultException)
        {
            throw new VaultAuthenticationException(
                "Error reading Vault token", ex);
        }
    }

    /// <summary>
    /// Creates the AWS IAM authentication method.
    /// </summary>
    private static IAuthMethodInfo CreateAwsIamAuthMethod(this VaultAwsConfiguration config)
    {
        var awsAuthMountPoint = config.AwsAuthMountPoint;

        // Determine the role name
        string roleName = !string.IsNullOrWhiteSpace(config.AwsIamRoleName)
            ? config.AwsIamRoleName
            // Standard pattern: {MountPoint}-{Environment}-role if not provided
            : $"{config.MountPoint}-{config.Environment}-role";

        try
        {
            // Get AWS credentials
            var credentials = new Amazon.Runtime.InstanceProfileAWSCredentials();
            var immutableCredentials = credentials.GetCredentials();

            // STS Configuration - Global endpoint (us-east-1)
            var region = "us-east-1";
            var stsHost = "sts.amazonaws.com";
            var requestBody = "Action=GetCallerIdentity&Version=2011-06-15";

            var headers = new Dictionary<string, string>
            {
                ["Content-Type"] = "application/x-www-form-urlencoded"
            };

            // Sign the request with AWS SigV4
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

            // Create IAMAWSAuthMethodInfo with the determined role name
            var authMethodInfo = new IAMAWSAuthMethodInfo(
                mountPoint: awsAuthMountPoint,
                requestHeaders: base64EncodedIamRequestHeaders,
                requestBody: base64EncodedIamRequestBody,
                roleName: roleName);

            // Validate authentication
            ValidateAwsAuthentication(config, authMethodInfo);

            return authMethodInfo;
        }
        catch (Exception ex) when (ex is not VaultException)
        {
            throw new VaultAuthenticationException(
                $"Unable to authenticate with Vault using role '{roleName}'.\n" +
                $"Error: {ex.Message}\n\n" +
                "Please verify that:\n" +
                "- The role exists in Vault: vault list auth/aws/role\n" +
                $"- The role '{roleName}' is configured with auth_type=iam\n" +
                "- The bound_iam_principal_arn matches your EC2/ECS role\n" +
                "- AWS credentials are available (instance profile, task role, etc.)",
                ex);
        }
    }

    /// <summary>
    /// Validates that AWS authentication works.
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

            // Synchronous validation - acceptable here as it's called once at startup
            var testToken = testClient.V1.Auth.Token.LookupSelfAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            throw new VaultAuthenticationException(
                "AWS IAM authentication validation with Vault failed", ex);
        }
    }

    /// <summary>
    /// Reads the token from a file.
    /// </summary>
    private static string ReadTokenFromFile(string tokenFilePath)
    {
        var expandedPath = Environment.ExpandEnvironmentVariables(tokenFilePath);

        if (!File.Exists(expandedPath))
        {
            throw new FileNotFoundException($"Token file does not exist: {expandedPath}");
        }

        return File.ReadAllText(expandedPath).Trim();
    }
}
