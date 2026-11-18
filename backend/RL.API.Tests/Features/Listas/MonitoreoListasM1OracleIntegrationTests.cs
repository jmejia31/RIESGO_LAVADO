using System.Diagnostics;
using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Oracle.ManagedDataAccess.Client;
using RL.API.Features.Auditoria.Persistence;
using RL.API.Features.Listas.Contracts;
using RL.API.Features.Listas.Persistence;
using RL.API.Infrastructure.Caching;
using RL.API.Infrastructure.Database;
using RL.API.Tests.Features.Identidad;
using RL.API.Tests.Support;
using Xunit;
using Xunit.Abstractions;

namespace RL.API.Tests.Features.Listas;

[Collection("MatricesRiesgosOracle")]
public sealed class MonitoreoListasM1OracleIntegrationTests
{
    private readonly ITestOutputHelper _output;

    public MonitoreoListasM1OracleIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [P0FOracleRequiredFact]
    [Trait("Category", "P0FOracleIntegration")]
    public async Task MonitoreoM1_AgrupacionDniYRecuperacionEmpleados_EnOracleXeDescartable()
    {
        string connectionString = OracleTestDatabaseProfile.RequireLocalXeConnection(
            Environment.GetEnvironmentVariable("P0F_ORACLE_CONNECTION_STRING"));
        ValidarDestinoDisposableXe(connectionString);

        OracleConnection.ClearAllPools();
        await using var connection = new OracleConnection(connectionString);
        await connection.OpenAsync();
        FixtureState fixture = await EnsureFixtureAsync(connection);

        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        using var appCache = new ApplicationMemoryCache(memoryCache);
        var auditoria = InterfaceStub.Create<IAuditoriaRepository>(out _);
        var repository = new ListasRepository(
            new OracleDbContext(connectionString),
            auditoria,
            new HttpContextAccessor(),
            appCache,
            new ApplicationCacheSettings { MonitoreoMetadataTtlSeconds = 5 });

        try
        {
            await SeedSyntheticDataAsync(connection);

            // M1-01, M1-02, M1-03, M1-04, M1-07, M1-08, M1-13, M1-19, M1-22:
            // Personas naturales identificadas agrupadas por DNI valido en SQL antes de paginar.
            var swNat = Stopwatch.StartNew();
            var natIdent = await repository.ObtenerNaturalesPaginadasAsync(new ConsultaMonitoreoPaginadaDto
            {
                Pagina = 1,
                TamanoPagina = 20,
                CategoriaIdentidad = "identificadas",
                Orden = "identidad",
                Direccion = "asc"
            });
            swNat.Stop();

            // Esperados en identificadas:
            // 1) M1-01 DNI unico 0801-1980-00001 (1 coincidencia)
            // 2) M1-02/M1-03 DNI multiple 0801-1981-00002 (3 coincidencias en 2 listas, 2 empresas)
            // 3) M1-04 DNI ambiguo 0801-1982-00003 (2 coincidencias con 2 sujetos distintos)
            // 4) M1-08 DNI sin empresa 0801-1983-00004 (1 coincidencia, fechas nulas M1-19)
            // 5) M1-07 Manual cumplimiento 0801-1984-00005 (manual puro)
            // 6) M1-09 Empleado con TIPO_PERSONA='SOCIO' (tambien figura en naturales salvo que sea clasificado IHSS)
            // EXPLICITAMENTE EXCLUIDO (M1-13): Empleado clasificado TIPO_PERSONA='EMPLEADO IHSS' (0801-1991-00011)
            Assert.DoesNotContain(natIdent.Items, x => x.NumeroIdentificacion == "0801-1991-00011");

            var m101 = Assert.Single(natIdent.Items, x => x.NumeroIdentificacion == "0801-1980-00001");
            Assert.Equal(1, m101.TotalRepetidos);
            Assert.Equal(EstadoIdentidadMonitoreo.Verified, m101.EstadoIdentidad);
            Assert.Equal(EstadoIdentidadMonitoreo.Verified, m101.EstadoAsociacion);

            var m102 = Assert.Single(natIdent.Items, x => x.NumeroIdentificacion == "0801-1981-00002");
            Assert.Equal(3, m102.TotalRepetidos);
            Assert.Contains("(+1)", m102.ListaCoincidencia, StringComparison.Ordinal);
            Assert.Equal(EstadoIdentidadMonitoreo.Verified, m102.EstadoIdentidad);
            Assert.Equal(EstadoIdentidadMonitoreo.Ambiguous, m102.EstadoAsociacion);

            var m104 = Assert.Single(natIdent.Items, x => x.NumeroIdentificacion == "0801-1982-00003");
            Assert.Equal(2, m104.TotalRepetidos);
            Assert.Equal(EstadoIdentidadMonitoreo.Ambiguous, m104.EstadoIdentidad);
            Assert.Equal(EstadoIdentidadMonitoreo.Ambiguous, m104.EstadoAsociacion);

            var m108 = Assert.Single(natIdent.Items, x => x.NumeroIdentificacion == "0801-1983-00004");
            Assert.Equal(1, m108.TotalRepetidos);
            Assert.Null(m108.FechaEncontro);
            Assert.Null(m108.FechaCalifico);
            Assert.Equal(EstadoIdentidadMonitoreo.UnverifiedAssociation, m108.EstadoIdentidad);

            var m107 = Assert.Single(natIdent.Items, x => x.NumeroIdentificacion == "0801-1984-00005");
            Assert.True(m107.EsManual);
            Assert.Equal("MANUAL_CUMPLIMIENTO", m107.TipoFuente);

            Assert.True(natIdent.Totales.TotalCoincidenciasFuente > natIdent.Totales.TotalRegistros);

            // M1-02 & M1-03: "Ver detalle" devuelve TODAS las coincidencias del grupo DNI y sus empresas verificadas
            var swDet = Stopwatch.StartNew();
            var detM102 = await repository.ObtenerDetalleNaturalAsync(m102.NumeroIdentificacion, m102.IdRegistroFuente, m102.TipoFuente);
            swDet.Stop();
            Assert.Equal(3, detM102.Select(d => d.IdRegistroFuente).Distinct().Count());
            Assert.Contains(detM102, d => d.NombreEmpresa == "EMPRESA ALPHA SA");
            Assert.Contains(detM102, d => d.NombreEmpresa == "EMPRESA BETA SA");
            Assert.Contains(detM102, d => d.ListaCoincidencia == "OFAC");
            Assert.Contains(detM102, d => d.ListaCoincidencia == "PEPS");

            // M1-04: DNI ambiguo mantiene aislamiento por sujeto y bloquea mutaciones
            var detM104 = await repository.ObtenerDetalleNaturalAsync(m104.NumeroIdentificacion, m104.IdRegistroFuente, m104.TipoFuente);
            Assert.Equal(2, detM104.Count);
            Assert.Contains(detM104, d => d.NombresPersona == "CARLOS UNO" && d.NombreEmpresa == "EMPRESA CARLOS UNO");
            Assert.Contains(detM104, d => d.NombresPersona == "CARLOS DOS" && d.NombreEmpresa == "EMPRESA CARLOS DOS");
            Assert.DoesNotContain(detM104, d => d.NombresPersona == "CARLOS UNO" && d.NombreEmpresa == "EMPRESA CARLOS DOS");
            var resAmbigua = await repository.ResolverIdentidadAsync("0801-1982-00003", TipoSujetoMonitoreo.Natural);
            Assert.Equal(EstadoIdentidadMonitoreo.Ambiguous, resAmbigua.Estado);
            Assert.False(resAmbigua.EsOperable);

            // M1-08: Natural sin empresa devuelve coincidencia sin empresa inventada
            var detM108 = await repository.ObtenerDetalleNaturalAsync(m108.NumeroIdentificacion, m108.IdRegistroFuente, m108.TipoFuente);
            var detM108Row = Assert.Single(detM108);
            Assert.Equal(string.Empty, detM108Row.NombreEmpresa);
            Assert.Equal(EstadoIdentidadMonitoreo.UnverifiedAssociation, detM108Row.EstadoAsociacion);

            // M1-05 & M1-06: Marcadores N/A(3) y NULL NO se agrupan entre si y se recuperan por ID fuente
            var natSinId = await repository.ObtenerNaturalesPaginadasAsync(new ConsultaMonitoreoPaginadaDto
            {
                Pagina = 1,
                TamanoPagina = 20,
                CategoriaIdentidad = "sin_identificacion"
            });
            var marcadoresNa = natSinId.Items.Where(x => x.NumeroIdentificacion == "N/A(3)").ToList();
            Assert.Equal(2, marcadoresNa.Count);
            Assert.All(marcadoresNa, row => Assert.Equal(1, row.TotalRepetidos));
            Assert.NotEqual(marcadoresNa[0].IdRegistroFuente, marcadoresNa[1].IdRegistroFuente);

            var detNa1 = await repository.ObtenerDetalleNaturalAsync("N/A(3)", marcadoresNa[0].IdRegistroFuente, "DNP_COINCIDENCIA");
            Assert.Single(detNa1);
            Assert.Equal(marcadoresNa[0].IdRegistroFuente, detNa1[0].IdRegistroFuente);
            await Assert.ThrowsAsync<AmbiguousIdentityException>(() => repository.ObtenerDetalleNaturalAsync("N/A(3)"));

            var filaNula = Assert.Single(natSinId.Items, x => string.IsNullOrEmpty(x.NumeroIdentificacion));
            var detNula = await repository.ObtenerDetalleNaturalAsync(string.Empty, filaNula.IdRegistroFuente, "DNP_COINCIDENCIA");
            Assert.Single(detNula);
            Assert.Equal(filaNula.IdRegistroFuente, detNula[0].IdRegistroFuente);

            // M1-14 & M1-15: Paginacion agrupada sin duplicados ni omisiones + busqueda por lista secundaria
            var p1 = await repository.ObtenerNaturalesPaginadasAsync(new ConsultaMonitoreoPaginadaDto
            {
                Pagina = 1, TamanoPagina = 2, CategoriaIdentidad = "identificadas", Orden = "identidad", Direccion = "asc"
            });
            var p2 = await repository.ObtenerNaturalesPaginadasAsync(new ConsultaMonitoreoPaginadaDto
            {
                Pagina = 2, TamanoPagina = 2, CategoriaIdentidad = "identificadas", Orden = "identidad", Direccion = "asc"
            });
            var p3 = await repository.ObtenerNaturalesPaginadasAsync(new ConsultaMonitoreoPaginadaDto
            {
                Pagina = 3, TamanoPagina = 2, CategoriaIdentidad = "identificadas", Orden = "identidad", Direccion = "asc"
            });
            var idsPaginados = p1.Items.Concat(p2.Items).Concat(p3.Items).Select(x => x.IdRegistroFuente).ToList();
            Assert.Equal(natIdent.TotalRegistros, idsPaginados.Count);
            Assert.Equal(idsPaginados.Count, idsPaginados.Distinct().Count());

            var busquedaSecundaria = await repository.ObtenerNaturalesPaginadasAsync(new ConsultaMonitoreoPaginadaDto
            {
                Pagina = 1, TamanoPagina = 10, CategoriaIdentidad = "identificadas", Buscar = "PEPS"
            });
            Assert.Contains(busquedaSecundaria.Items, x => x.NumeroIdentificacion == "0801-1981-00002");

            // M1-09, M1-10, M1-11, M1-12, M1-16: Empleados historicos recuperados sin filtro fragil '%IHSS'
            var swEmp = Stopwatch.StartNew();
            var empleados = await repository.ObtenerEmpleadosPaginadasAsync(new ConsultaMonitoreoPaginadaDto
            {
                Pagina = 1, TamanoPagina = 20, Orden = "identidad", Direccion = "asc"
            });
            swEmp.Stop();

            // M1-09 & M1-10: Empleado cuya TIPO_PERSONA es 'SOCIO' (NO '%IHSS') y aparece en 3 planillas historicas
            var empHist = Assert.Single(empleados.Items, x => x.Identidad == "0801-1990-00010");
            Assert.Equal(2, empHist.TotalRepetidos); // 2 coincidencias, NO multiplicadas por 3 planillas (6)!

            // M1-11: Empleado clasificado 'EMPLEADO IHSS' sin planilla vinculada
            var empSinPlanilla = Assert.Single(empleados.Items, x => x.Identidad == "0801-1991-00011");
            Assert.Equal(1, empSinPlanilla.TotalRepetidos);
            Assert.Equal(EstadoIdentidadMonitoreo.UnverifiedAssociation, empSinPlanilla.EstadoIdentidad);

            // M1-12: Empleado en planilla SIN coincidencia NO se etiqueta como positivo en Monitoreo
            Assert.DoesNotContain(empleados.Items, x => x.Identidad == "0801-1992-00012");

            // M1-16: Filtros mensuales exacto y por rango inclusivo sobre planillas historicas
            var empMesExacto = await repository.ObtenerEmpleadosPaginadasAsync(new ConsultaMonitoreoPaginadaDto
            {
                Pagina = 1, TamanoPagina = 20, PeriodoPlanilla = new DateTime(2026, 1, 1)
            });
            Assert.Single(empMesExacto.Items, x => x.Identidad == "0801-1990-00010");
            Assert.DoesNotContain(empMesExacto.Items, x => x.Identidad == "0801-1991-00011");

            var empRango = await repository.ObtenerEmpleadosPaginadasAsync(new ConsultaMonitoreoPaginadaDto
            {
                Pagina = 1, TamanoPagina = 20,
                PeriodoPlanillaDesde = new DateTime(2025, 11, 1),
                PeriodoPlanillaHasta = new DateTime(2026, 2, 1)
            });
            Assert.Single(empRango.Items, x => x.Identidad == "0801-1990-00010");

            var empFueraRango = await repository.ObtenerEmpleadosPaginadasAsync(new ConsultaMonitoreoPaginadaDto
            {
                Pagina = 1, TamanoPagina = 20,
                PeriodoPlanillaDesde = new DateTime(2024, 1, 1),
                PeriodoPlanillaHasta = new DateTime(2024, 12, 1)
            });
            Assert.Empty(empFueraRango.Items);

            // Detalle empleado recupera todos los periodos historicos y respeta filtro mensual
            var detEmpTodos = await repository.ObtenerDetalleEmpleadoAsync(empHist.Identidad, empHist.IdRegistroFuente, empHist.TipoFuente);
            Assert.Equal(3, detEmpTodos.Select(d => d.Periodo).Where(p => p.HasValue).Distinct().Count());
            var detEmpMes = await repository.ObtenerDetalleEmpleadoAsync(empHist.Identidad, empHist.IdRegistroFuente, empHist.TipoFuente, new DateTime(2026, 1, 1));
            Assert.All(detEmpMes, d => Assert.Equal(new DateTime(2026, 1, 1), d.Periodo));

            var detEmpSinPlanilla = await repository.ObtenerDetalleEmpleadoAsync(empSinPlanilla.Identidad, empSinPlanilla.IdRegistroFuente, empSinPlanilla.TipoFuente);
            Assert.Single(detEmpSinPlanilla);
            Assert.Contains("PLANILLA NO VINCULADA", detEmpSinPlanilla[0].TipoCondicionActuaDesc, StringComparison.OrdinalIgnoreCase);

            _output.WriteLine($"M1_ORACLE_XE_NATURALES_MS={swNat.ElapsedMilliseconds}");
            _output.WriteLine($"M1_ORACLE_XE_EMPLEADOS_MS={swEmp.ElapsedMilliseconds}");
            _output.WriteLine($"M1_ORACLE_XE_DETALLE_MS={swDet.ElapsedMilliseconds}");
            _output.WriteLine("M1_ORACLE_XE_SYNTHETIC_MATRIX=PASS");
        }
        finally
        {
            await CleanupFixtureAsync(connection, fixture);
        }
    }

