// Copyright (c) Bouygues Telecom. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;
using Vault.Abstractions;
using Vault.Enum;
using Vault.Extentions;
using Vault.Options;
using Vault.Options.Configuration;
using VaultSharp.V1.AuthMethods;
using Xunit;

namespace Vault.Tests.Extensions;

/// <summary>
/// Unit tests for VaultExtension.
/// </summary>
public class VaultExtensionTests
{
    [Fact]
    public void AddVault_WithNullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;
        var configuration = new ConfigurationBuilder();
        var vaultOptions = new VaultOptions { IsActivated = false };
        var environment = "dev";

        // Act & Assert
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            services!.AddVault(configuration, vaultOptions, environment));
        Assert.Equal(nameof(services), exception.ParamName);
    }

    [Fact]
    public void AddVault_WithNullConfiguration_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        IConfigurationBuilder? configuration = null;
        var vaultOptions = new VaultOptions { IsActivated = false };
        var environment = "dev";

        // Act & Assert
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            services.AddVault(configuration!, vaultOptions, environment));
        Assert.Equal(nameof(configuration), exception.ParamName);
    }

    [Fact]
    public void AddVault_WithNullVaultOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder();
        VaultOptions? vaultOptions = null;
        var environment = "dev";

        // Act & Assert
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            services.AddVault(configuration, vaultOptions!, environment));
        Assert.Equal(nameof(vaultOptions), exception.ParamName);
    }

    [Fact]
    public void AddVault_WithEmptyEnvironment_ThrowsArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder();
        var vaultOptions = new VaultOptions { IsActivated = false };
        var environment = string.Empty;

        // Act & Assert
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            services.AddVault(configuration, vaultOptions, environment));
        Assert.Equal(nameof(environment), exception.ParamName);
        Assert.Contains("cannot be empty", exception.Message);
    }

    [Fact]
    public void AddVault_WithWhitespaceEnvironment_ThrowsArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder();
        var vaultOptions = new VaultOptions { IsActivated = false };
        var environment = "   ";

        // Act & Assert
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            services.AddVault(configuration, vaultOptions, environment));
        Assert.Equal(nameof(environment), exception.ParamName);
    }

    [Fact]
    public void AddVault_WithInactivatedVault_RegistersOptionsOnly()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder();
        var vaultOptions = new VaultOptions
        {
            IsActivated = false,
            AuthenticationType = VaultAuthenticationType.None,
        };
        var environment = "dev";

        // Act
        services.AddVault(configuration, vaultOptions, environment);
        ServiceProvider serviceProvider = services.BuildServiceProvider();

        // Assert
        VaultOptions? registeredOptions = serviceProvider.GetService<VaultOptions>();
        Assert.NotNull(registeredOptions);
        Assert.Same(vaultOptions, registeredOptions);

        // VaultService should not be registered when Vault is not activated
        IVaultService? vaultService = serviceProvider.GetService<IVaultService>();
        Assert.Null(vaultService);
    }

    [Fact]
    public void AddVault_WithInvalidConfiguration_ThrowsInvalidOperationException()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder();
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
        var environment = "dev";

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            services.AddVault(configuration, vaultOptions, environment));
    }

    [Fact]
    public void AddVault_ReturnsSameServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder();
        var vaultOptions = new VaultOptions { IsActivated = false };
        var environment = "dev";

        // Act
        IServiceCollection result = services.AddVault(configuration, vaultOptions, environment);

        // Assert
        Assert.Same(services, result);
    }

    [Fact]
    public void AddVault_RegistersIOptionsSnapshot()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder();
        var vaultOptions = new VaultOptions
        {
            IsActivated = false,
            AuthenticationType = VaultAuthenticationType.None,
        };
        var environment = "dev";

        // Act
        services.AddVault(configuration, vaultOptions, environment);
        ServiceProvider serviceProvider = services.BuildServiceProvider();

        // Assert
        IOptionsSnapshot<VaultOptions>? optionsSnapshot = serviceProvider.GetService<IOptionsSnapshot<VaultOptions>>();
        Assert.NotNull(optionsSnapshot);
        Assert.Same(vaultOptions, optionsSnapshot.Value);
    }

    [Fact]
    public void AddVault_RegistersIOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder();
        var vaultOptions = new VaultOptions
        {
            IsActivated = false,
            AuthenticationType = VaultAuthenticationType.None,
        };
        var environment = "dev";

        // Act
        services.AddVault(configuration, vaultOptions, environment);
        ServiceProvider serviceProvider = services.BuildServiceProvider();

        // Assert
        IOptions<VaultOptions>? options = serviceProvider.GetService<IOptions<VaultOptions>>();
        Assert.NotNull(options);
        Assert.Same(vaultOptions, options.Value);
    }

    [Fact]
    public void AddVault_RegistersIOptionsMonitor()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder();
        var vaultOptions = new VaultOptions
        {
            IsActivated = false,
            AuthenticationType = VaultAuthenticationType.None,
        };
        var environment = "dev";

        // Act
        services.AddVault(configuration, vaultOptions, environment);
        ServiceProvider serviceProvider = services.BuildServiceProvider();

        // Assert
        IOptionsMonitor<VaultOptions>? optionsMonitor = serviceProvider.GetService<IOptionsMonitor<VaultOptions>>();
        Assert.NotNull(optionsMonitor);
        Assert.Same(vaultOptions, optionsMonitor.CurrentValue);
    }
}
