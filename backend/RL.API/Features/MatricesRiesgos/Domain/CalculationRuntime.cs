using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using RL.API.Features.Catalogos.Contracts;
using RL.API.Features.MatricesRiesgos.Contracts;
using CatalogElement = RL.API.Features.Catalogos.Contracts.ElementoCatalogoMatricesDto;

namespace RL.API.Features.MatricesRiesgos.Domain;

public enum FormulaValueType
{
    Blank,
    Number,
    Boolean,
    Text,
    Date
}

public readonly record struct FormulaValue(FormulaValueType Type, double? Number, bool? Boolean, string? Text, DateTime? Date)
{
    public static FormulaValue Blank() => new(FormulaValueType.Blank, null, null, null, null);
    public static FormulaValue NumberValue(double value) => new(FormulaValueType.Number, value, null, null, null);
    public static FormulaValue BooleanValue(bool value) => new(FormulaValueType.Boolean, null, value, null, null);
    public static FormulaValue TextValue(string value) => new(FormulaValueType.Text, null, null, value, null);
    public static FormulaValue DateValue(DateTime value) => new(FormulaValueType.Date, null, null, null, value);

    public static FormulaValue FromJson(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Number when value.TryGetDouble(out double number) => NumberValue(number),
        JsonValueKind.True => BooleanValue(true),
        JsonValueKind.False => BooleanValue(false),
        JsonValueKind.String => TextValue(value.GetString() ?? string.Empty),
        _ => Blank()
    };

    public double AsNumber() => Type switch
    {
        FormulaValueType.Number => Number!.Value,
        FormulaValueType.Boolean => Boolean!.Value ? 1d : 0d,
        FormulaValueType.Text when double.TryParse(Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed) => parsed,
        _ => throw new FormulaRuntimeException(FormulaErrorCode.FORMULA_TYPE_MISMATCH, "Se esperaba un número.")
    };

    public bool AsBoolean() => Type switch
    {
        FormulaValueType.Boolean => Boolean!.Value,
        FormulaValueType.Number => Number!.Value != 0,
        FormulaValueType.Text => !string.IsNullOrEmpty(Text),
        _ => false
    };

    public object? ToObject() => Type switch
    {
        FormulaValueType.Number => Number,
        FormulaValueType.Boolean => Boolean,
        FormulaValueType.Text => Text,
        FormulaValueType.Date => Date,
        _ => null
    };
}

public sealed class FormulaRuntimeException : Exception
{
    public FormulaRuntimeException(FormulaErrorCode code, string message) : base(message) => Code = code;
    public FormulaErrorCode Code { get; }
}

public sealed record FunctionArgumentDefinition(
    int Position,
    string Code,
    string Type,
    bool Required,
    bool Variadic,
    string? DefaultJson);

public sealed record FunctionVersionDefinition(
    string Code,
    int Version,
    string Type,
    string ResultType,
    string? HandlerKey,
    string? DefinitionDsl,
    int MinArity,
    int? MaxArity,
    string State,
    string Hash,
    IReadOnlyList<FunctionArgumentDefinition> Arguments)
{
    public string Identity => $"{Code.ToUpperInvariant()}@{Version}";
    public bool IsComposite => Type.Equals("COMPOSITE", StringComparison.OrdinalIgnoreCase);
}

public sealed record ParameterVersionDefinition(
    string Code,
    int Version,
    string Type,
    FormulaValue Value,
    string State,
    string Hash)
{
    public string Identity => $"{Code.ToUpperInvariant()}@{Version}";
}

public sealed record CatalogSnapshot(string Code, bool Active, IReadOnlyList<CatalogElement> Elements);

public interface ICalculationLookup
{
    FormulaValue Lookup(string catalogCode, FormulaValue input, string? resultField = null);
}

public sealed class CatalogCalculationLookup : ICalculationLookup
{
    private readonly IReadOnlyDictionary<string, CatalogSnapshot> _catalogs;

    public CatalogCalculationLookup(IEnumerable<CatalogSnapshot> catalogs)
    {
        _catalogs = catalogs.ToDictionary(c => c.Code, StringComparer.OrdinalIgnoreCase);
    }

