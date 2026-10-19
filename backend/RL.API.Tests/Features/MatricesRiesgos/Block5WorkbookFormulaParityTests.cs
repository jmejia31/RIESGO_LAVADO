using System.Globalization;
using System.Text.Json;
using Block5WorkbookFormulaOracle;
using RL.API.Features.Catalogos.Contracts;
using RL.API.Features.MatricesRiesgos.Domain;
using RL.Tools.ReconcileMatricesBaseline;
using BaselineReconciler = RL.Tools.ReconcileMatricesBaseline.Program;
using Xunit;
using CatalogElement = RL.API.Features.Catalogos.Contracts.ElementoCatalogoMatricesDto;

namespace RL.API.Tests.Features.MatricesRiesgos;

public sealed class Block5WorkbookFormulaParityTests
{
    private static readonly int[] Fields = [12, 13, 22, 23, 26, 27, 30, 31, 33, 34, 35, 36, 37, 38, .. Enumerable.Range(50, 20)];
    private static readonly Dictionary<int, int> SourceFieldToFormulaNumber = Fields.Select((field, index) => (field, formula: index + 1)).ToDictionary(item => item.field, item => item.formula);
    private static readonly IReadOnlyDictionary<int, object?> GoldenValues = new Dictionary<int, object?>
    {
        [12] = 3m, [13] = "Riesgo bajo", [22] = 5m, [23] = 0.9m, [26] = 0m, [27] = 0m, [30] = 0m, [31] = 0m,
        [33] = 0.63d, [37] = 1d, [38] = "Riesgo no significativo", [50] = 0.37d, [51] = 0.37d * 3d, [52] = 2d,
        [53] = 1m, [54] = 1m, [55] = 1m, [56] = 3m, [57] = 0m, [58] = 2m, [59] = 0m, [60] = 0m,
        [61] = 0m, [62] = 0m, [63] = 1m, [64] = 0m, [65] = 1m, [66] = 0m, [67] = 0m, [68] = 2m, [69] = 2m
    };

    [Fact]
    public void IndependentOracle_ResolvesWorkbookTablesAndAbsoluteCrossSheetReferences()
    {
        WorkbookFormulaOracle oracle = WorkbookFormulaOracle.Load(FindWorkbook());
        IReadOnlyDictionary<string, string[][]> tables = oracle.TableValues;
        Assert.Contains("Listas!A29:C35", oracle.TableLocations["t_efectividad"], StringComparison.Ordinal);
        Assert.Contains(tables["t_efectividad"], row => row.Contains("Inexistente", StringComparer.Ordinal));
        Assert.Contains(tables["t_pesos_controles"], row => row.Contains("0.7", StringComparer.Ordinal));

        WorkbookFormulaResult weighted = oracle.EvaluateFormulaCells().First(result => result.FieldNumber == 33);
        Assert.Contains(weighted.References, reference => reference.Contains("'Otras Tablas'!$B$3", StringComparison.Ordinal));
        Assert.False(weighted.IsError);
    }

