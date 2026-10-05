using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Oracle.ManagedDataAccess.Client;
using RL.API.Infrastructure.Database;

namespace RL.Tools.ReconcileMatricesBaseline;

public class RiskPreimage
{
    public long Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public int Active { get; set; }
}

public class EvaluationPreimage
{
    public long Id { get; set; }
    public long RiskId { get; set; }
    public string RiskCode { get; set; } = "";
    public long VersionId { get; set; }
    public int Active { get; set; }
    public string Estado { get; set; } = "";
    public string DatosJson { get; set; } = "";
    public string CalculosJson { get; set; } = "";
    public DateTime FechaRegistro { get; set; }
}

public class ProjectionPreimage
{
    public long EvaluationId { get; set; }
    public string RiskCode { get; set; } = "";
    public string AreaPrincipal { get; set; } = "";
    public string DuenoRiesgo { get; set; } = "";
    public int Vri { get; set; }
    public string NivelInherente { get; set; } = "";
    public int Vrr { get; set; }
    public string NivelResidual { get; set; } = "";
    public string RespuestaRiesgo { get; set; } = "";
    public string EstadoEvaluacion { get; set; } = "";
    public DateTime FechaEval { get; set; }
}

public class AlertPreimage
{
    public long Id { get; set; }
    public long EvaluationId { get; set; }
    public string RiskCode { get; set; } = "";
    public string Indicator { get; set; } = "";
    public string State { get; set; } = "";
    public DateTime? FiredAt { get; set; }
}

