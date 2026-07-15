using Microsoft.AspNetCore.Http;
using RL.API.Features.Listas.Contracts;
using RL.API.Shared.Results;

namespace RL.API.Features.Listas.Application;

public interface IEvidenciasService
{
    EvidenciaPoliticaDto ObtenerPolitica();
    string? ValidarArchivos(List<IFormFile>? archivos);
    Task GuardarArchivosAsync(long detalleId, List<IFormFile>? archivos, long usuarioId);
    Task<ServiceResult> RegistrarSeguimientoAsync(string noDocumento, string? motivoIngreso, List<IFormFile>? archivos, long usuarioId);
    Task<ServiceResult> ActualizarSeguimientoAsync(long detalleId, string? motivoIngreso, List<IFormFile>? archivos, long usuarioId);
    Task<ServiceResult<EvidenciaDescargaDto>> DescargarEvidenciaAsync(long evidenciaId, long usuarioId);
    Task<ServiceResult> EliminarEvidenciaAsync(long evidenciaId, string? motivoEliminacion, long usuarioId);
    Task<ServiceResult> EliminarSeguimientoAsync(long detalleId, string? motivoEliminacion, long usuarioId);
    Task RegistrarReporteImpresoAsync(string noDocumento, string dataJson, long usuarioId);
}
