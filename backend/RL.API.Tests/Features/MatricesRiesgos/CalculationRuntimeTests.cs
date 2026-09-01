using RL.API.Features.Catalogos.Contracts;
using RL.API.Features.MatricesRiesgos.Contracts;
using RL.API.Features.MatricesRiesgos.Domain;
using Xunit;
using CatalogElement = RL.API.Features.Catalogos.Contracts.ElementoCatalogoMatricesDto;

namespace RL.API.Tests.Features.MatricesRiesgos;

public sealed class CalculationRuntimeTests
{
    [Fact]
    public void NativeRegistry_ExposesTenAllowlistedFunctions()
    {
        Assert.Equal(10, FormulaEngine.SupportedFunctionNames.Count);
        Assert.Equal(10, NativeFunctionCatalog.CreateDefaultDefinitions().Count);
        Assert.Contains("MIN", FormulaEngine.SupportedFunctionNames);
        Assert.Contains("AND", FormulaEngine.SupportedFunctionNames);
        Assert.Contains("LOOKUP", FormulaEngine.SupportedFunctionNames);
    }

    [Theory]
    [InlineData("MIN(5,2)", 2d)]
    [InlineData("MIN(-3,4)", -3d)]
    [InlineData("MIN(0,0)", 0d)]
    public void Min_IsDeterministic(string expression, double expected)
    {
        Assert.Equal(expected, new FormulaEngine().EvaluateExpression(expression).ToObject());
    }

    [Theory]
    [InlineData("AND(TRUE,TRUE)", true)]
    [InlineData("AND(TRUE,FALSE)", false)]
    [InlineData("AND(FALSE,TRUE)", false)]
    [InlineData("AND(FALSE,FALSE)", false)]
    public void And_UsesExplicitBooleanContract(string expression, bool expected)
    {
        Assert.Equal(expected, new FormulaEngine().EvaluateExpression(expression).ToObject());
    }

    [Fact]
    public void Lookup_UsesSemanticCatalogAndRejectsAmbiguity()
    {
        var catalog = new CatalogSnapshot("NIVEL_RIESGO", true,
        [
            new CatalogElement(1, "LOW", "Bajo", 1, true),
            new CatalogElement(2, "HIGH", "Alto", 2, true)
        ]);
        var options = new FormulaRuntimeOptions(
            new InMemoryFunctionRegistry(NativeFunctionCatalog.CreateDefaultDefinitions()),
            Lookup: new CatalogCalculationLookup([catalog]));

        Assert.Equal("Bajo", new FormulaEngine().EvaluateExpression("LOOKUP(\"NIVEL_RIESGO\", \"LOW\")", options: options).ToObject());

        var ambiguous = new CatalogSnapshot("AMBIGUO", true,
        [new CatalogElement(1, "X", "X", 1, true), new CatalogElement(2, "Y", "X", 2, true)]);
        var ambiguousOptions = options with { Lookup = new CatalogCalculationLookup([ambiguous]) };
        var error = Assert.Throws<FormulaRuntimeException>(() => new FormulaEngine().EvaluateExpression("LOOKUP(\"AMBIGUO\", \"X\")", options: ambiguousOptions));
        Assert.Equal(FormulaErrorCode.FORMULA_ARGUMENT_INVALID, error.Code);
    }

    [Fact]
    public void Composite_UsesTheSameParserAndEvaluator()
    {
        var registry = Registry(
            Composite("DOUBLE", 1, "x*2", 1, 1, Arg(1, "X")),
            Composite("CLAMP", 1, "MIN(MAX(x,min),max)", 3, 3, Arg(1, "X"), Arg(2, "MIN"), Arg(3, "MAX")),
            Native("MAX", "MAX_V1", 1, null, "PUBLISHED", Arg(1, "VALUES", variadic: true)),
            Native("MIN", "MIN_V1", 1, null, "PUBLISHED", Arg(1, "VALUES", variadic: true)));
        var engine = new FormulaEngine();

        Assert.Equal(8d, engine.EvaluateExpression("DOUBLE(4)", options: new(registry)).ToObject());
        Assert.Equal(5d, engine.EvaluateExpression("CLAMP(8,1,5)", options: new(registry)).ToObject());
    }

    [Fact]
    public void Composite_CannotReadOuterFieldScope()
    {
        var registry = Registry(Composite("DOUBLE", 1, "x*2+secret", 1, 1, Arg(1, "X")));
        var options = new FormulaRuntimeOptions(registry);

        var error = Assert.Throws<FormulaRuntimeException>(() =>
            new FormulaEngine().EvaluateExpression("DOUBLE(4)", new Dictionary<string, FormulaValue> { ["secret"] = FormulaValue.NumberValue(7) }, options));

        Assert.Equal(FormulaErrorCode.FORMULA_REFERENCE_UNKNOWN, error.Code);
    }

