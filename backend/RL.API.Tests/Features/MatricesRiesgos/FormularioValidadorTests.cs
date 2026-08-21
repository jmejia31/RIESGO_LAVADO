using System;
using System.Threading.Tasks;
using Xunit;
using RL.API.Features.MatricesRiesgos.Domain;

namespace RL.API.Tests.Features.MatricesRiesgos;

public sealed class FormularioValidadorTests
{
    private readonly FormularioValidador _validador = new();

    private const string ConfigValida = @"
    {
        ""codigoFormulario"": ""TEST_FORM"",
        ""nombreFormulario"": ""Formulario de Prueba"",
        ""secciones"": [
            {
                ""id"": ""sec1"",
                ""titulo"": ""Sección 1"",
                ""campos"": [
                    { ""id"": ""campo_texto"", ""etiqueta"": ""Texto"", ""tipo"": ""texto"", ""obligatorio"": true },
                    { ""id"": ""campo_num"", ""etiqueta"": ""Número"", ""tipo"": ""numero"", ""obligatorio"": false },
                    { ""id"": ""campo_regex"", ""etiqueta"": ""Regex"", ""tipo"": ""texto"", ""obligatorio"": false, ""regexValidacion"": ""^[A-Z]{3}-\\d{3}$"" }
                ]
            }
        ]
    }";

    [Fact]
    public async Task ValidarRespuestas_ConfigNula_RetornaError()
    {
        var result = await _validador.ValidarRespuestasAsync("{}", "");
        
        Assert.False(result.Valido);
        Assert.Contains(result.Errores, e => e.Campo == "Configuracion");
    }

    [Fact]
    public async Task ValidarRespuestas_RespuestasVacias_CamposObligatorios_RetornaError()
    {
        // El campo 'campo_texto' es obligatorio y no está presente
        var result = await _validador.ValidarRespuestasAsync("{}", ConfigValida);
        
        Assert.False(result.Valido);
        Assert.Contains(result.Errores, e => e.Campo == "campo_texto" && e.Mensaje.Contains("obligatorio"));
    }

    [Fact]
    public async Task ValidarRespuestas_RespuestasValidas_RetornaOk()
    {
        var respuestas = @"{
            ""campo_texto"": ""Hola mundo"",
            ""campo_num"": 123.45,
            ""campo_regex"": ""ABC-123""
        }";

        var result = await _validador.ValidarRespuestasAsync(respuestas, ConfigValida);
        
