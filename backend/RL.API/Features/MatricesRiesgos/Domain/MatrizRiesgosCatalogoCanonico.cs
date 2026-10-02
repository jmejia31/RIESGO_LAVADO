using System.Reflection;
using System.Text.Json;
using CatalogElement = RL.API.Features.Catalogos.Contracts.ElementoCatalogoMatricesDto;

namespace RL.API.Features.MatricesRiesgos.Domain;

/// <summary>
/// Read-only catalog resolver backed by the versioned institutional manifest.
/// Canonical keys and explicitly listed aliases are the only accepted inputs.
/// </summary>
public static class MatrizRiesgosCatalogoCanonico
{
    private static readonly IReadOnlyDictionary<string, CatalogDefinition> Catalogs = Load();

    public static string ObtenerEtiqueta(string catalogId, string value)
    {
        CatalogItem item = ObtenerItem(catalogId, value);
        return item.Label;
    }

    public static string NormalizarClave(string catalogId, string value)
    {
        CatalogItem item = ObtenerItem(catalogId, value);
        return item.Key;
    }

    public static string ObtenerClavePersistente(string catalogId, string value)
    {
        CatalogItem item = ObtenerItem(catalogId, value);
        return item.DbKey;
    }

    public static bool EsValorValido(string catalogId, string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || !Catalogs.TryGetValue(catalogId, out CatalogDefinition? catalog)) return false;
        string candidate = value.Trim();
        return catalog.Items.Count(item =>
            string.Equals(item.Key, candidate, StringComparison.Ordinal)
            || string.Equals(item.Label, candidate, StringComparison.Ordinal)
            || string.Equals(item.DbKey, candidate, StringComparison.Ordinal)
            || item.Aliases.Contains(candidate, StringComparer.Ordinal)) == 1;
    }

    public static bool EsPorcentajeEfectividadValido(decimal? value) => value is null
        || Catalogs["CONTROL_EFFECTIVENESS"].Items.Any(item => item.Percentage.HasValue && item.Percentage.Value * 100m == value.Value);

    public static string ObtenerEtiquetaNivelRiesgo(int value) =>
        ObtenerEtiqueta("RISK_LEVEL", value.ToString(System.Globalization.CultureInfo.InvariantCulture));

    public static CatalogSnapshot CrearSnapshotNivelRiesgo(string catalogCode = "CAT_NIVEL_RIESGO")
    {
        CatalogDefinition catalog = Catalogs["RISK_LEVEL"];
        CatalogElement[] elements = catalog.Items.Select((item, index) => new CatalogElement(
            index + 1, item.Key, item.Label, index + 1, true)).ToArray();
        return new(catalogCode, true, elements);
    }

    private static CatalogItem ObtenerItem(string catalogId, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(catalogId);
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        if (!Catalogs.TryGetValue(catalogId, out CatalogDefinition? catalog))
            throw new InvalidOperationException($"El catálogo '{catalogId}' no existe en el manifiesto institucional.");

        string candidate = value.Trim();
        CatalogItem[] matches = catalog.Items.Where(item =>
            string.Equals(item.Key, candidate, StringComparison.Ordinal)
            || string.Equals(item.Label, candidate, StringComparison.Ordinal)
            || string.Equals(item.DbKey, candidate, StringComparison.Ordinal)
            || item.Aliases.Contains(candidate, StringComparer.Ordinal)).ToArray();
        return matches.Length switch
        {
            1 => matches[0],
            0 => throw new InvalidOperationException($"El valor '{candidate}' no está definido en el catálogo '{catalogId}'."),
            _ => throw new InvalidOperationException($"El valor '{candidate}' es ambiguo en el catálogo '{catalogId}'.")
        };
    }

    private static IReadOnlyDictionary<string, CatalogDefinition> Load()
    {
        Assembly assembly = typeof(MatrizRiesgosCatalogoCanonico).Assembly;
        string resourceName = assembly.GetManifestResourceNames().Single(name => name.EndsWith(
            "matriz_riesgos_catalogos_manifest.json", StringComparison.Ordinal));
        using Stream stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException("No se pudo leer el manifiesto de catálogos institucionales.");
        using JsonDocument document = JsonDocument.Parse(stream);
        JsonElement catalogs = document.RootElement.GetProperty("catalogs");
        var definitions = new Dictionary<string, CatalogDefinition>(StringComparer.Ordinal);
        foreach (JsonElement catalog in catalogs.EnumerateArray())
        {
            string id = catalog.GetProperty("catalogId").GetString()!;
            var items = new List<CatalogItem>();
            foreach (JsonElement item in catalog.GetProperty("items").EnumerateArray())
            {
                string[] aliases = item.TryGetProperty("aliases", out JsonElement aliasArray)
                    ? aliasArray.EnumerateArray().Select(alias => alias.GetString()!).ToArray()
                    : Array.Empty<string>();
                string key = item.GetProperty("key").GetString()!;
                string dbKey = item.TryGetProperty("dbKey", out JsonElement databaseKey)
                    ? databaseKey.ValueKind == JsonValueKind.String ? databaseKey.GetString()! : string.Empty
                    : key;
                decimal? percentage = item.TryGetProperty("percentage", out JsonElement percentageValue)
                    ? percentageValue.GetDecimal()
                    : null;
                items.Add(new(key, item.GetProperty("label").GetString()!, dbKey, aliases, percentage));
            }
            if (!definitions.TryAdd(id, new(items)))
                throw new InvalidOperationException($"Catálogo duplicado en el manifiesto: '{id}'.");
        }
        return definitions;
    }

    private sealed record CatalogDefinition(IReadOnlyList<CatalogItem> Items);
    private sealed record CatalogItem(string Key, string Label, string DbKey, string[] Aliases, decimal? Percentage);
}
