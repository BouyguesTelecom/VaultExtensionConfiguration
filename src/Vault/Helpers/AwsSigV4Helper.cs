using System.Security.Cryptography;
using System.Text;

namespace Vault.Helpers;

/// <summary>
/// Helper to sign HTTP requests with AWS Signature Version 4
/// Simplified implementation for Vault IAM Auth.
/// </summary>
public static class AwsSigV4Helper
{
    /// <summary>
    /// Signs an HTTP request using AWS Signature Version 4.
    /// </summary>
    /// <param name="accessKey">The AWS access key ID.</param>
    /// <param name="secretKey">The AWS secret access key.</param>
    /// <param name="sessionToken">The optional AWS session token for temporary credentials.</param>
    /// <param name="region">The AWS region.</param>
    /// <param name="service">The AWS service name.</param>
    /// <param name="method">The HTTP method.</param>
    /// <param name="host">The target host.</param>
    /// <param name="path">The request path.</param>
    /// <param name="queryString">The query string.</param>
    /// <param name="headers">The request headers.</param>
    /// <param name="body">The optional request body.</param>
    /// <returns>A dictionary containing the signed headers including the Authorization header.</returns>
    public static Dictionary<string, string> SignRequest(
        string accessKey,
        string secretKey,
        string? sessionToken,
        string region,
        string service,
        string method,
        string host,
        string path,
        string queryString,
        Dictionary<string, string> headers,
        string? body)
    {
        var now = DateTime.UtcNow;
        var dateStamp = now.ToString("yyyyMMdd");
        var amzDate = now.ToString("yyyyMMddTHHmmssZ");

        // Add mandatory AWS headers
        var signedHeaders = new Dictionary<string, string>(headers)
        {
            ["host"] = host,
            ["x-amz-date"] = amzDate
        };

        if (!string.IsNullOrEmpty(sessionToken))
        {
            signedHeaders["x-amz-security-token"] = sessionToken;
        }

        // Create the canonical request
        var canonicalUri = string.IsNullOrEmpty(path) ? "/" : path;
        var canonicalQueryString = queryString ?? "";
        var canonicalHeaders = string.Join(
            "\n",
            signedHeaders.OrderBy(h => h.Key).Select(h => $"{h.Key.ToLowerInvariant()}:{h.Value.Trim()}")) + "\n";
        var signedHeadersList = string.Join(";", signedHeaders.Keys.OrderBy(k => k).Select(k => k.ToLowerInvariant()));

        var payloadHash = HashSHA256(body ?? "");

        var canonicalRequest = $"{method}\n{canonicalUri}\n{canonicalQueryString}\n{canonicalHeaders}\n{signedHeadersList}\n{payloadHash}";

        // Create the string to sign
        var credentialScope = $"{dateStamp}/{region}/{service}/aws4_request";
        var stringToSign = $"AWS4-HMAC-SHA256\n{amzDate}\n{credentialScope}\n{HashSHA256(canonicalRequest)}";

        // Calculate the signature
        var signingKey = GetSignatureKey(secretKey, dateStamp, region, service);
        var signature = HexEncode(HmacSHA256(stringToSign, signingKey));

        // Add the authorization header
        signedHeaders["Authorization"] = $"AWS4-HMAC-SHA256 Credential={accessKey}/{credentialScope}, SignedHeaders={signedHeadersList}, Signature={signature}";

        return signedHeaders;
    }

    private static byte[] GetSignatureKey(string key, string dateStamp, string regionName, string serviceName)
    {
        var kDate = HmacSHA256(dateStamp, Encoding.UTF8.GetBytes("AWS4" + key));
        var kRegion = HmacSHA256(regionName, kDate);
        var kService = HmacSHA256(serviceName, kRegion);
        var kSigning = HmacSHA256("aws4_request", kService);
        return kSigning;
    }

    private static byte[] HmacSHA256(string data, byte[] key)
    {
        using var hmac = new HMACSHA256(key);
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
    }

    private static string HashSHA256(string data)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(data));
        return HexEncode(hash);
    }

    private static string HexEncode(byte[] bytes)
    {
        return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
    }
}
