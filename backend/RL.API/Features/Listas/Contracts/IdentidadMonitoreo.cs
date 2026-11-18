namespace RL.API.Features.Listas.Contracts;

public enum TipoSujetoMonitoreo
{
    Juridica = 1,
    Natural = 2,
    Empleado = 3
}

public static class EstadoIdentidadMonitoreo
{
    public const string Verified = "VERIFIED";
    public const string Ambiguous = "AMBIGUOUS";
    public const string UnverifiedAssociation = "UNVERIFIED_ASSOCIATION";
    public const string Invalid = "INVALID";
}

public sealed record ResolucionIdentidadMonitoreo(
    string Documento,
    string Estado,
    int Referencias,
    int SujetosDistintos,
    string? Motivo = null)
{
    public bool EsOperable => Estado == EstadoIdentidadMonitoreo.Verified;
}

public sealed class AmbiguousIdentityException : Exception
{
    public AmbiguousIdentityException(string message) : base(message) { }
}

public sealed class UnverifiedAssociationException : Exception
{
    public UnverifiedAssociationException(string message) : base(message) { }
}

public static class IdentidadMonitoreoPolicy
{
    public static string NormalizarDocumento(string? documento) => documento?.Trim() ?? string.Empty;

    public static bool EsMarcadorNoIdentificacion(string? documento)
    {
        var valor = NormalizarDocumento(documento).ToUpperInvariant();
        return valor.StartsWith("N/A", StringComparison.Ordinal)
            || valor is "NULL" or "VACIO" or "VACÍO"
            or "SIN IDENTIFICACION" or "SIN IDENTIFICACIÓN";
    }

    // Para mutaciones y seguimientos DNP, tanto la persona como la coincidencia
    // fuente deben ser unicas. Sin fila fuente no hay asociacion verificable.
    public static string EstadoFuenteParaOperacion(
        string? documento, int referencias, int sujetosDistintos, int coincidenciasFuente)
    {
        if (string.IsNullOrWhiteSpace(documento))
            return EstadoIdentidadMonitoreo.Invalid;
        if (EsMarcadorNoIdentificacion(documento))
            return EstadoIdentidadMonitoreo.Ambiguous;
        if (sujetosDistintos > 1 || coincidenciasFuente > 1)
            return EstadoIdentidadMonitoreo.Ambiguous;
        if (referencias <= 0 || sujetosDistintos != 1 || coincidenciasFuente != 1)
            return EstadoIdentidadMonitoreo.UnverifiedAssociation;
        return EstadoIdentidadMonitoreo.Verified;
    }

    // Estado de la relación documental, no de la persona: con varias coincidencias
    // fuente para un mismo DNI no es seguro reutilizar motivos/seguimientos por DNI.
    public static string EstadoAsociacionParaPresentacion(
        string? documento, int referencias, int sujetosDistintos, int coincidenciasFuente)
    {
        var estado = EstadoParaPresentacion(documento, referencias, sujetosDistintos);
        if (estado != EstadoIdentidadMonitoreo.Verified) return estado;
        if (coincidenciasFuente > 1) return EstadoIdentidadMonitoreo.Ambiguous;
        return coincidenciasFuente == 1
            ? EstadoIdentidadMonitoreo.Verified
            : EstadoIdentidadMonitoreo.UnverifiedAssociation;
    }

    public static string EstadoParaPresentacion(string? documento)
        => EstadoParaPresentacion(documento, 0, 0);

    public static string EstadoParaPresentacion(string? documento, int referencias, int sujetosDistintos)
    {
        if (string.IsNullOrWhiteSpace(documento))
            return EstadoIdentidadMonitoreo.Invalid;

        if (EsMarcadorNoIdentificacion(documento))
            return EstadoIdentidadMonitoreo.Ambiguous;

        if (sujetosDistintos > 1)
            return EstadoIdentidadMonitoreo.Ambiguous;

        return referencias > 0 && sujetosDistintos == 1
            ? EstadoIdentidadMonitoreo.Verified
            : EstadoIdentidadMonitoreo.UnverifiedAssociation;
    }
}
