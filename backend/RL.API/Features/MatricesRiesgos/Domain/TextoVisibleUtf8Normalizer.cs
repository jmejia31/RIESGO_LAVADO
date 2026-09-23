using System.Text.RegularExpressions;

namespace RL.API.Features.MatricesRiesgos.Domain;

/// <summary>Detecta y repara patrones conocidos de texto visible corrupto.</summary>
public static class TextoVisibleUtf8Normalizer
{
    private static readonly (string From, string To)[] ReemplazosMojibake =
    {
        ("\u00C3\u00A1", "\u00E1"), ("\u00C3\u00A9", "\u00E9"),
        ("\u00C3\u00AD", "\u00ED"), ("\u00C3\u00B3", "\u00F3"),
        ("\u00C3\u00BA", "\u00FA"), ("\u00C3\u00B1", "\u00F1"),
        ("\u00C3\u0081", "\u00C1"), ("\u00C3\u0089", "\u00C9"),
        ("\u00C3\u008D", "\u00CD"), ("\u00C3\u0093", "\u00D3"),
        ("\u00C3\u009A", "\u00DA"), ("\u00C3\u0091", "\u00D1"),
        ("\u00C2\u00BF", "\u00BF"), ("\u00C2\u00A1", "\u00A1"),
        ("\u00C2\u00B0", "\u00B0"), ("\u00C2\u00BA", "\u00BA"),
        ("\u00C2\u00AA", "\u00AA")
    };

    private static readonly (string From, string To)[] ReparacionesContextuales =
    {
        ("informaci\u00BFn", "informaci\u00F3n"), ("evaluaci\u00BFn", "evaluaci\u00F3n"),
        ("gesti\u00BFn", "gesti\u00F3n"), ("aprobaci\u00BFn", "aprobaci\u00F3n"),
        ("adjudicaci\u00BFn", "adjudicaci\u00F3n"), ("contrataci\u00BFn", "contrataci\u00F3n"),
        ("licitaci\u00BFn", "licitaci\u00F3n"), ("pensi\u00BFn", "pensi\u00F3n"),
        ("prestaci\u00BFn", "prestaci\u00F3n"), ("definici\u00BFn", "definici\u00F3n"),
        ("ejecuci\u00BFn", "ejecuci\u00F3n"), ("supervisi\u00BFn", "supervisi\u00F3n"),
        ("prevenci\u00BFn", "prevenci\u00F3n"), ("Due\u00BFo", "Due\u00F1o"),
        ("due\u00BFo", "due\u00F1o"), ("v\u00BFnculo", "v\u00EDnculo"),
        ("t\u00BFrmin", "t\u00E9rmin"), ("t\u00BFcnica", "t\u00E9cnica"),
        ("p\u00BAblica", "p\u00FAblica"), ("m\u00BFs", "m\u00E1s"),
        ("Descripci\u00BFn", "Descripción"), ("descripci\u00BFn", "descripción"),
        ("vinculaci\u00BFn", "vinculación"), ("P\u00BFrdidas", "Pérdidas"),
        ("p\u00BFrdidas", "pérdidas"), ("econ\u00BFmicas", "económicas"),
        ("verificaci\u00BFn", "verificación"), ("validaci\u00BFn", "validación"),
        ("instituci\u00BFn", "institución"), ("autom\u00BFticos", "automáticos"),
        ("il\u00BFcitas", "ilícitas"), ("capacitaci\u00BFn", "capacitación"),
        ("documentaci\u00BFn", "documentación"), ("organizaci\u00BFn", "organización"),
        ("operaci\u00BFn", "operación"), ("protecci\u00BFn", "protección"),
        ("situaci\u00BFn", "situación"), ("funci\u00BFn", "función"),
        ("administraci\u00BFn", "administración"), ("identificaci\u00BFn", "identificación"),
        ("calificaci\u00BFn", "calificación"), ("relaci\u00BFn", "relación")
    };

    private static readonly Regex SignoPreguntaIncrustado =
        new("(?i)(?<=\\p{L})\\u00BF(?=\\p{L})", RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));

    public static bool ContieneMojibake(string? valor)
    {
        if (string.IsNullOrEmpty(valor)) return false;
        return valor.Contains('\uFFFD')
            || valor.Contains("\u00EF\u00BF\u00BD", StringComparison.Ordinal)
            || valor.Contains('\u00C3') || valor.Contains('\u00C2')
            || valor.Contains("\u00E2\u20AC", StringComparison.Ordinal)
            || valor.Contains("\u00F0\u0178", StringComparison.Ordinal)
            || SignoPreguntaIncrustado.IsMatch(valor);
    }

    public static string Normalizar(string valor)
    {
        if (string.IsNullOrEmpty(valor)) return valor;
        string resultado = valor;
        foreach ((string from, string to) in ReemplazosMojibake)
            resultado = resultado.Replace(from, to, StringComparison.Ordinal);
        resultado = Regex.Replace(resultado, "(?<=\\p{L})\\uFFFD(?=\\p{L})", "\u00BF",
            RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
        resultado = resultado.Replace("\u00EF\u00BF\uFFFD", "\u00BF", StringComparison.Ordinal);
        resultado = resultado.Replace("\u00EF\u00BF\u00BD", "\u00BF", StringComparison.Ordinal)
            .Replace("\u00EF\u00BFo", "\u00F1o", StringComparison.Ordinal)
            .Replace("\uFFFDo", "\u00F1o", StringComparison.Ordinal);
        foreach ((string from, string to) in ReparacionesContextuales)
            resultado = resultado.Replace(from, to, StringComparison.OrdinalIgnoreCase);
        return resultado;
    }

    public static string? NormalizarNullable(string? valor) => valor is null ? null : Normalizar(valor);
}
