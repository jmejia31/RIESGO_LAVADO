using Newtonsoft.Json;

namespace RL.API.Features.MatricesRiesgos.Contracts;

public sealed class MatrizRiesgoFiltroDto
{
    public string? Buscar { get; set; }
    public string? Estado { get; set; }
    public string? SujetoTipo { get; set; }
    public DateTime? FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
}

public sealed class MatrizRiesgoReporteFiltroDto
{
    public string? Buscar { get; set; }
    public string? Estado { get; set; }
    public string? SujetoTipo { get; set; }
    public string? NivelResidual { get; set; }
    public string? ModeloVersion { get; set; }
    public string? Responsable { get; set; }
    public DateTime? FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
}

public class MatrizRiesgoResumenDto
{
    public long MatrizId { get; set; }
    public long ModeloId { get; set; }
    public string ModeloVersion { get; set; } = string.Empty;
    public string SujetoTipo { get; set; } = string.Empty;
    public string? SujetoIdExt { get; set; }
    public string? Documento { get; set; }
    public string NombreSujeto { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public DateTime FechaEvaluacion { get; set; }
    public decimal? PuntajeInherente { get; set; }
    public string? NivelInherente { get; set; }
    public decimal? PuntajeResidual { get; set; }
    public string? NivelResidual { get; set; }
    public bool RequierePlanAccion { get; set; }
}

public sealed class MatrizRiesgoDetalleDto : MatrizRiesgoResumenDto
{
    public string OrigenDatos { get; set; } = string.Empty;
    public string? MotivoEstado { get; set; }
    public string? SnapshotMetodo { get; set; }
    public List<MatrizRiesgoVariableDetalleDto> Detalles { get; set; } = new();
    public List<MatrizRiesgoControlDto> Controles { get; set; } = new();
    public List<MatrizRiesgoResultadoPersistidoDto> Resultados { get; set; } = new();
    public List<MatrizRiesgoPlanAccionDto> PlanesAccion { get; set; } = new();
    public List<MatrizRiesgoEvidenciaDto> Evidencias { get; set; } = new();
}

public sealed class MatrizRiesgoVariableDetalleDto
{
    public long DetalleId { get; set; }
    public long VariableId { get; set; }
    public long FactorId { get; set; }
    public string FactorCodigo { get; set; } = string.Empty;
    public string FactorNombre { get; set; } = string.Empty;
    public decimal FactorPesoInstitucional { get; set; }
    public string VariableCodigo { get; set; } = string.Empty;
    public string VariableNombre { get; set; } = string.Empty;
    public decimal VariablePesoInterno { get; set; }
    public string? ValorCapturado { get; set; }
    public decimal? Puntaje { get; set; }
    public decimal? PuntajePonderado { get; set; }
    public string? Justificacion { get; set; }
    public string? FuenteDato { get; set; }
    public bool Obligatoria { get; set; }
}

public sealed class MatrizRiesgoControlDto
{
    public long ControlId { get; set; }
    public long? FactorId { get; set; }
    public string? FactorCodigo { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string? Periodicidad { get; set; }
    public string? Oportunidad { get; set; }
    public string? Automatizacion { get; set; }
    public string? Procedimientos { get; set; }
    public string? Calidad { get; set; }
    public decimal EfectividadPct { get; set; }
    public string? Responsable { get; set; }
    public string Estado { get; set; } = "ACTIVO";
    public bool EvidenciaObligatoria { get; set; }
    public bool TieneEvidencia { get; set; }
}

public sealed class MatrizRiesgoResultadoPersistidoDto
{
    public long ResultadoId { get; set; }
    public long? FactorId { get; set; }
    public string TipoResultado { get; set; } = string.Empty;
    public string VersionCalculo { get; set; } = string.Empty;
    public bool EsVigente { get; set; }
    public decimal PuntajeInherente { get; set; }
    public string NivelInherente { get; set; } = string.Empty;
    public decimal MitigacionPct { get; set; }
    public decimal PuntajeResidual { get; set; }
    public string NivelResidual { get; set; } = string.Empty;
    public bool RequierePlanAccion { get; set; }
    public string? MotivoRecalculo { get; set; }
    public DateTime FechaCalculo { get; set; }
}

public sealed class MatrizRiesgoCrearRequestDto
{
    public string SujetoTipo { get; set; } = string.Empty;
    public string? SujetoIdExt { get; set; }
    public string? Documento { get; set; }
    public string NombreSujeto { get; set; } = string.Empty;
    public string OrigenDatos { get; set; } = "CAPTURA";
    public List<MatrizRiesgoDetalleRequestDto> Detalles { get; set; } = new();
    public List<MatrizRiesgoControlRequestDto> Controles { get; set; } = new();
}

public sealed class MatrizRiesgoDetalleRequestDto
{
    public long VariableId { get; set; }
    public string? ValorCapturado { get; set; }
    public decimal Puntaje { get; set; }
    public string? Justificacion { get; set; }
    public string? FuenteDato { get; set; }
}

public sealed class MatrizRiesgoControlRequestDto
{
    public long? FactorId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string? Periodicidad { get; set; }
    public string? Oportunidad { get; set; }
    public string? Automatizacion { get; set; }
    public string? Procedimientos { get; set; }
    public string? Calidad { get; set; }
    public decimal EfectividadPct { get; set; }
    public string? Responsable { get; set; }
    public bool EvidenciaObligatoria { get; set; }
}

public sealed class MatrizRiesgoCalcularRequestDto
{
    public string TipoCalculo { get; set; } = "GLOBAL";
    public string? MotivoCalculo { get; set; }
}

public sealed class MatrizRiesgoCambiarEstadoRequestDto
{
    public string Estado { get; set; } = string.Empty;
    public string Motivo { get; set; } = string.Empty;
}

public sealed class MatrizRiesgoHistorialDto
{
    public long HistorialId { get; set; }
    public long? MatrizId { get; set; }
    public string Tabla { get; set; } = string.Empty;
    public string RegistroId { get; set; } = string.Empty;
    public string Accion { get; set; } = string.Empty;
    public string? EstadoAnterior { get; set; }
    public string? EstadoNuevo { get; set; }
    public string? Motivo { get; set; }
    public long? UsuarioId { get; set; }
    public string? UsuarioEmail { get; set; }
    public string? Ip { get; set; }
    public DateTime Fecha { get; set; }
}

public sealed class MatrizRiesgoCriterioDto
{
    public long CriterioId { get; set; }
    public long FactorId { get; set; }
    public string FactorCodigo { get; set; } = string.Empty;
    public string FactorNombre { get; set; } = string.Empty;
    public long VariableId { get; set; }
    public string VariableCodigo { get; set; } = string.Empty;
    public string VariableNombre { get; set; } = string.Empty;
    public long? EscalaId { get; set; }
    public string? EscalaTipo { get; set; }
    public string? EscalaNivel { get; set; }
    public decimal? ValorDesde { get; set; }
    public decimal? ValorHasta { get; set; }
    public decimal Puntaje { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public bool Activo { get; set; }
    public string? MotivoInactivo { get; set; }
}

public sealed class MatrizRiesgoCriterioRequestDto
{
    public long VariableId { get; set; }
    public long? EscalaId { get; set; }
    public decimal? ValorDesde { get; set; }
    public decimal? ValorHasta { get; set; }
    public decimal Puntaje { get; set; }
    public string Descripcion { get; set; } = string.Empty;
}

public sealed class MatrizRiesgoInactivarRequestDto
{
    public string Motivo { get; set; } = string.Empty;
}

public sealed class MatrizRiesgoPlanAccionDto
{
    public long PlanId { get; set; }
    public long MatrizId { get; set; }
    public long? ResultadoId { get; set; }
    public string Actividad { get; set; } = string.Empty;
    public string Responsable { get; set; } = string.Empty;
    public string? Periodicidad { get; set; }
    public DateTime? FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    public string? MedioPrueba { get; set; }
    public string? Observaciones { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string? MotivoCierre { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaCierre { get; set; }
    public bool Vencido { get; set; }
}

public sealed class MatrizRiesgoPlanAccionRequestDto
{
    public long? ResultadoId { get; set; }
    public string Actividad { get; set; } = string.Empty;
    public string Responsable { get; set; } = string.Empty;
    public string? Periodicidad { get; set; }
    public DateTime? FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    public string? MedioPrueba { get; set; }
    public string? Observaciones { get; set; }
}

public sealed class MatrizRiesgoPlanEstadoRequestDto
{
    public string Estado { get; set; } = string.Empty;
    public string Motivo { get; set; } = string.Empty;
}

public sealed class MatrizRiesgoEvidenciaDto
{
    public long EvidenciaId { get; set; }
    public long MatrizId { get; set; }
    public long? ControlId { get; set; }
    public long? PlanId { get; set; }
    public string NombreOriginal { get; set; } = string.Empty;
    public string NombreFisico { get; set; } = string.Empty;
    public string? TipoMime { get; set; }
    public string? Extension { get; set; }
    public long TamanoBytes { get; set; }
    [JsonIgnore]
    public string RutaFisica { get; set; } = string.Empty;
    public string? HashSha256 { get; set; }
    public bool Activa { get; set; }
    public string? MotivoInactivo { get; set; }
    public DateTime FechaCreacion { get; set; }
}

public sealed class MatrizRiesgoEvidenciaRegistroDto
{
    public long MatrizId { get; set; }
    public long? ControlId { get; set; }
    public long? PlanId { get; set; }
    public string NombreOriginal { get; set; } = string.Empty;
    public string NombreFisico { get; set; } = string.Empty;
    public string? TipoMime { get; set; }
    public string? Extension { get; set; }
    public long TamanoBytes { get; set; }
    public string RutaFisica { get; set; } = string.Empty;
    public string? HashSha256 { get; set; }
}

public sealed class MatrizRiesgoEvidenciaDescargaDto
{
    public string NombreArchivo { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
    public byte[] Contenido { get; set; } = Array.Empty<byte>();
}

public sealed class MatricesRiesgoDashboardDto
{
    public int TotalMatrices { get; set; }
    public int TotalCalculadas { get; set; }
    public int TotalCerradas { get; set; }
    public int TotalConPlanAccion { get; set; }
    public List<MatrizRiesgoConteoDto> PorEstado { get; set; } = new();
    public List<MatrizRiesgoConteoDto> PorNivelResidual { get; set; } = new();
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
