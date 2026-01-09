// Copyright (c) Bouygues Telecom. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NSubstitute;
using Vault.Enum;
using Vault.Helpers;
using Vault.Options;
using Vault.Options.Configuration;
using VaultSharp.V1.AuthMethods;
using VaultSharp.V1.AuthMethods.Token;
using Xunit;

namespace Vault.Tests.Helpers;

/// <summary>
/// Unit tests for VaultHelpers.
/// </summary>
public class VaultHelpersTests
{
    [Fact]
    public void GetConfiguration_ReturnsConfiguration()
    {
        // Arrange
        var config = new VaultLocalConfiguration
        {
            VaultUrl = "https://vault.example.com",
            MountPoint = "secret",
            TokenFilePath = "/path/to/token",
        };
        var options = new VaultOptions
        {
            IsActivated = true,
            AuthenticationType = VaultAuthenticationType.Local,
            Configuration = config,
        };

        // Act
        VaultDefaultConfiguration result = options.GetConfiguration();

        // Assert
        Assert.Same(config, result);
    }

    [Fact]
    public void CreateAuthMethod_WithNoneType_ThrowsNotSupportedException()
    {
        // Arrange
        var options = new VaultOptions
        {
            IsActivated = true,
            AuthenticationType = VaultAuthenticationType.None,
            Configuration = new VaultDefaultConfiguration
            {
                VaultUrl = "https://vault.example.com",
                MountPoint = "secret",
            },
        };

        // Act & Assert
        NotSupportedException exception = Assert.Throws<NotSupportedException>(() => options.CreateAuthMethod());
        Assert.Contains("None", exception.Message);
        Assert.Contains("not supported", exception.Message);
    }

    [Fact]
    public void CreateAuthMethod_WithCustomTypeAndNullFactory_ThrowsInvalidOperationException()
    {
        // Arrange
        var options = new VaultOptions
        {
            IsActivated = true,
            AuthenticationType = VaultAuthenticationType.Custom,
            Configuration = new VaultCustomConfiguration
            {
                VaultUrl = "https://vault.example.com",
                MountPoint = "secret",
                AuthMethodFactory = null,
            },
        };

        // Act & Assert
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => options.CreateAuthMethod());
        Assert.Contains("AuthMethodFactory", exception.Message);
        Assert.Contains("must be provided", exception.Message);
    }

    [Fact]
    public void CreateAuthMethod_WithCustomTypeAndValidFactory_ReturnsAuthMethod()
    {
        // Arrange
        IAuthMethodInfo mockAuthMethod = Substitute.For<IAuthMethodInfo>();
        var options = new VaultOptions
        {
            IsActivated = true,
            AuthenticationType = VaultAuthenticationType.Custom,
            Configuration = new VaultCustomConfiguration
            {
                VaultUrl = "https://vault.example.com",
                MountPoint = "secret",
                AuthMethodFactory = () => mockAuthMethod,
            },
        };

        // Act
        IAuthMethodInfo? result = options.CreateAuthMethod();

        // Assert
        Assert.Same(mockAuthMethod, result);
    }

    [Fact]
    public void CreateAuthMethod_WithCustomTypeAndFactoryThrowsException_ThrowsInvalidOperationException()
    {
        // Arrange
        var options = new VaultOptions
        {
            IsActivated = true,
            AuthenticationType = VaultAuthenticationType.Custom,
            Configuration = new VaultCustomConfiguration
            {
                VaultUrl = "https://vault.example.com",
                MountPoint = "secret",
                AuthMethodFactory = () => throw new Exception("Factory error"),
            },
        };

        // Act & Assert
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => options.CreateAuthMethod());
        Assert.Contains("Error creating custom authentication method", exception.Message);
        Assert.Contains("Factory error", exception.Message);
    }
}
