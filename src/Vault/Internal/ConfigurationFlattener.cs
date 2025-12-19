using System.Text.Json;

namespace Vault.Internal;

/// <summary>
/// Helper pour aplatir les structures JSON complexes au format de configuration .NET.
/// </summary>
internal static class ConfigurationFlattener
{
    /// <summary>
    /// Aplatit un dictionnaire récursivement pour le format de configuration .NET.
    /// Exemple: { "level1": { "level2": { "level3": "value" } } } devient { "level1:level2:level3": "value" }.
    /// </summary>
    internal static Dictionary<string, string?> FlattenDictionary(
        Dictionary<string, object> source,
        string? prefix = null)
    {
        var result = new Dictionary<string, string?>();

        foreach (var kvp in source)
        {
            var key = string.IsNullOrWhiteSpace(prefix)
                ? kvp.Key
                : $"{prefix}:{kvp.Key}";

            if (kvp.Value is Dictionary<string, object> nestedDict)
            {
                // Récursion pour les objets imbriqués
                var flattened = FlattenDictionary(nestedDict, key);
                foreach (var item in flattened)
                {
                    result[item.Key] = item.Value;
                }
            }
            else if (kvp.Value is JsonElement jsonElement)
            {
                // Gérer les JsonElement retournés par VaultSharp
                FlattenJsonElement(jsonElement, key, result);
            }
            else
            {
                // Valeur simple
                result[key] = kvp.Value?.ToString();
            }
        }

        return result;
    }

    /// <summary>
    /// Aplatit un JsonElement récursivement.
    /// </summary>
    private static void FlattenJsonElement(
        JsonElement element,
        string key,
        Dictionary<string, string?> result)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                // Objet imbriqué - récursion
                foreach (var property in element.EnumerateObject())
                {
                    var nestedKey = $"{key}:{property.Name}";
                    FlattenJsonElement(property.Value, nestedKey, result);
                }

                break;

            case JsonValueKind.Array:
                // Tableau - indexer les éléments
                int index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    var arrayKey = $"{key}:{index}";
                    FlattenJsonElement(item, arrayKey, result);
                    index++;
                }

                break;

            case JsonValueKind.String:
                result[key] = element.GetString();
                break;

            case JsonValueKind.Number:
                result[key] = element.GetRawText();
                break;

            case JsonValueKind.True:
            case JsonValueKind.False:
                result[key] = element.GetBoolean().ToString();
                break;

            case JsonValueKind.Null:
                result[key] = null;
                break;

            default:
                result[key] = element.GetRawText();
                break;
        }
    }
}
