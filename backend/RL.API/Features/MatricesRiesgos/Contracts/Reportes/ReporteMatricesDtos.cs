namespace RL.API.Features.MatricesRiesgos.Contracts;

/// <summary>
/// Fila tipada de la Matriz Consolidada de Riesgos basada en el modelo dinámico definitivo.
/// </summary>
public sealed class RiesgoReporteFilaDto
{
    public long RiesgoId { get; set; }
    public long EvaluacionId { get; set; }
    public long VersionFormularioId { get; set; }
    public string CodigoRiesgo { get; set; } = string.Empty;
    public string AreaPrincipal { get; set; } = string.Empty;
    public string DuenoRiesgo { get; set; } = string.Empty;
    public int Vri { get; set; }
    public string NivelInherente { get; set; } = string.Empty;
    public int Vrr { get; set; }
    public string NivelResidual { get; set; } = string.Empty;
    public string RespuestaRiesgo { get; set; } = string.Empty;
    public string EstadoEvaluacion { get; set; } = string.Empty;
    public DateTime FechaEvaluacion { get; set; }
}

public sealed class ReporteMatricesPaginadoDto
{
    public IReadOnlyList<RiesgoReporteFilaDto> Items { get; set; } = Array.Empty<RiesgoReporteFilaDto>();
    public int Pagina { get; set; }
    public int TamanoPagina { get; set; }
    public int TotalRegistros { get; set; }
    public int TotalPaginas { get; set; }
    public ReporteMatricesTotalesDto Totales { get; set; } = new();
}

public sealed class ReporteMatricesTotalesDto
{
    public int TotalRiesgos { get; set; }
    public int TotalConEvaluacionOficial { get; set; }
    public int TotalSinEvaluacionOficial { get; set; }
    public int TotalAltoCritico { get; set; }
}

public sealed class MatrizRiesgoDashboardDinamicoDto
{
    public DateTime FechaGeneracion { get; set; }
    public int TotalRiesgos { get; set; }
    public int TotalConEvaluacionOficial { get; set; }
    public int TotalSinEvaluacionOficial { get; set; }
    public IReadOnlyList<MapaTransicionCeldaDto> MapaTransicion { get; set; } = Array.Empty<MapaTransicionCeldaDto>();
    public IReadOnlyList<RiesgoReporteFilaDto> PendientesOperativos { get; set; } = Array.Empty<RiesgoReporteFilaDto>();
}

public sealed class MapaTransicionCeldaDto
{
    public string NivelInherente { get; set; } = string.Empty;
    public string NivelResidual { get; set; } = string.Empty;
    public int Total { get; set; }
    public decimal PromedioInherente { get; set; }
    public decimal PromedioResidual { get; set; }
}

public sealed class FiltroReporteMatricesDto
{
    public string? Buscar { get; set; }
    public string? Area { get; set; }
    public string? DuenoRiesgo { get; set; }
    public string? EstadoEvaluacion { get; set; }
    public string? NivelInherente { get; set; }
    public string? NivelResidual { get; set; }
    public string? RespuestaRiesgo { get; set; }
    public DateTime? FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    public int Pagina { get; set; } = 1;
    public int TamanoPagina { get; set; } = 20;
}
