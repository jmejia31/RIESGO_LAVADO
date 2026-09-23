using System.Text.RegularExpressions;

namespace RL.API.Features.MatricesRiesgos.Domain;

public static class TextoVisibleUtf8Normalizer
{
    private const string TokenReemplazo = "(?:\u00EF\u00BF\u00BD|\uFFFD)";

    private static readonly (string From, string To)[] ReemplazosMojibake =
    {
        ("\u00C3\u00A1", "á"),
        ("\u00C3\u00A9", "é"),
        ("\u00C3\u00AD", "í"),
        ("\u00C3\u00B3", "ó"),
        ("\u00C3\u00BA", "ú"),
        ("\u00C3\u00B1", "ñ"),
        ("\u00C3\u0081", "Á"),
        ("\u00C3\u0089", "É"),
        ("\u00C3\u008D", "Í"),
        ("\u00C3\u0093", "Ó"),
        ("\u00C3\u009A", "Ú"),
        ("\u00C3\u0091", "Ñ"),
        ("\u00C2\u00BF", "¿"),
        ("\u00C2\u00A1", "¡"),
        ("\u00C2\u00B0", "°")
    };

    private static readonly (Regex Pattern, string Replacement)[] ReparacionesContexto =
    {
        (CrearRegex("Identificaci" + TokenReemplazo + "n"), "Identificación"),
        (CrearRegex(TokenReemplazo + "rea\\b"), "Área"),
        (CrearRegex("Due" + TokenReemplazo + "o"), "Dueño"),
        (CrearRegex("estrat" + TokenReemplazo + "gic"), "estratégic"),
        (CrearRegex("R" + TokenReemplazo + "gimen"), "Régimen"),
        (CrearRegex("interrelaci" + TokenReemplazo + "n"), "interrelación"),
        (CrearRegex("i" + TokenReemplazo + "n"), "ión"),
        (CrearRegex("e" + TokenReemplazo + "o"), "eño"),
        (CrearRegex("est" + TokenReemplazo + "n\\b"), "están"),
        (CrearRegex("inter" + TokenReemplazo + "s\\b"), "interés"),
        (CrearRegex("t" + TokenReemplazo + "cnic"), "técnic"),
        (CrearRegex("econ" + TokenReemplazo + "mic"), "económic"),
        (CrearRegex("t" + TokenReemplazo + "rmin"), "términ"),
        (CrearRegex("garant" + TokenReemplazo + "a"), "garantía"),
        (CrearRegex("p" + TokenReemplazo + "blic"), "públic"),
        (CrearRegex("Pol" + TokenReemplazo + "tic"), "Polític"),
        (CrearRegex("pol" + TokenReemplazo + "tic"), "polític"),
        (CrearRegex("c" + TokenReemplazo + "nyuge"), "cónyuge"),
        (CrearRegex("v" + TokenReemplazo + "ncul"), "víncul")
    };

    public static bool ContieneMojibake(string? valor)
    {
        if (string.IsNullOrEmpty(valor)) return false;

        return valor.Contains('\uFFFD')
            || valor.Contains("\u00EF\u00BF\u00BD", StringComparison.Ordinal)
            || valor.Contains('\u00C3')
            || valor.Contains('\u00C2')
            || valor.Contains("\u00E2\u20AC", StringComparison.Ordinal)
            || valor.Contains("\u00F0\u0178", StringComparison.Ordinal);
    }

    public static string Normalizar(string valor)
    {
        if (string.IsNullOrEmpty(valor)) return valor;

        string resultado = valor;
        foreach ((string from, string to) in ReemplazosMojibake)
        {
            resultado = resultado.Replace(from, to, StringComparison.Ordinal);
        }

        foreach ((Regex pattern, string replacement) in ReparacionesContexto)
        {
            resultado = pattern.Replace(resultado, replacement);
        }

        return resultado;
    }

    public static string? NormalizarNullable(string? valor) =>
        valor is null ? null : Normalizar(valor);

    private static Regex CrearRegex(string pattern) =>
        new(pattern, RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
}