        Assert.True(result.Valido);
        Assert.Empty(result.Errores);
    }

    [Fact]
    public async Task ValidarRespuestas_CamposSucios_RetornaError()
    {
        // 'campo_sucio' no está definido en el esquema
        var respuestas = @"{
            ""campo_texto"": ""Hola"",
            ""campo_sucio"": ""Basura""
        }";

        var result = await _validador.ValidarRespuestasAsync(respuestas, ConfigValida);
        
        Assert.False(result.Valido);
        Assert.Contains(result.Errores, e => e.Campo == "campo_sucio" && e.Mensaje.Contains("no está definida"));
    }

    [Fact]
    public async Task ValidarRespuestas_TiposDeDatosIncorrectos_RetornaError()
    {
        // 'campo_num' debe ser un número pero se envía un string que no parsea
        var respuestas = @"{
            ""campo_texto"": ""Hola"",
            ""campo_num"": ""no-un-numero""
        }";

        var result = await _validador.ValidarRespuestasAsync(respuestas, ConfigValida);
        
        Assert.False(result.Valido);
        Assert.Contains(result.Errores, e => e.Campo == "campo_num" && e.Mensaje.Contains("numérico"));
    }

    [Fact]
    public async Task ValidarRespuestas_RegexInvalido_RetornaError()
    {
        // 'campo_regex' no cumple con el formato
        var respuestas = @"{
            ""campo_texto"": ""Hola"",
            ""campo_regex"": ""abc-12""
        }";

        var result = await _validador.ValidarRespuestasAsync(respuestas, ConfigValida);
        
        Assert.False(result.Valido);
        Assert.Contains(result.Errores, e => e.Campo == "campo_regex" && e.Mensaje.Contains("formato requerido"));
    }

    [Fact]
    public async Task ValidarRespuestas_RegexConfigIncorrecta_RetornaError()
    {
        // Expresión regular con sintaxis rota en la plantilla del formulario
        const string configRegexRota = @"
        {
            ""secciones"": [
                {
                    ""campos"": [
                        { ""id"": ""c1"", ""etiqueta"": ""C1"", ""tipo"": ""texto"", ""obligatorio"": false, ""regexValidacion"": ""["" }
                    ]
                }
            ]
        }";

        var respuestas = @"{ ""c1"": ""valor"" }";
        var result = await _validador.ValidarRespuestasAsync(respuestas, configRegexRota);

        Assert.False(result.Valido);
        Assert.Contains(result.Errores, e => e.Campo == "c1" && e.Mensaje.Contains("Expresión regular de validación inválida"));
    }

    [Fact]
    public async Task ObtenerCamposDefinidos_SeccionesNoEsArray_RetornaVacio()
    {
        // Secciones es un objeto, no un array
        const string config = @"{ ""secciones"": {} }";
        var result = await _validador.ValidarRespuestasAsync("{}", config);
        // Debe ser válido porque no hay campos declarados/requeridos
        Assert.True(result.Valido);
    }

    [Fact]
    public async Task ObtenerCamposDefinidos_CamposNoEsArray_RetornaVacio()
    {
        // Campos es un objeto, no un array
        const string config = @"{
            ""secciones"": [
                { ""campos"": {} }
            ]
        }";
        var result = await _validador.ValidarRespuestasAsync("{}", config);
        Assert.True(result.Valido);
    }

    [Fact]
    public async Task ObtenerCamposDefinidos_CampoSinId_IgnoraCampo()
    {
        // El campo no tiene la propiedad ID
        const string config = @"{
            ""secciones"": [
                {
                    ""campos"": [
                        { ""etiqueta"": ""Sin Id"", ""tipo"": ""texto"", ""obligatorio"": true }
                    ]
                }
            ]
        }";
        var result = await _validador.ValidarRespuestasAsync("{}", config);
        Assert.True(result.Valido); // No hay errores porque el campo sin ID se ignora en la definición
    }

    [Fact]
    public async Task ObtenerCamposDefinidos_CampoConIdVacio_IgnoraCampo()
    {
        const string config = @"{
            ""secciones"": [
                {
                    ""campos"": [
                        { ""id"": "" "", ""etiqueta"": ""Vacio"", ""tipo"": ""texto"", ""obligatorio"": true }
                    ]
                }
            ]
        }";
        var result = await _validador.ValidarRespuestasAsync("{}", config);
        Assert.True(result.Valido);
    }

    [Fact]
    public async Task ObtenerCamposDefinidos_CampoSinEtiqueta_UsaIdComoEtiqueta()
    {
        const string config = @"{
            ""secciones"": [
                {
                    ""campos"": [
                        { ""id"": ""c1"", ""tipo"": ""texto"", ""obligatorio"": true }
                    ]
                }
            ]
        }";
        var result = await _validador.ValidarRespuestasAsync("{}", config);
        Assert.False(result.Valido);
        // Debe usar 'c1' como etiqueta en el mensaje
        Assert.Contains(result.Errores, e => e.Campo == "c1" && e.Mensaje.Contains("'c1' es obligatorio"));
    }

    [Fact]
    public async Task ObtenerCamposDefinidos_CampoSinTipo_UsaTextoComoTipo()
    {
        const string config = @"{
            ""secciones"": [
                {
                    ""campos"": [
                        { ""id"": ""c1"", ""obligatorio"": true }
                    ]
                }
            ]
        }";
        var result = await _validador.ValidarRespuestasAsync(@"{ ""c1"": 123 }", config);
        // Dado que por defecto el tipo es 'texto' y se envía un número, debe dar error de tipo texto
        Assert.False(result.Valido);
        Assert.Contains(result.Errores, e => e.Campo == "c1" && e.Mensaje.Contains("debe ser de tipo texto"));
    }

    [Fact]
    public async Task ValidarRespuestas_CampoNumericoComoStringValido_ValidaCorrectamente()
    {
        const string config = @"{
            ""secciones"": [
                {
                    ""campos"": [
                        { ""id"": ""c1"", ""tipo"": ""numero"", ""obligatorio"": true }
                    ]
                }
            ]
        }";
        // Se envía un número en string
        var result = await _validador.ValidarRespuestasAsync(@"{ ""c1"": ""123.45"" }", config);
        Assert.True(result.Valido);
    }

    [Fact]
    public async Task ObtenerCamposDefinidos_EstructuraJsonTotalmenteInvalida_RetornaVacio()
    {
        // El JSON raíz no es un objeto, sino un array
        const string config = @"[]";
        var result = await _validador.ValidarRespuestasAsync("{}", config);
        Assert.True(result.Valido);
    }

    [Fact]
    public async Task ValidarRespuestas_TipoTextoLargo_ValidaCorrectamente()
    {
        const string config = @"{
            ""secciones"": [
                {
                    ""campos"": [
                        { ""id"": ""c1"", ""tipo"": ""texto-largo"", ""obligatorio"": true }
                    ]
                }
            ]
        }";
        var result = await _validador.ValidarRespuestasAsync(@"{ ""c1"": ""Un texto muy largo de prueba"" }", config);
        Assert.True(result.Valido);
    }

    [Fact]
    public async Task ValidarRespuestas_TipoTextoLargoConTipoIncorrecto_RetornaError()
    {
        const string config = @"{
            ""secciones"": [
                {
                    ""campos"": [
                        { ""id"": ""c1"", ""tipo"": ""texto-largo"", ""obligatorio"": true }
                    ]
                }
            ]
        }";
        // Se envía un boolean en vez de texto
        var result = await _validador.ValidarRespuestasAsync(@"{ ""c1"": true }", config);
        Assert.False(result.Valido);
        Assert.Contains(result.Errores, e => e.Campo == "c1" && e.Mensaje.Contains("debe ser de tipo texto"));
    }

    [Fact]
    public async Task ValidarRespuestas_TipoCatalogoValido_RetornaValido()
    {
        const string config = @"{
            ""secciones"": [
                {
                    ""campos"": [
                        { ""id"": ""c1"", ""tipo"": ""catalogo"", ""obligatorio"": true }
                    ]
                }
            ]
        }";
        // Catalogo espera un número entero (ID)
        var result = await _validador.ValidarRespuestasAsync(@"{ ""c1"": 45 }", config);
        Assert.True(result.Valido);
    }

    [Fact]
    public async Task ValidarRespuestas_TipoCatalogoInvalido_RetornaError()
    {
        const string config = @"{
            ""secciones"": [
                {
                    ""campos"": [
                        { ""id"": ""c1"", ""tipo"": ""catalogo"", ""obligatorio"": true }
                    ]
                }
            ]
        }";
        // Catalogo con tipo no primitivo ni string/numero (un booleano o lista)
        var result = await _validador.ValidarRespuestasAsync(@"{ ""c1"": true }", config);
        Assert.False(result.Valido);
        Assert.Contains(result.Errores, e => e.Campo == "c1" && e.Mensaje.Contains("texto o número"));
    }

    [Fact]
    public async Task ValidarRespuestas_TipoCatalogoMultipleValido_RetornaValido()
    {
        const string config = @"{
            ""secciones"": [
                {
                    ""campos"": [
                        { ""id"": ""c1"", ""tipo"": ""catalogo-multiple"", ""obligatorio"": true }
                    ]
                }
            ]
        }";
        // Espera un array de números enteros
        var result = await _validador.ValidarRespuestasAsync(@"{ ""c1"": [1, 2, 3] }", config);
        Assert.True(result.Valido);
    }

    [Fact]
    public async Task ValidarRespuestas_TipoCatalogoMultipleNoArray_RetornaError()
    {
        const string config = @"{
            ""secciones"": [
                {
                    ""campos"": [
                        { ""id"": ""c1"", ""tipo"": ""catalogo-multiple"", ""obligatorio"": true }
                    ]
                }
            ]
        }";
        // Se envía un entero plano en vez de array
        var result = await _validador.ValidarRespuestasAsync(@"{ ""c1"": 1 }", config);
        Assert.False(result.Valido);
        Assert.Contains(result.Errores, e => e.Campo == "c1" && e.Mensaje.Contains("lista de enteros"));
    }

    [Fact]
    public async Task ValidarRespuestas_TipoCatalogoMultipleElementosNoEnteros_RetornaError()
    {
        const string config = @"{
            ""secciones"": [
                {
                    ""campos"": [
                        { ""id"": ""c1"", ""tipo"": ""catalogo-multiple"", ""obligatorio"": true }
                    ]
                }
            ]
        }";
        // Array contiene elementos que no son números (un boolean)
        var result = await _validador.ValidarRespuestasAsync(@"{ ""c1"": [1, ""dos"", 3] }", config);
        Assert.False(result.Valido);
        Assert.Contains(result.Errores, e => e.Campo == "c1" && e.Mensaje.Contains("deben ser números enteros"));
    }
}

