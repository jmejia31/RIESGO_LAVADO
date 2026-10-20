namespace RL.API.Features.MatricesRiesgos.Domain;

using RL.API.Features.MatricesRiesgos.Contracts;

/// <summary>Reglas de transición del flujo institucional; los guards permanecen en backend.</summary>
public static class EvaluacionWorkflow
{
    private static readonly IReadOnlyList<EstadoEvaluacionCatalogoDto> Estados =
    [
        new("APROBADA", "Aprobada"),
        new("BORRADOR", "Borrador"),
        new("CERRADA", "Cerrada"),
        new("EN_REVISION", "En revisión"),
        new("OBSERVADA", "Observada"),
        new("RECHAZADA", "Rechazada")
    ];

    public static IReadOnlyList<EstadoEvaluacionCatalogoDto> ObtenerCatalogoEstados() => Estados;

    public static bool EsEstadoCanonico(string estado) =>
        !string.IsNullOrWhiteSpace(estado) &&
        Estados.Any(item => item.Codigo.Equals(estado.Trim(), StringComparison.OrdinalIgnoreCase));

    public static bool EsTransicionPermitida(string estadoActual, string estadoDestino) =>
        !string.IsNullOrWhiteSpace(estadoActual) &&
        !string.IsNullOrWhiteSpace(estadoDestino) &&
        !string.Equals(estadoActual.Trim(), estadoDestino.Trim(), StringComparison.OrdinalIgnoreCase) &&
        EsEstadoCanonico(estadoActual) && EsEstadoCanonico(estadoDestino);

    public static IReadOnlyList<string> ObtenerDestinosPermitidos(string estadoActual) =>
        !string.IsNullOrWhiteSpace(estadoActual) && Estados.Any(estado => estado.Codigo.Equals(estadoActual.Trim(), StringComparison.OrdinalIgnoreCase))
            ? Estados.Where(estado => !estado.Codigo.Equals(estadoActual.Trim(), StringComparison.OrdinalIgnoreCase))
                .Select(estado => estado.Codigo).ToArray()
            : Array.Empty<string>();
}
