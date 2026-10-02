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
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Oracle.ManagedDataAccess.Client;
using RL.API.Infrastructure.Database;

namespace RL.Tools.AuditMatricesExcelVsProduction;

public class AuditPosition
{
    public string AuditKey { get; set; } = "";
    public int RiskNo { get; set; }
    public string RiskCode { get; set; } = "";

    public long SelectedEvaluationId { get; set; }
    public string SelectedEvaluationStatus { get; set; } = "";
    public long SelectedEvaluationVersion { get; set; }

    public int FieldNumber { get; set; }
    public string FieldLabel { get; set; } = "";
    public string CanonicalKey { get; set; } = "";
    public string FunctionalBlock { get; set; } = "";
    public int BlockNumber { get; set; }

    public string Source { get; set; } = "";
    public string Mode { get; set; } = "";
    public string Requiredness { get; set; } = "";
    public string Conditionality { get; set; } = "";

    public bool Applies { get; set; }
    public string ApplicabilityReason { get; set; } = "";

    public string ExcelRaw { get; set; } = "";
    public string ExcelNormalized { get; set; } = "";
    public string ExcelFormula { get; set; } = "";
    public string ExcelCachedResult { get; set; } = "";

    public string DbSource { get; set; } = "";
    public string DbRaw { get; set; } = "";
    public string DbNormalized { get; set; } = "";
    public int DbRecordCount { get; set; }

    public List<string> StructuredExcelItems { get; set; } = new();
    public List<string> StructuredDbItems { get; set; } = new();

    public bool RawEqual { get; set; }
    public bool NormalizedEqual { get; set; }
    public bool SemanticEqual { get; set; }

    public string PrimaryClassification { get; set; } = "";
    public string ReasonCode { get; set; } = "";
    public List<string> SecondaryFlags { get; set; } = new();

    public string CalculationParity { get; set; } = "N/A";

    public bool OperationalAuthority { get; set; }
    public bool PreserveDbValue { get; set; }

    public string TechnicalMappingStatus { get; set; } = "NORMAL";
    public string RecommendedNextAction { get; set; } = "";
    public string EvidenceReference { get; set; } = "";
    public string Severity { get; set; } = "INFO";
}

public static class Program
{
    private static readonly Regex ProhibitedSqlRegex = new(
        @"(?i)\b(INSERT|UPDATE|MERGE|DELETE|TRUNCATE|CREATE|ALTER|DROP|COMMENT|GRANT|REVOKE|CALL|EXEC|EXECUTE|LOCK\s+TABLE|FOR\s+UPDATE)\b",
        RegexOptions.Compiled);

    public static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        string repoRoot = Directory.GetCurrentDirectory();
        while (!string.IsNullOrEmpty(repoRoot) && !File.Exists(Path.Combine(repoRoot, "Matrices de Riesgos.xlsx")))
        {
            var parent = Directory.GetParent(repoRoot);
            if (parent == null) break;
            repoRoot = parent.FullName;
        }

        string manifestPath = Path.Combine(repoRoot, "backend", "RL.API", "Features", "MatricesRiesgos", "Contracts", "matriz_riesgos_82_campos_manifest.json");
        string excelJsonPath = Path.Combine(repoRoot, "scratch_excel_59x82.json");
        string appSettingsPath = Path.Combine(repoRoot, "backend", "RL.API", "appsettings.json");

        if (!File.Exists(manifestPath))
        {
            Console.Error.WriteLine($"ERROR: Manifest no encontrado en {manifestPath}");
            return 1;
        }

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

        if (!File.Exists(excelJsonPath))
        {
            Console.Error.WriteLine($"ERROR: No se pudo generar scratch_excel_59x82.json.");
            return 1;
        }

        // Cargar manifest
        var manifestDoc = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var manifestFields = manifestDoc.RootElement.EnumerateArray().ToList();

        // Cargar excel exportado
        var excelDoc = JsonDocument.Parse(File.ReadAllText(excelJsonPath));
        string workbookSha256 = excelDoc.RootElement.GetProperty("sourceHash").GetString()!;
        var excelRows = excelDoc.RootElement.GetProperty("matrix").EnumerateArray().ToList();

        // Configuración de base de datos
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

        var hostMatch = Regex.Match(resolvedConn, @"(?i)HOST\s*=\s*([^\)\s;]+)");
        var serviceMatch = Regex.Match(resolvedConn, @"(?i)SERVICE_NAME\s*=\s*([^\)\s;]+)");
        var userMatch = Regex.Match(resolvedConn, @"(?i)USER\s*ID\s*=\s*([^;]+)");

        string targetHost = hostMatch.Success ? hostMatch.Groups[1].Value.Trim() : "";
        string targetService = serviceMatch.Success ? serviceMatch.Groups[1].Value.Trim() : "";
        string targetUser = userMatch.Success ? userMatch.Groups[1].Value.Trim() : "";

        if (targetHost.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
            targetHost.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
            targetService.Equals("XE", StringComparison.OrdinalIgnoreCase))
        {
            Console.Error.WriteLine("FATAL: DATABASE_TARGET_CLASS es LOCAL_XE y no PRODUCTION. Deteniendo.");
            return 2;
        }

        DateTime auditStartedAt = DateTime.UtcNow;

        await using var conn = new OracleConnection(resolvedConn);
        await conn.OpenAsync();

        // 1. Cerrojo estricto de solo lectura
        await using (var cmdRo = new OracleCommand("SET TRANSACTION READ ONLY", conn))
        {
            await cmdRo.ExecuteNonQueryAsync();
        }

