using FluentAssertions;
using FluentValidation.TestHelper;
using Vault.Options;
using Vault.Validators;
using Xunit;

namespace Vault.Tests.Validators;

/// <summary>
/// Tests unitaires pour VaultAwsConfigurationValidator.
/// </summary>
public class VaultAwsConfigurationValidatorTests
{
    private readonly VaultAwsConfigurationValidator _validator;

    public VaultAwsConfigurationValidatorTests()
    {
        _validator = new VaultAwsConfigurationValidator();
    }

    #region Tests Environment (toujours requis)

    [Fact]
    public void Should_Have_Error_When_Environment_Is_Empty()
    {
        // Arrange
        var config = new VaultAwsConfiguration
        {
            VaultUrl = "https://vault.example.com",
            MountPoint = "kv",
            Environment = string.Empty,
            AwsAuthMountPoint = "aws"
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Environment)
            .WithErrorMessage(VaultOptionsResources.Environment_Not_Empty);
    }

    [Fact]
    public void Should_Have_Error_When_Environment_Is_Null()
    {
        // Arrange
        var config = new VaultAwsConfiguration
        {
            VaultUrl = "https://vault.example.com",
            MountPoint = "kv",
            Environment = null!,
            AwsAuthMountPoint = "aws"
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Environment);
    }

    [Fact]
    public void Should_Not_Have_Error_When_All_Required_Fields_Are_Valid()
    {
        // Arrange
        var config = new VaultAwsConfiguration
        {
            VaultUrl = "https://vault.example.com",
            MountPoint = "kv",
            Environment = "thomas",
            AwsAuthMountPoint = "aws"
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region Tests AwsIamRoleName (optionnel)

    [Fact]
    public void Should_Not_Have_Error_When_AwsIamRoleName_Is_Provided()
    {
        // Arrange
        var config = new VaultAwsConfiguration
        {
            VaultUrl = "https://vault.example.com",
            MountPoint = "kv",
            Environment = "production",
            AwsIamRoleName = "my-custom-role",
            AwsAuthMountPoint = "aws"
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Not_Have_Error_When_AwsIamRoleName_Is_Null()
    {
        // Arrange - AwsIamRoleName est optionnel
        var config = new VaultAwsConfiguration
        {
            VaultUrl = "https://vault.example.com",
            MountPoint = "kv",
            Environment = "thomas",
            AwsIamRoleName = null,
            AwsAuthMountPoint = "aws"
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Not_Have_Error_When_AwsIamRoleName_Is_Empty()
    {
        // Arrange - AwsIamRoleName est optionnel
        var config = new VaultAwsConfiguration
        {
            VaultUrl = "https://vault.example.com",
            MountPoint = "kv",
            Environment = "thomas",
            AwsIamRoleName = string.Empty,
            AwsAuthMountPoint = "aws"
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region Tests AwsAuthMountPoint (optionnel)

    [Fact]
    public void Should_Not_Have_Error_When_AwsAuthMountPoint_Is_Default()
    {
        // Arrange
        var config = new VaultAwsConfiguration
        {
            VaultUrl = "https://vault.example.com",
            MountPoint = "kv",
            Environment = "thomas",
            AwsAuthMountPoint = "aws" // Valeur par défaut
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Not_Have_Error_When_AwsAuthMountPoint_Is_Custom()
    {
        // Arrange
        var config = new VaultAwsConfiguration
        {
            VaultUrl = "https://vault.example.com",
            MountPoint = "kv",
            Environment = "thomas",
            AwsAuthMountPoint = "custom-aws-auth"
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region Tests validations héritées

    [Fact]
    public void Should_Validate_Base_Configuration_Properties()
    {
        // Arrange - Missing VaultUrl
        var config = new VaultAwsConfiguration
        {
            VaultUrl = string.Empty,
            MountPoint = "kv",
            Environment = "thomas",
            AwsAuthMountPoint = "aws"
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.VaultUrl);
    }

    [Fact]
    public void Should_Have_Multiple_Errors_When_All_Required_Fields_Are_Invalid()
    {
        // Arrange
        var config = new VaultAwsConfiguration
        {
            VaultUrl = string.Empty,
            MountPoint = string.Empty,
            Environment = string.Empty,
            AwsAuthMountPoint = "aws"
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.VaultUrl);
        result.ShouldHaveValidationErrorFor(x => x.MountPoint);
        result.ShouldHaveValidationErrorFor(x => x.Environment);
        result.Errors.Should().HaveCount(3); // VaultUrl + MountPoint + Environment
    }

    #endregion

    #region Tests scénarios réels

    [Theory]
    [InlineData("production", "HELLOWORLD-FORMATION")]
    [InlineData("dev", "MY-APP")]
    [InlineData("thomas", "TEST-PROJECT")]
    public void Should_Accept_Valid_Configurations(string environment, string mountPoint)
    {
        // Arrange
        var config = new VaultAwsConfiguration
        {
            VaultUrl = "https://vault.example.com",
            MountPoint = mountPoint,
            Environment = environment,
            AwsAuthMountPoint = "aws"
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Accept_Full_Configuration_With_All_Properties()
    {
        // Arrange
        var config = new VaultAwsConfiguration
        {
            VaultUrl = "https://vault.production.com:8200",
            MountPoint = "HELLOWORLD-FORMATION",
            AwsIamRoleName = "HELLOWORLD-FORMATION-prod-role",
            Environment = "production",
            AwsAuthMountPoint = "aws",
            IgnoreSslErrors = false
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Accept_Minimal_Configuration()
    {
        // Arrange - Configuration minimale : VaultUrl, MountPoint, Environment
        var config = new VaultAwsConfiguration
        {
            VaultUrl = "https://vault.example.com",
            MountPoint = "kv",
            Environment = "dev"
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion
}
