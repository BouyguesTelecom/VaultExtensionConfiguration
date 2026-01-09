// Copyright (c) Bouygues Telecom. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Vault.Helpers;
using Xunit;

namespace Vault.Tests.Helpers;

/// <summary>
/// Unit tests for AwsSigV4Helper.
/// </summary>
public class AwsSigV4HelperTests
{
    [Fact]
    public void SignRequest_WithValidParameters_ReturnsSignedHeaders()
    {
        // Arrange
        var accessKey = "AKIAIOSFODNN7EXAMPLE";
        var secretKey = "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY";
        var region = "us-east-1";
        var service = "sts";
        var method = "POST";
        var host = "sts.amazonaws.com";
        var path = "/";
        var queryString = string.Empty;
        var headers = new Dictionary<string, string>
        {
            ["Content-Type"] = "application/x-www-form-urlencoded",
        };
        var body = "Action=GetCallerIdentity&Version=2011-06-15";

        // Act
        var result = AwsSigV4Helper.SignRequest(
            accessKey: accessKey,
            secretKey: secretKey,
            sessionToken: null,
            region: region,
            service: service,
            method: method,
            host: host,
            path: path,
            queryString: queryString,
            headers: headers,
            body: body);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Authorization", result.Keys);
        Assert.Contains("x-amz-date", result.Keys);
        Assert.Contains("host", result.Keys);
        Assert.Contains("Content-Type", result.Keys);

        // Verify Authorization header format
        var authHeader = result["Authorization"];
        Assert.StartsWith("AWS4-HMAC-SHA256 Credential=", authHeader);
        Assert.Contains("SignedHeaders=", authHeader);
        Assert.Contains("Signature=", authHeader);
        Assert.Contains(accessKey, authHeader);
        Assert.Contains(region, authHeader);
        Assert.Contains(service, authHeader);
    }

    [Fact]
    public void SignRequest_WithSessionToken_IncludesSecurityTokenHeader()
    {
        // Arrange
        var accessKey = "AKIAIOSFODNN7EXAMPLE";
        var secretKey = "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY";
        var sessionToken = "AQoDYXdzEJr...SESSION_TOKEN";
        var region = "us-east-1";
        var service = "sts";
        var method = "POST";
        var host = "sts.amazonaws.com";
        var path = "/";
        var queryString = string.Empty;
        var headers = new Dictionary<string, string>
        {
            ["Content-Type"] = "application/x-www-form-urlencoded",
        };
        var body = "Action=GetCallerIdentity&Version=2011-06-15";

        // Act
        var result = AwsSigV4Helper.SignRequest(
            accessKey: accessKey,
            secretKey: secretKey,
            sessionToken: sessionToken,
            region: region,
            service: service,
            method: method,
            host: host,
            path: path,
            queryString: queryString,
            headers: headers,
            body: body);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("x-amz-security-token", result.Keys);
        Assert.Equal(sessionToken, result["x-amz-security-token"]);
    }

    [Fact]
    public void SignRequest_WithNullBody_HandlesCorrectly()
    {
        // Arrange
        var accessKey = "AKIAIOSFODNN7EXAMPLE";
        var secretKey = "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY";
        var region = "us-east-1";
        var service = "sts";
        var method = "GET";
        var host = "sts.amazonaws.com";
        var path = "/";
        var queryString = string.Empty;
        var headers = new Dictionary<string, string>();

        // Act
        var result = AwsSigV4Helper.SignRequest(
            accessKey: accessKey,
            secretKey: secretKey,
            sessionToken: null,
            region: region,
            service: service,
            method: method,
            host: host,
            path: path,
            queryString: queryString,
            headers: headers,
            body: null);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Authorization", result.Keys);
    }

    [Fact]
    public void SignRequest_WithEmptyPath_UsesRootPath()
    {
        // Arrange
        var accessKey = "AKIAIOSFODNN7EXAMPLE";
        var secretKey = "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY";
        var region = "us-east-1";
        var service = "sts";
        var method = "POST";
        var host = "sts.amazonaws.com";
        var path = string.Empty;
        var queryString = string.Empty;
        var headers = new Dictionary<string, string>();
        var body = "Action=GetCallerIdentity&Version=2011-06-15";

        // Act
        var result = AwsSigV4Helper.SignRequest(
            accessKey: accessKey,
            secretKey: secretKey,
            sessionToken: null,
            region: region,
            service: service,
            method: method,
            host: host,
            path: path,
            queryString: queryString,
            headers: headers,
            body: body);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Authorization", result.Keys);
    }

    [Fact]
    public void SignRequest_PreservesOriginalHeaders()
    {
        // Arrange
        var accessKey = "AKIAIOSFODNN7EXAMPLE";
        var secretKey = "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY";
        var region = "us-east-1";
        var service = "sts";
        var method = "POST";
        var host = "sts.amazonaws.com";
        var path = "/";
        var queryString = string.Empty;
        var headers = new Dictionary<string, string>
        {
            ["Content-Type"] = "application/x-www-form-urlencoded",
            ["Custom-Header"] = "CustomValue",
        };
        var body = "Action=GetCallerIdentity&Version=2011-06-15";

        // Act
        var result = AwsSigV4Helper.SignRequest(
            accessKey: accessKey,
            secretKey: secretKey,
            sessionToken: null,
            region: region,
            service: service,
            method: method,
            host: host,
            path: path,
            queryString: queryString,
            headers: headers,
            body: body);

        // Assert
        Assert.Contains("Content-Type", result.Keys);
        Assert.Equal("application/x-www-form-urlencoded", result["Content-Type"]);
        Assert.Contains("Custom-Header", result.Keys);
        Assert.Equal("CustomValue", result["Custom-Header"]);
    }

    [Fact]
    public void SignRequest_MultipleCallsWithSameParameters_GenerateDifferentSignatures()
    {
        // Arrange
        var accessKey = "AKIAIOSFODNN7EXAMPLE";
        var secretKey = "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY";
        var region = "us-east-1";
        var service = "sts";
        var method = "POST";
        var host = "sts.amazonaws.com";
        var path = "/";
        var queryString = string.Empty;
        var headers = new Dictionary<string, string>();
        var body = "Action=GetCallerIdentity&Version=2011-06-15";

        // Act - wait a bit to ensure different timestamps
        var result1 = AwsSigV4Helper.SignRequest(
            accessKey, secretKey, null, region, service, method, host, path, queryString, headers, body);

        Thread.Sleep(1100); // Wait to get different timestamp

        var result2 = AwsSigV4Helper.SignRequest(
            accessKey, secretKey, null, region, service, method, host, path, queryString, headers, body);

        // Assert - signatures should differ due to timestamp
        Assert.NotEqual(result1["Authorization"], result2["Authorization"]);
        Assert.NotEqual(result1["x-amz-date"], result2["x-amz-date"]);
    }
}
