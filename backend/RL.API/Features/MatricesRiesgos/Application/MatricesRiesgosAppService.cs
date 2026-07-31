using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using RL.API.Features.MatricesRiesgos.Contracts;
using RL.API.Features.MatricesRiesgos.Domain;
using RL.API.Features.MatricesRiesgos.Persistence;
using RL.API.Shared.Results;

namespace RL.API.Features.MatricesRiesgos.Application;

public sealed class MatricesRiesgosAppService : IMatricesRiesgosAppService
{
    private readonly IMatricesRiesgosRepository _repo;
    private readonly IFormularioValidador _validador;
    private readonly IMatricesRiesgoService _calculador;
    private readonly RL.API.Features.Auditoria.Persistence.IAuditoriaRepository _auditoriaRepo;

    public MatricesRiesgosAppService(
        IMatricesRiesgosRepository repo, 
        IFormularioValidador validador, 
        IMatricesRiesgoService calculador,
        RL.API.Features.Auditoria.Persistence.IAuditoriaRepository auditoriaRepo)
    {
        _repo = repo;
        _validador = validador;
        _calculador = calculador;
        _auditoriaRepo = auditoriaRepo;
    }

    // ============================================================
    // 1. GESTIÓN DEL CICLO DE VIDA DEL FORMULARIO Y VERSIONES
    // ============================================================

    public async Task<ServiceResult<VersionFormularioDto>> ObtenerVersionVigenteFormularioAsync(string familiaCodigo)
    {
        var version = await _repo.ObtenerVersionVigenteFormularioAsync(familiaCodigo);
        if (version == null)
        {
            return ServiceResult<VersionFormularioDto>.NotFound($"No existe una versión de formulario publicada y vigente para la familia '{familiaCodigo}'.");
        }
        return ServiceResult<VersionFormularioDto>.Ok(version);
    }

    public async Task<ServiceResult<VersionFormularioDto>> ObtenerVersionFormularioAsync(long versionId)
    {
        var version = await _repo.ObtenerVersionFormularioAsync(versionId);
        if (version == null)
        {
            return ServiceResult<VersionFormularioDto>.NotFound($"No se encontró la versión de formulario con ID {versionId}.");
        }
        return ServiceResult<VersionFormularioDto>.Ok(version);
    }

    public async Task<ServiceResult<long>> CrearBorradorFormularioAsync(long familiaId, string codigoFormulario, string jsonConfig, long usuarioId)
    {
        if (string.IsNullOrWhiteSpace(jsonConfig))
        {
            return ServiceResult<long>.BadRequest("El contenido JSON de configuración es obligatorio.");
        }

        try
        {
            // Validar que sea un JSON sintácticamente válido
            JsonDocument.Parse(jsonConfig);
        }
        catch (JsonException ex)
        {
            return ServiceResult<long>.BadRequest($"El formato del JSON de configuración es inválido: {ex.Message}");
        }

        long nuevoId = await _repo.CrearBorradorFormularioAsync(familiaId, codigoFormulario, jsonConfig, usuarioId);
        return ServiceResult<long>.Ok(nuevoId, "Borrador de formulario creado exitosamente.");
    }

    public async Task<ServiceResult<long>> ClonarVersionFormularioAsync(long versionOrigenId, long usuarioId)
    {
        try
        {
            long nuevoId = await _repo.ClonarVersionFormularioAsync(versionOrigenId, usuarioId);
            return ServiceResult<long>.Ok(nuevoId, "Versión de formulario clonada como borrador exitosamente.");
        }
        catch (KeyNotFoundException ex)
        {
            return ServiceResult<long>.NotFound(ex.Message);
        }
    }

    public async Task<ServiceResult> ActualizarBorradorFormularioAsync(long versionId, string jsonConfig, long usuarioId)
    {
        if (string.IsNullOrWhiteSpace(jsonConfig))
        {
            return ServiceResult.BadRequest("El contenido JSON de configuración es obligatorio.");
        }

        try
        {
            JsonDocument.Parse(jsonConfig);
        }
        catch (JsonException ex)
        {
            return ServiceResult.BadRequest($"El formato del JSON de configuración es inválido: {ex.Message}");
        }

        string hash = CalcularHashSha256(jsonConfig);
        bool exito = await _repo.ActualizarBorradorFormularioAsync(versionId, jsonConfig, hash, usuarioId);
        
        if (!exito)
        {
            return ServiceResult.BadRequest("No se pudo actualizar el formulario. Verifique que exista y esté en estado DRAFT.");
        }
        return ServiceResult.Ok("Borrador de formulario actualizado exitosamente.");
    }

