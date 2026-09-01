using RL.API.Features.Catalogos.Contracts;
using RL.API.Features.MatricesRiesgos.Contracts;
using RL.API.Features.MatricesRiesgos.Domain;
using RL.API.Features.MatricesRiesgos.Persistence;

namespace RL.API.Features.MatricesRiesgos.Application;

public sealed class DbDrivenCalculationRuntimeFactory
{
    private readonly ICalculoConfiguracionRepository _configuration;

    public DbDrivenCalculationRuntimeFactory(ICalculoConfiguracionRepository configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public async Task<FormulaRuntimeOptions> CreatePublishedAsync(
        CalculationPinning pinning,
        IReadOnlyList<CatalogSnapshot> catalogSnapshots,
        CalculationRuntimeLimits? limits = null)
    {
        if (!pinning.Published) throw new InvalidOperationException("Published runtime requires a published pinning snapshot.");

        IReadOnlyList<FuncionDto> functions = await _configuration.ListarFuncionesAsync(false);
        var versions = new List<FuncionVersionDto>();
        var arguments = new List<FuncionArgumentoDto>();
        foreach (FuncionDto function in functions)
        {
            IReadOnlyList<FuncionVersionDto> functionVersions = await _configuration.ListarFuncionVersionesAsync(function.Id);
            versions.AddRange(functionVersions);
            foreach (FuncionVersionDto version in functionVersions)
                arguments.AddRange(await _configuration.ListarFuncionArgumentosAsync(version.Id));
        }

        var registry = new DbDrivenFunctionRegistry(functions, versions, arguments);
        IReadOnlyList<ParametroDto> parameters = await _configuration.ListarParametrosAsync(false);
        var parameterVersions = new List<ParametroVersionDto>();
        foreach (ParametroDto parameter in parameters)
            parameterVersions.AddRange(await _configuration.ListarParametroVersionesAsync(parameter.Id));

        var parameterResolver = new DbDrivenParameterResolver(parameters, parameterVersions);
        var values = new Dictionary<string, FormulaValue>(StringComparer.OrdinalIgnoreCase);
        foreach (string code in pinning.ParameterVersions.Keys)
            values[code] = parameterResolver.Resolve(code, pinning);

        var snapshots = catalogSnapshots.ToDictionary(snapshot => snapshot.Code, StringComparer.OrdinalIgnoreCase);
        foreach (string code in pinning.CatalogSnapshots.Keys)
            if (!snapshots.ContainsKey(code)) throw new InvalidOperationException($"Catalog snapshot '{code}' is not available.");

        return new FormulaRuntimeOptions(
            registry,
            values,
            new CatalogCalculationLookup(snapshots.Values),
            pinning,
            limits);
    }
}
