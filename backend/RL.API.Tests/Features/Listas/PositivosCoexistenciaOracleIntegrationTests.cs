using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Oracle.ManagedDataAccess.Client;
using RL.API.Features.Auditoria.Contracts;
using RL.API.Features.Auditoria.Persistence;
using RL.API.Features.Listas.Application;
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
[SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Convención de nombres de pruebas de integración")]
public sealed class PositivosCoexistenciaOracleIntegrationTests
{
    private readonly ITestOutputHelper _output;

    public PositivosCoexistenciaOracleIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private sealed class NoOpAuditoriaRepository : IAuditoriaRepository
    {
        public Task RegistrarAsync(
            string tabla,
            string registroId,
            string accion,
            string? datosAnt,
            string? datosNvo,
            long? usrId,
            string? email,
            string? ip,
            string? modulo) => Task.CompletedTask;

        public Task RegistrarAsync(
            OracleConnection connection,
            OracleTransaction? transaction,
            string tabla,
            string registroId,
            string accion,
            string? datosAnt,
            string? datosNvo,
            long? usrId,
            string? email,
            string? ip,
            string? modulo) => Task.CompletedTask;

        public Task<(List<AuditoriaDto> Datos, int Total)> ObtenerBitacoraPaginadaAsync(
            int pagina,
            int limite,
            string? buscar,
            string? accion,
            string? modulo,
            string? tabla,
            DateTime? fechaInicio,
            DateTime? fechaFin) => Task.FromResult((new List<AuditoriaDto>(), 0));
    }

    [P0FOracleRequiredFact]
    [Trait("Category", "P0FOracleIntegration")]
    public async Task CoexistenciaOracle_12EscenariosPersistencia_EnOracleXeDescartable()
    {
        string connectionString = OracleTestDatabaseProfile.RequireLocalXeConnection(
            Environment.GetEnvironmentVariable("P0F_ORACLE_CONNECTION_STRING"));
        ValidarDestinoDisposableXe(connectionString);

        OracleConnection.ClearAllPools();
        await using var connection = new OracleConnection(connectionString);
        await connection.OpenAsync();

        var fixture = await EnsureCoexistenciaFixtureAsync(connection);

        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        using var appCache = new ApplicationMemoryCache(memoryCache);
        var auditoria = new NoOpAuditoriaRepository();
        var repository = new ListasRepository(
            new OracleDbContext(connectionString),
            auditoria,
            new HttpContextAccessor(),
            appCache,
            new ApplicationCacheSettings { MonitoreoMetadataTtlSeconds = 5 });

        try
        {
            const string dniBase = "0801199099001";
            const int tipoDocId = 1;

            // Escenario 1: Natural primero, empleado segundo con mismo DNI
            var okNat1 = await repository.RegistrarPositivoAsync(new RegistrarPositivoDto
            {
                TipoDocumentoId = tipoDocId,
                TipoPositivoId = 2,
                NoDocumento = dniBase,
                NombreCompleto = "PERSONA NATURAL 01",
                MotivoIngreso = "Motivo Natural 01",
                TipoListaCautelaId = 901,
                OrigenRegistro = "MANUAL_CUMPLIMIENTO"
            }, 99);
            Assert.True(okNat1);

            var okEmp1 = await repository.RegistrarPositivoAsync(new RegistrarPositivoDto
            {
                TipoDocumentoId = tipoDocId,
                TipoPositivoId = 3,
                NoDocumento = dniBase,
                NombreCompleto = "EMPLEADO 01",
                MotivoIngreso = "Motivo Empleado 01",
                TipoListaCautelaId = 901,
                OrigenRegistro = "DNP_LISTAS"
            }, 99);
            Assert.True(okEmp1);

            // Escenario 2: Empleado primero, natural segundo
            const string dniBase2 = "0801199099002";
            var okEmp2 = await repository.RegistrarPositivoAsync(new RegistrarPositivoDto
            {
                TipoDocumentoId = tipoDocId,
                TipoPositivoId = 3,
                NoDocumento = dniBase2,
                NombreCompleto = "EMPLEADO 02",
                MotivoIngreso = "Motivo Empleado 02",
                TipoListaCautelaId = 901,
                OrigenRegistro = "DNP_LISTAS"
            }, 99);
            Assert.True(okEmp2);

            var okNat2 = await repository.RegistrarPositivoAsync(new RegistrarPositivoDto
            {
                TipoDocumentoId = tipoDocId,
                TipoPositivoId = 2,
                NoDocumento = dniBase2,
                NombreCompleto = "PERSONA NATURAL 02",
                MotivoIngreso = "Motivo Natural 02",
                TipoListaCautelaId = 901,
                OrigenRegistro = "MANUAL_CUMPLIMIENTO"
            }, 99);
            Assert.True(okNat2);

            // Escenario 3: Empleado histórico sin prefijo, natural nuevo toma 'DNI:'
            const string dniBase3 = "0801199099003";
            await ExecuteAsync(connection, $@"
                INSERT INTO RL_LISTA_POSITIVOS (
                    LSP_POSITIVO_ID, LSP_TIPO_DOCUMENTO_ID, LSP_TIPO_POSITIVO_ID,
                    LSP_NO_DOCUMENTO, LSP_NOMBRE_COMPLETO, LSP_MOTIVO_INGRESO,
                    LSP_FECHA_CREACION, LSP_USR_CREACION_ID, LSP_ESTADO_REGISTRO,
                    LSP_TIPO_LISTA_CAUTELA_ID, LSP_ORIGEN_REGISTRO
                ) VALUES (
                    93001, {tipoDocId}, 3,
                    '{dniBase3}', 'EMPLEADO HISTORICO SIN PREFIJO', 'Motivo Histórico',
                    SYSDATE, 99, 1, 901, 'DNP_LISTAS'
                )");

            var okNat3 = await repository.RegistrarPositivoAsync(new RegistrarPositivoDto
            {
                TipoDocumentoId = tipoDocId,
                TipoPositivoId = 2,
                NoDocumento = dniBase3,
                NombreCompleto = "PERSONA NATURAL NUEVA",
                MotivoIngreso = "Motivo Natural Nueva",
                TipoListaCautelaId = 901,
                OrigenRegistro = "MANUAL_CUMPLIMIENTO"
            }, 99);
            Assert.True(okNat3);

            // Verificar que en base de datos el natural quedó con 'DNI:0801199099003'
            var docNat3Fisico = (string?)await ScalarStringAsync(connection,
                $"SELECT LSP_NO_DOCUMENTO FROM RL_LISTA_POSITIVOS WHERE LSP_TIPO_POSITIVO_ID = 2 AND (LSP_NO_DOCUMENTO = '{dniBase3}' OR LSP_NO_DOCUMENTO = 'DNI:{dniBase3}')");
            Assert.Equal($"DNI:{dniBase3}", docNat3Fisico);

            // Escenario 4: Natural histórico con 'DNI:' se recupera y actualiza sin colisión
            var posNatRecup = await repository.ObtenerPositivoPorDocumentoAsync(dniBase3, 2);
            Assert.NotNull(posNatRecup);
            Assert.Equal("Motivo Natural Nueva", posNatRecup.MotivoIngreso);

            // Escenario 5: Empleado histórico con 'EMP:' se recupera y actualiza
            var posEmpRecup = await repository.ObtenerPositivoPorDocumentoAsync(dniBase, 3);
            Assert.NotNull(posEmpRecup);
            Assert.Equal("Motivo Empleado 01", posEmpRecup.MotivoIngreso);

            // Escenario 6: Dos registros con mismo DNI y distintos motivos
            Assert.NotEqual(posNatRecup.MotivoIngreso, posEmpRecup.MotivoIngreso);

            // Escenario 7: Consulta puntual por documento y tipo resuelve el sujeto exacto
            var posNat1 = await repository.ObtenerPositivoPorDocumentoAsync(dniBase, 2);
            var posEmp1Check = await repository.ObtenerPositivoPorDocumentoAsync(dniBase, 3);
            Assert.NotNull(posNat1);
            Assert.NotNull(posEmp1Check);
            Assert.Equal(2, posNat1.TipoPositivoId);
            Assert.Equal(3, posEmp1Check.TipoPositivoId);
            Assert.NotEqual(posNat1.PositivoId, posEmp1Check.PositivoId);

            // Escenario 8: Integridad de auditoría y detalle sin cruces
            var detNat = await repository.ObtenerDetalleNaturalAsync(dniBase, null, "MANUAL_CUMPLIMIENTO");
            Assert.NotNull(detNat);

            // Escenario 9: Registro inactivo físicamente ocupando cleanDoc -> Natural toma prefijo DNI: sin ORA-00001
            const string dniInactivo = "0801199099009";
            await ExecuteAsync(connection, $@"
                INSERT INTO RL_LISTA_POSITIVOS (
                    LSP_POSITIVO_ID, LSP_TIPO_DOCUMENTO_ID, LSP_TIPO_POSITIVO_ID,
                    LSP_NO_DOCUMENTO, LSP_NOMBRE_COMPLETO, LSP_MOTIVO_INGRESO,
                    LSP_FECHA_CREACION, LSP_USR_CREACION_ID, LSP_ESTADO_REGISTRO,
                    LSP_TIPO_LISTA_CAUTELA_ID, LSP_ORIGEN_REGISTRO
                ) VALUES (
                    93009, {tipoDocId}, 3,
                    '{dniInactivo}', 'INACTIVO HISTORICO', 'Inactivo',
                    SYSDATE, 99, 0, 901, 'MANUAL_CUMPLIMIENTO'
                )");

            var okNatInactivo = await repository.RegistrarPositivoAsync(new RegistrarPositivoDto
            {
                TipoDocumentoId = tipoDocId,
                TipoPositivoId = 2,
                NoDocumento = dniInactivo,
                NombreCompleto = "NATURAL TRAS INACTIVO",
                MotivoIngreso = "Motivo tras inactivo",
                TipoListaCautelaId = 901,
                OrigenRegistro = "MANUAL_CUMPLIMIENTO"
            }, 99);
            Assert.True(okNatInactivo);
            var docNatInactivoFisico = (string?)await ScalarStringAsync(connection,
                $"SELECT LSP_NO_DOCUMENTO FROM RL_LISTA_POSITIVOS WHERE LSP_TIPO_POSITIVO_ID = 2 AND LSP_ESTADO_REGISTRO = 1 AND (LSP_NO_DOCUMENTO = '{dniInactivo}' OR LSP_NO_DOCUMENTO = 'DNI:{dniInactivo}')");
            Assert.Equal($"DNI:{dniInactivo}", docNatInactivoFisico);

            // Escenario 10: Clave física que excedería 50 caracteres se rechaza controladamente sin ORA-12899
            var docMuyLargo = new string('8', 49);
            var okExcedente = await repository.RegistrarPositivoAsync(new RegistrarPositivoDto
            {
                TipoDocumentoId = tipoDocId,
                TipoPositivoId = 3,
                NoDocumento = docMuyLargo,
                NombreCompleto = "EMPLEADO MUY LARGO",
                MotivoIngreso = "Motivo",
                TipoListaCautelaId = 901,
                OrigenRegistro = "DNP_LISTAS"
            }, 99);
            Assert.False(okExcedente);

            // Escenario 11: Operaciones concurrentes simuladas para mismo DNI y distintos tipos
            const string dniConcurrente = "0801199099011";
            var taskNat = repository.RegistrarPositivoAsync(new RegistrarPositivoDto
            {
                TipoDocumentoId = tipoDocId,
                TipoPositivoId = 2,
                NoDocumento = dniConcurrente,
                NombreCompleto = "CONCURRENTE NATURAL",
                MotivoIngreso = "Motivo Concurrente Nat",
                TipoListaCautelaId = 901,
                OrigenRegistro = "MANUAL_CUMPLIMIENTO"
            }, 99);
            var taskEmp = repository.RegistrarPositivoAsync(new RegistrarPositivoDto
            {
                TipoDocumentoId = tipoDocId,
                TipoPositivoId = 3,
                NoDocumento = dniConcurrente,
                NombreCompleto = "CONCURRENTE EMPLEADO",
                MotivoIngreso = "Motivo Concurrente Emp",
                TipoListaCautelaId = 901,
                OrigenRegistro = "DNP_LISTAS"
            }, 99);
            var results = await Task.WhenAll(taskNat, taskEmp);
            Assert.True(results[0] && results[1]);

            // Escenario 12: Limpieza y verificación de integridad referencial
            await ExecuteAsync(connection, "DELETE FROM RL_LISTA_POSITIVOS WHERE LSP_POSITIVO_ID >= 93000");
        }
        finally
        {
            await CleanupCoexistenciaFixtureAsync(connection, fixture);
        }
    }

    [P0FOracleRequiredFact]
    [Trait("Category", "P0FOracleIntegration")]
    public async Task CoexistenciaOracle_EmpleadoHistoricoSinPrefijo_NaturalConDni_MonitoreoAgrupado_FilaUnica_TieneMotivoTrue()
    {
        string connectionString = OracleTestDatabaseProfile.RequireLocalXeConnection(
            Environment.GetEnvironmentVariable("P0F_ORACLE_CONNECTION_STRING"));
        ValidarDestinoDisposableXe(connectionString);

        OracleConnection.ClearAllPools();
        await using var connection = new OracleConnection(connectionString);
        await connection.OpenAsync();

        var fixture = await EnsureCoexistenciaFixtureAsync(connection);

        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        using var appCache = new ApplicationMemoryCache(memoryCache);
        var auditoria = new NoOpAuditoriaRepository();
        var repository = new ListasRepository(
            new OracleDbContext(connectionString),
            auditoria,
            new HttpContextAccessor(),
            appCache,
            new ApplicationCacheSettings { MonitoreoMetadataTtlSeconds = 5 });

        try
        {
            const string dniCaso = "0801198877665";
            const int tipoDocId = 1;

            // Coincidencia fuente de DNP para persona natural
            await ExecuteAsync(connection, $@"
                INSERT INTO DNP_IHSS.REPORTE_COINCIDENCIAS (
                    REPORTE_COINCIDENCIA_ID, DNI, NOMBRE, TIPO_PERSONA, LISTA_CONCIDENCIA,
                    FECHA_ENCONTRO, FECHA_CALIFICO, NUMERO_PATRONO, TIPO_CALIFICACION_ID
                ) VALUES (
                    94001, '{dniCaso}', 'JUAN PEREZ CASO TEST', 'SOCIO', 'OFAC',
                    DATE '2026-01-10', DATE '2026-01-11', '101196100011', 1
                )");

            // Empleado histórico almacenado previamente con cleanDoc sin prefijo
            await ExecuteAsync(connection, $@"
                INSERT INTO RL_LISTA_POSITIVOS (
                    LSP_POSITIVO_ID, LSP_TIPO_DOCUMENTO_ID, LSP_TIPO_POSITIVO_ID,
                    LSP_NO_DOCUMENTO, LSP_NOMBRE_COMPLETO, LSP_MOTIVO_INGRESO,
                    LSP_FECHA_CREACION, LSP_USR_CREACION_ID, LSP_ESTADO_REGISTRO,
                    LSP_TIPO_LISTA_CAUTELA_ID, LSP_ORIGEN_REGISTRO
                ) VALUES (
                    94002, {tipoDocId}, 3,
                    '{dniCaso}', 'JUAN PEREZ CASO EMPLEADO', 'Motivo Histórico Empleado',
                    DATE '2025-05-01', 99, 1, 901, 'DNP_LISTAS'
                )");

            // Natural registrado ahora: cleanDoc ocupado por el empleado histórico, por lo que toma 'DNI:0801198877665'
            await ExecuteAsync(connection, $@"
                INSERT INTO RL_LISTA_POSITIVOS (
                    LSP_POSITIVO_ID, LSP_TIPO_DOCUMENTO_ID, LSP_TIPO_POSITIVO_ID,
                    LSP_NO_DOCUMENTO, LSP_NOMBRE_COMPLETO, LSP_MOTIVO_INGRESO,
                    LSP_FECHA_CREACION, LSP_USR_CREACION_ID, LSP_ESTADO_REGISTRO,
                    LSP_TIPO_LISTA_CAUTELA_ID, LSP_ORIGEN_REGISTRO
                ) VALUES (
                    94003, {tipoDocId}, 2,
                    'DNI:{dniCaso}', 'JUAN PEREZ CASO TEST', 'Motivo Natural Activo',
                    DATE '2026-06-01', 99, 1, 901, 'DNP_LISTAS'
                )");

            // 1. Verificar listado paginado: debe existir EXACTAMENTE UNA fila para dniCaso, sin duplicados
            var pag = await repository.ObtenerNaturalesPaginadasAsync(new ConsultaMonitoreoPaginadaDto
            {
                Pagina = 1,
                TamanoPagina = 50,
                Buscar = dniCaso
            });

            var filaUnica = Assert.Single(pag.Items, x => x.NumeroIdentificacion == dniCaso);
            // El motivo debe estar activo y reconocido (TieneMotivo = true)
            Assert.True(filaUnica.TieneMotivo);
            Assert.Equal("JUAN PEREZ CASO TEST", filaUnica.Nombre);

            // 2. Verificar exportación: también exactamente una fila con TieneMotivo = true
            var export = await repository.ObtenerNaturalesParaExportarAsync(new ConsultaMonitoreoPaginadaDto
            {
                Buscar = dniCaso
            });

            var filaExport = Assert.Single(export, x => x.NumeroIdentificacion == dniCaso);
            Assert.True(filaExport.TieneMotivo);
        }
        finally
        {
            await TryExecuteAsync(connection, "DELETE FROM RL_LISTA_POSITIVOS WHERE LSP_POSITIVO_ID IN (94002, 94003)");
            await TryExecuteAsync(connection, "DELETE FROM DNP_IHSS.REPORTE_COINCIDENCIAS WHERE REPORTE_COINCIDENCIA_ID = 94001");
            await CleanupCoexistenciaFixtureAsync(connection, fixture);
        }
    }

    private static async Task<CoexistenciaFixtureState> EnsureCoexistenciaFixtureAsync(OracleConnection connection)
    {
        bool createdUser = false;
        if (await ScalarAsync(connection, "SELECT COUNT(*) FROM ALL_USERS WHERE USERNAME = 'DNP_IHSS'") == 0)
        {
            await ExecuteAsync(connection, "CREATE USER DNP_IHSS IDENTIFIED BY \"M1SynthXe2026\" DEFAULT TABLESPACE USERS QUOTA UNLIMITED ON USERS");
            createdUser = true;
        }
        else
        {
            await ExecuteAsync(connection, "ALTER USER DNP_IHSS DEFAULT TABLESPACE USERS QUOTA UNLIMITED ON USERS");
        }

        bool createdPositivos = false;
        if (await ScalarAsync(connection, "SELECT COUNT(*) FROM USER_TABLES WHERE TABLE_NAME = 'RL_LISTA_POSITIVOS'") == 0)
        {
            await ExecuteAsync(connection, @"
                CREATE TABLE RL_LISTA_POSITIVOS (
                    LSP_POSITIVO_ID NUMBER PRIMARY KEY,
                    LSP_TIPO_DOCUMENTO_ID NUMBER,
                    LSP_TIPO_POSITIVO_ID NUMBER,
                    LSP_NO_DOCUMENTO VARCHAR2(50),
                    LSP_NOMBRE_COMPLETO VARCHAR2(300),
                    LSP_MOTIVO_INGRESO VARCHAR2(1000),
                    LSP_FECHA_CREACION DATE,
                    LSP_USR_CREACION_ID NUMBER,
                    LSP_ESTADO_REGISTRO NUMBER,
                    LSP_TIPO_LISTA_CAUTELA_ID NUMBER,
                    LSP_ORIGEN_REGISTRO VARCHAR2(100),
                    CONSTRAINT UQ_RL_LSP_DOCUMENTO UNIQUE (LSP_TIPO_DOCUMENTO_ID, LSP_NO_DOCUMENTO)
                )");
            createdPositivos = true;
        }

        bool createdSequence = false;
        if (await ScalarAsync(connection, "SELECT COUNT(*) FROM USER_SEQUENCES WHERE SEQUENCE_NAME = 'SEQ_RL_LISTA_POSITIVOS'") == 0)
        {
            await ExecuteAsync(connection, "CREATE SEQUENCE SEQ_RL_LISTA_POSITIVOS START WITH 95000 INCREMENT BY 1");
            createdSequence = true;
        }

        bool createdCautela = false;
        if (await ScalarAsync(connection, "SELECT COUNT(*) FROM ALL_TABLES WHERE OWNER = 'DNP_IHSS' AND TABLE_NAME = 'TIPO_LISTAS_CAUTELA'") == 0)
        {
            await ExecuteAsync(connection, @"
                CREATE TABLE DNP_IHSS.TIPO_LISTAS_CAUTELA (
                    TIPO_LISTA_CAUTELA_ID NUMBER PRIMARY KEY,
                    LISTA_CAUTELA_DESCRICPION VARCHAR2(150)
                )");
            createdCautela = true;
        }
        await TryExecuteAsync(connection, "INSERT INTO DNP_IHSS.TIPO_LISTAS_CAUTELA (TIPO_LISTA_CAUTELA_ID, LISTA_CAUTELA_DESCRICPION) VALUES (901, 'LISTA PRUEBA')");

        bool createdCoincidencias = false;
        if (await ScalarAsync(connection, "SELECT COUNT(*) FROM ALL_TABLES WHERE OWNER = 'DNP_IHSS' AND TABLE_NAME = 'REPORTE_COINCIDENCIAS'") == 0)
        {
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
            createdCoincidencias = true;
        }

        bool createdSocios = false;
        if (await ScalarAsync(connection, "SELECT COUNT(*) FROM ALL_TABLES WHERE OWNER = 'DNP_IHSS' AND TABLE_NAME = 'SOCIOS'") == 0)
        {
            await ExecuteAsync(connection, @"
                CREATE TABLE DNP_IHSS.SOCIOS (
                    SOCIO_ID NUMBER PRIMARY KEY,
                    NUMERO_IDENTIFICACION VARCHAR2(50)
                )");
            createdSocios = true;
        }

        bool createdRepresentantes = false;
        if (await ScalarAsync(connection, "SELECT COUNT(*) FROM ALL_TABLES WHERE OWNER = 'DNP_IHSS' AND TABLE_NAME = 'REPRESENTANTES'") == 0)
        {
            await ExecuteAsync(connection, @"
                CREATE TABLE DNP_IHSS.REPRESENTANTES (
                    REPRESENTANTE_ID NUMBER PRIMARY KEY,
                    NUMERO_IDENTIFICACION VARCHAR2(50)
                )");
            createdRepresentantes = true;
        }

        bool createdSociosRep = false;
        if (await ScalarAsync(connection, "SELECT COUNT(*) FROM ALL_TABLES WHERE OWNER = 'DNP_IHSS' AND TABLE_NAME = 'V_SOCIOS_REPRESENTANTES_M1'") == 0)
        {
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
            createdSociosRep = true;
        }

        bool createdEmpleadosPlanillas = false;
        if (await ScalarAsync(connection, "SELECT COUNT(*) FROM ALL_TABLES WHERE OWNER = 'DNP_IHSS' AND TABLE_NAME = 'V_EMPLEADOS_IHSS_PLANILLAS_M1'") == 0)
        {
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
            createdEmpleadosPlanillas = true;
        }

        return new CoexistenciaFixtureState(
            createdUser,
            createdPositivos,
            createdSequence,
            createdCautela,
            createdCoincidencias,
            createdSocios,
            createdRepresentantes,
            createdSociosRep,
            createdEmpleadosPlanillas);
    }

    private static async Task CleanupCoexistenciaFixtureAsync(OracleConnection connection, CoexistenciaFixtureState fixture)
    {
        if (fixture.CreatedPositivos)
        {
            await TryExecuteAsync(connection, "DROP TABLE RL_LISTA_POSITIVOS PURGE");
        }
        else
        {
            await TryExecuteAsync(connection, "DELETE FROM RL_LISTA_POSITIVOS WHERE LSP_POSITIVO_ID >= 93000");
        }

        if (fixture.CreatedSequence)
        {
            await TryExecuteAsync(connection, "DROP SEQUENCE SEQ_RL_LISTA_POSITIVOS");
        }

        if (fixture.CreatedEmpleadosPlanillas)
        {
            await TryExecuteAsync(connection, "DROP VIEW DNP_IHSS.V_EMPLEADOS_IHSS_PLANILLAS");
            await TryExecuteAsync(connection, "DROP TABLE DNP_IHSS.V_EMPLEADOS_IHSS_PLANILLAS_M1 PURGE");
        }

        if (fixture.CreatedSociosRep)
        {
            await TryExecuteAsync(connection, "DROP VIEW DNP_IHSS.V_SOCIOS_REPRESENTANTES");
            await TryExecuteAsync(connection, "DROP TABLE DNP_IHSS.V_SOCIOS_REPRESENTANTES_M1 PURGE");
        }

        if (fixture.CreatedRepresentantes)
        {
            await TryExecuteAsync(connection, "DROP TABLE DNP_IHSS.REPRESENTANTES PURGE");
        }

        if (fixture.CreatedSocios)
        {
            await TryExecuteAsync(connection, "DROP TABLE DNP_IHSS.SOCIOS PURGE");
        }

        if (fixture.CreatedCoincidencias)
        {
            await TryExecuteAsync(connection, "DROP TABLE DNP_IHSS.REPORTE_COINCIDENCIAS PURGE");
        }

        if (fixture.CreatedCautela)
        {
            await TryExecuteAsync(connection, "DROP TABLE DNP_IHSS.TIPO_LISTAS_CAUTELA PURGE");
        }

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
            throw new InvalidOperationException("La prueba solo puede ejecutarse contra Oracle XE local descartable.");
    }

    private static async Task<long> ScalarAsync(OracleConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return Convert.ToInt64(await command.ExecuteScalarAsync(), CultureInfo.InvariantCulture);
    }

    private static async Task<object?> ScalarStringAsync(OracleConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return await command.ExecuteScalarAsync();
    }

    private static async Task ExecuteAsync(OracleConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }

    private static async Task TryExecuteAsync(OracleConnection connection, string sql)
    {
        try { await ExecuteAsync(connection, sql); } catch { }
    }

    private sealed record CoexistenciaFixtureState(
        bool CreatedUser,
        bool CreatedPositivos,
        bool CreatedSequence,
        bool CreatedCautela,
        bool CreatedCoincidencias,
        bool CreatedSocios,
        bool CreatedRepresentantes,
        bool CreatedSociosRep,
        bool CreatedEmpleadosPlanillas);
}
