using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using RL.API.Features.Auditoria.Persistence;
using RL.API.Features.MatricesRiesgos.Contracts;
using RL.API.Features.MatricesRiesgos.Domain;
using RL.API.Features.MatricesRiesgos.Persistence;
using RL.API.Infrastructure.Files;
using RL.API.Shared.Results;

namespace RL.API.Features.MatricesRiesgos.Application;

public sealed class MatricesRiesgosAppService : IMatricesRiesgosAppService
{
    private readonly IMatricesRiesgosRepository _repo;
    private readonly IFormularioValidador _validador;
    private readonly IMatricesRiesgoService _calculador;
    private readonly IAuditoriaRepository _auditoriaRepo;
    private readonly VersionedCalculationRuntimeService _versionedRuntime;
    private readonly IMatricesRiesgosMitigacionRepository _controlsRepository;
    private readonly ILogger<MatricesRiesgosAppService> _logger;

    public MatricesRiesgosAppService(
        IMatricesRiesgosRepository repo,
        IFormularioValidador validador,
        IMatricesRiesgoService calculador,
        IAuditoriaRepository auditoriaRepo,
        VersionedCalculationRuntimeService versionedRuntime,
        IMatricesRiesgosMitigacionRepository controlsRepository,
        ILogger<MatricesRiesgosAppService>? logger = null)
    {
        _repo = repo ?? throw new ArgumentNullException(nameof(repo));
        _validador = validador ?? throw new ArgumentNullException(nameof(validador));
        _calculador = calculador ?? throw new ArgumentNullException(nameof(calculador));
        _auditoriaRepo = auditoriaRepo ?? throw new ArgumentNullException(nameof(auditoriaRepo));
        _versionedRuntime = versionedRuntime ?? throw new ArgumentNullException(nameof(versionedRuntime));
        _controlsRepository = controlsRepository ?? throw new ArgumentNullException(nameof(controlsRepository));
        _logger = logger ?? NullLogger<MatricesRiesgosAppService>.Instance;
    }

    public async Task<ServiceResult<VersionFormularioDto>> ObtenerVersionVigenteFormularioAsync(string? familiaCodigo)
    {
        if (string.IsNullOrWhiteSpace(familiaCodigo))
        {
            FamiliaPredeterminadaDto? predeterminada = await _repo.ObtenerFamiliaPredeterminadaAsync();
            if (predeterminada is null || !predeterminada.Configurada || string.IsNullOrWhiteSpace(predeterminada.FamiliaCodigo))
            {
                return ServiceResult<VersionFormularioDto>.NotFound(
                    "No existe una familia de formulario predeterminada configurada.");
            }

            familiaCodigo = predeterminada.FamiliaCodigo;
        }

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

        // C03: Validar que la familia asociada exista y esté activa
        FamiliaFormularioDto? familia = await _repo.ObtenerFamiliaFormularioPorIdAsync(version.VerFamiliaId);
        if (familia is null)
        {
            return ServiceResult.BadRequest($"La familia de formulario asociada ID {version.VerFamiliaId} no existe.");
        }

        if (!familia.FamActivo)
        {
            return ServiceResult.BadRequest($"No se puede publicar una versión en la familia '{familia.FamCodigo}' porque se encuentra inactiva.");
        }

        FormularioDefinitionValidationResult definicion =
            await _validador.ValidarDefinicionPublicableAsync(version.VerJson);
        if (!definicion.Valido)
        {
            return ServiceResult.BadRequest(
                $"La definición no puede publicarse porque no cumple el contrato del formulario: {string.Join("; ", definicion.Errores.ConvertAll(e => e.Mensaje))}");
        }

        {
            IReadOnlyList<FormulaDiagnostic> runtimeErrors = await _versionedRuntime.ValidateForPublicationAsync(versionId, version.VerJson);
            if (runtimeErrors.Count > 0)
                return ServiceResult.BadRequest("La versión no puede publicarse porque el runtime gobernado está incompleto: " +
                    string.Join("; ", runtimeErrors.Select(error => $"{error.Field}: {error.Code}")));
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
        // C04: Validar existencia y estado PUBLISHED antes de delegar al repositorio
        VersionFormularioDto? version = await _repo.ObtenerVersionFormularioAsync(versionId);
        if (version is null)
        {
            return ServiceResult.NotFound($"No se encontró la versión de formulario con ID {versionId}.");
        }

        if (!version.VerEstado.Equals("PUBLISHED", StringComparison.OrdinalIgnoreCase))
        {
            return ServiceResult.BadRequest("Solo las versiones en estado PUBLISHED pueden cambiar su estado de vigencia.");
        }

        if (!vigente && version.VerVigente)
        {
            FamiliaFormularioDto? familia = await _repo.ObtenerFamiliaFormularioPorIdAsync(version.VerFamiliaId);
            if (familia?.FamPredeterminada == true && familia.TieneVersionVigente)
            {
                return ServiceResult.BadRequest(
                    "No se puede retirar la única versión publicada y vigente de la familia predeterminada.");
            }
        }

        bool actualizado = await _repo.CambiarEstadoVigenciaFormularioAsync(versionId, vigente, usuarioId);
        return actualizado
            ? ServiceResult.Ok("Vigencia actualizada correctamente.")
            : ServiceResult.BadRequest("No se pudo actualizar la vigencia de la versión especificada.");
    }

    public async Task<ServiceResult> EliminarVersionFormularioAsync(long versionId)
    {
        VersionFormularioDto? v = await _repo.ObtenerVersionFormularioAsync(versionId);
        if (v is null)
        {
            return ServiceResult.NotFound($"No se encontró el formulario con ID {versionId}.");
        }

        // C01: Solo DRAFT no vigente puede eliminarse
        if (!v.VerEstado.Equals("DRAFT", StringComparison.OrdinalIgnoreCase) || v.VerVigente)
        {
            return ServiceResult.BadRequest("Las versiones publicadas forman parte del historial y no pueden eliminarse. Solo los borradores (DRAFT) no vigentes pueden eliminarse.");
        }

        bool eliminado = await _repo.EliminarVersionFormularioAsync(versionId);
        return eliminado
            ? ServiceResult.Ok("Formulario eliminado correctamente.")
            : ServiceResult.BadRequest("No se pudo eliminar el formulario. Verifique que sea un borrador (DRAFT) no vigente.");
    }

    public async Task<ServiceResult<List<VersionFormularioDto>>> ListarHistorialVersionesFormularioAsync(string familiaCodigo)
    {
        List<VersionFormularioDto> versiones = await _repo.ListarHistorialVersionesFormularioAsync(familiaCodigo);
        return ServiceResult<List<VersionFormularioDto>>.Ok(versiones);
    }

    public async Task<ServiceResult<FamiliasFormularioPaginadasDto>> ListarFamiliasFormularioPaginadasAsync(ConsultaFamiliasFormularioPaginadaDto filtro)
    {
        filtro ??= new ConsultaFamiliasFormularioPaginadaDto();
        var resultado = await _repo.ListarFamiliasFormularioPaginadasAsync(filtro);
        return ServiceResult<FamiliasFormularioPaginadasDto>.Ok(resultado);
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
            return ServiceResult<long>.Conflict($"Ya existe una familia de formulario registrada con el código '{codigoNormalizado}'.");
        }

        long famId = await _repo.CrearFamiliaFormularioAsync(
            codigoNormalizado,
            nombreNormalizado,
            dto.FamDescripcion?.Trim(),
            famActivo: true);

        return ServiceResult<long>.Ok(famId, "Familia de formulario creada exitosamente.");
    }

