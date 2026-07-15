using Microsoft.AspNetCore.Http;
using RL.API.Features.Listas.Contracts;
using RL.API.Shared.Results;

namespace RL.API.Features.Listas.Application;

public interface IListasService
{
    Task<List<CoincidenciaJuridicaDto>> ObtenerJuridicasAsync();
    Task<List<CoincidenciaNaturalDto>> ObtenerNaturalesAsync();
    Task<List<CoincidenciaEmpleadoDto>> ObtenerEmpleadosAsync();
    Task<List<DetalleCoincidenciaNaturalDto>> ObtenerDetalleNaturalAsync(string numeroIdentificacion);
    Task<List<DetalleCoincidenciaEmpleadoDto>> ObtenerDetalleEmpleadoAsync(string numeroIdentificacion);
    Task<List<TipoDocumentoDto>> ObtenerTiposDocumentoAsync();
    Task<List<TipoListaCautelaDto>> ObtenerTiposListasCautelaAsync();
    Task<List<ResumenListaDto>> ObtenerResumenListasAsync();
    Task<ServiceResult<List<Dictionary<string, object>>>> ObtenerDetalleListaParaExportarAsync(int id, long usuarioId, string? ip);
    Task<ServiceResult<TipoListaCautelaCreadaDto>> CrearTipoListaCautelaAsync(TipoListaCautelaDto dto, long usuarioId);
    Task<ServiceResult> ActualizarTipoListaCautelaAsync(int id, TipoListaCautelaDto dto, long usuarioId);
    Task<ServiceResult> EliminarTipoListaCautelaAsync(int id, long usuarioId);
    Task<ServiceResult> RegistrarPositivoAsync(RegistrarPositivoDto dto, long creadoPorId);
    Task<ExistingPositivoDto?> ObtenerPositivoPorDocumentoAsync(string noDocumento);
    Task<ServiceResult<List<SeguimientoDto>>> ObtenerSeguimientosAsync(string noDocumento, DateTime? desde, DateTime? hasta);
    Task<ServiceResult> ProcesarCargaCautelaAsync(IFormFile? archivo, int tipoListaCautelaId, long usuarioId);
}
