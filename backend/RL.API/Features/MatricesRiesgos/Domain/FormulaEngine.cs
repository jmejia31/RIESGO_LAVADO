using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;

namespace RL.API.Features.MatricesRiesgos.Domain;

[SuppressMessage("Naming", "CA1707", Justification = "Los códigos forman parte del contrato estable de errores del motor DSL.")]
public enum FormulaErrorCode
{
    FORMULA_SYNTAX_INVALID,
    FORMULA_OPERATOR_UNSUPPORTED,
    FORMULA_FUNCTION_UNSUPPORTED,
    FORMULA_REFERENCE_UNKNOWN,
    FORMULA_SELF_REFERENCE,
    FORMULA_CYCLE,
    FORMULA_TYPE_MISMATCH,
    FORMULA_DIVISION_BY_ZERO,
    FORMULA_ARGUMENT_INVALID,
    FORMULA_LIMIT_EXCEEDED
}

public sealed record FormulaDiagnostic(FormulaErrorCode Code, string Field, string Message);

public sealed class FormulaEvaluationResult
{
    public Dictionary<string, object?> Values { get; } = new(StringComparer.OrdinalIgnoreCase);
    public List<FormulaDiagnostic> Errors { get; } = new();
    public bool Success => Errors.Count == 0;
}

/// <summary>
/// Parser y evaluador determinista del DSL de fórmulas de Matrices.
/// No interpreta código: únicamente construye un AST de tokens permitidos.
/// </summary>
public sealed class FormulaEngine
{
    public const int MaxExpressionLength = 4096;
    public const int MaxTokens = 512;
    public const int MaxAstDepth = 64;
    public const int MaxOperations = 2048;

    private static readonly HashSet<string> Functions = new(StringComparer.OrdinalIgnoreCase)
    { "IF", "IFERROR", "ROUND", "ROUNDDOWN", "MAX", "MOD", "OR" };

