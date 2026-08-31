using RL.API.Features.MatricesRiesgos.Contracts;

namespace RL.API.Features.MatricesRiesgos.Persistence;

public interface ICalculoConfiguracionRepository
{
    Task<IReadOnlyList<FormulaDto>> ListarFormulasAsync(bool incluirInactivas);
    Task<FormulaDto?> ObtenerFormulaAsync(long id);
    Task<long> CrearFormulaAsync(CrearFormulaDto dto, long usuarioId, string? ip);
    Task<long> CrearFormulaVersionAsync(long formulaId, CrearFormulaVersionDto dto, long usuarioId, string? ip);
    Task<bool> ActualizarFormulaBorradorAsync(long versionId, ActualizarFormulaBorradorDto dto, long usuarioId, string? ip);
    Task<bool> CrearFormulaUsoAsync(CrearFormulaUsoDto dto, long usuarioId, string? ip);
    Task<IReadOnlyList<FormulaVersionDto>> ListarFormulaVersionesAsync(long formulaId);
    Task<IReadOnlyList<FormulaUsageDto>> ListarFormulaUsagesAsync(long formulaId);
    Task<bool> CambiarEstadoFormulaAsync(long formulaId, string estado, int versionRow, long usuarioId, string? ip);

    Task<IReadOnlyList<FuncionDto>> ListarFuncionesAsync(bool incluirInactivas);
    Task<FuncionDto?> ObtenerFuncionAsync(long id);
    Task<long> CrearFuncionAsync(CrearFuncionDto dto, long usuarioId, string? ip);
    Task<long> CrearFuncionVersionAsync(long funcionId, CrearFuncionVersionDto dto, long usuarioId, string? ip);
    Task<bool> ActualizarFuncionBorradorAsync(long versionId, ActualizarFuncionBorradorDto dto, long usuarioId, string? ip);
    Task<bool> CambiarEstadoFuncionVersionAsync(long versionId, CambiarEstadoConfiguracionDto dto, long usuarioId, string? ip);
    Task<IReadOnlyList<FuncionVersionDto>> ListarFuncionVersionesAsync(long funcionId);
    Task<IReadOnlyList<FuncionArgumentoDto>> ListarFuncionArgumentosAsync(long versionId);

    Task<IReadOnlyList<ParametroDto>> ListarParametrosAsync(bool incluirInactivos);
    Task<ParametroDto?> ObtenerParametroAsync(long id);
    Task<long> CrearParametroAsync(CrearParametroDto dto, long usuarioId, string? ip);
    Task<long> CrearParametroVersionAsync(long parametroId, CrearParametroVersionDto dto, long usuarioId, string? ip);
    Task<bool> ActualizarParametroBorradorAsync(long versionId, ActualizarParametroBorradorDto dto, long usuarioId, string? ip);
    Task<bool> CambiarEstadoParametroVersionAsync(long versionId, CambiarEstadoConfiguracionDto dto, long usuarioId, string? ip);
    Task<IReadOnlyList<ParametroVersionDto>> ListarParametroVersionesAsync(long parametroId);
}