    public FormulaValue Lookup(string catalogCode, FormulaValue input, string? resultField = null)
    {
        if (!_catalogs.TryGetValue(catalogCode, out CatalogSnapshot? catalog) || !catalog.Active)
            throw new FormulaRuntimeException(FormulaErrorCode.FORMULA_REFERENCE_UNKNOWN, $"Catálogo '{catalogCode}' inexistente o inactivo.");

        string key = input.Type == FormulaValueType.Text ? input.Text ?? string.Empty : Convert.ToString(input.ToObject(), CultureInfo.InvariantCulture) ?? string.Empty;
        List<CatalogElement> matches = catalog.Elements
            .Where(e => e.Activo && (e.Codigo.Equals(key, StringComparison.OrdinalIgnoreCase) || e.Valor.Equals(key, StringComparison.OrdinalIgnoreCase)))
            .ToList();
        if (matches.Count == 0) throw new FormulaRuntimeException(FormulaErrorCode.FORMULA_REFERENCE_UNKNOWN, $"No existe coincidencia en el catálogo '{catalogCode}'.");
        if (matches.Count > 1) throw new FormulaRuntimeException(FormulaErrorCode.FORMULA_ARGUMENT_INVALID, $"La búsqueda en el catálogo '{catalogCode}' es ambigua.");

        string result = resultField?.Trim().ToUpperInvariant() switch
        {
            null or "" or "VALUE" => matches[0].Valor,
            "CODE" => matches[0].Codigo,
            _ => throw new FormulaRuntimeException(FormulaErrorCode.FORMULA_ARGUMENT_INVALID, "El campo de resultado del catálogo no está permitido.")
        };
        return FormulaValue.TextValue(result);
    }
}

public sealed record CalculationRuntimeLimits(int MaxFunctionDepth = 32, int MaxFunctionCalls = 256, int MaxDependencyDepth = 32);

public sealed class CalculationPinning
{
    public CalculationPinning(
        IReadOnlyDictionary<string, int> functionVersions,
        IReadOnlyDictionary<string, int> parameterVersions,
        IReadOnlyDictionary<string, string>? catalogSnapshots = null,
        bool published = false)
    {
        FunctionVersions = new ReadOnlyDictionary<string, int>(new Dictionary<string, int>(functionVersions, StringComparer.OrdinalIgnoreCase));
        ParameterVersions = new ReadOnlyDictionary<string, int>(new Dictionary<string, int>(parameterVersions, StringComparer.OrdinalIgnoreCase));
        CatalogSnapshots = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(catalogSnapshots ?? new Dictionary<string, string>(), StringComparer.OrdinalIgnoreCase));
        Published = published;
    }

    public IReadOnlyDictionary<string, int> FunctionVersions { get; }
    public IReadOnlyDictionary<string, int> ParameterVersions { get; }
    public IReadOnlyDictionary<string, string> CatalogSnapshots { get; }
    public bool Published { get; }

    public int? FunctionVersion(string code) => FunctionVersions.TryGetValue(code, out int version) ? version : null;
    public int? ParameterVersion(string code) => ParameterVersions.TryGetValue(code, out int version) ? version : null;
}

public sealed record FormulaRuntimeOptions(
    IFunctionRegistry Registry,
    IReadOnlyDictionary<string, FormulaValue>? Parameters = null,
    ICalculationLookup? Lookup = null,
    CalculationPinning? Pinning = null,
    CalculationRuntimeLimits? Limits = null)
{
    public bool RequirePinnedDependencies => Pinning?.Published == true;
    public CalculationRuntimeLimits EffectiveLimits => Limits ?? new();
}

public interface IFunctionRegistry
{
    IReadOnlyCollection<string> FunctionCodes { get; }
    FunctionVersionDefinition Resolve(string code, int? pinnedVersion = null, bool requirePinned = false);
    bool Contains(string code);
}

