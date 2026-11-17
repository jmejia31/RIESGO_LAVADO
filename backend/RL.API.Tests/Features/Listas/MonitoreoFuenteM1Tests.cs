using System.Reflection;
using RL.API.Features.Listas.Contracts;
using RL.API.Features.Listas.Persistence;
using Xunit;

namespace RL.API.Tests.Features.Listas;

/// <summary>
/// M1: pruebas estructurales de consultas (NO sustituyen Oracle E2E real).
/// Previenen la reintroduccion de agrupaciones que ocultan registros.
/// </summary>
public sealed class MonitoreoFuenteM1Tests
{
    private static string Sql(string method)
    {
        var info = typeof(ListasRepository).GetMethod(method,
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(info);
        return Assert.IsType<string>(info.Invoke(null, null));
    }

    [Fact]
    public void Naturales_UsaClaveFuentePorFila_YSeparaMarcadores()
    {
        var sql = Sql("ConstruirConsultaMonitoreoNaturales");
        Assert.Contains("REPORTE_COINCIDENCIA_ID ID_REGISTRO_FUENTE", sql);
        Assert.Contains("LSP_POSITIVO_ID", sql);
        Assert.Contains("SIN_IDENTIFICACION", sql);
        Assert.Contains("NUMERO_PATRONAL", sql);
        Assert.DoesNotContain("GROUP BY DNI, LISTA_CONCIDENCIA", sql);
        Assert.DoesNotContain("ASOCIACIÓN NO VERIFICADA'\n", sql);
    }

    [Fact]
    public void Empleados_SeObtienenDesdeReporteClasificado_SinMultiplicarPorPlanillas()
    {
        var sql = Sql("ConstruirConsultaMonitoreoEmpleados");
        Assert.Contains("V_REPORTE_COINCIDENCIA", sql);
        Assert.Contains("TIPO_PERSONA LIKE '%IHSS'", sql);
        Assert.Contains("REPORTE_COINCIDENCIA_ID ID_REGISTRO_FUENTE", sql);
        Assert.Contains("V_EMPLEADOS_IHSS_PLANILLAS", sql);
        Assert.DoesNotContain("JOIN DNP_IHSS.REPORTE_COINCIDENCIAS R ON D.IDENTIDAD", sql);
        Assert.DoesNotContain("COUNT(*) TOTAL_REPETIDOS", sql);
    }

    [Theory]
    [InlineData("N/A(3)")]
    [InlineData("N/A(7)")]
    [InlineData("N/A")]
    [InlineData("  ")]
    public void Marcadores_SonNoOperables(string documento)
    {
        if (!string.IsNullOrWhiteSpace(documento))
            Assert.True(IdentidadMonitoreoPolicy.EsMarcadorNoIdentificacion(documento));
        Assert.NotEqual(EstadoIdentidadMonitoreo.Verified,
            IdentidadMonitoreoPolicy.EstadoParaPresentacion(documento, 2, 1));
    }
}
