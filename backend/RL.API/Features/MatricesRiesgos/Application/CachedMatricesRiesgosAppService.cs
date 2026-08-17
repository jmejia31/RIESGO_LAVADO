using RL.API.Features.MatricesRiesgos.Contracts;
using RL.API.Infrastructure.Caching;
using RL.API.Shared.Results;

namespace RL.API.Features.MatricesRiesgos.Application;

/// <summary>
/// Decorador BE-02: cachea exclusivamente lecturas estables de definición/versionado
/// de formularios. Evaluaciones, evidencias, consolidado y demás datos transaccionales
/// permanecen fuera de caché.
/// </summary>
public sealed class CachedMatricesRiesgosAppService : IMatricesRiesgosAppService
{
    private readonly MatricesRiesgosAppService _inner;
    private readonly IApplicationCache _cache;
    private readonly ApplicationCacheSettings _settings;

    public CachedMatricesRiesgosAppService(
        MatricesRiesgosAppService inner,
        IApplicationCache cache,
        ApplicationCacheSettings settings)
    {
        _inner = inner;
        _cache = cache;
        _settings = settings;
    }

    public Task<ServiceResult<VersionFormularioDto>> ObtenerVersionVigenteFormularioAsync(string familiaCodigo) =>
        _cache.GetOrCreateAsync(
            ApplicationCacheScopes.MatricesFormularios,
            $"vigente:{NormalizeFamily(familiaCodigo)}",
            _settings.FormularioVersionTtl,
            () => _inner.ObtenerVersionVigenteFormularioAsync(familiaCodigo),
            static result => result.Success);

    public Task<ServiceResult<VersionFormularioDto>> ObtenerVersionFormularioAsync(long versionId) =>
        _cache.GetOrCreateAsync(
            ApplicationCacheScopes.MatricesFormularios,
            $"version:{versionId}",
            _settings.FormularioVersionTtl,
            () => _inner.ObtenerVersionFormularioAsync(versionId),
            static result => result.Success);

    public async Task<ServiceResult<long>> CrearBorradorFormularioAsync(
        long familiaId,
        string codigoFormulario,
        string jsonConfig,
        long usuarioId)
    {
        ServiceResult<long> result = await _inner.CrearBorradorFormularioAsync(
            familiaId,
            codigoFormulario,
            jsonConfig,
            usuarioId);
        InvalidateIfSuccessful(result.Success);
        return result;
    }

    public async Task<ServiceResult<long>> ClonarVersionFormularioAsync(long versionOrigenId, long usuarioId)
    {
        ServiceResult<long> result = await _inner.ClonarVersionFormularioAsync(versionOrigenId, usuarioId);
        InvalidateIfSuccessful(result.Success);
        return result;
    }

    public async Task<ServiceResult> ActualizarBorradorFormularioAsync(
        long versionId,
        string jsonConfig,
        long usuarioId)
    {
        ServiceResult result = await _inner.ActualizarBorradorFormularioAsync(versionId, jsonConfig, usuarioId);
        InvalidateIfSuccessful(result.Success);
        return result;
    }

    public async Task<ServiceResult> PublicarVersionFormularioAsync(long versionId, long usuarioId)
    {
        ServiceResult result = await _inner.PublicarVersionFormularioAsync(versionId, usuarioId);
        InvalidateIfSuccessful(result.Success);
        return result;
    }

    public async Task<ServiceResult> CambiarEstadoVigenciaFormularioAsync(
        long versionId,
        bool vigente,
        long usuarioId)
    {
        ServiceResult result = await _inner.CambiarEstadoVigenciaFormularioAsync(versionId, vigente, usuarioId);
        InvalidateIfSuccessful(result.Success);
        return result;
    }

    public async Task<ServiceResult> EliminarVersionFormularioAsync(long versionId)
    {
        ServiceResult result = await _inner.EliminarVersionFormularioAsync(versionId);
        InvalidateIfSuccessful(result.Success);
        return result;
    }

    public Task<ServiceResult<List<VersionFormularioDto>>> ListarHistorialVersionesFormularioAsync(string familiaCodigo) =>
        _cache.GetOrCreateAsync(
            ApplicationCacheScopes.MatricesFormularios,
            $"historial:{NormalizeFamily(familiaCodigo)}",
            _settings.FormularioVersionTtl,
            () => _inner.ListarHistorialVersionesFormularioAsync(familiaCodigo),
            static result => result.Success);

    public Task<ServiceResult<List<FamiliaFormularioDto>>> ListarFamiliasFormularioAsync() =>
        _cache.GetOrCreateAsync(
            ApplicationCacheScopes.MatricesFormularios,
            "familias-list",
            _settings.FormularioVersionTtl,
            _inner.ListarFamiliasFormularioAsync,
            static result => result.Success);

