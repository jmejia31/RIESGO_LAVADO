namespace RL.API.Features.MatricesRiesgos.Contracts;

public sealed class ControlRiesgoDto
{
    public long ConId { get; set; }
    public long ConEvaluacionId { get; set; }
    public string ConTipo { get; set; } = string.Empty;
    public string ConDescripcion { get; set; } = string.Empty;
    public string ConAutomatizacion { get; set; } = string.Empty;
    public string ConEstado { get; set; } = string.Empty;
}

public sealed class ControlRiesgoGuardarDto
{
    public long ConEvaluacionId { get; set; }
    public string ConTipo { get; set; } = string.Empty;
    public string ConDescripcion { get; set; } = string.Empty;
    public string ConAutomatizacion { get; set; } = string.Empty;
    public string ConEstado { get; set; } = string.Empty;
}

public sealed class EvaluacionControlDto
{
    public long EcoId { get; set; }
    public long EcoControlId { get; set; }
    public decimal EcoEfectividad { get; set; }
    public string? EcoComentario { get; set; }
}

public sealed class EvaluacionControlGuardarDto
{
    public decimal EcoEfectividad { get; set; }
    public string? EcoComentario { get; set; }
}

public sealed class PlanMitigacionDto
{
    public long PlaId { get; set; }
    public long PlaEvaluacionId { get; set; }
    public string PlaDescripcion { get; set; } = string.Empty;
    public decimal PlaAvance { get; set; }
    public decimal PlaPresupuesto { get; set; }
    public DateTime PlaFechaInicio { get; set; }
    public DateTime PlaFechaFin { get; set; }
    public string PlaEstado { get; set; } = string.Empty;
}

public sealed class PlanMitigacionGuardarDto
{
    public long PlaEvaluacionId { get; set; }
    public string PlaDescripcion { get; set; } = string.Empty;
    public decimal PlaAvance { get; set; }
    public decimal PlaPresupuesto { get; set; }
    public DateTime PlaFechaInicio { get; set; }
    public DateTime PlaFechaFin { get; set; }
    public string PlaEstado { get; set; } = string.Empty;
}

public sealed class ActividadPlanDto
{
    public long ActId { get; set; }
    public long ActPlanId { get; set; }
    public string ActDescripcion { get; set; } = string.Empty;
    public string ActResponsable { get; set; } = string.Empty;
    public decimal ActAvance { get; set; }
    public DateTime ActFechaInicio { get; set; }
    public DateTime ActFechaFin { get; set; }
    public string ActEstado { get; set; } = string.Empty;
}

public sealed class ActividadPlanGuardarDto
{
    public long ActPlanId { get; set; }
    public string ActDescripcion { get; set; } = string.Empty;
    public string ActResponsable { get; set; } = string.Empty;
    public decimal ActAvance { get; set; }
    public DateTime ActFechaInicio { get; set; }
    public DateTime ActFechaFin { get; set; }
    public string ActEstado { get; set; } = string.Empty;
}
