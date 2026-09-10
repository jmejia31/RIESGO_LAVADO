using Microsoft.AspNetCore.Http;
using RL.API.Features.Listas.Contracts;
using RL.API.Shared.Results;

namespace RL.API.Features.Listas.Application;

public interface IListasService
{
    Task<MonitoreoPaginadoDto<CoincidenciaJuridicaDto>> ObtenerJuridicasPaginadasAsync(ConsultaMonitoreoPaginadaDto consulta, CancellationToken cancellationToken = default);
    Task<List<CoincidenciaJuridicaDto>> ObtenerJuridicasParaExportarAsync(ConsultaMonitoreoPaginadaDto consulta);
    Task<MonitoreoPaginadoDto<CoincidenciaNaturalDto>> ObtenerNaturalesPaginadasAsync(ConsultaMonitoreoPaginadaDto consulta, CancellationToken cancellationToken = default);
    Task<List<CoincidenciaNaturalDto>> ObtenerNaturalesParaExportarAsync(ConsultaMonitoreoPaginadaDto consulta);
    Task<MonitoreoPaginadoDto<CoincidenciaEmpleadoDto>> ObtenerEmpleadosPaginadasAsync(ConsultaMonitoreoPaginadaDto consulta, CancellationToken cancellationToken = default);
    Task<List<CoincidenciaEmpleadoDto>> ObtenerEmpleadosParaExportarAsync(ConsultaMonitoreoPaginadaDto consulta);
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
