# Features

Vault.Extension.Configuration provides comprehensive integration with HashiCorp Vault for .NET applications. This page details all available features and capabilities.

## 🔐 Multiple Authentication Methods

The library supports three authentication methods to fit different deployment scenarios:

### Local Token Authentication

Ideal for development environments where a Vault token file is available:

```csharp
using Vault.Extentions;
using Vault.Options;
using Vault.Enum;

var vaultOptions = new VaultOptions
{
    AuthenticationType = VaultAuthenticationType.Local,
    Configuration = new VaultLocalConfiguration
    {
        VaultUrl = "https://vault.example.com",
        MountPoint = "secret",
        TokenFilePath = "~/.vault-token",
        IgnoreSslErrors = false
    }
};

builder.Services.AddVault(builder.Configuration, vaultOptions, environment: "development");
```

**Configuration Properties:**
- `VaultUrl`: Vault server URL
- `MountPoint`: KV secrets engine mount point
- `TokenFilePath`: Path to the token file (supports environment variable expansion like `~/`)
- `IgnoreSslErrors`: Disable SSL validation (use only for development)

### AWS IAM Authentication

Perfect for applications running on AWS infrastructure (EC2, ECS, EKS, Lambda):

```csharp
var vaultOptions = new VaultOptions
{
    AuthenticationType = VaultAuthenticationType.AWS_IAM,
    Configuration = new VaultAwsConfiguration
    {
        VaultUrl = "https://vault.example.com",
        MountPoint = "myapp",
        Environment = "production",
        AwsAuthMountPoint = "aws",
        AwsIamRoleName = "myapp-production-role" // Optional
    }
};

builder.Services.AddVault(builder.Configuration, vaultOptions, environment: "production");
```

**Configuration Properties:**
- `VaultUrl`: Vault server URL
- `MountPoint`: KV secrets engine mount point
- `Environment`: Environment name (used for role naming)
- `AwsAuthMountPoint`: AWS auth method mount point (default: `"aws"`)
- `AwsIamRoleName`: IAM role name (optional, defaults to `{MountPoint}-{Environment}-role`)
- `IgnoreSslErrors`: Disable SSL validation

**How it works:**
- Automatically retrieves AWS credentials from the instance metadata service
- Signs the authentication request with instance IAM role
- No hardcoded credentials needed in your application

### Custom Authentication

For authentication methods not natively supported (AppRole, LDAP, UserPass, Kubernetes, etc.):

```csharp
using VaultSharp.V1.AuthMethods.AppRole;
using VaultSharp.V1.AuthMethods.UserPass;

// AppRole example
var vaultOptions = new VaultOptions
{
    AuthenticationType = VaultAuthenticationType.Custom,
    Configuration = new VaultDefaultConfiguration
    {
        VaultUrl = "https://vault.example.com",
        MountPoint = "secret"
    },
    CustomAuthMethodInfo = new AppRoleAuthMethodInfo("role-id", "secret-id")
};

// UserPass example
var vaultOptionsUserPass = new VaultOptions
{
    AuthenticationType = VaultAuthenticationType.Custom,
    Configuration = new VaultDefaultConfiguration
    {
        VaultUrl = "https://vault.example.com",
        MountPoint = "secret"
    },
    CustomAuthMethodInfo = new UserPassAuthMethodInfo("username", "password")
};

builder.Services.AddVault(builder.Configuration, vaultOptions, environment: "production");
```

## ⚙️ Seamless Configuration Integration

### IConfiguration Access

Secrets are automatically loaded into the ASP.NET Core configuration system, making them accessible just like appsettings.json values:

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
        // Access Vault secrets like any other configuration value
        var connectionString = _configuration["Database:ConnectionString"];
        var password = _configuration["Database:Password"];
        var timeout = _configuration.GetValue<int>("Database:Timeout", 30);

        Console.WriteLine($"Connecting to database with timeout {timeout}s");
    }
}
```

**Key Features:**
- Hierarchical keys using colon notation (`Database:Password`)
- Automatic type conversion with `GetValue<T>`
- Fallback values for missing keys
- Secrets override appsettings.json values (by default)

### IOptions Pattern Support

Bind Vault secrets to strongly-typed configuration classes with full validation support:

```csharp
// Define your settings class
public class EmailSettings
{
    public string SmtpServer { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty; // From Vault
}

// In Program.cs
builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("Email"));

// Use in your services
public class EmailService
{
    private readonly EmailSettings _settings;

    public EmailService(IOptions<EmailSettings> options)
    {
        _settings = options.Value;
    }