    [Fact]
    public void DerivedCounters_CountParsedWorkbookPlanAndActivityStructuresForAllRisks()
    {
        WorkbookFormulaOracle oracle = WorkbookFormulaOracle.Load(FindWorkbook());
        var rows = new List<string> { "RISK_CODE,RISK_NO,FIELD_NUMBER,EXPECTED_FROM_SOURCE_STRUCTURE,WORKBOOK_REFERENCE,ACTUAL_FROM_SOURCE_STRUCTURE,MATCH" };
        int positions = 0;
        for (int row = 2; row <= 60; row++)
        {
            string riskCode = oracle.GetText(row, 2);
            foreach ((int sourceField, int counterField, bool hierarchical) in new[] { (40, 41, false), (42, 43, true) })
            {
                string raw = oracle.GetText(row, sourceField);
                SourceItemsParseResult parsed = BaselineReconciler.ParseSourceItems(raw, hierarchical);
                Assert.False(parsed.IsAmbiguous, $"Ambiguous F{sourceField} for {riskCode}: {parsed.ParseRule}");
                int expected = parsed.Items.Count(item => !string.IsNullOrWhiteSpace(item) && !IsCanonicalNoAction(item));
                object? reference = oracle.GetRawValue(row, counterField);
                positions++;
                rows.Add(string.Join(',', Csv(riskCode), Csv(oracle.GetText(row, 1)), counterField.ToString(CultureInfo.InvariantCulture), Csv(raw), Csv(Convert.ToString(reference, CultureInfo.InvariantCulture) ?? ""), expected.ToString(CultureInfo.InvariantCulture), "DERIVED_FROM_PARSED_STRUCTURE"));
            }
        }

        Assert.Equal(118, positions);
        string? artifactDirectory = Environment.GetEnvironmentVariable("BLOCK5_ARTIFACT_DIR");
        if (!string.IsNullOrWhiteSpace(artifactDirectory))
        {
            Directory.CreateDirectory(artifactDirectory);
            File.WriteAllLines(Path.Combine(artifactDirectory, "block5_derived_counters_parity.csv"), rows, new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        }
    }

    [Fact]
    public void IndependentOpenXmlFormulaOracle_MatchesBackendAcrossAllWorkbookPositions()
    {
        string workbookPath = FindWorkbook();
        Assert.True(File.Exists(workbookPath), $"Official workbook missing at {workbookPath}.");
        var oracle = WorkbookFormulaOracle.Load(workbookPath);
        WorkbookFormulaResult[] reference = oracle.EvaluateFormulaCells().ToArray();
        Assert.Equal(1742, oracle.PhysicalFormulaCount);
        Assert.Equal(264, oracle.TableCalculatedFormulaCount);
        Assert.Equal(2006, reference.Length);
        Assert.Equal(Enumerable.Range(1, 34), SourceFieldToFormulaNumber.Values.Order());

        var referenceByKey = reference.ToDictionary(item => (item.RiskCode, item.FieldNumber));
        string definition = JsonSerializer.Serialize(new
        {
            secciones = new[] { new { campos = InstitutionalFormulaDataset.All.Select(formula => new { clave = formula.TargetField, formula = formula.SemanticExpression }) } }
        });
        FormulaEngine engine = new();
        FormulaRuntimeOptions options = BuildOptions();
        int positionsCompared = 0;
        int ropCump54Positions = 0;
        int ropCump54Mismatches = 0;
        var differences = new List<string>();
        var errors = new List<string>();
        var parityRows = new List<string> { "RISK_CODE,RISK_NO,FIELD_NUMBER,FIELD_LABEL,SOURCE_CELL,REFERENCE_KIND,RAW_FORMULA,REFERENCE_EXPECTED,BACKEND_ACTUAL,MATCH,DIFFERENCE,ROOT_CAUSE" };

        for (int row = 2; row <= 60; row++)
        {
            string riskCode = oracle.GetText(row, 2);
            var input = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["frecuencia"] = oracle.GetRawValue(row, 10), ["impacto"] = oracle.GetRawValue(row, 11),
                ["riesgo_inherente_descripcion"] = oracle.GetRawValue(row, 8),
                ["control_preventivo"] = oracle.GetRawValue(row, 20), ["escala_preventivo"] = EffectiveScale(oracle, row, 20, 21),
                ["control_detectivo"] = oracle.GetRawValue(row, 24), ["escala_detectivo"] = EffectiveScale(oracle, row, 24, 25),
                ["control_correctivo"] = oracle.GetRawValue(row, 28), ["escala_correctivo"] = EffectiveScale(oracle, row, 28, 29)
            };
            FormulaEvaluationResult backend = engine.Evaluate(definition, JsonSerializer.Serialize(input), options);
            if (!backend.Success) errors.Add($"{riskCode}: {string.Join("; ", backend.Errors.Select(error => error.Code))}");

            foreach (int field in Fields)
            {
                positionsCompared++;
                object? expected;
                if (referenceByKey.TryGetValue((riskCode, field), out WorkbookFormulaResult? formulaResult))
                {
                    if (formulaResult.IsError) errors.Add($"Oracle returned formula error at {riskCode}/F{field} {formulaResult.Cell}.");
                    expected = ExpectedInstitutionalNoControlValue(oracle, row, field) ?? formulaResult.ExpectedValue;
                }
                else
                {
                    expected = ExpectedInstitutionalNoControlValue(oracle, row, field);
                    if (expected is null) { errors.Add($"Unclassified source nonformula {riskCode}/F{field}."); continue; }
                }

                string target = InstitutionalFormulaDataset.All.Single(item => item.Number == SourceFieldToFormulaNumber[field]).TargetField;
                backend.Values.TryGetValue(target, out object? actual);
                bool matches = Equivalent(expected, actual);
                if (!matches) differences.Add($"{riskCode}|F{field}|expected={Format(expected)}|actual={Format(actual)}");
                if (riskCode == "ROP-CUMP-54")
                {
                    ropCump54Positions++;
                    if (!matches) ropCump54Mismatches++;
                }
                if (riskCode == "ROP-CUMP-59" && GoldenValues.TryGetValue(field, out object? golden) && !Equivalent(golden, actual))
                    differences.Add($"ROP-CUMP-59_GOLDEN|F{field}|expected={Format(golden)}|actual={Format(actual)}");
                referenceByKey.TryGetValue((riskCode, field), out WorkbookFormulaResult? paritySource);
                parityRows.Add(string.Join(',',
                    Csv(riskCode), Csv(oracle.GetText(row, 1)), field.ToString(CultureInfo.InvariantCulture), Csv(oracle.GetText(1, field)), Csv(paritySource?.Cell ?? ""),
                    Csv(paritySource?.SourceKind ?? "INSTITUTIONAL_NONFORMULA_RULE"), Csv(paritySource?.Formula ?? "NO_CONTROL_DEFAULT"),
                    Csv(Format(expected)), Csv(Format(actual)), matches ? "YES" : "NO", Csv(matches ? "" : "VALUE_MISMATCH"), Csv(matches ? "" : "BACKEND_FORMULA_DRIFT")));
            }
        }

        Assert.Empty(errors);
        Assert.Equal(2006, positionsCompared);
        Assert.Equal(34, ropCump54Positions);
        Assert.Equal(0, ropCump54Mismatches);
        string? artifactDirectory = Environment.GetEnvironmentVariable("BLOCK5_ARTIFACT_DIR");
        if (!string.IsNullOrWhiteSpace(artifactDirectory))
        {
            Directory.CreateDirectory(artifactDirectory);
            File.WriteAllLines(Path.Combine(artifactDirectory, "block5_formula_parity_final.csv"), parityRows, new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        }
        Assert.True(differences.Count == 0, $"Calculation drift {differences.Count}/2006: {string.Join("; ", differences.Take(30))}");
    }

    internal static IReadOnlyDictionary<(string RiskCode, int FieldNumber), object?> BuildIndependentProjectionExpectations(WorkbookFormulaOracle oracle)
    {
        WorkbookFormulaResult[] formulaResults = oracle.EvaluateFormulaCells().ToArray();
        var byFormula = formulaResults.ToDictionary(result => (result.RiskCode, result.FieldNumber));
        var expected = new Dictionary<(string RiskCode, int FieldNumber), object?>();
        for (int row = 2; row <= 60; row++)
        {
            string riskCode = oracle.GetText(row, 2);
            foreach (int fieldNumber in Fields)
            {
                int formulaNumber = SourceFieldToFormulaNumber[fieldNumber];
                Assert.True(byFormula.TryGetValue((riskCode, formulaNumber), out WorkbookFormulaResult? result),
                    $"The independent workbook source has no value for {riskCode}/F{fieldNumber}.");
                Assert.False(result!.IsError, $"The independent workbook source has a formula error for {riskCode}/F{fieldNumber}.");
                object? value = ExpectedInstitutionalNoControlValue(oracle, row, fieldNumber) ?? result.ExpectedValue;
                expected.Add((riskCode, fieldNumber), value);
            }
        }

        Assert.Equal(2006, expected.Count);
        return expected;
    }

    [Theory]
    [InlineData(1, "Riesgo no significativo")]
    [InlineData(2, "Riesgo no significativo")]
    [InlineData(3, "Riesgo bajo")]
    [InlineData(4, "Riesgo bajo")]
    [InlineData(5, "Riesgo Medio")]
    [InlineData(6, "Riesgo Alto")]
    [InlineData(7, "Riesgo Alto")]
    [InlineData(8, "Riesgo Intolerable")]
    [InlineData(9, "Riesgo Intolerable")]
    public void BackendRiskLookup_UsesAllWorkbookRiskLevelBoundaries(int score, string expected)
    {
        const string definition = """{"secciones":[{"campos":[{"clave":"nivel","formula":"LOOKUP(\"CAT_NIVEL_RIESGO\",score)"}]}]}""";
        FormulaEvaluationResult result = new FormulaEngine().Evaluate(definition, JsonSerializer.Serialize(new { score }), BuildOptions());
        Assert.True(result.Success, string.Join("; ", result.Errors.Select(error => error.Message)));
        Assert.Equal(expected, result.Values["nivel"]);
    }

    [Theory]
    [InlineData("Inexistente", 0d, 0d)]
    [InlineData("Inefectivo", 1d, 0d)]
    [InlineData("Es inefectivo", 1d, 0d)]
    [InlineData("Razonable", 2d, 0.3d)]
    [InlineData("Parcialmente Efectivo", 3d, 0.5d)]
    [InlineData("Moderado", 4d, 0.85d)]
    [InlineData("Alta Efectividad", 5d, 0.9d)]
    public void BackendControlEffectiveness_UsesInstitutionalCatalogAndAlias(string scale, double expectedLevel, double expectedPercent)
    {
        FormulaEvaluationResult result = EvaluateWorkbookFormulas(new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["frecuencia"] = 3, ["impacto"] = 5, ["riesgo_inherente_descripcion"] = "Riesgo de prueba",
            ["control_preventivo"] = "Control de prueba", ["escala_preventivo"] = scale,
            ["control_detectivo"] = "", ["escala_detectivo"] = "Inexistente",
            ["control_correctivo"] = "", ["escala_correctivo"] = "Inexistente"
        });

        Assert.True(result.Success, string.Join("; ", result.Errors.Select(error => error.Message)));
        Assert.Equal(expectedLevel, Number(result.Values["nivel_control_preventivo"]));
        Assert.Equal(expectedPercent, Number(result.Values["porcentaje_control_preventivo"]));
    }

