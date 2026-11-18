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
        Assert.Contains("LSP_ORIGEN_REGISTRO", sql);
        Assert.Contains("POSITIVO_DNP", sql);
        Assert.Contains("NOT EXISTS (SELECT 1 FROM Coincidencias C", sql);
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
        Assert.Contains("POSITIVO_DNP", sql);
        Assert.Contains("LSP_ORIGEN_REGISTRO", sql);
        Assert.Contains("NOT EXISTS (SELECT 1 FROM Coincidencias C", sql);
        Assert.Contains("TIPO_PERSONA LIKE '%IHSS'", sql);
        Assert.Contains("REPORTE_COINCIDENCIA_ID ID_REGISTRO_FUENTE", sql);
        Assert.Contains("V_EMPLEADOS_IHSS_PLANILLAS", sql);
        Assert.DoesNotContain("JOIN DNP_IHSS.REPORTE_COINCIDENCIAS R ON D.IDENTIDAD", sql);
        Assert.DoesNotContain("COUNT(*) TOTAL_REPETIDOS", sql);
    }


    [Fact]
    public void PositivosDnpConMarcadores_NoSeOcultanPorCompartirDocumento()
    {
        foreach (var method in new[] { "ConstruirConsultaMonitoreoNaturales", "ConstruirConsultaMonitoreoEmpleados" })
        {
            var sql = Sql(method);
            Assert.Contains("LP.LSP_NO_DOCUMENTO IS NULL", sql);
            Assert.Contains("UPPER(TRIM(LP.LSP_NO_DOCUMENTO)) LIKE 'N/A%'", sql);
            Assert.Contains("UPPER(TRIM(LP.LSP_NO_DOCUMENTO)) IN ('NULL'", sql);
            Assert.Contains("NOT EXISTS (SELECT 1 FROM Coincidencias C", sql);
        }
    }

    [Fact]
    public void PositivosDnp_SeAsocianSoloConOrigenCompatibleYSinDniDuplicado()
    {
        foreach (var method in new[] { "ConstruirConsultaMonitoreoNaturales", "ConstruirConsultaMonitoreoEmpleados" })
        {
            var sql = Sql(method);
            Assert.Contains("AND UPPER(TRIM(LSP_ORIGEN_REGISTRO)) = 'DNP_LISTAS'", sql);
            Assert.Contains("COUNT(*) OVER (PARTITION BY R.DNI) COINCIDENCIAS_FUENTE", sql);
            Assert.Contains("I.SUJETOS = 1 AND COUNT(*) OVER (PARTITION BY R.DNI) = 1", sql);
        }
    }

    [Fact]
    public void DetallePorReferenciaManual_AdmiteDocumentoNuloOSinIdentificacion()
    {
        var sql = Sql("CompararDocumentoPositivoFuente");
        Assert.Contains("NVL(TRIM(P.LSP_NO_DOCUMENTO), '__SIN_IDENTIFICACION__')", sql);
        Assert.Contains("NVL(TRIM(:numeroIdentificacion), '__SIN_IDENTIFICACION__')", sql);
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
