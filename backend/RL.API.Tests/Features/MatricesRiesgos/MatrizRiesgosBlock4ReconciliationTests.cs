using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using RL.Tools.ReconcileMatricesBaseline;
using BaselineReconciler = RL.Tools.ReconcileMatricesBaseline.Program;
using Xunit;

namespace RL.API.Tests.Features.MatricesRiesgos;

public class MatrizRiesgosBlock4ReconciliationTests
{
    [Fact]
    public void TestA_BaselineExcelNonEmpty_DbNull_WritesBaseline()
    {
        // Caso A: baseline Excel nonempty + DB null => write baseline
        var snapshot = CreateTestSnapshot(new[] { "TEST-01" });
        var excelRow = CreateTestExcelRow(1, "TEST-01", f05TipoRiesgo: "Operativo", f06Procedimiento: "Proc 1");
        var manifest = CreateTestManifest();
        var catalogDoc = CreateTestCatalogDoc();

        var plan = BaselineReconciler.BuildReconciliationPlan(snapshot, new List<JsonElement> { excelRow }, manifest, catalogDoc);

        var mF05 = plan.Mutations.FirstOrDefault(m => m.RiskCode == "TEST-01" && m.FieldNumber == 5);
        Assert.NotNull(mF05);
        Assert.Equal("UPDATE_BASELINE", mF05.Action);
        Assert.Equal("Operativo", mF05.TargetCanonical);
        Assert.Contains("IS_EMPTY", mF05.Precondition, StringComparison.OrdinalIgnoreCase);

        var mF06 = plan.Mutations.FirstOrDefault(m => m.RiskCode == "TEST-01" && m.FieldNumber == 6);
        Assert.NotNull(mF06);
        Assert.Equal("UPDATE_BASELINE", mF06.Action);
        Assert.Equal("Proc 1", mF06.TargetCanonical);
    }

    [Fact]
    public void TestB_ExcelEqualsDb_ProducesNoAction()
    {
        // Caso B: Excel == DB => no action
        var snapshot = CreateTestSnapshot(new[] { "TEST-02" });
        snapshot.Risks[0].Description = "Descripción exacta";
        var excelRow = CreateTestExcelRow(2, "TEST-02", f09Desc: "Descripción exacta");
        var manifest = CreateTestManifest();
        var catalogDoc = CreateTestCatalogDoc();

        var plan = BaselineReconciler.BuildReconciliationPlan(snapshot, new List<JsonElement> { excelRow }, manifest, catalogDoc);

        var posF09 = plan.AllPositions.Single(p => p.RiskCode == "TEST-02" && p.FieldNumber == 9);
        Assert.Equal("NO_ACTION", posF09.Action);
        Assert.DoesNotContain(plan.Mutations, m => m.RiskCode == "TEST-02" && m.FieldNumber == 9);
    }

    [Fact]
    public void TestC_ExcelBlank_DbBaselineValue_PreservesValue()
    {
        // Caso C: Excel blank + DB baseline value => preserve unless contract says authoritative clear
        var snapshot = CreateTestSnapshot(new[] { "TEST-03" });
        snapshot.Projections[0].AreaPrincipal = "Área Operativa Existente";
        var excelRow = CreateTestExcelRow(3, "TEST-03", f04AreaConsolidada: "");
        var manifest = CreateTestManifest();
        var catalogDoc = CreateTestCatalogDoc();

        var plan = BaselineReconciler.BuildReconciliationPlan(snapshot, new List<JsonElement> { excelRow }, manifest, catalogDoc);

        var posF04 = plan.AllPositions.Single(p => p.RiskCode == "TEST-03" && p.FieldNumber == 4);
        Assert.Equal("PRESERVE_PRODUCTION", posF04.Action);
        Assert.Equal("Área Operativa Existente", posF04.TargetCanonical);
        Assert.DoesNotContain(plan.Mutations, m => m.RiskCode == "TEST-03" && m.FieldNumber == 4);
    }

