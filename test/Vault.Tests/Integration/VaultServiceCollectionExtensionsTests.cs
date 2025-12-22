// Copyright (c) Bouygues Telecom. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Vault.Exceptions;
using Vault.Options;
using VaultSharp.V1.AuthMethods;
using Xunit;

namespace Vault.Tests.Integration;

/// <summary>
/// Tests d'intégration pour VaultServiceCollectionExtensions.
/// Vérifie l'enregistrement des services et la validation complète.
/// </summary>
public class VaultServiceCollectionExtensionsTests
{
    [Fact]
    public void AddVaultService_Should_Register_Services_With_Valid_Local_Configuration()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddVaultService(options =>
        {
            options.AuthenticationType = VaultAuthenticationType.Local;
            options.Configuration = new VaultLocalConfiguration
            {
                VaultUrl = "https://vault.example.com",
                MountPoint = "kv",
                TokenFilePath = "%USERPROFILE%\\.vault-token",
            };
        });

        // Assert
        ServiceProvider serviceProvider = services.BuildServiceProvider();
        VaultOptions? vaultOptions = serviceProvider.GetService<VaultOptions>();

        vaultOptions.Should().NotBeNull();
        vaultOptions!.AuthenticationType.Should().Be(VaultAuthenticationType.Local);
        vaultOptions.Configuration.Should().BeOfType<VaultLocalConfiguration>();
    }

    [Fact]
    public void AddVaultService_Should_Register_Services_With_Local_Configuration_Without_TokenFilePath()
    {
        // Arrange - TokenFilePath n'est plus requis
        var services = new ServiceCollection();

        // Act
        services.AddVaultService(options =>
        {
            options.AuthenticationType = VaultAuthenticationType.Local;
            options.Configuration = new VaultLocalConfiguration
            {
                VaultUrl = "https://vault.example.com",
                MountPoint = "kv",
                TokenFilePath = string.Empty, // Pas requis
            };
        });

        // Assert
        ServiceProvider serviceProvider = services.BuildServiceProvider();
        VaultOptions? vaultOptions = serviceProvider.GetService<VaultOptions>();

        vaultOptions.Should().NotBeNull();
        vaultOptions!.AuthenticationType.Should().Be(VaultAuthenticationType.Local);
    }

    [Fact]
    public void AddVaultService_Should_Throw_When_Local_Configuration_Has_Wrong_Type()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        Action act = () => services.AddVaultService(options =>
        {
            options.AuthenticationType = VaultAuthenticationType.Local;
            options.Configuration = new VaultAwsConfiguration // Wrong type
            {
                VaultUrl = "https://vault.example.com",
                MountPoint = "kv",
                Environment = "thomas",
            };
        });

        // Assert
        act.Should().Throw<VaultConfigurationException>()
            .WithMessage("*VaultLocalConfiguration*");
    }

    [Fact]
    public void AddVaultService_Should_Register_Services_With_Valid_AWS_Configuration_Explicit_Role()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddVaultService(options =>
        {
            options.AuthenticationType = VaultAuthenticationType.AWS_IAM;
            options.Configuration = new VaultAwsConfiguration
            {
                VaultUrl = "https://vault.example.com",
                MountPoint = "kv",
                Environment = "thomas",
                AwsIamRoleName = "my-custom-role",
                AwsAuthMountPoint = "aws",
            };
        });

        // Assert
        ServiceProvider serviceProvider = services.BuildServiceProvider();
        VaultOptions? vaultOptions = serviceProvider.GetService<VaultOptions>();

        vaultOptions.Should().NotBeNull();
        vaultOptions!.AuthenticationType.Should().Be(VaultAuthenticationType.AWS_IAM);
        vaultOptions.Configuration.Should().BeOfType<VaultAwsConfiguration>();

        var awsConfig = (VaultAwsConfiguration)vaultOptions.Configuration!;
        awsConfig.AwsIamRoleName.Should().Be("my-custom-role");
    }

    [Fact]
    public void AddVaultService_Should_Register_Services_With_Valid_AWS_Configuration_Without_Role()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddVaultService(options =>
        {
            options.AuthenticationType = VaultAuthenticationType.AWS_IAM;
            options.Configuration = new VaultAwsConfiguration
            {
                VaultUrl = "https://vault.example.com",
                MountPoint = "HELLOWORLD-FORMATION",
                Environment = "thomas",
                AwsAuthMountPoint = "aws",
            };
        });

        // Assert
        ServiceProvider serviceProvider = services.BuildServiceProvider();
        VaultOptions? vaultOptions = serviceProvider.GetService<VaultOptions>();

        vaultOptions.Should().NotBeNull();
        vaultOptions!.AuthenticationType.Should().Be(VaultAuthenticationType.AWS_IAM);

        var awsConfig = (VaultAwsConfiguration)vaultOptions.Configuration!;
        awsConfig.MountPoint.Should().Be("HELLOWORLD-FORMATION");
        awsConfig.Environment.Should().Be("thomas");
    }

    [Fact]
    public void AddVaultService_Should_Throw_When_AWS_Configuration_Missing_Environment()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        Action act = () => services.AddVaultService(options =>
        {
            options.AuthenticationType = VaultAuthenticationType.AWS_IAM;
            options.Configuration = new VaultAwsConfiguration
            {
                VaultUrl = "https://vault.example.com",
                MountPoint = "HELLOWORLD-FORMATION",
                Environment = string.Empty, // Requis
                AwsAuthMountPoint = "aws",
            };
        });

        // Assert
        act.Should().Throw<VaultConfigurationException>()
            .WithMessage("*Environment*");
    }

    [Fact]
    public void AddVaultService_Should_Throw_When_AWS_Configuration_Has_Wrong_Type()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        Action act = () => services.AddVaultService(options =>
        {
            options.AuthenticationType = VaultAuthenticationType.AWS_IAM;
            options.Configuration = new VaultLocalConfiguration // Wrong type
            {
                VaultUrl = "https://vault.example.com",
                MountPoint = "kv",
            };
        });

        // Assert
        act.Should().Throw<VaultConfigurationException>()
            .WithMessage("*VaultAwsConfiguration*");
    }

    [Fact]
    public void AddVaultService_Should_Register_Services_With_Valid_Custom_Configuration()
    {
        // Arrange
        var services = new ServiceCollection();
        IAuthMethodInfo mockAuthMethod = Substitute.For<IAuthMethodInfo>();

        // Act
        services.AddVaultService(options =>
        {
            options.AuthenticationType = VaultAuthenticationType.Custom;
            options.Configuration = new VaultDefaultConfiguration
            {
                VaultUrl = "https://vault.example.com",
                MountPoint = "kv",
            };
            options.CustomAuthMethodInfo = mockAuthMethod;
        });

        // Assert
        ServiceProvider serviceProvider = services.BuildServiceProvider();
        VaultOptions? vaultOptions = serviceProvider.GetService<VaultOptions>();

        vaultOptions.Should().NotBeNull();
        vaultOptions!.AuthenticationType.Should().Be(VaultAuthenticationType.Custom);
        vaultOptions.CustomAuthMethodInfo.Should().NotBeNull();
        vaultOptions.Configuration.Should().BeOfType<VaultDefaultConfiguration>();
    }

    [Fact]
    public void AddVaultService_Should_Throw_When_Custom_Configuration_Missing_AuthMethodInfo()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        Action act = () => services.AddVaultService(options =>
        {
            options.AuthenticationType = VaultAuthenticationType.Custom;
            options.Configuration = new VaultDefaultConfiguration
            {
                VaultUrl = "https://vault.example.com",
                MountPoint = "kv",
            };
            options.CustomAuthMethodInfo = null; // Missing
        });

        // Assert
        act.Should().Throw<VaultConfigurationException>()
            .WithMessage("*CustomAuthMethodInfo*");
    }

    [Fact]
    public void AddVaultService_Should_Throw_When_Custom_Configuration_Has_Wrong_Type()
    {
        // Arrange
        var services = new ServiceCollection();
        IAuthMethodInfo mockAuthMethod = Substitute.For<IAuthMethodInfo>();

        // Act
        Action act = () => services.AddVaultService(options =>
        {
            options.AuthenticationType = VaultAuthenticationType.Custom;
            options.Configuration = new VaultAwsConfiguration // Should be VaultDefaultConfiguration
            {
                VaultUrl = "https://vault.example.com",
                MountPoint = "kv",
                Environment = "thomas",
            };
            options.CustomAuthMethodInfo = mockAuthMethod;
        });

        // Assert
        act.Should().Throw<VaultConfigurationException>()
            .WithMessage("*VaultDefaultConfiguration*");
    }

    [Fact]
    public void AddVaultService_Should_Throw_When_AuthenticationType_Is_None()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        Action act = () => services.AddVaultService(options =>
        {
            options.AuthenticationType = VaultAuthenticationType.None;
            options.Configuration = new VaultDefaultConfiguration
            {
                VaultUrl = "https://vault.example.com",
                MountPoint = "kv",
            };
        });

        // Assert
        act.Should().Throw<VaultConfigurationException>()
            .WithMessage("*AuthenticationType*None*");
    }

    [Fact]
    public void AddVaultService_Should_Throw_When_Configuration_Is_Null()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        Action act = () => services.AddVaultService(options =>
        {
            options.AuthenticationType = VaultAuthenticationType.Local;
            options.Configuration = null;
        });

        // Assert
        act.Should().Throw<VaultConfigurationException>()
            .WithMessage("*Configuration*defini*");
    }

    [Fact]
    public void AddVaultService_Should_Throw_When_VaultUrl_Is_Missing()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        Action act = () => services.AddVaultService(options =>
        {
            options.AuthenticationType = VaultAuthenticationType.Local;
            options.Configuration = new VaultLocalConfiguration
            {
                VaultUrl = string.Empty,
                MountPoint = "kv",
                TokenFilePath = "%USERPROFILE%\\.vault-token",
            };
        });

        // Assert
        act.Should().Throw<VaultConfigurationException>()
            .WithMessage("*VaultUrl*");
    }

    [Fact]
    public void AddVaultService_Should_Throw_When_MountPoint_Is_Missing()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        Action act = () => services.AddVaultService(options =>
        {
            options.AuthenticationType = VaultAuthenticationType.Local;
            options.Configuration = new VaultLocalConfiguration
            {
                VaultUrl = "https://vault.example.com",
                MountPoint = string.Empty,
                TokenFilePath = "%USERPROFILE%\\.vault-token",
            };
        });

        // Assert
        act.Should().Throw<VaultConfigurationException>()
            .WithMessage("*MountPoint*");
    }

    [Fact]
    public void AddVaultService_Should_Include_All_Errors_In_Exception_Message()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        Action act = () => services.AddVaultService(options =>
        {
            options.AuthenticationType = VaultAuthenticationType.Local;
            options.Configuration = new VaultLocalConfiguration
            {
                VaultUrl = string.Empty,
                MountPoint = string.Empty,
                TokenFilePath = string.Empty,
            };
        });

        // Assert
        act.Should().Throw<VaultConfigurationException>()
            .Which.Message.Should().Contain("VaultUrl")
            .And.Contain("MountPoint");

        // TokenFilePath n'est plus validé
    }

    [Fact]
    public void AddVaultService_Should_Register_VaultOptions_As_Singleton()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddVaultService(options =>
        {
            options.AuthenticationType = VaultAuthenticationType.Local;
            options.Configuration = new VaultLocalConfiguration
            {
                VaultUrl = "https://vault.example.com",
                MountPoint = "kv",
                TokenFilePath = "%USERPROFILE%\\.vault-token",
            };
        });

        // Assert
        ServiceProvider serviceProvider = services.BuildServiceProvider();
        VaultOptions? instance1 = serviceProvider.GetService<VaultOptions>();
        VaultOptions? instance2 = serviceProvider.GetService<VaultOptions>();

        instance1.Should().BeSameAs(instance2);
    }

    [Fact]
    public void AddVaultService_Should_Return_ServiceCollection_For_Chaining()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        IServiceCollection result = services.AddVaultService(options =>
        {
            options.AuthenticationType = VaultAuthenticationType.Local;
            options.Configuration = new VaultLocalConfiguration
            {
                VaultUrl = "https://vault.example.com",
                MountPoint = "kv",
                TokenFilePath = "%USERPROFILE%\\.vault-token",
            };
        });

        // Assert
        result.Should().BeSameAs(services);
    }
}
