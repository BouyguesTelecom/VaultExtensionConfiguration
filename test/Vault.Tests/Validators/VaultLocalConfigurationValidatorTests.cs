using FluentAssertions;
using FluentValidation.TestHelper;
using Vault.Options;
using Vault.Validators;
using Xunit;

namespace Vault.Tests.Validators;

/// <summary>
/// Tests unitaires pour VaultLocalConfigurationValidator.
/// </summary>
public class VaultLocalConfigurationValidatorTests
{
    private readonly VaultLocalConfigurationValidator _validator;

    public VaultLocalConfigurationValidatorTests()
    {
        _validator = new VaultLocalConfigurationValidator();
    }

    [Fact]
    public void Should_Not_Have_Error_When_Configuration_Is_Valid()
    {
        // Arrange
        var config = new VaultLocalConfiguration
        {
            VaultUrl = "https://vault.example.com",
            MountPoint = "kv",
            TokenFilePath = "%USERPROFILE%\\.vault-token"
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Not_Have_Error_When_TokenFilePath_Is_Empty()
    {
        // Arrange - TokenFilePath n'est plus validé
        var config = new VaultLocalConfiguration
        {
            VaultUrl = "https://vault.example.com",
            MountPoint = "kv",
            TokenFilePath = string.Empty
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("%USERPROFILE%\\.vault-token")]
    [InlineData("C:\\Users\\john\\.vault-token")]
    [InlineData("/home/user/.vault-token")]
    [InlineData("~/.vault-token")]
    [InlineData("")] // TokenFilePath n'est plus requis
    public void Should_Accept_Any_Token_File_Path(string tokenFilePath)
    {
        // Arrange
        var config = new VaultLocalConfiguration
        {
            VaultUrl = "https://vault.example.com",
            MountPoint = "kv",
            TokenFilePath = tokenFilePath
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Validate_Base_Configuration_Properties()
    {
        // Arrange - Missing VaultUrl and MountPoint
        var config = new VaultLocalConfiguration
        {
            VaultUrl = string.Empty,
            MountPoint = string.Empty,
            TokenFilePath = "%USERPROFILE%\\.vault-token"
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.VaultUrl);
        result.ShouldHaveValidationErrorFor(x => x.MountPoint);
    }

    [Fact]
    public void Should_Have_Errors_When_Base_Required_Fields_Are_Empty()
    {
        // Arrange - TokenFilePath n'est plus validé, seulement les champs de base
        var config = new VaultLocalConfiguration
        {
            VaultUrl = string.Empty,
            MountPoint = string.Empty,
            TokenFilePath = string.Empty
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.VaultUrl);
        result.ShouldHaveValidationErrorFor(x => x.MountPoint);
        result.Errors.Should().HaveCount(2); // Seulement VaultUrl et MountPoint
    }

    [Fact]
    public void Should_Accept_Configuration_With_IgnoreSslErrors_True()
    {
        // Arrange
        var config = new VaultLocalConfiguration
        {
            VaultUrl = "https://vault.example.com",
            MountPoint = "kv",
            TokenFilePath = "%USERPROFILE%\\.vault-token",
            IgnoreSslErrors = true
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Accept_Configuration_With_IgnoreSslErrors_False()
    {
        // Arrange
        var config = new VaultLocalConfiguration
        {
            VaultUrl = "https://vault.example.com",
            MountPoint = "kv",
            TokenFilePath = "%USERPROFILE%\\.vault-token",
            IgnoreSslErrors = false
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