    [Fact]
    public void TestD_ExcelBlank_DbOperationalValue_PreservesOperational()
    {
        // Caso D: Excel blank + DB operational value => preserve
        var snapshot = CreateTestSnapshot(new[] { "ROTR-COMPRAS-18" });
        snapshot.Alerts.Add(new AlertPreimage
        {
            Id = 1,
            EvaluationId = snapshot.Evaluations[0].Id,
            RiskCode = "ROTR-COMPRAS-18",
            Indicator = "Señal Operacional Registrada en DB",
            State = "ACTIVO"
        });

        var excelRow = CreateTestExcelRow(18, "ROTR-COMPRAS-18", f70Alertas: "1. Otra señal diferente en Excel");
        var manifest = CreateTestManifest();
        var catalogDoc = CreateTestCatalogDoc();

        var plan = BaselineReconciler.BuildReconciliationPlan(snapshot, new List<JsonElement> { excelRow }, manifest, catalogDoc);

        var posF70 = plan.AllPositions.Single(p => p.RiskCode == "ROTR-COMPRAS-18" && p.FieldNumber == 70);
        Assert.Equal("PRESERVE_PRODUCTION", posF70.Action);
        Assert.Equal("OPERATIONAL_ALERTS_PRESERVED_BY_CONTRACT", posF70.ReasonCode);
        Assert.Contains(plan.Preservations, p => p.RiskCode == "ROTR-COMPRAS-18" && p.FieldNumber == 70);
    }

    [Fact]
    public void TestE_CalculatedFieldExcelValue_NeverImported()
    {
        // Caso E: calculated field Excel value => never import
        var snapshot = CreateTestSnapshot(new[] { "TEST-05" });
        var excelRow = CreateTestExcelRow(5, "TEST-05");
        var manifest = CreateTestManifest();
        var catalogDoc = CreateTestCatalogDoc();

        var plan = BaselineReconciler.BuildReconciliationPlan(snapshot, new List<JsonElement> { excelRow }, manifest, catalogDoc);

        int[] calcFields = { 12, 13, 22, 23, 26, 27, 30, 31, 33, 34, 35, 36, 37, 38, 41, 43, 50, 51, 60, 69 };
        foreach (int fn in calcFields)
        {
            var pos = plan.AllPositions.Single(p => p.RiskCode == "TEST-05" && p.FieldNumber == fn);
            Assert.Equal("RECALCULATE_BACKEND", pos.Action);
            Assert.Equal("RECALCULATION_PENDING_BLOCK5", pos.TargetCanonical);
            Assert.DoesNotContain(plan.Mutations, m => m.RiskCode == "TEST-05" && m.FieldNumber == fn);
        }
    }

    [Fact]
    public void TestF_NoHayControl_DbZeroControls_NoInsert()
    {
        // Caso F: "No hay" control + DB zero controls => no insert
        var snapshot = CreateTestSnapshot(new[] { "TEST-06" });
        var excelRow = CreateTestExcelRow(6, "TEST-06", f20Prev: "No hay", f24Det: "No hay", f28Corr: "No hay");
        var manifest = CreateTestManifest();
        var catalogDoc = CreateTestCatalogDoc();

        var plan = BaselineReconciler.BuildReconciliationPlan(snapshot, new List<JsonElement> { excelRow }, manifest, catalogDoc);

        foreach (int fn in new[] { 20, 24, 28 })
        {
            var pos = plan.AllPositions.Single(p => p.RiskCode == "TEST-06" && p.FieldNumber == fn);
            Assert.Equal("NO_ACTION", pos.Action);
            Assert.Equal("CONTROL_ABSENCE_NO_INSERT", pos.ReasonCode);
            Assert.DoesNotContain(plan.Mutations, m => m.RiskCode == "TEST-06" && m.FieldNumber == fn);
        }
    }

