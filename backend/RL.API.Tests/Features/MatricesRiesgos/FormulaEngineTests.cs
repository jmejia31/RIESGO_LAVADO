using RL.API.Features.MatricesRiesgos.Domain;
using Xunit;

namespace RL.API.Tests.Features.MatricesRiesgos;

public sealed class FormulaEngineTests
{
    private const string Definition = """
    {"secciones":[{"clave":"s","titulo":"S","campos":[
      {"clave":"a","etiqueta":"A","tipo":"numero"},
      {"clave":"b","etiqueta":"B","tipo":"numero"},
      {"clave":"c","etiqueta":"C","tipo":"formula","formula":"IF(a > b, ROUND(a^2, 0), ROUNDDOWN(MAX(1, MOD(b, 3)), 0))"},
      {"clave":"d","etiqueta":"D","tipo":"formula","formula":"IFERROR(a / 0, 9)"}
    ]}]}
    """;

    [Fact]
    public void LexerParserAst_EvaluaPrecedenciaPotenciaComparacionYFunciones()
    {
        var result = new FormulaEngine().Evaluate(Definition, "{\"a\":4,\"b\":2}");

        Assert.True(result.Success);
        Assert.Equal(16d, result.Values["c"]);
        Assert.Equal(9d, result.Values["d"]);
    }

    [Theory]
    [InlineData("ROUND(2.5,0)", 3d)]
    [InlineData("ROUND(-2.5,0)", -3d)]
    [InlineData("ROUNDDOWN(-2.99,0)", -2d)]
    [InlineData("MOD(-5,3)", 1d)]
    [InlineData("MOD(5,-3)", -1d)]
    public void Funciones_ConservanSemanticaDeterminista(string expression, double expected)
    {
        var definition = Definition.Replace("IF(a > b, ROUND(a^2, 0), ROUNDDOWN(MAX(1, MOD(b, 3)), 0))", expression);
        var result = new FormulaEngine().Evaluate(definition, "{\"a\":1,\"b\":1}");

        Assert.True(result.Success);
        Assert.Equal(expected, result.Values["c"]);
    }

    [Fact]
    public void Validator_RechazaReferenciaDesconocidaAutorreferenciaYCiclo()
    {
        const string invalid = """
        {"secciones":[{"clave":"s","titulo":"S","campos":[
          {"clave":"a","etiqueta":"A","tipo":"formula","formula":"missing + 1"},
          {"clave":"b","etiqueta":"B","tipo":"formula","formula":"b + 1"},
          {"clave":"c","etiqueta":"C","tipo":"formula","formula":"d + 1"},
          {"clave":"d","etiqueta":"D","tipo":"formula","formula":"c + 1"}
        ]}]}
        """;

        var errors = new FormulaEngine().ValidateDefinition(invalid);

        Assert.Contains(errors, e => e.Code == FormulaErrorCode.FORMULA_REFERENCE_UNKNOWN);
        Assert.Contains(errors, e => e.Code == FormulaErrorCode.FORMULA_SELF_REFERENCE);
        Assert.Contains(errors, e => e.Code == FormulaErrorCode.FORMULA_CYCLE);
    }

    [Theory]
    [InlineData("unknown(1)", FormulaErrorCode.FORMULA_FUNCTION_UNSUPPORTED)]
    [InlineData("a / 0", FormulaErrorCode.FORMULA_DIVISION_BY_ZERO)]
    [InlineData("a + b", FormulaErrorCode.FORMULA_TYPE_MISMATCH)]
    public void Evaluator_DevuelveErroresTipados(string expression, FormulaErrorCode expected)
    {
        var definition = Definition.Replace("IF(a > b, ROUND(a^2, 0), ROUNDDOWN(MAX(1, MOD(b, 3)), 0))", expression);
        var responses = expected == FormulaErrorCode.FORMULA_TYPE_MISMATCH ? "{\"a\":2,\"b\":\"x\"}" : "{\"a\":2,\"b\":1}";
        var result = new FormulaEngine().Evaluate(definition, responses);

        Assert.Contains(result.Errors, e => e.Code == expected);
    }
}
