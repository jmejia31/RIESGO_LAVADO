using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RL.Tools.ReconcileMatricesBaseline;

public sealed record BusinessInputRequestRow(
    string RequestKey, string RiskCode, int RiskNo, string Area, int FieldNumber,
    string FieldLabel, string EntityType, int EntityOrdinal, int? ParentOrdinal,
    string EntityDescription, string Attribute, string DbColumn, string MissingInformation,
    string AcceptedValuesIfCatalog, string WhyRequired, string DependencyRequestKey,
    string BusinessOwner = "", string BusinessResponse = "", string BusinessNotes = "");

public sealed record BusinessQualificationRow(
    string QualificationKey, string RiskCode, int FieldNumber, string EntityType,
    int EntityOrdinal, int? ParentOrdinal, string SourceRaw, string SourceParsed,
    string DbMatch, string Attribute, string DbColumn, bool Required, string DbDefault,
    string WorkbookSource, string SystemRule, string SystemRuleEvidence,
    string CurrentDbSource, bool HumanInputRequired, string Status, string Reason);

public sealed record BusinessParsingAuditRow(
    string RiskCode, int RiskNo, int FieldNumber, string FieldLabel, string RawCell,
    int ParsedCount, int ItemOrdinal, int? ParentOrdinal, string ParsedItemText,
    string ParseRule, bool Ambiguous, string Evidence);

public sealed record BusinessCreationRuleEvidenceRow(
    string RuleId, string ExpectedValue, string SourceType, string SourceFileOrDdl,
    string SourceSymbol, bool Verified, string Notes);

public sealed record BusinessBlockerDependencyRow(
    string BlockerId, string EntityType, string EntityKey, string DependsOnBlockerId,
    string RootCause, string MissingAttribute, bool SourceAvailable,
    bool HumanInputRequired);

public sealed record BusinessField45ReconciliationRow(
    string RiskCode, int RiskNo, string SourceRaw, int ParsedResponsibleCount,
    int SourcePlanCount, int ExistingPlanCount, string Status, string Detail);

public sealed class BusinessCreationRules
{
    public string ControlState { get; init; } = "";
    public string AutomationWhenF32Empty { get; init; } = "";
    public string PlanState { get; init; } = "";
    public string ActivityState { get; init; } = "";
    public bool PlanAdvanceDefaultVerified { get; set; }
    public bool PlanBudgetDefaultVerified { get; set; }
    public bool ActivityAdvanceDefaultVerified { get; set; }
    public bool PlanAdvanceCreateFlowVerified { get; init; }
    public bool PlanBudgetCreateFlowVerified { get; init; }
    public bool ActivityAdvanceCreateFlowVerified { get; init; }
    public List<BusinessCreationRuleEvidenceRow> Evidence { get; set; } = [];

    public bool AllVerified => ControlState.Length > 0 && AutomationWhenF32Empty.Length > 0 &&
        PlanState.Length > 0 && ActivityState.Length > 0 &&
        (PlanAdvanceDefaultVerified || PlanAdvanceCreateFlowVerified) &&
        (PlanBudgetDefaultVerified || PlanBudgetCreateFlowVerified) &&
        (ActivityAdvanceDefaultVerified || ActivityAdvanceCreateFlowVerified) &&
        Evidence.Where(x => !x.RuleId.StartsWith("DB_DEFAULT_", StringComparison.Ordinal)).All(x => x.Verified);
}

public sealed class BusinessInputQualificationResult
{
    public string RunId { get; init; } = "";
    public string GeneratorGitSha { get; init; } = "";
    public string WorkbookSha256 { get; init; } = "";
    public int HeaderCount { get; init; }
    public int RiskCount { get; init; }
    public int UniqueRiskCodeCount { get; init; }
    public int RisksMatched { get; init; }
    public int EvaluationsMatched { get; init; }
    public int ProjectionsMatched { get; init; }
    public int ControlSourceCells { get; init; }
    public int ControlSourceItems { get; init; }
    public int PreventiveItems { get; init; }
    public int DetectiveItems { get; init; }
    public int CorrectiveItems { get; init; }
    public int PlanSourceCells { get; init; }
    public int PlanSourceItems { get; init; }
    public int ActivitySourceCells { get; init; }
    public int ActivitySourceItems { get; init; }
    public int ParserAmbiguities { get; init; }
    public int HumanInputEntities { get; init; }
    public int HumanInputAttributes => RequestRows.Count;
    public int UnexplainedQualificationRows { get; init; }
    public int EmptyRequestKeys { get; init; }
    public int DuplicateRequestKeys { get; init; }
    public int NonemptyBusinessResponses { get; init; }
    public string DatabaseName { get; init; } = "";
    public string ServiceName { get; init; } = "";
    public string InstanceName { get; init; } = "";
    public string CurrentSchema { get; init; } = "";
    public string SessionUser { get; init; } = "";
    public List<BusinessInputRequestRow> RequestRows { get; init; } = [];
    public List<BusinessQualificationRow> QualificationRows { get; init; } = [];
    public List<BusinessParsingAuditRow> ControlParsingAudit { get; init; } = [];
    public List<BusinessParsingAuditRow> PlanParsingAudit { get; init; } = [];
    public List<BusinessParsingAuditRow> ActivityParsingAudit { get; init; } = [];
    public List<BusinessQualificationRow> MissingControls { get; init; } = [];
    public List<BusinessQualificationRow> MissingPlans { get; init; } = [];
    public List<BusinessQualificationRow> MissingActivities { get; init; } = [];
    public List<BusinessField45ReconciliationRow> Field45Reconciliation { get; init; } = [];
    public List<BusinessBlockerDependencyRow> Dependencies { get; init; } = [];
    public List<BusinessCreationRuleEvidenceRow> CreationRuleEvidence { get; init; } = [];
}

public static class BusinessInputQualification
{
    public const int PreviousReportedAttributeCount = 215;
    public const string ExpectedWorkbookSha256 = "5c3fc00864947afe1e34d3d6ffdfc6da008eaa3c8f1c6c764161014d5ef9a385";

