namespace RL.API.Features.MatricesRiesgos.Contracts;

public sealed class MatrizRiesgoReporteFiltroDto
{
    public string? Buscar { get; set; }
    public string? Estado { get; set; }
    public string? SujetoTipo { get; set; }
    public string? NivelInherente { get; set; }
    public string? NivelResidual { get; set; }
    public string? ModeloVersion { get; set; }
    public string? Responsable { get; set; }
    public DateTime? FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
}

public sealed class MatricesRiesgoDashboardDto
{
    public DateTime FechaGeneracion { get; set; } = DateTime.Now;
    public MatrizRiesgoReporteFiltroDto Filtro { get; set; } = new();
    public int TotalMatrices { get; set; }
    public int TotalCalculadas { get; set; }
    public int TotalSinCalculo { get; set; }
    public int TotalCerradas { get; set; }
    public int TotalConPlanAccion { get; set; }
    public int TotalAltoCritico { get; set; }
    public int TotalPlanesVencidos { get; set; }
    public List<MatrizRiesgoConteoDto> PorEstado { get; set; } = new();
    public List<MatrizRiesgoConteoDto> PorSujetoTipo { get; set; } = new();
    public List<MatrizRiesgoConteoDto> PorNivelInherente { get; set; } = new();
    public List<MatrizRiesgoConteoDto> PorNivelResidual { get; set; } = new();
    public List<MatrizRiesgoMapaTransicionDto> MapaTransicion { get; set; } = new();
    public List<MatrizRiesgoResumenDto> MatricesCriticas { get; set; } = new();
    public List<MatrizRiesgoResumenDto> MatricesFiltradas { get; set; } = new();
    public List<MatrizRiesgoPlanAccionReporteDto> PlanesAccion { get; set; } = new();
}

public sealed class MatrizRiesgoMapaTransicionDto
{
    public string NivelInherente { get; set; } = string.Empty;
    public string NivelResidual { get; set; } = string.Empty;
    public int Total { get; set; }
    public decimal PromedioInherente { get; set; }
    public decimal PromedioResidual { get; set; }
}

public sealed class MatrizRiesgoConteoDto
{
    public string Nombre { get; set; } = string.Empty;
    public int Total { get; set; }
}

public sealed class MatricesRiesgoReporteDto
{
    public DateTime FechaGeneracion { get; set; } = DateTime.Now;
    public MatrizRiesgoReporteFiltroDto Filtro { get; set; } = new();
    public MatricesRiesgoReporteTotalesDto Totales { get; set; } = new();
    public List<MatrizRiesgoConteoDto> PorEstado { get; set; } = new();
    public List<MatrizRiesgoConteoDto> PorNivelResidual { get; set; } = new();
    public List<MatrizRiesgoConteoDto> PorSujetoTipo { get; set; } = new();
    public List<MatrizRiesgoFactorReporteDto> PorFactor { get; set; } = new();
    public List<MatrizRiesgoMapaNivelDto> MapaInherente { get; set; } = new();
    public List<MatrizRiesgoMapaNivelDto> MapaResidual { get; set; } = new();
    public List<MatrizRiesgoResumenDto> MatricesFiltradas { get; set; } = new();
    public List<MatrizRiesgoResumenDto> MatricesCriticas { get; set; } = new();
    public List<MatrizRiesgoPlanAccionReporteDto> PlanesAccion { get; set; } = new();
}

public sealed class MatricesRiesgoReporteTotalesDto
{
    public int TotalMatrices { get; set; }
    public int TotalCalculadas { get; set; }
    public int TotalCerradas { get; set; }
    public int TotalAltoCritico { get; set; }
    public int TotalPlanAccionRequerido { get; set; }
    public int TotalPlanesVencidos { get; set; }
}

public sealed class MatrizRiesgoFactorReporteDto
{
    public long FactorId { get; set; }
    public string FactorCodigo { get; set; } = string.Empty;
    public string FactorNombre { get; set; } = string.Empty;
    public int TotalMatrices { get; set; }
    public decimal PromedioInherente { get; set; }
    public decimal PromedioResidual { get; set; }
    public int TotalAltoCritico { get; set; }
    public int TotalPlanAccionRequerido { get; set; }
}

public sealed class MatrizRiesgoMapaNivelDto
{
    public string Nivel { get; set; } = string.Empty;
    public int Total { get; set; }
    public decimal Promedio { get; set; }
}

public sealed class MatrizRiesgoPlanAccionReporteDto
{
    public string Estado { get; set; } = string.Empty;
    public int Total { get; set; }
    public int Vencidos { get; set; }
}

public sealed class MatrizRiesgoExportacionDto
{
    public string NombreArchivo { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public byte[] Contenido { get; set; } = Array.Empty<byte>();
}
