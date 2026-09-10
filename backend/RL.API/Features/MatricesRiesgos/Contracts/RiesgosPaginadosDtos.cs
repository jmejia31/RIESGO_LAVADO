using RL.API.Shared.Results;

namespace RL.API.Features.MatricesRiesgos.Contracts;

public sealed class ConsultaRiesgosPaginadaDto
{
    public int Pagina { get; set; } = 1;
    public int TamanoPagina { get; set; } = 25;
    public string? Buscar { get; set; }
    public bool IncluirInactivos { get; set; }
}

public sealed class RiesgosPaginadosDto : PaginadoDto<RiesgoDto>
{
}