public sealed class DbDrivenFunctionRegistry : IFunctionRegistry
{
    private readonly IReadOnlyDictionary<string, IReadOnlyDictionary<int, FunctionVersionDefinition>> _definitions;
    private readonly IReadOnlyDictionary<string, string> _masterStates;

    public DbDrivenFunctionRegistry(IEnumerable<FuncionDto> functions, IEnumerable<FuncionVersionDto> versions, IEnumerable<FuncionArgumentoDto> arguments)
    {
        var masterRows = functions.Select(f => new
        {
            f.Id,
            Code = (f.Codigo ?? string.Empty).Trim().ToUpperInvariant(),
            State = (f.Estado ?? string.Empty).Trim().ToUpperInvariant()
        }).ToList();
        if (masterRows.Any(f => string.IsNullOrWhiteSpace(f.Code)) || masterRows.Select(f => f.Code).Distinct(StringComparer.OrdinalIgnoreCase).Count() != masterRows.Count)
            throw new FormulaRuntimeException(FormulaErrorCode.FORMULA_FUNCTION_UNSUPPORTED, "El catálogo de funciones contiene códigos maestros inválidos o duplicados.");
        var masters = masterRows.ToDictionary(f => f.Id, f => f.Code);
        _masterStates = masterRows.ToDictionary(f => f.Code, f => f.State, StringComparer.OrdinalIgnoreCase);
        var argsByVersion = arguments.GroupBy(a => a.FuncionVersionId).ToDictionary(
            g => g.Key,
            g => (IReadOnlyList<FunctionArgumentDefinition>)g.OrderBy(a => a.Posicion)
                .Select(a => new FunctionArgumentDefinition(a.Posicion, a.Codigo.Trim().ToUpperInvariant(), a.Tipo.Trim().ToUpperInvariant(), a.Requerido, a.Variadic, a.ValorDefaultJson)).ToList());

        _definitions = versions
            .Where(v => masters.ContainsKey(v.FuncionId))
            .GroupBy(v => masters[v.FuncionId], StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyDictionary<int, FunctionVersionDefinition>)g.ToDictionary(
                    v => v.Version,
                    v => new FunctionVersionDefinition(
                        g.Key,
                        v.Version,
                        v.Tipo.Trim().ToUpperInvariant(),
                        v.TipoResultado.Trim().ToUpperInvariant(),
                        v.HandlerKey?.Trim().ToUpperInvariant(),
                        v.DefinicionDsl,
                        v.MinArity,
                        v.MaxArity,
                        v.Estado.Trim().ToUpperInvariant(),
                        v.Hash,
                        argsByVersion.TryGetValue(v.Id, out IReadOnlyList<FunctionArgumentDefinition>? args) ? args : Array.Empty<FunctionArgumentDefinition>())),
                StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyCollection<string> FunctionCodes => _definitions.Keys.ToArray();
    public bool Contains(string code) => _definitions.ContainsKey(code.Trim());

    public FunctionVersionDefinition Resolve(string code, int? pinnedVersion = null, bool requirePinned = false)
    {
        string normalized = code.Trim().ToUpperInvariant();
        if (!_definitions.TryGetValue(normalized, out IReadOnlyDictionary<int, FunctionVersionDefinition>? versions))
            throw new FormulaRuntimeException(FormulaErrorCode.FORMULA_FUNCTION_UNSUPPORTED, $"Función '{code}' no registrada.");
        if (!_masterStates.TryGetValue(normalized, out string? masterState) || masterState != "ACTIVE")
            throw new FormulaRuntimeException(FormulaErrorCode.FORMULA_FUNCTION_UNSUPPORTED, "Function master is not active.");
        if (requirePinned && !pinnedVersion.HasValue)
            throw new FormulaRuntimeException(FormulaErrorCode.FORMULA_ARGUMENT_INVALID, $"La función '{normalized}' no está versionada/pinneada.");

        FunctionVersionDefinition definition = pinnedVersion.HasValue
            ? versions.TryGetValue(pinnedVersion.Value, out FunctionVersionDefinition? pinned) ? pinned : throw new FormulaRuntimeException(FormulaErrorCode.FORMULA_FUNCTION_UNSUPPORTED, $"Versión {normalized}@{pinnedVersion} inexistente.")
            : versions.Values.Where(v => v.State is "PUBLISHED").OrderByDescending(v => v.Version).FirstOrDefault()
              ?? throw new FormulaRuntimeException(FormulaErrorCode.FORMULA_FUNCTION_UNSUPPORTED, $"Función '{normalized}' sin versión resoluble.");

        if (requirePinned && definition.State != "PUBLISHED")
            throw new FormulaRuntimeException(FormulaErrorCode.FORMULA_FUNCTION_UNSUPPORTED, "Pinned runtime dependencies must be published.");
        if (definition.State is "RETIRED" or "ARCHIVED")
            throw new FormulaRuntimeException(FormulaErrorCode.FORMULA_FUNCTION_UNSUPPORTED, $"La versión de función '{definition.Identity}' no está activa.");
        if (definition.Version < 1 || definition.MinArity < 0 || definition.MaxArity is < 0 || definition.MaxArity < definition.MinArity)
            throw new FormulaRuntimeException(FormulaErrorCode.FORMULA_ARGUMENT_INVALID, "Invalid function arity contract.");
        if (!Regex.IsMatch(definition.Hash ?? string.Empty, "^[0-9A-Fa-f]{64}$", RegexOptions.CultureInvariant))
            throw new FormulaRuntimeException(FormulaErrorCode.FORMULA_ARGUMENT_INVALID, "Invalid function version hash.");
        if (definition.Type is not ("NATIVE" or "COMPOSITE"))
            throw new FormulaRuntimeException(FormulaErrorCode.FORMULA_FUNCTION_UNSUPPORTED, $"Tipo de función inválido para '{definition.Identity}'.");
        if (definition.IsComposite && (string.IsNullOrWhiteSpace(definition.DefinitionDsl) || definition.HandlerKey is not null))
            throw new FormulaRuntimeException(FormulaErrorCode.FORMULA_FUNCTION_UNSUPPORTED, $"La función compuesta '{definition.Identity}' no tiene DSL.");
        if (!definition.IsComposite && (definition.DefinitionDsl is not null || !NativeFunctionCatalog.Matches(definition.Code, definition.HandlerKey)))
            throw new FormulaRuntimeException(FormulaErrorCode.FORMULA_FUNCTION_UNSUPPORTED, $"Handler no permitido para '{definition.Identity}'.");
        ValidateArgumentContract(definition);
        return definition;
    }

    private static void ValidateArgumentContract(FunctionVersionDefinition definition)
    {
        if (definition.Arguments.Select(a => a.Position).Distinct().Count() != definition.Arguments.Count
            || definition.Arguments.Select(a => a.Code).Distinct(StringComparer.OrdinalIgnoreCase).Count() != definition.Arguments.Count
            || definition.Arguments.Any(a => a.Position < 1 || a.Variadic && a.Position != definition.Arguments.Max(x => x.Position)))
            throw new FormulaRuntimeException(FormulaErrorCode.FORMULA_ARGUMENT_INVALID, "Invalid function argument contract.");
        if (definition.Arguments.Any(a => a.Type is not ("VALUE" or "INTEGER" or "DECIMAL" or "BOOLEAN" or "TEXT" or "DATE")))
            throw new FormulaRuntimeException(FormulaErrorCode.FORMULA_TYPE_MISMATCH, "Invalid function argument type.");
        for (int position = 1; position <= definition.MinArity; position++)
            if (!definition.Arguments.Any(a => a.Position == position))
                throw new FormulaRuntimeException(FormulaErrorCode.FORMULA_ARGUMENT_INVALID, "The function signature omits a required argument.");
        if (!definition.IsComposite && !NativeFunctionCatalog.MatchesContract(definition))
            throw new FormulaRuntimeException(FormulaErrorCode.FORMULA_ARGUMENT_INVALID, $"Invalid native contract for '{definition.Identity}'.");
    }
}

public sealed class DbDrivenParameterResolver
{
    private readonly IReadOnlyDictionary<string, IReadOnlyDictionary<int, ParameterVersionDefinition>> _definitions;
    private readonly IReadOnlyDictionary<string, string> _masterStates;

