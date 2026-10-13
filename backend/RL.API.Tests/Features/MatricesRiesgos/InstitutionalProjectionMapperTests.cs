using System.Text.Json;
using System.Text.Json.Nodes;
using RL.API.Features.MatricesRiesgos.Application;
using RL.API.Features.MatricesRiesgos.Contracts;
using Xunit;

namespace RL.API.Tests.Features.MatricesRiesgos;

public sealed class InstitutionalProjectionMapperTests
{
    [Fact]
    public void Map_ReturnsExactly82FieldsInManifestOrder_WithExplicitControlAbsence()
    {
        InstitutionalProjectionMapper mapper = new();
        InstitutionalEvaluationProjectionDto result = mapper.Map(CreateSource());

        Assert.Equal(82, result.Fields.Count);
        Assert.Equal(Enumerable.Range(1, 82), result.Fields.Select(field => field.FieldNumber));
        Assert.Equal(82, result.Fields.Select(field => field.FieldNumber).Distinct().Count());
        Assert.Equal(3L, result.Fields[11].Value);
        Assert.Equal(1L, result.Fields[36].Value);
        Assert.Equal("Sección de Cumplimiento", result.Fields[2].Value);
        Assert.Equal("Riesgo inherente institucional", result.Fields[7].Value);
        Assert.Equal("Evaluación narrativa institucional", result.Fields[8].Value);
        Assert.Equal("Aceptar", result.Fields[38].Value);
        Assert.Equal("No hay", result.Fields[19].Value);
        Assert.Equal("ABSENT_CONTROL_PROJECTED", result.Fields[19].State);
        Assert.Equal("Inexistente", result.Fields[20].Value);
        Assert.Equal("LEGIT_FORMULA_BLANK", result.Fields[32].State);
        Assert.Equal(59, result.Fields[0].Value);
    }

    [Fact]
    public void Map_PreservesRepeatedControlDescriptionsAndPlanActivityCounts()
    {
        InstitutionalProjectionMapper mapper = new();
        InstitutionalProjectionSource source = CreateSource() with
        {
            Evaluation = WithCalculation(CreateSource().Evaluation, 33, 0.63m),
            Controls =
            [
                new ControlRiesgoDto { ConId = 20, ConEvaluacionId = 123, ConTipo = "PREVENTIVO", ConDescripcion = "Primero", ConAutomatizacion = "MANUAL" },
                new ControlRiesgoDto { ConId = 21, ConEvaluacionId = 123, ConTipo = "PREVENTIVO", ConDescripcion = "Segundo", ConAutomatizacion = "MANUAL" }
            ],
            Mitigation = new MitigacionBloque4Dto
            {
                CantidadAcciones = 2,
                Planes =
                [
                    new PlanMitigacionDto { PlaId = 20, PlaEvaluacionId = 123, PlaDescripcion = "P1", PlaFechaInicio = new(2026,1,1), PlaFechaFin = new(2026,2,1), Actividades = [new() { ActId = 90, ActPlanId = 20, ActDescripcion = "A1" }, new() { ActId = 91, ActPlanId = 20, ActDescripcion = "A2" }] },
                    new PlanMitigacionDto { PlaId = 21, PlaEvaluacionId = 123, PlaDescripcion = "P2", PlaFechaInicio = new(2026,3,1), PlaFechaFin = new(2026,4,1), Actividades = [new() { ActId = 92, ActPlanId = 21, ActDescripcion = "A3" }] }
                ]
            }
        };

        InstitutionalEvaluationProjectionDto result = mapper.Map(source);
        Assert.Contains("1. Primero", Assert.IsType<string>(result.Fields[19].Value));
        Assert.Contains("2. Segundo", Assert.IsType<string>(result.Fields[19].Value));
        Assert.Equal(2, result.Fields[40].Value);
        Assert.Equal(3, result.Fields[42].Value);
        InstitutionalPlanProjectionValueDto[] plans = Assert.IsType<InstitutionalPlanProjectionValueDto[]>(result.Fields[39].Value);
        Assert.Equal(new[] { 1, 2 }, plans.Select(plan => plan.PlanOrdinalIndex));
        Assert.Equal(new object?[] { "P1", "P2" }, plans.Select(plan => plan.Value));
        Assert.Contains("2.1 A3", Assert.IsType<string>(result.Fields[41].Value));
    }

