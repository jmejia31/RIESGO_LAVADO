namespace RL.Tools.ReconcileMatricesBaseline;

public sealed class RepresentativeSampleCandidate
{
    public string RiskCode { get; init; } = "";
    public int RiskNo { get; init; }
    public string Area { get; init; } = "";
    public string RiskType { get; init; } = "";
    public int SourceNonemptyFields { get; init; }
    public string ControlProfile { get; init; } = "";
    public bool PreventiveControlPresent { get; init; }
    public bool DetectiveControlPresent { get; init; }
    public bool CorrectiveControlPresent { get; init; }
    public bool ControlSourcePresent { get; init; }
    public bool UnrepresentedSourceControl { get; init; }
    public string ResponseProfile { get; init; } = "";
    public bool AlertPresent { get; init; }
    public bool PlanTextPresent { get; init; }
    public bool ActivityTextPresent { get; init; }
    public bool GticSourcePresent { get; init; }
    public bool CurrentProductionExists { get; init; }
    public int CurrentProductionControlCount { get; init; }
    public int CurrentProductionPlanCount { get; init; }
    public int CurrentProductionActivityCount { get; init; }
    public int CurrentProductionMonitoringCount { get; init; }
    public int HumanInputNeededCount { get; init; }
    public int BlockedCount { get; init; }
    public int AmbiguousCount { get; init; }
    public int PlannedMutationCount { get; init; }
    public bool SampleEligible { get; init; }
    public string RejectionReason { get; init; } = "";
    public string CurrentProductionState { get; init; } = "";
    public string CoverageSummary { get; init; } = "";

    public IReadOnlySet<string> CoverageKeys
    {
        get
        {
            var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string[] profile = ControlProfile.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (string controlType in new[] { "PREVENTIVO", "DETECTIVO", "CORRECTIVO" })
                if (profile.Contains(controlType, StringComparer.OrdinalIgnoreCase))
                    keys.Add($"CONTROL:{controlType}");

            if (profile.Contains("NO_CONTROL", StringComparer.OrdinalIgnoreCase)) keys.Add("CONTROL:NO_CONTROL");
            if (!string.IsNullOrWhiteSpace(ResponseProfile))
                keys.Add($"RESPONSE:{ResponseProfile.Trim()}");
            if (AlertPresent) keys.Add("ALERT_PRESENT");
            if (PlanTextPresent) keys.Add("PLAN_SOURCE_PRESENT");
            if (ActivityTextPresent) keys.Add("ACTIVITY_SOURCE_PRESENT");
            return keys;
        }
    }
}

public static class RepresentativeSampleSelection
{
    public const int TargetSize = 5;
    public const string AnchorRiskCode = "ROP-CUMP-59";

    private static readonly IReadOnlyDictionary<string, int> CoverageWeights = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
    {
        ["CONTROL:PREVENTIVO"] = 8,
        ["CONTROL:DETECTIVO"] = 8,
        ["CONTROL:CORRECTIVO"] = 8,
        ["CONTROL:NO_CONTROL"] = 7,
        ["ALERT_PRESENT"] = 7,
        ["PLAN_SOURCE_PRESENT"] = 3,
        ["ACTIVITY_SOURCE_PRESENT"] = 3
    };

    public static IReadOnlyList<RepresentativeSampleCandidate> SelectFive(
        IReadOnlyCollection<RepresentativeSampleCandidate> candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        var eligible = candidates.Where(candidate => candidate.SampleEligible)
            .OrderBy(candidate => candidate.RiskNo)
            .ThenBy(candidate => candidate.RiskCode, StringComparer.Ordinal)
            .ToList();
        if (eligible.Select(candidate => candidate.RiskCode).Distinct(StringComparer.OrdinalIgnoreCase).Count() < TargetSize)
            throw new InvalidOperationException($"SAMPLE_SELECTION_BLOCKED: eligibleDistinctCodes={eligible.Select(x => x.RiskCode).Distinct(StringComparer.OrdinalIgnoreCase).Count()}; required={TargetSize}.");

        var selected = new List<RepresentativeSampleCandidate>(TargetSize);
        RepresentativeSampleCandidate? anchor = eligible.SingleOrDefault(candidate =>
            candidate.RiskCode.Equals(AnchorRiskCode, StringComparison.OrdinalIgnoreCase));
        if (anchor != null) selected.Add(anchor);

        while (selected.Count < TargetSize)
        {
            var covered = selected.SelectMany(candidate => candidate.CoverageKeys)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            RepresentativeSampleCandidate next = eligible
                .Where(candidate => selected.All(item => !item.RiskCode.Equals(candidate.RiskCode, StringComparison.OrdinalIgnoreCase)))
                .Select(candidate => new
                {
                    Candidate = candidate,
                    Score = GetSelectionScore(candidate, covered)
                })
                .OrderByDescending(item => item.Score)
                .ThenBy(item => item.Candidate.RiskNo)
                .ThenBy(item => item.Candidate.RiskCode, StringComparer.Ordinal)
                .Select(item => item.Candidate)
                .First();
            selected.Add(next);
        }

        return selected;
    }

    public static int GetSelectionScore(RepresentativeSampleCandidate candidate, IReadOnlySet<string>? alreadyCovered = null)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        alreadyCovered ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        int coverageScore = candidate.CoverageKeys.Where(key => !alreadyCovered.Contains(key))
            .Sum(key => CoverageWeights.TryGetValue(key, out int weight) ? weight : 5);
        int stabilityScore = candidate.PlannedMutationCount == 0 ? 4 : 0;
        return checked((coverageScore * 10_000) + (stabilityScore * 100) + candidate.SourceNonemptyFields);
    }

    public static string ExplainSelection(RepresentativeSampleCandidate candidate, IReadOnlyCollection<RepresentativeSampleCandidate> selectedBefore)
    {
        var alreadyCovered = selectedBefore.SelectMany(item => item.CoverageKeys).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var added = candidate.CoverageKeys.Where(key => !alreadyCovered.Contains(key)).OrderBy(key => key, StringComparer.Ordinal).ToArray();
        var reasons = new List<string>();
        if (candidate.RiskCode.Equals(AnchorRiskCode, StringComparison.OrdinalIgnoreCase)) reasons.Add("anchor ROP-CUMP-59 elegible");
        if (added.Length > 0) reasons.Add("amplía cobertura: " + string.Join("; ", added));
        if (candidate.PlannedMutationCount == 0) reasons.Add("representación productiva sin mutaciones propuestas");
        if (reasons.Count == 0) reasons.Add("completa los cinco casos aptos con el mayor puntaje marginal determinista disponible");
        return string.Join(" | ", reasons);
    }
}
