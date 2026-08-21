using RL.API.Features.MatricesRiesgos.Persistence;
using RL.API.Infrastructure.Caching;
using RL.API.Shared.Results;

namespace RL.API.Features.MatricesRiesgos.Application;

public sealed class FamiliasFormularioLifecycleService
{
    private readonly IFamiliasFormularioLifecycleRepository _repository;
    private readonly IApplicationCache _cache;

    public FamiliasFormularioLifecycleService(
        IFamiliasFormularioLifecycleRepository repository,
        IApplicationCache cache)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    public async Task<ServiceResult> ActivarFamiliaFormularioAsync(long famId)
    {
        if (famId <= 0)
        {
            return ServiceResult.BadRequest("El ID de familia especificado es inválido.");
        }

        ResultadoCambioEstadoFamiliaFormulario resultado =
            await _repository.ActivarFamiliaFormularioAtomicoAsync(famId);

        return resultado switch
        {
            ResultadoCambioEstadoFamiliaFormulario.Exito => InvalidarYRetornar(
                "Familia de formulario activada exitosamente."),
            ResultadoCambioEstadoFamiliaFormulario.YaEstabaEnEstado =>
                ServiceResult.Ok("La familia de formulario ya se encuentra activa."),
            ResultadoCambioEstadoFamiliaFormulario.NoExiste =>
                ServiceResult.NotFound($"No se encontró la familia de formulario con ID {famId}."),
            _ => ServiceResult.BadRequest("No se pudo activar la familia de formulario.")
        };
    }

    public async Task<ServiceResult> EliminarFamiliaFormularioAsync(long famId)
    {
        if (famId <= 0)
        {
            return ServiceResult.BadRequest("El ID de familia especificado es inválido.");
        }

        ResultadoEliminacionFamiliaFormulario resultado =
            await _repository.EliminarFamiliaFormularioSeguraAsync(famId);

        return resultado switch
        {
            ResultadoEliminacionFamiliaFormulario.Exito => InvalidarYRetornar(
                "Familia de formulario eliminada exitosamente."),
            ResultadoEliminacionFamiliaFormulario.NoExiste =>
                ServiceResult.NotFound($"No se encontró la familia de formulario con ID {famId}."),
            ResultadoEliminacionFamiliaFormulario.TieneVersiones =>
                ServiceResult.BadRequest(
                    "No se puede eliminar la familia porque posee versiones asociadas. Desactívela para conservar la trazabilidad histórica."),
            _ => ServiceResult.BadRequest("No se pudo eliminar la familia de formulario.")
        };
    }

    private ServiceResult InvalidarYRetornar(string mensaje)
    {
        _cache.Invalidate(ApplicationCacheScopes.MatricesFormularios);
        return ServiceResult.Ok(mensaje);
    }
}