    public async Task<ServiceResult> PublicarVersionFormularioAsync(long versionId, long usuarioId)
    {
        var version = await _repo.ObtenerVersionFormularioAsync(versionId);
        if (version == null)
        {
            return ServiceResult.NotFound($"No se encontró la versión de formulario con ID {versionId}.");
        }

        if (!version.VerEstado.Equals("DRAFT", StringComparison.OrdinalIgnoreCase))
        {
            return ServiceResult.BadRequest("Solo se pueden publicar versiones de formularios que estén en estado DRAFT.");
        }

        string hash = CalcularHashSha256(version.VerJson);
        bool exito = await _repo.PublicarVersionFormularioAsync(versionId, hash, usuarioId);

        if (!exito)
        {
            return ServiceResult.BadRequest("Ocurrió un error al intentar publicar la versión del formulario.");
        }
        return ServiceResult.Ok("Versión de formulario publicada y activada como vigente exitosamente.");
    }

    public async Task<ServiceResult> CambiarEstadoVigenciaFormularioAsync(long versionId, bool vigente, long usuarioId)
    {
        bool exito = await _repo.CambiarEstadoVigenciaFormularioAsync(versionId, vigente, usuarioId);
        if (!exito)
        {
            return ServiceResult.NotFound($"No se encontró la versión de formulario con ID {versionId} para cambiar su vigencia.");
        }
        return ServiceResult.Ok("Estado de vigencia de la versión de formulario actualizado con éxito.");
    }

    public async Task<ServiceResult<List<VersionFormularioDto>>> ListarHistorialVersionesFormularioAsync(string familiaCodigo)
    {
        var historial = await _repo.ListarHistorialVersionesFormularioAsync(familiaCodigo);
        return ServiceResult<List<VersionFormularioDto>>.Ok(historial);
    }

    // ============================================================
    // 2. GESTIÓN DE EVALUACIONES E HISTORIAL DE CAMBIOS
    // ============================================================

    public async Task<ServiceResult<EvaluacionRiesgoDto>> ObtenerEvaluacionAsync(long evaId)
    {
        var eva = await _repo.ObtenerEvaluacionAsync(evaId);
        if (eva == null)
        {
            return ServiceResult<EvaluacionRiesgoDto>.NotFound($"No se encontró la evaluación de riesgo con ID {evaId}.");
        }
        return ServiceResult<EvaluacionRiesgoDto>.Ok(eva);
    }

    public async Task<ServiceResult<List<EvaluacionRiesgoDto>>> ListarEvaluacionesPaginadasAsync(ConsultaEvaluacionPaginadaDto filtro)
    {
        var lista = await _repo.ListarEvaluacionesPaginadasAsync(filtro);
        return ServiceResult<List<EvaluacionRiesgoDto>>.Ok(lista);
    }

    public async Task<ServiceResult<long>> CrearEvaluacionAsync(EvaluacionRiesgoDto dto, long usuarioId, string? ip)
    {
        // 1. Obtener la versión de formulario vinculada
        var version = await _repo.ObtenerVersionFormularioAsync(dto.EvaVersionId);
        if (version == null)
        {
            return ServiceResult<long>.BadRequest($"La versión de formulario ID {dto.EvaVersionId} no existe en el sistema.");
        }

        // 2. Validación Dura del JSON de respuestas
        var valResult = await _validador.ValidarRespuestasAsync(dto.EvaDataJson, version.VerJson);
        if (!valResult.Valido)
        {
            var errorsList = new List<string>();
            foreach (var err in valResult.Errores)
            {
                errorsList.Add($"Campo '{err.Campo}': {err.Mensaje}");
            }
            return ServiceResult<long>.BadRequest("Error de validación de estructura JSON:\n" + string.Join("\n", errorsList));
        }

        // 3. Ejecutar motor de cálculo y coherencia residual (VRI, ETP, VRR)
        var vars = ExtraerVariablesCalculo(dto.EvaDataJson);
        var calcResult = _calculador.CalcularYValidarRiesgo(vars.frec, vars.imp, vars.prev, vars.det, vars.corr, vars.frecRes, vars.impRes);
        if (!calcResult.Success)
        {
            return ServiceResult<long>.BadRequest(calcResult.Message ?? "Error de cálculo");
        }

        // 4. Inyectar los valores calculados al DTO de persistencia
        dto.EvaVri = calcResult.Data!.Vri;
        dto.EvaEtp = calcResult.Data!.Etp;
        dto.EvaVrr = calcResult.Data!.Vrr;

        // Formar el JSON calculado
        dto.EvaDataCalcJson = JsonSerializer.Serialize(calcResult.Data);

        long nuevoId = await _repo.CrearEvaluacionAsync(dto, usuarioId, ip);
        return ServiceResult<long>.Ok(nuevoId, "Evaluación de riesgo creada y calculada exitosamente.");
    }