    [Fact]
    public void Map_AssignsDynamicOrdinalAfterLegacyMapAndIncludesInactiveRiskIds()
    {
        InstitutionalProjectionMapper mapper = new();
        InstitutionalProjectionSource source = CreateSource() with
        {
            Risk = new RiesgoDto { RieId = 900, RieCodigo = "UNMAPPED-RISK", RieNombre = "Risk", RieDescripcion = "Description" },
            AllRisks = [
                new RiesgoDto { RieId = 900, RieCodigo = "UNMAPPED-RISK", RieNombre = "Risk", RieActivo = false },
                new RiesgoDto { RieId = 901, RieCodigo = "ANOTHER-NEW", RieNombre = "Risk", RieActivo = true }
            ]
        };

        InstitutionalEvaluationProjectionDto result = mapper.Map(source);
        Assert.Equal(60, result.Fields[0].Value);
        InstitutionalProjectionSource later = source with { Risk = source.AllRisks![1] };
        Assert.Equal(61, mapper.Map(later).Fields[0].Value);
    }

    [Fact]
    public void Map_PreservesBlankPlanPositionsAcrossEveryPlanScopedField()
    {
        InstitutionalProjectionSource source = CreateSource() with
        {
            Mitigation = new MitigacionBloque4Dto { Planes = [
                new PlanMitigacionDto { PlaId=10, PlaEvaluacionId=123, PlaDescripcion="A", PlaFechaInicio=new(2026,1,1), PlaFechaFin=new(2026,2,1), PlaResponsables="Equipo A", PlaMonitoreoSeguimiento=null, PlaRecursos="R1", PlaPresupuesto=100 },
                new PlanMitigacionDto { PlaId=11, PlaEvaluacionId=123, PlaDescripcion="B", PlaFechaInicio=new(2026,3,1), PlaFechaFin=new(2026,4,1), PlaResponsables=null, PlaMonitoreoSeguimiento="Seguimiento B", PlaRecursos=null, PlaPresupuesto=200 }
            ] }
        };
        InstitutionalEvaluationProjectionDto result = new InstitutionalProjectionMapper().Map(source);
        foreach (int number in new[] { 40, 44, 45, 46, 47, 48, 49 })
        {
            InstitutionalPlanProjectionValueDto[]? values = result.Fields[number - 1].Value as InstitutionalPlanProjectionValueDto[];
            Assert.NotNull(values);
            Assert.Equal(new[] { 1, 2 }, values!.Select(value => value.PlanOrdinalIndex));
        }
        var responsible = Assert.IsType<InstitutionalPlanProjectionValueDto[]>(result.Fields[44].Value);
        Assert.Equal("Equipo A", responsible[0].Value);
        Assert.Null(responsible[1].Value);
        Assert.Equal("LEGIT_BLANK", responsible[1].State);
    }

    [Fact]
    public void Map_FailsClosedWhenRequiredTextIsWhitespace()
    {
        InstitutionalProjectionSource source = CreateSource();
        JsonObject answers = JsonNode.Parse(source.Evaluation.EvaDataJson)!.AsObject();
        answers["area_principal"] = "   ";
        source = source with
        {
            Evaluation = new EvaluacionRiesgoDto
            {
                EvaId = source.Evaluation.EvaId,
                EvaRiesgoId = source.Evaluation.EvaRiesgoId,
                EvaDataJson = answers.ToJsonString(),
                EvaDataCalcJson = source.Evaluation.EvaDataCalcJson
            }
        };

        InstitutionalProjectionException exception = Assert.Throws<InstitutionalProjectionException>(() => new InstitutionalProjectionMapper().Map(source));
        Assert.Contains("está vacío", exception.Message);
    }

