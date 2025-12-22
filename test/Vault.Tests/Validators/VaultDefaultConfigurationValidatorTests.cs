// Copyright (c) Bouygues Telecom. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using FluentValidation.TestHelper;
using Vault.Options;
using Vault.Validators;
using Xunit;

namespace Vault.Tests.Validators;

/// <summary>
/// Tests unitaires pour VaultDefaultConfigurationValidator.
/// </summary>
public class VaultDefaultConfigurationValidatorTests
{
    private readonly VaultDefaultConfigurationValidator validator;

    public VaultDefaultConfigurationValidatorTests()
    {
        this.validator = new VaultDefaultConfigurationValidator();
    }

    [Fact]
    public void Should_Have_Error_When_VaultUrl_Is_Empty()
    {
        // Arrange
        var config = new VaultDefaultConfiguration
        {
            VaultUrl = string.Empty,
            MountPoint = "kv",
        };

        // Act
        TestValidationResult<VaultDefaultConfiguration> result = this.validator.TestValidate(config);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.VaultUrl)
            .WithErrorMessage(VaultOptionsResources.VaultUrl_Not_Empty);
    }

    [Fact]
    public void Should_Have_Error_When_VaultUrl_Is_Null()
    {
        // Arrange
        var config = new VaultDefaultConfiguration
        {
            VaultUrl = null!,
            MountPoint = "kv",
        };

        // Act
        TestValidationResult<VaultDefaultConfiguration> result = this.validator.TestValidate(config);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.VaultUrl);
    }

    [Fact]
    public void Should_Have_Error_When_MountPoint_Is_Empty()
    {
        // Arrange
        var config = new VaultDefaultConfiguration
        {
            VaultUrl = "https://vault.example.com",
            MountPoint = string.Empty,
        };

        // Act
        TestValidationResult<VaultDefaultConfiguration> result = this.validator.TestValidate(config);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MountPoint)
            .WithErrorMessage(VaultOptionsResources.MountPoint_Not_Empty);
    }

    [Fact]
    public void Should_Have_Error_When_MountPoint_Is_Null()
    {
        // Arrange
        var config = new VaultDefaultConfiguration
        {
            VaultUrl = "https://vault.example.com",
            MountPoint = null!,
        };

        // Act
        TestValidationResult<VaultDefaultConfiguration> result = this.validator.TestValidate(config);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MountPoint);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Configuration_Is_Valid()
    {
        // Arrange
        var config = new VaultDefaultConfiguration
        {
            VaultUrl = "https://vault.example.com",
            MountPoint = "kv",
        };

        // Act
        TestValidationResult<VaultDefaultConfiguration> result = this.validator.TestValidate(config);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("https://vault.example.com", "kv")]
    [InlineData("http://localhost:8200", "secret")]
    [InlineData("https://vault.production.com:8200", "HELLOWORLD-FORMATION")]
    public void Should_Accept_Valid_Configurations(string vaultUrl, string mountPoint)
    {
        // Arrange
        var config = new VaultDefaultConfiguration
        {
            VaultUrl = vaultUrl,
            MountPoint = mountPoint,
        };

        // Act
        TestValidationResult<VaultDefaultConfiguration> result = this.validator.TestValidate(config);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Have_Multiple_Errors_When_Both_Required_Fields_Are_Empty()
    {
        // Arrange
        var config = new VaultDefaultConfiguration
        {
            VaultUrl = string.Empty,
            MountPoint = string.Empty,
        };

        // Act
        TestValidationResult<VaultDefaultConfiguration> result = this.validator.TestValidate(config);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.VaultUrl);
        result.ShouldHaveValidationErrorFor(x => x.MountPoint);
        result.Errors.Should().HaveCount(2);
    }
}
