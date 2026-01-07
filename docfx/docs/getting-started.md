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

### Step 1: Register Vault Services

Use the `AddVault` extension method on `IServiceCollection` to register Vault services:

```csharp
using Vault.Extentions;
using Vault.Options;
using Vault.Enum;

var builder = WebApplication.CreateBuilder(args);

// Register VaultService with dependency injection
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

// Initialize Vault providers (required!)
app.UseVault();

app.Run();
```

### Step 2: Choose Your Authentication Method

The library supports three authentication methods:

#### Option A: Local Token (Development)

Best for local development with a Vault token file:

```csharp
var vaultOptions = new VaultOptions
{
    AuthenticationType = VaultAuthenticationType.Local,
    Configuration = new VaultLocalConfiguration
    {
        VaultUrl = "https://vault.example.com",
        MountPoint = "secret",
        TokenFilePath = "~/.vault-token",
        IgnoreSslErrors = false // Only true for development
    }
};

builder.Services.AddVault(builder.Configuration, vaultOptions, environment: "development");
```

#### Option B: AWS IAM (Production on AWS)

Ideal for applications running on AWS infrastructure:

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

#### Option C: Custom Authentication

For other authentication methods (AppRole, LDAP, UserPass, Kubernetes, etc.):

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

## Using Secrets in Your Application

### Method 1: IConfiguration (Recommended for Static Configuration)

Secrets are automatically loaded into the configuration system:

```csharp
public class DatabaseService
{
    private readonly IConfiguration _configuration;

    public DatabaseService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void Connect()
    {
        // Access secrets like any other configuration value
        var connectionString = _configuration["Database:ConnectionString"];
        var password = _configuration["Database:Password"];

        // Use the secrets
        Console.WriteLine($"Connecting with password: {password?.Substring(0, 3)}...");
    }
}
```

### Method 2: IOptions Pattern (Strongly-Typed Configuration)

Bind Vault secrets to strongly-typed classes:

```csharp
// Define your settings class
public class DatabaseSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public int Timeout { get; set; } = 30;
}

// In Program.cs - bind configuration section
builder.Services.Configure<DatabaseSettings>(
    builder.Configuration.GetSection("Database"));

// Use in your services with type safety
public class DatabaseService
{
    private readonly DatabaseSettings _settings;

    public DatabaseService(IOptions<DatabaseSettings> options)
    {
        _settings = options.Value;
    }

    public void Connect()
    {
        var fullConnectionString = $"{_settings.ConnectionString};Password={_settings.Password};Timeout={_settings.Timeout}";
        // Use connection string
    }
}
```

### Method 3: IVaultService (Dynamic Secret Retrieval)

