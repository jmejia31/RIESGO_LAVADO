namespace RL.API.Features.MatricesRiesgos.Domain;

public static class PaginacionEvaluacionesHelper
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
}
