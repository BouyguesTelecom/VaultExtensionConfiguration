// Copyright (c) Bouygues Telecom. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NSubstitute;
using Vault.Enum;
using Vault.Options;
using Vault.Options.Configuration;
using Vault.Validators;
using VaultSharp.V1.AuthMethods;
using Xunit;

namespace Vault.Tests.Validators;

/// <summary>
/// Unit tests for VaultOptionsValidator.
/// </summary>
public class VaultOptionsValidatorTests
{
    [Fact]
    public void Validate_WhenVaultOptionsIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        VaultOptions? vaultOptions = null;

        // Act & Assert
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => VaultOptionsValidator.Validate(vaultOptions!));
        Assert.Equal(nameof(vaultOptions), exception.ParamName);
    }

    [Fact]
    public void Validate_WhenVaultIsNotActivated_DoesNotThrow()
    {
        // Arrange
        var vaultOptions = new VaultOptions
        {
            IsActivated = false,
            AuthenticationType = VaultAuthenticationType.None,
        };

        // Act & Assert - should not throw
        VaultOptionsValidator.Validate(vaultOptions);
    }

    [Fact]
    public void Validate_WhenAuthenticationTypeIsNone_ThrowsInvalidOperationException()
    {
        // Arrange
        var vaultOptions = new VaultOptions
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
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => VaultOptionsValidator.Validate(vaultOptions));
        Assert.Contains("AuthenticationType", exception.Message);
        Assert.Contains("cannot be 'None'", exception.Message);
    }

    [Fact]
    public void Validate_WhenConfigurationIsNull_ThrowsInvalidOperationException()
    {
        // Arrange
        var vaultOptions = new VaultOptions
        {
            IsActivated = true,
            AuthenticationType = VaultAuthenticationType.Local,
            Configuration = null!,
        };

        // Act & Assert
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => VaultOptionsValidator.Validate(vaultOptions));
        Assert.Contains("Configuration is missing", exception.Message);
    }

    [Fact]
    public void Validate_WhenVaultUrlIsEmpty_ThrowsInvalidOperationException()
    {
        // Arrange
        var vaultOptions = new VaultOptions
        {
            IsActivated = true,
            AuthenticationType = VaultAuthenticationType.Local,
            Configuration = new VaultLocalConfiguration
            {
                VaultUrl = string.Empty,
                MountPoint = "secret",
                TokenFilePath = "/path/to/token",
            },
        };

        // Act & Assert
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => VaultOptionsValidator.Validate(vaultOptions));
        Assert.Contains("VaultUrl", exception.Message);
        Assert.Contains("missing", exception.Message);
    }

    [Fact]
    public void Validate_WhenVaultUrlIsWhitespace_ThrowsInvalidOperationException()
    {
        // Arrange
        var vaultOptions = new VaultOptions
        {
            IsActivated = true,
            AuthenticationType = VaultAuthenticationType.Local,
            Configuration = new VaultLocalConfiguration
            {
                VaultUrl = "   ",
                MountPoint = "secret",
                TokenFilePath = "/path/to/token",
            },
        };

        // Act & Assert
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => VaultOptionsValidator.Validate(vaultOptions));
        Assert.Contains("VaultUrl", exception.Message);
        Assert.Contains("missing", exception.Message);
    }

    [Fact]
    public void Validate_WhenMountPointIsEmpty_ThrowsInvalidOperationException()
    {
        // Arrange
        var vaultOptions = new VaultOptions
        {
            IsActivated = true,
            AuthenticationType = VaultAuthenticationType.Local,
            Configuration = new VaultLocalConfiguration
            {
                VaultUrl = "https://vault.example.com",
                MountPoint = string.Empty,
                TokenFilePath = "/path/to/token",
            },
        };

        // Act & Assert
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => VaultOptionsValidator.Validate(vaultOptions));
        Assert.Contains("MountPoint", exception.Message);
        Assert.Contains("missing", exception.Message);
    }

    [Fact]
    public void Validate_WhenMountPointIsNull_ThrowsInvalidOperationException()
    {
        // Arrange
        var vaultOptions = new VaultOptions
        {
            IsActivated = true,
            AuthenticationType = VaultAuthenticationType.Local,
            Configuration = new VaultLocalConfiguration
            {
                VaultUrl = "https://vault.example.com",
                MountPoint = null!,
                TokenFilePath = "/path/to/token",
            },
        };

        // Act & Assert
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => VaultOptionsValidator.Validate(vaultOptions));
        Assert.Contains("MountPoint", exception.Message);
        Assert.Contains("missing", exception.Message);
    }

    [Fact]
    public void Validate_WhenLocalAuthWithValidConfiguration_DoesNotThrow()
    {
        // Arrange
        var vaultOptions = new VaultOptions
        {
            IsActivated = true,
            AuthenticationType = VaultAuthenticationType.Local,
            Configuration = new VaultLocalConfiguration
            {
                VaultUrl = "https://vault.example.com",
                MountPoint = "secret",
                TokenFilePath = "%USERPROFILE%\\.vault-token",
            },
        };

        // Act & Assert - should not throw
        VaultOptionsValidator.Validate(vaultOptions);
    }

    [Fact]
    public void Validate_WhenLocalAuthWithWrongConfigurationType_ThrowsInvalidOperationException()
    {
        // Arrange
        var vaultOptions = new VaultOptions
        {
            IsActivated = true,
            AuthenticationType = VaultAuthenticationType.Local,
            Configuration = new VaultDefaultConfiguration
            {
                VaultUrl = "https://vault.example.com",
                MountPoint = "secret",
            },
        };

        // Act & Assert
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => VaultOptionsValidator.Validate(vaultOptions));
        Assert.Contains("VaultLocalConfiguration", exception.Message);
        Assert.Contains("Local authentication", exception.Message);
    }

    [Fact]
    public void Validate_WhenLocalAuthWithEmptyTokenFilePath_ThrowsInvalidOperationException()
    {
        // Arrange
        var vaultOptions = new VaultOptions
        {
            IsActivated = true,
            AuthenticationType = VaultAuthenticationType.Local,
            Configuration = new VaultLocalConfiguration
            {
                VaultUrl = "https://vault.example.com",
                MountPoint = "secret",
                TokenFilePath = string.Empty,
            },
        };

        // Act & Assert
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => VaultOptionsValidator.Validate(vaultOptions));
        Assert.Contains("TokenFilePath", exception.Message);
        Assert.Contains("missing", exception.Message);
    }

    [Fact]
    public void Validate_WhenLocalAuthWithNullTokenFilePath_ThrowsInvalidOperationException()
    {
        // Arrange
        var vaultOptions = new VaultOptions
        {
            IsActivated = true,
            AuthenticationType = VaultAuthenticationType.Local,
            Configuration = new VaultLocalConfiguration
            {
                VaultUrl = "https://vault.example.com",
                MountPoint = "secret",
                TokenFilePath = null!,
            },
        };

        // Act & Assert
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => VaultOptionsValidator.Validate(vaultOptions));
        Assert.Contains("TokenFilePath", exception.Message);
        Assert.Contains("missing", exception.Message);
    }

    [Fact]
    public void Validate_WhenAwsIamAuthWithValidConfiguration_DoesNotThrow()
    {
        // Arrange
        var vaultOptions = new VaultOptions
        {
            IsActivated = true,
            AuthenticationType = VaultAuthenticationType.AWS_IAM,
            Configuration = new VaultAwsIAMConfiguration
            {
                VaultUrl = "https://vault.example.com",
                MountPoint = "secret",
                AwsIamRoleName = "my-role",
                Environment = "prod",
            },
        };

        // Act & Assert - should not throw
        VaultOptionsValidator.Validate(vaultOptions);
    }

    [Fact]
    public void Validate_WhenAwsIamAuthWithWrongConfigurationType_ThrowsInvalidOperationException()
    {
        // Arrange
        var vaultOptions = new VaultOptions
        {
            IsActivated = true,
            AuthenticationType = VaultAuthenticationType.AWS_IAM,
            Configuration = new VaultDefaultConfiguration
            {
                VaultUrl = "https://vault.example.com",
                MountPoint = "secret",
            },
        };

        // Act & Assert
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => VaultOptionsValidator.Validate(vaultOptions));
        Assert.Contains("VaultAwsIAMConfiguration", exception.Message);
        Assert.Contains("AWS_IAM authentication", exception.Message);
    }

    [Fact]
    public void Validate_WhenAwsIamAuthWithMinimalConfiguration_DoesNotThrow()
    {
        // Arrange
        var vaultOptions = new VaultOptions
        {
            IsActivated = true,
            AuthenticationType = VaultAuthenticationType.AWS_IAM,
            Configuration = new VaultAwsIAMConfiguration
            {
                VaultUrl = "https://vault.example.com",
                MountPoint = "secret",
            },
        };

        // Act & Assert - should not throw
        VaultOptionsValidator.Validate(vaultOptions);
    }

    [Fact]
    public void Validate_WhenCustomAuthWithValidConfiguration_DoesNotThrow()
    {
        // Arrange
        IAuthMethodInfo mockAuthMethod = Substitute.For<IAuthMethodInfo>();

        var vaultOptions = new VaultOptions
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

        // Act & Assert - should not throw
        VaultOptionsValidator.Validate(vaultOptions);
    }

    [Fact]
    public void Validate_WhenCustomAuthWithWrongConfigurationType_ThrowsInvalidOperationException()
    {
        // Arrange
        var vaultOptions = new VaultOptions
        {
            IsActivated = true,
            AuthenticationType = VaultAuthenticationType.Custom,
            Configuration = new VaultDefaultConfiguration
            {
                VaultUrl = "https://vault.example.com",
                MountPoint = "secret",
            },
        };

        // Act & Assert
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => VaultOptionsValidator.Validate(vaultOptions));
        Assert.Contains("VaultCustomConfiguration", exception.Message);
        Assert.Contains("Custom authentication", exception.Message);
    }

    [Fact]
    public void Validate_WhenCustomAuthWithNullFactory_ThrowsInvalidOperationException()
    {
        // Arrange
        var vaultOptions = new VaultOptions
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
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => VaultOptionsValidator.Validate(vaultOptions));
        Assert.Contains("AuthMethodFactory", exception.Message);
        Assert.Contains("must be provided", exception.Message);
    }
}
