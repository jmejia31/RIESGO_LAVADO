using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using RL.API.Features.Auditoria.Persistence;
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
    private readonly IAuditoriaRepository _auditoriaRepo;

    public MatricesRiesgosAppService(
        IMatricesRiesgosRepository repo,
        IFormularioValidador validador,
        IMatricesRiesgoService calculador,
        IAuditoriaRepository auditoriaRepo)
    {
        _repo = repo;
        _validador = validador;
        _calculador = calculador;
        _auditoriaRepo = auditoriaRepo;
    }

    public async Task<ServiceResult<VersionFormularioDto>> ObtenerVersionVigenteFormularioAsync(string familiaCodigo)
    {
        VersionFormularioDto? version = await _repo.ObtenerVersionVigenteFormularioAsync(familiaCodigo);
        return version is null
            ? ServiceResult<VersionFormularioDto>.NotFound($"No existe una versión publicada y vigente para la familia '{familiaCodigo}'.")
            : ServiceResult<VersionFormularioDto>.Ok(version);
    }

    public async Task<ServiceResult<VersionFormularioDto>> ObtenerVersionFormularioAsync(long versionId)
    {
        VersionFormularioDto? version = await _repo.ObtenerVersionFormularioAsync(versionId);
        return version is null
            ? ServiceResult<VersionFormularioDto>.NotFound($"No se encontró la versión de formulario con ID {versionId}.")
            : ServiceResult<VersionFormularioDto>.Ok(version);
    }

    public async Task<ServiceResult<long>> CrearBorradorFormularioAsync(
        long familiaId,
        string codigoFormulario,
        string jsonConfig,
        long usuarioId)
    {
        if (string.IsNullOrWhiteSpace(jsonConfig))
        {
            return ServiceResult<long>.BadRequest("La definición del formulario es obligatoria.");
        }

        try
        {
            using JsonDocument _ = JsonDocument.Parse(jsonConfig);
        }
        catch (JsonException ex)
        {
            return ServiceResult<long>.BadRequest($"La definición del formulario no es válida: {ex.Message}");
        }

        long id = await _repo.CrearBorradorFormularioAsync(
            familiaId,
            codigoFormulario,
            jsonConfig,
            usuarioId);
        return ServiceResult<long>.Ok(id, "Borrador de formulario creado exitosamente.");
    }

    public async Task<ServiceResult<long>> ClonarVersionFormularioAsync(long versionOrigenId, long usuarioId)
    {
        try
        {
            long id = await _repo.ClonarVersionFormularioAsync(versionOrigenId, usuarioId);
            return ServiceResult<long>.Ok(id, "Versión clonada como borrador exitosamente.");
        }
        catch (KeyNotFoundException ex)
        {
            return ServiceResult<long>.NotFound(ex.Message);
        }
    }

    public async Task<ServiceResult> ActualizarBorradorFormularioAsync(
        long versionId,
        string jsonConfig,
        long usuarioId)
    {
        if (string.IsNullOrWhiteSpace(jsonConfig))
        {
            return ServiceResult.BadRequest("La definición del formulario es obligatoria.");
        }

        try
        {
            using JsonDocument _ = JsonDocument.Parse(jsonConfig);
        }
        catch (JsonException ex)
        {
            return ServiceResult.BadRequest($"La definición del formulario no es válida: {ex.Message}");
        }

        bool actualizado = await _repo.ActualizarBorradorFormularioAsync(
            versionId,
            jsonConfig,
            CalcularHashSha256(jsonConfig),
            usuarioId);

        return actualizado
            ? ServiceResult.Ok("Borrador actualizado exitosamente.")
            : ServiceResult.BadRequest("No se pudo actualizar. Verifique que la versión exista y permanezca en DRAFT.");
    }

    public async Task<ServiceResult> PublicarVersionFormularioAsync(long versionId, long usuarioId)
    {
        VersionFormularioDto? version = await _repo.ObtenerVersionFormularioAsync(versionId);
        if (version is null)
        {
            return ServiceResult.NotFound($"No se encontró la versión de formulario con ID {versionId}.");
        }

        if (!version.VerEstado.Equals("DRAFT", StringComparison.OrdinalIgnoreCase)
            && !version.VerEstado.Equals("APPROVED", StringComparison.OrdinalIgnoreCase))
        {
            return ServiceResult.BadRequest("Solo una versión DRAFT o APPROVED puede publicarse.");
        }

        bool publicado = await _repo.PublicarVersionFormularioAsync(
            versionId,
            CalcularHashSha256(version.VerJson),
            usuarioId);

        return publicado
            ? ServiceResult.Ok("Versión publicada y activada como vigente.")
            : ServiceResult.BadRequest("No fue posible publicar la versión del formulario.");
    }

    public async Task<ServiceResult> CambiarEstadoVigenciaFormularioAsync(
        long versionId,
        bool vigente,
        long usuarioId)
    {
        bool actualizado = await _repo.CambiarEstadoVigenciaFormularioAsync(versionId, vigente, usuarioId);
        return actualizado
            ? ServiceResult.Ok("Vigencia actualizada correctamente.")
            : ServiceResult.NotFound($"No se encontró una versión publicada con ID {versionId}.");
    }

    public async Task<ServiceResult> EliminarVersionFormularioAsync(long versionId)
    {
        VersionFormularioDto? v = await _repo.ObtenerVersionFormularioAsync(versionId);
        if (v is null)
        {
            return ServiceResult.NotFound($"No se encontró el formulario con ID {versionId}.");
        }

        if (v.VerVigente)
        {
            return ServiceResult.BadRequest("No se puede eliminar el formulario activo (vigente) de la familia.");
        }

        bool eliminado = await _repo.EliminarVersionFormularioAsync(versionId);
        return eliminado
            ? ServiceResult.Ok("Formulario eliminado correctamente.")
            : ServiceResult.BadRequest("No se pudo eliminar el formulario. Verifique que no esté activo.");
    }

    public async Task<ServiceResult<List<VersionFormularioDto>>> ListarHistorialVersionesFormularioAsync(string familiaCodigo)
    {
        List<VersionFormularioDto> versiones = await _repo.ListarHistorialVersionesFormularioAsync(familiaCodigo);
        return ServiceResult<List<VersionFormularioDto>>.Ok(versiones);
    }

    public async Task<ServiceResult<List<FamiliaFormularioDto>>> ListarFamiliasFormularioAsync()
    {
        List<FamiliaFormularioDto> familias = await _repo.ListarFamiliasFormularioAsync();
        return ServiceResult<List<FamiliaFormularioDto>>.Ok(familias);
    }

    public async Task<ServiceResult<FamiliaFormularioDto>> ObtenerFamiliaFormularioPorIdAsync(long famId)
    {
        if (famId <= 0)
        {
            return ServiceResult<FamiliaFormularioDto>.BadRequest("El ID de familia especificado es inválido.");
        }

        FamiliaFormularioDto? familia = await _repo.ObtenerFamiliaFormularioPorIdAsync(famId);
        return familia is null
            ? ServiceResult<FamiliaFormularioDto>.NotFound($"No se encontró la familia de formulario con ID {famId}.")
            : ServiceResult<FamiliaFormularioDto>.Ok(familia);
    }

    public async Task<ServiceResult<long>> CrearFamiliaFormularioAsync(CrearFamiliaFormularioDto dto)
    {
        if (dto is null)
        {
            return ServiceResult<long>.BadRequest("Los datos de la familia son obligatorios.");
        }

        string codigoNormalizado = (dto.FamCodigo ?? string.Empty).Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(codigoNormalizado) || codigoNormalizado.Length < 3 || codigoNormalizado.Length > 50)
        {
            return ServiceResult<long>.BadRequest("El código de la familia debe tener entre 3 y 50 caracteres.");
        }

        if (codigoNormalizado.Any(c => !char.IsLetterOrDigit(c) && c != '_'))
        {
            return ServiceResult<long>.BadRequest("El código de la familia solo permite letras, números y guion bajo.");
        }

        string nombreNormalizado = (dto.FamNombre ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(nombreNormalizado))
        {
            return ServiceResult<long>.BadRequest("El nombre de la familia es obligatorio.");
        }

        FamiliaFormularioDto? existente = await _repo.ObtenerFamiliaFormularioPorCodigoAsync(codigoNormalizado);
        if (existente is not null)
        {
            return ServiceResult<long>.BadRequest($"Ya existe una familia de formulario registrada con el código '{codigoNormalizado}'.");
        }

        long famId = await _repo.CrearFamiliaFormularioAsync(
            codigoNormalizado,
            nombreNormalizado,
            dto.FamDescripcion?.Trim(),
            famActivo: true);

        return ServiceResult<long>.Ok(famId, "Familia de formulario creada exitosamente.");
    }

    public async Task<ServiceResult> ActualizarFamiliaFormularioAsync(long famId, ActualizarFamiliaFormularioDto dto)
    {
        if (famId <= 0)
        {
            return ServiceResult.BadRequest("El ID de familia especificado es inválido.");
        }

        if (dto is null)
        {
            return ServiceResult.BadRequest("Los datos a actualizar son obligatorios.");
        }

        FamiliaFormularioDto? existente = await _repo.ObtenerFamiliaFormularioPorIdAsync(famId);
        if (existente is null)
        {
            return ServiceResult.NotFound($"No se encontró la familia de formulario con ID {famId}.");
        }

        string nombreNormalizado = (dto.FamNombre ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(nombreNormalizado))
        {
            return ServiceResult.BadRequest("El nombre de la familia es obligatorio.");
        }

        if (!dto.FamActivo && existente.TieneVersionVigente)
        {
            return ServiceResult.BadRequest("No se puede desactivar la familia mientras posea una versión publicada vigente.");
        }

        bool actualizado = await _repo.ActualizarFamiliaFormularioAsync(
            famId,
            nombreNormalizado,
            dto.FamDescripcion?.Trim(),
            dto.FamActivo);

        return actualizado
            ? ServiceResult.Ok("Familia de formulario actualizada correctamente.")
            : ServiceResult.BadRequest("No se pudo actualizar la familia de formulario.");
    }

    public async Task<ServiceResult> DesactivarFamiliaFormularioAsync(long famId)
    {
        if (famId <= 0)
        {
            return ServiceResult.BadRequest("El ID de familia especificado es inválido.");
        }

        FamiliaFormularioDto? existente = await _repo.ObtenerFamiliaFormularioPorIdAsync(famId);
        if (existente is null)
        {
            return ServiceResult.NotFound($"No se encontró la familia de formulario con ID {famId}.");
        }

        bool desactivado = await _repo.DesactivarFamiliaFormularioAtomicoAsync(famId);
        return desactivado
            ? ServiceResult.Ok("Familia de formulario desactivada exitosamente.")
            : ServiceResult.BadRequest("No se pudo desactivar la familia. Verifique que no posea una versión publicada vigente.");
    }

    public async Task<ServiceResult<EvaluacionRiesgoDto>> ObtenerEvaluacionAsync(long evaId)
    {
        EvaluacionRiesgoDto? evaluacion = await _repo.ObtenerEvaluacionAsync(evaId);
        return evaluacion is null
            ? ServiceResult<EvaluacionRiesgoDto>.NotFound($"No se encontró la evaluación con ID {evaId}.")
            : ServiceResult<EvaluacionRiesgoDto>.Ok(evaluacion);
    }

    public async Task<ServiceResult<List<EvaluacionRiesgoDto>>> ListarEvaluacionesPaginadasAsync(
        ConsultaEvaluacionPaginadaDto filtro)
    {
        List<EvaluacionRiesgoDto> evaluaciones = await _repo.ListarEvaluacionesPaginadasAsync(filtro);
        return ServiceResult<List<EvaluacionRiesgoDto>>.Ok(evaluaciones);
    }

    public async Task<ServiceResult<long>> CrearEvaluacionAsync(
        EvaluacionRiesgoDto dto,
        long usuarioId,
        string? ip)
    {
        VersionFormularioDto? version = await _repo.ObtenerVersionFormularioAsync(dto.EvaVersionId);
        if (version is null)
        {
            return ServiceResult<long>.BadRequest($"La versión de formulario ID {dto.EvaVersionId} no existe.");
        }

        if (!version.VerEstado.Equals("PUBLISHED", StringComparison.OrdinalIgnoreCase)
            || !version.VerVigente)
        {
            return ServiceResult<long>.BadRequest("La evaluación debe originarse en una versión publicada y vigente.");
        }

        ServiceResult? validacion = await ValidarYCalcularEvaluacionAsync(dto, version.VerJson);
        if (validacion is not null)
        {
            return new ServiceResult<long>(false, null, validacion.Message, validacion.StatusCode);
        }

        try
        {
            long id = await _repo.CrearEvaluacionAsync(dto, usuarioId, ip);
            return ServiceResult<long>.Ok(id, "Evaluación creada y calculada exitosamente.");
        }
        catch (KeyNotFoundException ex)
        {
            return ServiceResult<long>.BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return ServiceResult<long>.BadRequest(ex.Message);
        }
    }

    public async Task<ServiceResult> ActualizarEvaluacionAsync(
        EvaluacionRiesgoDto dto,
        long usuarioId,
        string? ip)
    {
        VersionFormularioDto? version = await _repo.ObtenerVersionFormularioAsync(dto.EvaVersionId);
        if (version is null)
        {
            return ServiceResult.BadRequest($"La versión de formulario ID {dto.EvaVersionId} no existe.");
        }

        ServiceResult? validacion = await ValidarYCalcularEvaluacionAsync(dto, version.VerJson);
        if (validacion is not null)
        {
            return validacion;
        }

        try
        {
            bool actualizado = await _repo.ActualizarEvaluacionAsync(dto, usuarioId, ip);
            return actualizado
                ? ServiceResult.Ok("Evaluación actualizada y calculada exitosamente.")
                : ServiceResult.NotFound($"No se encontró la evaluación con ID {dto.EvaId}.");
        }
        catch (DBConcurrencyException ex)
        {
            return new ServiceResult(false, ex.Message, 409);
        }
        catch (InvalidOperationException ex)
        {
            return ServiceResult.BadRequest(ex.Message);
        }
    }

    public async Task<ServiceResult> TransicionarEstadoEvaluacionAsync(
        long evaId,
        string nuevoEstado,
        string? motivo,
        long usuarioId,
        string? ip)
    {
        EvaluacionRiesgoDto? evaluacion = await _repo.ObtenerEvaluacionAsync(evaId);
        if (evaluacion is null)
        {
            return ServiceResult.NotFound($"No se encontró la evaluación ID {evaId}.");
        }

        string actual = evaluacion.EvaEstado.ToUpperInvariant();
        string nuevo = nuevoEstado.Trim().ToUpperInvariant();
        bool permitida =
            actual == "BORRADOR" && nuevo == "EN_REVISION"
            || actual == "EN_REVISION" && nuevo is "OBSERVADA" or "APROBADA" or "RECHAZADA"
            || actual == "OBSERVADA" && nuevo == "BORRADOR"
            || actual == "APROBADA" && nuevo == "CERRADA";

        if (!permitida)
        {
            return ServiceResult.BadRequest($"Transición inválida: no se permite pasar de '{actual}' a '{nuevo}'.");
        }

        bool actualizado = await _repo.TransicionarEstadoEvaluacionAsync(
            evaId,
            nuevo,
            motivo,
            usuarioId,
            ip);

        return actualizado
            ? ServiceResult.Ok($"Transición a '{nuevo}' realizada exitosamente.")
            : ServiceResult.BadRequest("No se pudo realizar la transición de estado.");
    }

    public async Task<ServiceResult<List<FlujoEvaluacionDto>>> ObtenerFlujosEvaluacionAsync(long evaId) =>
        ServiceResult<List<FlujoEvaluacionDto>>.Ok(await _repo.ObtenerFlujosEvaluacionAsync(evaId));

    public async Task<ServiceResult<EvidenciaDto>> CargarArchivoEvidenciaFisicaAsync(IFormFile archivo, long usuarioId)
    {
        if (archivo is null || archivo.Length == 0)
        {
            return ServiceResult<EvidenciaDto>.BadRequest("El archivo cargado está vacío.");
        }

        string uploadsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data", "Evidencias");
        Directory.CreateDirectory(uploadsPath);
        string extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
        string nombreFisico = $"{Guid.NewGuid()}{extension}";
        string rutaCompleta = Path.Combine(uploadsPath, nombreFisico);

        try
        {
            await using (var stream = new FileStream(rutaCompleta, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await archivo.CopyToAsync(stream);
            }

            string hash;
            using (var sha = SHA256.Create())
            await using (var stream = File.OpenRead(rutaCompleta))
            {
                hash = Convert.ToHexString(await sha.ComputeHashAsync(stream)).ToLowerInvariant();
            }

            var dto = new EvidenciaRegistroDto
            {
                EviNombreArchivo = archivo.FileName,
                EviExtension = extension.TrimStart('.'),
                EviTamano = archivo.Length,
                EviHash = hash,
                EviRuta = Path.Combine("App_Data", "Evidencias", nombreFisico),
                EviUsrCreacion = usuarioId
            };

            long id = await _repo.RegistrarEvidenciaFisicaAsync(dto, usuarioId);
            EvidenciaDto? evidencia = await _repo.ObtenerEvidenciaFisicaAsync(id);
            return evidencia is null
                ? new ServiceResult<EvidenciaDto>(false, null, "La evidencia fue registrada pero no pudo recuperarse.", 500)
                : ServiceResult<EvidenciaDto>.Ok(evidencia, "Evidencia cargada correctamente.");
        }
        catch (Exception ex)
        {
            if (File.Exists(rutaCompleta))
            {
                try { File.Delete(rutaCompleta); } catch { }
            }
            return new ServiceResult<EvidenciaDto>(false, null, $"No se pudo cargar la evidencia: {ex.Message}", 500);
        }
    }

    public async Task<ServiceResult<EvidenciaDto>> ObtenerEvidenciaFisicaAsync(long evidenciaId)
    {
        EvidenciaDto? evidencia = await _repo.ObtenerEvidenciaFisicaAsync(evidenciaId);
        return evidencia is null
            ? ServiceResult<EvidenciaDto>.NotFound($"No se encontró la evidencia con ID {evidenciaId}.")
            : ServiceResult<EvidenciaDto>.Ok(evidencia);
    }

    public async Task<ServiceResult> VincularEvidenciaAsync(VincularEvidenciaDto dto, long usuarioId, string? ip)
    {
        if (dto.EvidenciaId <= 0 || dto.EntidadId <= 0)
            return ServiceResult.BadRequest("La evidencia y la entidad destino son obligatorias.");

        try
        {
            return ResponderVinculo(await _repo.VincularEvidenciaAsync(dto, usuarioId, ip));
        }
        catch (KeyNotFoundException ex)
        {
            return ServiceResult.NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return ServiceResult.BadRequest(ex.Message);
        }
    }

    public async Task<ServiceResult> EliminarEvidenciaAsync(long evidenciaId, long usuarioId, string? ip)
    {
        EvidenciaDto? evidencia = await _repo.ObtenerEvidenciaFisicaAsync(evidenciaId);
        if (evidencia is null)
        {
            return ServiceResult.Ok("La evidencia no existe o ya fue eliminada.");
        }

        Func<Task<bool>> eliminarArchivo = () =>
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(evidencia.EviRuta))
                {
                    string ruta = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, evidencia.EviRuta);
                    if (File.Exists(ruta)) File.Delete(ruta);
                }
                return Task.FromResult(true);
            }
            catch
            {
                return Task.FromResult(false);
            }
        };

        ResultadoEliminacionEvidencia resultado = await _repo.EliminarEvidenciaSeguraAsync(
            evidenciaId,
            eliminarArchivo,
            usuarioId,
            ip);

        switch (resultado)
        {
            case ResultadoEliminacionEvidencia.Exito:
                await _auditoriaRepo.RegistrarAsync(
                    "RL_MR_EVIDENCIAS",
                    evidenciaId.ToString(),
                    "DELETE",
                    $"Eliminación física exitosa de {evidencia.EviNombreArchivo}.",
                    null,
                    usuarioId,
                    null,
                    ip,
                    "MatricesRiesgos");
                return ServiceResult.Ok("Evidencia eliminada correctamente.");
            case ResultadoEliminacionEvidencia.NoExiste:
                return ServiceResult.Ok("La evidencia no existe o ya fue eliminada.");
            case ResultadoEliminacionEvidencia.TieneVinculos:
                return ServiceResult.BadRequest("La evidencia tiene vínculos activos y no puede eliminarse.");
            case ResultadoEliminacionEvidencia.FalloDisco:
                return ServiceResult.BadRequest("Falló la eliminación física; el registro Oracle se mantiene.");
            case ResultadoEliminacionEvidencia.FalloCommit:
                await _auditoriaRepo.RegistrarAsync(
                    "RL_MR_EVIDENCIAS",
                    evidenciaId.ToString(),
                    "ERROR_COMPENSACION_EVIDENCIA",
                    $"El archivo {evidencia.EviNombreArchivo} fue eliminado, pero falló el commit Oracle.",
                    null,
                    usuarioId,
                    null,
                    ip,
                    "MatricesRiesgos");
                return new ServiceResult(false, "Falló la confirmación Oracle después de eliminar el archivo físico.", 500);
            default:
                return ServiceResult.BadRequest("Resultado de eliminación desconocido.");
        }
    }

    public async Task<ServiceResult<IReadOnlyList<RiesgoReporteFilaDto>>> ObtenerConsolidadoTipadoAsync()
    {
        IReadOnlyList<RiesgoReporteFilaDto> filas = await _repo.ObtenerConsolidadoTipadoAsync();
        return ServiceResult<IReadOnlyList<RiesgoReporteFilaDto>>.Ok(filas);
    }

    public async Task<ServiceResult<MetodologiaFormularioDto>> ObtenerMetodologiaDinamicaVigenteAsync()
    {
        MetodologiaFormularioDto? metodologia = await _repo.ObtenerMetodologiaDinamicaVigenteAsync();
        return metodologia is null
            ? ServiceResult<MetodologiaFormularioDto>.NotFound("No existe una metodología dinámica publicada y vigente.")
            : ServiceResult<MetodologiaFormularioDto>.Ok(metodologia);
    }

    private async Task<ServiceResult?> ValidarYCalcularEvaluacionAsync(
        EvaluacionRiesgoDto dto,
        string definicionFormulario)
    {
        var validacion = await _validador.ValidarRespuestasAsync(dto.EvaDataJson, definicionFormulario);
        if (!validacion.Valido)
        {
            var errores = new List<string>();
            foreach (var error in validacion.Errores)
            {
                errores.Add($"Campo '{error.Campo}': {error.Mensaje}");
            }
            return ServiceResult.BadRequest("Error de validación de respuestas:\n" + string.Join("\n", errores));
        }

        var variables = ExtraerVariablesCalculo(dto.EvaDataJson);
        var calculo = _calculador.CalcularYValidarRiesgo(
            variables.Frecuencia,
            variables.Impacto,
            variables.Preventivo,
            variables.Detectivo,
            variables.Correctivo,
            variables.FrecuenciaResidual,
            variables.ImpactoResidual);

        if (!calculo.Success || calculo.Data is null)
        {
            return ServiceResult.BadRequest(calculo.Message ?? "No fue posible calcular la evaluación.");
        }

        dto.EvaVri = calculo.Data.Vri;
        dto.EvaEtp = calculo.Data.Etp;
        dto.EvaVrr = calculo.Data.Vrr;
        dto.EvaDataCalcJson = JsonSerializer.Serialize(calculo.Data);
        return null;
    }

    private static ServiceResult ResponderVinculo(bool exito) =>
        exito
            ? ServiceResult.Ok("Evidencia vinculada correctamente.")
            : ServiceResult.BadRequest("No se pudo vincular la evidencia.");

    private static string CalcularHashSha256(string contenido)
    {
        using var sha = SHA256.Create();
        return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(contenido))).ToLowerInvariant();
    }

    private static VariablesCalculo ExtraerVariablesCalculo(string respuestas)
    {
        using JsonDocument document = JsonDocument.Parse(respuestas);
        JsonElement root = document.RootElement;
        return new VariablesCalculo(
            LeerEntero(root, "frecuencia_inherente"),
            LeerEntero(root, "impacto_inherente"),
            LeerDecimal(root, "controles_preventivo"),
            LeerDecimal(root, "controles_detectivo"),
            LeerDecimal(root, "controles_correctivo"),
            LeerEntero(root, "frecuencia_residual"),
            LeerEntero(root, "impacto_residual"));
    }

    private static int LeerEntero(JsonElement root, string propiedad)
    {
        if (!root.TryGetProperty(propiedad, out JsonElement value)) return 0;
        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out int numero)) return numero;
        return value.ValueKind == JsonValueKind.String && int.TryParse(value.GetString(), out numero) ? numero : 0;
    }

    private static decimal LeerDecimal(JsonElement root, string propiedad)
    {
        if (!root.TryGetProperty(propiedad, out JsonElement value)) return 0m;
        if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out decimal numero)) return numero;
        return value.ValueKind == JsonValueKind.String && decimal.TryParse(value.GetString(), out numero) ? numero : 0m;
    }

    private sealed record VariablesCalculo(
        int Frecuencia,
        int Impacto,
        decimal Preventivo,
        decimal Detectivo,
        decimal Correctivo,
        int FrecuenciaResidual,
        int ImpactoResidual);
}
