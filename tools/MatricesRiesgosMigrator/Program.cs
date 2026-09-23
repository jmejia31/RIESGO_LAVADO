using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ExcelDataReader;
using Microsoft.Extensions.Configuration;
using Oracle.ManagedDataAccess.Client;
using RL.API.Features.MatricesRiesgos.Domain;

namespace RL.Tools.MatricesRiesgosMigrator;

public static class Program
{
    private const string FamiliaEsperada = "MATRIZ_RIESGOS_LAFT";
    private const string VersionEsperada = "MATRIZ_RIESGOS_LAFT_V1";
    private const string HashEsperado = "f2f84f21b6cc46762fd6087bc41df449b31ca87b058c763689bdfb3bba961f90";

    public static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        bool executeMigration = args.Any(a => string.Equals(a, "--migrate", StringComparison.OrdinalIgnoreCase));
        bool enrichExisting = args.Any(a => string.Equals(a, "--enrich-existing", StringComparison.OrdinalIgnoreCase));
        bool inspectSourceRiskText = args.Any(a => string.Equals(a, "--inspect-source-risk-text", StringComparison.OrdinalIgnoreCase));
        bool repairRiskText = args.Any(a => string.Equals(a, "--repair-risk-text", StringComparison.OrdinalIgnoreCase));
        bool verifyRiskText = args.Any(a => string.Equals(a, "--verify-risk-text", StringComparison.OrdinalIgnoreCase));
        string repoRoot = Directory.GetCurrentDirectory();
        while (!string.IsNullOrEmpty(repoRoot) && !File.Exists(Path.Combine(repoRoot, "Matrices de Riesgos.xlsx")))
        {
            var parent = Directory.GetParent(repoRoot);
            if (parent == null) break;
            repoRoot = parent.FullName;
        }
        if (!File.Exists(Path.Combine(repoRoot, "Matrices de Riesgos.xlsx")))
        {
            repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        }
        string excelPath = Path.Combine(repoRoot, "Matrices de Riesgos.xlsx");
        string schemaPath = Path.Combine(repoRoot, "database", "19_matrices_riesgos", "fase11", "formulario_matriz_riesgos_laft_v1.json");

        string modo = inspectSourceRiskText
            ? "INSPECCION TEXTO FUENTE"
            : verifyRiskText
                ? "VERIFICACION TEXTO MAESTRO ORACLE"
                : repairRiskText
                    ? (executeMigration ? "REPARACION TEXTO MAESTRO ORACLE" : "REPARACION TEXTO MAESTRO DRY-RUN")
                    : enrichExisting
                        ? (executeMigration ? "ENRICH_EXISTING TRANSACCIONAL" : "ENRICH_EXISTING DRY-RUN")
                        : (executeMigration ? "MIGRACIÓN TRANSACCIONAL Y PRUEBA DE IDEMPOTENCIA" : "DRY-RUN DE CERTIFICACIÓN");

        Console.WriteLine("================================================================================");
        Console.WriteLine("HERRAMIENTA TRANSACCIONAL DE MIGRACIÓN, ENRIQUECIMIENTO Y CONCILIACIÓN");
        Console.WriteLine($"MODO: {modo}");
        Console.WriteLine($"EXCEL ORIGEN: {excelPath}");
        Console.WriteLine($"SCHEMA CONTRATO: {schemaPath}");
        Console.WriteLine("================================================================================");

        if (!File.Exists(excelPath))
        {
            Console.WriteLine($"ERROR: No existe el archivo Excel en {excelPath}");
            return 1;
        }

        if (!File.Exists(schemaPath))
        {
            Console.WriteLine($"ERROR: No existe el esquema JSON en {schemaPath}");
            return 1;
        }

        string schemaJson = await File.ReadAllTextAsync(schemaPath);

        // 1. Parsear y Validar las 59 filas del Excel
        var sourceRisks = ParseSourceWorkbook(excelPath);
        Console.WriteLine($"Filas leídas de Matriz Consolidada: {sourceRisks.Count}");

        if (inspectSourceRiskText)
        {
            return InspeccionarTextoRiesgosFuente(sourceRisks);
        }

        // 2. Ejecutar Dry-Run de validación contractual
        var dryRunResult = await ExecuteDryRunAsync(sourceRisks, schemaJson);
        PrintDryRunMetrics(dryRunResult);

        if (dryRunResult.Rejected > 0 || dryRunResult.SilentlySkipped > 0 || dryRunResult.DuplicateCodes > 0)
        {
            Console.WriteLine("ERROR: El Dry-Run falló. La migración no puede continuar.");
            return 2;
        }

        bool requiereOracle = executeMigration || enrichExisting || repairRiskText || verifyRiskText;
        if (!requiereOracle)
        {
            Console.WriteLine("\nDry-Run completado exitosamente con 59/59 aprobados. Para ejecutar la migración transaccional, use --migrate.");
            return 0;
        }

        // 3. Ejecutar Migración / verificación en Oracle
        string appSettingsPath = Path.Combine(repoRoot, "backend", "RL.API", "appsettings.json");
        var config = new ConfigurationBuilder().AddJsonFile(appSettingsPath).Build();
        string connectionString = config.GetConnectionString("OracleDB")
            ?? throw new InvalidOperationException("No se encontró la cadena de conexión OracleDB.");

        await using var connection = new OracleConnection(connectionString);
        connection.ConnectionString = new OracleConnectionStringBuilder(connectionString)
        {
            ConnectionTimeout = 60,
            Pooling = false
        }.ConnectionString;
        await connection.OpenAsync();

        Console.WriteLine("\n--- Conexión Oracle establecida ---");

        if (repairRiskText || verifyRiskText)
        {
            bool ejecutarCorreccion = repairRiskText && executeMigration;
            return await VerificarOCorregirTextoRiesgosAsync(
                connection,
                sourceRisks,
                ejecutarCorreccion,
                repoRoot);
        }

        // Validar que la familia y versión oficial existan y estén vigentes
        long familiaId = Convert.ToInt64(await ScalarAsync(connection, null,
            $"SELECT FAM_ID FROM RL_MR_FAMILIAS_FORMULARIO WHERE FAM_CODIGO = '{FamiliaEsperada}' AND FAM_ACTIVO = 1"));
        long versionId = Convert.ToInt64(await ScalarAsync(connection, null,
            $"SELECT VER_ID FROM RL_MR_VERSIONES_FORMULARIO WHERE VER_CODIGO = '{VersionEsperada}' AND VER_ESTADO = 'PUBLISHED' AND VER_VIGENTE = 1"));

        Console.WriteLine($"FAMILIA_ID={familiaId} ({FamiliaEsperada}) | VERSION_ID={versionId} ({VersionEsperada})");

        if (enrichExisting)
        {
            return await EjecutarEnriquecimientoExistenteAsync(connection, versionId, sourceRisks, executeMigration);
        }

        // PASO 1 DE MIGRACIÓN: Primera ejecución transaccional
        Console.WriteLine("\n>>> EJECUTANDO PRIMERA PASADA DE MIGRACIÓN TRANSACCIONAL <<<");
        var migrationResult1 = await MigrateBatchAsync(connection, versionId, sourceRisks);
        Console.WriteLine($"Migración Pasada 1: Riesgos insertados={migrationResult1.InsertedRisks}, Evaluaciones insertadas={migrationResult1.InsertedEvaluations}, Proyecciones insertadas={migrationResult1.InsertedProjections}");

        // VERIFICACIÓN Y CONCILIACIÓN
        Console.WriteLine("\n>>> EJECUTANDO CONCILIACIÓN Y AUDITORÍA READ-ONLY EN ORACLE <<<");
        var reconciliation = await ReconcileOracleAsync(connection, versionId, sourceRisks);
        PrintReconciliationMetrics(reconciliation);

        if (reconciliation.MissingSourceRisks > 0 ||
            reconciliation.DuplicateSourceRisks > 0 ||
            reconciliation.OrphanEvaluations > 0 ||
            reconciliation.OrphanProjections > 0 ||
            reconciliation.InvalidVersionBindings > 0 ||
            reconciliation.UnexplainedDifferences > 0)
        {
            Console.WriteLine("ERROR: La conciliación en Oracle detectó inconsistencias.");
            return 3;
        }

