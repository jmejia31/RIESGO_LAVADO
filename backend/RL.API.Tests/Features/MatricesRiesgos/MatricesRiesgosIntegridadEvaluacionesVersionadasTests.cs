using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RL.API.Features.Auditoria.Persistence;
using RL.API.Features.MatricesRiesgos.Application;
using RL.API.Features.MatricesRiesgos.Contracts;
using RL.API.Features.MatricesRiesgos.Domain;
using RL.API.Features.MatricesRiesgos.Persistence;
using RL.API.Shared.Results;
using RL.API.Tests.Support;
using Xunit;

namespace RL.API.Tests.Features.MatricesRiesgos;

public class MatricesRiesgosIntegridadEvaluacionesVersionadasTests
{
    private static MatricesRiesgosAppService CrearServicioConValidador(
        IFormularioValidador validador,
        out InterfaceStub repoStub)
    {
        IMatricesRiesgosRepository repo = InterfaceStub.Create<IMatricesRiesgosRepository>(out repoStub);
        IMatricesRiesgoService calculador = InterfaceStub.Create<IMatricesRiesgoService>(out _);
        IAuditoriaRepository auditoria = InterfaceStub.Create<IAuditoriaRepository>(out InterfaceStub auditoriaStub);
        auditoriaStub.On("RegistrarAsync", _ => Task.CompletedTask);
        return new MatricesRiesgosAppService(repo, validador, calculador, auditoria);
    }

    [Fact]
    public async Task ActualizarEvaluacion_MismatchEvaVersionId_Retorna400()
    {
        var evaluacionPersistida = new EvaluacionRiesgoDto
        {
            EvaId = 50,
            EvaRiesgoId = 1,
            EvaVersionId = 100, // Persistido es 100
            EvaEstado = "BORRADOR"
        };

        var dtoEnviado = new EvaluacionRiesgoDto
        {
            EvaId = 50,
            EvaRiesgoId = 1,
            EvaVersionId = 200, // Mismatch (se envía 200)
            EvaEstado = "BORRADOR",
            EvaDataJson = "{}",
            EvaDataCalcJson = "{}"
        };

        IFormularioValidador validador = InterfaceStub.Create<IFormularioValidador>(out _);
        MatricesRiesgosAppService service = CrearServicioConValidador(validador, out InterfaceStub repoStub);
        repoStub.On("ObtenerEvaluacionAsync", _ => Task.FromResult<EvaluacionRiesgoDto?>(evaluacionPersistida));

        ServiceResult res = await service.ActualizarEvaluacionAsync(dtoEnviado, 99, "127.0.0.1");

        Assert.False(res.Success);
        Assert.Equal(400, res.StatusCode);
        Assert.Contains("no coincide con el persistido", res.Message);
    }

    [Theory]
    [InlineData("EN_REVISION")]
    [InlineData("APROBADA")]
    [InlineData("RECHAZADA")]
    [InlineData("CERRADA")]
    public async Task ActualizarEvaluacion_EstadoNoBorrador_Retorna400(string estadoNoBorrador)
    {
        var evaluacionPersistida = new EvaluacionRiesgoDto
        {
            EvaId = 50,
            EvaRiesgoId = 1,
            EvaVersionId = 100,
            EvaEstado = estadoNoBorrador
        };

        var dtoEnviado = new EvaluacionRiesgoDto
        {
            EvaId = 50,
            EvaRiesgoId = 1,
            EvaVersionId = 100,
            EvaEstado = estadoNoBorrador,
            EvaDataJson = "{}",
            EvaDataCalcJson = "{}"
        };

        IFormularioValidador validador = InterfaceStub.Create<IFormularioValidador>(out _);
        MatricesRiesgosAppService service = CrearServicioConValidador(validador, out InterfaceStub repoStub);
        repoStub.On("ObtenerEvaluacionAsync", _ => Task.FromResult<EvaluacionRiesgoDto?>(evaluacionPersistida));

        ServiceResult res = await service.ActualizarEvaluacionAsync(dtoEnviado, 99, "127.0.0.1");

        Assert.False(res.Success);
        Assert.Equal(400, res.StatusCode);
        Assert.Contains("Solo se permite editar evaluaciones en estado BORRADOR", res.Message);
    }

    [Fact]
    public async Task Validador_FormularioValidador_SoportaClaveYAliasesLegacy()
    {
        var validador = new FormularioValidador();
        string configJson = @"{
            ""secciones"": [
                {
                    ""campos"": [
                        { ""clave"": ""campo_clave"", ""tipo"": ""texto"", ""obligatorio"": true },
                        { ""rutaDatos"": ""campo_ruta"", ""tipo"": ""texto"", ""obligatorio"": true },
                        { ""identificador"": ""campo_ident"", ""tipo"": ""texto"", ""obligatorio"": true },
                        { ""id"": ""campo_id_legacy"", ""tipo"": ""texto"", ""obligatorio"": true }
                    ]
                }
            ]
        }";