    public async Task<ServiceResult> ActualizarEvaluacionAsync(EvaluacionRiesgoDto dto, long usuarioId, string? ip)
    {
        var version = await _repo.ObtenerVersionFormularioAsync(dto.EvaVersionId);
        if (version == null)
        {
            return ServiceResult.BadRequest($"La versión de formulario ID {dto.EvaVersionId} no existe.");
        }

        // 1. Validación Dura del JSON
        var valResult = await _validador.ValidarRespuestasAsync(dto.EvaDataJson, version.VerJson);
        if (!valResult.Valido)
        {
            var errorsList = new List<string>();
            foreach (var err in valResult.Errores)
            {
                errorsList.Add($"Campo '{err.Campo}': {err.Mensaje}");
            }
            return ServiceResult.BadRequest("Error de validación de estructura JSON:\n" + string.Join("\n", errorsList));
        }

        // 2. Ejecutar motor de cálculo
        var vars = ExtraerVariablesCalculo(dto.EvaDataJson);
        var calcResult = _calculador.CalcularYValidarRiesgo(vars.frec, vars.imp, vars.prev, vars.det, vars.corr, vars.frecRes, vars.impRes);
        if (!calcResult.Success)
        {
            return ServiceResult.BadRequest(calcResult.Message ?? "Error de cálculo");
        }

        dto.EvaVri = calcResult.Data!.Vri;
        dto.EvaEtp = calcResult.Data!.Etp;
        dto.EvaVrr = calcResult.Data!.Vrr;
        dto.EvaDataCalcJson = JsonSerializer.Serialize(calcResult.Data);

        try
        {
            bool exito = await _repo.ActualizarEvaluacionAsync(dto, usuarioId, ip);
            if (!exito)
            {
                return ServiceResult.NotFound($"No se encontró la evaluación con ID {dto.EvaId} para actualizar.");
            }
            return ServiceResult.Ok("Evaluación actualizada y calculada con éxito.");
        }
        catch (System.Data.DBConcurrencyException ex)
        {
            return new ServiceResult(false, ex.Message, 409);
        }
    }

    public async Task<ServiceResult> TransicionarEstadoEvaluacionAsync(long evaId, string nuevoEstado, string? motivo, long usuarioId, string? ip)
    {
        var evaluacion = await _repo.ObtenerEvaluacionAsync(evaId);
        if (evaluacion == null)
        {
            return ServiceResult.NotFound($"No se encontró la evaluación ID {evaId}.");
        }

        // Validar transiciones autorizadas de la Máquina de Estados
        string actual = evaluacion.EvaEstado.ToUpperInvariant();
        string nuevo = nuevoEstado.ToUpperInvariant();
        bool transicionValida = false;

        if (actual == "BORRADOR" && nuevo == "EN_REVISION") transicionValida = true;
        else if (actual == "EN_REVISION" && (nuevo == "OBSERVADA" || nuevo == "APROBADA" || nuevo == "RECHAZADA")) transicionValida = true;
        else if (actual == "OBSERVADA" && nuevo == "BORRADOR") transicionValida = true;
        else if (actual == "APROBADA" && nuevo == "CERRADA") transicionValida = true;

        if (!transicionValida)
        {
            return ServiceResult.BadRequest($"Transición de estado inválida: No se permite pasar del estado '{actual}' al estado '{nuevo}' según el flujo de la máquina de estados.");
        }

        bool exito = await _repo.TransicionarEstadoEvaluacionAsync(evaId, nuevo, motivo, usuarioId, ip);
        if (!exito)
        {
            return ServiceResult.BadRequest("No se pudo realizar la transición de estado.");
        }
        return ServiceResult.Ok($"Transición de estado a '{nuevo}' realizada exitosamente.");
    }