    [Fact]
    public void CompositeCycles_AreRejectedBeforeStackOverflow()
    {
        var registry = Registry(Composite("A", 1, "B(x)", 1, 1, Arg(1, "X")), Composite("B", 1, "A(x)", 1, 1, Arg(1, "X")));
        DependencyGraphResult graph = CalculationDependencyGraph.Build(new FormulaEngine(), registry);

        Assert.False(graph.IsValid);
        Assert.NotEmpty(graph.Cycles);
        var error = Assert.Throws<FormulaRuntimeException>(() => new FormulaEngine().EvaluateExpression("A(1)", options: new(registry)));
        Assert.Equal(FormulaErrorCode.FORMULA_CYCLE, error.Code);
    }

    [Fact]
    public void NativeHandler_IsAllowlistedAndUnknownHandlersFailClosed()
    {
        var unknown = Registry(new FunctionVersionDefinition("EVIL", 1, "NATIVE", "DECIMAL", "ARBITRARY_HANDLER", null, 1, 1, "PUBLISHED", Hash(), [Arg(1, "VALUE")]));
        Assert.Throws<FormulaRuntimeException>(() => unknown.Resolve("EVIL", 1, true));
        Assert.False(NativeFunctionCatalog.IsAllowedHandler("Assembly.Type, Assembly"));
    }

    [Fact]
    public void NativeContract_MustDeclareTheHandlerSignature()
    {
        var malformed = Registry(new FunctionVersionDefinition("MIN", 1, "NATIVE", "DECIMAL", "MIN_V1", null, 1, null, "PUBLISHED", Hash(), []));

        Assert.Throws<FormulaRuntimeException>(() => malformed.Resolve("MIN", 1, true));
    }

    [Fact]
    public void PublishedRuntime_RejectsDraftPinnedFunctionAndRequiresCatalogPin()
    {
        var draft = Registry(new FunctionVersionDefinition("DRAFT_FN", 1, "COMPOSITE", "DECIMAL", null, "x*2", 1, 1, "DRAFT", Hash(), [Arg(1, "X")]));
        var pinning = new CalculationPinning(new Dictionary<string, int> { ["DRAFT_FN"] = 1 }, new Dictionary<string, int>(), published: true);
        var draftError = Assert.Throws<FormulaRuntimeException>(() =>
            new FormulaEngine().EvaluateExpression("DRAFT_FN(2)", options: new FormulaRuntimeOptions(draft, Pinning: pinning)));
        Assert.Equal(FormulaErrorCode.FORMULA_FUNCTION_UNSUPPORTED, draftError.Code);

        var lookupRegistry = Registry(Native("LOOKUP", "LOOKUP_V1", 2, 3, "PUBLISHED", new FunctionArgumentDefinition(1, "CATALOG_CODE", "TEXT", true, false, null), new FunctionArgumentDefinition(2, "INPUT", "VALUE", true, false, null)));
        var lookupPinning = new CalculationPinning(new Dictionary<string, int> { ["LOOKUP"] = 1 }, new Dictionary<string, int>(), published: true);
        var lookup = new CatalogCalculationLookup([new CatalogSnapshot("CAT", true, [new CatalogElement(1, "X", "Result", 1, true)])]);
        var lookupError = Assert.Throws<FormulaRuntimeException>(() =>
            new FormulaEngine().EvaluateExpression("LOOKUP(\"CAT\", \"X\")", options: new FormulaRuntimeOptions(lookupRegistry, Lookup: lookup, Pinning: lookupPinning)));
        Assert.Equal(FormulaErrorCode.FORMULA_ARGUMENT_INVALID, lookupError.Code);
    }

    [Fact]
    public void DbDrivenRegistry_ResolvesPersistedFunctionVersionAndRejectsUnknownCode()
    {
        var functions = new[] { new FuncionDto { Id = 10, Codigo = "DOUBLE", Estado = "ACTIVE" } };
        var versions = new[] { new FuncionVersionDto { Id = 20, FuncionId = 10, Version = 1, Tipo = "COMPOSITE", TipoResultado = "DECIMAL", DefinicionDsl = "x*2", MinArity = 1, MaxArity = 1, Estado = "PUBLISHED", Hash = Hash() } };
        var arguments = new[] { new FuncionArgumentoDto { Id = 30, FuncionVersionId = 20, Posicion = 1, Codigo = "X", Nombre = "X", Tipo = "DECIMAL", Requerido = true } };
        var registry = new DbDrivenFunctionRegistry(functions, versions, arguments);

        Assert.Equal("DOUBLE@1", registry.Resolve("double", 1, true).Identity);
        Assert.Throws<FormulaRuntimeException>(() => registry.Resolve("unknown", 1, true));
    }

