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
    public void Naturales_AgrupaDniValidoEnSql_YAislaMarcadoresPorClaveFuente()
    {
        var sql = Sql("ConstruirConsultaMonitoreoNaturales");
        Assert.Contains("REPORTE_GRUPOS AS", sql);
        Assert.Contains("'SRC:' || TO_CHAR(R.REPORTE_COINCIDENCIA_ID)", sql);
        Assert.Contains("'DNI:' || TRIM(TRANSLATE(R.DNI, CHR(9)||CHR(10)||CHR(13), '   '))", sql);
        Assert.Contains("MIN(R.REPORTE_COINCIDENCIA_ID) ID_REGISTRO_FUENTE", sql);
        Assert.Contains("COUNT(*) TOTAL_REPETIDOS", sql);
        Assert.Contains("LSP_POSITIVO_ID", sql);
        Assert.Contains("LSP_ORIGEN_REGISTRO", sql);
        Assert.Contains("POSITIVO_DNP", sql);
        Assert.Contains("NOT EXISTS (SELECT 1 FROM Coincidencias C", sql);
        Assert.Contains("SIN_IDENTIFICACION", sql);
        Assert.Contains("NUMERO_PATRONAL", sql);
        Assert.Contains("UPPER(TRIM(R.TIPO_PERSONA)) NOT LIKE '%IHSS%'", sql);
        Assert.DoesNotContain("GROUP BY DNI, LISTA_CONCIDENCIA", sql);
        Assert.DoesNotContain("ASOCIACIÓN NO VERIFICADA'\n", sql);
        Assert.DoesNotContain("COUNT(*) OVER", sql);
    }

    [Fact]
    public void Empleados_RecuperaHistoricosDesdePlanillasYClasificacion_SinMultiplicarPorPlanillas()
    {
        var sql = Sql("ConstruirConsultaMonitoreoEmpleados");
        Assert.Contains("V_EMPLEADOS_IHSS_PLANILLAS", sql);
        Assert.DoesNotContain("PLANILLA_ULTIMA", sql);
        Assert.DoesNotContain("FROM DNP_IHSS.V_EMPLEADOS_IHSS_PLANILLAS", sql);
        Assert.Contains("UPPER(TRIM(NVL(R.TIPO_PERSONA, ''))) LIKE '%IHSS%'", sql);
        Assert.Contains("UPPER(TRIM(NVL(R.TIPO_PERSONA, ''))) LIKE '%EMPLEAD%'", sql);
        Assert.Contains("REPORTE_GRUPOS AS", sql);
        Assert.Contains("'SRC:' || TO_CHAR(R.REPORTE_COINCIDENCIA_ID)", sql);
        Assert.Contains("'DNI:' || TRIM(TRANSLATE(R.DNI, CHR(9)||CHR(10)||CHR(13), '   '))", sql);
        Assert.Contains("POSITIVO_DNP", sql);
        Assert.Contains("LSP_ORIGEN_REGISTRO", sql);
        Assert.Contains("NOT EXISTS (SELECT 1 FROM Coincidencias C", sql);
        Assert.DoesNotContain("JOIN DNP_IHSS.REPORTE_COINCIDENCIAS R ON D.IDENTIDAD", sql);
        Assert.DoesNotContain("COUNT(*) OVER", sql);
        Assert.DoesNotContain("V.TIPO_PERSONA LIKE '%IHSS'", sql);
    }


    [Fact]
    public void RangoPlanilla_UsaExistsConBordesMensualesInclusivos()
    {
        var method = typeof(ListasRepository).GetMethod("AppendMonitoringFilters",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);
        object[] args =
        {
            "SELECT f.* FROM (SELECT * FROM DNP_IHSS.V_EMPLEADOS_IHSS_PLANILLAS) f WHERE 1 = 1",
            new ConsultaMonitoreoPaginadaDto
            {
                PeriodoPlanillaDesde = new DateTime(2026, 1, 1),
                PeriodoPlanillaHasta = new DateTime(2026, 7, 1)
            }
        };
        method.Invoke(null, args);
        var sql = Assert.IsType<string>(args[0]);
        Assert.Contains("AND EXISTS (SELECT 1 FROM DNP_IHSS.V_EMPLEADOS_IHSS_PLANILLAS M1P", sql);
        Assert.Contains("M1P.IDENTIDAD = TRIM(TRANSLATE(f.IDENTIDAD, CHR(9)||CHR(10)||CHR(13), '   '))", sql);
        Assert.DoesNotContain("TRIM(M1P.NUMEPATRO) = TRIM(f.NUMERO_PATRONO)", sql);
        Assert.Contains("TRUNC(:periodoPlanillaDesde,'MM')", sql);
        Assert.Contains("ADD_MONTHS(TRUNC(:periodoPlanillaHasta,'MM'),1)", sql);
        Assert.DoesNotContain(":periodoPlanilla,'MM'", sql);
    }

    [Fact]
    public void BusquedaEnGrupos_BuscaTambienMiembrosSecundariosDelGrupoDni()
    {
        var method = typeof(ListasRepository).GetMethod("AppendMonitoringFilters",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);
        object[] args =
        {
            "SELECT f.* FROM (SELECT * FROM REPORTE_GRUPOS) f WHERE 1 = 1",
            new ConsultaMonitoreoPaginadaDto { Buscar = "PEPS" }
        };
        method.Invoke(null, args);
        var sql = Assert.IsType<string>(args[0]);
        Assert.Contains("FROM DNP_IHSS.REPORTE_COINCIDENCIAS RB", sql);
        Assert.Contains("TRIM(TRANSLATE(RB.DNI, CHR(9)||CHR(10)||CHR(13), '   ')) = TRIM(TRANSLATE(f.NUMERO_IDENTIFICACION, CHR(9)||CHR(10)||CHR(13), '   '))", sql);
    }

    [Fact]
    public void PositivosDnpConMarcadores_NoSeOcultanPorCompartirDocumento()
    {
        foreach (var method in new[] { "ConstruirConsultaMonitoreoNaturales", "ConstruirConsultaMonitoreoEmpleados" })
        {
            var sql = Sql(method);
            Assert.Contains("LP.LSP_NO_DOCUMENTO IS NULL", sql);
            Assert.Contains("UPPER(TRIM(TRANSLATE(LP.LSP_NO_DOCUMENTO, CHR(9)||CHR(10)||CHR(13), '   '))) LIKE 'N/A%'", sql);
            Assert.Contains("UPPER(TRIM(TRANSLATE(LP.LSP_NO_DOCUMENTO, CHR(9)||CHR(10)||CHR(13), '   '))) IN ('NULL'", sql);
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
            Assert.Contains("G.TOTAL_REPETIDOS = 1", sql);
            Assert.Contains("G.NOMBRES_DISTINTOS <= 1", sql);
            Assert.Contains("G.TOTAL_REPETIDOS COINCIDENCIAS_FUENTE", sql);
            Assert.DoesNotContain("COUNT(*) OVER", sql);
        }
    }

    [Fact]
    public void DetallePorReferenciaManual_AdmiteDocumentoNuloOSinIdentificacion()
    {
        var sql = Sql("CompararDocumentoPositivoFuente");
        Assert.Contains("NVL(TRIM(TRANSLATE(P.LSP_NO_DOCUMENTO, CHR(9)||CHR(10)||CHR(13), '   ')), '__SIN_IDENTIFICACION__')", sql);
        Assert.Contains("NVL(TRIM(TRANSLATE(:numeroIdentificacion, CHR(9)||CHR(10)||CHR(13), '   ')), '__SIN_IDENTIFICACION__')", sql);
    }

    [Fact]
    public void P0_ClasificacionNaturalEmpleado_EsComplementariaYSinVistaRemotaEnListado()
    {
        var field = typeof(ListasRepository).GetField("CondicionEsEmpleadoIhss",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(field);
        var esEmpleado = Assert.IsType<string>(field.GetValue(null));
        Assert.DoesNotContain("V_EMPLEADOS_IHSS_PLANILLAS", esEmpleado);
        Assert.Contains("LIKE '%IHSS%'", esEmpleado);
        Assert.Contains("LIKE '%EMPLEAD%'", esEmpleado);

        var empleados = Sql("ConstruirConsultaMonitoreoEmpleados");
        Assert.DoesNotContain("FROM DNP_IHSS.V_EMPLEADOS_IHSS_PLANILLAS", empleados);
        Assert.DoesNotContain("JOIN DNP_IHSS.V_EMPLEADOS_IHSS_PLANILLAS", empleados);
        var naturales = Sql("ConstruirConsultaMonitoreoNaturales");
        Assert.Contains("NOT LIKE '%IHSS%'", naturales);
        Assert.Contains("NOT LIKE '%EMPLEAD%'", naturales);
    }

    [Fact]
    public void P0_NormalizacionDocumento_EliminaTabulacionesYSaltosDeLinea()
    {
        foreach (var method in new[] { "ConstruirConsultaMonitoreoNaturales", "ConstruirConsultaMonitoreoEmpleados" })
        {
            var sql = Sql(method);
            Assert.Contains("TRANSLATE(R.DNI, CHR(9)||CHR(10)||CHR(13), '   ')", sql);
            Assert.DoesNotContain("'DNI:' || TRIM(R.DNI)", sql);
        }
    }

    [Fact]
    public void P0_DetalleNatural_UnaFilaPorCoincidenciaSinProductoCartesianoPorEmpresas()
    {
        var root = AppContext.BaseDirectory;
        while (!File.Exists(Path.Combine(root, "RIESGO_LAVADO.sln")))
            root = Directory.GetParent(root)!.FullName;
        var source = File.ReadAllText(Path.Combine(root, "backend/RL.API/Features/Listas/Persistence/ListasRepository.cs"));
        var start = source.IndexOf("public async Task<List<DetalleCoincidenciaNaturalDto>> ObtenerDetalleNaturalAsync", StringComparison.Ordinal);
        var end = source.IndexOf("public async Task<List<DetalleCoincidenciaEmpleadoDto>> ObtenerDetalleEmpleadoAsync", start, StringComparison.Ordinal);
        Assert.True(start >= 0 && end > start);
        var detalle = source[start..end];
        Assert.Contains("SOCIOS_RESUMEN", detalle);
        Assert.Contains("S.FILAS_DNI = 1 AND S.SUJETOS_DNI = 1", detalle);
        Assert.Contains("S.CANDIDATOS_NOMBRE = 1", detalle);
        Assert.Contains("WHEN NVL(SR.TOTAL_VINCULOS, 0) > 1 THEN 'AMBIGUOUS'", detalle);
        Assert.DoesNotContain("ROWNUM = 1", detalle);
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

    [Theory]
    [InlineData("activos", "AND NVL(f.ES_CERRADO_PASIVO, 0) = 0")]
    [InlineData("pendiente", "AND f.TIENE_MOTIVO = 0 AND NVL(f.ES_CERRADO_PASIVO, 0) = 0")]
    [InlineData("con_motivo", "AND f.TIENE_MOTIVO = 1 AND NVL(f.ES_CERRADO_PASIVO, 0) = 0")]
    [InlineData("manual", "AND f.ES_MANUAL = 1 AND NVL(f.ES_CERRADO_PASIVO, 0) = 0")]
    [InlineData("cerrado_pasivo", "AND NVL(f.ES_CERRADO_PASIVO, 0) = 1")]
    [InlineData("otro_estado", "AND ESTADO_MONITOREO = :estado")]
    public void FiltrosEstado_GeneranCondicionesExactasEnSqlYDefaultSeguro(string estado, string condicionEsperada)
    {
        var method = typeof(ListasRepository).GetMethod("AppendMonitoringFilters",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);
        object[] args =
        {
            "SELECT f.* FROM (SELECT 1 AS ID) f WHERE 1 = 1",
            new ConsultaMonitoreoPaginadaDto { Estado = estado }
        };
        method.Invoke(null, args);
        var sql = Assert.IsType<string>(args[0]);
        Assert.Contains(condicionEsperada, sql, StringComparison.Ordinal);
    }

    [Fact]
    public void FiltroEstadoTodos_NoAgregaCondicionDeEstado()
    {
        var method = typeof(ListasRepository).GetMethod("AppendMonitoringFilters",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);
        object[] args =
        {
            "SELECT f.* FROM (SELECT 1 AS ID) f WHERE 1 = 1",
            new ConsultaMonitoreoPaginadaDto { Estado = "todos" }
        };
        method.Invoke(null, args);
        var sql = Assert.IsType<string>(args[0]);
        Assert.DoesNotContain("ESTADO_MONITOREO", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("ES_CERRADO_PASIVO", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void ConstruirConsultaMonitoreoContextual_OmiteFiltroDeEstadoYConservaFiltrosGenerales()
    {
        var method = typeof(ListasRepository).GetMethod("ConstruirConsultaMonitoreoContextual",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);
        var baseSql = "SELECT '0801' AS NUMERO_IDENTIFICACION, 'JUAN' AS NOMBRE, 0 AS TIENE_MOTIVO, 0 AS ES_MANUAL FROM DUAL";
        var consulta = new ConsultaMonitoreoPaginadaDto
        {
            Buscar = "JUAN",
            Estado = "con_motivo"
        };
        var contextualSql = Assert.IsType<string>(method.Invoke(null, new object[] { baseSql, consulta }));

        Assert.Contains("UPPER(NOMBRE) LIKE", contextualSql);
        Assert.DoesNotContain("AND f.TIENE_MOTIVO = 1", contextualSql);
        Assert.DoesNotContain("AND ESTADO_MONITOREO = :estado", contextualSql);
    }
}
