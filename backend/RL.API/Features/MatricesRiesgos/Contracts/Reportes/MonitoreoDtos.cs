namespace RL.API.Features.MatricesRiesgos.Contracts;

public sealed class SenalAlertaDto
{
    public long AleId { get; set; }
    public long AleEvaluacionId { get; set; }
    public string AleCodigo { get; set; } = string.Empty;
    public string AleIndicador { get; set; } = string.Empty;
    public string AleEstado { get; set; } = string.Empty;
    public DateTime? AleFechaDisparo { get; set; }
}

public sealed class SenalAlertaGuardarDto
{
    public long AleEvaluacionId { get; set; }
    public string AleCodigo { get; set; } = string.Empty;
    public string AleIndicador { get; set; } = string.Empty;
    public string AleEstado { get; set; } = "INACTIVO";
}

public sealed class SenalAlertaEstadoDto
{
    public string AleEstado { get; set; } = string.Empty;
}

public sealed class AutomonitoreoDto
{
    public long MonId { get; set; }
    public long MonEvaluacionId { get; set; }
    public string MonEstadoRiesgo { get; set; } = string.Empty;
    public string MonEstadoContr { get; set; } = string.Empty;
    public string MonResultado { get; set; } = string.Empty;
    public long MonUsrId { get; set; }
    public DateTime MonFecha { get; set; }
    public string? MonObservacionesArea { get; set; }
    public string? MonObservacionesUgr { get; set; }
}

public sealed class MatrizBloque6Dto
{
    public IReadOnlyList<SenalAlertaDto> SenalesAlerta { get; init; } = Array.Empty<SenalAlertaDto>();
    public string? EstadoRiesgo { get; init; }
    public IReadOnlyList<ControlMonitoreoMatrizDto> Controles { get; init; } = Array.Empty<ControlMonitoreoMatrizDto>();
    public string? ObservacionesArea { get; init; }
    public string? ObservacionesUgr { get; init; }
    public bool PuedeEditarObservacionesArea { get; init; }
    public bool PuedeEditarObservacionesUgr { get; init; }
}

public sealed class ObservacionMonitoreoGuardarDto
{
    public string? Texto { get; set; }
}

public sealed class ControlMonitoreoMatrizDto
{
    public long ControlId { get; init; }
    public string Tipo { get; init; } = string.Empty;
    public string Descripcion { get; init; } = string.Empty;
    public string? EstadoMonitoreo { get; init; }
    public decimal? EfectividadMonitoreo { get; init; }
    public IReadOnlyList<EvidenciaMatrizDto> Evidencias { get; init; } = Array.Empty<EvidenciaMatrizDto>();
}

public sealed record EvidenciaMatrizDto(long Id, string NombreArchivo);

public sealed class AutomonitoreoGuardarDto
{
    public long MonEvaluacionId { get; set; }
    public string MonEstadoRiesgo { get; set; } = string.Empty;
    public string MonEstadoContr { get; set; } = string.Empty;
    public string MonResultado { get; set; } = string.Empty;
}

public sealed class ResumenMatricesOperativoDto
{
    public DateTime FechaGeneracion { get; set; }
    public int RiesgosActivos { get; set; }
    public int EvaluacionesActivas { get; set; }
    public int EvaluacionesAprobadas { get; set; }
    public int RiesgosAltoCritico { get; set; }
    public int AlertasActivas { get; set; }
    public int PlanesAbiertos { get; set; }
    public int ActividadesVencidas { get; set; }
    public int AutomonitoreosUltimos30Dias { get; set; }
}

public sealed record ArchivoReporteDto(byte[] Contenido, string ContentType, string NombreArchivo);