    public IReadOnlyList<FormulaDiagnostic> ValidateDefinition(string json)
    {
        var errors = new List<FormulaDiagnostic>();
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (root.TryGetProperty("definicionFormulario", out var nested)) root = nested;
            var fields = ReadFields(root, errors);
            var formulas = new Dictionary<string, (string Expression, Node Ast)>(StringComparer.OrdinalIgnoreCase);
            foreach (var field in fields.Values.Where(f => !string.IsNullOrWhiteSpace(f.Expression)))
            {
                try
                {
                    var ast = Parse(field.Expression!);
                    formulas[field.Key] = (field.Expression!, ast);
                    ValidateReferences(ast, fields.Keys, field.Key, errors);
                }
                catch (FormulaException ex) { errors.Add(new(ex.Code, field.Key, ex.Message)); }
            }
            DetectCycles(formulas, errors);
        }
        catch (JsonException ex) { errors.Add(new(FormulaErrorCode.FORMULA_SYNTAX_INVALID, "JSON", ex.Message)); }
        return errors;
    }

    public FormulaEvaluationResult Evaluate(string definitionJson, string responsesJson)
    {
        var result = new FormulaEvaluationResult();
        using var definition = JsonDocument.Parse(definitionJson);
        using var responses = JsonDocument.Parse(string.IsNullOrWhiteSpace(responsesJson) ? "{}" : responsesJson);
        var root = definition.RootElement;
        if (root.TryGetProperty("definicionFormulario", out var nested)) root = nested;
        var fields = ReadFields(root, result.Errors);
        if (result.Errors.Count > 0) return result;
        var values = new Dictionary<string, Value>(StringComparer.OrdinalIgnoreCase);
        if (responses.RootElement.ValueKind == JsonValueKind.Object)
            foreach (var property in responses.RootElement.EnumerateObject()) values[property.Name] = Value.FromJson(property.Value);
        var formulas = new Dictionary<string, Node>(StringComparer.OrdinalIgnoreCase);
        foreach (var field in fields.Values.Where(f => !string.IsNullOrWhiteSpace(f.Expression)))
        {
            try { formulas[field.Key] = Parse(field.Expression!); }
            catch (FormulaException ex) { result.Errors.Add(new(ex.Code, field.Key, ex.Message)); }
        }
        if (result.Errors.Count > 0) return result;
        DetectCyclesOrThrow(formulas);
        var state = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var cache = new Dictionary<string, Value>(StringComparer.OrdinalIgnoreCase);
        foreach (var key in formulas.Keys)
        {
            try { result.Values[key] = EvaluateField(key, formulas, values, cache, state).ToObject(); }
            catch (FormulaException ex) { result.Errors.Add(new(ex.Code, key, ex.Message)); }
        }
        return result;
    }

    private static Value EvaluateField(string key, Dictionary<string, Node> formulas, Dictionary<string, Value> values, Dictionary<string, Value> cache, HashSet<string> state)
    {
        if (cache.TryGetValue(key, out var cached)) return cached;
        if (!state.Add(key)) throw new FormulaException(FormulaErrorCode.FORMULA_CYCLE, "Dependencia circular detectada.");
        var value = EvaluateNode(formulas[key], formulas, values, cache, state);
        state.Remove(key); cache[key] = value; values[key] = value; return value;
    }

    private static Value EvaluateNode(Node node, Dictionary<string, Node> formulas, Dictionary<string, Value> values, Dictionary<string, Value> cache, HashSet<string> state)
    {
        switch (node)
        {
            case NumberNode n: return Value.Number(n.Number);
            case StringNode s: return Value.String(s.Text);
            case ReferenceNode r:
                if (formulas.ContainsKey(r.Name)) return EvaluateField(r.Name, formulas, values, cache, state);
                if (values.TryGetValue(r.Name, out var value)) return value;
                throw new FormulaException(FormulaErrorCode.FORMULA_REFERENCE_UNKNOWN, $"Referencia desconocida '{r.Name}'.");
            case UnaryNode u:
                var operand = EvaluateNode(u.Operand, formulas, values, cache, state).AsNumber();
                return Value.Number(u.Operator == "-" ? -operand : operand);
            case BinaryNode b:
                var left = EvaluateNode(b.Left, formulas, values, cache, state);
                var right = EvaluateNode(b.Right, formulas, values, cache, state);
                return ApplyBinary(b.Operator, left, right);
            case CallNode c: return EvaluateCall(c, formulas, values, cache, state);
            default: throw new FormulaException(FormulaErrorCode.FORMULA_SYNTAX_INVALID, "AST no soportado.");
        }
    }

    private static Value EvaluateCall(CallNode call, Dictionary<string, Node> formulas, Dictionary<string, Value> values, Dictionary<string, Value> cache, HashSet<string> state)
    {
        if (call.Name.Equals("IF", StringComparison.OrdinalIgnoreCase))
        {
            RequireArity(call, 3); return EvaluateNode(call.Arguments[0], formulas, values, cache, state).AsBoolean()
                ? EvaluateNode(call.Arguments[1], formulas, values, cache, state) : EvaluateNode(call.Arguments[2], formulas, values, cache, state);
        }
        if (call.Name.Equals("IFERROR", StringComparison.OrdinalIgnoreCase))
        {
            RequireArity(call, 2); try { return EvaluateNode(call.Arguments[0], formulas, values, cache, state); }
            catch (FormulaException ex) when (ex.Code is FormulaErrorCode.FORMULA_DIVISION_BY_ZERO or FormulaErrorCode.FORMULA_TYPE_MISMATCH or FormulaErrorCode.FORMULA_ARGUMENT_INVALID)
            { return EvaluateNode(call.Arguments[1], formulas, values, cache, state); }
        }
        var args = call.Arguments.Select(a => EvaluateNode(a, formulas, values, cache, state)).ToArray();
        if (call.Name.Equals("OR", StringComparison.OrdinalIgnoreCase)) { if (args.Length is < 1 or > 32) throw new FormulaException(FormulaErrorCode.FORMULA_ARGUMENT_INVALID, "OR requiere entre 1 y 32 argumentos."); return Value.Boolean(args.Any(a => a.AsBoolean())); }
        if (call.Name.Equals("MAX", StringComparison.OrdinalIgnoreCase)) { if (args.Length is < 1 or > 32) throw new FormulaException(FormulaErrorCode.FORMULA_ARGUMENT_INVALID, "MAX requiere entre 1 y 32 argumentos."); return Value.Number(args.Max(a => a.AsNumber())); }
        RequireArity(call, 2);
        if (call.Name.Equals("ROUND", StringComparison.OrdinalIgnoreCase)) return Value.Number(Math.Round(args[0].AsNumber(), ToDigits(args[1]), MidpointRounding.AwayFromZero));
        if (call.Name.Equals("ROUNDDOWN", StringComparison.OrdinalIgnoreCase)) { var d = ToDigits(args[1]); var factor = Math.Pow(10, d); return Value.Number(Math.Truncate(args[0].AsNumber() * factor) / factor); }
        if (call.Name.Equals("MOD", StringComparison.OrdinalIgnoreCase)) { var divisor = args[1].AsNumber(); if (divisor == 0) throw new FormulaException(FormulaErrorCode.FORMULA_DIVISION_BY_ZERO, "MOD no acepta divisor cero."); return Value.Number(args[0].AsNumber() - divisor * Math.Floor(args[0].AsNumber() / divisor)); }
        throw new FormulaException(FormulaErrorCode.FORMULA_FUNCTION_UNSUPPORTED, $"Función '{call.Name}' no soportada.");
    }

    private static Value ApplyBinary(string op, Value left, Value right)
    {
        if (op is "=" or "<>" or "<" or "<=" or ">" or ">=") { var comparison = left.Compare(right); return Value.Boolean(op switch { "=" => comparison == 0, "<>" => comparison != 0, "<" => comparison < 0, "<=" => comparison <= 0, ">" => comparison > 0, _ => comparison >= 0 }); }
        var a = left.AsNumber(); var b = right.AsNumber();
        return op switch { "+" => Value.Number(a + b), "-" => Value.Number(a - b), "*" => Value.Number(a * b), "/" when b == 0 => throw new FormulaException(FormulaErrorCode.FORMULA_DIVISION_BY_ZERO, "División por cero."), "/" => Value.Number(a / b), "^" => Value.Number(Math.Pow(a, b)), _ => throw new FormulaException(FormulaErrorCode.FORMULA_OPERATOR_UNSUPPORTED, $"Operador '{op}' no soportado.") };
    }

    private static int ToDigits(Value value) { var digits = value.AsNumber(); if (digits is < -15 or > 15 || digits != Math.Truncate(digits)) throw new FormulaException(FormulaErrorCode.FORMULA_ARGUMENT_INVALID, "La precisión debe ser un entero entre -15 y 15."); return (int)digits; }
    private static void RequireArity(CallNode call, int arity) { if (call.Arguments.Count != arity) throw new FormulaException(FormulaErrorCode.FORMULA_ARGUMENT_INVALID, $"{call.Name} requiere {arity} argumentos."); }

    private static Dictionary<string, Field> ReadFields(JsonElement root, List<FormulaDiagnostic> errors)
    {
        var fields = new Dictionary<string, Field>(StringComparer.OrdinalIgnoreCase);
        if (!root.TryGetProperty("secciones", out var sections) || sections.ValueKind != JsonValueKind.Array) return fields;
        foreach (var section in sections.EnumerateArray()) if (section.TryGetProperty("campos", out var items) && items.ValueKind == JsonValueKind.Array)
            foreach (var item in items.EnumerateArray())
            {
                var key = Text(item, "clave") ?? Text(item, "rutaDatos") ?? Text(item, "identificador"); if (string.IsNullOrWhiteSpace(key)) continue;
                var expression = Text(item, "formula") ?? Text(item, "calculo") ?? Text(item, "referenciaCalculo");
                if (!fields.TryAdd(key, new Field(key, expression))) errors.Add(new(FormulaErrorCode.FORMULA_SYNTAX_INVALID, key, "La clave del campo está duplicada."));
            }
        return fields;
    }
    private static string? Text(JsonElement element, string property) => element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString()?.Trim() : null;

    private static Node Parse(string expression)
    {
        if (expression.StartsWith("=")) expression = expression[1..].Trim();
        if (expression.Length == 0 || expression.Length > MaxExpressionLength) throw new FormulaException(FormulaErrorCode.FORMULA_LIMIT_EXCEEDED, "Longitud de fórmula fuera de límites.");
        var parser = new Parser(expression); return parser.Parse();
    }
    private static void ValidateReferences(Node node, IEnumerable<string> keys, string field, List<FormulaDiagnostic> errors)
    {
        var known = new HashSet<string>(keys, StringComparer.OrdinalIgnoreCase);
        foreach (var reference in node.References()) if (!known.Contains(reference)) errors.Add(new(FormulaErrorCode.FORMULA_REFERENCE_UNKNOWN, field, $"Referencia desconocida '{reference}'."));
        if (node.References().Any(r => r.Equals(field, StringComparison.OrdinalIgnoreCase))) errors.Add(new(FormulaErrorCode.FORMULA_SELF_REFERENCE, field, "La fórmula se referencia a sí misma."));
    }
    private static void DetectCycles(Dictionary<string, (string Expression, Node Ast)> formulas, List<FormulaDiagnostic> errors)
    { try { DetectCyclesOrThrow(formulas.ToDictionary(p => p.Key, p => p.Value.Ast, StringComparer.OrdinalIgnoreCase)); } catch (FormulaException ex) { errors.Add(new(ex.Code, "formula", ex.Message)); } }
    private static void DetectCyclesOrThrow(Dictionary<string, Node> formulas)
    { foreach (var key in formulas.Keys) Visit(key, formulas, new HashSet<string>(StringComparer.OrdinalIgnoreCase)); }
    private static void Visit(string key, Dictionary<string, Node> formulas, HashSet<string> path)
    { if (!path.Add(key)) throw new FormulaException(FormulaErrorCode.FORMULA_CYCLE, "Dependencia circular detectada."); if (formulas.TryGetValue(key, out var ast)) foreach (var reference in ast.References().Where(formulas.ContainsKey)) Visit(reference, formulas, new HashSet<string>(path, StringComparer.OrdinalIgnoreCase)); }

    private sealed record Field(string Key, string? Expression);
    private sealed class FormulaException(FormulaErrorCode code, string message) : Exception(message) { public FormulaErrorCode Code { get; } = code; }

    private abstract record Node { public abstract IEnumerable<string> References(); }
    private sealed record NumberNode(double Number) : Node { public override IEnumerable<string> References() => Array.Empty<string>(); }
    private sealed record StringNode(string Text) : Node { public override IEnumerable<string> References() => Array.Empty<string>(); }
    private sealed record ReferenceNode(string Name) : Node { public override IEnumerable<string> References() => new[] { Name }; }
    private sealed record UnaryNode(string Operator, Node Operand) : Node { public override IEnumerable<string> References() => Operand.References(); }
    private sealed record BinaryNode(string Operator, Node Left, Node Right) : Node { public override IEnumerable<string> References() => Left.References().Concat(Right.References()); }
    private sealed record CallNode(string Name, List<Node> Arguments) : Node { public override IEnumerable<string> References() => Arguments.SelectMany(a => a.References()); }

    private readonly record struct Value(double? Numeric, string? Text, bool? Logical)
    {
        public static Value Number(double value) => new(value, null, null); public static Value String(string value) => new(null, value, null); public static Value Boolean(bool value) => new(null, null, value);
        public static Value FromJson(JsonElement element) => element.ValueKind switch { JsonValueKind.Number when element.TryGetDouble(out var n) => Number(n), JsonValueKind.True => Boolean(true), JsonValueKind.False => Boolean(false), JsonValueKind.String => String(element.GetString() ?? string.Empty), _ => String(string.Empty) };
        public double AsNumber() => Numeric ?? (double.TryParse(Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) ? parsed : throw new FormulaException(FormulaErrorCode.FORMULA_TYPE_MISMATCH, "Se esperaba un número."));
        public bool AsBoolean() => Logical ?? (Numeric.HasValue ? Numeric.Value != 0 : !string.IsNullOrEmpty(Text));
        public int Compare(Value other) { if (Numeric.HasValue || other.Numeric.HasValue) return AsNumber().CompareTo(other.AsNumber()); return string.Compare(Text ?? Logical?.ToString(), other.Text ?? other.Logical?.ToString(), StringComparison.OrdinalIgnoreCase); }
        public object? ToObject() => Numeric ?? (object?)Text ?? Logical;
    }

    private sealed class Parser
    {
        private readonly List<Token> _tokens; private int _position; private int _depth;
        public Parser(string expression) { _tokens = Lexer.Tokenize(expression); }
        public Node Parse() { var result = Comparison(); if (Current.Kind != TokenKind.End) throw new FormulaException(FormulaErrorCode.FORMULA_SYNTAX_INVALID, "Token inesperado en la fórmula."); return result; }
        private Node Comparison() { var node = Additive(); while (Current.Kind == TokenKind.Operator && Current.Text is "=" or "<>" or "<" or "<=" or ">" or ">=") node = new BinaryNode(Next().Text, node, Additive()); return node; }
        private Node Additive() { var node = Multiplicative(); while (Current.Text is "+" or "-") node = new BinaryNode(Next().Text, node, Multiplicative()); return node; }
        private Node Multiplicative() { var node = Power(); while (Current.Text is "*" or "/") node = new BinaryNode(Next().Text, node, Power()); return node; }
        private Node Power() { var node = Unary(); if (Current.Text == "^") node = new BinaryNode(Next().Text, node, Power()); return node; }
        private Node Unary() { if (Current.Text is "+" or "-") return new UnaryNode(Next().Text, Unary()); return Primary(); }
        private Node Primary() { if (++_depth > MaxAstDepth) throw new FormulaException(FormulaErrorCode.FORMULA_LIMIT_EXCEEDED, "Profundidad AST excedida."); try { if (Current.Kind == TokenKind.Number) return new NumberNode(double.Parse(Next().Text, CultureInfo.InvariantCulture)); if (Current.Kind == TokenKind.String) return new StringNode(Next().Text); if (Current.Kind == TokenKind.Identifier) { var name = Next().Text; if (Current.Text == "(") { Next(); var args = new List<Node>(); if (Current.Text != ")") { do { args.Add(Comparison()); } while (Current.Text == "," && Next().Kind != TokenKind.End); } Expect(")"); if (!Functions.Contains(name)) throw new FormulaException(FormulaErrorCode.FORMULA_FUNCTION_UNSUPPORTED, $"Función '{name}' no soportada."); return new CallNode(name, args); } return new ReferenceNode(name); } if (Current.Text == "(") { Next(); var nested = Comparison(); Expect(")"); return nested; } throw new FormulaException(FormulaErrorCode.FORMULA_SYNTAX_INVALID, "Se esperaba literal, referencia o paréntesis."); } finally { _depth--; } }
        private Token Current => _tokens[_position]; private Token Next() => _tokens[_position++]; private void Expect(string text) { if (Current.Text != text) throw new FormulaException(FormulaErrorCode.FORMULA_SYNTAX_INVALID, "Paréntesis o separador inválido."); Next(); }
    }

    private enum TokenKind { Number, Identifier, String, Operator, End }
    private readonly record struct Token(TokenKind Kind, string Text);
    private static class Lexer
    {
        public static List<Token> Tokenize(string input)
        { var result = new List<Token>(); for (var i = 0; i < input.Length;) { var c = input[i]; if (char.IsWhiteSpace(c)) { i++; continue; } if (char.IsDigit(c) || c == '.') { var start = i++; while (i < input.Length && (char.IsDigit(input[i]) || input[i] == '.')) i++; if (!double.TryParse(input[start..i], NumberStyles.Float, CultureInfo.InvariantCulture, out _)) throw new FormulaException(FormulaErrorCode.FORMULA_SYNTAX_INVALID, "Número inválido."); result.Add(new(TokenKind.Number, input[start..i])); continue; } if (char.IsLetter(c) || c == '_') { var start = i++; while (i < input.Length && (char.IsLetterOrDigit(input[i]) || input[i] == '_')) i++; result.Add(new(TokenKind.Identifier, input[start..i])); continue; } if (c == '"') { var start = ++i; while (i < input.Length && input[i] != '"') i++; if (i >= input.Length) throw new FormulaException(FormulaErrorCode.FORMULA_SYNTAX_INVALID, "Cadena sin cierre."); result.Add(new(TokenKind.String, input[start..i++])); continue; } var op = c.ToString(); if (i + 1 < input.Length && (input[i..(i + 2)] is "<>" or "<=" or ">=")) { op = input[i..(i + 2)]; i += 2; } else i++; if ("+-*/^=() ,<>".Contains(c)) result.Add(new(op is "(" or ")" or "," ? TokenKind.Operator : TokenKind.Operator, op)); else throw new FormulaException(FormulaErrorCode.FORMULA_OPERATOR_UNSUPPORTED, $"Operador '{c}' no soportado."); } result.Add(new(TokenKind.End, string.Empty)); if (result.Count > MaxTokens) throw new FormulaException(FormulaErrorCode.FORMULA_LIMIT_EXCEEDED, "Cantidad de tokens excedida."); return result; }
    }
}
