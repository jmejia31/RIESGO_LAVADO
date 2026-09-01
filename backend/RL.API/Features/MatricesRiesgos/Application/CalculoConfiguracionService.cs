using System.Data;
using Oracle.ManagedDataAccess.Client;
using RL.API.Features.MatricesRiesgos.Contracts;
using RL.API.Features.MatricesRiesgos.Domain;
using RL.API.Features.MatricesRiesgos.Persistence;
using RL.API.Shared.Results;

namespace RL.API.Features.MatricesRiesgos.Application;

public sealed class CalculoConfiguracionService : ICalculoConfiguracionService
{
    private static readonly HashSet<string> VersionStates = new(StringComparer.OrdinalIgnoreCase)
    { "DRAFT", "IN_REVIEW", "APPROVED", "PUBLISHED", "RETIRED", "ARCHIVED" };

    private readonly ICalculoConfiguracionRepository _repository;

    public CalculoConfiguracionService(ICalculoConfiguracionRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<ServiceResult<IReadOnlyList<FormulaDto>>> ListarFormulasAsync(bool incluirInactivas) =>
        ServiceResult<IReadOnlyList<FormulaDto>>.Ok(await _repository.ListarFormulasAsync(incluirInactivas));

    public async Task<ServiceResult<FormulaDto>> ObtenerFormulaAsync(long id) => id <= 0
        ? ServiceResult<FormulaDto>.BadRequest("El ID de fórmula es inválido.")
        : await Found(_repository.ObtenerFormulaAsync(id), "No se encontró la fórmula.");

    public async Task<ServiceResult<long>> CrearFormulaAsync(CrearFormulaDto dto, long usuarioId, string? ip)
    {
        try
        {
            dto.Codigo = CalculoConfiguracionValidation.NormalizeCode(dto.Codigo, "Código");
            CalculoConfiguracionValidation.ValidateFormulaVersion(dto.VersionInicial.Expresion, dto.VersionInicial.TipoResultado);
            return ServiceResult<long>.Ok(await _repository.CrearFormulaAsync(dto, usuarioId, ip));
        }
        catch (Exception ex) { return Failure<long>(ex); }
    }

    public async Task<ServiceResult<long>> CrearFormulaVersionAsync(long id, CrearFormulaVersionDto dto, long usuarioId, string? ip)
    {
        if (id <= 0) return ServiceResult<long>.BadRequest("El ID de fórmula es inválido.");
        try { CalculoConfiguracionValidation.ValidateFormulaVersion(dto.Expresion, dto.TipoResultado); return ServiceResult<long>.Ok(await _repository.CrearFormulaVersionAsync(id, dto, usuarioId, ip)); }
        catch (Exception ex) { return Failure<long>(ex); }
    }

    public async Task<ServiceResult> ActualizarFormulaBorradorAsync(long id, ActualizarFormulaBorradorDto dto, long usuarioId, string? ip)
    {
        try { CalculoConfiguracionValidation.ValidateFormulaVersion(dto.Expresion, dto.TipoResultado); return await Changed(await _repository.ActualizarFormulaBorradorAsync(id, dto, usuarioId, ip)); }
        catch (Exception ex) { return Failure(ex); }
    }

    public async Task<ServiceResult<IReadOnlyList<FormulaVersionDto>>> ListarFormulaVersionesAsync(long id) => id <= 0
        ? ServiceResult<IReadOnlyList<FormulaVersionDto>>.BadRequest("El ID de fórmula es inválido.")
        : ServiceResult<IReadOnlyList<FormulaVersionDto>>.Ok(await _repository.ListarFormulaVersionesAsync(id));

    public async Task<ServiceResult<IReadOnlyList<FormulaUsageDto>>> ListarFormulaUsagesAsync(long id) => id <= 0
        ? ServiceResult<IReadOnlyList<FormulaUsageDto>>.BadRequest("El ID de fórmula es inválido.")
        : ServiceResult<IReadOnlyList<FormulaUsageDto>>.Ok(await _repository.ListarFormulaUsagesAsync(id));

    public async Task<ServiceResult> CrearFormulaUsoAsync(CrearFormulaUsoDto dto, long usuarioId, string? ip)
    {
        if (dto.VersionFormularioId <= 0 || dto.FormulaVersionId <= 0 || string.IsNullOrWhiteSpace(dto.CampoClave))
            return ServiceResult.BadRequest("La versión de formulario, la versión de fórmula y el campo son obligatorios.");
        try { return await Changed(await _repository.CrearFormulaUsoAsync(dto, usuarioId, ip)); }
        catch (Exception ex) { return Failure(ex); }
    }

    public async Task<ServiceResult> CambiarEstadoFormulaAsync(long id, CambiarEstadoConfiguracionDto dto, long usuarioId, string? ip)
    {
        if (id <= 0 || !IsMasterState(dto.Estado)) return ServiceResult.BadRequest("Estado de fórmula inválido.");
        try { return await Changed(await _repository.CambiarEstadoFormulaAsync(id, dto.Estado.Trim().ToUpperInvariant(), dto.VersionRow, usuarioId, ip)); }
        catch (Exception ex) { return Failure(ex); }
    }

    public async Task<ServiceResult<IReadOnlyList<FuncionDto>>> ListarFuncionesAsync(bool incluirInactivas) =>
        ServiceResult<IReadOnlyList<FuncionDto>>.Ok(await _repository.ListarFuncionesAsync(incluirInactivas));

    public async Task<ServiceResult<FuncionDto>> ObtenerFuncionAsync(long id) => id <= 0
        ? ServiceResult<FuncionDto>.BadRequest("El ID de función es inválido.")
        : await Found(_repository.ObtenerFuncionAsync(id), "No se encontró la función.");

    public async Task<ServiceResult<long>> CrearFuncionAsync(CrearFuncionDto dto, long usuarioId, string? ip)
    {
        try { dto.Codigo = CalculoConfiguracionValidation.NormalizeCode(dto.Codigo, "Código"); CalculoConfiguracionValidation.ValidateFunctionVersion(dto.VersionInicial); return ServiceResult<long>.Ok(await _repository.CrearFuncionAsync(dto, usuarioId, ip)); }
        catch (Exception ex) { return Failure<long>(ex); }
    }

    public async Task<ServiceResult<long>> CrearFuncionVersionAsync(long id, CrearFuncionVersionDto dto, long usuarioId, string? ip)
    {
        if (id <= 0) return ServiceResult<long>.BadRequest("El ID de función es inválido.");
        try { CalculoConfiguracionValidation.ValidateFunctionVersion(dto); return ServiceResult<long>.Ok(await _repository.CrearFuncionVersionAsync(id, dto, usuarioId, ip)); }
        catch (Exception ex) { return Failure<long>(ex); }
    }

    public async Task<ServiceResult> ActualizarFuncionBorradorAsync(long id, ActualizarFuncionBorradorDto dto, long usuarioId, string? ip)
    {
        try { CalculoConfiguracionValidation.ValidateFunctionVersion(dto); return await Changed(await _repository.ActualizarFuncionBorradorAsync(id, dto, usuarioId, ip)); }
        catch (Exception ex) { return Failure(ex); }
    }

    public async Task<ServiceResult> CambiarEstadoFuncionVersionAsync(long id, CambiarEstadoConfiguracionDto dto, long usuarioId, string? ip) =>
        await ChangeVersion(id, dto, (version, state, row) => _repository.CambiarEstadoFuncionVersionAsync(version, new CambiarEstadoConfiguracionDto { Estado = state, VersionRow = row }, usuarioId, ip));

    public async Task<ServiceResult<IReadOnlyList<FuncionVersionDto>>> ListarFuncionVersionesAsync(long id) => id <= 0
        ? ServiceResult<IReadOnlyList<FuncionVersionDto>>.BadRequest("El ID de función es inválido.")
        : ServiceResult<IReadOnlyList<FuncionVersionDto>>.Ok(await _repository.ListarFuncionVersionesAsync(id));

    public async Task<ServiceResult<IReadOnlyList<FuncionArgumentoDto>>> ListarFuncionArgumentosAsync(long id) => id <= 0
        ? ServiceResult<IReadOnlyList<FuncionArgumentoDto>>.BadRequest("El ID de versión es inválido.")
        : ServiceResult<IReadOnlyList<FuncionArgumentoDto>>.Ok(await _repository.ListarFuncionArgumentosAsync(id));

    public async Task<ServiceResult<IReadOnlyList<ParametroDto>>> ListarParametrosAsync(bool incluirInactivas) =>
        ServiceResult<IReadOnlyList<ParametroDto>>.Ok(await _repository.ListarParametrosAsync(incluirInactivas));

    public async Task<ServiceResult<ParametroDto>> ObtenerParametroAsync(long id) => id <= 0
        ? ServiceResult<ParametroDto>.BadRequest("El ID de parámetro es inválido.")
        : await Found(_repository.ObtenerParametroAsync(id), "No se encontró el parámetro.");

    public async Task<ServiceResult<long>> CrearParametroAsync(CrearParametroDto dto, long usuarioId, string? ip)
    {
        try { dto.Codigo = CalculoConfiguracionValidation.NormalizeCode(dto.Codigo, "Código"); CalculoConfiguracionValidation.ValidateParameterVersion(dto.VersionInicial); return ServiceResult<long>.Ok(await _repository.CrearParametroAsync(dto, usuarioId, ip)); }
        catch (Exception ex) { return Failure<long>(ex); }
    }

    public async Task<ServiceResult<long>> CrearParametroVersionAsync(long id, CrearParametroVersionDto dto, long usuarioId, string? ip)
    {
        if (id <= 0) return ServiceResult<long>.BadRequest("El ID de parámetro es inválido.");
        try { CalculoConfiguracionValidation.ValidateParameterVersion(dto); return ServiceResult<long>.Ok(await _repository.CrearParametroVersionAsync(id, dto, usuarioId, ip)); }
        catch (Exception ex) { return Failure<long>(ex); }
    }

    public async Task<ServiceResult> ActualizarParametroBorradorAsync(long id, ActualizarParametroBorradorDto dto, long usuarioId, string? ip)
    {
        try { CalculoConfiguracionValidation.ValidateParameterVersion(dto); return await Changed(await _repository.ActualizarParametroBorradorAsync(id, dto, usuarioId, ip)); }
        catch (Exception ex) { return Failure(ex); }
    }

    public async Task<ServiceResult> CambiarEstadoParametroVersionAsync(long id, CambiarEstadoConfiguracionDto dto, long usuarioId, string? ip) =>
        await ChangeVersion(id, dto, (version, state, row) => _repository.CambiarEstadoParametroVersionAsync(version, new CambiarEstadoConfiguracionDto { Estado = state, VersionRow = row }, usuarioId, ip));

    public async Task<ServiceResult<IReadOnlyList<ParametroVersionDto>>> ListarParametroVersionesAsync(long id) => id <= 0
        ? ServiceResult<IReadOnlyList<ParametroVersionDto>>.BadRequest("El ID de parámetro es inválido.")
        : ServiceResult<IReadOnlyList<ParametroVersionDto>>.Ok(await _repository.ListarParametroVersionesAsync(id));

    private static bool IsMasterState(string? state) => state?.Trim().ToUpperInvariant() is "ACTIVE" or "INACTIVE" or "RETIRED";
    private static async Task<ServiceResult> ChangeVersion(long id, CambiarEstadoConfiguracionDto dto, Func<long, string, int, Task<bool>> change)
    {
        if (dto.Estado?.Trim().Equals("PUBLISHED", StringComparison.OrdinalIgnoreCase) == true)
            return ServiceResult.BadRequest("La publicación requiere validación del Publication Gate único.");
        if (id <= 0 || string.IsNullOrWhiteSpace(dto.Estado) || !VersionStates.Contains(dto.Estado)) return ServiceResult.BadRequest("Estado de versión inválido.");
        try { return await Changed(await change(id, dto.Estado.Trim().ToUpperInvariant(), dto.VersionRow)); }
        catch (Exception ex) { return Failure(ex); }
    }
    private static async Task<ServiceResult<T>> Found<T>(Task<T?> task, string message) where T : class => (await task) is { } item
        ? ServiceResult<T>.Ok(item)
        : ServiceResult<T>.NotFound(message);
    private static Task<ServiceResult> Changed(bool changed) => Task.FromResult(changed ? ServiceResult.Ok() : ServiceResult.Conflict("El recurso no existe, no está en borrador o fue modificado por otra operación."));
    private static ServiceResult Failure(Exception ex) => ex switch
    {
        InvalidOperationException => ServiceResult.BadRequest(ex.Message),
        KeyNotFoundException => ServiceResult.NotFound(ex.Message),
        OracleException oracle when oracle.Number is 1 or 2291 or 2292 => ServiceResult.Conflict("La operación viola una regla de integridad o un recurso ya existe."),
        DBConcurrencyException => ServiceResult.Conflict("El recurso fue modificado por otra operación."),
        _ => throw ex
    };
    private static ServiceResult<T> Failure<T>(Exception ex) => ex switch
    {
        InvalidOperationException => ServiceResult<T>.BadRequest(ex.Message),
        KeyNotFoundException => ServiceResult<T>.NotFound(ex.Message),
        OracleException oracle when oracle.Number is 1 or 2291 or 2292 => ServiceResult<T>.Conflict("La operación viola una regla de integridad o un recurso ya existe."),
        DBConcurrencyException => ServiceResult<T>.Conflict("El recurso fue modificado por otra operación."),
        _ => throw ex
    };
}
