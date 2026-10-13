using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using RL.API.Features.MatricesRiesgos.Contracts;
using RL.API.Features.MatricesRiesgos.Domain;

namespace RL.API.Features.MatricesRiesgos.Application;

public sealed record InstitutionalProjectionSource(
    EvaluacionRiesgoDto Evaluation,
    RiesgoDto Risk,
    IReadOnlyList<ControlRiesgoDto> Controls,
    MitigacionBloque4Dto Mitigation,
    MatrizBloque6Dto Monitoring,
    IReadOnlyList<RiesgoDto>? AllRisks = null);

public sealed class InstitutionalProjectionException(string message) : InvalidOperationException(message);

/// <summary>Maps persisted institutional sources into the exact manifest field order.</summary>
public sealed class InstitutionalProjectionMapper
{
    private const string ContractResourceSuffix = "matriz_riesgos_82_campos_manifest.json";
    private const string OrdinalResourceSuffix = "block6_risk_ordinal_map.json";
    private static readonly ProjectionManifestField[] Fields = LoadManifest();
    private static readonly IReadOnlyDictionary<string, int> Ordinals = LoadOrdinals();

    public InstitutionalEvaluationProjectionDto Map(InstitutionalProjectionSource source)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (source.Evaluation.EvaId <= 0 || source.Evaluation.EvaId != source.Mitigation.Planes.FirstOrDefault()?.PlaEvaluacionId
            && source.Mitigation.Planes.Count > 0)
            throw new InstitutionalProjectionException("La proyección contiene una evaluación inválida o entidades de mitigación ajenas.");
        if (!string.Equals(source.Risk.RieCodigo, source.Risk.RieCodigo.Trim(), StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(source.Risk.RieCodigo))
            throw new InstitutionalProjectionException("El código institucional del riesgo no es válido.");
        if (source.Controls.Any(control => control.ConEvaluacionId != source.Evaluation.EvaId)
            || source.Controls.Select(control => control.ConId).Distinct().Count() != source.Controls.Count
            || source.Mitigation.Planes.Any(plan => plan.PlaEvaluacionId != source.Evaluation.EvaId
                || plan.Actividades.Any(activity => activity.ActPlanId != plan.PlaId))
            || source.Mitigation.Planes.Select(plan => plan.PlaId).Distinct().Count() != source.Mitigation.Planes.Count)
            throw new InstitutionalProjectionException("La proyección contiene identidades de control, plan o actividad inconsistentes.");
        int riskOrdinal = ResolveRiskOrdinal(source);

        Dictionary<string, JsonElement> answers = ReadObject(source.Evaluation.EvaDataJson, "EVA_DATOS_JSON");
        Dictionary<string, JsonElement> calculations = ReadObject(source.Evaluation.EvaDataCalcJson, "EVA_CALCULOS_JSON");
        var fields = new List<InstitutionalProjectionFieldDto>(Fields.Length);
        foreach (ProjectionManifestField manifest in Fields)
        {
            (object? value, string state) = ResolveValue(manifest, source, answers, calculations, riskOrdinal);
            if (value is null && state is not ("LEGIT_BLANK" or "LEGIT_FORMULA_BLANK" or "NOT_APPLICABLE" or "OPERATIONAL_PENDING" or "ABSENT_CONTROL_PROJECTED"))
                throw new InstitutionalProjectionException($"F{manifest.Number:D2} terminó sin valor ni semántica de blank.");
            string valueType = manifest.Cardinality.StartsWith("ONE_TO_MANY", StringComparison.Ordinal)
                ? $"COLLECTION<{ValueTypeOf(manifest.DataType)}>"
                : ValueTypeOf(manifest.DataType);
            fields.Add(new(manifest.Number, manifest.CanonicalKey, manifest.Label, valueType, value, state, manifest.Source));
        }

        if (fields.Count != 82 || fields.Select(field => field.FieldNumber).Distinct().Count() != 82
            || fields.Where((field, index) => field.FieldNumber != index + 1).Any())
            throw new InstitutionalProjectionException("El manifest no produjo exactamente 82 posiciones únicas y ordenadas.");
        return new(source.Evaluation.EvaId, source.Risk.RieCodigo, fields);
    }

