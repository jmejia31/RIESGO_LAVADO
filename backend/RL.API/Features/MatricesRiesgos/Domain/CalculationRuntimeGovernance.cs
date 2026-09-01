using RL.API.Features.MatricesRiesgos.Contracts;

namespace RL.API.Features.MatricesRiesgos.Domain;

public static class CalculationDependencyGraph
{
    public static DependencyGraphResult Build(FormulaEngine engine, IFunctionRegistry registry, int maxDependencyDepth = 32)
        => Build(engine, registry, null, maxDependencyDepth);

    public static DependencyGraphResult Build(FormulaEngine engine, IFunctionRegistry registry, CalculationPinning? pinning, int maxDependencyDepth = 32)
    {
        var edges = new Dictionary<string, IReadOnlySet<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (string code in registry.FunctionCodes)
        {
            FunctionVersionDefinition definition;
            try { definition = registry.Resolve(code, pinning?.FunctionVersion(code), pinning?.Published == true); }
            catch (FormulaRuntimeException) { continue; }
            if (!definition.IsComposite) continue;
            FormulaExpressionAnalysis analysis;
            try { analysis = engine.AnalyzeExpression(definition.DefinitionDsl!, registry); }
            catch (FormulaRuntimeException ex) { return new(edges, new[] { ex.Message }); }
            var targets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string name in analysis.ReferencedFunctions)
            {
                try { targets.Add(registry.Resolve(name, pinning?.FunctionVersion(name), pinning?.Published == true).Identity); }
                catch (FormulaRuntimeException ex) { return new(edges, new[] { ex.Message }); }
            }
            edges[definition.Identity] = targets;
        }

        var cycles = new List<string>();
        foreach (string node in edges.Keys)
            Visit(node, edges, new HashSet<string>(StringComparer.OrdinalIgnoreCase), new List<string>(), cycles, maxDependencyDepth);
        return new(edges, cycles.Distinct(StringComparer.OrdinalIgnoreCase).ToList());
    }

    private static void Visit(string node, IReadOnlyDictionary<string, IReadOnlySet<string>> edges, HashSet<string> path, List<string> trail, List<string> cycles, int maxDepth)
    {
        if (trail.Count >= maxDepth)
        {
            cycles.Add("DEPENDENCY_DEPTH_LIMIT");
            return;
        }
        if (!path.Add(node))
        {
            int index = trail.FindIndex(item => item.Equals(node, StringComparison.OrdinalIgnoreCase));
            cycles.Add(string.Join(" -> ", trail.Skip(Math.Max(index, 0)).Append(node)));
            return;
        }
        trail.Add(node);
        if (edges.TryGetValue(node, out IReadOnlySet<string>? targets))
            foreach (string target in targets.Where(edges.ContainsKey)) Visit(target, edges, new(path, StringComparer.OrdinalIgnoreCase), new(trail), cycles, maxDepth);
    }
}

public sealed record PublicationValidationResult(bool CanPublish, IReadOnlyList<FormulaDiagnostic> Errors, CalculationPinning? Pinning);

/// <summary>
/// Único punto de validación semántica previo a publicar una configuración.
/// No muta Oracle ni contiene una vía alternativa de publicación.
/// </summary>
public sealed class PublicationGate
{
    private readonly FormulaEngine _engine;

    public PublicationGate(FormulaEngine? engine = null) => _engine = engine ?? new FormulaEngine();