        string respuestasJson = @"{
            ""campo_clave"": ""valor1"",
            ""campo_ruta"": ""valor2"",
            ""campo_ident"": ""valor3"",
            ""campo_id_legacy"": ""valor4""
        }";

        FormularioValidationResult res = await validador.ValidarRespuestasAsync(respuestasJson, configJson);

        Assert.True(res.Valido);
        Assert.Empty(res.Errores);
    }

    [Fact]
    public async Task Validador_SelectorYCatalogo_AdmiteCodigosStringAlfanumericosYEvitaPerdida()
    {
        var validador = new FormularioValidador();
        string configJson = @"{
            ""secciones"": [
                {
                    ""campos"": [
                        {
                            ""clave"": ""proceso"",
                            ""tipo"": ""selector-catalogo"",
                            ""obligatorio"": true,
                            ""opciones"": [
                                { ""codigo"": ""001"", ""nombre"": ""Proceso 001"" },
                                { ""codigo"": ""G-IVM"", ""nombre"": ""Gerencia IVM"" }
                            ]
                        },
                        {
                            ""clave"": ""subprocesos"",
                            ""tipo"": ""catalogo-multiple"",
                            ""obligatorio"": true,
                            ""opciones"": [
                                { ""codigo"": ""GTIC"", ""nombre"": ""Gerencia TIC"" },
                                { ""codigo"": ""G-IVM"", ""nombre"": ""Gerencia IVM"" }
                            ]
                        }
                    ]
                }
            ]
        }";

        string respuestasJson = @"{
            ""proceso"": ""G-IVM"",
            ""subprocesos"": [""GTIC"", ""G-IVM""]
        }";

        FormularioValidationResult res = await validador.ValidarRespuestasAsync(respuestasJson, configJson);

        Assert.True(res.Valido);
        Assert.Empty(res.Errores);
    }

    [Fact]
    public async Task Validador_TipoCatalogo_AceptaCodigosAlfanumericosYNumericos()
    {
        var validador = new FormularioValidador();
        string configJson = @"{
            ""secciones"": [
                {
                    ""campos"": [
                        {
                            ""clave"": ""cat_alfa1"",
                            ""tipo"": ""catalogo"",
                            ""obligatorio"": true,
                            ""opciones"": [
                                { ""codigo"": ""001"", ""nombre"": ""Primer Código"" },
                                { ""codigo"": ""G-IVM"", ""nombre"": ""Gerencia IVM"" }
                            ]
                        },
                        {
                            ""clave"": ""cat_num1"",
                            ""tipo"": ""catalogo"",
                            ""obligatorio"": true,
                            ""opciones"": [
                                { ""codigo"": ""10"", ""nombre"": ""Diez"" }
                            ]
                        }
                    ]
                }
            ]
        }";

        string respuestasValidasAlfa = @"{ ""cat_alfa1"": ""001"", ""cat_num1"": 10 }";
        string respuestasValidasGivm = @"{ ""cat_alfa1"": ""G-IVM"", ""cat_num1"": ""10"" }";

        FormularioValidationResult resAlfa = await validador.ValidarRespuestasAsync(respuestasValidasAlfa, configJson);
        FormularioValidationResult resGivm = await validador.ValidarRespuestasAsync(respuestasValidasGivm, configJson);

        Assert.True(resAlfa.Valido);
        Assert.Empty(resAlfa.Errores);

        Assert.True(resGivm.Valido);
        Assert.Empty(resGivm.Errores);
    }

    [Fact]
    public async Task Validador_TipoCatalogo_RechazaEtiquetaVisibleYCodigoInexistente()
    {
        var validador = new FormularioValidador();
        string configJson = @"{
            ""secciones"": [
                {
                    ""campos"": [
                        {
                            ""clave"": ""cat_campo"",
                            ""tipo"": ""catalogo"",
                            ""obligatorio"": true,
                            ""opciones"": [
                                { ""codigo"": ""001"", ""nombre"": ""Gerencia General"" },
                                { ""codigo"": ""G-IVM"", ""nombre"": ""Gerencia IVM"" }
                            ]
                        }
                    ]
                }
            ]
        }";

        string respEtiqueta = @"{ ""cat_campo"": ""Gerencia General"" }";
        string respInexistente = @"{ ""cat_campo"": ""INVALIDO_999"" }";

        FormularioValidationResult resEtiqueta = await validador.ValidarRespuestasAsync(respEtiqueta, configJson);
        FormularioValidationResult resInexistente = await validador.ValidarRespuestasAsync(respInexistente, configJson);

        Assert.False(resEtiqueta.Valido);
        Assert.Single(resEtiqueta.Errores);
        Assert.Contains("no corresponde a un código válido", resEtiqueta.Errores[0].Mensaje);

        Assert.False(resInexistente.Valido);
        Assert.Single(resInexistente.Errores);
        Assert.Contains("no corresponde a un código válido", resInexistente.Errores[0].Mensaje);
    }

    [Fact]
    public async Task Validador_CodigoCatalogoHistoricoReferenciado_ResuelveOpcionesDesdeCatalogosRaiz()
    {
        var validador = new FormularioValidador();
        string configJson = @"{
            ""secciones"": [
                {
                    ""campos"": [
                        {
                            ""clave"": ""nivel_riesgo"",
                            ""tipo"": ""catalogo"",
                            ""codigoCatalogo"": ""CAT_NIVEL_RIESGO"",
                            ""obligatorio"": true
                        },
                        {
                            ""clave"": ""gerencia_resp"",
                            ""tipo"": ""selector-catalogo"",
                            ""codigoCatalogo"": ""CAT_GERENCIAS"",
                            ""obligatorio"": true
                        }
                    ]
                }
            ],
            ""catalogos"": [
                {
                    ""codigo"": ""CAT_NIVEL_RIESGO"",
                    ""elementos"": [
                        { ""codigo"": ""001"", ""valor"": ""Bajo"" },
                        { ""codigo"": ""002"", ""valor"": ""Medio"" }
                    ]
                },
                {
                    ""codigo"": ""CAT_GERENCIAS"",
                    ""elementos"": [
                        { ""codigo"": ""G-IVM"", ""valor"": ""Gerencia IVM"" }
                    ]
                }
            ]
        }";

        string respValida = @"{ ""nivel_riesgo"": ""001"", ""gerencia_resp"": ""G-IVM"" }";
        string respInvalidaEtiqueta = @"{ ""nivel_riesgo"": ""Bajo"", ""gerencia_resp"": ""G-IVM"" }";
        string respInvalidaInexistente = @"{ ""nivel_riesgo"": ""001"", ""gerencia_resp"": ""G-TIC"" }";

        FormularioValidationResult resValida = await validador.ValidarRespuestasAsync(respValida, configJson);
        FormularioValidationResult resEtiqueta = await validador.ValidarRespuestasAsync(respInvalidaEtiqueta, configJson);
        FormularioValidationResult resInexistente = await validador.ValidarRespuestasAsync(respInvalidaInexistente, configJson);

        Assert.True(resValida.Valido);
        Assert.Empty(resValida.Errores);

        Assert.False(resEtiqueta.Valido);
        Assert.Single(resEtiqueta.Errores);
        Assert.Contains("no corresponde a un código válido", resEtiqueta.Errores[0].Mensaje);

        Assert.False(resInexistente.Valido);
        Assert.Single(resInexistente.Errores);
        Assert.Contains("no corresponde a un código válido", resInexistente.Errores[0].Mensaje);
    }

    [Fact]
    public async Task Validador_CodigoInexistente_O_EtiquetaEnviadaComoCodigo_ReportaError()
    {
        var validador = new FormularioValidador();
        string configJson = @"{
            ""secciones"": [
                {
                    ""campos"": [
                        {
                            ""clave"": ""proceso"",
                            ""tipo"": ""selector-catalogo"",
                            ""obligatorio"": true,
                            ""opciones"": [
                                { ""codigo"": ""001"", ""nombre"": ""Proceso 001"" }
                            ]
                        }
                    ]
                }
            ]
        }";

        // Se envía la etiqueta 'Proceso 001' o un código inexistente '999'
        string respuestasInexistente = @"{ ""proceso"": ""999"" }";
        string respuestasEtiqueta = @"{ ""proceso"": ""Proceso 001"" }";

        FormularioValidationResult resInexistente = await validador.ValidarRespuestasAsync(respuestasInexistente, configJson);
        FormularioValidationResult resEtiqueta = await validador.ValidarRespuestasAsync(respuestasEtiqueta, configJson);

        Assert.False(resInexistente.Valido);
        Assert.Single(resInexistente.Errores);
        Assert.Contains("no corresponde a un código válido", resInexistente.Errores[0].Mensaje);

        Assert.False(resEtiqueta.Valido);
        Assert.Single(resEtiqueta.Errores);
        Assert.Contains("no corresponde a un código válido", resEtiqueta.Errores[0].Mensaje);
    }
}
