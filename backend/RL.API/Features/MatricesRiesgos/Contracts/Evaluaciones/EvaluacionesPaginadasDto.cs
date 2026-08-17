using System;
using System.Collections.Generic;

namespace RL.API.Features.MatricesRiesgos.Contracts;

public sealed class EvaluacionesPaginadasDto
{
    public List<EvaluacionRiesgoResumenDto> Items { get; set; } = new();
    public int Pagina { get; set; }
    public int RegistrosPorPagina { get; set; }
    public int TotalRegistros { get; set; }
    public int TotalPaginas => RegistrosPorPagina > 0
        ? (int)Math.Ceiling((double)TotalRegistros / RegistrosPorPagina)
        : 0;
}
