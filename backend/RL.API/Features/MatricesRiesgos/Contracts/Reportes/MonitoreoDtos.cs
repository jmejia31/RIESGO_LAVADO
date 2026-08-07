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
}

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
