// Copyright (c) Bouygues Telecom. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Vault.Enum;
using Vault.Options;
using Vault.Options.Configuration;
using Xunit;

namespace Vault.Tests.Options;

/// <summary>
/// Unit tests for VaultOptions.
/// </summary>
public class VaultOptionsTests
{
    [Fact]
    public void VaultOptions_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var options = new VaultOptions();

        // Assert
        Assert.True(options.IsActivated);
        Assert.Equal(VaultAuthenticationType.None, options.AuthenticationType);
        Assert.NotNull(options.Configuration);
        Assert.IsType<VaultDefaultConfiguration>(options.Configuration);
    }

    [Fact]
    public void VaultOptions_CanSetIsActivated()
    {
        // Arrange
        var options = new VaultOptions();

        // Act
        options.IsActivated = false;

        // Assert
        Assert.False(options.IsActivated);
    }

    [Fact]
    public void VaultOptions_CanSetAuthenticationType()
    {
        // Arrange
        var options = new VaultOptions();

        // Act
        options.AuthenticationType = VaultAuthenticationType.Local;

        // Assert
        Assert.Equal(VaultAuthenticationType.Local, options.AuthenticationType);
    }

    [Fact]
    public void VaultOptions_CanSetConfiguration()
    {
        // Arrange
        var options = new VaultOptions();
        var config = new VaultLocalConfiguration
        {
            VaultUrl = "https://vault.example.com",
            MountPoint = "secret",
            TokenFilePath = "/path/to/token",
        };

        // Act
        options.Configuration = config;

        // Assert
        Assert.Same(config, options.Configuration);
        Assert.IsType<VaultLocalConfiguration>(options.Configuration);
    }

    [Fact]
    public void VaultDefaultConfiguration_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var config = new VaultDefaultConfiguration();

        // Assert
        Assert.Equal(string.Empty, config.VaultUrl);
        Assert.Equal(string.Empty, config.MountPoint);
        Assert.True(config.IgnoreSslErrors);
    }

    [Fact]
    public void VaultDefaultConfiguration_CanSetProperties()
    {
        // Arrange
        var config = new VaultDefaultConfiguration();

        // Act
        config.VaultUrl = "https://vault.example.com";
        config.MountPoint = "secret";
        config.IgnoreSslErrors = false;

        // Assert
        Assert.Equal("https://vault.example.com", config.VaultUrl);
        Assert.Equal("secret", config.MountPoint);
        Assert.False(config.IgnoreSslErrors);
    }

    [Fact]
    public void VaultLocalConfiguration_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var config = new VaultLocalConfiguration();

        // Assert
        Assert.Equal(string.Empty, config.VaultUrl);
        Assert.Equal(string.Empty, config.MountPoint);
        Assert.True(config.IgnoreSslErrors);
        Assert.Equal("%USERPROFILE%\\.vault-token", config.TokenFilePath);
    }

    [Fact]
    public void VaultLocalConfiguration_InheritsFromDefaultConfiguration()
    {
        // Arrange & Act
        var config = new VaultLocalConfiguration
        {
            VaultUrl = "https://vault.example.com",
            MountPoint = "secret",
            IgnoreSslErrors = false,
            TokenFilePath = "/custom/path",
        };

        // Assert
        Assert.IsAssignableFrom<VaultDefaultConfiguration>(config);
        Assert.Equal("https://vault.example.com", config.VaultUrl);
        Assert.Equal("secret", config.MountPoint);
        Assert.False(config.IgnoreSslErrors);
        Assert.Equal("/custom/path", config.TokenFilePath);
    }

    [Fact]
    public void VaultAwsIAMConfiguration_InheritsFromDefaultConfiguration()
    {
        // Arrange & Act
        var config = new VaultAwsIAMConfiguration
        {
            VaultUrl = "https://vault.example.com",
            MountPoint = "secret",
            AwsIamRoleName = "my-role",
            Environment = "prod",
        };

        // Assert
        Assert.IsAssignableFrom<VaultDefaultConfiguration>(config);
        Assert.Equal("https://vault.example.com", config.VaultUrl);
        Assert.Equal("secret", config.MountPoint);
        Assert.Equal("my-role", config.AwsIamRoleName);
        Assert.Equal("prod", config.Environment);
    }

    [Fact]
    public void VaultCustomConfiguration_InheritsFromDefaultConfiguration()
    {
        // Arrange & Act
        var config = new VaultCustomConfiguration
        {
            VaultUrl = "https://vault.example.com",
            MountPoint = "secret",
        };

        // Assert
        Assert.IsAssignableFrom<VaultDefaultConfiguration>(config);
        Assert.Equal("https://vault.example.com", config.VaultUrl);
        Assert.Equal("secret", config.MountPoint);
        Assert.Null(config.AuthMethodFactory);
    }
}