    public async Task<ServiceResult<List<RevisionEvaluacionDto>>> ObtenerRevisionesEvaluacionAsync(long evaId)
    {
        var lista = await _repo.ObtenerRevisionesEvaluacionAsync(evaId);
        return ServiceResult<List<RevisionEvaluacionDto>>.Ok(lista);
    }

    // ============================================================
    // 3. ARCHIVO FÍSICO CENTRAL DE EVIDENCIAS Y SUS VINCULACIONES
    // ============================================================

    public async Task<ServiceResult<EvidenciaDto>> CargarArchivoEvidenciaFisicaAsync(IFormFile archivo, long usuarioId)
    {
        if (archivo == null || archivo.Length == 0)
        {
            return ServiceResult<EvidenciaDto>.BadRequest("El archivo cargado está vacío o es nulo.");
        }

        try
        {
            // Generar ruta de almacenamiento físico
            string uploadsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data", "Evidencias");
            if (!Directory.Exists(uploadsPath))
            {
                Directory.CreateDirectory(uploadsPath);
            }

            string ext = Path.GetExtension(archivo.FileName).ToLowerInvariant();
            string nombreFisico = $"{Guid.NewGuid()}{ext}";
            string rutaCompleta = Path.Combine(uploadsPath, nombreFisico);

            // Guardar archivo físico
            using (var stream = new FileStream(rutaCompleta, FileMode.Create))
            {
                await archivo.CopyToAsync(stream);
            }

            // Calcular Hash SHA-256 del archivo
            string hash = string.Empty;
            using (var sha = SHA256.Create())
            {
                using var stream = File.OpenRead(rutaCompleta);
                byte[] hashBytes = await sha.ComputeHashAsync(stream);
                hash = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }

            var dto = new EvidenciaRegistroDto
            {
                EviNombreArchivo = archivo.FileName,
                EviExtension = ext.Replace(".", ""),
                EviTamano = archivo.Length,
                EviHash = hash,
                EviRuta = Path.Combine("App_Data", "Evidencias", nombreFisico),
                EviUsrCreacion = usuarioId
            };

            long nuevoId = await _repo.RegistrarEvidenciaFisicaAsync(dto, usuarioId);
            var result = await _repo.ObtenerEvidenciaFisicaAsync(nuevoId);
            
            return ServiceResult<EvidenciaDto>.Ok(result!, "Archivo físico cargado y registrado de forma exitosa.");
        }
        catch (Exception ex)
        {
            return new ServiceResult<EvidenciaDto>(false, null, $"Error físico de carga de archivo: {ex.Message}", 500);
        }
    }

    public async Task<ServiceResult<EvidenciaDto>> ObtenerEvidenciaFisicaAsync(long evidenciaId)
    {
        var evidencia = await _repo.ObtenerEvidenciaFisicaAsync(evidenciaId);
        if (evidencia == null)
        {
            return ServiceResult<EvidenciaDto>.NotFound($"No se encontró el registro de evidencia con ID {evidenciaId}.");
        }
        return ServiceResult<EvidenciaDto>.Ok(evidencia);
    }

    public async Task<ServiceResult> VincularEvidenciaRiesgoAsync(AsociarEvidenciaRiesgoDto dto, long usuarioId, string? ip)
    {
        bool exito = await _repo.VincularEvidenciaRiesgoAsync(dto, usuarioId, ip);
        return ResponderVinculo(exito);
    }

    public async Task<ServiceResult> VincularEvidenciaEvaluacionAsync(AsociarEvidenciaEvaluacionDto dto, long usuarioId, string? ip)
    {
        bool exito = await _repo.VincularEvidenciaEvaluacionAsync(dto, usuarioId, ip);
        return ResponderVinculo(exito);
    }

    public async Task<ServiceResult> VincularEvidenciaControlAsync(AsociarEvidenciaControlDto dto, long usuarioId, string? ip)
    {
        bool exito = await _repo.VincularEvidenciaControlAsync(dto, usuarioId, ip);
        return ResponderVinculo(exito);
    }

    public async Task<ServiceResult> VincularEvidenciaPlanAsync(AsociarEvidenciaPlanDto dto, long usuarioId, string? ip)
    {
        bool exito = await _repo.VincularEvidenciaPlanAsync(dto, usuarioId, ip);
        return ResponderVinculo(exito);
    }

