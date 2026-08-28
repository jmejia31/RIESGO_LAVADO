namespace RL.API.Features.MatricesRiesgos.Domain;

/// <summary>Reglas de transición del flujo institucional; los guards permanecen en backend.</summary>
public static class EvaluacionWorkflow
{
    private static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> Transiciones =
        new Dictionary<string, IReadOnlySet<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["BORRADOR"] = new HashSet<string>(new[] { "EN_REVISION" }, StringComparer.OrdinalIgnoreCase),
            ["EN_REVISION"] = new HashSet<string>(new[] { "OBSERVADA", "APROBADA", "RECHAZADA" }, StringComparer.OrdinalIgnoreCase),
            ["OBSERVADA"] = new HashSet<string>(new[] { "BORRADOR" }, StringComparer.OrdinalIgnoreCase),
            ["APROBADA"] = new HashSet<string>(new[] { "CERRADA" }, StringComparer.OrdinalIgnoreCase)
        };

    public static bool EsTransicionPermitida(string estadoActual, string estadoDestino) =>
        !string.IsNullOrWhiteSpace(estadoActual) &&
        !string.IsNullOrWhiteSpace(estadoDestino) &&
        Transiciones.TryGetValue(estadoActual.Trim(), out var destinos) &&
        destinos.Contains(estadoDestino.Trim());
}
