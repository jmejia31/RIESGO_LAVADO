namespace RL.API.Features.MatricesRiesgos.Contracts;

public sealed class ConsultaEvaluacionPaginadaDto
{
    public int Pagina { get; set; } = 1;
    public int RegistrosPorPagina { get; set; } = 10;
    public long? RiesgoId { get; set; }
    public string? Estado { get; set; }
    public string? Area { get; set; }
    public string? NivelResidual { get; set; }
    public string? Buscar { get; set; }
}
