namespace RL.API.Features.MatricesRiesgos.Contracts;

public sealed class CalculoRiesgoResultadoDto
{
    public int Vri { get; set; }
    public decimal Etp { get; set; }
    public int Vrr { get; set; }
    public int Vrr2 { get; set; }
    public string NivelResidual { get; set; } = string.Empty; // 'BAJO', 'MODERADO', 'ALTO', 'CRÍTICO'
    public bool Coherente { get; set; }
}
