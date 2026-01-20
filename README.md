# Vault.Extension.Configuration

***A configuration extension for Microsoft.Extensions.Configuration that integrates with HashiCorp Vault using VaultSharp***

[![NuGet package](https://img.shields.io/nuget/v/Vault.Extension.Configuration.svg)](https://www.nuget.org/packages/Vault.Extension.Configuration)
[![Build Status](https://github.com/BouyguesTelecom/VaultExtensionConfiguration/actions/workflows/build.yml/badge.svg)](https://github.com/BouyguesTelecom/VaultExtensionConfiguration/actions)
[![codecov](https://codecov.io/gh/BouyguesTelecom/VaultExtensionConfiguration/branch/main/graph/badge.svg)](https://codecov.io/gh/BouyguesTelecom/VaultExtensionConfiguration)
[![Documentation](https://img.shields.io/badge/docs-online-blue.svg)](https://bouyguestelecom.github.io/VaultExtensionConfiguration/docs/getting-started.html)

## Features

- 🔐 **Multiple authentication methods**: Local token, AWS IAM, or custom authentication
- ⚙️ **Seamless integration** with `Microsoft.Extensions.Configuration` and `IOptions<T>`
- 🔄 **Immediate secret loading** into configuration after build
- ✅ **Fluent validation** for configuration options
- 🏗️ **Full dependency injection** support
- 🔌 **Direct Vault access** via `IVaultService`

## Installation

```bash
dotnet add package Vault.Extension.Configuration
```

## Prerequisites

- .NET 8.0 or later
- Access to a HashiCorp Vault server
- Appropriate authentication credentials (token, AWS IAM role, or custom method)

## Quick Start

### Step 1: Configure Vault in your application

```csharp
using Vault.Extentions;
using Vault.Options;
using Vault.Enum;

var builder = WebApplication.CreateBuilder(args);

// Register VaultService and load secrets immediately into configuration
builder.Services.AddVault(
    builder.Configuration,
    new VaultOptions
    {
        AuthenticationType = VaultAuthenticationType.Local,
        Configuration = new VaultLocalConfiguration
        {
            VaultUrl = "https://vault.example.com",
            MountPoint = "secret",
            TokenFilePath = "~/.vault-token"
        }
    },
    environment: "production");

var app = builder.Build();

// Secrets are already loaded and available - no additional initialization required!

app.Run();
```

### Step 2: Use secrets in your application

You can access Vault secrets in three different ways:


## Usage Examples

### 1. Using IConfiguration

Secrets are automatically loaded into the configuration system and can be accessed like any other configuration value:

```csharp
public class MyController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public MyController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public IActionResult GetSecret()
    {
        // Access secrets directly from configuration
        var dbPassword = _configuration["Database:Password"];
        var apiKey = _configuration["ApiSettings:ApiKey"];

        return Ok(new { hasPassword = !string.IsNullOrEmpty(dbPassword) });
    }
}
```

### 2. Using IOptions Pattern

Bind Vault secrets to strongly-typed configuration classes:

```csharp
// Define your configuration class
public class DatabaseSettings
{
    public string ConnectionString { get; set; }
    public string Password { get; set; }
    public int Timeout { get; set; }
}

// In Program.cs
builder.Services.Configure<DatabaseSettings>(builder.Configuration.GetSection("Database"));

// Use in your services
public class DatabaseService
{
    private readonly DatabaseSettings _settings;

    public DatabaseService(IOptions<DatabaseSettings> options)
    {
        _settings = options.Value;
    }

    public void Connect()
    {
        var connectionString = $"{_settings.ConnectionString};Password={_settings.Password}";
        // Use connection string
    }
}
```

### 3. Using IVaultService Directly

For dynamic secret retrieval at runtime:

```csharp
using Vault.Abstractions;

public class SecretManager
{
    private readonly IVaultService _vaultService;

    public SecretManager(IVaultService vaultService)
    {
        _vaultService = vaultService;
    }

    // Get a single secret value
    public async Task<string?> GetDatabasePasswordAsync()
    {
        var password = await _vaultService.GetSecretValueAsync("production", "Database:Password");
        return password?.ToString();
    }

    // Get all secrets for an environment
    public async Task<Dictionary<string, object>> GetAllSecretsAsync()
    {
        return await _vaultService.GetSecretsAsync("production");
    }

    // Get nested secret using dot notation
    public async Task<string?> GetNestedSecretAsync()
    {
        var value = await _vaultService.GetNestedSecretValueAsync("production", "Api.Settings.Key");
        return value?.ToString();
    }

    // List all available environments
    public async Task<IEnumerable<string>> GetEnvironmentsAsync()
    {
        return await _vaultService.ListEnvironmentsAsync();
    }
}
```

## Authentication Methods

### Local Token Authentication

Best for development environments:

```csharp
var vaultOptions = new VaultOptions
{
    AuthenticationType = VaultAuthenticationType.Local,
    Configuration = new VaultLocalConfiguration
    {
        VaultUrl = "https://vault.example.com",
        MountPoint = "secret",
        TokenFilePath = "~/.vault-token",
        IgnoreSslErrors = false // Set to true only in development
    }
};

builder.Services.AddVault(builder.Configuration, vaultOptions, environment: "development");
```

### AWS IAM Authentication

Ideal for production on AWS infrastructure:

```csharp
var vaultOptions = new VaultOptions
{
    AuthenticationType = VaultAuthenticationType.AWS_IAM,
    Configuration = new VaultAwsConfiguration
    {
        VaultUrl = "https://vault.example.com",
        MountPoint = "secret",
        Environment = "production",
        AwsAuthMountPoint = "aws",
        AwsIamRoleName = "my-app-role" // Optional
    }
};

builder.Services.AddVault(builder.Configuration, vaultOptions, environment: "production");
```

### Custom Authentication

For AppRole, LDAP, UserPass, Kubernetes, etc.:

```csharp
using VaultSharp.V1.AuthMethods.AppRole;

var vaultOptions = new VaultOptions
{
    AuthenticationType = VaultAuthenticationType.Custom,
    Configuration = new VaultDefaultConfiguration
    {
        VaultUrl = "https://vault.example.com",
        MountPoint = "secret"
    },
    CustomAuthMethodInfo = new AppRoleAuthMethodInfo("my-role-id", "my-secret-id")
};

builder.Services.AddVault(builder.Configuration, vaultOptions, environment: "production");
```


## Configuration Reference

### VaultOptions

| Property | Type | Description |
|----------|------|-------------|
| `IsActivated` | `bool` | Enable/disable Vault integration (default: `true`) |
| `AuthenticationType` | `VaultAuthenticationType` | Authentication method: `Local`, `AWS_IAM`, or `Custom` |
| `Configuration` | `VaultDefaultConfiguration` | Base configuration (use specific subclass based on auth type) |
| `CustomAuthMethodInfo` | `IAuthMethodInfo` | Custom authentication info (only for `Custom` type) |

### VaultLocalConfiguration

| Property | Type | Description |
|----------|------|-------------|
| `VaultUrl` | `string` | Vault server URL (e.g., `https://vault.example.com`) |
| `MountPoint` | `string` | Secret engine mount point (e.g., `secret`) |
| `TokenFilePath` | `string` | Path to token file (supports environment variables like `~/.vault-token`) |
| `IgnoreSslErrors` | `bool` | Ignore SSL certificate errors (default: `false`, use only in development) |

### VaultAwsConfiguration

| Property | Type | Description |
|----------|------|-------------|
| `VaultUrl` | `string` | Vault server URL |
| `MountPoint` | `string` | Secret engine mount point |
| `Environment` | `string` | Environment name (used for default role naming) |
| `AwsAuthMountPoint` | `string` | AWS auth method mount point (default: `"aws"`) |
| `AwsIamRoleName` | `string` | AWS IAM role name (optional, defaults to `{MountPoint}-{Environment}-role`) |
| `IgnoreSslErrors` | `bool` | Ignore SSL certificate errors |

### IVaultService Methods

| Method | Description |
|--------|-------------|
| `ListEnvironmentsAsync()` | Lists all available environments in the Vault |
| `GetSecretsAsync(string environment)` | Retrieves all secrets for a given environment |
| `GetSecretValueAsync(string environment, string key)` | Gets a specific secret value by key |
| `GetNestedSecretValueAsync(string environment, string path)` | Gets a nested secret using dot notation (e.g., `"Level1.Level2.Key"`) |

## Complete Example

Here's a complete example showing all three usage patterns:

```csharp
using Vault.Extentions;
using Vault.Options;
using Vault.Enum;
using Vault.Abstractions;

var builder = WebApplication.CreateBuilder(args);

// 1. Configure Vault - secrets are loaded immediately into configuration
builder.Services.AddVault(
    builder.Configuration,
    new VaultOptions
    {
        AuthenticationType = VaultAuthenticationType.AWS_IAM,
        Configuration = new VaultAwsConfiguration
        {
            VaultUrl = "https://vault.example.com",
            MountPoint = "myapp",
            Environment = "production"
        }
    },
    environment: "production");

// 2. Register strongly-typed configuration
public class AppSettings
{
    public string ApiKey { get; set; }
    public string DatabasePassword { get; set; }
}

builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));

// 3. Register your services
builder.Services.AddScoped<MyService>();

var app = builder.Build();

// No additional initialization required - secrets are already available!

app.MapGet("/config", (IConfiguration config) =>
    new { apiKey = config["AppSettings:ApiKey"] });

app.MapGet("/options", (IOptions<AppSettings> options) =>
    new { settings = options.Value });

app.MapGet("/vault", async (IVaultService vault) =>
    await vault.GetSecretsAsync("production"));

app.Run();
```

## Best Practices

1. **Environment-specific configuration**: Use different Vault environments for development, staging, and production
2. **Secret naming**: Use hierarchical naming with colons (e.g., `Database:Password`, `Api:Settings:Key`)
3. **Error handling**: Always handle cases where secrets might not exist
4. **SSL certificates**: Never set `IgnoreSslErrors = true` in production
5. **Token security**: Store Vault tokens securely and rotate them regularly
6. **IVaultService usage**: Use `IVaultService` for dynamic secret retrieval, `IConfiguration`/`IOptions` for startup configuration

## Troubleshooting

### Secrets not loading

Ensure the `AddVault` method is called with `builder.Configuration` (the configuration builder, not the built configuration):

```csharp
// Correct - pass the configuration builder
builder.Services.AddVault(
    builder.Configuration,  // IConfigurationBuilder
    vaultOptions,
    environment: "production");
```

### Authentication errors

- **Local**: Verify token file exists and has valid permissions
- **AWS IAM**: Ensure IAM role is properly configured and the application has AWS credentials
- **Custom**: Verify the custom auth method credentials are valid

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.



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
