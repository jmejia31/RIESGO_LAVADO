using RL.API.Features.MatricesRiesgos.Persistence;
using RL.API.Features.MatricesRiesgos.Contracts;
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
            ResultadoCambioEstadoFamiliaFormulario.EstaArchivada =>
                ServiceResult.Conflict("La familia archivada debe restaurarse antes de activarla."),
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

    public async Task<ServiceResult> ArchivarFamiliaFormularioAsync(long famId, string? motivo)
    {
        if (famId <= 0) return ServiceResult.BadRequest("El ID de familia especificado es inválido.");
        if (string.IsNullOrWhiteSpace(motivo)) return ServiceResult.BadRequest("La justificación del archivado es obligatoria.");
        if (motivo.Trim().Length > 500) return ServiceResult.BadRequest("La justificación no puede exceder 500 caracteres.");

        ResultadoArchivadoFamiliaFormulario resultado = await _repository.ArchivarFamiliaFormularioAtomicoAsync(famId, motivo.Trim());
        return resultado switch
        {
            ResultadoArchivadoFamiliaFormulario.Exito => InvalidarYRetornar("Familia de formulario archivada correctamente. El historial se conserva íntegramente."),
            ResultadoArchivadoFamiliaFormulario.NoExiste => ServiceResult.NotFound($"No se encontró la familia de formulario con ID {famId}."),
            ResultadoArchivadoFamiliaFormulario.YaArchivada => ServiceResult.Conflict("La familia ya se encuentra archivada."),
            ResultadoArchivadoFamiliaFormulario.FamiliaActiva => ServiceResult.Conflict("Solo se pueden archivar familias inactivas."),
            ResultadoArchivadoFamiliaFormulario.EsPredeterminada => ServiceResult.Conflict("No se puede archivar la familia predeterminada."),
            ResultadoArchivadoFamiliaFormulario.TieneVersionVigente => ServiceResult.Conflict("No se puede archivar la familia porque posee una versión publicada y vigente."),
            _ => ServiceResult.BadRequest("No se pudo archivar la familia de formulario.")
        };
    }

    public async Task<ServiceResult> RestaurarFamiliaFormularioAsync(long famId, string? motivo)
    {
        if (famId <= 0) return ServiceResult.BadRequest("El ID de familia especificado es inválido.");
        if (string.IsNullOrWhiteSpace(motivo)) return ServiceResult.BadRequest("La justificacion de la restauracion es obligatoria.");
        if (motivo.Trim().Length > 500) return ServiceResult.BadRequest("La justificacion no puede exceder 500 caracteres.");
        ResultadoRestauracionFamiliaFormulario resultado = await _repository.RestaurarFamiliaFormularioAtomicoAsync(famId, motivo.Trim());
        return resultado switch
        {
            ResultadoRestauracionFamiliaFormulario.Exito => InvalidarYRetornar("Familia restaurada como inactiva. No se publicaron ni activaron versiones."),
            ResultadoRestauracionFamiliaFormulario.NoExiste => ServiceResult.NotFound($"No se encontró la familia de formulario con ID {famId}."),
            ResultadoRestauracionFamiliaFormulario.NoEstaArchivada => ServiceResult.Conflict("La familia no se encuentra archivada."),
            _ => ServiceResult.BadRequest("No se pudo restaurar la familia de formulario.")
        };
    }

    public async Task<ServiceResult<FamiliaFormularioPurgaPrecheckDto>> ObtenerPurgaPrecheckAsync(long famId)
    {
        if (famId <= 0) return ServiceResult<FamiliaFormularioPurgaPrecheckDto>.BadRequest("El ID de familia especificado es inválido.");
        FamiliaFormularioPurgaPrecheckDto? resultado = await _repository.ObtenerPurgaPrecheckAsync(famId);
        return resultado is null
            ? ServiceResult<FamiliaFormularioPurgaPrecheckDto>.NotFound($"No se encontró la familia de formulario con ID {famId}.")
            : ServiceResult<FamiliaFormularioPurgaPrecheckDto>.Ok(resultado);
    }

    private ServiceResult InvalidarYRetornar(string mensaje)
    {
        _cache.Invalidate(ApplicationCacheScopes.MatricesFormularios);
        return ServiceResult.Ok(mensaje);
    }
}