    public DbDrivenParameterResolver(IEnumerable<ParametroDto> parameters, IEnumerable<ParametroVersionDto> versions)
    {
        var masterRows = parameters.Select(p => new
        {
            p.Id,
            Code = (p.Codigo ?? string.Empty).Trim().ToUpperInvariant(),
            State = (p.Estado ?? string.Empty).Trim().ToUpperInvariant()
        }).ToList();
        if (masterRows.Any(p => string.IsNullOrWhiteSpace(p.Code)) || masterRows.Select(p => p.Code).Distinct(StringComparer.OrdinalIgnoreCase).Count() != masterRows.Count)
            throw new FormulaRuntimeException(FormulaErrorCode.FORMULA_REFERENCE_UNKNOWN, "El catálogo de parámetros contiene códigos inválidos o duplicados.");
        var masters = masterRows.ToDictionary(p => p.Id, p => p.Code);
        _masterStates = masterRows.ToDictionary(p => p.Code, p => p.State, StringComparer.OrdinalIgnoreCase);
        _definitions = versions
            .Where(v => masters.ContainsKey(v.ParametroId))
            .GroupBy(v => masters[v.ParametroId], StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyDictionary<int, ParameterVersionDefinition>)g.ToDictionary(
                    v => v.Version,
                    v => new ParameterVersionDefinition(g.Key, v.Version, v.Tipo.Trim().ToUpperInvariant(), ToValue(v), v.Estado.Trim().ToUpperInvariant(), v.Hash)),
                StringComparer.OrdinalIgnoreCase);
    }

