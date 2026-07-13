using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using RL.API.DTOs;
using RL.API.Repositories;

namespace RL.API.Services;

public sealed record TipoListaCautelaCreadaDto(int TipoListaCautelaId, string Descripcion, string? TipoArchivo, int? CantidadColumnas);

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

public sealed class ListasService : IListasService
{
    private readonly IListasRepository _repo;
    private readonly IAuditoriaRepository _auditoriaRepo;

    public ListasService(IListasRepository repo, IAuditoriaRepository auditoriaRepo)
    {
        _repo = repo;
        _auditoriaRepo = auditoriaRepo;
    }

    public Task<List<CoincidenciaJuridicaDto>> ObtenerJuridicasAsync() => _repo.ObtenerJuridicasAsync();
    public Task<List<CoincidenciaNaturalDto>> ObtenerNaturalesAsync() => _repo.ObtenerNaturalesAsync();
    public Task<List<CoincidenciaEmpleadoDto>> ObtenerEmpleadosAsync() => _repo.ObtenerEmpleadosAsync();
    public Task<List<DetalleCoincidenciaNaturalDto>> ObtenerDetalleNaturalAsync(string numeroIdentificacion) => _repo.ObtenerDetalleNaturalAsync(numeroIdentificacion);
    public Task<List<DetalleCoincidenciaEmpleadoDto>> ObtenerDetalleEmpleadoAsync(string numeroIdentificacion) => _repo.ObtenerDetalleEmpleadoAsync(numeroIdentificacion);
    public Task<List<TipoDocumentoDto>> ObtenerTiposDocumentoAsync() => _repo.ObtenerTiposDocumentoAsync();
    public Task<List<TipoListaCautelaDto>> ObtenerTiposListasCautelaAsync() => _repo.ObtenerTiposListasCautelaAsync();
    public Task<List<ResumenListaDto>> ObtenerResumenListasAsync() => _repo.ObtenerResumenListasAsync();
    public Task<ExistingPositivoDto?> ObtenerPositivoPorDocumentoAsync(string noDocumento) => _repo.ObtenerPositivoPorDocumentoAsync(noDocumento);

    public async Task<ServiceResult<List<Dictionary<string, object>>>> ObtenerDetalleListaParaExportarAsync(int id, long usuarioId, string? ip)
    {
        // Proceso de exportación: obtiene datos desde DNP/listas y registra auditoría
        // con usuario, IP y cantidad de registros antes de entregar el resultado al frontend.
        var result = await _repo.ObtenerDetalleListaParaExportarAsync(id);
        var auditoria = JsonConvert.SerializeObject(new
        {
            Accion = "EXPORTACION_EXCEL",
            TipoListaCautelaId = id,
            CantidadRegistros = result.Count
        });

        await _auditoriaRepo.RegistrarAsync("DNP_IHSS.LISTA_CAUTELA", id.ToString(), "VER", null, auditoria, usuarioId, null, ip, "ExportacionListas");
        return ServiceResult<List<Dictionary<string, object>>>.Ok(result);
    }

    public async Task<ServiceResult<TipoListaCautelaCreadaDto>> CrearTipoListaCautelaAsync(TipoListaCautelaDto dto, long usuarioId)
    {
        var newId = await _repo.CrearTipoListaCautelaAsync(dto.Descripcion, dto.TipoArchivo, dto.CantidadColumnas, usuarioId);
        return newId > 0
            ? ServiceResult<TipoListaCautelaCreadaDto>.Ok(
                new TipoListaCautelaCreadaDto(newId, dto.Descripcion, dto.TipoArchivo, dto.CantidadColumnas),
                "Tipo de lista creado exitosamente.")
            : ServiceResult<TipoListaCautelaCreadaDto>.BadRequest("No se pudo crear el tipo de lista.");
    }

    public async Task<ServiceResult> ActualizarTipoListaCautelaAsync(int id, TipoListaCautelaDto dto, long usuarioId)
    {
        var ok = await _repo.ActualizarTipoListaCautelaAsync(id, dto.Descripcion, dto.TipoArchivo, dto.CantidadColumnas, usuarioId);
        return ok
            ? ServiceResult.Ok("Tipo de lista actualizado exitosamente.")
            : ServiceResult.NotFound("No se encontró el tipo de lista a actualizar o no hubo cambios.");
    }

    public async Task<ServiceResult> EliminarTipoListaCautelaAsync(int id, long usuarioId)
    {
        var ok = await _repo.EliminarTipoListaCautelaAsync(id, usuarioId);
        return ok
            ? ServiceResult.Ok("Tipo de lista eliminado exitosamente.")
            : ServiceResult.NotFound("No se encontró el tipo de lista o ya fue eliminado.");
    }

