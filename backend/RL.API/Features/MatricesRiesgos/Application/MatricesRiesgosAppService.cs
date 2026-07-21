using RL.API.Features.MatricesRiesgos.Contracts;
using RL.API.Features.MatricesRiesgos.Domain;
using RL.API.Features.MatricesRiesgos.Persistence;
using RL.API.Shared.Results;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace RL.API.Features.MatricesRiesgos.Application;

public sealed class MatricesRiesgosAppService : IMatricesRiesgosAppService
{
    private static readonly HashSet<string> SujetosPermitidos = new(StringComparer.OrdinalIgnoreCase)
    {
        "PROVEEDOR",
        "CLIENTE_PATRONO",
        "EMPLEADO",
        "AREA",
        "PROCESO",
        "CASO_POSITIVO",
        "INSTITUCIONAL"
    };

    private static readonly HashSet<string> EstadosPermitidos = new(StringComparer.OrdinalIgnoreCase)
    {
        "EN_REVISION",
        "APROBADA",
        "CERRADA",
        "INACTIVA"
    };

    private static readonly HashSet<string> EstadosPlanPermitidos = new(StringComparer.OrdinalIgnoreCase)
    {
        "PENDIENTE",
        "EN_PROCESO",
        "CERRADO",
        "VENCIDO",
        "INACTIVO"
    };

