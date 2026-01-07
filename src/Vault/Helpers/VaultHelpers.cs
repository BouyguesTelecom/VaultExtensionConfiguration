using Vault.Enum;
using Vault.Options;
using Vault.Options.Configuration;
using VaultSharp;
using VaultSharp.V1.AuthMethods;
using VaultSharp.V1.AuthMethods.AWS;
using VaultSharp.V1.AuthMethods.Token;

namespace Vault.Helpers;

public static class VaultHelpers
{
    /// <summary>
    /// Retrieve the configuration (already in the correct instance according to the authentication type).
    /// </summary>
    public static VaultDefaultConfiguration GetConfiguration(this VaultOptions options)
    {
        return options.Configuration;
    }

    /// <summary>
    /// Create the appropriate authentication method according to the configured type.
    /// </summary>
    public static IAuthMethodInfo? CreateAuthMethod(this VaultOptions options)
    {
        return options.AuthenticationType switch
        {
            VaultAuthenticationType.Local => ((VaultLocalConfiguration)options.Configuration).CreateLocalAuthMethod(),
            VaultAuthenticationType.AWS_IAM => ((VaultAwsIAMConfiguration)options.Configuration).CreateAwsIamAuthMethod(),
            VaultAuthenticationType.Custom => ((VaultCustomConfiguration)options.Configuration).CreateCustomAuthMethod(),
            _ => throw new NotSupportedException($"Authentication type '{options.AuthenticationType}' is not supported")
        };
    }

    private static IAuthMethodInfo CreateLocalAuthMethod(this VaultLocalConfiguration config)
    {
        var token = ReadTokenFromFile(config.TokenFilePath);

        const string bearerPrefix = "Bearer ";
        if (token.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            token = token.Substring(bearerPrefix.Length).Trim();
        }

        return new TokenAuthMethodInfo(token);
    }

    private static IAuthMethodInfo CreateAwsIamAuthMethod(this VaultAwsIAMConfiguration config)
    {
        var awsAuthMountPoint = config.AwsAuthMountPoint;

        // Determine the role name
        // TODO we will add a fluent check on the options to say:
        // - MountPoint & Environment are mandatory in any case
        string roleName = !string.IsNullOrWhiteSpace(config.AwsIamRoleName)
            ? config.AwsIamRoleName
            : $"{config.MountPoint}-{config.Environment}-role";

        // Retrieve AWS credentials
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

        try
        {
            // Create IAMAWSAuthMethodInfo with the determined role name
            var authMethodInfo = new IAMAWSAuthMethodInfo(
                mountPoint: awsAuthMountPoint,
                requestHeaders: base64EncodedIamRequestHeaders,
                requestBody: base64EncodedIamRequestBody,
                roleName: roleName);

            // Validate authentication
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
            var testToken = testClient.V1.Auth.Token.LookupSelfAsync().GetAwaiter().GetResult();

            // Authentication successful
            return authMethodInfo;
        }
        catch (Exception authEx)
        {
            throw new InvalidOperationException(
                $"Unable to authenticate with Vault using role '{roleName}'.\n" +
                $"Error: {authEx.Message}\n\n" +
                "Check that:\n" +
                "- The role exists in Vault: vault list auth/aws/role\n" +
                $"- The role '{roleName}' is configured with auth_type=iam\n" +
                "- The bound_iam_principal_arn matches your EC2 role",
                authEx);
        }
    }

    private static IAuthMethodInfo CreateCustomAuthMethod(this VaultCustomConfiguration config)
    {
        if (config.AuthMethodFactory == null)
        {
            throw new InvalidOperationException(
                "AuthMethodFactory factory must be provided for Custom authentication. " +
                "Define VaultOptions.Configuration with a VaultCustomConfiguration instance and its AuthMethodFactory.");
        }

        try
        {
            return config.AuthMethodFactory();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Error creating custom authentication method: {ex.Message}",
                ex);
        }
    }

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
