using System.Text.Json;
using System.Text.RegularExpressions;
using RL.API.Features.Catalogos.Contracts;
using RL.API.Features.MatricesRiesgos.Contracts;
using RL.API.Features.MatricesRiesgos.Domain;
using Xunit;
using CatalogElement = RL.API.Features.Catalogos.Contracts.ElementoCatalogoMatricesDto;

namespace RL.API.Tests.Features.MatricesRiesgos;

public sealed class InstitutionalFormulaParityTests
{
    [Fact]
    public void InstitutionalDataset_HasThirtyFourTraceableSemanticFormulas()
    {
        Assert.Equal(InstitutionalFormulaDataset.ExpectedCount, InstitutionalFormulaDataset.All.Count);
        Assert.Equal(Enumerable.Range(1, 34), InstitutionalFormulaDataset.All.Select(f => f.Number));
        Assert.All(InstitutionalFormulaDataset.All, formula =>
        {
            Assert.Matches("^Matriz Consolidada![A-Z]+2$", formula.SourceCell);
            Assert.DoesNotContain("!$", formula.SemanticExpression);
            Assert.DoesNotContain("VLOOKUP", formula.SemanticExpression, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Listas", formula.SemanticExpression, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Otras", formula.SemanticExpression, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public void InstitutionalFormulaFunctionInventory_MatchesCertifiedWorkbookCounts()
    {
        var expected = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["IF"] = 26, ["IFERROR"] = 23, ["LOOKUP"] = 8, ["OR"] = 9, ["MAX"] = 8,
            ["MIN"] = 5, ["MOD"] = 4, ["ROUND"] = 2, ["ROUNDDOWN"] = 2, ["AND"] = 1
        };

        foreach ((string function, int count) in expected)
        {
            int actual = InstitutionalFormulaDataset.All.Sum(formula =>
                Regex.Matches(formula.SemanticExpression, $@"\b{function}\s*\(", RegexOptions.IgnoreCase).Count);
            Assert.Equal(count, actual);
        }
    }

    [Fact]
    public void PublishedFormulaRuntime_ResolvesThePinnedFormulaVersion()
    {
        var registry = new DbDrivenFormulaRegistry(
            [new FormulaDto { Id = 1, Codigo = "F_REPRODUCIBLE", Estado = "ACTIVE" }],
            [
                new FormulaVersionDto { Id = 11, FormulaId = 1, Version = 1, Expresion = "1+1", TipoResultado = "DECIMAL", Estado = "PUBLISHED", Hash = Hash() },
                new FormulaVersionDto { Id = 12, FormulaId = 1, Version = 2, Expresion = "2+2", TipoResultado = "DECIMAL", Estado = "PUBLISHED", Hash = Hash() }
            ]);
        var pinning = new CalculationPinning(
            new Dictionary<string, int>(),
            new Dictionary<string, int>(),
            published: true,
            formulaVersions: new Dictionary<string, int> { ["F_REPRODUCIBLE"] = 1 });

        FormulaVersionDefinition resolved = registry.Resolve("F_REPRODUCIBLE", pinning.FormulaVersion("F_REPRODUCIBLE"), requirePinned: true);

        Assert.Equal(1, resolved.Version);
        Assert.Equal("1+1", resolved.Expression);
        Assert.Throws<FormulaRuntimeException>(() => registry.Resolve("F_REPRODUCIBLE", null, requirePinned: true));
    }

    [Fact]
    public void InstitutionalFormulas_MatchExcelReferenceCaseThroughTheSingleEngine()
    {
        var pinning = new CalculationPinning(
            NativeFunctionCatalog.FunctionCodes.ToDictionary(code => code, _ => 1, StringComparer.OrdinalIgnoreCase),
            new Dictionary<string, int> { ["PESO_PREVENTIVO"] = 1, ["PESO_DETECTIVO"] = 1, ["PESO_CORRECTIVO"] = 1 },
            new Dictionary<string, string>
            {
                ["CAT_NIVEL_RIESGO"] = "SNAPSHOT_NIVEL_RIESGO_V1",
                ["CAT_EFECTIVIDAD_NIVEL"] = "SNAPSHOT_EFECTIVIDAD_NIVEL_V1",
                ["CAT_EFECTIVIDAD_PORCENTAJE"] = "SNAPSHOT_EFECTIVIDAD_PORCENTAJE_V1"
            },
            published: true);

        var parameterResolver = new DbDrivenParameterResolver(
            [
                new ParametroDto { Id = 1, Codigo = "PESO_PREVENTIVO", Estado = "ACTIVE" },
                new ParametroDto { Id = 2, Codigo = "PESO_DETECTIVO", Estado = "ACTIVE" },
                new ParametroDto { Id = 3, Codigo = "PESO_CORRECTIVO", Estado = "ACTIVE" }
            ],
            [
                new ParametroVersionDto { Id = 11, ParametroId = 1, Version = 1, Tipo = "DECIMAL", ValorDecimal = 0.70m, Estado = "PUBLISHED", Hash = Hash() },
                new ParametroVersionDto { Id = 12, ParametroId = 2, Version = 1, Tipo = "DECIMAL", ValorDecimal = 0.15m, Estado = "PUBLISHED", Hash = Hash() },
                new ParametroVersionDto { Id = 13, ParametroId = 3, Version = 1, Tipo = "DECIMAL", ValorDecimal = 0.15m, Estado = "PUBLISHED", Hash = Hash() }
            ]);

        var parameters = pinning.ParameterVersions.ToDictionary(
            item => item.Key,
            item => parameterResolver.Resolve(item.Key, pinning),
            StringComparer.OrdinalIgnoreCase);
        var registry = new InMemoryFunctionRegistry(NativeFunctionCatalog.CreateDefaultDefinitions());
        var lookup = new CatalogCalculationLookup(
            [
                new CatalogSnapshot("CAT_NIVEL_RIESGO", true,
                [
                    new CatalogElement(1, "1", "Riesgo no significativo", 1, true),
                    new CatalogElement(2, "7", "Riesgo Alto", 2, true)
                ]),
                new CatalogSnapshot("CAT_EFECTIVIDAD_NIVEL", true,
                [new CatalogElement(3, "Alta Efectividad", "5", 1, true)]),
                new CatalogSnapshot("CAT_EFECTIVIDAD_PORCENTAJE", true,
                [new CatalogElement(4, "Alta Efectividad", "0.9", 1, true)])
            ]);
        var options = new FormulaRuntimeOptions(registry, parameters, lookup, pinning);
        var definition = JsonSerializer.Serialize(new
        {
            secciones = new[]
            {
                new
                {
                    campos = InstitutionalFormulaDataset.All.Select(formula => new
                    {
                        clave = formula.TargetField,
                        formula = formula.SemanticExpression
                    })
                }
            }
        });
        var input = new Dictionary<string, object?>
        {
            ["frecuencia"] = 3, ["impacto"] = 5,
            ["riesgo_inherente_descripcion"] = "Riesgo institucional de referencia",
            ["control_preventivo"] = "Control preventivo", ["control_detectivo"] = "Control detectivo", ["control_correctivo"] = "Control correctivo",
            ["escala_preventivo"] = "Alta Efectividad", ["escala_detectivo"] = "Alta Efectividad", ["escala_correctivo"] = "Alta Efectividad"
        };

        FormulaEvaluationResult result = new FormulaEngine().Evaluate(definition, JsonSerializer.Serialize(input), options);

        Assert.True(result.Success, string.Join("; ", result.Errors.Select(error => $"{error.Field}: {error.Message}")));
        Assert.Equal(34, result.Values.Count);
        var expectedByField = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["valor_riesgo_inherente"] = 7d,
            ["nivel_riesgo_inherente"] = "Riesgo Alto",
            ["nivel_control_preventivo"] = 5d,
            ["porcentaje_control_preventivo"] = 0.9d,
            ["nivel_control_detectivo"] = 5d,
            ["porcentaje_control_detectivo"] = 0.9d,
            ["nivel_control_correctivo"] = 5d,
            ["porcentaje_control_correctivo"] = 0.9d,
            ["efectividad_total_ponderada"] = 0.9d,
            ["riesgo_residual_descripcion"] = "Riesgo institucional de referencia",
            ["frecuencia_residual"] = 1d,
            ["impacto_residual"] = 1d,
            ["valor_riesgo_residual"] = 1d,
            ["nivel_riesgo_residual"] = "Riesgo no significativo",
            ["frecuencia_residual_aux"] = 0.3d,
            ["impacto_residual_aux"] = 0.5d,
            ["suma_residual_redondeada_aux"] = 2d,
            ["f_base"] = 1d,
            ["i_base"] = 1d,
            ["tope_f"] = 3d,
            ["tope_i"] = 5d,
            ["capacidad_f_aux"] = 2d,
            ["capacidad_i_aux"] = 4d,
            ["resto_aux"] = 0d,
            ["prefiere_i_aux"] = 1d,
            ["incremento_i_aux"] = 0d,
            ["incremento_f_aux"] = 0d,
            ["valor_riesgo_residual_aux"] = 1d,
            ["verificacion"] = 0d,
            ["vrr_2"] = 1d,
            ["verificar_vrr_2"] = 0d,
            ["verificar_frecuencia"] = 2d,
            ["verificar_impacto"] = 4d,
            ["diferencia_vri_vrr"] = 6d
        };
        Assert.Equal(InstitutionalFormulaDataset.ExpectedCount, expectedByField.Count);
        Assert.Equal(
            expectedByField.Keys.OrderBy(key => key, StringComparer.OrdinalIgnoreCase),
            InstitutionalFormulaDataset.All.Select(formula => formula.TargetField).OrderBy(key => key, StringComparer.OrdinalIgnoreCase));
        foreach (InstitutionalFormulaDefinition formula in InstitutionalFormulaDataset.All)
        {
            Assert.True(result.Values.TryGetValue(formula.TargetField, out object? actual), $"No se obtuvo el resultado de {formula.Code}.");
            object? expected = expectedByField[formula.TargetField];
            if (expected is double expectedNumber)
                Assert.Equal(expectedNumber, Number(actual), precision: 12);
            else
                Assert.Equal(expected, actual);
        }
        Assert.Equal(7d, Number(result.Values["valor_riesgo_inherente"]));
        Assert.Equal("Riesgo Alto", result.Values["nivel_riesgo_inherente"]);
        Assert.Equal(5d, Number(result.Values["nivel_control_preventivo"]));
        Assert.Equal(0.9d, Number(result.Values["porcentaje_control_preventivo"]));
        Assert.Equal(0.9d, Number(result.Values["efectividad_total_ponderada"]));
        Assert.Equal(1d, Number(result.Values["valor_riesgo_residual"]));
        Assert.Equal("Riesgo no significativo", result.Values["nivel_riesgo_residual"]);
        Assert.Equal(0d, Number(result.Values["resto_aux"]));
        Assert.Equal(1d, Number(result.Values["valor_riesgo_residual_aux"]));
        Assert.Equal(0d, Number(result.Values["verificacion"]));
        Assert.Equal(6d, Number(result.Values["diferencia_vri_vrr"]));
    }

    private static double Number(object? value) => Convert.ToDouble(value, System.Globalization.CultureInfo.InvariantCulture);
    private static string Hash() => new('0', 64);
}
