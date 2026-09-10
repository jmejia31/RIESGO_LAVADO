using System;
using System.Collections.Generic;

namespace RL.API.Shared.Results;

/// <summary>
/// Contrato común para consultas de grillas paginadas en base de datos.
/// Items contiene únicamente la página solicitada; los totales representan
/// el universo filtrado completo.
/// </summary>
public class PaginadoDto<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    public int Pagina { get; init; }
    public int TamanoPagina { get; init; }
    public int TotalRegistros { get; init; }
    public int TotalPaginas { get; init; }
}
