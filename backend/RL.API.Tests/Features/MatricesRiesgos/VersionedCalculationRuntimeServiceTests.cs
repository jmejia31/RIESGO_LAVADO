using System.Text.Json;
using RL.API.Features.MatricesRiesgos.Application;
using RL.API.Features.MatricesRiesgos.Contracts;
using RL.API.Features.MatricesRiesgos.Domain;
using RL.API.Features.MatricesRiesgos.Persistence;
using RL.API.Tests.Support;
using Xunit;
using CatalogElement = RL.API.Features.Catalogos.Contracts.ElementoCatalogoMatricesDto;

namespace RL.API.Tests.Features.MatricesRiesgos;

public sealed class VersionedCalculationRuntimeServiceTests
{
    [Fact]
    public async Task Calculate_UsesFormulaUsageAndCatalogSnapshotFromRequestedVersion()
    {
        CatalogSnapshot catalog = new("CAT_VERSION_A", true,
        [new CatalogElement(1, "SCALE_A", "0.3", 1, true)]);
        ICalculoConfiguracionRepository repository = InterfaceStub.Create<ICalculoConfiguracionRepository>(out InterfaceStub stub);
        stub.On(nameof(ICalculoConfiguracionRepository.ListarFormulaBindingsPorVersionFormularioAsync), _ =>
            Task.FromResult<IReadOnlyList<FormulaBindingDto>>(
            [new FormulaBindingDto
            {
                VersionFormularioId = 63, CampoClave = "resultado", FormulaVersionId = 10,
                FormulaCodigo = "F_VERSION_A", FormulaVersion = 1,
                Expresion = "LOOKUP(\"CAT_VERSION_A\", escala, \"NUMBER\")", TipoResultado = "DECIMAL",
                EstadoVersion = "PUBLISHED", EstadoFormula = "ACTIVE", Hash = Hash()
            }]));
        stub.On(nameof(ICalculoConfiguracionRepository.ObtenerSnapshotRuntimeAsync), _ => Task.FromResult(SnapshotWithLookup()));

        var service = new VersionedCalculationRuntimeService(repository, new DbDrivenCalculationRuntimeFactory(repository));
        string definition = JsonSerializer.Serialize(new
        {
            secciones = new[] { new { campos = new[] { new { clave = "escala" }, new { clave = "resultado" } } } },
            catalogos = new[]
            {
                new { codigo = catalog.Code, activo = catalog.Active, elementos = catalog.Elements.Select(element => new { codigo = element.Codigo, valor = element.Valor, orden = element.Orden, activo = element.Activo }) }
            },
            runtimeCalculo = new
            {
                funciones = new Dictionary<string, int> { ["LOOKUP"] = 1 },
                parametros = new Dictionary<string, int>(),
                catalogos = new Dictionary<string, string> { [catalog.Code] = CatalogSnapshotHasher.Compute(catalog) }
            }
        });

        GovernedCalculationResult result = await service.CalculateAsync(63, definition, "{\"escala\":\"SCALE_A\"}");

        Assert.True(result.IsGoverned);
        Assert.True(result.Evaluation!.Success, string.Join("; ", result.Evaluation.Errors.Select(error => error.Message)));
        Assert.Equal(0.3d, result.Evaluation.Values["resultado"]);
        Assert.Empty(await service.ValidateForPublicationAsync(63, definition));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CalculateAsync(63, definition,
            "{\"escala\":\"SCALE_A\",\"resultado\":999}"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CalculateAsync(63, definition,
            "{\"escala\":\"SCALE_A\",\"control_preventivo\":\"spoof\"}"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CalculateAsync(64, definition,
            "{\"escala\":\"SCALE_A\"}"));
        string tamperedDefinition = definition.Replace(CatalogSnapshotHasher.Compute(catalog), new string('0', 64), StringComparison.Ordinal);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CalculateAsync(63, tamperedDefinition,
            "{\"escala\":\"SCALE_A\"}"));
        Assert.Contains(stub.CallsTo(nameof(ICalculoConfiguracionRepository.ListarFormulaBindingsPorVersionFormularioAsync)),
            call => (long)call.Arguments[0]! == 63);
    }