    public FormulaValue Resolve(string code, CalculationPinning pinning, bool requirePinned = true)
    {
        string normalized = code.Trim().ToUpperInvariant();
        if (!_definitions.TryGetValue(normalized, out IReadOnlyDictionary<int, ParameterVersionDefinition>? versions))
            throw new FormulaRuntimeException(FormulaErrorCode.FORMULA_REFERENCE_UNKNOWN, $"Parámetro '{code}' no registrado.");
        int? version = pinning.ParameterVersion(normalized);
        if (!_masterStates.TryGetValue(normalized, out string? masterState) || masterState != "ACTIVE")
            throw new FormulaRuntimeException(FormulaErrorCode.FORMULA_REFERENCE_UNKNOWN, "El parámetro no está activo.");
        if (requirePinned && !version.HasValue)
            throw new FormulaRuntimeException(FormulaErrorCode.FORMULA_ARGUMENT_INVALID, $"El parámetro '{normalized}' no está versionado/pinneado.");
        ParameterVersionDefinition resolved = version.HasValue
            ? versions.TryGetValue(version.Value, out ParameterVersionDefinition? pinned) ? pinned : throw new FormulaRuntimeException(FormulaErrorCode.FORMULA_REFERENCE_UNKNOWN, $"Versión {normalized}@{version} inexistente.")
            : versions.Values.Where(v => v.State == "PUBLISHED").OrderByDescending(v => v.Version).FirstOrDefault()
              ?? throw new FormulaRuntimeException(FormulaErrorCode.FORMULA_REFERENCE_UNKNOWN, $"Parámetro '{normalized}' sin versión publicada.");
        if (resolved.State is "RETIRED" or "ARCHIVED") throw new FormulaRuntimeException(FormulaErrorCode.FORMULA_REFERENCE_UNKNOWN, $"La versión de parámetro '{resolved.Identity}' no está activa.");
        if (requirePinned && resolved.State != "PUBLISHED") throw new FormulaRuntimeException(FormulaErrorCode.FORMULA_REFERENCE_UNKNOWN, "La versión de parámetro pinneada no está publicada.");
        if (!Regex.IsMatch(resolved.Hash ?? string.Empty, "^[0-9A-Fa-f]{64}$", RegexOptions.CultureInvariant))
            throw new FormulaRuntimeException(FormulaErrorCode.FORMULA_ARGUMENT_INVALID, "Invalid parameter version hash.");
        return resolved.Value;
    }

