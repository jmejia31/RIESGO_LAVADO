using RL.API.DTOs;
using RL.API.Services;
using System.Runtime.Versioning;
using Xunit;

[assembly: SupportedOSPlatform("windows")]

namespace RL.API.Tests.Services;

public sealed class MatricesRiesgoServiceTests
{
    private readonly MatricesRiesgoService _service = new();

    [Fact]
    public void Calcular_FactorConControlValido_AplicaMitigacionYPlanPorRiesgoAlto()
    {
        var request = CrearRequestFactor(puntaje: 5m, mitigacion: 25m, activo: true, evidencia: true);

        var result = _service.Calcular(request);

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(5m, result.Data.PuntajeInherente);
        Assert.Equal(25m, result.Data.MitigacionPct);
        Assert.Equal(3.75m, result.Data.PuntajeResidual);
        Assert.Equal("ALTO", result.Data.NivelResidual);
        Assert.True(result.Data.RequierePlanAccion);
    }

    [Fact]
    public void Calcular_ControlSinEvidencia_NoReduceRiesgo()
    {
        var request = CrearRequestFactor(puntaje: 5m, mitigacion: 25m, activo: true, evidencia: false);

        var result = _service.Calcular(request);

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(0m, result.Data.MitigacionPct);
        Assert.Equal(5m, result.Data.PuntajeResidual);
        Assert.Equal("CRITICO", result.Data.NivelResidual);
        Assert.True(result.Data.RequierePlanAccion);
    }

    [Fact]
    public void Calcular_RecalculoSinMotivo_RechazaSolicitud()
    {
        var request = CrearRequestFactor(puntaje: 3m);
        request.EsRecalculo = true;
        request.MotivoCalculo = " ";

        var result = _service.Calcular(request);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.Contains("motivo de recálculo", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Calcular_GlobalSinFactoresObligatorios_RechazaSolicitud()
    {
        var request = CrearRequestFactor(puntaje: 3m);
        request.TipoCalculo = "GLOBAL";

        var result = _service.Calcular(request);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.Contains("ponderación institucional global", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("CLIENTES", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("EMPLEADOS", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static MatrizCalculoRequestDto CrearRequestFactor(
        decimal puntaje,
        decimal mitigacion = 0m,
        bool activo = true,
        bool evidencia = false)
    {
        var metodologia = new MetodologiaCalculoDto
        {
            Version = "TEST-1",
            PesoTotalEsperado = 100m,
            PuntajeMinimo = 1m,
            PuntajeMaximo = 5m,
            MitigacionMaximaPct = 50m,
            DecimalesCalculo = 4,
            DecimalesVisualizacion = 2,
            MitigacionesPermitidas = [0m, 25m, 50m],
            FactoresInstitucionales =
            [
                new() { Codigo = "PROVEEDORES", Nombre = "Proveedores", PesoInstitucional = 50m },
                new() { Codigo = "CLIENTES", Nombre = "Clientes", PesoInstitucional = 25m },
                new() { Codigo = "EMPLEADOS", Nombre = "Empleados", PesoInstitucional = 25m }
            ],
            EscalasRiesgo =
            [
                new() { Nivel = "BAJO", Color = "#16a34a", ValorMinimo = 1m, ValorMaximo = 1.99m },
                new() { Nivel = "MEDIO", Color = "#eab308", ValorMinimo = 2m, ValorMaximo = 2.99m },
                new() { Nivel = "ALTO", Color = "#f97316", ValorMinimo = 3m, ValorMaximo = 3.99m, RequierePlanAccion = true },
                new() { Nivel = "CRITICO", Color = "#dc2626", ValorMinimo = 4m, ValorMaximo = 5m, RequierePlanAccion = true }
            ]
        };

        return new MatrizCalculoRequestDto
        {
            TipoCalculo = "FACTOR",
            Metodologia = metodologia,
            Factores =
            [
                new()
                {
                    Codigo = "PROVEEDORES",
                    Nombre = "Proveedores",
                    PesoInstitucional = 50m,
                    Variables =
                    [
                        new()
                        {
                            Codigo = "V1",
                            Nombre = "Variable de prueba",
                            PesoInterno = 100m,
                            Puntaje = puntaje,
                            Obligatoria = true,
                            TieneValor = true
                        }
                    ],
                    Controles = mitigacion > 0m
                        ?
                        [
                            new()
                            {
                                Codigo = "C1",
                                Nombre = "Control de prueba",
                                MitigacionPct = mitigacion,
                                Activo = activo,
                                TieneEvidencia = evidencia
                            }
                        ]
                        : []
                }
            ]
        };
    }
}