    [Fact]
    public async Task Calculate_FailsClosedWhenGovernedVersionHasNoRuntimePins()
    {
        ICalculoConfiguracionRepository repository = InterfaceStub.Create<ICalculoConfiguracionRepository>(out InterfaceStub stub);
        stub.On(nameof(ICalculoConfiguracionRepository.ListarFormulaBindingsPorVersionFormularioAsync), _ =>
            Task.FromResult<IReadOnlyList<FormulaBindingDto>>(
            [new FormulaBindingDto
            {
                VersionFormularioId = 63, CampoClave = "resultado", FormulaVersionId = 10,
                FormulaCodigo = "F_VERSION_A", FormulaVersion = 1, Expresion = "1+1", TipoResultado = "DECIMAL",
                EstadoVersion = "PUBLISHED", EstadoFormula = "ACTIVE", Hash = Hash()
            }]));

        var service = new VersionedCalculationRuntimeService(repository, new DbDrivenCalculationRuntimeFactory(repository));

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CalculateAsync(63,
            """{"secciones":[{"campos":[{"clave":"resultado"}]}]}""", "{}"));
    }

    [Fact]
    public void GovernedControlRecalculation_PreservesExistingCalculationMetadata()
    {
        string merged = MatricesRiesgosMitigacionService.MergeCalculatedJson(
            """{"reglaCodigo":"BASE","reglaVersion":"1","valor_anterior":4}""",
            new Dictionary<string, object?> { ["efectividad_total_ponderada"] = 0.9d });
        using JsonDocument result = JsonDocument.Parse(merged);

        Assert.Equal("BASE", result.RootElement.GetProperty("reglaCodigo").GetString());
        Assert.Equal("1", result.RootElement.GetProperty("reglaVersion").GetString());
        Assert.Equal(4, result.RootElement.GetProperty("valor_anterior").GetInt32());
        Assert.Equal(0.9, result.RootElement.GetProperty("efectividad_total_ponderada").GetDouble());
    }

    [Fact]
    public void GovernedControlRepository_UsesOneOptimisticDraftTransactionForMutationAndCalculation()
    {
        string repository = ReadRepositorySource("MatricesRiesgosMitigacionRepository.cs");
        string create = MethodBody(repository, "public async Task<long> CrearControlGobernadoAtomicoAsync", "public async Task<bool> ActualizarControlGobernadoAtomicoAsync");
        string update = MethodBody(repository, "public async Task<bool> ActualizarControlGobernadoAtomicoAsync", "private static async Task LockGovernedDraftEvaluationAsync");

        foreach (string method in new[] { create, update })
        {
            Assert.Contains("BeginTransaction()", method, StringComparison.Ordinal);
            Assert.Contains("LockGovernedDraftEvaluationAsync", method, StringComparison.Ordinal);
            Assert.Contains("UpdateGovernedCalculationAsync", method, StringComparison.Ordinal);
            Assert.Contains("await transaction.CommitAsync()", method, StringComparison.Ordinal);
            Assert.Contains("await transaction.RollbackAsync()", method, StringComparison.Ordinal);
            Assert.Contains("await AuditarAsync", method, StringComparison.Ordinal);
        }
        Assert.Contains("EVA_VERSION_ROW=EVA_VERSION_ROW+1", repository, StringComparison.Ordinal);
        Assert.Contains("expectedVersionRow", repository, StringComparison.Ordinal);
        Assert.Contains("RL_MR_FLUJOS_EVALUACION", repository, StringComparison.Ordinal);
        Assert.Contains("BORRADOR", repository, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Inexistente", 0d, 0d)]
    [InlineData("Inefectivo", 1d, 0d)]
    [InlineData("Razonable", 2d, 0.3d)]
    [InlineData("Parcialmente Efectivo", 3d, 0.5d)]
    [InlineData("Moderado", 4d, 0.85d)]
    [InlineData("Alta Efectividad", 5d, 0.9d)]
    public async Task Calculate_F03ThroughF09UsesEveryPinnedInstitutionalScale(string scale, double expectedLevel, double expectedPercentage)
    {
        CatalogSnapshot levels = EffectivenessCatalog("CAT_EFECTIVIDAD_NIVEL", includeNumericValues: true);
        CatalogSnapshot percentages = EffectivenessCatalog("CAT_EFECTIVIDAD_PORCENTAJE", includeNumericValues: false);
        CatalogSnapshot labels = EffectivenessCatalog("CAT_EFECTIVIDAD_ESCALA", includeNumericValues: null);
        CatalogSnapshot[] catalogs = [labels, levels, percentages];
        ICalculoConfiguracionRepository repository = InterfaceStub.Create<ICalculoConfiguracionRepository>(out InterfaceStub stub);
        stub.On(nameof(ICalculoConfiguracionRepository.ListarFormulaBindingsPorVersionFormularioAsync), _ =>
            Task.FromResult<IReadOnlyList<FormulaBindingDto>>(EffectivenessBindings()));
        stub.On(nameof(ICalculoConfiguracionRepository.ObtenerSnapshotRuntimeAsync), _ => Task.FromResult(FullEffectivenessSnapshot()));
        var service = new VersionedCalculationRuntimeService(repository, new DbDrivenCalculationRuntimeFactory(repository));
        string definition = EffectivenessDefinition(catalogs);
        string answers = JsonSerializer.Serialize(new Dictionary<string, string>
        {
            ["escala_preventivo"] = scale, ["escala_detectivo"] = scale, ["escala_correctivo"] = scale
        });
        IReadOnlyDictionary<string, bool> presence = new Dictionary<string, bool>
        {
            [InstitutionalCalculationContextKeys.PreventiveControl] = true,
            [InstitutionalCalculationContextKeys.DetectiveControl] = true,
            [InstitutionalCalculationContextKeys.CorrectiveControl] = true
        };

        GovernedCalculationResult result = await service.CalculateAsync(63, definition, answers, presence);

        Assert.True(result.IsGoverned);
        Assert.True(result.Evaluation!.Success, string.Join("; ", result.Evaluation.Errors.Select(error => error.Message)));
        Assert.Equal(expectedLevel, result.Evaluation.Values["nivel_control_preventivo"]);
        Assert.Equal(expectedLevel, result.Evaluation.Values["nivel_control_detectivo"]);
        Assert.Equal(expectedLevel, result.Evaluation.Values["nivel_control_correctivo"]);
        Assert.Equal(expectedPercentage, Convert.ToDouble(result.Evaluation.Values["porcentaje_control_preventivo"]), 10);
        Assert.Equal(expectedPercentage, Convert.ToDouble(result.Evaluation.Values["porcentaje_control_detectivo"]), 10);
        Assert.Equal(expectedPercentage, Convert.ToDouble(result.Evaluation.Values["porcentaje_control_correctivo"]), 10);
        Assert.Equal(expectedPercentage, Convert.ToDouble(result.Evaluation.Values["efectividad_total_ponderada"]), 10);

        GovernedCalculationResult mixed = await service.CalculateAsync(63, definition,
            """{"escala_preventivo":"Moderado","escala_detectivo":"Razonable","escala_correctivo":"Parcialmente Efectivo"}""", presence);
        Assert.True(mixed.Evaluation!.Success, string.Join("; ", mixed.Evaluation.Errors.Select(error => error.Message)));
        Assert.Equal(0.715d, Convert.ToDouble(mixed.Evaluation.Values["efectividad_total_ponderada"]), 10);

        IReadOnlyDictionary<string, bool> noControls = new Dictionary<string, bool>
        {
            [InstitutionalCalculationContextKeys.PreventiveControl] = false,
            [InstitutionalCalculationContextKeys.DetectiveControl] = false,
            [InstitutionalCalculationContextKeys.CorrectiveControl] = false
        };
        GovernedCalculationResult empty = await service.CalculateAsync(63, definition,
            """{"escala_preventivo":"","escala_detectivo":"","escala_correctivo":""}""", noControls);
        Assert.True(empty.Evaluation!.Success, string.Join("; ", empty.Evaluation.Errors.Select(error => error.Message)));
        Assert.Equal(string.Empty, empty.Evaluation.Values["efectividad_total_ponderada"]);
    }

    private static CalculationConfigurationSnapshotDto SnapshotWithLookup()
    {
        FunctionVersionDefinition lookup = NativeFunctionCatalog.CreateDefaultDefinitions().Single(function => function.Code == "LOOKUP");
        return new CalculationConfigurationSnapshotDto
        {
            Funciones = [new FuncionDto { Id = 1, Codigo = "LOOKUP", Estado = "ACTIVE" }],
            VersionesFuncion = [new FuncionVersionDto
            {
                Id = 11, FuncionId = 1, Version = 1, Tipo = lookup.Type, TipoResultado = lookup.ResultType,
                HandlerKey = lookup.HandlerKey, MinArity = lookup.MinArity, MaxArity = lookup.MaxArity, Estado = "PUBLISHED", Hash = Hash()
            }],
            ArgumentosFuncion = lookup.Arguments.Select(argument => new FuncionArgumentoDto
            {
                Id = argument.Position, FuncionVersionId = 11, Posicion = argument.Position, Codigo = argument.Code,
                Nombre = argument.Code, Tipo = argument.Type, Requerido = argument.Required, Variadic = argument.Variadic,
                ValorDefaultJson = argument.DefaultJson
            }).ToArray()
        };
    }

    private static IReadOnlyList<FormulaBindingDto> EffectivenessBindings() => InstitutionalFormulaDataset.All
        .Where(formula => formula.Number is >= 3 and <= 9)
        .Select((formula, index) => new FormulaBindingDto
        {
            VersionFormularioId = 63, CampoClave = formula.TargetField, FormulaVersionId = 100 + index,
            FormulaCodigo = formula.Code, FormulaVersion = 1, Expresion = formula.SemanticExpression,
            TipoResultado = formula.ResultType, EstadoVersion = "PUBLISHED", EstadoFormula = "ACTIVE", Hash = Hash()
        }).ToArray();

    private static string EffectivenessDefinition(IReadOnlyList<CatalogSnapshot> catalogs)
    {
        var catalogCodes = catalogs.ToDictionary(catalog => catalog.Code, CatalogSnapshotHasher.Compute, StringComparer.OrdinalIgnoreCase);
        return JsonSerializer.Serialize(new
        {
            secciones = new[]
            {
                new { campos = new[]
                {
                    new { clave = "escala_preventivo" }, new { clave = "escala_detectivo" }, new { clave = "escala_correctivo" },
                    new { clave = "nivel_control_preventivo" }, new { clave = "porcentaje_control_preventivo" },
                    new { clave = "nivel_control_detectivo" }, new { clave = "porcentaje_control_detectivo" },
                    new { clave = "nivel_control_correctivo" }, new { clave = "porcentaje_control_correctivo" },
                    new { clave = "efectividad_total_ponderada" }
                } }
            },
            catalogos = catalogs.Select(catalog => new
            {
                codigo = catalog.Code, activo = catalog.Active,
                elementos = catalog.Elements.Select(element => new { codigo = element.Codigo, valor = element.Valor, orden = element.Orden, activo = element.Activo })
            }),
            runtimeCalculo = new
            {
                funciones = NativeFunctionCatalog.FunctionCodes.ToDictionary(code => code, _ => 1),
                parametros = new Dictionary<string, int>
                {
                    ["PESO_PREVENTIVO"] = 1, ["PESO_DETECTIVO"] = 1, ["PESO_CORRECTIVO"] = 1
                },
                catalogos = catalogCodes
            }
        });
    }

    private static CatalogSnapshot EffectivenessCatalog(string code, bool? includeNumericValues)
    {
        string[] scales = ["Inexistente", "Inefectivo", "Razonable", "Parcialmente Efectivo", "Moderado", "Alta Efectividad"];
        string[] levels = ["0", "1", "2", "3", "4", "5"];
        string[] percentages = ["0", "0", "0.3", "0.5", "0.85", "0.9"];
        var elements = scales.Select((scale, index) => new CatalogElement(index + 1, scale,
            includeNumericValues is null ? scale : includeNumericValues.Value ? levels[index] : percentages[index], index + 1, true)).ToArray();
        return new CatalogSnapshot(code, true, elements);
    }

    private static CalculationConfigurationSnapshotDto FullEffectivenessSnapshot()
    {
        FunctionVersionDefinition[] definitions = NativeFunctionCatalog.CreateDefaultDefinitions().ToArray();
        var functionIds = definitions.Select((definition, index) => (definition.Code, Id: (long)index + 1)).ToDictionary(value => value.Code, value => value.Id, StringComparer.OrdinalIgnoreCase);
        var functionVersionIds = definitions.Select((definition, index) => (definition.Code, Id: (long)index + 101)).ToDictionary(value => value.Code, value => value.Id, StringComparer.OrdinalIgnoreCase);
        return new CalculationConfigurationSnapshotDto
        {
            Funciones = definitions.Select(definition => new FuncionDto { Id = functionIds[definition.Code], Codigo = definition.Code, Estado = "ACTIVE" }).ToArray(),
            VersionesFuncion = definitions.Select(definition => new FuncionVersionDto
            {
                Id = functionVersionIds[definition.Code], FuncionId = functionIds[definition.Code], Version = 1, Tipo = definition.Type,
                TipoResultado = definition.ResultType, HandlerKey = definition.HandlerKey, DefinicionDsl = definition.DefinitionDsl,
                MinArity = definition.MinArity, MaxArity = definition.MaxArity, Estado = "PUBLISHED", Hash = Hash()
            }).ToArray(),
            ArgumentosFuncion = definitions.SelectMany(definition => definition.Arguments.Select(argument => new FuncionArgumentoDto
            {
                Id = functionVersionIds[definition.Code] * 100 + argument.Position, FuncionVersionId = functionVersionIds[definition.Code],
                Posicion = argument.Position, Codigo = argument.Code, Nombre = argument.Code, Tipo = argument.Type,
                Requerido = argument.Required, Variadic = argument.Variadic, ValorDefaultJson = argument.DefaultJson
            })).ToArray(),
            Parametros = new[]
            {
                new ParametroDto { Id = 1, Codigo = "PESO_PREVENTIVO", Estado = "ACTIVE" },
                new ParametroDto { Id = 2, Codigo = "PESO_DETECTIVO", Estado = "ACTIVE" },
                new ParametroDto { Id = 3, Codigo = "PESO_CORRECTIVO", Estado = "ACTIVE" }
            },
            VersionesParametro = new[]
            {
                new ParametroVersionDto { Id = 11, ParametroId = 1, Version = 1, Tipo = "DECIMAL", ValorDecimal = 0.70m, Estado = "PUBLISHED", Hash = Hash() },
                new ParametroVersionDto { Id = 12, ParametroId = 2, Version = 1, Tipo = "DECIMAL", ValorDecimal = 0.15m, Estado = "PUBLISHED", Hash = Hash() },
                new ParametroVersionDto { Id = 13, ParametroId = 3, Version = 1, Tipo = "DECIMAL", ValorDecimal = 0.15m, Estado = "PUBLISHED", Hash = Hash() },
                new ParametroVersionDto { Id = 14, ParametroId = 1, Version = 2, Tipo = "DECIMAL", ValorDecimal = 0.99m, Estado = "PUBLISHED", Hash = Hash() }
            }
        };
    }

    private static string Hash() => new('A', 64);

    private static string ReadRepositorySource(string fileName)
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "RIESGO_LAVADO.sln"))) directory = directory.Parent;
        Assert.NotNull(directory);
        return File.ReadAllText(Path.Combine(directory!.FullName, "backend", "RL.API", "Features", "MatricesRiesgos", "Persistence", fileName));
    }

    private static string MethodBody(string source, string methodName, string nextMethodName)
    {
        int start = source.IndexOf(methodName, StringComparison.Ordinal);
        int end = source.IndexOf(nextMethodName, start + methodName.Length, StringComparison.Ordinal);
        Assert.True(start >= 0 && end > start, $"No se encontró el límite esperado de {methodName}.");
        return source[start..end];
    }
}
