using System;

namespace RL.API.Features.MatricesRiesgos.Contracts;

public sealed class RevisionEvaluacionDto
{
    public long RevId { get; set; }
    public long RevEvaluacionId { get; set; }
    public string RevDatosJson { get; set; } = string.Empty; // Instantánea histórica del JSON
    public DateTime RevFecha { get; set; }
    public long RevUsrId { get; set; }
}
