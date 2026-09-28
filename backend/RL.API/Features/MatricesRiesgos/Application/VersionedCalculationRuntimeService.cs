using System.Text.Json;
using System.Text.RegularExpressions;
using System.Globalization;
using RL.API.Features.Catalogos.Contracts;
using RL.API.Features.MatricesRiesgos.Contracts;
using RL.API.Features.MatricesRiesgos.Domain;
using RL.API.Features.MatricesRiesgos.Persistence;
using CatalogElement = RL.API.Features.Catalogos.Contracts.ElementoCatalogoMatricesDto;

namespace RL.API.Features.MatricesRiesgos.Application;

public sealed record GovernedCalculationResult(bool IsGoverned, FormulaEvaluationResult? Evaluation);

/// <summary>Construye el único runtime pinneado de una versión de formulario y evalúa sus bindings FUS.</summary>
public sealed class VersionedCalculationRuntimeService
{
    private static readonly Regex Sha256 = new("^[0-9A-Fa-f]{64}$", RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private readonly ICalculoConfiguracionRepository _configuration;
    private readonly DbDrivenCalculationRuntimeFactory _factory;
    private readonly FormulaEngine _engine;
    private readonly PublicationGate _publicationGate;

    public VersionedCalculationRuntimeService(ICalculoConfiguracionRepository configuration, DbDrivenCalculationRuntimeFactory factory, FormulaEngine? engine = null)
    {
        _configuration = configuration;
        _factory = factory;
        _engine = engine ?? new FormulaEngine();
        _publicationGate = new PublicationGate(_engine);
    }

    public async Task<GovernedCalculationResult> CalculateAsync(
        long versionFormularioId,
        string definitionJson,
        string answersJson,
        IReadOnlyDictionary<string, bool>? controlPresence = null)
    {
        IReadOnlyList<FormulaBindingDto> bindings = await _configuration.ListarFormulaBindingsPorVersionFormularioAsync(versionFormularioId);
        if (bindings.Count == 0) return new(false, null);

        (IReadOnlyList<GovernedFormulaBinding> formulas, FormulaRuntimeOptions runtime, HashSet<string> targets) =
            await PrepareAsync(versionFormularioId, definitionJson, bindings);
        IReadOnlyList<FormulaDiagnostic> diagnostics = _engine.ValidateGovernedDefinition(definitionJson, formulas, runtime);
        if (diagnostics.Count > 0)
            throw new InvalidOperationException(string.Join("; ", diagnostics.Select(diagnostic => $"{diagnostic.Field}: {diagnostic.Message}")));

        using JsonDocument answers = JsonDocument.Parse(string.IsNullOrWhiteSpace(answersJson) ? "{}" : answersJson);
        if (answers.RootElement.ValueKind != JsonValueKind.Object)
            throw new InvalidOperationException("Las respuestas de una versión gobernada deben ser un objeto JSON.");
        foreach (JsonProperty property in answers.RootElement.EnumerateObject())
        {
            if (InstitutionalCalculationContextKeys.Reserved.Contains(property.Name))
                throw new InvalidOperationException($"La respuesta '{property.Name}' es contexto reservado del cálculo y no puede enviarla el cliente.");
            if (targets.Contains(property.Name))
                throw new InvalidOperationException($"El resultado calculado '{property.Name}' no puede enviarse como respuesta del cliente.");
        }

        var context = new Dictionary<string, FormulaValue>(StringComparer.OrdinalIgnoreCase);
        if (controlPresence is not null)
            foreach ((string key, bool present) in controlPresence)
            {
                if (!InstitutionalCalculationContextKeys.Reserved.Contains(key))
                    throw new InvalidOperationException("El contexto suplementario contiene una clave no autorizada.");
                context[key] = present ? FormulaValue.TextValue("1") : FormulaValue.TextValue(string.Empty);
            }

        FormulaEvaluationResult result = _engine.EvaluateGoverned(definitionJson, answersJson, formulas, runtime, context);
        return new(true, result);
    }

    public async Task<IReadOnlyList<FormulaDiagnostic>> ValidateForPublicationAsync(long versionFormularioId, string definitionJson)
    {
        IReadOnlyList<FormulaBindingDto> bindings = await _configuration.ListarFormulaBindingsPorVersionFormularioAsync(versionFormularioId);
        if (bindings.Count == 0) return Array.Empty<FormulaDiagnostic>();
        try
        {
            (IReadOnlyList<GovernedFormulaBinding> formulas, FormulaRuntimeOptions runtime, _) = await PrepareAsync(versionFormularioId, definitionJson, bindings);
            IReadOnlyList<FormulaDiagnostic> runtimeDiagnostics = _engine.ValidateGovernedDefinition(definitionJson, formulas, runtime);
            return _publicationGate.ValidatePublishedRuntimeBindings(formulas, runtime.Pinning!, runtimeDiagnostics).Errors;
        }
        catch (Exception exception) when (exception is InvalidOperationException or FormulaRuntimeException or JsonException)
        {
            return [new(FormulaErrorCode.FORMULA_ARGUMENT_INVALID, "runtimeCalculo", exception.Message)];
        }
    }

    private async Task<(IReadOnlyList<GovernedFormulaBinding> Bindings, FormulaRuntimeOptions Runtime, HashSet<string> Targets)> PrepareAsync(
        long versionFormularioId,
        string definitionJson,
        IReadOnlyList<FormulaBindingDto> sourceBindings)
    {
        Dictionary<string, string> fields = ReadFieldKeys(definitionJson);
        var targets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var formulas = new List<GovernedFormulaBinding>();
        foreach (FormulaBindingDto binding in sourceBindings)
        {
            string target = binding.CampoClave.Trim();
            if (binding.VersionFormularioId != versionFormularioId
                || string.IsNullOrWhiteSpace(target)
                || !targets.Add(target)
                || !fields.ContainsKey(target))
                throw new InvalidOperationException($"El binding de fórmula para '{target}' es inválido, duplicado o no está declarado en el formulario.");
            if (!binding.EstadoFormula.Equals("ACTIVE", StringComparison.OrdinalIgnoreCase)
                || !binding.EstadoVersion.Equals("PUBLISHED", StringComparison.OrdinalIgnoreCase)
                || !Sha256.IsMatch(binding.Hash))
                throw new InvalidOperationException($"La dependencia {binding.FormulaCodigo}@{binding.FormulaVersion} no está activa/publicada o carece de hash SHA-256 válido.");
            formulas.Add(new(target, binding.FormulaCodigo, binding.FormulaVersion, binding.Expresion,
                binding.TipoResultado, binding.EstadoVersion, binding.EstadoFormula, binding.Hash));
        }

        (Dictionary<string, int> functions, Dictionary<string, int> parameters, IReadOnlyList<CatalogSnapshot> catalogs) = ReadRuntimePins(definitionJson);
        ValidateEffectivenessCatalogParity(catalogs);
        var formulaVersions = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (IGrouping<string, GovernedFormulaBinding> group in formulas.GroupBy(binding => binding.FormulaCode, StringComparer.OrdinalIgnoreCase))
        {
            int[] versions = group.Select(binding => binding.FormulaVersion).Distinct().ToArray();
            if (versions.Length != 1) throw new InvalidOperationException($"Los bindings de '{group.Key}' fijan versiones incompatibles.");
            formulaVersions.Add(group.Key, versions[0]);
        }
        var pinning = new CalculationPinning(functions, parameters,
            catalogs.ToDictionary(catalog => catalog.Code, CatalogSnapshotHasher.Compute, StringComparer.OrdinalIgnoreCase),
            published: true, formulaVersions: formulaVersions);
        FormulaRuntimeOptions runtime = await _factory.CreatePublishedAsync(pinning, catalogs);
        return (formulas, runtime, targets);
    }

    private static void ValidateEffectivenessCatalogParity(IReadOnlyList<CatalogSnapshot> catalogs)
    {
        string[] codes = ["CAT_EFECTIVIDAD_ESCALA", "CAT_EFECTIVIDAD_NIVEL", "CAT_EFECTIVIDAD_PORCENTAJE"];
        Dictionary<string, CatalogSnapshot> byCode = catalogs.ToDictionary(catalog => catalog.Code, StringComparer.OrdinalIgnoreCase);
        CatalogSnapshot[] present = codes.Where(byCode.ContainsKey).Select(code => byCode[code]).ToArray();
        if (present.Length == 0) return;
        if (present.Length != codes.Length)
            throw new InvalidOperationException("Los tres catálogos de efectividad deben estar presentes como un conjunto versionado.");

        CatalogElement[] scale = Ordered(present[0]);
        CatalogElement[] levels = Ordered(present[1]);
        CatalogElement[] percentages = Ordered(present[2]);
        if (scale.Length == 0 || levels.Length != scale.Length || percentages.Length != scale.Length)
            throw new InvalidOperationException("Los catálogos de efectividad deben tener la misma cantidad de elementos.");

        for (int index = 0; index < scale.Length; index++)
        {
            if (!scale[index].Activo || !levels[index].Activo || !percentages[index].Activo
                || !scale[index].Codigo.Equals(levels[index].Codigo, StringComparison.Ordinal)
                || !scale[index].Codigo.Equals(percentages[index].Codigo, StringComparison.Ordinal))
                throw new InvalidOperationException("Los catálogos de efectividad deben conservar exactamente el mismo conjunto y orden de códigos activos.");
            if (!int.TryParse(levels[index].Valor, NumberStyles.Integer, CultureInfo.InvariantCulture, out _)
                || !double.TryParse(percentages[index].Valor, NumberStyles.Float, CultureInfo.InvariantCulture, out double ratio)
                || !double.IsFinite(ratio) || ratio is < 0d or > 1d)
                throw new InvalidOperationException("Los niveles deben ser enteros y los porcentajes de efectividad deben ser proporciones entre 0 y 1.");
        }

        static CatalogElement[] Ordered(CatalogSnapshot catalog) => catalog.Elements
            .OrderBy(element => element.Orden)
            .ThenBy(element => element.Codigo, StringComparer.Ordinal)
            .ToArray();
    }

    private static Dictionary<string, string> ReadFieldKeys(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement root = document.RootElement;
        if (root.TryGetProperty("definicionFormulario", out JsonElement nested)) root = nested;
        var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!root.TryGetProperty("secciones", out JsonElement sections) || sections.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException("La definición gobernada no tiene secciones de formulario.");
        foreach (JsonElement section in sections.EnumerateArray())
            if (section.TryGetProperty("campos", out JsonElement items) && items.ValueKind == JsonValueKind.Array)
                foreach (JsonElement field in items.EnumerateArray())
                {
                    string? key = GetText(field, "clave") ?? GetText(field, "rutaDatos") ?? GetText(field, "identificador");
                    if (string.IsNullOrWhiteSpace(key) || !fields.TryAdd(key, key))
                        throw new InvalidOperationException("La definición gobernada contiene una clave vacía o duplicada.");
                }
        return fields;
    }

    private static (Dictionary<string, int> Functions, Dictionary<string, int> Parameters, IReadOnlyList<CatalogSnapshot> Catalogs) ReadRuntimePins(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement root = document.RootElement;
        if (!root.TryGetProperty("runtimeCalculo", out JsonElement runtime) || runtime.ValueKind != JsonValueKind.Object)
            throw new InvalidOperationException("La versión gobernada no declara runtimeCalculo.");
        var functions = ReadVersions(runtime, "funciones");
        var parameters = ReadVersions(runtime, "parametros");
        if (!runtime.TryGetProperty("catalogos", out JsonElement hashes) || hashes.ValueKind != JsonValueKind.Object)
            throw new InvalidOperationException("runtimeCalculo.catalogos debe declarar los snapshots fijados.");
        var catalogs = ReadCatalogs(root);
        foreach (JsonProperty hash in hashes.EnumerateObject())
            if (hash.Value.ValueKind != JsonValueKind.String || !Sha256.IsMatch(hash.Value.GetString() ?? string.Empty))
                throw new InvalidOperationException($"El pin de catálogo '{hash.Name}' no contiene un SHA-256 válido.");
        Dictionary<string, CatalogSnapshot> byCode = catalogs.ToDictionary(catalog => catalog.Code, StringComparer.OrdinalIgnoreCase);
        foreach (JsonProperty hash in hashes.EnumerateObject())
        {
            if (!byCode.TryGetValue(hash.Name, out CatalogSnapshot? catalog))
                throw new InvalidOperationException($"Falta el snapshot versionado del catálogo '{hash.Name}'.");
            if (!CatalogSnapshotHasher.Compute(catalog).Equals(hash.Value.GetString(), StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"El hash del snapshot '{hash.Name}' no coincide con runtimeCalculo.");
        }
        return (functions, parameters, catalogs);
    }

    private static Dictionary<string, int> ReadVersions(JsonElement runtime, string propertyName)
    {
        if (!runtime.TryGetProperty(propertyName, out JsonElement versions) || versions.ValueKind != JsonValueKind.Object)
            throw new InvalidOperationException($"runtimeCalculo.{propertyName} debe ser un objeto de versiones fijadas.");
        var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (JsonProperty version in versions.EnumerateObject())
            if (string.IsNullOrWhiteSpace(version.Name) || !version.Value.TryGetInt32(out int number) || number < 1 || !result.TryAdd(version.Name, number))
                throw new InvalidOperationException($"La versión fijada '{propertyName}.{version.Name}' es inválida o duplicada.");
        return result;
    }

    private static IReadOnlyList<CatalogSnapshot> ReadCatalogs(JsonElement root)
    {
        var catalogs = new List<CatalogSnapshot>();
        if (!root.TryGetProperty("catalogos", out JsonElement values) || values.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException("La versión gobernada debe declarar sus catálogos en VER_JSON.");
        foreach (JsonElement catalog in values.EnumerateArray())
        {
            string code = GetText(catalog, "codigo") ?? string.Empty;
            bool active = !catalog.TryGetProperty("activo", out JsonElement activeJson) || activeJson.ValueKind != JsonValueKind.False;
            if (string.IsNullOrWhiteSpace(code) || !catalog.TryGetProperty("elementos", out JsonElement items) || items.ValueKind != JsonValueKind.Array)
                throw new InvalidOperationException("Un catálogo versionado carece de código o elementos.");
            var elements = new List<CatalogElement>();
            long id = 1;
            foreach (JsonElement item in items.EnumerateArray())
            {
                string itemCode = GetText(item, "codigo") ?? string.Empty;
                string value = GetText(item, "valor") ?? string.Empty;
                int order = item.TryGetProperty("orden", out JsonElement orderJson) && orderJson.TryGetInt32(out int parsedOrder) ? parsedOrder : 0;
                bool itemActive = !item.TryGetProperty("activo", out JsonElement itemActiveJson) || itemActiveJson.ValueKind != JsonValueKind.False;
                if (string.IsNullOrWhiteSpace(itemCode) || !elements.All(element => !element.Codigo.Equals(itemCode, StringComparison.OrdinalIgnoreCase)))
                    throw new InvalidOperationException($"El catálogo '{code}' tiene un código de elemento vacío o duplicado.");
                elements.Add(new(id++, itemCode, value, order, itemActive));
            }
            catalogs.Add(new(code, active, elements));
        }
        if (catalogs.Select(catalog => catalog.Code).Distinct(StringComparer.OrdinalIgnoreCase).Count() != catalogs.Count)
            throw new InvalidOperationException("La definición contiene códigos de catálogo duplicados.");
        return catalogs;
    }

    private static string? GetText(JsonElement element, string property) =>
        element.TryGetProperty(property, out JsonElement value) && value.ValueKind == JsonValueKind.String ? value.GetString()?.Trim() : null;
}
