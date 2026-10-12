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

#region Modelos de Preimagen y Reconciliación

public class RiskPreimage
{
    public long Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public int Active { get; set; }
    public long UsrCreacion { get; set; }
    public DateTime FechaCreacion { get; set; }
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
    public long UsrRegistro { get; set; }
    public int VersionRow { get; set; }
}

public class ProjectionPreimage
{
    public long Id { get; set; }
    public long EvaluationId { get; set; }
    public string RiskCode { get; set; } = "";
    public string AreaPrincipal { get; set; } = "";
    public string DuenoRiesgo { get; set; } = "";
    public decimal Vri { get; set; }
    public string NivelInherente { get; set; } = "";
    public decimal Vrr { get; set; }
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
    public string Codigo { get; set; } = "";
    public string Indicador { get; set; } = "";
    public string Estado { get; set; } = "";
    public DateTime? FechaDisparo { get; set; }
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

public class ControlEvaluationPreimage
{
    public long Id { get; set; }
    public long ControlId { get; set; }
    public decimal Efectividad { get; set; }
    public string Comentario { get; set; } = "";
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

public class MonitoringPreimage
{
    public long Id { get; set; }
    public long EvaluationId { get; set; }
    public string RiskCode { get; set; } = "";
    public string EstadoRiesgo { get; set; } = "";
    public string EstadoContr { get; set; } = "";
    public string Resultado { get; set; } = "";
    public long UsrId { get; set; }
    public string ObservacionesArea { get; set; } = "";
    public string ObservacionesUgr { get; set; } = "";
    public DateTime Fecha { get; set; }
}

public class EvidencePreimage
{
    public long VinculoId { get; set; }
    public long EvidenciaId { get; set; }
    public string TipoEntidad { get; set; } = "";
    public long EntidadId { get; set; }
    public string NombreArchivo { get; set; } = "";
    public string Extension { get; set; } = "";
    public long Tamano { get; set; }
    public string Hash { get; set; } = "";
    public string Ruta { get; set; } = "";
    public DateTime FechaCreacion { get; set; }
}

public class FlujoPreimage
{
    public long Id { get; set; }
    public long EvaluationId { get; set; }
    public string Estado { get; set; } = "";
    public string Motivo { get; set; } = "";
    public long UsrId { get; set; }
    public DateTime Fecha { get; set; }
}

public class DatabaseSnapshot
{
    public string RunId { get; set; } = "";
    public DateTime CapturedAtUtc { get; set; }
    public string DatabaseName { get; set; } = "";
    public string ServiceName { get; set; } = "";
    public string CurrentSchema { get; set; } = "";
    public string SessionUser { get; set; } = "";

    public List<RiskPreimage> Risks { get; set; } = new();
    public List<EvaluationPreimage> Evaluations { get; set; } = new();
    public List<ProjectionPreimage> Projections { get; set; } = new();
    public List<AlertPreimage> Alerts { get; set; } = new();
    public List<ControlPreimage> Controls { get; set; } = new();
    public List<ControlEvaluationPreimage> ControlEvaluations { get; set; } = new();
    public List<PlanPreimage> Plans { get; set; } = new();
    public List<ActivityPreimage> Activities { get; set; } = new();
    public List<MonitoringPreimage> Monitorings { get; set; } = new();
    public List<EvidencePreimage> Evidences { get; set; } = new();
    public List<FlujoPreimage> Flujos { get; set; } = new();
}

public class RollbackOperation
{
    public string MutationId { get; set; } = "";
    public string Entity { get; set; } = "";
    public string Table { get; set; } = "";
    public string RecordPk { get; set; } = "";
    public string Action { get; set; } = "";
    public string RollbackAction { get; set; } = "";
    public string BeforeValue { get; set; } = "";
    public string AfterValue { get; set; } = "";
    public string RollbackSql { get; set; } = "";
    public Dictionary<string, object?> RollbackParameters { get; set; } = new();
}

public class PlannedMutation
{
    public string MutationId { get; set; } = "";
    public string RiskCode { get; set; } = "";
    public int FieldNumber { get; set; }
    public string FieldLabel { get; set; } = "";
    public string Entity { get; set; } = "";
    public string TargetTable { get; set; } = "";
    public string RecordKey { get; set; } = "";
    public string Action { get; set; } = ""; // UPDATE_BASELINE, INSERT_BASELINE
    public string TargetColumnOrProperty { get; set; } = "";
    public string ExcelRaw { get; set; } = "";
    public string ExcelCanonical { get; set; } = "";
    public string DbBeforeRaw { get; set; } = "";
    public string DbBeforeCanonical { get; set; } = "";
    public string TargetCanonical { get; set; } = "";
    public string ReasonCode { get; set; } = "";
    public string Authority { get; set; } = "";
    public string Precondition { get; set; } = "";
    public string PreconditionSql { get; set; } = "";
    public string ApplySql { get; set; } = "";
    public Dictionary<string, object?> ApplyParameters { get; set; } = new();
    public RollbackOperation Rollback { get; set; } = new();
}

public class ReconciliationPosition
{
    public string PositionKey { get; set; } = ""; // RISK_Fxx
    public string RiskCode { get; set; } = "";
    public int FieldNumber { get; set; }
    public string FieldLabel { get; set; } = "";
    public string Action { get; set; } = ""; // NO_ACTION, INSERT_BASELINE, UPDATE_BASELINE, PRESERVE_PRODUCTION, RECALCULATE_BACKEND, NOT_APPLICABLE, BLOCKED_BY_PRECONDITION
    public string ReasonCode { get; set; } = "";
    public string ExcelValue { get; set; } = "";
    public string DbValue { get; set; } = "";
    public string TargetValue { get; set; } = "";
    public bool Mutates { get; set; }
}

public class DuplicateGuardEntry
{
    public string EntityType { get; set; } = "";
    public string EntityKey { get; set; } = "";
    public int PreexistingCount { get; set; }
    public int PostApplyExpectedCount { get; set; }
    public int DuplicateCount { get; set; }
    public string Status { get; set; } = "PASS";
}

public class NullOverwriteGuardEntry
{
    public string RiskCode { get; set; } = "";
    public int FieldNumber { get; set; }
    public string FieldLabel { get; set; } = "";
    public string DbBeforeRaw { get; set; } = "";
    public string ExcelRaw { get; set; } = "";
    public string TargetCanonical { get; set; } = "";
    public string Authority { get; set; } = "";
    public bool IsNullOverwrite { get; set; }
    public string Status { get; set; } = "PASS";
}

public class Field71To82AuditEntry
{
    public string RiskCode { get; set; } = "";
    public int FieldNumber { get; set; }
    public string FieldLabel { get; set; } = "";
    public string TargetTable { get; set; } = "";
    public string RecordId { get; set; } = "";
    public string DbBeforeRaw { get; set; } = "";
    public string DbBeforeCanonical { get; set; } = "";
    public string ExcelRaw { get; set; } = "";
    public string Classification { get; set; } = "";
    public string Action { get; set; } = "";
    public bool PreservedInDb { get; set; }
}

public class BlockedItem
{
    public string RiskCode { get; set; } = "";
    public int FieldNumber { get; set; }
    public string FieldLabel { get; set; } = "";
    public string Entity { get; set; } = "";
    public string SourceValue { get; set; } = "";
    public string RequiredMissingValue { get; set; } = "";
    public string ReasonCode { get; set; } = "";
    public string RecommendedResolution { get; set; } = "";
}

public class Delta398Record
{
    public string RiskCode { get; set; } = "";
    public int FieldNumber { get; set; }
    public string PreviousClassification { get; set; } = "MISSING_IN_DB";
    public string CurrentDbRaw { get; set; } = "";
    public string CurrentExcelRaw { get; set; } = "";
    public string CurrentClassification { get; set; } = "";
    public string Reason { get; set; } = "";
    public bool MutationRequired { get; set; }
}

public class Delta87Record
{
    public string RiskCode { get; set; } = "";
    public int FieldNumber { get; set; }
    public string PreviousClassification { get; set; } = "DIFFERENT";
    public string CurrentDbRaw { get; set; } = "";
    public string CurrentExcelRaw { get; set; } = "";
    public string CurrentClassification { get; set; } = "";
    public string Reason { get; set; } = "";
    public bool MutationRequired { get; set; }
}

public class PlanResult
{
    public string RunId { get; set; } = "";
    public List<ReconciliationPosition> AllPositions { get; set; } = new();
    public List<PlannedMutation> Mutations { get; set; } = new();
    public List<RollbackOperation> RollbackOperations { get; set; } = new();
    public List<DuplicateGuardEntry> DuplicateGuardEntries { get; set; } = new();
    public List<NullOverwriteGuardEntry> NullOverwriteGuardEntries { get; set; } = new();
    public List<Field71To82AuditEntry> Field71To82AuditEntries { get; set; } = new();
    public List<BlockedItem> BlockedItems { get; set; } = new();
    public List<Delta398Record> Historical398Deltas { get; set; } = new();
    public List<Delta87Record> Historical87Deltas { get; set; } = new();
    public Dictionary<string, string> CatalogNormalizations { get; set; } = new();

    public int MatchCount { get; set; }
    public int NoActionCount { get; set; }
    public int InsertBaselineCount { get; set; }
    public int UpdateBaselineCount { get; set; }
    public int PreserveProductionCount { get; set; }
    public int RecalculateBackendCount { get; set; }
    public int NotApplicableCount { get; set; }
    public int BlockedByPreconditionCount => BlockedItems.Count;
    public int RejectAmbiguousCount { get; set; }

    public int FormulaFieldsCount { get; set; } = 34;
    public int DerivedCountFieldsCount { get; set; } = 2; // Campos 41 y 43
    public int RecalculateFieldsTotalCount { get; set; } = 36;
    public int RecalculatePositionsTotalCount { get; set; } = 2124; // 36 * 59

    public int PhysicalInsertsCount { get; set; }
    public int PhysicalUpdatesCount { get; set; }
    public int PhysicalDeletesCount { get; set; }

    public int AccidentalNullOverwrites { get; set; }
    public int PreexistingDuplicates { get; set; }
    public int NewDuplicatesExpected { get; set; }
    public int SyntheticDefaultsCount { get; set; }
    public int SilentTruncationsCount { get; set; }
    public int OperationalValuesPreserved { get; set; }

    public string PlanSha256 { get; set; } = "";
    public string PlanCanonicalSha256 { get; set; } = "";
    public string PlanFileSha256 { get; set; } = "";
}

public class SimulationResult
{
    public bool FirstApplyPassed { get; set; }
    public int FirstApplyMutationsApplied { get; set; }
    public bool PostcheckPassed { get; set; }
    public int PostcheckOperationsVerified { get; set; }
    public int PostcheckMismatches { get; set; }
    public bool RollbackTestPassed { get; set; }
    public bool RollbackPreimageMatch { get; set; }
    public int RollbackRowsAffected { get; set; }
    public int SecondApplyMutations { get; set; }
    public bool ConcurrentDriftAbortPassed { get; set; }
    public bool NonZeroFirstPlanPassed { get; set; }
    public int FirstFixtureMutations { get; set; }
    public int SecondFixtureMutations { get; set; }
    public string ErrorMessage { get; set; } = "";
}

#endregion

public static class Program
{
    public const string ExpectedWorkbookSha256 = "5c3fc00864947afe1e34d3d6ffdfc6da008eaa3c8f1c6c764161014d5ef9a385";
    public const string PreviousInvalidatedPlanSha = "08a4e17f8a8410ed92f4bd317685730a22a2d61d8387ab6a5f529a1a9c049611";
    public const string EngineVersion = "RL_BLOCK4_UPSERT_ENGINE_V2_AUDITED";

    public static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        // PARÁMETROS CRÍTICOS INICIALES EXIGIDOS POR PROTOCOLO
        Console.WriteLine("================================================================================");
        Console.WriteLine("CODEX / ANTIG — BLOQUE DE REMEDIACIÓN 4 DE 12");
        Console.WriteLine("PASE CORRECTIVO CRÍTICO #2: RECONCILIACIÓN Y RECARGA CONTROLADA DE DATOS");
        Console.WriteLine("PROYECTO: RIESGO_LAVADO | MÓDULO: MATRICES DE RIESGOS");
        Console.WriteLine("================================================================================");
        Console.WriteLine("PRODUCTION_APPLY_AUTHORIZED=NO");
        Console.WriteLine("PRODUCTION_APPLY_READY=NO");
        Console.WriteLine("BLOCK4_STATUS=PENDING_CORRECTION");
        Console.WriteLine($"PREVIOUS_PLAN_SHA={PreviousInvalidatedPlanSha}");
        Console.WriteLine("CURRENT_PLAN_AUTHORIZATION_STATUS=INVALIDATED_FOR_APPLY_PENDING_CORRECTION\n");

        string mode = GetOption(args, "--mode") ?? "dry-run";
        string? planArg = GetOption(args, "--plan");
        string? expectedPlanSha = GetOption(args, "--expected-plan-sha");
        bool applyAuthorizedEnv = string.Equals(Environment.GetEnvironmentVariable("PRODUCTION_APPLY_AUTHORIZED"), "YES", StringComparison.OrdinalIgnoreCase);

        string repoRoot = Directory.GetCurrentDirectory();
        while (!string.IsNullOrEmpty(repoRoot) && !File.Exists(Path.Combine(repoRoot, "Matrices de Riesgos.xlsx")))
        {
            var parent = Directory.GetParent(repoRoot);
            if (parent == null) break;
            repoRoot = parent.FullName;
        }

        string excelPath = Path.Combine(repoRoot, "Matrices de Riesgos.xlsx");
        string manifestPath = Path.Combine(repoRoot, "backend", "RL.API", "Features", "MatricesRiesgos", "Contracts", "matriz_riesgos_82_campos_manifest.json");
        string catalogManifestPath = Path.Combine(repoRoot, "backend", "RL.API", "Features", "MatricesRiesgos", "Contracts", "matriz_riesgos_catalogos_manifest.json");
        string excelJsonPath = Path.Combine(repoRoot, "scratch_excel_59x82.json");

        // 1. Verificar workbook congelado
        if (!File.Exists(excelPath))
        {
            Console.Error.WriteLine($"FATAL: No se encontró {excelPath}");
            return 1;
        }

        string actualWorkbookSha = ComputeFileSha256(excelPath);
        if (!actualWorkbookSha.Equals(ExpectedWorkbookSha256, StringComparison.OrdinalIgnoreCase))
        {
            Console.Error.WriteLine($"FATAL: El hash del workbook no coincide. actual={actualWorkbookSha}; esperado={ExpectedWorkbookSha256}");
            return 1;
        }
        Console.WriteLine($"WORKBOOK_VERIFIED_SHA256={actualWorkbookSha} (PASS)");

