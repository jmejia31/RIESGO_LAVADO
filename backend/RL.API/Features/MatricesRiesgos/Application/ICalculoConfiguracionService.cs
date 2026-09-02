using RL.API.Features.MatricesRiesgos.Contracts;
using RL.API.Shared.Results;

namespace RL.API.Features.MatricesRiesgos.Application;

public interface ICalculoConfiguracionService
{
    Task<ServiceResult<IReadOnlyList<FormulaDto>>> ListarFormulasAsync(bool incluirInactivas);
    Task<ServiceResult<FormulaDto>> ObtenerFormulaAsync(long id);
    Task<ServiceResult<long>> CrearFormulaAsync(CrearFormulaDto dto, long usuarioId, string? ip);
    Task<ServiceResult<long>> CrearFormulaVersionAsync(long id, CrearFormulaVersionDto dto, long usuarioId, string? ip);
    Task<ServiceResult> ActualizarFormulaBorradorAsync(long id, ActualizarFormulaBorradorDto dto, long usuarioId, string? ip);
    Task<ServiceResult<IReadOnlyList<FormulaVersionDto>>> ListarFormulaVersionesAsync(long id);
    Task<ServiceResult<IReadOnlyList<FormulaUsageDto>>> ListarFormulaUsagesAsync(long id);
    Task<ServiceResult> CrearFormulaUsoAsync(CrearFormulaUsoDto dto, long usuarioId, string? ip);
    Task<ServiceResult> ReemplazarFormulaUsosAsync(long versionFormularioId, ReemplazarFormulaUsosDto dto, long usuarioId, string? ip);
    Task<ServiceResult> CambiarEstadoFormulaAsync(long id, CambiarEstadoConfiguracionDto dto, long usuarioId, string? ip);

    Task<ServiceResult<IReadOnlyList<FuncionDto>>> ListarFuncionesAsync(bool incluirInactivas);
    Task<ServiceResult<FuncionDto>> ObtenerFuncionAsync(long id);
    Task<ServiceResult<long>> CrearFuncionAsync(CrearFuncionDto dto, long usuarioId, string? ip);
    Task<ServiceResult<long>> CrearFuncionVersionAsync(long id, CrearFuncionVersionDto dto, long usuarioId, string? ip);
    Task<ServiceResult> ActualizarFuncionBorradorAsync(long id, ActualizarFuncionBorradorDto dto, long usuarioId, string? ip);
    Task<ServiceResult> CambiarEstadoFuncionVersionAsync(long id, CambiarEstadoConfiguracionDto dto, long usuarioId, string? ip);
    Task<ServiceResult<IReadOnlyList<FuncionVersionDto>>> ListarFuncionVersionesAsync(long id);
    Task<ServiceResult<IReadOnlyList<FuncionArgumentoDto>>> ListarFuncionArgumentosAsync(long id);

    Task<ServiceResult<IReadOnlyList<ParametroDto>>> ListarParametrosAsync(bool incluirInactivos);
    Task<ServiceResult<ParametroDto>> ObtenerParametroAsync(long id);
    Task<ServiceResult<long>> CrearParametroAsync(CrearParametroDto dto, long usuarioId, string? ip);
    Task<ServiceResult<long>> CrearParametroVersionAsync(long id, CrearParametroVersionDto dto, long usuarioId, string? ip);
    Task<ServiceResult> ActualizarParametroBorradorAsync(long id, ActualizarParametroBorradorDto dto, long usuarioId, string? ip);
    Task<ServiceResult> CambiarEstadoParametroVersionAsync(long id, CambiarEstadoConfiguracionDto dto, long usuarioId, string? ip);
    Task<ServiceResult<IReadOnlyList<ParametroVersionDto>>> ListarParametroVersionesAsync(long id);
}
