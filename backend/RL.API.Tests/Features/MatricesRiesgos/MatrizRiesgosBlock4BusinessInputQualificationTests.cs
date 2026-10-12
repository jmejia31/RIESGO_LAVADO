using System.Text.Json;
using RL.Tools.ReconcileMatricesBaseline;
using Xunit;

namespace RL.API.Tests.Features.MatricesRiesgos;

public sealed class MatrizRiesgosBlock4BusinessInputQualificationTests
{
    [Fact]
    public void BusinessRequest_ContainsOnlyHumanInputs()
    {
        var result = Build(f40: "1. Plan A");
        Assert.Equal(new[] { "END_DATE", "START_DATE" }, result.RequestRows.Select(x => x.Attribute).OrderBy(x => x).ToArray());
    }

    [Fact]
    public void BusinessRequest_KeyIsDeterministic()
    {
        var first = Build(f40: "1. Plan A").RequestRows.Select(x => x.RequestKey);
        var second = Build(f40: "1. Plan A").RequestRows.Select(x => x.RequestKey);
        Assert.Equal(first, second);
    }

    [Fact]
    public void BusinessRequest_NoDuplicateKeys()
    {
        var result = Build(f40: "1. Plan A", f42: "Activity A");
        Assert.Equal(0, result.DuplicateRequestKeys);
        Assert.Equal(result.RequestRows.Count, result.RequestRows.Select(x => x.RequestKey).Distinct().Count());
    }

    [Fact]
    public void BusinessRequest_ResponseColumnsStartBlank()
    {
        var result = Build(f40: "1. Plan A");
        Assert.All(result.RequestRows, row => Assert.True(string.IsNullOrEmpty(row.BusinessResponse) && string.IsNullOrEmpty(row.BusinessNotes)));
        Assert.Equal(0, result.NonemptyBusinessResponses);
    }

    [Fact]
    public void ExistingControl_DoesNotCreateBusinessInput()
    {
        var result = Build(f20: "1. Control A", existingControl: "Control A");
        Assert.Empty(result.RequestRows);
        Assert.Contains(result.QualificationRows, x => x.Status == "MATCH_EXISTING");
    }

    [Fact]
    public void MissingControl_WithCreationRules_DoesNotRequestStateOrAutomation()
    {
        var result = Build(f20: "1. Control A", f32: "");
        Assert.Empty(result.RequestRows);
        Assert.Contains(result.MissingControls, x => x.Status == "MISSING_CONTROL");
        Assert.DoesNotContain(result.RequestRows, x => x.Attribute is "CON_ESTADO" or "CON_AUTOMATIZACION");
    }

    [Fact]
    public void EmptyTerminalControl_RequestsDescription()
    {
        var result = Build(f20: "1. Control A\n2.");
        var request = Assert.Single(result.RequestRows);
        Assert.Equal("CON_DESCRIPCION", request.Attribute);
        Assert.Equal(2, request.EntityOrdinal);
    }

    [Fact]
    public void MissingPlan_RequestsStartAndEndDates()
    {
        var result = Build(f40: "1. Plan A");
        Assert.Equal(2, result.RequestRows.Count);
        Assert.Contains(result.RequestRows, x => x.Attribute == "START_DATE" && x.DbColumn == "PLA_FECHA_INICIO");
        Assert.Contains(result.RequestRows, x => x.Attribute == "END_DATE" && x.DbColumn == "PLA_FECHA_FIN");
    }

    [Fact]
    public void MissingPlan_WithSystemState_DoesNotRequestStatus()
    {
        var result = Build(f40: "1. Plan A");
        Assert.DoesNotContain(result.RequestRows, x => x.Attribute == "PLA_ESTADO");
        Assert.Contains(result.CreationRuleEvidence, x => x.RuleId == "NEW_PLAN_STATE" && x.Verified && x.ExpectedValue == "PENDIENTE");
    }

    [Fact]
    public void EmptyTerminalPlan_RequestsDescription()
    {
        var result = Build(f40: "1. Plan A\n2.");
        Assert.Contains(result.RequestRows, x => x.EntityOrdinal == 2 && x.Attribute == "PLA_DESCRIPCION");
        Assert.Contains(result.RequestRows, x => x.EntityOrdinal == 2 && x.Attribute == "START_DATE");
        Assert.Contains(result.RequestRows, x => x.EntityOrdinal == 2 && x.Attribute == "END_DATE");
    }