For runtime secret access and dynamic scenarios:

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
    public async Task<string?> GetApiKeyAsync()
    {
        var apiKey = await _vaultService.GetSecretValueAsync("production", "ApiKey");
        return apiKey?.ToString();
    }

    // Get all secrets for an environment
    public async Task<Dictionary<string, object>> LoadAllSecretsAsync()
    {
        return await _vaultService.GetSecretsAsync("production");
    }

    // Get nested secret using dot notation
    public async Task<string?> GetNestedValueAsync()
    {
        // Access nested structure: { "Api": { "Settings": { "Key": "value" } } }
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

## Complete Example

Here's a complete example combining all three approaches:

```csharp
using Vault.Extentions;
using Vault.Options;
using Vault.Enum;
using Vault.Abstractions;

var builder = WebApplication.CreateBuilder(args);

// 1. Configure Vault
var vaultOptions = new VaultOptions
{
    AuthenticationType = VaultAuthenticationType.AWS_IAM,
    Configuration = new VaultAwsConfiguration
    {
        VaultUrl = "https://vault.example.com",
        MountPoint = "myapp",
        Environment = "production"
    }
};

builder.Services.AddVault(builder.Configuration, vaultOptions, environment: "production");

// 2. Register strongly-typed configuration
public class ApiSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
}

builder.Services.Configure<ApiSettings>(builder.Configuration.GetSection("Api"));

// 3. Register your services
builder.Services.AddScoped<SecretManager>();

var app = builder.Build();

// Initialize Vault (required!)
app.UseVault();

// Example endpoints showing all three usage patterns
app.MapGet("/config", (IConfiguration config) =>
{
    return new
    {
        apiKey = config["Api:ApiKey"],
        baseUrl = config["Api:BaseUrl"]
    };
});

app.MapGet("/options", (IOptions<ApiSettings> options) =>
{
    return new { settings = options.Value };
});

app.MapGet("/vault", async (IVaultService vault) =>
{
    return await vault.GetSecretsAsync("production");
});

app.Run();
```



## Vault Server Setup

### Required Vault Configuration

1. **Enable KV Secrets Engine (v2)**:
   ```bash
   vault secrets enable -path=secret kv-v2
   ```

2. **Create secrets for your environment**:
   ```bash
   # Simple key-value secrets
   vault kv put secret/production \
       ApiKey="your-api-key" \
       Database:ConnectionString="Server=..." \
       Database:Password="secret-password"

   # Nested structure
   vault kv put secret/production \
       Api.Settings.Key="api-key" \
       Api.Settings.BaseUrl="https://api.example.com"
   ```

3. **Configure authentication method**:

   **For Local (token-based)**:
   ```bash
   # Generate a token
   vault token create -policy=secret-read
   # Save token to file (e.g., ~/.vault-token)
   ```

   **For AWS IAM**:
   ```bash
   vault auth enable aws
   vault write auth/aws/role/myapp-production-role \
       auth_type=iam \
       bound_iam_principal_arn="arn:aws:iam::123456789:role/my-app-role" \
       policies=myapp-production-policy \
       max_ttl=1h
   ```

   **For AppRole**:
   ```bash
   vault auth enable approle
   vault write auth/approle/role/my-app \
       token_policies=secret-read \
       token_ttl=1h
   vault read auth/approle/role/my-app/role-id
   vault write -f auth/approle/role/my-app/secret-id
   ```

4. **Create policy for secret access**:
   ```bash
   vault policy write myapp-production-policy - <<EOF
   path "secret/data/production/*" {
     capabilities = ["read", "list"]
   }
   path "secret/metadata/production/*" {
     capabilities = ["read", "list"]
   }
   EOF
   ```

## Important: UseVault() Call

**You must call `app.UseVault()` after building your application!** This initializes the Vault configuration providers:

```csharp
var app = builder.Build();

// This is REQUIRED - do not forget!
app.UseVault();

app.MapControllers();
app.Run();
```

Without `UseVault()`, secrets won't be loaded into the configuration system.

## Error Handling

The library provides specific exceptions for different scenarios:

```csharp
using Vault.Exceptions;

try
{
    var vaultOptions = new VaultOptions { /* ... */ };
    builder.Services.AddVault(builder.Configuration, vaultOptions, environment: "production");

    var app = builder.Build();
    app.UseVault();
}
catch (VaultConfigurationException ex)
{
    // Configuration validation errors
    Console.Error.WriteLine($"Configuration error: {ex.Message}");
    throw;
}
catch (VaultAuthenticationException ex)
{
    // Authentication failures
    Console.Error.WriteLine($"Authentication failed: {ex.Message}");
    throw;
}
catch (FileNotFoundException ex) when (ex.Message.Contains("Token file"))
{
    // Missing token file (Local authentication)
    Console.Error.WriteLine($"Token file not found: {ex.Message}");
    throw;
}
```

## Best Practices

### 1. Use Environment-Specific Configuration

```csharp
var environment = builder.Environment.EnvironmentName; // "Development", "Production", etc.

var vaultOptions = new VaultOptions
{
    AuthenticationType = builder.Environment.IsDevelopment()
        ? VaultAuthenticationType.Local
        : VaultAuthenticationType.AWS_IAM,
    Configuration = builder.Environment.IsDevelopment()
        ? new VaultLocalConfiguration
        {
            VaultUrl = "http://localhost:8200",
            MountPoint = "secret",
            TokenFilePath = "~/.vault-token",
            IgnoreSslErrors = true // Only in development!
        }
        : new VaultAwsConfiguration
        {
            VaultUrl = "https://vault.production.example.com",
            MountPoint = "myapp",
            Environment = "production"
        }
};

builder.Services.AddVault(builder.Configuration, vaultOptions, environment: environment);
```

### 2. Use Environment Variables for Vault Configuration

```csharp
var vaultOptions = new VaultOptions
{
    AuthenticationType = VaultAuthenticationType.Local,
    Configuration = new VaultLocalConfiguration
    {
        VaultUrl = Environment.GetEnvironmentVariable("VAULT_URL")
            ?? throw new InvalidOperationException("VAULT_URL not set"),
        MountPoint = Environment.GetEnvironmentVariable("VAULT_MOUNT_POINT") ?? "secret",
        TokenFilePath = Environment.GetEnvironmentVariable("VAULT_TOKEN_FILE") ?? "~/.vault-token"
    }
};
```

### 3. Never Disable SSL in Production

```csharp
var vaultOptions = new VaultOptions
{
    Configuration = new VaultLocalConfiguration
    {
        VaultUrl = "https://vault.example.com",
        MountPoint = "secret",
        TokenFilePath = "~/.vault-token",
        // NEVER set to true in production!
        IgnoreSslErrors = builder.Environment.IsDevelopment()
    }
};
```

### 4. Validate Configuration Early

```csharp
try
{
    var app = builder.Build();
    app.UseVault();
    await app.RunAsync();
}
catch (VaultConfigurationException ex)
{
    Console.Error.WriteLine($"Vault configuration invalid: {ex.Message}");
    Environment.Exit(1);
}
```

### 5. Choose the Right Access Pattern

- **IConfiguration**: Best for secrets needed at startup (database connections, API keys loaded once)
- **IOptions<T>**: Best for strongly-typed configuration with validation
- **IVaultService**: Best for dynamic secret retrieval, refreshing secrets, or runtime access

## Troubleshooting

### "VaultService not registered" error

Ensure you call `AddVault()` before building the application:

```csharp
builder.Services.AddVault(builder.Configuration, vaultOptions, environment: "production");
var app = builder.Build();
```

### Secrets not appearing in IConfiguration

1. Verify you called `app.UseVault()` after building
2. Check the environment parameter matches your Vault path
3. Ensure secrets exist at `{MountPoint}/data/{environment}/` (KV v2)
4. Verify authentication and permissions

### Authentication failures

- **Local**: Check token file exists, is readable, and contains a valid token
  ```bash
  cat ~/.vault-token
  vault token lookup
  ```

- **AWS IAM**: Ensure the EC2/ECS instance has the correct IAM role attached
  ```bash
  curl http://169.254.169.254/latest/meta-data/iam/security-credentials/
  ```

- **AppRole**: Verify role-id and secret-id are correct
  ```bash
  vault write auth/approle/login role_id="..." secret_id="..."
  ```

### SSL certificate errors

For production, ensure proper certificates. For development:

```csharp
IgnoreSslErrors = builder.Environment.IsDevelopment()
```

Or configure your system to trust the Vault CA certificate.

### "Unable to load service index" or connection errors

- Verify Vault URL is accessible from your application
- Check firewall rules and network connectivity
- Ensure Vault is running:
  ```bash
  vault status
  ```

## Next Steps

- Learn about all [Features](features.md) and advanced capabilities
- Check the [API Reference](../api/index.html) for detailed documentation
- See example projects in the GitHub repository
