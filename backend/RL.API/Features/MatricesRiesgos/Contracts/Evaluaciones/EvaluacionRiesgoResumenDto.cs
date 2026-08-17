using System;

namespace RL.API.Features.MatricesRiesgos.Contracts;

public sealed class EvaluacionRiesgoResumenDto
{
    public long EvaId { get; set; }
    public long EvaRiesgoId { get; set; }
    public string RiesgoCodigo { get; set; } = string.Empty;
    public string RiesgoNombre { get; set; } = string.Empty;
    public long EvaVersionId { get; set; }
    public string VersionCodigo { get; set; } = string.Empty;
    public int VersionNumero { get; set; }
    public string Estado { get; set; } = string.Empty;
    public int? Vri { get; set; }
    public int? Vrr { get; set; }
    public string? NivelResidual { get; set; }
    public DateTime FechaEval { get; set; }
}
