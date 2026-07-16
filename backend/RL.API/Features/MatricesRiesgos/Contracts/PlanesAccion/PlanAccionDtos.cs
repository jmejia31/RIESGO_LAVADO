namespace RL.API.Features.MatricesRiesgos.Contracts;

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