    [Fact]
    public void Pinning_UsesPinnedFunctionInsteadOfLatestVersion()
    {
        var registry = Registry(
            Composite("F", 1, "x+1", 1, 1, Arg(1, "X")),
            Composite("F", 2, "x+100", 1, 1, Arg(1, "X")));
        var engine = new FormulaEngine();
        var pinned = new CalculationPinning(new Dictionary<string, int> { ["F"] = 1 }, new Dictionary<string, int>(), published: true);
        var pinnedOptions = new FormulaRuntimeOptions(registry, Pinning: pinned);

        Assert.Equal(3d, engine.EvaluateExpression("F(2)", options: pinnedOptions).ToObject());
        Assert.Equal(102d, engine.EvaluateExpression("F(2)", options: new(registry)).ToObject());
    }

    [Fact]
    public void PublishedFormula_RequiresPinnedDependenciesAtPublicationGate()
    {
        var registry = Registry(Native("MIN", "MIN_V1", 1, null, "PUBLISHED", Arg(1, "VALUES", variadic: true)));
        var formula = new FormulaVersionDto { Id = 1, FormulaId = 1, Version = 1, Expresion = "MIN(5,2)", TipoResultado = "DECIMAL", Estado = "APPROVED", Hash = Hash() };
        var gate = new PublicationGate();
        var rejected = gate.Validate(formula, registry, new Dictionary<string, ParameterVersionDefinition>(), new(new Dictionary<string, int>(), new Dictionary<string, int>(), published: true));
        Assert.False(rejected.CanPublish);
        Assert.Contains(rejected.Errors, error => error.Message.Contains("pinneada", StringComparison.OrdinalIgnoreCase));

        var accepted = gate.Validate(formula, registry, new Dictionary<string, ParameterVersionDefinition>(), new(new Dictionary<string, int> { ["MIN"] = 1 }, new Dictionary<string, int>(), published: true));
        Assert.True(accepted.CanPublish);
    }

    [Fact]
    public void PublicationGate_RejectsUndeclaredCompositeReference()
    {
        var registry = Registry(Composite("BAD", 1, "x+secret", 1, 1, Arg(1, "X")));
        var formula = new FormulaVersionDto { Id = 1, FormulaId = 1, Version = 1, Expresion = "BAD(1)", TipoResultado = "DECIMAL", Estado = "APPROVED", Hash = Hash() };
        var pinning = new CalculationPinning(new Dictionary<string, int> { ["BAD"] = 1 }, new Dictionary<string, int>(), published: true);

        PublicationValidationResult result = new PublicationGate().Validate(formula, registry, new Dictionary<string, ParameterVersionDefinition>(), pinning);

        Assert.False(result.CanPublish);
        Assert.Contains(result.Errors, error => error.Code == FormulaErrorCode.FORMULA_REFERENCE_UNKNOWN);
    }

    [Fact]
    public void RuntimeLimits_RejectExcessiveCompositeDepth()
    {
        var registry = Registry(Composite("A", 1, "B(x)", 1, 1, Arg(1, "X")), Composite("B", 1, "C(x)", 1, 1, Arg(1, "X")), Composite("C", 1, "x+1", 1, 1, Arg(1, "X")));
        var options = new FormulaRuntimeOptions(registry, Limits: new(MaxFunctionDepth: 2, MaxFunctionCalls: 10, MaxDependencyDepth: 2));
        var error = Assert.Throws<FormulaRuntimeException>(() => new FormulaEngine().EvaluateExpression("A(1)", options: options));
        Assert.Equal(FormulaErrorCode.FORMULA_LIMIT_EXCEEDED, error.Code);
    }

    [Fact]
    public void RuntimeLimits_AllowConfiguredBoundaryAndRejectAboveIt()
    {
        var registry = Registry(Composite("A", 1, "x+1", 1, 1, Arg(1, "X")));
        Assert.Equal(3d, new FormulaEngine().EvaluateExpression("A(2)", options: new FormulaRuntimeOptions(registry, Limits: new(MaxFunctionDepth: 1, MaxFunctionCalls: 1, MaxDependencyDepth: 1))).ToObject());

        var error = Assert.Throws<FormulaRuntimeException>(() =>
            new FormulaEngine().EvaluateExpression("A(2)", options: new FormulaRuntimeOptions(registry, Limits: new(MaxFunctionDepth: 0, MaxFunctionCalls: 1, MaxDependencyDepth: 1))));
        Assert.Equal(FormulaErrorCode.FORMULA_LIMIT_EXCEEDED, error.Code);
    }

