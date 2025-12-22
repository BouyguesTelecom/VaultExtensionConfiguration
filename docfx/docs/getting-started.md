# Getting Started

## Prerequisites

Before using Vault.Extension.Configuration, ensure you have:

- **.NET 8.0 or later** installed
- Access to a **HashiCorp Vault server**
- Appropriate **authentication credentials** (token, AWS IAM role, or custom method)

## Installation

Install the NuGet package using one of the following methods:

### .NET CLI

```bash
dotnet add package Vault.Extension.Configuration
```

### Package Manager Console

```powershell
Install-Package Vault.Extension.Configuration
```

### PackageReference

Add to your `.csproj` file:

```xml
<PackageReference Include="Vault.Extension.Configuration" Version="*" />
```

[![NuGet package](https://img.shields.io/nuget/v/Vault.Extension.Configuration.svg)](https://nuget.org/packages/Vault.Extension.Configuration)

## Basic Setup

### Step 1: Choose Your Authentication Method

The library supports three authentication methods. Choose the one that fits your deployment scenario:

#### Option A: Local Token (Development)

Best for local development where you have a Vault token file:

```csharp
using Vault.Extensions;
using Vault.Options;
using Vault.Enum;

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
}, environment: "development");

var app = builder.Build();
app.Run();
```

#### Option B: AWS IAM (Production on AWS)

Ideal for applications running on AWS infrastructure:

```csharp
using Vault.Extensions;
using Vault.Options;
using Vault.Enum;

var builder = WebApplication.CreateBuilder(args);

builder.AddVault(options =>
{
    options.AuthenticationType = VaultAuthenticationType.AWS_IAM;
    options.Configuration = new VaultAwsConfiguration
    {
        VaultUrl = "https://vault.example.com",
        MountPoint = "secret",
        Environment = "production",
        AwsAuthMountPoint = "aws"
    };
}, environment: "production");

var app = builder.Build();
app.Run();
```

#### Option C: Custom Authentication

For other authentication methods (AppRole, LDAP, etc.):

```csharp
using Vault.Extensions;
using Vault.Options;
using Vault.Enum;
using VaultSharp.V1.AuthMethods.AppRole;

var builder = WebApplication.CreateBuilder(args);

var roleId = "your-role-id";
var secretId = "your-secret-id";

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

var app = builder.Build();
app.Run();
```

### Step 2: Access Secrets in Your Application

#### Using IConfiguration

Secrets are automatically loaded into the configuration pipeline:

```csharp
public class DatabaseService
{
    private readonly string _connectionString;

    public DatabaseService(IConfiguration configuration)
    {
        // Access secrets like any other configuration value
        _connectionString = configuration["Database:ConnectionString"]
            ?? throw new InvalidOperationException("Connection string not found");
    }
}
```

#### Using IVaultService

For dynamic secret retrieval:

```csharp
using Vault.Abstractions;

public class SecretService
{
    private readonly IVaultService _vaultService;

    public SecretService(IVaultService vaultService)
    {
        _vaultService = vaultService;
    }

    public async Task<string> GetApiKeyAsync()
    {
        var value = await _vaultService.GetSecretValueAsync("production", "ApiKey");
        return value?.ToString() ?? throw new InvalidOperationException("API key not found");
    }

    public async Task<Dictionary<string, object>> GetAllSecretsAsync()
    {
        return await _vaultService.GetSecretsAsync("production");
    }
}
```

## Configuration Options

### Environment Parameter

The `environment` parameter specifies which Vault path to load secrets from:

```csharp
builder.AddVault(
    options => { /* ... */ },
    environment: "production" // Loads from {MountPoint}/production
);
```

### Section Prefix

Organize secrets under a configuration section:

```csharp
builder.AddVault(
    options => { /* ... */ },
    environment: "production",
    sectionPrefix: "MyApp" // Secrets available under MyApp:SecretName
);
```

### Add Unregistered Entries

Control whether to include secrets not already in configuration:

```csharp
builder.AddVault(
    options => { /* ... */ },
    environment: "production",
    sectionPrefix: null,
    addUnregisteredEntries: true // Include all Vault secrets
);
```

## Vault Server Setup

### Required Vault Configuration

1. **Enable KV Secrets Engine**:
   ```bash
   vault secrets enable -path=secret kv-v2
   ```

2. **Create secrets for your environment**:
   ```bash
   vault kv put secret/production Database:ConnectionString="Server=..." ApiKey="..."
   ```

3. **Configure authentication method** (example for AWS IAM):
   ```bash
   vault auth enable aws
   vault write auth/aws/role/secret-production-role \
       auth_type=iam \
       bound_iam_principal_arn="arn:aws:iam::123456789:role/my-app-role" \
       policies=secret-production-policy
   ```

## Error Handling

The library provides specific exceptions for different error scenarios:

```csharp
using Vault.Exceptions;

try
{
    builder.AddVault(options =>
    {
        // Configuration
    }, environment: "production");
}
catch (VaultConfigurationException ex)
{
    // Handle configuration errors (missing required fields, invalid types, etc.)
    Console.Error.WriteLine($"Configuration error: {ex.Message}");
    throw;
}
catch (VaultAuthenticationException ex)
{
    // Handle authentication failures
    Console.Error.WriteLine($"Authentication failed: {ex.Message}");
    throw;
}
catch (FileNotFoundException ex) when (ex.Message.Contains("Token file"))
{
    // Handle missing token file for Local authentication
    Console.Error.WriteLine($"Token file not found: {ex.Message}");
    throw;
}
```

## Best Practices

### 1. Use Environment Variables for Sensitive Data

```csharp
builder.AddVault(options =>
{
    options.Configuration = new VaultLocalConfiguration
    {
        VaultUrl = Environment.GetEnvironmentVariable("VAULT_URL")
            ?? throw new InvalidOperationException("VAULT_URL not set"),
        MountPoint = "secret",
        TokenFilePath = "~/.vault-token"
    };
}, environment: builder.Environment.EnvironmentName);
```

### 2. Validate Configuration on Startup

The library automatically validates configuration, but catch exceptions early:

```csharp
try
{
    var app = builder.Build();
    await app.RunAsync();
}
catch (VaultConfigurationException ex)
{
    // Log and exit gracefully
    Console.Error.WriteLine($"Failed to start: {ex.Message}");
    Environment.Exit(1);
}
```

### 3. Use Appropriate Authentication for Each Environment

```csharp
var isDevelopment = builder.Environment.IsDevelopment();

builder.AddVault(options =>
{
    if (isDevelopment)
    {
        options.AuthenticationType = VaultAuthenticationType.Local;
        options.Configuration = new VaultLocalConfiguration { /* ... */ };
    }
    else
    {
        options.AuthenticationType = VaultAuthenticationType.AWS_IAM;
        options.Configuration = new VaultAwsConfiguration { /* ... */ };
    }
}, environment: builder.Environment.EnvironmentName);
```

### 4. Don't Disable SSL in Production

```csharp
options.Configuration = new VaultLocalConfiguration
{
    VaultUrl = "https://vault.example.com",
    MountPoint = "secret",
    TokenFilePath = "~/.vault-token",
    // Only for development/testing!
    IgnoreSslErrors = builder.Environment.IsDevelopment()
};
```

## Next Steps

- Explore [Features](features.md) for detailed capabilities
- Check out example projects in the repository

## Troubleshooting

### "Unable to load service index" error

Ensure your Vault URL is correct and accessible from your application.

### "Authentication failed" error

Verify your authentication credentials:
- **Local**: Check token file exists and contains valid token
- **AWS IAM**: Verify IAM role has proper permissions and Vault role configuration
- **Custom**: Ensure custom auth method info is correctly configured

### Secrets not appearing in configuration

Check that:
1. The `environment` parameter matches your Vault path
2. Secrets exist at `{MountPoint}/{environment}`
3. The authenticated identity has read permissions on the secrets path

### SSL certificate errors

For development only, you can disable SSL validation:
```csharp
IgnoreSslErrors = true // Not recommended for production
```

For production, ensure proper SSL certificates are configured on your Vault server.
