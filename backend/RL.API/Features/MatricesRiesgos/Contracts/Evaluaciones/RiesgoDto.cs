namespace RL.API.Features.MatricesRiesgos.Contracts;

/// <summary>
/// Riesgo maestro del modelo dinámico de Matrices de Riesgos.
/// </summary>
public sealed class RiesgoDto
{
    public long RieId { get; set; }
    public string RieCodigo { get; set; } = string.Empty;
    public string RieNombre { get; set; } = string.Empty;
    public string? RieDescripcion { get; set; }
    public bool RieActivo { get; set; }
    public long RieUsrCreacion { get; set; }
    public DateTime RieFechaCreacion { get; set; }
}

/// <summary>
/// Contrato de alta/actualización del catálogo operativo de riesgos.
/// El código es estable y se utiliza como clave funcional; no se aceptan IDs fijos.
/// </summary>
public sealed class RiesgoGuardarDto
{
    public string RieCodigo { get; set; } = string.Empty;
    public string RieNombre { get; set; } = string.Empty;
    public string? RieDescripcion { get; set; }
    public bool RieActivo { get; set; } = true;
}
