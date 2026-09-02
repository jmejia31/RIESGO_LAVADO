using System.Globalization;
using System.Text.Json;

namespace RL.API.Features.MatricesRiesgos.Domain;

public enum FormulaErrorCode
{
    FORMULA_SYNTAX_INVALID, FORMULA_OPERATOR_UNSUPPORTED, FORMULA_FUNCTION_UNSUPPORTED,
    FORMULA_REFERENCE_UNKNOWN, FORMULA_SELF_REFERENCE, FORMULA_CYCLE, FORMULA_TYPE_MISMATCH,
    FORMULA_DIVISION_BY_ZERO, FORMULA_ARGUMENT_INVALID, FORMULA_LIMIT_EXCEEDED
}

public sealed record FormulaDiagnostic(FormulaErrorCode Code, string Field, string Message);

public sealed class FormulaEvaluationResult
{
    public Dictionary<string, object?> Values { get; } = new(StringComparer.OrdinalIgnoreCase);
    public List<FormulaDiagnostic> Errors { get; } = new();
    public bool Success => Errors.Count == 0;
}

public sealed record FormulaExpressionAnalysis(IReadOnlySet<string> ReferencedNames, IReadOnlySet<string> ReferencedFunctions);

/// <summary>Único parser, AST y evaluador seguro del DSL de matrices de riesgos.</summary>
public sealed class FormulaEngine
{
    public const int MaxExpressionLength = 4096;
    public const int MaxTokens = 512;
    public const int MaxAstDepth = 64;
    public const int MaxOperations = 2048;
    private static readonly IFunctionRegistry DefaultRegistry = new InMemoryFunctionRegistry(NativeFunctionCatalog.CreateDefaultDefinitions());
    public static IReadOnlyCollection<string> SupportedFunctionNames => NativeFunctionCatalog.FunctionCodes;

    public IReadOnlyList<FormulaDiagnostic> ValidateDefinition(string json) => ValidateDefinition(json, new(DefaultRegistry));