    private static readonly HashSet<string> ExtensionesEvidenciaPermitidas = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf",
        ".png",
        ".jpg",
        ".jpeg",
        ".doc",
        ".docx",
        ".xls",
        ".xlsx"
    };

    private static readonly Dictionary<string, byte[][]> FirmasEvidenciaPermitidas = new(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = new[] { new byte[] { 0x25, 0x50, 0x44, 0x46 } },
        [".png"] = new[] { new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A } },
        [".jpg"] = new[] { new byte[] { 0xFF, 0xD8, 0xFF } },
        [".jpeg"] = new[] { new byte[] { 0xFF, 0xD8, 0xFF } },
        [".doc"] = new[] { new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 } },
        [".xls"] = new[] { new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 } },
        [".docx"] = new[] { new byte[] { 0x50, 0x4B, 0x03, 0x04 }, new byte[] { 0x50, 0x4B, 0x05, 0x06 }, new byte[] { 0x50, 0x4B, 0x07, 0x08 } },
        [".xlsx"] = new[] { new byte[] { 0x50, 0x4B, 0x03, 0x04 }, new byte[] { 0x50, 0x4B, 0x05, 0x06 }, new byte[] { 0x50, 0x4B, 0x07, 0x08 } }
    };

    private static readonly Dictionary<string, string[]> MimeTypesEvidenciaPermitidos = new(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = new[] { "application/pdf" },
        [".png"] = new[] { "image/png" },
        [".jpg"] = new[] { "image/jpeg", "image/pjpeg" },
        [".jpeg"] = new[] { "image/jpeg", "image/pjpeg" },
        [".doc"] = new[] { "application/msword", "application/octet-stream" },
        [".docx"] = new[] { "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "application/zip", "application/octet-stream" },
        [".xls"] = new[] { "application/vnd.ms-excel", "application/octet-stream" },
        [".xlsx"] = new[] { "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "application/zip", "application/octet-stream" }
    };

    private readonly IMatricesRiesgosRepository _repo;
    private readonly IMatricesRiesgoService _motorCalculo;
    private readonly IConfiguration? _configuration;
    private readonly IWebHostEnvironment? _environment;

    public MatricesRiesgosAppService(IMatricesRiesgosRepository repo, IMatricesRiesgoService motorCalculo, IConfiguration? configuration = null, IWebHostEnvironment? environment = null)
    {
        _repo = repo;
        _motorCalculo = motorCalculo;
        _configuration = configuration;
        _environment = environment;
    }

    public async Task<ServiceResult<MetodologiaCalculoDto>> ObtenerMetodologiaVigenteAsync()
    {
        var metodologia = await _repo.ObtenerMetodologiaVigenteAsync();
        return metodologia == null
            ? ServiceResult<MetodologiaCalculoDto>.NotFound("No existe una metodología aprobada vigente para Matrices de Riesgos.")
            : ServiceResult<MetodologiaCalculoDto>.Ok(metodologia);
    }

    public async Task<ServiceResult<MatricesRiesgoDashboardDto>> ObtenerDashboardAsync(MatrizRiesgoReporteFiltroDto filtro)
    {
        filtro ??= new MatrizRiesgoReporteFiltroDto();
        var errorFiltro = NormalizarFiltroReporte(filtro);
        if (errorFiltro != null)
            return ServiceResult<MatricesRiesgoDashboardDto>.BadRequest(errorFiltro);

        var dashboard = await _repo.ObtenerDashboardAsync(filtro);
        return ServiceResult<MatricesRiesgoDashboardDto>.Ok(dashboard);
    }

    public async Task<ServiceResult<MatricesRiesgoReporteDto>> ObtenerReporteAsync(MatrizRiesgoReporteFiltroDto filtro)
    {
        var errorFiltro = NormalizarFiltroReporte(filtro);
        if (errorFiltro != null)
            return ServiceResult<MatricesRiesgoReporteDto>.BadRequest(errorFiltro);

        var reporte = await _repo.ObtenerReporteAsync(filtro);
        return ServiceResult<MatricesRiesgoReporteDto>.Ok(reporte);
    }

    public async Task<ServiceResult<MatrizRiesgoExportacionDto>> ExportarReporteAsync(MatrizRiesgoReporteFiltroDto filtro, string formato, long usuarioId, string? usuarioEmail, string? ip)
    {
        var errorFiltro = NormalizarFiltroReporte(filtro);
        if (errorFiltro != null)
            return ServiceResult<MatrizRiesgoExportacionDto>.BadRequest(errorFiltro);

        var formatoNormalizado = string.IsNullOrWhiteSpace(formato) ? "EXCEL" : formato.Trim().ToUpperInvariant();
        if (formatoNormalizado != "EXCEL" && formatoNormalizado != "PDF")
            return ServiceResult<MatrizRiesgoExportacionDto>.BadRequest("El formato de exportación debe ser EXCEL o PDF.");

        var reporte = await _repo.ObtenerReporteAsync(filtro);
        var archivo = formatoNormalizado == "PDF"
            ? ConstruirPdfReporte(reporte)
            : ConstruirExcelReporte(reporte);

        await _repo.RegistrarExportacionReporteAsync(filtro, formatoNormalizado, usuarioId, usuarioEmail, ip);
        return ServiceResult<MatrizRiesgoExportacionDto>.Ok(archivo, "Reporte generado correctamente.");
    }

    public async Task<ServiceResult<List<MatrizRiesgoResumenDto>>> ListarAsync(MatrizRiesgoFiltroDto filtro)
    {
        var datos = await _repo.ListarMatricesAsync(filtro);
        return ServiceResult<List<MatrizRiesgoResumenDto>>.Ok(datos);
    }

    public async Task<ServiceResult<MatrizRiesgoDetalleDto>> ObtenerAsync(long matrizId)
    {
        if (matrizId <= 0)
            return ServiceResult<MatrizRiesgoDetalleDto>.BadRequest("El identificador de la matriz es obligatorio.");

        var matriz = await _repo.ObtenerMatrizAsync(matrizId);
        return matriz == null
            ? ServiceResult<MatrizRiesgoDetalleDto>.NotFound("No se encontró la matriz de riesgos.")
            : ServiceResult<MatrizRiesgoDetalleDto>.Ok(matriz);
    }

    public async Task<ServiceResult<MatrizRiesgoDetalleDto>> CrearAsync(MatrizRiesgoCrearRequestDto dto, long usuarioId, string? usuarioEmail, string? ip)
    {
        var error = ValidarCreacion(dto);
        if (error != null)
            return ServiceResult<MatrizRiesgoDetalleDto>.BadRequest(error);

        try
        {
            var matrizId = await _repo.CrearMatrizAsync(dto, usuarioId, usuarioEmail, ip);
            var matriz = await _repo.ObtenerMatrizAsync(matrizId);
            return matriz == null
                ? ServiceResult<MatrizRiesgoDetalleDto>.NotFound("La matriz fue creada, pero no pudo consultarse el detalle.")
                : ServiceResult<MatrizRiesgoDetalleDto>.Ok(matriz, "Matriz de riesgos creada correctamente.");
        }
        catch (InvalidOperationException ex)
        {
            return ServiceResult<MatrizRiesgoDetalleDto>.BadRequest(ex.Message);
        }
    }

    public async Task<ServiceResult<MatrizRiesgoDetalleDto>> ActualizarAsync(long matrizId, MatrizRiesgoCrearRequestDto dto, long usuarioId, string? usuarioEmail, string? ip)
    {
        if (matrizId <= 0)
            return ServiceResult<MatrizRiesgoDetalleDto>.BadRequest("El identificador de la matriz es obligatorio.");

        var error = ValidarCreacion(dto);
        if (error != null)
            return ServiceResult<MatrizRiesgoDetalleDto>.BadRequest(error);

        try
        {
            var ok = await _repo.ActualizarMatrizAsync(matrizId, dto, usuarioId, usuarioEmail, ip);
            if (!ok)
                return ServiceResult<MatrizRiesgoDetalleDto>.NotFound("No se encontró la matriz de riesgos.");

            var matriz = await _repo.ObtenerMatrizAsync(matrizId);
            return matriz == null
                ? ServiceResult<MatrizRiesgoDetalleDto>.NotFound("La matriz fue actualizada, pero no pudo consultarse el detalle.")
                : ServiceResult<MatrizRiesgoDetalleDto>.Ok(matriz, "Matriz de riesgos actualizada correctamente.");
        }
        catch (InvalidOperationException ex)
        {
            return ServiceResult<MatrizRiesgoDetalleDto>.BadRequest(ex.Message);
        }
    }

    public async Task<ServiceResult<MatrizCalculoResultadoDto>> CalcularAsync(long matrizId, MatrizRiesgoCalcularRequestDto dto, bool esRecalculo, long usuarioId, string? usuarioEmail, string? ip)
    {
        if (matrizId <= 0)
            return ServiceResult<MatrizCalculoResultadoDto>.BadRequest("El identificador de la matriz es obligatorio.");

        if (esRecalculo && string.IsNullOrWhiteSpace(dto.MotivoCalculo))
            return ServiceResult<MatrizCalculoResultadoDto>.BadRequest("El motivo de recálculo es obligatorio.");

        try
        {
            // Orquestación del cálculo: arma la solicitud desde datos persistidos,
            // ejecuta el motor aprobado y luego guarda resultado, historial y auditoría.
            var solicitud = await _repo.PrepararSolicitudCalculoAsync(matrizId, dto.TipoCalculo, dto.MotivoCalculo, esRecalculo);
            if (solicitud == null)
                return ServiceResult<MatrizCalculoResultadoDto>.NotFound("No se encontró la matriz de riesgos.");

            var resultado = _motorCalculo.Calcular(solicitud);
            if (!resultado.Success || resultado.Data == null)
                return ServiceResult<MatrizCalculoResultadoDto>.BadRequest(resultado.Message ?? "No se pudo calcular la matriz de riesgos.");

            await _repo.PersistirResultadoCalculoAsync(matrizId, resultado.Data, dto.MotivoCalculo, esRecalculo, usuarioId, usuarioEmail, ip);
            return ServiceResult<MatrizCalculoResultadoDto>.Ok(resultado.Data, esRecalculo ? "Matriz recalculada correctamente." : "Matriz calculada correctamente.");
        }
        catch (InvalidOperationException ex)
        {
            return ServiceResult<MatrizCalculoResultadoDto>.BadRequest(ex.Message);
        }
    }

    public async Task<ServiceResult> CambiarEstadoAsync(long matrizId, MatrizRiesgoCambiarEstadoRequestDto dto, long usuarioId, string? usuarioEmail, string? ip)
    {
        if (matrizId <= 0)
            return ServiceResult.BadRequest("El identificador de la matriz es obligatorio.");

        if (dto == null)
            return ServiceResult.BadRequest("La solicitud de cambio de estado es obligatoria.");

        var estado = dto.Estado?.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(estado) || !EstadosPermitidos.Contains(estado))
            return ServiceResult.BadRequest("El estado solicitado no es válido para la gestión operativa de Matrices de Riesgos.");

        if (string.IsNullOrWhiteSpace(dto.Motivo))
            return ServiceResult.BadRequest("El motivo del cambio de estado es obligatorio.");

        try
        {
            // Regla operativa Fase 11: el cálculo/recálculo se ejecuta al guardar,
            // por eso la API solo permite estados de revisión, aprobación, cierre e inactivación.
            var matriz = await _repo.ObtenerMatrizAsync(matrizId);
            if (matriz == null)
                return ServiceResult.NotFound("No se encontró la matriz de riesgos.");

            if (matriz.Estado.Equals("INACTIVA", StringComparison.OrdinalIgnoreCase) && estado != "EN_REVISION")
                return ServiceResult.BadRequest("Una matriz inactiva solo puede activarse nuevamente al estado En Revisión.");

            if (estado == "CERRADA")
            {
                // El cierre exige gestión documentada cuando el residual requiere plan.
                // Así se evita cerrar riesgos altos/críticos sin tratamiento.
                if (matriz.RequierePlanAccion && !await _repo.TienePlanTratadoParaCierreAsync(matrizId))
                    return ServiceResult.BadRequest("No se puede cerrar la matriz porque requiere plan de acción y no tiene un plan cerrado o una justificación aprobada.");
            }

            var ok = await _repo.CambiarEstadoAsync(matrizId, estado, dto.Motivo, usuarioId, usuarioEmail, ip);
            return ok
                ? ServiceResult.Ok("Estado de la matriz actualizado correctamente.")
                : ServiceResult.NotFound("No se encontró la matriz de riesgos.");
        }
        catch (InvalidOperationException ex)
        {
            return ServiceResult.BadRequest(ex.Message);
        }
    }

    public async Task<ServiceResult> EliminarMatrizAsync(long matrizId, MatrizRiesgoInactivarRequestDto dto, long usuarioId, string? usuarioEmail, string? ip)
    {
        if (matrizId <= 0)
            return ServiceResult.BadRequest("El identificador de la matriz es obligatorio.");

        if (dto == null || string.IsNullOrWhiteSpace(dto.Motivo))
            return ServiceResult.BadRequest("El motivo de eliminación de la matriz es obligatorio.");

        try
        {
            // La eliminación lógica solo aplica a registros todavía operativos.
            // Una matriz aprobada, cerrada o inactiva ya forma parte del expediente auditable.
            var matriz = await _repo.ObtenerMatrizAsync(matrizId);
            if (matriz == null)
                return ServiceResult.NotFound("No se encontró la matriz de riesgos.");

            if (matriz.Estado is "APROBADA" or "CERRADA" or "INACTIVA")
                return ServiceResult.BadRequest("La matriz no puede eliminarse porque ya fue aprobada, cerrada o se encuentra inactiva.");

            var ok = await _repo.EliminarMatrizAsync(matrizId, dto.Motivo.Trim(), usuarioId, usuarioEmail, ip);
            return ok
                ? ServiceResult.Ok("Matriz eliminada correctamente.")
                : ServiceResult.NotFound("No se encontró la matriz de riesgos.");
        }
        catch (InvalidOperationException ex)
        {
            return ServiceResult.BadRequest(ex.Message);
        }
    }

    public async Task<ServiceResult<List<MatrizRiesgoHistorialDto>>> ObtenerHistorialAsync(long matrizId)
    {
        if (matrizId <= 0)
            return ServiceResult<List<MatrizRiesgoHistorialDto>>.BadRequest("El identificador de la matriz es obligatorio.");

        var historial = await _repo.ObtenerHistorialAsync(matrizId);
        return ServiceResult<List<MatrizRiesgoHistorialDto>>.Ok(historial);
    }

    public async Task<ServiceResult<List<MatrizRiesgoPlanAccionDto>>> ListarPlanesAsync(long matrizId)
    {
        if (matrizId <= 0)
            return ServiceResult<List<MatrizRiesgoPlanAccionDto>>.BadRequest("El identificador de la matriz es obligatorio.");

        var matriz = await _repo.ObtenerMatrizAsync(matrizId);
        if (matriz == null)
            return ServiceResult<List<MatrizRiesgoPlanAccionDto>>.NotFound("No se encontró la matriz de riesgos.");

        return ServiceResult<List<MatrizRiesgoPlanAccionDto>>.Ok(await _repo.ListarPlanesAsync(matrizId));
    }

    public async Task<ServiceResult<MatrizRiesgoPlanAccionDto>> CrearPlanAsync(long matrizId, MatrizRiesgoPlanAccionRequestDto dto, long usuarioId, string? usuarioEmail, string? ip)
    {
        var error = ValidarPlan(matrizId, dto);
        if (error != null)
            return ServiceResult<MatrizRiesgoPlanAccionDto>.BadRequest(error);

        try
        {
            var planId = await _repo.CrearPlanAsync(matrizId, dto, usuarioId, usuarioEmail, ip);
            var plan = (await _repo.ListarPlanesAsync(matrizId)).FirstOrDefault(x => x.PlanId == planId);
            return plan == null
                ? ServiceResult<MatrizRiesgoPlanAccionDto>.NotFound("El plan fue creado, pero no pudo consultarse.")
                : ServiceResult<MatrizRiesgoPlanAccionDto>.Ok(plan, "Plan de acción registrado correctamente.");
        }
        catch (InvalidOperationException ex)
        {
            return ServiceResult<MatrizRiesgoPlanAccionDto>.BadRequest(ex.Message);
        }
    }

    public async Task<ServiceResult<MatrizRiesgoPlanAccionDto>> ActualizarPlanAsync(long matrizId, long planId, MatrizRiesgoPlanAccionRequestDto dto, long usuarioId, string? usuarioEmail, string? ip)
    {
        if (planId <= 0)
            return ServiceResult<MatrizRiesgoPlanAccionDto>.BadRequest("El identificador del plan es obligatorio.");

        var error = ValidarPlan(matrizId, dto);
        if (error != null)
            return ServiceResult<MatrizRiesgoPlanAccionDto>.BadRequest(error);

        try
        {
            var ok = await _repo.ActualizarPlanAsync(matrizId, planId, dto, usuarioId, usuarioEmail, ip);
            if (!ok)
                return ServiceResult<MatrizRiesgoPlanAccionDto>.NotFound("No se encontró el plan de acción activo.");

            var plan = (await _repo.ListarPlanesAsync(matrizId)).FirstOrDefault(x => x.PlanId == planId);
            return plan == null
                ? ServiceResult<MatrizRiesgoPlanAccionDto>.NotFound("El plan fue actualizado, pero no pudo consultarse.")
                : ServiceResult<MatrizRiesgoPlanAccionDto>.Ok(plan, "Plan de acción actualizado correctamente.");
        }
        catch (InvalidOperationException ex)
        {
            return ServiceResult<MatrizRiesgoPlanAccionDto>.BadRequest(ex.Message);
        }
    }

    public async Task<ServiceResult> CambiarEstadoPlanAsync(long matrizId, long planId, MatrizRiesgoPlanEstadoRequestDto dto, long usuarioId, string? usuarioEmail, string? ip)
    {
        if (matrizId <= 0 || planId <= 0)
            return ServiceResult.BadRequest("La matriz y el plan son obligatorios.");

        var estado = dto?.Estado?.Trim().ToUpperInvariant() ?? string.Empty;
        if (!EstadosPlanPermitidos.Contains(estado) || estado == "INACTIVO")
            return ServiceResult.BadRequest("El estado del plan no es válido.");

        if (string.IsNullOrWhiteSpace(dto?.Motivo))
            return ServiceResult.BadRequest("El motivo del cambio de estado del plan es obligatorio.");

        try
        {
            var ok = await _repo.CambiarEstadoPlanAsync(matrizId, planId, estado, dto.Motivo.Trim(), usuarioId, usuarioEmail, ip);
            return ok ? ServiceResult.Ok("Estado del plan actualizado correctamente.") : ServiceResult.NotFound("No se encontró el plan de acción.");
        }
        catch (InvalidOperationException ex)
        {
            return ServiceResult.BadRequest(ex.Message);
        }
    }

    public async Task<ServiceResult> InactivarPlanAsync(long matrizId, long planId, MatrizRiesgoInactivarRequestDto dto, long usuarioId, string? usuarioEmail, string? ip)
    {
        if (matrizId <= 0 || planId <= 0)
            return ServiceResult.BadRequest("La matriz y el plan son obligatorios.");

        if (dto == null || string.IsNullOrWhiteSpace(dto.Motivo))
            return ServiceResult.BadRequest("El motivo de desactivación del plan es obligatorio.");

        var ok = await _repo.InactivarPlanAsync(matrizId, planId, dto.Motivo.Trim(), usuarioId, usuarioEmail, ip);
        return ok ? ServiceResult.Ok("Plan de acción desactivado correctamente.") : ServiceResult.NotFound("No se encontró el plan de acción activo.");
    }

    public async Task<ServiceResult> ReactivarPlanAsync(long matrizId, long planId, MatrizRiesgoInactivarRequestDto dto, long usuarioId, string? usuarioEmail, string? ip)
    {
        if (matrizId <= 0 || planId <= 0)
            return ServiceResult.BadRequest("La matriz y el plan son obligatorios.");

        if (dto == null || string.IsNullOrWhiteSpace(dto.Motivo))
            return ServiceResult.BadRequest("El motivo de reactivación del plan es obligatorio.");

        try
        {
            var ok = await _repo.ReactivarPlanAsync(matrizId, planId, dto.Motivo.Trim(), usuarioId, usuarioEmail, ip);
            return ok ? ServiceResult.Ok("Plan de acción reactivado correctamente.") : ServiceResult.NotFound("No se encontró el plan de acción inactivo.");
        }
        catch (InvalidOperationException ex)
        {
            return ServiceResult.BadRequest(ex.Message);
        }
    }

    public async Task<ServiceResult<List<MatrizRiesgoEvidenciaDto>>> ListarEvidenciasAsync(long matrizId)
    {
        if (matrizId <= 0)
            return ServiceResult<List<MatrizRiesgoEvidenciaDto>>.BadRequest("El identificador de la matriz es obligatorio.");

        var matriz = await _repo.ObtenerMatrizAsync(matrizId);
        if (matriz == null)
            return ServiceResult<List<MatrizRiesgoEvidenciaDto>>.NotFound("No se encontró la matriz de riesgos.");

        return ServiceResult<List<MatrizRiesgoEvidenciaDto>>.Ok(await _repo.ListarEvidenciasAsync(matrizId));
    }

    public async Task<ServiceResult<MatrizRiesgoEvidenciaDto>> CargarEvidenciaAsync(long matrizId, long? controlId, long? planId, IFormFile? archivo, long usuarioId, string? usuarioEmail, string? ip)
    {
        var error = ValidarArchivoEvidencia(matrizId, archivo);
        if (error != null)
            return ServiceResult<MatrizRiesgoEvidenciaDto>.BadRequest(error);

        var matriz = await _repo.ObtenerMatrizAsync(matrizId);
        if (matriz == null)
            return ServiceResult<MatrizRiesgoEvidenciaDto>.NotFound("No se encontró la matriz de riesgos.");

        var extension = Path.GetExtension(archivo!.FileName).ToLowerInvariant();
        var nombreFisico = $"{Guid.NewGuid():N}{extension}";
        var directorio = ObtenerDirectorioEvidenciasMatrices();
        var rutaFisica = Path.Combine(directorio, nombreFisico);
        var evidenciaRegistrada = false;

        try
        {
            Directory.CreateDirectory(directorio);
            await using (var stream = new FileStream(rutaFisica, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await archivo.CopyToAsync(stream);
            }

            var registro = new MatrizRiesgoEvidenciaRegistroDto
            {
                MatrizId = matrizId,
                ControlId = controlId,
                PlanId = planId,
                NombreOriginal = Path.GetFileName(archivo.FileName),
                NombreFisico = nombreFisico,
                TipoMime = archivo.ContentType,
                Extension = extension,
                TamanoBytes = archivo.Length,
                RutaFisica = rutaFisica,
                HashSha256 = await CalcularHashSha256Async(rutaFisica)
            };

            var evidenciaId = await _repo.RegistrarEvidenciaAsync(registro, usuarioId, usuarioEmail, ip);
            evidenciaRegistrada = true;
            var evidencia = await _repo.ObtenerEvidenciaAsync(matrizId, evidenciaId);
            return evidencia == null
                ? ServiceResult<MatrizRiesgoEvidenciaDto>.NotFound("La evidencia fue registrada, pero no pudo consultarse.")
                : ServiceResult<MatrizRiesgoEvidenciaDto>.Ok(evidencia, "Evidencia registrada correctamente.");
        }
        catch
        {
            if (!evidenciaRegistrada)
                EliminarArchivoSilenciosamente(rutaFisica);
            throw;
        }
    }

    public async Task<ServiceResult<MatrizRiesgoEvidenciaDescargaDto>> DescargarEvidenciaAsync(long matrizId, long evidenciaId, long usuarioId, string? usuarioEmail, string? ip)
    {
        var evidencia = await _repo.ObtenerEvidenciaAsync(matrizId, evidenciaId);
        if (evidencia == null || !evidencia.Activa)
            return ServiceResult<MatrizRiesgoEvidenciaDescargaDto>.NotFound("No se encontró la evidencia activa.");

        var rutaSegura = ObtenerRutaEvidenciaSegura(evidencia.RutaFisica);
        if (rutaSegura == null || !File.Exists(rutaSegura))
            return ServiceResult<MatrizRiesgoEvidenciaDescargaDto>.NotFound("El archivo físico no existe en el almacenamiento protegido.");

        var contenido = await File.ReadAllBytesAsync(rutaSegura);
        await _repo.RegistrarDescargaEvidenciaAsync(matrizId, evidenciaId, usuarioId, usuarioEmail, ip);

        return ServiceResult<MatrizRiesgoEvidenciaDescargaDto>.Ok(new MatrizRiesgoEvidenciaDescargaDto
        {
            NombreArchivo = evidencia.NombreOriginal,
            ContentType = string.IsNullOrWhiteSpace(evidencia.TipoMime) ? "application/octet-stream" : evidencia.TipoMime,
            Contenido = contenido
        });
    }

    public async Task<ServiceResult> InactivarEvidenciaAsync(long matrizId, long evidenciaId, MatrizRiesgoInactivarRequestDto dto, long usuarioId, string? usuarioEmail, string? ip)
    {
        if (matrizId <= 0 || evidenciaId <= 0)
            return ServiceResult.BadRequest("La matriz y la evidencia son obligatorias.");

        if (dto == null || string.IsNullOrWhiteSpace(dto.Motivo))
            return ServiceResult.BadRequest("El motivo de eliminación lógica de la evidencia es obligatorio.");

        var ok = await _repo.InactivarEvidenciaAsync(matrizId, evidenciaId, dto.Motivo.Trim(), usuarioId, usuarioEmail, ip);
        return ok ? ServiceResult.Ok("Evidencia eliminada correctamente.") : ServiceResult.NotFound("No se encontró la evidencia activa.");
    }

    public async Task<ServiceResult<List<MatrizRiesgoCriterioDto>>> ListarCriteriosAsync(bool incluirInactivos)
    {
        var criterios = await _repo.ListarCriteriosAsync(incluirInactivos);
        return ServiceResult<List<MatrizRiesgoCriterioDto>>.Ok(criterios);
    }

    public async Task<ServiceResult<MatrizRiesgoCriterioDto>> CrearCriterioAsync(MatrizRiesgoCriterioRequestDto dto, long usuarioId, string? usuarioEmail, string? ip)
    {
        var error = ValidarCriterio(dto);
        if (error != null)
            return ServiceResult<MatrizRiesgoCriterioDto>.BadRequest(error);

        try
        {
            var criterioId = await _repo.CrearCriterioAsync(dto, usuarioId, usuarioEmail, ip);
            var criterio = (await _repo.ListarCriteriosAsync(true)).FirstOrDefault(x => x.CriterioId == criterioId);
            return criterio == null
                ? ServiceResult<MatrizRiesgoCriterioDto>.NotFound("El criterio fue creado, pero no pudo consultarse.")
                : ServiceResult<MatrizRiesgoCriterioDto>.Ok(criterio, "Criterio registrado correctamente.");
        }
        catch (InvalidOperationException ex)
        {
            return ServiceResult<MatrizRiesgoCriterioDto>.BadRequest(ex.Message);
        }
    }

    public async Task<ServiceResult<MatrizRiesgoCriterioDto>> ActualizarCriterioAsync(long criterioId, MatrizRiesgoCriterioRequestDto dto, long usuarioId, string? usuarioEmail, string? ip)
    {
        if (criterioId <= 0)
            return ServiceResult<MatrizRiesgoCriterioDto>.BadRequest("El identificador del criterio es obligatorio.");

        var error = ValidarCriterio(dto);
        if (error != null)
            return ServiceResult<MatrizRiesgoCriterioDto>.BadRequest(error);

        try
        {
            var ok = await _repo.ActualizarCriterioAsync(criterioId, dto, usuarioId, usuarioEmail, ip);
            if (!ok)
                return ServiceResult<MatrizRiesgoCriterioDto>.NotFound("No se encontró el criterio activo.");

            var criterio = (await _repo.ListarCriteriosAsync(true)).FirstOrDefault(x => x.CriterioId == criterioId);
            return criterio == null
                ? ServiceResult<MatrizRiesgoCriterioDto>.NotFound("El criterio fue actualizado, pero no pudo consultarse.")
                : ServiceResult<MatrizRiesgoCriterioDto>.Ok(criterio, "Criterio actualizado correctamente.");
        }
        catch (InvalidOperationException ex)
        {
            return ServiceResult<MatrizRiesgoCriterioDto>.BadRequest(ex.Message);
        }
    }

    public async Task<ServiceResult> InactivarCriterioAsync(long criterioId, MatrizRiesgoInactivarRequestDto dto, long usuarioId, string? usuarioEmail, string? ip)
    {
        if (criterioId <= 0)
            return ServiceResult.BadRequest("El identificador del criterio es obligatorio.");

        if (dto == null || string.IsNullOrWhiteSpace(dto.Motivo))
            return ServiceResult.BadRequest("El motivo de desactivación del criterio es obligatorio.");

        var ok = await _repo.InactivarCriterioAsync(criterioId, dto.Motivo.Trim(), usuarioId, usuarioEmail, ip);
        return ok
            ? ServiceResult.Ok("Criterio desactivado correctamente.")
            : ServiceResult.NotFound("No se encontró el criterio activo.");
    }


    public async Task<ServiceResult> ReactivarCriterioAsync(long criterioId, MatrizRiesgoInactivarRequestDto dto, long usuarioId, string? usuarioEmail, string? ip)
    {
        if (criterioId <= 0)
            return ServiceResult.BadRequest("El identificador del criterio es obligatorio.");

        if (dto == null || string.IsNullOrWhiteSpace(dto.Motivo))
            return ServiceResult.BadRequest("El motivo de reactivación del criterio es obligatorio.");

        try
        {
            var ok = await _repo.ReactivarCriterioAsync(criterioId, dto.Motivo.Trim(), usuarioId, usuarioEmail, ip);
            return ok
                ? ServiceResult.Ok("Criterio activado correctamente.")
                : ServiceResult.NotFound("No se encontró el criterio inactivo.");
        }
        catch (InvalidOperationException ex)
        {
            return ServiceResult.BadRequest(ex.Message);
        }
    }

    public async Task<ServiceResult> EliminarCriterioAsync(long criterioId, MatrizRiesgoInactivarRequestDto dto, long usuarioId, string? usuarioEmail, string? ip)
    {
        if (criterioId <= 0)
            return ServiceResult.BadRequest("El identificador del criterio es obligatorio.");

        if (dto == null || string.IsNullOrWhiteSpace(dto.Motivo))
            return ServiceResult.BadRequest("El motivo de eliminación del criterio es obligatorio.");

        try
        {
            if (await _repo.CriterioTieneUsoHistoricoAsync(criterioId))
                return ServiceResult.BadRequest("El criterio está relacionado con evaluaciones históricas y no puede eliminarse físicamente. Desactívelo para conservar la trazabilidad.");

            var ok = await _repo.EliminarCriterioAsync(criterioId, dto.Motivo.Trim(), usuarioId, usuarioEmail, ip);
            return ok
                ? ServiceResult.Ok("Criterio eliminado correctamente.")
                : ServiceResult.NotFound("No se encontró el criterio.");
        }
        catch (InvalidOperationException ex)
        {
            return ServiceResult.BadRequest(ex.Message);
        }
        catch (Oracle.ManagedDataAccess.Client.OracleException ex) when (ex.Number == 2292)
        {
            return ServiceResult.BadRequest("El criterio ya está relacionado con información histórica y no puede eliminarse físicamente. Puede desactivarlo para conservar la trazabilidad.");
        }
    }

    private static string? ValidarPlan(long matrizId, MatrizRiesgoPlanAccionRequestDto dto)
    {
        if (matrizId <= 0)
            return "El identificador de la matriz es obligatorio.";

        if (dto == null)
            return "La solicitud del plan de acción es obligatoria.";

        dto.Actividad = dto.Actividad?.Trim() ?? string.Empty;
        dto.Responsable = dto.Responsable?.Trim() ?? string.Empty;
        dto.Periodicidad = dto.Periodicidad?.Trim();
        dto.MedioPrueba = dto.MedioPrueba?.Trim();
        dto.Observaciones = dto.Observaciones?.Trim();

        if (string.IsNullOrWhiteSpace(dto.Actividad))
            return "La actividad del plan de acción es obligatoria.";

        if (string.IsNullOrWhiteSpace(dto.Responsable))
            return "El responsable del plan de acción es obligatorio.";

        if (dto.Actividad.Length > 1500)
            return "La actividad del plan no debe superar los 1500 caracteres.";

        if (dto.Responsable.Length > 300)
            return "El responsable del plan no debe superar los 300 caracteres.";

        if (dto.Periodicidad?.Length > 80 || dto.MedioPrueba?.Length > 300 || dto.Observaciones?.Length > 1500)
            return "La periodicidad, el medio de prueba o las observaciones superan la longitud permitida.";

        if (dto.FechaInicio.HasValue && dto.FechaInicio.Value.Date < DateTime.Today)
            return "La fecha de inicio no puede ser menor a la fecha actual.";

        if (dto.FechaFin.HasValue && dto.FechaFin.Value.Date < DateTime.Today)
            return "La fecha final no puede ser menor a la fecha actual.";

        if (dto.FechaInicio.HasValue && dto.FechaFin.HasValue && dto.FechaFin.Value.Date < dto.FechaInicio.Value.Date)
            return "La fecha de finalización no puede ser menor que la fecha de inicio.";

        return null;
    }

    private string? ValidarArchivoEvidencia(long matrizId, IFormFile? archivo)
    {
        if (matrizId <= 0)
            return "El identificador de la matriz es obligatorio.";

        if (archivo == null || archivo.Length == 0)
            return "El archivo de evidencia es obligatorio.";

        var maxMb = _configuration?.GetValue<int?>("Evidencias:MaxFileSizeMb") ?? 10;
        if (maxMb <= 0)
            maxMb = 10;
        var maxBytes = maxMb * 1024L * 1024L;
        if (archivo.Length > maxBytes)
            return $"El archivo supera el tamaño máximo permitido de {maxMb} MB.";

        var nombreOriginal = Path.GetFileName(archivo.FileName);
        if (string.IsNullOrWhiteSpace(nombreOriginal) || nombreOriginal.Length > 255 || nombreOriginal.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            return "El nombre del archivo de evidencia no es válido.";

        var extension = Path.GetExtension(nombreOriginal).ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(extension) || !ExtensionesEvidenciaPermitidas.Contains(extension))
            return "La extensión del archivo no está permitida para evidencias.";

        var tiposPermitidos = _configuration?
            .GetSection($"Evidencias:AllowedMimeTypes:{extension.ToLowerInvariant()}")
            .Get<string[]>();

        if (tiposPermitidos is not { Length: > 0 })
            tiposPermitidos = MimeTypesEvidenciaPermitidos[extension];

        if (string.IsNullOrWhiteSpace(archivo.ContentType) || !tiposPermitidos.Contains(archivo.ContentType.Trim(), StringComparer.OrdinalIgnoreCase))
            return "El tipo MIME del archivo no coincide con la extensión permitida.";

        if ((_configuration?.GetValue<bool?>("Evidencias:ValidateFileSignature") ?? true) && !TieneFirmaEvidenciaPermitida(archivo, extension))
            return "El contenido del archivo no coincide con la firma real de su extensión.";

        return null;
    }

    private string ObtenerDirectorioEvidenciasMatrices()
    {
        var rutaEspecifica = _configuration?["MatricesRiesgos:Evidencias:StoragePath"];
        if (!string.IsNullOrWhiteSpace(rutaEspecifica))
            return ResolverRutaAlmacenamiento(rutaEspecifica);

        var rutaBase = _configuration?["Evidencias:StoragePath"] ?? "App_Data/Evidencias";
        return ResolverRutaAlmacenamiento(Path.Combine(rutaBase, "MatricesRiesgos"));
    }

    private string ResolverRutaAlmacenamiento(string ruta)
    {
        if (Path.IsPathRooted(ruta))
            return Path.GetFullPath(ruta);
        var raiz = _environment?.ContentRootPath ?? AppContext.BaseDirectory;
        return Path.GetFullPath(Path.Combine(raiz, ruta));
    }

    private string? ObtenerRutaEvidenciaSegura(string rutaRegistrada)
    {
        if (string.IsNullOrWhiteSpace(rutaRegistrada))
            return null;

        try
        {
            var directorio = Path.GetFullPath(ObtenerDirectorioEvidenciasMatrices());
            var candidata = Path.GetFullPath(rutaRegistrada);
            var prefijo = directorio.EndsWith(Path.DirectorySeparatorChar) ? directorio : directorio + Path.DirectorySeparatorChar;
            return candidata.StartsWith(prefijo, StringComparison.OrdinalIgnoreCase) ? candidata : null;
        }
        catch
        {
            return null;
        }
    }

    private static bool TieneFirmaEvidenciaPermitida(IFormFile archivo, string extension)
    {
        if (!FirmasEvidenciaPermitidas.TryGetValue(extension, out var firmas))
            return false;

        Span<byte> buffer = stackalloc byte[8];
        using var stream = archivo.OpenReadStream();
        var leidos = stream.Read(buffer);
        foreach (var firma in firmas)
        {
            if (leidos >= firma.Length && buffer[..firma.Length].SequenceEqual(firma))
                return true;
        }

        return false;
    }

    private static void EliminarArchivoSilenciosamente(string rutaFisica)
    {
        try
        {
            if (File.Exists(rutaFisica))
                File.Delete(rutaFisica);
        }
        catch
        {
            // La excepción original conserva prioridad; el archivo queda sujeto a limpieza operativa.
        }
    }

    private static async Task<string> CalcularHashSha256Async(string rutaFisica)
    {
        await using var stream = File.OpenRead(rutaFisica);
        var hash = await SHA256.HashDataAsync(stream);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string? ValidarCreacion(MatrizRiesgoCrearRequestDto dto)
    {
        if (dto == null)
            return "La solicitud de creación de matriz es obligatoria.";

        dto.SujetoTipo = dto.SujetoTipo?.Trim().ToUpperInvariant() ?? string.Empty;
        dto.NombreSujeto = dto.NombreSujeto?.Trim() ?? string.Empty;
        dto.OrigenDatos = string.IsNullOrWhiteSpace(dto.OrigenDatos) ? "CAPTURA" : dto.OrigenDatos.Trim().ToUpperInvariant();

        if (!SujetosPermitidos.Contains(dto.SujetoTipo))
            return "El tipo de sujeto evaluado no es válido.";

        if (string.IsNullOrWhiteSpace(dto.NombreSujeto))
            return "El nombre del sujeto evaluado es obligatorio.";

        if (dto.Detalles == null || dto.Detalles.Count == 0)
            return "Debe registrar al menos un detalle de variable para evaluar la matriz.";

        dto.Controles ??= new List<MatrizRiesgoControlRequestDto>();

        foreach (var detalle in dto.Detalles)
        {
            if (detalle.VariableId <= 0)
                return "Cada detalle debe indicar una variable válida.";

            if (detalle.Puntaje < 0)
                return "El puntaje de una variable no puede ser negativo.";
        }

        if (dto.Controles != null)
        {
            foreach (var control in dto.Controles)
            {
                if (string.IsNullOrWhiteSpace(control.Nombre))
                    return "Cada control debe tener nombre.";

                control.Nombre = control.Nombre.Trim();
                control.Descripcion = control.Descripcion?.Trim();
                control.Responsable = control.Responsable?.Trim();

                if (control.Descripcion?.Length > 1500)
                    return "La descripción del control no debe superar los 1500 caracteres.";

                if (control.Responsable?.Length > 300)
                    return "El responsable del control no debe superar los 300 caracteres.";

                if (control.EfectividadPct < 0 || control.EfectividadPct > 100)
                    return "La efectividad del control debe estar entre 0% y 100%.";
            }
        }

        return null;
    }

    private static string? NormalizarFiltroReporte(MatrizRiesgoReporteFiltroDto filtro)
    {
        if (filtro == null)
            return null;

        filtro.Buscar = filtro.Buscar?.Trim();
        filtro.Estado = filtro.Estado?.Trim().ToUpperInvariant();
        filtro.SujetoTipo = filtro.SujetoTipo?.Trim().ToUpperInvariant();
        filtro.NivelInherente = filtro.NivelInherente?.Trim();
        filtro.NivelResidual = filtro.NivelResidual?.Trim();
        filtro.ModeloVersion = filtro.ModeloVersion?.Trim();
        filtro.Responsable = filtro.Responsable?.Trim();

        var fechaMinima = new DateTime(2000, 1, 1);
        if (filtro.FechaInicio.HasValue && filtro.FechaInicio.Value.Date < fechaMinima)
            return "La fecha de inicio del reporte no puede ser menor al 01/01/2000.";

        if (filtro.FechaFin.HasValue && filtro.FechaFin.Value.Date < fechaMinima)
            return "La fecha final del reporte no puede ser menor al 01/01/2000.";

        if (filtro.FechaInicio.HasValue && filtro.FechaInicio.Value.Date > DateTime.Today)
            return "La fecha de inicio del reporte no puede ser mayor a la fecha actual.";

        if (filtro.FechaFin.HasValue && filtro.FechaFin.Value.Date > DateTime.Today)
            return "La fecha final del reporte no puede ser mayor a la fecha actual.";

        if (filtro.FechaInicio.HasValue && filtro.FechaFin.HasValue && filtro.FechaFin.Value.Date < filtro.FechaInicio.Value.Date)
            return "La fecha final del reporte no puede ser menor que la fecha de inicio.";

        return null;
    }

    private static MatrizRiesgoExportacionDto ConstruirExcelReporte(MatricesRiesgoReporteDto reporte)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<html><head><meta charset=\"utf-8\" />");
        sb.AppendLine("<style>");
        sb.AppendLine("body{font-family:Arial,sans-serif;color:#111827} h1{font-size:20px;margin:0;color:#16337a} h2{font-size:15px;margin:18px 0 8px;color:#111827} p{margin:4px 0;font-size:12px} table{border-collapse:collapse;margin:8px 0 18px;width:100%} th{background:#1f3f91;color:#fff;font-weight:bold;text-align:left} th,td{border:1px solid #cbd5e1;padding:7px;font-size:12px;vertical-align:top} .meta td:first-child,.totales td:first-child{font-weight:bold;background:#f8fafc}.num{text-align:right}.alto{color:#c2410c;font-weight:bold}.critico{color:#b91c1c;font-weight:bold}.ok{color:#047857;font-weight:bold}.muted{color:#64748b}</style>");
        sb.AppendLine("</head><body>");
        sb.AppendLine("<h1>SGRLA-IHSS - Reporte de Matrices de Riesgos</h1>");
        sb.AppendLine("<p class=\"muted\">Instituto Hondureño de Seguridad Social</p>");
        sb.AppendLine($"<p>Fecha de generación: {Html(reporte.FechaGeneracion.ToString("dd/MM/yyyy HH:mm"))}</p>");

        AgregarTablaHtml(sb, "Filtros aplicados", new[] { "Filtro", "Valor" }, ConstruirResumenFiltros(reporte.Filtro), "meta");
        AgregarTablaHtml(sb, "Resumen ejecutivo", new[] { "Indicador", "Valor" }, new[]
        {
            new[] { "Total matrices", reporte.Totales.TotalMatrices.ToString() },
            new[] { "Calculadas", reporte.Totales.TotalCalculadas.ToString() },
            new[] { "Sin evaluar", reporte.Totales.TotalSinCalculo.ToString() },
            new[] { "Cerradas", reporte.Totales.TotalCerradas.ToString() },
            new[] { "Alto / Crítico", reporte.Totales.TotalAltoCritico.ToString() },
            new[] { "Plan requerido", reporte.Totales.TotalPlanAccionRequerido.ToString() },
            new[] { "Planes vencidos", reporte.Totales.TotalPlanesVencidos.ToString() }
        }, "totales");

        AgregarTablaHtml(sb, "Distribución por estado", new[] { "Estado", "Total" }, reporte.PorEstado.Select(x => new[] { x.Nombre, x.Total.ToString() }));
        AgregarTablaHtml(sb, "Distribución por nivel residual", new[] { "Nivel", "Total" }, reporte.PorNivelResidual.Select(x => new[] { x.Nombre, x.Total.ToString() }));
        AgregarTablaHtml(sb, "Distribución por sujeto", new[] { "Sujeto", "Total" }, reporte.PorSujetoTipo.Select(x => new[] { x.Nombre, x.Total.ToString() }));
        AgregarTablaHtml(sb, "Mapa inherente persistido", new[] { "Nivel", "Total", "Promedio" },
            reporte.MapaInherente.Select(x => new[] { x.Nivel, x.Total.ToString(), x.Promedio.ToString("0.0000") }));
        AgregarTablaHtml(sb, "Mapa residual persistido", new[] { "Nivel", "Total", "Promedio" },
            reporte.MapaResidual.Select(x => new[] { x.Nivel, x.Total.ToString(), x.Promedio.ToString("0.0000") }));
        AgregarTablaHtml(sb, "Mapa de transición inherente a residual", new[] { "Nivel inherente", "Nivel residual", "Total", "Promedio inherente", "Promedio residual" },
            reporte.MapaTransicion.Select(x => new[] { x.NivelInherente, x.NivelResidual, x.Total.ToString(), x.PromedioInherente.ToString("0.0000"), x.PromedioResidual.ToString("0.0000") }));
        AgregarTablaHtml(sb, "Matrices filtradas", new[] { "ID", "Sujeto", "Documento", "Tipo", "Estado", "Inherente", "Residual", "Plan", "Fecha" },
            reporte.MatricesFiltradas.Select(x => new[]
            {
                x.MatrizId.ToString(),
                x.NombreSujeto ?? string.Empty,
                x.Documento ?? string.Empty,
                x.SujetoTipo ?? string.Empty,
                x.Estado ?? string.Empty,
                $"{x.PuntajeInherente:0.00} {x.NivelInherente}".Trim(),
                $"{x.PuntajeResidual:0.00} {x.NivelResidual}".Trim(),
                x.RequierePlanAccion ? "Sí" : "No",
                x.FechaEvaluacion.ToString("dd/MM/yyyy")
            }));
        AgregarTablaHtml(sb, "Resultados por factor", new[] { "Factor", "Matrices", "Promedio inherente", "Promedio residual", "Alto/Crítico", "Plan requerido" },
            reporte.PorFactor.Select(x => new[]
            {
                $"{x.FactorCodigo} - {x.FactorNombre}",
                x.TotalMatrices.ToString(),
                x.PromedioInherente.ToString("0.0000"),
                x.PromedioResidual.ToString("0.0000"),
                x.TotalAltoCritico.ToString(),
                x.TotalPlanAccionRequerido.ToString()
            }));
        AgregarTablaHtml(sb, "Matrices Alto / Crítico", new[] { "ID", "Sujeto", "Documento", "Tipo", "Estado", "Inherente", "Residual", "Plan", "Fecha" },
            reporte.MatricesCriticas.Select(x => new[]
            {
                x.MatrizId.ToString(),
                x.NombreSujeto ?? string.Empty,
                x.Documento ?? string.Empty,
                x.SujetoTipo ?? string.Empty,
                x.Estado ?? string.Empty,
                $"{x.PuntajeInherente:0.00} {x.NivelInherente}",
                $"{x.PuntajeResidual:0.00} {x.NivelResidual}",
                x.RequierePlanAccion ? "Sí" : "No",
                x.FechaEvaluacion.ToString("dd/MM/yyyy")
            }));
        AgregarTablaHtml(sb, "Planes de acción", new[] { "Estado", "Total", "Vencidos" },
            reporte.PlanesAccion.Select(x => new[] { x.Estado, x.Total.ToString(), x.Vencidos.ToString() }));

        sb.AppendLine("</body></html>");
        return new MatrizRiesgoExportacionDto
        {
            NombreArchivo = $"Reporte_Matrices_Riesgos_{DateTime.Now:yyyyMMdd_HHmmss}.xls",
            ContentType = "application/vnd.ms-excel",
            Contenido = Encoding.UTF8.GetBytes(sb.ToString())
        };
    }

    private static MatrizRiesgoExportacionDto ConstruirPdfReporte(MatricesRiesgoReporteDto reporte)
    {
        var lineas = new List<string>
        {
            "INSTITUTO HONDUREÑO DE SEGURIDAD SOCIAL",
            "REPORTE DE MATRICES DE RIESGOS",
            $"SGRLA-IHSS | Fecha de Generación: {reporte.FechaGeneracion:dd/MM/yyyy HH:mm}",
            "",
            "1. FILTROS APLICADOS",
        };

        lineas.AddRange(ConstruirResumenFiltros(reporte.Filtro).Select(x => $" - {x[0]}: {x[1]}"));
        lineas.AddRange(new[]
        {
            "",
            "2. RESUMEN EJECUTIVO",
            $" - Total matrices: {reporte.Totales.TotalMatrices}",
            $" - Calculadas: {reporte.Totales.TotalCalculadas}",
            $" - Sin evaluar: {reporte.Totales.TotalSinCalculo}",
            $" - Cerradas: {reporte.Totales.TotalCerradas}",
            $" - Alto / Crítico: {reporte.Totales.TotalAltoCritico}",
            $" - Plan requerido: {reporte.Totales.TotalPlanAccionRequerido}",
            $" - Planes vencidos: {reporte.Totales.TotalPlanesVencidos}",
            "",
            "3. DISTRIBUCIÓN POR ESTADO"
        });

        lineas.AddRange(reporte.PorEstado.Select(x => $" - {QuitarSaltos(x.Nombre)}: {x.Total}"));
        lineas.Add("");
        lineas.Add("4. DISTRIBUCIÓN POR NIVEL RESIDUAL");
        lineas.AddRange(reporte.PorNivelResidual.Select(x => $" - {QuitarSaltos(x.Nombre)}: {x.Total}"));
        lineas.Add("");
        lineas.Add("5. MAPA INHERENTE PERSISTIDO");
        lineas.AddRange(reporte.MapaInherente.Select(x => $" - {QuitarSaltos(x.Nivel)} | Total {x.Total} | Promedio {x.Promedio:0.0000}"));
        lineas.Add("");
        lineas.Add("6. MAPA RESIDUAL PERSISTIDO");
        lineas.AddRange(reporte.MapaResidual.Select(x => $" - {QuitarSaltos(x.Nivel)} | Total {x.Total} | Promedio {x.Promedio:0.0000}"));
        lineas.Add("");
        lineas.Add("7. MAPA DE TRANSICIÓN INHERENTE A RESIDUAL");
        lineas.AddRange(reporte.MapaTransicion.Select(x => $" - {QuitarSaltos(x.NivelInherente)} -> {QuitarSaltos(x.NivelResidual)} | Total {x.Total} | Promedio inherente {x.PromedioInherente:0.0000} | Promedio residual {x.PromedioResidual:0.0000}"));
        lineas.Add("");
        lineas.Add("8. MATRICES FILTRADAS");
        lineas.AddRange(reporte.MatricesFiltradas.Select(x => $" - {x.MatrizId} | {QuitarSaltos(x.NombreSujeto)} | {QuitarSaltos(x.Estado)} | Residual {FormatoResultado(x.PuntajeResidual, x.NivelResidual)} | Plan {(x.RequierePlanAccion ? "Sí" : "No")} | Fecha {x.FechaEvaluacion:dd/MM/yyyy}"));
        lineas.Add("");
        lineas.Add("9. RESULTADOS POR FACTOR");
        lineas.AddRange(reporte.PorFactor.Select(x => $" - {QuitarSaltos(x.FactorCodigo)} {QuitarSaltos(x.FactorNombre)} | Matrices {x.TotalMatrices} | Residual {x.PromedioResidual:0.0000}"));
        lineas.Add("");
        lineas.Add("10. MATRICES ALTO / CRÍTICO");
        lineas.AddRange(reporte.MatricesCriticas.Select(x => $" - {x.MatrizId} | {QuitarSaltos(x.NombreSujeto)} | {QuitarSaltos(x.Estado)} | Residual {x.PuntajeResidual:0.00} {QuitarSaltos(x.NivelResidual)} | Plan {(x.RequierePlanAccion ? "Sí" : "No")}"));
        lineas.Add("");
        lineas.Add("11. PLANES DE ACCIÓN");
        lineas.AddRange(reporte.PlanesAccion.Select(x => $" - {QuitarSaltos(x.Estado)} | Total {x.Total} | Vencidos {x.Vencidos}"));

        return new MatrizRiesgoExportacionDto
        {
            NombreArchivo = $"Reporte_Matrices_Riesgos_{DateTime.Now:yyyyMMdd_HHmmss}.pdf",
            ContentType = "application/pdf",
            Contenido = CrearPdfSimple(lineas)
        };
    }

    private static void AgregarTablaHtml(StringBuilder sb, string titulo, string[] encabezados, IEnumerable<string[]> filas, string? clase = null)
    {
        var filasMaterializadas = filas.ToList();
        sb.AppendLine($"<h2>{Html(titulo)}</h2>");
        sb.AppendLine($"<table{(string.IsNullOrWhiteSpace(clase) ? string.Empty : $" class=\"{clase}\"")}>");
        sb.AppendLine("<tr>");
        foreach (var encabezado in encabezados)
            sb.AppendLine($"<th>{Html(encabezado)}</th>");
        sb.AppendLine("</tr>");
        if (filasMaterializadas.Count == 0)
        {
            sb.AppendLine($"<tr><td colspan=\"{encabezados.Length}\" class=\"muted\">Sin registros para mostrar.</td></tr>");
        }

        foreach (var fila in filasMaterializadas)
        {
            sb.AppendLine("<tr>");
            foreach (var celda in fila)
                sb.AppendLine($"<td>{Html(celda)}</td>");
            sb.AppendLine("</tr>");
        }
        sb.AppendLine("</table>");
    }

    private static IEnumerable<string[]> ConstruirResumenFiltros(MatrizRiesgoReporteFiltroDto filtro)
    {
        var filtros = new[]
        {
            new[] { "Búsqueda general", ValorFiltro(filtro.Buscar) },
            new[] { "Estado", ValorFiltro(filtro.Estado) },
            new[] { "Tipo de sujeto", ValorFiltro(filtro.SujetoTipo) },
            new[] { "Nivel inherente", ValorFiltro(filtro.NivelInherente) },
            new[] { "Nivel residual", ValorFiltro(filtro.NivelResidual) },
            new[] { "Versión metodología", ValorFiltro(filtro.ModeloVersion) },
            new[] { "Responsable", ValorFiltro(filtro.Responsable) },
            new[] { "Fecha inicio", filtro.FechaInicio?.ToString("dd/MM/yyyy") ?? "Todos" },
            new[] { "Fecha fin", filtro.FechaFin?.ToString("dd/MM/yyyy") ?? "Todos" }
        };
        return filtros;
    }

    private static string ValorFiltro(string? valor) => string.IsNullOrWhiteSpace(valor) ? "Todos" : valor.Trim();

    private static string Html(string? valor) => WebUtility.HtmlEncode(valor ?? string.Empty);

    private static string FormatoResultado(decimal? puntaje, string? nivel)
    {
        return puntaje.HasValue
            ? $"{puntaje.Value:0.00} {QuitarSaltos(nivel)}".Trim()
            : "-";
    }

    private static byte[] CrearPdfSimple(IEnumerable<string> lineas)
    {
        var paginas = lineas
            .SelectMany(linea => DividirLinea(QuitarSaltos(linea), 95))
            .Chunk(48)
            .ToList();

        if (paginas.Count == 0)
            paginas.Add(Array.Empty<string>());

        var objetos = new List<string>
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [] /Count 0 >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Courier /Encoding /WinAnsiEncoding >>"
        };

        var pageObjectIds = new List<int>();
        const int fontObjectId = 3;
        foreach (var pagina in paginas)
        {
            var pageId = objetos.Count + 1;
            var contentId = objetos.Count + 2;
            pageObjectIds.Add(pageId);
            objetos.Add($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 {fontObjectId} 0 R >> >> /Contents {contentId} 0 R >>");
            var contenido = ConstruirContenidoPdf(pagina);
            objetos.Add($"<< /Length {Encoding.Latin1.GetByteCount(contenido)} >>\nstream\n{contenido}\nendstream");
        }

        objetos[1] = $"<< /Type /Pages /Kids [{string.Join(" ", pageObjectIds.Select(id => $"{id} 0 R"))}] /Count {pageObjectIds.Count} >>";

        using var ms = new MemoryStream();
        var offsets = new List<long> { 0 };
        EscribirPdf(ms, "%PDF-1.4\n");
        for (var i = 0; i < objetos.Count; i++)
        {
            offsets.Add(ms.Position);
            EscribirPdf(ms, $"{i + 1} 0 obj\n{objetos[i]}\nendobj\n");
        }

        var xrefOffset = ms.Position;
        EscribirPdf(ms, $"xref\n0 {objetos.Count + 1}\n0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1))
            EscribirPdf(ms, $"{offset:0000000000} 00000 n \n");
        EscribirPdf(ms, $"trailer\n<< /Size {objetos.Count + 1} /Root 1 0 R >>\nstartxref\n{xrefOffset}\n%%EOF");
        return ms.ToArray();
    }

    private static string ConstruirContenidoPdf(IEnumerable<string> lineas)
    {
        var sb = new StringBuilder();
        sb.AppendLine("BT");
        sb.AppendLine("/F1 9 Tf");
        sb.AppendLine("45 750 Td");
        sb.AppendLine("12 TL");
        foreach (var linea in lineas)
            sb.AppendLine($"({EscaparPdf(linea)}) Tj T*");
        sb.AppendLine("ET");
        return sb.ToString();
    }

    private static IEnumerable<string> DividirLinea(string linea, int maximo)
    {
        if (linea.Length <= maximo)
        {
            yield return linea;
            yield break;
        }

        for (var i = 0; i < linea.Length; i += maximo)
            yield return linea.Substring(i, Math.Min(maximo, linea.Length - i));
    }

    private static string EscaparPdf(string texto)
    {
        return texto.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
    }

    private static string QuitarSaltos(string? texto)
    {
        return string.IsNullOrWhiteSpace(texto)
            ? string.Empty
            : texto.Replace("\r", " ").Replace("\n", " ").Trim();
    }

    private static void EscribirPdf(Stream stream, string texto)
    {
        var bytes = Encoding.Latin1.GetBytes(texto);
        stream.Write(bytes, 0, bytes.Length);
    }

    private static string? ValidarCriterio(MatrizRiesgoCriterioRequestDto dto)
    {
        if (dto == null)
            return "La solicitud del criterio es obligatoria.";

        if (dto.VariableId <= 0)
            return "La variable asociada al criterio es obligatoria.";

        if (dto.EscalaId.HasValue && dto.EscalaId.Value <= 0)
            return "La escala asociada al criterio no es válida.";

        if (string.IsNullOrWhiteSpace(dto.Descripcion))
            return "La descripción del criterio es obligatoria.";

        if (dto.Descripcion.Trim().Length > 1500)
            return "La descripción del criterio no puede superar 1500 caracteres.";

        if (dto.Puntaje < 0)
            return "El puntaje del criterio no puede ser negativo.";

        if (dto.ValorDesde.HasValue && dto.ValorHasta.HasValue && dto.ValorDesde.Value > dto.ValorHasta.Value)
            return "El valor inicial del rango no puede ser mayor que el valor final.";

        dto.Descripcion = dto.Descripcion.Trim();
        return null;
    }
}