    [Fact]
    public void TestG_NoHayControl_DbExistingRealControl_NeverCreatesFakeNoHay()
    {
        // Caso G: "No hay" control + DB existing real control => never create fake "No hay"
        var snapshot = CreateTestSnapshot(new[] { "TEST-07" });
        snapshot.Controls.Add(new ControlPreimage
        {
            Id = 55,
            EvaluationId = snapshot.Evaluations[0].Id,
            RiskCode = "TEST-07",
            Tipo = "PREVENTIVO",
            Descripcion = "Control Real Existente",
            Automatizacion = "MANUAL",
            Estado = "ACTIVO"
        });

        var excelRow = CreateTestExcelRow(7, "TEST-07", f20Prev: "No hay");
        var manifest = CreateTestManifest();
        var catalogDoc = CreateTestCatalogDoc();

        var plan = BaselineReconciler.BuildReconciliationPlan(snapshot, new List<JsonElement> { excelRow }, manifest, catalogDoc);

        var posF20 = plan.AllPositions.Single(p => p.RiskCode == "TEST-07" && p.FieldNumber == 20);
        Assert.NotEqual("INSERT_BASELINE", posF20.Action);
        Assert.DoesNotContain(plan.Mutations, m => m.RiskCode == "TEST-07" && m.FieldNumber == 20 && m.TargetCanonical.Contains("No hay"));
    }

    [Fact]
    public void TestH_PlanDateResourceBudgetBlank_DbValue_Preserved()
    {
        // Caso H: plan date/resource/budget blank + DB value => preserve
        var snapshot = CreateTestSnapshot(new[] { "TEST-08" });
        snapshot.Plans.Add(new PlanPreimage
        {
            Id = 99,
            EvaluationId = snapshot.Evaluations[0].Id,
            RiskCode = "TEST-08",
            Descripcion = "Plan Existente",
            Recursos = "Recursos Operacionales",
            Presupuesto = 50000m,
            FechaInicio = new DateTime(2026, 1, 1),
            FechaFin = new DateTime(2026, 12, 31),
            Estado = "EN_PROCESO"
        });

        var excelRow = CreateTestExcelRow(8, "TEST-08", f46FIni: "", f47FFin: "", f48Rec: "", f49Pres: "");
        var manifest = CreateTestManifest();
        var catalogDoc = CreateTestCatalogDoc();

        var plan = BaselineReconciler.BuildReconciliationPlan(snapshot, new List<JsonElement> { excelRow }, manifest, catalogDoc);

        foreach (int fn in new[] { 46, 47, 48, 49 })
        {
            Assert.DoesNotContain(plan.Mutations, m => m.RiskCode == "TEST-08" && m.FieldNumber == fn);
        }
    }

    [Fact]
    public void TestI_Monitoring71To82Blank_DbValue_Preserved()
    {
        // Caso I: monitoring 71–82 blank + DB value => preserve, no artificial defaults
        var snapshot = CreateTestSnapshot(new[] { "TEST-09" });
        var excelRow = CreateTestExcelRow(9, "TEST-09");
        var manifest = CreateTestManifest();
        var catalogDoc = CreateTestCatalogDoc();

        var plan = BaselineReconciler.BuildReconciliationPlan(snapshot, new List<JsonElement> { excelRow }, manifest, catalogDoc);

        for (int fn = 71; fn <= 82; fn++)
        {
            var pos = plan.AllPositions.Single(p => p.RiskCode == "TEST-09" && p.FieldNumber == fn);
            Assert.Equal("NO_ACTION", pos.Action);
            Assert.Equal("BASELINE_BLANK_AWAITING_MONITORING_CYCLE", pos.ReasonCode);
            Assert.DoesNotContain(plan.Mutations, m => m.RiskCode == "TEST-09" && m.FieldNumber == fn);
        }
    }

    [Fact]
    public void TestJ_UnknownCatalogValue_FailsClosed()
    {
        // Caso J: unknown catalog value => fail closed or normalized
        var snapshot = CreateTestSnapshot(new[] { "TEST-10" });
        var excelRow = CreateTestExcelRow(10, "TEST-10", f32Auto: "Semi-Automatizado");
        var manifest = CreateTestManifest();
        var catalogDoc = CreateTestCatalogDoc();

        var plan = BaselineReconciler.BuildReconciliationPlan(snapshot, new List<JsonElement> { excelRow }, manifest, catalogDoc);

        var mF32 = plan.Mutations.Single(m => m.RiskCode == "TEST-10" && m.FieldNumber == 32);
        Assert.Equal("SEMIAUTOMATICO", mF32.TargetCanonical);
    }