    private static async Task SeedSyntheticDataAsync(OracleConnection connection)
    {
        await ExecuteAsync(connection, "INSERT INTO DNP_IHSS.TIPO_LISTAS_CAUTELA (TIPO_LISTA_CAUTELA_ID, LISTA_CAUTELA_DESCRICPION) VALUES (901, 'LISTA MANUAL M1')");

        // Socios / Representantes y vista V_SOCIOS_REPRESENTANTES
        await ExecuteAsync(connection, "INSERT INTO DNP_IHSS.SOCIOS (SOCIO_ID, NUMERO_IDENTIFICACION) VALUES (9001, '0801-1980-00001')");
        await ExecuteAsync(connection, "INSERT INTO DNP_IHSS.SOCIOS (SOCIO_ID, NUMERO_IDENTIFICACION) VALUES (9002, '0801-1981-00002')");
        await ExecuteAsync(connection, "INSERT INTO DNP_IHSS.SOCIOS (SOCIO_ID, NUMERO_IDENTIFICACION) VALUES (9003, '0801-1982-00003')");
        await ExecuteAsync(connection, "INSERT INTO DNP_IHSS.REPRESENTANTES (REPRESENTANTE_ID, NUMERO_IDENTIFICACION) VALUES (9004, '0801-1982-00003')");

        await ExecuteAsync(connection, "INSERT INTO DNP_IHSS.V_SOCIOS_REPRESENTANTES_M1 (NUMERO_IDENTIFICACION, NUMERO_PATRONAL, NOMBRES_PERSONA, TIPO_CONDICION_ACTUA_DESC, NOMBRE_EMPRESA, ES_PEP) VALUES ('0801-1980-00001', '1001001', 'ANA UNICA', 'SOCIO', 'EMPRESA UNICA SA', 'NO')");
        await ExecuteAsync(connection, "INSERT INTO DNP_IHSS.V_SOCIOS_REPRESENTANTES_M1 (NUMERO_IDENTIFICACION, NUMERO_PATRONAL, NOMBRES_PERSONA, TIPO_CONDICION_ACTUA_DESC, NOMBRE_EMPRESA, ES_PEP) VALUES ('0801-1981-00002', '2001001', 'MARIA MULTI', 'SOCIO', 'EMPRESA ALPHA SA', 'SI')");
        await ExecuteAsync(connection, "INSERT INTO DNP_IHSS.V_SOCIOS_REPRESENTANTES_M1 (NUMERO_IDENTIFICACION, NUMERO_PATRONAL, NOMBRES_PERSONA, TIPO_CONDICION_ACTUA_DESC, NOMBRE_EMPRESA, ES_PEP) VALUES ('0801-1981-00002', '2001002', 'MARIA MULTI', 'REPRESENTANTE LEGAL', 'EMPRESA BETA SA', 'NO')");
        await ExecuteAsync(connection, "INSERT INTO DNP_IHSS.V_SOCIOS_REPRESENTANTES_M1 (NUMERO_IDENTIFICACION, NUMERO_PATRONAL, NOMBRES_PERSONA, TIPO_CONDICION_ACTUA_DESC, NOMBRE_EMPRESA, ES_PEP) VALUES ('0801-1982-00003', '3001001', 'CARLOS UNO', 'SOCIO', 'EMPRESA CARLOS UNO', 'NO')");
        await ExecuteAsync(connection, "INSERT INTO DNP_IHSS.V_SOCIOS_REPRESENTANTES_M1 (NUMERO_IDENTIFICACION, NUMERO_PATRONAL, NOMBRES_PERSONA, TIPO_CONDICION_ACTUA_DESC, NOMBRE_EMPRESA, ES_PEP) VALUES ('0801-1982-00003', '3001002', 'CARLOS DOS', 'REPRESENTANTE LEGAL', 'EMPRESA CARLOS DOS', 'NO')");

        // Planillas historicas de empleados (3 periodos para 0801-1990-00010 + 1 empleado sin coincidencia 0801-1992-00012)
        await ExecuteAsync(connection, "INSERT INTO DNP_IHSS.V_EMPLEADOS_IHSS_PLANILLAS_M1 (IDENTIDAD, NUMEPATRO, NOMBRE, RAZOSOCI, NOMBRE_EMPLEADO, PERIODO) VALUES ('0801-1990-00010', '101196100011', 'IHSS', 'INSTITUTO HONDURENO DE SEGURIDAD SOCIAL', 'PEDRO EMPLEADO HISTORICO', DATE '2025-11-01')");
        await ExecuteAsync(connection, "INSERT INTO DNP_IHSS.V_EMPLEADOS_IHSS_PLANILLAS_M1 (IDENTIDAD, NUMEPATRO, NOMBRE, RAZOSOCI, NOMBRE_EMPLEADO, PERIODO) VALUES ('0801-1990-00010', '101196100011', 'IHSS', 'INSTITUTO HONDURENO DE SEGURIDAD SOCIAL', 'PEDRO EMPLEADO HISTORICO', DATE '2026-01-01')");
        await ExecuteAsync(connection, "INSERT INTO DNP_IHSS.V_EMPLEADOS_IHSS_PLANILLAS_M1 (IDENTIDAD, NUMEPATRO, NOMBRE, RAZOSOCI, NOMBRE_EMPLEADO, PERIODO) VALUES ('0801-1990-00010', '101196100011', 'IHSS', 'INSTITUTO HONDURENO DE SEGURIDAD SOCIAL', 'PEDRO EMPLEADO HISTORICO', DATE '2026-07-01')");
        await ExecuteAsync(connection, "INSERT INTO DNP_IHSS.V_EMPLEADOS_IHSS_PLANILLAS_M1 (IDENTIDAD, NUMEPATRO, NOMBRE, RAZOSOCI, NOMBRE_EMPLEADO, PERIODO) VALUES ('0801-1992-00012', '101196100011', 'IHSS', 'INSTITUTO HONDURENO DE SEGURIDAD SOCIAL', 'EMPLEADO SIN COINCIDENCIA', DATE '2026-07-01')");

        // Coincidencias fuente en DNP_IHSS.REPORTE_COINCIDENCIAS (imitando ETL real donde NUMERO_PATRONO es NULL para personas/empleados)
        await ExecuteAsync(connection, "INSERT INTO DNP_IHSS.REPORTE_COINCIDENCIAS (REPORTE_COINCIDENCIA_ID, DNI, NOMBRE, TIPO_PERSONA, LISTA_CONCIDENCIA, FECHA_ENCONTRO, FECHA_CALIFICO, NUMERO_PATRONO, TIPO_CALIFICACION_ID) VALUES (91001, '0801-1980-00001', 'ANA UNICA', 'SOCIO', 'OFAC', DATE '2026-06-01', DATE '2026-06-02', NULL, 1)");
        await ExecuteAsync(connection, "INSERT INTO DNP_IHSS.REPORTE_COINCIDENCIAS (REPORTE_COINCIDENCIA_ID, DNI, NOMBRE, TIPO_PERSONA, LISTA_CONCIDENCIA, FECHA_ENCONTRO, FECHA_CALIFICO, NUMERO_PATRONO, TIPO_CALIFICACION_ID) VALUES (91002, '0801-1981-00002', 'MARIA MULTI', 'SOCIO', 'OFAC', DATE '2026-05-01', DATE '2026-05-02', NULL, 1)");
        await ExecuteAsync(connection, "INSERT INTO DNP_IHSS.REPORTE_COINCIDENCIAS (REPORTE_COINCIDENCIA_ID, DNI, NOMBRE, TIPO_PERSONA, LISTA_CONCIDENCIA, FECHA_ENCONTRO, FECHA_CALIFICO, NUMERO_PATRONO, TIPO_CALIFICACION_ID) VALUES (91003, '0801-1981-00002', 'MARIA MULTI', 'SOCIO', 'PEPS', DATE '2026-06-10', DATE '2026-06-11', NULL, 1)");
        await ExecuteAsync(connection, "INSERT INTO DNP_IHSS.REPORTE_COINCIDENCIAS (REPORTE_COINCIDENCIA_ID, DNI, NOMBRE, TIPO_PERSONA, LISTA_CONCIDENCIA, FECHA_ENCONTRO, FECHA_CALIFICO, NUMERO_PATRONO, TIPO_CALIFICACION_ID) VALUES (91004, '0801-1981-00002', 'MARIA MULTI', 'REPRESENTANTE LEGAL', 'OFAC', DATE '2026-07-15', DATE '2026-07-16', NULL, 1)");
        await ExecuteAsync(connection, "INSERT INTO DNP_IHSS.REPORTE_COINCIDENCIAS (REPORTE_COINCIDENCIA_ID, DNI, NOMBRE, TIPO_PERSONA, LISTA_CONCIDENCIA, FECHA_ENCONTRO, FECHA_CALIFICO, NUMERO_PATRONO, TIPO_CALIFICACION_ID) VALUES (91005, '0801-1982-00003', 'CARLOS UNO', 'SOCIO', 'ONU', DATE '2026-04-01', NULL, NULL, 1)");
        await ExecuteAsync(connection, "INSERT INTO DNP_IHSS.REPORTE_COINCIDENCIAS (REPORTE_COINCIDENCIA_ID, DNI, NOMBRE, TIPO_PERSONA, LISTA_CONCIDENCIA, FECHA_ENCONTRO, FECHA_CALIFICO, NUMERO_PATRONO, TIPO_CALIFICACION_ID) VALUES (91006, '0801-1982-00003', 'CARLOS DOS', 'REPRESENTANTE LEGAL', 'ONU', DATE '2026-04-05', NULL, NULL, 1)");
        await ExecuteAsync(connection, "INSERT INTO DNP_IHSS.REPORTE_COINCIDENCIAS (REPORTE_COINCIDENCIA_ID, DNI, NOMBRE, TIPO_PERSONA, LISTA_CONCIDENCIA, FECHA_ENCONTRO, FECHA_CALIFICO, NUMERO_PATRONO, TIPO_CALIFICACION_ID) VALUES (91007, '0801-1983-00004', 'PERSONA SIN EMPRESA', 'SOCIO', 'ENGEL', NULL, NULL, NULL, 1)");
        await ExecuteAsync(connection, "INSERT INTO DNP_IHSS.REPORTE_COINCIDENCIAS (REPORTE_COINCIDENCIA_ID, DNI, NOMBRE, TIPO_PERSONA, LISTA_CONCIDENCIA, FECHA_ENCONTRO, FECHA_CALIFICO, NUMERO_PATRONO, TIPO_CALIFICACION_ID) VALUES (91008, 'N/A(3)', 'SUJETO MARCADOR A', 'SOCIO', 'OFAC', DATE '2026-03-01', NULL, NULL, 1)");
        await ExecuteAsync(connection, "INSERT INTO DNP_IHSS.REPORTE_COINCIDENCIAS (REPORTE_COINCIDENCIA_ID, DNI, NOMBRE, TIPO_PERSONA, LISTA_CONCIDENCIA, FECHA_ENCONTRO, FECHA_CALIFICO, NUMERO_PATRONO, TIPO_CALIFICACION_ID) VALUES (91009, 'N/A(3)', 'SUJETO MARCADOR B', 'SOCIO', 'PEPS', DATE '2026-03-02', NULL, NULL, 1)");
        await ExecuteAsync(connection, "INSERT INTO DNP_IHSS.REPORTE_COINCIDENCIAS (REPORTE_COINCIDENCIA_ID, DNI, NOMBRE, TIPO_PERSONA, LISTA_CONCIDENCIA, FECHA_ENCONTRO, FECHA_CALIFICO, NUMERO_PATRONO, TIPO_CALIFICACION_ID) VALUES (91010, NULL, 'SUJETO SIN DNI', 'SOCIO', 'ONU', DATE '2026-03-03', NULL, NULL, 1)");

        // Empleado historico cuyo TIPO_PERSONA NO coincide con '%IHSS' (escenario real que causaba EMPLEADOS=0)
        await ExecuteAsync(connection, "INSERT INTO DNP_IHSS.REPORTE_COINCIDENCIAS (REPORTE_COINCIDENCIA_ID, DNI, NOMBRE, TIPO_PERSONA, LISTA_CONCIDENCIA, FECHA_ENCONTRO, FECHA_CALIFICO, NUMERO_PATRONO, TIPO_CALIFICACION_ID) VALUES (91011, '0801-1990-00010', 'PEDRO EMPLEADO HISTORICO', 'SOCIO', 'OFAC', DATE '2025-11-15', DATE '2025-11-16', NULL, 1)");
        await ExecuteAsync(connection, "INSERT INTO DNP_IHSS.REPORTE_COINCIDENCIAS (REPORTE_COINCIDENCIA_ID, DNI, NOMBRE, TIPO_PERSONA, LISTA_CONCIDENCIA, FECHA_ENCONTRO, FECHA_CALIFICO, NUMERO_PATRONO, TIPO_CALIFICACION_ID) VALUES (91012, '0801-1990-00010', 'PEDRO EMPLEADO HISTORICO', 'SOCIO', 'PEPS', DATE '2026-01-20', DATE '2026-01-21', NULL, 1)");

        // Empleado clasificado 'EMPLEADO IHSS' sin planilla vinculada (M1-11 & M1-13)
        await ExecuteAsync(connection, "INSERT INTO DNP_IHSS.REPORTE_COINCIDENCIAS (REPORTE_COINCIDENCIA_ID, DNI, NOMBRE, TIPO_PERSONA, LISTA_CONCIDENCIA, FECHA_ENCONTRO, FECHA_CALIFICO, NUMERO_PATRONO, TIPO_CALIFICACION_ID) VALUES (91013, '0801-1991-00011', 'LAURA SIN PLANILLA', 'EMPLEADO IHSS', 'ONU', DATE '2026-02-10', NULL, NULL, 1)");

        // Positivo manual natural (M1-07)
        await ExecuteAsync(connection, "INSERT INTO RL_LISTA_POSITIVOS (LSP_POSITIVO_ID, LSP_NO_DOCUMENTO, LSP_NOMBRE_COMPLETO, LSP_TIPO_POSITIVO_ID, LSP_TIPO_LISTA_CAUTELA_ID, LSP_ORIGEN_REGISTRO, LSP_MOTIVO_INGRESO, LSP_FECHA_CREACION, LSP_ESTADO_REGISTRO) VALUES (92001, '0801-1984-00005', 'MANUAL CUMPLIMIENTO', 2, 901, 'MANUAL_CUMPLIMIENTO', 'Registro manual M1', DATE '2026-07-01', 1)");
    }

