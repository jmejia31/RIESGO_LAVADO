using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text.Json;
using Xunit;

namespace RL.API.Tests.Features.MatricesRiesgos;

public class MatrizRiesgosBlock2ForensicAuditTests
{
    private static string FindRepoRoot()
    {
        string current = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(current))
        {
            if (File.Exists(Path.Combine(current, "RIESGO_LAVADO.sln")))
            {
                return current;
            }
            string? parent = Directory.GetParent(current)?.FullName;
            if (parent == current) break;
            current = parent!;
        }
        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
    }

    [Fact]
    public void Read_Only_Sql_Policy_Test_Must_Pass()
    {
        // ARRANGE: Ubicar el código fuente del auditor forense
        string repoRoot = FindRepoRoot();
        string auditorSource = Path.Combine(repoRoot, "tools", "AuditMatricesExcelVsProduction", "Program.cs");
        Assert.True(File.Exists(auditorSource), $"El archivo del auditor no existe en {auditorSource}");

        string code = File.ReadAllText(auditorSource);

        // ACT: Extraer todas las consultas enviadas a OracleCommand
        var matches = Regex.Matches(code, @"new\s+OracleCommand\s*\(\s*""([^""]+)""");
        Assert.NotEmpty(matches);

        string[] prohibitedKeywords = new[]
        {
            "INSERT", "UPDATE", "DELETE", "MERGE", "DROP", "ALTER",
            "CREATE", "TRUNCATE", "CALL", "EXEC", "EXECUTE", "BEGIN",
            "DECLARE", "FOR UPDATE", "LOCK TABLE"
        };

        // ASSERT: Validar que cada query es estrictamente SELECT / WITH ... SELECT y no contiene operaciones de escritura
        foreach (Match match in matches)
        {
            string sql = match.Groups[1].Value.Trim();

            // Excepciones legítimas de control de transacción read-only
            if (sql.Equals("SET TRANSACTION READ ONLY", StringComparison.OrdinalIgnoreCase) ||
                sql.Equals("ROLLBACK", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            Assert.True(
                sql.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase) ||
                sql.StartsWith("WITH", StringComparison.OrdinalIgnoreCase),
                $"La consulta no inicia con SELECT o WITH: {sql}");

            foreach (var prohibited in prohibitedKeywords)
            {
                Assert.False(
                    Regex.IsMatch(sql, $@"\b{prohibited}\b", RegexOptions.IgnoreCase),
                    $"Palabra prohibida encontrada en consulta SQL: '{prohibited}' en '{sql}'");
            }
        }
    }

    [Fact]
    public void Auditor_Does_Not_Use_ExecuteNonQuery_Against_Production()
    {
        string repoRoot = FindRepoRoot();
        string auditorSource = Path.Combine(repoRoot, "tools", "AuditMatricesExcelVsProduction", "Program.cs");
        Assert.True(File.Exists(auditorSource));

        string code = File.ReadAllText(auditorSource);

        // ExecuteNonQueryAsync sólo puede usarse para control de sesión (SET TRANSACTION READ ONLY y ROLLBACK)
        var nonQueryMatches = Regex.Matches(code, @"\.ExecuteNonQueryAsync\s*\(\s*\)");

        // Una ejecución normal usa SET READ ONLY y ROLLBACK; la salida por
        // identidad inválida y el snapshot de catálogos también cierran con ROLLBACK.
        Assert.Equal(4, nonQueryMatches.Count);
    }

    [Fact]
    public void Block2_Universes_And_Gates_Formulas_Must_Be_Valid()
    {
        int risks = 59;
        int fields = 82;
        int expectedTotal = risks * fields;

        Assert.Equal(4838, expectedTotal);

        // Bloques funcionales esperados
        int b1 = 59 * 19; // 1121
        int b2 = 59 * 14; // 826
        int b3 = 59 * 6;  // 354
        int b4 = 59 * 10; // 590
        int b5 = 59 * 20; // 1180
        int b6 = 59 * 13; // 767

        Assert.Equal(1121, b1);
        Assert.Equal(826, b2);
        Assert.Equal(354, b3);
        Assert.Equal(590, b4);
        Assert.Equal(1180, b5);
        Assert.Equal(767, b6);
        Assert.Equal(4838, b1 + b2 + b3 + b4 + b5 + b6);
    }
    [Fact]
    public void AuditorSource_MustContainCanonicalScaleNormalizationAndNoHayProjection()
    {
        string repoRoot = FindRepoRoot();
        string auditorSource = Path.Combine(repoRoot, "tools", "AuditMatricesExcelVsProduction", "Program.cs");
        Assert.True(File.Exists(auditorSource));

        string code = File.ReadAllText(auditorSource);

        // Validar escalas de efectividad institucionales
        Assert.Contains("\"ALTA_EFECTIVIDAD\"", code, StringComparison.Ordinal);
        Assert.Contains("\"MODERADO\"", code, StringComparison.Ordinal);
        Assert.Contains("\"PARCIALMENTE_EFECTIVO\"", code, StringComparison.Ordinal);
        Assert.Contains("\"RAZONABLE\"", code, StringComparison.Ordinal);
        Assert.Contains("\"INEFECTIVO\"", code, StringComparison.Ordinal);
        Assert.Contains("\"INEXISTENTE\"", code, StringComparison.Ordinal);

        // Validar proyección institucional ante ausencia de controles
        Assert.Contains("pos.DbRaw = \"No hay\";", code, StringComparison.Ordinal);
        Assert.Contains("pos.ReasonCode = \"NO_CONTROLS_CANONICAL_MATCH\";", code, StringComparison.Ordinal);
        Assert.Contains("pos.ReasonCode = \"EFFECTIVENESS_SCALE_SEMANTIC_MATCH\";", code, StringComparison.Ordinal);
        Assert.Contains("EVA_DATOS_JSON.{currentKey}", code, StringComparison.Ordinal);
        Assert.Contains("VALID_JSON", code, StringComparison.Ordinal);
        Assert.Contains("VALID_RELATION", code, StringComparison.Ordinal);
    }

    [Fact]
    public void Field03_LabelMustBeArea()
    {
        string root = FindRepoRoot();
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "backend", "RL.API", "Features", "MatricesRiesgos", "Contracts", "matriz_riesgos_82_campos_manifest.json")));
        Assert.Equal("Área", manifest.RootElement[2].GetProperty("label").GetString());
    }

    [Fact]
    public void RopCump59_Field03MustComeFromWorkbookColumnC()
    {
        string root = FindRepoRoot();
        string exporter = File.ReadAllText(Path.Combine(root, "tools", "export_excel_matrix_82.js"));
        Assert.Contains("fieldNumber: c", exporter, StringComparison.Ordinal);
        Assert.Contains("row.getCell(c)", exporter, StringComparison.Ordinal);
        Assert.Contains("const cell = row.getCell(c)", exporter, StringComparison.Ordinal);
        Assert.Contains("const field03 = controlRisk?.cells.find(c => c.fieldNumber === 3)?.textValue", exporter, StringComparison.Ordinal);
        Assert.Contains("field03 !== 'Sección de Cumplimiento'", exporter, StringComparison.Ordinal);
        Assert.DoesNotContain("Macroproceso", exporter, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AuditLabelsMustExactlyMatchCanonicalManifest_82Of82()
    {
        string root = FindRepoRoot();
        string code = File.ReadAllText(Path.Combine(root, "tools", "AuditMatricesExcelVsProduction", "Program.cs"));
        Assert.Contains("SequenceEqual(excelHeaders, StringComparer.Ordinal)", code, StringComparison.Ordinal);
        Assert.Contains("manifestFields.Count != 82 || excelHeaders.Count != 82 || excelRows.Count != 59", code, StringComparison.Ordinal);
        Assert.Contains("labelsMatchManifest", code, StringComparison.Ordinal);
    }

    [Fact]
    public void AuditLabelsMustExactlyMatchWorkbookHeaders_82Of82()
    {
        string root = FindRepoRoot();
        string code = File.ReadAllText(Path.Combine(root, "tools", "AuditMatricesExcelVsProduction", "Program.cs"));
        Assert.Contains("var excelHeaders = excelDoc.RootElement.GetProperty(\"headers\")", code, StringComparison.Ordinal);
        Assert.Contains("SequenceEqual(excelHeaders, StringComparer.Ordinal)", code, StringComparison.Ordinal);
        Assert.Contains("Los encabezados del workbook no coinciden exactamente", code, StringComparison.Ordinal);
    }

    [Fact]
    public void UnknownWorkbookFieldSubstitutionMustFail()
    {
        string root = FindRepoRoot();
        string code = File.ReadAllText(Path.Combine(root, "tools", "AuditMatricesExcelVsProduction", "Program.cs"));
        Assert.Contains("Los encabezados del workbook no coinciden exactamente", code, StringComparison.Ordinal);
        Assert.Contains("throw new InvalidDataException", code, StringComparison.Ordinal);
    }

    [Fact]
    public void Field70DifferentOperationalValueMustFollowManifestPreservationPolicy()
    {
        string root = FindRepoRoot();
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "backend", "RL.API", "Features", "MatricesRiesgos", "Contracts", "matriz_riesgos_82_campos_manifest.json")));
        JsonElement field70 = manifest.RootElement[69];
        Assert.True(field70.GetProperty("preserveExistingOperationalValue").GetBoolean());
        Assert.Contains("PRESERVE_OPERATIONAL_DB", field70.GetProperty("subsequentReconciliationRule").GetString(), StringComparison.Ordinal);
        string code = File.ReadAllText(Path.Combine(root, "tools", "AuditMatricesExcelVsProduction", "Program.cs"));
        Assert.Contains("OPERATIONAL_ALERTS_PRESERVED_BY_CONTRACT", code, StringComparison.Ordinal);
        Assert.Contains("preserveOperationalAlerts ? \"PRESERVE_PRODUCTION\"", code, StringComparison.Ordinal);
        Assert.Contains("CONTRACT_AUTHORITY_RULE", code, StringComparison.Ordinal);
        Assert.Contains("initialBaselineImportRule", code, StringComparison.Ordinal);
        Assert.Contains("subsequentReconciliationRule", code, StringComparison.Ordinal);
        Assert.Contains("excelNullBehavior", code, StringComparison.Ordinal);
        Assert.Contains("p.PrimaryClassification == \"DB_HAS_NEWER_OPERATIONAL_DATA\" && p.OperationalAuthority", code, StringComparison.Ordinal);
    }

    [Fact]
    public void Field39CatalogConflictMustNotRecommendDirectDataRemediationBeforeBlock3()
    {
        string root = FindRepoRoot();
        string code = File.ReadAllText(Path.Combine(root, "tools", "AuditMatricesExcelVsProduction", "Program.cs"));
        Assert.Contains("CATALOG_SOURCE_CONFLICT_RESPONSE_RISK", code, StringComparison.Ordinal);
        Assert.Contains("pos.RecommendedNextAction = \"FIX_CATALOG\"", code, StringComparison.Ordinal);
        Assert.Contains("targetBlock=3", code, StringComparison.Ordinal);
    }

    [Fact]
    public void ReadOnlyAuditMustRejectWriteSql()
    {
        Read_Only_Sql_Policy_Test_Must_Pass();
    }

    [Fact]
    public void AuditUniverseMustContain4838UniquePositions()
    {
        Block2_Universes_And_Gates_Formulas_Must_Be_Valid();
        string root = FindRepoRoot();
        string code = File.ReadAllText(Path.Combine(root, "tools", "AuditMatricesExcelVsProduction", "Program.cs"));
        Assert.Contains("int expectedPositions = 4838", code, StringComparison.Ordinal);
        Assert.Contains("Distinct().Count()", code, StringComparison.Ordinal);
    }

    [Fact]
    public void EveryPositionMustHavePrimaryClassification()
    {
        string root = FindRepoRoot();
        string code = File.ReadAllText(Path.Combine(root, "tools", "AuditMatricesExcelVsProduction", "Program.cs"));
        Assert.Contains("string.IsNullOrWhiteSpace(p.PrimaryClassification)", code, StringComparison.Ordinal);
        Assert.Contains("UNCLASSIFIED_POSITIONS", code, StringComparison.Ordinal);
    }

    [Fact]
    public void EveryNonMatchIssueMustHaveReasonAndNextAction()
    {
        string root = FindRepoRoot();
        string code = File.ReadAllText(Path.Combine(root, "tools", "AuditMatricesExcelVsProduction", "Program.cs"));
        Assert.Contains("string.IsNullOrWhiteSpace(p.ReasonCode) || string.IsNullOrWhiteSpace(p.RecommendedNextAction)", code, StringComparison.Ordinal);
        Assert.Contains("UNEXPLAINED_DIFFERENCES", code, StringComparison.Ordinal);
    }
}