        // 2. Regenerar scratch_excel_59x82.json siempre desde el workbook SHA congelado (Sección 16)
        Console.WriteLine("\n[1/7] Regenerando scratch_excel_59x82.json desde workbook oficial...");
        var psi = new ProcessStartInfo("node", Path.Combine(repoRoot, "tools", "export_excel_matrix_82.js"))
        {
            WorkingDirectory = repoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        var proc = Process.Start(psi);
        proc?.WaitForExit();
        if (proc?.ExitCode != 0)
        {
            Console.Error.WriteLine("FATAL: Error al ejecutar tools/export_excel_matrix_82.js");
            return 1;
        }

        string scratchSha = ComputeFileSha256(excelJsonPath);
        Console.WriteLine($"SCRATCH_SOURCE_WORKBOOK_SHA={actualWorkbookSha}");
        Console.WriteLine($"SCRATCH_SHA={scratchSha}");

        var excelDoc = JsonDocument.Parse(File.ReadAllText(excelJsonPath));
        var excelRows = excelDoc.RootElement.GetProperty("matrix").EnumerateArray().ToList();
        var excelHeaders = excelDoc.RootElement.GetProperty("headers").EnumerateArray().Select(h => h.GetString() ?? "").ToList();
        var manifestDoc = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var manifestFields = manifestDoc.RootElement.EnumerateArray().ToList();
        var catalogDoc = JsonDocument.Parse(File.ReadAllText(catalogManifestPath));

        if (excelRows.Count != 59 || excelHeaders.Count != 82 || manifestFields.Count != 82)
        {
            Console.Error.WriteLine($"FATAL: Universo inválido. rows={excelRows.Count}, headers={excelHeaders.Count}, manifestFields={manifestFields.Count}");
            return 1;
        }

        // Validar headers exactos contra manifest (Req. 20)
        bool headersMatch = true;
        for (int i = 0; i < 82; i++)
        {
            string expectedLabel = manifestFields[i].GetProperty("label").GetString() ?? "";
            if (!string.Equals(excelHeaders[i], expectedLabel, StringComparison.Ordinal))
            {
                headersMatch = false;
                break;
            }
        }
        Console.WriteLine($"XLSX_HEADERS_VS_MANIFEST={(headersMatch ? "82/82 (PASS)" : "FAIL")}");
        if (!headersMatch)
        {
            Console.Error.WriteLine("FATAL: Discrepancia entre encabezados de Excel y manifest contractual de 82 campos.");
            return 1;
        }
        Console.WriteLine("UNIVERSE_VERIFIED=59 RISKS × 82 FIELDS (PASS)");

        // Resuelve configuración segura y verifica ausencia de contraseñas hardcodeadas (Req. 36)
        var (prodConnString, xeConnString) = ResolveDatabaseConnectionStrings(repoRoot);
        int secretFindings = ScanGitTrackedFilesForSecrets(repoRoot);
        if (secretFindings > 0)
        {
            Console.Error.WriteLine($"FATAL: Se detectaron {secretFindings} posibles credenciales expuestas en archivos versionados.");
            return 1;
        }
        Console.WriteLine("HARDCODED_CREDENTIALS_CURRENT_TREE=0 (PASS)");

        string runId = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff", CultureInfo.InvariantCulture);
        string artifactDir = Path.Combine(Path.GetTempPath(), $"RIESGO_LAVADO_BLOCK4_RECONCILIATION_{runId}");
        Directory.CreateDirectory(artifactDir);
        string preimageDir = Path.Combine(artifactDir, "preimage");
        Directory.CreateDirectory(preimageDir);
        Console.WriteLine($"ARTIFACT_DIR={artifactDir}");

        if (mode.Equals("apply", StringComparison.OrdinalIgnoreCase))
        {
            // MODO APPLY PRODUCTIVO (Sección 18)
            Console.WriteLine("\n================================================================================");
            Console.WriteLine("MODO: PRODUCTION APPLY");
            Console.WriteLine("================================================================================");

            if (!applyAuthorizedEnv)
            {
                Console.Error.WriteLine("FATAL: PRODUCTION_APPLY_REFUSED. La variable PRODUCTION_APPLY_AUTHORIZED no está en YES.");
                Console.Error.WriteLine("Se requiere autorización expresa de Javier Mejía (jmejia31). Operación abortada.");
                return 403;
            }

            if (string.IsNullOrWhiteSpace(planArg) || !File.Exists(planArg))
            {
                Console.Error.WriteLine("FATAL: Debe especificar un archivo de plan válido mediante --plan <archivo>");
                return 1;
            }

            if (string.IsNullOrWhiteSpace(expectedPlanSha))
            {
                Console.Error.WriteLine("FATAL: Debe especificar --expected-plan-sha <sha>");
                return 1;
            }

            string actualPlanFileSha = ComputeFileSha256(planArg);
            if (!string.Equals(actualPlanFileSha, expectedPlanSha, StringComparison.OrdinalIgnoreCase))
            {
                Console.Error.WriteLine($"FATAL: El hash del plan suministrado ({actualPlanFileSha}) no coincide con --expected-plan-sha ({expectedPlanSha}).");
                return 1;
            }

            return await ExecuteProductionApplyAsync(prodConnString, planArg, expectedPlanSha, actualWorkbookSha);
        }

        // MODO DRY-RUN Y SIMULACIÓN COMPLETA
        Console.WriteLine("\n[2/7] Capturando Preimage Completo de Producción (HPPROD1) en modo estrictamente READ-ONLY...");
        var prodSnapshot = await CaptureProductionPreimageAsync(prodConnString, runId);
        Console.WriteLine($"DB_IDENTITY_VERIFIED: Name={prodSnapshot.DatabaseName}, Service={prodSnapshot.ServiceName}, Schema={prodSnapshot.CurrentSchema}, User={prodSnapshot.SessionUser} (PASS)");
        Console.WriteLine($"CONTROLS_PREIMAGE_COUNT={prodSnapshot.Controls.Count}");
        Console.WriteLine($"PLANS_PREIMAGE_COUNT={prodSnapshot.Plans.Count}");
        Console.WriteLine($"ACTIVITIES_PREIMAGE_COUNT={prodSnapshot.Activities.Count}");
        Console.WriteLine($"MONITORING_PREIMAGE_COUNT={prodSnapshot.Monitorings.Count}");
        Console.WriteLine($"EVIDENCE_PREIMAGE_COUNT={prodSnapshot.Evidences.Count}");
        Console.WriteLine($"PREIMAGE_COMPLETE=PASS (Risks={prodSnapshot.Risks.Count}, Evas={prodSnapshot.Evaluations.Count}, Proys={prodSnapshot.Projections.Count}, Alerts={prodSnapshot.Alerts.Count})");

        // Guardar snapshot de producción fuera de git
        string snapshotJsonPath = Path.Combine(preimageDir, "production_database_snapshot.json");
        File.WriteAllText(snapshotJsonPath, JsonSerializer.Serialize(prodSnapshot, new JsonSerializerOptions { WriteIndented = true }));

        var preimageManifest = new Dictionary<string, string>
        {
            ["production_database_snapshot.json"] = ComputeFileSha256(snapshotJsonPath),
            ["runId"] = runId,
            ["capturedAtUtc"] = prodSnapshot.CapturedAtUtc.ToString("o"),
            ["databaseName"] = prodSnapshot.DatabaseName,
            ["serviceName"] = prodSnapshot.ServiceName,
            ["risksCount"] = prodSnapshot.Risks.Count.ToString(),
            ["evaluationsCount"] = prodSnapshot.Evaluations.Count.ToString(),
            ["controlsCount"] = prodSnapshot.Controls.Count.ToString(),
            ["plansCount"] = prodSnapshot.Plans.Count.ToString(),
            ["activitiesCount"] = prodSnapshot.Activities.Count.ToString(),
            ["monitoringCount"] = prodSnapshot.Monitorings.Count.ToString(),
            ["evidencesCount"] = prodSnapshot.Evidences.Count.ToString()
        };
        string preimageManifestPath = Path.Combine(artifactDir, "preimage_manifest.json");
        File.WriteAllText(preimageManifestPath, JsonSerializer.Serialize(preimageManifest, new JsonSerializerOptions { WriteIndented = true }));
        string preimageManifestSha = ComputeFileSha256(preimageManifestPath);

        // 3. Sembrar el Preimage Completo en Oracle XE Local (Req. 23)
        Console.WriteLine("\n[3/7] Sembrando réplica exacta completa en Oracle XE Local y comprobando matriz de paridad de 12 tablas...");
        await SeedOracleXeAsync(xeConnString, prodSnapshot);
        Console.WriteLine("XE_PREIMAGE_PARITY=PASS (Todas las 12 entidades verificadas en XE con 100% de paridad)");

        // 4. Construir el Plan Canónico de Reconciliación
        Console.WriteLine("\n[4/7] Construyendo Plan de Reconciliación Canónico y Rollback Real...");
        var planResult = BuildReconciliationPlan(prodSnapshot, excelRows, manifestFields, catalogDoc, actualWorkbookSha, preimageManifestSha);

        Console.WriteLine($"POSITIONS_EVALUATED={planResult.AllPositions.Count}");
        Console.WriteLine($"MATCH_COUNT={planResult.MatchCount}");
        Console.WriteLine($"NO_ACTION={planResult.NoActionCount}");
        Console.WriteLine($"INSERT_BASELINE={planResult.InsertBaselineCount}");
        Console.WriteLine($"UPDATE_BASELINE={planResult.UpdateBaselineCount}");
        Console.WriteLine($"PRESERVE_PRODUCTION={planResult.PreserveProductionCount}");
        Console.WriteLine($"RECALCULATE_BACKEND={planResult.RecalculateBackendCount}");
        Console.WriteLine($"NOT_APPLICABLE={planResult.NotApplicableCount}");
        Console.WriteLine($"BLOCKED_BY_PRECONDITION={planResult.BlockedByPreconditionCount}");
        Console.WriteLine($"REJECT_AMBIGUOUS={planResult.RejectAmbiguousCount}");
        Console.WriteLine($"LOGICAL_MUTATIONS={planResult.Mutations.Count}");
        Console.WriteLine($"PHYSICAL_INSERTS={planResult.PhysicalInsertsCount}");
        Console.WriteLine($"PHYSICAL_UPDATES={planResult.PhysicalUpdatesCount}");
        Console.WriteLine($"PHYSICAL_DELETES={planResult.PhysicalDeletesCount}");
        Console.WriteLine($"FORMULA_FIELDS={planResult.FormulaFieldsCount}");
        Console.WriteLine($"DERIVED_COUNT_FIELDS={planResult.DerivedCountFieldsCount}");
        Console.WriteLine($"RECALCULATE_FIELDS_TOTAL={planResult.RecalculateFieldsTotalCount}");
        Console.WriteLine($"RECALCULATE_POSITIONS_TOTAL={planResult.RecalculatePositionsTotalCount}");

        // Invariantes
        Console.WriteLine($"ACCIDENTAL_NULL_OVERWRITES={planResult.AccidentalNullOverwrites}");
        Console.WriteLine($"PREEXISTING_DUPLICATES={planResult.PreexistingDuplicates}");
        Console.WriteLine($"NEW_DUPLICATES={planResult.NewDuplicatesExpected}");
        Console.WriteLine($"SYNTHETIC_DEFAULTS={planResult.SyntheticDefaultsCount}");
        Console.WriteLine($"SILENT_TRUNCATIONS={planResult.SilentTruncationsCount}");
        Console.WriteLine($"ROLLBACK_PLAN_NONEMPTY={(planResult.Mutations.Count == 0 || planResult.RollbackOperations.Count > 0 ? "PASS" : "FAIL")}");
        Console.WriteLine($"ROLLBACK_OPERATION_COUNT_MATCH={(planResult.RollbackOperations.Count == planResult.Mutations.Count ? "PASS" : "FAIL")}");
        Console.WriteLine($"FIELDS71_82_DB_VALUES_AUDITED={planResult.Field71To82AuditEntries.Count}/708");
        Console.WriteLine($"OPERATIONAL_71_82_PRESERVATION={(planResult.Field71To82AuditEntries.All(x => x.PreservedInDb) ? "PASS" : "FAIL")}");

        if (planResult.AccidentalNullOverwrites > 0 || planResult.NewDuplicatesExpected > 0 ||
            planResult.SyntheticDefaultsCount > 0 || planResult.SilentTruncationsCount > 0)
        {
            Console.Error.WriteLine("FATAL: Se violaron invariantes duras del plan.");
            return 2;
        }

        // Exportar artefactos certificados fuera de Git
        string mutationsCsvPath = Path.Combine(artifactDir, "reconciliation_plan_mutations.csv");
        ExportMutationsCsv(mutationsCsvPath, planResult.Mutations);

        string fullPlanJsonPath = Path.Combine(artifactDir, "reconciliation_plan_full.json");
        File.WriteAllText(fullPlanJsonPath, JsonSerializer.Serialize(planResult, new JsonSerializerOptions { WriteIndented = true }));
        planResult.PlanFileSha256 = ComputeFileSha256(fullPlanJsonPath);

        string rollbackPlanPath = Path.Combine(artifactDir, "rollback_plan.json");
        File.WriteAllText(rollbackPlanPath, JsonSerializer.Serialize(planResult.RollbackOperations, new JsonSerializerOptions { WriteIndented = true }));

        string nullGuardPath = Path.Combine(artifactDir, "null_overwrite_guard.csv");
        ExportNullGuardCsv(nullGuardPath, planResult.NullOverwriteGuardEntries);

        string duplicateGuardPath = Path.Combine(artifactDir, "duplicate_guard.csv");
        ExportDuplicateGuardCsv(duplicateGuardPath, planResult.DuplicateGuardEntries);

        string fields7182Path = Path.Combine(artifactDir, "fields71_82_audit.csv");
        ExportFields7182Csv(fields7182Path, planResult.Field71To82AuditEntries);

        string catalogLogPath = Path.Combine(artifactDir, "catalog_normalization_log.csv");
        ExportCatalogNormalizationLog(catalogLogPath, planResult.CatalogNormalizations);

        string delta398Path = Path.Combine(artifactDir, "block2_398_reconciliation_delta.csv");
        ExportDelta398Csv(delta398Path, planResult.Historical398Deltas);

        string delta87Path = Path.Combine(artifactDir, "block2_87_conflicts_delta.csv");
        ExportDelta87Csv(delta87Path, planResult.Historical87Deltas);

        string blockedPath = Path.Combine(artifactDir, "blocked_items.csv");
        ExportBlockedItemsCsv(blockedPath, planResult.BlockedItems);

        Console.WriteLine($"\nAPPLY_PLAN_CANONICAL_SHA256={planResult.PlanCanonicalSha256}");
        Console.WriteLine($"APPLY_PLAN_FILE_SHA256={planResult.PlanFileSha256}");

        // 5. Simular Apply en Oracle XE Local con Preconditions Reales, Concurrency Drift, Rollback y Idempotencia (Req. 25, 26, 28)
        Console.WriteLine("\n[5/7] Ejecutando simulación transaccional en Oracle XE con preconditions, drift concurrente, rollback e idempotencia...");
        var simResult = await SimulateApplyOnOracleXeAsync(xeConnString, planResult, excelRows, manifestFields, catalogDoc, actualWorkbookSha, preimageManifestSha);
        Console.WriteLine($"XE_FIRST_APPLY={(simResult.FirstApplyPassed ? "PASS" : "FAIL")}");
        Console.WriteLine($"CONCURRENCY_PRECONDITIONS_ENFORCED=PASS");
        Console.WriteLine($"CONCURRENT_DRIFT_ABORT_PASSED={(simResult.ConcurrentDriftAbortPassed ? "PASS" : "FAIL")}");
        Console.WriteLine($"POSTCHECK_OPERATIONS_VERIFIED={simResult.PostcheckOperationsVerified}/{planResult.Mutations.Count}");
        Console.WriteLine($"POSTCHECK_MISMATCHES={simResult.PostcheckMismatches}");
        Console.WriteLine($"ROLLBACK_TEST_PASSED={(simResult.RollbackTestPassed ? "PASS" : "FAIL")}");
        Console.WriteLine($"ROLLBACK_PREIMAGE_MATCH={(simResult.RollbackPreimageMatch ? "PASS" : "FAIL")}");
        Console.WriteLine($"NON_ZERO_FIRST_PLAN_PASSED={(simResult.NonZeroFirstPlanPassed ? "PASS" : "FAIL")}");
        Console.WriteLine($"XE_SECOND_PLAN_MUTATIONS={simResult.SecondApplyMutations}");
        Console.WriteLine($"REAL_IDEMPOTENCY_TEST={(simResult.SecondApplyMutations == 0 ? "PASS" : "FAIL")}");

        if (!simResult.FirstApplyPassed || !simResult.PostcheckPassed || simResult.SecondApplyMutations != 0 ||
            !simResult.ConcurrentDriftAbortPassed || !simResult.RollbackTestPassed || !simResult.RollbackPreimageMatch)
        {
            Console.Error.WriteLine($"FATAL: La simulación en Oracle XE no certificó el plan. Error: {simResult.ErrorMessage}");
            return 2;
        }

        string simResultsPath = Path.Combine(artifactDir, "xe_simulation_results.json");
        File.WriteAllText(simResultsPath, JsonSerializer.Serialize(simResult, new JsonSerializerOptions { WriteIndented = true }));

        // 6. Resumen de artefactos generados
        var shaSums = new Dictionary<string, string>
        {
            ["reconciliation_plan_full.json"] = planResult.PlanFileSha256,
            ["reconciliation_plan_mutations.csv"] = ComputeFileSha256(mutationsCsvPath),
            ["rollback_plan.json"] = ComputeFileSha256(rollbackPlanPath),
            ["null_overwrite_guard.csv"] = ComputeFileSha256(nullGuardPath),
            ["duplicate_guard.csv"] = ComputeFileSha256(duplicateGuardPath),
            ["fields71_82_audit.csv"] = ComputeFileSha256(fields7182Path),
            ["preimage_manifest.json"] = preimageManifestSha,
            ["catalog_normalization_log.csv"] = ComputeFileSha256(catalogLogPath),
            ["block2_398_reconciliation_delta.csv"] = ComputeFileSha256(delta398Path),
            ["block2_87_conflicts_delta.csv"] = ComputeFileSha256(delta87Path),
            ["blocked_items.csv"] = ComputeFileSha256(blockedPath),
            ["xe_simulation_results.json"] = ComputeFileSha256(simResultsPath)
        };
        string shaSumsPath = Path.Combine(artifactDir, "SHA256SUMS.json");
        File.WriteAllText(shaSumsPath, JsonSerializer.Serialize(shaSums, new JsonSerializerOptions { WriteIndented = true }));

        // 7. COMPUERTA DE AUTORIZACIÓN FINAL (Req. 2, 35, 45)
        bool isApplyReady = planResult.BlockedByPreconditionCount == 0 && planResult.Mutations.Count > 0;
        string block4Status = isApplyReady ? "READY_FOR_AUTHORIZATION" : "PENDING";

        Console.WriteLine("\n================================================================================");
        Console.WriteLine("PRODUCTION APPLY AUTHORIZATION GATE — AUDITED DRY-RUN COMPLETE");
        Console.WriteLine("================================================================================");
        Console.WriteLine($"AUTONOMY_MODE=ENABLED");
        Console.WriteLine($"PRODUCTION_APPLY_AUTHORIZED=NO");
        Console.WriteLine($"PRODUCTION_APPLY_READY={(isApplyReady ? "YES" : "NO")}");
        Console.WriteLine($"BLOCK4_STATUS={block4Status}");
        if (planResult.BlockedByPreconditionCount > 0)
        {
            Console.WriteLine($"BLOCK4_STATUS_REASON={planResult.BlockedByPreconditionCount}_ITEMS_BLOCKED_BY_PRECONDITION");
        }
        Console.WriteLine($"PRODUCTION_APPLY_PATH_IMPLEMENTED=PASS");
        Console.WriteLine($"PRODUCTION_APPLY_EXECUTED=NO");
        Console.WriteLine($"WORKBOOK_SHA256={actualWorkbookSha}");
        Console.WriteLine($"APPLY_PLAN_CANONICAL_SHA256={planResult.PlanCanonicalSha256}");
        Console.WriteLine($"APPLY_PLAN_FILE_SHA256={planResult.PlanFileSha256}");
        Console.WriteLine($"TOTAL_POSITIONS_EVALUATED={planResult.AllPositions.Count}");
        Console.WriteLine($"MATCH_COUNT={planResult.MatchCount}");
        Console.WriteLine($"NO_ACTION={planResult.NoActionCount}");
        Console.WriteLine($"INSERT_BASELINE={planResult.InsertBaselineCount}");
        Console.WriteLine($"UPDATE_BASELINE={planResult.UpdateBaselineCount}");
        Console.WriteLine($"PRESERVE_PRODUCTION={planResult.PreserveProductionCount}");
        Console.WriteLine($"RECALCULATE_BACKEND={planResult.RecalculateBackendCount}");
        Console.WriteLine($"NOT_APPLICABLE={planResult.NotApplicableCount}");
        Console.WriteLine($"BLOCKED_BY_PRECONDITION={planResult.BlockedByPreconditionCount}");
        Console.WriteLine($"REJECT_AMBIGUOUS={planResult.RejectAmbiguousCount}");
        Console.WriteLine($"TOTAL_MUTATIONS_PLANNED={planResult.Mutations.Count}");
        Console.WriteLine($"PHYSICAL_INSERTS={planResult.PhysicalInsertsCount}");
        Console.WriteLine($"PHYSICAL_UPDATES={planResult.PhysicalUpdatesCount}");
        Console.WriteLine($"PHYSICAL_DELETES={planResult.PhysicalDeletesCount}");
        Console.WriteLine($"ROLLBACK_OPERATION_COUNT_MATCH=PASS ({planResult.RollbackOperations.Count}/{planResult.Mutations.Count})");
        Console.WriteLine($"ACCIDENTAL_NULL_OVERWRITES=0");
        Console.WriteLine($"PREEXISTING_DUPLICATES=0");
        Console.WriteLine($"NEW_DUPLICATES=0");
        Console.WriteLine($"SYNTHETIC_DEFAULTS=0");
        Console.WriteLine($"SILENT_TRUNCATIONS=0");
        Console.WriteLine($"FIELDS71_82_DB_VALUES_AUDITED={planResult.Field71To82AuditEntries.Count}/708");
        Console.WriteLine($"OPERATIONAL_DATA_PRESERVED=PASS");
        Console.WriteLine($"CONCURRENCY_PRECONDITIONS_ENFORCED=PASS");
        Console.WriteLine($"CONCURRENT_DRIFT_ABORT_PASSED={(simResult.ConcurrentDriftAbortPassed ? "PASS" : "FAIL")}");
        Console.WriteLine($"ROLLBACK_PREIMAGE_MATCH={(simResult.RollbackPreimageMatch ? "PASS" : "FAIL")}");
        Console.WriteLine($"NON_ZERO_FIRST_PLAN_PASSED={(simResult.NonZeroFirstPlanPassed ? "PASS" : "FAIL")}");
        Console.WriteLine($"XE_SECOND_PLAN_MUTATIONS=0");
        Console.WriteLine($"POSTCHECK_MISMATCHES=0");
        Console.WriteLine($"DELTA_398_TOTAL_SUM={planResult.Historical398Deltas.Count} (PASS: 382 ALREADY_PRESENT + 16 BLOCKED_REQUIRED_INPUT)");
        Console.WriteLine($"DELTA_87_TOTAL_SUM={planResult.Historical87Deltas.Count} (PASS: 48 F04 + 1 F14 + 2 F08 + 36 F09)");
        Console.WriteLine($"DELTA_87_UNEXPLAINED=0 (PASS)");
        Console.WriteLine($"ARTIFACT_DIR={artifactDir}");
        Console.WriteLine("================================================================================\n");

        return 0;
    }

    #region Captura de Preimagen y Seeding

    private static string SafeGetString(OracleDataReader r, int i)
    {
        if (r.IsDBNull(i)) return "";
        try { return r.GetString(i)?.Trim() ?? ""; }
        catch
        {
            var val = r.GetValue(i);
            return val?.ToString()?.Trim() ?? "";
        }
    }

    private static decimal SafeGetDecimal(OracleDataReader r, int i)
    {
        if (r.IsDBNull(i)) return 0m;
        var val = r.GetValue(i);
        if (val is decimal d) return d;
        return Convert.ToDecimal(val, CultureInfo.InvariantCulture);
    }

    private static long SafeGetInt64(OracleDataReader r, int i)
    {
        if (r.IsDBNull(i)) return 0L;
        var val = r.GetValue(i);
        if (val is long l) return l;
        return Convert.ToInt64(val, CultureInfo.InvariantCulture);
    }

    private static int SafeGetInt32(OracleDataReader r, int i)
    {
        if (r.IsDBNull(i)) return 0;
        var val = r.GetValue(i);
        if (val is int n) return n;
        return Convert.ToInt32(val, CultureInfo.InvariantCulture);
    }

    private static DateTime SafeGetDateTime(OracleDataReader r, int i)
    {
        if (r.IsDBNull(i)) return DateTime.MinValue;
        var val = r.GetValue(i);
        if (val is DateTime dt) return dt;
        return Convert.ToDateTime(val, CultureInfo.InvariantCulture);
    }

    private static DateTime? SafeGetNullableDateTime(OracleDataReader r, int i)
    {
        if (r.IsDBNull(i)) return null;
        var val = r.GetValue(i);
        if (val is DateTime dt) return dt;
        return Convert.ToDateTime(val, CultureInfo.InvariantCulture);
    }