    public async Task<ServiceResult> RegistrarPositivoAsync(RegistrarPositivoDto dto, long creadoPorId)
    {
        var validationError = ValidarPositivo(dto);
        if (validationError != null)
            return ServiceResult.BadRequest(validationError);

        var ok = await _repo.RegistrarPositivoAsync(dto, creadoPorId);
        return ok
            ? ServiceResult.Ok("Motivo registrado exitosamente.")
            : ServiceResult.BadRequest("No se pudo registrar el motivo.");
    }

    public async Task<ServiceResult<List<SeguimientoDto>>> ObtenerSeguimientosAsync(string noDocumento, DateTime? desde, DateTime? hasta)
    {
        if (desde.HasValue && hasta.HasValue && desde.Value.Date > hasta.Value.Date)
            return ServiceResult<List<SeguimientoDto>>.BadRequest("La fecha desde no puede ser mayor que la fecha hasta.");

        var result = await _repo.ObtenerSeguimientosAsync(noDocumento, desde, hasta);
        return ServiceResult<List<SeguimientoDto>>.Ok(result);
    }

    public async Task<ServiceResult> ProcesarCargaCautelaAsync(IFormFile? archivo, int tipoListaCautelaId, long usuarioId)
    {
        // Proceso de carga de listas: valida archivo, identifica el formato aprobado
        // y delega el procesamiento al repositorio especializado sin mezclar reglas en el controlador.
        if (archivo == null || archivo.Length <= 0)
            return ServiceResult.BadRequest("Debe adjuntar un archivo de lista de cautela.");

        var validation = await _repo.ValidarArchivoCautelaAsync(archivo, tipoListaCautelaId);
        if (!validation.EsValido)
            return ServiceResult.BadRequest(validation.Mensaje);

        var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
        (bool Success, string Mensaje) processResult;

        if (extension == ".xml")
        {
            processResult = await _repo.ProcesarArchivoXmlOnuAsync(archivo, tipoListaCautelaId, usuarioId);
        }
        else if (extension == ".xls" || extension == ".xlsx")
        {
            var descripcion = await _repo.ObtenerDescripcionListaAsync(tipoListaCautelaId);
            processResult = descripcion.ToUpperInvariant().Contains("ENGEL")
                ? await _repo.ProcesarArchivoExcelEngelAsync(archivo, tipoListaCautelaId, usuarioId)
                : await _repo.ProcesarArchivoExcelPepsAsync(archivo, tipoListaCautelaId, usuarioId);
        }
        else
        {
            processResult = await _repo.ProcesarArchivoCsvOfacAsync(archivo, tipoListaCautelaId, usuarioId);
        }

        return processResult.Success
            ? ServiceResult.Ok(processResult.Mensaje)
            : ServiceResult.BadRequest(processResult.Mensaje);
    }

    private static string? ValidarPositivo(RegistrarPositivoDto dto)
    {
        // Validación funcional de positivo manual: normaliza datos obligatorios y evita
        // registrar orígenes o tipos no autorizados por el módulo de monitoreo.
        dto.NoDocumento = dto.NoDocumento?.Trim();
        dto.NombreCompleto = dto.NombreCompleto.Trim();
        dto.MotivoIngreso = dto.MotivoIngreso.Trim();
        dto.OrigenRegistro = dto.OrigenRegistro?.Trim().ToUpperInvariant();

        if (dto.TipoDocumentoId <= 0)
            return "El tipo de documento es obligatorio.";

        if (dto.TipoPositivoId is < 1 or > 3)
            return "El tipo de positivo no es válido.";

        if (string.IsNullOrWhiteSpace(dto.NoDocumento))
            return "El numero de documento es obligatorio.";

        if (dto.NoDocumento.Length > 50)
            return "El numero de documento no debe superar 50 caracteres.";

        if (string.IsNullOrWhiteSpace(dto.NombreCompleto))
            return "El nombre completo es obligatorio.";

        if (string.IsNullOrWhiteSpace(dto.MotivoIngreso))
            return "El motivo de ingreso es obligatorio.";
        if (dto.MotivoIngreso.Length > 1000)
            return "El motivo de ingreso no debe superar los 1000 caracteres.";

        if (!dto.TipoListaCautelaId.HasValue || dto.TipoListaCautelaId <= 0)
            return "El tipo de lista de cautela es obligatorio.";

        var origenesValidos = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "DNP_LISTAS",
            "MANUAL_CUMPLIMIENTO",
            "NOTICIA_PRENSA",
            "OTRO"
        };

        if (string.IsNullOrWhiteSpace(dto.OrigenRegistro) || !origenesValidos.Contains(dto.OrigenRegistro))
            return "El origen del registro no es válido.";

        return null;
    }
}