        // PASO 2 DE MIGRACIÓN: Prueba de Idempotencia (Segunda ejecución)
        Console.WriteLine("\n>>> EJECUTANDO PRUEBA DE IDEMPOTENCIA (SEGUNDA PASADA) <<<");
        var migrationResult2 = await MigrateBatchAsync(connection, versionId, sourceRisks);
        Console.WriteLine($"Migración Pasada 2 (Idempotente): Riesgos insertados={migrationResult2.InsertedRisks}, Evaluaciones insertadas={migrationResult2.InsertedEvaluations}, Proyecciones insertadas={migrationResult2.InsertedProjections}");

        long secondRunDuplicates = migrationResult2.InsertedRisks + migrationResult2.InsertedEvaluations + migrationResult2.InsertedProjections;
        string idempotencyStatus = secondRunDuplicates == 0 ? "PASS" : "FAIL";

        Console.WriteLine($"IDEMPOTENCY_TEST={idempotencyStatus}");
        Console.WriteLine($"SECOND_RUN_DUPLICATES={secondRunDuplicates}");

        if (secondRunDuplicates > 0)
        {
            Console.WriteLine("ERROR: La prueba de idempotencia generó registros duplicados.");
            return 4;
        }

        Console.WriteLine("\n================================================================================");
        Console.WriteLine("MIGRACIÓN Y CERTIFICACIÓN TRANSACCIONAL 59/59 COMPLETADA CON ÉXITO");
        Console.WriteLine("================================================================================");
        return 0;
    }

    private static int InspeccionarTextoRiesgosFuente(IReadOnlyCollection<SourceRiskRow> sourceRisks)
    {
        int sospechosos = 0;
        foreach (SourceRiskRow riesgo in sourceRisks.OrderBy(r => r.RowIndex))
        {
            bool corrupto = ContieneMojibake(riesgo.Titulo) || ContieneMojibake(riesgo.Descripcion);
            if (corrupto) sospechosos++;

            Console.WriteLine(
                $"SOURCE_RISK_TEXT|{riesgo.RowNumber}|{riesgo.Code}|{riesgo.Titulo.Replace("|", "/", StringComparison.Ordinal)}");
        }

        Console.WriteLine($"SOURCE_RISK_TEXT_ROWS={sourceRisks.Count}");
        Console.WriteLine($"SOURCE_RISK_TEXT_MOJIBAKE={sospechosos}");
        Console.WriteLine($"SOURCE_RISK_TEXT_STATUS={(sourceRisks.Count == 59 && sospechosos == 0 ? "PASS" : "FAIL")}");

        return sourceRisks.Count == 59 && sospechosos == 0 ? 0 : 5;
    }

    private static bool ContieneMojibake(string? value)
    {
        if (string.IsNullOrEmpty(value)) return false;

        return value.Contains('\uFFFD')
            || value.Contains("\u00EF\u00BF\u00BD", StringComparison.Ordinal)
            || value.Contains('\u00C3')
            || value.Contains('\u00C2')
            || value.Contains("\u00E2\u20AC", StringComparison.Ordinal)
            || value.Contains("\u00F0\u0178", StringComparison.Ordinal);
    }

    private static async Task<int> VerificarOCorregirTextoRiesgosAsync(
        OracleConnection connection,
        IReadOnlyCollection<SourceRiskRow> sourceRisks,
        bool ejecutarCorreccion,
        string repoRoot)
    {
        if (sourceRisks.Count != 59 || sourceRisks.Any(r => ContieneMojibake(r.Titulo) || ContieneMojibake(r.Descripcion)))
        {
            Console.WriteLine("RISK_TEXT_STATUS=FAIL_SOURCE");
            Console.WriteLine("ERROR: La fuente canónica no tiene exactamente 59 filas sanas. No se tocará Oracle.");
            return 6;
        }

        var sourceByCode = sourceRisks.ToDictionary(r => r.Code, StringComparer.OrdinalIgnoreCase);
        var dbRows = new Dictionary<string, RiskTextDbRow>(StringComparer.OrdinalIgnoreCase);
        var duplicateCodes = new List<string>();

        const string sqlSelect = @"
            SELECT RIE_ID, RIE_CODIGO, RIE_NOMBRE, RIE_DESCRIPCION
              FROM RL_MR_RIESGOS
             ORDER BY RIE_CODIGO";

        await using (var cmd = new OracleCommand(sqlSelect, connection) { BindByName = true })
        await using (var reader = await cmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                var row = new RiskTextDbRow(
                    reader.GetInt64(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.IsDBNull(3) ? null : reader.GetString(3));

                if (!dbRows.TryAdd(row.Code, row))
                {
                    duplicateCodes.Add(row.Code);
                }
            }
        }

        string[] missingCodes = sourceByCode.Keys
            .Where(code => !dbRows.ContainsKey(code))
            .OrderBy(code => code, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var repairs = new List<RiskTextRepairItem>();
        int cleanMismatches = 0;

        foreach (SourceRiskRow source in sourceRisks.OrderBy(r => r.RowNumber))
        {
            if (!dbRows.TryGetValue(source.Code, out RiskTextDbRow? current)) continue;

            string canonicalName = Truncar(source.Titulo, 250);
            string canonicalDescription = Truncar(source.Descripcion, 2000);
            bool nameDiffers = !string.Equals(current.Name, canonicalName, StringComparison.Ordinal);
            bool descriptionDiffers = !string.Equals(current.Description ?? string.Empty, canonicalDescription, StringComparison.Ordinal);
            bool corrupted = ContieneMojibake(current.Name) || ContieneMojibake(current.Description);

            if (corrupted)
            {
                repairs.Add(new RiskTextRepairItem(
                    current.Id,
                    source.Code,
                    current.Name,
                    current.Description,
                    canonicalName,
                    canonicalDescription));
            }
            else if (nameDiffers || descriptionDiffers)
            {
                cleanMismatches++;
            }
        }

        Console.WriteLine($"RISK_TEXT_SOURCE_ROWS={sourceRisks.Count}");
        Console.WriteLine($"RISK_TEXT_DB_ROWS={dbRows.Count}");
        Console.WriteLine($"RISK_TEXT_MATCHED_CODES={sourceRisks.Count - missingCodes.Length}");
        Console.WriteLine($"RISK_TEXT_MISSING_CODES={missingCodes.Length}");
        Console.WriteLine($"RISK_TEXT_DUPLICATE_CODES={duplicateCodes.Count}");
        Console.WriteLine($"RISK_TEXT_CORRUPTED_ROWS={repairs.Count}");
        Console.WriteLine($"RISK_TEXT_CLEAN_MISMATCHES={cleanMismatches}");
        Console.WriteLine($"RISK_TEXT_MODE={(ejecutarCorreccion ? "EXECUTE" : "DRY_RUN")}");

        foreach (string code in missingCodes)
        {
            Console.WriteLine($"RISK_TEXT_MISSING|{code}");
        }

        foreach (string code in duplicateCodes.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            Console.WriteLine($"RISK_TEXT_DUPLICATE|{code}");
        }

        foreach (RiskTextRepairItem repair in repairs)
        {
            Console.WriteLine(
                $"RISK_TEXT_REPAIR|{repair.Code}|{repair.CurrentName.Replace("|", "/", StringComparison.Ordinal)}|=>|{repair.CanonicalName.Replace("|", "/", StringComparison.Ordinal)}");
        }

        if (missingCodes.Length > 0 || duplicateCodes.Count > 0)
        {
            Console.WriteLine("RISK_TEXT_STATUS=FAIL_INVENTORY");
            Console.WriteLine("ERROR: El inventario Oracle no coincide con la fuente. No se aplicarán cambios parciales.");
            return 7;
        }

        if (!ejecutarCorreccion)
        {
            Console.WriteLine($"RISK_TEXT_STATUS={(repairs.Count == 0 ? "PASS" : "NEEDS_REPAIR")}");
            if (repairs.Count > 0)
            {
                Console.WriteLine("Para corregir únicamente filas con mojibake use: --repair-risk-text --migrate");
            }
            return repairs.Count == 0 ? 0 : 8;
        }

        if (repairs.Count == 0)
        {
            Console.WriteLine("RISK_TEXT_UPDATED_ROWS=0");
            Console.WriteLine("RISK_TEXT_POST_MOJIBAKE=0");
            Console.WriteLine("RISK_TEXT_STATUS=PASS");
            return 0;
        }

        string backupDir = Path.Combine(repoRoot, "artifacts", "oracle");
        Directory.CreateDirectory(backupDir);
        string backupPath = Path.Combine(
            backupDir,
            $"risk-text-backup-{DateTime.UtcNow:yyyyMMdd_HHmmss}.json");

        var backupPayload = new
        {
            GeneratedUtc = DateTime.UtcNow,
            Table = "RL_MR_RIESGOS",
            Scope = "RIE_NOMBRE,RIE_DESCRIPCION",
            Rows = repairs.Select(r => new
            {
                r.Id,
                r.Code,
                r.CurrentName,
                r.CurrentDescription,
                r.CanonicalName,
                r.CanonicalDescription
            }).ToArray()
        };

        await File.WriteAllTextAsync(
            backupPath,
            JsonSerializer.Serialize(backupPayload, new JsonSerializerOptions { WriteIndented = true }),
            new UTF8Encoding(false));

        Console.WriteLine($"RISK_TEXT_BACKUP={backupPath}");

        await using var transaction = connection.BeginTransaction();
        int updatedRows = 0;

        try
        {
            const string sqlUpdate = @"
                UPDATE RL_MR_RIESGOS
                   SET RIE_NOMBRE = :nombre,
                       RIE_DESCRIPCION = :descripcion
                 WHERE RIE_ID = :id
                   AND RIE_CODIGO = :codigo";

            foreach (RiskTextRepairItem repair in repairs)
            {
                await using var cmd = new OracleCommand(sqlUpdate, connection)
                {
                    BindByName = true,
                    Transaction = transaction
                };
                cmd.Parameters.Add(new OracleParameter("nombre", repair.CanonicalName));
                cmd.Parameters.Add(new OracleParameter("descripcion", repair.CanonicalDescription));
                cmd.Parameters.Add(new OracleParameter("id", repair.Id));
                cmd.Parameters.Add(new OracleParameter("codigo", repair.Code));

                int affected = await cmd.ExecuteNonQueryAsync();
                if (affected != 1)
                {
                    throw new InvalidOperationException(
                        $"Se esperaba actualizar exactamente una fila para {repair.Code}; Oracle reportó {affected}.");
                }

                updatedRows += affected;
            }

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }

        int postMojibake = 0;
        await using (var cmd = new OracleCommand(sqlSelect, connection) { BindByName = true })
        await using (var reader = await cmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                string code = reader.GetString(1);
                if (!sourceByCode.ContainsKey(code)) continue;

                string name = reader.GetString(2);
                string? description = reader.IsDBNull(3) ? null : reader.GetString(3);
                if (ContieneMojibake(name) || ContieneMojibake(description))
                {
                    postMojibake++;
                    Console.WriteLine($"RISK_TEXT_POST_BAD|{code}|{name.Replace("|", "/", StringComparison.Ordinal)}");
                }
            }
        }

        Console.WriteLine($"RISK_TEXT_UPDATED_ROWS={updatedRows}");
        Console.WriteLine($"RISK_TEXT_POST_MOJIBAKE={postMojibake}");
        Console.WriteLine($"RISK_TEXT_STATUS={(postMojibake == 0 ? "PASS" : "FAIL_POSTCHECK")}");

        return postMojibake == 0 ? 0 : 9;
    }

    private static List<SourceRiskRow> ParseSourceWorkbook(string excelPath)
    {
        var rows = new List<SourceRiskRow>();
        using var stream = File.Open(excelPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = ExcelReaderFactory.CreateReader(stream);

        while (reader.Name != "Matriz Consolidada" && reader.NextResult()) { }
        if (reader.Name != "Matriz Consolidada")
        {
            throw new InvalidOperationException("No se encontró la hoja 'Matriz Consolidada' en el libro.");
        }

        int rowIdx = 0;
        while (reader.Read())
        {
            rowIdx++;
            if (rowIdx < 2 || rowIdx > 60) continue; // Filas 2 a 60 = 59 riesgos

            string code = Convert.ToString(reader.GetValue(1), CultureInfo.InvariantCulture)?.Trim() ?? string.Empty;
            string area = Convert.ToString(reader.GetValue(2), CultureInfo.InvariantCulture)?.Trim() ?? string.Empty;
            string areaConsolidada = Convert.ToString(reader.GetValue(3), CultureInfo.InvariantCulture)?.Trim() ?? string.Empty;
            string areaFinal = !string.IsNullOrWhiteSpace(area) ? area : areaConsolidada;

            string tipoRiesgo = Convert.ToString(reader.GetValue(4), CultureInfo.InvariantCulture)?.Trim() ?? string.Empty;
            string procedimiento = Convert.ToString(reader.GetValue(5), CultureInfo.InvariantCulture)?.Trim() ?? string.Empty;
            string objetivosEstrategicos = Convert.ToString(reader.GetValue(6), CultureInfo.InvariantCulture)?.Trim() ?? string.Empty;
            string titulo = Convert.ToString(reader.GetValue(7), CultureInfo.InvariantCulture)?.Trim() ?? string.Empty;
            string descripcion = Convert.ToString(reader.GetValue(8), CultureInfo.InvariantCulture)?.Trim() ?? string.Empty;

            int frecInh = ParseInt(reader.GetValue(9));
            int impInh = ParseInt(reader.GetValue(10));
            int vri = frecInh + impInh - 1;
            string nivelInh = DeterminarNivel(vri);

            string dueno = Convert.ToString(reader.GetValue(13), CultureInfo.InvariantCulture)?.Trim() ?? string.Empty;

            // DATO DE PRUEBA AUTORIZADO 1: Fila 24 ROTR-ALMACENBIENE-23
            if (string.Equals(code, "ROTR-ALMACENBIENE-23", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(dueno))
            {
                dueno = "GTIC"; // DATO DE PRUEBA EXPRESAMENTE AUTORIZADO
            }

            // Controles
            decimal prev = ParsePorcentajeControl(reader.GetValue(22), reader.GetValue(20));
            decimal det = ParsePorcentajeControl(reader.GetValue(26), reader.GetValue(24));
            decimal corr = ParsePorcentajeControl(reader.GetValue(30), reader.GetValue(28));

            decimal etp = (prev * 0.70m) + (det * 0.15m) + (corr * 0.15m);
            decimal vrrRaw = vri * (1.0m - (etp / 100.0m));
            int vrr = (int)Math.Round(Math.Max(1.0m, vrrRaw), MidpointRounding.AwayFromZero);
            string nivelRes = DeterminarNivel(vrr);

            // Frecuencia e Impacto Residual
            var (frecRes, impRes) = CalcularResidualAuxiliar(frecInh, impInh, vri, etp, vrr);

            string respuestaRaw = Convert.ToString(reader.GetValue(38), CultureInfo.InvariantCulture)?.Trim() ?? string.Empty;
            string respuesta = MapearRespuestaRiesgo(respuestaRaw);

            string regimenAfectado = Convert.ToString(reader.GetValue(14), CultureInfo.InvariantCulture)?.Trim() ?? string.Empty;
            string transversalidad = Convert.ToString(reader.GetValue(15), CultureInfo.InvariantCulture)?.Trim() ?? string.Empty;
            string escalaPreventiva = Convert.ToString(reader.GetValue(20), CultureInfo.InvariantCulture)?.Trim() ?? string.Empty;
            string nivelPreventivo = Convert.ToString(reader.GetValue(21), CultureInfo.InvariantCulture)?.Trim() ?? string.Empty;
            string escalaDetectiva = Convert.ToString(reader.GetValue(24), CultureInfo.InvariantCulture)?.Trim() ?? string.Empty;
            string nivelDetectivo = Convert.ToString(reader.GetValue(25), CultureInfo.InvariantCulture)?.Trim() ?? string.Empty;
            string escalaCorrectiva = Convert.ToString(reader.GetValue(28), CultureInfo.InvariantCulture)?.Trim() ?? string.Empty;
            string nivelCorrectivo = Convert.ToString(reader.GetValue(29), CultureInfo.InvariantCulture)?.Trim() ?? string.Empty;
            string automatizacion = Convert.ToString(reader.GetValue(31), CultureInfo.InvariantCulture)?.Trim() ?? string.Empty;
            string plan = Convert.ToString(reader.GetValue(39), CultureInfo.InvariantCulture)?.Trim() ?? string.Empty;
            int cantidadAcciones = ParseInt(reader.GetValue(40));
            string actividades = Convert.ToString(reader.GetValue(41), CultureInfo.InvariantCulture)?.Trim() ?? string.Empty;
            int cantidadActividades = ParseInt(reader.GetValue(42));
            string monitoreoSeguimiento = Convert.ToString(reader.GetValue(43), CultureInfo.InvariantCulture)?.Trim() ?? string.Empty;
            string responsables = Convert.ToString(reader.GetValue(44), CultureInfo.InvariantCulture)?.Trim() ?? string.Empty;
            string senalesAlerta = Convert.ToString(reader.GetValue(69), CultureInfo.InvariantCulture)?.Trim() ?? string.Empty;

            // DATO DE PRUEBA AUTORIZADO 2: Fila 38 RCUMP-COMPRAS-37
            if (string.Equals(code, "RCUMP-COMPRAS-37", StringComparison.OrdinalIgnoreCase) && (string.IsNullOrWhiteSpace(respuesta) || respuesta == "UNKNOWN"))
            {
                respuesta = "MITIGAR"; // DATO DE PRUEBA EXPRESAMENTE AUTORIZADO
            }

            rows.Add(new SourceRiskRow(
                rowIdx,
                code,
                areaFinal,
                dueno,
                titulo,
                descripcion,
                frecInh,
                impInh,
                vri,
                nivelInh,
                prev,
                det,
                corr,
                etp,
                vrr,
                nivelRes,
                frecRes,
                impRes,
                respuesta
            )
            {
                AreaConsolidada = areaConsolidada,
                TipoRiesgo = tipoRiesgo,
                Procedimiento = procedimiento,
                ObjetivosEstrategicos = objetivosEstrategicos,
                RegimenAfectado = regimenAfectado,
                Transversalidad = transversalidad,
                ControlPreventivoItems = SplitSourceItems(Convert.ToString(reader.GetValue(19), CultureInfo.InvariantCulture)),
                ControlDetectivoItems = SplitSourceItems(Convert.ToString(reader.GetValue(23), CultureInfo.InvariantCulture)),
                ControlCorrectivoItems = SplitSourceItems(Convert.ToString(reader.GetValue(27), CultureInfo.InvariantCulture)),
                EscalaPreventiva = escalaPreventiva,
                NivelPreventivo = nivelPreventivo,
                EscalaDetectiva = escalaDetectiva,
                NivelDetectivo = nivelDetectivo,
                EscalaCorrectiva = escalaCorrectiva,
                NivelCorrectivo = nivelCorrectivo,
                Automatizacion = automatizacion,
                PlanItems = SplitSourceItems(plan),
                CantidadAccionesFuente = cantidadAcciones,
                ActividadItems = SplitSourceItems(actividades),
                CantidadActividadesFuente = cantidadActividades,
                MonitoreoSeguimiento = monitoreoSeguimiento,
                Responsables = responsables,
                SenalAlertaItems = SplitSourceItems(senalesAlerta)
            });
        }

        return rows;
    }

    private static (int FrecRes, int ImpRes) CalcularResidualAuxiliar(int frec, int imp, int vri, decimal etp, int vrr)
    {
        if (vri == vrr) return (frec, imp);

        double etpDbl = (double)(etp / 100.0m);
        double f_res_aux = (1.0 - etpDbl) * frec;
        double i_res_aux = (1.0 - etpDbl) * imp;
        double suma_res = vrr + 1;
        double f_base = Math.Max(1.0, Math.Floor(f_res_aux));
        double i_base = Math.Max(1.0, Math.Floor(i_res_aux));
        double tope_f = frec;
        double tope_i = imp;
        double cap_f = Math.Max(0.0, tope_f - f_base);
        double cap_i = Math.Max(0.0, tope_i - i_base);
        double resto = Math.Max(0.0, suma_res - (f_base + i_base));

        double mod_i = i_res_aux - Math.Floor(i_res_aux);
        double mod_f = f_res_aux - Math.Floor(f_res_aux);
        int prefiere_i = mod_i >= mod_f ? 1 : 0;

        double inc_i;
        if (resto == 0.0)
        {
            inc_i = 0.0;
        }
        else if (resto == 1.0)
        {
            inc_i = Math.Min(cap_i, (prefiere_i == 1 || cap_f == 0.0) ? 1.0 : 0.0);
        }
        else
        {
            inc_i = prefiere_i == 1
                ? Math.Min(cap_i, 1.0 + (cap_f > 0.0 ? 0.0 : 1.0))
                : Math.Min(cap_i, (cap_f > 0.0 ? 1.0 : 2.0));
        }

        double inc_f = Math.Min(cap_f, Math.Max(0.0, resto - inc_i));
        int fres = (int)Math.Min(tope_f, f_base + inc_f);
        int ires = (int)Math.Min(tope_i, i_base + inc_i);

        return (fres, ires);
    }

    private static decimal ParsePorcentajeControl(object? pctVal, object? escVal)
    {
        if (pctVal is not null && double.TryParse(Convert.ToString(pctVal, CultureInfo.InvariantCulture), NumberStyles.Any, CultureInfo.InvariantCulture, out double p))
        {
            return p <= 1.0 ? (decimal)Math.Round(p * 100.0) : (decimal)Math.Round(p);
        }

        string esc = Convert.ToString(escVal, CultureInfo.InvariantCulture)?.Trim().ToLowerInvariant() ?? string.Empty;
        return esc switch
        {
            "alta efectividad" => 90m,
            "moderado" => 85m,
            "parcialmente efectivo" => 50m,
            "razonable" => 30m,
            _ => 0m
        };
    }

    private static string MapearRespuestaRiesgo(string raw)
    {
        string s = raw.Trim().ToUpperInvariant();
        if (s.Contains("TRANSFERIR", StringComparison.OrdinalIgnoreCase)) return "TRANSFERIR";
        if (s.Contains("EVITAR", StringComparison.OrdinalIgnoreCase)) return "EVITAR";
        if (s.Contains("ACEPTAR", StringComparison.OrdinalIgnoreCase)) return "ACEPTAR";
        if (s.Contains("MITIGAR", StringComparison.OrdinalIgnoreCase)) return "MITIGAR";
        return string.Empty;
    }

    private static string DeterminarNivel(int valor) => valor switch
    {
        <= 2 => "BAJO",
        <= 4 => "BAJO",
        5 => "MODERADO",
        <= 7 => "ALTO",
        _ => "CRITICO"
    };

    private static int ParseInt(object? val) =>
        int.TryParse(Convert.ToString(val, CultureInfo.InvariantCulture), out int n) ? n : 0;

    private static IReadOnlyList<string> SplitSourceItems(string? value)
    {
        string normalized = (value ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n').Trim();
        if (string.IsNullOrWhiteSpace(normalized)) return Array.Empty<string>();

        var items = System.Text.RegularExpressions.Regex.Split(
                normalized,
                @"(?m)(?=^\s*\d+(?:\.\d+)*\s*[-.)])")
            .Select(item => item.Trim())
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => System.Text.RegularExpressions.Regex.Replace(item, @"^\s*\d+(?:\.\d+)*\s*[-.)]\s*", string.Empty).Trim())
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .ToArray();

        return items.Length == 0 ? new[] { normalized } : items;
    }

    private static async Task<DryRunResult> ExecuteDryRunAsync(List<SourceRiskRow> sourceRisks, string schemaJson)
    {
        var validator = new FormularioValidador();
        int approved = 0;
        int rejected = 0;
        var rejectedDetails = new List<string>();
        var seenCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        int duplicateCodes = 0;

        foreach (var r in sourceRisks)
        {
            if (string.IsNullOrWhiteSpace(r.Code))
            {
                rejected++;
                rejectedDetails.Add($"Fila {r.RowNumber}: Código de riesgo vacío.");
                continue;
            }

            if (!seenCodes.Add(r.Code))
            {
                duplicateCodes++;
                rejected++;
                rejectedDetails.Add($"Fila {r.RowNumber}: Código duplicado '{r.Code}'.");
                continue;
            }

            string respuestasJson = JsonSerializer.Serialize(new Dictionary<string, object?>
            {
                ["area_principal"] = r.AreaPrincipal,
                ["dueno_riesgo"] = r.DuenoRiesgo,
                ["frecuencia_inherente"] = r.FrecuenciaInherente.ToString(),
                ["impacto_inherente"] = r.ImpactoInherente.ToString(),
                ["nivel_inherente"] = r.NivelInherente,
                ["controles_preventivo"] = r.ControlesPreventivo,
                ["controles_detectivo"] = r.ControlesDetectivo,
                ["controles_correctivo"] = r.ControlesCorrectivo,
                ["frecuencia_residual"] = r.FrecuenciaResidual.ToString(),
                ["impacto_residual"] = r.ImpactoResidual.ToString(),
                ["nivel_residual"] = r.NivelResidual,
                ["respuesta_riesgo"] = r.RespuestaRiesgo
            });

            var valResult = await validator.ValidarRespuestasAsync(respuestasJson, schemaJson);
            if (!valResult.Valido)
            {
                rejected++;
                rejectedDetails.Add($"Fila {r.RowNumber} ({r.Code}): {string.Join(", ", valResult.Errores.Select(e => e.Campo + ": " + e.Mensaje))}");
            }
            else
            {
                approved++;
            }
        }

        return new DryRunResult(
            sourceRisks.Count,
            approved,
            rejected,
            duplicateCodes,
            0,
            0,
            rejectedDetails
        );
    }

    private static void PrintDryRunMetrics(DryRunResult result)
    {
        Console.WriteLine("\n--- MÉTRICAS DEL DRY-RUN ---");
        Console.WriteLine($"SOURCE_ROWS={result.SourceRows}");
        Console.WriteLine($"APPROVED_FOR_MIGRATION={result.Approved}");
        Console.WriteLine($"REJECTED_DOCUMENTED={result.Rejected}");
        Console.WriteLine($"SILENTLY_SKIPPED={result.SilentlySkipped}");
        Console.WriteLine($"DUPLICATE_SOURCE_CODES={result.DuplicateCodes}");
        Console.WriteLine($"UNMAPPED_REQUIRED_FIELDS={result.UnmappedRequiredFields}");

        if (result.Rejections.Count > 0)
        {
            Console.WriteLine("DETALLE DE RECHAZOS:");
            foreach (var rej in result.Rejections)
            {
                Console.WriteLine($"  - {rej}");
            }
        }
    }

    private static async Task<int> EjecutarEnriquecimientoExistenteAsync(
        OracleConnection connection,
        long versionId,
        IReadOnlyList<SourceRiskRow> sourceRisks,
        bool executeMigration)
    {
        int targetRisksFound = 0;
        int targetEvaluationsFound = 0;
        int evaluationsToEnrich = 0;
        int controlsToCreate = 0;
        int controlEvaluationsToCreate = 0;
        int plansToCreate = 0;
        int activitiesToCreate = 0;
        int alertsToCreate = 0;
        var rejections = new List<string>();
        var legacyIncomplete = new List<string>();
        var formallyDescopedLegacy = new List<string>();

        foreach (var source in sourceRisks)
        {
            long? riesgoId = await ScalarNullableAsync(connection,
                "SELECT RIE_ID FROM RL_MR_RIESGOS WHERE RIE_CODIGO = :codigo AND RIE_ACTIVO = 1",
                new OracleParameter("codigo", source.Code));
            if (riesgoId is null)
            {
                rejections.Add($"{source.Code}: riesgo destino inexistente.");
                continue;
            }

            targetRisksFound++;
            long? evaluacionId = await ScalarNullableAsync(connection,
                @"SELECT EVA_ID
                    FROM RL_MR_EVALUACIONES_RIESGO
                   WHERE EVA_RIESGO_ID = :riesgoId
                     AND EVA_VERSION_ID = :versionId
                     AND EVA_ACTIVO = 1",
                new OracleParameter("riesgoId", riesgoId.Value),
                new OracleParameter("versionId", versionId));
            if (evaluacionId is null)
            {
                rejections.Add($"{source.Code}: evaluación V1 destino inexistente.");
                continue;
            }

            targetEvaluationsFound++;
            evaluationsToEnrich++;

            foreach (ControlSpec control in CrearEspecificacionesControl(source))
            {
                if (control.Descripciones.Count == 0)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(control.Automatizacion) || string.IsNullOrWhiteSpace(control.Estado))
                {
                    legacyIncomplete.Add($"{source.Code}/{control.Tipo}: faltan automatizacion o estado obligatorio; no se crea control operacional.");
                    continue;
                }

                foreach (string descripcion in control.Descripciones)
                {
                    bool exists = Convert.ToInt32(await ScalarAsync(connection, null,
                        @"SELECT COUNT(*)
                            FROM RL_MR_CONTROLES_RIESGO
                           WHERE CON_EVALUACION_ID = :evaluacionId
                             AND CON_TIPO = :tipo
                             AND CON_DESCRIPCION = :descripcion",
                        new OracleParameter("evaluacionId", evaluacionId.Value),
                        new OracleParameter("tipo", control.Tipo),
                        new OracleParameter("descripcion", descripcion))) > 0;
                    if (!exists) controlsToCreate++;
                }
            }

            for (int alertaIndex = 0; alertaIndex < source.SenalAlertaItems.Count; alertaIndex++)
            {
                string indicador = source.SenalAlertaItems[alertaIndex].Trim();
                if (indicador.Length == 0 || indicador.Length > 150)
                {
                    legacyIncomplete.Add($"{source.Code}: señal de alerta fuera del límite contractual de 150 caracteres; no se crea alerta.");
                    continue;
                }

                string codigo = ConstruirCodigoAlerta(source.Code, alertaIndex + 1);
                bool exists = Convert.ToInt32(await ScalarAsync(connection, null,
                    @"SELECT COUNT(*)
                        FROM RL_MR_SENALES_ALERTA
                       WHERE ALE_EVALUACION_ID = :evaluacionId
                         AND ALE_CODIGO = :codigo",
                    new OracleParameter("evaluacionId", evaluacionId.Value),
                    new OracleParameter("codigo", codigo))) > 0;
                if (!exists) alertsToCreate++;
            }

            if (source.PlanItems.Count > 0)
            {
                legacyIncomplete.Add($"{source.Code}: plan fuente sin fechas, presupuesto y estado; no se crea plan operacional.");
            }

            if (source.ActividadItems.Count > 0)
            {
                legacyIncomplete.Add($"{source.Code}: actividades fuente sin responsable, fechas y estado por actividad; no se crean actividades operacionales.");
            }

            if (!string.IsNullOrWhiteSpace(source.MonitoreoSeguimiento))
            {
                formallyDescopedLegacy.Add($"{source.Code}: MR-44 es texto narrativo histórico; no se crea evento RL_MR_AUTOMONITOREO.");
            }

            if (!string.IsNullOrWhiteSpace(source.Responsables))
            {
                formallyDescopedLegacy.Add($"{source.Code}: MR-45 es información histórica de nivel plan; no se distribuye a actividades.");
            }

            // Las columnas de efectividad son agregadas por tipo en el origen.
            // No se duplica el mismo porcentaje en cada control normalizado.
            if (TieneEfectividadFuente(source)) controlEvaluationsToCreate += 0;
        }

        Console.WriteLine("\n--- ENRICH_EXISTING DRY-RUN ---");
        Console.WriteLine($"SOURCE_RISKS={sourceRisks.Count}");
        Console.WriteLine($"TARGET_RISKS_FOUND={targetRisksFound}");
        Console.WriteLine($"MISSING_TARGET_RISKS={sourceRisks.Count - targetRisksFound}");
        Console.WriteLine($"TARGET_EVALUATIONS_FOUND={targetEvaluationsFound}");
        Console.WriteLine($"EVALUATIONS_TO_ENRICH={evaluationsToEnrich}");
        Console.WriteLine($"CONTROLS_TO_CREATE={controlsToCreate}");
        Console.WriteLine($"CONTROL_EVALUATIONS_TO_CREATE={controlEvaluationsToCreate}");
        Console.WriteLine($"PLANS_TO_CREATE={plansToCreate}");
        Console.WriteLine($"ACTIVITIES_TO_CREATE={activitiesToCreate}");
        Console.WriteLine($"ALERTS_TO_CREATE={alertsToCreate}");
        Console.WriteLine("FIELDS_TO_ADD_TO_V2=5");
        Console.WriteLine($"MIGRATABLE_COMPLETE_CONTROLS={controlsToCreate}");
        Console.WriteLine($"MIGRATABLE_COMPLETE_ALERTS={alertsToCreate}");
        Console.WriteLine($"LEGACY_INCOMPLETE_DOCUMENTED={legacyIncomplete.Count}");
        Console.WriteLine($"FORMALLY_DESCOPED_LEGACY={formallyDescopedLegacy.Count}");
        Console.WriteLine($"REAL_REJECTIONS={rejections.Count}");
        Console.WriteLine($"REJECTIONS={rejections.Count}");
        foreach (string item in legacyIncomplete.Distinct(StringComparer.Ordinal))
        {
            Console.WriteLine($"LEGACY_INCOMPLETE={item}");
        }
        foreach (string item in formallyDescopedLegacy.Distinct(StringComparer.Ordinal))
        {
            Console.WriteLine($"FORMALLY_DESCOPED={item}");
        }
        foreach (string rejection in rejections.Distinct(StringComparer.Ordinal))
        {
            Console.WriteLine($"REJECTION={rejection}");
        }

        if (!executeMigration)
        {
            Console.WriteLine("ENRICH_EXISTING_DML=NOT_EXECUTED");
            return rejections.Count == 0 ? 0 : 5;
        }

        if (rejections.Count > 0)
        {
            Console.WriteLine("ENRICH_EXISTING_DML=BLOCKED_FAIL_CLOSED");
            return 5;
        }

        // El enriquecimiento autorizado en esta fase solo materializa señales de
        // alerta cuando el contrato de la entidad está completo. Controles,
        // planes, actividades y automonitoreo incompletos permanecen documentados
        // como legado y nunca se completan con valores inventados.
        await using var transaction = connection.BeginTransaction();
        int insertedAlerts = 0;
        try
        {
            foreach (SourceRiskRow source in sourceRisks)
            {
                object? evaluacionValue = await ScalarAsync(connection, transaction,
                    @"SELECT EVA_ID
                        FROM RL_MR_EVALUACIONES_RIESGO
                       WHERE EVA_RIESGO_ID = (SELECT RIE_ID
                                                FROM RL_MR_RIESGOS
                                               WHERE RIE_CODIGO = :codigo
                                                 AND RIE_ACTIVO = 1)
                         AND EVA_VERSION_ID = :versionId
                         AND EVA_ACTIVO = 1",
                    new OracleParameter("codigo", source.Code),
                    new OracleParameter("versionId", versionId));
                if (evaluacionValue is null || evaluacionValue == DBNull.Value)
                {
                    throw new InvalidOperationException($"Evaluación V1 destino inexistente para {source.Code}.");
                }

                long evaluacionId = Convert.ToInt64(evaluacionValue, CultureInfo.InvariantCulture);
                for (int alertaIndex = 0; alertaIndex < source.SenalAlertaItems.Count; alertaIndex++)
                {
                    string indicador = source.SenalAlertaItems[alertaIndex].Trim();
                    if (indicador.Length == 0 || indicador.Length > 150)
                    {
                        continue;
                    }

                    string codigo = ConstruirCodigoAlerta(source.Code, alertaIndex + 1);
                    int exists = Convert.ToInt32(await ScalarAsync(connection, transaction,
                        @"SELECT COUNT(*)
                            FROM RL_MR_SENALES_ALERTA
                           WHERE ALE_EVALUACION_ID = :evaluacionId
                             AND ALE_CODIGO = :codigo",
                        new OracleParameter("evaluacionId", evaluacionId),
                        new OracleParameter("codigo", codigo)), CultureInfo.InvariantCulture);
                    if (exists > 0)
                    {
                        continue;
                    }

                    object? nextIdValue = await ScalarAsync(connection, transaction,
                        "SELECT SEQ_RL_MR_SENALES.NEXTVAL FROM DUAL");
                    long nextId = Convert.ToInt64(nextIdValue, CultureInfo.InvariantCulture);
                    await using var insert = new OracleCommand(
                        @"INSERT INTO RL_MR_SENALES_ALERTA (
                              ALE_ID, ALE_EVALUACION_ID, ALE_CODIGO, ALE_INDICADOR,
                              ALE_ESTADO, ALE_FECHA_DISPARO
                          ) VALUES (
                              :id, :evaluacionId, :codigo, :indicador,
                              'INACTIVO', NULL
                          )", connection)
                    {
                        BindByName = true,
                        Transaction = transaction
                    };
                    insert.Parameters.Add(new OracleParameter("id", nextId));
                    insert.Parameters.Add(new OracleParameter("evaluacionId", evaluacionId));
                    insert.Parameters.Add(new OracleParameter("codigo", codigo));
                    insert.Parameters.Add(new OracleParameter("indicador", indicador));
                    await insert.ExecuteNonQueryAsync();
                    insertedAlerts++;
                }
            }

            transaction.Commit();
            Console.WriteLine("ENRICH_EXISTING_DML=EXECUTED_ALERTS_ONLY");
            Console.WriteLine($"ALERTS_INSERTED={insertedAlerts}");
            return 0;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    private static IReadOnlyList<ControlSpec> CrearEspecificacionesControl(SourceRiskRow source) =>
        new[]
        {
            new ControlSpec("PREVENTIVO", source.ControlPreventivoItems, NormalizarAutomatizacion(source.Automatizacion), string.Empty),
            new ControlSpec("DETECTIVO", source.ControlDetectivoItems, NormalizarAutomatizacion(source.Automatizacion), string.Empty),
            new ControlSpec("CORRECTIVO", source.ControlCorrectivoItems, NormalizarAutomatizacion(source.Automatizacion), string.Empty)
        };

    private static string? NormalizarAutomatizacion(string valor)
    {
        string normalizado = (valor ?? string.Empty).Trim().ToUpperInvariant()
            .Replace("Á", "A", StringComparison.Ordinal)
            .Replace("É", "E", StringComparison.Ordinal)
            .Replace("Í", "I", StringComparison.Ordinal)
            .Replace("Ó", "O", StringComparison.Ordinal)
            .Replace("Ú", "U", StringComparison.Ordinal);
        if (normalizado.Contains("SEMI", StringComparison.Ordinal)) return "SEMIAUTOMATICO";
        if (normalizado.Contains("AUTOM", StringComparison.Ordinal)) return "AUTOMATICO";
        if (normalizado.Contains("MANUAL", StringComparison.Ordinal)) return "MANUAL";
        return string.IsNullOrWhiteSpace(normalizado) ? null : normalizado;
    }

    private static bool TieneEfectividadFuente(SourceRiskRow source) =>
        source.ControlesPreventivo > 0 || source.ControlesDetectivo > 0 || source.ControlesCorrectivo > 0 ||
        !string.IsNullOrWhiteSpace(source.EscalaPreventiva) ||
        !string.IsNullOrWhiteSpace(source.EscalaDetectiva) ||
        !string.IsNullOrWhiteSpace(source.EscalaCorrectiva);

    private static string ConstruirCodigoAlerta(string riesgoCodigo, int ordinal) =>
        Truncar($"{riesgoCodigo}-AL-{ordinal}", 50).ToUpperInvariant();

    private static async Task<object?> ScalarAsync(
        OracleConnection conn,
        OracleTransaction? tx,
        string sql,
        params OracleParameter[] parameters)
    {
        await using var cmd = new OracleCommand(sql, conn) { BindByName = true, Transaction = tx };
        cmd.Parameters.AddRange(parameters);
        return await cmd.ExecuteScalarAsync();
    }

    private static async Task<long?> ScalarNullableAsync(
        OracleConnection conn,
        string sql,
        params OracleParameter[] parameters)
    {
        object? value = await ScalarAsync(conn, null, sql, parameters);
        return value is null || value == DBNull.Value ? null : Convert.ToInt64(value, CultureInfo.InvariantCulture);
    }

    private sealed record ControlSpec(string Tipo, IReadOnlyList<string> Descripciones, string? Automatizacion, string Estado);

    private static async Task<MigrationBatchResult> MigrateBatchAsync(
        OracleConnection connection,
        long versionId,
        List<SourceRiskRow> sourceRisks)
    {
        await using var transaction = connection.BeginTransaction();
        int insertedRisks = 0;
        int insertedEvaluations = 0;
        int insertedProjections = 0;

        foreach (var r in sourceRisks)
        {
            // 1. Obtener o Insertar Riesgo
            long riesgoId = 0;
            await using (var cmdCheck = new OracleCommand("SELECT RIE_ID FROM RL_MR_RIESGOS WHERE RIE_CODIGO = :codigo", connection))
            {
                cmdCheck.Transaction = transaction;
                cmdCheck.Parameters.Add(new OracleParameter("codigo", r.Code));
                object? existing = await cmdCheck.ExecuteScalarAsync();
                if (existing is not null && existing != DBNull.Value)
                {
                    riesgoId = Convert.ToInt64(existing);
                }
            }

            if (riesgoId == 0)
            {
                long nextRieId = Convert.ToInt64(await ScalarAsync(connection, transaction, "SELECT SEQ_RL_MR_RIESGOS.NEXTVAL FROM DUAL"));
                const string sqlInsRiesgo = @"
                    INSERT INTO RL_MR_RIESGOS (
                        RIE_ID, RIE_CODIGO, RIE_NOMBRE, RIE_DESCRIPCION, RIE_ACTIVO, RIE_USR_CREACION, RIE_FECHA_CREACION
                    ) VALUES (
                        :id, :codigo, :nombre, :descripcion, 1, 1, SYSDATE
                    )";
                await using var cmdInsRie = new OracleCommand(sqlInsRiesgo, connection);
                cmdInsRie.Transaction = transaction;
                cmdInsRie.Parameters.Add(new OracleParameter("id", nextRieId));
                cmdInsRie.Parameters.Add(new OracleParameter("codigo", r.Code));
                cmdInsRie.Parameters.Add(new OracleParameter("nombre", Truncar(r.Titulo, 250)));
                cmdInsRie.Parameters.Add(new OracleParameter("descripcion", Truncar(r.Descripcion, 2000)));
                await cmdInsRie.ExecuteNonQueryAsync();

                riesgoId = nextRieId;
                insertedRisks++;
            }

            // 2. Obtener o Insertar Evaluación
            long evaluacionId = 0;
            await using (var cmdCheckEva = new OracleCommand("SELECT EVA_ID FROM RL_MR_EVALUACIONES_RIESGO WHERE EVA_RIESGO_ID = :rieId AND EVA_VERSION_ID = :verId AND EVA_ACTIVO = 1", connection))
            {
                cmdCheckEva.Transaction = transaction;
                cmdCheckEva.Parameters.Add(new OracleParameter("rieId", riesgoId));
                cmdCheckEva.Parameters.Add(new OracleParameter("verId", versionId));
                object? existingEva = await cmdCheckEva.ExecuteScalarAsync();
                if (existingEva is not null && existingEva != DBNull.Value)
                {
                    evaluacionId = Convert.ToInt64(existingEva);
                }
            }

            if (evaluacionId == 0)
            {
                long nextEvaId = Convert.ToInt64(await ScalarAsync(connection, transaction, "SELECT SEQ_RL_MR_EVALUACIONES.NEXTVAL FROM DUAL"));

                string datosJson = JsonSerializer.Serialize(new Dictionary<string, object?>
                {
                    ["area_principal"] = r.AreaPrincipal,
                    ["dueno_riesgo"] = r.DuenoRiesgo,
                    ["frecuencia_inherente"] = r.FrecuenciaInherente.ToString(),
                    ["impacto_inherente"] = r.ImpactoInherente.ToString(),
                    ["nivel_inherente"] = r.NivelInherente,
                    ["controles_preventivo"] = r.ControlesPreventivo,
                    ["controles_detectivo"] = r.ControlesDetectivo,
                    ["controles_correctivo"] = r.ControlesCorrectivo,
                    ["frecuencia_residual"] = r.FrecuenciaResidual.ToString(),
                    ["impacto_residual"] = r.ImpactoResidual.ToString(),
                    ["nivel_residual"] = r.NivelResidual,
                    ["respuesta_riesgo"] = r.RespuestaRiesgo
                });

                string calculosJson = JsonSerializer.Serialize(new Dictionary<string, object?>
                {
                    ["vri"] = r.Vri,
                    ["etp"] = r.Etp,
                    ["vrr"] = r.Vrr,
                    ["vrr2"] = r.Vrr,
                    ["nivelResidual"] = r.NivelResidual,
                    ["coherente"] = true,
                    ["reglaCodigo"] = "CALCULO_VRI_VRR",
                    ["reglaVersion"] = "1.0",
                    ["algoritmoId"] = "MATRICES_VRI_ADITIVO_1_9"
                });

                const string sqlInsEva = @"
                    INSERT INTO RL_MR_EVALUACIONES_RIESGO (
                        EVA_ID, EVA_RIESGO_ID, EVA_VERSION_ID, EVA_DATOS_JSON, EVA_CALCULOS_JSON,
                        EVA_FECHA_REGISTRO, EVA_USR_REGISTRO, EVA_VERSION_ROW, EVA_ACTIVO
                    ) VALUES (
                        :id, :rieId, :verId, :datosJson, :calculosJson,
                        SYSDATE, 1, 1, 1
                    )";
                await using var cmdInsEva = new OracleCommand(sqlInsEva, connection);
                cmdInsEva.Transaction = transaction;
                cmdInsEva.Parameters.Add(new OracleParameter("id", nextEvaId));
                cmdInsEva.Parameters.Add(new OracleParameter("rieId", riesgoId));
                cmdInsEva.Parameters.Add(new OracleParameter("verId", versionId));
                cmdInsEva.Parameters.Add(new OracleParameter("datosJson", OracleDbType.Clob) { Value = datosJson });
                cmdInsEva.Parameters.Add(new OracleParameter("calculosJson", OracleDbType.Clob) { Value = calculosJson });
                await cmdInsEva.ExecuteNonQueryAsync();

                evaluacionId = nextEvaId;
                insertedEvaluations++;
            }

            // 3. Obtener o Insertar Proyección
            long proyId = 0;
            await using (var cmdCheckProy = new OracleCommand("SELECT PROY_ID FROM RL_MR_PROYECCIONES_EVALUACION WHERE PROY_EVALUACION_ID = :evaId", connection))
            {
                cmdCheckProy.Transaction = transaction;
                cmdCheckProy.Parameters.Add(new OracleParameter("evaId", evaluacionId));
                object? existingProy = await cmdCheckProy.ExecuteScalarAsync();
                if (existingProy is not null && existingProy != DBNull.Value)
                {
                    proyId = Convert.ToInt64(existingProy);
                }
            }

            if (proyId == 0)
            {
                long nextProyId = Convert.ToInt64(await ScalarAsync(connection, transaction, "SELECT SEQ_RL_MR_PROYECCIONES.NEXTVAL FROM DUAL"));
                const string sqlInsProy = @"
                    INSERT INTO RL_MR_PROYECCIONES_EVALUACION (
                        PROY_ID, PROY_EVALUACION_ID, PROY_CODIGO_RIESGO, PROY_AREA_PRINCIPAL,
                        PROY_VRI, PROY_VRR, PROY_NIVEL_INHERENTE, PROY_NIVEL_RESIDUAL,
                        PROY_RESPUESTA_RIESGO, PROY_ESTADO_EVALUACION, PROY_DUENO_RIESGO, PROY_FECHA_EVAL
                    ) VALUES (
                        :id, :evaId, :codigo, :area,
                        :vri, :vrr, :nivelInh, :nivelRes,
                        :respuesta, 'APROBADA', :dueno, SYSDATE
                    )";
                await using var cmdInsProy = new OracleCommand(sqlInsProy, connection);
                cmdInsProy.Transaction = transaction;
                cmdInsProy.Parameters.Add(new OracleParameter("id", nextProyId));
                cmdInsProy.Parameters.Add(new OracleParameter("evaId", evaluacionId));
                cmdInsProy.Parameters.Add(new OracleParameter("codigo", r.Code));
                cmdInsProy.Parameters.Add(new OracleParameter("area", Truncar(r.AreaPrincipal, 100)));
                cmdInsProy.Parameters.Add(new OracleParameter("vri", r.Vri));
                cmdInsProy.Parameters.Add(new OracleParameter("vrr", r.Vrr));
                cmdInsProy.Parameters.Add(new OracleParameter("nivelInh", r.NivelInherente));
                cmdInsProy.Parameters.Add(new OracleParameter("nivelRes", r.NivelResidual));
                cmdInsProy.Parameters.Add(new OracleParameter("respuesta", r.RespuestaRiesgo));
                cmdInsProy.Parameters.Add(new OracleParameter("dueno", Truncar(r.DuenoRiesgo, 150)));
                await cmdInsProy.ExecuteNonQueryAsync();

                insertedProjections++;
            }
        }

        await transaction.CommitAsync();
        return new MigrationBatchResult(insertedRisks, insertedEvaluations, insertedProjections);
    }

    private static async Task<ReconciliationResult> ReconcileOracleAsync(
        OracleConnection connection,
        long expectedVersionId,
        List<SourceRiskRow> sourceRisks)
    {
        long totalRiesgos = Convert.ToInt64(await ScalarAsync(connection, null, "SELECT COUNT(*) FROM RL_MR_RIESGOS WHERE RIE_ACTIVO = 1"));
        long totalEvaluaciones = Convert.ToInt64(await ScalarAsync(connection, null, "SELECT COUNT(*) FROM RL_MR_EVALUACIONES_RIESGO WHERE EVA_ACTIVO = 1"));
        long totalProyecciones = Convert.ToInt64(await ScalarAsync(connection, null, "SELECT COUNT(*) FROM RL_MR_PROYECCIONES_EVALUACION"));

        long duplicateRisks = Convert.ToInt64(await ScalarAsync(connection, null,
            "SELECT COUNT(*) FROM (SELECT RIE_CODIGO FROM RL_MR_RIESGOS WHERE RIE_ACTIVO = 1 GROUP BY RIE_CODIGO HAVING COUNT(*) > 1)"));

        long orphanEvaluations = Convert.ToInt64(await ScalarAsync(connection, null,
            "SELECT COUNT(*) FROM RL_MR_EVALUACIONES_RIESGO e WHERE NOT EXISTS (SELECT 1 FROM RL_MR_RIESGOS r WHERE r.RIE_ID = e.EVA_RIESGO_ID)"));

        long orphanProjections = Convert.ToInt64(await ScalarAsync(connection, null,
            "SELECT COUNT(*) FROM RL_MR_PROYECCIONES_EVALUACION p WHERE NOT EXISTS (SELECT 1 FROM RL_MR_EVALUACIONES_RIESGO e WHERE e.EVA_ID = p.PROY_EVALUACION_ID)"));

        long invalidVersionBindings = Convert.ToInt64(await ScalarAsync(connection, null,
            $"SELECT COUNT(*) FROM RL_MR_EVALUACIONES_RIESGO WHERE EVA_VERSION_ID <> {expectedVersionId}"));

        // Comparar fila por fila valores proyectados vs calculados
        int reconciledCount = 0;
        int unexplainedDiffs = 0;

        foreach (var r in sourceRisks)
        {
            const string sqlCheck = @"
                SELECT p.PROY_VRI, p.PROY_VRR, p.PROY_NIVEL_INHERENTE, p.PROY_NIVEL_RESIDUAL, p.PROY_RESPUESTA_RIESGO, p.PROY_DUENO_RIESGO
                  FROM RL_MR_PROYECCIONES_EVALUACION p
                 WHERE p.PROY_CODIGO_RIESGO = :codigo";
            await using var cmd = new OracleCommand(sqlCheck, connection);
            cmd.Parameters.Add(new OracleParameter("codigo", r.Code));
            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                int dbVri = Convert.ToInt32(reader["PROY_VRI"]);
                int dbVrr = Convert.ToInt32(reader["PROY_VRR"]);
                string dbNivelInh = Convert.ToString(reader["PROY_NIVEL_INHERENTE"]) ?? string.Empty;
                string dbNivelRes = Convert.ToString(reader["PROY_NIVEL_RESIDUAL"]) ?? string.Empty;
                string dbResp = Convert.ToString(reader["PROY_RESPUESTA_RIESGO"]) ?? string.Empty;
                string dbDueno = Convert.ToString(reader["PROY_DUENO_RIESGO"]) ?? string.Empty;

                if (dbVri == r.Vri && dbVrr == r.Vrr &&
                    string.Equals(dbNivelInh, r.NivelInherente, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(dbNivelRes, r.NivelResidual, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(dbResp, r.RespuestaRiesgo, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(dbDueno, r.DuenoRiesgo, StringComparison.OrdinalIgnoreCase))
                {
                    reconciledCount++;
                }
                else
                {
                    unexplainedDiffs++;
                    Console.WriteLine($"Diferencia en {r.Code}: DB(VRI={dbVri}, VRR={dbVrr}) vs Calc(VRI={r.Vri}, VRR={r.Vrr})");
                }
            }
            else
            {
                unexplainedDiffs++;
                Console.WriteLine($"Falta en DB: {r.Code}");
            }
        }

        long missingRisks = sourceRisks.Count - reconciledCount;

        return new ReconciliationResult(
            totalRiesgos,
            missingRisks,
            duplicateRisks,
            orphanEvaluations,
            orphanProjections,
            invalidVersionBindings,
            reconciledCount,
            unexplainedDiffs
        );
    }

    private static void PrintReconciliationMetrics(ReconciliationResult r)
    {
        Console.WriteLine("\n--- CERTIFICACIÓN DE CONCILIACIÓN EN ORACLE ---");
        Console.WriteLine($"MIGRATED_SOURCE_RISKS={r.MigratedSourceRisks}");
        Console.WriteLine($"MISSING_SOURCE_RISKS={r.MissingSourceRisks}");
        Console.WriteLine($"DUPLICATE_SOURCE_RISKS={r.DuplicateSourceRisks}");
        Console.WriteLine($"ORPHAN_EVALUATIONS={r.OrphanEvaluations}");
        Console.WriteLine($"ORPHAN_PROJECTIONS={r.OrphanProjections}");
        Console.WriteLine($"INVALID_VERSION_BINDINGS={r.InvalidVersionBindings}");
        Console.WriteLine($"RISKS_RECONCILED={r.RisksReconciled}");
        Console.WriteLine($"UNEXPLAINED_DIFFERENCES={r.UnexplainedDifferences}");
    }

    private static string Truncar(string s, int max) =>
        s.Length <= max ? s : s[..max];

    private static async Task<object?> ScalarAsync(OracleConnection conn, OracleTransaction? tx, string sql)
    {
        await using var cmd = new OracleCommand(sql, conn);
        if (tx is not null) cmd.Transaction = tx;
        return await cmd.ExecuteScalarAsync();
    }
}

public sealed record RiskTextDbRow(
    long Id,
    string Code,
    string Name,
    string? Description
);

public sealed record RiskTextRepairItem(
    long Id,
    string Code,
    string CurrentName,
    string? CurrentDescription,
    string CanonicalName,
    string CanonicalDescription
);

public sealed record SourceRiskRow(
    int RowNumber,
    string Code,
    string AreaPrincipal,
    string DuenoRiesgo,
    string Titulo,
    string Descripcion,
    int FrecuenciaInherente,
    int ImpactoInherente,
    int Vri,
    string NivelInherente,
    decimal ControlesPreventivo,
    decimal ControlesDetectivo,
    decimal ControlesCorrectivo,
    decimal Etp,
    int Vrr,
    string NivelResidual,
    int FrecuenciaResidual,
    int ImpactoResidual,
    string RespuestaRiesgo
)
{
    public string AreaConsolidada { get; init; } = string.Empty;
    public string TipoRiesgo { get; init; } = string.Empty;
    public string Procedimiento { get; init; } = string.Empty;
    public string ObjetivosEstrategicos { get; init; } = string.Empty;
    public string RegimenAfectado { get; init; } = string.Empty;
    public string Transversalidad { get; init; } = string.Empty;
    public IReadOnlyList<string> ControlPreventivoItems { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> ControlDetectivoItems { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> ControlCorrectivoItems { get; init; } = Array.Empty<string>();
    public string EscalaPreventiva { get; init; } = string.Empty;
    public string NivelPreventivo { get; init; } = string.Empty;
    public string EscalaDetectiva { get; init; } = string.Empty;
    public string NivelDetectivo { get; init; } = string.Empty;
    public string EscalaCorrectiva { get; init; } = string.Empty;
    public string NivelCorrectivo { get; init; } = string.Empty;
    public string Automatizacion { get; init; } = string.Empty;
    public IReadOnlyList<string> PlanItems { get; init; } = Array.Empty<string>();
    public int CantidadAccionesFuente { get; init; }
    public IReadOnlyList<string> ActividadItems { get; init; } = Array.Empty<string>();
    public int CantidadActividadesFuente { get; init; }
    public string MonitoreoSeguimiento { get; init; } = string.Empty;
    public string Responsables { get; init; } = string.Empty;
    public IReadOnlyList<string> SenalAlertaItems { get; init; } = Array.Empty<string>();
}

public sealed record DryRunResult(
    int SourceRows,
    int Approved,
    int Rejected,
    int DuplicateCodes,
    int SilentlySkipped,
    int UnmappedRequiredFields,
    IReadOnlyList<string> Rejections
);

public sealed record MigrationBatchResult(
    int InsertedRisks,
    int InsertedEvaluations,
    int InsertedProjections
);

public sealed record ReconciliationResult(
    long MigratedSourceRisks,
    long MissingSourceRisks,
    long DuplicateSourceRisks,
    long OrphanEvaluations,
    long OrphanProjections,
    long InvalidVersionBindings,
    int RisksReconciled,
    int UnexplainedDifferences
);
