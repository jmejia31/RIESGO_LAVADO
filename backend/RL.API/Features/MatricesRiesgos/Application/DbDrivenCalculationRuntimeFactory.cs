using RL.API.Features.Catalogos.Contracts;
using RL.API.Features.MatricesRiesgos.Contracts;
using RL.API.Features.MatricesRiesgos.Domain;
using RL.API.Features.MatricesRiesgos.Persistence;
using System.Text.RegularExpressions;

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

        CalculationConfigurationSnapshotDto snapshot = await _configuration.ObtenerSnapshotRuntimeAsync();
        var registry = new DbDrivenFunctionRegistry(snapshot.Funciones, snapshot.VersionesFuncion, snapshot.ArgumentosFuncion);
        foreach ((string code, int version) in pinning.FunctionVersions)
            _ = registry.Resolve(code, version, requirePinned: true);
        var parameterResolver = new DbDrivenParameterResolver(snapshot.Parametros, snapshot.VersionesParametro);
        var values = new Dictionary<string, FormulaValue>(StringComparer.OrdinalIgnoreCase);
        foreach (string code in pinning.ParameterVersions.Keys)
            values[code] = parameterResolver.Resolve(code, pinning);

        var snapshots = catalogSnapshots.ToDictionary(snapshot => snapshot.Code, StringComparer.OrdinalIgnoreCase);
        foreach (string code in pinning.CatalogSnapshots.Keys)
        {
            if (!snapshots.TryGetValue(code, out CatalogSnapshot? catalog))
                throw new InvalidOperationException($"Catalog snapshot '{code}' is not available.");
            string expectedHash = pinning.CatalogSnapshots[code];
            if (!Regex.IsMatch(expectedHash, "^[0-9A-Fa-f]{64}$", RegexOptions.CultureInvariant)
                || !CatalogSnapshotHasher.Compute(catalog).Equals(expectedHash, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Catalog snapshot '{code}' does not match its pinned SHA-256 hash.");
        }

        // The workbook's 1..9 risk levels are the institutional display contract.
        // Validate the pinned source above, then project those numeric keys through
        // the canonical manifest instead of the legacy four-band display catalog.
        if (snapshots.ContainsKey("CAT_NIVEL_RIESGO"))
            snapshots["CAT_NIVEL_RIESGO"] = MatrizRiesgosCatalogoCanonico.CrearSnapshotNivelRiesgo();

        return new FormulaRuntimeOptions(
            registry,
            values,
            new CatalogCalculationLookup(snapshots.Values),
            pinning,
            limits);
    }
}