    private static (object? Value, string State) ResolveValue(
        ProjectionManifestField field,
        InstitutionalProjectionSource source,
        IReadOnlyDictionary<string, JsonElement> answers,
        IReadOnlyDictionary<string, JsonElement> calculations,
        int riskOrdinal)
    {
        if (field.Number == 1) return (riskOrdinal, "VALUE");
        if (field.Number == 2) return (source.Risk.RieCodigo, "VALUE");
        if (field.Number == 8) return (Text(source.Risk.RieNombre), "VALUE");
        if (field.Number == 9) return Optional(source.Risk.RieDescripcion, "RISK_MASTER");
        if (field.Number is 20 or 24 or 28)
        {
            string type = field.Number switch { 20 => "PREVENTIVO", 24 => "DETECTIVO", _ => "CORRECTIVO" };
            string[] descriptions = OrderedControls(source, type).Select(control => Text(control.ConDescripcion)).ToArray();
            return descriptions.Length == 0 ? ("No hay", "ABSENT_CONTROL_PROJECTED") : (Enumerate(descriptions), "VALUE");
        }
        if (field.Number is 21 or 25 or 29)
        {
            string key = field.Number switch { 21 => "escala_preventivo", 25 => "escala_detectivo", _ => "escala_correctivo" };
            string type = field.Number switch { 21 => "PREVENTIVO", 25 => "DETECTIVO", _ => "CORRECTIVO" };
            if (!OrderedControls(source, type).Any()) return ("Inexistente", "ABSENT_CONTROL_PROJECTED");
            return FromJson(answers, key, optional: true);
        }
        if (field.Number is 22 or 23 or 26 or 27 or 30 or 31)
        {
            string type = field.Number switch { 22 or 23 => "PREVENTIVO", 26 or 27 => "DETECTIVO", _ => "CORRECTIVO" };
            if (!OrderedControls(source, type).Any()) return (field.Number is 22 or 26 or 30 ? 0 : 0m, "ABSENT_CONTROL_PROJECTED");
            return FromJson(calculations, JsonKey(field), optional: false);
        }
        if (field.Number == 32)
        {
            string[] values = OrderedControls(source).Select(control => CanonicalAutomation(control.ConAutomatizacion)).ToArray();
            return values.Length == 0 ? (null, "LEGIT_BLANK") : (Enumerate(values), "VALUE");
        }
        if (field.Number == 33)
        {
            if (OrderedControls(source).Count == 0 && !calculations.ContainsKey(JsonKey(field))) return (null, "LEGIT_FORMULA_BLANK");
            return FromJson(calculations, JsonKey(field), optional: false);
        }
        if (field.Number == 39)
        {
            (object? value, string state) = FromJson(answers, JsonKey(field), optional: false);
            try { return (MatrizRiesgosCatalogoCanonico.ObtenerEtiqueta("RISK_RESPONSE", Convert.ToString(value, CultureInfo.InvariantCulture)!), state); }
            catch (InvalidOperationException exception) { throw new InstitutionalProjectionException("La respuesta de riesgo no pertenece al catálogo institucional: " + exception.Message); }
        }
        if (field.Number is 40 or 41 or 42 or 43 or 44 or 45 or 46 or 47 or 48 or 49)
            return ResolveMitigation(field.Number, source);
        if (field.Number == 70)
        {
            string[] alerts = source.Monitoring.SenalesAlerta.Where(alert => alert.AleEstado.Equals("ACTIVO", StringComparison.OrdinalIgnoreCase))
                .OrderBy(alert => alert.AleId).Select(alert => Text(alert.AleIndicador)).ToArray();
            return alerts.Length == 0 ? (null, "LEGIT_BLANK") : (Enumerate(alerts), "VALUE");
        }
        if (field.Number == 71)
            return Optional(source.Monitoring.EstadoRiesgo, "MONITORING");
        if (field.Number is 72 or 73 or 74 or 75 or 76 or 77 or 78 or 79 or 80)
            return ResolveMonitoredControl(field.Number, source);
        if (field.Number is 81 or 82)
            return Optional(field.Number == 81 ? source.Monitoring.ObservacionesArea : source.Monitoring.ObservacionesUgr, "MONITORING");
        if (field.Source.Equals("EXCEL_FORMULA", StringComparison.Ordinal))
            return FromJson(calculations, JsonKey(field), optional: false);
        if (field.Number is 17 or 18 or 19)
            return IsGtic(source, answers)
                ? FromJson(answers, JsonKey(field), optional: false)
                : (null, "NOT_APPLICABLE");
        return FromJson(answers, JsonKey(field), optional: field.Requiredness == "OPTIONAL");
    }

