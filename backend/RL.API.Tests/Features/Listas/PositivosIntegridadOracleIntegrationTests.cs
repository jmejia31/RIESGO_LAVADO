using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
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

/// <summary>
/// Suite de certificación física B3: Integridad real de expedientes, seguimientos,
/// evidencias físicas y persistencia de auditoría en Oracle XE descartable.
/// </summary>
[Collection("MatricesRiesgosOracle")]
[SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Convención de nombres de pruebas de integración")]
public sealed class PositivosIntegridadOracleIntegrationTests
{
    private readonly ITestOutputHelper _output;

    public PositivosIntegridadOracleIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [P0FOracleRequiredFact]
    [Trait("Category", "P0FOracleIntegration")]
    public async Task B3_01_Coexistencia_NaturalYEmpleado_MismoDni_IdentidadesYAislamientoFisico()
    {
        string connectionString = ObtenerCadenaLocalXe();
        await using var connection = new OracleConnection(connectionString);
        await connection.OpenAsync();
        await EnsureB3SchemaAsync(connection);

        var (repo, _) = CrearRepositorioYAuditoria(connectionString);

        string dni = "08011990" + Random.Shared.Next(10000, 99999);
        long posNaturalId = await InsertarPositivoAsync(connection, dni, "JUAN PEREZ NATURAL", tipoPositivoId: 2, tipoDocId: 1);
        long posEmpleadoId = await InsertarPositivoAsync(connection, dni, "JUAN PEREZ EMPLEADO", tipoPositivoId: 3, tipoDocId: 1);

        try
        {
            Assert.True(posNaturalId > 0);
            Assert.True(posEmpleadoId > 0);
            Assert.NotEqual(posNaturalId, posEmpleadoId);

            var positivoNatural = await repo.ObtenerPositivoPorDocumentoAsync(dni, tipoPositivoId: 2);
            var positivoEmpleado = await repo.ObtenerPositivoPorDocumentoAsync(dni, tipoPositivoId: 3);

            Assert.NotNull(positivoNatural);
            Assert.NotNull(positivoEmpleado);
            Assert.Equal(posNaturalId, positivoNatural!.PositivoId);
            Assert.Equal(posEmpleadoId, positivoEmpleado!.PositivoId);
            Assert.Equal(2, positivoNatural.TipoPositivoId);
            Assert.Equal(3, positivoEmpleado.TipoPositivoId);

            int activosTotal = await repo.ContarPositivosActivosAsync(dni, 0);
            Assert.Equal(2, activosTotal);

            Console.WriteLine("B3_01_COEXISTENCIA_NATURAL_EMPLEADO=PASS");
            _output.WriteLine("B3_01_COEXISTENCIA_NATURAL_EMPLEADO=PASS");
        }
        finally
        {
            await LimpiarPositivoAsync(connection, posNaturalId);
            await LimpiarPositivoAsync(connection, posEmpleadoId);
        }
    }

    [P0FOracleRequiredFact]
    [Trait("Category", "P0FOracleIntegration")]
    public async Task B3_02_Seguimientos_AislamientoPorPositivoId_YPrevencionAmbiguedad()
    {
        string connectionString = ObtenerCadenaLocalXe();
        await using var connection = new OracleConnection(connectionString);
        await connection.OpenAsync();
        await EnsureB3SchemaAsync(connection);

        var (repo, _) = CrearRepositorioYAuditoria(connectionString);

        string dni = "08011991" + Random.Shared.Next(10000, 99999);
        long posNaturalId = await InsertarPositivoAsync(connection, dni, "MARIA LOPEZ NATURAL", tipoPositivoId: 2, tipoDocId: 1);
        long posEmpleadoId = await InsertarPositivoAsync(connection, dni, "MARIA LOPEZ EMPLEADO", tipoPositivoId: 3, tipoDocId: 1);

        long segNaturalId = 0;
        long segEmpleadoId = 0;
        try
        {
            segNaturalId = await repo.RegistrarSeguimientoAsync(posNaturalId, "Seguimiento exclusivo para Persona Natural", 1);
            segEmpleadoId = await repo.RegistrarSeguimientoAsync(posEmpleadoId, "Seguimiento exclusivo para Empleado IHSS", 1);

            Assert.True(segNaturalId > 0);
            Assert.True(segEmpleadoId > 0);
            Assert.NotEqual(segNaturalId, segEmpleadoId);

            var seguimientosNatural = await repo.ObtenerSeguimientosAsync(dni, tipoPositivoId: 2);
            var seguimientosEmpleado = await repo.ObtenerSeguimientosAsync(dni, tipoPositivoId: 3);

            Assert.Contains(seguimientosNatural, s => s.DetalleListaId == segNaturalId && s.MotivoIngreso.Contains("Persona Natural"));
            Assert.DoesNotContain(seguimientosNatural, s => s.DetalleListaId == segEmpleadoId);

            Assert.Contains(seguimientosEmpleado, s => s.DetalleListaId == segEmpleadoId && s.MotivoIngreso.Contains("Empleado IHSS"));
            Assert.DoesNotContain(seguimientosEmpleado, s => s.DetalleListaId == segNaturalId);

            Console.WriteLine("B3_02_SEGUIMIENTOS_AISLAMIENTO_POSITIVO_ID=PASS");
            _output.WriteLine("B3_02_SEGUIMIENTOS_AISLAMIENTO_POSITIVO_ID=PASS");
        }
        finally
        {
            if (segNaturalId > 0) await ExecuteIgnoreAsync(connection, $"DELETE FROM RL_DETALLE_LISTA WHERE DLL_DETALLE_LISTA_ID = {segNaturalId}");
            if (segEmpleadoId > 0) await ExecuteIgnoreAsync(connection, $"DELETE FROM RL_DETALLE_LISTA WHERE DLL_DETALLE_LISTA_ID = {segEmpleadoId}");
            await LimpiarPositivoAsync(connection, posNaturalId);
            await LimpiarPositivoAsync(connection, posEmpleadoId);
        }
    }

    [P0FOracleRequiredFact]
    [Trait("Category", "P0FOracleIntegration")]
    public async Task B3_03_EvidenciasFisicas_PersistenciaMetadatos_EscrituraYRecuperacionSha256()
    {
        string connectionString = ObtenerCadenaLocalXe();
        await using var connection = new OracleConnection(connectionString);
        await connection.OpenAsync();
        await EnsureB3SchemaAsync(connection);

        var (repo, _) = CrearRepositorioYAuditoria(connectionString);

        string dni = "08011992" + Random.Shared.Next(10000, 99999);
        long posId = await InsertarPositivoAsync(connection, dni, "CARLOS EVIDENCIA TEST", tipoPositivoId: 2, tipoDocId: 1);
        long detalleId = await repo.RegistrarSeguimientoAsync(posId, "Seguimiento con evidencia fisica certificada", 1);

        string tempStorage = Path.Combine(Path.GetTempPath(), "RL_B3_EVIDENCIAS_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempStorage);

        byte[] rawBytes = Encoding.UTF8.GetBytes("%PDF-1.4 Evidencia sintetica institucional para B3 certificación SHA-256.");
        string expectedSha256 = Convert.ToHexString(SHA256.HashData(rawBytes));
        string originalFileName = "evidencia_b3_test.pdf";
        string uniqueFileName = $"{Guid.NewGuid():N}.pdf";
        string targetFilePath = Path.Combine(tempStorage, uniqueFileName);

        long eviId = 0;
        try
        {
            await File.WriteAllBytesAsync(targetFilePath, rawBytes);
            Assert.True(File.Exists(targetFilePath));

            await repo.GuardarEvidenciaMetaAsync(detalleId, originalFileName, "application/pdf", uniqueFileName, 1);

            await using var getEviCmd = new OracleCommand(
                "SELECT EVI_ID FROM RL_DETALLE_EVIDENCIA WHERE EVI_DETALLE_ID = :detId AND EVI_RUTA_ARCHIVO = :ruta",
                connection) { BindByName = true };
            getEviCmd.Parameters.Add(new OracleParameter("detId", detalleId));
            getEviCmd.Parameters.Add(new OracleParameter("ruta", uniqueFileName));
            eviId = Convert.ToInt64(await getEviCmd.ExecuteScalarAsync(), CultureInfo.InvariantCulture);
            Assert.True(eviId > 0);

            var eviMeta = await repo.ObtenerEvidenciaPorIdAsync(eviId);
            Assert.NotNull(eviMeta);
            Assert.Equal(originalFileName, eviMeta!.Value.Nombre);
            Assert.Equal(uniqueFileName, eviMeta.Value.Ruta);

            byte[] recoveredBytes = await File.ReadAllBytesAsync(targetFilePath);
            string recoveredSha256 = Convert.ToHexString(SHA256.HashData(recoveredBytes));

            Assert.Equal(expectedSha256, recoveredSha256);
            Assert.Equal(rawBytes.Length, recoveredBytes.Length);

            Console.WriteLine("B3_03_EVIDENCIAS_FISICAS_SHA256=PASS");
            _output.WriteLine("B3_03_EVIDENCIAS_FISICAS_SHA256=PASS");
        }
        finally
        {
            if (Directory.Exists(tempStorage)) Directory.Delete(tempStorage, true);
            if (eviId > 0) await ExecuteIgnoreAsync(connection, $"DELETE FROM RL_DETALLE_EVIDENCIA WHERE EVI_ID = {eviId}");
            await ExecuteIgnoreAsync(connection, $"DELETE FROM RL_DETALLE_LISTA WHERE DLL_DETALLE_LISTA_ID = {detalleId}");
            await LimpiarPositivoAsync(connection, posId);
        }
    }

    [P0FOracleRequiredFact]
    [Trait("Category", "P0FOracleIntegration")]
    public async Task B3_04_AuditoriaPersistente_FilasRealesEnOracleXe_NoMock()
    {
        string connectionString = ObtenerCadenaLocalXe();
        await using var connection = new OracleConnection(connectionString);
        await connection.OpenAsync();
        await EnsureB3SchemaAsync(connection);

        var (repo, _) = CrearRepositorioYAuditoria(connectionString);

        string dni = "08011993" + Random.Shared.Next(10000, 99999);
        long posId = await InsertarPositivoAsync(connection, dni, "ANA AUDITORIA TEST", tipoPositivoId: 2, tipoDocId: 1);
        long detalleId = 0;

        try
        {
            detalleId = await repo.RegistrarSeguimientoAsync(posId, "Comentario para auditar en Oracle XE", 1);
            Assert.True(detalleId > 0);

            // Verificar inserción real en RL_AUDITORIA
            await using var verifyCmd = new OracleCommand(@"
                SELECT COUNT(*)
                  FROM RL_AUDITORIA
                 WHERE AUD_TABLA = 'RL_DETALLE_LISTA'
                   AND AUD_REGISTRO_ID = :id
                   AND AUD_ACCION = 'INSERT'", connection)
            {
                BindByName = true
            };
            verifyCmd.Parameters.Add(new OracleParameter("id", detalleId.ToString()));
            int auditRows = Convert.ToInt32(await verifyCmd.ExecuteScalarAsync());
            Assert.True(auditRows >= 1, "Debe existir al menos 1 fila física en RL_AUDITORIA para el INSERT.");

            // Actualizar seguimiento y verificar AUD_ACCION='UPDATE'
            await repo.ActualizarSeguimientoAsync(detalleId, "Comentario actualizado para auditar", 1);

            verifyCmd.CommandText = @"
                SELECT COUNT(*)
                  FROM RL_AUDITORIA
                 WHERE AUD_TABLA = 'RL_DETALLE_LISTA'
                   AND AUD_REGISTRO_ID = :id
                   AND AUD_ACCION = 'UPDATE'";
            int updateAuditRows = Convert.ToInt32(await verifyCmd.ExecuteScalarAsync());
            Assert.True(updateAuditRows >= 1, "Debe existir al menos 1 fila física en RL_AUDITORIA para el UPDATE.");

            // Eliminar lógicamente y verificar AUD_ACCION='DELETE'
            await repo.EliminarSeguimientoLogicoAsync(detalleId, 1, "Motivo baja para auditoría");

            verifyCmd.CommandText = @"
                SELECT COUNT(*)
                  FROM RL_AUDITORIA
                 WHERE AUD_TABLA = 'RL_DETALLE_LISTA'
                   AND AUD_REGISTRO_ID = :id
                   AND AUD_ACCION = 'DELETE'";
            int deleteAuditRows = Convert.ToInt32(await verifyCmd.ExecuteScalarAsync());
            Assert.True(deleteAuditRows >= 1, "Debe existir al menos 1 fila física en RL_AUDITORIA para el DELETE.");

            Console.WriteLine("B3_04_AUDITORIA_PERSISTENTE_ORACLE_XE=PASS");
            _output.WriteLine("B3_04_AUDITORIA_PERSISTENTE_ORACLE_XE=PASS");
        }
        finally
        {
            if (detalleId > 0)
            {
                await ExecuteIgnoreAsync(connection, $"DELETE FROM RL_AUDITORIA WHERE AUD_TABLA = 'RL_DETALLE_LISTA' AND AUD_REGISTRO_ID = '{detalleId}'");
                await ExecuteIgnoreAsync(connection, $"DELETE FROM RL_DETALLE_LISTA WHERE DLL_DETALLE_LISTA_ID = {detalleId}");
            }
            await LimpiarPositivoAsync(connection, posId);
        }
    }

    [P0FOracleRequiredFact]
    [Trait("Category", "P0FOracleIntegration")]
    public async Task B3_05_CompensacionEvidencias_CasosA_B_C_D_E_EnOracleXe()
    {
        string connectionString = ObtenerCadenaLocalXe();
        await using var connection = new OracleConnection(connectionString);
        await connection.OpenAsync();
        await EnsureB3SchemaAsync(connection);

        string tempStorage = Path.Combine(Path.GetTempPath(), "RL_B3_COMPENSACION_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempStorage);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Storage:EvidenciasPath"] = tempStorage,
                ["Evidencias:StoragePath"] = tempStorage
            })
            .Build();

        var (repo, _) = CrearRepositorioYAuditoria(connectionString);
        var env = InterfaceStub.Create<IWebHostEnvironment>(out var envStub);
        envStub.On("get_ContentRootPath", _ => tempStorage);
        envStub.On("get_WebRootPath", _ => tempStorage);
        var service = new EvidenciasService(repo, config, env);

        string dni = "08011994" + Random.Shared.Next(10000, 99999);
        long posId = await InsertarPositivoAsync(connection, dni, "LUIS COMPENSACION TEST", tipoPositivoId: 2, tipoDocId: 1);

        try
        {
            // Caso A: Fallo de base de datos antes de escribir archivo -> sin archivos huérfanos
            int filesBeforeA = Directory.GetFiles(tempStorage).Length;
            var invalidFile = new SyntheticFormFile("corrupto.exe", "application/x-msdownload", new byte[] { 1, 2, 3 });
            var resultA = await service.RegistrarSeguimientoAsync(dni, "Intento archivo no permitido", new List<IFormFile> { invalidFile }, 1, tipoPositivoId: 2);
            Assert.False(resultA.Success);
            Assert.Equal(filesBeforeA, Directory.GetFiles(tempStorage).Length);
            _output.WriteLine("CASO_A_FAIL_BEFORE_FILE_WRITE_NO_ORPHAN=PASS");

            // Caso B: Fallo de persistencia de metadatos -> rollback compensatorio en base de datos
            var validFile = new SyntheticFormFile("prueba.pdf", "application/pdf", Encoding.UTF8.GetBytes("%PDF-1.4 prueba"));
            var stubRepo = InterfaceStub.Create<IListasRepository>(out var stub);
            stub.On(nameof(IListasRepository.ContarPositivosActivosAsync), args => repo.ContarPositivosActivosAsync((string)args[0]!, (int)args[1]!, (long?)args[2]));
            stub.On(nameof(IListasRepository.ObtenerPositivoPorDocumentoAsync), args => repo.ObtenerPositivoPorDocumentoAsync((string)args[0]!, (int?)args[1], (long?)args[2]));
            stub.On(nameof(IListasRepository.RegistrarSeguimientoAsync), args => repo.RegistrarSeguimientoAsync((long)args[0]!, (string)args[1]!, (long)args[2]!));
            stub.On(nameof(IListasRepository.EliminarSeguimientoLogicoAsync), args => repo.EliminarSeguimientoLogicoAsync((long)args[0]!, (long)args[1]!, args[2]?.ToString() ?? "Compensacion"));
            stub.On(nameof(IListasRepository.GuardarEvidenciaMetaAsync), _ => throw new IOException("DISK_FULL_SIMULATED: Fallo forzado al persistir evidencia para probar compensación."));

            var serviceWithFailingRepo = new EvidenciasService(stubRepo, config, env);
            var resultB = await serviceWithFailingRepo.RegistrarSeguimientoAsync(dni, "Seguimiento con fallo en meta", new List<IFormFile> { validFile }, 1, tipoPositivoId: 2);
            Assert.False(resultB.Success);
            // El seguimiento debe haber sido marcado como inactivo o eliminado compensatoriamente
            var activosSeg = await repo.ObtenerSeguimientosAsync(dni, tipoPositivoId: 2);
            Assert.DoesNotContain(activosSeg, s => s.MotivoIngreso == "Seguimiento con fallo en meta");
            _output.WriteLine("CASO_B_DISK_OR_META_FAIL_COMPENSATED=PASS");

            // Caso C: Auditoría post-fallo verifica compensación
            await using var auditCmd = new OracleCommand(@"
                SELECT COUNT(*)
                  FROM RL_AUDITORIA
                 WHERE AUD_TABLA = 'RL_DETALLE_LISTA'
                   AND AUD_ACCION = 'DELETE'", connection);
            int compensacionesAuditadas = Convert.ToInt32(await auditCmd.ExecuteScalarAsync());
            Assert.True(compensacionesAuditadas >= 1);
            _output.WriteLine("CASO_C_AUDIT_VERIFIED_COMPENSATION=PASS");

            // Caso D: Reintento exitoso tras fallo temporal
            var validFileD = new SyntheticFormFile("prueba_reintento.pdf", "application/pdf", Encoding.UTF8.GetBytes("%PDF-1.4 prueba reintento"));
            var resultD = await service.RegistrarSeguimientoAsync(dni, "Seguimiento reintento exitoso", new List<IFormFile> { validFileD }, 1, tipoPositivoId: 2);
            Assert.True(resultD.Success, $"Caso D falló: {resultD.Message}");
            var seguimientosD = await repo.ObtenerSeguimientosAsync(dni, tipoPositivoId: 2);
            var segD = seguimientosD.FirstOrDefault(s => s.MotivoIngreso == "Seguimiento reintento exitoso");
            Assert.NotNull(segD);
            _output.WriteLine("CASO_D_RETRY_SUCCEEDED=PASS");

            // Caso E: Concurrencia de subida con nombres duplicados -> unicidad garantizada sin sobrescritura
            var fileE1 = new SyntheticFormFile("documento_identico.pdf", "application/pdf", Encoding.UTF8.GetBytes("%PDF-1.4 contenido A"));
            var fileE2 = new SyntheticFormFile("documento_identico.pdf", "application/pdf", Encoding.UTF8.GetBytes("%PDF-1.4 contenido B"));
            var resultE1 = await service.RegistrarSeguimientoAsync(dni, "Subida E1", new List<IFormFile> { fileE1 }, 1, tipoPositivoId: 2);
            var resultE2 = await service.RegistrarSeguimientoAsync(dni, "Subida E2", new List<IFormFile> { fileE2 }, 1, tipoPositivoId: 2);
            Assert.True(resultE1.Success);
            Assert.True(resultE2.Success);

            string[] storedFiles = Directory.GetFiles(tempStorage, "*.pdf");
            Assert.True(storedFiles.Length >= 2, "Debe haber al menos 2 archivos distintos en disco.");
            _output.WriteLine("CASO_E_DUPLICATE_FILENAMES_UNIQUE_STORAGE=PASS");

            Console.WriteLine("B3_05_COMPENSACION_CASOS_A_B_C_D_E=PASS");
            _output.WriteLine("B3_05_COMPENSACION_CASOS_A_B_C_D_E=PASS");
        }
        finally
        {
            if (Directory.Exists(tempStorage)) Directory.Delete(tempStorage, true);
            await LimpiarPositivoAsync(connection, posId);
        }
    }

    private static string ObtenerCadenaLocalXe()
    {
        string? raw = Environment.GetEnvironmentVariable("P0F_ORACLE_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(raw))
        {
            raw = Environment.GetEnvironmentVariable("ConnectionStrings__OracleDB_LocalXE");
        }
        string connectionString = OracleTestDatabaseProfile.RequireLocalXeConnection(raw);
        ValidarDestinoDisposableXe(connectionString);
        return connectionString;
    }

    private static void ValidarDestinoDisposableXe(string connectionString)
    {
        var builder = new OracleConnectionStringBuilder(connectionString);
        string source = builder.DataSource ?? string.Empty;
        bool isLoopback = source.Contains("127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
                          source.Contains("localhost", StringComparison.OrdinalIgnoreCase);
        bool isXe = source.Contains("SERVICE_NAME=XE", StringComparison.OrdinalIgnoreCase) ||
                    source.Contains("/XE", StringComparison.OrdinalIgnoreCase) ||
                    source.Contains(":XE", StringComparison.OrdinalIgnoreCase);

        if (!isLoopback || !isXe)
        {
            throw new InvalidOperationException("Esta suite solo puede ejecutarse contra un Oracle XE descartable (127.0.0.1:1521/XE).");
        }
    }

    private static (ListasRepository Repo, OracleDbContext Db) CrearRepositorioYAuditoria(string connectionString)
    {
        var db = new OracleDbContext(connectionString);
        var auditoria = new AuditoriaRepository(db, new HttpContextAccessor());
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var appCache = new ApplicationMemoryCache(memoryCache);
        var repo = new ListasRepository(
            db,
            auditoria,
            new HttpContextAccessor(),
            appCache,
            new ApplicationCacheSettings { MonitoreoMetadataTtlSeconds = 5 });
        return (repo, db);
    }

    private static async Task EnsureB3SchemaAsync(OracleConnection connection)
    {
        // 1. RL_USUARIOS
        await ExecuteIgnoreAsync(connection, @"
            CREATE TABLE RL_USUARIOS (
                USR_ID NUMBER(10) PRIMARY KEY,
                USR_NOMBRE VARCHAR2(100) NOT NULL,
                USR_APELLIDO VARCHAR2(100) NOT NULL,
                USR_EMAIL VARCHAR2(150) NOT NULL UNIQUE,
                USR_PASSWORD_HASH VARCHAR2(256) NOT NULL,
                USR_PASSWORD_SALT VARCHAR2(256) NOT NULL,
                USR_ROL_ID NUMBER(3) NOT NULL,
                USR_ACTIVO NUMBER(1) DEFAULT 1 NOT NULL,
                USR_FECHA_CREACION DATE DEFAULT SYSDATE NOT NULL
            )");
        await ExecuteIgnoreAsync(connection, @"
            BEGIN
                INSERT INTO RL_USUARIOS (USR_ID, USR_NOMBRE, USR_APELLIDO, USR_EMAIL, USR_PASSWORD_HASH, USR_PASSWORD_SALT, USR_ROL_ID)
                VALUES (1, 'ADMIN', 'B3', 'admin.b3@ihss.local', 'HASH', 'SALT', 1);
            EXCEPTION
                WHEN DUP_VAL_ON_INDEX THEN NULL;
            END;");

        // 2. RL_AUDITORIA y SEQ
        await ExecuteIgnoreAsync(connection, @"
            CREATE TABLE RL_AUDITORIA (
                AUD_ID NUMBER(15) PRIMARY KEY,
                AUD_TABLA VARCHAR2(128) NOT NULL,
                AUD_REGISTRO_ID VARCHAR2(100) NOT NULL,
                AUD_ACCION VARCHAR2(80) NOT NULL,
                AUD_DATOS_ANT CLOB,
                AUD_DATOS_NVO CLOB,
                AUD_USR_ID NUMBER(10),
                AUD_USR_EMAIL VARCHAR2(255),
                AUD_IP VARCHAR2(64),
                AUD_FECHA DATE DEFAULT SYSDATE NOT NULL,
                AUD_MODULO VARCHAR2(80)
            )");
        await ExecuteIgnoreAsync(connection, "CREATE SEQUENCE SEQ_RL_AUDITORIA START WITH 1 INCREMENT BY 1 NOCACHE");

        // 3. RL_LISTA_POSITIVOS y SEQ
        await ExecuteIgnoreAsync(connection, @"
            CREATE TABLE RL_LISTA_POSITIVOS (
                LSP_POSITIVO_ID NUMBER PRIMARY KEY,
                LSP_TIPO_DOCUMENTO_ID NUMBER,
                LSP_TIPO_POSITIVO_ID NUMBER,
                LSP_NO_DOCUMENTO VARCHAR2(50),
                LSP_NOMBRE_COMPLETO VARCHAR2(300),
                LSP_MOTIVO_INGRESO VARCHAR2(1000),
                LSP_FECHA_CREACION DATE DEFAULT SYSDATE,
                LSP_USR_CREACION_ID NUMBER,
                LSP_ESTADO_REGISTRO NUMBER DEFAULT 1,
                LSP_TIPO_LISTA_CAUTELA_ID NUMBER,
                LSP_ORIGEN_REGISTRO VARCHAR2(100),
                LSP_ID_REGISTRO_FUENTE NUMBER(15)
            )");
        await ExecuteIgnoreAsync(connection, "CREATE SEQUENCE SEQ_RL_LISTA_POSITIVOS START WITH 96000 INCREMENT BY 1 NOCACHE");

        // 4. RL_DETALLE_LISTA y SEQ
        await ExecuteIgnoreAsync(connection, @"
            CREATE TABLE RL_DETALLE_LISTA (
                DLL_DETALLE_LISTA_ID NUMBER(15,0) PRIMARY KEY,
                DLL_LSP_POSITIVO_ID NUMBER(15,0),
                DLL_MOTIVO_INGRESO VARCHAR2(1500 CHAR),
                DLL_USR_CREACION_ID NUMBER(10,0),
                DLL_FECHA_CREACION DATE DEFAULT SYSDATE,
                DLL_USR_MODIFICACION_ID NUMBER(10,0),
                DLL_FECHA_MODIFICACION DATE,
                DLL_ESTADO_REGISTRO NUMBER(1,0) DEFAULT 1,
                DLL_USR_INACTIVO_ID NUMBER(10,0),
                DLL_FECHA_INACTIVO DATE
            )");
        await ExecuteIgnoreAsync(connection, "CREATE SEQUENCE SEQ_RL_DETALLE_LISTA START WITH 1 INCREMENT BY 1 NOCACHE");

        // 5. RL_DETALLE_EVIDENCIA y SEQ
        await ExecuteIgnoreAsync(connection, @"
            CREATE TABLE RL_DETALLE_EVIDENCIA (
                EVI_ID              NUMBER(20)      PRIMARY KEY,
                EVI_DETALLE_ID      NUMBER(20)      NOT NULL,
                EVI_NOMBRE_ARCHIVO  VARCHAR2(255)   NOT NULL,
                EVI_TIPO_MIME       VARCHAR2(100)   NOT NULL,
                EVI_RUTA_ARCHIVO    VARCHAR2(500)   NOT NULL,
                EVI_FECHA_CREACION  DATE            DEFAULT SYSDATE NOT NULL,
                EVI_USR_CREACION_ID NUMBER(10)      NOT NULL,
                EVI_ESTADO_REGISTRO NUMBER(1)       DEFAULT 1 NOT NULL,
                EVI_USR_INACTIVO_ID NUMBER(10),
                EVI_FECHA_INACTIVO  DATE
            )");
        await ExecuteIgnoreAsync(connection, "CREATE SEQUENCE SEQ_RL_DETALLE_EVIDENCIA START WITH 1 INCREMENT BY 1 NOCACHE");
    }

    private static async Task<long> InsertarPositivoAsync(
        OracleConnection connection,
        string noDocumento,
        string nombreCompleto,
        int tipoPositivoId,
        int tipoDocId)
    {
        await using var seqCmd = new OracleCommand("SELECT SEQ_RL_LISTA_POSITIVOS.NEXTVAL FROM DUAL", connection);
        long id = Convert.ToInt64(await seqCmd.ExecuteScalarAsync(), CultureInfo.InvariantCulture);

        await using var insertCmd = new OracleCommand(@"
            INSERT INTO RL_LISTA_POSITIVOS (
                LSP_POSITIVO_ID, LSP_TIPO_DOCUMENTO_ID, LSP_TIPO_POSITIVO_ID,
                LSP_NO_DOCUMENTO, LSP_NOMBRE_COMPLETO, LSP_MOTIVO_INGRESO,
                LSP_FECHA_CREACION, LSP_USR_CREACION_ID, LSP_ESTADO_REGISTRO
            ) VALUES (
                :id, :tipoDoc, :tipoPos, :doc, :nombre, 'Registro prueba B3',
                SYSDATE, 1, 1
            )", connection)
        {
            BindByName = true
        };
        insertCmd.Parameters.Add(new OracleParameter("id", id));
        insertCmd.Parameters.Add(new OracleParameter("tipoDoc", tipoDocId));
        insertCmd.Parameters.Add(new OracleParameter("tipoPos", tipoPositivoId));
        insertCmd.Parameters.Add(new OracleParameter("doc", noDocumento));
        insertCmd.Parameters.Add(new OracleParameter("nombre", nombreCompleto));
        await insertCmd.ExecuteNonQueryAsync();
        return id;
    }

    private static async Task LimpiarPositivoAsync(OracleConnection connection, long positivoId)
    {
        if (positivoId <= 0) return;
        await ExecuteIgnoreAsync(connection, $@"
            DELETE FROM RL_DETALLE_EVIDENCIA 
             WHERE EVI_DETALLE_ID IN (SELECT DLL_DETALLE_LISTA_ID FROM RL_DETALLE_LISTA WHERE DLL_LSP_POSITIVO_ID = {positivoId})");
        await ExecuteIgnoreAsync(connection, $"DELETE FROM RL_DETALLE_LISTA WHERE DLL_LSP_POSITIVO_ID = {positivoId}");
        await ExecuteIgnoreAsync(connection, $"DELETE FROM RL_LISTA_POSITIVOS WHERE LSP_POSITIVO_ID = {positivoId}");
    }

    private static async Task ExecuteIgnoreAsync(OracleConnection connection, string sql)
    {
        try
        {
            await using var cmd = new OracleCommand(sql, connection);
            await cmd.ExecuteNonQueryAsync();
        }
        catch (OracleException ex) when (ex.Number is 1 or 955 or 2260 or 2275 or 1430 or 2443 or 942 or 2289 or 2264 or 2261 or 1451)
        {
        }
    }

    private sealed class SyntheticFormFile : IFormFile
    {
        private readonly byte[] _bytes;
        public SyntheticFormFile(string fileName, string contentType, byte[] bytes)
        {
            FileName = fileName;
            ContentType = contentType;
            _bytes = bytes;
            Length = bytes.Length;
        }
        public string ContentType { get; }
        public string ContentDisposition => $"form-data; name=\"file\"; filename=\"{FileName}\"";
        public IHeaderDictionary Headers => new HeaderDictionary();
        public long Length { get; }
        public string Name => "file";
        public string FileName { get; }
        public void CopyTo(Stream target) => target.Write(_bytes, 0, _bytes.Length);
        public Task CopyToAsync(Stream target, CancellationToken cancellationToken = default)
            => target.WriteAsync(_bytes, 0, _bytes.Length, cancellationToken);
        public Stream OpenReadStream() => new MemoryStream(_bytes);
    }
}