    [Fact]
    public void BackendControlEffectiveness_RejectsUnknownCatalogScale()
    {
        FormulaEvaluationResult result = EvaluateWorkbookFormulas(new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["frecuencia"] = 3, ["impacto"] = 5, ["riesgo_inherente_descripcion"] = "Riesgo de prueba",
            ["control_preventivo"] = "Control de prueba", ["escala_preventivo"] = "ETIQUETA NO CATALOGADA",
            ["control_detectivo"] = "", ["escala_detectivo"] = "Inexistente",
            ["control_correctivo"] = "", ["escala_correctivo"] = "Inexistente"
        });

        Assert.False(result.Success);
        Assert.Contains(result.Errors, error => error.Field == "escala_preventivo" && error.Code == FormulaErrorCode.FORMULA_REFERENCE_UNKNOWN);
    }

    [Fact]
    public void BackendWeightedEffectiveness_HandlesEveryControlPresenceCombination()
    {
        var combinations = new (bool Preventive, bool Detective, bool Corrective)[]
        {
            (false, false, false), (true, false, false), (false, true, false), (false, false, true),
            (true, true, false), (true, false, true), (false, true, true), (true, true, true)
        };
        foreach ((bool preventive, bool detective, bool corrective) in combinations)
        {
            var input = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["frecuencia"] = 3, ["impacto"] = 5, ["riesgo_inherente_descripcion"] = "Riesgo de prueba",
                ["control_preventivo"] = preventive ? "Control preventivo" : "", ["escala_preventivo"] = preventive ? "Razonable" : "Inexistente",
                ["control_detectivo"] = detective ? "Control detectivo" : "", ["escala_detectivo"] = detective ? "Razonable" : "Inexistente",
                ["control_correctivo"] = corrective ? "Control correctivo" : "", ["escala_correctivo"] = corrective ? "Razonable" : "Inexistente"
            };
            FormulaEvaluationResult result = EvaluateWorkbookFormulas(input);
            Assert.True(result.Success, string.Join("; ", result.Errors.Select(error => error.Message)));
            if (!preventive && !detective && !corrective)
                Assert.Equal("", result.Values["efectividad_total_ponderada"]);
            else
            {
                double expected = (preventive ? 0.70d : 0d) * 0.30d + (detective ? 0.15d : 0d) * 0.30d + (corrective ? 0.15d : 0d) * 0.30d;
                Assert.Equal(expected, Number(result.Values["efectividad_total_ponderada"]));
            }
        }
    }

    private static string EffectiveScale(WorkbookFormulaOracle oracle, int row, int descriptionColumn, int scaleColumn)
    {
        string description = oracle.GetText(row, descriptionColumn).Trim();
        string scale = oracle.GetText(row, scaleColumn).Trim();
        bool absent = description.Length == 0 || new[] { "NO HAY", "INEXISTENTE", "0", "0%" }.Contains(description.ToUpperInvariant(), StringComparer.Ordinal);
        return absent && scale.Length == 0 ? "Inexistente" : scale;
    }

    private static object? ExpectedInstitutionalNoControlValue(WorkbookFormulaOracle oracle, int row, int field)
    {
        int descriptionColumn = field switch { 22 or 23 => 20, 26 or 27 => 24, 30 or 31 => 28, _ => 0 };
        if (descriptionColumn == 0) return null;
        string description = oracle.GetText(row, descriptionColumn).Trim();
        int scaleColumn = field switch { 22 or 23 => 21, 26 or 27 => 25, 30 or 31 => 29, _ => 0 };
        string scale = oracle.GetText(row, scaleColumn).Trim();
        bool absent = description.Length == 0 || new[] { "NO HAY", "INEXISTENTE", "0", "0%" }.Contains(description.ToUpperInvariant(), StringComparer.Ordinal);
        bool absentScale = scale.Length == 0 || scale.Equals("Inexistente", StringComparison.OrdinalIgnoreCase);
        return absent && absentScale ? 0m : null;
    }

    private static bool Equivalent(object? expected, object? actual)
    {
        if (expected is JsonElement element)
            expected = element.ValueKind switch { JsonValueKind.Number => element.GetDouble(), JsonValueKind.String => element.GetString(), JsonValueKind.True => true, JsonValueKind.False => false, JsonValueKind.Null => null, _ => element.ToString() };
        if (expected is null || actual is null) return expected is null && actual is null;
        if (IsNumeric(expected) && IsNumeric(actual)) return Convert.ToDouble(expected, CultureInfo.InvariantCulture).Equals(Convert.ToDouble(actual, CultureInfo.InvariantCulture));
        return string.Equals(Convert.ToString(expected, CultureInfo.InvariantCulture), Convert.ToString(actual, CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }
    private static bool IsNumeric(object value) => value is byte or short or int or long or float or double or decimal;
    private static string Format(object? value) => value is null ? "<null>" : Convert.ToString(value, CultureInfo.InvariantCulture) ?? "<null>";
    private static string Csv(string value) => "\"" + value.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
    private static bool IsCanonicalNoAction(string value) => new[] { "NO HAY", "INEXISTENTE", "0", "0%" }.Contains(value.Trim(), StringComparer.OrdinalIgnoreCase);
    private static double Number(object? value) => Convert.ToDouble(value, CultureInfo.InvariantCulture);
    private static FormulaEvaluationResult EvaluateWorkbookFormulas(IReadOnlyDictionary<string, object?> input)
    {
        string definition = JsonSerializer.Serialize(new
        {
            secciones = new[] { new { campos = InstitutionalFormulaDataset.All.Select(formula => new { clave = formula.TargetField, formula = formula.SemanticExpression }) } }
        });
        return new FormulaEngine().Evaluate(definition, JsonSerializer.Serialize(input), BuildOptions());
    }

    private static FormulaRuntimeOptions BuildOptions()
    {
        CatalogSnapshot riskLevels = new("CAT_NIVEL_RIESGO", true, new[]
        {
            (1, "Riesgo no significativo"), (2, "Riesgo no significativo"), (3, "Riesgo bajo"), (4, "Riesgo bajo"), (5, "Riesgo Medio"),
            (6, "Riesgo Alto"), (7, "Riesgo Alto"), (8, "Riesgo Intolerable"), (9, "Riesgo Intolerable")
        }.Select((item, index) => new CatalogElement(index + 1, item.Item1.ToString(CultureInfo.InvariantCulture), item.Item2, index + 1, true)).ToArray());
        string[] scales = ["Inexistente", "Inefectivo", "Es inefectivo", "Razonable", "Parcialmente Efectivo", "Moderado", "Alta Efectividad"];
        string[] levels = ["0", "1", "1", "2", "3", "4", "5"];
        string[] percentages = ["0", "0", "0", "0.3", "0.5", "0.85", "0.9"];
        CatalogSnapshot levelCatalog = new("CAT_EFECTIVIDAD_NIVEL", true, scales.Select((scale, index) => new CatalogElement(index + 1, scale, levels[index], index + 1, true)).ToArray());
        CatalogSnapshot percentCatalog = new("CAT_EFECTIVIDAD_PORCENTAJE", true, scales.Select((scale, index) => new CatalogElement(index + 1, scale, percentages[index], index + 1, true)).ToArray());
        return new FormulaRuntimeOptions(
            new InMemoryFunctionRegistry(NativeFunctionCatalog.CreateDefaultDefinitions()),
            new Dictionary<string, FormulaValue>(StringComparer.OrdinalIgnoreCase)
            {
                ["PESO_PREVENTIVO"] = FormulaValue.NumberValue(0.70), ["PESO_DETECTIVO"] = FormulaValue.NumberValue(0.15), ["PESO_CORRECTIVO"] = FormulaValue.NumberValue(0.15)
            },
            new CatalogCalculationLookup([riskLevels, levelCatalog, percentCatalog]));
    }

    private static string FindWorkbook()
    {
        for (DirectoryInfo? directory = new(Directory.GetCurrentDirectory()); directory is not null; directory = directory.Parent)
        {
            string candidate = Path.Combine(directory.FullName, "Matrices de Riesgos.xlsx");
            if (File.Exists(candidate)) return candidate;
        }
        throw new FileNotFoundException("Official workbook is not available from the test working directory.");
    }
}