    private static (object? Value, string State) ResolveMitigation(int number, InstitutionalProjectionSource source)
    {
        PlanMitigacionDto[] plans = source.Mitigation.Planes.OrderBy(plan => plan.PlaId).ToArray();
        ActividadPlanDto[] activities = plans.SelectMany(plan => plan.Actividades.OrderBy(activity => activity.ActId)).ToArray();
        bool mitigationApplies = MitigationApplies(source);
        if (number == 41) return plans.Length == 0 && !mitigationApplies ? (null, "NOT_APPLICABLE") : (plans.Length, "VALUE");
        if (number == 43) return plans.Length == 0 && !mitigationApplies ? (null, "NOT_APPLICABLE") : (activities.Length, "VALUE");
        if (plans.Length == 0) return (null, mitigationApplies ? "OPERATIONAL_PENDING" : "NOT_APPLICABLE");
        InstitutionalPlanProjectionValueDto[] PlanValues(Func<PlanMitigacionDto, object?> selector) => plans
            .Select((plan, index) =>
            {
                object? value = selector(plan);
                return new InstitutionalPlanProjectionValueDto(index + 1, value, value is null ? "LEGIT_BLANK" : "VALUE");
            }).ToArray();
        return number switch
        {
            40 => (PlanValues(plan => Text(plan.PlaDescripcion)), "VALUE"),
            42 => (string.Join(Environment.NewLine, plans.Select((plan, pi) => string.Join(Environment.NewLine,
                plan.Actividades.OrderBy(activity => activity.ActId).Select((activity, ai) => $"{pi + 1}.{ai + 1} {Text(activity.ActDescripcion)}")))),
                activities.Length == 0 ? "LEGIT_BLANK" : "VALUE"),
            44 => (PlanValues(plan => OptionalPlanText(plan.PlaMonitoreoSeguimiento)), "VALUE"),
            45 => (PlanValues(plan => OptionalPlanText(plan.PlaResponsables)), "VALUE"),
            46 => (PlanValues(plan => plan.PlaFechaInicio), "VALUE"),
            47 => (PlanValues(plan => plan.PlaFechaFin), "VALUE"),
            48 => (PlanValues(plan => OptionalPlanText(plan.PlaRecursos)), "VALUE"),
            49 => (PlanValues(plan => plan.PlaPresupuesto), "VALUE"),
            _ => throw new InstitutionalProjectionException($"F{number} no tiene resolver institucional.")
        };
    }

    private static (object? Value, string State) ResolveMonitoredControl(int number, InstitutionalProjectionSource source)
    {
        string type = number switch { <= 74 => "PREVENTIVO", <= 77 => "DETECTIVO", _ => "CORRECTIVO" };
        int within = number switch { <= 74 => number - 72, <= 77 => number - 75, _ => number - 78 };
        ControlMonitoreoMatrizDto[] controls = source.Monitoring.Controles
            .Where(control => control.Tipo.Equals(type, StringComparison.OrdinalIgnoreCase))
            .OrderBy(control => control.ControlId).ToArray();
        if (controls.Length == 0) return (null, "ABSENT_CONTROL_PROJECTED");
        string?[] values = within switch
        {
            0 => controls.Select(control => control.EstadoMonitoreo).ToArray(),
            1 => controls.Select(control => control.EfectividadMonitoreo?.ToString(CultureInfo.InvariantCulture)).ToArray(),
            _ => controls.SelectMany(control => control.Evidencias.OrderBy(evidence => evidence.FechaCreacion).ThenBy(evidence => evidence.Id).Select(evidence => evidence.NombreArchivo)).ToArray()
        };
        string? joined = Join(values);
        return joined is null ? (null, "OPERATIONAL_PENDING") : (Enumerate(joined.Split(Environment.NewLine)), "VALUE");
    }

    private static string? OptionalPlanText(string? value) => string.IsNullOrWhiteSpace(value) ? null : Text(value);

    private static int ResolveRiskOrdinal(InstitutionalProjectionSource source)
    {
        if (Ordinals.TryGetValue(source.Risk.RieCodigo, out int legacyOrdinal)) return legacyOrdinal;
        IReadOnlyList<RiesgoDto> risks = source.AllRisks
            ?? throw new InstitutionalProjectionException("Para un código nuevo, el ordinal requiere el universo completo de riesgos persistidos.");
        if (risks.Count(risk => risk.RieId == source.Risk.RieId && risk.RieCodigo == source.Risk.RieCodigo) != 1)
            throw new InstitutionalProjectionException("El riesgo nuevo no aparece exactamente una vez en el universo de ordinales.");
        RiesgoDto[] newRisks = risks.Where(risk => !Ordinals.ContainsKey(risk.RieCodigo)).OrderBy(risk => risk.RieId).ToArray();
        int index = Array.FindIndex(newRisks, risk => risk.RieId == source.Risk.RieId && risk.RieCodigo == source.Risk.RieCodigo);
        if (index < 0) throw new InstitutionalProjectionException($"El código '{source.Risk.RieCodigo}' no tiene ordinal institucional resoluble.");
        return checked(Ordinals.Count + index + 1);
    }

