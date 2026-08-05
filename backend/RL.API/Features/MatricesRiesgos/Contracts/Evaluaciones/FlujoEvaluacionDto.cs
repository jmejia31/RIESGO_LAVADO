using System;

namespace RL.API.Features.MatricesRiesgos.Contracts;

public sealed class FlujoEvaluacionDto
{
    public long FluId { get; set; }
    public long FluEvaluacionId { get; set; }
    public string FluEstado { get; set; } = string.Empty;
    public string? FluMotivo { get; set; }
    public long FluUsrId { get; set; }
    public DateTime FluFecha { get; set; }
}
