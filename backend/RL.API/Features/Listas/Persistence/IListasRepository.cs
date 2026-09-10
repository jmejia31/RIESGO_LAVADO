using Microsoft.AspNetCore.Http;
using RL.API.Features.Listas.Contracts;
using RL.API.Shared.Results;

namespace RL.API.Features.Listas.Persistence;

public interface IListasRepository
{
    Task<List<CoincidenciaJuridicaDto>> ObtenerJuridicasAsync();
    Task<MonitoreoPaginadoDto<CoincidenciaJuridicaDto>> ObtenerJuridicasPaginadasAsync(ConsultaMonitoreoPaginadaDto consulta);
    Task<List<CoincidenciaJuridicaDto>> ObtenerJuridicasParaExportarAsync(ConsultaMonitoreoPaginadaDto consulta);
    Task<List<CoincidenciaNaturalDto>> ObtenerNaturalesAsync();
    Task<MonitoreoPaginadoDto<CoincidenciaNaturalDto>> ObtenerNaturalesPaginadasAsync(ConsultaMonitoreoPaginadaDto consulta);
    Task<List<CoincidenciaNaturalDto>> ObtenerNaturalesParaExportarAsync(ConsultaMonitoreoPaginadaDto consulta);
    Task<List<CoincidenciaEmpleadoDto>> ObtenerEmpleadosAsync();
    Task<MonitoreoPaginadoDto<CoincidenciaEmpleadoDto>> ObtenerEmpleadosPaginadasAsync(ConsultaMonitoreoPaginadaDto consulta);
    Task<List<CoincidenciaEmpleadoDto>> ObtenerEmpleadosParaExportarAsync(ConsultaMonitoreoPaginadaDto consulta);
    Task<List<DetalleCoincidenciaNaturalDto>> ObtenerDetalleNaturalAsync(string numeroIdentificacion);
    Task<List<DetalleCoincidenciaEmpleadoDto>> ObtenerDetalleEmpleadoAsync(string numeroIdentificacion);
    Task<List<TipoDocumentoDto>> ObtenerTiposDocumentoAsync();
    Task<List<TipoListaCautelaDto>> ObtenerTiposListasCautelaAsync();
    Task<bool> RegistrarPositivoAsync(RegistrarPositivoDto dto, long creadoPorId);
    Task<ExistingPositivoDto?> ObtenerPositivoPorDocumentoAsync(string noDocumento);
    Task<List<SeguimientoDto>> ObtenerSeguimientosAsync(string noDocumento, DateTime? desde = null, DateTime? hasta = null);
    Task<long> RegistrarSeguimientoAsync(long positivoId, string motivo, long usuarioId);
    Task GuardarEvidenciaMetaAsync(long detalleId, string nombreArchivo, string tipoMime, string rutaArchivo, long usuarioId);
    Task<long?> ObtenerPositivoIdPorDocumentoAsync(string noDocumento);
    Task<(string Nombre, string Ruta, string Mime)?> ObtenerEvidenciaPorIdAsync(long evidenciaId);
    Task RegistrarAuditoriaVisualizacionAsync(long evidenciaId, string dataJson, long usuarioId);
    Task<bool> ActualizarSeguimientoAsync(long detalleId, string motivoIngreso, long usuarioId);
    Task<bool> EliminarEvidenciaMetaAsync(long evidenciaId, long usuarioId, string motivoEliminacion);
    Task<bool> EliminarSeguimientoLogicoAsync(long detalleId, long usuarioId, string motivoEliminacion);
    Task RegistrarAuditoriaReporteImpresoAsync(string noDocumento, string dataJson, long usuarioId);
    Task<int> CrearTipoListaCautelaAsync(string descripcion, string? tipoArchivo, int? cantidadColumnas, long usuarioId);
    Task<bool> ActualizarTipoListaCautelaAsync(int id, string descripcion, string? tipoArchivo, int? cantidadColumnas, long usuarioId);
    Task<bool> EliminarTipoListaCautelaAsync(int id, long usuarioId);
    Task<List<ResumenListaDto>> ObtenerResumenListasAsync();
    Task<List<Dictionary<string, object>>> ObtenerDetalleListaParaExportarAsync(int tipoListaId);
    Task<List<CoincidenciaPatronoResumenDto>> ObtenerResumenCoincidenciasPatronoAsync();
    Task<PaginadoDto<CoincidenciaPatronoResumenDto>> ObtenerResumenCoincidenciasPatronoPaginadoAsync(ConsultaCoincidenciasPaginadaDto consulta);
    Task<List<CoincidenciaPatronoDetalleDto>> ObtenerDetalleCoincidenciasPatronoAsync(string fecha);
    Task<PaginadoDto<CoincidenciaPatronoDetalleDto>> ObtenerDetalleCoincidenciasPatronoPaginadoAsync(ConsultaCoincidenciasPaginadaDto consulta);
    Task<List<CoincidenciaPatronoResumenDto>> ObtenerResumenCoincidenciasEmpleadoAsync();
    Task<PaginadoDto<CoincidenciaPatronoResumenDto>> ObtenerResumenCoincidenciasEmpleadoPaginadoAsync(ConsultaCoincidenciasPaginadaDto consulta);
    Task<List<CoincidenciaPatronoDetalleDto>> ObtenerDetalleCoincidenciasEmpleadoAsync(string fecha);
    Task<PaginadoDto<CoincidenciaPatronoDetalleDto>> ObtenerDetalleCoincidenciasEmpleadoPaginadoAsync(ConsultaCoincidenciasPaginadaDto consulta);
    Task<bool> CalificarCoincidenciaAsync(long reporteCoincidenciaId, int tipoCalificacionId, long usuarioId, bool esEmpleado);
    Task<string> ObtenerResumenMatchListaAsync(long dataId, string nombre);
    Task<(bool EsValido, string Mensaje)> ValidarArchivoCautelaAsync(IFormFile archivo, int tipoListaCautelaId);
    Task<(bool Success, string Mensaje)> ProcesarArchivoCsvOfacAsync(IFormFile archivo, int tipoListaCautelaId, long usuarioId);
    Task<(bool Success, string Mensaje)> ProcesarArchivoXmlOnuAsync(IFormFile archivo, int tipoListaCautelaId, long usuarioId);
    Task<(bool Success, string Mensaje)> ProcesarArchivoExcelEngelAsync(IFormFile archivo, int tipoListaCautelaId, long usuarioId);
    Task<(bool Success, string Mensaje)> ProcesarArchivoExcelPepsAsync(IFormFile archivo, int tipoListaCautelaId, long usuarioId);
    Task<string> ObtenerDescripcionListaAsync(int tipoListaCautelaId);
}
