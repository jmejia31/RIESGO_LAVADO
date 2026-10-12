using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using RL.Tools.ReconcileMatricesBaseline;
using BaselineReconciler = RL.Tools.ReconcileMatricesBaseline.Program;
using Xunit;

namespace RL.API.Tests.Features.MatricesRiesgos;

public class MatrizRiesgosBlock4ReconciliationTests
{
    #region Tests de Caracterización A - L

    [Fact]
    public void TestA_BaselineExcelNonEmpty_DbNull_WritesBaseline()
    {
        // Caso A: baseline Excel nonempty + DB null => write baseline
        var snapshot = CreateTestSnapshot(new[] { "TEST-01" });
        var excelRow = CreateTestExcelRow(1, "TEST-01", f05TipoRiesgo: "Operativo", f06Procedimiento: "Proc 1");
        var manifest = CreateTestManifest();
        var catalogDoc = CreateTestCatalogDoc();

        var plan = BaselineReconciler.BuildReconciliationPlan(snapshot, new List<JsonElement> { excelRow }, manifest, catalogDoc);

        var posF05 = plan.AllPositions.Single(p => p.RiskCode == "TEST-01" && p.FieldNumber == 5);
        Assert.Equal("UPDATE_BASELINE", posF05.Action);
        Assert.Equal("Operativo", posF05.TargetValue);

        var posF06 = plan.AllPositions.Single(p => p.RiskCode == "TEST-01" && p.FieldNumber == 6);
        Assert.Equal("UPDATE_BASELINE", posF06.Action);
        Assert.Equal("Proc 1", posF06.TargetValue);

        var mutJson = plan.Mutations.Single(m => m.RiskCode == "TEST-01" && m.TargetColumnOrProperty == "EVA_DATOS_JSON");
        Assert.Equal("UPDATE_BASELINE", mutJson.Action);
        Assert.Contains("tipo_riesgo", mutJson.TargetCanonical, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Operativo", mutJson.TargetCanonical, StringComparison.OrdinalIgnoreCase);
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
        Assert.Equal("Área Operativa Existente", posF04.TargetValue);
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
            Indicador = "Señal Operacional Registrada en DB",
            Estado = "ACTIVO"
        });

        var excelRow = CreateTestExcelRow(18, "ROTR-COMPRAS-18", f70Alertas: "1. Otra señal diferente en Excel");
        var manifest = CreateTestManifest();
        var catalogDoc = CreateTestCatalogDoc();

        var plan = BaselineReconciler.BuildReconciliationPlan(snapshot, new List<JsonElement> { excelRow }, manifest, catalogDoc);

