// Copyright (c) Bouygues Telecom. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;
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
    public void Build_WithEmptyEnvironment_ThrowsInvalidOperationException()
    {
        // Arrange
        var source = new VaultConfigurationSource
        {
            Environment = string.Empty,
        };
        var builder = new ConfigurationBuilder();

        // Act & Assert
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => source.Build(builder));
        Assert.Contains("environment must be specified", exception.Message);
    }

    [Fact]
    public void Build_WithValidEnvironment_ReturnsProvider()
    {
        // Arrange
        var source = new VaultConfigurationSource
        {
            Environment = "dev",
        };
        var builder = new ConfigurationBuilder();

        // Act
        IConfigurationProvider provider = source.Build(builder);

        // Assert
        Assert.NotNull(provider);
        Assert.IsType<VaultConfigurationProvider>(provider);
    }

    [Fact]
    public void Build_WithWhitespaceEnvironment_ThrowsInvalidOperationException()
    {
        // Arrange
        var source = new VaultConfigurationSource
        {
            Environment = "   ",
        };
        var builder = new ConfigurationBuilder();

        // Act & Assert
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => source.Build(builder));
        Assert.Contains("environment must be specified", exception.Message);
    }

    [Fact]
    public void Build_WithCustomReloadInterval_PreservesConfiguration()
    {
        // Arrange
        var source = new VaultConfigurationSource
        {
            Environment = "dev",
            Optional = true,
            ReloadOnChange = true,
            ReloadIntervalSeconds = 600,
        };
        var builder = new ConfigurationBuilder();

        // Act
        IConfigurationProvider provider = source.Build(builder);

        // Assert
        Assert.NotNull(provider);
        Assert.IsType<VaultConfigurationProvider>(provider);
    }
}