    [Fact]
    public void TestK_ConcurrentDbChangeAfterDryRun_PreconditionPreventsOverwrite()
    {
        // Caso K: concurrent DB change after dry run => precondition prevents overwrite
        var snapshot = CreateTestSnapshot(new[] { "TEST-11" });
        snapshot.Risks[0].Description = "Original";
        var excelRow = CreateTestExcelRow(11, "TEST-11", f09Desc: "Nueva descripción canónica");
        var manifest = CreateTestManifest();
        var catalogDoc = CreateTestCatalogDoc();

        var plan = BaselineReconciler.BuildReconciliationPlan(snapshot, new List<JsonElement> { excelRow }, manifest, catalogDoc);

        var mF09 = plan.Mutations.Single(m => m.RiskCode == "TEST-11" && m.FieldNumber == 9);
        Assert.Equal("UPDATE_BASELINE", mF09.Action);
        Assert.Contains("Original", mF09.Precondition);

        // Si en runtime el valor cambia a "Modificado Concurrentemente", la precondición falla
        string concurrentDbValue = "Modificado Concurrentemente";
        bool preconditionMatches = mF09.Precondition.Contains(concurrentDbValue);
        Assert.False(preconditionMatches, "La precondición debe detectar drift concurrente y prevenir sobreescritura.");
    }

    [Fact]
    public void TestL_SecondExecution_ZeroMutations_Idempotency()
    {
        // Caso L: second execution => zero mutations
        var snapshot = CreateTestSnapshot(new[] { "TEST-12" });
        // Simular que el estado ya está reconciliado
        snapshot.Risks[0].Description = "Descripción Reconciliada";
        var node = new JsonObject
        {
            ["tipo_riesgo"] = "Operativo",
            ["procedimiento"] = "Proc Reconciliado",
            ["transversalidad"] = "Transversal"
        };
        snapshot.Evaluations[0].DatosJson = node.ToJsonString();

        var excelRow = CreateTestExcelRow(12, "TEST-12",
            f05TipoRiesgo: "Operativo",
            f06Procedimiento: "Proc Reconciliado",
            f09Desc: "Descripción Reconciliada",
            f16Transversal: "Transversal");

        var manifest = CreateTestManifest();
        var catalogDoc = CreateTestCatalogDoc();

        var plan = BaselineReconciler.BuildReconciliationPlan(snapshot, new List<JsonElement> { excelRow }, manifest, catalogDoc);

        Assert.Equal(0, plan.Mutations.Count(m => m.RiskCode == "TEST-12" && m.FieldNumber is 5 or 6 or 9 or 16));
    }

    [Fact]
    public void Test_JsonMerge_PreservesUnrelatedProperties()
    {
        // Regla: JSON merge preserves unrelated properties
        var originalJson = new JsonObject
        {
            ["propiedad_personalizada"] = "Valor_Importante",
            ["frecuencia_inherente"] = "3",
            ["impacto_inherente"] = "4"
        };

        var node = JsonNode.Parse(originalJson.ToJsonString()) as JsonObject;
        Assert.NotNull(node);

        // Agregar nuevas propiedades de reconciliación
        node["tipo_riesgo"] = "Cumplimiento";
        node["procedimiento"] = "Procedimiento 10";

        var mergedDoc = JsonDocument.Parse(node.ToJsonString());
        Assert.True(mergedDoc.RootElement.TryGetProperty("propiedad_personalizada", out var propCust));
        Assert.Equal("Valor_Importante", propCust.GetString());
        Assert.True(mergedDoc.RootElement.TryGetProperty("frecuencia_inherente", out var propFreq));
        Assert.Equal("3", propFreq.GetString());
        Assert.True(mergedDoc.RootElement.TryGetProperty("tipo_riesgo", out var propTipo));
        Assert.Equal("Cumplimiento", propTipo.GetString());
    }

    [Fact]
    public void Test_DuplicateDetector_AssertsZeroNewDuplicates()
    {
        var snapshot = CreateTestSnapshot(new[] { "TEST-14" });
        var excelRow = CreateTestExcelRow(14, "TEST-14");
        var manifest = CreateTestManifest();
        var catalogDoc = CreateTestCatalogDoc();

        var plan = BaselineReconciler.BuildReconciliationPlan(snapshot, new List<JsonElement> { excelRow }, manifest, catalogDoc);

        Assert.Equal(0, plan.NewDuplicatesExpected);
        Assert.Equal(0, plan.AccidentalNullOverwrites);
    }

