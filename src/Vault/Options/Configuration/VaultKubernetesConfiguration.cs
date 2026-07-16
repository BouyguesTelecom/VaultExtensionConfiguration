namespace Vault.Options.Configuration;

/// <summary>
/// Configuration for automatic authentication via Kubernetes
/// Uses the JWT token of the pod's service account to authenticate against a Kubernetes
/// auth backend mounted in Vault.
/// </summary>
public class VaultKubernetesConfiguration
    : VaultDefaultConfiguration
{
    /// <summary>
    /// Vault role name for Kubernetes authentication (optional)
    /// If not provided, the role will be automatically deduced according to the standard pattern:
    /// {MountPoint}-{Environment}-role
    /// Example: MountPoint="Point-Break", Environment="dev" -> "Point-Break-dev-role"
    /// Vault role names are case-sensitive: if your Vault role was created in lowercase,
    /// make sure MountPoint/Environment (or this property) are already lowercase.
    /// If you want to use a different role name, explicitly define this property.
    /// </summary>
    public string? KubernetesRoleName { get; set; }

    /// <summary>
    /// Deployment environment (dev, test, prod, thomas, etc.)
    /// Used to automatically build the Vault role name if KubernetesRoleName is not defined
    /// Pattern: {MountPoint}-{Environment}-role.
    /// </summary>
    public string? Environment { get; set; }

    /// <summary>
    /// Kubernetes auth method mount point in Vault.
    /// No default value: this always depends on the target cluster, e.g. "ocp-1", "ocp-2"
    /// for standard OCP clusters, or "kubeshift/my-cluster" for Kubeshift clusters.
    /// Must be set explicitly.
    /// </summary>
    public string KubernetesAuthMountPoint { get; set; } = string.Empty;

    /// <summary>
    /// Path to the file containing the service account JWT token
    /// Default convention: standard Kubernetes projected token path.
    /// Some clusters (e.g. Kubeshift) use a different bound/projected token path
    /// (e.g. "/var/run/secrets/tokens/vault-token") - override this property in that case.
    /// </summary>
    public string ServiceAccountTokenPath { get; set; } = "/var/run/secrets/kubernetes.io/serviceaccount/token";
}