    public static BusinessCreationRules LoadVerifiedCreationRules(string repoRoot)
    {
        string componentRelative = Path.Combine("frontend", "rl-app", "src", "app", "features", "admin", "matrices-riesgos", "components", "matrices-riesgos-mitigacion", "matrices-riesgos-mitigacion.component.ts");
        string componentPath = Path.Combine(repoRoot, componentRelative);
        string ddlRelative = Path.Combine("database", "19_matrices_riesgos", "transicion", "06_reconstruir_modelo_17_tablas.sql");
        string ddlPath = Path.Combine(repoRoot, ddlRelative);
        if (!File.Exists(componentPath) || !File.Exists(ddlPath))
            throw new InvalidOperationException("BUSINESS_INPUT_RULE_EVIDENCE_FILE_MISSING");

        string component = File.ReadAllText(componentPath);
        string ddl = File.ReadAllText(ddlPath);
        bool hasControlState = component.Contains("controlEstado = 'ACTIVO'", StringComparison.Ordinal)
            && component.Contains("this.controlEstado = 'ACTIVO'", StringComparison.Ordinal)
            && component.Contains("conEstado: this.controlEstado.trim() || 'ACTIVO'", StringComparison.Ordinal);
        bool hasAutomation = component.Contains("controlAutomatizacion: ControlRiesgoGuardarDto['conAutomatizacion'] = 'MANUAL'", StringComparison.Ordinal)
            && component.Contains("this.controlAutomatizacion = 'MANUAL'", StringComparison.Ordinal)
            && component.Contains("conAutomatizacion: this.controlAutomatizacion", StringComparison.Ordinal);
        bool hasPlanState = component.Contains("planEstado = 'PENDIENTE'", StringComparison.Ordinal)
            && component.Contains("this.planEstado = 'PENDIENTE'", StringComparison.Ordinal)
            && component.Contains("plaEstado: this.planEstado.trim() || 'PENDIENTE'", StringComparison.Ordinal);
        bool hasActivityState = component.Contains("actividadEstado = 'PENDIENTE'", StringComparison.Ordinal)
            && component.Contains("this.actividadEstado = 'PENDIENTE'", StringComparison.Ordinal)
            && component.Contains("actEstado: this.actividadEstado.trim() || 'PENDIENTE'", StringComparison.Ordinal);
        bool planAdvance = Regex.IsMatch(ddl, @"PLA_AVANCE\s+NUMBER\(5,2\)\s+DEFAULT\s+0\s+NOT\s+NULL", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        bool planBudget = Regex.IsMatch(ddl, @"PLA_PRESUPUESTO\s+NUMBER\(15,2\)\s+DEFAULT\s+0\s+NOT\s+NULL", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        bool activityAdvance = Regex.IsMatch(ddl, @"ACT_AVANCE\s+NUMBER\(5,2\)\s+DEFAULT\s+0\s+NOT\s+NULL", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        bool planAdvanceFlow = component.Contains("planAvance = 0", StringComparison.Ordinal) && component.Contains("plaAvance: Number(this.planAvance)", StringComparison.Ordinal);
        bool planBudgetFlow = component.Contains("planPresupuesto = 0", StringComparison.Ordinal) && component.Contains("plaPresupuesto: Number(this.planPresupuesto)", StringComparison.Ordinal);
        bool activityAdvanceFlow = component.Contains("actividadAvance = 0", StringComparison.Ordinal) && component.Contains("actAvance: Number(this.actividadAvance)", StringComparison.Ordinal);

        var evidence = new List<BusinessCreationRuleEvidenceRow>
        {
            Evidence("NEW_CONTROL_STATE", "ACTIVO", "APPLICATION_CREATE_FLOW", componentRelative, "iniciarNuevoControl / guardarControl", hasControlState, "New-control form initializes ACTIVO and submits it."),
            Evidence("EMPTY_F32_CONTROL_AUTOMATION", "MANUAL", "APPLICATION_CREATE_FLOW", componentRelative, "iniciarNuevoControl / guardarControl", hasAutomation, "New-control form initializes MANUAL and submits it; explicit F32 overrides it."),
            Evidence("NEW_PLAN_STATE", "PENDIENTE", "APPLICATION_CREATE_FLOW", componentRelative, "nuevoPlan / guardarPlan", hasPlanState, "New-plan form initializes PENDIENTE and submits it."),
            Evidence("NEW_ACTIVITY_STATE", "PENDIENTE", "APPLICATION_CREATE_FLOW", componentRelative, "nuevaActividad / guardarActividad", hasActivityState, "New-activity form initializes PENDIENTE and submits it."),
            Evidence("DB_DEFAULT_PLA_AVANCE", "0", "ORACLE_VERSIONED_DDL", ddlRelative, "RL_MR_PLANES.PLA_AVANCE", planAdvance, "DDL defines DEFAULT 0 NOT NULL."),
            Evidence("DB_DEFAULT_PLA_PRESUPUESTO", "0", "ORACLE_VERSIONED_DDL", ddlRelative, "RL_MR_PLANES.PLA_PRESUPUESTO", planBudget, "DDL defines DEFAULT 0 NOT NULL."),
            Evidence("DB_DEFAULT_ACT_AVANCE", "0", "ORACLE_VERSIONED_DDL", ddlRelative, "RL_MR_ACTIVIDADES.ACT_AVANCE", activityAdvance, "Versioned DDL declares DEFAULT 0 NOT NULL; production dictionary is separately audited at runtime."),
            Evidence("PLAN_ADVANCE_CREATE_FLOW", "0", "APPLICATION_CREATE_FLOW", componentRelative, "nuevoPlan / guardarPlan", planAdvanceFlow, "New-plan form initializes and submits advance."),
            Evidence("PLAN_BUDGET_CREATE_FLOW", "0", "APPLICATION_CREATE_FLOW", componentRelative, "nuevoPlan / guardarPlan", planBudgetFlow, "New-plan form initializes and submits budget."),
            Evidence("ACTIVITY_ADVANCE_CREATE_FLOW", "0", "APPLICATION_CREATE_FLOW", componentRelative, "nuevaActividad / guardarActividad", activityAdvanceFlow, "New-activity form initializes and submits advance.")
        };
        if (evidence.Any(x => !x.Verified))
            throw new InvalidOperationException("BUSINESS_INPUT_RULE_DRIFT: one or more creation rules/defaults are no longer verified.");

        return new BusinessCreationRules
        {
            ControlState = "ACTIVO", AutomationWhenF32Empty = "MANUAL", PlanState = "PENDIENTE",
            ActivityState = "PENDIENTE", PlanAdvanceDefaultVerified = planAdvance,
            PlanBudgetDefaultVerified = planBudget, ActivityAdvanceDefaultVerified = activityAdvance,
            PlanAdvanceCreateFlowVerified = planAdvanceFlow, PlanBudgetCreateFlowVerified = planBudgetFlow,
            ActivityAdvanceCreateFlowVerified = activityAdvanceFlow,
            Evidence = evidence
        };
    }

    public static BusinessInputQualificationResult Build(
        IReadOnlyList<JsonElement> workbookRows,
        IReadOnlyList<JsonElement> manifestFields,
        JsonDocument catalogManifest,
        DatabaseSnapshot snapshot,
        BusinessCreationRules rules,
        string runId,
        string generatorGitSha,
        string workbookSha256)
    {
        ArgumentNullException.ThrowIfNull(workbookRows);
        ArgumentNullException.ThrowIfNull(manifestFields);
        ArgumentNullException.ThrowIfNull(catalogManifest);
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(rules);
        if (!rules.AllVerified) throw new InvalidOperationException("BUSINESS_INPUT_CREATION_RULES_NOT_VERIFIED");
        if (!string.Equals(workbookSha256, ExpectedWorkbookSha256, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("BUSINESS_INPUT_WORKBOOK_HASH_MISMATCH");
        if (workbookRows.Count != 59 || manifestFields.Count != 82)
            throw new InvalidOperationException($"BUSINESS_INPUT_WORKBOOK_UNIVERSE_INVALID: risks={workbookRows.Count}, fields={manifestFields.Count}.");

        var seenCodes = new HashSet<string>(StringComparer.Ordinal);
        var activeEvaluations = snapshot.Evaluations.Where(e => e.Active == 1).GroupBy(e => e.RiskCode, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.ToList(), StringComparer.Ordinal);
        var projectionsByEva = snapshot.Projections.GroupBy(p => p.EvaluationId).ToDictionary(g => g.Key, g => g.ToList());
        var controlsByEva = snapshot.Controls.GroupBy(c => c.EvaluationId).ToDictionary(g => g.Key, g => g.ToList());
        var plansByEva = snapshot.Plans.GroupBy(p => p.EvaluationId).ToDictionary(g => g.Key, g => g.OrderBy(p => p.Id).ToList());
        var activitiesByPlan = snapshot.Activities.GroupBy(a => a.PlanId).ToDictionary(g => g.Key, g => g.ToList());
        var automationMap = BuildCatalogMap(catalogManifest.RootElement, "CONTROL_AUTOMATION");

        var requests = new List<BusinessInputRequestRow>();
        var qualification = new List<BusinessQualificationRow>();
        var controlAudit = new List<BusinessParsingAuditRow>();
        var planAudit = new List<BusinessParsingAuditRow>();
        var activityAudit = new List<BusinessParsingAuditRow>();
        var missingControls = new List<BusinessQualificationRow>();
        var missingPlans = new List<BusinessQualificationRow>();
        var missingActivities = new List<BusinessQualificationRow>();
        var field45Rows = new List<BusinessField45ReconciliationRow>();
        var dependencies = new List<BusinessBlockerDependencyRow>();
        var parserAmbiguities = 0;
        var sourceControlItems = 0;
        var sourcePlanItems = 0;
        var sourceActivityItems = 0;
        var preventive = 0;
        var detective = 0;
        var corrective = 0;
        int matchedRisks = 0, matchedEvaluations = 0, matchedProjections = 0;
        var plansByRiskOrdinal = new Dictionary<(string Risk, int Ordinal), (PlanPreimage? Existing, string Description, bool Missing)>();
        var activitiesByRiskOrdinal = new Dictionary<(string Risk, int Ordinal), (int? ParentOrdinal, string Description, string Status)>();

        foreach (JsonElement row in workbookRows.OrderBy(r => r.GetProperty("riskNo").GetInt32()))
        {
            string riskCode = row.GetProperty("riskCode").GetString()?.Trim() ?? "";
            int riskNo = row.GetProperty("riskNo").GetInt32();
            if (string.IsNullOrWhiteSpace(riskCode) || !seenCodes.Add(riskCode))
                throw new InvalidOperationException("BUSINESS_INPUT_RISK_CODE_EMPTY_OR_DUPLICATE");
            var riskMatches = snapshot.Risks.Where(r => string.Equals(r.Code, riskCode, StringComparison.Ordinal)).ToList();
            if (riskMatches.Count != 1) throw new InvalidOperationException($"BUSINESS_INPUT_RISK_CARDINALITY:{riskCode}:{riskMatches.Count}");
            matchedRisks++;
            if (!activeEvaluations.TryGetValue(riskCode, out var evalMatches) || evalMatches.Count != 1)
                throw new InvalidOperationException($"BUSINESS_INPUT_ACTIVE_EVALUATION_CARDINALITY:{riskCode}:{evalMatches?.Count ?? 0}");
            EvaluationPreimage evaluation = evalMatches[0];
            matchedEvaluations++;
            if (!projectionsByEva.TryGetValue(evaluation.Id, out var projectionMatches) || projectionMatches.Count != 1)
                throw new InvalidOperationException($"BUSINESS_INPUT_PROJECTION_CARDINALITY:{riskCode}:{projectionMatches?.Count ?? 0}");
            ProjectionPreimage projection = projectionMatches[0];
            matchedProjections++;

            string area = Cell(row, 3);
            string f32 = Cell(row, 32);
            string? automation = Canonicalize(automationMap, f32);
            bool mitigationApplies = Program.MitigationApplies(projection);
            string[] controlTypes = ["PREVENTIVO", "DETECTIVO", "CORRECTIVO"];
            foreach ((int field, string type) in new[] { (20, controlTypes[0]), (24, controlTypes[1]), (28, controlTypes[2]) })
            {
                string raw = Cell(row, field);
                if (string.IsNullOrWhiteSpace(raw)) continue;
                var parsed = Program.ParseSourceItems(raw);
                if (parsed.IsAmbiguous) { parserAmbiguities++; AddAudit(controlAudit, riskCode, riskNo, field, Label(manifestFields, field), raw, parsed, evidence: "CONTROL_SOURCE_PARSER_AMBIGUOUS"); continue; }
                for (int i = 0; i < parsed.Items.Count; i++)
                {
                    string item = parsed.Items[i];
                    int ordinal = i + 1;
                    sourceControlItems++;
                    if (type == "PREVENTIVO") preventive++; else if (type == "DETECTIVO") detective++; else corrective++;
                    AddAudit(controlAudit, riskCode, riskNo, field, Label(manifestFields, field), raw, parsed, i, "F20/F24/F28; ParseSourceItems is authoritative.");
                    var existing = controlsByEva.GetValueOrDefault(evaluation.Id, []).Where(c => string.Equals(c.Tipo, type, StringComparison.OrdinalIgnoreCase)).ToList();
                    string status;
                    bool isEmpty = string.IsNullOrWhiteSpace(item);
                    if (isEmpty)
                    {
                        status = "EMPTY_SOURCE_ITEM";
                        var request = Request(riskCode, riskNo, area, field, Label(manifestFields, field), "CONTROL", ordinal, null, item,
                            "CON_DESCRIPCION", "CON_DESCRIPCION", "Confirmar/elaborar la descripcion del control o indicar expresamente que el marcador no representa un control real.",
                            "Una fila numerada terminal esta vacia; no se descarta automaticamente.", "", "", "");
                        requests.Add(request);
                        var q = Qual(riskCode, field, "CONTROL", ordinal, null, raw, item, "NOT_APPLICABLE", "CON_DESCRIPCION", "CON_DESCRIPCION", true, "", "MATRIZ_CONSOLIDADA", "", "", "Workbook field cell", true, "HUMAN_INPUT_REQUIRED", "EMPTY_TERMINAL_NUMBERED_ITEM");
                        qualification.Add(q); missingControls.Add(q);
                        continue;
                    }
                    if (IsNoControl(item))
                    {
                        status = "NOT_APPLICABLE";
                        qualification.Add(Qual(riskCode, field, "CONTROL", ordinal, null, raw, item, status, "", "", false, "", "MATRIZ_CONSOLIDADA", "", "", "Workbook field cell", false, status, "CANONICAL_NO_CONTROL"));
                        continue;
                    }
                    var matches = existing.Where(c => SafeTextKey(c.Descripcion) == SafeTextKey(item)).ToList();
                    if (matches.Count > 1)
                    {
                        parserAmbiguities++;
                        qualification.Add(Qual(riskCode, field, "CONTROL", ordinal, null, raw, item, "AMBIGUOUS", "", "", true, "", "MATRIZ_CONSOLIDADA", "", "", "RL_MR_CONTROLES_RIESGO", false, "AMBIGUOUS", "DUPLICATE_EXISTING_CONTROL_IDENTITY"));
                        continue;
                    }
                    status = matches.Count == 1 ? "MATCH_EXISTING" : "MISSING_CONTROL";
                    var controlQ = Qual(
                        riskCode, field, "CONTROL", ordinal, null, raw, item, status,
                        matches.Count == 1 ? "" : "CON_ESTADO;CON_AUTOMATIZACION",
                        matches.Count == 1 ? "" : "RL_MR_CONTROLES_RIESGO.CON_ESTADO;RL_MR_CONTROLES_RIESGO.CON_AUTOMATIZACION",
                        matches.Count == 0, "", "MATRIZ_CONSOLIDADA",
                        matches.Count == 1 ? "" : rules.ControlState + ";" + (string.IsNullOrWhiteSpace(f32) ? rules.AutomationWhenF32Empty : automation ?? ""),
                        string.Join(";", rules.Evidence.Where(e => e.RuleId is "NEW_CONTROL_STATE" or "EMPTY_F32_CONTROL_AUTOMATION").Select(e => e.SourceFileOrDdl + ":" + e.SourceSymbol)),
                        matches.Count == 1 ? "RL_MR_CONTROLES_RIESGO" : "NEW_CONTROL_RULES", false, status,
                        matches.Count == 1 ? "EXACT_NORMALIZED_DESCRIPTION_MATCH" : "MISSING_CONTROL_WITH_VERIFIED_CREATE_RULES");
                    qualification.Add(controlQ);
                    if (matches.Count == 0)
                    {
                        qualification.Add(Qual(riskCode, field, "CONTROL", ordinal, null, raw, item, "SOURCE", "CON_DESCRIPCION", "RL_MR_CONTROLES_RIESGO.CON_DESCRIPCION", true, "", "MATRIZ_CONSOLIDADA", "", "", "WORKBOOK:F" + field, false, "RESOLVED_WORKBOOK", "CONTROL_DESCRIPTION_FROM_SOURCE"));
                        qualification.Add(Qual(riskCode, field, "CONTROL", ordinal, null, raw, item, "MISSING_CONTROL", "CON_ESTADO", "RL_MR_CONTROLES_RIESGO.CON_ESTADO", true, "", "APPLICATION_CREATE_FLOW", rules.ControlState, RuleEvidence(rules, "NEW_CONTROL_STATE"), "NEW_CONTROL_RULES", false, "RESOLVED_SYSTEM_CREATION_RULE", "STANDARD_CONTROL_CREATE_FORM_VALUE"));
                        qualification.Add(Qual(riskCode, field, "CONTROL", ordinal, null, raw, item, string.IsNullOrWhiteSpace(f32) ? "SYSTEM_RULE" : "WORKBOOK_F32", "CON_AUTOMATIZACION", "RL_MR_CONTROLES_RIESGO.CON_AUTOMATIZACION", true, "", string.IsNullOrWhiteSpace(f32) ? "APPLICATION_CREATE_FLOW" : "WORKBOOK_F32", string.IsNullOrWhiteSpace(f32) ? rules.AutomationWhenF32Empty : automation ?? "", RuleEvidence(rules, "EMPTY_F32_CONTROL_AUTOMATION"), string.IsNullOrWhiteSpace(f32) ? "NEW_CONTROL_RULES" : "MATRIZ_CONSOLIDADA.F32", false, string.IsNullOrWhiteSpace(f32) ? "RESOLVED_SYSTEM_CREATION_RULE" : "RESOLVED_WORKBOOK", "CONTROL_AUTOMATION_FROM_AUTHORIZED_SOURCE"));
                        if (!string.IsNullOrWhiteSpace(f32) && automation is null)
                        {
                            parserAmbiguities++;
                            var unresolved = controlQ with { HumanInputRequired = true, Status = "SOURCE_AMBIGUOUS", Reason = "UNKNOWN_CONTROL_AUTOMATION_CATALOG_VALUE" };
                            qualification[^1] = unresolved;
                            missingControls.Add(unresolved);
                            requests.Add(Request(riskCode, riskNo, area, 32, Label(manifestFields, 32), "CONTROL", ordinal, null, item, "CON_AUTOMATIZACION", "CON_AUTOMATIZACION", "Indicar automatizacion usando el catalogo institucional.", "El valor F32 no se resuelve por la lista de aliases explicitos.", "Automatizado|Semiautomatizado|Manual", "", ""));
                        }
                        else missingControls.Add(controlQ);
                    }
                }
            }

            var excelPlans = new List<(string Description, int Ordinal, int? ParentOrdinal)>();
            string f40 = Cell(row, 40);
            SourceItemsParseResult? parsedPlans = null;
            if (!string.IsNullOrWhiteSpace(f40))
            {
                parsedPlans = Program.ParseSourceItems(f40);
                AddAudit(planAudit, riskCode, riskNo, 40, Label(manifestFields, 40), f40, parsedPlans, evidence: "F40; ParseSourceItems is authoritative.");
                if (parsedPlans.IsAmbiguous) parserAmbiguities++;
                else sourcePlanItems += parsedPlans.Items.Count;
            }
            if (mitigationApplies && parsedPlans is { IsAmbiguous: false })
            {
                for (int i = 0; i < parsedPlans.Items.Count; i++)
                {
                    string desc = parsedPlans.Items[i]; int ordinal = i + 1;
                    excelPlans.Add((desc, ordinal, null));
                }
            }
            else if (!mitigationApplies && parsedPlans is { IsAmbiguous: false })
                for (int i = 0; i < parsedPlans.Items.Count; i++)
                    qualification.Add(Qual(riskCode, 40, "PLAN", i + 1, null, f40, parsedPlans.Items[i], "NOT_APPLICABLE", "", "", false, "", "MATRIZ_CONSOLIDADA", "", "", "MITIGATION_APPLICABILITY_RULE", false, "NOT_APPLICABLE", "CONTRACTUAL_MITIGATION_APPLICABILITY_FALSE"));
            var existingPlans = plansByEva.GetValueOrDefault(evaluation.Id, []);
            foreach (var ep in existingPlans.Select((p, i) => (Plan: p, Ordinal: i + 1)))
                plansByRiskOrdinal[(riskCode, ep.Ordinal)] = (ep.Plan, ep.Plan.Descripcion, false);
            foreach (var src in excelPlans)
            {
                var matches = existingPlans.Where(p => SafeTextKey(p.Descripcion) == SafeTextKey(src.Description)).ToList();
                if (matches.Count > 1)
                {
                    parserAmbiguities++;
                    var q = Qual(riskCode, 40, "PLAN", src.Ordinal, null, f40, src.Description, "AMBIGUOUS", "", "", true, "", "MATRIZ_CONSOLIDADA", "", "", "RL_MR_PLANES", false, "AMBIGUOUS", "DUPLICATE_EXISTING_PLAN_IDENTITY");
                    qualification.Add(q); missingPlans.Add(q); continue;
                }
                bool isMissing = matches.Count == 0;
                PlanPreimage? existingPlan = matches.SingleOrDefault();
                plansByRiskOrdinal[(riskCode, src.Ordinal)] = (existingPlan, src.Description, isMissing);
                string status = string.IsNullOrWhiteSpace(src.Description) ? "EMPTY_SOURCE_PLAN" : isMissing ? "MISSING_PLAN" : "MATCH_EXISTING_PLAN";
                if (isMissing)
                {
                    string description = src.Description;
                    if (string.IsNullOrWhiteSpace(description))
                    {
                        var request = Request(riskCode, riskNo, area, 40, Label(manifestFields, 40), "PLAN", src.Ordinal, null, "", "PLA_DESCRIPCION", "PLA_DESCRIPCION", "Describir la accion del plan o confirmar expresamente que el marcador no corresponde a un plan real.", "El marcador terminal de F40 no contiene descripcion.", "", "", "");
                        requests.Add(request);
                        AddQualificationRequest(qualification, missingPlans, request, f40, "", "EMPTY_TERMINAL_NUMBERED_ITEM", rules);
                    }
                    else qualification.Add(Qual(riskCode, 40, "PLAN", src.Ordinal, null, f40, description, "SOURCE", "PLA_DESCRIPCION", "RL_MR_PLANES.PLA_DESCRIPCION", true, "", "MATRIZ_CONSOLIDADA", "", "", "WORKBOOK:F40", false, "RESOLVED_WORKBOOK", "PLAN_DESCRIPTION_FROM_SOURCE"));
                    qualification.Add(Qual(riskCode, 40, "PLAN", src.Ordinal, null, f40, description, "MISSING_PLAN", "PLA_ESTADO", "RL_MR_PLANES.PLA_ESTADO", true, "", "APPLICATION_CREATE_FLOW", rules.PlanState, RuleEvidence(rules, "NEW_PLAN_STATE"), "NEW_PLAN_RULES", false, "RESOLVED_SYSTEM_CREATION_RULE", "STANDARD_PLAN_CREATE_FORM_VALUE"));
                    qualification.Add(Qual(riskCode, 40, "PLAN", src.Ordinal, null, f40, description, "MISSING_PLAN", "PLA_AVANCE", "RL_MR_PLANES.PLA_AVANCE", true, rules.PlanAdvanceDefaultVerified ? "0" : "", rules.PlanAdvanceDefaultVerified ? "ORACLE_DEFAULT" : "APPLICATION_CREATE_FLOW", rules.PlanAdvanceDefaultVerified ? "0" : "0", RuleEvidence(rules, rules.PlanAdvanceDefaultVerified ? "DB_DEFAULT_PLA_AVANCE" : "PLAN_ADVANCE_CREATE_FLOW"), "NEW_PLAN_RULES", false, rules.PlanAdvanceDefaultVerified ? "RESOLVED_DATABASE_DEFAULT" : "RESOLVED_SYSTEM_CREATION_RULE", "AUTHORIZED_PLAN_ADVANCE_ZERO"));
                    qualification.Add(Qual(riskCode, 40, "PLAN", src.Ordinal, null, f40, description, "MISSING_PLAN", "PLA_PRESUPUESTO", "RL_MR_PLANES.PLA_PRESUPUESTO", true, rules.PlanBudgetDefaultVerified ? "0" : "", rules.PlanBudgetDefaultVerified ? "ORACLE_DEFAULT" : "APPLICATION_CREATE_FLOW", rules.PlanBudgetDefaultVerified ? "0" : "0", RuleEvidence(rules, rules.PlanBudgetDefaultVerified ? "DB_DEFAULT_PLA_PRESUPUESTO" : "PLAN_BUDGET_CREATE_FLOW"), "NEW_PLAN_RULES", false, rules.PlanBudgetDefaultVerified ? "RESOLVED_DATABASE_DEFAULT" : "RESOLVED_SYSTEM_CREATION_RULE", "AUTHORIZED_PLAN_BUDGET_ZERO"));
                    foreach (string dateColumn in new[] { "PLA_FECHA_INICIO", "PLA_FECHA_FIN" })
                    {
                        string attr = dateColumn == "PLA_FECHA_INICIO" ? "START_DATE" : "END_DATE";
                        var request = Request(riskCode, riskNo, area, 40, Label(manifestFields, 40), "PLAN", src.Ordinal, null, description, attr, dateColumn,
                            dateColumn == "PLA_FECHA_INICIO" ? "Fecha de inicio real del plan en formato AAAA-MM-DD." : "Fecha final real del plan en formato AAAA-MM-DD.",
                            "La columna Oracle es NOT NULL y el workbook no proporciona fecha; no existe default autorizado.", "ISO-8601 date (YYYY-MM-DD)", "", "");
                        requests.Add(request);
                        AddQualificationRequest(qualification, missingPlans, request, f40, description, "MISSING_PLAN_DATE_NO_WORKBOOK_SOURCE", rules);
                    }
                    var planKey = RequestKey(riskCode, 40, "PLAN", src.Ordinal, "PLA_DESCRIPCION");
                    dependencies.Add(new BusinessBlockerDependencyRow(planKey, "PLAN", $"{riskCode}|{src.Ordinal}", "", "MISSING_PLAN", "PLA_FECHA_INICIO;PLA_FECHA_FIN", false, true));
                }
                else qualification.Add(Qual(riskCode, 40, "PLAN", src.Ordinal, null, f40, src.Description, status, "", "", false, "", "MATRIZ_CONSOLIDADA", "", "", "RL_MR_PLANES", false, status, "EXACT_NORMALIZED_DESCRIPTION_MATCH"));
            }

            string f42 = Cell(row, 42);
            if (!mitigationApplies && !string.IsNullOrWhiteSpace(f42))
            {
                var auditOnly = Program.ParseSourceItems(f42, allowHierarchicalNumbering: true);
                AddAudit(activityAudit, riskCode, riskNo, 42, Label(manifestFields, 42), f42, auditOnly, evidence: "F42 source audited; mitigation applicability is false.");
                if (auditOnly.IsAmbiguous) parserAmbiguities++;
                else
                {
                    sourceActivityItems += auditOnly.Items.Count;
                    for (int i = 0; i < auditOnly.Items.Count; i++)
                        qualification.Add(Qual(riskCode, 42, "ACTIVITY", i + 1, i < auditOnly.ParentOrdinals.Count ? auditOnly.ParentOrdinals[i] : null, f42, auditOnly.Items[i], "NOT_APPLICABLE", "", "", false, "", "MATRIZ_CONSOLIDADA", "", "", "MITIGATION_APPLICABILITY_RULE", false, "NOT_APPLICABLE", "CONTRACTUAL_MITIGATION_APPLICABILITY_FALSE"));
                }
            }
            if (mitigationApplies && !string.IsNullOrWhiteSpace(f42))
            {
                var parsed = Program.ParseSourceItems(f42, allowHierarchicalNumbering: true);
                if (parsed.IsAmbiguous) { parserAmbiguities++; AddAudit(activityAudit, riskCode, riskNo, 42, Label(manifestFields, 42), f42, parsed, evidence: "ACTIVITY_SOURCE_PARSER_AMBIGUOUS"); }
                else
                {
                    var existingActivities = existingPlans.SelectMany(p => activitiesByPlan.GetValueOrDefault(p.Id, [])).ToList();
                    for (int i = 0; i < parsed.Items.Count; i++)
                    {
                        string description = parsed.Items[i]; int ordinal = i + 1;
                        int? parentOrdinal = i < parsed.ParentOrdinals.Count ? parsed.ParentOrdinals[i] : null;
                        sourceActivityItems++;
                        AddAudit(activityAudit, riskCode, riskNo, 42, Label(manifestFields, 42), f42, parsed, i, "F42 hierarchical parent ordinal comes from ParseSourceItems.");
                        var matchingActivities = existingActivities.Where(a => SafeTextKey(a.Descripcion) == SafeTextKey(description)).ToList();
                        if (matchingActivities.Count > 1)
                        {
                            parserAmbiguities++;
                            var ambiguous = Qual(riskCode, 42, "ACTIVITY", ordinal, parentOrdinal, f42, description, "AMBIGUOUS", "", "", true, "", "MATRIZ_CONSOLIDADA", "", "", "RL_MR_ACTIVIDADES", false, "AMBIGUOUS", "DUPLICATE_EXISTING_ACTIVITY_IDENTITY");
                            qualification.Add(ambiguous); missingActivities.Add(ambiguous); continue;
                        }
                        if (matchingActivities.Count == 1)
                        {
                            var existingActivity = matchingActivities[0];
                            int? dbParentOrdinal = existingPlans.FindIndex(p => p.Id == existingActivity.PlanId) + 1;
                            string matchStatus = parentOrdinal is null || parentOrdinal == dbParentOrdinal ? "MATCH_EXISTING" : "AMBIGUOUS";
                            qualification.Add(Qual(riskCode, 42, "ACTIVITY", ordinal, parentOrdinal, f42, description, matchStatus, "", "", false, "", "MATRIZ_CONSOLIDADA", "", "", "RL_MR_ACTIVIDADES", false, matchStatus, matchStatus == "MATCH_EXISTING" ? "EXACT_NORMALIZED_DESCRIPTION_AND_PARENT_MATCH" : "SOURCE_AND_DB_PARENT_ORDINAL_CONFLICT"));
                            if (matchStatus == "AMBIGUOUS") parserAmbiguities++;
                            activitiesByRiskOrdinal[(riskCode, ordinal)] = (parentOrdinal, description, matchStatus);
                            continue;
                        }

                        activitiesByRiskOrdinal[(riskCode, ordinal)] = (parentOrdinal, description, "MISSING_ACTIVITY");
                        string activityStatus = "MISSING_ACTIVITY";
                        qualification.Add(Qual(riskCode, 42, "ACTIVITY", ordinal, parentOrdinal, f42, description, "SOURCE", "ACT_DESCRIPCION", "RL_MR_ACTIVIDADES.ACT_DESCRIPCION", true, "", "MATRIZ_CONSOLIDADA", "", "", "WORKBOOK:F42", false, "RESOLVED_WORKBOOK", "ACTIVITY_DESCRIPTION_FROM_SOURCE"));
                        var associationValues = excelPlans.OrderBy(p => p.Ordinal).Select(p => $"{p.Ordinal}: {p.Description}").ToList();
                        bool parentAvailable = parentOrdinal.HasValue && plansByRiskOrdinal.ContainsKey((riskCode, parentOrdinal.Value));
                        bool uniqueExistingParent = existingPlans.Count == 1;
                        bool uniqueSourceParent = excelPlans.Count == 1;
                        if (!parentAvailable && !uniqueExistingParent && !uniqueSourceParent)
                        {
                            var association = Request(riskCode, riskNo, area, 42, Label(manifestFields, 42), "ACTIVITY", ordinal, parentOrdinal, description,
                                "PLAN_ASSOCIATION", "ACT_PLAN_ID", "Indicar el ordinal/identidad funcional exacta del plan padre; no usar un ID Oracle.",
                                "La fuente F42 no especifica un padre n.m y hay varios planes posibles.", string.Join(" | ", associationValues),
                                excelPlans.Where(p => p.Ordinal == parentOrdinal).Select(p => p.Description).SingleOrDefault() ?? "", "");
                            requests.Add(association);
                            var qAssociation = Qual(riskCode, 42, "ACTIVITY", ordinal, parentOrdinal, f42, description, activityStatus, "PLAN_ASSOCIATION", "ACT_PLAN_ID", true, "", "MATRIZ_CONSOLIDADA", "", "", "MISSING_OR_AMBIGUOUS_PARENT", true, "HUMAN_INPUT_REQUIRED", "ACTIVITY_PARENT_REQUIRES_EXACT_BUSINESS_SELECTION");
                            qualification.Add(qAssociation); missingActivities.Add(qAssociation);
                        }
                        else qualification.Add(Qual(riskCode, 42, "ACTIVITY", ordinal, parentOrdinal, f42, description, activityStatus, "", "", false, "", "MATRIZ_CONSOLIDADA", "", "", parentAvailable ? "F42_PARENT_ORDINAL" : "UNIQUE_PARENT", false, "PARENT_RESOLVED", "EXACT_SOURCE_OR_SINGLE_PARENT"));
                        foreach ((string attr, string column, string labelText) in new[]
                        {
                            ("ACT_RESPONSABLE", "ACT_RESPONSABLE", "Responsable de la actividad"),
                            ("START_DATE", "ACT_FECHA_INICIO", "Fecha de inicio de la actividad"),
                            ("END_DATE", "ACT_FECHA_FIN", "Fecha final de la actividad")
                        })
                        {
                            var request = Request(riskCode, riskNo, area, 42, Label(manifestFields, 42), "ACTIVITY", ordinal, parentOrdinal, description,
                                attr, column, labelText, "El campo es NOT NULL y no hay valor fuente ni default autorizado.",
                                column.Contains("FECHA", StringComparison.Ordinal) ? "ISO-8601 date (YYYY-MM-DD)" : "Nombre/rol responsable valido (maximo 150 caracteres)",
                                parentAvailable ? RequestKey(riskCode, 40, "PLAN", parentOrdinal!.Value, "PLA_DESCRIPCION") : "", "");
                            requests.Add(request);
                            AddQualificationRequest(qualification, missingActivities, request, f42, description, "MISSING_ACTIVITY_REQUIRED_ATTRIBUTE", rules);
                        }
                        qualification.Add(Qual(riskCode, 42, "ACTIVITY", ordinal, parentOrdinal, f42, description, "MISSING_ACTIVITY", "ACT_ESTADO", "RL_MR_ACTIVIDADES.ACT_ESTADO", true, "", "APPLICATION_CREATE_FLOW", rules.ActivityState, RuleEvidence(rules, "NEW_ACTIVITY_STATE"), "NEW_ACTIVITY_RULES", false, "RESOLVED_SYSTEM_CREATION_RULE", "STANDARD_ACTIVITY_CREATE_FORM_VALUE"));
                        qualification.Add(Qual(riskCode, 42, "ACTIVITY", ordinal, parentOrdinal, f42, description, "MISSING_ACTIVITY", "ACT_AVANCE", "RL_MR_ACTIVIDADES.ACT_AVANCE", true, rules.ActivityAdvanceDefaultVerified ? "0" : "", rules.ActivityAdvanceDefaultVerified ? "ORACLE_DEFAULT" : "APPLICATION_CREATE_FLOW", "0", RuleEvidence(rules, rules.ActivityAdvanceDefaultVerified ? "DB_DEFAULT_ACT_AVANCE" : "ACTIVITY_ADVANCE_CREATE_FLOW"), "NEW_ACTIVITY_RULES", false, rules.ActivityAdvanceDefaultVerified ? "RESOLVED_DATABASE_DEFAULT" : "RESOLVED_SYSTEM_CREATION_RULE", "AUTHORIZED_ACTIVITY_ADVANCE_ZERO"));
                        string dependencyKey = parentAvailable ? RequestKey(riskCode, 40, "PLAN", parentOrdinal!.Value, "PLA_FECHA_INICIO") : "";
                        if (!string.IsNullOrWhiteSpace(dependencyKey))
                            dependencies.Add(new BusinessBlockerDependencyRow(RequestKey(riskCode, 42, "ACTIVITY", ordinal, "ACT_RESPONSABLE"), "ACTIVITY", $"{riskCode}|{ordinal}", dependencyKey, "ACTIVITY_PARENT_PLAN", "ACT_PLAN_ID", true, false));
                    }
                }
            }

            string f45 = Cell(row, 45);
            if (!string.IsNullOrWhiteSpace(f45))
            {
                var parsed = Program.ParseSourceItems(f45);
                if (!mitigationApplies)
                {
                    field45Rows.Add(new(riskCode, riskNo, f45, parsed.Items.Count, excelPlans.Count, existingPlans.Count, "NOT_APPLICABLE", "Mitigation applicability is false; F45 is not a plan-responsible input for this evaluation."));
                    qualification.Add(Qual(riskCode, 45, "PLAN_RESPONSIBLE", 1, null, f45, string.Join("\n", parsed.Items), "NOT_APPLICABLE", "", "", false, "", "MATRIZ_CONSOLIDADA", "", "", "MITIGATION_APPLICABILITY_RULE", false, "NOT_APPLICABLE", "CONTRACTUAL_MITIGATION_APPLICABILITY_FALSE"));
                }
                else if (parsed.IsAmbiguous) { parserAmbiguities++; field45Rows.Add(new(riskCode, riskNo, f45, 0, excelPlans.Count, existingPlans.Count, "AMBIGUOUS", parsed.ParseRule)); }
                else
                {
                    bool matchedByPersistedOrder = existingPlans.Count == parsed.Items.Count && existingPlans.Count > 0;
                    bool matchedBySourceOrder = excelPlans.Count == parsed.Items.Count && excelPlans.Count > 0;
                    string status = matchedByPersistedOrder ? "MATCH_EXISTING_BY_PLAN_ORDER" : matchedBySourceOrder ? "SOURCE_ORDINAL_MAPPING" : "PLAN_ASSOCIATION_REQUIRED";
                    field45Rows.Add(new(riskCode, riskNo, f45, parsed.Items.Count, excelPlans.Count, existingPlans.Count, status, "F45-to-plan association resolved only by equal cardinality/order; otherwise request a functional plan association."));
                    if (!matchedByPersistedOrder && !matchedBySourceOrder)
                    {
                        for (int i = 0; i < parsed.Items.Count; i++)
                        {
                            int ordinal = i + 1; string resp = parsed.Items[i];
                            var request = Request(riskCode, riskNo, area, 45, Label(manifestFields, 45), "PLAN_RESPONSIBLE", ordinal, null, resp,
                                "PLAN_ASSOCIATION", "PLA_RESPONSABLES", "Indicar exactamente a que plan fuente pertenece este responsable; no repetir el nombre del responsable.",
                                "La cardinalidad de F45 no permite mapearlo inequivocamente a los planes disponibles.", string.Join(" | ", excelPlans.Select(p => $"{p.Ordinal}: {p.Description}")), "", "");
                            requests.Add(request);
                            AddQualificationRequest(qualification, missingPlans, request, f45, resp, "FIELD45_CARDINALITY_DOES_NOT_MAP_TO_PLANS", rules);
                        }
                    }
                }
            }
        }

        var duplicateCount = requests.GroupBy(r => r.RequestKey, StringComparer.Ordinal).Sum(g => Math.Max(0, g.Count() - 1));
        int emptyKeys = requests.Count(r => string.IsNullOrWhiteSpace(r.RequestKey));
        int nonemptyResponses = requests.Count(r => !string.IsNullOrEmpty(r.BusinessResponse) || !string.IsNullOrEmpty(r.BusinessNotes));
        int humanEntities = requests.Select(r => $"{r.RiskCode}|{r.EntityType}|{r.EntityOrdinal}|{r.ParentOrdinal}").Distinct(StringComparer.Ordinal).Count();
        int unexplained = qualification.Count(q => q.Status is "SOURCE_AMBIGUOUS" or "AMBIGUOUS" or "UNEXPLAINED");

        return new BusinessInputQualificationResult
        {
            RunId = runId, GeneratorGitSha = generatorGitSha, WorkbookSha256 = workbookSha256,
            HeaderCount = manifestFields.Count, RiskCount = workbookRows.Count, UniqueRiskCodeCount = seenCodes.Count,
            RisksMatched = matchedRisks, EvaluationsMatched = matchedEvaluations, ProjectionsMatched = matchedProjections,
            ControlSourceCells = controlAudit.Select(x => x.RiskCode + "|" + x.FieldNumber).Distinct(StringComparer.Ordinal).Count(),
            ControlSourceItems = sourceControlItems, PreventiveItems = preventive, DetectiveItems = detective, CorrectiveItems = corrective,
            PlanSourceCells = planAudit.Select(x => x.RiskCode).Distinct(StringComparer.Ordinal).Count(), PlanSourceItems = sourcePlanItems,
            ActivitySourceCells = activityAudit.Select(x => x.RiskCode).Distinct(StringComparer.Ordinal).Count(), ActivitySourceItems = sourceActivityItems,
            ParserAmbiguities = parserAmbiguities, HumanInputEntities = humanEntities,
            UnexplainedQualificationRows = unexplained, EmptyRequestKeys = emptyKeys, DuplicateRequestKeys = duplicateCount,
            NonemptyBusinessResponses = nonemptyResponses, DatabaseName = snapshot.DatabaseName,
            ServiceName = snapshot.ServiceName, InstanceName = snapshot.InstanceName, CurrentSchema = snapshot.CurrentSchema,
            SessionUser = snapshot.SessionUser, RequestRows = requests.OrderBy(r => r.RequestKey, StringComparer.Ordinal).ToList(),
            QualificationRows = qualification, ControlParsingAudit = controlAudit, PlanParsingAudit = planAudit,
            ActivityParsingAudit = activityAudit, MissingControls = missingControls, MissingPlans = missingPlans,
            MissingActivities = missingActivities, Field45Reconciliation = field45Rows, Dependencies = dependencies,
            CreationRuleEvidence = rules.Evidence
        };
    }

    public static void ValidateForPublication(BusinessInputQualificationResult result)
    {
        if (result.HeaderCount != 82 || result.RiskCount != 59 || result.UniqueRiskCodeCount != 59)
            throw new InvalidOperationException("BUSINESS_INPUT_WORKBOOK_VALIDATION_FAILED");
        if (result.RisksMatched != 59 || result.EvaluationsMatched != 59 || result.ProjectionsMatched != 59)
            throw new InvalidOperationException("BUSINESS_INPUT_PRODUCTION_UNIVERSE_VALIDATION_FAILED");
        if (result.ParserAmbiguities != 0 || result.UnexplainedQualificationRows != 0)
            throw new InvalidOperationException("BUSINESS_INPUT_QUALIFICATION_UNRESOLVED");
        if (result.RequestRows.Count == 0 || result.EmptyRequestKeys != 0 || result.DuplicateRequestKeys != 0 || result.NonemptyBusinessResponses != 0)
            throw new InvalidOperationException("BUSINESS_INPUT_REQUEST_VALIDATION_FAILED");
        if (result.RequestRows.Any(r => string.IsNullOrWhiteSpace(r.RiskCode) || r.FieldNumber <= 0 || string.IsNullOrWhiteSpace(r.EntityType) || r.EntityOrdinal <= 0 || string.IsNullOrWhiteSpace(r.Attribute) || string.IsNullOrWhiteSpace(r.DbColumn)))
            throw new InvalidOperationException("BUSINESS_INPUT_REQUEST_TECHNICAL_KEY_EMPTY");
    }

    public static IReadOnlyDictionary<string, string> WriteArtifacts(BusinessInputQualificationResult result, string outputDirectory)
    {
        ValidateForPublication(result);
        string stagingRoot = Path.Combine(outputDirectory, ".staging", result.RunId);
        Directory.CreateDirectory(stagingRoot);
        var files = new Dictionary<string, byte[]>(StringComparer.Ordinal)
        {
            ["block4_business_input_request.csv"] = Csv(RequestHeaders, result.RequestRows.Select(RequestValues)),
            ["block4_business_input_qualification.csv"] = Csv(QualificationHeaders, result.QualificationRows.Select(QualificationValues)),
            ["control_cell_parsing_audit.csv"] = Csv(ParsingHeaders, result.ControlParsingAudit.Select(ParsingValues)),
            ["plan_cell_parsing_audit.csv"] = Csv(ParsingHeaders, result.PlanParsingAudit.Select(ParsingValues)),
            ["activity_cell_parsing_audit.csv"] = Csv(ParsingHeaders, result.ActivityParsingAudit.Select(ParsingValues)),
            ["missing_controls_qualified.csv"] = Csv(QualificationHeaders, result.MissingControls.Select(QualificationValues)),
            ["missing_plans_qualified.csv"] = Csv(QualificationHeaders, result.MissingPlans.Select(QualificationValues)),
            ["missing_activities_qualified.csv"] = Csv(QualificationHeaders, result.MissingActivities.Select(QualificationValues)),
            ["creation_rule_evidence.csv"] = Csv(CreationRuleHeaders, result.CreationRuleEvidence.Select(CreationRuleValues)),
            ["field45_cardinality_reconciliation.csv"] = Csv(Field45Headers, result.Field45Reconciliation.Select(Field45Values)),
            ["blocker_dependency_graph.csv"] = Csv(DependencyHeaders, result.Dependencies.Select(DependencyValues))
        };
        foreach (var file in files) File.WriteAllBytes(Path.Combine(stagingRoot, file.Key), file.Value);
        byte[] summaryBytes = JsonSerializer.SerializeToUtf8Bytes(Summary(result), new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllBytes(Path.Combine(stagingRoot, "block4_business_input_summary.json"), summaryBytes);
        var hashRows = files.ToDictionary(x => x.Key, x => Sha256(x.Value), StringComparer.Ordinal);
        hashRows["block4_business_input_summary.json"] = Sha256(summaryBytes);
        string manifestJson = JsonSerializer.Serialize(new
        {
            runId = result.RunId,
            generatorGitSha = result.GeneratorGitSha,
            workbookSha256 = result.WorkbookSha256,
            files = hashRows.OrderBy(x => x.Key, StringComparer.Ordinal).ToDictionary(x => x.Key, x => x.Value, StringComparer.Ordinal)
        }, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Path.Combine(stagingRoot, "SHA256SUMS.json"), manifestJson, new UTF8Encoding(false));

        var validatedHashes = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var fileName in files.Keys.Append("block4_business_input_summary.json"))
        {
            byte[] bytes = File.ReadAllBytes(Path.Combine(stagingRoot, fileName));
            string hash = Sha256(bytes);
            if (!hash.Equals(hashRows[fileName], StringComparison.Ordinal)) throw new InvalidOperationException($"BUSINESS_INPUT_STAGING_HASH_MISMATCH:{fileName}");
            validatedHashes[fileName] = hash;
        }
        using (var check = JsonDocument.Parse(File.ReadAllText(Path.Combine(stagingRoot, "SHA256SUMS.json"))))
        {
            foreach (var item in check.RootElement.GetProperty("files").EnumerateObject())
            {
                if (!validatedHashes.TryGetValue(item.Name, out string? hash) || !hash.Equals(item.Value.GetString(), StringComparison.Ordinal))
                    throw new InvalidOperationException("BUSINESS_INPUT_SHA256_MANIFEST_INVALID");
            }
        }

        Directory.CreateDirectory(outputDirectory);
        foreach (var fileName in files.Keys.Append("block4_business_input_summary.json"))
        {
            string staged = Path.Combine(stagingRoot, fileName);
            string target = Path.Combine(outputDirectory, fileName);
            string publishTemp = target + ".publishing." + result.RunId;
            File.Copy(staged, publishTemp, overwrite: false);
            File.Move(publishTemp, target, overwrite: true);
        }
        string manifestTemp = Path.Combine(outputDirectory, "SHA256SUMS.json.publishing." + result.RunId);
        File.Copy(Path.Combine(stagingRoot, "SHA256SUMS.json"), manifestTemp, overwrite: false);
        File.Move(manifestTemp, Path.Combine(outputDirectory, "SHA256SUMS.json"), overwrite: true);
        return validatedHashes;
    }

    private static BusinessCreationRuleEvidenceRow Evidence(string id, string value, string type, string source, string symbol, bool verified, string notes) => new(id, value, type, source, symbol, verified, notes);
    private static string RuleEvidence(BusinessCreationRules rules, string id) => string.Join(";", rules.Evidence.Where(e => e.RuleId == id).Select(e => e.SourceFileOrDdl + ":" + e.SourceSymbol));
    private static readonly string[] RequestHeaders = ["REQUEST_KEY", "RISK_CODE", "RISK_NO", "AREA", "FIELD_NUMBER", "FIELD_LABEL", "ENTITY_TYPE", "ENTITY_ORDINAL", "PARENT_ORDINAL", "ENTITY_DESCRIPTION", "ATTRIBUTE", "DB_COLUMN", "MISSING_INFORMATION", "ACCEPTED_VALUES_IF_CATALOG", "WHY_REQUIRED", "DEPENDENCY_REQUEST_KEY", "BUSINESS_OWNER", "BUSINESS_RESPONSE", "BUSINESS_NOTES"];
    private static readonly string[] QualificationHeaders = ["QUALIFICATION_KEY", "RISK_CODE", "FIELD_NUMBER", "ENTITY_TYPE", "ENTITY_ORDINAL", "PARENT_ORDINAL", "SOURCE_RAW", "SOURCE_PARSED", "DB_MATCH", "ATTRIBUTE", "DB_COLUMN", "REQUIRED", "DB_DEFAULT", "WORKBOOK_SOURCE", "SYSTEM_RULE", "SYSTEM_RULE_EVIDENCE", "CURRENT_DB_SOURCE", "HUMAN_INPUT_REQUIRED", "STATUS", "REASON"];
    private static readonly string[] ParsingHeaders = ["RISK_CODE", "RISK_NO", "FIELD_NUMBER", "FIELD_LABEL", "RAW_CELL", "PARSED_COUNT", "ITEM_ORDINAL", "PARENT_ORDINAL", "PARSED_ITEM_TEXT", "PARSE_RULE", "AMBIGUOUS", "EVIDENCE"];
    private static readonly string[] CreationRuleHeaders = ["RULE_ID", "EXPECTED_VALUE", "SOURCE_TYPE", "SOURCE_FILE_OR_DDL", "SOURCE_SYMBOL", "VERIFIED", "NOTES"];
    private static readonly string[] Field45Headers = ["RISK_CODE", "RISK_NO", "SOURCE_RAW", "PARSED_RESPONSIBLE_COUNT", "SOURCE_PLAN_COUNT", "EXISTING_PLAN_COUNT", "STATUS", "DETAIL"];
    private static readonly string[] DependencyHeaders = ["BLOCKER_ID", "ENTITY_TYPE", "ENTITY_KEY", "DEPENDS_ON_BLOCKER_ID", "ROOT_CAUSE", "MISSING_ATTRIBUTE", "SOURCE_AVAILABLE", "HUMAN_INPUT_REQUIRED"];

    private static string[] RequestValues(BusinessInputRequestRow r) => [r.RequestKey,r.RiskCode,r.RiskNo.ToString(CultureInfo.InvariantCulture),r.Area,r.FieldNumber.ToString(CultureInfo.InvariantCulture),r.FieldLabel,r.EntityType,r.EntityOrdinal.ToString("D3",CultureInfo.InvariantCulture),r.ParentOrdinal?.ToString("D3",CultureInfo.InvariantCulture) ?? "",r.EntityDescription,r.Attribute,r.DbColumn,r.MissingInformation,r.AcceptedValuesIfCatalog,r.WhyRequired,r.DependencyRequestKey,r.BusinessOwner,r.BusinessResponse,r.BusinessNotes];
    private static string[] QualificationValues(BusinessQualificationRow r) => [r.QualificationKey,r.RiskCode,r.FieldNumber.ToString(CultureInfo.InvariantCulture),r.EntityType,r.EntityOrdinal.ToString("D3",CultureInfo.InvariantCulture),r.ParentOrdinal?.ToString("D3",CultureInfo.InvariantCulture) ?? "",r.SourceRaw,r.SourceParsed,r.DbMatch,r.Attribute,r.DbColumn,r.Required.ToString().ToUpperInvariant(),r.DbDefault,r.WorkbookSource,r.SystemRule,r.SystemRuleEvidence,r.CurrentDbSource,r.HumanInputRequired.ToString().ToUpperInvariant(),r.Status,r.Reason];
    private static string[] ParsingValues(BusinessParsingAuditRow r) => [r.RiskCode,r.RiskNo.ToString(CultureInfo.InvariantCulture),r.FieldNumber.ToString(CultureInfo.InvariantCulture),r.FieldLabel,r.RawCell,r.ParsedCount.ToString(CultureInfo.InvariantCulture),r.ItemOrdinal.ToString(CultureInfo.InvariantCulture),r.ParentOrdinal?.ToString(CultureInfo.InvariantCulture) ?? "",r.ParsedItemText,r.ParseRule,r.Ambiguous.ToString().ToUpperInvariant(),r.Evidence];
    private static string[] CreationRuleValues(BusinessCreationRuleEvidenceRow r) => [r.RuleId,r.ExpectedValue,r.SourceType,r.SourceFileOrDdl,r.SourceSymbol,r.Verified.ToString().ToUpperInvariant(),r.Notes];
    private static string[] Field45Values(BusinessField45ReconciliationRow r) => [r.RiskCode,r.RiskNo.ToString(CultureInfo.InvariantCulture),r.SourceRaw,r.ParsedResponsibleCount.ToString(CultureInfo.InvariantCulture),r.SourcePlanCount.ToString(CultureInfo.InvariantCulture),r.ExistingPlanCount.ToString(CultureInfo.InvariantCulture),r.Status,r.Detail];
    private static string[] DependencyValues(BusinessBlockerDependencyRow r) => [r.BlockerId,r.EntityType,r.EntityKey,r.DependsOnBlockerId,r.RootCause,r.MissingAttribute,r.SourceAvailable.ToString().ToUpperInvariant(),r.HumanInputRequired.ToString().ToUpperInvariant()];
    private static byte[] Csv(string[] headers, IEnumerable<string[]> rows)
    {
        var sb = new StringBuilder(); sb.AppendLine(string.Join(",", headers.Select(EscapeCsv)));
        foreach (var row in rows) sb.AppendLine(string.Join(",", row.Select(EscapeCsv)));
        return new UTF8Encoding(false).GetBytes(sb.ToString());
    }
    private static string EscapeCsv(string? value) => "\"" + (value ?? "").Replace("\"", "\"\"", StringComparison.Ordinal).Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n') + "\"";
    private static string Sha256(byte[] data) => Convert.ToHexString(SHA256.HashData(data)).ToLowerInvariant();
    private static string SafeTextKey(string? value) => Regex.Replace((value ?? "").Normalize(NormalizationForm.FormC).Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n'), @"\s+", " ", RegexOptions.CultureInvariant).Trim();
    private static string Cell(JsonElement row, int field) => row.GetProperty("cells")[field - 1].GetProperty("textValue").GetString()?.Trim() ?? "";
    private static string Label(IReadOnlyList<JsonElement> fields, int field) => fields[field - 1].GetProperty("label").GetString() ?? $"Field {field}";
    private static bool IsNoControl(string value) => new[] { "no hay", "nohay", "inexistente", "0", "0%", "ninguno" }.Contains(value.Trim().ToLowerInvariant(), StringComparer.Ordinal);

    private static Dictionary<string, string> BuildCatalogMap(JsonElement root, string catalogId)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var catalog in root.GetProperty("catalogs").EnumerateArray().Where(c => c.GetProperty("catalogId").GetString() == catalogId))
        foreach (var item in catalog.GetProperty("items").EnumerateArray())
        {
            string key = item.GetProperty("key").GetString() ?? "";
            result[key] = key;
            if (item.TryGetProperty("label", out var label)) result[label.GetString() ?? ""] = key;
            if (item.TryGetProperty("aliases", out var aliases) && aliases.ValueKind == JsonValueKind.Array)
                foreach (var alias in aliases.EnumerateArray()) result[alias.GetString() ?? ""] = key;
        }
        return result;
    }
    private static string? Canonicalize(Dictionary<string, string> catalog, string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "";
        string trimmed = value.Trim(); return catalog.TryGetValue(trimmed, out var key) ? key : null;
    }
    private static string RequestKey(string riskCode, int field, string entityType, int ordinal, string attribute) => $"{riskCode}|F{field:D2}|{entityType}|{ordinal:D3}|{attribute}";
    private static BusinessInputRequestRow Request(string risk, int no, string area, int field, string label, string entity, int ordinal, int? parent, string description, string attr, string column, string missing, string why, string accepted, string dependency, string owner) =>
        new(RequestKey(risk, field, entity, ordinal, attr), risk, no, area, field, label, entity, ordinal, parent, description, attr, column, missing, accepted, why, dependency, owner);
    private static BusinessQualificationRow Qual(string risk, int field, string entity, int ordinal, int? parent, string raw, string parsed, string match, string attr, string column, bool required, string dbDefault, string workbook, string systemRule, string ruleEvidence, string currentDb, bool human, string status, string reason) =>
        new(RequestKey(risk, field, entity, ordinal, string.IsNullOrWhiteSpace(attr) ? "STATUS" : attr), risk, field, entity, ordinal, parent, raw, parsed, match, attr, column, required, dbDefault, workbook, systemRule, ruleEvidence, currentDb, human, status, reason);
    private static void AddQualificationRequest(List<BusinessQualificationRow> all, List<BusinessQualificationRow> missing, BusinessInputRequestRow request, string raw, string parsed, string reason, BusinessCreationRules rules)
    {
        var q = Qual(request.RiskCode, request.FieldNumber, request.EntityType, request.EntityOrdinal, request.ParentOrdinal, raw, parsed, "MISSING", request.Attribute, request.DbColumn, true, "", "MATRIZ_CONSOLIDADA", "", "", "", true, "HUMAN_INPUT_REQUIRED", reason);
        all.Add(q); missing.Add(q);
    }
    private static void AddAudit(List<BusinessParsingAuditRow> audit, string risk, int no, int field, string label, string raw, SourceItemsParseResult parsed, int itemIndex = -1, string evidence = "")
    {
        if (parsed.Items.Count == 0) { audit.Add(new(risk,no,field,label,raw,0,0,null,"",parsed.ParseRule,parsed.IsAmbiguous,evidence)); return; }
        IEnumerable<int> indices = itemIndex >= 0 ? [itemIndex] : Enumerable.Range(0, parsed.Items.Count);
        foreach (int i in indices)
            audit.Add(new(risk,no,field,label,raw,parsed.Items.Count,i+1,i<parsed.ParentOrdinals.Count?parsed.ParentOrdinals[i]:null,parsed.Items[i],parsed.ParseRule,parsed.IsAmbiguous,evidence));
    }
    private static object Summary(BusinessInputQualificationResult r) => new
    {
        r.RunId, r.GeneratorGitSha, r.WorkbookSha256, headerCount=r.HeaderCount, riskCount=r.RiskCount, uniqueRiskCodeCount=r.UniqueRiskCodeCount,
        risksMatched=r.RisksMatched, evaluationsMatched=r.EvaluationsMatched, projectionsMatched=r.ProjectionsMatched,
        controlSourceCells=r.ControlSourceCells, controlSourceItems=r.ControlSourceItems, preventiveItems=r.PreventiveItems, detectiveItems=r.DetectiveItems, correctiveItems=r.CorrectiveItems,
        planSourceCells=r.PlanSourceCells, planSourceItems=r.PlanSourceItems, activitySourceCells=r.ActivitySourceCells, activitySourceItems=r.ActivitySourceItems,
        parserAmbiguities=r.ParserAmbiguities, humanInputEntities=r.HumanInputEntities, humanInputAttributes=r.HumanInputAttributes,
        unexplainedQualificationRows=r.UnexplainedQualificationRows, requestRows=r.RequestRows.Count, duplicateRequestKeys=r.DuplicateRequestKeys,
        emptyRequestKeys=r.EmptyRequestKeys, nonemptyBusinessResponses=r.NonemptyBusinessResponses, previousReportedHumanAttributes=PreviousReportedAttributeCount,
        requestRowCountParity=r.RequestRows.Count==PreviousReportedAttributeCount, databaseIdentity=new { r.DatabaseName,r.ServiceName,r.InstanceName,r.CurrentSchema,r.SessionUser },
        productionDmlExecuted=0, productionDdlExecuted=0, productionApplyExecuted=false, block5Status="NOT_STARTED"
    };
}
