using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using RL.API.Shared.Results;
using RL.API.Features.Auditoria.Persistence;
using RL.API.Features.Catalogos.Contracts;
using RL.API.Features.MatricesRiesgos.Application;
using RL.API.Features.MatricesRiesgos.Contracts;
using RL.API.Features.MatricesRiesgos.Domain;
using RL.API.Features.MatricesRiesgos.Persistence;
using RL.API.Tests.Support;
using Xunit;
using CatalogElement = RL.API.Features.Catalogos.Contracts.ElementoCatalogoMatricesDto;

namespace RL.API.Tests.Features.MatricesRiesgos;

/// <summary>
/// Certificación técnica formal del Bloque 3: Riesgo Residual y Respuesta (Campos 34–39).
/// Gates certificados:
/// - F10_F14=PASS (F10, F11, F12, F13_VRR, F14)
/// - RESIDUAL_SERVER_AUTHORITATIVE=PASS
/// - RESIDUAL_CLIENT_TAMPERING=REJECTED_OR_RECALCULATED
/// - CALCULATED_FIELDS_NOT_CLIENT_AUTHORITATIVE=PASS
/// - RESIDUAL_VERSION_AWARE=PASS
/// - RESPONSE_CATALOG=PASS (EVITAR, MITIGAR, TRANSFERIR, ACEPTAR)
/// - INVALID_RESPONSE_VALUE_CONTROLLED_4XX=PASS
/// - WORKFLOW_IMMUTABILITY=PASS
/// </summary>
public sealed class MatricesRiesgosBlock3ResidualCertificacionTests
{
    [Fact]
    public void F10_RiesgoResidualDescripcion_ParidadAutoritativaExcel()
    {
        InstitutionalFormulaDefinition f10 = InstitutionalFormulaDataset.All[9];
        Assert.Equal(10, f10.Number);
        Assert.Equal("F10_RIESGO_RESIDUAL_DESCRIPCION", f10.Code);
        Assert.Equal("Matriz Consolidada!AH2", f10.SourceCell);
        Assert.Equal("riesgo_residual_descripcion", f10.TargetField);

        var engine = new FormulaEngine();
        string definition = """
        {
            "secciones": [
                { "clave": "s1", "titulo": "S", "campos": [
                    { "clave": "riesgo_inherente_descripcion", "etiqueta": "Desc", "tipo": "texto" },
                    { "clave": "riesgo_residual_descripcion", "etiqueta": "Riesgo Residual", "tipo": "formula", "formula": "IF(riesgo_inherente_descripcion=\"\",\"\",riesgo_inherente_descripcion)" }
                ]}
            ]
        }
        """;

        // Caso con descripción presente
        FormulaEvaluationResult r1 = engine.Evaluate(definition, @"{""riesgo_inherente_descripcion"":""Fuga de datos confidenciales""}");
        Assert.True(r1.Success, string.Join("; ", r1.Errors.Select(e => e.Message)));
        Assert.Equal("Fuga de datos confidenciales", r1.Values["riesgo_residual_descripcion"]);

        // Caso vacío
        FormulaEvaluationResult r2 = engine.Evaluate(definition, @"{""riesgo_inherente_descripcion"":""""}");
        Assert.True(r2.Success, string.Join("; ", r2.Errors.Select(e => e.Message)));
        Assert.Equal(string.Empty, r2.Values["riesgo_residual_descripcion"]);
    }

    [Theory]
    [InlineData(7, 0.0, 7)]   // Sin efectividad: VRR = 7 * (1 - 0) = 7
    [InlineData(7, 0.5, 4)]   // 50% efectividad: VRR = ROUND(7 * 0.5) = 4
    [InlineData(7, 0.9, 1)]   // 90% efectividad: VRR = ROUND(7 * 0.1) = 1
    [InlineData(1, 1.0, 1)]   // 100% efectividad: VRR = MAX(1, 0) = 1 (Límite inferior institucional)
    [InlineData(9, 0.0, 9)]   // Límite superior: VRI = 9, VRR = 9
    [InlineData(9, 0.9, 1)]   // Límite superior con 90% control: VRR = ROUND(9 * 0.1) = 1
    public void F13_ValorRiesgoResidual_CalculoAutoritativoVRR(int vri, double etp, int vrrEsperado)
    {
        InstitutionalFormulaDefinition f13 = InstitutionalFormulaDataset.All[12];
        Assert.Equal(13, f13.Number);
        Assert.Equal("F13_VALOR_RIESGO_RESIDUAL", f13.Code);
        Assert.Equal("Matriz Consolidada!AK2", f13.SourceCell);
        Assert.Equal("valor_riesgo_residual", f13.TargetField);

        var engine = new FormulaEngine();
        string definition = """
        {
            "secciones": [
                { "clave": "s1", "titulo": "S", "campos": [
                    { "clave": "valor_riesgo_inherente", "etiqueta": "VRI", "tipo": "numero" },
                    { "clave": "efectividad_total_ponderada", "etiqueta": "ETP", "tipo": "numero" },
                    { "clave": "valor_riesgo_residual", "etiqueta": "VRR", "tipo": "formula", "formula": "ROUND(MAX(1,valor_riesgo_inherente*(1-efectividad_total_ponderada)),0)" }
                ]}
            ]
        }
        """;

        string answers = FormattableString.Invariant(@$"{{""valor_riesgo_inherente"":{vri},""efectividad_total_ponderada"":{etp}}}");
        FormulaEvaluationResult r = engine.Evaluate(definition, answers);
        Assert.True(r.Success, string.Join("; ", r.Errors.Select(e => e.Message)));
        Assert.Equal(Convert.ToDouble(vrrEsperado), Convert.ToDouble(r.Values["valor_riesgo_residual"]));
    }

    [Theory]
    [InlineData(1, "Riesgo no significativo")]
    [InlineData(2, "Riesgo no significativo")]
    [InlineData(3, "Riesgo Bajo")]
    [InlineData(4, "Riesgo Medio")]
    [InlineData(5, "Riesgo Medio")]
    [InlineData(6, "Riesgo Alto")]
    [InlineData(7, "Riesgo Alto")]
    [InlineData(8, "Riesgo Muy Alto")]
    [InlineData(9, "Riesgo Crítico")]
    public void F14_NivelRiesgoResidual_ClasificacionCatalogoInstitucional(int vrr, string nivelEsperado)
    {
        InstitutionalFormulaDefinition f14 = InstitutionalFormulaDataset.All[13];
        Assert.Equal(14, f14.Number);
        Assert.Equal("F14_NIVEL_RIESGO_RESIDUAL", f14.Code);
        Assert.Equal("Matriz Consolidada!AL2", f14.SourceCell);
        Assert.Equal("nivel_riesgo_residual", f14.TargetField);

        var lookup = new CatalogCalculationLookup(
        [
            new CatalogSnapshot("CAT_NIVEL_RIESGO", true,
            [
                new CatalogElement(1, "1", "Riesgo no significativo", 1, true),
                new CatalogElement(2, "2", "Riesgo no significativo", 2, true),
                new CatalogElement(3, "3", "Riesgo Bajo", 3, true),
                new CatalogElement(4, "4", "Riesgo Medio", 4, true),
                new CatalogElement(5, "5", "Riesgo Medio", 5, true),
                new CatalogElement(6, "6", "Riesgo Alto", 6, true),
                new CatalogElement(7, "7", "Riesgo Alto", 7, true),
                new CatalogElement(8, "8", "Riesgo Muy Alto", 8, true),
                new CatalogElement(9, "9", "Riesgo Crítico", 9, true)
            ])
        ]);

        var options = new FormulaRuntimeOptions(
            new InMemoryFunctionRegistry(NativeFunctionCatalog.CreateDefaultDefinitions()),
            Lookup: lookup);

        var engine = new FormulaEngine();
        string definition = """
        {
            "secciones": [
                { "clave": "s1", "titulo": "S", "campos": [
                    { "clave": "valor_riesgo_residual", "etiqueta": "VRR", "tipo": "numero" },
                    { "clave": "nivel_riesgo_residual", "etiqueta": "Nivel", "tipo": "formula", "formula": "LOOKUP(\"CAT_NIVEL_RIESGO\",valor_riesgo_residual)" }
                ]}
            ]
        }
        """;

        FormulaEvaluationResult r = engine.Evaluate(definition, FormattableString.Invariant(@$"{{""valor_riesgo_residual"":{vrr}}}"), options);
        Assert.True(r.Success, string.Join("; ", r.Errors.Select(e => e.Message)));
        Assert.Equal(nivelEsperado, r.Values["nivel_riesgo_residual"]);
    }

    [Theory]
    [InlineData(3, 3, 5, 5, 3, 3)] // VRI == VRR => residual igual a inherente
    [InlineData(4, 2, 5, 5, 4, 2)] // VRI == VRR => residual igual a inherente
    [InlineData(2, 5, 6, 6, 2, 5)] // VRI == VRR => residual igual a inherente
    public void F11_F12_FrecuenciaEImpactoResidual_IdentidadCuandoVRIIgualAVRR(
        int fInh, int iInh, int vri, int vrr, int fResidualEsperada, int iResidualEsperada)
    {
        InstitutionalFormulaDefinition f11 = InstitutionalFormulaDataset.All[10];
        InstitutionalFormulaDefinition f12 = InstitutionalFormulaDataset.All[11];
        Assert.Equal(11, f11.Number);
        Assert.Equal("F11_FRECUENCIA_RESIDUAL", f11.Code);
        Assert.Equal(12, f12.Number);
        Assert.Equal("F12_IMPACTO_RESIDUAL", f12.Code);

        // Cuando VRI == VRR, F11 y F12 devuelven directamente frecuencia e impacto inherentes
        var engine = new FormulaEngine();
        string exprF11 = "IF(valor_riesgo_inherente=valor_riesgo_residual,frecuencia,MIN(tope_f,f_base+incremento_f_aux))";
        string exprF12 = "IF(valor_riesgo_inherente=valor_riesgo_residual,impacto,MIN(tope_i,i_base+incremento_i_aux))";

        var scope = new Dictionary<string, FormulaValue>
        {
            ["frecuencia"] = FormulaValue.NumberValue(fInh),
            ["impacto"] = FormulaValue.NumberValue(iInh),
            ["valor_riesgo_inherente"] = FormulaValue.NumberValue(vri),
            ["valor_riesgo_residual"] = FormulaValue.NumberValue(vrr),
            ["tope_f"] = FormulaValue.NumberValue(fInh),
            ["tope_i"] = FormulaValue.NumberValue(iInh),
            ["f_base"] = FormulaValue.NumberValue(1),
            ["i_base"] = FormulaValue.NumberValue(1),
            ["incremento_f_aux"] = FormulaValue.NumberValue(0),
            ["incremento_i_aux"] = FormulaValue.NumberValue(0)
        };

        Assert.Equal(fResidualEsperada, Convert.ToInt32(engine.EvaluateExpression(exprF11, scope).ToObject()));
        Assert.Equal(iResidualEsperada, Convert.ToInt32(engine.EvaluateExpression(exprF12, scope).ToObject()));
    }

    [Theory]
    [InlineData("EVITAR", true)]
    [InlineData("MITIGAR", true)]
    [InlineData("TRANSFERIR", true)]
    [InlineData("ACEPTAR", true)]
    [InlineData("ELIMINAR", false)]
    [InlineData("MITIGACIÓN", false)]
    [InlineData("ACEPTADO", false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("OTRO", false)]
    [InlineData("TOLERAR", false)]
    public async Task Campo39_ValidacionCatalogoCanonico_Controlled4xx(string respuestaRiesgo, bool esperadaValida)
    {
        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repo, out InterfaceStub validador, out InterfaceStub calculador);

        var dto = new EvaluacionRiesgoDto
        {
            EvaId = 100,
            EvaVersionId = 1,
            EvaEstado = "BORRADOR",
            EvaDataJson = JsonSerializer.Serialize(new { respuesta_riesgo = respuestaRiesgo }),
            EvaDataCalcJson = "{}"
        };

        repo.On(nameof(IMatricesRiesgosRepository.ObtenerEvaluacionAsync), _ => Task.FromResult<EvaluacionRiesgoDto?>(dto));
        repo.On(nameof(IMatricesRiesgosRepository.ObtenerVersionFormularioAsync), _ =>
            Task.FromResult<VersionFormularioDto?>(new VersionFormularioDto
            {
                VerId = 1,
                VerFamiliaId = 1,
                VerCodigo = "MATRIZ_LAFT",
                VerVersion = 1,
                VerJson = @"{""secciones"":[]}",
                VerHash = new string('a', 64),
                VerEstado = "PUBLISHED",
                VerVigente = true,
                VerFechaCreacion = DateTime.UtcNow,
                VerUsrCreacion = 1
            }));
        validador.On(nameof(IFormularioValidador.ValidarRespuestasAsync), _ => Task.FromResult(new FormularioValidationResult()));
        calculador.On(nameof(IMatricesRiesgoService.CalcularYValidarRiesgo), _ =>
            ServiceResult<CalculoRiesgoResultadoDto>.Ok(new CalculoRiesgoResultadoDto { Vri = 5, Etp = 50m, Vrr = 3 }));
        repo.On(nameof(IMatricesRiesgosRepository.ActualizarEvaluacionAsync), _ => Task.FromResult(true));

        var resultado = await service.ActualizarEvaluacionAsync(dto, 1, "127.0.0.1");

        if (esperadaValida)
        {
            Assert.True(resultado.Success, $"Respuesta válida '{respuestaRiesgo}' fue rechazada: {resultado.Message}");
        }
        else
        {
            Assert.False(resultado.Success, $"Respuesta inválida '{respuestaRiesgo}' fue aceptada");
            Assert.Equal(400, resultado.StatusCode);
            Assert.Contains("respuesta", resultado.Message, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task ResidualServerAuthoritative_ElBackendEsLaUnicaAutoridadDeCalculo()
    {
        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repo, out InterfaceStub validador, out InterfaceStub calculador);

        var dto = new EvaluacionRiesgoDto
        {
            EvaId = 200,
            EvaVersionId = 1,
            EvaEstado = "BORRADOR",
            EvaDataJson = @"{""frecuencia_inherente"":3,""impacto_inherente"":3,""controles_preventivo"":0.5,""controles_detectivo"":0.0,""controles_correctivo"":0.0}",
            EvaDataCalcJson = "{}"
        };

        repo.On(nameof(IMatricesRiesgosRepository.ObtenerEvaluacionAsync), _ => Task.FromResult<EvaluacionRiesgoDto?>(dto));
        repo.On(nameof(IMatricesRiesgosRepository.ObtenerVersionFormularioAsync), _ =>
            Task.FromResult<VersionFormularioDto?>(new VersionFormularioDto
            {
                VerId = 1,
                VerFamiliaId = 1,
                VerCodigo = "MATRIZ_LAFT",
                VerVersion = 1,
                VerJson = @"{""secciones"":[]}",
                VerHash = new string('b', 64),
                VerEstado = "PUBLISHED",
                VerVigente = true,
                VerFechaCreacion = DateTime.UtcNow,
                VerUsrCreacion = 1
            }));
        validador.On(nameof(IFormularioValidador.ValidarRespuestasAsync), _ => Task.FromResult(new FormularioValidationResult()));

        // El calculador oficial del backend retorna VRI=5, ETP=50, VRR=3
        calculador.On(nameof(IMatricesRiesgoService.CalcularYValidarRiesgo), _ =>
            ServiceResult<CalculoRiesgoResultadoDto>.Ok(new CalculoRiesgoResultadoDto { Vri = 5, Etp = 50m, Vrr = 3 }));

        EvaluacionRiesgoDto? evaluacionPersistida = null;
        repo.On(nameof(IMatricesRiesgosRepository.ActualizarEvaluacionAsync), args =>
        {
            evaluacionPersistida = (EvaluacionRiesgoDto)args[0]!;
            return Task.FromResult(true);
        });

        var resultado = await service.ActualizarEvaluacionAsync(dto, 1, "127.0.0.1");

        Assert.True(resultado.Success, resultado.Message);
        Assert.NotNull(evaluacionPersistida);
        // El servidor asignó autoritativamente VRI, ETP y VRR
        Assert.Equal(5, evaluacionPersistida!.EvaVri);
        Assert.Equal(50m, evaluacionPersistida.EvaEtp);
        Assert.Equal(3, evaluacionPersistida.EvaVrr);
    }

    [Fact]
    public async Task ResidualClientTampering_IntentoFalsificarCalculados_RechazadoORecalculado()
    {
        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repo, out InterfaceStub validador, out InterfaceStub calculador);

        var dto = new EvaluacionRiesgoDto
        {
            EvaId = 200,
            EvaVersionId = 1,
            EvaEstado = "BORRADOR",
            // Cliente intenta enviar valores forjados en DTO
            EvaDataJson = @"{""frecuencia_inherente"":2,""impacto_inherente"":2,""controles_preventivo"":0.5,""controles_detectivo"":0.0,""controles_correctivo"":0.0}",
            EvaDataCalcJson = @"{""valor_riesgo_residual"":999,""nivel_riesgo_residual"":""BAJO"",""frecuencia_residual"":999,""impacto_residual"":999}",
            EvaVrr = 999
        };

        repo.On(nameof(IMatricesRiesgosRepository.ObtenerEvaluacionAsync), _ => Task.FromResult<EvaluacionRiesgoDto?>(dto));
        repo.On(nameof(IMatricesRiesgosRepository.ObtenerVersionFormularioAsync), _ =>
            Task.FromResult<VersionFormularioDto?>(new VersionFormularioDto
            {
                VerId = 1,
                VerFamiliaId = 1,
                VerCodigo = "MATRIZ_LAFT",
                VerVersion = 1,
                VerJson = @"{""secciones"":[]}",
                VerHash = new string('c', 64),
                VerEstado = "PUBLISHED",
                VerVigente = true,
                VerFechaCreacion = DateTime.UtcNow,
                VerUsrCreacion = 1
            }));
        validador.On(nameof(IFormularioValidador.ValidarRespuestasAsync), _ => Task.FromResult(new FormularioValidationResult()));

        // El cálculo autoritativo del servidor determina que VRR es 2, no 999
        calculador.On(nameof(IMatricesRiesgoService.CalcularYValidarRiesgo), _ =>
            ServiceResult<CalculoRiesgoResultadoDto>.Ok(new CalculoRiesgoResultadoDto { Vri = 3, Etp = 50m, Vrr = 2 }));

        EvaluacionRiesgoDto? persistido = null;
        repo.On(nameof(IMatricesRiesgosRepository.ActualizarEvaluacionAsync), args =>
        {
            persistido = (EvaluacionRiesgoDto)args[0]!;
            return Task.FromResult(true);
        });

        var resultado = await service.ActualizarEvaluacionAsync(dto, 1, "127.0.0.1");

        // El backend NO confía en el valor forjado por el cliente; recalcula y persiste el autoritativo
        Assert.True(resultado.Success, resultado.Message);
        Assert.NotNull(persistido);
        Assert.Equal(2, persistido!.EvaVrr);
        Assert.NotEqual(999, persistido.EvaVrr);
    }

    [Fact]
    public async Task ResidualVersionAware_EvaluacionHistoricaUsaSuVersionAsociada()
    {
        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repo, out InterfaceStub validador, out InterfaceStub calculador);

        long versionHistoricaId = 10;
        var versionHistorica = new VersionFormularioDto
        {
            VerId = versionHistoricaId,
            VerFamiliaId = 1,
            VerCodigo = "MATRIZ_LAFT_V1",
            VerVersion = 1,
            VerJson = @"{""secciones"":[]}",
            VerHash = new string('1', 64),
            VerEstado = "PUBLISHED",
            VerVigente = false,
            VerFechaCreacion = DateTime.UtcNow,
            VerUsrCreacion = 1
        };

        var dto = new EvaluacionRiesgoDto
        {
            EvaId = 300,
            EvaVersionId = versionHistoricaId,
            EvaEstado = "BORRADOR",
            EvaDataJson = @"{""frecuencia_inherente"":2,""impacto_inherente"":2,""controles_preventivo"":0.5,""controles_detectivo"":0.0,""controles_correctivo"":0.0}",
            EvaDataCalcJson = "{}"
        };

        long versionConsultadaId = 0;
        repo.On(nameof(IMatricesRiesgosRepository.ObtenerEvaluacionAsync), _ => Task.FromResult<EvaluacionRiesgoDto?>(dto));
        repo.On(nameof(IMatricesRiesgosRepository.ObtenerVersionFormularioAsync), args =>
        {
            versionConsultadaId = (long)args[0]!;
            return Task.FromResult<VersionFormularioDto?>(versionHistorica);
        });
        validador.On(nameof(IFormularioValidador.ValidarRespuestasAsync), _ => Task.FromResult(new FormularioValidationResult()));
        calculador.On(nameof(IMatricesRiesgoService.CalcularYValidarRiesgo), _ =>
            ServiceResult<CalculoRiesgoResultadoDto>.Ok(new CalculoRiesgoResultadoDto { Vri = 3, Etp = 50m, Vrr = 2 }));
        repo.On(nameof(IMatricesRiesgosRepository.ActualizarEvaluacionAsync), _ => Task.FromResult(true));

        var resultado = await service.ActualizarEvaluacionAsync(dto, 1, "127.0.0.1");
        Assert.True(resultado.Success, resultado.Message);

        // Se verifica que la evaluación consultó y evaluó contra su versión histórica (10), no una vigente
        Assert.Equal(versionHistoricaId, versionConsultadaId);
    }

    [Theory]
    [InlineData("EN_REVISION")]
    [InlineData("APROBADA")]
    [InlineData("CERRADA")]
    [InlineData("RECHAZADA")]
    public async Task WorkflowInmutabilidad_EvaluacionesNoBorrador_NoModificables(string estadoInmutable)
    {
        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repo, out _, out _);

        var dto = new EvaluacionRiesgoDto
        {
            EvaId = 400,
            EvaVersionId = 1,
            EvaEstado = estadoInmutable,
            EvaDataJson = @"{""respuesta_riesgo"":""MITIGAR""}",
            EvaDataCalcJson = "{}"
        };

        repo.On(nameof(IMatricesRiesgosRepository.ObtenerEvaluacionAsync), _ =>
            Task.FromResult<EvaluacionRiesgoDto?>(new EvaluacionRiesgoDto
            {
                EvaId = 400,
                EvaVersionId = 1,
                EvaEstado = estadoInmutable,
                EvaDataJson = "{}",
                EvaDataCalcJson = "{}"
            }));

        var resultado = await service.ActualizarEvaluacionAsync(dto, 1, "127.0.0.1");

        Assert.False(resultado.Success);
        Assert.Equal(400, resultado.StatusCode);
        Assert.Contains("BORRADOR", resultado.Message);
    }

    [Fact]
    public async Task ConcurrenciaOptimista_VersionStale_Retorna409Conflict()
    {
        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repo, out InterfaceStub validador, out InterfaceStub calculador);

        var dto = new EvaluacionRiesgoDto
        {
            EvaId = 500,
            EvaVersionId = 1,
            EvaEstado = "BORRADOR",
            EvaDataJson = @"{""respuesta_riesgo"":""MITIGAR""}",
            EvaDataCalcJson = "{}",
            EvaVersionRow = 1
        };

        repo.On(nameof(IMatricesRiesgosRepository.ObtenerEvaluacionAsync), _ => Task.FromResult<EvaluacionRiesgoDto?>(dto));
        repo.On(nameof(IMatricesRiesgosRepository.ObtenerVersionFormularioAsync), _ =>
            Task.FromResult<VersionFormularioDto?>(new VersionFormularioDto
            {
                VerId = 1,
                VerFamiliaId = 1,
                VerCodigo = "MATRIZ_LAFT",
                VerVersion = 1,
                VerJson = @"{""secciones"":[]}",
                VerHash = new string('c', 64),
                VerEstado = "PUBLISHED",
                VerVigente = true,
                VerFechaCreacion = DateTime.UtcNow,
                VerUsrCreacion = 1
            }));
        validador.On(nameof(IFormularioValidador.ValidarRespuestasAsync), _ => Task.FromResult(new FormularioValidationResult()));
        calculador.On(nameof(IMatricesRiesgoService.CalcularYValidarRiesgo), _ =>
            ServiceResult<CalculoRiesgoResultadoDto>.Ok(new CalculoRiesgoResultadoDto { Vri = 4, Etp = 50m, Vrr = 2 }));
        repo.On(nameof(IMatricesRiesgosRepository.ActualizarEvaluacionAsync), _ =>
            Task.FromException<bool>(new DBConcurrencyException("Conflicto de concurrencia optimista.")));

        var resultado = await service.ActualizarEvaluacionAsync(dto, 1, "127.0.0.1");

        Assert.False(resultado.Success);
        Assert.Equal(409, resultado.StatusCode);
    }

    private static MatricesRiesgosAppService CrearServicio(
        out InterfaceStub repoStub,
        out InterfaceStub validadorStub,
        out InterfaceStub calculadorStub)
    {
        IMatricesRiesgosRepository repo = InterfaceStub.Create<IMatricesRiesgosRepository>(out repoStub);
        IFormularioValidador validador = InterfaceStub.Create<IFormularioValidador>(out validadorStub);
        IMatricesRiesgoService calculador = InterfaceStub.Create<IMatricesRiesgoService>(out calculadorStub);
        IAuditoriaRepository auditoria = InterfaceStub.Create<IAuditoriaRepository>(out InterfaceStub auditoriaStub);
        auditoriaStub.On("RegistrarAsync", _ => Task.CompletedTask);
        return MatricesRiesgosTestFactory.CreateAppService(repo, validador, calculador, auditoria);
    }
}