    public async Task<ServiceResult> VincularEvidenciaActividadAsync(AsociarEvidenciaActividadDto dto, long usuarioId, string? ip)
    {
        bool exito = await _repo.VincularEvidenciaActividadAsync(dto, usuarioId, ip);
        return ResponderVinculo(exito);
    }

    public async Task<ServiceResult> VincularEvidenciaAlertaAsync(AsociarEvidenciaAlertaDto dto, long usuarioId, string? ip)
    {
        bool exito = await _repo.VincularEvidenciaAlertaAsync(dto, usuarioId, ip);
        return ResponderVinculo(exito);
    }

    public async Task<ServiceResult> VincularEvidenciaAutomonitoreoAsync(AsociarEvidenciaAutomonitoreoDto dto, long usuarioId, string? ip)
    {
        bool exito = await _repo.VincularEvidenciaAutomonitoreoAsync(dto, usuarioId, ip);
        return ResponderVinculo(exito);
    }

    public async Task<ServiceResult> VincularEvidenciaRevisionAsync(AsociarEvidenciaRevisionDto dto, long usuarioId, string? ip)
    {
        bool exito = await _repo.VincularEvidenciaRevisionAsync(dto, usuarioId, ip);
        return ResponderVinculo(exito);
    }

    public async Task<ServiceResult> VincularEvidenciaAprobacionAsync(AsociarEvidenciaAprobacionDto dto, long usuarioId, string? ip)
    {
        bool exito = await _repo.VincularEvidenciaAprobacionAsync(dto, usuarioId, ip);
        return ResponderVinculo(exito);
    }

    public async Task<ServiceResult> EliminarEvidenciaAsync(long evidenciaId, long usuarioId, string? ip)
    {
        var evidencia = await _repo.ObtenerEvidenciaFisicaAsync(evidenciaId);
        if (evidencia == null)
        {
            // Idempotencia: Si ya fue eliminada, responder exitosamente sin error funcional.
            return ServiceResult.Ok("La evidencia no existe o ya fue eliminada.");
        }

        // Definir la lambda para borrar el archivo físico del disco del servidor
        Func<Task<bool>> eliminarArchivoFisico = () =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(evidencia.EviRuta))
                {
                    return Task.FromResult(true); // Sin archivo físico que eliminar, consideramos limpio.
                }