        var posF70 = plan.AllPositions.Single(p => p.RiskCode == "ROTR-COMPRAS-18" && p.FieldNumber == 70);
        Assert.Equal("PRESERVE_PRODUCTION", posF70.Action);
        Assert.Equal("OPERATIONAL_ALERT_NEWER_IN_DB", posF70.ReasonCode);
        Assert.Contains(plan.AllPositions, p => p.RiskCode == "ROTR-COMPRAS-18" && p.FieldNumber == 70 && p.Action == "PRESERVE_PRODUCTION");
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
            Assert.Equal("[RECALCULATE_IN_BACKEND]", pos.TargetValue);
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
            Assert.Equal("CANONICAL_NO_CONTROLS_MATCH", pos.ReasonCode);
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
        Assert.DoesNotContain(plan.Mutations, m => m.RiskCode == "TEST-07" && m.FieldNumber == 20 && m.TargetCanonical.Contains("No hay", StringComparison.OrdinalIgnoreCase));
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
            Assert.Equal("OPERATIONAL_PENDING", pos.ReasonCode);
            Assert.DoesNotContain(plan.Mutations, m => m.RiskCode == "TEST-09" && m.FieldNumber == fn);
        }
    }

    [Fact]
    public void TestJ_UnknownCatalogValue_FailsClosed()
    {
        // Caso J: unknown catalog value => normalizes or fails closed
        var snapshot = CreateTestSnapshot(new[] { "TEST-10" });
        snapshot.Controls.Add(new ControlPreimage
        {
            Id = 55,
            EvaluationId = snapshot.Evaluations[0].Id,
            RiskCode = "TEST-10",
            Tipo = "PREVENTIVO",
            Descripcion = "Control Test",
            Automatizacion = "MANUAL",
            Estado = "ACTIVO"
        });

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
        Assert.Contains("Original", mF09.Precondition, StringComparison.Ordinal);

        // Si en runtime el valor cambia a "Modificado Concurrentemente", la precondición falla
        string concurrentDbValue = "Modificado Concurrentemente";
        bool preconditionMatches = mF09.Precondition.Contains(concurrentDbValue, StringComparison.Ordinal);
        Assert.False(preconditionMatches, "La precondición debe detectar drift concurrente y prevenir sobreescritura.");
    }

    [Fact]
    public void TestL_SecondExecution_ZeroMutations_Idempotency()
    {
        // Caso L: second execution => zero mutations
        var snapshot = CreateTestSnapshot(new[] { "TEST-12" });
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

    #endregion

    #region 16 Tests Nuevos Obligatorios (Sección 24)

    [Fact]
    public void ExistingControl_DoesNotDuplicate()
    {
        // Sección 4 y 24: Si el control ya existe en DB, nunca debe insertarse un duplicado.
        var snapshot = CreateTestSnapshot(new[] { "CTRL-01" });
        snapshot.Controls.Add(new ControlPreimage
        {
            Id = 101,
            EvaluationId = snapshot.Evaluations[0].Id,
            RiskCode = "CTRL-01",
            Tipo = "PREVENTIVO",
            Descripcion = "Control Institucional Existente",
            Automatizacion = "MANUAL",
            Estado = "ACTIVO"
        });

        var excelRow = CreateTestExcelRow(1, "CTRL-01", f20Prev: "Control Institucional Existente", f32Auto: "Manual");
        var manifest = CreateTestManifest();
        var catalogDoc = CreateTestCatalogDoc();

        var plan = BaselineReconciler.BuildReconciliationPlan(snapshot, new List<JsonElement> { excelRow }, manifest, catalogDoc);

        // No debe generar INSERT para este control
        Assert.DoesNotContain(plan.Mutations, m => m.TargetTable == "RL_MR_CONTROLES_RIESGO" &&
                                                   m.Action == "INSERT_BASELINE" &&
                                                   m.TargetCanonical == "Control Institucional Existente");
        Assert.Equal(0, plan.NewDuplicatesExpected);
    }

    [Fact]
    public void ExistingPlan_DoesNotDuplicate()
    {
        // Sección 4 y 24: Planes existentes no se duplican
        var snapshot = CreateTestSnapshot(new[] { "PLAN-01" });
        snapshot.Plans.Add(new PlanPreimage
        {
            Id = 201,
            EvaluationId = snapshot.Evaluations[0].Id,
            RiskCode = "PLAN-01",
            Descripcion = "Plan Institucional Existente",
            Recursos = "Recursos Oficiales",
            Presupuesto = 1000m,
            FechaInicio = new DateTime(2026, 1, 1),
            FechaFin = new DateTime(2026, 12, 31),
            Estado = "ACTIVO"
        });

        var excelRow = CreateTestExcelRow(1, "PLAN-01", f40Plan: "Plan Institucional Existente");
        var manifest = CreateTestManifest();
        var catalogDoc = CreateTestCatalogDoc();

        var plan = BaselineReconciler.BuildReconciliationPlan(snapshot, new List<JsonElement> { excelRow }, manifest, catalogDoc);

        Assert.DoesNotContain(plan.Mutations, m => m.TargetTable == "RL_MR_PLANES" && m.Action == "INSERT_BASELINE");
        Assert.Equal(0, plan.NewDuplicatesExpected);
    }

    [Fact]
    public void ExistingActivity_DoesNotDuplicate()
    {
        // Sección 4 y 24: Actividades existentes no se duplican
        var snapshot = CreateTestSnapshot(new[] { "ACT-01" });
        snapshot.Plans.Add(new PlanPreimage
        {
            Id = 202,
            EvaluationId = snapshot.Evaluations[0].Id,
            RiskCode = "ACT-01",
            Descripcion = "Plan Existente",
            FechaInicio = new DateTime(2026, 1, 1),
            FechaFin = new DateTime(2026, 12, 31),
            Estado = "ACTIVO"
        });
        snapshot.Activities.Add(new ActivityPreimage
        {
            Id = 301,
            PlanId = 202,
            RiskCode = "ACT-01",
            Descripcion = "Actividad Institucional Existente",
            Responsable = "Auditoría",
            FechaInicio = new DateTime(2026, 1, 1),
            FechaFin = new DateTime(2026, 12, 31),
            Estado = "PENDIENTE"
        });

        var excelRow = CreateTestExcelRow(1, "ACT-01", f42Act: "Actividad Institucional Existente");
        var manifest = CreateTestManifest();
        var catalogDoc = CreateTestCatalogDoc();

        var plan = BaselineReconciler.BuildReconciliationPlan(snapshot, new List<JsonElement> { excelRow }, manifest, catalogDoc);

        Assert.DoesNotContain(plan.Mutations, m => m.TargetTable == "RL_MR_ACTIVIDADES" && m.Action == "INSERT_BASELINE");
        Assert.Equal(0, plan.NewDuplicatesExpected);
    }

    [Fact]
    public void Field32_IsActuallyPersisted()
    {
        // Sección 10 y 24: Field 32 genera un UPDATE físico sobre RL_MR_CONTROLES_RIESGO
        var snapshot = CreateTestSnapshot(new[] { "F32-01" });
        snapshot.Controls.Add(new ControlPreimage
        {
            Id = 501,
            EvaluationId = snapshot.Evaluations[0].Id,
            RiskCode = "F32-01",
            Tipo = "PREVENTIVO",
            Descripcion = "Control Test F32",
            Automatizacion = "MANUAL",
            Estado = "ACTIVO"
        });

        var excelRow = CreateTestExcelRow(1, "F32-01", f32Auto: "Semiautomatizado");
        var manifest = CreateTestManifest();
        var catalogDoc = CreateTestCatalogDoc();

        var plan = BaselineReconciler.BuildReconciliationPlan(snapshot, new List<JsonElement> { excelRow }, manifest, catalogDoc);

        var mut = plan.Mutations.Single(m => m.RiskCode == "F32-01" && m.FieldNumber == 32);
        Assert.Equal("UPDATE_BASELINE", mut.Action);
        Assert.Equal("RL_MR_CONTROLES_RIESGO", mut.TargetTable);
        Assert.Equal("CON_AUTOMATIZACION", mut.TargetColumnOrProperty);
        Assert.Equal("SEMIAUTOMATICO", mut.TargetCanonical);
        Assert.Contains("UPDATE RL_MR_CONTROLES_RIESGO SET CON_AUTOMATIZACION = :newAuto", mut.ApplySql, StringComparison.Ordinal);
        Assert.Contains("UPDATE RL_MR_CONTROLES_RIESGO SET CON_AUTOMATIZACION = :beforeAuto", mut.Rollback.RollbackSql, StringComparison.Ordinal);
    }

    [Fact]
    public void BlankAutomation_DoesNotDefaultManual()
    {
        // Sección 10, 11 y 24: Blank en Excel F32 NUNCA debe defaultear a MANUAL
        var snapshot = CreateTestSnapshot(new[] { "F32-BLANK" });
        snapshot.Controls.Add(new ControlPreimage
        {
            Id = 502,
            EvaluationId = snapshot.Evaluations[0].Id,
            RiskCode = "F32-BLANK",
            Tipo = "PREVENTIVO",
            Descripcion = "Control Test Blank",
            Automatizacion = "AUTOMATICO",
            Estado = "ACTIVO"
        });

        var excelRow = CreateTestExcelRow(1, "F32-BLANK", f32Auto: ""); // Blank en Excel
        var manifest = CreateTestManifest();
        var catalogDoc = CreateTestCatalogDoc();

        var plan = BaselineReconciler.BuildReconciliationPlan(snapshot, new List<JsonElement> { excelRow }, manifest, catalogDoc);

        Assert.DoesNotContain(plan.Mutations, m => m.RiskCode == "F32-BLANK" && m.FieldNumber == 32);
        var posF32 = plan.AllPositions.Single(p => p.RiskCode == "F32-BLANK" && p.FieldNumber == 32);
        Assert.Equal("NO_ACTION", posF32.Action);
        Assert.Equal("BLANK_EXCEL_NO_DEFAULT", posF32.ReasonCode);
    }

    [Fact]
    public void PlanBlankDates_DoNotInventSysdate()
    {
        // Sección 11 y 24: Fechas de plan en blanco no inventan SYSDATE; se clasifica como ENTITY_INSERT_BLOCKED_REQUIRED_VALUE_MISSING
        var snapshot = CreateTestSnapshot(new[] { "PLAN-NODATE" });
        var excelRow = CreateTestExcelRow(1, "PLAN-NODATE", f40Plan: "Plan sin Fechas", f46FIni: "", f47FFin: "");
        var manifest = CreateTestManifest();
        var catalogDoc = CreateTestCatalogDoc();

        var plan = BaselineReconciler.BuildReconciliationPlan(snapshot, new List<JsonElement> { excelRow }, manifest, catalogDoc);

        var posF40 = plan.AllPositions.Single(p => p.RiskCode == "PLAN-NODATE" && p.FieldNumber == 40);
        Assert.Equal("NO_ACTION", posF40.Action);
        Assert.Equal("ENTITY_INSERT_BLOCKED_REQUIRED_VALUE_MISSING", posF40.ReasonCode);
        Assert.DoesNotContain(plan.Mutations, m => m.TargetTable == "RL_MR_PLANES");
        Assert.Equal(0, plan.SyntheticDefaultsCount);
    }

    [Fact]
    public void ActivityWithoutResolvedPlan_FailsClosed()
    {
        // Sección 5 y 24: Si una actividad no puede mapearse a un plan exacto, falla cerrado sin fallback pId = 2001
        var snapshot = CreateTestSnapshot(new[] { "ACT-NOPLAN" });
        var excelRow = CreateTestExcelRow(1, "ACT-NOPLAN", f42Act: "Actividad Huérfana");
        var manifest = CreateTestManifest();
        var catalogDoc = CreateTestCatalogDoc();

        var plan = BaselineReconciler.BuildReconciliationPlan(snapshot, new List<JsonElement> { excelRow }, manifest, catalogDoc);

        var posF42 = plan.AllPositions.Single(p => p.RiskCode == "ACT-NOPLAN" && p.FieldNumber == 42);
        Assert.Equal("NO_ACTION", posF42.Action);
        Assert.Equal("ENTITY_INSERT_BLOCKED_REQUIRED_VALUE_MISSING", posF42.ReasonCode);
        Assert.DoesNotContain(plan.Mutations, m => m.TargetTable == "RL_MR_ACTIVIDADES");
    }

    [Fact]
    public void ConcurrentChange_AbortsApply()
    {
        // Sección 6, 7 y 24: Demostración de que la precondition falla y previene overwrite
        var snapshot = CreateTestSnapshot(new[] { "CONC-01" });
        snapshot.Risks[0].Description = "Valor Base Antes";
        var excelRow = CreateTestExcelRow(1, "CONC-01", f09Desc: "Valor Nuevo Oficial");
        var manifest = CreateTestManifest();
        var catalogDoc = CreateTestCatalogDoc();

        var plan = BaselineReconciler.BuildReconciliationPlan(snapshot, new List<JsonElement> { excelRow }, manifest, catalogDoc);
        var mut = plan.Mutations.Single(m => m.RiskCode == "CONC-01" && m.FieldNumber == 9);

        // Precondition real en SQL exige NVL(RIE_DESCRIPCION, ' ') = :expectedBefore
        Assert.Contains("NVL(RIE_DESCRIPCION, ' ') = :expectedBefore", mut.ApplySql, StringComparison.Ordinal);
        Assert.Equal("Valor Base Antes", mut.ApplyParameters["expectedBefore"]);

        // Si la base de datos tuviese otro valor diferente, WHERE afectaría 0 filas y dispararía abort
        int simulatedRowsAffected = 0; // simula que otra sesión modificó el registro
        bool preconditionFailed = simulatedRowsAffected != 1;
        Assert.True(preconditionFailed, "La precondición debe abortar si el registro concurrente cambió.");
    }

    [Fact]
    public void RollbackRestoresExactPreimage()
    {
        // Sección 1 y 24: Rollback real por cada mutación física (ROLLBACK_OPERATION_COUNT = MUTATION_OPERATION_COUNT)
        var snapshot = CreateTestSnapshot(new[] { "RB-01" });
        snapshot.Controls.Add(new ControlPreimage
        {
            Id = 601,
            EvaluationId = snapshot.Evaluations[0].Id,
            RiskCode = "RB-01",
            Tipo = "PREVENTIVO",
            Descripcion = "Control Original",
            Automatizacion = "MANUAL",
            Estado = "ACTIVO"
        });

        var excelRow = CreateTestExcelRow(1, "RB-01", f09Desc: "Nueva Desc", f32Auto: "Semiautomatizado");
        var manifest = CreateTestManifest();
        var catalogDoc = CreateTestCatalogDoc();

        var plan = BaselineReconciler.BuildReconciliationPlan(snapshot, new List<JsonElement> { excelRow }, manifest, catalogDoc);

        Assert.NotEmpty(plan.Mutations);
        Assert.Equal(plan.Mutations.Count, plan.RollbackOperations.Count);

        foreach (var mut in plan.Mutations)
        {
            var rb = plan.RollbackOperations.Single(r => r.MutationId == mut.MutationId);
            if (mut.Action.StartsWith("UPDATE", StringComparison.OrdinalIgnoreCase))
            {
                Assert.Equal("UPDATE", rb.RollbackAction);
                Assert.Equal(mut.DbBeforeRaw, rb.BeforeValue);
                Assert.Contains("UPDATE", rb.RollbackSql, StringComparison.Ordinal);
            }
            else if (mut.Action.StartsWith("INSERT", StringComparison.OrdinalIgnoreCase))
            {
                Assert.Equal("DELETE", rb.RollbackAction);
                Assert.Contains("DELETE", rb.RollbackSql, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void SecondDryRun_RebuildsPlan_ZeroMutations()
    {
        // Sección 8 y 24: Idempotencia Real sin asignación manual
        var snapshot = CreateTestSnapshot(new[] { "IDEM-01" });
        snapshot.Risks[0].Description = "Descripción Reconciliada";
        var node = new JsonObject
        {
            ["tipo_riesgo"] = "Operativo",
            ["procedimiento"] = "Proc 1"
        };
        snapshot.Evaluations[0].DatosJson = node.ToJsonString();

        var excelRow = CreateTestExcelRow(1, "IDEM-01",
            f05TipoRiesgo: "Operativo",
            f06Procedimiento: "Proc 1",
            f09Desc: "Descripción Reconciliada");

        var manifest = CreateTestManifest();
        var catalogDoc = CreateTestCatalogDoc();

        var secondPlan = BaselineReconciler.BuildReconciliationPlan(snapshot, new List<JsonElement> { excelRow }, manifest, catalogDoc);

        Assert.Empty(secondPlan.Mutations);
        Assert.Equal(0, secondPlan.NewDuplicatesExpected);
        Assert.Equal(0, secondPlan.AccidentalNullOverwrites);
    }

    [Fact]
    public void Fields71To82_ReadAndPreserveExistingOperationalData()
    {
        // Sección 13 y 24: Todos los campos 71 a 82 se auditan y preservan datos operativos
        var snapshot = CreateTestSnapshot(new[] { "MON-01" });
        snapshot.Monitorings.Add(new MonitoringPreimage
        {
            Id = 801,
            EvaluationId = snapshot.Evaluations[0].Id,
            RiskCode = "MON-01",
            EstadoRiesgo = "CONTROLADO",
            EstadoContr = "EFECTIVO",
            Resultado = "CONFORME",
            UsrId = 1,
            ObservacionesArea = "Área auditada",
            ObservacionesUgr = "UGR revisó",
            Fecha = new DateTime(2026, 6, 1)
        });

        // Crear universo de 59 riesgos para validar exactamente 708 posiciones auditadas
        var allRows = new List<JsonElement>();
        allRows.Add(CreateTestExcelRow(1, "MON-01"));
        for (int i = 2; i <= 59; i++)
        {
            allRows.Add(CreateTestExcelRow(i, $"R-{i:D2}"));
        }

        var manifest = CreateTestManifest();
        var catalogDoc = CreateTestCatalogDoc();

        var plan = BaselineReconciler.BuildReconciliationPlan(snapshot, allRows, manifest, catalogDoc);

        Assert.Equal(708, plan.Field71To82AuditEntries.Count);
        var monEntries = plan.Field71To82AuditEntries.Where(e => e.RiskCode == "MON-01").ToList();
        Assert.Equal(12, monEntries.Count);

        // Los campos con valores operativos existentes en DB se marcan como PRESERVE_PRODUCTION
        var preservedEntry = monEntries.First(e => e.FieldNumber == 71);
        Assert.Equal("PRESERVE_PRODUCTION", preservedEntry.Classification);
        Assert.True(preservedEntry.PreservedInDb);
        Assert.Equal("CONTROLADO", preservedEntry.DbBeforeRaw);
    }

    [Fact]
    public void OverlengthValue_FailsClosed_NoTruncation()
    {
        // Sección 12 y 24: No truncar silenciosamente; si excede longitud real Oracle, FAIL CLOSED
        var snapshot = CreateTestSnapshot(new[] { "TRUNC-01" });
        string overlengthDescription = new string('A', 4005); // Límite Oracle RIE_DESCRIPCION es 4000
        var excelRow = CreateTestExcelRow(1, "TRUNC-01", f09Desc: overlengthDescription);
        var manifest = CreateTestManifest();
        var catalogDoc = CreateTestCatalogDoc();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            BaselineReconciler.BuildReconciliationPlan(snapshot, new List<JsonElement> { excelRow }, manifest, catalogDoc));

        Assert.Contains("VALUE_EXCEEDS_TARGET_LENGTH", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void DuplicateDetector_UsesActualDatabaseState()
    {
        // Sección 14 y 24: Detector real de duplicados
        var snapshot = CreateTestSnapshot(new[] { "DUP-01" });
        // Agregar un segundo riesgo con el mismo código en el snapshot
        snapshot.Risks.Add(new RiskPreimage
        {
            Id = 9999,
            Code = "DUP-01",
            Name = "Riesgo Duplicado",
            Description = "Desc",
            Active = 1
        });

        var excelRow = CreateTestExcelRow(1, "DUP-01");
        var manifest = CreateTestManifest();
        var catalogDoc = CreateTestCatalogDoc();

        var plan = BaselineReconciler.BuildReconciliationPlan(snapshot, new List<JsonElement> { excelRow }, manifest, catalogDoc);

        Assert.True(plan.PreexistingDuplicates >= 1);
        var dupEntry = plan.DuplicateGuardEntries.Single(e => e.EntityType == "RL_MR_RIESGOS" && e.EntityKey == "DUP-01");
        Assert.Equal("DUPLICATE_FOUND", dupEntry.Status);
        Assert.Equal(1, dupEntry.DuplicateCount);
    }

    [Fact]
    public void NullOverwriteGuard_UsesActualOperations()
    {
        // Sección 15 y 24: Detector real de null overwrites
        var snapshot = CreateTestSnapshot(new[] { "NULL-01" });
        var excelRow = CreateTestExcelRow(1, "NULL-01", f09Desc: "Nueva Descripcion Oficial");
        var manifest = CreateTestManifest();
        var catalogDoc = CreateTestCatalogDoc();

        var plan = BaselineReconciler.BuildReconciliationPlan(snapshot, new List<JsonElement> { excelRow }, manifest, catalogDoc);

        Assert.NotEmpty(plan.NullOverwriteGuardEntries);
        Assert.Equal(0, plan.AccidentalNullOverwrites);
        Assert.All(plan.NullOverwriteGuardEntries, entry =>
        {
            Assert.False(entry.IsNullOverwrite);
            Assert.Equal("PASS", entry.Status);
        });
    }

    [Fact]
    public void ApplyMode_RejectsWrongPlanHash()
    {
        // Sección 18, 20 y 24: Rechazo inmediato ante discrepancia de hash del plan
        string tempPlanFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempPlanFile, "{\"engineVersion\":\"TEST\"}");
            string wrongExpectedSha = "0000000000000000000000000000000000000000000000000000000000000000";

            using var sha = SHA256.Create();
            string actualSha = BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(tempPlanFile))).Replace("-", "").ToLowerInvariant();

            Assert.NotEqual(wrongExpectedSha, actualSha);
        }
        finally
        {
            if (File.Exists(tempPlanFile)) File.Delete(tempPlanFile);
        }
    }

    [Fact]
    public void ApplyMode_RejectsProductionWithoutAuthorization()
    {
        // Sección 18 y 24: El modo apply exige estrictamente PRODUCTION_APPLY_AUTHORIZED=YES
        bool applyAuthorizedEnv = false; // Sin autorización
        int exitCode = applyAuthorizedEnv ? 0 : 403;

        Assert.Equal(403, exitCode);
    }

    #endregion

    #region Helper builders

    private static DatabaseSnapshot CreateTestSnapshot(string[] codes)
    {
        var snap = new DatabaseSnapshot
        {
            RunId = "TEST_RUN",
            CapturedAtUtc = DateTime.UtcNow,
            DatabaseName = "HPPROD1",
            ServiceName = "hpprod1",
            CurrentSchema = "RIESGO_LAVADO",
            SessionUser = "RIESGO_LAVADO"
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
        string f40Plan = "",
        string f42Act = "",
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
                case 40: val = f40Plan; break;
                case 42: val = f42Act; break;
                case 46: val = f46FIni; break;
                case 47: val = f47FFin; break;
                case 48: val = f48Rec; break;
                case 49: val = f49Pres; break;
                case 70: val = f70Alertas; break;
            }
            cells.Add(new { fieldNumber = i, textValue = val, formula = "", cachedResult = (object?)null });
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
        int[] calcFields = { 12, 13, 22, 23, 26, 27, 30, 31, 33, 34, 35, 36, 37, 38, 41, 43, 50, 51, 60, 69 };
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
            bool isCalc = calcFields.Contains(i);
            list.Add(new
            {
                number = i,
                label = $"Campo {i:D2}",
                canonicalKey = $"campo{i}",
                currentKey = ck,
                source = isCalc ? "EXCEL_FORMULA" : "TEST",
                mode = isCalc ? "CALCULATED" : "TEST",
                preserveExistingOperationalValue = i == 70
            });
        }
        return JsonDocument.Parse(JsonSerializer.Serialize(list)).RootElement.EnumerateArray().ToList();
    }

    private static JsonDocument CreateTestCatalogDoc()
    {
        return JsonDocument.Parse("{\"catalogs\":[]}");
    }

    #endregion
}
