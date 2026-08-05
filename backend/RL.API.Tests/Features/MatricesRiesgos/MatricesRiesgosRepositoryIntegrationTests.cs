#pragma warning disable CA1416
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Oracle.ManagedDataAccess.Client;
using RL.API.Features.Auditoria.Contracts;
using RL.API.Features.Auditoria.Persistence;
using RL.API.Features.MatricesRiesgos.Contracts;
using RL.API.Features.MatricesRiesgos.Persistence;
using RL.API.Infrastructure.Database;
using Xunit;
using Xunit.Abstractions;

namespace RL.API.Tests.Features.MatricesRiesgos;

public sealed class MatricesRiesgosRepositoryIntegrationTests : IAsyncDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly string? _connectionString;
    private readonly bool _integrationRequired;

    // Lista blanca cerrada de secuencias autorizadas en la infraestructura de pruebas para evitar inyección SQL
    private static readonly HashSet<string> SecuenciasPermitidas = new(StringComparer.OrdinalIgnoreCase)
    {
        "SEQ_RL_MR_RIESGOS",
        "SEQ_RL_MR_EVALUACIONES",
        "SEQ_RL_MR_EVIDENCIAS",
        "SEQ_RL_MR_VERSIONES",
        "SEQ_RL_MR_APROBACIONES",
        "SEQ_RL_MR_CONTROLES",
        "SEQ_RL_MR_PLANES",
        "SEQ_RL_MR_ACTIVIDADES",
        "SEQ_RL_MR_SENALES_ALERTA",
        "SEQ_RL_MR_AUTOMONITOREO"
    };

    // Inventario en memoria para limpieza física de datos de prueba
    private readonly List<long> _evidenciasCreadas = new();
    private readonly List<long> _evaluacionesCreadas = new();
    private readonly List<long> _aprobacionesCreadas = new();
    private readonly List<long> _versionesCreadas = new();
    private const string TestPrefix = "TEST_INT_12_";

    public MatricesRiesgosRepositoryIntegrationTests(ITestOutputHelper output)
    {
        _output = output;

        // 1. Carga segura de configuración (Environment -> User Secrets -> appsettings.json)
        var config = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .AddUserSecrets<MatricesRiesgosRepositoryIntegrationTests>(optional: true)
            .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "../../../../RL.API/appsettings.json"), optional: true)
            .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "../../../../RL.API/appsettings.Development.json"), optional: true)
            .Build();

        _connectionString = config.GetConnectionString("OracleDB") ?? config["ConnectionStrings:OracleDB"];

        // 2. Control del modo de ejecución mediante la variable de entorno obligatoria
        string? requiredEnv = Environment.GetEnvironmentVariable("RL_ORACLE_INTEGRATION_REQUIRED");
        _integrationRequired = string.Equals(requiredEnv, "true", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<bool> ValidarEntornoEjecucionAsync()
    {
        if (!_integrationRequired)
        {
            _output.WriteLine("[ADVERTENCIA] Pruebas Oracle omitidas: RL_ORACLE_INTEGRATION_REQUIRED no está habilitada. Este resultado no certifica la Fase 1.2.");
            return false; // Omisión controlada en el entorno ordinario
        }

        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            Assert.Fail("Fallo de Certificación: La cadena de conexión a OracleDB no está configurada y la variable de entorno obligatoria RL_ORACLE_INTEGRATION_REQUIRED=true está activa.");
            return false;
        }

        try
        {
            await using var conn = new OracleConnection(_connectionString);
            var builder = new OracleConnectionStringBuilder(_connectionString)
            {
                ConnectionTimeout = 5
            };
            conn.ConnectionString = builder.ConnectionString;
            await conn.OpenAsync();

            // Validar que el esquema actual sea obligatoriamente RIESGO_LAVADO
            await using var cmdSchema = new OracleCommand("SELECT SYS_CONTEXT('USERENV', 'CURRENT_SCHEMA') FROM DUAL", conn);
            string? currentSchema = await cmdSchema.ExecuteScalarAsync() as string;
            if (!string.Equals(currentSchema, "RIESGO_LAVADO", StringComparison.OrdinalIgnoreCase))
            {
                Assert.Fail($"Fallo de Certificación: El esquema de la conexión no es RIESGO_LAVADO. Esquema actual: {currentSchema}.");
                return false;
            }
        }
        catch (Exception ex)
        {
            Assert.Fail($"Fallo de Certificación: Error al intentar conectar al esquema RIESGO_LAVADO. Detalle: {ex.Message}");
            return false;
        }

        return true;
    }

    private static async Task<long> ObtenerSiguienteSecuenciaPruebaAsync(OracleConnection conn, OracleTransaction? trans, string secuencia)
    {
        if (!SecuenciasPermitidas.Contains(secuencia))
        {
            throw new ArgumentException($"La secuencia '{secuencia}' no está permitida en la lista blanca de la infraestructura de pruebas.");
        }
        await using var cmd = new OracleCommand($"SELECT {secuencia}.NEXTVAL FROM DUAL", conn);
        if (trans is not null)
        {
            cmd.Transaction = trans;
        }
        return Convert.ToInt64(await cmd.ExecuteScalarAsync());
    }

    [Fact]
    public async Task Certificacion_Fase12_PruebaDeIntegracion_CommitYRollback_DeEvidenciasYAuditorias()
    {
        if (!await ValidarEntornoEjecucionAsync()) return;

        var dbContext = new OracleDbContext(_connectionString!);
        var auditoriaStub = new AuditoriaRepositoryStub();
        var repository = new MatricesRiesgosRepository(dbContext, auditoriaStub);

        await using var conn = dbContext.CreateConnection();
        await conn.OpenAsync();

        // 1. Crear evidencia inicial de prueba en RL_MR_EVIDENCIAS
        long evidenciaId = await ObtenerSiguienteSecuenciaPruebaAsync(conn, null, "SEQ_RL_MR_EVIDENCIAS");
        _evidenciasCreadas.Add(evidenciaId);

        const string sqlInsertEvidencia = @"
            INSERT INTO RL_MR_EVIDENCIAS (EVI_ID, EVI_NOMBRE_ARCHIVO, EVI_EXTENSION, EVI_TAMANO, EVI_HASH, EVI_RUTA, EVI_USR_CREACION)
            VALUES (:id, :nombre, '.txt', 1024, 'abc123hash', '/pruebas/txt', 99)";
        await using (var cmd = new OracleCommand(sqlInsertEvidencia, conn))
        {
            cmd.Parameters.Add(new OracleParameter("id", evidenciaId));
            cmd.Parameters.Add(new OracleParameter("nombre", TestPrefix + "archivo.txt"));
            await cmd.ExecuteNonQueryAsync();
        }

        // 2. Crear aprobación temporal de prueba en RL_MR_APROBACIONES
        long aprobacionId = await ObtenerSiguienteSecuenciaPruebaAsync(conn, null, "SEQ_RL_MR_APROBACIONES");
        _aprobacionesCreadas.Add(aprobacionId);
        const string sqlInsertAprobacion = @"
            INSERT INTO RL_MR_APROBACIONES (APR_ID, APR_FECHA_APROBACION, APR_USR_APROBACION)
            VALUES (:id, SYSDATE, 99)";
        await using (var cmd = new OracleCommand(sqlInsertAprobacion, conn))
        {
            cmd.Parameters.Add(new OracleParameter("id", aprobacionId));
            await cmd.ExecuteNonQueryAsync();
        }

        // ==========================================
        // CASO 1: COMMIT CONJUNTO (VÍNCULO Y AUDITORÍA TRANSVERSAL)
        // ==========================================
        var dtoVinculo = new AsociarEvidenciaAprobacionDto
        {
            EvapAprobacionId = aprobacionId,
            EvapEvidenciaId = evidenciaId,
            UsrId = 99
        };

        bool exitoVinculo = await repository.VincularEvidenciaAprobacionAsync(dtoVinculo, 99, "127.0.0.1");
        Assert.True(exitoVinculo);

        // Validar inserción en la tabla puente RL_MR_EVI_APROBACION
        const string sqlCheckVinculo = "SELECT COUNT(*) FROM RL_MR_EVI_APROBACION WHERE EVAP_APROBACION_ID = :aprobacionId AND EVAP_EVIDENCIA_ID = :evidenciaId";
        await using (var cmd = new OracleCommand(sqlCheckVinculo, conn))
        {
            cmd.Parameters.Add(new OracleParameter("aprobacionId", aprobacionId));
            cmd.Parameters.Add(new OracleParameter("evidenciaId", evidenciaId));
            int conteo = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            Assert.Equal(1, conteo);
        }

        // Validar que se llamó al stub de auditoría
        Assert.True(auditoriaStub.RegistrarLlamado);
        Assert.Equal("RL_MR_EVI_APROBACION", auditoriaStub.UltimaTabla);
        Assert.Equal($"{aprobacionId}:{evidenciaId}", auditoriaStub.UltimoRegistroId);

        // Limpiar vínculo
        const string sqlDeleteVinculo = "DELETE FROM RL_MR_EVI_APROBACION WHERE EVAP_APROBACION_ID = :aprobacionId AND EVAP_EVIDENCIA_ID = :evidenciaId";
        await using (var cmd = new OracleCommand(sqlDeleteVinculo, conn))
        {
            cmd.Parameters.Add(new OracleParameter("aprobacionId", aprobacionId));
            cmd.Parameters.Add(new OracleParameter("evidenciaId", evidenciaId));
            await cmd.ExecuteNonQueryAsync();
        }

        // ==========================================
        // CASO 2: ROLLBACK CUANDO FALLA LA AUDITORÍA TRANSVERSAL
        // ==========================================
        auditoriaStub.Reset();
        auditoriaStub.LanzarError = true; // Forzar excepción post-insert de vínculo

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repository.VincularEvidenciaAprobacionAsync(dtoVinculo, 99, "127.0.0.1"));
        Assert.Contains("Fallo inducido en la auditoría transversal", ex.Message);

        // Verificar que por el rollback no quedó rastro en la tabla puente
        await using (var cmd = new OracleCommand(sqlCheckVinculo, conn))
        {
            cmd.Parameters.Add(new OracleParameter("aprobacionId", aprobacionId));
            cmd.Parameters.Add(new OracleParameter("evidenciaId", evidenciaId));
            int conteo = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            Assert.Equal(0, conteo); // Revertido exitosamente
        }
    }

    [Fact]
    public async Task Certificacion_Fase12_PruebaDeIntegracion_Rollback_PorFalloDeVinculoOracle()
    {
        if (!await ValidarEntornoEjecucionAsync()) return;

        var dbContext = new OracleDbContext(_connectionString!);
        var auditoriaStub = new AuditoriaRepositoryStub();
        var repository = new MatricesRiesgosRepository(dbContext, auditoriaStub);

        await using var conn = dbContext.CreateConnection();
        await conn.OpenAsync();

        // Evidencia inexistente (Fallo previo al insert)
        var dtoInexistente = new AsociarEvidenciaAprobacionDto
        {
            EvapAprobacionId = 999999,
            EvapEvidenciaId = 888888, // Inexistente
            UsrId = 99
        };

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            repository.VincularEvidenciaAprobacionAsync(dtoInexistente, 99, "127.0.0.1"));

        Assert.False(auditoriaStub.RegistrarLlamado); // No debe registrar auditoría si falla el vínculo

        // Fallo físico en Oracle (Duplicación de PK/UQ)
        long evidenciaId = await ObtenerSiguienteSecuenciaPruebaAsync(conn, null, "SEQ_RL_MR_EVIDENCIAS");
        _evidenciasCreadas.Add(evidenciaId);
        long aprobacionId = await ObtenerSiguienteSecuenciaPruebaAsync(conn, null, "SEQ_RL_MR_APROBACIONES");
        _aprobacionesCreadas.Add(aprobacionId);

        const string sqlInsertEvidencia = @"
            INSERT INTO RL_MR_EVIDENCIAS (EVI_ID, EVI_NOMBRE_ARCHIVO, EVI_EXTENSION, EVI_TAMANO, EVI_HASH, EVI_RUTA, EVI_USR_CREACION)
            VALUES (:id, :nombre, '.txt', 1024, 'abc123hash', '/pruebas/txt', 99)";
        await using (var cmd = new OracleCommand(sqlInsertEvidencia, conn))
        {
            cmd.Parameters.Add(new OracleParameter("id", evidenciaId));
            cmd.Parameters.Add(new OracleParameter("nombre", TestPrefix + "archivo_2.txt"));
            await cmd.ExecuteNonQueryAsync();
        }

        const string sqlInsertAprobacion = @"
            INSERT INTO RL_MR_APROBACIONES (APR_ID, APR_FECHA_APROBACION, APR_USR_APROBACION)
            VALUES (:id, SYSDATE, 99)";
        await using (var cmd = new OracleCommand(sqlInsertAprobacion, conn))
        {
            cmd.Parameters.Add(new OracleParameter("id", aprobacionId));
            await cmd.ExecuteNonQueryAsync();
        }

        var dtoNormal = new AsociarEvidenciaAprobacionDto
        {
            EvapAprobacionId = aprobacionId,
            EvapEvidenciaId = evidenciaId,
            UsrId = 99
        };

        // Insertar el primer vínculo exitosamente
        bool primerExito = await repository.VincularEvidenciaAprobacionAsync(dtoNormal, 99, "127.0.0.1");
        Assert.True(primerExito);

        auditoriaStub.Reset();

        // Reintentar el mismo vínculo exacto (forzar fallo de PK/UQ en la tabla puente)
        await Assert.ThrowsAnyAsync<Exception>(() =>
            repository.VincularEvidenciaAprobacionAsync(dtoNormal, 99, "127.0.0.1"));

        // No debe llamarse a auditoría en el segundo intento fallido
        Assert.False(auditoriaStub.RegistrarLlamado);
    }

    [Fact]
    public async Task Certificacion_Fase12_PruebaDeIntegracion_CompensacionEvidencias_FisicoYOracle()
    {
        if (!await ValidarEntornoEjecucionAsync()) return;

        var dbContext = new OracleDbContext(_connectionString!);
        var repository = new MatricesRiesgosRepository(dbContext);

        await using var conn = dbContext.CreateConnection();
        await conn.OpenAsync();

        // Crear una evidencia física de prueba
        long evidenciaId = await ObtenerSiguienteSecuenciaPruebaAsync(conn, null, "SEQ_RL_MR_EVIDENCIAS");
        _evidenciasCreadas.Add(evidenciaId);

        const string sqlInsert = @"
            INSERT INTO RL_MR_EVIDENCIAS (EVI_ID, EVI_NOMBRE_ARCHIVO, EVI_EXTENSION, EVI_TAMANO, EVI_HASH, EVI_RUTA, EVI_USR_CREACION)
            VALUES (:id, :nombre, '.txt', 10, 'hashf', '/pruebas/ruta', 99)";
        await using (var cmd = new OracleCommand(sqlInsert, conn))
        {
            cmd.Parameters.Add(new OracleParameter("id", evidenciaId));
            cmd.Parameters.Add(new OracleParameter("nombre", TestPrefix + "fisico.txt"));
            await cmd.ExecuteNonQueryAsync();
        }

        // Escenario 1: Simulación de fallo físico en disco (revierte Oracle)
        bool archivoEliminadoFisico = false;
        var exitoEliminacion = await repository.EliminarEvidenciaSeguraAsync(evidenciaId, () =>
        {
            // Simular fallo de I/O
            throw new IOException("Fallo de acceso en disco simulado.");
        }, 99, "127.0.0.1");

        Assert.Equal(ResultadoEliminacionEvidencia.FalloDisco, exitoEliminacion);

        // Verificar que el registro permaneció intacto en base de datos Oracle
        const string sqlCheck = "SELECT COUNT(*) FROM RL_MR_EVIDENCIAS WHERE EVI_ID = :id";
        await using (var cmd = new OracleCommand(sqlCheck, conn))
        {
            cmd.Parameters.Add(new OracleParameter("id", evidenciaId));
            int conteo = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            Assert.Equal(1, conteo);
        }

        // Escenario 2: Eliminación exitosa (registro y archivo desaparecen)
        var exitoReal = await repository.EliminarEvidenciaSeguraAsync(evidenciaId, () =>
        {
            archivoEliminadoFisico = true;
            return Task.FromResult(true);
        }, 99, "127.0.0.1");

        Assert.Equal(ResultadoEliminacionEvidencia.Exito, exitoReal);
        Assert.True(archivoEliminadoFisico);

        // Verificar que desapareció de base de datos
        await using (var cmd = new OracleCommand(sqlCheck, conn))
        {
            cmd.Parameters.Add(new OracleParameter("id", evidenciaId));
            int conteo = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            Assert.Equal(0, conteo);
        }

        // Nota de Diseño (Compensación Física y Oracle):
        // Si la eliminación física de disco tiene éxito pero el commit posterior de base de datos
        // falla por un error de conexión, se documenta el riesgo residual de pérdida del archivo
        // físico, pendiente de aceptación expresa de Javier Mejía para implementar estrategias
        // futuras de cuarentena o movimiento temporal.
    }

    [Fact]
    public async Task Certificacion_Fase12_PruebaDeIntegracion_ConcurrenciaOptimistaYPesimista()
    {
        if (!await ValidarEntornoEjecucionAsync()) return;

        var dbContext = new OracleDbContext(_connectionString!);
        var repository = new MatricesRiesgosRepository(dbContext);

        await using var conn = dbContext.CreateConnection();
        await conn.OpenAsync();

        // 1. Preparar datos de prueba para concurrencia optimista
        long riesgoId = 1; // Asumimos riesgo ID 1 existe en el esquema de desarrollo
        long versionId = 1; // Asumimos version ID 1 existe y está publicada

        long evaluacionId = await ObtenerSiguienteSecuenciaPruebaAsync(conn, null, "SEQ_RL_MR_EVALUACIONES");
        _evaluacionesCreadas.Add(evaluacionId);

        // Sembrar evaluación inicial con versión de fila 1
        const string sqlInsertEva = @"
            INSERT INTO RL_MR_EVALUACIONES_RIESGO (EVA_ID, EVA_RIESGO_ID, EVA_VERSION_ID, EVA_DATA_JSON, EVA_DATA_CALC_JSON, EVA_FECHA_REGISTRO, EVA_USR_REGISTRO, EVA_VERSION_ROW, EVA_ACTIVO)
            VALUES (:id, :riesgoId, :versionId, '{}', '{}', SYSDATE, 99, 1, 1)";
        await using (var cmd = new OracleCommand(sqlInsertEva, conn))
        {
            cmd.Parameters.Add(new OracleParameter("id", evaluacionId));
            cmd.Parameters.Add(new OracleParameter("riesgoId", riesgoId));
            cmd.Parameters.Add(new OracleParameter("versionId", versionId));
            await cmd.ExecuteNonQueryAsync();
        }

        // ==========================================
        // CASO CONCURRENCIA OPTIMISTA DE EVALUACIONES
        // ==========================================
        var evaluacionLeida1 = await repository.ObtenerEvaluacionAsync(evaluacionId);
        var evaluacionLeida2 = await repository.ObtenerEvaluacionAsync(evaluacionId);

        Assert.NotNull(evaluacionLeida1);
        Assert.NotNull(evaluacionLeida2);
        Assert.Equal(1, evaluacionLeida1!.EvaVersionRow);

        // Hilo/Operación 1 actualiza con versión 1
        evaluacionLeida1.EvaDataJson = "{\"test\": 1}";
        bool exito1 = await repository.ActualizarEvaluacionAsync(evaluacionLeida1, 99, "127.0.0.1");
        Assert.True(exito1);

        // Verificar que incrementó versión a 2
        var evaluacionActualizada = await repository.ObtenerEvaluacionAsync(evaluacionId);
        Assert.Equal(2, evaluacionActualizada!.EvaVersionRow);

        // Hilo/Operación 2 intenta actualizar usando versión vieja 1
        evaluacionLeida2.EvaDataJson = "{\"test\": 2}";
        bool exito2 = await repository.ActualizarEvaluacionAsync(evaluacionLeida2, 99, "127.0.0.1");
        
        // Debe fallar por control optimista de versión de fila
        Assert.False(exito2);

        // ==========================================
        // CASO BLOQUEO PESIMISTA DE EVIDENCIA (FOR UPDATE)
        // ==========================================
        long evidenciaId = await ObtenerSiguienteSecuenciaPruebaAsync(conn, null, "SEQ_RL_MR_EVIDENCIAS");
        _evidenciasCreadas.Add(evidenciaId);

        const string sqlInsertEvi = @"
            INSERT INTO RL_MR_EVIDENCIAS (EVI_ID, EVI_NOMBRE_ARCHIVO, EVI_EXTENSION, EVI_TAMANO, EVI_HASH, EVI_RUTA, EVI_USR_CREACION)
            VALUES (:id, :nombre, '.txt', 10, 'hashf', '/pruebas/ruta', 99)";
        await using (var cmd = new OracleCommand(sqlInsertEvi, conn))
        {
            cmd.Parameters.Add(new OracleParameter("id", evidenciaId));
            cmd.Parameters.Add(new OracleParameter("nombre", TestPrefix + "concurrencia.txt"));
            await cmd.ExecuteNonQueryAsync();
        }

        // Iniciar Conexión A y Transaction A para adquirir bloqueo FOR UPDATE
        await using var connA = dbContext.CreateConnection();
        await connA.OpenAsync();
        await using var transA = connA.BeginTransaction();

        const string sqlLock = "SELECT EVI_ID FROM RL_MR_EVIDENCIAS WHERE EVI_ID = :id FOR UPDATE NOWAIT";
        await using (var cmdLock = new OracleCommand(sqlLock, connA))
        {
            cmdLock.Transaction = transA;
            cmdLock.Parameters.Add(new OracleParameter("id", evidenciaId));
            object? idLocked = await cmdLock.ExecuteScalarAsync();
            Assert.NotNull(idLocked);
        }

        // Iniciar Conexión B y Transaction B paralela e intentar bloquear o borrar el mismo registro con NOWAIT
        await using var connB = dbContext.CreateConnection();
        await connB.OpenAsync();
        await using var transB = connB.BeginTransaction();

        // Debe lanzar ORA-00054 (Resource busy and acquire with NOWAIT specified)
        var dex = await Assert.ThrowsAsync<OracleException>(async () =>
        {
            await using var cmdLockB = new OracleCommand(sqlLock, connB);
            cmdLockB.Transaction = transB;
            cmdLockB.Parameters.Add(new OracleParameter("id", evidenciaId));
            await cmdLockB.ExecuteScalarAsync();
        });

        Assert.Equal(54, dex.Number); // ORA-00054

        await transB.RollbackAsync();
        await transA.RollbackAsync();
    }

    [Fact]
    public async Task Certificacion_Fase12_PruebaDeIntegracion_ReglasVersionadasYTrazasDeCalculo()
    {
        if (!await ValidarEntornoEjecucionAsync()) return;

        var dbContext = new OracleDbContext(_connectionString!);
        var repository = new MatricesRiesgosRepository(dbContext);

        await using var conn = dbContext.CreateConnection();
        await conn.OpenAsync();

        // 1. Crear versión de formulario temporal de prueba
        long versionId = await ObtenerSiguienteSecuenciaPruebaAsync(conn, null, "SEQ_RL_MR_VERSIONES");
        _versionesCreadas.Add(versionId);

        const string sqlInsertVersion = @"
            INSERT INTO RL_MR_VERSIONES_FORMULARIO (
                VER_ID, VER_FAMILIA_ID, VER_CODIGO, VER_VERSION, VER_JSON, VER_HASH, VER_ESTADO, VER_VIGENTE, VER_FECHA_INICIO, VER_USR_CREACION
            ) VALUES (
                :id, 1, :codigo, 99, :json, 'hashv', 'PUBLISHED', 1, SYSDATE, 99
            )";

        string verJson = @"
        {
            ""reglas"": [
                {
                    ""codigo"": ""CALCULO_VRI_VRR"",
                    ""version"": ""1.0"",
                    ""algoritmo"": ""MATRICES_VRI_ADITIVO_1_9"",
                    ""parametros"": {}
                }
            ]
        }";

        await using (var cmd = new OracleCommand(sqlInsertVersion, conn))
        {
            cmd.Parameters.Add(new OracleParameter("id", versionId));
            cmd.Parameters.Add(new OracleParameter("codigo", TestPrefix + "FORM"));
            cmd.Parameters.Add(new OracleParameter("json", OracleDbType.Clob) { Value = verJson });
            await cmd.ExecuteNonQueryAsync();
        }

        // 2. Crear una evaluación
        long evaluacionId = await ObtenerSiguienteSecuenciaPruebaAsync(conn, null, "SEQ_RL_MR_EVALUACIONES");
        _evaluacionesCreadas.Add(evaluacionId);

        var dtoEva = new EvaluacionRiesgoDto
        {
            EvaRiesgoId = 1, // Asumimos riesgo ID 1 existe
            EvaVersionId = versionId,
            EvaDataJson = "{\"frecuencia\": 3, \"impacto\": 4}",
            EvaDataCalcJson = "{\"vri\": 6, \"nivel\": \"Moderado\"}" // Frecuencia 3 + Impacto 4 - 1 = 6
        };

        long createdId = await repository.CrearEvaluacionAsync(dtoEva, 99, "127.0.0.1");
        Assert.True(createdId > 0);

        // 3. Validar que se haya persistido la traza en RL_MR_TRAZAS_CALCULO
        const string sqlCheckTraza = @"
            SELECT t.TRA_REGLA_ID, t.TRA_FORMULA_APLICADA, r.REG_CODIGO, r.REG_VERSION, r.REG_ALGORITMO
              FROM RL_MR_TRAZAS_CALCULO t
              JOIN RL_MR_REGLAS_CALCULO r ON r.REG_ID = t.TRA_REGLA_ID
             WHERE t.TRA_EVALUACION_ID = :evaluacionId";

        await using (var cmd = new OracleCommand(sqlCheckTraza, conn))
        {
            cmd.Parameters.Add(new OracleParameter("evaluacionId", createdId));
            await using var reader = await cmd.ExecuteReaderAsync();
            Assert.True(await reader.ReadAsync());

            Assert.True(reader.GetInt64(0) > 0); // TRA_REGLA_ID
            Assert.Equal("VRI = Frecuencia + Impacto - 1", reader.GetString(1)); // Fórmula aplicada en base a la semilla
            Assert.Equal("CALCULO_VRI_VRR", reader.GetString(2)); // Código de regla
            Assert.Equal("1.0", reader.GetString(3)); // Versión de regla
            Assert.Equal("MATRICES_VRI_ADITIVO_1_9", reader.GetString(4)); // Algoritmo
        }
    }

    public async ValueTask DisposeAsync()
    {
        // Limpieza física incondicional de los datos de prueba insertados en base de datos
        if (string.IsNullOrWhiteSpace(_connectionString)) return;

        try
        {
            await using var conn = new OracleConnection(_connectionString);
            await conn.OpenAsync();

            // 1. Eliminar vínculos temporales de aprobaciones
            foreach (long aprobacionId in _aprobacionesCreadas)
            {
                const string sqlDeleteVinculo = "DELETE FROM RL_MR_EVI_APROBACION WHERE EVAP_APROBACION_ID = :aprobacionId";
                await using var cmd = new OracleCommand(sqlDeleteVinculo, conn);
                cmd.Parameters.Add(new OracleParameter("aprobacionId", aprobacionId));
                await cmd.ExecuteNonQueryAsync();
            }

            // 2. Eliminar aprobaciones temporales
            foreach (long aprobacionId in _aprobacionesCreadas)
            {
                const string sqlDelete = "DELETE FROM RL_MR_APROBACIONES WHERE APR_ID = :id";
                await using var cmd = new OracleCommand(sqlDelete, conn);
                cmd.Parameters.Add(new OracleParameter("id", aprobacionId));
                await cmd.ExecuteNonQueryAsync();
            }

            // 3. Eliminar vínculos e información relacionada a evaluaciones de prueba creadas
            foreach (long evaluacionId in _evaluacionesCreadas)
            {
                // Limpiar trazas
                const string sqlDeleteTrazas = "DELETE FROM RL_MR_TRAZAS_CALCULO WHERE TRA_EVALUACION_ID = :id";
                await using (var cmd = new OracleCommand(sqlDeleteTrazas, conn))
                {
                    cmd.Parameters.Add(new OracleParameter("id", evaluacionId));
                    await cmd.ExecuteNonQueryAsync();
                }

                // Limpiar proyecciones
                const string sqlDeleteProy = "DELETE FROM RL_MR_PROYECCIONES_EVALUACION WHERE PROY_EVALUACION_ID = :id";
                await using (var cmd = new OracleCommand(sqlDeleteProy, conn))
                {
                    cmd.Parameters.Add(new OracleParameter("id", evaluacionId));
                    await cmd.ExecuteNonQueryAsync();
                }

                // Limpiar flujos
                const string sqlDeleteFlujos = "DELETE FROM RL_MR_FLUJOS_EVALUACION WHERE FLU_EVALUACION_ID = :id";
                await using (var cmd = new OracleCommand(sqlDeleteFlujos, conn))
                {
                    cmd.Parameters.Add(new OracleParameter("id", evaluacionId));
                    await cmd.ExecuteNonQueryAsync();
                }

                // Limpiar auditoría interna del módulo
                const string sqlDeleteAud = "DELETE FROM RL_MR_AUDITORIA WHERE AUD_EVALUACION_ID = :id";
                await using (var cmd = new OracleCommand(sqlDeleteAud, conn))
                {
                    cmd.Parameters.Add(new OracleParameter("id", evaluacionId));
                    await cmd.ExecuteNonQueryAsync();
                }

                // Eliminar evaluación
                const string sqlDeleteEva = "DELETE FROM RL_MR_EVALUACIONES_RIESGO WHERE EVA_ID = :id";
                await using (var cmd = new OracleCommand(sqlDeleteEva, conn))
                {
                    cmd.Parameters.Add(new OracleParameter("id", evaluacionId));
                    await cmd.ExecuteNonQueryAsync();
                }
            }

            // 4. Eliminar evidencias de prueba
            foreach (long evidenciaId in _evidenciasCreadas)
            {
                const string sqlDeleteEvi = "DELETE FROM RL_MR_EVIDENCIAS WHERE EVI_ID = :id";
                await using var cmd = new OracleCommand(sqlDeleteEvi, conn);
                cmd.Parameters.Add(new OracleParameter("id", evidenciaId));
                await cmd.ExecuteNonQueryAsync();
            }

            // 5. Eliminar versiones de formulario temporales
            foreach (long versionId in _versionesCreadas)
            {
                const string sqlDeleteVer = "DELETE FROM RL_MR_VERSIONES_FORMULARIO WHERE VER_ID = :id";
                await using var cmd = new OracleCommand(sqlDeleteVer, conn);
                cmd.Parameters.Add(new OracleParameter("id", versionId));
                await cmd.ExecuteNonQueryAsync();
            }
        }
        catch (Exception ex)
        {
            _output.WriteLine($"[ADVERTENCIA] Error durante la limpieza física de datos de prueba: {ex.Message}");
        }
    }

    // ==========================================
    // STUB DE REPOSITORIO DE AUDITORÍA TRANSVERSAL
    // ==========================================
    private sealed class AuditoriaRepositoryStub : IAuditoriaRepository
    {
        public bool LanzarError { get; set; }
        public bool RegistrarLlamado { get; private set; }
        public string? UltimaTabla { get; private set; }
        public string? UltimoRegistroId { get; private set; }

        public void Reset()
        {
            LanzarError = false;
            RegistrarLlamado = false;
            UltimaTabla = null;
            UltimoRegistroId = null;
        }

        public Task RegistrarAsync(string tabla, string registroId, string accion, string? datosAnt, string? datosNvo, long? usrId, string? email, string? ip, string? modulo)
        {
            RegistrarLlamado = true;
            UltimaTabla = tabla;
            UltimoRegistroId = registroId;
            if (LanzarError)
            {
                throw new InvalidOperationException("Fallo inducido en la auditoría transversal para rollback.");
            }
            return Task.CompletedTask;
        }

        public Task RegistrarAsync(OracleConnection connection, OracleTransaction? transaction, string tabla, string registroId, string accion, string? datosAnt, string? datosNvo, long? usrId, string? email, string? ip, string? modulo)
        {
            RegistrarLlamado = true;
            UltimaTabla = tabla;
            UltimoRegistroId = registroId;
            if (LanzarError)
            {
                throw new InvalidOperationException("Fallo inducido en la auditoría transversal para rollback.");
            }
            return Task.CompletedTask;
        }

        public Task<(List<AuditoriaDto> Datos, int Total)> ObtenerBitacoraPaginadaAsync(int pagina, int limite, string? buscar, string? accion, string? modulo, string? tabla, DateTime? fechaInicio, DateTime? fechaFin)
        {
            return Task.FromResult<(List<AuditoriaDto> Datos, int Total)>((new List<AuditoriaDto>(), 0));
        }
    }
}