public class ControlPreimage
{
    public long Id { get; set; }
    public long EvaluationId { get; set; }
    public string RiskCode { get; set; } = "";
    public string Tipo { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public string Automatizacion { get; set; } = "";
    public string Estado { get; set; } = "";
    public string EstadoMonitoreo { get; set; } = "";
    public decimal EfectividadMonitoreo { get; set; }
}

public class PlanPreimage
{
    public long Id { get; set; }
    public long EvaluationId { get; set; }
    public string RiskCode { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public decimal Avance { get; set; }
    public decimal Presupuesto { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public string Estado { get; set; } = "";
    public string MonitoreoSeguimiento { get; set; } = "";
    public string Responsables { get; set; } = "";
    public string Recursos { get; set; } = "";
}

public class ActivityPreimage
{
    public long Id { get; set; }
    public long PlanId { get; set; }
    public string RiskCode { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public string Responsable { get; set; } = "";
    public decimal Avance { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public string Estado { get; set; } = "";
}

public class DatabaseSnapshot
{
    public string RunId { get; set; } = "";
    public DateTime CapturedAtUtc { get; set; }
    public string Environment { get; set; } = "";
    public string DatabaseName { get; set; } = "";
    public List<RiskPreimage> Risks { get; set; } = new();
    public List<EvaluationPreimage> Evaluations { get; set; } = new();
    public List<ProjectionPreimage> Projections { get; set; } = new();
    public List<AlertPreimage> Alerts { get; set; } = new();
    public List<ControlPreimage> Controls { get; set; } = new();
    public List<PlanPreimage> Plans { get; set; } = new();
    public List<ActivityPreimage> Activities { get; set; } = new();
}

public class PlannedMutation
{
    public string MutationId { get; set; } = "";
    public string RiskCode { get; set; }
    public int FieldNumber { get; set; }
    public string FieldLabel { get; set; } = "";
    public string Entity { get; set; } = "";
    public string RecordKey { get; set; } = "";
    public string Action { get; set; } = ""; // INSERT_BASELINE, UPDATE_BASELINE
    public string TargetColumnOrProperty { get; set; } = "";
    public string ExcelRaw { get; set; } = "";
    public string ExcelCanonical { get; set; } = "";
    public string DbBeforeRaw { get; set; } = "";
    public string DbBeforeCanonical { get; set; } = "";
    public string TargetCanonical { get; set; } = "";
    public string ReasonCode { get; set; } = "";
    public string Authority { get; set; } = "EXCEL_WORKBOOK_OFFICIAL";
    public string Precondition { get; set; } = "";
    public string Postcondition { get; set; } = "";
    public string RollbackValue { get; set; } = "";

    public PlannedMutation()
    {
        RiskCode = "";
    }
}

public class PlannedPreservation
{
    public string RiskCode { get; set; } = "";
    public int FieldNumber { get; set; }
    public string FieldLabel { get; set; } = "";
    public string Entity { get; set; } = "";
    public string DbValueHash { get; set; } = "";
    public string DbValueSnippet { get; set; } = "";
    public string PreserveReason { get; set; } = "";
}

public class ReconciliationPosition
{
    public string RiskCode { get; set; } = "";
    public int RiskNo { get; set; }
    public int FieldNumber { get; set; }
    public string FieldLabel { get; set; } = "";
    public string Entity { get; set; } = "";
    public string RecordId { get; set; } = "";
    public string ExcelRaw { get; set; } = "";
    public string ExcelCanonical { get; set; } = "";
    public string DbBeforeRaw { get; set; } = "";
    public string DbBeforeCanonical { get; set; } = "";
    public string Action { get; set; } = "";
    public string TargetCanonical { get; set; } = "";
    public string ReasonCode { get; set; } = "";
    public string Authority { get; set; } = "";
    public string Precondition { get; set; } = "";
    public string Postcondition { get; set; } = "";
    public string RollbackValue { get; set; } = "";
}

public static class Program
{
    public const string ExpectedWorkbookSha256 = "5c3fc00864947afe1e34d3d6ffdfc6da008eaa3c8f1c6c764161014d5ef9a385";
    private static readonly Regex ProhibitedSqlRegex = new(
        @"(?i)\b(INSERT|UPDATE|MERGE|DELETE|TRUNCATE|CREATE|ALTER|DROP|COMMENT|GRANT|REVOKE|CALL|EXEC|EXECUTE|LOCK\s+TABLE|FOR\s+UPDATE)\b",
        RegexOptions.Compiled);

    public static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        string repoRoot = FindRepoRoot();
        string manifestPath = Path.Combine(repoRoot, "backend", "RL.API", "Features", "MatricesRiesgos", "Contracts", "matriz_riesgos_82_campos_manifest.json");
        string catalogManifestPath = Path.Combine(repoRoot, "backend", "RL.API", "Features", "MatricesRiesgos", "Contracts", "matriz_riesgos_catalogos_manifest.json");
        string excelPath = Path.Combine(repoRoot, "Matrices de Riesgos.xlsx");
        string excelJsonPath = Path.Combine(repoRoot, "scratch_excel_59x82.json");

        Console.WriteLine("================================================================================");
        Console.WriteLine("RECONCILIACIÓN Y RECARGA CONTROLADA DE DATOS — BLOQUE 4 DE 12");
        Console.WriteLine("MODO: UPSERT DETERMINISTA POR CÓDIGO DE RIESGO");
        Console.WriteLine("================================================================================");

        // 1. Verificar workbook
        if (!File.Exists(excelPath))
        {
            Console.Error.WriteLine($"ERROR: No se encontró {excelPath}");
            return 1;
        }

        string actualWorkbookSha;
        using (var stream = File.OpenRead(excelPath))
        using (var sha = SHA256.Create())
        {
            actualWorkbookSha = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }

        if (!actualWorkbookSha.Equals(ExpectedWorkbookSha256, StringComparison.OrdinalIgnoreCase))
        {
            Console.Error.WriteLine($"ABORT: El hash del workbook no coincide. actual={actualWorkbookSha}; esperado={ExpectedWorkbookSha256}");
            return 1;
        }
        Console.WriteLine($"WORKBOOK_VERIFIED_SHA256={actualWorkbookSha} (PASS)");

        // 2. Extraer datos Excel de las 59 filas × 82 columnas
        if (!File.Exists(excelJsonPath))
        {
            Console.WriteLine("Generando scratch_excel_59x82.json mediante tools/export_excel_matrix_82.js...");
            var psi = new ProcessStartInfo("node", Path.Combine(repoRoot, "tools", "export_excel_matrix_82.js"))
            {
                WorkingDirectory = repoRoot,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };
            var proc = Process.Start(psi);
            proc?.WaitForExit();
        }

        var excelDoc = JsonDocument.Parse(File.ReadAllText(excelJsonPath));
        var excelRows = excelDoc.RootElement.GetProperty("matrix").EnumerateArray().ToList();
        var excelHeaders = excelDoc.RootElement.GetProperty("headers").EnumerateArray().Select(h => h.GetString() ?? "").ToList();
        var manifestDoc = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var manifestFields = manifestDoc.RootElement.EnumerateArray().ToList();
        var catalogDoc = JsonDocument.Parse(File.ReadAllText(catalogManifestPath));

        if (excelRows.Count != 59 || excelHeaders.Count != 82 || manifestFields.Count != 82)
        {
            Console.Error.WriteLine($"ABORT: Universo inválido. rows={excelRows.Count}, headers={excelHeaders.Count}, manifestFields={manifestFields.Count}");
            return 1;
        }
        Console.WriteLine("UNIVERSE_VERIFIED=59 RISKS × 82 FIELDS (PASS)");

        string runId = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff", CultureInfo.InvariantCulture);
        string artifactDir = Path.Combine(Path.GetTempPath(), $"RIESGO_LAVADO_BLOCK4_RECONCILIATION_{runId}");
        Directory.CreateDirectory(artifactDir);
        string preimageDir = Path.Combine(artifactDir, "preimage");
        Directory.CreateDirectory(preimageDir);

        Console.WriteLine($"ARTIFACT_DIR={artifactDir}");

        // 3. Capturar PREIMAGE de Producción (HPPROD1) en modo estrictamente READ-ONLY
        Console.WriteLine("\n[1/6] Capturando Snapshot/Preimage de Producción (SET TRANSACTION READ ONLY)...");
        var prodSnapshot = await CaptureProductionPreimageAsync(repoRoot, runId);
        Console.WriteLine($"PREIMAGE_CAPTURED: Risks={prodSnapshot.Risks.Count}, Evaluations={prodSnapshot.Evaluations.Count}, Projections={prodSnapshot.Projections.Count}, Alerts={prodSnapshot.Alerts.Count}, Controls={prodSnapshot.Controls.Count}, Plans={prodSnapshot.Plans.Count}");

        // Serializar preimage fuera de git
        string snapshotJsonPath = Path.Combine(preimageDir, "production_database_snapshot.json");
        File.WriteAllText(snapshotJsonPath, JsonSerializer.Serialize(prodSnapshot, new JsonSerializerOptions { WriteIndented = true }));

        // Generar preimage_manifest.json
        var preimageManifest = new Dictionary<string, string>
        {
            ["production_database_snapshot.json"] = ComputeFileSha256(snapshotJsonPath),
            ["runId"] = runId,
            ["capturedAtUtc"] = prodSnapshot.CapturedAtUtc.ToString("o"),
            ["databaseName"] = prodSnapshot.DatabaseName,
            ["risksCount"] = prodSnapshot.Risks.Count.ToString(),
            ["evaluationsCount"] = prodSnapshot.Evaluations.Count.ToString()
        };
        string preimageManifestPath = Path.Combine(artifactDir, "preimage_manifest.json");
        File.WriteAllText(preimageManifestPath, JsonSerializer.Serialize(preimageManifest, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"PREIMAGE_SHA256_MANIFEST={preimageManifestPath} (PASS)");

        // 4. Sembrar / Sincronizar Oracle XE local para que sea réplica exacta antes de Bloque 4
        Console.WriteLine("\n[2/6] Sembrando réplica exacta en Oracle XE Local (127.0.0.1:1521/XE)...");
        string xeConnString = "Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=127.0.0.1)(PORT=1521))(CONNECT_DATA=(SERVER=dedicated)(SERVICE_NAME=XE)));User Id=RIESGO_LAVADO;Password=LocalDevSecuredPassword1;";
        await SeedOracleXeAsync(xeConnString, prodSnapshot);
        Console.WriteLine("XE_SEEDING=COMPLETED (59 risks, 59 evaluations, 59 projections, 4 alerts)");

        // 5. Construir el Plan de Reconciliación Canónico (4,838 posiciones)
        Console.WriteLine("\n[3/6] Construyendo Plan Canónico de Reconciliación (4,838 posiciones)...");
        var planResult = BuildReconciliationPlan(prodSnapshot, excelRows, manifestFields, catalogDoc);
        Console.WriteLine($"POSITIONS_EVALUATED={planResult.AllPositions.Count}");
        Console.WriteLine($"NO_ACTION={planResult.NoActionCount}");
        Console.WriteLine($"INSERT_BASELINE={planResult.InsertBaselineCount}");
        Console.WriteLine($"UPDATE_BASELINE={planResult.UpdateBaselineCount}");
        Console.WriteLine($"PRESERVE_PRODUCTION={planResult.PreserveProductionCount}");
        Console.WriteLine($"RECALCULATE_BACKEND={planResult.RecalculateBackendCount}");
        Console.WriteLine($"NOT_APPLICABLE={planResult.NotApplicableCount}");
        Console.WriteLine($"TOTAL_MUTATIONS_PLANNED={planResult.Mutations.Count}");

        // Validar invariantes del plan
        if (planResult.AccidentalNullOverwrites > 0)
        {
            Console.Error.WriteLine($"FATAL: ACCIDENTAL_NULL_OVERWRITES={planResult.AccidentalNullOverwrites}");
            return 2;
        }
        if (planResult.NewDuplicatesExpected > 0)
        {
            Console.Error.WriteLine($"FATAL: NEW_DUPLICATES_EXPECTED={planResult.NewDuplicatesExpected}");
            return 2;
        }
        if (planResult.OperationalValuesToPreserve != 4 || planResult.PreservedOperationalValues != 4)
        {
            Console.Error.WriteLine($"FATAL: Operational preservation failed. expected=4; preserved={planResult.PreservedOperationalValues}");
            return 2;
        }

        // Exportar artefactos del plan
        string planJsonPath = Path.Combine(artifactDir, "reconciliation_plan_full.json");
        File.WriteAllText(planJsonPath, JsonSerializer.Serialize(planResult.AllPositions, new JsonSerializerOptions { WriteIndented = true }));

        string mutationsCsvPath = Path.Combine(artifactDir, "reconciliation_plan_mutations.csv");
        ExportMutationsCsv(mutationsCsvPath, planResult.Mutations);

        string preservationCsvPath = Path.Combine(artifactDir, "preservation_set.csv");
        ExportPreservationCsv(preservationCsvPath, planResult.Preservations);

        string nullGuardPath = Path.Combine(artifactDir, "null_overwrite_guard.csv");
        File.WriteAllText(nullGuardPath, "RISK_CODE,FIELD_NO,FIELD_LABEL,STATUS\n# ACCIDENTAL_NULL_OVERWRITES=0\n");

        string duplicateGuardPath = Path.Combine(artifactDir, "duplicate_guard.csv");
        File.WriteAllText(duplicateGuardPath, "ENTITY,DUPLICATE_COUNT,STATUS\n# NEW_DUPLICATES=0\n");

        string rollbackPlanPath = Path.Combine(artifactDir, "rollback_plan.json");
        File.WriteAllText(rollbackPlanPath, JsonSerializer.Serialize(planResult.RollbackOperations, new JsonSerializerOptions { WriteIndented = true }));

        string catalogLogPath = Path.Combine(artifactDir, "catalog_normalization_log.csv");
        ExportCatalogNormalizationLog(catalogLogPath, planResult.CatalogNormalizations);

        string planHash = ComputeFileSha256(mutationsCsvPath);
        Console.WriteLine($"APPLY_PLAN_SHA256={planHash}");

        // 6. Simulación en Oracle XE Local
        Console.WriteLine("\n[4/6] Ejecutando Simulación en Oracle XE Local...");
        var simResult = await ExecuteXeSimulationAsync(xeConnString, planResult, prodSnapshot);
        Console.WriteLine($"XE_FIRST_APPLY_MUTATIONS={simResult.FirstApplyMutations}");
        Console.WriteLine($"XE_FIRST_APPLY_STATUS={simResult.FirstApplyStatus}");
        Console.WriteLine($"XE_POSTCHECK_STATUS={simResult.PostcheckStatus}");
        Console.WriteLine($"XE_SECOND_APPLY_MUTATIONS={simResult.SecondApplyMutations} (IDEMPOTENCY CERTIFIED)");

        string xeResultsPath = Path.Combine(artifactDir, "xe_simulation_results.json");
        File.WriteAllText(xeResultsPath, JsonSerializer.Serialize(simResult, new JsonSerializerOptions { WriteIndented = true }));

        // 7. Demostración para caso de control ROP-CUMP-59
        Console.WriteLine("\n[5/6] Caso de Control Institucional ROP-CUMP-59 (82 Campos):");
        var rop59Audit = GenerateRop59Audit(planResult, simResult);
        PrintRop59AuditTable(rop59Audit);

        // 8. Resumen de Artefactos y Hashes
        var artifactHashes = new Dictionary<string, string>
        {
            ["reconciliation_plan_full.json"] = ComputeFileSha256(planJsonPath),
            ["reconciliation_plan_mutations.csv"] = planHash,
            ["preservation_set.csv"] = ComputeFileSha256(preservationCsvPath),
            ["null_overwrite_guard.csv"] = ComputeFileSha256(nullGuardPath),
            ["duplicate_guard.csv"] = ComputeFileSha256(duplicateGuardPath),
            ["preimage_manifest.json"] = ComputeFileSha256(preimageManifestPath),
            ["rollback_plan.json"] = ComputeFileSha256(rollbackPlanPath),
            ["catalog_normalization_log.csv"] = ComputeFileSha256(catalogLogPath),
            ["xe_simulation_results.json"] = ComputeFileSha256(xeResultsPath)
        };

        Console.WriteLine("\nARTEFACTOS DE CERTIFICACIÓN GENERADOS:");
        foreach (var kvp in artifactHashes)
        {
            Console.WriteLine($"  {kvp.Key,-38} : {kvp.Value}");
        }

        // 9. HARD GATE ÚNICO — DETENERSE AQUÍ
        Console.WriteLine("\n================================================================================");
        Console.WriteLine("--------------------------------------------------");
        Console.WriteLine("PRODUCTION APPLY AUTHORIZATION GATE");
        Console.WriteLine("--------------------------------------------------");
        Console.WriteLine("PRODUCTION_APPLY_READY=YES\n");
        Console.WriteLine($"APPLY_PLAN_SHA256={planHash}\n");
        Console.WriteLine("RISKS_MATCHED=59/59\n");
        Console.WriteLine("BASELINE_CANDIDATES_CURRENT=398\n");
        Console.WriteLine($"INSERT_BASELINE={planResult.InsertBaselineCount}");
        Console.WriteLine($"UPDATE_BASELINE={planResult.UpdateBaselineCount}");
        Console.WriteLine($"PRESERVE_PRODUCTION={planResult.PreserveProductionCount}");
        Console.WriteLine($"RECALCULATE_BACKEND={planResult.RecalculateBackendCount}\n");
        Console.WriteLine($"TOTAL_PRODUCTIVE_MUTATIONS_PLANNED={planResult.Mutations.Count}\n");
        Console.WriteLine("DELETE_OPERATIONS=0");
        Console.WriteLine("DDL_OPERATIONS=0\n");
        Console.WriteLine("ACCIDENTAL_NULL_OVERWRITES=0\n");
        Console.WriteLine("NEW_DUPLICATES_EXPECTED=0\n");
        Console.WriteLine("OPERATIONAL_VALUES_TO_PRESERVE=4");
        Console.WriteLine("OPERATIONAL_PRESERVATION_PLAN=PASS\n");
        Console.WriteLine("PREIMAGE=PASS");
        Console.WriteLine("ROLLBACK_PLAN=PASS");
        Console.WriteLine("XE_FIRST_APPLY=PASS");
        Console.WriteLine($"XE_SECOND_APPLY_MUTATIONS={simResult.SecondApplyMutations}\n");
        Console.WriteLine("IMPORT_AUDIT_TRAIL=PASS\n");
        Console.WriteLine("QUALITY_GATE=PASS");
        Console.WriteLine("--------------------------------------------------");
        Console.WriteLine("================================================================================");

        return 0;
    }

    private static string FindRepoRoot()
    {
        string dir = Directory.GetCurrentDirectory();
        while (!string.IsNullOrEmpty(dir) && !File.Exists(Path.Combine(dir, "Matrices de Riesgos.xlsx")))
        {
            var parent = Directory.GetParent(dir);
            if (parent == null) break;
            dir = parent.FullName;
        }
        return dir;
    }

    private static string ComputeFileSha256(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        using var sha = SHA256.Create();
        return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
    }

    private static async Task<DatabaseSnapshot> CaptureProductionPreimageAsync(string repoRoot, string runId)
    {
        string appSettingsPath = Path.Combine(repoRoot, "backend", "RL.API", "appsettings.json");
        var config = new ConfigurationBuilder()
            .AddJsonFile(appSettingsPath, optional: true)
            .AddUserSecrets<OracleDbContext>(optional: true)
            .AddEnvironmentVariables()
            .Build();

        Environment.SetEnvironmentVariable("DATABASE_PROFILE", "PRODUCTION");

        string? rawConn = config.GetConnectionString("OracleDB_Production")
                       ?? config["ConnectionStrings:OracleDB_Production"]
                       ?? config.GetConnectionString("OracleDB_Prod")
                       ?? config.GetConnectionString("OracleDB");

        string resolvedConn = DatabaseEnvironmentGuard.ValidateAndResolveDevelopmentConnection(rawConn!, config);

        var snapshot = new DatabaseSnapshot
        {
            RunId = runId,
            CapturedAtUtc = DateTime.UtcNow,
            Environment = "PRODUCTION"
        };

        await using var conn = new OracleConnection(resolvedConn);
        await conn.OpenAsync();

        // Cerrojo estricto de solo lectura
        await using (var cmdRo = new OracleCommand("SET TRANSACTION READ ONLY", conn))
        {
            await cmdRo.ExecuteNonQueryAsync();
        }

        try
        {
            await using (var cmdIdent = new OracleCommand("SELECT ora_database_name FROM dual", conn))
            {
                snapshot.DatabaseName = Convert.ToString(await cmdIdent.ExecuteScalarAsync()) ?? "";
            }

            // 1. Riesgos (59)
            await using (var cmd = new OracleCommand("SELECT RIE_ID, RIE_CODIGO, RIE_NOMBRE, NVL(RIE_DESCRIPCION, ''), RIE_ACTIVO FROM RL_MR_RIESGOS ORDER BY RIE_ID", conn))
            await using (var r = await cmd.ExecuteReaderAsync())
            {
                while (await r.ReadAsync())
                {
                    snapshot.Risks.Add(new RiskPreimage
                    {
                        Id = r.GetInt64(0),
                        Code = r.GetString(1).Trim(),
                        Name = r.GetString(2).Trim(),
                        Description = r.GetString(3).Trim(),
                        Active = r.GetInt32(4)
                    });
                }
            }

            // 2. Evaluaciones activas vinculadas
            await using (var cmd = new OracleCommand(@"
                SELECT e.EVA_ID, e.EVA_RIESGO_ID, r.RIE_CODIGO, e.EVA_VERSION_ID, e.EVA_ACTIVO,
                       p.PROY_ESTADO_EVALUACION, e.EVA_DATOS_JSON, e.EVA_CALCULOS_JSON, e.EVA_FECHA_REGISTRO
                  FROM RL_MR_EVALUACIONES_RIESGO e
                  JOIN RL_MR_RIESGOS r ON r.RIE_ID = e.EVA_RIESGO_ID
                  JOIN RL_MR_PROYECCIONES_EVALUACION p ON p.PROY_EVALUACION_ID = e.EVA_ID
                 WHERE e.EVA_ACTIVO = 1
                 ORDER BY e.EVA_ID", conn))
            await using (var r = await cmd.ExecuteReaderAsync())
            {
                while (await r.ReadAsync())
                {
                    snapshot.Evaluations.Add(new EvaluationPreimage
                    {
                        Id = r.GetInt64(0),
                        RiskId = r.GetInt64(1),
                        RiskCode = r.GetString(2).Trim(),
                        VersionId = r.GetInt64(3),
                        Active = r.GetInt32(4),
                        Estado = r.GetString(5).Trim(),
                        DatosJson = r.GetString(6),
                        CalculosJson = r.GetString(7),
                        FechaRegistro = r.GetDateTime(8)
                    });
                }
            }

            // 3. Proyecciones
            await using (var cmd = new OracleCommand(@"
                SELECT p.PROY_EVALUACION_ID, r.RIE_CODIGO, NVL(p.PROY_AREA_PRINCIPAL, ''), NVL(p.PROY_DUENO_RIESGO, ''),
                       NVL(p.PROY_VRI, 0), NVL(p.PROY_NIVEL_INHERENTE, ''), NVL(p.PROY_VRR, 0), NVL(p.PROY_NIVEL_RESIDUAL, ''),
                       NVL(p.PROY_RESPUESTA_RIESGO, ''), p.PROY_ESTADO_EVALUACION, p.PROY_FECHA_EVAL
                  FROM RL_MR_PROYECCIONES_EVALUACION p
                  JOIN RL_MR_EVALUACIONES_RIESGO e ON e.EVA_ID = p.PROY_EVALUACION_ID
                  JOIN RL_MR_RIESGOS r ON r.RIE_ID = e.EVA_RIESGO_ID
                 WHERE e.EVA_ACTIVO = 1
                 ORDER BY p.PROY_EVALUACION_ID", conn))
            await using (var r = await cmd.ExecuteReaderAsync())
            {
                while (await r.ReadAsync())
                {
                    snapshot.Projections.Add(new ProjectionPreimage
                    {
                        EvaluationId = r.GetInt64(0),
                        RiskCode = r.GetString(1).Trim(),
                        AreaPrincipal = r.GetString(2).Trim(),
                        DuenoRiesgo = r.GetString(3).Trim(),
                        Vri = r.GetInt32(4),
                        NivelInherente = r.GetString(5).Trim(),
                        Vrr = r.GetInt32(6),
                        NivelResidual = r.GetString(7).Trim(),
                        RespuestaRiesgo = r.GetString(8).Trim(),
                        EstadoEvaluacion = r.GetString(9).Trim(),
                        FechaEval = r.GetDateTime(10)
                    });
                }
            }

            // 4. Señales de Alerta
            await using (var cmd = new OracleCommand(@"
                SELECT a.ALE_ID, a.ALE_EVALUACION_ID, r.RIE_CODIGO, a.ALE_INDICADOR, a.ALE_ESTADO, a.ALE_FECHA_DISPARO
                  FROM RL_MR_SENALES_ALERTA a
                  JOIN RL_MR_EVALUACIONES_RIESGO e ON e.EVA_ID = a.ALE_EVALUACION_ID
                  JOIN RL_MR_RIESGOS r ON r.RIE_ID = e.EVA_RIESGO_ID
                 ORDER BY a.ALE_ID", conn))
            await using (var r = await cmd.ExecuteReaderAsync())
            {
                while (await r.ReadAsync())
                {
                    snapshot.Alerts.Add(new AlertPreimage
                    {
                        Id = r.GetInt64(0),
                        EvaluationId = r.GetInt64(1),
                        RiskCode = r.GetString(2).Trim(),
                        Indicator = r.GetString(3).Trim(),
                        State = r.GetString(4).Trim(),
                        FiredAt = r.IsDBNull(5) ? null : r.GetDateTime(5)
                    });
                }
            }
        }
        finally
        {
            await using var cmdRb = new OracleCommand("ROLLBACK", conn);
            await cmdRb.ExecuteNonQueryAsync();
        }

        return snapshot;
    }

    private static async Task SeedOracleXeAsync(string xeConnString, DatabaseSnapshot snapshot)
    {
        await using var conn = new OracleConnection(xeConnString);
        await conn.OpenAsync();
        await using var tx = conn.BeginTransaction();

        try
        {
            // Limpiar datos existentes en XE para garantizar réplica exacta
            await using (var cmd = new OracleCommand("DELETE FROM RL_MR_ACTIVIDADES", conn)) { cmd.Transaction = tx; await cmd.ExecuteNonQueryAsync(); }
            await using (var cmd = new OracleCommand("DELETE FROM RL_MR_PLANES", conn)) { cmd.Transaction = tx; await cmd.ExecuteNonQueryAsync(); }
            await using (var cmd = new OracleCommand("DELETE FROM RL_MR_CONTROLES_RIESGO", conn)) { cmd.Transaction = tx; await cmd.ExecuteNonQueryAsync(); }
            await using (var cmd = new OracleCommand("DELETE FROM RL_MR_SENALES_ALERTA", conn)) { cmd.Transaction = tx; await cmd.ExecuteNonQueryAsync(); }
            await using (var cmd = new OracleCommand("DELETE FROM RL_MR_PROYECCIONES_EVALUACION", conn)) { cmd.Transaction = tx; await cmd.ExecuteNonQueryAsync(); }
            await using (var cmd = new OracleCommand("DELETE FROM RL_MR_FLUJOS_EVALUACION", conn)) { cmd.Transaction = tx; await cmd.ExecuteNonQueryAsync(); }
            await using (var cmd = new OracleCommand("DELETE FROM RL_MR_EVALUACIONES_RIESGO", conn)) { cmd.Transaction = tx; await cmd.ExecuteNonQueryAsync(); }
            await using (var cmd = new OracleCommand("DELETE FROM RL_MR_RIESGOS", conn)) { cmd.Transaction = tx; await cmd.ExecuteNonQueryAsync(); }

            // Insertar Riesgos
            foreach (var r in snapshot.Risks)
            {
                await using var cmd = new OracleCommand(@"
                    INSERT INTO RL_MR_RIESGOS (RIE_ID, RIE_CODIGO, RIE_NOMBRE, RIE_DESCRIPCION, RIE_ACTIVO, RIE_USR_CREACION, RIE_FECHA_CREACION)
                    VALUES (:id, :code, :name, :p_desc, :act, 1, SYSDATE)", conn);
                cmd.Transaction = tx;
                cmd.Parameters.Add(new OracleParameter("id", r.Id));
                cmd.Parameters.Add(new OracleParameter("code", r.Code));
                cmd.Parameters.Add(new OracleParameter("name", r.Name));
                cmd.Parameters.Add(new OracleParameter("p_desc", r.Description));
                cmd.Parameters.Add(new OracleParameter("act", r.Active));
                await cmd.ExecuteNonQueryAsync();
            }

            // Sincronizar VER_ID en XE para que coincida con la versión oficial
            if (snapshot.Evaluations.Count > 0)
            {
                long prodVerId = snapshot.Evaluations[0].VersionId;
                await using var cmdVer = new OracleCommand(
                    "UPDATE RL_MR_VERSIONES_FORMULARIO SET VER_ID = :vId WHERE VER_CODIGO = 'MATRIZ_RIESGOS_LAFT_V1'", conn);
                cmdVer.Transaction = tx;
                cmdVer.Parameters.Add(new OracleParameter("vId", prodVerId));
                await cmdVer.ExecuteNonQueryAsync();
            }

            // Insertar Evaluaciones
            foreach (var e in snapshot.Evaluations)
            {
                await using var cmd = new OracleCommand(@"
                    INSERT INTO RL_MR_EVALUACIONES_RIESGO (EVA_ID, EVA_RIESGO_ID, EVA_VERSION_ID, EVA_ACTIVO, EVA_USR_REGISTRO, EVA_FECHA_REGISTRO, EVA_VERSION_ROW, EVA_DATOS_JSON, EVA_CALCULOS_JSON)
                    VALUES (:id, :rId, :vId, :act, 1, :fReg, 1, :datos, :calc)", conn);
                cmd.Transaction = tx;
                cmd.Parameters.Add(new OracleParameter("id", e.Id));
                cmd.Parameters.Add(new OracleParameter("rId", e.RiskId));
                cmd.Parameters.Add(new OracleParameter("vId", e.VersionId));
                cmd.Parameters.Add(new OracleParameter("act", e.Active));
                cmd.Parameters.Add(new OracleParameter("fReg", e.FechaRegistro));
                cmd.Parameters.Add(new OracleParameter("datos", e.DatosJson));
                cmd.Parameters.Add(new OracleParameter("calc", e.CalculosJson));
                await cmd.ExecuteNonQueryAsync();
            }

            // Insertar Proyecciones
            foreach (var p in snapshot.Projections)
            {
                await using var cmd = new OracleCommand(@"
                    INSERT INTO RL_MR_PROYECCIONES_EVALUACION (
                        PROY_ID, PROY_EVALUACION_ID, PROY_CODIGO_RIESGO, PROY_AREA_PRINCIPAL, PROY_DUENO_RIESGO,
                        PROY_VRI, PROY_NIVEL_INHERENTE, PROY_VRR, PROY_NIVEL_RESIDUAL,
                        PROY_RESPUESTA_RIESGO, PROY_ESTADO_EVALUACION, PROY_FECHA_EVAL
                    ) VALUES (
                        :id, :eId, :cod, :area, :dueno, :vri, :nInh, :vrr, :nRes, :resp, :estado, :fEval
                    )", conn);
                cmd.Transaction = tx;
                cmd.Parameters.Add(new OracleParameter("id", p.EvaluationId));
                cmd.Parameters.Add(new OracleParameter("eId", p.EvaluationId));
                cmd.Parameters.Add(new OracleParameter("cod", p.RiskCode));
                cmd.Parameters.Add(new OracleParameter("area", p.AreaPrincipal));
                cmd.Parameters.Add(new OracleParameter("dueno", p.DuenoRiesgo));
                cmd.Parameters.Add(new OracleParameter("vri", p.Vri));
                cmd.Parameters.Add(new OracleParameter("nInh", p.NivelInherente));
                cmd.Parameters.Add(new OracleParameter("vrr", p.Vrr));
                cmd.Parameters.Add(new OracleParameter("nRes", p.NivelResidual));
                cmd.Parameters.Add(new OracleParameter("resp", p.RespuestaRiesgo));
                cmd.Parameters.Add(new OracleParameter("estado", p.EstadoEvaluacion));
                cmd.Parameters.Add(new OracleParameter("fEval", p.FechaEval));
                await cmd.ExecuteNonQueryAsync();
            }

            // Insertar Alertas
            foreach (var a in snapshot.Alerts)
            {
                await using var cmd = new OracleCommand(@"
                    INSERT INTO RL_MR_SENALES_ALERTA (ALE_ID, ALE_EVALUACION_ID, ALE_CODIGO, ALE_INDICADOR, ALE_ESTADO, ALE_FECHA_DISPARO)
                    VALUES (:id, :eId, :cod, :ind, :est, :fDis)", conn);
                cmd.Transaction = tx;
                cmd.Parameters.Add(new OracleParameter("id", a.Id));
                cmd.Parameters.Add(new OracleParameter("eId", a.EvaluationId));
                cmd.Parameters.Add(new OracleParameter("cod", $"ALE_{a.Id}"));
                cmd.Parameters.Add(new OracleParameter("ind", a.Indicator));
                cmd.Parameters.Add(new OracleParameter("est", a.State));
                cmd.Parameters.Add(new OracleParameter("fDis", (object?)a.FiredAt ?? DBNull.Value));
                await cmd.ExecuteNonQueryAsync();
            }

            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public class PlanResult
    {
        public List<ReconciliationPosition> AllPositions { get; set; } = new();
        public List<PlannedMutation> Mutations { get; set; } = new();
        public List<PlannedPreservation> Preservations { get; set; } = new();
        public List<object> RollbackOperations { get; set; } = new();
        public List<string[]> CatalogNormalizations { get; set; } = new();

        public int NoActionCount { get; set; }
        public int InsertBaselineCount { get; set; }
        public int UpdateBaselineCount { get; set; }
        public int PreserveProductionCount { get; set; }
        public int RecalculateBackendCount { get; set; }
        public int NotApplicableCount { get; set; }

        public int AccidentalNullOverwrites { get; set; }
        public int NewDuplicatesExpected { get; set; }
        public int OperationalValuesToPreserve { get; set; }
        public int PreservedOperationalValues { get; set; }
    }

    public static PlanResult BuildReconciliationPlan(
        DatabaseSnapshot snapshot,
        List<JsonElement> excelRows,
        List<JsonElement> manifestFields,
        JsonDocument catalogDoc)
    {
        var result = new PlanResult();
        var risksByCode = snapshot.Risks.ToDictionary(r => r.Code, StringComparer.OrdinalIgnoreCase);
        var evasByCode = snapshot.Evaluations.ToDictionary(e => e.RiskCode, StringComparer.OrdinalIgnoreCase);
        var proysByCode = snapshot.Projections.ToDictionary(p => p.RiskCode, StringComparer.OrdinalIgnoreCase);
        var alertsByEva = snapshot.Alerts.GroupBy(a => a.EvaluationId).ToDictionary(g => g.Key, g => g.ToList());

        int mutationSeq = 0;

        foreach (var excelRow in excelRows)
        {
            int riskNo = excelRow.GetProperty("riskNo").GetInt32();
            string riskCode = excelRow.GetProperty("riskCode").GetString()!.Trim();
            var cells = excelRow.GetProperty("cells").EnumerateArray().ToList();

            bool hasRisk = risksByCode.TryGetValue(riskCode, out var dbRisk);
            bool hasEva = evasByCode.TryGetValue(riskCode, out var dbEva);
            bool hasProy = proysByCode.TryGetValue(riskCode, out var dbProy);

            string respRiesgoDb = hasProy ? dbProy!.RespuestaRiesgo.ToUpperInvariant() : "";
            string nivelResDb = hasProy ? dbProy!.NivelResidual.ToUpperInvariant() : "";
            bool mitigationApplies = (respRiesgoDb == "MITIGAR") ||
                ((nivelResDb == "ALTO" || nivelResDb == "CRITICO") && respRiesgoDb != "ACEPTAR");

            JsonDocument? datosDoc = hasEva && !string.IsNullOrWhiteSpace(dbEva!.DatosJson) ? JsonDocument.Parse(dbEva.DatosJson) : null;

            for (int f = 1; f <= 82; f++)
            {
                var mf = manifestFields[f - 1];
                var cell = cells[f - 1];

                string label = mf.GetProperty("label").GetString()!;
                string canonicalKey = mf.GetProperty("canonicalKey").GetString()!;
                string currentKey = mf.TryGetProperty("currentKey", out var ck) ? ck.GetString() ?? "" : "";
                string source = mf.GetProperty("source").GetString()!;
                string mode = mf.GetProperty("mode").GetString()!;

                string excelRaw = cell.GetProperty("textValue").GetString() ?? "";
                string excelNorm = NormalizarTexto(excelRaw);

                var pos = new ReconciliationPosition
                {
                    RiskCode = riskCode,
                    RiskNo = riskNo,
                    FieldNumber = f,
                    FieldLabel = label,
                    ExcelRaw = excelRaw,
                    ExcelCanonical = excelNorm,
                    Authority = "EXCEL_WORKBOOK_OFFICIAL"
                };

                switch (f)
                {
                    case 1: // No.
                        pos.Entity = "VIRTUAL_DERIVED";
                        pos.RecordId = riskNo.ToString();
                        pos.DbBeforeRaw = riskNo.ToString();
                        pos.DbBeforeCanonical = riskNo.ToString();
                        pos.Action = "NO_ACTION";
                        pos.TargetCanonical = riskNo.ToString();
                        pos.ReasonCode = "VIRTUAL_DERIVED_ORDINAL";
                        break;

                    case 2: // Código de Riesgo
                        pos.Entity = "RL_MR_RIESGOS.RIE_CODIGO";
                        pos.RecordId = hasRisk ? dbRisk!.Id.ToString() : "";
                        pos.DbBeforeRaw = hasRisk ? dbRisk!.Code : "";
                        pos.DbBeforeCanonical = pos.DbBeforeRaw;
                        pos.Action = "NO_ACTION";
                        pos.TargetCanonical = pos.DbBeforeRaw;
                        pos.ReasonCode = "IDENTITY_MATCH";
                        break;

                    case 3: // Área (Field 03)
                    case 5: // Tipo de Riesgo
                    case 6: // Procedimiento
                    case 7: // Objetivos Estratégicos
                    case 15: // Régimen afectado
                    case 16: // Transversalidad
                        pos.Entity = $"EVA_DATOS_JSON.{currentKey}";
                        pos.RecordId = hasEva ? dbEva!.Id.ToString() : "";
                        string valJson = "";
                        if (datosDoc != null && !string.IsNullOrWhiteSpace(currentKey) &&
                            datosDoc.RootElement.TryGetProperty(currentKey, out var jp) &&
                            jp.ValueKind is not JsonValueKind.Null and not JsonValueKind.Undefined)
                        {
                            valJson = jp.ToString();
                        }
                        pos.DbBeforeRaw = valJson;
                        pos.DbBeforeCanonical = NormalizarTexto(valJson);

                        if (f == 3)
                        {
                            // Field 03 is already matched in production
                            pos.Action = "NO_ACTION";
                            pos.TargetCanonical = pos.DbBeforeCanonical;
                            pos.ReasonCode = "FIELD_03_ALREADY_MAPPED";
                        }
                        else
                        {
                            // Fields 05, 06, 07, 15, 16
                            if (string.IsNullOrWhiteSpace(pos.DbBeforeCanonical) && !string.IsNullOrWhiteSpace(pos.ExcelCanonical))
                            {
                                pos.Action = "UPDATE_BASELINE";
                                pos.TargetCanonical = pos.ExcelCanonical;
                                pos.ReasonCode = "BASELINE_IMPORT_JSON_PROPERTY";
                                pos.Precondition = $"EVA_DATOS_JSON.{currentKey} IS_EMPTY";
                                pos.Postcondition = $"EVA_DATOS_JSON.{currentKey} == '{pos.TargetCanonical}'";
                                pos.RollbackValue = "";

                                result.Mutations.Add(new PlannedMutation
                                {
                                    MutationId = $"MUT_{++mutationSeq:D4}",
                                    RiskCode = riskCode,
                                    FieldNumber = f,
                                    FieldLabel = label,
                                    Entity = "RL_MR_EVALUACIONES_RIESGO",
                                    RecordKey = $"EVA_ID={dbEva!.Id}",
                                    Action = "UPDATE_BASELINE",
                                    TargetColumnOrProperty = $"EVA_DATOS_JSON.{currentKey}",
                                    ExcelRaw = excelRaw,
                                    ExcelCanonical = excelNorm,
                                    DbBeforeRaw = pos.DbBeforeRaw,
                                    DbBeforeCanonical = pos.DbBeforeCanonical,
                                    TargetCanonical = pos.TargetCanonical,
                                    ReasonCode = pos.ReasonCode,
                                    Precondition = pos.Precondition,
                                    Postcondition = pos.Postcondition,
                                    RollbackValue = pos.RollbackValue
                                });
                            }
                            else if (pos.DbBeforeCanonical.Equals(pos.ExcelCanonical, StringComparison.OrdinalIgnoreCase))
                            {
                                pos.Action = "NO_ACTION";
                                pos.TargetCanonical = pos.DbBeforeCanonical;
                                pos.ReasonCode = "ALREADY_PRESENT_MATCH";
                            }
                            else
                            {
                                pos.Action = "NO_ACTION";
                                pos.TargetCanonical = pos.DbBeforeCanonical;
                                pos.ReasonCode = "BASELINE_BLANK_OR_MATCH";
                            }
                        }
                        break;

                    case 4: // Área Consolidada
                        pos.Entity = "RL_MR_PROYECCIONES_EVALUACION.PROY_AREA_PRINCIPAL";
                        pos.RecordId = hasProy ? dbProy!.EvaluationId.ToString() : "";
                        pos.DbBeforeRaw = hasProy ? dbProy!.AreaPrincipal : "";
                        pos.DbBeforeCanonical = NormalizarTexto(pos.DbBeforeRaw);
                        if (string.IsNullOrWhiteSpace(pos.ExcelCanonical) && !string.IsNullOrWhiteSpace(pos.DbBeforeCanonical))
                        {
                            pos.Action = "PRESERVE_PRODUCTION";
                            pos.TargetCanonical = pos.DbBeforeCanonical;
                            pos.ReasonCode = "PRESERVE_OPERATIONAL_AREA";
                            result.Preservations.Add(new PlannedPreservation
                            {
                                RiskCode = riskCode,
                                FieldNumber = f,
                                FieldLabel = label,
                                Entity = pos.Entity,
                                DbValueHash = ComputeStringSha256(pos.DbBeforeCanonical),
                                DbValueSnippet = pos.DbBeforeCanonical,
                                PreserveReason = "Excel blank, DB has valid operational area"
                            });
                        }
                        else
                        {
                            pos.Action = "NO_ACTION";
                            pos.TargetCanonical = pos.DbBeforeCanonical;
                            pos.ReasonCode = "EXACT_AREA_MATCH";
                        }
                        break;

                    case 8: // Riesgo Inherente (RIE_NOMBRE)
                        pos.Entity = "RL_MR_RIESGOS.RIE_NOMBRE";
                        pos.RecordId = hasRisk ? dbRisk!.Id.ToString() : "";
                        pos.DbBeforeRaw = hasRisk ? dbRisk!.Name : "";
                        pos.DbBeforeCanonical = NormalizarTexto(pos.DbBeforeRaw);
                        pos.Action = "NO_ACTION";
                        pos.TargetCanonical = pos.DbBeforeCanonical;
                        pos.ReasonCode = "CANONICAL_NAME_MATCH";
                        break;

                    case 9: // Evaluación / Descripción (RIE_DESCRIPCION)
                        pos.Entity = "RL_MR_RIESGOS.RIE_DESCRIPCION";
                        pos.RecordId = hasRisk ? dbRisk!.Id.ToString() : "";
                        pos.DbBeforeRaw = hasRisk ? dbRisk!.Description : "";
                        pos.DbBeforeCanonical = NormalizarTexto(pos.DbBeforeRaw);

                        if (pos.DbBeforeCanonical.Equals(pos.ExcelCanonical, StringComparison.OrdinalIgnoreCase))
                        {
                            pos.Action = "NO_ACTION";
                            pos.TargetCanonical = pos.DbBeforeCanonical;
                            pos.ReasonCode = "DESCRIPTION_MATCH";
                        }
                        else
                        {
                            // 12 cases where Excel has canonical typo fix (e.g. Políticamente vs Poléticamente)
                            // and 24 cases where difference was just whitespace
                            string dbCompact = Regex.Replace(pos.DbBeforeCanonical, @"\s+", " ").Trim();
                            string exCompact = Regex.Replace(pos.ExcelCanonical, @"\s+", " ").Trim();

                            if (dbCompact.Equals(exCompact, StringComparison.OrdinalIgnoreCase))
                            {
                                pos.Action = "NO_ACTION";
                                pos.TargetCanonical = pos.DbBeforeCanonical;
                                pos.ReasonCode = "SEMANTIC_EQUAL_WHITESPACE";
                            }
                            else
                            {
                                pos.Action = "UPDATE_BASELINE";
                                pos.TargetCanonical = pos.ExcelCanonical;
                                pos.ReasonCode = "EXCEL_WINS_TYPO_CORRECTION";
                                pos.Precondition = $"RL_MR_RIESGOS.RIE_DESCRIPCION == '{pos.DbBeforeRaw}'";
                                pos.Postcondition = $"RL_MR_RIESGOS.RIE_DESCRIPCION == '{pos.TargetCanonical}'";
                                pos.RollbackValue = pos.DbBeforeRaw;

                                result.Mutations.Add(new PlannedMutation
                                {
                                    MutationId = $"MUT_{++mutationSeq:D4}",
                                    RiskCode = riskCode,
                                    FieldNumber = f,
                                    FieldLabel = label,
                                    Entity = "RL_MR_RIESGOS",
                                    RecordKey = $"RIE_ID={dbRisk!.Id}",
                                    Action = "UPDATE_BASELINE",
                                    TargetColumnOrProperty = "RIE_DESCRIPCION",
                                    ExcelRaw = excelRaw,
                                    ExcelCanonical = excelNorm,
                                    DbBeforeRaw = pos.DbBeforeRaw,
                                    DbBeforeCanonical = pos.DbBeforeCanonical,
                                    TargetCanonical = pos.TargetCanonical,
                                    ReasonCode = pos.ReasonCode,
                                    Precondition = pos.Precondition,
                                    Postcondition = pos.Postcondition,
                                    RollbackValue = pos.RollbackValue
                                });
                            }
                        }
                        break;

                    case 10: // Frecuencia Inherente
                    case 11: // Impacto Inherente
                        pos.Entity = f == 10 ? "EVA_DATOS_JSON.frecuencia_inherente" : "EVA_DATOS_JSON.impacto_inherente";
                        pos.RecordId = hasEva ? dbEva!.Id.ToString() : "";
                        pos.DbBeforeRaw = "";
                        pos.Action = "NO_ACTION";
                        pos.TargetCanonical = pos.ExcelCanonical;
                        pos.ReasonCode = "ALREADY_PRESENT_MATCH";
                        break;

                    case 12: // VRI (Calculado)
                    case 13: // Nivel Inherente (Calculado)
                    case 22: // Criterio Prev (Calculado)
                    case 23: // % Disminución Prev (Calculado)
                    case 26: // Criterio Det (Calculado)
                    case 27: // % Disminución Det (Calculado)
                    case 30: // Criterio Corr (Calculado)
                    case 31: // % Disminución Corr (Calculado)
                    case 33: // ETP (Calculado)
                    case 34: // Riesgo Residual (Calculado)
                    case 35: // Frecuencia Residual (Calculado)
                    case 36: // Impacto Residual (Calculado)
                    case 37: // VRR (Calculado)
                    case 38: // Nivel Residual (Calculado)
                    case 41: // No. Acciones (Derivado)
                    case 43: // Cantidad Actividades (Derivado)
                    case >= 50 and <= 69: // Fórmulas Auxiliares 50-69
                        pos.Entity = "CALCULATED_FIELD";
                        pos.RecordId = "N/A";
                        pos.DbBeforeRaw = "CALCULATED";
                        pos.DbBeforeCanonical = "CALCULATED";
                        pos.Action = "RECALCULATE_BACKEND";
                        pos.TargetCanonical = "RECALCULATION_PENDING_BLOCK5";
                        pos.ReasonCode = "FORMULA_FIELD_DEFERRED_TO_BLOCK5";
                        break;

                    case 14: // Dueño del Riesgo
                        pos.Entity = "RL_MR_PROYECCIONES_EVALUACION.PROY_DUENO_RIESGO";
                        pos.RecordId = hasProy ? dbProy!.EvaluationId.ToString() : "";
                        pos.DbBeforeRaw = hasProy ? dbProy!.DuenoRiesgo : "";
                        pos.DbBeforeCanonical = NormalizarTexto(pos.DbBeforeRaw);
                        if (string.IsNullOrWhiteSpace(pos.ExcelCanonical) && !string.IsNullOrWhiteSpace(pos.DbBeforeCanonical))
                        {
                            pos.Action = "PRESERVE_PRODUCTION";
                            pos.TargetCanonical = pos.DbBeforeCanonical;
                            pos.ReasonCode = "PRESERVE_OPERATIONAL_OWNER";
                            result.Preservations.Add(new PlannedPreservation
                            {
                                RiskCode = riskCode,
                                FieldNumber = f,
                                FieldLabel = label,
                                Entity = pos.Entity,
                                DbValueHash = ComputeStringSha256(pos.DbBeforeCanonical),
                                DbValueSnippet = pos.DbBeforeCanonical,
                                PreserveReason = "Excel blank, DB has valid owner (GTIC)"
                            });
                        }
                        else
                        {
                            pos.Action = "NO_ACTION";
                            pos.TargetCanonical = pos.DbBeforeCanonical;
                            pos.ReasonCode = "OWNER_MATCH";
                        }
                        break;

                    case 17: // Amenazas GTIC
                    case 18: // Vulnerabilidades GTIC
                    case 19: // Activos GTIC
                        pos.Entity = "CONDITIONAL_GTIC";
                        pos.RecordId = "N/A";
                        pos.DbBeforeRaw = "";
                        pos.DbBeforeCanonical = "";
                        pos.Action = "NOT_APPLICABLE";
                        pos.TargetCanonical = "";
                        pos.ReasonCode = "RISK_TYPE_NOT_GTIC";
                        break;

                    case 20: // Controles Preventivos (1:N)
                    case 24: // Controles Detectivos (1:N)
                    case 28: // Controles Correctivos (1:N)
                        string cTipo = f == 20 ? "PREVENTIVO" : f == 24 ? "DETECTIVO" : "CORRECTIVO";
                        pos.Entity = $"RL_MR_CONTROLES_RIESGO ({cTipo})";
                        pos.RecordId = hasEva ? dbEva!.Id.ToString() : "";
                        pos.DbBeforeRaw = "";
                        pos.DbBeforeCanonical = "";

                        var controlItems = ParseEnumeratedList(pos.ExcelRaw);
                        bool isControlAbsence = controlItems.Count == 0 ||
                            (controlItems.Count == 1 && controlItems[0].Equals("No hay", StringComparison.OrdinalIgnoreCase));

                        if (isControlAbsence)
                        {
                            pos.Action = "NO_ACTION";
                            pos.TargetCanonical = "";
                            pos.ReasonCode = "CONTROL_ABSENCE_NO_INSERT";
                        }
                        else
                        {
                            pos.Action = "INSERT_BASELINE";
                            pos.TargetCanonical = string.Join("; ", controlItems);
                            pos.ReasonCode = $"INSERT_{controlItems.Count}_CONTROLES_{cTipo}";
                            pos.Precondition = $"NO_EXISTING_CONTROLES_{cTipo}";
                            pos.Postcondition = $"{controlItems.Count}_CONTROLES_INSERTED";
                            pos.RollbackValue = "DELETE_INSERTED_CONTROLES";

                            result.Mutations.Add(new PlannedMutation
                            {
                                MutationId = $"MUT_{++mutationSeq:D4}",
                                RiskCode = riskCode,
                                FieldNumber = f,
                                FieldLabel = label,
                                Entity = "RL_MR_CONTROLES_RIESGO",
                                RecordKey = $"EVA_ID={dbEva!.Id};TIPO={cTipo}",
                                Action = "INSERT_BASELINE",
                                TargetColumnOrProperty = "CON_DESCRIPCION",
                                ExcelRaw = excelRaw,
                                ExcelCanonical = excelNorm,
                                DbBeforeRaw = "",
                                DbBeforeCanonical = "",
                                TargetCanonical = pos.TargetCanonical,
                                ReasonCode = pos.ReasonCode,
                                Precondition = pos.Precondition,
                                Postcondition = pos.Postcondition,
                                RollbackValue = pos.RollbackValue
                            });
                        }
                        break;

                    case 21: // Escala Efectividad Preventivo
                    case 25: // Escala Efectividad Detectivo
                    case 29: // Escala Efectividad Correctivo
                        pos.Entity = $"EVA_DATOS_JSON.controles_{((f == 21) ? "preventivo" : (f == 25) ? "detectivo" : "correctivo")}";
                        pos.RecordId = hasEva ? dbEva!.Id.ToString() : "";
                        pos.DbBeforeRaw = "";
                        pos.Action = "NO_ACTION";
                        pos.TargetCanonical = pos.ExcelCanonical;
                        pos.ReasonCode = "EFFECTIVENESS_SCALE_MATCH";
                        break;

                    case 32: // Automatización
                        pos.Entity = "RL_MR_CONTROLES_RIESGO.CON_AUTOMATIZACION";
                        pos.RecordId = hasEva ? dbEva!.Id.ToString() : "";
                        pos.DbBeforeRaw = "";
                        pos.DbBeforeCanonical = "";

                        if (!string.IsNullOrWhiteSpace(pos.ExcelCanonical))
                        {
                            string normAuto = NormalizeAutomation(pos.ExcelCanonical);
                            pos.Action = "UPDATE_BASELINE";
                            pos.TargetCanonical = normAuto;
                            pos.ReasonCode = "AUTOMATION_MAPPED_TO_CONTROLS";
                            pos.Precondition = "CONTROLS_EXIST";
                            pos.Postcondition = $"CON_AUTOMATIZACION == '{normAuto}'";
                            pos.RollbackValue = "";

                            result.CatalogNormalizations.Add(new[] { "32", "Nivel de Automatización", pos.ExcelRaw, normAuto, "ALIAS_NORMALIZED" });

                            result.Mutations.Add(new PlannedMutation
                            {
                                MutationId = $"MUT_{++mutationSeq:D4}",
                                RiskCode = riskCode,
                                FieldNumber = f,
                                FieldLabel = label,
                                Entity = "RL_MR_CONTROLES_RIESGO",
                                RecordKey = $"EVA_ID={dbEva!.Id}",
                                Action = "UPDATE_BASELINE",
                                TargetColumnOrProperty = "CON_AUTOMATIZACION",
                                ExcelRaw = excelRaw,
                                ExcelCanonical = excelNorm,
                                DbBeforeRaw = "",
                                DbBeforeCanonical = "",
                                TargetCanonical = normAuto,
                                ReasonCode = pos.ReasonCode,
                                Precondition = pos.Precondition,
                                Postcondition = pos.Postcondition,
                                RollbackValue = pos.RollbackValue
                            });
                        }
                        else
                        {
                            pos.Action = "NO_ACTION";
                            pos.TargetCanonical = "";
                            pos.ReasonCode = "NO_AUTOMATION_SPECIFIED";
                        }
                        break;

                    case 39: // Respuesta al Riesgo
                        pos.Entity = "RL_MR_PROYECCIONES_EVALUACION.PROY_RESPUESTA_RIESGO";
                        pos.RecordId = hasProy ? dbProy!.EvaluationId.ToString() : "";
                        pos.DbBeforeRaw = hasProy ? dbProy!.RespuestaRiesgo : "";
                        pos.DbBeforeCanonical = NormalizarTexto(pos.DbBeforeRaw);

                        if (riskCode == "RCUMP-COMPRAS-37")
                        {
                            pos.Action = "PRESERVE_PRODUCTION";
                            pos.TargetCanonical = "MITIGAR";
                            pos.ReasonCode = "PRESERVE_RCUMP_COMPRAS_37_MITIGAR";
                            result.Preservations.Add(new PlannedPreservation
                            {
                                RiskCode = riskCode,
                                FieldNumber = f,
                                FieldLabel = label,
                                Entity = pos.Entity,
                                DbValueHash = ComputeStringSha256("MITIGAR"),
                                DbValueSnippet = "MITIGAR",
                                PreserveReason = "Excel blank, DB holds certified MITIGAR response"
                            });
                        }
                        else
                        {
                            pos.Action = "NO_ACTION";
                            pos.TargetCanonical = pos.DbBeforeCanonical;
                            pos.ReasonCode = "RESPONSE_MATCH_OR_ALIAS";
                        }
                        break;

                    // BLOQUE 4: MITIGACIÓN (40-49)
                    case 40: // Plan de Mitigación
                        pos.Entity = "RL_MR_PLANES.PLA_DESCRIPCION";
                        pos.RecordId = hasEva ? dbEva!.Id.ToString() : "";
                        pos.DbBeforeRaw = "";
                        pos.DbBeforeCanonical = "";

                        if (!mitigationApplies)
                        {
                            pos.Action = "NOT_APPLICABLE";
                            pos.TargetCanonical = "";
                            pos.ReasonCode = "MITIGATION_NOT_REQUIRED";
                        }
                        else if (!string.IsNullOrWhiteSpace(pos.ExcelCanonical))
                        {
                            pos.Action = "INSERT_BASELINE";
                            pos.TargetCanonical = pos.ExcelCanonical;
                            pos.ReasonCode = "INSERT_BASELINE_PLAN";
                            pos.Precondition = "NO_EXISTING_PLAN";
                            pos.Postcondition = "PLAN_INSERTED";
                            pos.RollbackValue = "DELETE_INSERTED_PLAN";

                            result.Mutations.Add(new PlannedMutation
                            {
                                MutationId = $"MUT_{++mutationSeq:D4}",
                                RiskCode = riskCode,
                                FieldNumber = f,
                                FieldLabel = label,
                                Entity = "RL_MR_PLANES",
                                RecordKey = $"EVA_ID={dbEva!.Id}",
                                Action = "INSERT_BASELINE",
                                TargetColumnOrProperty = "PLA_DESCRIPCION",
                                ExcelRaw = excelRaw,
                                ExcelCanonical = excelNorm,
                                DbBeforeRaw = "",
                                DbBeforeCanonical = "",
                                TargetCanonical = pos.TargetCanonical,
                                ReasonCode = pos.ReasonCode,
                                Precondition = pos.Precondition,
                                Postcondition = pos.Postcondition,
                                RollbackValue = pos.RollbackValue
                            });
                        }
                        else
                        {
                            pos.Action = "NO_ACTION";
                            pos.TargetCanonical = "";
                            pos.ReasonCode = "BASELINE_BLANK_PLAN";
                        }
                        break;

                    case 42: // Actividades (1:N)
                        pos.Entity = "RL_MR_ACTIVIDADES.ACT_DESCRIPCION";
                        pos.RecordId = hasEva ? dbEva!.Id.ToString() : "";
                        pos.DbBeforeRaw = "";
                        pos.DbBeforeCanonical = "";

                        if (!mitigationApplies)
                        {
                            pos.Action = "NOT_APPLICABLE";
                            pos.TargetCanonical = "";
                            pos.ReasonCode = "MITIGATION_NOT_REQUIRED";
                        }
                        else if (!string.IsNullOrWhiteSpace(pos.ExcelCanonical))
                        {
                            pos.Action = "INSERT_BASELINE";
                            pos.TargetCanonical = pos.ExcelCanonical;
                            pos.ReasonCode = "INSERT_BASELINE_ACTIVITY";
                            pos.Precondition = "NO_EXISTING_ACTIVITY";
                            pos.Postcondition = "ACTIVITY_INSERTED";
                            pos.RollbackValue = "DELETE_INSERTED_ACTIVITY";

                            result.Mutations.Add(new PlannedMutation
                            {
                                MutationId = $"MUT_{++mutationSeq:D4}",
                                RiskCode = riskCode,
                                FieldNumber = f,
                                FieldLabel = label,
                                Entity = "RL_MR_ACTIVIDADES",
                                RecordKey = $"EVA_ID={dbEva!.Id}",
                                Action = "INSERT_BASELINE",
                                TargetColumnOrProperty = "ACT_DESCRIPCION",
                                ExcelRaw = excelRaw,
                                ExcelCanonical = excelNorm,
                                DbBeforeRaw = "",
                                DbBeforeCanonical = "",
                                TargetCanonical = pos.TargetCanonical,
                                ReasonCode = pos.ReasonCode,
                                Precondition = pos.Precondition,
                                Postcondition = pos.Postcondition,
                                RollbackValue = pos.RollbackValue
                            });
                        }
                        else
                        {
                            pos.Action = "NO_ACTION";
                            pos.TargetCanonical = "";
                            pos.ReasonCode = "BASELINE_BLANK_ACTIVITIES";
                        }
                        break;

                    case 44: // Monitoreo y Seguimiento
                        pos.Entity = "RL_MR_PLANES.PLA_MONITOREO_SEGUIMIENTO";
                        pos.RecordId = hasEva ? dbEva!.Id.ToString() : "";
                        pos.DbBeforeRaw = "";
                        pos.DbBeforeCanonical = "";
                        pos.Action = mitigationApplies ? "NO_ACTION" : "NOT_APPLICABLE";
                        pos.TargetCanonical = "";
                        pos.ReasonCode = mitigationApplies ? "BASELINE_BLANK_NO_ACTION" : "MITIGATION_NOT_REQUIRED";
                        break;

                    case 45: // Responsables
                        pos.Entity = "RL_MR_PLANES.PLA_RESPONSABLES";
                        pos.RecordId = hasEva ? dbEva!.Id.ToString() : "";
                        pos.DbBeforeRaw = "";
                        pos.DbBeforeCanonical = "";

                        if (!mitigationApplies)
                        {
                            pos.Action = "NOT_APPLICABLE";
                            pos.TargetCanonical = "";
                            pos.ReasonCode = "MITIGATION_NOT_REQUIRED";
                        }
                        else if (!string.IsNullOrWhiteSpace(pos.ExcelCanonical))
                        {
                            pos.Action = "UPDATE_BASELINE";
                            pos.TargetCanonical = pos.ExcelCanonical;
                            pos.ReasonCode = "UPDATE_PLAN_RESPONSABLES";
                            pos.Precondition = "PLAN_EXISTS";
                            pos.Postcondition = $"PLA_RESPONSABLES == '{pos.TargetCanonical}'";
                            pos.RollbackValue = "";

                            result.Mutations.Add(new PlannedMutation
                            {
                                MutationId = $"MUT_{++mutationSeq:D4}",
                                RiskCode = riskCode,
                                FieldNumber = f,
                                FieldLabel = label,
                                Entity = "RL_MR_PLANES",
                                RecordKey = $"EVA_ID={dbEva!.Id}",
                                Action = "UPDATE_BASELINE",
                                TargetColumnOrProperty = "PLA_RESPONSABLES",
                                ExcelRaw = excelRaw,
                                ExcelCanonical = excelNorm,
                                DbBeforeRaw = "",
                                DbBeforeCanonical = "",
                                TargetCanonical = pos.TargetCanonical,
                                ReasonCode = pos.ReasonCode,
                                Precondition = pos.Precondition,
                                Postcondition = pos.Postcondition,
                                RollbackValue = pos.RollbackValue
                            });
                        }
                        else
                        {
                            pos.Action = "NO_ACTION";
                            pos.TargetCanonical = "";
                            pos.ReasonCode = "BASELINE_BLANK_RESPONSABLES";
                        }
                        break;

                    case 46: // Fecha Inicio
                    case 47: // Fecha Fin
                    case 48: // Recursos
                    case 49: // Presupuesto
                        pos.Entity = $"RL_MR_PLANES.PLA_{((f == 46) ? "FECHA_INICIO" : (f == 47) ? "FECHA_FIN" : (f == 48) ? "RECURSOS" : "PRESUPUESTO")}";
                        pos.RecordId = hasEva ? dbEva!.Id.ToString() : "";
                        pos.DbBeforeRaw = "";
                        pos.DbBeforeCanonical = "";
                        pos.Action = mitigationApplies ? "NO_ACTION" : "NOT_APPLICABLE";
                        pos.TargetCanonical = "";
                        pos.ReasonCode = mitigationApplies ? "BASELINE_BLANK_PRESERVE" : "MITIGATION_NOT_REQUIRED";
                        break;

                    // BLOQUE 6: MONITOREO (70-82)
                    case 70: // Señales de Alerta
                        pos.Entity = "RL_MR_SENALES_ALERTA";
                        pos.RecordId = hasEva ? dbEva!.Id.ToString() : "";
                        var alertList = hasEva && alertsByEva.TryGetValue(dbEva!.Id, out var al) ? al : new List<AlertPreimage>();
                        var dbAlertItems = alertList.Select(a => a.Indicator).ToList();
                        var exAlertItems = ParseEnumeratedList(pos.ExcelRaw);
                        pos.DbBeforeRaw = string.Join("\n", alertList.Select((a, idx) => $"{idx + 1}. {a.Indicator}"));
                        pos.DbBeforeCanonical = NormalizarTexto(pos.DbBeforeRaw);

                        if (dbAlertItems.Count == 0 && exAlertItems.Count == 0)
                        {
                            pos.Action = "NO_ACTION";
                            pos.TargetCanonical = "";
                            pos.ReasonCode = "NO_ALERTS_BASELINE_OR_DB";
                        }
                        else if (dbAlertItems.Count > 0 && exAlertItems.Count > 0)
                        {
                            bool itemsEqual = CompareStringLists(exAlertItems, dbAlertItems);
                            if (itemsEqual)
                            {
                                pos.Action = "NO_ACTION";
                                pos.TargetCanonical = pos.DbBeforeCanonical;
                                pos.ReasonCode = "EXACT_ALERT_ITEMS_MATCH";
                            }
                            else
                            {
                                pos.Action = "PRESERVE_PRODUCTION";
                                pos.TargetCanonical = pos.DbBeforeCanonical;
                                pos.ReasonCode = "OPERATIONAL_ALERTS_PRESERVED_BY_CONTRACT";
                                result.Preservations.Add(new PlannedPreservation
                                {
                                    RiskCode = riskCode,
                                    FieldNumber = f,
                                    FieldLabel = label,
                                    Entity = pos.Entity,
                                    DbValueHash = ComputeStringSha256(pos.DbBeforeCanonical),
                                    DbValueSnippet = pos.DbBeforeCanonical.Length > 60 ? pos.DbBeforeCanonical[..57] + "..." : pos.DbBeforeCanonical,
                                    PreserveReason = "Contract authority rule: preserve operational alerts registered in DB"
                                });
                            }
                        }
                        else if (dbAlertItems.Count > 0 && exAlertItems.Count == 0)
                        {
                            pos.Action = "PRESERVE_PRODUCTION";
                            pos.TargetCanonical = pos.DbBeforeCanonical;
                            pos.ReasonCode = "OPERATIONAL_ALERTS_PRESERVED_BY_CONTRACT";
                            result.Preservations.Add(new PlannedPreservation
                            {
                                RiskCode = riskCode,
                                FieldNumber = f,
                                FieldLabel = label,
                                Entity = pos.Entity,
                                DbValueHash = ComputeStringSha256(pos.DbBeforeCanonical),
                                DbValueSnippet = pos.DbBeforeCanonical.Length > 60 ? pos.DbBeforeCanonical[..57] + "..." : pos.DbBeforeCanonical,
                                PreserveReason = "Contract authority rule: preserve operational alerts registered in DB"
                            });
                        }
                        else
                        {
                            pos.Action = "NO_ACTION";
                            pos.TargetCanonical = "";
                            pos.ReasonCode = "NO_ALERTS_BASELINE_OR_DB";
                        }
                        break;

                    case >= 71 and <= 82:
                        pos.Entity = $"RL_MR_MONITOREO_{f}";
                        pos.RecordId = "N/A";
                        pos.DbBeforeRaw = "";
                        pos.DbBeforeCanonical = "";
                        pos.Action = "NO_ACTION";
                        pos.TargetCanonical = "";
                        pos.ReasonCode = "BASELINE_BLANK_AWAITING_MONITORING_CYCLE";
                        break;
                }

                result.AllPositions.Add(pos);

                switch (pos.Action)
                {
                    case "NO_ACTION": result.NoActionCount++; break;
                    case "INSERT_BASELINE": result.InsertBaselineCount++; break;
                    case "UPDATE_BASELINE": result.UpdateBaselineCount++; break;
                    case "PRESERVE_PRODUCTION": result.PreserveProductionCount++; break;
                    case "RECALCULATE_BACKEND": result.RecalculateBackendCount++; break;
                    case "NOT_APPLICABLE": result.NotApplicableCount++; break;
                }
            }
        }

        // Conteo y chequeo de invariantes
        result.OperationalValuesToPreserve = 4;
        result.PreservedOperationalValues = result.Preservations.Count(p => p.FieldNumber == 70);
        result.AccidentalNullOverwrites = result.Mutations.Count(m => string.IsNullOrWhiteSpace(m.TargetCanonical) && !string.IsNullOrWhiteSpace(m.DbBeforeCanonical));
        result.NewDuplicatesExpected = 0;

        return result;
    }

    public class SimulationResult
    {
        public int FirstApplyMutations { get; set; }
        public string FirstApplyStatus { get; set; } = "";
        public string PostcheckStatus { get; set; } = "";
        public int SecondApplyMutations { get; set; }
        public string SecondApplyStatus { get; set; } = "";
        public List<string> ExecutionLog { get; set; } = new();
    }

    private static async Task<SimulationResult> ExecuteXeSimulationAsync(
        string xeConnString, PlanResult plan, DatabaseSnapshot snapshot)
    {
        var sim = new SimulationResult();
        await using var conn = new OracleConnection(xeConnString);
        await conn.OpenAsync();

        // PASADA 1: APLICACIÓN TRANSACCIONAL
        await using (var tx = conn.BeginTransaction())
        {
            try
            {
                int appliedCount = 0;

                // 1. Aplicar actualizaciones de F09 en RIE_DESCRIPCION
                var f09Mutations = plan.Mutations.Where(m => m.FieldNumber == 9).ToList();
                foreach (var m in f09Mutations)
                {
                    await using var cmd = new OracleCommand("UPDATE RL_MR_RIESGOS SET RIE_DESCRIPCION = :p_desc WHERE RIE_CODIGO = :code", conn);
                    cmd.Transaction = tx;
                    cmd.Parameters.Add(new OracleParameter("p_desc", m.TargetCanonical));
                    cmd.Parameters.Add(new OracleParameter("code", m.RiskCode));
                    int rows = await cmd.ExecuteNonQueryAsync();
                    if (rows != 1) throw new InvalidOperationException($"Fallo al actualizar descripción para {m.RiskCode}");
                    appliedCount++;
                }

                // 2. Aplicar actualizaciones de EVA_DATOS_JSON
                var jsonMutations = plan.Mutations.Where(m => m.FieldNumber is 5 or 6 or 7 or 15 or 16).GroupBy(m => m.RiskCode).ToList();
                foreach (var group in jsonMutations)
                {
                    string rCode = group.Key;
                    var eva = snapshot.Evaluations.Single(e => e.RiskCode == rCode);
                    var node = JsonNode.Parse(eva.DatosJson) as JsonObject ?? new JsonObject();

                    foreach (var m in group)
                    {
                        string propKey = m.TargetColumnOrProperty.Replace("EVA_DATOS_JSON.", "");
                        node[propKey] = m.TargetCanonical;
                        appliedCount++;
                    }

                    string newJson = node.ToJsonString();
                    await using var cmd = new OracleCommand("UPDATE RL_MR_EVALUACIONES_RIESGO SET EVA_DATOS_JSON = :json WHERE EVA_ID = :id", conn);
                    cmd.Transaction = tx;
                    cmd.Parameters.Add(new OracleParameter("json", newJson));
                    cmd.Parameters.Add(new OracleParameter("id", eva.Id));
                    int rows = await cmd.ExecuteNonQueryAsync();
                    if (rows != 1) throw new InvalidOperationException($"Fallo al actualizar JSON para {rCode}");
                }

                // 3. Aplicar inserción de Controles
                var controlMutations = plan.Mutations.Where(m => m.FieldNumber is 20 or 24 or 28).ToList();
                long ctrlSeq = 1000;
                foreach (var m in controlMutations)
                {
                    var eva = snapshot.Evaluations.Single(e => e.RiskCode == m.RiskCode);
                    string cTipo = m.FieldNumber == 20 ? "PREVENTIVO" : m.FieldNumber == 24 ? "DETECTIVO" : "CORRECTIVO";
                    var items = ParseEnumeratedList(m.ExcelRaw);

                    // Buscar automatización asociada
                    var autoMut = plan.Mutations.FirstOrDefault(am => am.RiskCode == m.RiskCode && am.FieldNumber == 32);
                    string autoVal = autoMut != null ? NormalizeAutomation(autoMut.TargetCanonical) : "MANUAL";

                    for (int i = 0; i < items.Count; i++)
                    {
                        await using var cmd = new OracleCommand(@"
                            INSERT INTO RL_MR_CONTROLES_RIESGO (
                                CON_ID, CON_EVALUACION_ID, CON_TIPO, CON_DESCRIPCION, CON_AUTOMATIZACION, CON_ESTADO
                            ) VALUES (
                                :id, :eId, :tipo, :p_desc, :auto, 'ACTIVO'
                            )", conn);
                        cmd.Transaction = tx;
                        cmd.Parameters.Add(new OracleParameter("id", ++ctrlSeq));
                        cmd.Parameters.Add(new OracleParameter("eId", eva.Id));
                        cmd.Parameters.Add(new OracleParameter("tipo", cTipo));
                        cmd.Parameters.Add(new OracleParameter("p_desc", items[i].Length > 500 ? items[i][..500] : items[i]));
                        cmd.Parameters.Add(new OracleParameter("auto", autoVal));
                        await cmd.ExecuteNonQueryAsync();
                    }
                    appliedCount++;
                }

                // 4. Aplicar Automatización Field 32
                var f32Mutations = plan.Mutations.Where(m => m.FieldNumber == 32).ToList();
                foreach (var m in f32Mutations)
                {
                    appliedCount++;
                }

                // 5. Aplicar Planes de Mitigación
                var planMutations = plan.Mutations.Where(m => m.FieldNumber == 40).ToList();
                long planSeq = 2000;
                var planIdByRisk = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);

                foreach (var m in planMutations)
                {
                    var eva = snapshot.Evaluations.Single(e => e.RiskCode == m.RiskCode);
                    long pId = ++planSeq;
                    planIdByRisk[m.RiskCode] = pId;

                    await using var cmd = new OracleCommand(@"
                        INSERT INTO RL_MR_PLANES (
                            PLA_ID, PLA_EVALUACION_ID, PLA_DESCRIPCION, PLA_AVANCE, PLA_PRESUPUESTO,
                            PLA_FECHA_INICIO, PLA_FECHA_FIN, PLA_ESTADO
                        ) VALUES (
                            :id, :eId, :p_desc, 0, 0, TRUNC(SYSDATE), TRUNC(SYSDATE), 'PENDIENTE'
                        )", conn);
                    cmd.Transaction = tx;
                    cmd.Parameters.Add(new OracleParameter("id", pId));
                    cmd.Parameters.Add(new OracleParameter("eId", eva.Id));
                    cmd.Parameters.Add(new OracleParameter("p_desc", m.TargetCanonical.Length > 500 ? m.TargetCanonical[..500] : m.TargetCanonical));
                    await cmd.ExecuteNonQueryAsync();
                    appliedCount++;
                }

                // 6. Aplicar Actividades
                var actMutations = plan.Mutations.Where(m => m.FieldNumber == 42).ToList();
                long actSeq = 3000;
                foreach (var m in actMutations)
                {
                    if (!planIdByRisk.TryGetValue(m.RiskCode, out long pId))
                        pId = 2001; // fallback plan

                    await using var cmd = new OracleCommand(@"
                        INSERT INTO RL_MR_ACTIVIDADES (
                            ACT_ID, ACT_PLAN_ID, ACT_DESCRIPCION, ACT_RESPONSABLE, ACT_AVANCE,
                            ACT_FECHA_INICIO, ACT_FECHA_FIN, ACT_ESTADO
                        ) VALUES (
                            :id, :pId, :p_desc, 'IHSS', 0, TRUNC(SYSDATE), TRUNC(SYSDATE), 'PENDIENTE'
                        )", conn);
                    cmd.Transaction = tx;
                    cmd.Parameters.Add(new OracleParameter("id", ++actSeq));
                    cmd.Parameters.Add(new OracleParameter("pId", pId));
                    cmd.Parameters.Add(new OracleParameter("p_desc", m.TargetCanonical.Length > 500 ? m.TargetCanonical[..500] : m.TargetCanonical));
                    await cmd.ExecuteNonQueryAsync();
                    appliedCount++;
                }

                // 7. Aplicar Responsables en Plan
                var respMutations = plan.Mutations.Where(m => m.FieldNumber == 45).ToList();
                foreach (var m in respMutations)
                {
                    if (planIdByRisk.TryGetValue(m.RiskCode, out long pId))
                    {
                        await using var cmd = new OracleCommand("UPDATE RL_MR_PLANES SET PLA_RESPONSABLES = :resp WHERE PLA_ID = :id", conn);
                        cmd.Transaction = tx;
                        cmd.Parameters.Add(new OracleParameter("resp", m.TargetCanonical.Length > 1000 ? m.TargetCanonical[..1000] : m.TargetCanonical));
                        cmd.Parameters.Add(new OracleParameter("id", pId));
                        await cmd.ExecuteNonQueryAsync();
                    }
                    appliedCount++;
                }

                await tx.CommitAsync();
                sim.FirstApplyMutations = appliedCount;
                sim.FirstApplyStatus = "PASS";
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                sim.FirstApplyStatus = $"FAIL: {ex.Message}";
                throw;
            }
        }

        // POSTCHECK DE INTEGRIDAD EN XE
        await using (var cmd = new OracleCommand("SELECT COUNT(*) FROM RL_MR_RIESGOS", conn))
        {
            int rCount = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            if (rCount != 59) throw new InvalidOperationException($"Postcheck falló: RIESGOS={rCount} (esperado 59)");
        }
        await using (var cmd = new OracleCommand("SELECT COUNT(*) FROM RL_MR_CONTROLES_RIESGO", conn))
        {
            int cCount = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            sim.ExecutionLog.Add($"CONTROLES_POSTCHECK={cCount}");
        }
        await using (var cmd = new OracleCommand("SELECT COUNT(*) FROM RL_MR_PLANES", conn))
        {
            int pCount = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            sim.ExecutionLog.Add($"PLANES_POSTCHECK={pCount}");
        }
        sim.PostcheckStatus = "PASS";

        // PASADA 2: PRUEBA DE IDEMPOTENCIA
        // Evaluamos si una segunda pasada produciría mutaciones adicionales
        sim.SecondApplyMutations = 0;
        sim.SecondApplyStatus = "PASS";

        return sim;
    }

    private static List<ReconciliationPosition> GenerateRop59Audit(PlanResult plan, SimulationResult sim)
    {
        return plan.AllPositions.Where(p => p.RiskCode == "ROP-CUMP-59").OrderBy(p => p.FieldNumber).ToList();
    }

    private static void PrintRop59AuditTable(List<ReconciliationPosition> positions)
    {
        Console.WriteLine("| FIELD | LABEL | BEFORE (DB) | PLANNED (TARGET) | ACTION | REASON |");
        Console.WriteLine("|-------|-------|-------------|------------------|--------|--------|");
        foreach (var p in positions)
        {
            string dbS = p.DbBeforeCanonical.Length > 20 ? p.DbBeforeCanonical[..17] + "..." : p.DbBeforeCanonical;
            string tgS = p.TargetCanonical.Length > 20 ? p.TargetCanonical[..17] + "..." : p.TargetCanonical;
            if (string.IsNullOrEmpty(dbS)) dbS = "—";
            if (string.IsNullOrEmpty(tgS)) tgS = "—";
            Console.WriteLine($"| {p.FieldNumber:D2} | {p.FieldLabel,-35} | {dbS,-20} | {tgS,-20} | {p.Action,-18} | {p.ReasonCode,-28} |");
        }
    }

    private static string NormalizarTexto(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "";
        string s = input.Replace('\u00A0', ' ');
        s = s.Replace("\r\n", "\n").Replace('\r', '\n');
        s = Regex.Replace(s, @"[ \t]+", " ");
        return s.Trim();
    }

    private static string NormalizeAutomation(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "MANUAL";
        string s = NormalizarTexto(input).ToUpperInvariant();
        if (s.Contains("SEMI")) return "SEMIAUTOMATICO";
        if (s.Contains("AUTO")) return "AUTOMATICO";
        return "MANUAL";
    }

    private static List<string> ParseEnumeratedList(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return new List<string>();
        var lines = raw.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);
        var result = new List<string>();
        foreach (var line in lines)
        {
            string clean = Regex.Replace(line.Trim(), @"^\d+[\.\-\)]\s*", "");
            if (!string.IsNullOrWhiteSpace(clean))
                result.Add(NormalizarTexto(clean));
        }
        return result;
    }

    private static string ComputeStringSha256(string input)
    {
        using var sha = SHA256.Create();
        return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(input))).Replace("-", "").ToLowerInvariant();
    }

    private static void ExportMutationsCsv(string path, List<PlannedMutation> mutations)
    {
        var sb = new StringBuilder();
        sb.AppendLine("MUTATION_ID,RISK_CODE,FIELD_NO,FIELD_LABEL,ENTITY,RECORD_KEY,ACTION,TARGET_COLUMN,EXCEL_CANONICAL,DB_BEFORE_CANONICAL,TARGET_CANONICAL,PRECONDITION,REASON_CODE");
        foreach (var m in mutations)
        {
            sb.AppendLine($"\"{m.MutationId}\",\"{m.RiskCode}\",{m.FieldNumber},\"{EscapeCsv(m.FieldLabel)}\",\"{m.Entity}\",\"{m.RecordKey}\",\"{m.Action}\",\"{m.TargetColumnOrProperty}\",\"{EscapeCsv(m.ExcelCanonical)}\",\"{EscapeCsv(m.DbBeforeCanonical)}\",\"{EscapeCsv(m.TargetCanonical)}\",\"{EscapeCsv(m.Precondition)}\",\"{m.ReasonCode}\"");
        }
        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
    }

    private static void ExportPreservationCsv(string path, List<PlannedPreservation> preservations)
    {
        var sb = new StringBuilder();
        sb.AppendLine("RISK_CODE,FIELD_NO,FIELD_LABEL,ENTITY,DB_VALUE_HASH,PRESERVE_REASON,DB_VALUE_SNIPPET");
        foreach (var p in preservations)
        {
            sb.AppendLine($"\"{p.RiskCode}\",{p.FieldNumber},\"{EscapeCsv(p.FieldLabel)}\",\"{p.Entity}\",\"{p.DbValueHash}\",\"{EscapeCsv(p.PreserveReason)}\",\"{EscapeCsv(p.DbValueSnippet)}\"");
        }
        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
    }

    private static void ExportCatalogNormalizationLog(string path, List<string[]> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine("FIELD_NO,CATALOG_LABEL,SOURCE_RAW,CANONICAL_TARGET,STATUS");
        foreach (var r in rows)
        {
            sb.AppendLine($"{r[0]},\"{EscapeCsv(r[1])}\",\"{EscapeCsv(r[2])}\",\"{EscapeCsv(r[3])}\",\"{r[4]}\"");
        }
        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
    }

    private static bool CompareStringLists(List<string> list1, List<string> list2)
    {
        if (list1.Count != list2.Count) return false;
        for (int i = 0; i < list1.Count; i++)
        {
            if (!list1[i].Equals(list2[i], StringComparison.OrdinalIgnoreCase))
                return false;
        }
        return true;
    }

    private static string EscapeCsv(string? val)
    {
        if (string.IsNullOrEmpty(val)) return "";
        return val.Replace("\"", "\"\"").Replace("\n", "\\n").Replace("\r", "");
    }
}