    [Fact]
    public void RuntimeLimits_EnforceFunctionCallsAndDependencyDepth()
    {
        var functionRegistry = Registry(Composite("A", 1, "x+1", 1, 1, Arg(1, "X")));
        var atCallLimit = new FormulaRuntimeOptions(functionRegistry, Limits: new(MaxFunctionDepth: 1, MaxFunctionCalls: 2, MaxDependencyDepth: 1));
        Assert.Equal(6d, new FormulaEngine().EvaluateExpression("A(2)+A(2)", options: atCallLimit).ToObject());

        var callError = Assert.Throws<FormulaRuntimeException>(() =>
            new FormulaEngine().EvaluateExpression("A(2)+A(2)", options: atCallLimit with { Limits = new(MaxFunctionDepth: 1, MaxFunctionCalls: 1, MaxDependencyDepth: 1) }));
        Assert.Equal(FormulaErrorCode.FORMULA_LIMIT_EXCEEDED, callError.Code);

        var dependencyRegistry = Registry(Composite("A", 1, "B(x)", 1, 1, Arg(1, "X")), Composite("B", 1, "x+1", 1, 1, Arg(1, "X")));
        Assert.True(CalculationDependencyGraph.Build(new FormulaEngine(), dependencyRegistry, maxDependencyDepth: 2).IsValid);
        Assert.False(CalculationDependencyGraph.Build(new FormulaEngine(), dependencyRegistry, maxDependencyDepth: 1).IsValid);
    }

    [Fact]
    public void FormulaEngineLimits_RemainAtApprovedBoundaries()
    {
        Assert.Equal(4096, FormulaEngine.MaxExpressionLength);
        Assert.Equal(512, FormulaEngine.MaxTokens);
        Assert.Equal(64, FormulaEngine.MaxAstDepth);
        Assert.Equal(2048, FormulaEngine.MaxOperations);
        Assert.Equal(32, new CalculationRuntimeLimits().MaxFunctionDepth);
        Assert.Equal(256, new CalculationRuntimeLimits().MaxFunctionCalls);
        Assert.Equal(32, new CalculationRuntimeLimits().MaxDependencyDepth);
    }

    [Fact]
    public void ParameterResolver_UsesPinnedPublishedTypedVersion()
    {
        var resolver = new DbDrivenParameterResolver(
            [new ParametroDto { Id = 1, Codigo = "PESO", Estado = "ACTIVE" }],
            [new ParametroVersionDto { Id = 2, ParametroId = 1, Version = 1, Tipo = "DECIMAL", ValorDecimal = 0.25m, Estado = "PUBLISHED", Hash = Hash() }]);
        var pinning = new CalculationPinning(new Dictionary<string, int>(), new Dictionary<string, int> { ["PESO"] = 1 }, published: true);

        Assert.Equal(0.25d, resolver.Resolve("PESO", pinning).ToObject());
    }

    [Fact]
    public void PublicationGate_RejectsUnpinnedPublicationAndInvalidArity()
    {
        var registry = Registry(Native("MIN", "MIN_V1", 1, null, "PUBLISHED", Arg(1, "VALUES", variadic: true)));
        var formula = new FormulaVersionDto { Id = 1, FormulaId = 1, Version = 1, Expresion = "MIN()", TipoResultado = "DECIMAL", Estado = "APPROVED", Hash = Hash() };
        var gate = new PublicationGate();

        var invalidArity = gate.Validate(formula, registry, new Dictionary<string, ParameterVersionDefinition>(), new(new Dictionary<string, int> { ["MIN"] = 1 }, new Dictionary<string, int>(), published: true));
        Assert.False(invalidArity.CanPublish);
        Assert.Contains(invalidArity.Errors, error => error.Code == FormulaErrorCode.FORMULA_ARGUMENT_INVALID);

        var unpinnedFormula = new FormulaVersionDto { Id = 1, FormulaId = 1, Version = 1, Expresion = "MIN(1)", TipoResultado = "DECIMAL", Estado = "APPROVED", Hash = Hash() };
        var unpinned = gate.Validate(unpinnedFormula, registry, new Dictionary<string, ParameterVersionDefinition>(), new(new Dictionary<string, int>(), new Dictionary<string, int>(), published: false));
        Assert.False(unpinned.CanPublish);
    }

    private static InMemoryFunctionRegistry Registry(params FunctionVersionDefinition[] definitions) => new(definitions);
    private static FunctionVersionDefinition Native(string code, string handler, int min, int? max, string state, params FunctionArgumentDefinition[] args) => new(code, 1, "NATIVE", code is "AND" or "OR" ? "BOOLEAN" : "DECIMAL", handler, null, min, max, state, Hash(), args);
    private static FunctionVersionDefinition Composite(string code, int version, string dsl, int min, int? max, params FunctionArgumentDefinition[] args) => new(code, version, "COMPOSITE", "DECIMAL", null, dsl, min, max, "PUBLISHED", Hash(), args);
    private static FunctionArgumentDefinition Arg(int position, string code, bool variadic = false) => new(position, code, "DECIMAL", true, variadic, null);
    private static string Hash() => new('0', 64);
}