    public Task<ServiceResult<FamiliaFormularioDto>> ObtenerFamiliaFormularioPorIdAsync(long famId) =>
        _cache.GetOrCreateAsync(
            ApplicationCacheScopes.MatricesFormularios,
            $"familia-id:{famId}",
            _settings.FormularioVersionTtl,
            () => _inner.ObtenerFamiliaFormularioPorIdAsync(famId),
            static result => result.Success);

    public async Task<ServiceResult<long>> CrearFamiliaFormularioAsync(CrearFamiliaFormularioDto dto)
    {
        ServiceResult<long> result = await _inner.CrearFamiliaFormularioAsync(dto);
        InvalidateIfSuccessful(result.Success);
        return result;
    }

    public async Task<ServiceResult> ActualizarFamiliaFormularioAsync(long famId, ActualizarFamiliaFormularioDto dto)
    {
        ServiceResult result = await _inner.ActualizarFamiliaFormularioAsync(famId, dto);
        InvalidateIfSuccessful(result.Success);
        return result;
    }

    public async Task<ServiceResult> DesactivarFamiliaFormularioAsync(long famId)
    {
        ServiceResult result = await _inner.DesactivarFamiliaFormularioAsync(famId);
        InvalidateIfSuccessful(result.Success);
        return result;
    }

    public Task<ServiceResult<EvaluacionRiesgoDto>> ObtenerEvaluacionAsync(long evaId) =>
        _inner.ObtenerEvaluacionAsync(evaId);

    public Task<ServiceResult<EvaluacionesPaginadasDto>> ListarEvaluacionesPaginadasAsync(ConsultaEvaluacionPaginadaDto filtro) =>
        _inner.ListarEvaluacionesPaginadasAsync(filtro);

    public Task<ServiceResult<long>> CrearEvaluacionAsync(EvaluacionRiesgoDto dto, long usuarioId, string? ip) =>
        _inner.CrearEvaluacionAsync(dto, usuarioId, ip);

    public Task<ServiceResult> ActualizarEvaluacionAsync(EvaluacionRiesgoDto dto, long usuarioId, string? ip) =>
        _inner.ActualizarEvaluacionAsync(dto, usuarioId, ip);

    public Task<ServiceResult> TransicionarEstadoEvaluacionAsync(
        long evaId,
        string nuevoEstado,
        string? motivo,
        long usuarioId,
        string? ip) =>
        _inner.TransicionarEstadoEvaluacionAsync(evaId, nuevoEstado, motivo, usuarioId, ip);

    public Task<ServiceResult<List<FlujoEvaluacionDto>>> ObtenerFlujosEvaluacionAsync(long evaId) =>
        _inner.ObtenerFlujosEvaluacionAsync(evaId);

    public Task<ServiceResult<EvidenciaDto>> CargarArchivoEvidenciaFisicaAsync(IFormFile archivo, long usuarioId) =>
        _inner.CargarArchivoEvidenciaFisicaAsync(archivo, usuarioId);

    public Task<ServiceResult<EvidenciaDto>> ObtenerEvidenciaFisicaAsync(long evidenciaId) =>
        _inner.ObtenerEvidenciaFisicaAsync(evidenciaId);

    public Task<ServiceResult> VincularEvidenciaAsync(VincularEvidenciaDto dto, long usuarioId, string? ip) =>
        _inner.VincularEvidenciaAsync(dto, usuarioId, ip);

    public Task<ServiceResult> EliminarEvidenciaAsync(long evidenciaId, long usuarioId, string? ip) =>
        _inner.EliminarEvidenciaAsync(evidenciaId, usuarioId, ip);

    public Task<ServiceResult<IReadOnlyList<RiesgoReporteFilaDto>>> ObtenerConsolidadoTipadoAsync() =>
        _inner.ObtenerConsolidadoTipadoAsync();

    public Task<ServiceResult<MetodologiaFormularioDto>> ObtenerMetodologiaDinamicaVigenteAsync() =>
        _cache.GetOrCreateAsync(
            ApplicationCacheScopes.MatricesFormularios,
            "metodologia-vigente",
            _settings.FormularioVersionTtl,
            _inner.ObtenerMetodologiaDinamicaVigenteAsync,
            static result => result.Success);

    public Task<ServiceResult<MetodologiaFormularioDto>> ObtenerMetodologiaDinamicaPorVersionAsync(long versionId) =>
        _cache.GetOrCreateAsync(
            ApplicationCacheScopes.MatricesFormularios,
            $"metodologia-version:{versionId}",
            _settings.FormularioVersionTtl,
            () => _inner.ObtenerMetodologiaDinamicaPorVersionAsync(versionId),
            static result => result.Success);

    private void InvalidateIfSuccessful(bool success)
    {
        if (success)
        {
            _cache.Invalidate(ApplicationCacheScopes.MatricesFormularios);
        }
    }

    private static string NormalizeFamily(string familiaCodigo) =>
        (familiaCodigo ?? string.Empty).Trim().ToUpperInvariant();
}
