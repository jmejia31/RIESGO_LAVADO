using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using RL.API.Features.MatricesRiesgos.Contracts;

namespace RL.API.Features.MatricesRiesgos.Domain;

public static partial class CalculoConfiguracionValidation
{
    private static readonly HashSet<string> Types = new(StringComparer.OrdinalIgnoreCase)
    { "INTEGER", "DECIMAL", "BOOLEAN", "TEXT", "DATE" };

    [GeneratedRegex("^[A-Z][A-Z0-9_]{0,79}$", RegexOptions.CultureInvariant)]
    private static partial Regex CodeRegex();

    public static string NormalizeCode(string? value, string field)
    {
        string code = value?.Trim().ToUpperInvariant() ?? string.Empty;
        if (!CodeRegex().IsMatch(code)) throw new InvalidOperationException($"{field} inválido.");
        return code;
    }

    public static void ValidateFormulaVersion(string expression, string type)
    {
        if (string.IsNullOrWhiteSpace(expression) || expression.Length > FormulaEngine.MaxExpressionLength)
            throw new InvalidOperationException("La expresión debe contener entre 1 y 4096 caracteres.");
        if (string.IsNullOrWhiteSpace(type) || type.Trim().Length > 20)
            throw new InvalidOperationException("El tipo de resultado es obligatorio.");
    }

    public static (string Tipo, string? Handler, string? Dsl, string Signature) ValidateFunctionVersion(CrearFuncionVersionDto dto)
    {
        string type = dto.Tipo.Trim().ToUpperInvariant();
        if (type is not ("NATIVE" or "COMPOSITE")) throw new InvalidOperationException("El tipo de función debe ser NATIVE o COMPOSITE.");
        string? handler = string.IsNullOrWhiteSpace(dto.HandlerKey) ? null : NormalizeCode(dto.HandlerKey, "HandlerKey");
        string? dsl = string.IsNullOrWhiteSpace(dto.DefinicionDsl) ? null : dto.DefinicionDsl.Trim();
        if (type == "NATIVE" && (handler is null || dsl is not null)) throw new InvalidOperationException("Una función NATIVE requiere HandlerKey y no admite DSL.");
        if (type == "COMPOSITE" && (handler is not null || dsl is null)) throw new InvalidOperationException("Una función COMPOSITE requiere DSL y no admite HandlerKey.");
        if (dsl?.Length > FormulaEngine.MaxExpressionLength) throw new InvalidOperationException("El DSL excede el límite de 4096 caracteres.");
        if (dto.MinArity < 0 || dto.MaxArity is < 0 || (dto.MaxArity.HasValue && dto.MaxArity.Value < dto.MinArity)) throw new InvalidOperationException("La aridad de la funcion es invalida.");
        if (dto.Argumentos.Count == 0 && dto.MinArity > 0) throw new InvalidOperationException("La firma debe declarar sus argumentos.");
        var args = dto.Argumentos.OrderBy(a => a.Posicion).ToList();
        if (args.Select(a => a.Posicion).Distinct().Count() != args.Count || args.Select(a => a.Codigo.Trim().ToUpperInvariant()).Distinct().Count() != args.Count)
            throw new InvalidOperationException("Los argumentos no pueden repetir posición ni código.");
        foreach (var arg in args)
        {
            NormalizeCode(arg.Codigo, "Código de argumento");
            if (!Types.Contains(arg.Tipo.Trim().ToUpperInvariant())) throw new InvalidOperationException("Tipo de argumento no soportado.");
            if (arg.Requerido && !string.IsNullOrWhiteSpace(arg.ValorDefaultJson)) throw new InvalidOperationException("Un argumento requerido no puede tener valor por defecto.");
            if (!string.IsNullOrWhiteSpace(arg.ValorDefaultJson)) JsonDocument.Parse(arg.ValorDefaultJson);
        }
        string signature = JsonSerializer.Serialize(new { minArity = dto.MinArity, maxArity = dto.MaxArity, arguments = args.Select(a => new { position = a.Posicion, code = a.Codigo.Trim().ToUpperInvariant(), type = a.Tipo.Trim().ToUpperInvariant(), required = a.Requerido, variadic = a.Variadic }) });
        return (type, handler, dsl, signature);
    }

    public static string ValidateParameterVersion(CrearParametroVersionDto dto)
    {
        string type = dto.Tipo.Trim().ToUpperInvariant();
        if (!Types.Contains(type)) throw new InvalidOperationException("Tipo de parámetro no soportado.");
        int populated = (dto.ValorEntero.HasValue ? 1 : 0) + (dto.ValorDecimal.HasValue ? 1 : 0) + (dto.ValorBooleano.HasValue ? 1 : 0) + (dto.ValorTexto is not null ? 1 : 0) + (dto.ValorFecha.HasValue ? 1 : 0);
        if (populated != 1) throw new InvalidOperationException("La versión debe contener exactamente un valor tipado.");
        bool valid = type switch
        {
            "INTEGER" => dto.ValorEntero.HasValue && !dto.ValorDecimal.HasValue && !dto.ValorBooleano.HasValue && dto.ValorTexto is null && !dto.ValorFecha.HasValue,
            "DECIMAL" => dto.ValorDecimal.HasValue && !dto.ValorEntero.HasValue && !dto.ValorBooleano.HasValue && dto.ValorTexto is null && !dto.ValorFecha.HasValue,
            "BOOLEAN" => dto.ValorBooleano.HasValue && !dto.ValorEntero.HasValue && !dto.ValorDecimal.HasValue && dto.ValorTexto is null && !dto.ValorFecha.HasValue,
            "TEXT" => dto.ValorTexto is not null && !dto.ValorEntero.HasValue && !dto.ValorDecimal.HasValue && !dto.ValorBooleano.HasValue && !dto.ValorFecha.HasValue,
            "DATE" => dto.ValorFecha.HasValue && !dto.ValorEntero.HasValue && !dto.ValorDecimal.HasValue && !dto.ValorBooleano.HasValue && dto.ValorTexto is null,
            _ => false
        };
        if (!valid) throw new InvalidOperationException("El valor no corresponde al tipo declarado.");
        return type;
    }

    public static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