    public void SendEmail()
    {
        // Use settings with full IntelliSense support
        var client = new SmtpClient(_settings.SmtpServer, _settings.Port)
        {
            Credentials = new NetworkCredential(_settings.Username, _settings.Password)
        };
    }
}
```

**Benefits:**
- Strong typing with compile-time safety
- IntelliSense support in your IDE
- Works with `IOptionsMonitor<T>` for configuration reloading
- Compatible with data annotations validation



## 🔄 Direct Vault Access with IVaultService

For dynamic secret retrieval at runtime, inject and use `IVaultService`:

```csharp
using Vault.Abstractions;

public class SecretManager
{
    private readonly IVaultService _vaultService;
    private readonly ILogger<SecretManager> _logger;

    public SecretManager(IVaultService vaultService, ILogger<SecretManager> logger)
    {
        _vaultService = vaultService;
        _logger = logger;
    }

    // Get a specific secret value
    public async Task<string?> GetApiKeyAsync(string environment)
    {
        var value = await _vaultService.GetSecretValueAsync(environment, "ApiKey");
        return value?.ToString();
    }

    // Get all secrets for an environment
    public async Task<Dictionary<string, object>> LoadEnvironmentSecretsAsync(string environment)
    {
        _logger.LogInformation("Loading secrets for environment: {Environment}", environment);
        return await _vaultService.GetSecretsAsync(environment);
    }

    // Get nested secret using dot notation
    public async Task<string?> GetNestedSecretAsync()
    {
        // Access nested structure: { "Api": { "Settings": { "Key": "value" } } }
        var value = await _vaultService.GetNestedSecretValueAsync("production", "Api.Settings.Key");
        return value?.ToString();
    }

    // List all available environments
    public async Task<IEnumerable<string>> GetAvailableEnvironmentsAsync()
    {
        return await _vaultService.ListEnvironmentsAsync();
    }

    // Refresh secrets at runtime
    public async Task RefreshSecretsAsync(string environment)
    {
        var secrets = await _vaultService.GetSecretsAsync(environment);
        _logger.LogInformation("Loaded {Count} secrets", secrets.Count);
        // Process updated secrets
    }
}
```

### IVaultService Methods

| Method | Description | Returns |
|--------|-------------|---------|
| `ListEnvironmentsAsync()` | Lists all environment paths in Vault | `IEnumerable<string>` |
| `GetSecretsAsync(string environment)` | Retrieves all secrets for an environment | `Dictionary<string, object>` |
| `GetSecretValueAsync(string environment, string key)` | Gets a specific secret value | `object?` |
| `GetNestedSecretValueAsync(string environment, string path)` | Gets nested secret using dot notation | `object?` |

**Use Cases for IVaultService:**
- Dynamic secret retrieval during application runtime
- Refreshing secrets without restarting the application
- Loading secrets based on user input or business logic
- Querying available environments
- Accessing nested JSON structures in secrets

## ✅ Automatic Configuration Validation

Configuration is validated using FluentValidation on startup:

```csharp
var vaultOptions = new VaultOptions
{
    AuthenticationType = VaultAuthenticationType.Local,
    Configuration = new VaultLocalConfiguration
    {
        // Missing VaultUrl - will throw VaultConfigurationException
        MountPoint = "secret",
        TokenFilePath = "~/.vault-token"
    }
};

// Throws VaultConfigurationException with detailed error message
builder.Services.AddVault(builder.Configuration, vaultOptions, environment: "production");
```

**Validated Rules:**
- Required fields: `VaultUrl`, `MountPoint`, `TokenFilePath` (for Local), etc.
- Configuration type must match authentication type
- File paths are validated for Local authentication
- Custom auth method info is required when using Custom authentication
- Environment name cannot be empty

**Validation Errors Example:**
```
VaultConfigurationException:
- 'Vault Url' must not be empty.
- 'Token File Path' is required for Local authentication.
```

## 🏗️ Full Dependency Injection Support

The library integrates seamlessly with ASP.NET Core's DI container:

### Registration

```csharp
using Vault.Extentions;

var builder = WebApplication.CreateBuilder(args);

// Register Vault services
var vaultOptions = new VaultOptions { /* ... */ };
builder.Services.AddVault(builder.Configuration, vaultOptions, environment: "production");

// Register your services that depend on Vault
builder.Services.AddScoped<SecretManager>();
builder.Services.AddSingleton<CacheService>();

var app = builder.Build();

// Initialize Vault providers
app.UseVault();
```

### Injection

```csharp
// Inject IVaultService
public class ApiController : ControllerBase
{
    private readonly IVaultService _vaultService;

    public ApiController(IVaultService vaultService)
    {
        _vaultService = vaultService;
    }

    [HttpGet("secrets")]
    public async Task<IActionResult> GetSecrets()
    {
        var secrets = await _vaultService.GetSecretsAsync("production");
        return Ok(new { count = secrets.Count });
    }
}

// Inject IConfiguration
public class DatabaseFactory
{
    private readonly IConfiguration _configuration;

