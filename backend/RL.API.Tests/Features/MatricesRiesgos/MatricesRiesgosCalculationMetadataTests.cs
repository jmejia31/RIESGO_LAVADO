using System;
using System.Reflection;
using System.Text.Json;
using RL.API.Features.MatricesRiesgos.Persistence;
using Xunit;

namespace RL.API.Tests.Features.MatricesRiesgos;

public sealed class MatricesRiesgosCalculationMetadataTests
{
    [Fact]
    public void MetadatosDeRegla_SobrescribenValoresRemitidosPorElCliente()
    {
        MethodInfo metodo = ObtenerMetodoIncorporacion();
        const string calculosCliente =
            "{\"reglaCodigo\":\"REGLA_CLIENTE\",\"reglaVersion\":\"999\",\"algoritmoId\":\"ALGORITMO_CLIENTE\",\"vri\":7,\"vrr\":4}";

        string resultado = Assert.IsType<string>(metodo.Invoke(
            null,
            new object[]
            {
                calculosCliente,
                "CALCULO_VRI_VRR",
                "1.0",
                "MATRICES_VRI_ADITIVO_1_9"
            }));

        using JsonDocument documento = JsonDocument.Parse(resultado);
        JsonElement raiz = documento.RootElement;

        Assert.Equal("CALCULO_VRI_VRR", raiz.GetProperty("reglaCodigo").GetString());
        Assert.Equal("1.0", raiz.GetProperty("reglaVersion").GetString());
        Assert.Equal("MATRICES_VRI_ADITIVO_1_9", raiz.GetProperty("algoritmoId").GetString());
        Assert.Equal(7, raiz.GetProperty("vri").GetInt32());
        Assert.Equal(4, raiz.GetProperty("vrr").GetInt32());
    }

    [Fact]
    public void MetadatosDeRegla_RechazanResultadosQueNoSeanObjetoJson()
    {
        MethodInfo metodo = ObtenerMetodoIncorporacion();

        TargetInvocationException error = Assert.Throws<TargetInvocationException>(() =>
            metodo.Invoke(
                null,
                new object[]
                {
                    "[]",
                    "CALCULO_VRI_VRR",
                    "1.0",
                    "MATRICES_VRI_ADITIVO_1_9"
                }));

        ArgumentException causa = Assert.IsType<ArgumentException>(error.InnerException);
        Assert.Contains("objeto JSON", causa.Message);
    }

    private static MethodInfo ObtenerMetodoIncorporacion()
    {
        return typeof(MatricesRiesgosRepository).GetMethod(
                   "IncorporarMetadatosRegla",
                   BindingFlags.NonPublic | BindingFlags.Static)
               ?? throw new InvalidOperationException(
                   "No se encontró el método que incorpora los metadatos institucionales de la regla.");
    }
}