    public IReadOnlyList<FormulaDiagnostic> ValidateDefinition(string json, FormulaRuntimeOptions options)
    {
        var errors = new List<FormulaDiagnostic>();
        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            JsonElement root = document.RootElement;
            if (root.TryGetProperty("definicionFormulario", out JsonElement nested)) root = nested;
            Dictionary<string, Field> fields = ReadFields(root, errors);
            var formulas = new Dictionary<string, (string Expression, Node Ast)>(StringComparer.OrdinalIgnoreCase);
            foreach (Field field in fields.Values.Where(f => !string.IsNullOrWhiteSpace(f.Expression)))
            {
                try
                {
                    Node ast = Parse(field.Expression!, options.Registry.FunctionCodes);
                    formulas[field.Key] = (field.Expression!, ast);
                    ValidateReferences(ast, fields.Keys, field.Key, errors);
                }
                catch (FormulaRuntimeException ex) { errors.Add(new(ex.Code, field.Key, ex.Message)); }
            }
            DetectCycles(formulas, errors);
        }
        catch (JsonException ex) { errors.Add(new(FormulaErrorCode.FORMULA_SYNTAX_INVALID, "JSON", ex.Message)); }
        return errors;
    }

    public FormulaExpressionAnalysis AnalyzeExpression(string expression, IFunctionRegistry? registry = null)
    {
        Node ast = Parse(expression, (registry ?? DefaultRegistry).FunctionCodes);
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var functions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        Collect(ast, names, functions);
        return new(names, functions);
    }

    public IReadOnlyList<FormulaDiagnostic> ValidateExpression(string expression, FormulaRuntimeOptions options)
    {
        var errors = new List<FormulaDiagnostic>();
        try
        {
            Node ast = Parse(expression, options.Registry.FunctionCodes);
            ValidateExpressionNode(ast, options, errors);
        }
        catch (FormulaRuntimeException ex)
        {
            errors.Add(new(ex.Code, "expression", ex.Message));
        }
        return errors;
    }

    public FormulaValue EvaluateExpression(string expression, IReadOnlyDictionary<string, FormulaValue>? values = null, FormulaRuntimeOptions? options = null)
    {
        FormulaRuntimeOptions runtime = options ?? new(DefaultRegistry);
        Node ast = Parse(expression, runtime.Registry.FunctionCodes);
        var state = new EvaluationState(runtime, new(StringComparer.OrdinalIgnoreCase));
        return state.Evaluate(ast, new Dictionary<string, FormulaValue>(values ?? new Dictionary<string, FormulaValue>(), StringComparer.OrdinalIgnoreCase));
    }

    public FormulaEvaluationResult Evaluate(string definitionJson, string responsesJson) => Evaluate(definitionJson, responsesJson, new(DefaultRegistry));

    public FormulaEvaluationResult Evaluate(string definitionJson, string responsesJson, FormulaRuntimeOptions options)
    {
        var result = new FormulaEvaluationResult();
        try
        {
            using JsonDocument definition = JsonDocument.Parse(definitionJson);
            using JsonDocument responses = JsonDocument.Parse(string.IsNullOrWhiteSpace(responsesJson) ? "{}" : responsesJson);
            JsonElement root = definition.RootElement;
            if (root.TryGetProperty("definicionFormulario", out JsonElement nested)) root = nested;
            Dictionary<string, Field> fields = ReadFields(root, result.Errors);
            if (result.Errors.Count > 0) return result;
            var values = new Dictionary<string, FormulaValue>(StringComparer.OrdinalIgnoreCase);
            if (responses.RootElement.ValueKind == JsonValueKind.Object)
                foreach (JsonProperty property in responses.RootElement.EnumerateObject()) values[property.Name] = FormulaValue.FromJson(property.Value);
            var formulas = new Dictionary<string, Node>(StringComparer.OrdinalIgnoreCase);
            foreach (Field field in fields.Values.Where(f => !string.IsNullOrWhiteSpace(f.Expression)))
            {
                try { formulas[field.Key] = Parse(field.Expression!, options.Registry.FunctionCodes); }
                catch (FormulaRuntimeException ex) { result.Errors.Add(new(ex.Code, field.Key, ex.Message)); }
            }
            if (result.Errors.Count > 0) return result;
            var state = new EvaluationState(options, formulas);
            foreach (string key in formulas.Keys)
            {
                try { result.Values[key] = state.EvaluateField(key, values).ToObject(); }
                catch (FormulaRuntimeException ex) { result.Errors.Add(new(ex.Code, key, ex.Message)); }
            }
        }
        catch (JsonException ex) { result.Errors.Add(new(FormulaErrorCode.FORMULA_SYNTAX_INVALID, "JSON", ex.Message)); }
        return result;
    }

    private static void Collect(Node node, HashSet<string> names, HashSet<string> functions)
    {
        switch (node)
        {
            case ReferenceNode reference: names.Add(reference.Name); break;
            case CallNode call: functions.Add(call.Name); foreach (Node argument in call.Arguments) Collect(argument, names, functions); break;
            case UnaryNode unary: Collect(unary.Operand, names, functions); break;
            case BinaryNode binary: Collect(binary.Left, names, functions); Collect(binary.Right, names, functions); break;
        }
    }

    private static void ValidateExpressionNode(Node node, FormulaRuntimeOptions options, List<FormulaDiagnostic> errors)
    {
        switch (node)
        {
            case UnaryNode unary:
                ValidateExpressionNode(unary.Operand, options, errors);
                break;
            case BinaryNode binary:
                ValidateExpressionNode(binary.Left, options, errors);
                ValidateExpressionNode(binary.Right, options, errors);
                break;
            case CallNode call:
                foreach (Node argument in call.Arguments) ValidateExpressionNode(argument, options, errors);
                try
                {
                    FunctionVersionDefinition definition = options.Registry.Resolve(call.Name, options.Pinning?.FunctionVersion(call.Name), options.RequirePinnedDependencies);
                    ValidateExpressionArity(definition, call.Arguments.Count);
                    ValidateStaticArgumentTypes(definition, call.Arguments);
                }
                catch (FormulaRuntimeException ex)
                {
                    errors.Add(new(ex.Code, call.Name, ex.Message));
                }
                break;
        }
    }

    private static void ValidateExpressionArity(FunctionVersionDefinition definition, int count)
    {
        if (count < definition.MinArity || definition.MaxArity.HasValue && count > definition.MaxArity.Value)
            throw new FormulaRuntimeException(FormulaErrorCode.FORMULA_ARGUMENT_INVALID, $"La aridad de la función '{definition.Code}' es inválida.");
    }

    private static void ValidateStaticArgumentTypes(FunctionVersionDefinition definition, IReadOnlyList<Node> arguments)
    {
        if (definition.Arguments.Count == 0) return;
        for (int index = 0; index < arguments.Count; index++)
        {
            FunctionArgumentDefinition? argument = definition.Arguments.FirstOrDefault(a => a.Position == index + 1) ?? definition.Arguments.LastOrDefault(a => a.Variadic);
            if (argument is null) throw new FormulaRuntimeException(FormulaErrorCode.FORMULA_ARGUMENT_INVALID, "Argumento no declarado en la firma de la función.");
            FormulaValueType? actual = StaticType(arguments[index]);
            if (!actual.HasValue || argument.Type.Equals("VALUE", StringComparison.OrdinalIgnoreCase)) continue;
            bool valid = argument.Type.ToUpperInvariant() switch
            {
                "INTEGER" => actual == FormulaValueType.Number && (arguments[index] is not NumberNode number || number.Number == Math.Truncate(number.Number)),
                "DECIMAL" => actual == FormulaValueType.Number,
                "BOOLEAN" => actual is FormulaValueType.Boolean or FormulaValueType.Number,
                "TEXT" => actual == FormulaValueType.Text,
                "DATE" => actual == FormulaValueType.Date,
                _ => false
            };
            if (!valid) throw new FormulaRuntimeException(FormulaErrorCode.FORMULA_TYPE_MISMATCH, $"El argumento '{argument.Code}' no coincide con el tipo declarado.");
        }
    }

    private static FormulaValueType? StaticType(Node node)
    {
        return node switch
        {
            NumberNode => FormulaValueType.Number,
            BooleanNode => FormulaValueType.Boolean,
            StringNode => FormulaValueType.Text,
            UnaryNode => FormulaValueType.Number,
            BinaryNode binary when binary.Operator is "=" or "<>" or "<" or "<=" or ">" or ">=" => FormulaValueType.Boolean,
            BinaryNode => FormulaValueType.Number,
            CallNode => null,
            _ => null
        };
    }

    private static Dictionary<string, Field> ReadFields(JsonElement root, List<FormulaDiagnostic> errors)
    {
        var fields = new Dictionary<string, Field>(StringComparer.OrdinalIgnoreCase);
        if (!root.TryGetProperty("secciones", out JsonElement sections) || sections.ValueKind != JsonValueKind.Array) return fields;
        foreach (JsonElement section in sections.EnumerateArray())
            if (section.TryGetProperty("campos", out JsonElement items) && items.ValueKind == JsonValueKind.Array)
                foreach (JsonElement item in items.EnumerateArray())
                {
                    string? key = Text(item, "clave") ?? Text(item, "rutaDatos") ?? Text(item, "identificador");
                    if (string.IsNullOrWhiteSpace(key)) continue;
                    string? expression = Text(item, "formula") ?? Text(item, "calculo") ?? Text(item, "referenciaCalculo");
                    if (!fields.TryAdd(key, new Field(key, expression))) errors.Add(new(FormulaErrorCode.FORMULA_SYNTAX_INVALID, key, "La clave del campo está duplicada."));
                }
        return fields;
    }

    private static string? Text(JsonElement element, string property) => element.TryGetProperty(property, out JsonElement value) && value.ValueKind == JsonValueKind.String ? value.GetString()?.Trim() : null;

    private static Node Parse(string expression, IReadOnlyCollection<string> functions)
    {
        string source = expression.StartsWith("=", StringComparison.Ordinal) ? expression[1..].Trim() : expression.Trim();
        if (source.Length == 0 || source.Length > MaxExpressionLength) throw new FormulaRuntimeException(FormulaErrorCode.FORMULA_LIMIT_EXCEEDED, "La longitud de la fórmula está fuera de los límites.");
        return new Parser(source, functions).Parse();
    }

    private static void ValidateReferences(Node node, IEnumerable<string> keys, string field, List<FormulaDiagnostic> errors)
    {
        var known = new HashSet<string>(keys, StringComparer.OrdinalIgnoreCase);
        foreach (string reference in node.References())
        {
            string resolved = ResolveReferenceName(reference, known);
            if (!known.Contains(resolved)) errors.Add(new(FormulaErrorCode.FORMULA_REFERENCE_UNKNOWN, field, $"Referencia desconocida '{reference}'."));
        }
        if (node.References().Any(r => r.Equals(field, StringComparison.OrdinalIgnoreCase))) errors.Add(new(FormulaErrorCode.FORMULA_SELF_REFERENCE, field, "La fórmula se referencia a sí misma."));
    }

    private static string ResolveReferenceName(string reference, IEnumerable<string> knownNames) => reference.Equals("C1", StringComparison.OrdinalIgnoreCase) && knownNames.Any(n => n.Equals("c", StringComparison.OrdinalIgnoreCase)) ? "c" : reference;

    private static void DetectCycles(Dictionary<string, (string Expression, Node Ast)> formulas, List<FormulaDiagnostic> errors)
    {
        try { foreach (string key in formulas.Keys) Visit(key, formulas, new(StringComparer.OrdinalIgnoreCase)); }
        catch (FormulaRuntimeException ex) { errors.Add(new(ex.Code, "formula", ex.Message)); }
    }

    private static void Visit(string key, Dictionary<string, (string Expression, Node Ast)> formulas, HashSet<string> path)
    {
        if (!path.Add(key)) throw new FormulaRuntimeException(FormulaErrorCode.FORMULA_CYCLE, "Dependencia circular detectada.");
        if (formulas.TryGetValue(key, out var formula)) foreach (string reference in formula.Ast.References().Where(formulas.ContainsKey)) Visit(reference, formulas, new(path, StringComparer.OrdinalIgnoreCase));
    }

    private sealed record Field(string Key, string? Expression);

    private sealed class EvaluationState
    {
        private readonly FormulaRuntimeOptions _options;
        private readonly Dictionary<string, Node> _formulas;
        private readonly Dictionary<string, FormulaValue> _cache = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _fieldPath = new(StringComparer.OrdinalIgnoreCase);
        private readonly Stack<string> _functionPath = new();
        private int _operations;
        private int _functionCalls;
        public EvaluationState(FormulaRuntimeOptions options, Dictionary<string, Node> formulas) { _options = options; _formulas = formulas; }
        public FormulaValue Evaluate(Node node, Dictionary<string, FormulaValue> values) => EvaluateNode(node, values);

        public FormulaValue EvaluateField(string key, Dictionary<string, FormulaValue> values)
        {
            if (_cache.TryGetValue(key, out FormulaValue cached)) return cached;
            if (!_fieldPath.Add(key)) throw new FormulaRuntimeException(FormulaErrorCode.FORMULA_CYCLE, "Dependencia circular detectada.");
            try { FormulaValue value = EvaluateNode(_formulas[key], values); _cache[key] = value; values[key] = value; return value; }
            finally { _fieldPath.Remove(key); }
        }

        private FormulaValue EvaluateNode(Node node, Dictionary<string, FormulaValue> values)
        {
            if (++_operations > FormulaEngine.MaxOperations) throw new FormulaRuntimeException(FormulaErrorCode.FORMULA_LIMIT_EXCEEDED, "Cantidad máxima de operaciones excedida.");
            return node switch
            {
                NumberNode number => FormulaValue.NumberValue(number.Number), BooleanNode boolean => FormulaValue.BooleanValue(boolean.Value), StringNode text => FormulaValue.TextValue(text.Text),
                ReferenceNode reference => EvaluateReference(reference.Name, values), UnaryNode unary => ApplyUnary(unary, values),
                BinaryNode binary => ApplyBinary(binary, values), CallNode call => EvaluateCall(call, values),
                _ => throw new FormulaRuntimeException(FormulaErrorCode.FORMULA_SYNTAX_INVALID, "AST no soportado.")
            };
        }

        private FormulaValue EvaluateReference(string name, Dictionary<string, FormulaValue> values)
        {
            string resolved = ResolveReferenceName(name, _formulas.Keys.Concat(values.Keys).Concat(_options.Parameters?.Keys ?? Array.Empty<string>()));
            if (_formulas.ContainsKey(resolved)) return EvaluateField(resolved, values);
            if (values.TryGetValue(resolved, out FormulaValue value)) return value;
            if (_options.Parameters is not null && _options.Parameters.TryGetValue(resolved, out FormulaValue parameter)) return parameter;
            throw new FormulaRuntimeException(FormulaErrorCode.FORMULA_REFERENCE_UNKNOWN, $"Referencia desconocida '{name}'.");
        }

        private FormulaValue ApplyUnary(UnaryNode unary, Dictionary<string, FormulaValue> values)
        {
            double value = EvaluateNode(unary.Operand, values).AsNumber();
            return FormulaValue.NumberValue(unary.Operator == "-" ? -value : value);
        }

        private FormulaValue ApplyBinary(BinaryNode binary, Dictionary<string, FormulaValue> values)
        {
            FormulaValue left = EvaluateNode(binary.Left, values), right = EvaluateNode(binary.Right, values);
            if (binary.Operator is "=" or "<>" or "<" or "<=" or ">" or ">=")
            {
                int comparison = Compare(left, right);
                return FormulaValue.BooleanValue(binary.Operator switch { "=" => comparison == 0, "<>" => comparison != 0, "<" => comparison < 0, "<=" => comparison <= 0, ">" => comparison > 0, _ => comparison >= 0 });
            }
            double a = left.AsNumber(), b = right.AsNumber();
            return binary.Operator switch
            {
                "+" => FormulaValue.NumberValue(a + b), "-" => FormulaValue.NumberValue(a - b), "*" => FormulaValue.NumberValue(a * b),
                "/" when b == 0 => throw new FormulaRuntimeException(FormulaErrorCode.FORMULA_DIVISION_BY_ZERO, "División por cero."),
                "/" => FormulaValue.NumberValue(a / b), "^" => FormulaValue.NumberValue(Math.Pow(a, b)),
                _ => throw new FormulaRuntimeException(FormulaErrorCode.FORMULA_OPERATOR_UNSUPPORTED, $"Operador '{binary.Operator}' no soportado.")
            };
        }

        private FormulaValue EvaluateCall(CallNode call, Dictionary<string, FormulaValue> values)
        {
            FunctionVersionDefinition definition = _options.Registry.Resolve(call.Name, _options.Pinning?.FunctionVersion(call.Name), _options.RequirePinnedDependencies);
            ValidateArity(definition, call.Arguments.Count);
            using FunctionScope scope = EnterFunction(definition);
            if (definition.HandlerKey?.Equals("IF_V1", StringComparison.OrdinalIgnoreCase) == true)
                return EvaluateNode(call.Arguments[0], values).AsBoolean() ? EvaluateNode(call.Arguments[1], values) : EvaluateNode(call.Arguments[2], values);
            if (definition.HandlerKey?.Equals("IFERROR_V1", StringComparison.OrdinalIgnoreCase) == true)
            {
                try { return EvaluateNode(call.Arguments[0], values); }
                catch (FormulaRuntimeException ex) when (ex.Code is FormulaErrorCode.FORMULA_DIVISION_BY_ZERO or FormulaErrorCode.FORMULA_TYPE_MISMATCH or FormulaErrorCode.FORMULA_ARGUMENT_INVALID) { return EvaluateNode(call.Arguments[1], values); }
            }
            FormulaValue[] args = call.Arguments.Select(argument => EvaluateNode(argument, values)).ToArray();
            ValidateArgumentTypes(definition, args);
            return definition.IsComposite ? EvaluateComposite(definition, args) : ExecuteNative(definition, args);
        }

        private FormulaValue EvaluateComposite(FunctionVersionDefinition definition, IReadOnlyList<FormulaValue> args)
        {
            Node body = Parse(definition.DefinitionDsl!, _options.Registry.FunctionCodes);
            var local = new Dictionary<string, FormulaValue>(_options.Parameters ?? new Dictionary<string, FormulaValue>(), StringComparer.OrdinalIgnoreCase);
            for (int index = 0; index < args.Count; index++)
            {
                FunctionArgumentDefinition? argument = definition.Arguments.FirstOrDefault(a => a.Position == index + 1) ?? definition.Arguments.LastOrDefault(a => a.Variadic);
                if (argument is null) throw new FormulaRuntimeException(FormulaErrorCode.FORMULA_ARGUMENT_INVALID, "Argumento compuesto no declarado.");
                local[argument.Code] = args[index];
            }
            FormulaExpressionAnalysis analysis = Analyze(body);
            var allowed = definition.Arguments.Select(a => a.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);
            allowed.UnionWith(_options.Parameters?.Keys ?? Array.Empty<string>());
            if (analysis.ReferencedNames.Any(name => !allowed.Contains(name))) throw new FormulaRuntimeException(FormulaErrorCode.FORMULA_REFERENCE_UNKNOWN, "Composite function scope contains an undeclared reference.");
            return EvaluateNode(body, local);
        }

        private FormulaValue ExecuteNative(FunctionVersionDefinition definition, IReadOnlyList<FormulaValue> args)
        {
            if (!NativeFunctionCatalog.Matches(definition.Code, definition.HandlerKey)) throw new FormulaRuntimeException(FormulaErrorCode.FORMULA_FUNCTION_UNSUPPORTED, $"Handler no permitido para '{definition.Identity}'.");
            return definition.HandlerKey!.ToUpperInvariant() switch
            {
                "ROUND_V1" => FormulaValue.NumberValue(Math.Round(args[0].AsNumber(), ToDigits(args[1]), MidpointRounding.AwayFromZero)),
                "ROUNDDOWN_V1" => RoundDown(args), "MAX_V1" => FormulaValue.NumberValue(args.Select(a => a.AsNumber()).Max()),
                "MIN_V1" => FormulaValue.NumberValue(args.Select(a => a.AsNumber()).Min()), "MOD_V1" => Mod(args),
                "OR_V1" => FormulaValue.BooleanValue(args.Any(a => a.AsBoolean())), "AND_V1" => FormulaValue.BooleanValue(args.All(a => a.AsBoolean())),
                "LOOKUP_V1" => ExecuteLookup(args), _ => throw new FormulaRuntimeException(FormulaErrorCode.FORMULA_FUNCTION_UNSUPPORTED, $"Handler '{definition.HandlerKey}' no permitido.")
            };
        }

        private FormulaValue ExecuteLookup(IReadOnlyList<FormulaValue> args)
        {
            if (_options.RequirePinnedDependencies && _options.Pinning is null)
                throw new FormulaRuntimeException(FormulaErrorCode.FORMULA_ARGUMENT_INVALID, "Published LOOKUP requires pinned catalog semantics.");
            if (_options.Lookup is null) throw new FormulaRuntimeException(FormulaErrorCode.FORMULA_REFERENCE_UNKNOWN, "No existe resolver de catálogos configurado.");
            if (args[0].Type != FormulaValueType.Text) throw new FormulaRuntimeException(FormulaErrorCode.FORMULA_TYPE_MISMATCH, "LOOKUP requiere código de catálogo textual.");
            if (_options.RequirePinnedDependencies && !_options.Pinning!.CatalogSnapshots.ContainsKey(args[0].Text ?? string.Empty))
                throw new FormulaRuntimeException(FormulaErrorCode.FORMULA_ARGUMENT_INVALID, "Published LOOKUP requires pinned catalog semantics.");
            return _options.Lookup.Lookup(args[0].Text ?? string.Empty, args[1], args.Count == 3 ? args[2].Text : null);
        }

        private FunctionScope EnterFunction(FunctionVersionDefinition definition)
        {
            if (++_functionCalls > _options.EffectiveLimits.MaxFunctionCalls) throw new FormulaRuntimeException(FormulaErrorCode.FORMULA_LIMIT_EXCEEDED, "Cantidad máxima de llamadas de función excedida.");
            if (_functionPath.Count >= _options.EffectiveLimits.MaxFunctionDepth) throw new FormulaRuntimeException(FormulaErrorCode.FORMULA_LIMIT_EXCEEDED, "Profundidad máxima de funciones excedida.");
            if (definition.IsComposite && _functionPath.Contains(definition.Identity, StringComparer.OrdinalIgnoreCase)) throw new FormulaRuntimeException(FormulaErrorCode.FORMULA_CYCLE, "Recursión de función detectada.");
            _functionPath.Push(definition.Identity); return new FunctionScope(_functionPath);
        }

        private static void ValidateArity(FunctionVersionDefinition definition, int count)
        { if (count < definition.MinArity || definition.MaxArity.HasValue && count > definition.MaxArity.Value) throw new FormulaRuntimeException(FormulaErrorCode.FORMULA_ARGUMENT_INVALID, $"La aridad de la función '{definition.Code}' es inválida."); }
        private static void ValidateArgumentTypes(FunctionVersionDefinition definition, IReadOnlyList<FormulaValue> values)
        {
            if (definition.Arguments.Count == 0) return;
            for (int index = 0; index < values.Count; index++)
            {
                FunctionArgumentDefinition? argument = definition.Arguments.FirstOrDefault(a => a.Position == index + 1) ?? definition.Arguments.LastOrDefault(a => a.Variadic);
                if (argument is null) throw new FormulaRuntimeException(FormulaErrorCode.FORMULA_ARGUMENT_INVALID, "Argumento no declarado en la firma de la función.");
                bool valid = argument.Type switch
                {
                    "VALUE" => true,
                    "INTEGER" => values[index].Type == FormulaValueType.Number && values[index].Number == Math.Truncate(values[index].Number!.Value),
                    "DECIMAL" => values[index].Type == FormulaValueType.Number,
                    "BOOLEAN" => values[index].Type is FormulaValueType.Boolean or FormulaValueType.Number,
                    "TEXT" => values[index].Type == FormulaValueType.Text,
                    "DATE" => values[index].Type == FormulaValueType.Date,
                    _ => false
                };
                if (!valid) throw new FormulaRuntimeException(FormulaErrorCode.FORMULA_TYPE_MISMATCH, $"El argumento '{argument.Code}' no coincide con el tipo declarado.");
            }
        }
        private static FormulaExpressionAnalysis Analyze(Node ast) { var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase); var calls = new HashSet<string>(StringComparer.OrdinalIgnoreCase); Collect(ast, names, calls); return new(names, calls); }
        private static FormulaValue RoundDown(IReadOnlyList<FormulaValue> args) { int digits = ToDigits(args[1]); double factor = Math.Pow(10, digits); return FormulaValue.NumberValue(Math.Truncate(args[0].AsNumber() * factor) / factor); }
        private static FormulaValue Mod(IReadOnlyList<FormulaValue> args) { double value = args[0].AsNumber(), divisor = args[1].AsNumber(); if (divisor == 0) throw new FormulaRuntimeException(FormulaErrorCode.FORMULA_DIVISION_BY_ZERO, "MOD no acepta divisor cero."); return FormulaValue.NumberValue(value - divisor * Math.Floor(value / divisor)); }
        private static int ToDigits(FormulaValue value) { double digits = value.AsNumber(); if (digits is < -15 or > 15 || digits != Math.Truncate(digits)) throw new FormulaRuntimeException(FormulaErrorCode.FORMULA_ARGUMENT_INVALID, "La precisión debe ser un entero entre -15 y 15."); return (int)digits; }
        private static int Compare(FormulaValue left, FormulaValue right)
        {
            bool leftBlank = left.Type == FormulaValueType.Blank || left.Type == FormulaValueType.Text && string.IsNullOrEmpty(left.Text);
            bool rightBlank = right.Type == FormulaValueType.Blank || right.Type == FormulaValueType.Text && string.IsNullOrEmpty(right.Text);
            if (leftBlank || rightBlank)
            {
                if (leftBlank && rightBlank) return 0;
                return leftBlank ? -1 : 1;
            }
            if (left.Type == FormulaValueType.Number || right.Type == FormulaValueType.Number) return left.AsNumber().CompareTo(right.AsNumber());
            return string.Compare(left.Text ?? left.Boolean?.ToString(), right.Text ?? right.Boolean?.ToString(), StringComparison.OrdinalIgnoreCase);
        }
        private sealed class FunctionScope : IDisposable { private readonly Stack<string> _path; public FunctionScope(Stack<string> path) => _path = path; public void Dispose() => _path.Pop(); }
    }

    private abstract record Node { public abstract IEnumerable<string> References(); }
    private sealed record NumberNode(double Number) : Node { public override IEnumerable<string> References() => Array.Empty<string>(); }
    private sealed record BooleanNode(bool Value) : Node { public override IEnumerable<string> References() => Array.Empty<string>(); }
    private sealed record StringNode(string Text) : Node { public override IEnumerable<string> References() => Array.Empty<string>(); }
    private sealed record ReferenceNode(string Name) : Node { public override IEnumerable<string> References() => new[] { Name }; }
    private sealed record UnaryNode(string Operator, Node Operand) : Node { public override IEnumerable<string> References() => Operand.References(); }
    private sealed record BinaryNode(string Operator, Node Left, Node Right) : Node { public override IEnumerable<string> References() => Left.References().Concat(Right.References()); }
    private sealed record CallNode(string Name, List<Node> Arguments) : Node { public override IEnumerable<string> References() => Arguments.SelectMany(a => a.References()); }

    private sealed class Parser
    {
        private readonly List<Token> _tokens; private readonly HashSet<string> _functions; private int _position; private int _depth;
        public Parser(string expression, IEnumerable<string> functions) { _tokens = Lexer.Tokenize(expression); _functions = functions.ToHashSet(StringComparer.OrdinalIgnoreCase); }
        public Node Parse() { Node result = Comparison(); if (Current.Kind != TokenKind.End) throw new FormulaRuntimeException(FormulaErrorCode.FORMULA_SYNTAX_INVALID, "Token inesperado en la fórmula."); return result; }
        private Node Comparison() { Node node = Additive(); while (Current.Kind == TokenKind.Operator && Current.Text is "=" or "<>" or "<" or "<=" or ">" or ">=") node = new BinaryNode(Next().Text, node, Additive()); return node; }
        private Node Additive() { Node node = Multiplicative(); while (Current.Text is "+" or "-") node = new BinaryNode(Next().Text, node, Multiplicative()); return node; }
        private Node Multiplicative() { Node node = Power(); while (Current.Text is "*" or "/") node = new BinaryNode(Next().Text, node, Power()); return node; }
        private Node Power() { Node node = Unary(); if (Current.Text == "^") node = new BinaryNode(Next().Text, node, Power()); return node; }
        private Node Unary() { if (Current.Text is "+" or "-") return new UnaryNode(Next().Text, Unary()); return Primary(); }
        private Node Primary()
        {
            if (++_depth > MaxAstDepth) throw new FormulaRuntimeException(FormulaErrorCode.FORMULA_LIMIT_EXCEEDED, "Profundidad AST excedida.");
            try
            {
                if (Current.Kind == TokenKind.Number) return new NumberNode(double.Parse(Next().Text, CultureInfo.InvariantCulture));
                if (Current.Kind == TokenKind.String) return new StringNode(Next().Text);
                if (Current.Kind == TokenKind.Identifier)
                {
                    string name = Next().Text;
                    if (Current.Text == "(")
                    {
                        Next(); var args = new List<Node>();
                        if (Current.Text != ")") do { args.Add(Comparison()); } while (Current.Text == "," && Next().Kind != TokenKind.End);
                        Expect(")"); if (!_functions.Contains(name)) throw new FormulaRuntimeException(FormulaErrorCode.FORMULA_FUNCTION_UNSUPPORTED, $"Función '{name}' no registrada.");
                        return new CallNode(name, args);
                    }
                    if (name.Equals("TRUE", StringComparison.OrdinalIgnoreCase)) return new BooleanNode(true);
                    if (name.Equals("FALSE", StringComparison.OrdinalIgnoreCase)) return new BooleanNode(false);
                    return new ReferenceNode(name);
                }
                if (Current.Text == "(") { Next(); Node nested = Comparison(); Expect(")"); return nested; }
                throw new FormulaRuntimeException(FormulaErrorCode.FORMULA_SYNTAX_INVALID, "Se esperaba literal, referencia o paréntesis.");
            }
            finally { _depth--; }
        }
        private Token Current => _tokens[_position]; private Token Next() => _tokens[_position++];
        private void Expect(string text) { if (Current.Text != text) throw new FormulaRuntimeException(FormulaErrorCode.FORMULA_SYNTAX_INVALID, "Paréntesis o separador inválido."); Next(); }
    }

    private enum TokenKind { Number, Identifier, String, Operator, End }
    private readonly record struct Token(TokenKind Kind, string Text);
    private static class Lexer
    {
        public static List<Token> Tokenize(string input)
        {
            var result = new List<Token>();
            for (int i = 0; i < input.Length;)
            {
                char c = input[i]; if (char.IsWhiteSpace(c)) { i++; continue; }
                if (char.IsDigit(c) || c == '.') { int start = i++; while (i < input.Length && (char.IsDigit(input[i]) || input[i] == '.')) i++; if (!double.TryParse(input[start..i], NumberStyles.Float, CultureInfo.InvariantCulture, out _)) throw new FormulaRuntimeException(FormulaErrorCode.FORMULA_SYNTAX_INVALID, "Número inválido."); result.Add(new(TokenKind.Number, input[start..i])); continue; }
                if (char.IsLetter(c) || c == '_') { int start = i++; while (i < input.Length && (char.IsLetterOrDigit(input[i]) || input[i] == '_')) i++; result.Add(new(TokenKind.Identifier, input[start..i])); continue; }
                if (c == '"') { int start = ++i; while (i < input.Length && input[i] != '"') i++; if (i >= input.Length) throw new FormulaRuntimeException(FormulaErrorCode.FORMULA_SYNTAX_INVALID, "Cadena sin cierre."); result.Add(new(TokenKind.String, input[start..i++])); continue; }
                string op = c.ToString(); if (i + 1 < input.Length && input[i..(i + 2)] is "<>" or "<=" or ">=") { op = input[i..(i + 2)]; i += 2; } else i++;
                if ("+-*/^=() ,<>".Contains(c)) result.Add(new(TokenKind.Operator, op)); else throw new FormulaRuntimeException(FormulaErrorCode.FORMULA_OPERATOR_UNSUPPORTED, $"Operador '{c}' no soportado.");
            }
            result.Add(new(TokenKind.End, string.Empty)); if (result.Count > MaxTokens) throw new FormulaRuntimeException(FormulaErrorCode.FORMULA_LIMIT_EXCEEDED, "Cantidad de tokens excedida."); return result;
        }
    }
}