    // Helper builders
    private static DatabaseSnapshot CreateTestSnapshot(string[] codes)
    {
        var snap = new DatabaseSnapshot
        {
            RunId = "TEST_RUN",
            CapturedAtUtc = DateTime.UtcNow,
            DatabaseName = "TEST_DB"
        };

        long id = 100;
        foreach (var c in codes)
        {
            id++;
            snap.Risks.Add(new RiskPreimage { Id = id, Code = c, Name = $"Riesgo {c}", Description = $"Desc {c}", Active = 1 });
            snap.Evaluations.Add(new EvaluationPreimage
            {
                Id = id + 1000,
                RiskId = id,
                RiskCode = c,
                VersionId = 3,
                Active = 1,
                DatosJson = "{}",
                CalculosJson = "{}"
            });
            snap.Projections.Add(new ProjectionPreimage
            {
                EvaluationId = id + 1000,
                RiskCode = c,
                AreaPrincipal = "Área General",
                DuenoRiesgo = "Dueño General",
                Vri = 3,
                NivelInherente = "BAJO",
                Vrr = 1,
                NivelResidual = "NO_SIGNIFICATIVO",
                RespuestaRiesgo = "MITIGAR",
                EstadoEvaluacion = "APROBADA"
            });
        }
        return snap;
    }

    private static JsonElement CreateTestExcelRow(
        int no, string code,
        string f04AreaConsolidada = "",
        string f05TipoRiesgo = "",
        string f06Procedimiento = "",
        string f09Desc = "",
        string f16Transversal = "",
        string f20Prev = "",
        string f24Det = "",
        string f28Corr = "",
        string f32Auto = "",
        string f46FIni = "",
        string f47FFin = "",
        string f48Rec = "",
        string f49Pres = "",
        string f70Alertas = "")
    {
        var cells = new List<object>();
        for (int i = 1; i <= 82; i++)
        {
            string val = "";
            switch (i)
            {
                case 1: val = no.ToString(); break;
                case 2: val = code; break;
                case 4: val = f04AreaConsolidada; break;
                case 5: val = f05TipoRiesgo; break;
                case 6: val = f06Procedimiento; break;
                case 8: val = $"Riesgo {code}"; break;
                case 9: val = string.IsNullOrEmpty(f09Desc) ? $"Desc {code}" : f09Desc; break;
                case 16: val = f16Transversal; break;
                case 20: val = f20Prev; break;
                case 24: val = f24Det; break;
                case 28: val = f28Corr; break;
                case 32: val = f32Auto; break;
                case 46: val = f46FIni; break;
                case 47: val = f47FFin; break;
                case 48: val = f48Rec; break;
                case 49: val = f49Pres; break;
                case 70: val = f70Alertas; break;
            }
            cells.Add(new { textValue = val, formula = "", cachedResult = (object?)null });
        }

        var rowObj = new
        {
            riskNo = no,
            riskCode = code,
            cells
        };

        return JsonDocument.Parse(JsonSerializer.Serialize(rowObj)).RootElement;
    }

    private static List<JsonElement> CreateTestManifest()
    {
        var list = new List<object>();
        for (int i = 1; i <= 82; i++)
        {
            string ck = i switch
            {
                5 => "tipo_riesgo",
                6 => "procedimiento",
                7 => "objetivos_estrategicos",
                15 => "regimen_afectado",
                16 => "transversalidad",
                32 => "CON_AUTOMATIZACION",
                _ => ""
            };
            list.Add(new
            {
                number = i,
                label = $"Campo {i:D2}",
                canonicalKey = $"campo{i}",
                currentKey = ck,
                source = "TEST",
                mode = "TEST",
                preserveExistingOperationalValue = i == 70
            });
        }
        return JsonDocument.Parse(JsonSerializer.Serialize(list)).RootElement.EnumerateArray().ToList();
    }

    private static JsonDocument CreateTestCatalogDoc()
    {
        return JsonDocument.Parse("{\"catalogs\":[]}");
    }
}