    private static FormulaValue ToValue(ParametroVersionDto value) => value.Tipo.Trim().ToUpperInvariant() switch
    {
        "INTEGER" when value.ValorEntero.HasValue => FormulaValue.NumberValue(value.ValorEntero.Value),
        "DECIMAL" when value.ValorDecimal.HasValue => FormulaValue.NumberValue((double)value.ValorDecimal.Value),
        "BOOLEAN" when value.ValorBooleano.HasValue => FormulaValue.BooleanValue(value.ValorBooleano.Value),
        "TEXT" when value.ValorTexto is not null => FormulaValue.TextValue(value.ValorTexto),
        "DATE" when value.ValorFecha.HasValue => FormulaValue.DateValue(value.ValorFecha.Value),
        _ => throw new FormulaRuntimeException(FormulaErrorCode.FORMULA_TYPE_MISMATCH, $"Valor tipado inválido para el parámetro '{value.Id}'.")
    };
}

public static class NativeFunctionCatalog
{
    private static readonly IReadOnlyDictionary<string, string> Handlers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["IF"] = "IF_V1", ["IFERROR"] = "IFERROR_V1", ["ROUND"] = "ROUND_V1", ["ROUNDDOWN"] = "ROUNDDOWN_V1",
        ["MAX"] = "MAX_V1", ["MIN"] = "MIN_V1", ["MOD"] = "MOD_V1", ["OR"] = "OR_V1", ["AND"] = "AND_V1", ["LOOKUP"] = "LOOKUP_V1"
    };

    public static IReadOnlyCollection<string> FunctionCodes => Handlers.Keys.ToArray();
    public static bool IsAllowedHandler(string? handler) => handler is not null && Handlers.Values.Contains(handler, StringComparer.OrdinalIgnoreCase);
    public static bool Matches(string code, string? handler) => handler is not null && Handlers.TryGetValue(code, out string? expected) && expected.Equals(handler, StringComparison.OrdinalIgnoreCase);
    public static bool MatchesContract(FunctionVersionDefinition definition)
    {
        FunctionVersionDefinition expected = ExpectedDefinition(definition.Code);
        if (definition.MinArity != expected.MinArity || definition.MaxArity != expected.MaxArity) return false;
        foreach (FunctionArgumentDefinition required in expected.Arguments.Where(argument => argument.Required))
        {
            FunctionArgumentDefinition? actual = definition.Arguments.FirstOrDefault(argument => argument.Position == required.Position);
            if (actual is null || !actual.Code.Equals(required.Code, StringComparison.OrdinalIgnoreCase) || !actual.Type.Equals(required.Type, StringComparison.OrdinalIgnoreCase) || actual.Required != required.Required || actual.Variadic != required.Variadic)
                return false;
        }
        return definition.Arguments.All(actual => expected.Arguments.Any(item => item.Position == actual.Position && item.Code.Equals(actual.Code, StringComparison.OrdinalIgnoreCase) && item.Type.Equals(actual.Type, StringComparison.OrdinalIgnoreCase) && item.Variadic == actual.Variadic));
    }

    private static FunctionVersionDefinition ExpectedDefinition(string code) => CreateDefaultDefinitions().Single(definition => definition.Code.Equals(code, StringComparison.OrdinalIgnoreCase));

    public static IReadOnlyList<FunctionVersionDefinition> CreateDefaultDefinitions() => Handlers.Select(pair => new FunctionVersionDefinition(
        pair.Key, 1, "NATIVE", pair.Key is "OR" or "AND" ? "BOOLEAN" : pair.Key == "LOOKUP" ? "TEXT" : "DECIMAL", pair.Value, null,
        pair.Key switch { "IF" => 3, "IFERROR" or "ROUND" or "ROUNDDOWN" or "MOD" => 2, "LOOKUP" => 2, _ => 1 },
        pair.Key switch { "IF" => 3, "IFERROR" or "ROUND" or "ROUNDDOWN" or "MOD" => 2, "LOOKUP" => 3, _ => null },
        "PUBLISHED", new string('0', 64), ArgumentsFor(pair.Key))).ToList();

    private static IReadOnlyList<FunctionArgumentDefinition> ArgumentsFor(string code) => code switch
    {
        "IF" => [Arg(1, "CONDITION", "BOOLEAN"), Arg(2, "TRUE_VALUE", "DECIMAL"), Arg(3, "FALSE_VALUE", "DECIMAL")],
        "IFERROR" => [Arg(1, "VALUE", "DECIMAL"), Arg(2, "FALLBACK", "DECIMAL")],
        "ROUND" or "ROUNDDOWN" => [Arg(1, "VALUE", "DECIMAL"), Arg(2, "DIGITS", "INTEGER")],
        "MOD" => [Arg(1, "VALUE", "DECIMAL"), Arg(2, "DIVISOR", "DECIMAL")],
        "MAX" or "MIN" => [Arg(1, "VALUES", "DECIMAL", variadic: true)],
        "OR" or "AND" => [Arg(1, "VALUES", "BOOLEAN", variadic: true)],
        "LOOKUP" => [Arg(1, "CATALOG_CODE", "TEXT"), Arg(2, "INPUT", "VALUE"), Arg(3, "RESULT_FIELD", "TEXT", required: false)],
        _ => Array.Empty<FunctionArgumentDefinition>()
    };

    private static FunctionArgumentDefinition Arg(int position, string code, string type, bool required = true, bool variadic = false) =>
        new(position, code, type, required, variadic, null);
}

