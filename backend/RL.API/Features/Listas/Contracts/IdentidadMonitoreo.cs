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
        return valor is "N/A" or "N/A(3)" or "NULL" or "VACIO" or "VACÍO"
            or "SIN IDENTIFICACION" or "SIN IDENTIFICACIÓN";
    }

    public static string EstadoParaPresentacion(string? documento)
        => string.IsNullOrWhiteSpace(documento)
            ? EstadoIdentidadMonitoreo.Invalid
            : EsMarcadorNoIdentificacion(documento)
            ? EstadoIdentidadMonitoreo.Ambiguous
            : EstadoIdentidadMonitoreo.Verified;
}
