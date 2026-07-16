namespace RL.API.Features.MatricesRiesgos.Contracts;

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