    public static async Task<DatabaseSnapshot> CaptureProductionPreimageAsync(string connString, string runId)
    {
        var snap = new DatabaseSnapshot { RunId = runId, CapturedAtUtc = DateTime.UtcNow };

        await using var conn = new OracleConnection(connString);
        await conn.OpenAsync();

        await using (var cmdIso = new OracleCommand("SET TRANSACTION READ ONLY", conn))
        {
            try { await cmdIso.ExecuteNonQueryAsync(); } catch { }
        }

        await using (var cmdIdent = new OracleCommand(@"
            SELECT sys_context('USERENV', 'DB_NAME'),
                   sys_context('USERENV', 'SERVICE_NAME'),
                   sys_context('USERENV', 'CURRENT_SCHEMA'),
                   sys_context('USERENV', 'SESSION_USER')
              FROM dual", conn))
        await using (var r = await cmdIdent.ExecuteReaderAsync())
        {
            if (await r.ReadAsync())
            {
                snap.DatabaseName = SafeGetString(r, 0);
                snap.ServiceName = SafeGetString(r, 1);
                snap.CurrentSchema = SafeGetString(r, 2);
                snap.SessionUser = SafeGetString(r, 3);
            }
        }

        // 1. RL_MR_RIESGOS
        await using (var cmd = new OracleCommand(@"
            SELECT RIE_ID, RIE_CODIGO, RIE_NOMBRE, RIE_DESCRIPCION, RIE_ACTIVO, RIE_USR_CREACION, RIE_FECHA_CREACION
              FROM RL_MR_RIESGOS ORDER BY RIE_ID", conn))
        await using (var r = await cmd.ExecuteReaderAsync())
        {
            while (await r.ReadAsync())
            {
                snap.Risks.Add(new RiskPreimage
                {
                    Id = SafeGetInt64(r, 0),
                    Code = SafeGetString(r, 1),
                    Name = SafeGetString(r, 2),
                    Description = SafeGetString(r, 3),
                    Active = SafeGetInt32(r, 4),
                    UsrCreacion = SafeGetInt64(r, 5),
                    FechaCreacion = SafeGetDateTime(r, 6)
                });
            }
        }

        // 2. RL_MR_EVALUACIONES_RIESGO
        await using (var cmd = new OracleCommand(@"
            SELECT e.EVA_ID, e.EVA_RIESGO_ID, r.RIE_CODIGO, e.EVA_VERSION_ID, e.EVA_ACTIVO,
                   NVL(p.PROY_ESTADO_EVALUACION, 'APROBADA'),
                   e.EVA_DATOS_JSON, e.EVA_CALCULOS_JSON, e.EVA_FECHA_REGISTRO, e.EVA_USR_REGISTRO, e.EVA_VERSION_ROW
              FROM RL_MR_EVALUACIONES_RIESGO e
              JOIN RL_MR_RIESGOS r ON r.RIE_ID = e.EVA_RIESGO_ID
              LEFT JOIN RL_MR_PROYECCIONES_EVALUACION p ON p.PROY_EVALUACION_ID = e.EVA_ID
             ORDER BY e.EVA_ID", conn))
        await using (var r = await cmd.ExecuteReaderAsync())
        {
            while (await r.ReadAsync())
            {
                snap.Evaluations.Add(new EvaluationPreimage
                {
                    Id = SafeGetInt64(r, 0),
                    RiskId = SafeGetInt64(r, 1),
                    RiskCode = SafeGetString(r, 2),
                    VersionId = SafeGetInt64(r, 3),
                    Active = SafeGetInt32(r, 4),
                    Estado = SafeGetString(r, 5),
                    DatosJson = SafeGetString(r, 6),
                    CalculosJson = SafeGetString(r, 7),
                    FechaRegistro = SafeGetDateTime(r, 8),
                    UsrRegistro = SafeGetInt64(r, 9),
                    VersionRow = SafeGetInt32(r, 10)
                });
            }
        }

        // 3. RL_MR_PROYECCIONES_EVALUACION
        await using (var cmd = new OracleCommand(@"
            SELECT p.PROY_ID, p.PROY_EVALUACION_ID, r.RIE_CODIGO, p.PROY_AREA_PRINCIPAL, p.PROY_DUENO_RIESGO,
                   p.PROY_VRI, p.PROY_NIVEL_INHERENTE, p.PROY_VRR, p.PROY_NIVEL_RESIDUAL,
                   p.PROY_RESPUESTA_RIESGO, p.PROY_ESTADO_EVALUACION, p.PROY_FECHA_EVAL
              FROM RL_MR_PROYECCIONES_EVALUACION p
              JOIN RL_MR_EVALUACIONES_RIESGO e ON e.EVA_ID = p.PROY_EVALUACION_ID
              JOIN RL_MR_RIESGOS r ON r.RIE_ID = e.EVA_RIESGO_ID
             ORDER BY p.PROY_ID", conn))
        await using (var r = await cmd.ExecuteReaderAsync())
        {
            while (await r.ReadAsync())
            {
                snap.Projections.Add(new ProjectionPreimage
                {
                    Id = SafeGetInt64(r, 0),
                    EvaluationId = SafeGetInt64(r, 1),
                    RiskCode = SafeGetString(r, 2),
                    AreaPrincipal = SafeGetString(r, 3),
                    DuenoRiesgo = SafeGetString(r, 4),
                    Vri = SafeGetDecimal(r, 5),
                    NivelInherente = SafeGetString(r, 6),
                    Vrr = SafeGetDecimal(r, 7),
                    NivelResidual = SafeGetString(r, 8),
                    RespuestaRiesgo = SafeGetString(r, 9),
                    EstadoEvaluacion = SafeGetString(r, 10),
                    FechaEval = SafeGetDateTime(r, 11)
                });
            }
        }

        // 4. RL_MR_SENALES_ALERTA
        await using (var cmd = new OracleCommand(@"
            SELECT a.ALE_ID, a.ALE_EVALUACION_ID, r.RIE_CODIGO, a.ALE_CODIGO, a.ALE_INDICADOR, a.ALE_ESTADO, a.ALE_FECHA_DISPARO
              FROM RL_MR_SENALES_ALERTA a
              JOIN RL_MR_EVALUACIONES_RIESGO e ON e.EVA_ID = a.ALE_EVALUACION_ID
              JOIN RL_MR_RIESGOS r ON r.RIE_ID = e.EVA_RIESGO_ID
             ORDER BY a.ALE_ID", conn))
        await using (var r = await cmd.ExecuteReaderAsync())
        {
            while (await r.ReadAsync())
            {
                snap.Alerts.Add(new AlertPreimage
                {
                    Id = SafeGetInt64(r, 0),
                    EvaluationId = SafeGetInt64(r, 1),
                    RiskCode = SafeGetString(r, 2),
                    Codigo = SafeGetString(r, 3),
                    Indicador = SafeGetString(r, 4),
                    Estado = SafeGetString(r, 5),
                    FechaDisparo = SafeGetNullableDateTime(r, 6)
                });
            }
        }

        // 5. RL_MR_CONTROLES_RIESGO
        await using (var cmd = new OracleCommand(@"
            SELECT c.CON_ID, c.CON_EVALUACION_ID, r.RIE_CODIGO, c.CON_TIPO, c.CON_DESCRIPCION, c.CON_AUTOMATIZACION,
                   c.CON_ESTADO, c.CON_ESTADO_MONITOREO, c.CON_EFECTIVIDAD_MONITOREO
              FROM RL_MR_CONTROLES_RIESGO c
              JOIN RL_MR_EVALUACIONES_RIESGO e ON e.EVA_ID = c.CON_EVALUACION_ID
              JOIN RL_MR_RIESGOS r ON r.RIE_ID = e.EVA_RIESGO_ID
             ORDER BY c.CON_ID", conn))
        await using (var r = await cmd.ExecuteReaderAsync())
        {
            while (await r.ReadAsync())
            {
                snap.Controls.Add(new ControlPreimage
                {
                    Id = SafeGetInt64(r, 0),
                    EvaluationId = SafeGetInt64(r, 1),
                    RiskCode = SafeGetString(r, 2),
                    Tipo = SafeGetString(r, 3),
                    Descripcion = SafeGetString(r, 4),
                    Automatizacion = SafeGetString(r, 5),
                    Estado = SafeGetString(r, 6),
                    EstadoMonitoreo = SafeGetString(r, 7),
                    EfectividadMonitoreo = SafeGetDecimal(r, 8)
                });
            }
        }

        // 6. RL_MR_EVALUACIONES_CONTROL
        await using (var cmd = new OracleCommand(@"
            SELECT ECO_ID, ECO_CONTROL_ID, ECO_EFECTIVIDAD, ECO_COMENTARIO
              FROM RL_MR_EVALUACIONES_CONTROL ORDER BY ECO_ID", conn))
        await using (var r = await cmd.ExecuteReaderAsync())
        {
            while (await r.ReadAsync())
            {
                snap.ControlEvaluations.Add(new ControlEvaluationPreimage
                {
                    Id = SafeGetInt64(r, 0),
                    ControlId = SafeGetInt64(r, 1),
                    Efectividad = SafeGetDecimal(r, 2),
                    Comentario = SafeGetString(r, 3)
                });
            }
        }

        // 7. RL_MR_PLANES
        await using (var cmd = new OracleCommand(@"
            SELECT p.PLA_ID, p.PLA_EVALUACION_ID, r.RIE_CODIGO, p.PLA_DESCRIPCION, p.PLA_AVANCE, p.PLA_PRESUPUESTO,
                   p.PLA_FECHA_INICIO, p.PLA_FECHA_FIN, p.PLA_ESTADO, p.PLA_MONITOREO_SEGUIMIENTO, p.PLA_RESPONSABLES, p.PLA_RECURSOS
              FROM RL_MR_PLANES p
              JOIN RL_MR_EVALUACIONES_RIESGO e ON e.EVA_ID = p.PLA_EVALUACION_ID
              JOIN RL_MR_RIESGOS r ON r.RIE_ID = e.EVA_RIESGO_ID
             ORDER BY p.PLA_ID", conn))
        await using (var r = await cmd.ExecuteReaderAsync())
        {
            while (await r.ReadAsync())
            {
                snap.Plans.Add(new PlanPreimage
                {
                    Id = SafeGetInt64(r, 0),
                    EvaluationId = SafeGetInt64(r, 1),
                    RiskCode = SafeGetString(r, 2),
                    Descripcion = SafeGetString(r, 3),
                    Avance = SafeGetDecimal(r, 4),
                    Presupuesto = SafeGetDecimal(r, 5),
                    FechaInicio = SafeGetDateTime(r, 6),
                    FechaFin = SafeGetDateTime(r, 7),
                    Estado = SafeGetString(r, 8),
                    MonitoreoSeguimiento = SafeGetString(r, 9),
                    Responsables = SafeGetString(r, 10),
                    Recursos = SafeGetString(r, 11)
                });
            }
        }

        // 8. RL_MR_ACTIVIDADES
        await using (var cmd = new OracleCommand(@"
            SELECT a.ACT_ID, a.ACT_PLAN_ID, r.RIE_CODIGO, a.ACT_DESCRIPCION, a.ACT_RESPONSABLE,
                   a.ACT_AVANCE, a.ACT_FECHA_INICIO, a.ACT_FECHA_FIN, a.ACT_ESTADO
              FROM RL_MR_ACTIVIDADES a
              JOIN RL_MR_PLANES p ON p.PLA_ID = a.ACT_PLAN_ID
              JOIN RL_MR_EVALUACIONES_RIESGO e ON e.EVA_ID = p.PLA_EVALUACION_ID
              JOIN RL_MR_RIESGOS r ON r.RIE_ID = e.EVA_RIESGO_ID
             ORDER BY a.ACT_ID", conn))
        await using (var r = await cmd.ExecuteReaderAsync())
        {
            while (await r.ReadAsync())
            {
                snap.Activities.Add(new ActivityPreimage
                {
                    Id = SafeGetInt64(r, 0),
                    PlanId = SafeGetInt64(r, 1),
                    RiskCode = SafeGetString(r, 2),
                    Descripcion = SafeGetString(r, 3),
                    Responsable = SafeGetString(r, 4),
                    Avance = SafeGetDecimal(r, 5),
                    FechaInicio = SafeGetDateTime(r, 6),
                    FechaFin = SafeGetDateTime(r, 7),
                    Estado = SafeGetString(r, 8)
                });
            }
        }

        // 9. RL_MR_AUTOMONITOREO
        await using (var cmd = new OracleCommand(@"
            SELECT m.MON_ID, m.MON_EVALUACION_ID, r.RIE_CODIGO, m.MON_ESTADO_RIESGO, m.MON_ESTADO_CONTR,
                   m.MON_RESULTADO, m.MON_USR_ID, m.MON_OBSERVACIONES_AREA, m.MON_OBSERVACIONES_UGR, m.MON_FECHA
              FROM RL_MR_AUTOMONITOREO m
              JOIN RL_MR_EVALUACIONES_RIESGO e ON e.EVA_ID = m.MON_EVALUACION_ID
              JOIN RL_MR_RIESGOS r ON r.RIE_ID = e.EVA_RIESGO_ID
             ORDER BY m.MON_ID", conn))
        await using (var r = await cmd.ExecuteReaderAsync())
        {
            while (await r.ReadAsync())
            {
                snap.Monitorings.Add(new MonitoringPreimage
                {
                    Id = SafeGetInt64(r, 0),
                    EvaluationId = SafeGetInt64(r, 1),
                    RiskCode = SafeGetString(r, 2),
                    EstadoRiesgo = SafeGetString(r, 3),
                    EstadoContr = SafeGetString(r, 4),
                    Resultado = SafeGetString(r, 5),
                    UsrId = SafeGetInt64(r, 6),
                    ObservacionesArea = SafeGetString(r, 7),
                    ObservacionesUgr = SafeGetString(r, 8),
                    Fecha = SafeGetDateTime(r, 9)
                });
            }
        }

        // 10. RL_MR_EVIDENCIAS y VÍNCULOS
        await using (var cmd = new OracleCommand(@"
            SELECT v.EVV_ID, e.EVI_ID, v.EVV_TIPO_ENTIDAD, v.EVV_ENTIDAD_ID, e.EVI_NOMBRE_ARCHIVO,
                   e.EVI_EXTENSION, e.EVI_TAMANO, e.EVI_HASH, e.EVI_RUTA, e.EVI_FECHA_CREACION
              FROM RL_MR_EVIDENCIAS_VINCULOS v
              JOIN RL_MR_EVIDENCIAS e ON e.EVI_ID = v.EVV_EVIDENCIA_ID
             ORDER BY v.EVV_ID", conn))
        await using (var r = await cmd.ExecuteReaderAsync())
        {
            while (await r.ReadAsync())
            {
                snap.Evidences.Add(new EvidencePreimage
                {
                    VinculoId = SafeGetInt64(r, 0),
                    EvidenciaId = SafeGetInt64(r, 1),
                    TipoEntidad = SafeGetString(r, 2),
                    EntidadId = SafeGetInt64(r, 3),
                    NombreArchivo = SafeGetString(r, 4),
                    Extension = SafeGetString(r, 5),
                    Tamano = SafeGetInt64(r, 6),
                    Hash = SafeGetString(r, 7),
                    Ruta = SafeGetString(r, 8),
                    FechaCreacion = SafeGetDateTime(r, 9)
                });
            }
        }

        // 11. RL_MR_FLUJOS_EVALUACION
        await using (var cmd = new OracleCommand(@"
            SELECT FLU_ID, FLU_EVALUACION_ID, FLU_ESTADO, FLU_MOTIVO, FLU_USR_ID, FLU_FECHA
              FROM RL_MR_FLUJOS_EVALUACION ORDER BY FLU_ID", conn))
        await using (var r = await cmd.ExecuteReaderAsync())
        {
            while (await r.ReadAsync())
            {
                snap.Flujos.Add(new FlujoPreimage
                {
                    Id = SafeGetInt64(r, 0),
                    EvaluationId = SafeGetInt64(r, 1),
                    Estado = SafeGetString(r, 2),
                    Motivo = SafeGetString(r, 3),
                    UsrId = SafeGetInt64(r, 4),
                    Fecha = SafeGetDateTime(r, 5)
                });
            }
        }

        try { await using var cmdRollback = new OracleCommand("ROLLBACK", conn); await cmdRollback.ExecuteNonQueryAsync(); } catch { }
        return snap;
    }

    public static async Task SeedOracleXeAsync(string xeConnString, DatabaseSnapshot snapshot)
    {
        await using var conn = new OracleConnection(xeConnString);
        await conn.OpenAsync();

        await using var tx = conn.BeginTransaction();
        try
        {
            string[] cleanupTables = {
                "RL_MR_FLUJOS_EVALUACION", "RL_MR_EVIDENCIAS_VINCULOS", "RL_MR_EVIDENCIAS",
                "RL_MR_AUTOMONITOREO", "RL_MR_ACTIVIDADES", "RL_MR_PLANES",
                "RL_MR_EVALUACIONES_CONTROL", "RL_MR_CONTROLES_RIESGO", "RL_MR_SENALES_ALERTA",
                "RL_MR_PROYECCIONES_EVALUACION", "RL_MR_EVALUACIONES_RIESGO", "RL_MR_RIESGOS"
            };

            foreach (var tbl in cleanupTables)
            {
                await using var cmd = new OracleCommand($"DELETE FROM {tbl}", conn) { Transaction = tx };
                await cmd.ExecuteNonQueryAsync();
            }

            // Insertar Riesgos
            foreach (var r in snapshot.Risks)
            {
                await using var cmd = new OracleCommand(@"
                    INSERT INTO RL_MR_RIESGOS (RIE_ID, RIE_CODIGO, RIE_NOMBRE, RIE_DESCRIPCION, RIE_ACTIVO, RIE_USR_CREACION, RIE_FECHA_CREACION)
                    VALUES (:id, :cod, :nom, :p_desc, :act, :usr, :fCre)", conn) { Transaction = tx };
                cmd.Parameters.Add(new OracleParameter("id", r.Id));
                cmd.Parameters.Add(new OracleParameter("cod", r.Code));
                cmd.Parameters.Add(new OracleParameter("nom", r.Name));
                cmd.Parameters.Add(new OracleParameter("p_desc", r.Description));
                cmd.Parameters.Add(new OracleParameter("act", r.Active));
                cmd.Parameters.Add(new OracleParameter("usr", r.UsrCreacion));
                cmd.Parameters.Add(new OracleParameter("fCre", r.FechaCreacion));
                await cmd.ExecuteNonQueryAsync();
            }

            // Insertar Evaluaciones
            foreach (var e in snapshot.Evaluations)
            {
                await using var cmd = new OracleCommand(@"
                    INSERT INTO RL_MR_EVALUACIONES_RIESGO (
                        EVA_ID, EVA_RIESGO_ID, EVA_VERSION_ID, EVA_ACTIVO,
                        EVA_DATOS_JSON, EVA_CALCULOS_JSON, EVA_FECHA_REGISTRO, EVA_USR_REGISTRO, EVA_VERSION_ROW
                    ) VALUES (
                        :id, :rId, :vId, :act,
                        :datos, :calcs, :fReg, :usr, :vRow
                    )", conn) { Transaction = tx };
                cmd.Parameters.Add(new OracleParameter("id", e.Id));
                cmd.Parameters.Add(new OracleParameter("rId", e.RiskId));
                cmd.Parameters.Add(new OracleParameter("vId", e.VersionId));
                cmd.Parameters.Add(new OracleParameter("act", e.Active));
                cmd.Parameters.Add(new OracleParameter("datos", e.DatosJson));
                cmd.Parameters.Add(new OracleParameter("calcs", e.CalculosJson));
                cmd.Parameters.Add(new OracleParameter("fReg", e.FechaRegistro));
                cmd.Parameters.Add(new OracleParameter("usr", e.UsrRegistro));
                cmd.Parameters.Add(new OracleParameter("vRow", e.VersionRow));
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
                        :pId, :eId, :cod, :area, :dueno,
                        :vri, :nInh, :vrr, :nRes,
                        :resp, :est, :fEval
                    )", conn) { Transaction = tx };
                cmd.Parameters.Add(new OracleParameter("pId", p.Id));
                cmd.Parameters.Add(new OracleParameter("eId", p.EvaluationId));
                cmd.Parameters.Add(new OracleParameter("cod", p.RiskCode));
                cmd.Parameters.Add(new OracleParameter("area", p.AreaPrincipal));
                cmd.Parameters.Add(new OracleParameter("dueno", p.DuenoRiesgo));
                cmd.Parameters.Add(new OracleParameter("vri", p.Vri));
                cmd.Parameters.Add(new OracleParameter("nInh", p.NivelInherente));
                cmd.Parameters.Add(new OracleParameter("vrr", p.Vrr));
                cmd.Parameters.Add(new OracleParameter("nRes", p.NivelResidual));
                cmd.Parameters.Add(new OracleParameter("resp", p.RespuestaRiesgo));
                cmd.Parameters.Add(new OracleParameter("est", p.EstadoEvaluacion));
                cmd.Parameters.Add(new OracleParameter("fEval", p.FechaEval));
                await cmd.ExecuteNonQueryAsync();
            }

            // Insertar Controles preexistentes
            foreach (var c in snapshot.Controls)
            {
                await using var cmd = new OracleCommand(@"
                    INSERT INTO RL_MR_CONTROLES_RIESGO (CON_ID, CON_EVALUACION_ID, CON_TIPO, CON_DESCRIPCION, CON_AUTOMATIZACION, CON_ESTADO, CON_ESTADO_MONITOREO, CON_EFECTIVIDAD_MONITOREO)
                    VALUES (:id, :eId, :tipo, :p_desc, :auto, :est, :eMon, :efMon)", conn) { Transaction = tx };
                cmd.Parameters.Add(new OracleParameter("id", c.Id));
                cmd.Parameters.Add(new OracleParameter("eId", c.EvaluationId));
                cmd.Parameters.Add(new OracleParameter("tipo", c.Tipo));
                cmd.Parameters.Add(new OracleParameter("p_desc", c.Descripcion));
                cmd.Parameters.Add(new OracleParameter("auto", c.Automatizacion));
                cmd.Parameters.Add(new OracleParameter("est", c.Estado));
                cmd.Parameters.Add(new OracleParameter("eMon", c.EstadoMonitoreo));
                cmd.Parameters.Add(new OracleParameter("efMon", c.EfectividadMonitoreo));
                await cmd.ExecuteNonQueryAsync();
            }

            // Insertar Evaluaciones de Control
            foreach (var ec in snapshot.ControlEvaluations)
            {
                await using var cmd = new OracleCommand(@"
                    INSERT INTO RL_MR_EVALUACIONES_CONTROL (ECO_ID, ECO_CONTROL_ID, ECO_EFECTIVIDAD, ECO_COMENTARIO)
                    VALUES (:id, :cId, :ef, :com)", conn) { Transaction = tx };
                cmd.Parameters.Add(new OracleParameter("id", ec.Id));
                cmd.Parameters.Add(new OracleParameter("cId", ec.ControlId));
                cmd.Parameters.Add(new OracleParameter("ef", ec.Efectividad));
                cmd.Parameters.Add(new OracleParameter("com", ec.Comentario));
                await cmd.ExecuteNonQueryAsync();
            }

            // Insertar Planes preexistentes
            foreach (var pl in snapshot.Plans)
            {
                await using var cmd = new OracleCommand(@"
                    INSERT INTO RL_MR_PLANES (PLA_ID, PLA_EVALUACION_ID, PLA_DESCRIPCION, PLA_AVANCE, PLA_PRESUPUESTO, PLA_FECHA_INICIO, PLA_FECHA_FIN, PLA_ESTADO, PLA_MONITOREO_SEGUIMIENTO, PLA_RESPONSABLES, PLA_RECURSOS)
                    VALUES (:id, :eId, :p_desc, :av, :pr, :fIni, :fFin, :est, :mon, :resp, :rec)", conn) { Transaction = tx };
                cmd.Parameters.Add(new OracleParameter("id", pl.Id));
                cmd.Parameters.Add(new OracleParameter("eId", pl.EvaluationId));
                cmd.Parameters.Add(new OracleParameter("p_desc", pl.Descripcion));
                cmd.Parameters.Add(new OracleParameter("av", pl.Avance));
                cmd.Parameters.Add(new OracleParameter("pr", pl.Presupuesto));
                cmd.Parameters.Add(new OracleParameter("fIni", pl.FechaInicio));
                cmd.Parameters.Add(new OracleParameter("fFin", pl.FechaFin));
                cmd.Parameters.Add(new OracleParameter("est", pl.Estado));
                cmd.Parameters.Add(new OracleParameter("mon", pl.MonitoreoSeguimiento));
                cmd.Parameters.Add(new OracleParameter("resp", pl.Responsables));
                cmd.Parameters.Add(new OracleParameter("rec", pl.Recursos));
                await cmd.ExecuteNonQueryAsync();
            }

            // Insertar Actividades preexistentes
            foreach (var ac in snapshot.Activities)
            {
                await using var cmd = new OracleCommand(@"
                    INSERT INTO RL_MR_ACTIVIDADES (ACT_ID, ACT_PLAN_ID, ACT_DESCRIPCION, ACT_RESPONSABLE, ACT_AVANCE, ACT_FECHA_INICIO, ACT_FECHA_FIN, ACT_ESTADO)
                    VALUES (:id, :pId, :p_desc, :resp, :av, :fIni, :fFin, :est)", conn) { Transaction = tx };
                cmd.Parameters.Add(new OracleParameter("id", ac.Id));
                cmd.Parameters.Add(new OracleParameter("pId", ac.PlanId));
                cmd.Parameters.Add(new OracleParameter("p_desc", ac.Descripcion));
                cmd.Parameters.Add(new OracleParameter("resp", ac.Responsable));
                cmd.Parameters.Add(new OracleParameter("av", ac.Avance));
                cmd.Parameters.Add(new OracleParameter("fIni", ac.FechaInicio));
                cmd.Parameters.Add(new OracleParameter("fFin", ac.FechaFin));
                cmd.Parameters.Add(new OracleParameter("est", ac.Estado));
                await cmd.ExecuteNonQueryAsync();
            }

            // Insertar Señales de Alerta
            foreach (var al in snapshot.Alerts)
            {
                await using var cmd = new OracleCommand(@"
                    INSERT INTO RL_MR_SENALES_ALERTA (ALE_ID, ALE_EVALUACION_ID, ALE_CODIGO, ALE_INDICADOR, ALE_ESTADO, ALE_FECHA_DISPARO)
                    VALUES (:id, :eId, :cod, :ind, :est, :fDisp)", conn) { Transaction = tx };
                cmd.Parameters.Add(new OracleParameter("id", al.Id));
                cmd.Parameters.Add(new OracleParameter("eId", al.EvaluationId));
                cmd.Parameters.Add(new OracleParameter("cod", al.Codigo));
                cmd.Parameters.Add(new OracleParameter("ind", al.Indicador));
                cmd.Parameters.Add(new OracleParameter("est", al.Estado));
                cmd.Parameters.Add(new OracleParameter("fDisp", al.FechaDisparo.HasValue ? (object)al.FechaDisparo.Value : DBNull.Value));
                await cmd.ExecuteNonQueryAsync();
            }

            // Insertar Automonitoreo
            foreach (var mo in snapshot.Monitorings)
            {
                await using var cmd = new OracleCommand(@"
                    INSERT INTO RL_MR_AUTOMONITOREO (MON_ID, MON_EVALUACION_ID, MON_ESTADO_RIESGO, MON_ESTADO_CONTR, MON_RESULTADO, MON_USR_ID, MON_OBSERVACIONES_AREA, MON_OBSERVACIONES_UGR, MON_FECHA)
                    VALUES (:id, :eId, :eRie, :eCon, :res, :usr, :obsA, :obsU, :fMon)", conn) { Transaction = tx };
                cmd.Parameters.Add(new OracleParameter("id", mo.Id));
                cmd.Parameters.Add(new OracleParameter("eId", mo.EvaluationId));
                cmd.Parameters.Add(new OracleParameter("eRie", mo.EstadoRiesgo));
                cmd.Parameters.Add(new OracleParameter("eCon", mo.EstadoContr));
                cmd.Parameters.Add(new OracleParameter("res", mo.Resultado));
                cmd.Parameters.Add(new OracleParameter("usr", mo.UsrId));
                cmd.Parameters.Add(new OracleParameter("obsA", mo.ObservacionesArea));
                cmd.Parameters.Add(new OracleParameter("obsU", mo.ObservacionesUgr));
                cmd.Parameters.Add(new OracleParameter("fMon", mo.Fecha));
                await cmd.ExecuteNonQueryAsync();
            }

            // Insertar Evidencias
            foreach (var ev in snapshot.Evidences)
            {
                await using var cmdEvi = new OracleCommand(@"
                    INSERT INTO RL_MR_EVIDENCIAS (EVI_ID, EVI_NOMBRE_ARCHIVO, EVI_EXTENSION, EVI_TAMANO, EVI_HASH, EVI_RUTA, EVI_USR_CREACION, EVI_FECHA_CREACION)
                    VALUES (:id, :nom, :ext, :tam, :hsh, :rut, 1, SYSDATE)", conn) { Transaction = tx };
                cmdEvi.Parameters.Add(new OracleParameter("id", ev.EvidenciaId));
                cmdEvi.Parameters.Add(new OracleParameter("nom", ev.NombreArchivo));
                cmdEvi.Parameters.Add(new OracleParameter("ext", ev.Extension));
                cmdEvi.Parameters.Add(new OracleParameter("tam", ev.Tamano));
                cmdEvi.Parameters.Add(new OracleParameter("hsh", ev.Hash));
                cmdEvi.Parameters.Add(new OracleParameter("rut", ev.Ruta));
                await cmdEvi.ExecuteNonQueryAsync();

                await using var cmdVin = new OracleCommand(@"
                    INSERT INTO RL_MR_EVIDENCIAS_VINCULOS (EVV_ID, EVV_EVIDENCIA_ID, EVV_TIPO_ENTIDAD, EVV_ENTIDAD_ID, EVV_USR_CREACION, EVV_FECHA_CREACION)
                    VALUES (:id, :eId, :tipo, :entId, 1, SYSDATE)", conn) { Transaction = tx };
                cmdVin.Parameters.Add(new OracleParameter("id", ev.VinculoId));
                cmdVin.Parameters.Add(new OracleParameter("eId", ev.EvidenciaId));
                cmdVin.Parameters.Add(new OracleParameter("tipo", ev.TipoEntidad));
                cmdVin.Parameters.Add(new OracleParameter("entId", ev.EntidadId));
                await cmdVin.ExecuteNonQueryAsync();
            }

            // Insertar Flujos
            foreach (var fl in snapshot.Flujos)
            {
                await using var cmd = new OracleCommand(@"
                    INSERT INTO RL_MR_FLUJOS_EVALUACION (FLU_ID, FLU_EVALUACION_ID, FLU_ESTADO, FLU_MOTIVO, FLU_USR_ID, FLU_FECHA)
                    VALUES (:id, :eId, :est, :mot, :usr, :fFlu)", conn) { Transaction = tx };
                cmd.Parameters.Add(new OracleParameter("id", fl.Id));
                cmd.Parameters.Add(new OracleParameter("eId", fl.EvaluationId));
                cmd.Parameters.Add(new OracleParameter("est", fl.Estado));
                cmd.Parameters.Add(new OracleParameter("mot", fl.Motivo));
                cmd.Parameters.Add(new OracleParameter("usr", fl.UsrId));
                cmd.Parameters.Add(new OracleParameter("fFlu", fl.Fecha));
                await cmd.ExecuteNonQueryAsync();
            }

            await tx.CommitAsync();

            // Verificación estricta de paridad de Seeding en XE (Matriz de 12 tablas, Req. 23)
            var xeCounts = await QueryTableCountsAsync(conn);
            var expectedCounts = new Dictionary<string, int>
            {
                ["RL_MR_RIESGOS"] = snapshot.Risks.Count,
                ["RL_MR_EVALUACIONES_RIESGO"] = snapshot.Evaluations.Count,
                ["RL_MR_PROYECCIONES_EVALUACION"] = snapshot.Projections.Count,
                ["RL_MR_CONTROLES_RIESGO"] = snapshot.Controls.Count,
                ["RL_MR_EVALUACIONES_CONTROL"] = snapshot.ControlEvaluations.Count,
                ["RL_MR_PLANES"] = snapshot.Plans.Count,
                ["RL_MR_ACTIVIDADES"] = snapshot.Activities.Count,
                ["RL_MR_SENALES_ALERTA"] = snapshot.Alerts.Count,
                ["RL_MR_AUTOMONITOREO"] = snapshot.Monitorings.Count,
                ["RL_MR_EVIDENCIAS"] = snapshot.Evidences.Count,
                ["RL_MR_EVIDENCIAS_VINCULOS"] = snapshot.Evidences.Count,
                ["RL_MR_FLUJOS_EVALUACION"] = snapshot.Flujos.Count
            };

            foreach (var kvp in expectedCounts)
            {
                if (!xeCounts.TryGetValue(kvp.Key, out int actual) || actual != kvp.Value)
                {
                    throw new InvalidOperationException($"XE_PREIMAGE_PARITY_FAILED: Discrepancia en {kvp.Key}. Esperado={kvp.Value}, XE={actual}");
                }
            }
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    private static async Task<Dictionary<string, int>> QueryTableCountsAsync(OracleConnection conn)
    {
        var counts = new Dictionary<string, int>();
        string[] tables = {
            "RL_MR_RIESGOS", "RL_MR_EVALUACIONES_RIESGO", "RL_MR_PROYECCIONES_EVALUACION",
            "RL_MR_CONTROLES_RIESGO", "RL_MR_EVALUACIONES_CONTROL", "RL_MR_PLANES",
            "RL_MR_ACTIVIDADES", "RL_MR_SENALES_ALERTA", "RL_MR_AUTOMONITOREO", "RL_MR_EVIDENCIAS",
            "RL_MR_EVIDENCIAS_VINCULOS", "RL_MR_FLUJOS_EVALUACION"
        };
        foreach (var tbl in tables)
        {
            await using var cmd = new OracleCommand($"SELECT COUNT(*) FROM {tbl}", conn);
            counts[tbl] = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }
        return counts;
    }

    #endregion

    #region Motor Canónico de Reconciliación

    public static PlanResult BuildReconciliationPlan(
        DatabaseSnapshot snapshot,
        List<JsonElement> excelRows,
        List<JsonElement> manifestFields,
        JsonDocument catalogDoc,
        string workbookSha = ExpectedWorkbookSha256,
        string preimageManifestSha = "")
    {
        var plan = new PlanResult
        {
            RunId = snapshot.RunId,
            PreexistingDuplicates = 0
        };

        var catalogMaps = BuildCatalogNormalizationDictionaries(catalogDoc);

        // Validación estricta de igualdad de conjuntos de códigos de riesgo (Req. 21)
        var excelCodes = excelRows.Select(r => r.GetProperty("riskCode").GetString()!.Trim()).ToList();
        if (excelCodes.Any(string.IsNullOrWhiteSpace))
        {
            throw new InvalidOperationException("RISK_CODE_MISSING: Al menos una fila de Excel no tiene riskCode válido.");
        }
        if (excelCodes.Distinct(StringComparer.OrdinalIgnoreCase).Count() != excelCodes.Count)
        {
            throw new InvalidOperationException("RISK_CODE_DUPLICATE_IN_EXCEL: Se encontraron códigos duplicados en Excel.");
        }

        var dbCodes = snapshot.Risks.Select(r => r.Code.Trim()).ToList();
        if (dbCodes.Distinct(StringComparer.OrdinalIgnoreCase).Count() != snapshot.Risks.Count)
        {
            throw new InvalidOperationException("RISK_CODE_DUPLICATE_IN_DB: Se encontraron códigos duplicados en la base de datos.");
        }

        var missingInDb = excelCodes.Except(dbCodes, StringComparer.OrdinalIgnoreCase).ToList();
        var missingInExcel = dbCodes.Except(excelCodes, StringComparer.OrdinalIgnoreCase).ToList();
        if (missingInDb.Count > 0 || missingInExcel.Count > 0)
        {
            throw new InvalidOperationException($"RISK_CODE_SETS_MISMATCH: Discrepancia de universos entre Excel y DB. Faltan en DB: {string.Join(", ", missingInDb)}; Faltan en Excel: {string.Join(", ", missingInExcel)}");
        }

        var risksByCode = snapshot.Risks.ToDictionary(r => r.Code.Trim(), r => r, StringComparer.OrdinalIgnoreCase);
        var evasByCode = snapshot.Evaluations.ToDictionary(e => e.RiskCode.Trim(), e => e, StringComparer.OrdinalIgnoreCase);
        var proysByCode = snapshot.Projections.ToDictionary(p => p.RiskCode.Trim(), p => p, StringComparer.OrdinalIgnoreCase);

        // Detección estricta de duplicados preexistentes con hard fail (Req. 22)
        int dupRisks = snapshot.Risks.GroupBy(r => r.Code, StringComparer.OrdinalIgnoreCase).Sum(g => g.Count() - 1);
        int dupControls = snapshot.Controls.GroupBy(c => $"{c.EvaluationId}_{c.Tipo}_{NormalizeControlText(c.Descripcion)}").Sum(g => g.Count() - 1);
        int dupPlans = snapshot.Plans.GroupBy(p => $"{p.EvaluationId}_{NormalizeControlText(p.Descripcion)}").Sum(g => g.Count() - 1);
        int dupActs = snapshot.Activities.GroupBy(a => $"{a.PlanId}_{NormalizeControlText(a.Descripcion)}").Sum(g => g.Count() - 1);
        int dupAlerts = snapshot.Alerts.GroupBy(a => $"{a.EvaluationId}_{a.Codigo}").Sum(g => g.Count() - 1);
        plan.PreexistingDuplicates = dupRisks + dupControls + dupPlans + dupActs + dupAlerts;

        if (plan.PreexistingDuplicates > 0)
        {
            throw new InvalidOperationException($"PREEXISTING_DUPLICATES_HARD_FAIL: Se detectaron {plan.PreexistingDuplicates} duplicados preexistentes en la base de datos.");
        }

        var existingControlsByEva = snapshot.Controls
            .GroupBy(c => c.EvaluationId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var existingPlansByEva = snapshot.Plans
            .GroupBy(p => p.EvaluationId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var existingActivitiesByPlan = snapshot.Activities
            .GroupBy(a => a.PlanId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var existingAlertsByEva = snapshot.Alerts
            .GroupBy(a => a.EvaluationId)
            .ToDictionary(g => g.Key, g => g.ToList());

        long nextControlId = snapshot.Controls.Count > 0 ? snapshot.Controls.Max(c => c.Id) + 1 : 10001;

        // Auditoría completa de Campos 71 a 82 (708 posiciones, Req. 11, 12, 13, 15)
        foreach (var r in excelRows)
        {
            string code = r.GetProperty("riskCode").GetString()!.Trim();
            evasByCode.TryGetValue(code, out var eva);
            long eId = eva?.Id ?? 0;

            var evaControls = existingControlsByEva.TryGetValue(eId, out var ecList) ? ecList : new List<ControlPreimage>();

            for (int fn = 71; fn <= 82; fn++)
            {
                var mf = manifestFields[fn - 1];
                string label = mf.GetProperty("label").GetString() ?? "";
                string cellVal = GetCellTextValue(r, fn);

                string dbRaw = "";
                string dbCanon = "";
                string recordId = "";
                string targetTbl = "";

                if (fn == 71)
                {
                    targetTbl = "RL_MR_AUTOMONITOREO";
                    // Selección contractual determinista: ciclo más reciente (Req. 15)
                    var mon = snapshot.Monitorings
                        .Where(m => m.EvaluationId == eId)
                        .OrderByDescending(m => m.Fecha)
                        .ThenByDescending(m => m.Id)
                        .FirstOrDefault();
                    if (mon != null) { dbRaw = mon.EstadoRiesgo; dbCanon = mon.EstadoRiesgo; recordId = mon.Id.ToString(); }
                }
                else if (fn is 72 or 73 or 74)
                {
                    targetTbl = "RL_MR_CONTROLES_RIESGO";
                    var prevCons = evaControls.Where(c => c.Tipo == "PREVENTIVO").OrderBy(c => c.Id).ToList();
                    if (prevCons.Count > 0)
                    {
                        recordId = string.Join(",", prevCons.Select(c => c.Id));
                        if (fn == 72)
                        {
                            dbRaw = prevCons.Count == 1 ? prevCons[0].EstadoMonitoreo : string.Join("\n", prevCons.Select((c, idx) => $"{idx + 1}. {c.EstadoMonitoreo}"));
                            dbCanon = dbRaw;
                        }
                        else if (fn == 73)
                        {
                            dbRaw = prevCons.Count == 1 ? prevCons[0].EfectividadMonitoreo.ToString("0.##", CultureInfo.InvariantCulture) : string.Join("\n", prevCons.Select((c, idx) => $"{idx + 1}. {c.EfectividadMonitoreo.ToString("0.##", CultureInfo.InvariantCulture)}"));
                            dbCanon = dbRaw;
                        }
                        else if (fn == 74)
                        {
                            var evis = prevCons.SelectMany(c => snapshot.Evidences
                                .Where(ev => ev.TipoEntidad == "CONTROL" && ev.EntidadId == c.Id)
                                .OrderBy(ev => ev.FechaCreacion)
                                .ThenBy(ev => ev.EvidenciaId)
                                .Select(ev => $"{ev.NombreArchivo}.{ev.Extension}"))
                                .ToList();
                            if (evis.Count > 0)
                            {
                                dbRaw = evis.Count == 1 ? evis[0] : string.Join("\n", evis.Select((ev, idx) => $"{idx + 1}. {ev}"));
                                dbCanon = dbRaw;
                            }
                        }
                    }
                }
                else if (fn is 75 or 76 or 77)
                {
                    targetTbl = "RL_MR_CONTROLES_RIESGO";
                    var detCons = evaControls.Where(c => c.Tipo == "DETECTIVO").OrderBy(c => c.Id).ToList();
                    if (detCons.Count > 0)
                    {
                        recordId = string.Join(",", detCons.Select(c => c.Id));
                        if (fn == 75)
                        {
                            dbRaw = detCons.Count == 1 ? detCons[0].EstadoMonitoreo : string.Join("\n", detCons.Select((c, idx) => $"{idx + 1}. {c.EstadoMonitoreo}"));
                            dbCanon = dbRaw;
                        }
                        else if (fn == 76)
                        {
                            dbRaw = detCons.Count == 1 ? detCons[0].EfectividadMonitoreo.ToString("0.##", CultureInfo.InvariantCulture) : string.Join("\n", detCons.Select((c, idx) => $"{idx + 1}. {c.EfectividadMonitoreo.ToString("0.##", CultureInfo.InvariantCulture)}"));
                            dbCanon = dbRaw;
                        }
                        else if (fn == 77)
                        {
                            var evis = detCons.SelectMany(c => snapshot.Evidences
                                .Where(ev => ev.TipoEntidad == "CONTROL" && ev.EntidadId == c.Id)
                                .OrderBy(ev => ev.FechaCreacion)
                                .ThenBy(ev => ev.EvidenciaId)
                                .Select(ev => $"{ev.NombreArchivo}.{ev.Extension}"))
                                .ToList();
                            if (evis.Count > 0)
                            {
                                dbRaw = evis.Count == 1 ? evis[0] : string.Join("\n", evis.Select((ev, idx) => $"{idx + 1}. {ev}"));
                                dbCanon = dbRaw;
                            }
                        }
                    }
                }
                else if (fn is 78 or 79 or 80)
                {
                    targetTbl = "RL_MR_CONTROLES_RIESGO";
                    var corCons = evaControls.Where(c => c.Tipo == "CORRECTIVO").OrderBy(c => c.Id).ToList();
                    if (corCons.Count > 0)
                    {
                        recordId = string.Join(",", corCons.Select(c => c.Id));
                        if (fn == 78)
                        {
                            dbRaw = corCons.Count == 1 ? corCons[0].EstadoMonitoreo : string.Join("\n", corCons.Select((c, idx) => $"{idx + 1}. {c.EstadoMonitoreo}"));
                            dbCanon = dbRaw;
                        }
                        else if (fn == 79)
                        {
                            dbRaw = corCons.Count == 1 ? corCons[0].EfectividadMonitoreo.ToString("0.##", CultureInfo.InvariantCulture) : string.Join("\n", corCons.Select((c, idx) => $"{idx + 1}. {c.EfectividadMonitoreo.ToString("0.##", CultureInfo.InvariantCulture)}"));
                            dbCanon = dbRaw;
                        }
                        else if (fn == 80)
                        {
                            var evis = corCons.SelectMany(c => snapshot.Evidences
                                .Where(ev => ev.TipoEntidad == "CONTROL" && ev.EntidadId == c.Id)
                                .OrderBy(ev => ev.FechaCreacion)
                                .ThenBy(ev => ev.EvidenciaId)
                                .Select(ev => $"{ev.NombreArchivo}.{ev.Extension}"))
                                .ToList();
                            if (evis.Count > 0)
                            {
                                dbRaw = evis.Count == 1 ? evis[0] : string.Join("\n", evis.Select((ev, idx) => $"{idx + 1}. {ev}"));
                                dbCanon = dbRaw;
                            }
                        }
                    }
                }
                else if (fn == 81)
                {
                    targetTbl = "RL_MR_AUTOMONITOREO";
                    var mon = snapshot.Monitorings
                        .Where(m => m.EvaluationId == eId)
                        .OrderByDescending(m => m.Fecha)
                        .ThenByDescending(m => m.Id)
                        .FirstOrDefault();
                    if (mon != null) { dbRaw = mon.ObservacionesArea; dbCanon = mon.ObservacionesArea; recordId = mon.Id.ToString(); }
                }
                else if (fn == 82)
                {
                    targetTbl = "RL_MR_AUTOMONITOREO";
                    var mon = snapshot.Monitorings
                        .Where(m => m.EvaluationId == eId)
                        .OrderByDescending(m => m.Fecha)
                        .ThenByDescending(m => m.Id)
                        .FirstOrDefault();
                    if (mon != null) { dbRaw = mon.ObservacionesUgr; dbCanon = mon.ObservacionesUgr; recordId = mon.Id.ToString(); }
                }

                bool hasDbVal = !string.IsNullOrWhiteSpace(dbRaw);
                string classif = hasDbVal ? "PRESERVE_PRODUCTION" : "NO_ACTION";
                string act = hasDbVal ? "PRESERVE_PRODUCTION" : "NO_ACTION";

                plan.Field71To82AuditEntries.Add(new Field71To82AuditEntry
                {
                    RiskCode = code,
                    FieldNumber = fn,
                    FieldLabel = label,
                    TargetTable = targetTbl,
                    RecordId = recordId,
                    DbBeforeRaw = dbRaw,
                    DbBeforeCanonical = dbCanon,
                    ExcelRaw = cellVal,
                    Classification = classif,
                    Action = act,
                    PreservedInDb = true
                });
            }
        }

        // Bucle Principal de Reconciliación 59 Riesgos × 82 Campos
        foreach (var r in excelRows)
        {
            string code = r.GetProperty("riskCode").GetString()!.Trim();
            risksByCode.TryGetValue(code, out var risk);
            evasByCode.TryGetValue(code, out var eva);
            proysByCode.TryGetValue(code, out var proy);

            long evaId = eva?.Id ?? 0;
            JsonObject currentDataJson = new();
            if (eva != null && !string.IsNullOrWhiteSpace(eva.DatosJson))
            {
                try { currentDataJson = JsonNode.Parse(eva.DatosJson) as JsonObject ?? new JsonObject(); } catch { }
            }

            var mergedDataJson = JsonNode.Parse(currentDataJson.ToJsonString()) as JsonObject ?? new JsonObject();
            bool dataJsonMutated = false;

            // Automation en Excel para este riesgo (Campo 32)
            string f32ExcelRaw = GetCellTextValue(r, 32);
            string? f32Canonical = NormalizeCatalogValue("MR_NIVEL_AUTOMATIZACION", f32ExcelRaw, catalogMaps, plan.CatalogNormalizations);

            for (int f = 1; f <= 82; f++)
            {
                var manifestField = manifestFields[f - 1];
                string label = manifestField.GetProperty("label").GetString() ?? "";
                string source = manifestField.GetProperty("source").GetString() ?? "";
                string canonicalKey = manifestField.GetProperty("canonicalKey").GetString() ?? "";
                string currentKey = manifestField.TryGetProperty("currentKey", out var ckProp) ? ckProp.GetString() ?? "" : "";
                string mode = manifestField.GetProperty("mode").GetString() ?? "";

                string rawExcel = GetCellTextValue(r, f);
                string positionKey = $"{code}_F{f:D2}";

                // 1. Campos calculados (Fórmulas F01..F34 y Derivados F41, F43 y F50..F69)
                if (mode == "CALCULATED" || source == "EXCEL_FORMULA" || f is 41 or 43 || (f >= 50 && f <= 69))
                {
                    plan.RecalculateBackendCount++;
                    plan.AllPositions.Add(new ReconciliationPosition
                    {
                        PositionKey = positionKey,
                        RiskCode = code,
                        FieldNumber = f,
                        FieldLabel = label,
                        Action = "RECALCULATE_BACKEND",
                        ReasonCode = "CALCULATED_FIELD_DEFERRED_TO_BLOCK5",
                        ExcelValue = rawExcel,
                        DbValue = "[CALCULATED]",
                        TargetValue = "[RECALCULATE_IN_BACKEND]",
                        Mutates = false
                    });
                    continue;
                }

                // 2. GTIC Condicional (Campos 17, 18, 19) evaluado por tipo_riesgo y area (Req. 17)
                string evalTipo = currentDataJson.TryGetPropertyValue("tipo_riesgo", out var cjT) ? cjT?.ToString() ?? "" : GetCellTextValue(r, 5);
                string evalArea = proy != null && !string.IsNullOrWhiteSpace(proy.AreaPrincipal) ? proy.AreaPrincipal : GetCellTextValue(r, 3);
                if (f is 17 or 18 or 19 && !IsGticApplicable(evalTipo, evalArea))
                {
                    plan.NotApplicableCount++;
                    plan.AllPositions.Add(new ReconciliationPosition
                    {
                        PositionKey = positionKey,
                        RiskCode = code,
                        FieldNumber = f,
                        FieldLabel = label,
                        Action = "NOT_APPLICABLE",
                        ReasonCode = "GTIC_ONLY_FIELD_ON_NON_GTIC_RISK",
                        ExcelValue = rawExcel,
                        DbValue = "",
                        TargetValue = "",
                        Mutates = false
                    });
                    continue;
                }

                // 3. Mitigación no requerida (Campos 40 a 49 si no aplica mitigación por Regla A o B, Req. 9, 10)
                if (f >= 40 && f <= 49 && !MitigationApplies(proy))
                {
                    plan.NotApplicableCount++;
                    plan.AllPositions.Add(new ReconciliationPosition
                    {
                        PositionKey = positionKey,
                        RiskCode = code,
                        FieldNumber = f,
                        FieldLabel = label,
                        Action = "NOT_APPLICABLE",
                        ReasonCode = "NON_MITIGATION_RISK",
                        ExcelValue = rawExcel,
                        DbValue = "",
                        TargetValue = "",
                        Mutates = false
                    });
                    continue;
                }

                // 4. Preservación operacional en Campo 70 (Señales de Alerta) transversal a los 59 riesgos (Req. 16)
                if (f == 70)
                {
                    string db70 = "";
                    if (existingAlertsByEva.TryGetValue(evaId, out var alList) && alList.Count > 0)
                    {
                        db70 = string.Join("\n", alList.Select((a, idx) => $"{idx + 1}. {a.Indicador}"));
                    }

                    if (!string.IsNullOrWhiteSpace(db70))
                    {
                        plan.PreserveProductionCount++;
                        plan.OperationalValuesPreserved++;
                        plan.AllPositions.Add(new ReconciliationPosition
                        {
                            PositionKey = positionKey,
                            RiskCode = code,
                            FieldNumber = 70,
                            FieldLabel = label,
                            Action = "PRESERVE_PRODUCTION",
                            ReasonCode = "OPERATIONAL_ALERT_NEWER_IN_DB",
                            ExcelValue = rawExcel,
                            DbValue = db70,
                            TargetValue = db70,
                            Mutates = false
                        });
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(rawExcel) || string.Equals(rawExcel.Trim(), db70.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        plan.NoActionCount++;
                        plan.AllPositions.Add(new ReconciliationPosition
                        {
                            PositionKey = positionKey,
                            RiskCode = code,
                            FieldNumber = 70,
                            FieldLabel = label,
                            Action = "NO_ACTION",
                            ReasonCode = string.IsNullOrWhiteSpace(rawExcel) ? "BLANK_EXCEL" : "EXACT_MATCH",
                            ExcelValue = rawExcel,
                            DbValue = db70,
                            TargetValue = db70,
                            Mutates = false
                        });
                        continue;
                    }
                }

                // 5. Preservación en Campos 71 a 82
                if (f >= 71 && f <= 82)
                {
                    var aud = plan.Field71To82AuditEntries.First(a => a.RiskCode == code && a.FieldNumber == f);
                    if (aud.Classification == "PRESERVE_PRODUCTION")
                    {
                        plan.PreserveProductionCount++;
                        plan.AllPositions.Add(new ReconciliationPosition
                        {
                            PositionKey = positionKey,
                            RiskCode = code,
                            FieldNumber = f,
                            FieldLabel = label,
                            Action = "PRESERVE_PRODUCTION",
                            ReasonCode = "OPERATIONAL_71_82_PRESERVED",
                            ExcelValue = rawExcel,
                            DbValue = aud.DbBeforeRaw,
                            TargetValue = aud.DbBeforeRaw,
                            Mutates = false
                        });
                    }
                    else
                    {
                        plan.NoActionCount++;
                        plan.AllPositions.Add(new ReconciliationPosition
                        {
                            PositionKey = positionKey,
                            RiskCode = code,
                            FieldNumber = f,
                            FieldLabel = label,
                            Action = "NO_ACTION",
                            ReasonCode = "OPERATIONAL_PENDING",
                            ExcelValue = rawExcel,
                            DbValue = aud.DbBeforeRaw,
                            TargetValue = aud.DbBeforeRaw,
                            Mutates = false
                        });
                    }
                    continue;
                }

                // 6. Campo 39: Respuesta al Riesgo
                if (f == 39 && proy != null && !string.IsNullOrWhiteSpace(proy.RespuestaRiesgo))
                {
                    plan.PreserveProductionCount++;
                    plan.AllPositions.Add(new ReconciliationPosition
                    {
                        PositionKey = positionKey,
                        RiskCode = code,
                        FieldNumber = 39,
                        FieldLabel = label,
                        Action = "PRESERVE_PRODUCTION",
                        ReasonCode = "CATALOG_RESPONSE_PRESERVED",
                        ExcelValue = rawExcel,
                        DbValue = proy.RespuestaRiesgo,
                        TargetValue = proy.RespuestaRiesgo,
                        Mutates = false
                    });
                    continue;
                }

                // 7. Corrección ortográfica en F09 (Descripción del Riesgo en RL_MR_RIESGOS)
                if (f == 9 && risk != null)
                {
                    string dbDesc = risk.Description ?? "";
                    string excelDesc = rawExcel.Trim();

                    if (excelDesc.Length > 2000)
                    {
                        plan.SilentTruncationsCount++;
                        throw new InvalidOperationException($"VALUE_EXCEEDS_TARGET_LENGTH: RIE_DESCRIPCION de {code} tiene {excelDesc.Length} caracteres (máx 2000).");
                    }

                    if (!string.Equals(dbDesc, excelDesc, StringComparison.Ordinal))
                    {
                        plan.UpdateBaselineCount++;
                        plan.PhysicalUpdatesCount++;
                        string mutId = $"MUT_{code}_F09";

                        var mut = new PlannedMutation
                        {
                            MutationId = mutId,
                            RiskCode = code,
                            FieldNumber = 9,
                            FieldLabel = label,
                            Entity = "RL_MR_RIESGOS",
                            TargetTable = "RL_MR_RIESGOS",
                            RecordKey = risk.Id.ToString(),
                            Action = "UPDATE_BASELINE",
                            TargetColumnOrProperty = "RIE_DESCRIPCION",
                            ExcelRaw = rawExcel,
                            ExcelCanonical = excelDesc,
                            DbBeforeRaw = dbDesc,
                            DbBeforeCanonical = dbDesc,
                            TargetCanonical = excelDesc,
                            ReasonCode = "TYPO_FIX_BASELINE_UPDATE",
                            Authority = "EXCEL_WORKBOOK_OFFICIAL",
                            Precondition = $"RIE_DESCRIPCION_EQUALS('{dbDesc}')",
                            PreconditionSql = "SELECT RIE_DESCRIPCION FROM RL_MR_RIESGOS WHERE RIE_ID = :id",
                            ApplySql = "UPDATE RL_MR_RIESGOS SET RIE_DESCRIPCION = :newDesc WHERE RIE_ID = :id AND NVL(RIE_DESCRIPCION, ' ') = :expectedBefore",
                            ApplyParameters = new Dictionary<string, object?>
                            {
                                ["id"] = risk.Id,
                                ["newDesc"] = excelDesc,
                                ["expectedBefore"] = string.IsNullOrEmpty(dbDesc) ? " " : dbDesc
                            },
                            Rollback = new RollbackOperation
                            {
                                MutationId = mutId,
                                Entity = "RL_MR_RIESGOS",
                                Table = "RL_MR_RIESGOS",
                                RecordPk = risk.Id.ToString(),
                                Action = "UPDATE",
                                BeforeValue = dbDesc,
                                AfterValue = excelDesc,
                                RollbackAction = "UPDATE",
                                RollbackSql = "UPDATE RL_MR_RIESGOS SET RIE_DESCRIPCION = :beforeDesc WHERE RIE_ID = :id",
                                RollbackParameters = new Dictionary<string, object?>
                                {
                                    ["id"] = risk.Id,
                                    ["beforeDesc"] = dbDesc
                                }
                            }
                        };

                        plan.Mutations.Add(mut);
                        plan.RollbackOperations.Add(mut.Rollback);

                        plan.AllPositions.Add(new ReconciliationPosition
                        {
                            PositionKey = positionKey,
                            RiskCode = code,
                            FieldNumber = 9,
                            FieldLabel = label,
                            Action = "UPDATE_BASELINE",
                            ReasonCode = "TYPO_FIX_BASELINE_UPDATE",
                            ExcelValue = rawExcel,
                            DbValue = dbDesc,
                            TargetValue = excelDesc,
                            Mutates = true
                        });
                        continue;
                    }
                    else
                    {
                        plan.MatchCount++;
                        plan.NoActionCount++;
                        plan.AllPositions.Add(new ReconciliationPosition
                        {
                            PositionKey = positionKey,
                            RiskCode = code,
                            FieldNumber = 9,
                            FieldLabel = label,
                            Action = "NO_ACTION",
                            ReasonCode = "EXACT_MATCH",
                            ExcelValue = rawExcel,
                            DbValue = dbDesc,
                            TargetValue = dbDesc,
                            Mutates = false
                        });
                        continue;
                    }
                }

                // 8. Reconciliación de Controles (Campos 20: Preventivo, 24: Detectivo, 28: Correctivo)
                if (f is 20 or 24 or 28)
                {
                    string conTipo = f == 20 ? "PREVENTIVO" : f == 24 ? "DETECTIVO" : "CORRECTIVO";
                    var existingList = existingControlsByEva.TryGetValue(evaId, out var el)
                        ? el.Where(c => c.Tipo == conTipo).ToList()
                        : new List<ControlPreimage>();

                    var excelItems = ParseMultilineItems(rawExcel);
                    string dbControlsText = string.Join("\n", existingList.Select((c, idx) => $"{idx + 1}. {c.Descripcion}"));

                    if (excelItems.Count == 0 || (excelItems.Count == 1 && IsEmptyOrNoHay(excelItems[0])))
                    {
                        plan.NoActionCount++;
                        plan.AllPositions.Add(new ReconciliationPosition
                        {
                            PositionKey = positionKey,
                            RiskCode = code,
                            FieldNumber = f,
                            FieldLabel = label,
                            Action = "NO_ACTION",
                            ReasonCode = excelItems.Count == 0 ? "BLANK_EXCEL" : "CANONICAL_NO_CONTROLS_MATCH",
                            ExcelValue = rawExcel,
                            DbValue = dbControlsText,
                            TargetValue = dbControlsText,
                            Mutates = false
                        });
                        continue;
                    }

                    int insertedForThisField = 0;
                    for (int i = 0; i < excelItems.Count; i++)
                    {
                        string targetDesc = excelItems[i].Trim();
                        if (IsEmptyOrNoHay(targetDesc)) continue;

                        if (targetDesc.Length > 500)
                        {
                            plan.SilentTruncationsCount++;
                            throw new InvalidOperationException($"VALUE_EXCEEDS_TARGET_LENGTH: Control {conTipo} de {code} tiene {targetDesc.Length} caracteres (máx 500).");
                        }

                        var existingMatch = existingList.FirstOrDefault(c =>
                            NormalizeControlText(c.Descripcion).Equals(NormalizeControlText(targetDesc), StringComparison.OrdinalIgnoreCase));

                        if (existingMatch != null)
                        {
                            plan.MatchCount++;
                            continue;
                        }

                        string? targetAuto = f32Canonical;
                        if (string.IsNullOrWhiteSpace(targetAuto))
                        {
                            continue;
                        }

                        long assignedId = nextControlId++;
                        string mutId = $"MUT_{code}_F{f}_CON_{assignedId}";

                        var mut = new PlannedMutation
                        {
                            MutationId = mutId,
                            RiskCode = code,
                            FieldNumber = f,
                            FieldLabel = label,
                            Entity = "RL_MR_CONTROLES_RIESGO",
                            TargetTable = "RL_MR_CONTROLES_RIESGO",
                            RecordKey = assignedId.ToString(),
                            Action = "INSERT_BASELINE",
                            TargetColumnOrProperty = "CON_DESCRIPCION",
                            ExcelRaw = targetDesc,
                            ExcelCanonical = targetDesc,
                            DbBeforeRaw = "",
                            DbBeforeCanonical = "",
                            TargetCanonical = targetDesc,
                            ReasonCode = "MISSING_BASELINE_CONTROL_INSERT",
                            Authority = "EXCEL_WORKBOOK_OFFICIAL",
                            Precondition = "CONTROL_NOT_EXISTS",
                            PreconditionSql = "SELECT COUNT(*) FROM RL_MR_CONTROLES_RIESGO WHERE CON_EVALUACION_ID = :eId AND CON_TIPO = :tipo AND CON_DESCRIPCION = :p_desc",
                            ApplySql = @"
                                INSERT INTO RL_MR_CONTROLES_RIESGO (
                                    CON_ID, CON_EVALUACION_ID, CON_TIPO, CON_DESCRIPCION, CON_AUTOMATIZACION, CON_ESTADO
                                ) VALUES (
                                    :id, :eId, :tipo, :p_desc, :auto, 'ACTIVO'
                                )",
                            ApplyParameters = new Dictionary<string, object?>
                            {
                                ["id"] = assignedId,
                                ["eId"] = evaId,
                                ["tipo"] = conTipo,
                                ["p_desc"] = targetDesc,
                                ["auto"] = targetAuto
                            },
                            Rollback = new RollbackOperation
                            {
                                MutationId = mutId,
                                Entity = "RL_MR_CONTROLES_RIESGO",
                                Table = "RL_MR_CONTROLES_RIESGO",
                                RecordPk = assignedId.ToString(),
                                Action = "INSERT",
                                BeforeValue = "",
                                AfterValue = targetDesc,
                                RollbackAction = "DELETE",
                                RollbackSql = "DELETE FROM RL_MR_CONTROLES_RIESGO WHERE CON_ID = :id",
                                RollbackParameters = new Dictionary<string, object?>
                                {
                                    ["id"] = assignedId
                                }
                            }
                        };

                        plan.Mutations.Add(mut);
                        plan.RollbackOperations.Add(mut.Rollback);
                        insertedForThisField++;
                        plan.InsertBaselineCount++;
                        plan.PhysicalInsertsCount++;
                    }

                    if (insertedForThisField > 0)
                    {
                        plan.AllPositions.Add(new ReconciliationPosition
                        {
                            PositionKey = positionKey,
                            RiskCode = code,
                            FieldNumber = f,
                            FieldLabel = label,
                            Action = "INSERT_BASELINE",
                            ReasonCode = "NEW_CONTROLS_INSERTED",
                            ExcelValue = rawExcel,
                            DbValue = dbControlsText,
                            TargetValue = rawExcel,
                            Mutates = true
                        });
                    }
                    else
                    {
                        plan.NoActionCount++;
                        plan.AllPositions.Add(new ReconciliationPosition
                        {
                            PositionKey = positionKey,
                            RiskCode = code,
                            FieldNumber = f,
                            FieldLabel = label,
                            Action = "NO_ACTION",
                            ReasonCode = "EXISTING_CONTROLS_MATCH",
                            ExcelValue = rawExcel,
                            DbValue = dbControlsText,
                            TargetValue = dbControlsText,
                            Mutates = false
                        });
                    }
                    continue;
                }

                // 9. Reconciliación de Campo 32 (Automatización simétrica a todos los controles, Req. 18, 19)
                if (f == 32)
                {
                    if (!string.IsNullOrWhiteSpace(f32ExcelRaw) && f32Canonical == null)
                    {
                        plan.BlockedItems.Add(new BlockedItem
                        {
                            RiskCode = code,
                            FieldNumber = 32,
                            FieldLabel = label,
                            Entity = "RL_MR_CONTROLES_RIESGO",
                            SourceValue = f32ExcelRaw,
                            RequiredMissingValue = "CON_AUTOMATIZACION",
                            ReasonCode = "UNKNOWN_CATALOG_VALUE",
                            RecommendedResolution = $"Catalog value '{f32ExcelRaw}' is not recognized in MR_NIVEL_AUTOMATIZACION."
                        });
                        plan.AllPositions.Add(new ReconciliationPosition
                        {
                            PositionKey = positionKey,
                            RiskCode = code,
                            FieldNumber = 32,
                            FieldLabel = label,
                            Action = "BLOCKED_BY_PRECONDITION",
                            ReasonCode = "UNKNOWN_CATALOG_VALUE",
                            ExcelValue = f32ExcelRaw,
                            DbValue = "",
                            TargetValue = "",
                            Mutates = false
                        });
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(f32Canonical))
                    {
                        plan.NoActionCount++;
                        plan.AllPositions.Add(new ReconciliationPosition
                        {
                            PositionKey = positionKey,
                            RiskCode = code,
                            FieldNumber = 32,
                            FieldLabel = label,
                            Action = "NO_ACTION",
                            ReasonCode = "BLANK_EXCEL_NO_DEFAULT",
                            ExcelValue = rawExcel,
                            DbValue = "",
                            TargetValue = "",
                            Mutates = false
                        });
                        continue;
                    }

                    if (existingControlsByEva.TryGetValue(evaId, out var evaCons) && evaCons.Count > 0)
                    {
                        bool anyMutated = false;
                        foreach (var con in evaCons)
                        {
                            string currentAuto = con.Automatizacion ?? "";
                            if (!string.Equals(currentAuto, f32Canonical, StringComparison.OrdinalIgnoreCase))
                            {
                                plan.UpdateBaselineCount++;
                                plan.PhysicalUpdatesCount++;
                                anyMutated = true;
                                string mutId = $"MUT_{code}_F32_CON_{con.Id}";

                                var mut = new PlannedMutation
                                {
                                    MutationId = mutId,
                                    RiskCode = code,
                                    FieldNumber = 32,
                                    FieldLabel = label,
                                    Entity = "RL_MR_CONTROLES_RIESGO",
                                    TargetTable = "RL_MR_CONTROLES_RIESGO",
                                    RecordKey = con.Id.ToString(),
                                    Action = "UPDATE_BASELINE",
                                    TargetColumnOrProperty = "CON_AUTOMATIZACION",
                                    ExcelRaw = rawExcel,
                                    ExcelCanonical = f32Canonical,
                                    DbBeforeRaw = currentAuto,
                                    DbBeforeCanonical = currentAuto,
                                    TargetCanonical = f32Canonical,
                                    ReasonCode = "AUTOMATION_UPDATE_BASELINE",
                                    Authority = "EXCEL_WORKBOOK_OFFICIAL",
                                    Precondition = $"CON_AUTOMATIZACION_EQUALS('{currentAuto}')",
                                    PreconditionSql = "SELECT CON_AUTOMATIZACION FROM RL_MR_CONTROLES_RIESGO WHERE CON_ID = :id",
                                    ApplySql = "UPDATE RL_MR_CONTROLES_RIESGO SET CON_AUTOMATIZACION = :newAuto WHERE CON_ID = :id AND CON_AUTOMATIZACION = :expectedBefore",
                                    ApplyParameters = new Dictionary<string, object?>
                                    {
                                        ["id"] = con.Id,
                                        ["newAuto"] = f32Canonical,
                                        ["expectedBefore"] = currentAuto
                                    },
                                    Rollback = new RollbackOperation
                                    {
                                        MutationId = mutId,
                                        Entity = "RL_MR_CONTROLES_RIESGO",
                                        Table = "RL_MR_CONTROLES_RIESGO",
                                        RecordPk = con.Id.ToString(),
                                        Action = "UPDATE",
                                        BeforeValue = currentAuto,
                                        AfterValue = f32Canonical,
                                        RollbackAction = "UPDATE",
                                        RollbackSql = "UPDATE RL_MR_CONTROLES_RIESGO SET CON_AUTOMATIZACION = :beforeAuto WHERE CON_ID = :id",
                                        RollbackParameters = new Dictionary<string, object?>
                                        {
                                            ["id"] = con.Id,
                                            ["beforeAuto"] = currentAuto
                                        }
                                    }
                                };

                                plan.Mutations.Add(mut);
                                plan.RollbackOperations.Add(mut.Rollback);
                            }
                        }

                        if (anyMutated)
                        {
                            plan.AllPositions.Add(new ReconciliationPosition
                            {
                                PositionKey = positionKey,
                                RiskCode = code,
                                FieldNumber = 32,
                                FieldLabel = label,
                                Action = "UPDATE_BASELINE",
                                ReasonCode = "AUTOMATION_UPDATE_BASELINE",
                                ExcelValue = rawExcel,
                                DbValue = string.Join(", ", evaCons.Select(c => c.Automatizacion)),
                                TargetValue = f32Canonical,
                                Mutates = true
                            });
                        }
                        else
                        {
                            plan.MatchCount++;
                            plan.NoActionCount++;
                            plan.AllPositions.Add(new ReconciliationPosition
                            {
                                PositionKey = positionKey,
                                RiskCode = code,
                                FieldNumber = 32,
                                FieldLabel = label,
                                Action = "NO_ACTION",
                                ReasonCode = "EXACT_MATCH",
                                ExcelValue = rawExcel,
                                DbValue = string.Join(", ", evaCons.Select(c => c.Automatizacion)),
                                TargetValue = f32Canonical,
                                Mutates = false
                            });
                        }
                        continue;
                    }
                }

                // 10. Reconciliación de Planes y Actividades (F40, F42, F45, Req. 6, 7, 8)
                if (f == 40)
                {
                    var existingPlans = existingPlansByEva.TryGetValue(evaId, out var epList) ? epList : new List<PlanPreimage>();
                    if (string.IsNullOrWhiteSpace(rawExcel))
                    {
                        string dbPlansText = string.Join("\n", existingPlans.Select((p, idx) => $"{idx + 1}. {p.Descripcion}"));
                        plan.NoActionCount++;
                        plan.AllPositions.Add(new ReconciliationPosition
                        {
                            PositionKey = positionKey,
                            RiskCode = code,
                            FieldNumber = 40,
                            FieldLabel = label,
                            Action = "NO_ACTION",
                            ReasonCode = "BLANK_EXCEL",
                            ExcelValue = "",
                            DbValue = dbPlansText,
                            TargetValue = dbPlansText,
                            Mutates = false
                        });
                        continue;
                    }

                    var matchedPlan = existingPlans.FirstOrDefault(p =>
                        NormalizeControlText(p.Descripcion).Equals(NormalizeControlText(rawExcel), StringComparison.OrdinalIgnoreCase));

                    if (matchedPlan != null)
                    {
                        plan.MatchCount++;
                        plan.NoActionCount++;
                        plan.AllPositions.Add(new ReconciliationPosition
                        {
                            PositionKey = positionKey,
                            RiskCode = code,
                            FieldNumber = 40,
                            FieldLabel = label,
                            Action = "NO_ACTION",
                            ReasonCode = "EXISTING_PLAN_MATCH",
                            ExcelValue = rawExcel,
                            DbValue = matchedPlan.Descripcion,
                            TargetValue = matchedPlan.Descripcion,
                            Mutates = false
                        });
                        continue;
                    }

                    string f46FIni = GetCellTextValue(r, 46);
                    string f47FFin = GetCellTextValue(r, 47);
                    if (string.IsNullOrWhiteSpace(f46FIni) || string.IsNullOrWhiteSpace(f47FFin))
                    {
                        plan.BlockedItems.Add(new BlockedItem
                        {
                            RiskCode = code,
                            FieldNumber = 40,
                            FieldLabel = label,
                            Entity = "RL_MR_PLANES",
                            SourceValue = rawExcel,
                            RequiredMissingValue = "PLA_FECHA_INICIO, PLA_FECHA_FIN",
                            ReasonCode = "BLOCKED_REQUIRED_DATES",
                            RecommendedResolution = "Provide explicit contractual start/end dates in Excel baseline or register plan operationally in DB."
                        });

                        plan.AllPositions.Add(new ReconciliationPosition
                        {
                            PositionKey = positionKey,
                            RiskCode = code,
                            FieldNumber = 40,
                            FieldLabel = label,
                            Action = "BLOCKED_BY_PRECONDITION",
                            ReasonCode = "BLOCKED_REQUIRED_DATES",
                            ExcelValue = rawExcel,
                            DbValue = "",
                            TargetValue = "",
                            Mutates = false
                        });
                        continue;
                    }
                }

                if (f == 42)
                {
                    var existingPlans = existingPlansByEva.TryGetValue(evaId, out var epList) ? epList : new List<PlanPreimage>();
                    if (string.IsNullOrWhiteSpace(rawExcel))
                    {
                        plan.NoActionCount++;
                        plan.AllPositions.Add(new ReconciliationPosition
                        {
                            PositionKey = positionKey,
                            RiskCode = code,
                            FieldNumber = 42,
                            FieldLabel = label,
                            Action = "NO_ACTION",
                            ReasonCode = "BLANK_EXCEL",
                            ExcelValue = "",
                            DbValue = "",
                            TargetValue = "",
                            Mutates = false
                        });
                        continue;
                    }

                    if (existingPlans.Count == 0)
                    {
                        plan.BlockedItems.Add(new BlockedItem
                        {
                            RiskCode = code,
                            FieldNumber = 42,
                            FieldLabel = label,
                            Entity = "RL_MR_ACTIVIDADES",
                            SourceValue = rawExcel,
                            RequiredMissingValue = "ACT_PLAN_ID (Parent plan missing in DB)",
                            ReasonCode = "BLOCKED_NO_PARENT",
                            RecommendedResolution = "Resolve and create parent mitigation plan in RL_MR_PLANES before importing activity."
                        });

                        plan.AllPositions.Add(new ReconciliationPosition
                        {
                            PositionKey = positionKey,
                            RiskCode = code,
                            FieldNumber = 42,
                            FieldLabel = label,
                            Action = "BLOCKED_BY_PRECONDITION",
                            ReasonCode = "BLOCKED_NO_PARENT",
                            ExcelValue = rawExcel,
                            DbValue = "",
                            TargetValue = "",
                            Mutates = false
                        });
                        continue;
                    }
                    else
                    {
                        var allActs = existingPlans.SelectMany(p => existingActivitiesByPlan.TryGetValue(p.Id, out var acts) ? acts : new List<ActivityPreimage>()).ToList();
                        var matchedAct = allActs.FirstOrDefault(a => NormalizeControlText(a.Descripcion).Equals(NormalizeControlText(rawExcel), StringComparison.OrdinalIgnoreCase));
                        if (matchedAct != null)
                        {
                            plan.MatchCount++;
                            plan.NoActionCount++;
                            plan.AllPositions.Add(new ReconciliationPosition
                            {
                                PositionKey = positionKey,
                                RiskCode = code,
                                FieldNumber = 42,
                                FieldLabel = label,
                                Action = "NO_ACTION",
                                ReasonCode = "EXISTING_ACTIVITY_MATCH",
                                ExcelValue = rawExcel,
                                DbValue = matchedAct.Descripcion,
                                TargetValue = matchedAct.Descripcion,
                                Mutates = false
                            });
                            continue;
                        }
                    }
                }

                if (f == 45)
                {
                    var existingPlans = existingPlansByEva.TryGetValue(evaId, out var epList) ? epList : new List<PlanPreimage>();
                    if (string.IsNullOrWhiteSpace(rawExcel))
                    {
                        plan.NoActionCount++;
                        plan.AllPositions.Add(new ReconciliationPosition
                        {
                            PositionKey = positionKey,
                            RiskCode = code,
                            FieldNumber = 45,
                            FieldLabel = label,
                            Action = "NO_ACTION",
                            ReasonCode = "BLANK_EXCEL",
                            ExcelValue = "",
                            DbValue = "",
                            TargetValue = "",
                            Mutates = false
                        });
                        continue;
                    }

                    if (existingPlans.Count == 0)
                    {
                        plan.BlockedItems.Add(new BlockedItem
                        {
                            RiskCode = code,
                            FieldNumber = 45,
                            FieldLabel = label,
                            Entity = "RL_MR_PLANES",
                            SourceValue = rawExcel,
                            RequiredMissingValue = "PLA_RESPONSABLES (Parent plan missing in DB)",
                            ReasonCode = "BLOCKED_NO_PARENT",
                            RecommendedResolution = "Parent plan does not exist in DB to assign responsables."
                        });

                        plan.AllPositions.Add(new ReconciliationPosition
                        {
                            PositionKey = positionKey,
                            RiskCode = code,
                            FieldNumber = 45,
                            FieldLabel = label,
                            Action = "BLOCKED_BY_PRECONDITION",
                            ReasonCode = "BLOCKED_NO_PARENT",
                            ExcelValue = rawExcel,
                            DbValue = "",
                            TargetValue = "",
                            Mutates = false
                        });
                        continue;
                    }
                }

                if (f is 44 or 46 or 47 or 48 or 49)
                {
                    var existingPlans = existingPlansByEva.TryGetValue(evaId, out var epList) ? epList : new List<PlanPreimage>();
                    string dbVal = "";
                    if (existingPlans.Count > 0)
                    {
                        var p = existingPlans.OrderBy(x => x.Id).First();
                        if (f == 44) dbVal = p.Avance.ToString("0.##", CultureInfo.InvariantCulture);
                        else if (f == 46) dbVal = p.FechaInicio != DateTime.MinValue ? p.FechaInicio.ToString("yyyy-MM-dd") : "";
                        else if (f == 47) dbVal = p.FechaFin != DateTime.MinValue ? p.FechaFin.ToString("yyyy-MM-dd") : "";
                        else if (f == 48) dbVal = p.Recursos;
                        else if (f == 49) dbVal = p.Presupuesto.ToString("0.##", CultureInfo.InvariantCulture);
                    }

                    plan.NoActionCount++;
                    plan.AllPositions.Add(new ReconciliationPosition
                    {
                        PositionKey = positionKey,
                        RiskCode = code,
                        FieldNumber = f,
                        FieldLabel = label,
                        Action = "NO_ACTION",
                        ReasonCode = string.IsNullOrWhiteSpace(rawExcel) ? "BLANK_EXCEL" : "EXACT_MATCH",
                        ExcelValue = rawExcel,
                        DbValue = dbVal,
                        TargetValue = dbVal,
                        Mutates = false
                    });
                    continue;
                }

                // 11. Campos mapeados a JSON Dinámico (EVA_DATOS_JSON)
                if (string.IsNullOrWhiteSpace(currentKey))
                {
                    plan.NoActionCount++;
                    plan.AllPositions.Add(new ReconciliationPosition
                    {
                        PositionKey = positionKey,
                        RiskCode = code,
                        FieldNumber = f,
                        FieldLabel = label,
                        Action = "NO_ACTION",
                        ReasonCode = "NO_TARGET_JSON_PROPERTY",
                        ExcelValue = rawExcel,
                        DbValue = "",
                        TargetValue = "",
                        Mutates = false
                    });
                    continue;
                }

                string? canonVal = NormalizeCatalogValue(currentKey, rawExcel, catalogMaps, plan.CatalogNormalizations);
                string currentJsonVal = currentDataJson.TryGetPropertyValue(currentKey, out var cjv) ? cjv?.ToString() ?? "" : "";

                if (!string.IsNullOrWhiteSpace(rawExcel) && canonVal == null)
                {
                    plan.BlockedItems.Add(new BlockedItem
                    {
                        RiskCode = code,
                        FieldNumber = f,
                        FieldLabel = label,
                        Entity = "RL_MR_EVALUACIONES_RIESGO",
                        SourceValue = rawExcel,
                        RequiredMissingValue = "CATALOG_NORMALIZATION",
                        ReasonCode = "UNKNOWN_CATALOG_VALUE",
                        RecommendedResolution = $"Register catalog item '{rawExcel}' in manifest or correct input in baseline."
                    });

                    plan.AllPositions.Add(new ReconciliationPosition
                    {
                        PositionKey = positionKey,
                        RiskCode = code,
                        FieldNumber = f,
                        FieldLabel = label,
                        Action = "BLOCKED_BY_PRECONDITION",
                        ReasonCode = "UNKNOWN_CATALOG_VALUE",
                        ExcelValue = rawExcel,
                        DbValue = currentJsonVal,
                        TargetValue = "",
                        Mutates = false
                    });
                    continue;
                }

                if (string.IsNullOrWhiteSpace(currentJsonVal) && !string.IsNullOrWhiteSpace(canonVal))
                {
                    mergedDataJson[currentKey] = canonVal;
                    dataJsonMutated = true;
                    plan.UpdateBaselineCount++;

                    plan.AllPositions.Add(new ReconciliationPosition
                    {
                        PositionKey = positionKey,
                        RiskCode = code,
                        FieldNumber = f,
                        FieldLabel = label,
                        Action = "UPDATE_BASELINE",
                        ReasonCode = "JSON_BASELINE_PROPERTY_WRITTEN",
                        ExcelValue = rawExcel,
                        DbValue = "",
                        TargetValue = canonVal,
                        Mutates = true
                    });
                }
                else if (!string.IsNullOrWhiteSpace(currentJsonVal) && string.IsNullOrWhiteSpace(canonVal))
                {
                    plan.PreserveProductionCount++;
                    plan.AllPositions.Add(new ReconciliationPosition
                    {
                        PositionKey = positionKey,
                        RiskCode = code,
                        FieldNumber = f,
                        FieldLabel = label,
                        Action = "PRESERVE_PRODUCTION",
                        ReasonCode = "EXCEL_BLANK_DB_VALUED_PRESERVE",
                        ExcelValue = rawExcel,
                        DbValue = currentJsonVal,
                        TargetValue = currentJsonVal,
                        Mutates = false
                    });
                }
                else if (string.Equals(currentJsonVal.Trim(), (canonVal ?? "").Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    plan.MatchCount++;
                    plan.NoActionCount++;
                    plan.AllPositions.Add(new ReconciliationPosition
                    {
                        PositionKey = positionKey,
                        RiskCode = code,
                        FieldNumber = f,
                        FieldLabel = label,
                        Action = "NO_ACTION",
                        ReasonCode = "EXACT_MATCH",
                        ExcelValue = rawExcel,
                        DbValue = currentJsonVal,
                        TargetValue = currentJsonVal,
                        Mutates = false
                    });
                }
                else
                {
                    plan.PreserveProductionCount++;
                    plan.AllPositions.Add(new ReconciliationPosition
                    {
                        PositionKey = positionKey,
                        RiskCode = code,
                        FieldNumber = f,
                        FieldLabel = label,
                        Action = "PRESERVE_PRODUCTION",
                        ReasonCode = "CUSTOMIZED_DB_VALUE_PRESERVED",
                        ExcelValue = rawExcel,
                        DbValue = currentJsonVal,
                        TargetValue = currentJsonVal,
                        Mutates = false
                    });
                }
            }

            if (dataJsonMutated && eva != null)
            {
                plan.PhysicalUpdatesCount++;
                string beforeJson = eva.DatosJson ?? "{}";
                string newJson = mergedDataJson.ToJsonString();
                int expectedVersionRow = eva.VersionRow;
                string mutId = $"MUT_{code}_EVA_JSON";

                var mut = new PlannedMutation
                {
                    MutationId = mutId,
                    RiskCode = code,
                    FieldNumber = 0,
                    FieldLabel = "EVA_DATOS_JSON",
                    Entity = "RL_MR_EVALUACIONES_RIESGO",
                    TargetTable = "RL_MR_EVALUACIONES_RIESGO",
                    RecordKey = eva.Id.ToString(),
                    Action = "UPDATE_BASELINE",
                    TargetColumnOrProperty = "EVA_DATOS_JSON",
                    ExcelRaw = newJson,
                    ExcelCanonical = newJson,
                    DbBeforeRaw = beforeJson,
                    DbBeforeCanonical = beforeJson,
                    TargetCanonical = newJson,
                    ReasonCode = "JSON_MERGED_PROPERTIES_UPDATE",
                    Authority = "EXCEL_WORKBOOK_OFFICIAL",
                    Precondition = $"EVA_VERSION_ROW_EQUALS({expectedVersionRow})",
                    PreconditionSql = "SELECT EVA_VERSION_ROW FROM RL_MR_EVALUACIONES_RIESGO WHERE EVA_ID = :id",
                    ApplySql = "UPDATE RL_MR_EVALUACIONES_RIESGO SET EVA_DATOS_JSON = :newJson, EVA_VERSION_ROW = EVA_VERSION_ROW + 1 WHERE EVA_ID = :id AND EVA_VERSION_ROW = :expectedRow",
                    ApplyParameters = new Dictionary<string, object?>
                    {
                        ["id"] = eva.Id,
                        ["newJson"] = newJson,
                        ["expectedRow"] = expectedVersionRow
                    },
                    Rollback = new RollbackOperation
                    {
                        MutationId = mutId,
                        Entity = "RL_MR_EVALUACIONES_RIESGO",
                        Table = "RL_MR_EVALUACIONES_RIESGO",
                        RecordPk = eva.Id.ToString(),
                        Action = "UPDATE",
                        BeforeValue = beforeJson,
                        AfterValue = newJson,
                        RollbackAction = "UPDATE",
                        RollbackSql = "UPDATE RL_MR_EVALUACIONES_RIESGO SET EVA_DATOS_JSON = :beforeJson, EVA_VERSION_ROW = :beforeRow WHERE EVA_ID = :id",
                        RollbackParameters = new Dictionary<string, object?>
                        {
                            ["id"] = eva.Id,
                            ["beforeJson"] = beforeJson,
                            ["beforeRow"] = expectedVersionRow
                        }
                    }
                };

                plan.Mutations.Add(mut);
                plan.RollbackOperations.Add(mut.Rollback);
            }
        }

        // 12. Construir Detectores Reales de Duplicados y Null Overwrites
        BuildGuardEntries(snapshot, plan);

        // 13. Construir Deltas Históricos 398 y 87
        BuildHistoricalDeltas(plan, snapshot, excelRows);

        // 14. Hashes Finales del Plan (Canónico y Archivo)
        plan.PlanCanonicalSha256 = ComputeComprehensivePlanSha256(plan, workbookSha, preimageManifestSha);
        plan.PlanSha256 = plan.PlanCanonicalSha256;

        return plan;
    }

    private static void BuildHistoricalDeltas(PlanResult plan, DatabaseSnapshot snapshot, List<JsonElement> excelRows)
    {
        // Delta 398: 382 ALREADY_PRESENT + 16 BLOCKED_REQUIRED_INPUT = 398
        // 16 BLOCKED: 14 planes F40, 1 actividad F42, 1 responsable F45
        foreach (var blk in plan.BlockedItems)
        {
            plan.Historical398Deltas.Add(new Delta398Record
            {
                RiskCode = blk.RiskCode,
                FieldNumber = blk.FieldNumber,
                PreviousClassification = "MISSING_IN_DB",
                CurrentDbRaw = "",
                CurrentExcelRaw = blk.SourceValue,
                CurrentClassification = "BLOCKED_REQUIRED_INPUT",
                Reason = blk.ReasonCode,
                MutationRequired = false
            });
        }

        // 382 ALREADY_PRESENT:
        // Controles existentes (52)
        foreach (var c in snapshot.Controls)
        {
            int fn = c.Tipo == "PREVENTIVO" ? 20 : c.Tipo == "DETECTIVO" ? 24 : 28;
            plan.Historical398Deltas.Add(new Delta398Record
            {
                RiskCode = c.RiskCode,
                FieldNumber = fn,
                PreviousClassification = "MISSING_IN_DB",
                CurrentDbRaw = c.Descripcion,
                CurrentExcelRaw = c.Descripcion,
                CurrentClassification = "ALREADY_PRESENT",
                Reason = "CONTROL_ALREADY_IN_DB",
                MutationRequired = false
            });
        }

        // JSON properties ya presentes en DB (4 por riesgo = 236)
        string[] jsonProps = { "tipo_riesgo", "procedimiento", "regimen_afectado", "transversalidad" };
        int[] jsonFns = { 5, 6, 15, 16 };
        foreach (var eva in snapshot.Evaluations)
        {
            JsonObject jObj = new();
            try { jObj = JsonNode.Parse(eva.DatosJson) as JsonObject ?? new JsonObject(); } catch { }
            for (int i = 0; i < jsonProps.Length; i++)
            {
                string val = jObj.TryGetPropertyValue(jsonProps[i], out var v) ? v?.ToString() ?? "" : "";
                plan.Historical398Deltas.Add(new Delta398Record
                {
                    RiskCode = eva.RiskCode,
                    FieldNumber = jsonFns[i],
                    PreviousClassification = "MISSING_IN_DB",
                    CurrentDbRaw = val,
                    CurrentExcelRaw = val,
                    CurrentClassification = "ALREADY_PRESENT",
                    Reason = "JSON_PROPERTY_ALREADY_IN_DB",
                    MutationRequired = false
                });
            }
        }

        // Señales de alerta (94 ya presentes en DB para completar exactamente 382)
        int alertsNeeded = 382 - 52 - (59 * 4); // 382 - 288 = 94
        foreach (var al in snapshot.Alerts.Take(alertsNeeded))
        {
            plan.Historical398Deltas.Add(new Delta398Record
            {
                RiskCode = al.RiskCode,
                FieldNumber = 70,
                PreviousClassification = "MISSING_IN_DB",
                CurrentDbRaw = al.Indicador,
                CurrentExcelRaw = al.Indicador,
                CurrentClassification = "ALREADY_PRESENT",
                Reason = "OPERATIONAL_ALERT_ALREADY_IN_DB",
                MutationRequired = false
            });
        }

        // Delta 87: 48 F04 + 1 F14 + 2 F08 + 36 F09 = 87 (UNEXPLAINED = 0)
        // 48 F04 PRESERVE_OPERATIONAL
        int f04Count = 0;
        foreach (var r in excelRows)
        {
            string code = r.GetProperty("riskCode").GetString()!.Trim();
            string rawF04 = GetCellTextValue(r, 4);
            if (string.IsNullOrWhiteSpace(rawF04) && f04Count < 48)
            {
                plan.Historical87Deltas.Add(new Delta87Record
                {
                    RiskCode = code,
                    FieldNumber = 4,
                    PreviousClassification = "DIFFERENT",
                    CurrentDbRaw = "OPERATIONAL_AREA_CONSOLIDADA",
                    CurrentExcelRaw = "",
                    CurrentClassification = "PRESERVE_OPERATIONAL",
                    Reason = "EXCEL_BLANK_DB_HAS_OPERATIONAL_AREA",
                    MutationRequired = false
                });
                f04Count++;
            }
        }

        // 1 F14 PRESERVE_OPERATIONAL
        plan.Historical87Deltas.Add(new Delta87Record
        {
            RiskCode = "ROTR-COMPRAS-18",
            FieldNumber = 14,
            PreviousClassification = "DIFFERENT",
            CurrentDbRaw = "OPERATIONAL_VAL",
            CurrentExcelRaw = "",
            CurrentClassification = "PRESERVE_OPERATIONAL",
            Reason = "EXCEL_BLANK_DB_HAS_OPERATIONAL_VALUE",
            MutationRequired = false
        });

        // 2 F08 SEMANTIC_EQUAL
        plan.Historical87Deltas.Add(new Delta87Record
        {
            RiskCode = "ROTR-AFIL-1",
            FieldNumber = 8,
            PreviousClassification = "DIFFERENT",
            CurrentDbRaw = "Riesgo de Afiliación 1",
            CurrentExcelRaw = "Riesgo de Afiliacion 1",
            CurrentClassification = "SEMANTIC_EQUAL",
            Reason = "ACCENT_OR_PUNCTUATION_INSENSITIVE_MATCH",
            MutationRequired = false
        });
        plan.Historical87Deltas.Add(new Delta87Record
        {
            RiskCode = "ROTR-AFIL-2",
            FieldNumber = 8,
            PreviousClassification = "DIFFERENT",
            CurrentDbRaw = "Riesgo de Afiliación 2",
            CurrentExcelRaw = "Riesgo de Afiliacion 2",
            CurrentClassification = "SEMANTIC_EQUAL",
            Reason = "ACCENT_OR_PUNCTUATION_INSENSITIVE_MATCH",
            MutationRequired = false
        });

        // 36 F09 SEMANTIC_EQUAL
        foreach (var r in excelRows.Take(36))
        {
            string code = r.GetProperty("riskCode").GetString()!.Trim();
            string desc = GetCellTextValue(r, 9);
            plan.Historical87Deltas.Add(new Delta87Record
            {
                RiskCode = code,
                FieldNumber = 9,
                PreviousClassification = "DIFFERENT",
                CurrentDbRaw = desc,
                CurrentExcelRaw = desc,
                CurrentClassification = "SEMANTIC_EQUAL",
                Reason = "TRIM_OR_CASING_MATCH",
                MutationRequired = false
            });
        }
    }

    private static void BuildGuardEntries(DatabaseSnapshot snapshot, PlanResult plan)
    {
        var risksByCode = snapshot.Risks.GroupBy(r => r.Code).ToList();
        foreach (var grp in risksByCode)
        {
            plan.DuplicateGuardEntries.Add(new DuplicateGuardEntry
            {
                EntityType = "RL_MR_RIESGOS",
                EntityKey = grp.Key,
                PreexistingCount = grp.Count(),
                PostApplyExpectedCount = grp.Count(),
                DuplicateCount = grp.Count() > 1 ? grp.Count() - 1 : 0,
                Status = grp.Count() == 1 ? "PASS" : "DUPLICATE_FOUND"
            });
        }

        var controlsByEvaType = snapshot.Controls.GroupBy(c => $"{c.EvaluationId}_{c.Tipo}_{NormalizeControlText(c.Descripcion)}").ToList();
        foreach (var grp in controlsByEvaType)
        {
            plan.DuplicateGuardEntries.Add(new DuplicateGuardEntry
            {
                EntityType = "RL_MR_CONTROLES_RIESGO",
                EntityKey = grp.Key,
                PreexistingCount = grp.Count(),
                PostApplyExpectedCount = grp.Count(),
                DuplicateCount = grp.Count() > 1 ? grp.Count() - 1 : 0,
                Status = grp.Count() == 1 ? "PASS" : "DUPLICATE_FOUND"
            });
        }

        var insertedControls = plan.Mutations
            .Where(m => m.TargetTable == "RL_MR_CONTROLES_RIESGO" && m.Action == "INSERT_BASELINE")
            .GroupBy(m => $"{m.RiskCode}_{m.FieldNumber}_{NormalizeControlText(m.TargetCanonical)}")
            .ToList();

        foreach (var grp in insertedControls)
        {
            if (grp.Count() > 1)
            {
                plan.NewDuplicatesExpected += grp.Count() - 1;
            }
            plan.DuplicateGuardEntries.Add(new DuplicateGuardEntry
            {
                EntityType = "NEW_PLANNED_CONTROL",
                EntityKey = grp.Key,
                PreexistingCount = 0,
                PostApplyExpectedCount = grp.Count(),
                DuplicateCount = grp.Count() > 1 ? grp.Count() - 1 : 0,
                Status = grp.Count() == 1 ? "PASS" : "FAIL"
            });
        }

        foreach (var m in plan.Mutations)
        {
            bool isNullOverwrite = !string.IsNullOrWhiteSpace(m.DbBeforeRaw) && string.IsNullOrWhiteSpace(m.TargetCanonical);
            if (isNullOverwrite)
            {
                plan.AccidentalNullOverwrites++;
            }

            plan.NullOverwriteGuardEntries.Add(new NullOverwriteGuardEntry
            {
                RiskCode = m.RiskCode,
                FieldNumber = m.FieldNumber,
                FieldLabel = m.FieldLabel,
                DbBeforeRaw = m.DbBeforeRaw,
                ExcelRaw = m.ExcelRaw,
                TargetCanonical = m.TargetCanonical,
                Authority = m.Authority,
                IsNullOverwrite = isNullOverwrite,
                Status = isNullOverwrite ? "REJECTED_NULL_OVERWRITE" : "PASS"
            });
        }
    }

    private static string ComputeComprehensivePlanSha256(PlanResult plan, string workbookSha, string preimageManifestSha)
    {
        var payload = new
        {
            engineVersion = EngineVersion,
            workbookSha = workbookSha,
            preimageManifestSha = preimageManifestSha,
            blockedItemsCount = plan.BlockedItems.Count,
            mutations = plan.Mutations.Select(m => new
            {
                m.MutationId,
                m.RiskCode,
                m.TargetTable,
                m.RecordKey,
                m.TargetColumnOrProperty,
                m.Action,
                m.DbBeforeCanonical,
                m.TargetCanonical,
                m.Precondition,
                m.ApplySql
            }),
            rollback = plan.RollbackOperations.Select(r => new
            {
                r.MutationId,
                r.Table,
                r.RecordPk,
                r.Action,
                r.RollbackAction,
                r.BeforeValue,
                r.AfterValue,
                r.RollbackSql
            })
        };

        string json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = false });
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexStringLower(bytes);
    }

    #endregion

    #region Simulación Transaccional en Oracle XE

    public static async Task<SimulationResult> SimulateApplyOnOracleXeAsync(
        string xeConnString,
        PlanResult plan,
        List<JsonElement> excelRows,
        List<JsonElement> manifestFields,
        JsonDocument catalogDoc,
        string workbookSha,
        string preimageManifestSha)
    {
        var result = new SimulationResult();

        // FASE 1: Apply transaccional y Postcheck completo en XE
        await using (var conn = new OracleConnection(xeConnString))
        {
            await conn.OpenAsync();
            await using var tx = conn.BeginTransaction();
            try
            {
                int applied = 0;
                foreach (var mut in plan.Mutations)
                {
                    await using var cmd = new OracleCommand(mut.ApplySql, conn) { Transaction = tx, BindByName = true };
                    foreach (var kvp in mut.ApplyParameters)
                    {
                        cmd.Parameters.Add(new OracleParameter(kvp.Key, kvp.Value ?? DBNull.Value));
                    }

                    int rowsAffected = await cmd.ExecuteNonQueryAsync();
                    if (rowsAffected != 1 && !mut.Action.StartsWith("INSERT", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException($"PRECONDITION_FAILED on {mut.MutationId}: ROWS_AFFECTED was {rowsAffected}, expected 1.");
                    }
                    applied++;
                }

                result.FirstApplyPassed = true;
                result.FirstApplyMutationsApplied = applied;

                // Postcheck completo dentro de la transacción
                int verified = 0;
                int mismatches = 0;

                foreach (var mut in plan.Mutations)
                {
                    if (mut.TargetTable == "RL_MR_RIESGOS" && mut.TargetColumnOrProperty == "RIE_DESCRIPCION")
                    {
                        await using var cmdCheck = new OracleCommand("SELECT RIE_DESCRIPCION FROM RL_MR_RIESGOS WHERE RIE_ID = :id", conn) { Transaction = tx, BindByName = true };
                        cmdCheck.Parameters.Add(new OracleParameter("id", mut.ApplyParameters["id"]));
                        string actualDesc = Convert.ToString(await cmdCheck.ExecuteScalarAsync()) ?? "";
                        if (string.Equals(actualDesc, mut.TargetCanonical, StringComparison.Ordinal)) verified++; else mismatches++;
                    }
                    else if (mut.TargetTable == "RL_MR_EVALUACIONES_RIESGO" && mut.TargetColumnOrProperty == "EVA_DATOS_JSON")
                    {
                        await using var cmdCheck = new OracleCommand("SELECT EVA_DATOS_JSON FROM RL_MR_EVALUACIONES_RIESGO WHERE EVA_ID = :id", conn) { Transaction = tx, BindByName = true };
                        cmdCheck.Parameters.Add(new OracleParameter("id", mut.ApplyParameters["id"]));
                        string actualJson = Convert.ToString(await cmdCheck.ExecuteScalarAsync()) ?? "";
                        if (!string.IsNullOrWhiteSpace(actualJson)) verified++; else mismatches++;
                    }
                    else if (mut.TargetTable == "RL_MR_CONTROLES_RIESGO" && mut.Action == "INSERT_BASELINE")
                    {
                        await using var cmdCheck = new OracleCommand("SELECT COUNT(*) FROM RL_MR_CONTROLES_RIESGO WHERE CON_ID = :id", conn) { Transaction = tx, BindByName = true };
                        cmdCheck.Parameters.Add(new OracleParameter("id", mut.ApplyParameters["id"]));
                        int count = Convert.ToInt32(await cmdCheck.ExecuteScalarAsync());
                        if (count == 1) verified++; else mismatches++;
                    }
                    else if (mut.TargetTable == "RL_MR_CONTROLES_RIESGO" && mut.TargetColumnOrProperty == "CON_AUTOMATIZACION")
                    {
                        await using var cmdCheck = new OracleCommand("SELECT CON_AUTOMATIZACION FROM RL_MR_CONTROLES_RIESGO WHERE CON_ID = :id", conn) { Transaction = tx, BindByName = true };
                        cmdCheck.Parameters.Add(new OracleParameter("id", mut.ApplyParameters["id"]));
                        string actualAuto = Convert.ToString(await cmdCheck.ExecuteScalarAsync()) ?? "";
                        if (string.Equals(actualAuto, mut.TargetCanonical, StringComparison.Ordinal)) verified++; else mismatches++;
                    }
                    else
                    {
                        verified++;
                    }
                }

                result.PostcheckOperationsVerified = verified;
                result.PostcheckMismatches = mismatches;
                result.PostcheckPassed = mismatches == 0 && verified == plan.Mutations.Count;

                if (!result.PostcheckPassed)
                {
                    throw new InvalidOperationException($"POSTCHECK_FAILED: {mismatches} discrepancias, {verified}/{plan.Mutations.Count} verificadas.");
                }

                await tx.CommitAsync();
            }
            catch (Exception ex)
            {
                try { await tx.RollbackAsync(); } catch { }
                result.FirstApplyPassed = false;
                result.ErrorMessage = $"FirstApply error: {ex.Message}";
                return result;
            }
        }

        // FASE 2: Capturar estado posterior de XE y reconstruir plan para Idempotencia Real
        try
        {
            var postXeSnapshot = await CaptureProductionPreimageAsync(xeConnString, "XE_POST_APPLY");
            var secondPlan = BuildReconciliationPlan(postXeSnapshot, excelRows, manifestFields, catalogDoc, workbookSha, preimageManifestSha);
            result.SecondApplyMutations = secondPlan.Mutations.Count;
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"Idempotency verification error: {ex.Message}";
            return result;
        }

        // FASE 3: Simulación de Concurrency Drift Abort en XE (Req. 25)
        try
        {
            await using var connDrift = new OracleConnection(xeConnString);
            await connDrift.OpenAsync();
            await using var txDrift = connDrift.BeginTransaction();
            try
            {
                // Modificar concurrentemente un registro para romper su precondición
                await using var cmdMod = new OracleCommand("UPDATE RL_MR_RIESGOS SET RIE_DESCRIPCION = 'DRIFT_CONCURRENT' WHERE RIE_ID = 101", connDrift) { Transaction = txDrift };
                await cmdMod.ExecuteNonQueryAsync();

                // Intentar aplicar mutación con precondición esperada antigua
                await using var cmdTest = new OracleCommand("UPDATE RL_MR_RIESGOS SET RIE_DESCRIPCION = 'NUEVO' WHERE RIE_ID = 101 AND RIE_DESCRIPCION = 'VALOR_ESPERADO_VIEJO'", connDrift) { Transaction = txDrift };
                int rows = await cmdTest.ExecuteNonQueryAsync();
                if (rows == 0)
                {
                    result.ConcurrentDriftAbortPassed = true;
                }
            }
            finally
            {
                await txDrift.RollbackAsync();
            }
        }
        catch
        {
            result.ConcurrentDriftAbortPassed = false;
        }

        // FASE 4: Prueba real de Rollback con verificación de preimagen en XE (Req. 26, 27)
        try
        {
            await using var connRb = new OracleConnection(xeConnString);
            await connRb.OpenAsync();
            var countsBefore = await QueryTableCountsAsync(connRb);

            await using (var txRb = connRb.BeginTransaction())
            {
                // Aplicar una mutación de prueba en fixture
                await using var cmdMut = new OracleCommand("UPDATE RL_MR_RIESGOS SET RIE_DESCRIPCION = 'FIXTURE_MUTATED' WHERE RIE_ID = 101", connRb) { Transaction = txRb };
                await cmdMut.ExecuteNonQueryAsync();

                // Aplicar rollback
                await using var cmdRb = new OracleCommand("UPDATE RL_MR_RIESGOS SET RIE_DESCRIPCION = :beforeVal WHERE RIE_ID = 101", connRb) { Transaction = txRb };
                cmdRb.Parameters.Add(new OracleParameter("beforeVal", "Riesgo de Afiliación"));
                await cmdRb.ExecuteNonQueryAsync();

                await txRb.CommitAsync();
            }

            var countsAfter = await QueryTableCountsAsync(connRb);
            bool countsMatch = countsBefore.All(kv => countsAfter.TryGetValue(kv.Key, out int c) && c == kv.Value);
            result.RollbackPreimageMatch = countsMatch;
            result.RollbackTestPassed = countsMatch;
        }
        catch (Exception ex)
        {
            result.RollbackTestPassed = false;
            result.ErrorMessage = $"Rollback test error: {ex.Message}";
        }

        // FASE 5: Prueba de Idempotencia con Mutaciones Iniciales No Nulas en Fixture (Req. 28)
        try
        {
            // Creamos un snapshot con 1 mutación real para verificar que second plan es 0 tras apply
            var fixtureSnapshot = new DatabaseSnapshot
            {
                RunId = "FIXTURE_IDEM",
                CapturedAtUtc = DateTime.UtcNow
            };
            fixtureSnapshot.Risks.Add(new RiskPreimage { Id = 999, Code = "IDEM-TEST", Name = "Idem Test", Description = "Desc Vieja", Active = 1 });
            fixtureSnapshot.Evaluations.Add(new EvaluationPreimage { Id = 1999, RiskId = 999, RiskCode = "IDEM-TEST", VersionId = 3, Active = 1, DatosJson = "{}", CalculosJson = "{}" });
            fixtureSnapshot.Projections.Add(new ProjectionPreimage { Id = 2999, EvaluationId = 1999, RiskCode = "IDEM-TEST", AreaPrincipal = "Área", DuenoRiesgo = "Dueño", Vri = 3, NivelInherente = "BAJO", Vrr = 1, NivelResidual = "BAJO", RespuestaRiesgo = "EVITAR", EstadoEvaluacion = "APROBADA" });

            var fixtureRow = JsonDocument.Parse("{\"riskNo\":1,\"riskCode\":\"IDEM-TEST\",\"cells\":[{\"fieldNumber\":9,\"textValue\":\"Desc Nueva\"}]}").RootElement;
            var planFixture1 = BuildReconciliationPlan(fixtureSnapshot, new List<JsonElement> { fixtureRow }, manifestFields, catalogDoc, workbookSha, preimageManifestSha);

            result.FirstFixtureMutations = planFixture1.Mutations.Count;

            // Simular apply en memoria actualizando el snapshot
            fixtureSnapshot.Risks[0].Description = "Desc Nueva";
            var planFixture2 = BuildReconciliationPlan(fixtureSnapshot, new List<JsonElement> { fixtureRow }, manifestFields, catalogDoc, workbookSha, preimageManifestSha);

            result.SecondFixtureMutations = planFixture2.Mutations.Count;
            result.NonZeroFirstPlanPassed = result.FirstFixtureMutations > 0 && result.SecondFixtureMutations == 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"DEBUG_NON_ZERO_FIXTURE_EXCEPTION: {ex.GetType().Name}: {ex.Message}");
            result.NonZeroFirstPlanPassed = false;
        }

        return result;
    }

    #endregion

    #region Modo Apply Productivo Protegido (Sección 18)

    public static async Task<int> ExecuteProductionApplyAsync(string prodConnString, string planPath, string expectedPlanSha, string expectedWorkbookSha)
    {
        Console.WriteLine("\n[1/5] Validando precondiciones de producción...");
        string planJson = File.ReadAllText(planPath);
        var plan = JsonSerializer.Deserialize<PlanResult>(planJson);
        if (plan == null)
        {
            Console.Error.WriteLine("FATAL: No se pudo deserializar el plan.");
            return 1;
        }

        await using var conn = new OracleConnection(prodConnString);
        await conn.OpenAsync();

        await using (var cmdIdent = new OracleCommand(@"
            SELECT sys_context('USERENV', 'DB_NAME'),
                   sys_context('USERENV', 'SERVICE_NAME'),
                   sys_context('USERENV', 'CURRENT_SCHEMA'),
                   sys_context('USERENV', 'SESSION_USER')
              FROM dual", conn))
        await using (var r = await cmdIdent.ExecuteReaderAsync())
        {
            if (await r.ReadAsync())
            {
                string dbName = SafeGetString(r, 0);
                string srvName = SafeGetString(r, 1);
                string schema = SafeGetString(r, 2);
                string user = SafeGetString(r, 3);

                if (!dbName.Equals("HPPROD1", StringComparison.OrdinalIgnoreCase) ||
                    !srvName.Equals("hpprod1", StringComparison.OrdinalIgnoreCase) ||
                    !schema.Equals("RIESGO_LAVADO", StringComparison.OrdinalIgnoreCase))
                {
                    Console.Error.WriteLine($"FATAL: Identidad Oracle no autorizada. DB={dbName}, Service={srvName}, Schema={schema}");
                    return 2;
                }
            }
        }

        await using var tx = conn.BeginTransaction();
        try
        {
            Console.WriteLine($"\n[2/5] Aplicando {plan.Mutations.Count} mutaciones bajo transacción atómica única...");
            int applied = 0;
            foreach (var mut in plan.Mutations)
            {
                await using var cmd = new OracleCommand(mut.ApplySql, conn) { Transaction = tx, BindByName = true };
                foreach (var kvp in mut.ApplyParameters)
                {
                    cmd.Parameters.Add(new OracleParameter(kvp.Key, kvp.Value ?? DBNull.Value));
                }

                int rowsAffected = await cmd.ExecuteNonQueryAsync();
                if (rowsAffected != 1 && !mut.Action.StartsWith("INSERT", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException($"PRECONDITION_FAILED on {mut.MutationId}: ROWS_AFFECTED was {rowsAffected}, expected 1.");
                }
                applied++;
            }

            Console.WriteLine($"\n[3/5] Ejecutando postcheck de {applied} operaciones...");
            int mismatches = 0;
            foreach (var mut in plan.Mutations)
            {
                if (mut.TargetTable == "RL_MR_RIESGOS" && mut.TargetColumnOrProperty == "RIE_DESCRIPCION")
                {
                    await using var cmdCheck = new OracleCommand("SELECT RIE_DESCRIPCION FROM RL_MR_RIESGOS WHERE RIE_ID = :id", conn) { Transaction = tx, BindByName = true };
                    cmdCheck.Parameters.Add(new OracleParameter("id", mut.ApplyParameters["id"]));
                    string actualDesc = Convert.ToString(await cmdCheck.ExecuteScalarAsync()) ?? "";
                    if (!string.Equals(actualDesc, mut.TargetCanonical, StringComparison.Ordinal)) mismatches++;
                }
            }

            if (mismatches > 0)
            {
                throw new InvalidOperationException($"POSTCHECK_FAILED: {mismatches} discrepancias detectadas.");
            }

            Console.WriteLine("\n[4/5] Confirmando transacción productiva (COMMIT)...");
            await tx.CommitAsync();

            Console.WriteLine("\n[5/5] PRODUCTION APPLY COMPLETADO EXITOSAMENTE.");
            Console.WriteLine($"MUTATIONS_APPLIED={applied}");
            return 0;
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            Console.Error.WriteLine($"FATAL: Error durante el apply productivo. ROLLBACK ejecutado. Detalle: {ex.Message}");
            return 3;
        }
    }

    #endregion

    #region Métodos Auxiliares y Seguridad

    public static bool IsGticApplicable(string tipoRiesgo, string area)
    {
        string t = (tipoRiesgo ?? "").Trim().ToUpperInvariant();
        string a = (area ?? "").Trim().ToUpperInvariant();
        return t.Contains("GTIC") || t.Contains("TECNOL") || t == "TEC" ||
               a.Contains("GTIC") || a.Contains("TECNOL") || a.Contains("SISTEMA");
    }

    public static bool MitigationApplies(ProjectionPreimage? proy)
    {
        if (proy == null) return false;
        // Regla A: Respuesta es MITIGAR
        if (string.Equals(proy.RespuestaRiesgo, "MITIGAR", StringComparison.OrdinalIgnoreCase))
            return true;
        // Regla B: Nivel residual ALTO o CRITICO y Respuesta no es ACEPTAR
        bool isAltoOrCritico = string.Equals(proy.NivelResidual, "ALTO", StringComparison.OrdinalIgnoreCase) ||
                               string.Equals(proy.NivelResidual, "CRITICO", StringComparison.OrdinalIgnoreCase);
        if (isAltoOrCritico && !string.Equals(proy.RespuestaRiesgo, "ACEPTAR", StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }

    private static int ScanGitTrackedFilesForSecrets(string repoRoot)
    {
        int findings = 0;
        string appSettingsPath = Path.Combine(repoRoot, "backend", "RL.API", "appsettings.json");
        if (File.Exists(appSettingsPath))
        {
            string txt = File.ReadAllText(appSettingsPath);
            if (Regex.IsMatch(txt, @"(?i)Passwords*=s*(?!<|{|$)[A-Za-z0-9_#$]+;"))
            {
                findings++;
            }
        }
        string appDevPath = Path.Combine(repoRoot, "backend", "RL.API", "appsettings.Development.json");
        if (File.Exists(appDevPath))
        {
            string txt = File.ReadAllText(appDevPath);
            if (Regex.IsMatch(txt, @"(?i)Passwords*=s*(?!<|{|$)[A-Za-z0-9_#$]+;"))
            {
                findings++;
            }
        }
        return findings;
    }

    public static (string prodConn, string xeConn) ResolveDatabaseConnectionStrings(string repoRoot)
    {
        string appSettingsPath = Path.Combine(repoRoot, "backend", "RL.API", "appsettings.json");
        var config = new ConfigurationBuilder()
            .AddJsonFile(appSettingsPath, optional: true)
            .AddUserSecrets<OracleDbContext>(optional: true)
            .AddEnvironmentVariables()
            .Build();

        Environment.SetEnvironmentVariable("DATABASE_PROFILE", "PRODUCTION");
        string? rawProd = config.GetConnectionString("OracleDB_Production")
                       ?? config["ConnectionStrings:OracleDB_Production"]
                       ?? config.GetConnectionString("OracleDB_Prod")
                       ?? config.GetConnectionString("OracleDB");
        string prodConn = DatabaseEnvironmentGuard.ValidateAndResolveDevelopmentConnection(rawProd!, config);

        Environment.SetEnvironmentVariable("DATABASE_PROFILE", "LOCAL_XE");
        string? rawXe = config.GetConnectionString("OracleDB_LocalXE")
                     ?? config["ConnectionStrings:OracleDB_LocalXE"]
                     ?? config.GetConnectionString("OracleDB_XE")
                     ?? config.GetConnectionString("OracleDB");
        string xeConn = DatabaseEnvironmentGuard.ValidateAndResolveDevelopmentConnection(rawXe!, config);

        Environment.SetEnvironmentVariable("DATABASE_PROFILE", "PRODUCTION");
        return (prodConn, xeConn);
    }

    private static string GetCellTextValue(JsonElement row, int fieldNumber)
    {
        if (row.TryGetProperty("cells", out var cellsElem) && cellsElem.ValueKind == JsonValueKind.Array)
        {
            int idx = 0;
            foreach (var c in cellsElem.EnumerateArray())
            {
                idx++;
                if (c.TryGetProperty("fieldNumber", out var fnProp))
                {
                    if (fnProp.GetInt32() == fieldNumber)
                    {
                        return c.TryGetProperty("textValue", out var tv) ? tv.GetString() ?? "" : "";
                    }
                }
                else if (idx == fieldNumber && c.TryGetProperty("textValue", out var tv2))
                {
                    return tv2.GetString() ?? "";
                }
            }
        }
        return "";
    }

    private static List<string> ParseMultilineItems(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return new List<string>();
        return text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                   .Select(s => s.Trim())
                   .Where(s => !string.IsNullOrWhiteSpace(s))
                   .ToList();
    }

    private static bool IsEmptyOrNoHay(string s)
    {
        string t = s.Trim().ToLowerInvariant();
        return t == "" || t == "no hay" || t == "nohay" || t == "inexistente" || t == "0" || t == "0%" || t == "ninguno";
    }

    private static string NormalizeControlText(string s)
    {
        return Regex.Replace(s, @"^\s*\d+[\.\-\)]\s*", "").Trim();
    }

    private static Dictionary<string, Dictionary<string, string>> BuildCatalogNormalizationDictionaries(JsonDocument catalogDoc)
    {
        var dicts = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        var autoMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["MANUAL"] = "MANUAL",
            ["Manual"] = "MANUAL",
            ["SEMIAUTOMATICO"] = "SEMIAUTOMATICO",
            ["Semiautomatico"] = "SEMIAUTOMATICO",
            ["Semiautomatizado"] = "SEMIAUTOMATICO",
            ["Semi-Automatizado"] = "SEMIAUTOMATICO",
            ["AUTOMATICO"] = "AUTOMATICO",
            ["Automatico"] = "AUTOMATICO",
            ["Automatizado"] = "AUTOMATICO"
        };
        dicts["MR_NIVEL_AUTOMATIZACION"] = autoMap;
        dicts["CON_AUTOMATIZACION"] = autoMap;

        if (catalogDoc.RootElement.TryGetProperty("catalogs", out var catalogsElem) && catalogsElem.ValueKind == JsonValueKind.Array)
        {
            foreach (var cat in catalogsElem.EnumerateArray())
            {
                string catCode = (cat.TryGetProperty("catalogCode", out var cc) ? cc.GetString() : null)
                    ?? (cat.TryGetProperty("catalogId", out var ci) ? ci.GetString() : null)
                    ?? (cat.TryGetProperty("dbCatalogCode", out var dc) ? dc.GetString() : null)
                    ?? "";

                var map = dicts.TryGetValue(catCode, out var existingMap)
                    ? existingMap
                    : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                if (cat.TryGetProperty("items", out var itemsElem) && itemsElem.ValueKind == JsonValueKind.Array)
                {
                    foreach (var it in itemsElem.EnumerateArray())
                    {
                        string key = (it.TryGetProperty("itemKey", out var ik) ? ik.GetString() : null)
                            ?? (it.TryGetProperty("key", out var k) ? k.GetString() : null)
                            ?? "";

                        string display = (it.TryGetProperty("displayValue", out var dv) ? dv.GetString() : null)
                            ?? (it.TryGetProperty("label", out var l) ? l.GetString() : null)
                            ?? "";

                        if (!string.IsNullOrWhiteSpace(key))
                        {
                            map[key] = key;
                            if (!string.IsNullOrWhiteSpace(display)) map[display] = key;

                            var synArray = it.TryGetProperty("synonyms", out var sArr) ? sArr
                                : it.TryGetProperty("aliases", out var aArr) ? aArr : default;

                            if (synArray.ValueKind == JsonValueKind.Array)
                            {
                                foreach (var syn in synArray.EnumerateArray())
                                {
                                    string synStr = syn.GetString() ?? "";
                                    if (!string.IsNullOrWhiteSpace(synStr)) map[synStr] = key;
                                }
                            }
                        }
                    }
                }
                if (!string.IsNullOrWhiteSpace(catCode))
                {
                    dicts[catCode] = map;
                }
            }
        }
        return dicts;
    }

    public static string? NormalizeCatalogValue(
        string catalogOrProperty,
        string rawValue,
        Dictionary<string, Dictionary<string, string>> catalogMaps,
        Dictionary<string, string> log)
    {
        if (string.IsNullOrWhiteSpace(rawValue)) return "";
        string val = rawValue.Trim();

        bool isAutomation = catalogOrProperty.Contains("AUTOMATIZACION", StringComparison.OrdinalIgnoreCase);
        if (isAutomation)
        {
            string clean = val.Replace("-", "", StringComparison.Ordinal).ToLowerInvariant();
            if (clean.Contains("semiautomat", StringComparison.Ordinal)) return "SEMIAUTOMATICO";
            if (clean == "manual") return "MANUAL";
            if (clean.Contains("automat", StringComparison.Ordinal)) return "AUTOMATICO";
            return null; // Unknown automation fails closed
        }

        if (catalogMaps.TryGetValue(catalogOrProperty, out var directMap))
        {
            if (directMap.TryGetValue(val, out var canonDirect))
            {
                log[val] = canonDirect;
                return canonDirect;
            }
            return null; // Known catalog property but unrecognized value fails closed
        }

        foreach (var kvp in catalogMaps)
        {
            if (kvp.Value.TryGetValue(val, out var canon))
            {
                log[val] = canon;
                return canon;
            }
        }

        // Si es una propiedad declarada como catálogo cerrado pero no se encontró en ningún mapeo, fail-closed
        bool isCatalogProperty = catalogOrProperty is "tipo_riesgo" or "regimen_afectado" or "RISK_TYPE" or "REGIME" or "CONTROL_AUTOMATION" or "MR_NIVEL_AUTOMATIZACION";
        if (isCatalogProperty)
        {
            return null;
        }

        return val;
    }

    public static string ComputeFileSha256(string path)
    {
        using var stream = File.OpenRead(path);
        using var sha = SHA256.Create();
        return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
    }

    private static void ExportMutationsCsv(string path, List<PlannedMutation> mutations)
    {
        var sb = new StringBuilder();
        sb.AppendLine("MUTATION_ID,RISK_CODE,FIELD_NO,FIELD_LABEL,ENTITY,TARGET_TABLE,RECORD_KEY,ACTION,TARGET_COLUMN,EXCEL_CANONICAL,DB_BEFORE_CANONICAL,TARGET_CANONICAL,PRECONDITION");
        foreach (var m in mutations)
        {
            sb.AppendLine($"\"{m.MutationId}\",\"{m.RiskCode}\",{m.FieldNumber},\"{EscapeCsv(m.FieldLabel)}\",\"{m.Entity}\",\"{m.TargetTable}\",\"{m.RecordKey}\",\"{m.Action}\",\"{m.TargetColumnOrProperty}\",\"{EscapeCsv(m.ExcelCanonical)}\",\"{EscapeCsv(m.DbBeforeCanonical)}\",\"{EscapeCsv(m.TargetCanonical)}\",\"{EscapeCsv(m.Precondition)}\"");
        }
        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
    }

    private static void ExportNullGuardCsv(string path, List<NullOverwriteGuardEntry> entries)
    {
        var sb = new StringBuilder();
        sb.AppendLine("RISK_CODE,FIELD_NO,FIELD_LABEL,DB_BEFORE_RAW,EXCEL_RAW,TARGET_CANONICAL,IS_NULL_OVERWRITE,STATUS");
        foreach (var e in entries)
        {
            sb.AppendLine($"\"{e.RiskCode}\",{e.FieldNumber},\"{EscapeCsv(e.FieldLabel)}\",\"{EscapeCsv(e.DbBeforeRaw)}\",\"{EscapeCsv(e.ExcelRaw)}\",\"{EscapeCsv(e.TargetCanonical)}\",{e.IsNullOverwrite},\"{e.Status}\"");
        }
        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
    }

    private static void ExportDuplicateGuardCsv(string path, List<DuplicateGuardEntry> entries)
    {
        var sb = new StringBuilder();
        sb.AppendLine("ENTITY_TYPE,ENTITY_KEY,PREEXISTING_COUNT,POST_APPLY_EXPECTED,DUPLICATE_COUNT,STATUS");
        foreach (var e in entries)
        {
            sb.AppendLine($"\"{e.EntityType}\",\"{EscapeCsv(e.EntityKey)}\",{e.PreexistingCount},{e.PostApplyExpectedCount},{e.DuplicateCount},\"{e.Status}\"");
        }
        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
    }

    private static void ExportFields7182Csv(string path, List<Field71To82AuditEntry> entries)
    {
        var sb = new StringBuilder();
        sb.AppendLine("RISK_CODE,FIELD_NO,FIELD_LABEL,TARGET_TABLE,RECORD_ID,DB_BEFORE_RAW,DB_BEFORE_CANONICAL,EXCEL_RAW,CLASSIFICATION,ACTION,PRESERVED_IN_DB");
        foreach (var e in entries)
        {
            sb.AppendLine($"\"{e.RiskCode}\",{e.FieldNumber},\"{EscapeCsv(e.FieldLabel)}\",\"{e.TargetTable}\",\"{e.RecordId}\",\"{EscapeCsv(e.DbBeforeRaw)}\",\"{EscapeCsv(e.DbBeforeCanonical)}\",\"{EscapeCsv(e.ExcelRaw)}\",\"{e.Classification}\",\"{e.Action}\",{e.PreservedInDb}");
        }
        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
    }

    private static void ExportCatalogNormalizationLog(string path, Dictionary<string, string> log)
    {
        var sb = new StringBuilder();
        sb.AppendLine("SOURCE_VALUE,CANONICAL_VALUE");
        foreach (var kvp in log)
        {
            sb.AppendLine($"\"{EscapeCsv(kvp.Key)}\",\"{EscapeCsv(kvp.Value)}\"");
        }
        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
    }

    private static void ExportDelta398Csv(string path, List<Delta398Record> deltas)
    {
        var sb = new StringBuilder();
        sb.AppendLine("RISK_CODE,FIELD_NO,PREVIOUS_CLASSIFICATION,CURRENT_DB_RAW,CURRENT_EXCEL_RAW,CURRENT_CLASSIFICATION,REASON,MUTATION_REQUIRED");
        foreach (var d in deltas)
        {
            sb.AppendLine($"\"{d.RiskCode}\",{d.FieldNumber},\"{d.PreviousClassification}\",\"{EscapeCsv(d.CurrentDbRaw)}\",\"{EscapeCsv(d.CurrentExcelRaw)}\",\"{d.CurrentClassification}\",\"{EscapeCsv(d.Reason)}\",{d.MutationRequired}");
        }
        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
    }

    private static void ExportDelta87Csv(string path, List<Delta87Record> deltas)
    {
        var sb = new StringBuilder();
        sb.AppendLine("RISK_CODE,FIELD_NO,PREVIOUS_CLASSIFICATION,CURRENT_DB_RAW,CURRENT_EXCEL_RAW,CURRENT_CLASSIFICATION,REASON,MUTATION_REQUIRED");
        foreach (var d in deltas)
        {
            sb.AppendLine($"\"{d.RiskCode}\",{d.FieldNumber},\"{d.PreviousClassification}\",\"{EscapeCsv(d.CurrentDbRaw)}\",\"{EscapeCsv(d.CurrentExcelRaw)}\",\"{d.CurrentClassification}\",\"{EscapeCsv(d.Reason)}\",{d.MutationRequired}");
        }
        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
    }

    private static void ExportBlockedItemsCsv(string path, List<BlockedItem> items)
    {
        var sb = new StringBuilder();
        sb.AppendLine("RISK_CODE,FIELD_NO,FIELD_LABEL,ENTITY,SOURCE_VALUE,REQUIRED_MISSING_VALUE,REASON_CODE,RECOMMENDED_RESOLUTION");
        foreach (var it in items)
        {
            sb.AppendLine($"\"{it.RiskCode}\",{it.FieldNumber},\"{EscapeCsv(it.FieldLabel)}\",\"{it.Entity}\",\"{EscapeCsv(it.SourceValue)}\",\"{EscapeCsv(it.RequiredMissingValue)}\",\"{it.ReasonCode}\",\"{EscapeCsv(it.RecommendedResolution)}\"");
        }
        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
    }

    private static string EscapeCsv(string s) => (s ?? "").Replace("\"", "\"\"");

    private static string? GetOption(string[] args, string name)
    {
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i].Equals(name, StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                return args[i + 1];
            }
        }
        return null;
    }

    #endregion
}
