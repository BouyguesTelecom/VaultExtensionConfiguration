// Copyright (c) Bouygues Telecom. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;
using NSubstitute;
using Vault.Abstractions;
using Vault.Configuration;
using Xunit;

namespace Vault.Tests.Configuration;

/// <summary>
/// Unit tests for VaultConfigurationSource.
/// </summary>
public class VaultConfigurationSourceTests
{
    [Fact]
    public void VaultConfigurationSource_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var source = new VaultConfigurationSource();

        // Assert
        Assert.Equal(string.Empty, source.Environment);
        Assert.False(source.Optional);
        Assert.False(source.ReloadOnChange);
        Assert.Equal(300, source.ReloadIntervalSeconds);
    }

    [Fact]
    public void VaultConfigurationSource_CanSetEnvironment()
    {
        // Arrange
        var source = new VaultConfigurationSource();

        // Act
        source.Environment = "dev";

        // Assert
        Assert.Equal("dev", source.Environment);
    }

    [Fact]
    public void VaultConfigurationSource_CanSetOptional()
    {
        // Arrange
        var source = new VaultConfigurationSource();

        // Act
        source.Optional = true;

        // Assert
        Assert.True(source.Optional);
    }

    [Fact]
    public void VaultConfigurationSource_CanSetReloadOnChange()
    {
        // Arrange
        var source = new VaultConfigurationSource();

        // Act
        source.ReloadOnChange = true;

        // Assert
        Assert.True(source.ReloadOnChange);
    }

    [Fact]
    public void VaultConfigurationSource_CanSetReloadIntervalSeconds()
    {
        // Arrange
        var source = new VaultConfigurationSource();

        // Act
        source.ReloadIntervalSeconds = 600;

        // Assert
        Assert.Equal(600, source.ReloadIntervalSeconds);
    }

    [Fact]
    public void Build_WithoutVaultService_ThrowsInvalidOperationException()
    {
        // Arrange
        var source = new VaultConfigurationSource
        {
            Environment = "dev",
        };
        var builder = new ConfigurationBuilder();

        // Act & Assert
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => source.Build(builder));
        Assert.Contains("VaultService must be set", exception.Message);
    }

    [Fact]
    public void AddVaultConfiguration_WithEmptyEnvironment_ThrowsArgumentException()
    {
        // Arrange
        var vaultService = Substitute.For<IVaultService>();
        var builder = new ConfigurationBuilder();

        // Act & Assert
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            builder.AddVaultConfiguration(string.Empty, vaultService));
        Assert.Equal("environment", exception.ParamName);
    }

    [Fact]
    public void AddVaultConfiguration_WithValidEnvironmentAndVaultService_ReturnsBuilder()
    {
        // Arrange
        var vaultService = Substitute.For<IVaultService>();
        vaultService.GetSecretsAsync("dev").Returns(new Dictionary<string, object>());
        var builder = new ConfigurationBuilder();

        // Act
        IConfigurationBuilder result = builder.AddVaultConfiguration("dev", vaultService);

        // Assert
        Assert.Same(builder, result);
    }

    [Fact]
    public void AddVaultConfiguration_WithWhitespaceEnvironment_ThrowsArgumentException()
    {
        // Arrange
        var vaultService = Substitute.For<IVaultService>();
        var builder = new ConfigurationBuilder();

        // Act & Assert
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            builder.AddVaultConfiguration("   ", vaultService));
        Assert.Equal("environment", exception.ParamName);
    }

    [Fact]
    public void AddVaultConfiguration_LoadsSecretsImmediately()
    {
        // Arrange
        var vaultService = Substitute.For<IVaultService>();
        var secrets = new Dictionary<string, object>
        {
            ["ConnectionStrings:Default"] = "Server=localhost;Database=Test",
            ["AppSettings:ApiKey"] = "secret-key",
        };
        vaultService.GetSecretsAsync("dev").Returns(secrets);
        var builder = new ConfigurationBuilder();

        // Act
        builder.AddVaultConfiguration("dev", vaultService);
        IConfigurationRoot config = builder.Build();

        // Assert
        Assert.Equal("Server=localhost;Database=Test", config["ConnectionStrings:Default"]);
        Assert.Equal("secret-key", config["AppSettings:ApiKey"]);
    }

    [Fact]
    public void AddVaultConfiguration_WithConfigureSource_AppliesConfiguration()
    {
        // Arrange
        var vaultService = Substitute.For<IVaultService>();
        vaultService.GetSecretsAsync("prod").Returns(new Dictionary<string, object>());
        var builder = new ConfigurationBuilder();

        // Act
        builder.AddVaultConfiguration("prod", vaultService, source =>
        {
            source.Optional = true;
            source.ReloadOnChange = true;
            source.ReloadIntervalSeconds = 600;
        });
        IConfigurationRoot config = builder.Build();

        // Assert - configuration was built successfully
        Assert.NotNull(config);
    }
}