    [Fact]
    public void Activity_RequestsExactPlanAssociation()
    {
        var result = Build(f40: "1. Plan A\n2. Plan B", f42: "Activity A");
        Assert.Contains(result.RequestRows, x => x.EntityType == "ACTIVITY" && x.Attribute == "PLAN_ASSOCIATION");
    }

    [Fact]
    public void Activity_RequestsResponsible()
    {
        var result = Build(f40: "1. Plan A\n2. Plan B", f42: "Activity A");
        Assert.Contains(result.RequestRows, x => x.Attribute == "ACT_RESPONSABLE");
    }

    [Fact]
    public void Activity_RequestsDates()
    {
        var result = Build(f40: "1. Plan A\n2. Plan B", f42: "Activity A");
        Assert.Contains(result.RequestRows, x => x.Attribute == "START_DATE" && x.DbColumn == "ACT_FECHA_INICIO");
        Assert.Contains(result.RequestRows, x => x.Attribute == "END_DATE" && x.DbColumn == "ACT_FECHA_FIN");
    }

    [Fact]
    public void Activity_SystemStateNotRequestedWhenRuleVerified()
    {
        var result = Build(f40: "1. Plan A\n2. Plan B", f42: "Activity A");
        Assert.DoesNotContain(result.RequestRows, x => x.Attribute == "ACT_ESTADO");
        Assert.Contains(result.CreationRuleEvidence, x => x.RuleId == "NEW_ACTIVITY_STATE" && x.Verified && x.ExpectedValue == "PENDIENTE");
    }

    [Fact]
    public void F45_AssociationProducesSingleRequest()
    {
        var result = Build(f40: "1. Plan A\n2. Plan B", f45: "Responsable A");
        var request = Assert.Single(result.RequestRows.Where(x => x.Attribute == "PLAN_ASSOCIATION"));
        Assert.Equal("PLAN_ASSOCIATION", request.Attribute);
        Assert.Equal("PLA_RESPONSABLES", request.DbColumn);
    }

    [Fact]
    public void DatabaseDefaults_DoNotProduceHumanInput()
    {
        var result = Build(f40: "1. Plan A");
        Assert.DoesNotContain(result.RequestRows, x => x.Attribute is "PLA_AVANCE" or "PLA_PRESUPUESTO" or "ACT_AVANCE");
        Assert.Contains(result.CreationRuleEvidence, x => x.RuleId == "DB_DEFAULT_PLA_AVANCE" && x.Verified && x.ExpectedValue == "0");
        Assert.Contains(result.CreationRuleEvidence, x => x.RuleId == "DB_DEFAULT_PLA_PRESUPUESTO" && x.Verified && x.ExpectedValue == "0");
        Assert.Contains(result.CreationRuleEvidence, x => x.RuleId == "DB_DEFAULT_ACT_AVANCE" && x.Verified && x.ExpectedValue == "0");
    }

    [Fact]
    public void DependencyRows_DoNotDuplicateRootRequest()
    {
        var result = Build(f40: "1. Plan A\n2. Plan B", f42: "1.1 Activity A");
        Assert.Equal(result.RequestRows.Count, result.RequestRows.Select(x => x.RequestKey).Distinct().Count());
        Assert.NotEmpty(result.Dependencies);
        Assert.Equal(7, result.RequestRows.Count);
    }

    [Fact]
    public void Qualification_OutputArtifactsAreCompleteAndHashed()
    {
        string output = Path.Combine(Path.GetTempPath(), "block4-qualification-test-" + Guid.NewGuid().ToString("N"));
        try
        {
            var result = Build(f40: "1. Plan A");
            var hashes = BusinessInputQualification.WriteArtifacts(result, output);
            string[] expected =
            [
                "block4_business_input_request.csv", "block4_business_input_qualification.csv",
                "control_cell_parsing_audit.csv", "plan_cell_parsing_audit.csv", "activity_cell_parsing_audit.csv",
                "missing_controls_qualified.csv", "missing_plans_qualified.csv", "missing_activities_qualified.csv",
                "creation_rule_evidence.csv", "field45_cardinality_reconciliation.csv", "blocker_dependency_graph.csv",
                "block4_business_input_summary.json", "SHA256SUMS.json"
            ];
            Assert.All(expected, name => Assert.True(File.Exists(Path.Combine(output, name)), name));
            Assert.Equal(12, hashes.Count);
            using var summary = JsonDocument.Parse(File.ReadAllText(Path.Combine(output, "block4_business_input_summary.json")));
            Assert.Equal(result.RequestRows.Count, summary.RootElement.GetProperty("requestRows").GetInt32());
            Assert.Equal(0, summary.RootElement.GetProperty("duplicateRequestKeys").GetInt32());
        }
        finally
        {
            if (Directory.Exists(output)) Directory.Delete(output, recursive: true);
        }
    }

