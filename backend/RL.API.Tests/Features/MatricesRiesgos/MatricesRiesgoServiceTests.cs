using System;
using Xunit;
using RL.API.Features.MatricesRiesgos.Domain;
using RL.API.Features.MatricesRiesgos.Contracts;

namespace RL.API.Tests.Features.MatricesRiesgos;

public sealed class MatricesRiesgoServiceTests
{
    private readonly MatricesRiesgoService _service = new();

    [Fact]
    public void CalcularYValidarRiesgo_DebeCalcularVriCorrectamente()
    {
        // VRI = frecuencia + impacto - 1
        var result = _service.CalcularYValidarRiesgo(3, 4, 0, 0, 0, 3, 4);
        
        Assert.True(result.Success);
        Assert.Equal(6, result.Data!.Vri); // 3 + 4 - 1
    }

    [Fact]
    public void CalcularYValidarRiesgo_DebeCalcularEtpConPesosEstablecidos()
    {
        // ETP = (preventivo * 0.70) + (detectivo * 0.15) + (correctivo * 0.15)
        // ETP = (50 * 0.70) + (30 * 0.15) + (20 * 0.15) = 35 + 4.5 + 3 = 42.5
        var result = _service.CalcularYValidarRiesgo(3, 3, 50, 30, 20, 3, 2); // VRI = 5. Vrr = round(5 * (1 - 0.425)) = round(5 * 0.575) = round(2.875) = 3. Vrr2 = 3 + 2 - 1 = 4. (Incoherente)
        
        Assert.False(result.Success);
        Assert.Equal(42.5m, result.Data!.Etp);
    }

    [Theory]
    [InlineData(1, 1, 0, 0, 0, 1, 1)] // VRI = 1. ETP = 0. VRR = 1. Coherente
    [InlineData(5, 5, 0, 0, 0, 5, 5)] // VRI = 9. ETP = 0. VRR = 9. Coherente
    [InlineData(3, 3, 100, 100, 100, 1, 1)] // VRI = 5. ETP = 100. VRR = round(5 * 0) = 0 -> acotado a 1. Coherente
    [InlineData(4, 4, 50, 50, 50, 2, 3)] // VRI = 7. ETP = 50. VRR = round(7 * 0.5) = 4. VRR2 = 2 + 3 - 1 = 4. Coherente
    public void CalcularYValidarRiesgo_CasosCoherentes_DebeRetornarOk(
        int frec, int imp, decimal prev, decimal det, decimal corr, int frecRes, int impRes)
    {
        var result = _service.CalcularYValidarRiesgo(frec, imp, prev, det, corr, frecRes, impRes);
        Assert.True(result.Success);
        Assert.True(result.Data!.Coherente);
    }

    [Fact]
    public void CalcularYValidarRiesgo_Incoherente_DebeRetornarBadRequest()
    {
        // VRI = 5 + 5 - 1 = 9
        // ETP = 100 -> VRR = 1
        // VRR2 = 4 + 4 - 1 = 7. Incoherente
        var result = _service.CalcularYValidarRiesgo(5, 5, 100, 100, 100, 4, 4);
        
        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.False(result.Data!.Coherente);
    }

    [Fact]
    public void CalcularYValidarRiesgo_PruebaDeRegresionDeLos25ParesFrecuenciaImpacto()
    {
        // Validar todos los 25 pares posibles de Frecuencia (1-5) e Impacto (1-5) 
        // con mitigación nula (ETP = 0) de forma que VRR sea idéntico a VRI y sea siempre coherente
        for (int f = 1; f <= 5; f++)
        {
            for (int i = 1; i <= 5; i++)
            {
                int vriEsperado = f + i - 1;
                
                // Determinamos una combinación coherente de residuales que sume vriEsperado
                // Por simplicidad, residual = inherente cuando no hay mitigación
                var result = _service.CalcularYValidarRiesgo(f, i, 0, 0, 0, f, i);
                
                Assert.True(result.Success);
                Assert.Equal(vriEsperado, result.Data!.Vri);
                Assert.Equal(vriEsperado, result.Data!.Vrr);
                Assert.Equal(vriEsperado, result.Data!.Vrr2);
                Assert.True(result.Data.Coherente);
            }
        }
    }

