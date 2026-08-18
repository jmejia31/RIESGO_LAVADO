#pragma warning disable CA1707
using RL.API.Features.MatricesRiesgos.Domain;
using Xunit;

namespace RL.API.Tests.Features.MatricesRiesgos;

public sealed class MatricesRiesgosF42PaginaEfectivaTests
{
    [Fact]
    public void PaginaSolicitada_MayorQueTotalPaginas_DevuelveUltimaPaginaValida()
    {
        // 15 registros / size 10 / página 8 => 2
        int paginaEfectiva = PaginacionEvaluacionesHelper.CalcularPaginaEfectiva(totalRegistros: 15, registrosPorPagina: 10, paginaSolicitada: 8);
        Assert.Equal(2, paginaEfectiva);
    }

    [Fact]
    public void TotalRegistrosCero_DevuelvePagina1()
    {
        // 0 registros / size 10 / página 8 => 1
        int paginaEfectiva = PaginacionEvaluacionesHelper.CalcularPaginaEfectiva(totalRegistros: 0, registrosPorPagina: 10, paginaSolicitada: 8);
        Assert.Equal(1, paginaEfectiva);
    }

    [Fact]
    public void PaginaSolicitada_DentroDeRango_ConservaPagina()
    {
        // 25 registros / size 10 / página 2 => 2
        int paginaEfectiva = PaginacionEvaluacionesHelper.CalcularPaginaEfectiva(totalRegistros: 25, registrosPorPagina: 10, paginaSolicitada: 2);
        Assert.Equal(2, paginaEfectiva);
    }
}
