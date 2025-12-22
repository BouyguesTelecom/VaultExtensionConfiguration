// Copyright (c) Bouygues Telecom. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using FluentValidation.TestHelper;
using NSubstitute;
using Vault.Options;
using Vault.Validators;
using VaultSharp.V1.AuthMethods;
using Xunit;

namespace Vault.Tests.Validators;

/// <summary>
/// Tests unitaires pour VaultOptionsValidator.
/// </summary>
public class VaultOptionsValidatorTests
{
    private readonly VaultOptionsValidator validator;

    public VaultOptionsValidatorTests()
    {
        this.validator = new VaultOptionsValidator();
    }

    [Fact]
    public void Should_Have_Error_When_AuthenticationType_Is_None()
    {
        // Arrange
        var options = new VaultOptions
        {
            AuthenticationType = VaultAuthenticationType.None,
            Configuration = new VaultDefaultConfiguration
            {
                VaultUrl = "https://vault.example.com",
                MountPoint = "kv",
            },
        };

        // Act
        TestValidationResult<VaultOptions> result = this.validator.TestValidate(options);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AuthenticationType)
            .WithErrorMessage(VaultOptionsResources.VaultAuthenticationType_Not_In_Range);
    }

    [Fact]
    public void Should_Have_Error_When_Configuration_Is_Null()
    {
        // Arrange
        var options = new VaultOptions
        {
            AuthenticationType = VaultAuthenticationType.Local,
            Configuration = null,
        };

        // Act
        TestValidationResult<VaultOptions> result = this.validator.TestValidate(options);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Configuration)
            .WithErrorMessage(VaultOptionsResources.Vault_Configuration_Undefined);
    }

    [Fact]
    public void Should_Have_Error_When_Local_Auth_With_Wrong_Configuration_Type()
    {
        // Arrange
        var options = new VaultOptions
        {
            AuthenticationType = VaultAuthenticationType.Local,
            Configuration = new VaultAwsConfiguration // Mauvais type
            {
                VaultUrl = "https://vault.example.com",
                MountPoint = "kv",
                Environment = "thomas",
            },
        };

        // Act
        TestValidationResult<VaultOptions> result = this.validator.TestValidate(options);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Configuration)
            .WithErrorMessage(VaultOptionsResources.Configuration_Local_CheckType);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Local_Auth_Is_Valid()
    {
        // Arrange
        var options = new VaultOptions
        {
            AuthenticationType = VaultAuthenticationType.Local,
            Configuration = new VaultLocalConfiguration
            {
                VaultUrl = "https://vault.example.com",
                MountPoint = "kv",
                TokenFilePath = "%USERPROFILE%\\.vault-token",
            },
        };

        // Act
        TestValidationResult<VaultOptions> result = this.validator.TestValidate(options);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Not_Have_Error_When_Local_Auth_Without_TokenFilePath()
    {
        // Arrange - TokenFilePath n'est plus requis
        var options = new VaultOptions
        {
            AuthenticationType = VaultAuthenticationType.Local,
            Configuration = new VaultLocalConfiguration
            {
                VaultUrl = "https://vault.example.com",
                MountPoint = "kv",
                TokenFilePath = string.Empty,
            },
        };

        // Act
        TestValidationResult<VaultOptions> result = this.validator.TestValidate(options);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Have_Error_When_AWS_Auth_With_Wrong_Configuration_Type()
    {
        // Arrange
        var options = new VaultOptions
        {
            AuthenticationType = VaultAuthenticationType.AWS_IAM,
            Configuration = new VaultLocalConfiguration // Mauvais type
            {
                VaultUrl = "https://vault.example.com",
                MountPoint = "kv",
            },
        };

        // Act
        TestValidationResult<VaultOptions> result = this.validator.TestValidate(options);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Configuration)
            .WithErrorMessage(VaultOptionsResources.Configuration_AWS_IAM_CheckType);
    }

    [Fact]
    public void Should_Not_Have_Error_When_AWS_Auth_Is_Valid_With_Explicit_Role()
    {
        // Arrange
        var options = new VaultOptions
        {
            AuthenticationType = VaultAuthenticationType.AWS_IAM,
            Configuration = new VaultAwsConfiguration
            {
                VaultUrl = "https://vault.example.com",
                MountPoint = "kv",
                Environment = "thomas",
                AwsIamRoleName = "my-custom-role",
                AwsAuthMountPoint = "aws",
            },
        };

        // Act
        TestValidationResult<VaultOptions> result = this.validator.TestValidate(options);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Not_Have_Error_When_AWS_Auth_Is_Valid_Without_Role()
    {
        // Arrange - AwsIamRoleName n'est plus requis
        var options = new VaultOptions
        {
            AuthenticationType = VaultAuthenticationType.AWS_IAM,
            Configuration = new VaultAwsConfiguration
            {
                VaultUrl = "https://vault.example.com",
                MountPoint = "HELLOWORLD-FORMATION",
                Environment = "thomas",
                AwsAuthMountPoint = "aws",
            },
        };

        // Act
        TestValidationResult<VaultOptions> result = this.validator.TestValidate(options);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Have_Error_When_AWS_Auth_Missing_Environment()
    {
        // Arrange - Environment est toujours requis
        var options = new VaultOptions
        {
            AuthenticationType = VaultAuthenticationType.AWS_IAM,
            Configuration = new VaultAwsConfiguration
            {
                VaultUrl = "https://vault.example.com",
                MountPoint = "HELLOWORLD-FORMATION",
                Environment = string.Empty, // Requis
                AwsAuthMountPoint = "aws",
            },
        };

        // Act
        TestValidationResult<VaultOptions> result = this.validator.TestValidate(options);

        // Assert
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Environment"));
    }

    [Fact]
    public void Should_Have_Error_When_Custom_Auth_Without_CustomAuthMethodInfo()
    {
        // Arrange
        var options = new VaultOptions
        {
            AuthenticationType = VaultAuthenticationType.Custom,
            Configuration = new VaultDefaultConfiguration
            {
                VaultUrl = "https://vault.example.com",
                MountPoint = "kv",
            },
            CustomAuthMethodInfo = null,
        };

        // Act
        TestValidationResult<VaultOptions> result = this.validator.TestValidate(options);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CustomAuthMethodInfo)
            .WithErrorMessage(VaultOptionsResources.Vault_CustomAuthMethodInfo_Undefined);
    }

    [Fact]
    public void Should_Have_Error_When_Custom_Auth_With_Wrong_Configuration_Type()
    {
        // Arrange
        IAuthMethodInfo mockAuthMethod = Substitute.For<IAuthMethodInfo>();

        var options = new VaultOptions
        {
            AuthenticationType = VaultAuthenticationType.Custom,
            Configuration = new VaultLocalConfiguration // Doit être VaultDefaultConfiguration
            {
                VaultUrl = "https://vault.example.com",
                MountPoint = "kv",
            },
            CustomAuthMethodInfo = mockAuthMethod,
        };

        // Act
        TestValidationResult<VaultOptions> result = this.validator.TestValidate(options);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Configuration)
            .WithErrorMessage(VaultOptionsResources.Configuration_CUSTOM_CheckType);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Custom_Auth_Is_Valid()
    {
        // Arrange
        IAuthMethodInfo mockAuthMethod = Substitute.For<IAuthMethodInfo>();

        var options = new VaultOptions
        {
            AuthenticationType = VaultAuthenticationType.Custom,
            Configuration = new VaultDefaultConfiguration
            {
                VaultUrl = "https://vault.example.com",
                MountPoint = "kv",
            },
            CustomAuthMethodInfo = mockAuthMethod,
        };

        // Act
        TestValidationResult<VaultOptions> result = this.validator.TestValidate(options);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Have_Error_When_Custom_Auth_With_AwsConfiguration()
    {
        // Arrange
        IAuthMethodInfo mockAuthMethod = Substitute.For<IAuthMethodInfo>();

        var options = new VaultOptions
        {
            AuthenticationType = VaultAuthenticationType.Custom,
            Configuration = new VaultAwsConfiguration // Ne doit pas être un type dérivé
            {
                VaultUrl = "https://vault.example.com",
                MountPoint = "kv",
                Environment = "thomas",
            },
            CustomAuthMethodInfo = mockAuthMethod,
        };

        // Act
        TestValidationResult<VaultOptions> result = this.validator.TestValidate(options);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Configuration);
    }

    [Fact]
    public void Should_Validate_Complete_Local_Configuration()
    {
        // Arrange
        var options = new VaultOptions
        {
            AuthenticationType = VaultAuthenticationType.Local,
            Configuration = new VaultLocalConfiguration
            {
                VaultUrl = "https://vault.production.com:8200",
                MountPoint = "HELLOWORLD-FORMATION",
                TokenFilePath = "C:\\Users\\thomas\\.vault-token",
                IgnoreSslErrors = false,
            },
        };

        // Act
        TestValidationResult<VaultOptions> result = this.validator.TestValidate(options);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Validate_Complete_AWS_Configuration()
    {
        // Arrange
        var options = new VaultOptions
        {
            AuthenticationType = VaultAuthenticationType.AWS_IAM,
            Configuration = new VaultAwsConfiguration
            {
                VaultUrl = "https://vault.production.com:8200",
                MountPoint = "HELLOWORLD-FORMATION",
                Environment = "production",
                AwsAuthMountPoint = "aws",
                IgnoreSslErrors = false,
            },
        };

        // Act
        TestValidationResult<VaultOptions> result = this.validator.TestValidate(options);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Collect_Multiple_Validation_Errors()
    {
        // Arrange
        var options = new VaultOptions
        {
            AuthenticationType = VaultAuthenticationType.Local,
            Configuration = new VaultLocalConfiguration
            {
                VaultUrl = string.Empty,
                MountPoint = string.Empty,
                TokenFilePath = string.Empty,
            },
        };

        // Act
        TestValidationResult<VaultOptions> result = this.validator.TestValidate(options);

        // Assert
        result.Errors.Should().HaveCountGreaterThan(0);
        result.IsValid.Should().BeFalse();
    }
}