    public PublicationValidationResult Validate(
        FormulaVersionDto formula,
        IFunctionRegistry registry,
        IReadOnlyDictionary<string, ParameterVersionDefinition>? parameters,
        CalculationPinning pinning,
        ICalculationLookup? lookup = null,
        CalculationRuntimeLimits? limits = null,
        IReadOnlySet<string>? knownFields = null)
    {
        var errors = new List<FormulaDiagnostic>();
        if (!pinning.Published)
            errors.Add(new(FormulaErrorCode.FORMULA_ARGUMENT_INVALID, "pinning", "La publicación requiere un snapshot pinneado."));
        if (formula.Estado is not ("DRAFT" or "IN_REVIEW" or "APPROVED"))
            errors.Add(new(FormulaErrorCode.FORMULA_ARGUMENT_INVALID, "state", "Solo una versión no publicada puede pasar por el Publication Gate."));
        if (!System.Text.RegularExpressions.Regex.IsMatch(formula.Hash ?? string.Empty, "^[0-9A-Fa-f]{64}$"))
            errors.Add(new(FormulaErrorCode.FORMULA_ARGUMENT_INVALID, "hash", "La versión de fórmula no tiene un hash SHA-256 válido."));

        FormulaExpressionAnalysis analysis;
        try { analysis = _engine.AnalyzeExpression(formula.Expresion, registry); }
        catch (FormulaRuntimeException ex) { errors.Add(new(ex.Code, "expression", ex.Message)); return new(false, errors, null); }

        errors.AddRange(_engine.ValidateExpression(
            formula.Expresion,
            new FormulaRuntimeOptions(registry, Pinning: pinning, Lookup: lookup, Limits: limits)));

        foreach (string functionName in analysis.ReferencedFunctions)
        {
            int? version = pinning.FunctionVersion(functionName);
            if (!version.HasValue) { errors.Add(new(FormulaErrorCode.FORMULA_ARGUMENT_INVALID, functionName, "Dependencia de función no pinneada.")); continue; }
            try
            {
                FunctionVersionDefinition resolved = registry.Resolve(functionName, version, true);
                if (resolved.IsComposite)
                {
                    errors.AddRange(_engine.ValidateExpression(resolved.DefinitionDsl!, new FormulaRuntimeOptions(registry, Pinning: pinning, Lookup: lookup, Limits: limits)));
                    FormulaExpressionAnalysis compositeAnalysis = _engine.AnalyzeExpression(resolved.DefinitionDsl!, registry);
                    HashSet<string> allowedNames = resolved.Arguments.Select(argument => argument.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);
                    if (parameters is not null) allowedNames.UnionWith(parameters.Keys);
                    foreach (string name in compositeAnalysis.ReferencedNames.Where(name => !allowedNames.Contains(name)))
                        errors.Add(new(FormulaErrorCode.FORMULA_REFERENCE_UNKNOWN, functionName, "Composite function contains an undeclared reference."));
                }
                if (!resolved.State.Equals("PUBLISHED", StringComparison.OrdinalIgnoreCase))
                    errors.Add(new(FormulaErrorCode.FORMULA_ARGUMENT_INVALID, functionName, "La versión de función pinneada no está publicada."));
            }
            catch (FormulaRuntimeException ex) { errors.Add(new(ex.Code, functionName, ex.Message)); }
        }

        foreach (string parameterName in analysis.ReferencedNames)
        {
            if (knownFields?.Contains(parameterName) == true) continue;
            if (parameters is null || !parameters.TryGetValue(parameterName, out ParameterVersionDefinition? parameter))
            {
                errors.Add(new(FormulaErrorCode.FORMULA_REFERENCE_UNKNOWN, parameterName, "Parámetro o campo no registrado."));
                continue;
            }
            if (!parameter.State.Equals("PUBLISHED", StringComparison.OrdinalIgnoreCase))
                errors.Add(new(FormulaErrorCode.FORMULA_ARGUMENT_INVALID, parameterName, "Parameter version is not published."));
            if (!System.Text.RegularExpressions.Regex.IsMatch(parameter.Hash ?? string.Empty, "^[0-9A-Fa-f]{64}$"))
                errors.Add(new(FormulaErrorCode.FORMULA_ARGUMENT_INVALID, parameterName, "Invalid parameter hash."));
            if (!pinning.ParameterVersion(parameterName).HasValue || pinning.ParameterVersion(parameterName) != parameter.Version)
                errors.Add(new(FormulaErrorCode.FORMULA_ARGUMENT_INVALID, parameterName, "Dependencia de parámetro no pinneada."));
        }
        if (analysis.ReferencedFunctions.Contains("LOOKUP", StringComparer.OrdinalIgnoreCase) && pinning.CatalogSnapshots.Count == 0)
            errors.Add(new(FormulaErrorCode.FORMULA_ARGUMENT_INVALID, "LOOKUP", "Catalog dependency is not pinned."));
        if (analysis.ReferencedFunctions.Contains("LOOKUP", StringComparer.OrdinalIgnoreCase) && lookup is null)
            errors.Add(new(FormulaErrorCode.FORMULA_REFERENCE_UNKNOWN, "LOOKUP", "La resolución de catálogos no está disponible."));

        DependencyGraphResult graph = CalculationDependencyGraph.Build(_engine, registry, pinning, limits?.MaxDependencyDepth ?? 32);
        if (!graph.IsValid) errors.Add(new(FormulaErrorCode.FORMULA_CYCLE, "dependencies", "Se detectó un ciclo de dependencias."));
        foreach (string identity in graph.Edges.Values.SelectMany(values => values))
        {
            string code = identity[..identity.LastIndexOf('@')];
            int? version = pinning.FunctionVersion(code);
            if (!version.HasValue || !identity.EndsWith($"@{version.Value}", StringComparison.OrdinalIgnoreCase))
                errors.Add(new(FormulaErrorCode.FORMULA_ARGUMENT_INVALID, code, "Dependencia compuesta no pinneada."));
        }
        if (pinning.Published && pinning.FunctionVersions.Count < analysis.ReferencedFunctions.Count)
            errors.Add(new(FormulaErrorCode.FORMULA_ARGUMENT_INVALID, "pinning", "La publicación requiere fijar todas las funciones utilizadas."));
        if (limits is not null && (limits.MaxFunctionDepth <= 0 || limits.MaxFunctionCalls <= 0 || limits.MaxDependencyDepth <= 0))
            errors.Add(new(FormulaErrorCode.FORMULA_LIMIT_EXCEEDED, "limits", "Los límites runtime deben ser positivos."));
        return new(errors.Count == 0, errors, errors.Count == 0 ? pinning : null);
    }
}