    //Actualizar Familia de Formulario
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
        //Verificar que la familia exista
        FamiliaFormularioDto? existente = await _repo.ObtenerFamiliaFormularioPorIdAsync(famId);
        if (existente is null)
        {
            return ServiceResult.NotFound($"No se encontró la familia de formulario con ID {famId}.");
        }
//Validar que el nombre no sea nulo o vacio
        string nombreNormalizado = (dto.FamNombre ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(nombreNormalizado))
        {
            return ServiceResult.BadRequest("El nombre de la familia es obligatorio.");
        }
        //Validar que si se esta desactivando la familia, no sea la predeterminada
        if (!dto.FamActivo && existente.FamPredeterminada)
        {
            return ServiceResult.BadRequest(
                "Esta familia es actualmente la predeterminada. Establezca otra familia como predeterminada antes de desactivarla.");
        }
        //Validar que si se esta desactivando la familia, no tenga una version publicada vigente

        if (!dto.FamActivo && existente.TieneVersionVigente)
        {
            return ServiceResult.BadRequest("No se puede desactivar la familia mientras posea una versión publicada vigente.");
        }
        //Actualizar la familia
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

        if (existente.FamPredeterminada)
        {
            return ServiceResult.BadRequest(
                "Esta familia es actualmente la predeterminada. Establezca otra familia como predeterminada antes de desactivarla.");
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

    public async Task<ServiceResult<EvaluacionesPaginadasDto>> ListarEvaluacionesPaginadasAsync(
        ConsultaEvaluacionPaginadaDto filtro)
    {
        EvaluacionesPaginadasDto evaluaciones = await _repo.ListarEvaluacionesPaginadasAsync(filtro);
        return ServiceResult<EvaluacionesPaginadasDto>.Ok(evaluaciones);
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

        ServiceResult? validacion = await ValidarYCalcularEvaluacionAsync(
            dto, version.VerJson, version.VerId, CrearContextoPresenciaControles(Array.Empty<ControlRiesgoDto>()));
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
        EvaluacionRiesgoDto? evaluacionPersistida = await _repo.ObtenerEvaluacionAsync(dto.EvaId);
        if (evaluacionPersistida is null)
        {
            return ServiceResult.NotFound($"No se encontró la evaluación con ID {dto.EvaId}.");
        }

        if (dto.EvaVersionId != evaluacionPersistida.EvaVersionId)
        {
            return ServiceResult.BadRequest($"El EvaVersionId recibido ({dto.EvaVersionId}) no coincide con el persistido en la evaluación ({evaluacionPersistida.EvaVersionId}).");
        }

        if (!string.Equals(evaluacionPersistida.EvaEstado, "BORRADOR", StringComparison.OrdinalIgnoreCase))
        {
            return ServiceResult.BadRequest($"No se permite modificar una evaluación en estado '{evaluacionPersistida.EvaEstado}'. Solo se permite editar evaluaciones en estado BORRADOR.");
        }

        VersionFormularioDto? version = await _repo.ObtenerVersionFormularioAsync(evaluacionPersistida.EvaVersionId);
        if (version is null)
        {
            return ServiceResult.BadRequest($"La versión de formulario ID {evaluacionPersistida.EvaVersionId} asociada a la evaluación no existe.");
        }

        if (!PrepararPayloadHistorico(dto, evaluacionPersistida.EvaDataJson, version.VerJson,
                out string respuestasFormularioJson, out string respuestasPreservadasJson, out string? errorPayload))
        {
            return ServiceResult.BadRequest(errorPayload ?? "El payload contiene propiedades no permitidas.");
        }

        dto.EvaDataJson = respuestasPreservadasJson;
        ServiceResult? normalizacion = NormalizarRespuestaRiesgo(dto);
        if (normalizacion is not null) return normalizacion;
        respuestasPreservadasJson = dto.EvaDataJson;

        string payloadOriginal = dto.EvaDataJson;
        dto.EvaDataJson = respuestasFormularioJson;
        IReadOnlyDictionary<string, bool> controlPresence =
            CrearContextoPresenciaControles(await _controlsRepository.ListarControlesAsync(dto.EvaId));
        ServiceResult? validacion = await ValidarYCalcularEvaluacionAsync(dto, version.VerJson, version.VerId, controlPresence);
        if (validacion is not null)
        {
            dto.EvaDataJson = payloadOriginal;
            return validacion;
        }

        dto.EvaDataJson = respuestasPreservadasJson;

        try
        {
            bool actualizado = await _repo.ActualizarEvaluacionAsync(dto, usuarioId, ip);
            return actualizado
                ? ServiceResult.Ok("Evaluación actualizada y calculada exitosamente.")
                : ServiceResult.NotFound($"No se encontró la evaluación con ID {dto.EvaId}.");
        }
        catch (DBConcurrencyException)
        {
            return new ServiceResult(false, "La evaluación fue modificada por otra operación. Recargue e intente nuevamente.", 409);
        }
        catch (InvalidOperationException ex)
        {
            return ServiceResult.BadRequest(ex.Message);
        }
    }

    public async Task<ServiceResult<long>> GuardarBorradorEvaluacionAsync(
        EvaluacionRiesgoDto dto,
        long usuarioId,
        string? ip)
    {
        VersionFormularioDto? version = await _repo.ObtenerVersionFormularioAsync(dto.EvaVersionId);
        if (version is null || !version.VerEstado.Equals("PUBLISHED", StringComparison.OrdinalIgnoreCase) || !version.VerVigente)
        {
            return ServiceResult<long>.BadRequest("El borrador debe originarse en una versión publicada y vigente.");
        }

        FormularioValidationResult validacion = await _validador.ValidarRespuestasAsync(
            dto.EvaDataJson,
            version.VerJson,
            permitirObligatoriosPendientes: true);
        if (!validacion.Valido)
        {
            return ServiceResult<long>.BadRequest(FormatearErroresValidacion(validacion));
        }

        if (validacion.ObligatoriosPendientes.Count == 0)
        {
            ServiceResult? calculo = await ValidarYCalcularEvaluacionAsync(
                dto,
                version.VerJson,
                version.VerId,
                CrearContextoPresenciaControles(Array.Empty<ControlRiesgoDto>()));
            if (calculo is not null)
            {
                return new ServiceResult<long>(false, null, calculo.Message, calculo.StatusCode);
            }
        }
        else
        {
            dto.EvaDataCalcJson = "{}";
            dto.EvaVri = null;
            dto.EvaEtp = null;
            dto.EvaVrr = null;
        }

        try
        {
            long id = await _repo.CrearEvaluacionAsync(dto, usuarioId, ip);
            return ServiceResult<long>.Ok(id, "Borrador de evaluación guardado.");
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

    public async Task<ServiceResult> ActualizarBorradorEvaluacionAsync(
        EvaluacionRiesgoDto dto,
        long usuarioId,
        string? ip)
    {
        EvaluacionRiesgoDto? persisted = await _repo.ObtenerEvaluacionAsync(dto.EvaId);
        if (persisted is null) return ServiceResult.NotFound($"No se encontró la evaluación con ID {dto.EvaId}.");
        if (!persisted.EvaEstado.Equals("BORRADOR", StringComparison.OrdinalIgnoreCase))
            return ServiceResult.BadRequest("Solo se pueden guardar borradores mientras la evaluación está en estado BORRADOR.");
        if (dto.EvaVersionId != persisted.EvaVersionId)
            return ServiceResult.BadRequest("La versión del formulario no puede cambiar durante la edición del borrador.");
        if (dto.EvaRiesgoId != persisted.EvaRiesgoId)
            return ServiceResult.BadRequest("El riesgo asociado no puede cambiar durante la edición del borrador.");

        if (dto.EvaVersionRow != persisted.EvaVersionRow)
            return new ServiceResult(false, "La evaluación fue modificada por otra sesión. Recargue los datos antes de continuar.", 409);

        dto.EvaEstado = persisted.EvaEstado;
        dto.EvaActivo = persisted.EvaActivo;
        dto.EvaFechaEval = persisted.EvaFechaEval;
        dto.EvaUsrEval = persisted.EvaUsrEval;

        VersionFormularioDto? version = await _repo.ObtenerVersionFormularioAsync(persisted.EvaVersionId);
        if (version is null)
            return ServiceResult.BadRequest("La versión del formulario asociada al borrador ya no está disponible.");

        if (!PrepararPayloadHistorico(dto, persisted.EvaDataJson, version.VerJson,
                out string respuestasFormularioJson, out string respuestasPreservadasJson, out string? errorPayload))
            return ServiceResult.BadRequest(errorPayload ?? "El payload contiene propiedades no permitidas.");

        dto.EvaDataJson = respuestasPreservadasJson;
        ServiceResult? normalizacion = NormalizarRespuestaRiesgo(dto);
        if (normalizacion is not null) return normalizacion;
        respuestasPreservadasJson = dto.EvaDataJson;

        string payloadOriginal = dto.EvaDataJson;
        dto.EvaDataJson = respuestasFormularioJson;

        FormularioValidationResult validacion = await _validador.ValidarRespuestasAsync(
            dto.EvaDataJson,
            version.VerJson,
            permitirObligatoriosPendientes: true);
        if (!validacion.Valido)
        {
            dto.EvaDataJson = payloadOriginal;
            return ServiceResult.BadRequest(FormatearErroresValidacion(validacion));
        }

        if (validacion.ObligatoriosPendientes.Count == 0)
        {
            IReadOnlyDictionary<string, bool> controlPresence =
                CrearContextoPresenciaControles(await _controlsRepository.ListarControlesAsync(dto.EvaId));
            ServiceResult? calculo = await ValidarYCalcularEvaluacionAsync(dto, version.VerJson, version.VerId, controlPresence);
            if (calculo is not null)
            {
                dto.EvaDataJson = payloadOriginal;
                return calculo;
            }
        }
        else
        {
            dto.EvaDataCalcJson = "{}";
            dto.EvaVri = null;
            dto.EvaEtp = null;
            dto.EvaVrr = null;
        }

        dto.EvaDataJson = respuestasPreservadasJson;

        try
        {
            bool updated = await _repo.ActualizarEvaluacionAsync(dto, usuarioId, ip);
            return updated ? ServiceResult.Ok("Borrador actualizado.") : ServiceResult.NotFound($"No se encontró la evaluación con ID {dto.EvaId}.");
        }
        catch (DBConcurrencyException)
        {
            return new ServiceResult(false, "La evaluación fue modificada por otra operación. Recargue e intente nuevamente.", 409);
        }
        catch (InvalidOperationException ex)
        {
            return ServiceResult.BadRequest(ex.Message);
        }
    }

    private static bool PrepararPayloadHistorico(
        EvaluacionRiesgoDto dto,
        string jsonPersistido,
        string definicionFormulario,
        out string respuestasFormularioJson,
        out string respuestasPreservadasJson,
        out string? error)
    {
        respuestasFormularioJson = "{}";
        respuestasPreservadasJson = "{}";
        error = null;

        try
        {
            JsonObject persistidas = JsonNode.Parse(string.IsNullOrWhiteSpace(jsonPersistido) ? "{}" : jsonPersistido) as JsonObject
                ?? throw new JsonException("Las respuestas persistidas deben ser un objeto JSON.");
            JsonObject recibidas = JsonNode.Parse(string.IsNullOrWhiteSpace(dto.EvaDataJson) ? "{}" : dto.EvaDataJson) as JsonObject
                ?? throw new JsonException("Las respuestas recibidas deben ser un objeto JSON.");
            using JsonDocument definicion = JsonDocument.Parse(definicionFormulario);
            HashSet<string> camposFormulario = ObtenerClavesFormulario(definicion.RootElement);
            var formulario = new JsonObject();
            JsonObject combinado = (JsonObject)persistidas.DeepClone();

            foreach ((string clave, JsonNode? valor) in recibidas)
            {
                if (camposFormulario.Contains(clave))
                {
                    formulario[clave] = valor?.DeepClone();
                    string clavePersistida = combinado.Select(prop => prop.Key)
                        .FirstOrDefault(prop => string.Equals(prop, clave, StringComparison.OrdinalIgnoreCase)) ?? clave;
                    combinado[clavePersistida] = valor?.DeepClone();
                    continue;
                }

                KeyValuePair<string, JsonNode?> previa = persistidas.FirstOrDefault(prop =>
                    string.Equals(prop.Key, clave, StringComparison.OrdinalIgnoreCase));
                if (previa.Key is null || !JsonNode.DeepEquals(previa.Value, valor))
                {
                    error = $"La propiedad '{clave}' no está definida como campo editable y no puede agregarse ni modificarse.";
                    return false;
                }
            }

            // Un campo editable omitido por una versión antigua del cliente conserva su valor previo.
            foreach ((string clave, JsonNode? valor) in persistidas)
            {
                if (camposFormulario.Contains(clave) && !recibidas.Any(prop =>
                        string.Equals(prop.Key, clave, StringComparison.OrdinalIgnoreCase)))
                {
                    formulario[clave] = valor?.DeepClone();
                    combinado[clave] = valor?.DeepClone();
                }
            }

            respuestasFormularioJson = formulario.ToJsonString();
            respuestasPreservadasJson = combinado.ToJsonString();
            return true;
        }
        catch (JsonException)
        {
            error = "Las respuestas de la evaluación no contienen objetos JSON válidos.";
            return false;
        }
    }

    private static HashSet<string> ObtenerClavesFormulario(JsonElement definicion)
    {
        var claves = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (definicion.ValueKind != JsonValueKind.Object
            || !definicion.TryGetProperty("secciones", out JsonElement secciones)
            || secciones.ValueKind != JsonValueKind.Array)
            return claves;

        foreach (JsonElement seccion in secciones.EnumerateArray())
        {
            if (seccion.ValueKind != JsonValueKind.Object
                || !seccion.TryGetProperty("campos", out JsonElement campos)
                || campos.ValueKind != JsonValueKind.Array)
                continue;

            foreach (JsonElement campo in campos.EnumerateArray())
            {
                if (campo.ValueKind != JsonValueKind.Object) continue;
                foreach (string nombre in new[] { "clave", "rutaDatos", "identificador", "id" })
                {
                    if (campo.TryGetProperty(nombre, out JsonElement valor)
                        && valor.ValueKind == JsonValueKind.String
                        && !string.IsNullOrWhiteSpace(valor.GetString()))
                        claves.Add(valor.GetString()!);
                }
            }
        }

        return claves;
    }

    private static string FormatearErroresValidacion(FormularioValidationResult validacion)
    {
        var errores = validacion.Errores.Select(error => $"Campo '{error.Campo}': {error.Mensaje}");
        return "Error de validación de respuestas:\n" + string.Join("\n", errores);
    }

    public async Task<ServiceResult<CalculoPreviewEvaluacionDto>> PrevisualizarCalculoEvaluacionAsync(EvaluacionRiesgoDto dto)
    {
        VersionFormularioDto? version = await _repo.ObtenerVersionFormularioAsync(dto.EvaVersionId);
        if (version is null)
            return ServiceResult<CalculoPreviewEvaluacionDto>.BadRequest("La versión seleccionada no existe.");

        if (dto.EvaId > 0)
        {
            EvaluacionRiesgoDto? persisted = await _repo.ObtenerEvaluacionAsync(dto.EvaId);
            if (persisted is null)
                return ServiceResult<CalculoPreviewEvaluacionDto>.NotFound("No se encontró la evaluación para previsualizar el cálculo.");
            if (persisted.EvaVersionId != dto.EvaVersionId || persisted.EvaRiesgoId != dto.EvaRiesgoId)
                return ServiceResult<CalculoPreviewEvaluacionDto>.BadRequest("La identidad o versión de la evaluación no coincide con la persistida.");
        }

        FormularioValidationResult validacion = await _validador.ValidarRespuestasAsync(
            dto.EvaDataJson,
            version.VerJson,
            permitirObligatoriosPendientes: true);
        if (!validacion.Valido)
            return ServiceResult<CalculoPreviewEvaluacionDto>.BadRequest(FormatearErroresValidacion(validacion));

        if (validacion.ObligatoriosPendientes.Count > 0)
        {
            return ServiceResult<CalculoPreviewEvaluacionDto>.Ok(new(
                false, null, null, null, null, validacion.ObligatoriosPendientes));
        }

        IReadOnlyDictionary<string, bool> presence = dto.EvaId > 0
            ? CrearContextoPresenciaControles(await _controlsRepository.ListarControlesAsync(dto.EvaId))
            : CrearContextoPresenciaControles(Array.Empty<ControlRiesgoDto>());
        var copy = new EvaluacionRiesgoDto
        {
            EvaId = dto.EvaId,
            EvaRiesgoId = dto.EvaRiesgoId,
            EvaVersionId = dto.EvaVersionId,
            EvaDataJson = dto.EvaDataJson,
            EvaEstado = "BORRADOR"
        };
        ServiceResult? calculo = await ValidarYCalcularEvaluacionAsync(copy, version.VerJson, version.VerId, presence);
        if (calculo is not null)
            return new ServiceResult<CalculoPreviewEvaluacionDto>(false, null, calculo.Message, calculo.StatusCode);

        return ServiceResult<CalculoPreviewEvaluacionDto>.Ok(new(
            true, copy.EvaDataCalcJson, copy.EvaVri, copy.EvaEtp, copy.EvaVrr, Array.Empty<string>()));
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
        bool permitida = EvaluacionWorkflow.EsTransicionPermitida(actual, nuevo);

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

    public async Task<ServiceResult<TransicionesEvaluacionDto>> ObtenerTransicionesEvaluacionAsync(long evaId)
    {
        EvaluacionRiesgoDto? evaluacion = await _repo.ObtenerEvaluacionAsync(evaId);
        if (evaluacion is null)
            return ServiceResult<TransicionesEvaluacionDto>.NotFound($"No se encontró la evaluación ID {evaId}.");

        string estadoActual = evaluacion.EvaEstado.Trim().ToUpperInvariant();
        return ServiceResult<TransicionesEvaluacionDto>.Ok(new(
            estadoActual,
            EvaluacionWorkflow.ObtenerDestinosPermitidos(estadoActual)));
    }

    public async Task<ServiceResult<EvidenciaDto>> CargarArchivoEvidenciaFisicaAsync(IFormFile archivo, long usuarioId)
    {
        var validationError = FileUploadPolicyValidator.Validate(archivo, FileUploadPolicies.Evidence());
        if (validationError is not null)
            return ServiceResult<EvidenciaDto>.BadRequest(validationError);

        string nombreOriginal = Path.GetFileName(archivo.FileName);
        string extension = Path.GetExtension(nombreOriginal).ToLowerInvariant();
        string uploadsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data", "Evidencias");
        Directory.CreateDirectory(uploadsPath);
        string nombreFisico = $"{Guid.NewGuid()}{extension}";
        string? rutaCompleta = FileUploadPolicyValidator.ResolveSafePath(uploadsPath, nombreFisico);
        if (rutaCompleta is null)
            return ServiceResult<EvidenciaDto>.BadRequest("No se pudo resolver una ruta segura para guardar la evidencia.");

        string rutaRelativa = Path.Combine("App_Data", "Evidencias", nombreFisico);
        var journal = new DurableFileOperationJournal(uploadsPath);
        DurableFileOperationJournal.Lease? operation = null;
        try
        {
            operation = await journal.BeginAsync(
                DurableFileOperationType.MatrixEvidenceUpload,
                nombreFisico,
                rutaRelativa);

            await using (var stream = new FileStream(operation.PendingPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, FileOptions.WriteThrough))
            {
                await archivo.CopyToAsync(stream);
                stream.Flush(flushToDisk: true);
            }

            string hash;
            using (var sha = SHA256.Create())
            await using (var stream = File.OpenRead(operation.PendingPath))
            {
                hash = Convert.ToHexString(await sha.ComputeHashAsync(stream)).ToLowerInvariant();
            }

            var dto = new EvidenciaRegistroDto
            {
                EviNombreArchivo = archivo.FileName,
                EviExtension = extension.TrimStart('.'),
                EviTamano = archivo.Length,
                EviHash = hash,
                EviRuta = rutaRelativa,
                EviUsrCreacion = usuarioId
            };

            long id = await _repo.RegistrarEvidenciaFisicaAsync(dto, usuarioId);
            if (File.Exists(operation.TargetPath))
                throw new IOException("The generated evidence path is already occupied.");
            File.Move(operation.PendingPath, operation.TargetPath);
            await operation.CompleteAsync();

            EvidenciaDto? evidencia = await _repo.ObtenerEvidenciaFisicaAsync(id);
            return evidencia is null
                ? new ServiceResult<EvidenciaDto>(false, null, "La evidencia fue registrada pero no pudo recuperarse.", 500)
                : ServiceResult<EvidenciaDto>.Ok(evidencia, "Evidencia cargada correctamente.");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceError("No se pudo cargar la evidencia física para el usuario {0} ({1}).", usuarioId, ex.GetType().Name);
            // Leave the flushed journal and pending payload for Oracle-backed recovery.
            return new ServiceResult<EvidenciaDto>(false, null, "No se pudo cargar la evidencia.", 500);
        }
        finally
        {
            if (operation is not null)
                await operation.DisposeAsync();
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

    public async Task<ServiceResult> DesvincularEvidenciaAsync(DesvincularEvidenciaDto dto, long usuarioId, string? ip)
    {
        if (dto.EvidenciaId <= 0 || dto.EntidadId <= 0)
            return ServiceResult.BadRequest("La evidencia y la entidad destino son obligatorias.");
        try
        {
            return await _repo.DesvincularEvidenciaAsync(dto, usuarioId, ip)
                ? ServiceResult.Ok("Evidencia desvinculada; el archivo se conserva para otros vínculos o eliminación segura si queda huérfano.")
                : ServiceResult.NotFound("No existe el vínculo de evidencia indicado.");
        }
        catch (InvalidOperationException ex) { return ServiceResult.BadRequest(ex.Message); }
    }

    public async Task<ServiceResult> EliminarEvidenciaAsync(long evidenciaId, long usuarioId, string? ip)
    {
        EvidenciaDto? evidencia = await _repo.ObtenerEvidenciaFisicaAsync(evidenciaId);
        if (evidencia is null)
        {
            return ServiceResult.Ok("La evidencia no existe o ya fue eliminada.");
        }

        DurableFileOperationJournal.Lease? operation = null;
        string raizEvidencias = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data", "Evidencias"));
        string? rutaEnCuarentena = null;
        Func<Task<bool>> prepararEliminacionArchivo = async () =>
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(evidencia.EviRuta))
                {
                    string ruta = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, evidencia.EviRuta));
                    string prefijoRaiz = raizEvidencias.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                    if (!ruta.StartsWith(prefijoRaiz, StringComparison.OrdinalIgnoreCase))
                        return false;

                    string rutaRelativa = Path.GetRelativePath(raizEvidencias, ruta);
                    operation = await new DurableFileOperationJournal(raizEvidencias).BeginAsync(
                        DurableFileOperationType.MatrixEvidenceDelete,
                        rutaRelativa,
                        evidencia.EviRuta,
                        evidenciaId,
                        File.Exists(ruta));
                    rutaEnCuarentena = operation.PendingPath;
                    if (File.Exists(ruta))
                    {
                        File.Move(ruta, rutaEnCuarentena);
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("No se pudo poner en cuarentena la evidencia {0} antes del commit Oracle ({1}).", evidenciaId, ex.GetType().Name);
                return false;
            }
        };

        ResultadoEliminacionEvidencia resultado;
        try
        {
            resultado = await _repo.EliminarEvidenciaSeguraAsync(
                evidenciaId,
                prepararEliminacionArchivo,
                usuarioId,
                ip);
        }
        catch (Exception ex)
        {
            if (operation is not null)
                await operation.DisposeAsync();
            System.Diagnostics.Trace.TraceError("Falló la eliminación de evidencia {0}; el journal durable resolverá el estado Oracle al recuperarse ({1}).", evidenciaId, ex.GetType().Name);
            return new ServiceResult(false, "No se pudo eliminar la evidencia.", 500);
        }

        if (resultado == ResultadoEliminacionEvidencia.Exito && rutaEnCuarentena is not null)
        {
            try
            {
                File.Delete(rutaEnCuarentena);
                rutaEnCuarentena = null;
                if (operation is not null)
                    await operation.CompleteAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("La evidencia {0} se eliminó de Oracle, pero su archivo en cuarentena requiere limpieza ({1}).", evidenciaId, ex.GetType().Name);
            }
        }

        if (operation is not null)
            await operation.DisposeAsync();

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
                    $"Falló la confirmación Oracle al eliminar {evidencia.EviNombreArchivo}; la reconciliación durable resolverá el archivo según el estado persistido.",
                    null,
                    usuarioId,
                    null,
                    ip,
                    "MatricesRiesgos");
                return new ServiceResult(false, "Falló la confirmación Oracle al eliminar la evidencia.", 500);
            default:
                return ServiceResult.BadRequest("Resultado de eliminación desconocido.");
        }
    }

    public async Task<ServiceResult<FamiliaPredeterminadaDto>> ObtenerFamiliaPredeterminadaAsync()
    {
        FamiliaPredeterminadaDto? familia = await _repo.ObtenerFamiliaPredeterminadaAsync();
        return ServiceResult<FamiliaPredeterminadaDto>.Ok(familia ?? new FamiliaPredeterminadaDto());
    }

    public async Task<ServiceResult> EstablecerFamiliaPredeterminadaAsync(long famId, long usuarioId, string? ip)
    {
        if (famId <= 0)
        {
            return ServiceResult.BadRequest("El ID de familia especificado es inválido.");
        }

        ResultadoFamiliaPredeterminada resultado = await _repo.EstablecerFamiliaPredeterminadaAsync(famId, usuarioId, ip);
        return resultado switch
        {
            ResultadoFamiliaPredeterminada.Exito => ServiceResult.Ok("Familia de formulario predeterminada establecida exitosamente."),
            ResultadoFamiliaPredeterminada.NoExiste => ServiceResult.NotFound($"No se encontró la familia de formulario con ID {famId}."),
            ResultadoFamiliaPredeterminada.Conflicto => ServiceResult.Conflict("La familia predeterminada cambió concurrentemente. Intente nuevamente."),
            ResultadoFamiliaPredeterminada.Inactiva or ResultadoFamiliaPredeterminada.SinVersionVigente => ServiceResult.BadRequest(
                "La familia debe estar activa y contar con una versión publicada y vigente antes de establecerla como predeterminada."),
            _ => ServiceResult.BadRequest("No se pudo establecer la familia predeterminada.")
        };
    }

    public async Task<ServiceResult<ReporteMatricesPaginadoDto>> ObtenerConsolidadoPaginadoAsync(FiltroReporteMatricesDto filtro)
    {
        ReporteMatricesPaginadoDto resultado = await _repo.ObtenerConsolidadoPaginadoAsync(filtro);
        return ServiceResult<ReporteMatricesPaginadoDto>.Ok(resultado);
    }

    public async Task<ServiceResult<IReadOnlyList<RiesgoReporteFilaDto>>> ObtenerConsolidadoParaExportacionAsync(FiltroReporteMatricesDto filtro)
    {
        IReadOnlyList<RiesgoReporteFilaDto> filas = await _repo.ObtenerConsolidadoParaExportacionAsync(filtro);
        return ServiceResult<IReadOnlyList<RiesgoReporteFilaDto>>.Ok(filas);
    }

    public async Task<ServiceResult<MetodologiaFormularioDto>> ObtenerMetodologiaDinamicaVigenteAsync()
    {
        FamiliaPredeterminadaDto? predeterminada = await _repo.ObtenerFamiliaPredeterminadaAsync();
        if (predeterminada is null
            || !predeterminada.Configurada
            || !predeterminada.TieneVersionVigente
            || !predeterminada.VersionVigenteId.HasValue)
        {
            return ServiceResult<MetodologiaFormularioDto>.NotFound(
                "No existe una familia de formulario predeterminada con version publicada y vigente.");
        }

        MetodologiaFormularioDto? metodologia = await _repo.ObtenerMetodologiaDinamicaPorVersionAsync(
            predeterminada.VersionVigenteId.Value);
        return metodologia is null
            ? ServiceResult<MetodologiaFormularioDto>.NotFound("No existe una metodología dinámica publicada y vigente.")
            : ServiceResult<MetodologiaFormularioDto>.Ok(metodologia);
    }

    public async Task<ServiceResult<MetodologiaFormularioDto>> ObtenerMetodologiaDinamicaPorVersionAsync(long versionId)
    {
        MetodologiaFormularioDto? metodologia = await _repo.ObtenerMetodologiaDinamicaPorVersionAsync(versionId);
        return metodologia is null
            ? ServiceResult<MetodologiaFormularioDto>.NotFound($"No existe una metodología dinámica para la versión ID {versionId}.")
            : ServiceResult<MetodologiaFormularioDto>.Ok(metodologia);
    }

    private async Task<ServiceResult?> ValidarYCalcularEvaluacionAsync(
        EvaluacionRiesgoDto dto,
        string definicionFormulario,
        long versionFormularioId,
        IReadOnlyDictionary<string, bool>? controlPresence = null)
    {
        ServiceResult? validacionRespuesta = NormalizarRespuestaRiesgo(dto);
        if (validacionRespuesta is not null)
        {
            return validacionRespuesta;
        }

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

        {
            try
            {
                GovernedCalculationResult governed = await _versionedRuntime.CalculateAsync(
                    versionFormularioId, definicionFormulario, dto.EvaDataJson, controlPresence);
                if (governed.IsGoverned)
                {
                    FormulaEvaluationResult calculation = governed.Evaluation!;
                    if (!calculation.Success)
                        return ServiceResult.BadRequest("Error de cálculo gobernado: " + string.Join("; ", calculation.Errors.Select(error => error.Code.ToString())));
                    dto.EvaDataCalcJson = JsonSerializer.Serialize(calculation.Values);
                    dto.EvaVri = ObtenerEnteroCalculado(calculation.Values, "valor_riesgo_inherente", "vri", "vri_calculado");
                    dto.EvaEtp = ObtenerDecimalCalculado(calculation.Values, "efectividad_total_ponderada", "etp", "etp_calculado");
                    dto.EvaVrr = ObtenerEnteroCalculado(calculation.Values, "valor_riesgo_residual", "vrr", "vrr_calculado");
                    return null;
                }
            }
            catch (FormulaRuntimeException ex)
            {
                return ServiceResult.BadRequest("Runtime gobernado inválido: " + ex.Code);
            }
            catch (InvalidOperationException ex)
            {
                return ServiceResult.BadRequest("Runtime gobernado inválido: " + ex.Message);
            }
        }

        // El motor DSL es autoritativo para definiciones inline históricas sin formula usages.
        // El payload de cálculos recibido del cliente nunca participa en este flujo.
        var motor = new FormulaEngine();
        FormulaEvaluationResult evaluacionFormula = motor.Evaluate(definicionFormulario, dto.EvaDataJson);
        if (!evaluacionFormula.Success)
        {
            return ServiceResult.BadRequest(
                "Error de cálculo: " + string.Join("; ", evaluacionFormula.Errors.Select(e => e.Code.ToString())));
        }

        if (evaluacionFormula.Values.Count > 0)
        {
            dto.EvaDataCalcJson = JsonSerializer.Serialize(evaluacionFormula.Values);
            dto.EvaVri = ObtenerEnteroCalculado(evaluacionFormula.Values, "vri", "vri_calculado");
            dto.EvaEtp = ObtenerDecimalCalculado(evaluacionFormula.Values, "etp", "etp_calculado");
            dto.EvaVrr = ObtenerEnteroCalculado(evaluacionFormula.Values, "vrr", "vrr_calculado");
            return null;
        }

        // Compatibilidad temporal para contratos históricos que aún no declaran
        // fórmulas; no se usa cuando el contrato versionado sí las contiene.
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

    private static IReadOnlyDictionary<string, bool> CrearContextoPresenciaControles(IReadOnlyList<ControlRiesgoDto> controls) =>
        new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
        {
            [InstitutionalCalculationContextKeys.PreventiveControl] = controls.Any(control => control.ConTipo.Equals("PREVENTIVO", StringComparison.OrdinalIgnoreCase)),
            [InstitutionalCalculationContextKeys.DetectiveControl] = controls.Any(control => control.ConTipo.Equals("DETECTIVO", StringComparison.OrdinalIgnoreCase)),
            [InstitutionalCalculationContextKeys.CorrectiveControl] = controls.Any(control => control.ConTipo.Equals("CORRECTIVO", StringComparison.OrdinalIgnoreCase))
        };

    private static ServiceResult? NormalizarRespuestaRiesgo(EvaluacionRiesgoDto evaluacion)
    {
        if (string.IsNullOrWhiteSpace(evaluacion.EvaDataJson)) return null;

        try
        {
            using JsonDocument document = JsonDocument.Parse(evaluacion.EvaDataJson);
            if (document.RootElement.ValueKind != JsonValueKind.Object) return null;
            Dictionary<string, JsonElement> properties = document.RootElement.EnumerateObject()
                .ToDictionary(property => property.Name, property => property.Value.Clone(), StringComparer.Ordinal);

            foreach (string propName in new[] { "respuesta_riesgo", "respuestaRiesgo" })
            {
                if (properties.TryGetValue(propName, out JsonElement prop))
                {
                    if (prop.ValueKind == JsonValueKind.Null)
                    {
                        continue;
                    }

                    if (prop.ValueKind != JsonValueKind.String)
                    {
                        return ServiceResult.BadRequest($"El campo '{propName}' debe ser una cadena de texto.");
                    }

                    string valor = prop.GetString() ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(valor))
                    {
                        return ServiceResult.BadRequest($"El campo '{propName}' no puede ser vacío. Valores válidos: EVITAR, MITIGAR, TRANSFERIR, ACEPTAR.");
                    }

                    string canonicalKey;
                    try
                    {
                        canonicalKey = MatrizRiesgosCatalogoCanonico.NormalizarClave("RISK_RESPONSE", valor);
                    }
                    catch (InvalidOperationException)
                    {
                        return ServiceResult.BadRequest($"El valor '{valor}' para '{propName}' no es válido. Los valores canónicos permitidos son: EVITAR, MITIGAR, TRANSFERIR, ACEPTAR.");
                    }
                    properties[propName] = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(canonicalKey));
                }
            }

            evaluacion.EvaDataJson = JsonSerializer.Serialize(properties);
        }
        catch (JsonException)
        {
            return ServiceResult.BadRequest("El JSON de respuestas de evaluación no es válido.");
        }

        return null;
    }

    private static int? ObtenerEnteroCalculado(IReadOnlyDictionary<string, object?> valores, params string[] claves)
    {
        foreach (string clave in claves)
            if (valores.TryGetValue(clave, out object? valor) && valor is not null && int.TryParse(Convert.ToString(valor, System.Globalization.CultureInfo.InvariantCulture), out int entero))
                return entero;
        return null;
    }

    private static decimal? ObtenerDecimalCalculado(IReadOnlyDictionary<string, object?> valores, params string[] claves)
    {
        foreach (string clave in claves)
            if (valores.TryGetValue(clave, out object? valor) && valor is not null && decimal.TryParse(Convert.ToString(valor, System.Globalization.CultureInfo.InvariantCulture), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal numero))
                return numero;
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