public sealed class InMemoryFunctionRegistry : IFunctionRegistry
{
    private readonly DbDrivenFunctionRegistry _inner;

    public InMemoryFunctionRegistry(IEnumerable<FunctionVersionDefinition> definitions)
    {
        var groups = definitions.GroupBy(d => d.Code.Trim().ToUpperInvariant(), StringComparer.OrdinalIgnoreCase).ToList();
        var masters = groups.Select((group, index) => new FuncionDto { Id = index + 1, Codigo = group.Key, Estado = "ACTIVE" }).ToList();
        var versions = new List<FuncionVersionDto>();
        var arguments = new List<FuncionArgumentoDto>();
        long versionId = 1;
        foreach (var group in groups)
        {
            long functionId = masters[groups.IndexOf(group)].Id;
            foreach (FunctionVersionDefinition definition in group)
            {
                long currentVersionId = versionId++;
                versions.Add(new FuncionVersionDto
                {
                    Id = currentVersionId, FuncionId = functionId, Version = definition.Version, Tipo = definition.Type, TipoResultado = definition.ResultType,
                    HandlerKey = definition.HandlerKey, DefinicionDsl = definition.DefinitionDsl, MinArity = definition.MinArity, MaxArity = definition.MaxArity, Estado = definition.State, Hash = definition.Hash
                });
                arguments.AddRange(definition.Arguments.Select(argument => new FuncionArgumentoDto
                {
                    Id = currentVersionId * 1000 + argument.Position, FuncionVersionId = currentVersionId, Posicion = argument.Position, Codigo = argument.Code, Nombre = argument.Code, Tipo = argument.Type,
                    Requerido = argument.Required, Variadic = argument.Variadic, ValorDefaultJson = argument.DefaultJson
                }));
            }
        }
        _inner = new DbDrivenFunctionRegistry(masters, versions, arguments);
    }

    public IReadOnlyCollection<string> FunctionCodes => _inner.FunctionCodes;
    public bool Contains(string code) => _inner.Contains(code);
    public FunctionVersionDefinition Resolve(string code, int? pinnedVersion = null, bool requirePinned = false) => _inner.Resolve(code, pinnedVersion, requirePinned);
}

public sealed record DependencyGraphResult(IReadOnlyDictionary<string, IReadOnlySet<string>> Edges, IReadOnlyList<string> Cycles)
{
    public bool IsValid => Cycles.Count == 0;
}
