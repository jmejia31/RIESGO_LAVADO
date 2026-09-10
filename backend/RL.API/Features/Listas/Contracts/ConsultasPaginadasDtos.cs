using System;
using RL.API.Shared.Results;

namespace RL.API.Features.Listas.Contracts;

public sealed class ConsultaCoincidenciasPaginadaDto
{
    public int Pagina { get; set; } = 1;
    public int TamanoPagina { get; set; } = 25;
    public string? Buscar { get; set; }
    public string? Calificacion { get; set; }
    public string? Fecha { get; set; }
}

public sealed class ConsultaMonitoreoPaginadaDto
{
    public int Pagina { get; set; } = 1;
    public int TamanoPagina { get; set; } = 10;
    public string? Buscar { get; set; }
    public string Estado { get; set; } = "todos";
    public DateTime? FechaDesde { get; set; }
    public DateTime? FechaHasta { get; set; }
}

public sealed class MonitoreoTotalesDto
{
    public int TotalRegistros { get; init; }
    public int Pendientes { get; init; }
    public int ConMotivo { get; init; }
    public int Manuales { get; init; }
    public int CerradosPasivos { get; init; }
}

public sealed class MonitoreoPaginadoDto<T> : PaginadoDto<T>
{
    public MonitoreoTotalesDto Totales { get; init; } = new();
}
