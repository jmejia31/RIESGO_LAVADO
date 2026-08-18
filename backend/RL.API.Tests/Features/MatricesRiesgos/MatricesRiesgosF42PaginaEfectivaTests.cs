#pragma warning disable CA1707
using System;
using Xunit;

namespace RL.API.Tests.Features.MatricesRiesgos;

public sealed class MatricesRiesgosF42PaginaEfectivaTests
{
    public static int CalcularPaginaEfectiva(int totalRegistros, int registrosPorPagina, int paginaSolicitada)
    {
        int totalPaginas = registrosPorPagina > 0
            ? (int)Math.Ceiling((double)totalRegistros / registrosPorPagina)
            : 0;

        return totalPaginas == 0
            ? 1
            : Math.Min(paginaSolicitada, totalPaginas);
    }

    [Fact]
    public void PaginaSolicitada_MayorQueTotalPaginas_DevuelveUltimaPaginaValida()
    {
        // 15 registros con page-size 10 -> total 2 páginas. Se solicita página 8.
        int paginaEfectiva = CalcularPaginaEfectiva(totalRegistros: 15, registrosPorPagina: 10, paginaSolicitada: 8);
        Assert.Equal(2, paginaEfectiva);
    }

    [Fact]
    public void TotalRegistrosCero_DevuelvePagina1()
    {
        // 0 registros. Se solicita página 8.
        int paginaEfectiva = CalcularPaginaEfectiva(totalRegistros: 0, registrosPorPagina: 10, paginaSolicitada: 8);
        Assert.Equal(1, paginaEfectiva);
    }

    [Fact]
    public void PaginaSolicitada_DentroDeRango_ConservaPagina()
    {
        // 25 registros con page-size 10 -> total 3 páginas. Se solicita página 2.
        int paginaEfectiva = CalcularPaginaEfectiva(totalRegistros: 25, registrosPorPagina: 10, paginaSolicitada: 2);
        Assert.Equal(2, paginaEfectiva);
    }
}