    private static BusinessInputQualificationResult Build(
        string f20 = "", string f32 = "", string f40 = "", string f42 = "", string f45 = "", string existingControl = "")
    {
        var rows = new List<JsonElement>();
        using var rowsDoc = JsonDocument.Parse("[]");
        var snapshot = new DatabaseSnapshot
        {
            DatabaseName = "HPPROD1", ServiceName = "hpprod1", InstanceName = "hpprod1",
            CurrentSchema = "RIESGO_LAVADO", SessionUser = "RIESGO_LAVADO"
        };
        for (int i = 1; i <= 59; i++)
        {
            string code = $"RISK-{i:D2}";
            var cells = Enumerable.Range(1, 82).Select(_ => "").ToArray();
            if (i == 1)
            {
                cells[2] = "ÁREA DE PRUEBA";
                cells[19] = f20;
                cells[31] = f32;
                cells[39] = f40;
                cells[41] = f42;
                cells[44] = f45;
            }
            using var rowDoc = JsonDocument.Parse(JsonSerializer.Serialize(new
            {
                riskNo = i, riskCode = code,
                cells = cells.Select(textValue => new { textValue }).ToArray()
            }));
            rows.Add(rowDoc.RootElement.Clone());
            snapshot.Risks.Add(new RiskPreimage { Id = i, Code = code, Active = 1 });
            snapshot.Evaluations.Add(new EvaluationPreimage { Id = 1000 + i, RiskId = i, RiskCode = code, Active = 1 });
            snapshot.Projections.Add(new ProjectionPreimage { Id = 2000 + i, EvaluationId = 1000 + i, RiskCode = code, RespuestaRiesgo = "MITIGAR", NivelResidual = "BAJO" });
        }
        if (!string.IsNullOrWhiteSpace(existingControl))
            snapshot.Controls.Add(new ControlPreimage { Id = 3001, EvaluationId = 1001, RiskCode = "RISK-01", Tipo = "PREVENTIVO", Descripcion = existingControl });

        var fields = Enumerable.Range(1, 82).Select(i =>
        {
            using var doc = JsonDocument.Parse(JsonSerializer.Serialize(new { label = $"Field {i:D2}" }));
            return doc.RootElement.Clone();
        }).ToArray();
        using var catalog = JsonDocument.Parse("""
        {"catalogs":[{"catalogId":"CONTROL_AUTOMATION","items":[
          {"key":"AUTOMATIZADO","label":"Automatizado","aliases":[]},
          {"key":"SEMIAUTOMATIZADO","label":"Semiautomatizado","aliases":["Semi-Automatizado"]},
          {"key":"MANUAL","label":"Manual","aliases":[]}
        ]}]}
        """);
        var rules = new BusinessCreationRules
        {
            ControlState = "ACTIVO", AutomationWhenF32Empty = "MANUAL", PlanState = "PENDIENTE", ActivityState = "PENDIENTE",
            PlanAdvanceDefaultVerified = true, PlanBudgetDefaultVerified = true, ActivityAdvanceDefaultVerified = true,
            Evidence = [new("NEW_CONTROL_STATE", "ACTIVO", "TEST", "fixture", "symbol", true, "evidence"),
                       new("EMPTY_F32_CONTROL_AUTOMATION", "MANUAL", "TEST", "fixture", "symbol", true, "evidence"),
                       new("NEW_PLAN_STATE", "PENDIENTE", "TEST", "fixture", "symbol", true, "evidence"),
                       new("NEW_ACTIVITY_STATE", "PENDIENTE", "TEST", "fixture", "symbol", true, "evidence"),
                       new("DB_DEFAULT_PLA_AVANCE", "0", "TEST", "fixture", "symbol", true, "evidence"),
                       new("DB_DEFAULT_PLA_PRESUPUESTO", "0", "TEST", "fixture", "symbol", true, "evidence"),
                       new("DB_DEFAULT_ACT_AVANCE", "0", "TEST", "fixture", "symbol", true, "evidence")]
        };
        return BusinessInputQualification.Build(rows, fields, catalog, snapshot, rules, "TEST_RUN", new string('a', 40), BusinessInputQualification.ExpectedWorkbookSha256);
    }
}
