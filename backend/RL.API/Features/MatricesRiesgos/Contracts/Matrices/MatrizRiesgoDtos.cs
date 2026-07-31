namespace RL.API.Features.MatricesRiesgos.Contracts;

public sealed class MatrizRiesgoFiltroDto
{
    public string? Buscar { get; set; }
    public string? Estado { get; set; }
    public string? SujetoTipo { get; set; }
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

public sealed class MatrizRiesgoEvidenciaDto
{
    public long EvidenciaId { get; set; }
    public string NombreArchivo { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;
    public long Tamano { get; set; }
    public string Hash { get; set; } = string.Empty;
    public string Ruta { get; set; } = string.Empty;
}

// ============================================================
// DTOs: Metodología vigente (GET /metodologia/vigente)
// ============================================================

public sealed class MetodologiaMatricesDto
{
    public string Version { get; set; } = string.Empty;
    public decimal PesoTotalEsperado { get; set; }
    public decimal PuntajeMinimo { get; set; }
    public decimal PuntajeMaximo { get; set; }
    public decimal MitigacionMaximaPct { get; set; }
    public int DecimalesCalculo { get; set; } = 4;
    public int DecimalesVisualizacion { get; set; } = 2;
    public List<FactorInstitucionalDto> FactoresInstitucionales { get; set; } = new();
    public List<VariableMetodologiaRespuestaDto> Variables { get; set; } = new();
    public List<EscalaRiesgoDto> EscalasRiesgo { get; set; } = new();
    public List<EscalaRiesgoDto> EscalasCatalogo { get; set; } = new();
    public List<CriterioCalculoRespuestaDto> Criterios { get; set; } = new();
    public List<decimal> MitigacionesPermitidas { get; set; } = new();
}

public sealed class FactorInstitucionalDto
{
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public decimal PesoInstitucional { get; set; }
    public bool ObligatorioGlobal { get; set; } = true;
}

public sealed class VariableMetodologiaRespuestaDto
{
    public long VariableId { get; set; }
    public long FactorId { get; set; }
    public string FactorCodigo { get; set; } = string.Empty;
    public string FactorNombre { get; set; } = string.Empty;
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public decimal PesoInterno { get; set; }
    public bool Obligatoria { get; set; }
}

public sealed class EscalaRiesgoDto
{
    public long EscalaId { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Nivel { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public decimal ValorMinimo { get; set; }
    public decimal ValorMaximo { get; set; }
    public bool RequierePlanAccion { get; set; }
}

public sealed class CriterioCalculoRespuestaDto
{
    public long CriterioId { get; set; }
    public long FactorId { get; set; }
    public string FactorCodigo { get; set; } = string.Empty;
    public string FactorNombre { get; set; } = string.Empty;
    public long VariableId { get; set; }
    public string VariableCodigo { get; set; } = string.Empty;
    public string VariableNombre { get; set; } = string.Empty;
    public long? EscalaId { get; set; }
    public decimal? ValorDesde { get; set; }
    public decimal? ValorHasta { get; set; }
    public decimal Puntaje { get; set; }
    public string Descripcion { get; set; } = string.Empty;
}