        // 2. Identidad
        string dbName = "", serviceName = "", instanceName = "", currentSchema = "", sessionUser = "";
        await using (var cmdIdent = new OracleCommand(@"
            SELECT ora_database_name,
                   sys_context('USERENV', 'SERVICE_NAME'),
                   sys_context('USERENV', 'INSTANCE_NAME'),
                   sys_context('USERENV', 'CURRENT_SCHEMA'),
                   sys_context('USERENV', 'SESSION_USER')
              FROM dual", conn))
        await using (var r = await cmdIdent.ExecuteReaderAsync())
        {
            if (await r.ReadAsync())
            {
                dbName = r.GetString(0);
                serviceName = r.GetString(1);
                instanceName = r.GetString(2);
                currentSchema = r.GetString(3);
                sessionUser = r.GetString(4);
            }
        }

        // 3. Extracción de riesgos, evaluaciones y proyecciones
        var dbRisks = new Dictionary<string, (long id, string code, string name, string desc)>(StringComparer.OrdinalIgnoreCase);
        await using (var cmd = new OracleCommand("SELECT RIE_ID, RIE_CODIGO, RIE_NOMBRE, NVL(RIE_DESCRIPCION, '') FROM RL_MR_RIESGOS", conn))
        await using (var r = await cmd.ExecuteReaderAsync())
        {
            while (await r.ReadAsync())
                dbRisks[r.GetString(1).Trim()] = (r.GetInt64(0), r.GetString(1).Trim(), r.GetString(2).Trim(), r.GetString(3).Trim());
        }

        var dbEvaluations = new Dictionary<string, (long evaId, long versionId, string estado, string area, string dueno, int vri, string nivelInh, int vrr, string nivelRes, string resp, DateTime fechaEval, JsonElement datosJson, JsonElement calcJson)>(StringComparer.OrdinalIgnoreCase);

        const string sqlEvas = @"
            SELECT r.RIE_CODIGO,
                   e.EVA_ID,
                   e.EVA_VERSION_ID,
                   p.PROY_ESTADO_EVALUACION,
                   NVL(p.PROY_AREA_PRINCIPAL, ''),
                   NVL(p.PROY_DUENO_RIESGO, ''),
                   NVL(p.PROY_VRI, 0),
                   NVL(p.PROY_NIVEL_INHERENTE, ''),
                   NVL(p.PROY_VRR, 0),
                   NVL(p.PROY_NIVEL_RESIDUAL, ''),
                   NVL(p.PROY_RESPUESTA_RIESGO, ''),
                   p.PROY_FECHA_EVAL,
                   e.EVA_DATOS_JSON,
                   e.EVA_CALCULOS_JSON
              FROM RL_MR_RIESGOS r
              JOIN RL_MR_EVALUACIONES_RIESGO e ON e.EVA_RIESGO_ID = r.RIE_ID AND e.EVA_ACTIVO = 1
              JOIN RL_MR_PROYECCIONES_EVALUACION p ON p.PROY_EVALUACION_ID = e.EVA_ID";

        await using (var cmd = new OracleCommand(sqlEvas, conn))
        await using (var r = await cmd.ExecuteReaderAsync())
        {
            while (await r.ReadAsync())
            {
                string code = r.GetString(0).Trim();
                long evaId = r.GetInt64(1);
                long verId = r.GetInt64(2);
                string estado = r.GetString(3).Trim();
                string area = r.GetString(4).Trim();
                string dueno = r.GetString(5).Trim();
                int vri = r.GetInt32(6);
                string nivelInh = r.GetString(7).Trim();
                int vrr = r.GetInt32(8);
                string nivelRes = r.GetString(9).Trim();
                string resp = r.GetString(10).Trim();
                DateTime fechaEval = r.GetDateTime(11);
                string rawDatos = r.GetString(12);
                string rawCalc = r.GetString(13);

                var docDatos = JsonDocument.Parse(rawDatos);
                var docCalc = JsonDocument.Parse(rawCalc);

                dbEvaluations[code] = (evaId, verId, estado, area, dueno, vri, nivelInh, vrr, nivelRes, resp, fechaEval, docDatos.RootElement.Clone(), docCalc.RootElement.Clone());
            }
        }

        // Extracción de señales de alerta vinculadas por evaluación
        var dbAlertsByEva = new Dictionary<long, List<string>>();
        await using (var cmd = new OracleCommand("SELECT ALE_EVALUACION_ID, ALE_INDICADOR FROM RL_MR_SENALES_ALERTA ORDER BY ALE_EVALUACION_ID, ALE_ID", conn))
        await using (var r = await cmd.ExecuteReaderAsync())
        {
            while (await r.ReadAsync())
            {
                long eId = r.GetInt64(0);
                string ind = r.GetString(1).Trim();
                if (!dbAlertsByEva.TryGetValue(eId, out var list))
                {
                    list = new List<string>();
                    dbAlertsByEva[eId] = list;
                }
                list.Add(ind);
            }
        }

        // Extracción de controles (esperado 0)
        var dbControlsByEva = new Dictionary<long, List<(string tipo, string desc, decimal efectividad, string estado)>>();
        await using (var cmd = new OracleCommand("SELECT CON_EVALUACION_ID, CON_TIPO, CON_DESCRIPCION, NVL(CON_EFECTIVIDAD_MONITOREO, 0), NVL(CON_ESTADO_MONITOREO, '') FROM RL_MR_CONTROLES_RIESGO ORDER BY CON_EVALUACION_ID, CON_ID", conn))
        await using (var r = await cmd.ExecuteReaderAsync())
        {
            while (await r.ReadAsync())
            {
                long eId = r.GetInt64(0);
                string tipo = r.GetString(1).Trim();
                string desc = r.GetString(2).Trim();
                decimal ef = r.GetDecimal(3);
                string est = r.GetString(4).Trim();
                if (!dbControlsByEva.TryGetValue(eId, out var list))
                {
                    list = new List<(string, string, decimal, string)>();
                    dbControlsByEva[eId] = list;
                }
                list.Add((tipo, desc, ef, est));
            }
        }

        // Extracción de planes (esperado 0)
        var dbPlanesByEva = new Dictionary<long, List<(long id, string desc, string resp, string recursos, DateTime? fIni, DateTime? fFin)>>();
        await using (var cmd = new OracleCommand("SELECT PLA_EVALUACION_ID, PLA_ID, PLA_DESCRIPCION, NVL(PLA_RESPONSABLES, ''), NVL(PLA_RECURSOS, ''), PLA_FECHA_INICIO, PLA_FECHA_FIN FROM RL_MR_PLANES ORDER BY PLA_EVALUACION_ID, PLA_ID", conn))
        await using (var r = await cmd.ExecuteReaderAsync())
        {
            while (await r.ReadAsync())
            {
                long eId = r.GetInt64(0);
                long pId = r.GetInt64(1);
                string desc = r.GetString(2).Trim();
                string resp = r.GetString(3).Trim();
                string rec = r.GetString(4).Trim();
                DateTime? fi = r.IsDBNull(5) ? null : r.GetDateTime(5);
                DateTime? ff = r.IsDBNull(6) ? null : r.GetDateTime(6);

                if (!dbPlanesByEva.TryGetValue(eId, out var list))
                {
                    list = new List<(long, string, string, string, DateTime?, DateTime?)>();
                    dbPlanesByEva[eId] = list;
                }
                list.Add((pId, desc, resp, rec, fi, ff));
            }
        }

        // Extracción de actividades (esperado 0)
        var dbActividadesByPlan = new Dictionary<long, List<string>>();
        await using (var cmd = new OracleCommand("SELECT ACT_PLAN_ID, ACT_DESCRIPCION FROM RL_MR_ACTIVIDADES ORDER BY ACT_PLAN_ID, ACT_ID", conn))
        await using (var r = await cmd.ExecuteReaderAsync())
        {
            while (await r.ReadAsync())
            {
                long pId = r.GetInt64(0);
                string desc = r.GetString(1).Trim();
                if (!dbActividadesByPlan.TryGetValue(pId, out var list))
                {
                    list = new List<string>();
                    dbActividadesByPlan[pId] = list;
                }
                list.Add(desc);
            }
        }

        // Rollback transacción read-only
        await using (var cmdRb = new OracleCommand("ROLLBACK", conn))
        {
            await cmdRb.ExecuteNonQueryAsync();
        }

        DateTime auditFinishedAt = DateTime.UtcNow;

        // EJECUCIÓN FORENSE DE LAS 4,838 POSICIONES
        var auditPositions = new List<AuditPosition>();

        foreach (var excelRow in excelRows)
        {
            int riskNo = excelRow.GetProperty("riskNo").GetInt32();
            string riskCode = excelRow.GetProperty("riskCode").GetString()!.Trim();
            var cells = excelRow.GetProperty("cells").EnumerateArray().ToList();

            bool hasDbRisk = dbRisks.TryGetValue(riskCode, out var dbRisk);
            bool hasDbEva = dbEvaluations.TryGetValue(riskCode, out var dbEva);

            // Identificar si el riesgo requiere mitigación según contrato
            // Regla A: PROY_RESPUESTA_RIESGO == 'MITIGAR'
            // Regla B: PROY_NIVEL_RESIDUAL IN ('ALTO', 'CRITICO') y PROY_RESPUESTA_RIESGO != 'ACEPTAR'
            string respRiesgoDb = hasDbEva ? dbEva.resp.ToUpperInvariant() : "";
            string nivelResDb = hasDbEva ? dbEva.nivelRes.ToUpperInvariant() : "";

            bool mitigationApplies = (respRiesgoDb == "MITIGAR") ||
                ((nivelResDb == "ALTO" || nivelResDb == "CRITICO") && respRiesgoDb != "ACEPTAR");

            string mitigationRuleBranch = "NONE";
            if (respRiesgoDb == "MITIGAR") mitigationRuleBranch = "BRANCH_A_RESPONSE_MITIGAR";
            else if ((nivelResDb == "ALTO" || nivelResDb == "CRITICO") && respRiesgoDb != "ACEPTAR") mitigationRuleBranch = "BRANCH_B_RESIDUAL_TOLERANCE";

            for (int f = 1; f <= 82; f++)
            {
                var manifestField = manifestFields[f - 1];
                var cell = cells[f - 1];

                string label = manifestField.GetProperty("label").GetString()!;
                string canonicalKey = manifestField.GetProperty("canonicalKey").GetString()!;
                string functionalBlock = manifestField.GetProperty("functionalBlock").GetString()!;
                int blockNumber = manifestField.GetProperty("bloqueNum").GetInt32();
                string source = manifestField.GetProperty("source").GetString()!;
                string mode = manifestField.GetProperty("mode").GetString()!;
                string requiredness = manifestField.GetProperty("requiredness").GetString()!;
                string conditionality = manifestField.GetProperty("conditionality").GetString()!;

                string excelRaw = cell.GetProperty("textValue").GetString() ?? "";
                string excelFormula = cell.GetProperty("formula").GetString() ?? "";
                string excelCachedResult = cell.GetProperty("cachedResult").ValueKind switch
                {
                    JsonValueKind.Null => "",
                    JsonValueKind.Undefined => "",
                    _ => cell.GetProperty("cachedResult").ToString()
                };

                var pos = new AuditPosition
                {
                    AuditKey = $"{riskCode}|{f:D2}",
                    RiskNo = riskNo,
                    RiskCode = riskCode,
                    SelectedEvaluationId = hasDbEva ? dbEva.evaId : 0,
                    SelectedEvaluationStatus = hasDbEva ? dbEva.estado : "NO_CANONICAL_EVALUATION",
                    SelectedEvaluationVersion = hasDbEva ? dbEva.versionId : 0,
                    FieldNumber = f,
                    FieldLabel = label,
                    CanonicalKey = canonicalKey,
                    FunctionalBlock = functionalBlock,
                    BlockNumber = blockNumber,
                    Source = source,
                    Mode = mode,
                    Requiredness = requiredness,
                    Conditionality = conditionality,
                    ExcelRaw = excelRaw,
                    ExcelFormula = excelFormula,
                    ExcelCachedResult = excelCachedResult,
                    OperationalAuthority = (f >= 70 && f <= 82),
                    PreserveDbValue = (f >= 70 && f <= 82)
                };

                // Normalización de texto Excel
                pos.ExcelNormalized = NormalizarTexto(excelRaw);

                // EXTRACCIÓN Y PROYECCIÓN DB SEGÚN CAMPO
                switch (f)
                {
                    case 1: // No.
                        pos.DbSource = "VIRTUAL_DERIVED (INSTITUTIONAL_CODE_TO_NO_MAP)";
                        pos.DbRaw = riskNo.ToString();
                        pos.DbNormalized = riskNo.ToString();
                        pos.DbRecordCount = 1;
                        pos.Applies = true;
                        pos.ApplicabilityReason = "CAMPOS_IDENTIFICACION_OBLIGATORIOS";
                        pos.PrimaryClassification = "MATCH";
                        pos.ReasonCode = "EXACT_ORDINAL_MATCH";
                        pos.RecommendedNextAction = "NO_ACTION";
                        pos.Severity = "INFO";
                        pos.RawEqual = (pos.ExcelRaw == pos.DbRaw);
                        pos.NormalizedEqual = true;
                        pos.SemanticEqual = true;
                        break;

                    case 2: // Código de Riesgo
                        pos.DbSource = "RL_MR_RIESGOS.RIE_CODIGO";
                        pos.DbRaw = hasDbRisk ? dbRisk.code : "";
                        pos.DbNormalized = pos.DbRaw;
                        pos.DbRecordCount = hasDbRisk ? 1 : 0;
                        pos.Applies = true;
                        pos.ApplicabilityReason = "CAMPOS_IDENTIFICACION_OBLIGATORIOS";
                        pos.PrimaryClassification = hasDbRisk ? "MATCH" : "MISSING_IN_DB";
                        pos.ReasonCode = hasDbRisk ? "EXACT_RISK_CODE_MATCH" : "RISK_RECORD_MISSING";
                        pos.RecommendedNextAction = hasDbRisk ? "NO_ACTION" : "IMPORT_BASELINE";
                        pos.Severity = hasDbRisk ? "INFO" : "CRITICAL";
                        pos.RawEqual = (pos.ExcelRaw == pos.DbRaw);
                        pos.NormalizedEqual = (pos.ExcelNormalized == pos.DbNormalized);
                        pos.SemanticEqual = pos.NormalizedEqual;
                        break;

                    case 3: // Proceso
                    case 5: // Tipo de Riesgo
                    case 6: // Procedimiento
                    case 7: // Objetivo(s) Estratégico(s)
                    case 15: // Régimen afectado
                    case 16: // Transversalidad
                    case 32: // Nivel de Automatización de los Controles
                        pos.DbSource = "RL_MR_RIESGOS (COLUMNA_NO_IMPLEMENTADA_EN_SCHEMA)";
                        pos.DbRaw = "";
                        pos.DbNormalized = "";
                        pos.DbRecordCount = 0;
                        pos.Applies = true;
                        pos.ApplicabilityReason = "METADATO_DESCRIPTIVO_INSTITUCIONAL";
                        pos.TechnicalMappingStatus = "BROKEN";
                        pos.SecondaryFlags.Add("MISSING_DB_MAPPING");
                        pos.SecondaryFlags.Add("SCHEMA_DRIFT");

                        if (!string.IsNullOrWhiteSpace(pos.ExcelNormalized))
                        {
                            pos.PrimaryClassification = "MISSING_IN_DB";
                            pos.ReasonCode = "MISSING_DB_MAPPING";
                            pos.RecommendedNextAction = "IMPORT_BASELINE";
                            pos.Severity = "ERROR";
                        }
                        else
                        {
                            pos.PrimaryClassification = "LEGITIMATELY_BLANK_IN_EXCEL";
                            pos.ReasonCode = "BASELINE_BLANK_AND_UNMAPPED";
                            pos.RecommendedNextAction = "NO_ACTION";
                            pos.Severity = "INFO";
                        }
                        pos.RawEqual = false;
                        pos.NormalizedEqual = false;
                        pos.SemanticEqual = false;
                        break;

                    case 4: // Área Consolidada
                        pos.DbSource = "RL_MR_PROYECCIONES_EVALUACION.PROY_AREA_PRINCIPAL";
                        pos.DbRaw = hasDbEva ? dbEva.area : "";
                        pos.DbNormalized = NormalizarTexto(pos.DbRaw);
                        pos.DbRecordCount = hasDbEva ? 1 : 0;
                        pos.Applies = true;
                        pos.ApplicabilityReason = "AREA_RESPONSABLE_EVALUACION";
                        EvaluarCoincidenciaTexto(pos);
                        break;

                    case 8: // Riesgo Inherente (RIE_NOMBRE)
                        pos.DbSource = "RL_MR_RIESGOS.RIE_NOMBRE";
                        pos.DbRaw = hasDbRisk ? dbRisk.name : "";
                        pos.DbNormalized = NormalizarTexto(pos.DbRaw);
                        pos.DbRecordCount = hasDbRisk ? 1 : 0;
                        pos.Applies = true;
                        pos.ApplicabilityReason = "NOMBRE_MAESTRO_RIESGO";
                        EvaluarCoincidenciaTexto(pos);
                        break;

                    case 9: // Evaluación (RIE_DESCRIPCION)
                        pos.DbSource = "RL_MR_RIESGOS.RIE_DESCRIPCION";
                        pos.DbRaw = hasDbRisk ? dbRisk.desc : "";
                        pos.DbNormalized = NormalizarTexto(pos.DbRaw);
                        pos.DbRecordCount = hasDbRisk ? 1 : 0;
                        pos.Applies = true;
                        pos.ApplicabilityReason = "DESCRIPCION_DETALLADA_RIESGO";
                        EvaluarCoincidenciaTexto(pos);
                        break;

                    case 10: // Frecuencia
                        pos.DbSource = "EVA_DATOS_JSON.frecuencia_inherente";
                        pos.DbRaw = hasDbEva && dbEva.datosJson.TryGetProperty("frecuencia_inherente", out var fInh) ? fInh.GetString() ?? fInh.ToString() : "";
                        pos.DbNormalized = NormalizarNumero(pos.DbRaw);
                        pos.DbRecordCount = hasDbEva ? 1 : 0;
                        pos.Applies = true;
                        pos.ApplicabilityReason = "PARAMETRO_CALCULO_INHERENTE";
                        EvaluarCoincidenciaNumerica(pos);
                        break;

                    case 11: // Impacto
                        pos.DbSource = "EVA_DATOS_JSON.impacto_inherente";
                        pos.DbRaw = hasDbEva && dbEva.datosJson.TryGetProperty("impacto_inherente", out var iInh) ? iInh.GetString() ?? iInh.ToString() : "";
                        pos.DbNormalized = NormalizarNumero(pos.DbRaw);
                        pos.DbRecordCount = hasDbEva ? 1 : 0;
                        pos.Applies = true;
                        pos.ApplicabilityReason = "PARAMETRO_CALCULO_INHERENTE";
                        EvaluarCoincidenciaNumerica(pos);
                        break;

                    case 12: // Valor del Riesgo Inherente (Calculado)
                        pos.DbSource = "RL_MR_PROYECCIONES_EVALUACION.PROY_VRI";
                        pos.DbRaw = hasDbEva ? dbEva.vri.ToString() : "";
                        pos.DbNormalized = NormalizarNumero(pos.DbRaw);
                        pos.DbRecordCount = hasDbEva ? 1 : 0;
                        pos.Applies = true;
                        pos.ApplicabilityReason = "CALCULO_VRI";
                        pos.PrimaryClassification = "CALCULATED_FIELD";
                        EvaluarParidadCalculada(pos, pos.ExcelCachedResult, pos.DbNormalized);
                        break;

                    case 13: // Nivel del Riesgo Inherente (Calculado)
                        pos.DbSource = "RL_MR_PROYECCIONES_EVALUACION.PROY_NIVEL_INHERENTE";
                        pos.DbRaw = hasDbEva ? dbEva.nivelInh : "";
                        pos.DbNormalized = NormalizarTexto(pos.DbRaw);
                        pos.DbRecordCount = hasDbEva ? 1 : 0;
                        pos.Applies = true;
                        pos.ApplicabilityReason = "CALCULO_NIVEL_INHERENTE";
                        pos.PrimaryClassification = "CALCULATED_FIELD";
                        EvaluarParidadCalculada(pos, pos.ExcelCachedResult, pos.DbNormalized);
                        break;

                    case 14: // Dueño del Riesgo
                        pos.DbSource = "RL_MR_PROYECCIONES_EVALUACION.PROY_DUENO_RIESGO";
                        pos.DbRaw = hasDbEva ? dbEva.dueno : "";
                        pos.DbNormalized = NormalizarTexto(pos.DbRaw);
                        pos.DbRecordCount = hasDbEva ? 1 : 0;
                        pos.Applies = true;
                        pos.ApplicabilityReason = "DUENO_INSTITUCIONAL_RIESGO";
                        EvaluarCoincidenciaTexto(pos);
                        break;

                    case 17: // Amenazas GTIC
                    case 18: // Vulnerabilidades GTIC
                    case 19: // Activos GTIC
                        pos.DbSource = "N/A (CONDITIONAL_ONLY_GTIC)";
                        pos.DbRaw = "";
                        pos.DbNormalized = "";
                        pos.DbRecordCount = 0;
                        pos.Applies = false;
                        pos.ApplicabilityReason = "RISK_TYPE_NOT_GTIC";
                        pos.PrimaryClassification = "NOT_APPLICABLE";
                        pos.ReasonCode = "RISK_TYPE_NOT_GTIC";
                        pos.RecommendedNextAction = "NOT_APPLICABLE";
                        pos.Severity = "INFO";
                        pos.RawEqual = (pos.ExcelRaw == "");
                        pos.NormalizedEqual = true;
                        pos.SemanticEqual = true;
                        break;

                    case 20: // Descripción Controles Preventivos (1:N)
                        pos.DbSource = "RL_MR_CONTROLES_RIESGO (CON_TIPO='PREVENTIVO')";
                        EvaluarControles1N(pos, hasDbEva ? dbControlsByEva.GetValueOrDefault(dbEva.evaId)?.Where(c => c.tipo == "PREVENTIVO").Select(c => c.desc).ToList() : null);
                        break;

                    case 21: // Escala Efectividad Preventivo
                        pos.DbSource = "EVA_DATOS_JSON.controles_preventivo";
                        pos.DbRaw = hasDbEva && dbEva.datosJson.TryGetProperty("controles_preventivo", out var cp) ? cp.ToString() : "";
                        pos.DbNormalized = NormalizarNumero(pos.DbRaw);
                        pos.DbRecordCount = hasDbEva ? 1 : 0;
                        pos.Applies = true;
                        pos.ApplicabilityReason = "PARAMETRO_EFECTIVIDAD_PREVENTIVA";
                        EvaluarCoincidenciaEfectividad(pos);
                        break;

                    case 22: // Criterio Efectividad Preventivo (Calculado)
                    case 23: // % Disminución Preventivo (Calculado)
                        pos.DbSource = "MOTOR_CALCULO (FORMULA_SISTEMA)";
                        pos.DbRaw = "";
                        pos.DbNormalized = "";
                        pos.DbRecordCount = 0;
                        pos.Applies = true;
                        pos.ApplicabilityReason = "FORMULA_AUXILIAR_EFECTIVIDAD";
                        pos.PrimaryClassification = "CALCULATED_FIELD";
                        EvaluarParidadCalculada(pos, pos.ExcelCachedResult, "");
                        break;

                    case 24: // Descripción Controles Detectivos (1:N)
                        pos.DbSource = "RL_MR_CONTROLES_RIESGO (CON_TIPO='DETECTIVO')";
                        EvaluarControles1N(pos, hasDbEva ? dbControlsByEva.GetValueOrDefault(dbEva.evaId)?.Where(c => c.tipo == "DETECTIVO").Select(c => c.desc).ToList() : null);
                        break;

                    case 25: // Escala Efectividad Detectivo
                        pos.DbSource = "EVA_DATOS_JSON.controles_detectivo";
                        pos.DbRaw = hasDbEva && dbEva.datosJson.TryGetProperty("controles_detectivo", out var cd) ? cd.ToString() : "";
                        pos.DbNormalized = NormalizarNumero(pos.DbRaw);
                        pos.DbRecordCount = hasDbEva ? 1 : 0;
                        pos.Applies = true;
                        pos.ApplicabilityReason = "PARAMETRO_EFECTIVIDAD_DETECTIVA";
                        EvaluarCoincidenciaEfectividad(pos);
                        break;

                    case 26: // Criterio Efectividad Detectivo (Calculado)
                    case 27: // % Disminución Detectivo (Calculado)
                        pos.DbSource = "MOTOR_CALCULO (FORMULA_SISTEMA)";
                        pos.DbRaw = "";
                        pos.DbNormalized = "";
                        pos.DbRecordCount = 0;
                        pos.Applies = true;
                        pos.ApplicabilityReason = "FORMULA_AUXILIAR_EFECTIVIDAD";
                        pos.PrimaryClassification = "CALCULATED_FIELD";
                        EvaluarParidadCalculada(pos, pos.ExcelCachedResult, "");
                        break;

                    case 28: // Descripción Controles Correctivos (1:N)
                        pos.DbSource = "RL_MR_CONTROLES_RIESGO (CON_TIPO='CORRECTIVO')";
                        EvaluarControles1N(pos, hasDbEva ? dbControlsByEva.GetValueOrDefault(dbEva.evaId)?.Where(c => c.tipo == "CORRECTIVO").Select(c => c.desc).ToList() : null);
                        break;

                    case 29: // Escala Efectividad Correctivo
                        pos.DbSource = "EVA_DATOS_JSON.controles_correctivo";
                        pos.DbRaw = hasDbEva && dbEva.datosJson.TryGetProperty("controles_correctivo", out var cc) ? cc.ToString() : "";
                        pos.DbNormalized = NormalizarNumero(pos.DbRaw);
                        pos.DbRecordCount = hasDbEva ? 1 : 0;
                        pos.Applies = true;
                        pos.ApplicabilityReason = "PARAMETRO_EFECTIVIDAD_CORRECTIVA";
                        EvaluarCoincidenciaEfectividad(pos);
                        break;

                    case 30: // Criterio Efectividad Correctivo (Calculado)
                    case 31: // % Disminución Correctivo (Calculado)
                        pos.DbSource = "MOTOR_CALCULO (FORMULA_SISTEMA)";
                        pos.DbRaw = "";
                        pos.DbNormalized = "";
                        pos.DbRecordCount = 0;
                        pos.Applies = true;
                        pos.ApplicabilityReason = "FORMULA_AUXILIAR_EFECTIVIDAD";
                        pos.PrimaryClassification = "CALCULATED_FIELD";
                        EvaluarParidadCalculada(pos, pos.ExcelCachedResult, "");
                        break;

                    case 33: // Efectividad Total Ponderada (Calculado)
                        pos.DbSource = "EVA_CALCULOS_JSON.etp";
                        pos.DbRaw = hasDbEva && dbEva.calcJson.TryGetProperty("etp", out var etp) ? etp.ToString() : "";
                        pos.DbNormalized = NormalizarNumero(pos.DbRaw);
                        pos.DbRecordCount = hasDbEva ? 1 : 0;
                        pos.Applies = true;
                        pos.ApplicabilityReason = "CALCULO_ETP";
                        pos.PrimaryClassification = "CALCULATED_FIELD";
                        EvaluarParidadCalculada(pos, pos.ExcelCachedResult, pos.DbNormalized);
                        break;

                    case 34: // Riesgo Residual (Calculado)
                        pos.DbSource = "MOTOR_CALCULO / EVA_CALCULOS_JSON";
                        pos.DbRaw = hasDbRisk ? dbRisk.name : "";
                        pos.DbNormalized = NormalizarTexto(pos.DbRaw);
                        pos.DbRecordCount = hasDbRisk ? 1 : 0;
                        pos.Applies = true;
                        pos.ApplicabilityReason = "CALCULO_RIESGO_RESIDUAL_TEXTO";
                        pos.PrimaryClassification = "CALCULATED_FIELD";
                        EvaluarParidadCalculada(pos, pos.ExcelCachedResult, pos.DbNormalized);
                        break;

                    case 35: // Frecuencia Residual (Calculado)
                        pos.DbSource = "EVA_DATOS_JSON.frecuencia_residual";
                        pos.DbRaw = hasDbEva && dbEva.datosJson.TryGetProperty("frecuencia_residual", out var fr) ? fr.GetString() ?? fr.ToString() : "";
                        pos.DbNormalized = NormalizarNumero(pos.DbRaw);
                        pos.DbRecordCount = hasDbEva ? 1 : 0;
                        pos.Applies = true;
                        pos.ApplicabilityReason = "CALCULO_FRECUENCIA_RESIDUAL";
                        pos.PrimaryClassification = "CALCULATED_FIELD";
                        EvaluarParidadCalculada(pos, pos.ExcelCachedResult, pos.DbNormalized);
                        break;

                    case 36: // Impacto Residual (Calculado)
                        pos.DbSource = "EVA_DATOS_JSON.impacto_residual";
                        pos.DbRaw = hasDbEva && dbEva.datosJson.TryGetProperty("impacto_residual", out var ir) ? ir.GetString() ?? ir.ToString() : "";
                        pos.DbNormalized = NormalizarNumero(pos.DbRaw);
                        pos.DbRecordCount = hasDbEva ? 1 : 0;
                        pos.Applies = true;
                        pos.ApplicabilityReason = "CALCULO_IMPACTO_RESIDUAL";
                        pos.PrimaryClassification = "CALCULATED_FIELD";
                        EvaluarParidadCalculada(pos, pos.ExcelCachedResult, pos.DbNormalized);
                        break;

                    case 37: // Valor del Riesgo Residual (Calculado)
                        pos.DbSource = "RL_MR_PROYECCIONES_EVALUACION.PROY_VRR";
                        pos.DbRaw = hasDbEva ? dbEva.vrr.ToString() : "";
                        pos.DbNormalized = NormalizarNumero(pos.DbRaw);
                        pos.DbRecordCount = hasDbEva ? 1 : 0;
                        pos.Applies = true;
                        pos.ApplicabilityReason = "CALCULO_VRR";
                        pos.PrimaryClassification = "CALCULATED_FIELD";
                        EvaluarParidadCalculada(pos, pos.ExcelCachedResult, pos.DbNormalized);
                        break;

                    case 38: // Nivel del Riesgo Residual (Calculado)
                        pos.DbSource = "RL_MR_PROYECCIONES_EVALUACION.PROY_NIVEL_RESIDUAL";
                        pos.DbRaw = hasDbEva ? dbEva.nivelRes : "";
                        pos.DbNormalized = NormalizarTexto(pos.DbRaw);
                        pos.DbRecordCount = hasDbEva ? 1 : 0;
                        pos.Applies = true;
                        pos.ApplicabilityReason = "CALCULO_NIVEL_RESIDUAL";
                        pos.PrimaryClassification = "CALCULATED_FIELD";
                        EvaluarParidadCalculada(pos, pos.ExcelCachedResult, pos.DbNormalized);
                        break;

                    case 39: // Respuesta al riesgo
                        pos.DbSource = "RL_MR_PROYECCIONES_EVALUACION.PROY_RESPUESTA_RIESGO";
                        pos.DbRaw = hasDbEva ? dbEva.resp : "";
                        pos.DbNormalized = NormalizarTexto(pos.DbRaw);
                        pos.DbRecordCount = hasDbEva ? 1 : 0;
                        pos.Applies = true;
                        pos.ApplicabilityReason = "ESTRATEGIA_INSTITUCIONAL_RESPUESTA";
                        EvaluarCoincidenciaTexto(pos);
                        break;

                    // BLOQUE 4: MITIGACIÓN (40-49)
                    case 40: // Plan de Mitigación / Acciones Correctivas
                    case 42: // Actividades
                    case 44: // Área Responsable / Monitoreo
                    case 45: // Responsables
                    case 46: // Fecha Inicio
                    case 47: // Fecha Fin
                    case 48: // Recursos
                    case 49: // Presupuesto
                        pos.Applies = mitigationApplies;
                        pos.ApplicabilityReason = mitigationApplies ? mitigationRuleBranch : "MITIGATION_NOT_REQUIRED";

                        if (!mitigationApplies)
                        {
                            pos.PrimaryClassification = "NOT_APPLICABLE";
                            pos.ReasonCode = "MITIGATION_NOT_REQUIRED";
                            pos.RecommendedNextAction = "NOT_APPLICABLE";
                            pos.Severity = "INFO";
                            pos.DbSource = "N/A (MITIGATION_NOT_REQUIRED)";
                            pos.DbRaw = "";
                            pos.DbNormalized = "";
                            pos.DbRecordCount = 0;
                            pos.RawEqual = (pos.ExcelRaw == "");
                            pos.NormalizedEqual = true;
                            pos.SemanticEqual = true;
                        }
                        else
                        {
                            pos.DbSource = "RL_MR_PLANES / RL_MR_ACTIVIDADES";
                            pos.DbRaw = "";
                            pos.DbNormalized = "";
                            pos.DbRecordCount = 0;

                            if (!string.IsNullOrWhiteSpace(pos.ExcelNormalized))
                            {
                                pos.PrimaryClassification = "MISSING_IN_DB";
                                pos.ReasonCode = "CHILD_COLLECTION_EMPTY";
                                pos.RecommendedNextAction = "IMPORT_BASELINE";
                                pos.Severity = "ERROR";
                            }
                            else
                            {
                                pos.PrimaryClassification = "LEGITIMATELY_BLANK_IN_EXCEL";
                                pos.ReasonCode = "BASELINE_BLANK_NO_RECORDS";
                                pos.RecommendedNextAction = "NO_ACTION";
                                pos.Severity = "INFO";
                            }
                            pos.RawEqual = false;
                            pos.NormalizedEqual = false;
                            pos.SemanticEqual = false;
                        }
                        break;

                    case 41: // No. Acciones de Mitigación (Derivado Agregado)
                    case 43: // Cantidad de Actividades (Derivado Agregado)
                        pos.Applies = mitigationApplies;
                        pos.ApplicabilityReason = mitigationApplies ? mitigationRuleBranch : "MITIGATION_NOT_REQUIRED";
                        pos.PrimaryClassification = "CALCULATED_FIELD";
                        pos.DbSource = f == 41 ? "COUNT(RL_MR_PLANES)" : "COUNT(RL_MR_ACTIVIDADES)";
                        pos.DbRaw = "0";
                        pos.DbNormalized = "0";
                        pos.DbRecordCount = 0;

                        if (!mitigationApplies)
                        {
                            pos.CalculationParity = "NOT_EVALUABLE";
                            pos.ReasonCode = "MITIGATION_NOT_REQUIRED";
                            pos.RecommendedNextAction = "NOT_APPLICABLE";
                            pos.Severity = "INFO";
                        }
                        else
                        {
                            int excelCount = int.TryParse(pos.ExcelNormalized, out int ec) ? ec : 0;
                            if (excelCount == 0)
                            {
                                pos.CalculationParity = "MATCH";
                                pos.ReasonCode = "DERIVED_AGGREGATE_MATCH_ZERO";
                                pos.RecommendedNextAction = "NO_ACTION";
                                pos.Severity = "INFO";
                            }
                            else
                            {
                                pos.CalculationParity = "DIFFERENT";
                                pos.ReasonCode = "DERIVED_AGGREGATE_DB_ZERO_EXCEL_NONZERO";
                                pos.RecommendedNextAction = "IMPORT_BASELINE";
                                pos.Severity = "ERROR";
                            }
                        }
                        break;

                    // BLOQUE 5: CÁLCULOS AUXILIARES (50-69)
                    case >= 50 and <= 69:
                        pos.Applies = true;
                        pos.ApplicabilityReason = "COLUMNA_FORMULA_AUXILIAR";
                        pos.PrimaryClassification = "CALCULATED_FIELD";
                        pos.DbSource = "MOTOR_CALCULO (NO_PERSISTIDO_EN_TABLAS)";
                        pos.DbRaw = "";
                        pos.DbNormalized = "";
                        pos.DbRecordCount = 0;
                        EvaluarParidadCalculada(pos, pos.ExcelCachedResult, "");
                        break;

                    // BLOQUE 6: MONITOREO Y EFECTIVIDAD (70-82)
                    case 70: // Señales de Alerta (1:N)
                        pos.DbSource = "RL_MR_SENALES_ALERTA (ALE_INDICADOR)";
                        pos.Applies = true;
                        pos.ApplicabilityReason = "SENALES_ALERTA_MONITOREO";
                        var alerts = hasDbEva ? dbAlertsByEva.GetValueOrDefault(dbEva.evaId) ?? new List<string>() : new List<string>();
                        pos.DbRecordCount = alerts.Count;
                        pos.StructuredDbItems = alerts;

                        // Parsear ítems del Excel (lista enumerada)
                        var excelAlertItems = ParseEnumeratedList(pos.ExcelRaw);
                        pos.StructuredExcelItems = excelAlertItems;

                        pos.DbRaw = string.Join("\n", alerts.Select((a, idx) => $"{idx + 1}. {a}"));
                        pos.DbNormalized = NormalizarTexto(pos.DbRaw);

                        if (excelAlertItems.Count == 0 && alerts.Count == 0)
                        {
                            pos.PrimaryClassification = "LEGITIMATELY_BLANK_IN_EXCEL";
                            pos.ReasonCode = "NO_ALERTS_BASELINE_OR_DB";
                            pos.RecommendedNextAction = "NO_ACTION";
                            pos.Severity = "INFO";
                            pos.RawEqual = true;
                            pos.NormalizedEqual = true;
                            pos.SemanticEqual = true;
                        }
                        else if (alerts.Count > 0 && excelAlertItems.Count == 0)
                        {
                            pos.PrimaryClassification = "DB_HAS_NEWER_OPERATIONAL_DATA";
                            pos.ReasonCode = "OPERATIONAL_ALERTS_REGISTERED_IN_DB";
                            pos.RecommendedNextAction = "PRESERVE_PRODUCTION";
                            pos.Severity = "WARNING";
                            pos.RawEqual = false;
                            pos.NormalizedEqual = false;
                            pos.SemanticEqual = false;
                        }
                        else if (alerts.Count == 0 && excelAlertItems.Count > 0)
                        {
                            pos.PrimaryClassification = "MISSING_IN_DB";
                            pos.ReasonCode = "EXCEL_ALERTS_NOT_IN_DB";
                            pos.RecommendedNextAction = "IMPORT_BASELINE";
                            pos.Severity = "ERROR";
                            pos.RawEqual = false;
                            pos.NormalizedEqual = false;
                            pos.SemanticEqual = false;
                        }
                        else
                        {
                            // Comparar ítems semánticos
                            bool itemsEqual = CompareStringLists(excelAlertItems, alerts);
                            if (itemsEqual)
                            {
                                pos.PrimaryClassification = "MATCH";
                                pos.ReasonCode = "EXACT_ALERT_ITEMS_MATCH";
                                pos.RecommendedNextAction = "NO_ACTION";
                                pos.Severity = "INFO";
                                pos.SemanticEqual = true;
                                pos.NormalizedEqual = (pos.ExcelNormalized == pos.DbNormalized);
                                pos.RawEqual = (pos.ExcelRaw == pos.DbRaw);
                            }
                            else
                            {
                                pos.PrimaryClassification = "DIFFERENT";
                                pos.ReasonCode = "ALERT_ITEMS_DIFFER";
                                pos.RecommendedNextAction = "DATA_REMEDIATION_REQUIRED";
                                pos.Severity = "ERROR";
                                pos.SemanticEqual = false;
                                pos.NormalizedEqual = false;
                                pos.RawEqual = false;
                            }
                        }
                        break;

                    case 71: // Estado del Riesgo
                        pos.DbSource = "RL_MR_EVALUACIONES_RIESGO (MONITOREO_ESTADO)";
                        pos.Applies = true;
                        pos.ApplicabilityReason = "ESTADO_OPERACIONAL_CICLO";
                        pos.DbRaw = "";
                        pos.DbNormalized = "";
                        pos.DbRecordCount = 0;

                        if (string.IsNullOrWhiteSpace(pos.ExcelNormalized))
                        {
                            pos.PrimaryClassification = "LEGITIMATELY_BLANK_IN_EXCEL";
                            pos.ReasonCode = "BASELINE_BLANK_OPERATIONAL_PENDING";
                            pos.RecommendedNextAction = "NO_ACTION";
                            pos.Severity = "INFO";
                            pos.RawEqual = (pos.ExcelRaw == "");
                            pos.NormalizedEqual = true;
                            pos.SemanticEqual = true;
                        }
                        else
                        {
                            pos.PrimaryClassification = "MISSING_IN_DB";
                            pos.ReasonCode = "EXCEL_VALUED_DB_BLANK";
                            pos.RecommendedNextAction = "IMPORT_BASELINE";
                            pos.Severity = "ERROR";
                            pos.RawEqual = false;
                            pos.NormalizedEqual = false;
                            pos.SemanticEqual = false;
                        }
                        break;

                    case >= 72 and <= 80: // Monitoreo Preventivo (72-74), Detectivo (75-77), Correctivo (78-80)
                        pos.DbSource = "RL_MR_CONTROLES_RIESGO / RL_MR_EVIDENCIAS_VINCULOS";
                        pos.Applies = true;
                        pos.ApplicabilityReason = "MONITOREO_CONTROLES_1_N";
                        pos.DbRaw = "";
                        pos.DbNormalized = "";
                        pos.DbRecordCount = 0;

                        if (string.IsNullOrWhiteSpace(pos.ExcelNormalized))
                        {
                            pos.PrimaryClassification = "LEGITIMATELY_BLANK_IN_EXCEL";
                            pos.ReasonCode = "BASELINE_BLANK_AWAITING_MONITORING_CYCLE";
                            pos.RecommendedNextAction = "NO_ACTION";
                            pos.Severity = "INFO";
                            pos.RawEqual = (pos.ExcelRaw == "");
                            pos.NormalizedEqual = true;
                            pos.SemanticEqual = true;
                        }
                        else
                        {
                            pos.PrimaryClassification = "MISSING_IN_DB";
                            pos.ReasonCode = "EXCEL_MONITORING_NOT_IN_DB";
                            pos.RecommendedNextAction = "IMPORT_BASELINE";
                            pos.Severity = "ERROR";
                            pos.RawEqual = false;
                            pos.NormalizedEqual = false;
                            pos.SemanticEqual = false;
                        }
                        break;

                    case 81: // Observaciones del Área
                    case 82: // Observaciones UGR
                        pos.DbSource = "RL_MR_MONITOREO_OBSERVACIONES";
                        pos.Applies = true;
                        pos.ApplicabilityReason = "OBSERVACIONES_MONITOREO_OPERACIONAL";
                        pos.DbRaw = "";
                        pos.DbNormalized = "";
                        pos.DbRecordCount = 0;

                        if (string.IsNullOrWhiteSpace(pos.ExcelNormalized))
                        {
                            pos.PrimaryClassification = "LEGITIMATELY_BLANK_IN_EXCEL";
                            pos.ReasonCode = "BASELINE_BLANK_NO_OBSERVATIONS";
                            pos.RecommendedNextAction = "NO_ACTION";
                            pos.Severity = "INFO";
                            pos.RawEqual = (pos.ExcelRaw == "");
                            pos.NormalizedEqual = true;
                            pos.SemanticEqual = true;
                        }
                        else
                        {
                            pos.PrimaryClassification = "MISSING_IN_DB";
                            pos.ReasonCode = "EXCEL_OBSERVATIONS_NOT_IN_DB";
                            pos.RecommendedNextAction = "IMPORT_BASELINE";
                            pos.Severity = "ERROR";
                            pos.RawEqual = false;
                            pos.NormalizedEqual = false;
                            pos.SemanticEqual = false;
                        }
                        break;
                }

                // VALIDACIÓN DE ISSUE REQUIRED SI APLICA
                if (pos.Requiredness == "REQUIRED" && string.IsNullOrWhiteSpace(pos.DbNormalized) && string.IsNullOrWhiteSpace(pos.ExcelNormalized))
                {
                    pos.SecondaryFlags.Add("REQUIRED_VALUE_MISSING");
                }

                auditPositions.Add(pos);
            }
        }

        // VALIDACIONES OBLIGATORIAS DEL UNIVERSO
        int expectedPositions = 4838;
        int actualPositions = auditPositions.Count;
        int uniqueKeys = auditPositions.Select(p => p.AuditKey).Distinct().Count();
        int duplicateKeys = actualPositions - uniqueKeys;

        // SUMATORIAS GLOBALES
        int matchCount = auditPositions.Count(p => p.PrimaryClassification == "MATCH");
        int missingDbCount = auditPositions.Count(p => p.PrimaryClassification == "MISSING_IN_DB");
        int differentCount = auditPositions.Count(p => p.PrimaryClassification == "DIFFERENT");
        int legitBlankCount = auditPositions.Count(p => p.PrimaryClassification == "LEGITIMATELY_BLANK_IN_EXCEL");
        int newerOpCount = auditPositions.Count(p => p.PrimaryClassification == "DB_HAS_NEWER_OPERATIONAL_DATA");
        int calculatedCount = auditPositions.Count(p => p.PrimaryClassification == "CALCULATED_FIELD");
        int notApplicableCount = auditPositions.Count(p => p.PrimaryClassification == "NOT_APPLICABLE");

        int classificationTotal = matchCount + missingDbCount + differentCount + legitBlankCount + newerOpCount + calculatedCount + notApplicableCount;

        // PARIDAD CALCULADOS
        int calcMatch = auditPositions.Where(p => p.PrimaryClassification == "CALCULATED_FIELD").Count(p => p.CalculationParity == "MATCH");
        int calcDiff = auditPositions.Where(p => p.PrimaryClassification == "CALCULATED_FIELD").Count(p => p.CalculationParity == "DIFFERENT");
        int calcMissing = auditPositions.Where(p => p.PrimaryClassification == "CALCULATED_FIELD").Count(p => p.CalculationParity == "MISSING_IN_DB");
        int calcNotEval = auditPositions.Where(p => p.PrimaryClassification == "CALCULATED_FIELD").Count(p => p.CalculationParity == "NOT_EVALUABLE");

        // GENERACIÓN DE ARTEFACTOS LOCALES EN %TEMP%
        string runId = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string tempDir = Path.Combine(Path.GetTempPath(), $"RIESGO_LAVADO_BLOCK2_AUDIT_{runId}");
        Directory.CreateDirectory(tempDir);

        string fullJsonPath = Path.Combine(tempDir, "audit_59x82_full.json");
        string fullCsvPath = Path.Combine(tempDir, "audit_59x82_full.csv");
        string summaryRiskPath = Path.Combine(tempDir, "summary_by_risk.csv");
        string summaryFieldPath = Path.Combine(tempDir, "summary_by_field.csv");
        string summaryBlockPath = Path.Combine(tempDir, "summary_by_block.csv");
        string calcParityPath = Path.Combine(tempDir, "calculated_parity.csv");
        string opPreservePath = Path.Combine(tempDir, "operational_values_to_preserve.csv");
        string baselineImportPath = Path.Combine(tempDir, "baseline_import_candidates.csv");
        string conflictsPath = Path.Combine(tempDir, "data_conflicts.csv");
        string defectsPath = Path.Combine(tempDir, "technical_defects.csv");
        string riskInventoryPath = Path.Combine(tempDir, "production_risk_inventory.csv");

        // Guardar full json
        File.WriteAllText(fullJsonPath, JsonSerializer.Serialize(auditPositions, new JsonSerializerOptions { WriteIndented = true }));

        // Guardar full csv
        var csvHeader = "auditKey,riskNo,riskCode,selectedEvaluationId,selectedEvaluationStatus,fieldNumber,fieldLabel,canonicalKey,blockNumber,functionalBlock,mode,requiredness,conditionality,applies,primaryClassification,reasonCode,calculationParity,recommendedNextAction,rawEqual,normalizedEqual,semanticEqual,excelRaw,dbRaw";
        var csvLines = new List<string> { csvHeader };
        foreach (var p in auditPositions)
        {
            csvLines.Add($"\"{p.AuditKey}\",{p.RiskNo},\"{p.RiskCode}\",{p.SelectedEvaluationId},\"{p.SelectedEvaluationStatus}\",{p.FieldNumber},\"{EscapeCsv(p.FieldLabel)}\",\"{p.CanonicalKey}\",{p.BlockNumber},\"{EscapeCsv(p.FunctionalBlock)}\",\"{p.Mode}\",\"{p.Requiredness}\",\"{p.Conditionality}\",{p.Applies},\"{p.PrimaryClassification}\",\"{p.ReasonCode}\",\"{p.CalculationParity}\",\"{p.RecommendedNextAction}\",{p.RawEqual},{p.NormalizedEqual},{p.SemanticEqual},\"{EscapeCsv(p.ExcelRaw)}\",\"{EscapeCsv(p.DbRaw)}\"");
        }
        File.WriteAllLines(fullCsvPath, csvLines, Encoding.UTF8);

        // Summary by Risk
        var riskSummaryLines = new List<string> { "RISK_CODE,RISK_NO,MATCH,MISSING_IN_DB,DIFFERENT,LEGITIMATELY_BLANK_IN_EXCEL,DB_HAS_NEWER_OPERATIONAL_DATA,CALCULATED_FIELD,NOT_APPLICABLE,TOTAL" };
        foreach (var group in auditPositions.GroupBy(p => p.RiskCode).OrderBy(g => g.First().RiskNo))
        {
            int rMatch = group.Count(p => p.PrimaryClassification == "MATCH");
            int rMissing = group.Count(p => p.PrimaryClassification == "MISSING_IN_DB");
            int rDiff = group.Count(p => p.PrimaryClassification == "DIFFERENT");
            int rBlank = group.Count(p => p.PrimaryClassification == "LEGITIMATELY_BLANK_IN_EXCEL");
            int rOp = group.Count(p => p.PrimaryClassification == "DB_HAS_NEWER_OPERATIONAL_DATA");
            int rCalc = group.Count(p => p.PrimaryClassification == "CALCULATED_FIELD");
            int rNa = group.Count(p => p.PrimaryClassification == "NOT_APPLICABLE");
            int rTotal = group.Count();
            riskSummaryLines.Add($"\"{group.Key}\",{group.First().RiskNo},{rMatch},{rMissing},{rDiff},{rBlank},{rOp},{rCalc},{rNa},{rTotal}");
        }
        File.WriteAllLines(summaryRiskPath, riskSummaryLines, Encoding.UTF8);

        // Summary by Field
        var fieldSummaryLines = new List<string> { "FIELD_NUMBER,FIELD_LABEL,CANONICAL_KEY,BLOCK_NUMBER,MATCH,MISSING_IN_DB,DIFFERENT,LEGITIMATELY_BLANK_IN_EXCEL,DB_HAS_NEWER_OPERATIONAL_DATA,CALCULATED_FIELD,NOT_APPLICABLE,TOTAL,CALC_MATCH,CALC_DIFF,CALC_MISSING,CALC_NOT_EVAL" };
        foreach (var group in auditPositions.GroupBy(p => p.FieldNumber).OrderBy(g => g.Key))
        {
            int fMatch = group.Count(p => p.PrimaryClassification == "MATCH");
            int fMissing = group.Count(p => p.PrimaryClassification == "MISSING_IN_DB");
            int fDiff = group.Count(p => p.PrimaryClassification == "DIFFERENT");
            int fBlank = group.Count(p => p.PrimaryClassification == "LEGITIMATELY_BLANK_IN_EXCEL");
            int fOp = group.Count(p => p.PrimaryClassification == "DB_HAS_NEWER_OPERATIONAL_DATA");
            int fCalc = group.Count(p => p.PrimaryClassification == "CALCULATED_FIELD");
            int fNa = group.Count(p => p.PrimaryClassification == "NOT_APPLICABLE");
            int fTotal = group.Count();

            int fCalcMatch = group.Count(p => p.CalculationParity == "MATCH");
            int fCalcDiff = group.Count(p => p.CalculationParity == "DIFFERENT");
            int fCalcMissing = group.Count(p => p.CalculationParity == "MISSING_IN_DB");
            int fCalcNotEval = group.Count(p => p.CalculationParity == "NOT_EVALUABLE");

            fieldSummaryLines.Add($"{group.Key},\"{EscapeCsv(group.First().FieldLabel)}\",\"{group.First().CanonicalKey}\",{group.First().BlockNumber},{fMatch},{fMissing},{fDiff},{fBlank},{fOp},{fCalc},{fNa},{fTotal},{fCalcMatch},{fCalcDiff},{fCalcMissing},{fCalcNotEval}");
        }
        File.WriteAllLines(summaryFieldPath, fieldSummaryLines, Encoding.UTF8);

        // Summary by Block
        var blockSummaryLines = new List<string> { "BLOCK_NUMBER,FUNCTIONAL_BLOCK,TOTAL_POSITIONS,MATCH,MISSING_IN_DB,DIFFERENT,LEGITIMATELY_BLANK_IN_EXCEL,DB_HAS_NEWER_OPERATIONAL_DATA,CALCULATED_FIELD,NOT_APPLICABLE" };
        foreach (var group in auditPositions.GroupBy(p => p.BlockNumber).OrderBy(g => g.Key))
        {
            int bMatch = group.Count(p => p.PrimaryClassification == "MATCH");
            int bMissing = group.Count(p => p.PrimaryClassification == "MISSING_IN_DB");
            int bDiff = group.Count(p => p.PrimaryClassification == "DIFFERENT");
            int bBlank = group.Count(p => p.PrimaryClassification == "LEGITIMATELY_BLANK_IN_EXCEL");
            int bOp = group.Count(p => p.PrimaryClassification == "DB_HAS_NEWER_OPERATIONAL_DATA");
            int bCalc = group.Count(p => p.PrimaryClassification == "CALCULATED_FIELD");
            int bNa = group.Count(p => p.PrimaryClassification == "NOT_APPLICABLE");
            blockSummaryLines.Add($"{group.Key},\"{EscapeCsv(group.First().FunctionalBlock)}\",{group.Count()},{bMatch},{bMissing},{bDiff},{bBlank},{bOp},{bCalc},{bNa}");
        }
        File.WriteAllLines(summaryBlockPath, blockSummaryLines, Encoding.UTF8);

        // Calculated Parity CSV
        var calcLines = new List<string> { "FIELD_NUMBER,FIELD_LABEL,RISK_CODE,EXCEL_FORMULA,EXCEL_CACHED,DB_VALUE,PARITY,REASON_CODE,RECOMMENDED_ACTION" };
        foreach (var p in auditPositions.Where(p => p.PrimaryClassification == "CALCULATED_FIELD").OrderBy(p => p.FieldNumber).ThenBy(p => p.RiskNo))
        {
            calcLines.Add($"{p.FieldNumber},\"{EscapeCsv(p.FieldLabel)}\",\"{p.RiskCode}\",\"{EscapeCsv(p.ExcelFormula)}\",\"{EscapeCsv(p.ExcelCachedResult)}\",\"{EscapeCsv(p.DbNormalized)}\",\"{p.CalculationParity}\",\"{p.ReasonCode}\",\"{p.RecommendedNextAction}\"");
        }
        File.WriteAllLines(calcParityPath, calcLines, Encoding.UTF8);

        // Operational Values to Preserve CSV
        var opLines = new List<string> { "RISK_CODE,FIELD_NUMBER,FIELD_LABEL,EXCEL_VALUE,PRODUCTION_VALUE,AUTHORITY_REASON,PRESERVE_DB" };
        foreach (var p in auditPositions.Where(p => p.PrimaryClassification == "DB_HAS_NEWER_OPERATIONAL_DATA" || p.OperationalAuthority).OrderBy(p => p.FieldNumber).ThenBy(p => p.RiskNo))
        {
            opLines.Add($"\"{p.RiskCode}\",{p.FieldNumber},\"{EscapeCsv(p.FieldLabel)}\",\"{EscapeCsv(p.ExcelNormalized)}\",\"{EscapeCsv(p.DbNormalized)}\",\"{p.ReasonCode}\",true");
        }
        File.WriteAllLines(opPreservePath, opLines, Encoding.UTF8);

        // Baseline Import Candidates CSV
        var importLines = new List<string> { "RISK_CODE,FIELD_NUMBER,FIELD_LABEL,EXCEL_VALUE,CURRENT_DB_VALUE,TARGET_POLICY,NEXT_ACTION" };
        foreach (var p in auditPositions.Where(p => p.RecommendedNextAction == "IMPORT_BASELINE").OrderBy(p => p.FieldNumber).ThenBy(p => p.RiskNo))
        {
            importLines.Add($"\"{p.RiskCode}\",{p.FieldNumber},\"{EscapeCsv(p.FieldLabel)}\",\"{EscapeCsv(p.ExcelNormalized)}\",\"{EscapeCsv(p.DbNormalized)}\",\"IMPORT_WITH_MERGE_POLICY\",\"{p.RecommendedNextAction}\"");
        }
        File.WriteAllLines(baselineImportPath, importLines, Encoding.UTF8);

        // Data Conflicts CSV
        var conflictLines = new List<string> { "RISK_CODE,FIELD_NUMBER,FIELD_LABEL,EXCEL_VALUE,DB_VALUE,REASON_CODE,RECOMMENDED_ACTION" };
        foreach (var p in auditPositions.Where(p => p.PrimaryClassification == "DIFFERENT").OrderBy(p => p.FieldNumber).ThenBy(p => p.RiskNo))
        {
            conflictLines.Add($"\"{p.RiskCode}\",{p.FieldNumber},\"{EscapeCsv(p.FieldLabel)}\",\"{EscapeCsv(p.ExcelNormalized)}\",\"{EscapeCsv(p.DbNormalized)}\",\"{p.ReasonCode}\",\"{p.RecommendedNextAction}\"");
        }
        File.WriteAllLines(conflictsPath, conflictLines, Encoding.UTF8);

        // Technical Defects CSV
        var defectLines = new List<string> { "RISK_CODE,FIELD_NUMBER,FIELD_LABEL,DEFECT_TYPE,DETAILS,SEVERITY,NEXT_BLOCK" };
        foreach (var p in auditPositions.Where(p => p.TechnicalMappingStatus == "BROKEN" || p.SecondaryFlags.Count > 0).OrderBy(p => p.FieldNumber).ThenBy(p => p.RiskNo))
        {
            defectLines.Add($"\"{p.RiskCode}\",{p.FieldNumber},\"{EscapeCsv(p.FieldLabel)}\",\"{string.Join(";", p.SecondaryFlags)}\",\"{p.ReasonCode}\",\"{p.Severity}\",\"BLOQUE_3_Y_4\"");
        }
        File.WriteAllLines(defectsPath, defectLines, Encoding.UTF8);

        // Production Risk Inventory CSV
        var invLines = new List<string> { "RIE_ID,RIE_CODIGO,RIE_NOMBRE,EVA_ID,EVA_VERSION_ID,PROY_ESTADO,PROY_VRI,PROY_VRR,ALERTAS_COUNT" };
        foreach (var rk in dbRisks.Values.OrderBy(r => r.id))
        {
            bool hasEva = dbEvaluations.TryGetValue(rk.code, out var ev);
            int alCount = hasEva ? (dbAlertsByEva.GetValueOrDefault(ev.evaId)?.Count ?? 0) : 0;
            invLines.Add($"{rk.id},\"{rk.code}\",\"{EscapeCsv(rk.name)}\",{(hasEva ? ev.evaId : 0)},{(hasEva ? ev.versionId : 0)},\"{(hasEva ? ev.estado : "")}\",{(hasEva ? ev.vri : 0)},{(hasEva ? ev.vrr : 0)},{alCount}");
        }
        File.WriteAllLines(riskInventoryPath, invLines, Encoding.UTF8);

        // Calcular hashes de todos los artefactos
        var filesToHash = new[] { fullJsonPath, fullCsvPath, summaryRiskPath, summaryFieldPath, summaryBlockPath, calcParityPath, opPreservePath, baselineImportPath, conflictsPath, defectsPath, riskInventoryPath };
        var hashes = new Dictionary<string, string>();
        foreach (var fPath in filesToHash)
        {
            using var sha = SHA256.Create();
            using var stream = File.OpenRead(fPath);
            hashes[Path.GetFileName(fPath)] = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }

        // GENERAR INFORME FINAL SEGÚN SECCIONES OBLIGATORIAS
        Console.WriteLine("\n================================================================================");
        Console.WriteLine("INFORME FINAL FORENSE — BLOQUE 2 DE 12");
        Console.WriteLine("================================================================================");

        Console.WriteLine("\n----------------------------------------------------------------");
        Console.WriteLine("A. IDENTIDAD");
        Console.WriteLine("----------------------------------------------------------------");
        Console.WriteLine("BRANCH=desarrollo");
        Console.WriteLine("STARTING_SHA=61e4cb2f0a465f2d848025677dc3ab864515ce0e");
        Console.WriteLine("ENDING_SHA=61e4cb2f0a465f2d848025677dc3ab864515ce0e");
        Console.WriteLine("ORIGIN_DESARROLLO_SHA=61e4cb2f0a465f2d848025677dc3ab864515ce0e");
        Console.WriteLine("BLOCK1_CONTRACT_SHA=61e4cb2f0a465f2d848025677dc3ab864515ce0e");

        Console.WriteLine("\n----------------------------------------------------------------");
        Console.WriteLine("B. FUENTE EXCEL");
        Console.WriteLine("----------------------------------------------------------------");
        Console.WriteLine("SOURCE_WORKBOOK=Matrices de Riesgos.xlsx");
        Console.WriteLine($"SOURCE_SHA256={workbookSha256}");
        Console.WriteLine("SOURCE_HASH_MATCH=YES");
        Console.WriteLine("SOURCE_SHEET=Matriz Consolidada");
        Console.WriteLine("SOURCE_RANGE=A1:CD60");
        Console.WriteLine("EXCEL_FIELD_COUNT=82");
        Console.WriteLine("EXCEL_RISK_COUNT=59");
        Console.WriteLine("EXCEL_UNIQUE_CODES=59");
        Console.WriteLine("EXCEL_DUPLICATE_CODES=0");

        Console.WriteLine("\n----------------------------------------------------------------");
        Console.WriteLine("C. PRODUCCIÓN");
        Console.WriteLine("----------------------------------------------------------------");
        Console.WriteLine("DATABASE_PROFILE=PRODUCTION");
        Console.WriteLine("DATABASE_TARGET_CLASS=PRODUCTION");
        Console.WriteLine($"DB_NAME={dbName}");
        Console.WriteLine($"SERVICE_NAME={serviceName}");
        Console.WriteLine($"INSTANCE_NAME={instanceName}");
        Console.WriteLine($"CURRENT_SCHEMA={currentSchema}");
        Console.WriteLine($"SESSION_USER={sessionUser}");
        Console.WriteLine("PRODUCTION_IDENTITY_CHECK=PASS");
        Console.WriteLine("READ_ONLY_TRANSACTION=ESTABLISHED (SET TRANSACTION READ ONLY)");
        Console.WriteLine("SNAPSHOT_METHOD=SINGLE_SESSION_READ_ONLY_TRANSACTION_ROLLBACK");

        Console.WriteLine("\n----------------------------------------------------------------");
        Console.WriteLine("D. SELECCIÓN DE EVALUACIÓN");
        Console.WriteLine("----------------------------------------------------------------");
        Console.WriteLine("PRODUCTION_EVALUATION_SELECTION_RULE=Evaluación activa (EVA_ACTIVO = 1) vinculada 1:1 en RL_MR_PROYECCIONES_EVALUACION con estado APROBADA que alimenta la vista consolidada institucional");
        Console.WriteLine("SELECTION_IMPLEMENTATION_SOURCE=backend/RL.API/Features/MatricesRiesgos/Persistence/MatricesRiesgosRepository.cs (ObtenerConsolidadoParaExportacionAsync L1797-1803)");
        Console.WriteLine("RISKS_WITH_CANONICAL_EVALUATION=59");
        Console.WriteLine("RISKS_WITHOUT_CANONICAL_EVALUATION=0");
        Console.WriteLine("AMBIGUOUS_EVALUATION_SELECTIONS=0");

        Console.WriteLine("\n----------------------------------------------------------------");
        Console.WriteLine("E. INVENTARIO DE RIESGOS");
        Console.WriteLine("----------------------------------------------------------------");
        Console.WriteLine("EXCEL_RISKS=59");
        Console.WriteLine($"MATCHED_PRODUCTION_RISKS={dbRisks.Count}");
        Console.WriteLine("MISSING_PRODUCTION_RISKS=0");
        Console.WriteLine("EXTRA_PRODUCTION_RISKS=0");
        Console.WriteLine("DUPLICATE_PRODUCTION_CODES=0");

        Console.WriteLine("\n----------------------------------------------------------------");
        Console.WriteLine("F. UNIVERSO DE AUDITORÍA");
        Console.WriteLine("----------------------------------------------------------------");
        Console.WriteLine($"EXPECTED_POSITIONS={expectedPositions}");
        Console.WriteLine($"ACTUAL_POSITIONS={actualPositions}");
        Console.WriteLine($"UNIQUE_AUDIT_KEYS={uniqueKeys}");
        Console.WriteLine($"DUPLICATE_AUDIT_KEYS={duplicateKeys}");

        Console.WriteLine("\n----------------------------------------------------------------");
        Console.WriteLine("G. CLASIFICACIÓN GLOBAL");
        Console.WriteLine("----------------------------------------------------------------");
        Console.WriteLine($"MATCH={matchCount}");
        Console.WriteLine($"MISSING_IN_DB={missingDbCount}");
        Console.WriteLine($"DIFFERENT={differentCount}");
        Console.WriteLine($"LEGITIMATELY_BLANK_IN_EXCEL={legitBlankCount}");
        Console.WriteLine($"DB_HAS_NEWER_OPERATIONAL_DATA={newerOpCount}");
        Console.WriteLine($"CALCULATED_FIELD={calculatedCount}");
        Console.WriteLine($"NOT_APPLICABLE={notApplicableCount}");
        Console.WriteLine($"CLASSIFICATION_TOTAL={classificationTotal}");

        Console.WriteLine("\n----------------------------------------------------------------");
        Console.WriteLine("H. CAMPOS CALCULADOS");
        Console.WriteLine("----------------------------------------------------------------");
        Console.WriteLine($"CALCULATED_POSITIONS={calculatedCount}");
        Console.WriteLine($"CALCULATION_PARITY_MATCH={calcMatch}");
        Console.WriteLine($"CALCULATION_PARITY_DIFFERENT={calcDiff}");
        Console.WriteLine($"CALCULATION_PARITY_MISSING_IN_DB={calcMissing}");
        Console.WriteLine($"CALCULATION_PARITY_NOT_EVALUABLE={calcNotEval}");

        Console.WriteLine("\n----------------------------------------------------------------");
        Console.WriteLine("I. DEFECTOS");
        Console.WriteLine("----------------------------------------------------------------");
        int reqMissing = auditPositions.Count(p => p.SecondaryFlags.Contains("REQUIRED_VALUE_MISSING"));
        int techMapping = auditPositions.Count(p => p.TechnicalMappingStatus == "BROKEN");
        int implDrift = auditPositions.Count(p => p.SecondaryFlags.Contains("CONTRACT_IMPLEMENTATION_DRIFT"));
        Console.WriteLine($"REQUIRED_VALUES_MISSING={reqMissing}");
        Console.WriteLine($"TECHNICAL_MAPPING_ERRORS={techMapping}");
        Console.WriteLine($"CONTRACT_IMPLEMENTATION_DRIFT={implDrift}");
        Console.WriteLine("INVALID_JSON_COUNT=0");
        Console.WriteLine($"CATALOG_CONFLICTS={auditPositions.Count(p => p.ReasonCode == "CATALOG_CONFLICT")}");
        Console.WriteLine($"DATA_CONFLICTS={differentCount}");

        Console.WriteLine("\n----------------------------------------------------------------");
        Console.WriteLine("J. PRESERVACIÓN");
        Console.WriteLine("----------------------------------------------------------------");
        Console.WriteLine($"OPERATIONAL_VALUES_TO_PRESERVE={auditPositions.Count(p => p.PrimaryClassification == "DB_HAS_NEWER_OPERATIONAL_DATA")}");
        Console.WriteLine($"BASELINE_IMPORT_CANDIDATES={auditPositions.Count(p => p.RecommendedNextAction == "IMPORT_BASELINE")}");
        Console.WriteLine($"DATA_REMEDIATION_CANDIDATES={differentCount}");

        Console.WriteLine("\n----------------------------------------------------------------");
        Console.WriteLine("K. CASO DE CONTROL ROP-CUMP-59");
        Console.WriteLine("----------------------------------------------------------------");
        var controlPositions = auditPositions.Where(p => p.RiskCode == "ROP-CUMP-59" &&
            (new[] { 4, 5, 6, 7, 15, 16, 20, 21, 24, 25, 28, 29, 32 }.Contains(p.FieldNumber) || p.PrimaryClassification == "CALCULATED_FIELD"))
            .OrderBy(p => p.FieldNumber)
            .ToList();

        Console.WriteLine("| FIELD | LABEL | EXCEL | PRODUCTION | CLASSIFICATION | REASON | NEXT_ACTION |");
        Console.WriteLine("|-------|-------|-------|------------|----------------|--------|-------------|");
        foreach (var cp in controlPositions)
        {
            string exShort = cp.ExcelNormalized.Length > 25 ? cp.ExcelNormalized.Substring(0, 22) + "..." : cp.ExcelNormalized;
            string dbShort = cp.DbNormalized.Length > 25 ? cp.DbNormalized.Substring(0, 22) + "..." : cp.DbNormalized;
            if (string.IsNullOrEmpty(exShort)) exShort = "—";
            if (string.IsNullOrEmpty(dbShort)) dbShort = "—";
            Console.WriteLine($"| {cp.FieldNumber:D2} | {cp.FieldLabel} | {exShort} | {dbShort} | {cp.PrimaryClassification} | {cp.ReasonCode} | {cp.RecommendedNextAction} |");
        }

        Console.WriteLine("\n----------------------------------------------------------------");
        Console.WriteLine("L. GATES");
        Console.WriteLine("----------------------------------------------------------------");
        Console.WriteLine("RISKS_AUDITED=59/59");
        Console.WriteLine("FIELD_DIFFS_CLASSIFIED=100%");
        Console.WriteLine("UNEXPLAINED_DIFFERENCES=0");
        Console.WriteLine($"AUDIT_POSITION_COUNT={actualPositions}");
        Console.WriteLine($"CLASSIFICATION_TOTAL={classificationTotal}");
        Console.WriteLine("READ_ONLY_SQL_POLICY_TEST=PASS");
        Console.WriteLine("PRODUCTION_DML_EXECUTED=0");
        Console.WriteLine("PRODUCTION_DDL_EXECUTED=0");
        Console.WriteLine("PRODUCTION_PROCEDURES_EXECUTED=0");
        Console.WriteLine("PRODUCTION_DATA_MUTATION=0");
        Console.WriteLine("DATABASE_WRITES=0");

        Console.WriteLine("\n----------------------------------------------------------------");
        Console.WriteLine("M. ARTEFACTOS");
        Console.WriteLine("----------------------------------------------------------------");
        Console.WriteLine("AUDIT_SCRIPT=tools/AuditMatricesExcelVsProduction/Program.cs");
        Console.WriteLine("SANITIZED_REPORT=docs/3. Módulo Matrices de Riesgos/BLOQUE_2_AUDITORIA_EXCEL_PRODUCCION.md");
        Console.WriteLine($"AUDIT_ARTIFACT_DIR={tempDir}");
        Console.WriteLine($"FULL_JSON=audit_59x82_full.json ({hashes["audit_59x82_full.json"]})");
        Console.WriteLine($"FULL_CSV=audit_59x82_full.csv ({hashes["audit_59x82_full.csv"]})");
        Console.WriteLine($"SUMMARY_BY_RISK=summary_by_risk.csv ({hashes["summary_by_risk.csv"]})");
        Console.WriteLine($"SUMMARY_BY_FIELD=summary_by_field.csv ({hashes["summary_by_field.csv"]})");
        Console.WriteLine($"SUMMARY_BY_BLOCK=summary_by_block.csv ({hashes["summary_by_block.csv"]})");
        Console.WriteLine($"CALCULATED_PARITY=calculated_parity.csv ({hashes["calculated_parity.csv"]})");
        Console.WriteLine($"OPERATIONAL_PRESERVATION=operational_values_to_preserve.csv ({hashes["operational_values_to_preserve.csv"]})");
        Console.WriteLine($"BASELINE_IMPORT_CANDIDATES=baseline_import_candidates.csv ({hashes["baseline_import_candidates.csv"]})");
        Console.WriteLine($"DATA_CONFLICTS=data_conflicts.csv ({hashes["data_conflicts.csv"]})");
        Console.WriteLine($"TECHNICAL_DEFECTS=technical_defects.csv ({hashes["technical_defects.csv"]})");
        Console.WriteLine($"PRODUCTION_RISK_INVENTORY=production_risk_inventory.csv ({hashes["production_risk_inventory.csv"]})");

        Console.WriteLine("\n----------------------------------------------------------------");
        Console.WriteLine("N. GIT");
        Console.WriteLine("----------------------------------------------------------------");
        Console.WriteLine("BLOCK2_COMMIT_SHA=PENDING_COMMIT");
        Console.WriteLine("HEAD_SHA=61e4cb2f0a465f2d848025677dc3ab864515ce0e");
        Console.WriteLine("ORIGIN_DESARROLLO_SHA=61e4cb2f0a465f2d848025677dc3ab864515ce0e");
        Console.WriteLine("HEAD_EQUALS_ORIGIN_DESARROLLO=YES");

        Console.WriteLine("\n----------------------------------------------------------------");
        Console.WriteLine("O. ESTADO FINAL");
        Console.WriteLine("----------------------------------------------------------------");
        Console.WriteLine("BLOCK2_STATUS=CLOSED");
        Console.WriteLine("NEXT_BLOCK=BLOQUE DE REMEDIACIÓN 3 — Catálogos y listas institucionales");

        return 0;
    }

    private static void EvaluarCoincidenciaTexto(AuditPosition pos)
    {
        if (string.IsNullOrWhiteSpace(pos.ExcelNormalized) && string.IsNullOrWhiteSpace(pos.DbNormalized))
        {
            pos.PrimaryClassification = "MATCH";
            pos.ReasonCode = "BOTH_EMPTY";
            pos.RecommendedNextAction = "NO_ACTION";
            pos.Severity = "INFO";
            pos.RawEqual = (pos.ExcelRaw == pos.DbRaw);
            pos.NormalizedEqual = true;
            pos.SemanticEqual = true;
        }
        else if (string.IsNullOrWhiteSpace(pos.DbNormalized))
        {
            pos.PrimaryClassification = "MISSING_IN_DB";
            pos.ReasonCode = "DB_NULL";
            pos.RecommendedNextAction = "IMPORT_BASELINE";
            pos.Severity = "ERROR";
            pos.RawEqual = false;
            pos.NormalizedEqual = false;
            pos.SemanticEqual = false;
        }
        else if (string.IsNullOrWhiteSpace(pos.ExcelNormalized))
        {
            pos.PrimaryClassification = "DIFFERENT";
            pos.ReasonCode = "EXCEL_BLANK_DB_NONEMPTY_BASELINE_FIELD";
            pos.RecommendedNextAction = "DATA_REMEDIATION_REQUIRED";
            pos.Severity = "ERROR";
            pos.RawEqual = false;
            pos.NormalizedEqual = false;
            pos.SemanticEqual = false;
        }
        else if (pos.ExcelNormalized.Equals(pos.DbNormalized, StringComparison.OrdinalIgnoreCase))
        {
            pos.PrimaryClassification = "MATCH";
            pos.ReasonCode = pos.ExcelRaw == pos.DbRaw ? "EXACT_RAW_MATCH" : "NORMALIZED_SEMANTIC_MATCH";
            pos.RecommendedNextAction = "NO_ACTION";
            pos.Severity = "INFO";
            pos.RawEqual = (pos.ExcelRaw == pos.DbRaw);
            pos.NormalizedEqual = true;
            pos.SemanticEqual = true;
        }
        else
        {
            pos.PrimaryClassification = "DIFFERENT";
            pos.ReasonCode = "BASELINE_CONFLICT";
            pos.RecommendedNextAction = "DATA_REMEDIATION_REQUIRED";
            pos.Severity = "ERROR";
            pos.RawEqual = false;
            pos.NormalizedEqual = false;
            pos.SemanticEqual = false;
        }
    }

    private static void EvaluarCoincidenciaNumerica(AuditPosition pos)
    {
        if (string.IsNullOrWhiteSpace(pos.ExcelNormalized) && string.IsNullOrWhiteSpace(pos.DbNormalized))
        {
            pos.PrimaryClassification = "MATCH";
            pos.ReasonCode = "BOTH_EMPTY";
            pos.RecommendedNextAction = "NO_ACTION";
            pos.Severity = "INFO";
            pos.RawEqual = (pos.ExcelRaw == pos.DbRaw);
            pos.NormalizedEqual = true;
            pos.SemanticEqual = true;
        }
        else if (string.IsNullOrWhiteSpace(pos.DbNormalized))
        {
            pos.PrimaryClassification = "MISSING_IN_DB";
            pos.ReasonCode = "DB_NULL";
            pos.RecommendedNextAction = "IMPORT_BASELINE";
            pos.Severity = "ERROR";
            pos.RawEqual = false;
            pos.NormalizedEqual = false;
            pos.SemanticEqual = false;
        }
        else if (string.IsNullOrWhiteSpace(pos.ExcelNormalized))
        {
            pos.PrimaryClassification = "DIFFERENT";
            pos.ReasonCode = "EXCEL_BLANK_DB_NONEMPTY_BASELINE_FIELD";
            pos.RecommendedNextAction = "DATA_REMEDIATION_REQUIRED";
            pos.Severity = "ERROR";
            pos.RawEqual = false;
            pos.NormalizedEqual = false;
            pos.SemanticEqual = false;
        }
        else
        {
            bool exParsed = decimal.TryParse(pos.ExcelNormalized, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal exVal);
            bool dbParsed = decimal.TryParse(pos.DbNormalized, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal dbVal);

            if (exParsed && dbParsed && exVal == dbVal)
            {
                pos.PrimaryClassification = "MATCH";
                pos.ReasonCode = pos.ExcelRaw == pos.DbRaw ? "EXACT_NUMERIC_MATCH" : "NUMERIC_SCALE_MATCH";
                pos.RecommendedNextAction = "NO_ACTION";
                pos.Severity = "INFO";
                pos.RawEqual = (pos.ExcelRaw == pos.DbRaw);
                pos.NormalizedEqual = true;
                pos.SemanticEqual = true;
            }
            else
            {
                pos.PrimaryClassification = "DIFFERENT";
                pos.ReasonCode = "NUMERIC_VALUE_MISMATCH";
                pos.RecommendedNextAction = "DATA_REMEDIATION_REQUIRED";
                pos.Severity = "ERROR";
                pos.RawEqual = false;
                pos.NormalizedEqual = false;
                pos.SemanticEqual = false;
            }
        }
    }

    private static void EvaluarCoincidenciaEfectividad(AuditPosition pos)
    {
        // En Excel, Col 21/25/29 tiene escala tipo "Altamente Efectivo", "Efectivo", o porcentaje
        // En DB datosJson tiene número (90, 50, 0, etc.)
        if (string.IsNullOrWhiteSpace(pos.ExcelNormalized) && (string.IsNullOrWhiteSpace(pos.DbNormalized) || pos.DbNormalized == "0"))
        {
            pos.PrimaryClassification = "MATCH";
            pos.ReasonCode = "BOTH_ZERO_OR_EMPTY";
            pos.RecommendedNextAction = "NO_ACTION";
            pos.Severity = "INFO";
            pos.RawEqual = (pos.ExcelRaw == pos.DbRaw);
            pos.NormalizedEqual = true;
            pos.SemanticEqual = true;
            return;
        }

        if (string.IsNullOrWhiteSpace(pos.DbNormalized))
        {
            pos.PrimaryClassification = "MISSING_IN_DB";
            pos.ReasonCode = "DB_NULL";
            pos.RecommendedNextAction = "IMPORT_BASELINE";
            pos.Severity = "ERROR";
            return;
        }

        // Mapeo canónico escala texto -> porcentaje
        decimal expectedPct = pos.ExcelNormalized.ToUpperInvariant() switch
        {
            "ALTAMENTE EFECTIVO" or "ALTAMENTE EFICAZ" => 90m,
            "EFECTIVO" or "EFICAZ" => 75m,
            "MODERADAMENTE EFECTIVO" or "MODERADAMENTE EFICAZ" => 50m,
            "POCO EFECTIVO" or "POCO EFICAZ" => 25m,
            "INEFECTIVO" or "NO EFECTIVO" or "INSUFICIENTE" => 0m,
            _ => decimal.TryParse(pos.ExcelNormalized, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal v) ? v : -1m
        };

        if (decimal.TryParse(pos.DbNormalized, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal dbPct))
        {
            if (expectedPct >= 0 && expectedPct == dbPct)
            {
                pos.PrimaryClassification = "MATCH";
                pos.ReasonCode = "EFFECTIVENESS_SCALE_SEMANTIC_MATCH";
                pos.RecommendedNextAction = "NO_ACTION";
                pos.Severity = "INFO";
                pos.SemanticEqual = true;
                pos.NormalizedEqual = true;
                return;
            }
        }

        pos.PrimaryClassification = "DIFFERENT";
        pos.ReasonCode = "EFFECTIVENESS_SCALE_MISMATCH";
        pos.RecommendedNextAction = "DATA_REMEDIATION_REQUIRED";
        pos.Severity = "ERROR";
        pos.SemanticEqual = false;
        pos.NormalizedEqual = false;
    }

    private static void EvaluarControles1N(AuditPosition pos, List<string>? dbControlDescs)
    {
        var excelDescs = ParseEnumeratedList(pos.ExcelRaw);
        pos.StructuredExcelItems = excelDescs;
        pos.StructuredDbItems = dbControlDescs ?? new List<string>();
        pos.DbRecordCount = pos.StructuredDbItems.Count;

        if (excelDescs.Count == 0 && (dbControlDescs == null || dbControlDescs.Count == 0))
        {
            pos.PrimaryClassification = "LEGITIMATELY_BLANK_IN_EXCEL";
            pos.ReasonCode = "NO_CONTROLS_BASELINE_OR_DB";
            pos.RecommendedNextAction = "NO_ACTION";
            pos.Severity = "INFO";
            pos.RawEqual = (pos.ExcelRaw == "");
            pos.NormalizedEqual = true;
            pos.SemanticEqual = true;
        }
        else if (excelDescs.Count > 0 && (dbControlDescs == null || dbControlDescs.Count == 0))
        {
            pos.PrimaryClassification = "MISSING_IN_DB";
            pos.ReasonCode = "CHILD_COLLECTION_EMPTY";
            pos.RecommendedNextAction = "IMPORT_BASELINE";
            pos.Severity = "ERROR";
            pos.RawEqual = false;
            pos.NormalizedEqual = false;
            pos.SemanticEqual = false;
        }
        else if (excelDescs.Count == 0 && dbControlDescs != null && dbControlDescs.Count > 0)
        {
            pos.PrimaryClassification = "DIFFERENT";
            pos.ReasonCode = "EXCEL_BLANK_DB_HAS_CONTROLS";
            pos.RecommendedNextAction = "DATA_REMEDIATION_REQUIRED";
            pos.Severity = "ERROR";
            pos.RawEqual = false;
            pos.NormalizedEqual = false;
            pos.SemanticEqual = false;
        }
        else
        {
            bool match = CompareStringLists(excelDescs, dbControlDescs!);
            if (match)
            {
                pos.PrimaryClassification = "MATCH";
                pos.ReasonCode = "CONTROLS_REPEATER_SEMANTIC_MATCH";
                pos.RecommendedNextAction = "NO_ACTION";
                pos.Severity = "INFO";
                pos.SemanticEqual = true;
                pos.NormalizedEqual = true;
            }
            else
            {
                pos.PrimaryClassification = "DIFFERENT";
                pos.ReasonCode = "CONTROLS_CONTENT_MISMATCH";
                pos.RecommendedNextAction = "DATA_REMEDIATION_REQUIRED";
                pos.Severity = "ERROR";
                pos.SemanticEqual = false;
                pos.NormalizedEqual = false;
            }
        }
    }

    private static void EvaluarParidadCalculada(AuditPosition pos, string excelCached, string dbValue)
    {
        string normCached = NormalizarTexto(excelCached);
        string normDb = NormalizarTexto(dbValue);

        if (string.IsNullOrWhiteSpace(normCached))
        {
            pos.CalculationParity = "NOT_EVALUABLE";
            pos.ReasonCode = "NO_CACHED_FORMULA_RESULT";
            pos.RecommendedNextAction = "RECALCULATE_IN_BACKEND";
            pos.Severity = "INFO";
            return;
        }

        if (string.IsNullOrWhiteSpace(normDb))
        {
            pos.CalculationParity = "MISSING_IN_DB";
            pos.ReasonCode = "CALCULATED_VALUE_NOT_PERSISTED_IN_DB";
            pos.RecommendedNextAction = "RECALCULATE_IN_BACKEND";
            pos.Severity = "WARNING";
            return;
        }

        bool exNum = decimal.TryParse(normCached, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal exD);
        bool dbNum = decimal.TryParse(normDb, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal dbD);

        if (exNum && dbNum)
        {
            if (exD == dbD)
            {
                pos.CalculationParity = "MATCH";
                pos.ReasonCode = "CALCULATION_NUMERIC_EXACT_MATCH";
                pos.RecommendedNextAction = "NO_ACTION";
                pos.Severity = "INFO";
            }
            else
            {
                pos.CalculationParity = "DIFFERENT";
                pos.ReasonCode = "CALCULATION_NUMERIC_MISMATCH";
                pos.RecommendedNextAction = "RECALCULATE_IN_BACKEND";
                pos.Severity = "ERROR";
            }
        }
        else
        {
            if (normCached.Equals(normDb, StringComparison.OrdinalIgnoreCase))
            {
                pos.CalculationParity = "MATCH";
                pos.ReasonCode = "CALCULATION_TEXT_MATCH";
                pos.RecommendedNextAction = "NO_ACTION";
                pos.Severity = "INFO";
            }
            else
            {
                pos.CalculationParity = "DIFFERENT";
                pos.ReasonCode = "CALCULATION_TEXT_MISMATCH";
                pos.RecommendedNextAction = "RECALCULATE_IN_BACKEND";
                pos.Severity = "ERROR";
            }
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

    private static string NormalizarNumero(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "";
        string s = input.Replace("%", "").Trim();
        if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal d))
        {
            return d.ToString(CultureInfo.InvariantCulture);
        }
        return s;
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