    private static IReadOnlyList<ControlRiesgoDto> OrderedControls(InstitutionalProjectionSource source, string? type = null) =>
        source.Controls.Where(control => type is null || control.ConTipo.Equals(type, StringComparison.OrdinalIgnoreCase))
            .OrderBy(control => control.ConId).ToArray();

    private static bool IsGtic(InstitutionalProjectionSource source, IReadOnlyDictionary<string, JsonElement> answers) =>
        answers.TryGetValue("tipo_riesgo", out JsonElement type) && type.ValueKind == JsonValueKind.String
        && type.GetString()!.Contains("GTIC", StringComparison.OrdinalIgnoreCase);

    private static bool MitigationApplies(InstitutionalProjectionSource source)
    {
        try
        {
            using JsonDocument answers = JsonDocument.Parse(source.Evaluation.EvaDataJson);
            if (!answers.RootElement.TryGetProperty("respuesta_riesgo", out JsonElement response)
                || response.ValueKind != JsonValueKind.String)
                return false;
            string responseKey = MatrizRiesgosCatalogoCanonico.NormalizarClave("RISK_RESPONSE", response.GetString()!);
            if (responseKey.Equals("MITIGAR", StringComparison.Ordinal)) return true;
            if (responseKey.Equals("ACEPTAR", StringComparison.Ordinal)) return false;
            int? residual = source.Evaluation.EvaVrr ?? ReadCalculatedInteger(source.Evaluation.EvaDataCalcJson, "valor_riesgo_residual");
            if (residual is null or < 1 or > 9)
                throw new InstitutionalProjectionException("No existe un VRR válido para resolver la tolerancia institucional de mitigación.");
            string residualLabel = MatrizRiesgosCatalogoCanonico.ObtenerEtiquetaNivelRiesgo(residual.Value);
            return residualLabel.Contains("Alto", StringComparison.OrdinalIgnoreCase)
                || residualLabel.Contains("Intolerable", StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException)
        {
            throw new InstitutionalProjectionException("No se pudo resolver contractualmente la aplicabilidad de mitigación.");
        }
    }

    private static int? ReadCalculatedInteger(string json, string key)
    {
        Dictionary<string, JsonElement> values = ReadObject(json, "EVA_CALCULOS_JSON");
        return values.TryGetValue(key, out JsonElement value) && value.TryGetInt32(out int parsed) ? parsed : null;
    }

    private static (object? Value, string State) FromJson(IReadOnlyDictionary<string, JsonElement> source, string key, bool optional)
    {
        if (!source.TryGetValue(key, out JsonElement element) || element.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            return optional ? (null, "LEGIT_BLANK") : throw new InstitutionalProjectionException($"Falta el valor requerido '{key}' para la proyección.");
        if (element.ValueKind == JsonValueKind.String && string.IsNullOrWhiteSpace(element.GetString()))
            return optional ? (null, "LEGIT_BLANK") : throw new InstitutionalProjectionException($"El valor requerido '{key}' está vacío.");
        object? value = element.ValueKind switch
        {
            JsonValueKind.String => Text(element.GetString()),
            JsonValueKind.Number when element.TryGetInt64(out long integer) => integer,
            JsonValueKind.Number when element.TryGetDecimal(out decimal decimalValue) => decimalValue,
            JsonValueKind.Number => element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Array => element.EnumerateArray().Select(FromElement).ToArray(),
            JsonValueKind.Object => element.Clone(),
            _ => null
        };
        return value is null ? (null, "LEGIT_BLANK") : (value, "VALUE");
    }

    private static object? FromElement(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.String => Text(element.GetString()),
        JsonValueKind.Number when element.TryGetInt64(out long integer) => integer,
        JsonValueKind.Number when element.TryGetDecimal(out decimal decimalValue) => decimalValue,
        JsonValueKind.Number => element.GetDouble(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Null => null,
        _ => element.Clone()
    };

    private static (object? Value, string State) Optional(string? value, string _) =>
        string.IsNullOrWhiteSpace(value) ? (null, "LEGIT_BLANK") : (Text(value), "VALUE");

    private static Dictionary<string, JsonElement> ReadObject(string json, string source)
    {
        if (string.IsNullOrWhiteSpace(json)) return new(StringComparer.OrdinalIgnoreCase);
        using JsonDocument document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
            throw new InstitutionalProjectionException($"{source} debe ser un objeto JSON.");
        return document.RootElement.EnumerateObject().ToDictionary(property => property.Name, property => property.Value.Clone(), StringComparer.OrdinalIgnoreCase);
    }

    private static string JsonKey(ProjectionManifestField field)
    {
        int separator = field.DbOrEntityMapping.LastIndexOf("->", StringComparison.Ordinal);
        return separator >= 0 ? field.DbOrEntityMapping[(separator + 2)..].Trim() : field.CanonicalKey;
    }

    private static string ValueTypeOf(string dataType) => dataType.Split('|', StringSplitOptions.TrimEntries)[0];
    private static string Text(string? value) => TextoVisibleUtf8Normalizer.Normalizar(value?.Trim() ?? string.Empty);
    private static string? Join(IEnumerable<string?> values)
    {
        string[] present = values.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => Text(value)).ToArray();
        return present.Length == 0 ? null : string.Join(Environment.NewLine, present);
    }
    private static string Enumerate(IEnumerable<string> values) => string.Join(Environment.NewLine, values.Select((value, index) => $"{index + 1}. {value}"));
    private static string CanonicalAutomation(string value) => value.Trim().Replace("Semi-Automatizado", "Semiautomatizado", StringComparison.OrdinalIgnoreCase);

    private static ProjectionManifestField[] LoadManifest() => ReadResource<ProjectionManifestField[]>(ContractResourceSuffix)
        .OrderBy(field => field.Number).ToArray();

    private static IReadOnlyDictionary<string, int> LoadOrdinals()
    {
        OrdinalManifest manifest = ReadResource<OrdinalManifest>(OrdinalResourceSuffix);
        if (manifest.SourceWorkbookSha256 != "5c3fc00864947afe1e34d3d6ffdfc6da008eaa3c8f1c6c764161014d5ef9a385"
            || manifest.Entries.Length != 59
            || manifest.Entries.Select(entry => entry.RiskNo).Distinct().Count() != 59
            || !manifest.Entries.Select(entry => entry.RiskNo).Order().SequenceEqual(Enumerable.Range(1, 59))
            || manifest.Entries.Select(entry => entry.RiskCode).Distinct(StringComparer.Ordinal).Count() != 59)
            throw new InvalidOperationException("El mapa de ordinales no coincide con la matriz institucional certificada.");
        return manifest.Entries.ToDictionary(entry => entry.RiskCode, entry => entry.RiskNo, StringComparer.Ordinal);
    }

    private static T ReadResource<T>(string suffix)
    {
        Assembly assembly = typeof(InstitutionalProjectionMapper).Assembly;
        string name = assembly.GetManifestResourceNames().SingleOrDefault(resource => resource.EndsWith(suffix, StringComparison.Ordinal))
            ?? throw new InvalidOperationException($"No se encontró el recurso contractual {suffix}.");
        using Stream stream = assembly.GetManifestResourceStream(name)!;
        return JsonSerializer.Deserialize<T>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidOperationException($"El recurso contractual {suffix} está vacío.");
    }

    private sealed class ProjectionManifestField
    {
        [JsonPropertyName("number")] public int Number { get; set; }
        [JsonPropertyName("label")] public string Label { get; set; } = string.Empty;
        [JsonPropertyName("canonicalKey")] public string CanonicalKey { get; set; } = string.Empty;
        [JsonPropertyName("dataType")] public string DataType { get; set; } = string.Empty;
        [JsonPropertyName("source")] public string Source { get; set; } = string.Empty;
        [JsonPropertyName("cardinality")] public string Cardinality { get; set; } = string.Empty;
        [JsonPropertyName("requiredness")] public string Requiredness { get; set; } = string.Empty;
        [JsonPropertyName("dbOrEntityMapping")] public string DbOrEntityMapping { get; set; } = string.Empty;
    }
    private sealed class OrdinalManifest
    {
        [JsonPropertyName("sourceWorkbookSha256")] public string SourceWorkbookSha256 { get; set; } = string.Empty;
        [JsonPropertyName("entries")] public OrdinalEntry[] Entries { get; set; } = [];
    }
    private sealed class OrdinalEntry
    {
        [JsonPropertyName("riskCode")] public string RiskCode { get; set; } = string.Empty;
        [JsonPropertyName("riskNo")] public int RiskNo { get; set; }
    }
}
