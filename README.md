# Vault.Extension.Configuration

***A configuration extension for Microsoft.Extensions.Configuration that integrates with HashiCorp Vault using VaultSharp***

[![NuGet package](https://img.shields.io/nuget/v/Vault.Extension.Configuration.svg)](https://www.nuget.org/packages/Vault.Extension.Configuration)
[![Build Status](https://github.com/BouyguesTelecom/VaultExtensionConfiguration/actions/workflows/build.yml/badge.svg)](https://github.com/BouyguesTelecom/VaultExtensionConfiguration/actions)
[![codecov](https://codecov.io/gh/BouyguesTelecom/VaultExtensionConfiguration/branch/main/graph/badge.svg)](https://codecov.io/gh/BouyguesTelecom/VaultExtensionConfiguration)
[![Documentation](https://img.shields.io/badge/docs-online-blue.svg)](https://bouyguestelecom.github.io/VaultExtensionConfiguration/docs/features.html)

## Features

- 🔐 **Multiple authentication methods**: Local token, AWS IAM, or custom authentication
- ⚙️ **Seamless integration** with `Microsoft.Extensions.Configuration`
- 🔄 **Secret loading** into configuration at startup
- ✅ **Fluent validation** for configuration options
- 🏗️ **Dependency injection** support

## Installation

```bash
dotnet add package Vault.Extension.Configuration
```

## Quick Start

### Basic Usage with Local Token

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.AddVault(options =>
{
    options.AuthenticationType = VaultAuthenticationType.Local;
    options.Configuration = new VaultLocalConfiguration
    {
        VaultUrl = "https://vault.example.com",
        MountPoint = "secret",
        TokenFilePath = "~/.vault-token"
    };
}, environment: "production");

var app = builder.Build();
```

### AWS IAM Authentication

```csharp
builder.AddVault(options =>
{
    options.AuthenticationType = VaultAuthenticationType.AWS_IAM;
    options.Configuration = new VaultAwsConfiguration
    {
        VaultUrl = "https://vault.example.com",
        MountPoint = "secret",
        Environment = "production",
        AwsAuthMountPoint = "aws",
        AwsIamRoleName = "my-app-role" // Optional, defaults to {MountPoint}-{Environment}-role
    };
}, environment: "production");
```

### Custom Authentication

For authentication methods not natively supported (AppRole, LDAP, UserPass, etc.):

```csharp
builder.AddVault(options =>
{
    options.AuthenticationType = VaultAuthenticationType.Custom;
    options.Configuration = new VaultDefaultConfiguration
    {
        VaultUrl = "https://vault.example.com",
        MountPoint = "secret"
    };
    options.CustomAuthMethodInfo = new AppRoleAuthMethodInfo(roleId, secretId);
}, environment: "production");
```

## Configuration Options

### VaultOptions

| Property | Type | Description |
|----------|------|-------------|
| `AuthenticationType` | `VaultAuthenticationType` | Authentication method (Local, AWS_IAM, Custom) |
| `Configuration` | `VaultDefaultConfiguration` | Vault connection configuration |
| `CustomAuthMethodInfo` | `IAuthMethodInfo` | Custom authentication (only for Custom type) |

### VaultLocalConfiguration

| Property | Type | Description |
|----------|------|-------------|
| `VaultUrl` | `string` | Vault server URL |
| `MountPoint` | `string` | Secret engine mount point |
| `TokenFilePath` | `string` | Path to the token file |
| `IgnoreSslErrors` | `bool` | Ignore SSL certificate errors |

### VaultAwsConfiguration

| Property | Type | Description |
|----------|------|-------------|
| `VaultUrl` | `string` | Vault server URL |
| `MountPoint` | `string` | Secret engine mount point |
| `Environment` | `string` | Environment name (used for role naming) |
| `AwsAuthMountPoint` | `string` | AWS auth method mount point (default: "aws") |
| `AwsIamRoleName` | `string` | AWS IAM role name (optional) |
| `IgnoreSslErrors` | `bool` | Ignore SSL certificate errors |

## Advanced Usage

### Using IVaultService Directly

```csharp
public class MyService
{
    private readonly IVaultService _vaultService;

    public MyService(IVaultService vaultService)
    {
        _vaultService = vaultService;
    }

    public async Task<string> GetSecretAsync(string key)
    {
        var secretValue = await _vaultService.GetSecretValueAsync("production", key);
        return secretValue?.ToString() ?? throw new InvalidOperationException($"Secret '{key}' not found");
    }

    public async Task<Dictionary<string, object>> GetAllSecretsAsync()
    {
        return await _vaultService.GetSecretsAsync("production");
    }
}
```

### Loading Secrets with Prefix

```csharp
builder.AddVault(options => { /* ... */ },
    environment: "production",
    sectionPrefix: "MyApp",
    addUnregisteredEntries: true);
```

## Requirements

- .NET 8.0 or later
- HashiCorp Vault server
- VaultSharp (included as dependency)

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

---

## Template Information

<details>
<summary>Click to expand template documentation</summary>

This project is based on [Library.Template](https://github.com/AArnott/Library.Template).

### Build Features

* Auto-versioning via [Nerdbank.GitVersioning](https://github.com/dotnet/nerdbank.gitversioning)
* Static analyzers: [Code Analysis](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/overview) and [StyleCop](https://github.com/DotNetAnalyzers/StyleCopAnalyzers)
* Read-only source tree (builds to top-level bin/obj folders)
* Builds with a "pinned" .NET SDK for reproducible builds
* Testing on Windows, Linux and macOS

### Maintaining your repo based on this template

```ps1
git fetch
git checkout origin/main
.\tools\MergeFrom-Template.ps1
# resolve any conflicts, then commit the merge commit.
git push origin -u HEAD
```

</details>