    private static async Task<FixtureState> EnsureFixtureAsync(OracleConnection connection)
    {
        bool createdUser = false;
        if (await ScalarAsync(connection, "SELECT COUNT(*) FROM ALL_USERS WHERE USERNAME = 'DNP_IHSS'") == 0)
        {
            await ExecuteAsync(connection, "CREATE USER DNP_IHSS IDENTIFIED BY \"M1SynthXe2026\" QUOTA UNLIMITED ON USERS");
            createdUser = true;
        }

        bool createdPositivos = false;
        if (await ScalarAsync(connection, "SELECT COUNT(*) FROM USER_TABLES WHERE TABLE_NAME = 'RL_LISTA_POSITIVOS'") == 0)
        {
            await ExecuteAsync(connection, @"
                CREATE TABLE RL_LISTA_POSITIVOS (
                    LSP_POSITIVO_ID NUMBER PRIMARY KEY,
                    LSP_NO_DOCUMENTO VARCHAR2(100),
                    LSP_NOMBRE_COMPLETO VARCHAR2(300),
                    LSP_TIPO_POSITIVO_ID NUMBER,
                    LSP_TIPO_LISTA_CAUTELA_ID NUMBER,
                    LSP_ORIGEN_REGISTRO VARCHAR2(100),
                    LSP_MOTIVO_INGRESO VARCHAR2(1000),
                    LSP_FECHA_CREACION DATE,
                    LSP_ESTADO_REGISTRO NUMBER
                )");
            createdPositivos = true;
        }

        await ExecuteAsync(connection, @"
            CREATE TABLE DNP_IHSS.TIPO_LISTAS_CAUTELA (
                TIPO_LISTA_CAUTELA_ID NUMBER PRIMARY KEY,
                LISTA_CAUTELA_DESCRICPION VARCHAR2(150)
            )");
        await ExecuteAsync(connection, @"
            CREATE TABLE DNP_IHSS.REPORTE_COINCIDENCIAS (
                REPORTE_COINCIDENCIA_ID NUMBER PRIMARY KEY,
                DNI VARCHAR2(50),
                NOMBRE VARCHAR2(300),
                TIPO_PERSONA VARCHAR2(75),
                LISTA_CONCIDENCIA VARCHAR2(100),
                FECHA_ENCONTRO DATE,
                FECHA_CALIFICO DATE,
                NUMERO_PATRONO VARCHAR2(50),
                TIPO_CALIFICACION_ID NUMBER
            )");
        await ExecuteAsync(connection, @"
            CREATE TABLE DNP_IHSS.SOCIOS (
                SOCIO_ID NUMBER PRIMARY KEY,
                NUMERO_IDENTIFICACION VARCHAR2(50)
            )");
        await ExecuteAsync(connection, @"
            CREATE TABLE DNP_IHSS.REPRESENTANTES (
                REPRESENTANTE_ID NUMBER PRIMARY KEY,
                NUMERO_IDENTIFICACION VARCHAR2(50)
            )");
        await ExecuteAsync(connection, @"
            CREATE TABLE DNP_IHSS.V_SOCIOS_REPRESENTANTES_M1 (
                NUMERO_IDENTIFICACION VARCHAR2(50),
                NUMERO_PATRONAL VARCHAR2(50),
                NOMBRES_PERSONA VARCHAR2(300),
                TIPO_CONDICION_ACTUA_DESC VARCHAR2(150),
                NOMBRE_EMPRESA VARCHAR2(300),
                ES_PEP VARCHAR2(20)
            )");
        await ExecuteAsync(connection, "CREATE OR REPLACE VIEW DNP_IHSS.V_SOCIOS_REPRESENTANTES AS SELECT * FROM DNP_IHSS.V_SOCIOS_REPRESENTANTES_M1");
        await ExecuteAsync(connection, @"
            CREATE TABLE DNP_IHSS.V_EMPLEADOS_IHSS_PLANILLAS_M1 (
                IDENTIDAD VARCHAR2(50),
                NUMEPATRO VARCHAR2(50),
                NOMBRE VARCHAR2(300),
                RAZOSOCI VARCHAR2(300),
                NOMBRE_EMPLEADO VARCHAR2(300),
                PERIODO DATE
            )");
        await ExecuteAsync(connection, "CREATE OR REPLACE VIEW DNP_IHSS.V_EMPLEADOS_IHSS_PLANILLAS AS SELECT * FROM DNP_IHSS.V_EMPLEADOS_IHSS_PLANILLAS_M1");

        return new FixtureState(createdUser, createdPositivos);
    }

    private static async Task CleanupFixtureAsync(OracleConnection connection, FixtureState fixture)
    {
        if (fixture.CreatedPositivos)
        {
            await TryExecuteAsync(connection, "DROP TABLE RL_LISTA_POSITIVOS PURGE");
        }
        else
        {
            await TryExecuteAsync(connection, "DELETE FROM RL_LISTA_POSITIVOS WHERE LSP_POSITIVO_ID = 92001");
        }

        await TryExecuteAsync(connection, "DROP VIEW DNP_IHSS.V_EMPLEADOS_IHSS_PLANILLAS");
        await TryExecuteAsync(connection, "DROP TABLE DNP_IHSS.V_EMPLEADOS_IHSS_PLANILLAS_M1 PURGE");
        await TryExecuteAsync(connection, "DROP VIEW DNP_IHSS.V_SOCIOS_REPRESENTANTES");
        await TryExecuteAsync(connection, "DROP TABLE DNP_IHSS.V_SOCIOS_REPRESENTANTES_M1 PURGE");
        await TryExecuteAsync(connection, "DROP TABLE DNP_IHSS.REPRESENTANTES PURGE");
        await TryExecuteAsync(connection, "DROP TABLE DNP_IHSS.SOCIOS PURGE");
        await TryExecuteAsync(connection, "DROP TABLE DNP_IHSS.REPORTE_COINCIDENCIAS PURGE");
        await TryExecuteAsync(connection, "DROP TABLE DNP_IHSS.TIPO_LISTAS_CAUTELA PURGE");

        if (fixture.CreatedUser)
        {
            await TryExecuteAsync(connection, "DROP USER DNP_IHSS CASCADE");
        }
    }

    private static void ValidarDestinoDisposableXe(string connectionString)
    {
        bool esLocal = connectionString.Contains("127.0.0.1", StringComparison.OrdinalIgnoreCase)
            || connectionString.Contains("localhost", StringComparison.OrdinalIgnoreCase);
        bool esXe = connectionString.Contains("SERVICE_NAME=XE", StringComparison.OrdinalIgnoreCase)
            || connectionString.Contains("/XE", StringComparison.OrdinalIgnoreCase);
        if (!esLocal || !esXe)
            throw new InvalidOperationException("La prueba M1 solo puede ejecutarse contra Oracle XE local descartable.");
    }

    private static async Task<long> ScalarAsync(OracleConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return Convert.ToInt64(await command.ExecuteScalarAsync(), CultureInfo.InvariantCulture);
    }

    private static async Task ExecuteAsync(OracleConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }

    private static async Task TryExecuteAsync(OracleConnection connection, string sql)
    {
        try
        {
            await ExecuteAsync(connection, sql);
        }
        catch (OracleException)
        {
            // Limpieza best-effort en Oracle XE descartable.
        }
    }

    private sealed record FixtureState(bool CreatedUser, bool CreatedPositivos);
}
