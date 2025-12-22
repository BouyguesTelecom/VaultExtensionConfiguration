# Features

## Multiple Authentication Methods

Vault.Extension.Configuration supports three authentication methods to fit different deployment scenarios:

### 🔐 Local Token Authentication

Ideal for development environments where a token file is available:

```csharp
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
```

**Configuration Properties:**
- `VaultUrl`: Vault server URL
- `MountPoint`: Secret engine mount point
- `TokenFilePath`: Path to the token file (supports environment variables)
- `IgnoreSslErrors`: Option to ignore SSL certificate errors (not recommended for production)

### ☁️ AWS IAM Authentication

Perfect for applications running on AWS infrastructure:

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
        AwsIamRoleName = "my-app-role"
    };
}, environment: "production");
```

**Configuration Properties:**
- `VaultUrl`: Vault server URL
- `MountPoint`: Secret engine mount point
- `Environment`: Environment name (used for role naming)
- `AwsAuthMountPoint`: AWS auth method mount point (default: "aws")
- `AwsIamRoleName`: AWS IAM role name (optional, defaults to `{MountPoint}-{Environment}-role`)
- `IgnoreSslErrors`: Option to ignore SSL certificate errors

### 🔧 Custom Authentication

For other authentication methods not natively supported (AppRole, LDAP, UserPass, Kubernetes, etc.):

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

## ⚙️ Seamless Configuration Integration

The library integrates directly with `Microsoft.Extensions.Configuration`, making secrets available just like any other configuration source:

```csharp
public class MyService
{
    private readonly IConfiguration _configuration;

    public MyService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void UseSecret()
    {
        var dbPassword = _configuration["Database:Password"];
        var apiKey = _configuration["ApiKey"];
    }
}
```

### Configuration Prefixes

Organize your secrets with prefixes:

```csharp
builder.AddVault(
    options => { /* ... */ },
    environment: "production",
    sectionPrefix: "MyApp",
    addUnregisteredEntries: true
);
```

Secrets will be loaded under the `MyApp` configuration section.

## 🔄 Direct Secret Access with IVaultService

For dynamic secret retrieval at runtime:

```csharp
public class SecretManager
{
    private readonly IVaultService _vaultService;

    public SecretManager(IVaultService vaultService)
    {
        _vaultService = vaultService;
    }

    public async Task<string> GetSecretAsync(string key)
    {
        var value = await _vaultService.GetSecretValueAsync("production", key);
        return value?.ToString() ?? throw new InvalidOperationException($"Secret '{key}' not found");
    }

    public async Task<Dictionary<string, object>> GetAllSecretsAsync()
    {
        return await _vaultService.GetSecretsAsync("production");
    }
}
```

**Available Methods:**
- `GetSecretsAsync(string environment)`: Retrieve all secrets for an environment
- `GetSecretValueAsync(string environment, string key)`: Retrieve a specific secret value

## ✅ Built-in Validation

Configuration options are validated using FluentValidation to catch errors early:

```csharp
// Automatic validation on startup
builder.AddVault(options =>
{
    options.AuthenticationType = VaultAuthenticationType.Local;
    options.Configuration = new VaultLocalConfiguration
    {
        // VaultUrl is required - will throw VaultConfigurationException if missing
        VaultUrl = "https://vault.example.com",
        MountPoint = "secret",
        TokenFilePath = "~/.vault-token"
    };
}, environment: "production");
```

**Validated Rules:**
- Required fields (VaultUrl, MountPoint, etc.)
- Configuration type matches authentication method
- File paths are valid (for token files)
- Custom auth method info is provided when using Custom authentication

## 🏗️ Dependency Injection Support

Full integration with Microsoft.Extensions.DependencyInjection:

```csharp
// Register Vault services
services.AddVaultService(options =>
{
    options.AuthenticationType = VaultAuthenticationType.Local;
    options.Configuration = new VaultLocalConfiguration
    {
        VaultUrl = "https://vault.example.com",
        MountPoint = "secret",
        TokenFilePath = "~/.vault-token"
    };
});

// Inject IVaultService anywhere
public class MyController : ControllerBase
{
    private readonly IVaultService _vaultService;

    public MyController(IVaultService vaultService)
    {
        _vaultService = vaultService;
    }
}
```

## 🔒 Security Features

### SSL Certificate Validation

By default, SSL certificates are validated. For development or testing environments, you can disable this (not recommended for production):

```csharp
options.Configuration = new VaultLocalConfiguration
{
    VaultUrl = "https://vault.example.com",
    MountPoint = "secret",
    TokenFilePath = "~/.vault-token",
    IgnoreSslErrors = true // Use with caution
};
```

### Token File Security

When using Local authentication, token files are read securely:
- Supports environment variable expansion (e.g., `~/`, `%USERPROFILE%`)
- File existence is validated before attempting to read
- File content is trimmed to remove whitespace

## 🎯 Environment-Based Configuration

Load different secrets for different environments:

```csharp
var environment = builder.Environment.EnvironmentName;

builder.AddVault(
    options => { /* ... */ },
    environment: environment // "Development", "Staging", "Production"
);
```

Secrets are organized by environment in Vault, ensuring proper isolation.

## 📦 Exception Handling

The library provides specific exception types for better error handling:

- `VaultConfigurationException`: Configuration validation errors
- `VaultAuthenticationException`: Authentication failures

```csharp
try
{
    builder.AddVault(options => { /* ... */ }, environment: "production");
}
catch (VaultConfigurationException ex)
{
    // Handle configuration errors
    Console.WriteLine($"Configuration error: {ex.Message}");
}
catch (VaultAuthenticationException ex)
{
    // Handle authentication failures
    Console.WriteLine($"Authentication failed: {ex.Message}");
}
```