                string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, evidencia.EviRuta);
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error físico al eliminar archivo de evidencia ID {Id} en disco", evidenciaId);
                return Task.FromResult(false); // Retorna false si el disco falla.
            }
        };

        var resultado = await _repo.EliminarEvidenciaSeguraAsync(evidenciaId, eliminarArchivoFisico, usuarioId, ip);

        switch (resultado)
        {
            case ResultadoEliminacionEvidencia.Exito:
                // Auditoría exitosa
                await _auditoriaRepo.RegistrarAsync(
                    "RL_MR_EVIDENCIAS", 
                    evidenciaId.ToString(), 
                    "DELETE", 
                    $"Eliminación física exitosa del archivo {evidencia.EviNombreArchivo}.", 
                    null, 
                    usuarioId, 
                    null, 
                    ip, 
                    "MatricesRiesgos"
                );
                return ServiceResult.Ok("Evidencia eliminada de forma exitosa.");

            case ResultadoEliminacionEvidencia.NoExiste:
                return ServiceResult.Ok("La evidencia no existe o ya fue eliminada.");

            case ResultadoEliminacionEvidencia.TieneVinculos:
                return ServiceResult.BadRequest("No se puede eliminar la evidencia porque ya se encuentra vinculada a un elemento del sistema.");

            case ResultadoEliminacionEvidencia.FalloDisco:
                return ServiceResult.BadRequest("Error al eliminar el archivo físico en el disco del servidor. El registro en la base de datos se mantiene intacto.");

            case ResultadoEliminacionEvidencia.FalloCommit:
                // Registro de inconsistencia auditable e inmutable
                await _auditoriaRepo.RegistrarAsync(
                    "RL_MR_EVIDENCIAS", 
                    evidenciaId.ToString(), 
                    "ERROR_COMPENSACION_EVIDENCIA", 
                    $"Se eliminó el archivo físico {evidencia.EviNombreArchivo} pero falló la confirmación de la base de datos Oracle.", 
                    null, 
                    usuarioId, 
                    null, 
                    ip, 
                    "MatricesRiesgos"
                );
                return new ServiceResult(false, "Error crítico de persistencia: El archivo físico fue eliminado, pero falló la confirmación en la base de datos.", 500);

            default:
                return ServiceResult.BadRequest("Resultado de eliminación desconocido.");
        }
    }

    private static ServiceResult ResponderVinculo(bool exito)
    {
        if (exito) return ServiceResult.Ok("Evidencia vinculada exitosamente de forma relacional.");
        return ServiceResult.BadRequest("No se pudo realizar la vinculación relacional de la evidencia.");
    }

    // ============================================================
    // 4. REPORTES CONSOLIDADOS
    // ============================================================

    public async Task<ServiceResult<List<Dictionary<string, object>>>> ObtenerConsolidadoMatricesAsync()
    {
        var consolidado = await _repo.ObtenerConsolidadoMatricesAsync();
        return ServiceResult<List<Dictionary<string, object>>>.Ok(consolidado);
    }

    // ============================================================
    // AUXILIARES
    // ============================================================

    private static string CalcularHashSha256(string texto)
    {
        using var sha = SHA256.Create();
        byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(texto));
        return BitConverter.ToString(bytes).Replace("-", "").ToLower();
    }

    private static (int frec, int imp, decimal prev, decimal det, decimal corr, int frecRes, int impRes) ExtraerVariablesCalculo(string jsonRespuestas)
    {
        int frec = 0, imp = 0, frecRes = 0, impRes = 0;
        decimal prev = 0, det = 0, corr = 0;

        try
        {
            using var doc = JsonDocument.Parse(jsonRespuestas);
            var root = doc.RootElement;

            if (root.TryGetProperty("frecuencia_inherente", out var fProp) && fProp.ValueKind == JsonValueKind.Number) frec = fProp.GetInt32();
            else if (root.TryGetProperty("frecuencia_inherente", out var fPropStr) && fPropStr.ValueKind == JsonValueKind.String && int.TryParse(fPropStr.GetString(), out var fVal)) frec = fVal;

            if (root.TryGetProperty("impacto_inherente", out var iProp) && iProp.ValueKind == JsonValueKind.Number) imp = iProp.GetInt32();
            else if (root.TryGetProperty("impacto_inherente", out var iPropStr) && iPropStr.ValueKind == JsonValueKind.String && int.TryParse(iPropStr.GetString(), out var iVal)) imp = iVal;

            if (root.TryGetProperty("controles_preventivo", out var cpProp) && cpProp.ValueKind == JsonValueKind.Number) prev = cpProp.GetDecimal();
            else if (root.TryGetProperty("controles_preventivo", out var cpPropStr) && cpPropStr.ValueKind == JsonValueKind.String && decimal.TryParse(cpPropStr.GetString(), out var cpVal)) prev = cpVal;

            if (root.TryGetProperty("controles_detectivo", out var cdProp) && cdProp.ValueKind == JsonValueKind.Number) det = cdProp.GetDecimal();
            else if (root.TryGetProperty("controles_detectivo", out var cdPropStr) && cdPropStr.ValueKind == JsonValueKind.String && decimal.TryParse(cdPropStr.GetString(), out var cdVal)) det = cdVal;

            if (root.TryGetProperty("controles_correctivo", out var ccProp) && ccProp.ValueKind == JsonValueKind.Number) corr = ccProp.GetDecimal();
            else if (root.TryGetProperty("controles_correctivo", out var ccPropStr) && ccPropStr.ValueKind == JsonValueKind.String && decimal.TryParse(ccPropStr.GetString(), out var ccVal)) corr = ccVal;

            if (root.TryGetProperty("frecuencia_residual", out var frProp) && frProp.ValueKind == JsonValueKind.Number) frecRes = frProp.GetInt32();
            else if (root.TryGetProperty("frecuencia_residual", out var frPropStr) && frPropStr.ValueKind == JsonValueKind.String && int.TryParse(frPropStr.GetString(), out var frVal)) frecRes = frVal;

            if (root.TryGetProperty("impacto_residual", out var irProp) && irProp.ValueKind == JsonValueKind.Number) impRes = irProp.GetInt32();
            else if (root.TryGetProperty("impacto_residual", out var irPropStr) && irPropStr.ValueKind == JsonValueKind.String && int.TryParse(irPropStr.GetString(), out var irVal)) impRes = irVal;
        }
        catch { }

        return (frec, imp, prev, det, corr, frecRes, impRes);
    }
}