    [Theory]
    // Frecuencias o Impactos inherentes incorrectos
    [InlineData(0, 3, 0, 0, 0, 3, 3)]
    [InlineData(6, 3, 0, 0, 0, 3, 3)]
    [InlineData(3, 0, 0, 0, 0, 3, 3)]
    [InlineData(3, 6, 0, 0, 0, 3, 3)]
    // Frecuencias o Impactos residuales incorrectos
    [InlineData(3, 3, 0, 0, 0, 0, 3)]
    [InlineData(3, 3, 0, 0, 0, 6, 3)]
    [InlineData(3, 3, 0, 0, 0, 3, 0)]
    [InlineData(3, 3, 0, 0, 0, 3, 6)]
    // Controles fuera del rango 0-100
    [InlineData(3, 3, -1, 0, 0, 3, 3)]
    [InlineData(3, 3, 101, 0, 0, 3, 3)]
    [InlineData(3, 3, 0, -1, 0, 3, 3)]
    [InlineData(3, 3, 0, 101, 0, 3, 3)]
    [InlineData(3, 3, 0, 0, -1, 3, 3)]
    [InlineData(3, 3, 0, 0, 101, 3, 3)]
    public void CalcularYValidarRiesgo_InputsFueraDeRango_RetornaBadRequest(
        int frec, int imp, decimal prev, decimal det, decimal corr, int frecRes, int impRes)
    {
        var result = _service.CalcularYValidarRiesgo(frec, imp, prev, det, corr, frecRes, impRes);
        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public void CalcularYValidarRiesgo_ValidarEfectoMitigacionIndividual_Preventivo()
    {
        // 100% preventivo -> ETP = 70%
        // VRI = 3 + 3 - 1 = 5
        // VRR = 5 * (1 - 0.70) = 5 * 0.30 = 1.50 -> redondea a 2 (AwayFromZero)
        // VRR2 = 2 (coherente)
        var result = _service.CalcularYValidarRiesgo(3, 3, 100, 0, 0, 2, 1);
        
        Assert.True(result.Success);
        Assert.Equal(70m, result.Data!.Etp);
        Assert.Equal(2, result.Data!.Vrr);
    }

    [Fact]
    public void CalcularYValidarRiesgo_ValidarEfectoMitigacionIndividual_Detectivo()
    {
        // 100% detectivo -> ETP = 15%
        // VRI = 3 + 3 - 1 = 5
        // VRR = 5 * (1 - 0.15) = 5 * 0.85 = 4.25 -> redondea a 4 (AwayFromZero)
        // VRR2 = 4 (coherente)
        var result = _service.CalcularYValidarRiesgo(3, 3, 0, 100, 0, 3, 2);
        
        Assert.True(result.Success);
        Assert.Equal(15m, result.Data!.Etp);
        Assert.Equal(4, result.Data!.Vrr);
    }

    [Fact]
    public void CalcularYValidarRiesgo_ValidarEfectoMitigacionIndividual_Correctivo()
    {
        // 100% correctivo -> ETP = 15%
        // VRI = 3 + 3 - 1 = 5
        // VRR = 5 * (1 - 0.15) = 5 * 0.85 = 4.25 -> redondea a 4
        // VRR2 = 4 (coherente)
        var result = _service.CalcularYValidarRiesgo(3, 3, 0, 0, 100, 3, 2);
        
        Assert.True(result.Success);
        Assert.Equal(15m, result.Data!.Etp);
        Assert.Equal(4, result.Data!.Vrr);
    }
}