    public DatabaseFactory(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public DbContext CreateContext()
    {
        var connectionString = _configuration["Database:ConnectionString"];
        return new MyDbContext(connectionString);
    }
}

// Inject IOptions
public class EmailService
{
    private readonly EmailSettings _settings;

    public EmailService(IOptions<EmailSettings> options)
    {
        _settings = options.Value;
    }
}
```

## 🔒 Security Features

### SSL Certificate Validation

By default, SSL certificates are validated. Only disable for development:

```csharp
var vaultOptions = new VaultOptions
{
    Configuration = new VaultLocalConfiguration
    {
        VaultUrl = "https://vault.local",
        MountPoint = "secret",
        TokenFilePath = "~/.vault-token",
        // ONLY for development!
        IgnoreSslErrors = builder.Environment.IsDevelopment()
    }
};
```

**Production Recommendation:**
- Always use valid SSL certificates
- Configure your system to trust Vault's CA if using internal PKI
- Never set `IgnoreSslErrors = true` in production

### Secure Token Handling

For Local authentication, tokens are handled securely:
- Token file content is never logged
- File is read once at startup
- Supports environment variable expansion (`~/.vault-token`, `%USERPROFILE%\.vault-token`)
- File permissions are respected by the OS

### Least Privilege Access

The library only requires **read** access to Vault secrets:

```hcl
# Example Vault policy
path "secret/data/production/*" {
  capabilities = ["read", "list"]
}

path "secret/metadata/production/*" {
  capabilities = ["read", "list"]
}
```

## 🎯 Environment-Based Secret Management

Organize secrets by environment for proper isolation:

```csharp
var environment = builder.Environment.EnvironmentName; // "Development", "Staging", "Production"

var vaultOptions = new VaultOptions
{
    AuthenticationType = VaultAuthenticationType.AWS_IAM,
    Configuration = new VaultAwsConfiguration
    {
        VaultUrl = "https://vault.example.com",
        MountPoint = "myapp",
        Environment = environment.ToLower()
    }
};

builder.Services.AddVault(builder.Configuration, vaultOptions, environment: environment.ToLower());
```

**Vault Structure:**
```
secret/
├── development/
│   ├── Database:ConnectionString
│   └── Api:Key
├── staging/
│   ├── Database:ConnectionString
│   └── Api:Key
└── production/
    ├── Database:ConnectionString
    └── Api:Key
```

## 📦 Exception Handling

The library provides specific exception types for detailed error handling:

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
    // Configuration validation errors (missing fields, wrong types, etc.)
    _logger.LogError(ex, "Vault configuration is invalid");
    throw;
}
catch (VaultAuthenticationException ex)
{
    // Authentication failures (invalid token, insufficient permissions)
    _logger.LogError(ex, "Failed to authenticate with Vault");
    throw;
}
catch (FileNotFoundException ex) when (ex.Message.Contains("Token file"))
{
    // Token file not found (Local authentication only)
    _logger.LogError(ex, "Vault token file not found");
    throw;
}
catch (HttpRequestException ex)
{
    // Network errors (Vault server unreachable)
    _logger.LogError(ex, "Cannot connect to Vault server");
    throw;
}
```

## 🚀 Advanced Features

### Configuration Loading Order

Vault secrets are loaded **after** other configuration sources, so they override:

1. appsettings.json
2. appsettings.{Environment}.json
3. User secrets (development only)
4. Environment variables
5. **Vault secrets** ← Highest priority

### UseVault() Initialization

The `UseVault()` call is required to inject `IVaultService` into configuration providers:

```csharp
var app = builder.Build();

// This initializes all Vault configuration providers
// Without this, secrets won't be loaded!
app.UseVault();

app.MapControllers();
app.Run();
```

**What it does:**
- Locates all `VaultConfigurationProvider` instances in the configuration
- Injects the registered `IVaultService`
- Triggers secret loading from Vault

### Hierarchical Configuration Keys

Use colon notation for hierarchical keys:

```csharp
// In Vault: "Database:ConnectionString" = "Server=..."

// Access in code:
var connectionString = _configuration["Database:ConnectionString"];

// Or bind to nested class:
public class AppSettings
{
    public DatabaseConfig Database { get; set; }
}

public class DatabaseConfig
{
    public string ConnectionString { get; set; }
}
```

## 📊 Comparison: When to Use Each Method

| Method | Best For | Pros | Cons |
|--------|----------|------|------|
| **IConfiguration** | Static secrets needed at startup | Simple, familiar API | No type safety |
| **IOptions<T>** | Strongly-typed configuration | Type safety, IntelliSense, validation | Requires class definition |
| **IVaultService** | Dynamic runtime access | Flexible, can refresh secrets | More code, async |

**Recommendation:**
- Use **IConfiguration** for simple access
- Use **IOptions<T>** for production applications with validation
- Use **IVaultService** when you need to refresh or dynamically query secrets
