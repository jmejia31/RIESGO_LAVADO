namespace RL.API.Features.MatricesRiesgos.Domain;

public sealed class MatrizCalculoRequestDto
{
    public string TipoCalculo { get; set; } = "GLOBAL";
    public string? MotivoCalculo { get; set; }
    public bool EsRecalculo { get; set; }

    // En Fase 7 este bloque debe poblarse en backend desde RL_MR_*;
    // no debe recibirse directamente desde Angular para evitar manipulación de la metodología.
    public MetodologiaCalculoDto? Metodologia { get; set; }
    public List<FactorCalculoDto> Factores { get; set; } = new();
}

public sealed class MetodologiaCalculoDto
{
    public string Version { get; set; } = string.Empty;
    public decimal PesoTotalEsperado { get; set; }
    public decimal PuntajeMinimo { get; set; }
    public decimal PuntajeMaximo { get; set; }
    public decimal MitigacionMaximaPct { get; set; }
    public int DecimalesCalculo { get; set; }
    public int DecimalesVisualizacion { get; set; }
    public List<FactorInstitucionalCalculoDto> FactoresInstitucionales { get; set; } = new();
    public List<VariableMetodologiaDto> Variables { get; set; } = new();
    public List<EscalaRiesgoCalculoDto> EscalasRiesgo { get; set; } = new();
    public List<EscalaRiesgoCalculoDto> EscalasCatalogo { get; set; } = new();
    public List<CriterioCalculoDto> Criterios { get; set; } = new();
    public List<decimal> MitigacionesPermitidas { get; set; } = new();
}

public sealed class FactorInstitucionalCalculoDto
{
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public decimal PesoInstitucional { get; set; }
    public bool ObligatorioGlobal { get; set; } = true;
}

public sealed class VariableMetodologiaDto
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

public sealed class EscalaRiesgoCalculoDto
{
    public long EscalaId { get; set; }
    public string Tipo { get; set; } = "RIESGO";
    public string Nivel { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public decimal ValorMinimo { get; set; }
    public decimal ValorMaximo { get; set; }
    public bool RequierePlanAccion { get; set; }
}

public sealed class CriterioCalculoDto
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

public sealed class FactorCalculoDto
{
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public decimal PesoInstitucional { get; set; }
    public List<VariableCalculoDto> Variables { get; set; } = new();
    public List<ControlCalculoDto> Controles { get; set; } = new();
}

public sealed class VariableCalculoDto
{
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public decimal PesoInterno { get; set; }
    public decimal? Puntaje { get; set; }
    public bool Obligatoria { get; set; } = true;
    public bool TieneValor { get; set; } = true;
}

public sealed class ControlCalculoDto
{
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public decimal MitigacionPct { get; set; }
    public bool Activo { get; set; } = true;
    public bool TieneEvidencia { get; set; }
}

public sealed class MatrizCalculoResultadoDto
{
    public string VersionCalculo { get; set; } = string.Empty;
    public string VersionMetodologia { get; set; } = string.Empty;
    public decimal PuntajeInherente { get; set; }
    public string NivelInherente { get; set; } = string.Empty;
    public string ColorInherente { get; set; } = string.Empty;
    public decimal MitigacionPct { get; set; }
    public decimal PuntajeResidual { get; set; }
    public string NivelResidual { get; set; } = string.Empty;
    public string ColorResidual { get; set; } = string.Empty;
    public bool RequierePlanAccion { get; set; }
    public string Explicacion { get; set; } = string.Empty;
    public List<FactorCalculoResultadoDto> Factores { get; set; } = new();
    public List<string> Advertencias { get; set; } = new();
}

public sealed class FactorCalculoResultadoDto
{
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public decimal PesoInstitucional { get; set; }
    public decimal PuntajeInherente { get; set; }
    public string NivelInherente { get; set; } = string.Empty;
    public string ColorInherente { get; set; } = string.Empty;
    public decimal MitigacionPct { get; set; }
    public decimal PuntajeResidual { get; set; }
    public string NivelResidual { get; set; } = string.Empty;
    public string ColorResidual { get; set; } = string.Empty;
    public bool RequierePlanAccion { get; set; }
    public List<VariableCalculoResultadoDto> Variables { get; set; } = new();
}

public sealed class VariableCalculoResultadoDto
{
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public decimal PesoInterno { get; set; }
    public decimal Puntaje { get; set; }
    public decimal PuntajePonderado { get; set; }
    public string Nivel { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
}
