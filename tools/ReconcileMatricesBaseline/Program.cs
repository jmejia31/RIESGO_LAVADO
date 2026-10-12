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
using Oracle.ManagedDataAccess.Types;
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
    public string InstanceName { get; set; } = "";
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
    public Dictionary<string, TableContentFingerprint> TableContentFingerprints { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, TableContentFingerprint> XeTableContentFingerprints { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public class TableContentFingerprint
{
    public long RowCount { get; set; }
    public string Sha256 { get; set; } = "";
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
    public string FieldLabel { get; set; } = "";
    public string PreviousClassification { get; set; } = "MISSING_IN_DB";
    public string OriginalExcelRaw { get; set; } = "";
    public string OriginalDbRaw { get; set; } = "";
    public string OriginalReasonCode { get; set; } = "";
    public string OriginalRecommendedAction { get; set; } = "";
    public string CurrentDbRaw { get; set; } = "";
    public string CurrentExcelRaw { get; set; } = "";
    public string CurrentClassification { get; set; } = "";
    public string Reason { get; set; } = "";
    public string RecommendedAction { get; set; } = "";
    public string Evidence { get; set; } = "";
    public bool MutationRequired { get; set; }
}

public class Delta87Record
{
    public string RiskCode { get; set; } = "";
    public int FieldNumber { get; set; }
    public string FieldLabel { get; set; } = "";
    public string PreviousClassification { get; set; } = "DIFFERENT";
    public string OriginalExcelRaw { get; set; } = "";
    public string OriginalDbRaw { get; set; } = "";
    public string OriginalReasonCode { get; set; } = "";
    public string OriginalRecommendedAction { get; set; } = "";
    public string CurrentDbRaw { get; set; } = "";
    public string CurrentExcelRaw { get; set; } = "";
    public string CurrentClassification { get; set; } = "";
    public string Reason { get; set; } = "";
    public string RecommendedAction { get; set; } = "";
    public string Evidence { get; set; } = "";
    public bool MutationRequired { get; set; }
}

public sealed class HistoricalDeltaRow
{
    public Dictionary<string, string> Values { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public string Get(string key) => Values.TryGetValue(key, out var value) ? value : "";
}

public sealed class HistoricalDeltaSource
{
    public List<HistoricalDeltaRow> BaselineImports { get; init; } = new();
    public List<HistoricalDeltaRow> DataConflicts { get; init; } = new();
    public string BaselineImportsSha256 { get; init; } = "";
    public string DataConflictsSha256 { get; init; } = "";
    public string SourceDirectory { get; init; } = "";
}

public sealed class SourceItemsParseResult
{
    public IReadOnlyList<string> Items { get; init; } = Array.Empty<string>();
    public IReadOnlyList<int?> ParentOrdinals { get; init; } = Array.Empty<int?>();
    public string ParseRule { get; init; } = "";
    public bool IsAmbiguous { get; init; }
}

public class PlanResult
{
    public string RunId { get; set; } = "";
    public string EngineVersion { get; set; } = "";
    public string GitSha { get; set; } = "";
    public string WorkbookSha256 { get; set; } = "";
    public string PreimageManifestSha256 { get; set; } = "";
    public string Block2BaselineImportsSha256 { get; set; } = "";
    public string Block2DataConflictsSha256 { get; set; } = "";
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
    public bool XeContentParityPassed { get; set; }
    public bool OperationalPreservationPassed { get; set; }
    public int OperationalPreservationChecked { get; set; }
    public int OperationalPreservationMismatches { get; set; }
    public bool InsertPreconditionTestPassed { get; set; }
    public int InsertFixtureRowsAffected { get; set; }
    public int FirstFixtureMutations { get; set; }
    public int SecondFixtureMutations { get; set; }
    public string ErrorMessage { get; set; } = "";
}

#endregion

public static class Program
{
    public const string ExpectedWorkbookSha256 = "5c3fc00864947afe1e34d3d6ffdfc6da008eaa3c8f1c6c764161014d5ef9a385";
    public const string PreviousInvalidatedPlanSha = "08a4e17f8a8410ed92f4bd317685730a22a2d61d8387ab6a5f529a1a9c049611";
    public const string EngineVersion = "RL_BLOCK4_RECONCILIATION_ENGINE_V4_EXACT_PREIMAGE_AND_ROLLBACK";
    public const string ExpectedBlock2BaselineImportsSha256 = "06772eb693f3ab4fe7564e04c7c14de2fb5ca137089c95b57ee3e338d8313cb6";
    public const string ExpectedBlock2DataConflictsSha256 = "b28eba977ab9e7fa83d8a23d2e831ee13f6346e240b23690b1e93b2177360f4f";

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
        string? expectedFileSha = GetOption(args, "--expected-file-sha");
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
        string runId = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff", CultureInfo.InvariantCulture);
        string artifactDir = Path.Combine(Path.GetTempPath(), $"RIESGO_LAVADO_BLOCK4_RECONCILIATION_{runId}");
        Directory.CreateDirectory(artifactDir);
        string preimageDir = Path.Combine(artifactDir, "preimage");
        Directory.CreateDirectory(preimageDir);
        string excelJsonPath = Path.Combine(artifactDir, "workbook_extract_59x82.json");

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
        var psi = new ProcessStartInfo("node")
        {
            WorkingDirectory = repoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        psi.ArgumentList.Add(Path.Combine(repoRoot, "tools", "export_excel_matrix_82.js"));
        psi.ArgumentList.Add("--output");
        psi.ArgumentList.Add(excelJsonPath);
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
        Console.WriteLine("APPSETTINGS_CONNECTION_SECRET_SCAN=PASS");
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

            if (string.IsNullOrWhiteSpace(expectedPlanSha) || string.IsNullOrWhiteSpace(expectedFileSha))
            {
                Console.Error.WriteLine("FATAL: Apply requiere --expected-plan-sha <canonical_sha> y --expected-file-sha <file_sha>.");
                return 1;
            }

            string actualPlanFileSha = ComputeFileSha256(planArg);
            if (!string.Equals(actualPlanFileSha, expectedFileSha, StringComparison.OrdinalIgnoreCase))
            {
                Console.Error.WriteLine("FATAL: El hash de bytes del plan no coincide con --expected-file-sha.");
                return 1;
            }

            return await ExecuteProductionApplyAsync(prodConnString, planArg, expectedPlanSha, expectedFileSha, actualWorkbookSha);
        }

        // MODO DRY-RUN Y SIMULACIÓN COMPLETA
        Console.WriteLine("\n[2/7] Capturando Preimage Completo de Producción (HPPROD1) en modo estrictamente READ-ONLY...");
        var prodSnapshot = await CaptureProductionPreimageAsync(prodConnString, runId);
        bool productionIdentityVerified =
            prodSnapshot.DatabaseName.Equals("HPPROD1", StringComparison.OrdinalIgnoreCase) &&
            prodSnapshot.ServiceName.Equals("hpprod1", StringComparison.OrdinalIgnoreCase) &&
            prodSnapshot.InstanceName.Equals("hpprod1", StringComparison.OrdinalIgnoreCase) &&
            prodSnapshot.CurrentSchema.Equals("RIESGO_LAVADO", StringComparison.OrdinalIgnoreCase) &&
            prodSnapshot.SessionUser.Equals("RIESGO_LAVADO", StringComparison.OrdinalIgnoreCase);
        Console.WriteLine($"DATABASE_TARGET_CLASS={(productionIdentityVerified ? "PRODUCTION" : "IDENTITY_MISMATCH")}");
        Console.WriteLine($"DB_IDENTITY_VERIFIED: Name={prodSnapshot.DatabaseName}, Service={prodSnapshot.ServiceName}, Instance={prodSnapshot.InstanceName}, Schema={prodSnapshot.CurrentSchema}, User={prodSnapshot.SessionUser}");
        if (!productionIdentityVerified) throw new InvalidOperationException("PRODUCTION_READ_ONLY_IDENTITY_MISMATCH: conexión no corresponde al perfil productivo esperado.");
        Console.WriteLine($"CONTROLS_PREIMAGE_COUNT={prodSnapshot.Controls.Count}");
        Console.WriteLine($"PLANS_PREIMAGE_COUNT={prodSnapshot.Plans.Count}");
        Console.WriteLine($"ACTIVITIES_PREIMAGE_COUNT={prodSnapshot.Activities.Count}");
        Console.WriteLine($"MONITORING_PREIMAGE_COUNT={prodSnapshot.Monitorings.Count}");
        Console.WriteLine($"EVIDENCE_PREIMAGE_COUNT={prodSnapshot.Evidences.Count}");
        Console.WriteLine($"PREIMAGE_ROW_COUNTS=Risks:{prodSnapshot.Risks.Count};Evas:{prodSnapshot.Evaluations.Count};Proys:{prodSnapshot.Projections.Count};Alerts:{prodSnapshot.Alerts.Count}");

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
        prodSnapshot.XeTableContentFingerprints = await SeedOracleXeAsync(xeConnString, prodSnapshot);
        var tableParityRows = prodSnapshot.TableContentFingerprints
            .OrderBy(x => x.Key, StringComparer.Ordinal)
            .Select(x =>
            {
                prodSnapshot.XeTableContentFingerprints.TryGetValue(x.Key, out var xe);
                return new
                {
                    Table = x.Key,
                    ProductionCount = x.Value.RowCount,
                    XeCount = xe?.RowCount ?? -1,
                    ProductionSha256 = x.Value.Sha256,
                    XeSha256 = xe?.Sha256 ?? "",
                    Status = xe != null && xe.RowCount == x.Value.RowCount && xe.Sha256 == x.Value.Sha256 ? "PASS" : "CONTENT_MISMATCH"
                };
            }).ToList();
        bool xeContentParityPassed = tableParityRows.Count == 12 && tableParityRows.All(x => x.Status == "PASS");
        string tableParityPath = Path.Combine(artifactDir, "table_parity_hashes.csv");
        var tableParityCsv = new StringBuilder("TABLE,PROD_COUNT,XE_COUNT,PROD_CANONICAL_SHA256,XE_CANONICAL_SHA256,STATUS\r\n");
        foreach (var row in tableParityRows)
            tableParityCsv.AppendLine($"\"{row.Table}\",{row.ProductionCount},{row.XeCount},\"{row.ProductionSha256}\",\"{row.XeSha256}\",\"{row.Status}\"");
        File.WriteAllText(tableParityPath, tableParityCsv.ToString(), new UTF8Encoding(false));
        Console.WriteLine($"XE_PREIMAGE_PARITY_CONTENT_HASH={(xeContentParityPassed ? "PASS" : "FAIL")}");
        Console.WriteLine($"XE_PREIMAGE_PARITY_CONTENT_HASH_TABLES={tableParityRows.Count(x => x.Status == "PASS")}/12");

        // 4. Construir el Plan Canónico de Reconciliación
        Console.WriteLine("\n[4/7] Construyendo Plan de Reconciliación Canónico y Rollback Real...");
        var historicalDeltaSource = LoadCertifiedHistoricalDeltaSource(Path.GetTempPath());
        var planResult = BuildReconciliationPlan(prodSnapshot, excelRows, manifestFields, catalogDoc, actualWorkbookSha, preimageManifestSha, historicalDeltaSource);

        var original398Keys = historicalDeltaSource.BaselineImports.Select(r => $"{r.Get("RISK_CODE").Trim()}|{ParseFieldNumber(r.Get("FIELD_NUMBER"))}").ToHashSet(StringComparer.OrdinalIgnoreCase);
        var final398Keys = planResult.Historical398Deltas.Select(d => $"{d.RiskCode.Trim()}|{d.FieldNumber}").ToHashSet(StringComparer.OrdinalIgnoreCase);
        var original87Keys = historicalDeltaSource.DataConflicts.Select(r => $"{r.Get("RISK_CODE").Trim()}|{ParseFieldNumber(r.Get("FIELD_NUMBER"))}").ToHashSet(StringComparer.OrdinalIgnoreCase);
        var final87Keys = planResult.Historical87Deltas.Select(d => $"{d.RiskCode.Trim()}|{d.FieldNumber}").ToHashSet(StringComparer.OrdinalIgnoreCase);
        Console.WriteLine($"ORIGINAL_398_ROWS={historicalDeltaSource.BaselineImports.Count}");
        Console.WriteLine($"FINAL_398_ROWS={planResult.Historical398Deltas.Count}");
        Console.WriteLine($"BLOCK2_398_KEYSET_EQUAL={original398Keys.SetEquals(final398Keys)}");
        Console.WriteLine($"ORIGINAL_87_ROWS={historicalDeltaSource.DataConflicts.Count}");
        Console.WriteLine($"FINAL_87_ROWS={planResult.Historical87Deltas.Count}");
        Console.WriteLine($"BLOCK2_87_KEYSET_EQUAL={original87Keys.SetEquals(final87Keys)}");
        Console.WriteLine($"FIELD70_AUDITED={planResult.AllPositions.Count(p => p.FieldNumber == 70)}/59");
        Console.WriteLine($"FIELD70_SOURCE_NONBLANK={planResult.AllPositions.Count(p => p.FieldNumber == 70 && !string.IsNullOrWhiteSpace(p.ExcelValue))}");
        Console.WriteLine($"FIELDS71_82_AUDITED={planResult.Field71To82AuditEntries.Count}/708");

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
        Console.WriteLine($"OPERATIONAL_PRESERVATION_EXPECTED_COUNT={planResult.Field71To82AuditEntries.Count(x => x.Action == "PRESERVE_PRODUCTION")}");

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

        string sealedPlanManifestPath = Path.Combine(artifactDir, "sealed_plan_manifest.json");
        File.WriteAllText(sealedPlanManifestPath, JsonSerializer.Serialize(new
        {
            planCanonicalSha = planResult.PlanCanonicalSha256,
            planFileSha = planResult.PlanFileSha256,
            workbookSha = actualWorkbookSha,
            preimageManifestSha,
            gitSha = planResult.GitSha,
            engineVersion = planResult.EngineVersion,
            runId = planResult.RunId
        }, new JsonSerializerOptions { WriteIndented = true }));

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
        var simResult = await SimulateApplyOnOracleXeAsync(xeConnString, prodSnapshot, planResult, excelRows, manifestFields, catalogDoc, actualWorkbookSha, preimageManifestSha, historicalDeltaSource);
        simResult.XeContentParityPassed = xeContentParityPassed;
        Console.WriteLine($"XE_FIRST_APPLY={(simResult.FirstApplyPassed ? "PASS" : "FAIL")}");
        Console.WriteLine($"CONCURRENCY_PRECONDITIONS_ENFORCED={(simResult.ConcurrentDriftAbortPassed ? "PASS" : "FAIL")}");
        Console.WriteLine($"CONCURRENT_DRIFT_ABORT_PASSED={(simResult.ConcurrentDriftAbortPassed ? "PASS" : "FAIL")}");
        Console.WriteLine($"POSTCHECK_OPERATIONS_VERIFIED={simResult.PostcheckOperationsVerified}/{planResult.Mutations.Count}");
        Console.WriteLine($"POSTCHECK_MISMATCHES={simResult.PostcheckMismatches}");
        Console.WriteLine($"OPERATIONAL_PRESERVATION_CHECKED={simResult.OperationalPreservationChecked}");
        Console.WriteLine($"OPERATIONAL_PRESERVATION_MISMATCHES={simResult.OperationalPreservationMismatches}");
        Console.WriteLine($"OPERATIONAL_DATA_PRESERVED={(simResult.OperationalPreservationPassed ? "PASS" : "FAIL")}");
        Console.WriteLine($"FIRST_XE_FIXTURE_MUTATIONS={simResult.FirstFixtureMutations}");
        Console.WriteLine($"SECOND_XE_FIXTURE_MUTATIONS={simResult.SecondFixtureMutations}");
        Console.WriteLine($"ROLLBACK_TEST_PASSED={(simResult.RollbackTestPassed ? "PASS" : "FAIL")}");
        Console.WriteLine($"ROLLBACK_PREIMAGE_MATCH={(simResult.RollbackPreimageMatch ? "PASS" : "FAIL")}");
        Console.WriteLine($"ROLLBACK_ROWS_AFFECTED={simResult.RollbackRowsAffected}");
        Console.WriteLine($"INSERT_PRECONDITION_TEST={(simResult.InsertPreconditionTestPassed ? "PASS" : "FAIL")}");
        Console.WriteLine($"INSERT_FIXTURE_ROWS_AFFECTED={simResult.InsertFixtureRowsAffected}");
        Console.WriteLine($"NON_ZERO_FIRST_PLAN_PASSED={(simResult.NonZeroFirstPlanPassed ? "PASS" : "FAIL")}");
        Console.WriteLine($"XE_SECOND_PLAN_MUTATIONS={simResult.SecondApplyMutations}");
        Console.WriteLine($"REAL_IDEMPOTENCY_TEST={(simResult.NonZeroFirstPlanPassed && simResult.SecondApplyMutations == 0 ? "PASS" : "FAIL")}");

        string simResultsPath = Path.Combine(artifactDir, "xe_simulation_results.json");
        File.WriteAllText(simResultsPath, JsonSerializer.Serialize(simResult, new JsonSerializerOptions { WriteIndented = true }));
        if (!simResult.FirstApplyPassed || !simResult.PostcheckPassed || simResult.SecondApplyMutations != 0 || !simResult.InsertPreconditionTestPassed ||
            !simResult.ConcurrentDriftAbortPassed || !simResult.RollbackTestPassed || !simResult.RollbackPreimageMatch)
        {
            Console.Error.WriteLine($"FATAL: La simulación en Oracle XE no certificó el plan. Error: {simResult.ErrorMessage}");
            return 2;
        }

        // 6. Resumen de artefactos generados
        var shaSums = new Dictionary<string, string>
        {
            ["reconciliation_plan_full.json"] = planResult.PlanFileSha256,
            ["sealed_plan_manifest.json"] = ComputeFileSha256(sealedPlanManifestPath),
            ["reconciliation_plan_mutations.csv"] = ComputeFileSha256(mutationsCsvPath),
            ["rollback_plan.json"] = ComputeFileSha256(rollbackPlanPath),
            ["null_overwrite_guard.csv"] = ComputeFileSha256(nullGuardPath),
            ["duplicate_guard.csv"] = ComputeFileSha256(duplicateGuardPath),
            ["fields71_82_audit.csv"] = ComputeFileSha256(fields7182Path),
            ["table_parity_hashes.csv"] = ComputeFileSha256(tableParityPath),
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
        bool safetyGatesPassed = simResult.FirstApplyPassed && simResult.PostcheckPassed && simResult.ConcurrentDriftAbortPassed &&
            simResult.RollbackTestPassed && simResult.RollbackPreimageMatch && simResult.InsertPreconditionTestPassed && simResult.NonZeroFirstPlanPassed &&
            simResult.SecondApplyMutations == 0 && simResult.XeContentParityPassed && simResult.OperationalPreservationPassed &&
            planResult.RollbackOperations.Count == planResult.Mutations.Count &&
            planResult.AccidentalNullOverwrites == 0 && planResult.PreexistingDuplicates == 0 && planResult.NewDuplicatesExpected == 0 &&
            planResult.SyntheticDefaultsCount == 0 && planResult.SilentTruncationsCount == 0;
        bool isApplyReady = safetyGatesPassed && planResult.BlockedByPreconditionCount == 0 && planResult.Mutations.Count > 0;
        string block4Status = safetyGatesPassed && planResult.BlockedByPreconditionCount == 0 && planResult.Mutations.Count == 0
            ? "READY_FOR_CLOSURE_REVIEW"
            : isApplyReady ? "READY_FOR_AUTHORIZATION" : "PENDING";

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
        Console.WriteLine($"PRODUCTION_APPLY_PATH_IMPLEMENTED={(safetyGatesPassed ? "PASS" : "UNVERIFIED")}");
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
        Console.WriteLine($"ROLLBACK_OPERATION_COUNT_MATCH={(planResult.RollbackOperations.Count == planResult.Mutations.Count)} ({planResult.RollbackOperations.Count}/{planResult.Mutations.Count})");
        Console.WriteLine($"ACCIDENTAL_NULL_OVERWRITES={planResult.AccidentalNullOverwrites}");
        Console.WriteLine($"PREEXISTING_DUPLICATES={planResult.PreexistingDuplicates}");
        Console.WriteLine($"NEW_DUPLICATES={planResult.NewDuplicatesExpected}");
        Console.WriteLine($"SYNTHETIC_DEFAULTS={planResult.SyntheticDefaultsCount}");
        Console.WriteLine($"SILENT_TRUNCATIONS={planResult.SilentTruncationsCount}");
        Console.WriteLine($"FIELDS71_82_DB_VALUES_AUDITED={planResult.Field71To82AuditEntries.Count}/708");
        Console.WriteLine($"CONCURRENCY_PRECONDITIONS_ENFORCED={(simResult.ConcurrentDriftAbortPassed ? "PASS" : "FAIL")}");
        Console.WriteLine($"CONCURRENT_DRIFT_ABORT_PASSED={(simResult.ConcurrentDriftAbortPassed ? "PASS" : "FAIL")}");
        Console.WriteLine($"ROLLBACK_PREIMAGE_MATCH={(simResult.RollbackPreimageMatch ? "PASS" : "FAIL")}");
        Console.WriteLine($"NON_ZERO_FIRST_PLAN_PASSED={(simResult.NonZeroFirstPlanPassed ? "PASS" : "FAIL")}");
        Console.WriteLine($"XE_SECOND_PLAN_MUTATIONS={simResult.SecondApplyMutations}");
        Console.WriteLine($"POSTCHECK_MISMATCHES={simResult.PostcheckMismatches}");
        foreach (var group in planResult.Historical398Deltas.GroupBy(d => d.CurrentClassification).OrderBy(g => g.Key, StringComparer.Ordinal))
            Console.WriteLine($"DELTA_398_CLASSIFICATION_{group.Key}={group.Count()}");
        foreach (var group in planResult.Historical87Deltas.GroupBy(d => d.CurrentClassification).OrderBy(g => g.Key, StringComparer.Ordinal))
            Console.WriteLine($"DELTA_87_CLASSIFICATION_{group.Key}={group.Count()}");
        Console.WriteLine($"DELTA_398_TOTAL={planResult.Historical398Deltas.Count}");
        Console.WriteLine($"DELTA_87_TOTAL={planResult.Historical87Deltas.Count}");
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
            if (val is OracleClob clob) return clob.IsNull ? "" : clob.Value.Trim();
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
            await cmdIso.ExecuteNonQueryAsync();
        }

        await using (var cmdIdent = new OracleCommand(@"
            SELECT sys_context('USERENV', 'DB_NAME'),
                   sys_context('USERENV', 'SERVICE_NAME'),
                   sys_context('USERENV', 'INSTANCE_NAME'),
                   sys_context('USERENV', 'CURRENT_SCHEMA'),
                   sys_context('USERENV', 'SESSION_USER')
              FROM dual", conn))
        await using (var r = await cmdIdent.ExecuteReaderAsync())
        {
            if (await r.ReadAsync())
            {
                snap.DatabaseName = SafeGetString(r, 0);
                snap.ServiceName = SafeGetString(r, 1);
                snap.InstanceName = SafeGetString(r, 2);
                snap.CurrentSchema = SafeGetString(r, 3);
                snap.SessionUser = SafeGetString(r, 4);
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

        await using (var cmdRollback = new OracleCommand("ROLLBACK", conn))
        {
            snap.TableContentFingerprints = await ComputeOracleTableContentFingerprintsAsync(conn);
            await cmdRollback.ExecuteNonQueryAsync();
        }
        return snap;
    }

    private static async Task<Dictionary<string, TableContentFingerprint>> ComputeOracleTableContentFingerprintsAsync(OracleConnection connection)
    {
        // Static allowlist and primary keys keep this forensic query SELECT-only and deterministic.
        var tables = new (string Name, string PrimaryKey)[]
        {
            ("RL_MR_RIESGOS", "RIE_ID"),
            ("RL_MR_EVALUACIONES_RIESGO", "EVA_ID"),
            ("RL_MR_PROYECCIONES_EVALUACION", "PROY_ID"),
            ("RL_MR_CONTROLES_RIESGO", "CON_ID"),
            ("RL_MR_EVALUACIONES_CONTROL", "ECO_ID"),
            ("RL_MR_PLANES", "PLA_ID"),
            ("RL_MR_ACTIVIDADES", "ACT_ID"),
            ("RL_MR_SENALES_ALERTA", "ALE_ID"),
            ("RL_MR_AUTOMONITOREO", "MON_ID"),
            ("RL_MR_EVIDENCIAS", "EVI_ID"),
            ("RL_MR_EVIDENCIAS_VINCULOS", "EVV_ID"),
            ("RL_MR_FLUJOS_EVALUACION", "FLU_ID")
        };

        var result = new Dictionary<string, TableContentFingerprint>(StringComparer.OrdinalIgnoreCase);
        foreach (var (table, primaryKey) in tables)
        {
            await using var command = new OracleCommand($"SELECT * FROM {table} ORDER BY {primaryKey}", connection);
            await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess);
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            long rowCount = 0;
            while (await reader.ReadAsync())
            {
                var row = new List<string>(reader.FieldCount);
                for (var column = 0; column < reader.FieldCount; column++)
                {
                    string name = reader.GetName(column);
                    string value = reader.IsDBNull(column) ? "<NULL>" : NormalizeOracleHashValue(reader.GetValue(column));
                    row.Add($"{name.Length}:{name}{value.Length}:{value}");
                }
                var bytes = Encoding.UTF8.GetBytes(string.Join("", row));
                hash.AppendData(BitConverter.GetBytes(bytes.Length));
                hash.AppendData(bytes);
                rowCount++;
            }
            result[table] = new TableContentFingerprint
            {
                RowCount = rowCount,
                Sha256 = Convert.ToHexStringLower(hash.GetHashAndReset())
            };
        }
        return result;
    }

    private static string NormalizeOracleHashValue(object value) => value switch
    {
        OracleClob clob => clob.IsNull ? "<NULL>" : clob.Value,
        OracleBlob blob => blob.IsNull ? "<NULL>" : Convert.ToBase64String(blob.Value),
        byte[] bytes => Convert.ToBase64String(bytes),
        DateTime dateTime => dateTime.ToString("O", CultureInfo.InvariantCulture),
        decimal number => number.ToString(CultureInfo.InvariantCulture),
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture) ?? "",
        _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? ""
    };

    public static async Task<Dictionary<string, TableContentFingerprint>> SeedOracleXeAsync(string xeConnString, DatabaseSnapshot snapshot)
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

            return await ComputeOracleTableContentFingerprintsAsync(conn);
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
        string preimageManifestSha = "",
        HistoricalDeltaSource? historicalDeltaSource = null)
    {
        var plan = new PlanResult
        {
            RunId = snapshot.RunId,
            EngineVersion = EngineVersion,
            GitSha = ResolveGitSha(),
            WorkbookSha256 = workbookSha,
            PreimageManifestSha256 = preimageManifestSha,
            Block2BaselineImportsSha256 = historicalDeltaSource?.BaselineImportsSha256 ?? "",
            Block2DataConflictsSha256 = historicalDeltaSource?.DataConflictsSha256 ?? "",
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
        // The Consolidado query filters EVA_ACTIVO=1. Do not select an arbitrary historical row.
        var activeEvaluationGroups = snapshot.Evaluations.Where(e => e.Active == 1)
            .GroupBy(e => e.RiskCode.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);
        var evasByCode = new Dictionary<string, EvaluationPreimage>(StringComparer.OrdinalIgnoreCase);
        foreach (string code in excelCodes)
        {
            if (!activeEvaluationGroups.TryGetValue(code, out var matches) || matches.Count != 1)
                throw new InvalidOperationException($"ACTIVE_EVALUATION_SELECTION_NOT_UNIQUE: risk={code}; activeCount={matches?.Count ?? 0}.");
            evasByCode.Add(code, matches[0]);
        }
        var selectedEvaluationIds = evasByCode.Values.Select(e => e.Id).ToHashSet();
        var projectionGroups = snapshot.Projections.Where(p => selectedEvaluationIds.Contains(p.EvaluationId))
            .GroupBy(p => p.RiskCode.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);
        var proysByCode = new Dictionary<string, ProjectionPreimage>(StringComparer.OrdinalIgnoreCase);
        foreach (string code in excelCodes)
        {
            if (!projectionGroups.TryGetValue(code, out var matches) || matches.Count != 1)
                throw new InvalidOperationException($"ACTIVE_PROJECTION_SELECTION_NOT_UNIQUE: risk={code}; projectionCount={matches?.Count ?? 0}.");
            proysByCode.Add(code, matches[0]);
        }

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
                            dbRaw = ProjectEvidenceByControl(prevCons, snapshot.Evidences);
                            dbCanon = dbRaw;
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
                            dbRaw = ProjectEvidenceByControl(detCons, snapshot.Evidences);
                            dbCanon = dbRaw;
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
                            dbRaw = ProjectEvidenceByControl(corCons, snapshot.Evidences);
                            dbCanon = dbRaw;
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
                bool hasExcelVal = !string.IsNullOrWhiteSpace(cellVal);
                bool semanticMatch = hasDbVal && hasExcelVal && string.Equals(NormalizeMultiline(dbRaw), NormalizeMultiline(cellVal), StringComparison.Ordinal);
                string classif = hasDbVal ? semanticMatch ? "MATCH" : "PRESERVE_PRODUCTION" : hasExcelVal ? "MISSING_IN_DB" : "LEGITIMATELY_BLANK";
                string act = hasDbVal ? semanticMatch ? "NO_ACTION" : "PRESERVE_PRODUCTION" : hasExcelVal ? "BLOCKED_BY_PRECONDITION" : "NO_ACTION";
                if (!hasDbVal && hasExcelVal)
                {
                    plan.BlockedItems.Add(new BlockedItem
                    {
                        RiskCode = code, FieldNumber = fn, FieldLabel = label, Entity = targetTbl,
                        SourceValue = cellVal, RequiredMissingValue = "Verified operational import authority and a supported mutation mapping",
                        ReasonCode = "OPERATIONAL_VALUE_REQUIRES_AUTHORITY",
                        RecommendedResolution = "Do not default or write operational monitoring/evidence data in this pass."
                    });
                }

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
                    PreservedInDb = hasDbVal && !semanticMatch
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
                string? canonicalRiskType = NormalizeCatalogValue("RISK_TYPE", evalTipo, catalogMaps, plan.CatalogNormalizations);
                if (f is 17 or 18 or 19 && canonicalRiskType == null)
                {
                    plan.BlockedItems.Add(new BlockedItem
                    {
                        RiskCode = code, FieldNumber = f, FieldLabel = label, Entity = "RISK_TYPE",
                        SourceValue = evalTipo, RequiredMissingValue = "A value explicitly listed in RISK_TYPE catalog",
                        ReasonCode = "UNKNOWN_CATALOG_VALUE",
                        RecommendedResolution = "Resolve the risk type against the canonical catalog before deciding applicability."
                    });
                    plan.AllPositions.Add(new ReconciliationPosition
                    {
                        PositionKey = positionKey, RiskCode = code, FieldNumber = f, FieldLabel = label,
                        Action = "BLOCKED_BY_PRECONDITION", ReasonCode = "UNKNOWN_CATALOG_VALUE",
                        ExcelValue = rawExcel, DbValue = evalTipo, Mutates = false
                    });
                    continue;
                }
                if (f is 17 or 18 or 19 && !IsGticApplicable(canonicalRiskType!, ""))
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

                    if (!string.IsNullOrWhiteSpace(db70) &&
                        !string.IsNullOrWhiteSpace(rawExcel) &&
                        NormalizeMultiline(rawExcel).Equals(NormalizeMultiline(db70), StringComparison.OrdinalIgnoreCase))
                    {
                        plan.MatchCount++;
                        plan.NoActionCount++;
                        plan.AllPositions.Add(new ReconciliationPosition
                        {
                            PositionKey = positionKey,
                            RiskCode = code,
                            FieldNumber = 70,
                            FieldLabel = label,
                            Action = "NO_ACTION",
                            ReasonCode = "SEMANTIC_MATCH",
                            ExcelValue = rawExcel,
                            DbValue = db70,
                            TargetValue = db70,
                            Mutates = false
                        });
                        continue;
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
                            ReasonCode = "CONTRACT_AUTHORITY_RULE",
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
                    if (aud.Action == "PRESERVE_PRODUCTION")
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
                    else if (aud.Action == "BLOCKED_BY_PRECONDITION")
                    {
                        plan.AllPositions.Add(new ReconciliationPosition
                        {
                            PositionKey = positionKey, RiskCode = code, FieldNumber = f, FieldLabel = label,
                            Action = "BLOCKED_BY_PRECONDITION", ReasonCode = "OPERATIONAL_VALUE_REQUIRES_AUTHORITY",
                            ExcelValue = rawExcel, DbValue = aud.DbBeforeRaw, TargetValue = aud.DbBeforeRaw, Mutates = false
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
                                RollbackSql = "UPDATE RL_MR_RIESGOS SET RIE_DESCRIPCION = :beforeDesc WHERE RIE_ID = :id AND NVL(RIE_DESCRIPCION, ' ') = :afterDesc",
                                RollbackParameters = new Dictionary<string, object?>
                                {
                                    ["id"] = risk.Id,
                                    ["beforeDesc"] = dbDesc,
                                    ["afterDesc"] = excelDesc
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

                    SourceItemsParseResult parsedControls = ParseSourceItems(rawExcel);
                    if (parsedControls.IsAmbiguous)
                    {
                        AddParserAmbiguity(plan, code, f, label, "RL_MR_CONTROLES_RIESGO", rawExcel);
                        continue;
                    }
                    var excelItems = parsedControls.Items.ToList();
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

                    int controlsBlockedForField = 0;
                    for (int i = 0; i < excelItems.Count; i++)
                    {
                        string targetDesc = excelItems[i].Trim();
                        if (string.IsNullOrWhiteSpace(targetDesc))
                        {
                            plan.BlockedItems.Add(new BlockedItem
                            {
                                RiskCode = code, FieldNumber = f, FieldLabel = label, Entity = "RL_MR_CONTROLES_RIESGO",
                                SourceValue = $"{i + 1}. <empty>", RequiredMissingValue = "CON_DESCRIPCION (NOT NULL; numbered source item has no description)",
                                ReasonCode = "BLOCKED_REQUIRED_INPUT",
                                RecommendedResolution = "Confirm whether this numbered source item is intentionally empty or provide its control description."
                            });
                            controlsBlockedForField++;
                            continue;
                        }
                        if (IsEmptyOrNoHay(targetDesc)) continue;
                        if (targetDesc.Length > 500)
                        {
                            plan.SilentTruncationsCount++;
                            throw new InvalidOperationException($"VALUE_EXCEEDS_TARGET_LENGTH: Control {conTipo} de {code} tiene {targetDesc.Length} caracteres (máx 500).");
                        }
                        bool exists = existingList.Any(c => NormalizeControlText(c.Descripcion).Equals(NormalizeControlText(targetDesc), StringComparison.OrdinalIgnoreCase));
                        if (exists) { plan.MatchCount++; continue; }

                        plan.BlockedItems.Add(new BlockedItem
                        {
                            RiskCode = code, FieldNumber = f, FieldLabel = label, Entity = "RL_MR_CONTROLES_RIESGO",
                            SourceValue = targetDesc, RequiredMissingValue = "CON_ESTADO (NOT NULL; no source value or DB default)",
                            ReasonCode = "BLOCKED_REQUIRED_INPUT",
                            RecommendedResolution = "Provide CON_ESTADO from an authoritative source; the reconciler will not invent ACTIVO or automation."
                        });
                        controlsBlockedForField++;
                    }
                    if (controlsBlockedForField > 0)
                    {
                        plan.AllPositions.Add(new ReconciliationPosition
                        {
                            PositionKey = positionKey, RiskCode = code, FieldNumber = f, FieldLabel = label,
                            Action = "BLOCKED_BY_PRECONDITION", ReasonCode = "BLOCKED_REQUIRED_INPUT",
                            ExcelValue = rawExcel, DbValue = dbControlsText, TargetValue = dbControlsText, Mutates = false
                        });
                    }
                    else
                    {
                        plan.MatchCount++;
                        plan.NoActionCount++;
                        plan.AllPositions.Add(new ReconciliationPosition
                        {
                            PositionKey = positionKey, RiskCode = code, FieldNumber = f, FieldLabel = label,
                            Action = "NO_ACTION", ReasonCode = "ALL_EXCEL_CONTROLS_MATCH_EXISTING_CONTROLS",
                            ExcelValue = rawExcel, DbValue = dbControlsText, TargetValue = dbControlsText, Mutates = false
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
                            string? currentAutoCanonical = NormalizeCatalogValue("CONTROL_AUTOMATION", currentAuto, catalogMaps, plan.CatalogNormalizations);
                            if (!string.Equals(currentAutoCanonical, f32Canonical, StringComparison.Ordinal))
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
                                        ["newAuto"] = ResolveCatalogDbKey(catalogDoc, "CONTROL_AUTOMATION", f32Canonical),
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
                                        RollbackSql = "UPDATE RL_MR_CONTROLES_RIESGO SET CON_AUTOMATIZACION = :beforeAuto WHERE CON_ID = :id AND CON_AUTOMATIZACION = :afterAuto",
                                        RollbackParameters = new Dictionary<string, object?>
                                        {
                                            ["id"] = con.Id,
                                            ["beforeAuto"] = currentAuto,
                                            ["afterAuto"] = ResolveCatalogDbKey(catalogDoc, "CONTROL_AUTOMATION", f32Canonical)
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
                    var existingPlans = (existingPlansByEva.TryGetValue(evaId, out var epList) ? epList : new List<PlanPreimage>())
                        .OrderBy(p => p.Id)
                        .ToList();
                    string dbPlansText = ProjectPlanValues(existingPlans.Select(p => p.Descripcion));
                    SourceItemsParseResult parsedPlans = ParseSourceItems(rawExcel);
                    if (parsedPlans.IsAmbiguous)
                    {
                        AddParserAmbiguity(plan, code, f, label, "RL_MR_PLANES", rawExcel);
                        continue;
                    }
                    var excelPlans = parsedPlans.Items.Select(NormalizeControlText).ToList();
                    if (excelPlans.Count == 0)
                    {
                        bool hasDbPlans = existingPlans.Count > 0;
                        if (hasDbPlans) plan.PreserveProductionCount++; else plan.NoActionCount++;
                        plan.AllPositions.Add(new ReconciliationPosition
                        {
                            PositionKey = positionKey, RiskCode = code, FieldNumber = 40, FieldLabel = label,
                            Action = hasDbPlans ? "PRESERVE_PRODUCTION" : "NO_ACTION",
                            ReasonCode = hasDbPlans ? "EXCEL_BLANK_DB_PLANS_PRESERVED" : "BLANK_EXCEL_AND_DB",
                            ExcelValue = "", DbValue = dbPlansText, TargetValue = dbPlansText, Mutates = false
                        });
                        continue;
                    }

                    var unmatchedPlans = excelPlans
                        .Where(item => !existingPlans.Any(p => NormalizeControlText(p.Descripcion).Equals(item, StringComparison.OrdinalIgnoreCase)))
                        .ToList();
                    if (unmatchedPlans.Count == 0)
                    {
                        plan.MatchCount++; plan.NoActionCount++;
                        plan.AllPositions.Add(new ReconciliationPosition
                        {
                            PositionKey = positionKey, RiskCode = code, FieldNumber = 40, FieldLabel = label,
                            Action = "NO_ACTION", ReasonCode = "ALL_EXCEL_PLANS_MATCH_EXISTING_PLANS",
                            ExcelValue = rawExcel, DbValue = dbPlansText, TargetValue = dbPlansText, Mutates = false
                        });
                        continue;
                    }

                    string f46FIni = GetCellTextValue(r, 46);
                    string f47FFin = GetCellTextValue(r, 47);
                    string reason = "BLOCKED_REQUIRED_INPUT";
                    foreach (var missingPlan in unmatchedPlans)
                    {
                        string missingInputs = string.Join(", ", new[]
                        {
                            string.IsNullOrWhiteSpace(missingPlan) ? "PLA_DESCRIPCION" : "",
                            string.IsNullOrWhiteSpace(f46FIni) ? "PLA_FECHA_INICIO" : "",
                        string.IsNullOrWhiteSpace(f47FFin) ? "PLA_FECHA_FIN" : "",
                        "PLA_ESTADO (NOT NULL; no baseline field or DB default)"
                    }.Where(x => x.Length > 0));
                        plan.BlockedItems.Add(new BlockedItem
                        {
                            RiskCode = code, FieldNumber = 40, FieldLabel = label, Entity = "RL_MR_PLANES",
                            SourceValue = missingPlan, RequiredMissingValue = missingInputs,
                            ReasonCode = reason,
                            RecommendedResolution = "Supply every NOT NULL plan value from an authoritative source; do not synthesize dates or status."
                        });
                    }
                    plan.AllPositions.Add(new ReconciliationPosition
                    {
                        PositionKey = positionKey, RiskCode = code, FieldNumber = 40, FieldLabel = label,
                        Action = "BLOCKED_BY_PRECONDITION", ReasonCode = reason,
                        ExcelValue = rawExcel, DbValue = dbPlansText, TargetValue = dbPlansText, Mutates = false
                    });
                    continue;
                }
                if (f == 42)
                {
                    var existingPlans = (existingPlansByEva.TryGetValue(evaId, out var epList) ? epList : new List<PlanPreimage>())
                        .OrderBy(p => p.Id)
                        .ToList();
                    SourceItemsParseResult parsedActivities = ParseSourceItems(rawExcel, allowHierarchicalNumbering: true);
                    if (parsedActivities.IsAmbiguous)
                    {
                        AddParserAmbiguity(plan, code, f, label, "RL_MR_ACTIVIDADES", rawExcel);
                        continue;
                    }
                    var excelActivities = parsedActivities.Items.Select(NormalizeControlText).ToList();
                    var allActivities = existingPlans
                        .SelectMany(p => existingActivitiesByPlan.TryGetValue(p.Id, out var acts) ? acts : new List<ActivityPreimage>())
                        .ToList();
                    string dbActivitiesText = ProjectPlanValues(allActivities.Select(a => a.Descripcion));
                    if (excelActivities.Count == 0)
                    {
                        bool hasDbActivities = allActivities.Count > 0;
                        if (hasDbActivities) plan.PreserveProductionCount++; else plan.NoActionCount++;
                        plan.AllPositions.Add(new ReconciliationPosition
                        {
                            PositionKey = positionKey, RiskCode = code, FieldNumber = 42, FieldLabel = label,
                            Action = hasDbActivities ? "PRESERVE_PRODUCTION" : "NO_ACTION",
                            ReasonCode = hasDbActivities ? "EXCEL_BLANK_DB_ACTIVITIES_PRESERVED" : "BLANK_EXCEL_AND_DB",
                            ExcelValue = "", DbValue = dbActivitiesText, TargetValue = dbActivitiesText, Mutates = false
                        });
                        continue;
                    }

                    var unresolvedActivities = new List<string>();
                    bool ambiguousParent = false;
                    foreach (var item in excelActivities)
                    {
                        var parents = allActivities
                            .Where(a => NormalizeControlText(a.Descripcion).Equals(item, StringComparison.OrdinalIgnoreCase))
                            .Select(a => a.PlanId).Distinct().ToList();
                        if (parents.Count > 1) ambiguousParent = true;
                        if (parents.Count != 1) unresolvedActivities.Add(item);
                    }
                    if (unresolvedActivities.Count == 0)
                    {
                        plan.MatchCount++; plan.NoActionCount++;
                        plan.AllPositions.Add(new ReconciliationPosition
                        {
                            PositionKey = positionKey, RiskCode = code, FieldNumber = 42, FieldLabel = label,
                            Action = "NO_ACTION", ReasonCode = "ALL_ACTIVITIES_MATCH_WITH_UNIQUE_PLAN_PARENT",
                            ExcelValue = rawExcel, DbValue = dbActivitiesText, TargetValue = dbActivitiesText, Mutates = false
                        });
                        continue;
                    }

                    if (ambiguousParent || (existingPlans.Count > 1 && allActivities.Count == 0))
                    {
                        plan.RejectAmbiguousCount++;
                        plan.AllPositions.Add(new ReconciliationPosition
                        {
                            PositionKey = positionKey, RiskCode = code, FieldNumber = 42, FieldLabel = label,
                            Action = "REJECT_AMBIGUOUS", ReasonCode = "ACTIVITY_PARENT_NOT_UNIQUELY_RESOLVED",
                            ExcelValue = rawExcel, DbValue = dbActivitiesText, TargetValue = dbActivitiesText, Mutates = false
                        });
                        continue;
                    }

                    string reason = existingPlans.Count == 0 ? "BLOCKED_NO_PARENT" : "BLOCKED_REQUIRED_INPUT";
                    string missing = existingPlans.Count == 0
                        ? "ACT_PLAN_ID (no persisted parent plan)"
                        : "ACT_RESPONSABLE, ACT_FECHA_INICIO, ACT_FECHA_FIN, ACT_ESTADO (required columns without source values/defaults)";
                    foreach (var item in unresolvedActivities)
                    {
                        plan.BlockedItems.Add(new BlockedItem
                        {
                            RiskCode = code, FieldNumber = 42, FieldLabel = label, Entity = "RL_MR_ACTIVIDADES",
                            SourceValue = item, RequiredMissingValue = missing, ReasonCode = reason,
                            RecommendedResolution = existingPlans.Count == 0
                                ? "Resolve the exact parent plan before creating the activity."
                                : "Supply all NOT NULL activity values from an authoritative source; do not synthesize them."
                        });
                    }
                    plan.AllPositions.Add(new ReconciliationPosition
                    {
                        PositionKey = positionKey, RiskCode = code, FieldNumber = 42, FieldLabel = label,
                        Action = "BLOCKED_BY_PRECONDITION", ReasonCode = reason,
                        ExcelValue = rawExcel, DbValue = dbActivitiesText, TargetValue = dbActivitiesText, Mutates = false
                    });
                    continue;
                }
                if (f == 45)
                {
                    var existingPlans = (existingPlansByEva.TryGetValue(evaId, out var epList) ? epList : new List<PlanPreimage>())
                        .OrderBy(p => p.Id)
                        .ToList();
                    string dbResponsibles = ProjectPlanValues(existingPlans.Select(p => p.Responsables));
                    if (string.IsNullOrWhiteSpace(rawExcel))
                    {
                        bool hasOperationalValue = existingPlans.Any(p => !string.IsNullOrWhiteSpace(p.Responsables));
                        string action = hasOperationalValue ? "PRESERVE_PRODUCTION" : "NO_ACTION";
                        if (hasOperationalValue) plan.PreserveProductionCount++; else plan.NoActionCount++;
                        plan.AllPositions.Add(new ReconciliationPosition
                        {
                            PositionKey = positionKey, RiskCode = code, FieldNumber = 45, FieldLabel = label,
                            Action = action,
                            ReasonCode = hasOperationalValue ? "EXCEL_BLANK_DB_VALUE_PRESERVED" : "BLANK_EXCEL_AND_DB",
                            ExcelValue = "", DbValue = dbResponsibles, TargetValue = dbResponsibles, Mutates = false
                        });
                        continue;
                    }

                    if (existingPlans.Count == 0)
                    {
                        plan.BlockedItems.Add(new BlockedItem
                        {
                            RiskCode = code, FieldNumber = 45, FieldLabel = label,
                            Entity = "RL_MR_PLANES", SourceValue = rawExcel,
                            RequiredMissingValue = "PLA_RESPONSABLES (Parent plan missing in DB)",
                            ReasonCode = "BLOCKED_NO_PARENT",
                            RecommendedResolution = "Resolve and create the corresponding plan before reconciling its responsible value."
                        });
                        plan.AllPositions.Add(new ReconciliationPosition
                        {
                            PositionKey = positionKey, RiskCode = code, FieldNumber = 45, FieldLabel = label,
                            Action = "BLOCKED_BY_PRECONDITION", ReasonCode = "BLOCKED_NO_PARENT",
                            ExcelValue = rawExcel, DbValue = "", TargetValue = "", Mutates = false
                        });
                        continue;
                    }

                    SourceItemsParseResult parsedResponsibles = ParseSourceItems(rawExcel);
                    if (parsedResponsibles.IsAmbiguous)
                    {
                        AddParserAmbiguity(plan, code, f, label, "RL_MR_PLANES.PLA_RESPONSABLES", rawExcel);
                        continue;
                    }
                    var excelResponsibles = parsedResponsibles.Items.Select(NormalizeControlText).ToList();
                    if (excelResponsibles.Count != existingPlans.Count)
                    {
                        plan.RejectAmbiguousCount++;
                        plan.AllPositions.Add(new ReconciliationPosition
                        {
                            PositionKey = positionKey, RiskCode = code, FieldNumber = 45, FieldLabel = label,
                            Action = "REJECT_AMBIGUOUS", ReasonCode = "PLAN_RESPONSIBLE_CARDINALITY_MISMATCH",
                            ExcelValue = rawExcel, DbValue = dbResponsibles, TargetValue = dbResponsibles, Mutates = false
                        });
                        continue;
                    }

                    bool allEqual = existingPlans.Select((p, i) => string.Equals(
                        NormalizeControlText(p.Responsables ?? ""), excelResponsibles[i], StringComparison.OrdinalIgnoreCase)).All(x => x);
                    if (allEqual)
                    {
                        plan.MatchCount++; plan.NoActionCount++;
                        plan.AllPositions.Add(new ReconciliationPosition
                        {
                            PositionKey = positionKey, RiskCode = code, FieldNumber = 45, FieldLabel = label,
                            Action = "NO_ACTION", ReasonCode = "PLAN_RESPONSIBLES_MATCH_BY_PLAN_ORDER",
                            ExcelValue = rawExcel, DbValue = dbResponsibles, TargetValue = dbResponsibles, Mutates = false
                        });
                    }
                    else
                    {
                        plan.PreserveProductionCount++;
                        plan.AllPositions.Add(new ReconciliationPosition
                        {
                            PositionKey = positionKey, RiskCode = code, FieldNumber = 45, FieldLabel = label,
                            Action = "PRESERVE_PRODUCTION", ReasonCode = "PLAN_RESPONSIBLES_DIFFERENCE_REQUIRES_REVIEW",
                            ExcelValue = rawExcel, DbValue = dbResponsibles, TargetValue = dbResponsibles, Mutates = false
                        });
                    }
                    continue;
                }
                if (f is 44 or 46 or 47 or 48 or 49)
                {
                    var existingPlans = existingPlansByEva.TryGetValue(evaId, out var epList) ? epList : new List<PlanPreimage>();
                    string dbVal = "";
                    if (existingPlans.Count > 0)
                    {
                        if (f == 44) dbVal = ProjectPlanValues(existingPlans.OrderBy(x => x.Id).Select(p => p.MonitoreoSeguimiento));
                        else if (f == 46) dbVal = ProjectPlanValues(existingPlans.OrderBy(x => x.Id).Select(p => p.FechaInicio != DateTime.MinValue ? p.FechaInicio.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : ""));
                        else if (f == 47) dbVal = ProjectPlanValues(existingPlans.OrderBy(x => x.Id).Select(p => p.FechaFin != DateTime.MinValue ? p.FechaFin.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : ""));
                        else if (f == 48) dbVal = ProjectPlanValues(existingPlans.OrderBy(x => x.Id).Select(p => p.Recursos));
                        else if (f == 49) dbVal = ProjectPlanValues(existingPlans.OrderBy(x => x.Id).Select(p => p.Presupuesto.ToString("0.##", CultureInfo.InvariantCulture)));
                    }

                    bool excelBlank = string.IsNullOrWhiteSpace(rawExcel);
                    bool databaseHasValue = !string.IsNullOrWhiteSpace(dbVal);
                    bool semanticallyEqual = !excelBlank && string.Equals(NormalizeMultiline(rawExcel), NormalizeMultiline(dbVal), StringComparison.OrdinalIgnoreCase);
                    bool missingTargetValue = !excelBlank && !databaseHasValue;
                    string action = databaseHasValue && (excelBlank || !semanticallyEqual)
                        ? "PRESERVE_PRODUCTION"
                        : missingTargetValue ? "BLOCKED_BY_PRECONDITION" : "NO_ACTION";
                    string reason = excelBlank
                        ? databaseHasValue ? "EXCEL_BLANK_DB_VALUE_PRESERVED" : "BLANK_EXCEL_AND_DB"
                        : semanticallyEqual ? "SEMANTIC_MATCH" : databaseHasValue ? "DB_PLAN_VALUE_REQUIRES_REVIEW" : "BASELINE_VALUE_REQUIRES_PERSISTENCE_MAPPING";
                    if (action == "PRESERVE_PRODUCTION") plan.PreserveProductionCount++;
                    else if (action == "NO_ACTION") plan.NoActionCount++;
                    if (missingTargetValue)
                    {
                        plan.BlockedItems.Add(new BlockedItem
                        {
                            RiskCode = code, FieldNumber = f, FieldLabel = label, Entity = "RL_MR_PLANES",
                            SourceValue = rawExcel, RequiredMissingValue = "Verified one-to-one plan mapping and guarded UPDATE implementation",
                            ReasonCode = reason,
                            RecommendedResolution = "Do not mark a nonempty workbook value as NO_ACTION while the persisted target is blank."
                        });
                    }
                    plan.AllPositions.Add(new ReconciliationPosition
                    {
                        PositionKey = positionKey,
                        RiskCode = code,
                        FieldNumber = f,
                        FieldLabel = label,
                        Action = action,
                        ReasonCode = reason,
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
                        RollbackSql = "UPDATE RL_MR_EVALUACIONES_RIESGO SET EVA_DATOS_JSON = :beforeJson, EVA_VERSION_ROW = :beforeRow WHERE EVA_ID = :id AND EVA_VERSION_ROW = :afterRow AND EVA_DATOS_JSON = :afterJson",
                        RollbackParameters = new Dictionary<string, object?>
                        {
                            ["id"] = eva.Id,
                            ["beforeJson"] = beforeJson,
                            ["beforeRow"] = expectedVersionRow,
                            ["afterRow"] = expectedVersionRow + 1,
                            ["afterJson"] = newJson
                        }
                    }
                };

                plan.Mutations.Add(mut);
                plan.RollbackOperations.Add(mut.Rollback);
            }
        }

        // 12. Construir Detectores Reales de Duplicados y Null Overwrites
        BuildGuardEntries(snapshot, plan);
        var forbiddenSyntheticValues = new HashSet<string>(new[] { "PENDIENTE", "MANUAL", "ACTIVO" }, StringComparer.OrdinalIgnoreCase);
        plan.SyntheticDefaultsCount = plan.Mutations.Sum(m =>
            Regex.Matches(m.ApplySql, @"\b(?:SYSDATE|SYSTIMESTAMP)\b", RegexOptions.IgnoreCase).Count +
            Regex.Matches(m.ApplySql, @"'[^']+'", RegexOptions.IgnoreCase).Count +
            m.ApplyParameters.Values.Count(v => v is string value && forbiddenSyntheticValues.Contains(value) &&
                !string.Equals(value, m.ExcelRaw, StringComparison.Ordinal)));

        int expectedPositionCount = excelRows.Count * 82;
        int uniquePositionCount = plan.AllPositions.Select(p => p.PositionKey).Distinct(StringComparer.OrdinalIgnoreCase).Count();
        int unclassifiedCount = plan.AllPositions.Count(p => string.IsNullOrWhiteSpace(p.Action) || string.IsNullOrWhiteSpace(p.PositionKey));
        if (plan.AllPositions.Count != expectedPositionCount || uniquePositionCount != expectedPositionCount || unclassifiedCount > 0)
            throw new InvalidOperationException($"AUDIT_UNIVERSE_INVALID: expected={expectedPositionCount}; actual={plan.AllPositions.Count}; unique={uniquePositionCount}; unclassified={unclassifiedCount}.");

        // 13. Construir Deltas Históricos 398 y 87
        if (historicalDeltaSource != null)
        {
            BuildHistoricalDeltas(plan, historicalDeltaSource);
        }

        // 14. Hashes Finales del Plan (Canónico y Archivo)
        plan.PlanCanonicalSha256 = ComputeComprehensivePlanSha256(plan, workbookSha, preimageManifestSha);
        plan.PlanSha256 = plan.PlanCanonicalSha256;

        return plan;
    }

    public static HistoricalDeltaSource LoadCertifiedHistoricalDeltaSource(string tempRoot)
    {
        foreach (var directory in Directory.EnumerateDirectories(tempRoot, "RIESGO_LAVADO_BLOCK2_AUDIT_*", SearchOption.TopDirectoryOnly))
        {
            string importsPath = Path.Combine(directory, "baseline_import_candidates.csv");
            string conflictsPath = Path.Combine(directory, "data_conflicts.csv");
            if (!File.Exists(importsPath) || !File.Exists(conflictsPath)) continue;

            string importsSha = ComputeFileSha256(importsPath);
            string conflictsSha = ComputeFileSha256(conflictsPath);
            if (!importsSha.Equals(ExpectedBlock2BaselineImportsSha256, StringComparison.OrdinalIgnoreCase) ||
                !conflictsSha.Equals(ExpectedBlock2DataConflictsSha256, StringComparison.OrdinalIgnoreCase)) continue;

            var imports = ReadCsvRecords(importsPath);
            var conflicts = ReadCsvRecords(conflictsPath);
            ValidateHistoricalDeltaSet(imports.Select(r => $"{r.Get("RISK_CODE")}|{ParseFieldNumber(r.Get("FIELD_NUMBER"))}"), 398, "BLOCK2_398_SOURCE");
            ValidateHistoricalDeltaSet(conflicts.Select(r => $"{r.Get("RISK_CODE")}|{ParseFieldNumber(r.Get("FIELD_NUMBER"))}"), 87, "BLOCK2_87_SOURCE");
            return new HistoricalDeltaSource
            {
                BaselineImports = imports,
                DataConflicts = conflicts,
                BaselineImportsSha256 = importsSha,
                DataConflictsSha256 = conflictsSha,
                SourceDirectory = directory
            };
        }

        throw new FileNotFoundException("No se encontraron los CSV forenses originales de Bloque 2 con hashes SHA-256 certificados; no se generarán keysets sintéticos.");
    }

    private static List<HistoricalDeltaRow> ReadCsvRecords(string path)
    {
        string text = File.ReadAllText(path, Encoding.UTF8);
        var records = new List<List<string>>();
        var record = new List<string>();
        var field = new StringBuilder();
        bool quoted = false;
        for (int i = 0; i < text.Length; i++)
        {
            char ch = text[i];
            if (quoted)
            {
                if (ch == '"' && i + 1 < text.Length && text[i + 1] == '"') { field.Append('"'); i++; }
                else if (ch == '"') quoted = false;
                else field.Append(ch);
                continue;
            }
            if (ch == '"' && field.Length == 0) { quoted = true; continue; }
            if (ch == ',') { record.Add(field.ToString()); field.Clear(); continue; }
            if (ch is '\r' or '\n')
            {
                if (ch == '\r' && i + 1 < text.Length && text[i + 1] == '\n') i++;
                record.Add(field.ToString()); field.Clear();
                if (record.Any(v => v.Length > 0)) records.Add(record);
                record = new List<string>();
                continue;
            }
            field.Append(ch);
        }
        if (quoted) throw new InvalidDataException($"CSV inválido: campo sin cierre de comillas en {Path.GetFileName(path)}.");
        if (field.Length > 0 || record.Count > 0) { record.Add(field.ToString()); records.Add(record); }
        if (records.Count < 2) throw new InvalidDataException($"CSV sin filas de datos: {Path.GetFileName(path)}.");

        var headers = records[0].Select(h => h.Trim().TrimStart('\uFEFF')).ToList();
        return records.Skip(1).Select(values =>
        {
            if (values.Count != headers.Count) throw new InvalidDataException($"CSV con cardinalidad de columnas inválida en {Path.GetFileName(path)}.");
            return new HistoricalDeltaRow
            {
                Values = headers.Select((header, index) => new { header, value = values[index] })
                    .ToDictionary(x => x.header, x => x.value, StringComparer.OrdinalIgnoreCase)
            };
        }).ToList();
    }

    private static void BuildHistoricalDeltas(PlanResult plan, HistoricalDeltaSource source)
    {
        var positions = plan.AllPositions
            .GroupBy(p => $"{p.RiskCode}|{p.FieldNumber}", StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Count() == 1 ? g.Single() : null, StringComparer.OrdinalIgnoreCase);

        foreach (var row in source.BaselineImports)
        {
            int field = ParseFieldNumber(row.Get("FIELD_NUMBER"));
            var key = $"{row.Get("RISK_CODE").Trim()}|{field}";
            positions.TryGetValue(key, out var current);
            if (current == null) throw new InvalidOperationException($"BLOCK2_398_POSITION_NOT_UNIQUELY_MAPPED: {key}");
            plan.Historical398Deltas.Add(new Delta398Record
            {
                RiskCode = row.Get("RISK_CODE"),
                FieldNumber = field,
                FieldLabel = row.Get("FIELD_LABEL"),
                PreviousClassification = "MISSING_IN_DB",
                OriginalExcelRaw = row.Get("EXCEL_VALUE"),
                OriginalDbRaw = row.Get("CURRENT_DB_VALUE"),
                OriginalReasonCode = row.Get("TARGET_POLICY"),
                OriginalRecommendedAction = row.Get("NEXT_ACTION"),
                CurrentDbRaw = current?.DbValue ?? "",
                CurrentExcelRaw = current?.ExcelValue ?? "",
                CurrentClassification = ClassifyHistoricalImport(current),
                Reason = current?.ReasonCode ?? "POSITION_NOT_FOUND_IN_CURRENT_UNIVERSE",
                RecommendedAction = current?.Action ?? "SOURCE_REVIEW_REQUIRED",
                Evidence = current == null ? "NO_UNIQUE_CURRENT_POSITION" : $"POSITION_KEY={current.PositionKey};ACTION={current.Action};REASON={current.ReasonCode}",
                MutationRequired = current?.Mutates ?? false
            });
        }

        foreach (var row in source.DataConflicts)
        {
            int field = ParseFieldNumber(row.Get("FIELD_NUMBER"));
            var key = $"{row.Get("RISK_CODE").Trim()}|{field}";
            positions.TryGetValue(key, out var current);
            if (current == null) throw new InvalidOperationException($"BLOCK2_87_POSITION_NOT_UNIQUELY_MAPPED: {key}");
            plan.Historical87Deltas.Add(new Delta87Record
            {
                RiskCode = row.Get("RISK_CODE"),
                FieldNumber = field,
                FieldLabel = row.Get("FIELD_LABEL"),
                PreviousClassification = "DIFFERENT",
                OriginalExcelRaw = row.Get("EXCEL_VALUE"),
                OriginalDbRaw = row.Get("DB_VALUE"),
                OriginalReasonCode = row.Get("REASON_CODE"),
                OriginalRecommendedAction = row.Get("RECOMMENDED_ACTION"),
                CurrentDbRaw = current?.DbValue ?? "",
                CurrentExcelRaw = current?.ExcelValue ?? "",
                CurrentClassification = ClassifyHistoricalConflict(current),
                Reason = current?.ReasonCode ?? "POSITION_NOT_FOUND_IN_CURRENT_UNIVERSE",
                RecommendedAction = current?.Action ?? "SOURCE_REVIEW_REQUIRED",
                Evidence = current == null ? "NO_UNIQUE_CURRENT_POSITION" : $"POSITION_KEY={current.PositionKey};ACTION={current.Action};REASON={current.ReasonCode}",
                MutationRequired = current?.Mutates ?? false
            });
        }

        ValidateHistoricalDeltaSet(plan.Historical398Deltas.Select(d => $"{d.RiskCode}|{d.FieldNumber}"), 398, "BLOCK2_398");
        ValidateHistoricalDeltaSet(plan.Historical87Deltas.Select(d => $"{d.RiskCode}|{d.FieldNumber}"), 87, "BLOCK2_87");
    }

    private static int ParseFieldNumber(string value) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int fieldNumber) ? fieldNumber : 0;

    private static void ValidateHistoricalDeltaSet(IEnumerable<string> keys, int expected, string name)
    {
        var values = keys.ToList();
        int unique = values.Distinct(StringComparer.OrdinalIgnoreCase).Count();
        if (values.Count != expected || unique != expected)
        {
            throw new InvalidOperationException($"{name}_KEYSET_INVALID: rows={values.Count}; unique={unique}; expected={expected}.");
        }
    }

    private static string ClassifyHistoricalImport(ReconciliationPosition? position)
    {
        if (position == null) return "OTHER_EXPLAINED";
        return position.Action switch
        {
            "NO_ACTION" when !string.IsNullOrWhiteSpace(position.DbValue) => "ALREADY_PRESENT",
            "NO_ACTION" => "OTHER_EXPLAINED",
            "PRESERVE_PRODUCTION" => "PRESERVE_PRODUCTION",
            "UPDATE_BASELINE" or "INSERT_BASELINE" => "STILL_MISSING_EXECUTABLE",
            "BLOCKED_BY_PRECONDITION" => "BLOCKED_REQUIRED_INPUT",
            "NOT_APPLICABLE" => "NOT_APPLICABLE",
            _ => "OTHER_EXPLAINED"
        };
    }

    private static string ClassifyHistoricalConflict(ReconciliationPosition? position)
    {
        if (position == null) return "OTHER_EXPLAINED";
        return position.Action switch
        {
            "NO_ACTION" when position.ReasonCode.Contains("SEMANTIC", StringComparison.OrdinalIgnoreCase) || position.ReasonCode.Contains("MATCH", StringComparison.OrdinalIgnoreCase) => "SEMANTIC_EQUAL",
            "NO_ACTION" => "OTHER_EXPLAINED",
            "PRESERVE_PRODUCTION" => "PRESERVE_OPERATIONAL",
            "UPDATE_BASELINE" or "INSERT_BASELINE" => "EXCEL_WINS",
            "BLOCKED_BY_PRECONDITION" => "BLOCKED",
            "NOT_APPLICABLE" => "NOT_APPLICABLE",
            _ => "OTHER_EXPLAINED"
        };
    }
    private static void BuildGuardEntries(DatabaseSnapshot snapshot, PlanResult plan)
    {
        var risksByCode = snapshot.Risks.GroupBy(r => r.Code).ToList();
        foreach (var grp in risksByCode)
        {
            plan.PreexistingDuplicates += Math.Max(0, grp.Count() - 1);
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
            plan.PreexistingDuplicates += Math.Max(0, grp.Count() - 1);
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

        foreach (var grp in snapshot.Plans.GroupBy(p => $"{p.EvaluationId}_{NormalizeControlText(p.Descripcion)}"))
        {
            int duplicates = Math.Max(0, grp.Count() - 1);
            plan.PreexistingDuplicates += duplicates;
            plan.DuplicateGuardEntries.Add(new DuplicateGuardEntry
            {
                EntityType = "RL_MR_PLANES", EntityKey = grp.Key,
                PreexistingCount = grp.Count(), PostApplyExpectedCount = grp.Count(), DuplicateCount = duplicates,
                Status = duplicates == 0 ? "PASS" : "DUPLICATE_FOUND"
            });
        }

        foreach (var grp in snapshot.Activities.GroupBy(a => $"{a.PlanId}_{NormalizeControlText(a.Descripcion)}"))
        {
            int duplicates = Math.Max(0, grp.Count() - 1);
            plan.PreexistingDuplicates += duplicates;
            plan.DuplicateGuardEntries.Add(new DuplicateGuardEntry
            {
                EntityType = "RL_MR_ACTIVIDADES", EntityKey = grp.Key,
                PreexistingCount = grp.Count(), PostApplyExpectedCount = grp.Count(), DuplicateCount = duplicates,
                Status = duplicates == 0 ? "PASS" : "DUPLICATE_FOUND"
            });
        }

        foreach (var grp in snapshot.Alerts.GroupBy(a => $"{a.EvaluationId}_{NormalizeControlText(a.Codigo)}_{NormalizeControlText(a.Indicador)}"))
        {
            int duplicates = Math.Max(0, grp.Count() - 1);
            plan.PreexistingDuplicates += duplicates;
            plan.DuplicateGuardEntries.Add(new DuplicateGuardEntry
            {
                EntityType = "RL_MR_SENALES_ALERTA", EntityKey = grp.Key,
                PreexistingCount = grp.Count(), PostApplyExpectedCount = grp.Count(), DuplicateCount = duplicates,
                Status = duplicates == 0 ? "PASS" : "DUPLICATE_FOUND"
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

        var insertedPlans = plan.Mutations.Where(m => m.TargetTable == "RL_MR_PLANES" && m.Action == "INSERT_BASELINE")
            .GroupBy(m => $"{m.RiskCode}_{NormalizeControlText(m.TargetCanonical)}");
        foreach (var grp in insertedPlans)
        {
            int expectedCount = grp.Count();
            int duplicates = Math.Max(0, expectedCount - 1);
            plan.NewDuplicatesExpected += duplicates;
            plan.DuplicateGuardEntries.Add(new DuplicateGuardEntry
            {
                EntityType = "NEW_PLANNED_PLAN", EntityKey = grp.Key, PreexistingCount = 0,
                PostApplyExpectedCount = expectedCount, DuplicateCount = duplicates,
                Status = duplicates == 0 ? "PASS" : "FAIL"
            });
        }

        var insertedActivities = plan.Mutations.Where(m => m.TargetTable == "RL_MR_ACTIVIDADES" && m.Action == "INSERT_BASELINE")
            .GroupBy(m => $"{m.RecordKey}_{NormalizeControlText(m.TargetCanonical)}");
        foreach (var grp in insertedActivities)
        {
            int expectedCount = grp.Count();
            int duplicates = Math.Max(0, expectedCount - 1);
            plan.NewDuplicatesExpected += duplicates;
            plan.DuplicateGuardEntries.Add(new DuplicateGuardEntry
            {
                EntityType = "NEW_PLANNED_ACTIVITY", EntityKey = grp.Key, PreexistingCount = 0,
                PostApplyExpectedCount = expectedCount, DuplicateCount = duplicates,
                Status = duplicates == 0 ? "PASS" : "FAIL"
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

    private static string ResolveGitSha()
    {
        var psi = new ProcessStartInfo("git")
        {
            WorkingDirectory = Directory.GetCurrentDirectory(),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        psi.ArgumentList.Add("rev-parse");
        psi.ArgumentList.Add("HEAD");
        using var process = Process.Start(psi) ?? throw new InvalidOperationException("GIT_SHA_UNAVAILABLE: no fue posible iniciar git.");
        string sha = process.StandardOutput.ReadToEnd().Trim();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0 || !Regex.IsMatch(sha, "^[0-9a-fA-F]{40}$"))
        {
            throw new InvalidOperationException($"GIT_SHA_UNAVAILABLE: {error.Trim()}");
        }
        return sha.ToLowerInvariant();
    }

    public static string ComputeCanonicalPlanSha256(PlanResult plan)
        => ComputeComprehensivePlanSha256(plan, plan.WorkbookSha256, plan.PreimageManifestSha256);

    private static string ComputeComprehensivePlanSha256(PlanResult plan, string workbookSha, string preimageManifestSha)
    {
        var payload = new
        {
            plan.RunId,
            engineVersion = EngineVersion,
            gitSha = plan.GitSha,
            workbookSha = workbookSha,
            preimageManifestSha = preimageManifestSha,
            plan.Block2BaselineImportsSha256,
            plan.Block2DataConflictsSha256,
            blockedItems = plan.BlockedItems.OrderBy(x => x.RiskCode, StringComparer.Ordinal).ThenBy(x => x.FieldNumber).Select(x => new
            {
                x.RiskCode, x.FieldNumber, x.FieldLabel, x.Entity, x.SourceValue,
                x.RequiredMissingValue, x.ReasonCode, x.RecommendedResolution
            }),
            positions = plan.AllPositions.OrderBy(x => x.RiskCode, StringComparer.Ordinal).ThenBy(x => x.FieldNumber).Select(x => new
            {
                x.PositionKey, x.RiskCode, x.FieldNumber, x.FieldLabel, x.Action,
                x.ReasonCode, x.ExcelValue, x.DbValue, x.TargetValue, x.Mutates
            }),
            mutations = plan.Mutations.Select(m => new
            {
                m.MutationId, m.RiskCode, m.FieldNumber, m.FieldLabel, m.Entity,
                m.TargetTable, m.RecordKey, m.TargetColumnOrProperty, m.Action,
                m.Authority, m.DbBeforeRaw, m.DbBeforeCanonical, m.ExcelRaw,
                m.ExcelCanonical, m.TargetCanonical, m.ReasonCode, m.Precondition,
                m.PreconditionSql, m.ApplySql,
                applyParameters = new SortedDictionary<string, object?>(m.ApplyParameters, StringComparer.Ordinal)
            }).ToList(),
            rollback = plan.RollbackOperations.Select(r => new
            {
                r.MutationId, r.Entity, r.Table, r.RecordPk, r.Action,
                r.RollbackAction, r.BeforeValue, r.AfterValue, r.RollbackSql,
                rollbackParameters = new SortedDictionary<string, object?>(r.RollbackParameters, StringComparer.Ordinal)
            }).ToList()
        };

        string json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = false });
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexStringLower(bytes);
    }

    #endregion

    #region Simulación Transaccional en Oracle XE

    public static async Task<SimulationResult> SimulateApplyOnOracleXeAsync(
        string xeConnString,
        DatabaseSnapshot productionSnapshot,
        PlanResult plan,
        List<JsonElement> excelRows,
        List<JsonElement> manifestFields,
        JsonDocument catalogDoc,
        string workbookSha,
        string preimageManifestSha,
        HistoricalDeltaSource? historicalDeltaSource = null)
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
                foreach (var mutation in plan.Mutations)
                {
                    applied += await ApplyMutationAsync(conn, tx, mutation);
                }
                result.FirstApplyPassed = true;
                result.FirstApplyMutationsApplied = applied;
                result.FirstFixtureMutations = applied;

                int verified = 0;
                int mismatches = 0;
                foreach (var mutation in plan.Mutations)
                {
                    if (await VerifyMutationAsync(conn, tx, mutation)) verified++; else mismatches++;
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
            var secondPlan = BuildReconciliationPlan(postXeSnapshot, excelRows, manifestFields, catalogDoc, workbookSha, preimageManifestSha, historicalDeltaSource);
            result.SecondApplyMutations = secondPlan.Mutations.Count;
            result.NonZeroFirstPlanPassed = result.FirstFixtureMutations > 0 && result.SecondApplyMutations == 0;
            var beforeByKey = plan.AllPositions.ToDictionary(p => p.PositionKey, StringComparer.OrdinalIgnoreCase);
            var afterByKey = secondPlan.AllPositions.ToDictionary(p => p.PositionKey, StringComparer.OrdinalIgnoreCase);
            var preservePositions = plan.AllPositions.Where(p => p.Action == "PRESERVE_PRODUCTION").ToList();
            result.OperationalPreservationChecked = preservePositions.Count;
            result.OperationalPreservationMismatches = preservePositions.Count(p =>
                !afterByKey.TryGetValue(p.PositionKey, out var after) ||
                !string.Equals(p.DbValue, after.DbValue, StringComparison.Ordinal));
            result.OperationalPreservationPassed = result.OperationalPreservationMismatches == 0 &&
                plan.AllPositions.Count == secondPlan.AllPositions.Count &&
                ComputeEntityFingerprint(productionSnapshot.Plans) == ComputeEntityFingerprint(postXeSnapshot.Plans) &&
                ComputeEntityFingerprint(productionSnapshot.Activities) == ComputeEntityFingerprint(postXeSnapshot.Activities) &&
                ComputeEntityFingerprint(productionSnapshot.Controls) == ComputeEntityFingerprint(postXeSnapshot.Controls) &&
                ComputeEntityFingerprint(productionSnapshot.Evidences) == ComputeEntityFingerprint(postXeSnapshot.Evidences);
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"Idempotency verification error: {ex.Message}";
            return result;
        }

        // FASE 2B: Non-zero idempotency fixture applied to and recaptured from XE.
        string? fixtureCode = null;
        string? fixtureBefore = null;
        string? fixtureTarget = null;
        long fixtureRiskId = 0;
        bool fixtureCommitted = false;
        try
        {
            var xeBefore = await CaptureProductionPreimageAsync(xeConnString, "XE_IDEMPOTENCY_FIXTURE_BEFORE");
            fixtureCode = xeBefore.Risks.Select(r => r.Code).FirstOrDefault() ?? throw new InvalidOperationException("IDEMPOTENCY_FIXTURE_RISK_MISSING");
            var sourceRisk = xeBefore.Risks.Single(r => r.Code == fixtureCode);
            fixtureRiskId = sourceRisk.Id;
            fixtureBefore = sourceRisk.Description;
            fixtureTarget = $"BLOCK4_IDEMPOTENCY_{Guid.NewGuid():N}";
            var fixture = new DatabaseSnapshot { RunId = "XE_IDEMPOTENCY_FIXTURE", CapturedAtUtc = DateTime.UtcNow };
            fixture.Risks.Add(sourceRisk);
            fixture.Evaluations.Add(xeBefore.Evaluations.Single(e => e.RiskCode == fixtureCode && e.Active == 1));
            fixture.Projections.Add(xeBefore.Projections.Single(p => p.RiskCode == fixtureCode && p.EvaluationId == fixture.Evaluations[0].Id));
            var fixtureCells = Enumerable.Range(1, 82).Select(field => (object)new
            {
                fieldNumber = field,
                textValue = field switch { 1 => "1", 2 => fixtureCode, 9 => fixtureTarget, _ => "" },
                formula = "",
                cachedResult = (object?)null
            }).ToList();
            using var fixtureRowDoc = JsonDocument.Parse(JsonSerializer.Serialize(new { riskNo = 1, riskCode = fixtureCode, cells = fixtureCells }));
            var fixturePlan = BuildReconciliationPlan(fixture, new List<JsonElement> { fixtureRowDoc.RootElement }, manifestFields, catalogDoc, workbookSha, preimageManifestSha);
            if (fixturePlan.Mutations.Count != 1 || fixturePlan.Mutations[0].TargetColumnOrProperty != "RIE_DESCRIPCION")
                throw new InvalidOperationException($"IDEMPOTENCY_FIXTURE_PLAN_INVALID: mutationCount={fixturePlan.Mutations.Count}.");
            result.FirstFixtureMutations = fixturePlan.Mutations.Count;
            await using (var connection = new OracleConnection(xeConnString))
            {
                await connection.OpenAsync();
                await using var transaction = connection.BeginTransaction();
                int affected = await ApplyMutationAsync(connection, transaction, fixturePlan.Mutations[0]);
                if (affected != 1 || !await VerifyMutationAsync(connection, transaction, fixturePlan.Mutations[0]))
                    throw new InvalidOperationException("IDEMPOTENCY_FIXTURE_FIRST_APPLY_FAILED");
                await transaction.CommitAsync();
                fixtureCommitted = true;
            }
            var xeAfter = await CaptureProductionPreimageAsync(xeConnString, "XE_IDEMPOTENCY_FIXTURE_AFTER");
            var fixtureAfter = new DatabaseSnapshot { RunId = "XE_IDEMPOTENCY_FIXTURE_AFTER", CapturedAtUtc = DateTime.UtcNow };
            fixtureAfter.Risks.Add(xeAfter.Risks.Single(r => r.Code == fixtureCode));
            fixtureAfter.Evaluations.Add(xeAfter.Evaluations.Single(e => e.RiskCode == fixtureCode && e.Active == 1));
            fixtureAfter.Projections.Add(xeAfter.Projections.Single(p => p.RiskCode == fixtureCode && p.EvaluationId == fixtureAfter.Evaluations[0].Id));
            var fixturePlanAfter = BuildReconciliationPlan(fixtureAfter, new List<JsonElement> { fixtureRowDoc.RootElement }, manifestFields, catalogDoc, workbookSha, preimageManifestSha);
            result.SecondFixtureMutations = fixturePlanAfter.Mutations.Count;
            result.NonZeroFirstPlanPassed = result.FirstFixtureMutations > 0 && result.SecondFixtureMutations == 0;
        }
        catch (Exception ex)
        {
            result.NonZeroFirstPlanPassed = false;
            result.ErrorMessage = $"XE non-zero idempotency fixture failed: {ex.Message}";
        }
        finally
        {
            if (fixtureCommitted && fixtureRiskId > 0 && fixtureBefore != null && fixtureTarget != null)
            {
                await using var restoreConnection = new OracleConnection(xeConnString);
                await restoreConnection.OpenAsync();
                await using var restore = new OracleCommand("UPDATE RL_MR_RIESGOS SET RIE_DESCRIPCION=:before WHERE RIE_ID=:id AND RIE_DESCRIPCION=:target", restoreConnection);
                restore.Parameters.Add(new OracleParameter("before", fixtureBefore));
                restore.Parameters.Add(new OracleParameter("id", fixtureRiskId));
                restore.Parameters.Add(new OracleParameter("target", fixtureTarget));
                if (await restore.ExecuteNonQueryAsync() != 1) throw new InvalidOperationException("IDEMPOTENCY_FIXTURE_RESTORE_FAILED");
                await using var restoreCommit = new OracleCommand("COMMIT", restoreConnection);
                await restoreCommit.ExecuteNonQueryAsync();
            }
        }

        // FASE 3: concurrencia optimista real usando dos sesiones XE.
        try
        {
            string riskCode = plan.AllPositions.Select(p => p.RiskCode).FirstOrDefault() ?? throw new InvalidOperationException("CONCURRENCY_FIXTURE_RISK_MISSING");
            await using var connA = new OracleConnection(xeConnString);
            await using var connB = new OracleConnection(xeConnString);
            await connA.OpenAsync();
            await connB.OpenAsync();
            await using var txA = connA.BeginTransaction();
            long riskId;
            string before;
            await using (var readBefore = new OracleCommand("SELECT RIE_ID, RIE_DESCRIPCION FROM RL_MR_RIESGOS WHERE RIE_CODIGO=:code", connA) { Transaction = txA })
            {
                readBefore.Parameters.Add(new OracleParameter("code", riskCode));
                await using var reader = await readBefore.ExecuteReaderAsync();
                if (!await reader.ReadAsync()) throw new InvalidOperationException("CONCURRENCY_FIXTURE_ROW_MISSING");
                riskId = SafeGetInt64(reader, 0);
                before = SafeGetString(reader, 1);
            }
            string concurrentValue = $"BLOCK4_DRIFT_{Guid.NewGuid():N}";
            bool committedConcurrentValue = false;
            try
            {
                await using (var updateB = new OracleCommand("UPDATE RL_MR_RIESGOS SET RIE_DESCRIPCION=:value WHERE RIE_ID=:id AND RIE_DESCRIPCION=:before", connB))
                {
                    updateB.Parameters.Add(new OracleParameter("value", concurrentValue));
                    updateB.Parameters.Add(new OracleParameter("id", riskId));
                    updateB.Parameters.Add(new OracleParameter("before", before));
                    if (await updateB.ExecuteNonQueryAsync() != 1) throw new InvalidOperationException("CONCURRENCY_WRITER_PRECONDITION_FAILED");
                }
                await using (var commitB = new OracleCommand("COMMIT", connB)) await commitB.ExecuteNonQueryAsync();
                committedConcurrentValue = true;
                int rowsA;
                await using (var guardedA = new OracleCommand("UPDATE RL_MR_RIESGOS SET RIE_DESCRIPCION=:target WHERE RIE_ID=:id AND RIE_DESCRIPCION=:expected", connA) { Transaction = txA })
                {
                    guardedA.Parameters.Add(new OracleParameter("target", "guarded"));
                    guardedA.Parameters.Add(new OracleParameter("id", riskId));
                    guardedA.Parameters.Add(new OracleParameter("expected", before));
                    rowsA = await guardedA.ExecuteNonQueryAsync();
                }
                result.ConcurrentDriftAbortPassed = rowsA == 0;
            }
            finally
            {
                await txA.RollbackAsync();
                if (committedConcurrentValue)
                {
                    await using var restore = new OracleCommand("UPDATE RL_MR_RIESGOS SET RIE_DESCRIPCION=:before WHERE RIE_ID=:id AND RIE_DESCRIPCION=:value", connB);
                    restore.Parameters.Add(new OracleParameter("before", before));
                    restore.Parameters.Add(new OracleParameter("id", riskId));
                    restore.Parameters.Add(new OracleParameter("value", concurrentValue));
                    if (await restore.ExecuteNonQueryAsync() != 1) throw new InvalidOperationException("CONCURRENCY_FIXTURE_RESTORE_FAILED");
                    await using var commitRestore = new OracleCommand("COMMIT", connB);
                    await commitRestore.ExecuteNonQueryAsync();
                }
            }
        }
        catch (Exception ex)
        {
            result.ConcurrentDriftAbortPassed = false;
            result.ErrorMessage = $"Two-session concurrency verification failed: {ex.Message}";
        }

        // FASE 4: execute the sealed rollback SQL in XE and compare the full table content hash.
        try
        {
            await using var connRb = new OracleConnection(xeConnString);
            await connRb.OpenAsync();
            string riskCode = plan.AllPositions.Select(p => p.RiskCode).FirstOrDefault() ?? throw new InvalidOperationException("ROLLBACK_FIXTURE_RISK_MISSING");
            var beforeSnapshot = await CaptureProductionPreimageAsync(xeConnString, "XE_ROLLBACK_BEFORE");
            var sourceRisk = beforeSnapshot.Risks.Single(r => r.Code == riskCode);
            var fixtureSnapshot = new DatabaseSnapshot { RunId = "XE_ROLLBACK_FIXTURE", CapturedAtUtc = DateTime.UtcNow };
            fixtureSnapshot.Risks.Add(sourceRisk);
            fixtureSnapshot.Evaluations.Add(beforeSnapshot.Evaluations.Single(e => e.RiskCode == riskCode && e.Active == 1));
            fixtureSnapshot.Projections.Add(beforeSnapshot.Projections.Single(p => p.RiskCode == riskCode && p.EvaluationId == fixtureSnapshot.Evaluations[0].Id));
            string target = $"BLOCK4_ROLLBACK_{Guid.NewGuid():N}";
            var cells = Enumerable.Range(1, 82).Select(field => (object)new
            {
                fieldNumber = field,
                textValue = field switch { 1 => "1", 2 => riskCode, 9 => target, _ => "" },
                formula = "",
                cachedResult = (object?)null
            }).ToList();
            using var rowDoc = JsonDocument.Parse(JsonSerializer.Serialize(new { riskNo = 1, riskCode, cells }));
            var rollbackFixture = BuildReconciliationPlan(fixtureSnapshot, new List<JsonElement> { rowDoc.RootElement }, manifestFields, catalogDoc, workbookSha, preimageManifestSha);
            var rollbackMutation = rollbackFixture.Mutations.Single();
            string beforeHash = beforeSnapshot.TableContentFingerprints["RL_MR_RIESGOS"].Sha256;
            int rollbackRows;
            await using (var txRb = connRb.BeginTransaction())
            {
                if (await ApplyMutationAsync(connRb, txRb, rollbackMutation) != 1 || !await VerifyMutationAsync(connRb, txRb, rollbackMutation))
                    throw new InvalidOperationException("ROLLBACK_FIXTURE_APPLY_FAILED");
                rollbackRows = await ApplyRollbackOperationAsync(connRb, txRb, rollbackMutation);
                if (!await VerifyRollbackOperationAsync(connRb, txRb, rollbackMutation))
                    throw new InvalidOperationException("ROLLBACK_FIXTURE_POSTCHECK_FAILED");
                await txRb.CommitAsync();
            }
            var afterFingerprints = await CaptureProductionPreimageAsync(xeConnString, "XE_ROLLBACK_AFTER");
            string afterHash = afterFingerprints.TableContentFingerprints["RL_MR_RIESGOS"].Sha256;
            result.RollbackPreimageMatch = beforeHash == afterHash;
            result.RollbackTestPassed = result.RollbackPreimageMatch && rollbackRows == 1;
            result.RollbackRowsAffected = rollbackRows;
        }
        catch (Exception ex)
        {
            result.RollbackTestPassed = false;
            result.ErrorMessage = $"Rollback content verification failed: {ex.Message}";
        }

        try
        {
            var insertFixture = await RunXeInsertPreconditionAndRollbackFixtureAsync(xeConnString, productionSnapshot);
            result.InsertPreconditionTestPassed = insertFixture.Passed;
            result.InsertFixtureRowsAffected = insertFixture.RowsAffected;
            result.RollbackRowsAffected += insertFixture.RollbackRowsAffected;
        }
        catch (Exception ex)
        {
            result.InsertPreconditionTestPassed = false;
            result.ErrorMessage = $"XE insert precondition fixture failed: {ex.Message}";
        }
        return result;
    }

    private static async Task<(bool Passed, int RowsAffected, int RollbackRowsAffected)> RunXeInsertPreconditionAndRollbackFixtureAsync(
        string xeConnectionString, DatabaseSnapshot sourceSnapshot)
    {
        var evaluation = sourceSnapshot.Evaluations.FirstOrDefault(e => e.Active == 1)
            ?? throw new InvalidOperationException("XE_INSERT_FIXTURE_ACTIVE_EVALUATION_MISSING");
        var sourceControl = sourceSnapshot.Controls.FirstOrDefault(c => c.EvaluationId == evaluation.Id)
            ?? throw new InvalidOperationException("XE_INSERT_FIXTURE_VALID_CONTROL_MISSING");
        await using var connection = new OracleConnection(xeConnectionString);
        await connection.OpenAsync();
        var beforeFingerprints = await ComputeOracleTableContentFingerprintsAsync(connection);
        long newId;
        await using (var sequenceCommand = new OracleCommand("SELECT SEQ_RL_MR_CONTROLES.NEXTVAL FROM dual", connection))
            newId = Convert.ToInt64(await sequenceCommand.ExecuteScalarAsync(), CultureInfo.InvariantCulture);

        string fixtureDescription = $"BLOCK4_INSERT_PRECONDITION_{Guid.NewGuid():N}";
        var parameters = new Dictionary<string, object?>
        {
            ["id"] = newId,
            ["eId"] = evaluation.Id,
            ["tipo"] = sourceControl.Tipo,
            ["p_desc"] = fixtureDescription,
            ["auto"] = sourceControl.Automatizacion,
            ["estado"] = sourceControl.Estado
        };
        var fixtureMutation = new PlannedMutation
        {
            MutationId = $"XE_INSERT_FIXTURE_{newId}", RiskCode = sourceControl.RiskCode,
            FieldNumber = 20, FieldLabel = "XE-only insert fixture", Entity = "RL_MR_CONTROLES_RIESGO",
            TargetTable = "RL_MR_CONTROLES_RIESGO", RecordKey = newId.ToString(CultureInfo.InvariantCulture),
            Action = "INSERT_BASELINE", TargetColumnOrProperty = "CON_DESCRIPCION", TargetCanonical = fixtureDescription,
            Precondition = "CONTROL_NOT_EXISTS",
            PreconditionSql = "SELECT COUNT(*) FROM RL_MR_CONTROLES_RIESGO WHERE CON_EVALUACION_ID = :eId AND CON_TIPO = :tipo AND CON_DESCRIPCION = :p_desc",
            ApplySql = "INSERT INTO RL_MR_CONTROLES_RIESGO (CON_ID, CON_EVALUACION_ID, CON_TIPO, CON_DESCRIPCION, CON_AUTOMATIZACION, CON_ESTADO) VALUES (:id, :eId, :tipo, :p_desc, :auto, :estado)",
            ApplyParameters = parameters,
            Rollback = new RollbackOperation
            {
                MutationId = $"XE_INSERT_FIXTURE_{newId}", Entity = "RL_MR_CONTROLES_RIESGO", Table = "RL_MR_CONTROLES_RIESGO",
                RecordPk = newId.ToString(CultureInfo.InvariantCulture), Action = "INSERT", RollbackAction = "DELETE",
                RollbackSql = "DELETE FROM RL_MR_CONTROLES_RIESGO WHERE CON_ID = :id AND CON_EVALUACION_ID = :eId AND CON_TIPO = :tipo AND CON_DESCRIPCION = :p_desc",
                RollbackParameters = new Dictionary<string, object?>(parameters, StringComparer.Ordinal)
            }
        };

        int insertedRows;
        bool duplicateRejected = false;
        int rollbackRows;
        await using (var transaction = connection.BeginTransaction())
        {
            insertedRows = await ApplyMutationAsync(connection, transaction, fixtureMutation);
            if (!await VerifyMutationAsync(connection, transaction, fixtureMutation))
                throw new InvalidOperationException("XE_INSERT_FIXTURE_POSTCHECK_FAILED");
            try
            {
                await ApplyMutationAsync(connection, transaction, fixtureMutation);
            }
            catch (InvalidOperationException exception) when (exception.Message.StartsWith("PRECONDITION_FAILED:", StringComparison.Ordinal))
            {
                duplicateRejected = true;
            }
            rollbackRows = await ApplyRollbackOperationAsync(connection, transaction, fixtureMutation);
            if (!await VerifyRollbackOperationAsync(connection, transaction, fixtureMutation))
                throw new InvalidOperationException("XE_INSERT_FIXTURE_ROLLBACK_POSTCHECK_FAILED");
            await transaction.CommitAsync();
        }

        var afterFingerprints = await ComputeOracleTableContentFingerprintsAsync(connection);
        bool contentRestored = beforeFingerprints["RL_MR_CONTROLES_RIESGO"].Sha256 == afterFingerprints["RL_MR_CONTROLES_RIESGO"].Sha256 &&
            beforeFingerprints["RL_MR_CONTROLES_RIESGO"].RowCount == afterFingerprints["RL_MR_CONTROLES_RIESGO"].RowCount;
        return (insertedRows == 1 && duplicateRejected && rollbackRows == 1 && contentRestored, insertedRows, rollbackRows);
    }

    private static string ComputeEntityFingerprint<T>(IEnumerable<T> values)
    {
        var json = JsonSerializer.Serialize(values);
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(json)));
    }

    #endregion

    #region Modo Apply Productivo Protegido (Sección 18)

    public static async Task ValidatePlanPreconditionsReadOnlyAsync(string connectionString, PlanResult plan)
    {
        await using var connection = new OracleConnection(connectionString);
        await connection.OpenAsync();
        await using (var readOnly = new OracleCommand("SET TRANSACTION READ ONLY", connection))
            await readOnly.ExecuteNonQueryAsync();

        try
        {
            await using (var identity = new OracleCommand(@"
                SELECT sys_context('USERENV', 'DB_NAME'), sys_context('USERENV', 'SERVICE_NAME'),
                       sys_context('USERENV', 'INSTANCE_NAME'), sys_context('USERENV', 'CURRENT_SCHEMA'),
                       sys_context('USERENV', 'SESSION_USER') FROM dual", connection))
            await using (var reader = await identity.ExecuteReaderAsync())
            {
                if (!await reader.ReadAsync() ||
                    !SafeGetString(reader, 0).Equals("HPPROD1", StringComparison.OrdinalIgnoreCase) ||
                    !SafeGetString(reader, 1).Equals("hpprod1", StringComparison.OrdinalIgnoreCase) ||
                    !SafeGetString(reader, 2).Equals("hpprod1", StringComparison.OrdinalIgnoreCase) ||
                    !SafeGetString(reader, 3).Equals("RIESGO_LAVADO", StringComparison.OrdinalIgnoreCase) ||
                    !SafeGetString(reader, 4).Equals("RIESGO_LAVADO", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("PRODUCTION_IDENTITY_MISMATCH: read-only preflight.");
            }

            foreach (var mutation in plan.Mutations)
            {
                if (string.IsNullOrWhiteSpace(mutation.PreconditionSql))
                    throw new InvalidOperationException($"PRECONDITION_SQL_REQUIRED: {mutation.MutationId}");
                await using var command = new OracleCommand(mutation.PreconditionSql, connection) { BindByName = true };
                AddReferencedParameters(command, mutation.PreconditionSql, mutation.ApplyParameters);
                object? actual = await command.ExecuteScalarAsync();
                if (!IsExpectedPrecondition(mutation, actual))
                    throw new InvalidOperationException($"PRECONDITION_FAILED: {mutation.MutationId}");
            }
        }
        finally
        {
            await using var rollback = new OracleCommand("ROLLBACK", connection);
            await rollback.ExecuteNonQueryAsync();
        }
    }

    public static async Task<int> ApplyMutationAsync(OracleConnection connection, OracleTransaction transaction, PlannedMutation mutation)
    {
        if (string.IsNullOrWhiteSpace(mutation.PreconditionSql))
            throw new InvalidOperationException($"PRECONDITION_SQL_REQUIRED: {mutation.MutationId}");

        await using (var precondition = new OracleCommand(mutation.PreconditionSql, connection) { Transaction = transaction, BindByName = true })
        {
            AddReferencedParameters(precondition, mutation.PreconditionSql, mutation.ApplyParameters);
            object? actual = await precondition.ExecuteScalarAsync();
            if (!IsExpectedPrecondition(mutation, actual))
                throw new InvalidOperationException($"PRECONDITION_FAILED: {mutation.MutationId}");
        }

        await using var command = new OracleCommand(mutation.ApplySql, connection) { Transaction = transaction, BindByName = true };
        AddReferencedParameters(command, mutation.ApplySql, mutation.ApplyParameters);
        int rowsAffected = await command.ExecuteNonQueryAsync();
        if (rowsAffected == 0) throw new InvalidOperationException($"PRECONDITION_FAILED: {mutation.MutationId}; rowsAffected=0.");
        if (rowsAffected != 1) throw new InvalidOperationException($"CORRUPTION_GUARD_FAILED: {mutation.MutationId}; rowsAffected={rowsAffected}.");
        return rowsAffected;
    }

    public static async Task<bool> VerifyMutationAsync(OracleConnection connection, OracleTransaction transaction, PlannedMutation mutation)
    {
        string sql;
        Dictionary<string, object?> parameters;
        if (mutation.TargetTable == "RL_MR_RIESGOS" && mutation.TargetColumnOrProperty == "RIE_DESCRIPCION")
        {
            sql = "SELECT RIE_DESCRIPCION FROM RL_MR_RIESGOS WHERE RIE_ID = :id";
            parameters = mutation.ApplyParameters;
        }
        else if (mutation.TargetTable == "RL_MR_EVALUACIONES_RIESGO" && mutation.TargetColumnOrProperty == "EVA_DATOS_JSON")
        {
            sql = "SELECT EVA_DATOS_JSON FROM RL_MR_EVALUACIONES_RIESGO WHERE EVA_ID = :id";
            parameters = mutation.ApplyParameters;
        }
        else if (mutation.TargetTable == "RL_MR_CONTROLES_RIESGO" && mutation.Action == "INSERT_BASELINE")
        {
            sql = "SELECT COUNT(*) FROM RL_MR_CONTROLES_RIESGO WHERE CON_EVALUACION_ID = :eId AND CON_TIPO = :tipo AND CON_DESCRIPCION = :p_desc";
            parameters = mutation.ApplyParameters;
        }
        else if (mutation.TargetTable == "RL_MR_PLANES" && mutation.Action == "INSERT_BASELINE")
        {
            sql = "SELECT COUNT(*) FROM RL_MR_PLANES WHERE PLA_ID = :id AND PLA_EVALUACION_ID = :eId AND PLA_DESCRIPCION = :p_desc";
            parameters = mutation.ApplyParameters;
        }
        else if (mutation.TargetTable == "RL_MR_ACTIVIDADES" && mutation.Action == "INSERT_BASELINE")
        {
            sql = "SELECT COUNT(*) FROM RL_MR_ACTIVIDADES WHERE ACT_ID = :id AND ACT_PLAN_ID = :planId AND ACT_DESCRIPCION = :p_desc";
            parameters = mutation.ApplyParameters;
        }
        else if (mutation.TargetTable == "RL_MR_CONTROLES_RIESGO" && mutation.TargetColumnOrProperty == "CON_AUTOMATIZACION")
        {
            sql = "SELECT CON_AUTOMATIZACION FROM RL_MR_CONTROLES_RIESGO WHERE CON_ID = :id";
            parameters = mutation.ApplyParameters;
        }
        else
        {
            throw new InvalidOperationException($"POSTCHECK_UNSUPPORTED_MUTATION: {mutation.TargetTable}.{mutation.TargetColumnOrProperty}/{mutation.Action}");
        }

        await using var command = new OracleCommand(sql, connection) { Transaction = transaction, BindByName = true };
        AddReferencedParameters(command, sql, parameters);
        object? actual = await command.ExecuteScalarAsync();
        if (mutation.Action == "INSERT_BASELINE" && mutation.TargetTable is "RL_MR_CONTROLES_RIESGO" or "RL_MR_PLANES" or "RL_MR_ACTIVIDADES")
            return Convert.ToInt32(actual, CultureInfo.InvariantCulture) == 1;
        string actualText = actual == null || actual == DBNull.Value ? "" : Convert.ToString(actual, CultureInfo.InvariantCulture) ?? "";
        string expectedText = mutation.TargetTable == "RL_MR_CONTROLES_RIESGO" && mutation.TargetColumnOrProperty == "CON_AUTOMATIZACION"
            ? Convert.ToString(mutation.ApplyParameters.GetValueOrDefault("newAuto"), CultureInfo.InvariantCulture) ?? ""
            : mutation.TargetCanonical;
        return string.Equals(actualText, expectedText, StringComparison.Ordinal);
    }

    public static async Task<int> ApplyRollbackOperationAsync(OracleConnection connection, OracleTransaction transaction, PlannedMutation mutation)
    {
        var rollback = mutation.Rollback;
        bool supportedUpdate = mutation.Action == "UPDATE_BASELINE" && rollback.RollbackAction == "UPDATE" &&
            ((mutation.TargetTable == "RL_MR_RIESGOS" && mutation.TargetColumnOrProperty == "RIE_DESCRIPCION") ||
             (mutation.TargetTable == "RL_MR_EVALUACIONES_RIESGO" && mutation.TargetColumnOrProperty == "EVA_DATOS_JSON") ||
             (mutation.TargetTable == "RL_MR_CONTROLES_RIESGO" && mutation.TargetColumnOrProperty == "CON_AUTOMATIZACION"));
        bool supportedInsert = mutation.Action == "INSERT_BASELINE" && rollback.RollbackAction == "DELETE" &&
            (mutation.TargetTable is "RL_MR_CONTROLES_RIESGO" or "RL_MR_PLANES" or "RL_MR_ACTIVIDADES");
        string expectedRollbackPrefix = supportedInsert ? $"DELETE FROM {mutation.TargetTable} " : $"UPDATE {mutation.TargetTable} SET ";
        if ((!supportedUpdate && !supportedInsert) || !string.Equals(rollback.Table, mutation.TargetTable, StringComparison.Ordinal) ||
            string.IsNullOrWhiteSpace(rollback.RollbackSql) || !rollback.RollbackSql.StartsWith(expectedRollbackPrefix, StringComparison.OrdinalIgnoreCase) ||
            rollback.RollbackParameters.Count == 0)
            throw new InvalidOperationException($"ROLLBACK_UNSUPPORTED_MUTATION: {mutation.TargetTable}.{mutation.TargetColumnOrProperty}/{mutation.Action}");

        await using var command = new OracleCommand(rollback.RollbackSql, connection) { Transaction = transaction, BindByName = true };
        AddReferencedParameters(command, rollback.RollbackSql, rollback.RollbackParameters);
        int affected = await command.ExecuteNonQueryAsync();
        if (affected == 0) throw new InvalidOperationException($"ROLLBACK_PRECONDITION_FAILED: {mutation.MutationId}; rowsAffected=0.");
        if (affected != 1) throw new InvalidOperationException($"ROLLBACK_CORRUPTION_GUARD_FAILED: {mutation.MutationId}; rowsAffected={affected}.");
        return affected;
    }

    public static async Task<bool> VerifyRollbackOperationAsync(OracleConnection connection, OracleTransaction transaction, PlannedMutation mutation)
    {
        if (mutation.Action == "INSERT_BASELINE" && (mutation.TargetTable is "RL_MR_CONTROLES_RIESGO" or "RL_MR_PLANES" or "RL_MR_ACTIVIDADES"))
        {
            string countSql = mutation.TargetTable switch
            {
                "RL_MR_CONTROLES_RIESGO" => "SELECT COUNT(*) FROM RL_MR_CONTROLES_RIESGO WHERE CON_ID = :id",
                "RL_MR_PLANES" => "SELECT COUNT(*) FROM RL_MR_PLANES WHERE PLA_ID = :id",
                "RL_MR_ACTIVIDADES" => "SELECT COUNT(*) FROM RL_MR_ACTIVIDADES WHERE ACT_ID = :id",
                _ => throw new InvalidOperationException($"ROLLBACK_POSTCHECK_UNSUPPORTED_MUTATION: {mutation.TargetTable}")
            };
            await using var countCommand = new OracleCommand(countSql, connection) { Transaction = transaction, BindByName = true };
            AddReferencedParameters(countCommand, countSql, mutation.Rollback.RollbackParameters);
            return Convert.ToInt32(await countCommand.ExecuteScalarAsync(), CultureInfo.InvariantCulture) == 0;
        }

        if (mutation.TargetTable == "RL_MR_EVALUACIONES_RIESGO" && mutation.TargetColumnOrProperty == "EVA_DATOS_JSON")
        {
            const string evaluationSql = "SELECT EVA_DATOS_JSON, EVA_VERSION_ROW FROM RL_MR_EVALUACIONES_RIESGO WHERE EVA_ID = :id";
            await using var evaluationCommand = new OracleCommand(evaluationSql, connection) { Transaction = transaction, BindByName = true };
            AddReferencedParameters(evaluationCommand, evaluationSql, mutation.ApplyParameters);
            await using var evaluationReader = await evaluationCommand.ExecuteReaderAsync();
            if (!await evaluationReader.ReadAsync()) return false;
            string restoredJson = SafeGetString(evaluationReader, 0);
            int restoredVersion = SafeGetInt32(evaluationReader, 1);
            return string.Equals(restoredJson, mutation.Rollback.BeforeValue, StringComparison.Ordinal) &&
                restoredVersion == Convert.ToInt32(mutation.Rollback.RollbackParameters["beforeRow"], CultureInfo.InvariantCulture);
        }

        string sql = mutation.TargetTable switch
        {
            "RL_MR_RIESGOS" when mutation.TargetColumnOrProperty == "RIE_DESCRIPCION" => "SELECT RIE_DESCRIPCION FROM RL_MR_RIESGOS WHERE RIE_ID = :id",
            "RL_MR_CONTROLES_RIESGO" when mutation.TargetColumnOrProperty == "CON_AUTOMATIZACION" => "SELECT CON_AUTOMATIZACION FROM RL_MR_CONTROLES_RIESGO WHERE CON_ID = :id",
            _ => throw new InvalidOperationException($"ROLLBACK_POSTCHECK_UNSUPPORTED_MUTATION: {mutation.TargetTable}.{mutation.TargetColumnOrProperty}/{mutation.Action}")
        };
        await using var command = new OracleCommand(sql, connection) { Transaction = transaction, BindByName = true };
        AddReferencedParameters(command, sql, mutation.ApplyParameters);
        object? actual = await command.ExecuteScalarAsync();
        return string.Equals(actual == null || actual == DBNull.Value ? "" : Convert.ToString(actual, CultureInfo.InvariantCulture) ?? "",
            mutation.DbBeforeCanonical, StringComparison.Ordinal);
    }

    private static void AddReferencedParameters(OracleCommand command, string sql, Dictionary<string, object?> values)
    {
        var added = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match match in Regex.Matches(sql, ":([A-Za-z_][A-Za-z0-9_]*)"))
        {
            string name = match.Groups[1].Value;
            if (!added.Add(name)) continue;
            if (!values.TryGetValue(name, out object? value))
                throw new InvalidOperationException($"MUTATION_PARAMETER_MISSING: {name}");
            command.Parameters.Add(new OracleParameter(name, value ?? DBNull.Value));
        }
    }

    private static bool IsExpectedPrecondition(PlannedMutation mutation, object? actual)
    {
        if (mutation.Precondition == "CONTROL_NOT_EXISTS")
            return Convert.ToInt32(actual, CultureInfo.InvariantCulture) == 0;
        string expectedKey = mutation.TargetColumnOrProperty == "EVA_DATOS_JSON" ? "expectedRow" : "expectedBefore";
        if (mutation.Action == "UPDATE_BASELINE" && mutation.ApplyParameters.TryGetValue(expectedKey, out var expected))
        {
            string actualText = actual == null || actual == DBNull.Value ? "" : Convert.ToString(actual, CultureInfo.InvariantCulture) ?? "";
            string expectedText = expected == null || expected == DBNull.Value ? "" : Convert.ToString(expected, CultureInfo.InvariantCulture) ?? "";
            return string.Equals(actualText, expectedText, StringComparison.Ordinal);
        }
        return false;
    }

    public static async Task<int> ExecuteProductionApplyAsync(string prodConnString, string planPath, string expectedPlanSha, string expectedFileSha, string expectedWorkbookSha)
    {
        Console.WriteLine("\n[1/5] Validando precondiciones de producción...");
        if (!string.Equals(Environment.GetEnvironmentVariable("PRODUCTION_APPLY_AUTHORIZED"), "YES", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("PRODUCTION_APPLY_REFUSED: no existe autorización explícita.");
        if (!File.Exists(planPath)) throw new FileNotFoundException("PLAN_FILE_NOT_FOUND", planPath);
        string actualFileSha = ComputeFileSha256(planPath);
        if (!actualFileSha.Equals(expectedFileSha, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("PLAN_FILE_SHA_MISMATCH: --expected-file-sha no coincide con los bytes del artefacto.");
        string planJson = File.ReadAllText(planPath);
        var plan = JsonSerializer.Deserialize<PlanResult>(planJson);
        if (plan == null)
        {
            Console.Error.WriteLine("FATAL: No se pudo deserializar el plan.");
            return 1;
        }

        if (!plan.PlanCanonicalSha256.Equals(expectedPlanSha, StringComparison.OrdinalIgnoreCase) ||
            !ComputeComprehensivePlanSha256(plan, plan.WorkbookSha256, plan.PreimageManifestSha256).Equals(expectedPlanSha, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("PLAN_CANONICAL_SHA_MISMATCH: --expected-plan-sha no coincide con el contenido canónico.");
        if (!plan.WorkbookSha256.Equals(expectedWorkbookSha, StringComparison.OrdinalIgnoreCase) ||
            !plan.EngineVersion.Equals(EngineVersion, StringComparison.Ordinal) ||
            !plan.GitSha.Equals(ResolveGitSha(), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("PLAN_COMPATIBILITY_MISMATCH: workbook, engine o Git SHA no corresponde al runtime.");

        await ValidatePlanPreconditionsReadOnlyAsync(prodConnString, plan);

        await using var conn = new OracleConnection(prodConnString);
        await conn.OpenAsync();

        await using (var cmdIdent = new OracleCommand(@"
            SELECT sys_context('USERENV', 'DB_NAME'),
                   sys_context('USERENV', 'SERVICE_NAME'),
                   sys_context('USERENV', 'INSTANCE_NAME'),
                   sys_context('USERENV', 'CURRENT_SCHEMA'),
                   sys_context('USERENV', 'SESSION_USER')
              FROM dual", conn))
        await using (var r = await cmdIdent.ExecuteReaderAsync())
        {
            if (await r.ReadAsync())
            {
                string dbName = SafeGetString(r, 0);
                string srvName = SafeGetString(r, 1);
                string instance = SafeGetString(r, 2);
                string schema = SafeGetString(r, 3);
                string user = SafeGetString(r, 4);

                if (!dbName.Equals("HPPROD1", StringComparison.OrdinalIgnoreCase) ||
                    !srvName.Equals("hpprod1", StringComparison.OrdinalIgnoreCase) ||
                    !instance.Equals("hpprod1", StringComparison.OrdinalIgnoreCase) ||
                    !schema.Equals("RIESGO_LAVADO", StringComparison.OrdinalIgnoreCase) ||
                    !user.Equals("RIESGO_LAVADO", StringComparison.OrdinalIgnoreCase))
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
                applied += await ApplyMutationAsync(conn, tx, mut);
            }

            Console.WriteLine($"\n[3/5] Ejecutando postcheck de {applied} operaciones...");
            int mismatches = 0;
            int verified = 0;
            foreach (var mut in plan.Mutations)
            {
                if (await VerifyMutationAsync(conn, tx, mut)) verified++; else mismatches++;
            }

            if (mismatches > 0 || verified != plan.Mutations.Count)
            {
                throw new InvalidOperationException($"POSTCHECK_FAILED: {mismatches} discrepancias; verified={verified}/{plan.Mutations.Count}.");
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

    public static bool IsGticApplicable(string canonicalRiskType, string area) =>
        string.Equals(canonicalRiskType?.Trim(), "TEC", StringComparison.Ordinal);

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

    public static SourceItemsParseResult ParseSourceItems(string text, bool allowHierarchicalNumbering = false)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new SourceItemsParseResult { ParseRule = "BLANK_SOURCE" };

        string[] lines = text.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Split('\n');
        if (allowHierarchicalNumbering)
        {
            Regex hierarchicalItem = new("^\\s*(?<parent>\\d+)\\.(?<child>\\d+)\\s*(?:[.)-]\\s*)?(?<text>.*)$", RegexOptions.CultureInvariant);
            var hierarchicalMarkers = new List<(int LineIndex, int Parent, int Child, string Text)>();
            for (int index = 0; index < lines.Length; index++)
            {
                Match match = hierarchicalItem.Match(lines[index]);
                if (match.Success
                    && int.TryParse(match.Groups["parent"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out int parent)
                    && int.TryParse(match.Groups["child"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out int child))
                    hierarchicalMarkers.Add((index, parent, child, match.Groups["text"].Value.Trim()));
            }

            if (hierarchicalMarkers.Count > 0)
            {
                if (hierarchicalMarkers[0].Parent != 1
                    || lines.Take(hierarchicalMarkers[0].LineIndex).Any(line => !string.IsNullOrWhiteSpace(line)))
                    return new SourceItemsParseResult { ParseRule = "AMBIGUOUS_TEXT_BEFORE_HIERARCHICAL_ITEMS", IsAmbiguous = true };

                int priorParent = 0;
                int priorChild = 0;
                foreach (var marker in hierarchicalMarkers)
                {
                    if (marker.Parent < priorParent || marker.Parent > priorParent + 1
                        || (marker.Parent == priorParent && marker.Child != priorChild + 1)
                        || (marker.Parent > priorParent && marker.Child != 1))
                        return new SourceItemsParseResult { ParseRule = "AMBIGUOUS_HIERARCHICAL_NUMBERING", IsAmbiguous = true };
                    priorChild = marker.Child;
                    priorParent = marker.Parent;
                }

                var hierarchicalItems = new List<string>(hierarchicalMarkers.Count);
                var parentOrdinals = new List<int?>(hierarchicalMarkers.Count);
                for (int markerIndex = 0; markerIndex < hierarchicalMarkers.Count; markerIndex++)
                {
                    var marker = hierarchicalMarkers[markerIndex];
                    int end = markerIndex + 1 < hierarchicalMarkers.Count ? hierarchicalMarkers[markerIndex + 1].LineIndex : lines.Length;
                    var itemLines = new List<string>();
                    if (marker.Text.Length > 0) itemLines.Add(marker.Text);
                    for (int lineIndex = marker.LineIndex + 1; lineIndex < end; lineIndex++)
                    {
                        string continuation = lines[lineIndex].Trim();
                        if (continuation.Length > 0) itemLines.Add(continuation);
                    }
                    string item = string.Join("\n", itemLines).Trim();
                    if (item.Length == 0)
                        return new SourceItemsParseResult { ParseRule = "AMBIGUOUS_EMPTY_HIERARCHICAL_ITEM", IsAmbiguous = true };
                    hierarchicalItems.Add(item);
                    parentOrdinals.Add(marker.Parent);
                }
                return new SourceItemsParseResult
                {
                    Items = hierarchicalItems,
                    ParentOrdinals = parentOrdinals,
                    ParseRule = "HIERARCHICAL_NUMBERED_ITEMS_WITH_EXACT_PARENT_ORDINAL"
                };
            }
        }
        Regex numberedItem = new("^\\s*(?<ordinal>\\d+)\\s*(?:\\.\\s*(?:-\\s*)?|\\)\\s*|-\\s+)(?<text>.*)$", RegexOptions.CultureInvariant);
        var markers = new List<(int LineIndex, int Ordinal, string Text)>();
        for (int index = 0; index < lines.Length; index++)
        {
            Match match = numberedItem.Match(lines[index]);
            if (match.Success && int.TryParse(match.Groups["ordinal"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out int ordinal))
                markers.Add((index, ordinal, match.Groups["text"].Value.Trim()));
        }

        if (markers.Count == 0)
        {
            string[] nonBlankLines = lines.Select(line => line.Trim()).Where(line => line.Length > 0).ToArray();
            return nonBlankLines.Length switch
            {
                0 => new SourceItemsParseResult { ParseRule = "BLANK_SOURCE" },
                1 => new SourceItemsParseResult { Items = nonBlankLines, ParseRule = "SINGLE_UNNUMBERED_ITEM" },
                _ => new SourceItemsParseResult { Items = new[] { string.Join("\n", nonBlankLines) }, ParseRule = "UNNUMBERED_MULTILINE_AS_SINGLE_PARAGRAPH" }
            };
        }

        if (lines.Take(markers[0].LineIndex).Any(line => !string.IsNullOrWhiteSpace(line)))
            return new SourceItemsParseResult { ParseRule = "AMBIGUOUS_TEXT_BEFORE_NUMBERED_ITEMS", IsAmbiguous = true };

        if (!markers.Select(marker => marker.Ordinal).SequenceEqual(Enumerable.Range(1, markers.Count)))
            return new SourceItemsParseResult { ParseRule = "AMBIGUOUS_NON_SEQUENTIAL_NUMBERING", IsAmbiguous = true };

        var items = new List<string>(markers.Count);
        for (int markerIndex = 0; markerIndex < markers.Count; markerIndex++)
        {
            var marker = markers[markerIndex];
            int end = markerIndex + 1 < markers.Count ? markers[markerIndex + 1].LineIndex : lines.Length;
            var itemLines = new List<string>();
            if (marker.Text.Length > 0) itemLines.Add(marker.Text);
            for (int lineIndex = marker.LineIndex + 1; lineIndex < end; lineIndex++)
            {
                string continuation = lines[lineIndex].Trim();
                if (continuation.Length > 0) itemLines.Add(continuation);
            }
            string item = string.Join("\n", itemLines).Trim();
            if (item.Length == 0 && markerIndex < markers.Count - 1)
                return new SourceItemsParseResult { ParseRule = "AMBIGUOUS_EMPTY_NONTERMINAL_NUMBERED_ITEM", IsAmbiguous = true };
            items.Add(item);
        }

        bool hasEmptyTerminalItem = items.Count > 0 && items[^1].Length == 0;
        return new SourceItemsParseResult
        {
            Items = items,
            ParentOrdinals = Enumerable.Repeat<int?>(null, items.Count).ToArray(),
            ParseRule = hasEmptyTerminalItem ? "NUMBERED_ITEMS_WITH_EMPTY_TERMINAL_ITEM" : "NUMBERED_ITEMS_WITH_WRAPPED_CONTINUATIONS"
        };
    }

    private static void AddParserAmbiguity(PlanResult plan, string riskCode, int fieldNumber, string fieldLabel, string entity, string rawValue)
    {
        plan.RejectAmbiguousCount++;
        plan.BlockedItems.Add(new BlockedItem
        {
            RiskCode = riskCode,
            FieldNumber = fieldNumber,
            FieldLabel = fieldLabel,
            Entity = entity,
            SourceValue = rawValue,
            RequiredMissingValue = "Unambiguous source item boundaries",
            ReasonCode = "PARSER_AMBIGUOUS",
            RecommendedResolution = "Resolve the workbook item boundaries from an authoritative source; do not split wrapped paragraphs or infer separate items."
        });
        plan.AllPositions.Add(new ReconciliationPosition
        {
            PositionKey = $"{riskCode}_F{fieldNumber:D2}",
            RiskCode = riskCode,
            FieldNumber = fieldNumber,
            FieldLabel = fieldLabel,
            Action = "REJECT_AMBIGUOUS",
            ReasonCode = "PARSER_AMBIGUOUS",
            ExcelValue = rawValue,
            DbValue = "",
            TargetValue = "",
            Mutates = false
        });
    }

    private static string NormalizeMultiline(string text) =>
        string.Join("\n", text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n')
            .Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(NormalizeControlText).Select(s => s.Trim()));

    private static string ProjectPlanValues(IEnumerable<string?> values)
    {
        var items = values.ToList();
        if (items.Count == 0) return "";
        if (items.Count == 1) return items[0] ?? "";
        return string.Join("\n", items.Select((value, index) => $"{index + 1}. {value ?? ""}"));
    }

    private static string ProjectEvidenceByControl(IEnumerable<ControlPreimage> controls, IEnumerable<EvidencePreimage> evidences)
    {
        var orderedControls = controls.OrderBy(c => c.Id).ToList();
        var evidenceRows = evidences.ToList();
        var blocks = new List<string>();
        for (int index = 0; index < orderedControls.Count; index++)
        {
            var control = orderedControls[index];
            var names = evidenceRows.Where(e => e.TipoEntidad == "CONTROL" && e.EntidadId == control.Id)
                .OrderBy(e => e.FechaCreacion).ThenBy(e => e.EvidenciaId)
                .Select(e => string.IsNullOrWhiteSpace(e.Extension) ? e.NombreArchivo : $"{e.NombreArchivo}.{e.Extension}")
                .ToList();
            if (names.Count > 0)
                blocks.Add($"Control {index + 1} (CON_ID={control.Id}):\n{string.Join("\n", names.Select((name, ordinal) => $"  {ordinal + 1}. {name}"))}");
        }
        return string.Join("\n", blocks);
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
        var dicts = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);

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
                    : new Dictionary<string, string>(StringComparer.Ordinal);

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
        string catalogId = catalogOrProperty switch
        {
            "tipo_riesgo" => "RISK_TYPE",
            "regimen_afectado" => "REGIME",
            "CON_AUTOMATIZACION" or "MR_NIVEL_AUTOMATIZACION" => "CONTROL_AUTOMATION",
            _ => catalogOrProperty
        };

        if (catalogMaps.TryGetValue(catalogId, out var directMap))
        {
            if (directMap.TryGetValue(val, out var canonDirect))
            {
                log[val] = canonDirect;
                return canonDirect;
            }
            return null; // Known catalog property but unrecognized value fails closed
        }

        // Si es una propiedad declarada como catálogo cerrado pero no se encontró en ningún mapeo, fail-closed
        bool isCatalogProperty = catalogOrProperty is "tipo_riesgo" or "regimen_afectado" or "RISK_TYPE" or "REGIME" or "CONTROL_AUTOMATION" or "MR_NIVEL_AUTOMATIZACION" or "CON_AUTOMATIZACION";
        if (isCatalogProperty)
        {
            return null;
        }

        return val;
    }

    private static string ResolveCatalogDbKey(JsonDocument catalogDoc, string catalogId, string canonicalKey)
    {
        if (!catalogDoc.RootElement.TryGetProperty("catalogs", out var catalogs) || catalogs.ValueKind != JsonValueKind.Array)
            throw new InvalidDataException("CANONICAL_CATALOGS_MISSING");
        foreach (var catalog in catalogs.EnumerateArray())
        {
            if (!catalog.TryGetProperty("catalogId", out var id) || !string.Equals(id.GetString(), catalogId, StringComparison.Ordinal)) continue;
            if (!catalog.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array) break;
            foreach (var item in items.EnumerateArray())
            {
                if (item.TryGetProperty("key", out var key) && string.Equals(key.GetString(), canonicalKey, StringComparison.Ordinal))
                    return item.TryGetProperty("dbKey", out var dbKey) && !string.IsNullOrEmpty(dbKey.GetString()) ? dbKey.GetString()! : canonicalKey;
            }
            break;
        }
        throw new InvalidDataException($"CANONICAL_CATALOG_ITEM_MISSING: {catalogId}/{canonicalKey}");
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
        sb.AppendLine("RISK_CODE,FIELD_NO,FIELD_LABEL,PREVIOUS_CLASSIFICATION,ORIGINAL_EXCEL_RAW,ORIGINAL_DB_RAW,ORIGINAL_REASON_CODE,ORIGINAL_RECOMMENDED_ACTION,CURRENT_DB_RAW,CURRENT_EXCEL_RAW,CURRENT_CLASSIFICATION,REASON,RECOMMENDED_ACTION,EVIDENCE,MUTATION_REQUIRED");
        foreach (var d in deltas)
        {
            sb.AppendLine($"\"{EscapeCsv(d.RiskCode)}\",{d.FieldNumber},\"{EscapeCsv(d.FieldLabel)}\",\"{d.PreviousClassification}\",\"{EscapeCsv(d.OriginalExcelRaw)}\",\"{EscapeCsv(d.OriginalDbRaw)}\",\"{EscapeCsv(d.OriginalReasonCode)}\",\"{EscapeCsv(d.OriginalRecommendedAction)}\",\"{EscapeCsv(d.CurrentDbRaw)}\",\"{EscapeCsv(d.CurrentExcelRaw)}\",\"{d.CurrentClassification}\",\"{EscapeCsv(d.Reason)}\",\"{EscapeCsv(d.RecommendedAction)}\",\"{EscapeCsv(d.Evidence)}\",{d.MutationRequired}");
        }
        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
    }

    private static void ExportDelta87Csv(string path, List<Delta87Record> deltas)
    {
        var sb = new StringBuilder();
        sb.AppendLine("RISK_CODE,FIELD_NO,FIELD_LABEL,PREVIOUS_CLASSIFICATION,ORIGINAL_EXCEL_RAW,ORIGINAL_DB_RAW,ORIGINAL_REASON_CODE,ORIGINAL_RECOMMENDED_ACTION,CURRENT_DB_RAW,CURRENT_EXCEL_RAW,CURRENT_CLASSIFICATION,REASON,RECOMMENDED_ACTION,EVIDENCE,MUTATION_REQUIRED");
        foreach (var d in deltas)
        {
            sb.AppendLine($"\"{EscapeCsv(d.RiskCode)}\",{d.FieldNumber},\"{EscapeCsv(d.FieldLabel)}\",\"{d.PreviousClassification}\",\"{EscapeCsv(d.OriginalExcelRaw)}\",\"{EscapeCsv(d.OriginalDbRaw)}\",\"{EscapeCsv(d.OriginalReasonCode)}\",\"{EscapeCsv(d.OriginalRecommendedAction)}\",\"{EscapeCsv(d.CurrentDbRaw)}\",\"{EscapeCsv(d.CurrentExcelRaw)}\",\"{d.CurrentClassification}\",\"{EscapeCsv(d.Reason)}\",\"{EscapeCsv(d.RecommendedAction)}\",\"{EscapeCsv(d.Evidence)}\",{d.MutationRequired}");
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