    [Theory]
    [InlineData("Aceptar", 9, "NOT_APPLICABLE")]
    [InlineData("Evitar", 3, "NOT_APPLICABLE")]
    [InlineData("Evitar", 6, "OPERATIONAL_PENDING")]
    [InlineData("Mitigar", 3, "OPERATIONAL_PENDING")]
    public void Map_UsesContractualMitigationApplicability(string response, int residual, string expectedState)
    {
        InstitutionalProjectionSource source = CreateSource();
        source = source with
        {
            Evaluation = new EvaluacionRiesgoDto
            {
                EvaId = 123, EvaRiesgoId = 456, EvaVri = 3, EvaVrr = residual,
                EvaDataJson = source.Evaluation.EvaDataJson.Replace("\"respuesta_riesgo\":\"Aceptar\"", $"\"respuesta_riesgo\":\"{response}\"", StringComparison.Ordinal),
                EvaDataCalcJson = source.Evaluation.EvaDataCalcJson
            }
        };

        InstitutionalEvaluationProjectionDto result = new InstitutionalProjectionMapper().Map(source);

        Assert.Equal(expectedState, result.Fields[39].State);
    }

    private static InstitutionalProjectionSource CreateSource()
    {
        string answers = """
            {"area_principal":"Sección de Cumplimiento","area_consolidada":"Sección de Cumplimiento","tipo_riesgo":"Operativo","procedimiento":"PROC-1","objetivos_estrategicos":["OBJ-1"],"frecuencia_inherente":1,"impacto_inherente":3,"dueno_riesgo":"Responsable","regimen_afectado":"Régimen","transversalidad":"No","respuesta_riesgo":"Aceptar"}
            """;
        JsonObject calculations = new();
        foreach (int fieldNumber in new[] { 12, 13, 34, 35, 36, 37, 38, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, 64, 65, 66, 67, 68, 69 })
            calculations[FormulaKey(fieldNumber)] = fieldNumber is 13 or 34 or 38 ? "Riesgo bajo" : fieldNumber == 12 ? 3 : 1;
        foreach (int fieldNumber in new[] { 22, 23, 26, 27, 30, 31 })
            calculations[FormulaKey(fieldNumber)] = fieldNumber is 22 or 26 or 30 ? "Razonable" : 0.3m;

        return new(
            new EvaluacionRiesgoDto { EvaId = 123, EvaRiesgoId = 456, EvaDataJson = answers, EvaDataCalcJson = calculations.ToJsonString(), EvaVri = 3, EvaVrr = 1 },
            new RiesgoDto { RieId = 456, RieCodigo = "ROP-CUMP-59", RieNombre = "Riesgo inherente institucional", RieDescripcion = "Evaluación narrativa institucional" },
            Array.Empty<ControlRiesgoDto>(),
            new MitigacionBloque4Dto(),
            new MatrizBloque6Dto());
    }

    private static string FormulaKey(int fieldNumber)
    {
        string manifestPath = Path.Combine(FindRepositoryRoot(), "backend", "RL.API", "Features", "MatricesRiesgos", "Contracts", "matriz_riesgos_82_campos_manifest.json");
        using JsonDocument manifest = JsonDocument.Parse(File.ReadAllText(manifestPath));
        JsonElement field = manifest.RootElement.EnumerateArray().Single(item => item.GetProperty("number").GetInt32() == fieldNumber);
        string mapping = field.GetProperty("dbOrEntityMapping").GetString()!;
        return mapping[(mapping.LastIndexOf("->", StringComparison.Ordinal) + 2)..].Trim();
    }

    private static EvaluacionRiesgoDto WithCalculation(EvaluacionRiesgoDto source, int fieldNumber, decimal value)
    {
        JsonObject calculations = JsonNode.Parse(source.EvaDataCalcJson)!.AsObject();
        calculations[FormulaKey(fieldNumber)] = value;
        return new EvaluacionRiesgoDto
        {
            EvaId = source.EvaId,
            EvaRiesgoId = source.EvaRiesgoId,
            EvaVersionId = source.EvaVersionId,
            EvaEstado = source.EvaEstado,
            EvaDataJson = source.EvaDataJson,
            EvaDataCalcJson = calculations.ToJsonString(),
            EvaVri = source.EvaVri,
            EvaVrr = source.EvaVrr
        };
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "AGENTS.md"))) directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root was not found.");
    }
}
