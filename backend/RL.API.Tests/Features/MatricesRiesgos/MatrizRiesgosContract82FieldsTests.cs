using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace RL.API.Tests.Features.MatricesRiesgos;

public class MatrizRiesgosContract82FieldsTests
{
    private sealed class ManifestField
    {
        public int number { get; set; }
        public int num { get; set; }
        public string col { get; set; } = string.Empty;
        public string label { get; set; } = string.Empty;
        public string canonicalKey { get; set; } = string.Empty;
        public string currentKey { get; set; } = string.Empty;
        public string dataType { get; set; } = string.Empty;
        public string tipoInst { get; set; } = string.Empty;
        public string tipoTec { get; set; } = string.Empty;
        public string source { get; set; } = string.Empty;
        public string origen { get; set; } = string.Empty;
        public string mode { get; set; } = string.Empty;
        public string modo { get; set; } = string.Empty;
        public string requiredness { get; set; } = string.Empty;
        public string obligatoriedad { get; set; } = string.Empty;
        public string conditionality { get; set; } = string.Empty;
        public string condicionalidad { get; set; } = string.Empty;
        public string dbOrEntityMapping { get; set; } = string.Empty;
        public string tablaCol { get; set; } = string.Empty;
        public string dtoApiMapping { get; set; } = string.Empty;
        public string dtoApi { get; set; } = string.Empty;
        public string frontendMapping { get; set; } = string.Empty;
        public string controlFrontendActual { get; set; } = string.Empty;
        public string controlFrontendObjetivo { get; set; } = string.Empty;
        public string importRule { get; set; } = string.Empty;
        public string reglaImport { get; set; } = string.Empty;
        public string exportRule { get; set; } = string.Empty;
        public string reglaExport { get; set; } = string.Empty;
        public string functionalBlock { get; set; } = string.Empty;
        public string bloque { get; set; } = string.Empty;
        public int bloqueNum { get; set; }
        public string formulaDerivacion { get; set; } = string.Empty;
        public string dependencias { get; set; } = string.Empty;
        public string estadoMapping { get; set; } = string.Empty;
        public string evidencia { get; set; } = string.Empty;

        // 1:N Projection Rules
        public string cardinality { get; set; } = string.Empty;
        public string projectionRule { get; set; } = string.Empty;
        public string orderingRule { get; set; } = string.Empty;
        public string displaySeparator { get; set; } = string.Empty;
        public string exportSerialization { get; set; } = string.Empty;
        public string emptyCollectionBehavior { get; set; } = string.Empty;

        // Monitoring Import Rules
        public string initialBaselineImportRule { get; set; } = string.Empty;
        public string subsequentReconciliationRule { get; set; } = string.Empty;
        public bool preserveExistingOperationalValue { get; set; }
        public string excelNullBehavior { get; set; } = string.Empty;

        // Special semantics
        public string semanticScope { get; set; } = string.Empty;
        public string ordinalSource { get; set; } = string.Empty;
        public string ordinalPersistence { get; set; } = string.Empty;
        public string ordinalProjection { get; set; } = string.Empty;
        public string ordinalFallback { get; set; } = string.Empty;
        public string ordinalRule { get; set; } = string.Empty;
        public string ordinalUnresolvedBehavior { get; set; } = string.Empty;

        // Mitigation null semantics & catalog rules (40-49)
        public string catalogSource { get; set; } = string.Empty;
        public string catalogKey { get; set; } = string.Empty;
        public string catalogDisplayValue { get; set; } = string.Empty;
        public string mitigationApplicabilityRule { get; set; } = string.Empty;
        public string nullSemanticsRule { get; set; } = string.Empty;

        // Field 45 frozen attributes
        public string deduplicationRule { get; set; } = string.Empty;
        public string deduplicationKey { get; set; } = string.Empty;
        public string excelSerialization { get; set; } = string.Empty;
        public string pdfSerialization { get; set; } = string.Empty;

        // Controls monitoring semantics (72-80)
        public string monitoringControlCardinality { get; set; } = string.Empty;
        public string controlSelectionSemantics { get; set; } = string.Empty;
        public string stateProjectionSemantics { get; set; } = string.Empty;
        public string effectivenessProjectionSemantics { get; set; } = string.Empty;
        public string evidenceProjectionSemantics { get; set; } = string.Empty;

        // Control absence and combined scale semantics (20-31)
        public string controlDescriptionCardinality { get; set; } = string.Empty;
        public string controlEffectivenessScaleCardinality { get; set; } = string.Empty;
        public string canonicalRepresentationWhenEmpty { get; set; } = string.Empty;
        public string absenceSemanticState { get; set; } = string.Empty;
        public string projectionSemantics { get; set; } = string.Empty;
        public string importTargetSemantics { get; set; } = string.Empty;

        // Evidence representation (74, 77, 80)
        public string evidenceProjection { get; set; } = string.Empty;
        public string excelRepresentation { get; set; } = string.Empty;
        public string pdfRepresentation { get; set; } = string.Empty;
    }

    private ManifestField[] LoadManifest()
    {
        var current = AppContext.BaseDirectory;
        string? manifestPath = null;
        for (int i = 0; i < 6; i++)
        {
            var candidate = Path.Combine(current, "backend", "RL.API", "Features", "MatricesRiesgos", "Contracts", "matriz_riesgos_82_campos_manifest.json");
            if (File.Exists(candidate))
            {
                manifestPath = candidate;
                break;
            }
            var candidateDirect = Path.Combine(current, "..", "..", "..", "..", "RL.API", "Features", "MatricesRiesgos", "Contracts", "matriz_riesgos_82_campos_manifest.json");
            if (File.Exists(candidateDirect))
            {
                manifestPath = Path.GetFullPath(candidateDirect);
                break;
            }
            var parent = Directory.GetParent(current);
            if (parent == null) break;
            current = parent.FullName;
        }

        Assert.NotNull(manifestPath);
        Assert.True(File.Exists(manifestPath), $"Manifest not found at {manifestPath}");

        var json = File.ReadAllText(manifestPath);
        var fields = JsonSerializer.Deserialize<ManifestField[]>(json);
        Assert.NotNull(fields);
        return fields;
    }

    [Fact]
    public void Manifest_MustContainExactly82FieldsInSequence()
    {
        var fields = LoadManifest();
        Assert.Equal(82, fields.Length);

        for (int i = 0; i < 82; i++)
        {
            Assert.Equal(i + 1, fields[i].number);
            Assert.Equal(i + 1, fields[i].num);
        }
    }

    [Fact]
    public void Manifest_MustHaveNoDuplicateCanonicalKeys()
    {
        var fields = LoadManifest();
        var keys = fields.Select(f => f.canonicalKey).ToList();
        var uniqueKeys = keys.Distinct().ToList();

        Assert.Equal(82, uniqueKeys.Count);
    }

    [Fact]
    public void Manifest_MustHaveExactBlockDistribution()
    {
        var fields = LoadManifest();
        var blockCounts = fields.GroupBy(f => f.bloqueNum).ToDictionary(g => g.Key, g => g.Count());

        Assert.Equal(19, blockCounts[1]);
        Assert.Equal(14, blockCounts[2]);
        Assert.Equal(6, blockCounts[3]);
        Assert.Equal(10, blockCounts[4]);
        Assert.Equal(20, blockCounts[5]);
        Assert.Equal(13, blockCounts[6]);
    }

    [Fact]
    public void Manifest_MustHave34CalculatedAnd48NonFormulaFields()
    {
        var fields = LoadManifest();
        var calculated = fields.Where(f => f.mode == "CALCULATED").ToList();
        var nonFormula = fields.Where(f => f.mode != "CALCULATED").ToList();

        Assert.Equal(34, calculated.Count);
        Assert.Equal(48, nonFormula.Count);
    }

    [Fact]
    public void Manifest_MustEnforceCanonicalLabelsForFields08And09()
    {
        var fields = LoadManifest();
        var field08 = fields.First(f => f.number == 8);
        var field09 = fields.First(f => f.number == 9);

        Assert.Equal("Riesgo Inherente", field08.label);
        Assert.Equal("Evaluación", field09.label);

        Assert.Equal("ALIAS_TO_REMEDIATE", field08.estadoMapping);
        Assert.Equal("ALIAS_TO_REMEDIATE", field09.estadoMapping);
    }

    [Fact]
    public void Manifest_MustEnforceGticConditionalityOnFields17_18_19()
    {
        var fields = LoadManifest();
        var gticFields = fields.Where(f => f.number is 17 or 18 or 19).ToList();

        Assert.Equal(3, gticFields.Count);
        foreach (var field in gticFields)
        {
            Assert.Equal("ONLY_GTIC", field.conditionality);
            Assert.Equal("CONDITIONAL", field.mode);
        }
    }

    [Fact]
    public void Manifest_All82FieldsMustHavePopulatedCoreAttributes()
    {
        var fields = LoadManifest();
        foreach (var f in fields)
        {
            Assert.False(string.IsNullOrWhiteSpace(f.label));
            Assert.False(string.IsNullOrWhiteSpace(f.canonicalKey));
            Assert.False(string.IsNullOrWhiteSpace(f.dataType));
            Assert.False(string.IsNullOrWhiteSpace(f.source));
            Assert.False(string.IsNullOrWhiteSpace(f.mode));
            Assert.False(string.IsNullOrWhiteSpace(f.requiredness));
            Assert.False(string.IsNullOrWhiteSpace(f.conditionality));
            Assert.False(string.IsNullOrWhiteSpace(f.dbOrEntityMapping));
            Assert.False(string.IsNullOrWhiteSpace(f.dtoApiMapping));
            Assert.False(string.IsNullOrWhiteSpace(f.frontendMapping));
            Assert.False(string.IsNullOrWhiteSpace(f.importRule));
            Assert.False(string.IsNullOrWhiteSpace(f.exportRule));
            Assert.False(string.IsNullOrWhiteSpace(f.functionalBlock));
        }
    }

    [Fact]
    public void Manifest_RepeaterFieldsMustHaveExplicitProjectionRules()
    {
        var fields = LoadManifest();
        var oneToManyNums = new[] { 20, 24, 28, 40, 42, 45, 70, 74, 77, 80 };
        var repeaters = fields.Where(f => oneToManyNums.Contains(f.number)).ToList();

        Assert.Equal(10, repeaters.Count);
        foreach (var r in repeaters)
        {
            Assert.Equal("ONE_TO_MANY", r.cardinality);
            Assert.False(string.IsNullOrWhiteSpace(r.projectionRule));
            Assert.False(string.IsNullOrWhiteSpace(r.orderingRule));
            Assert.False(string.IsNullOrWhiteSpace(r.displaySeparator));
            Assert.False(string.IsNullOrWhiteSpace(r.exportSerialization));
            Assert.False(string.IsNullOrWhiteSpace(r.emptyCollectionBehavior));
        }
    }

    [Fact]
    public void Manifest_Field01MustHaveExactOrdinalSemantics()
    {
        var fields = LoadManifest();
        var f01 = fields.First(f => f.number == 1);

        Assert.Equal("INSTITUTIONAL_CODE_TO_NO_MAP", f01.ordinalSource);
        Assert.StartsWith("VIRTUAL_DERIVED", f01.ordinalPersistence);
        Assert.Equal("FAIL_CLOSED", f01.ordinalFallback);
        Assert.Contains("ORDINAL_UNRESOLVED", f01.ordinalUnresolvedBehavior);
        Assert.False(string.IsNullOrWhiteSpace(f01.ordinalProjection));
    }

    [Fact]
    public void Manifest_MitigationFieldsMustHaveRefinedNullSemantics()
    {
        var fields = LoadManifest();
        for (int n = 40; n <= 49; n++)
        {
            var fMit = fields.First(f => f.number == n);
            Assert.Equal("MITIGAR", fMit.catalogKey);
            Assert.Equal("Mitigar", fMit.catalogDisplayValue);
            Assert.Contains("APPLIES_IF_RESPONSE_IS_MITIGAR_OR_RESIDUAL_LEVEL_CRITICAL", fMit.mitigationApplicabilityRule);
            Assert.Contains("NOT_APPLICABLE", fMit.nullSemanticsRule);
            Assert.Contains("EMPTY_VALUE", fMit.nullSemanticsRule);
        }
    }

    [Fact]
    public void Manifest_Field45MustHaveFrozenDeduplicationAndSerialization()
    {
        var fields = LoadManifest();
        var f45 = fields.First(f => f.number == 45);

        Assert.Equal("NO_DEDUPLICATION_PER_PLAN (Se preserva la asignación específica de responsables por cada acción de mitigación; no se unifican ni descartan responsables entre distintos planes).", f45.deduplicationRule);
        Assert.DoesNotContain(" o ", f45.deduplicationRule);
        Assert.Equal("PLAN_ORDINAL_INDEX (La clave de asociación es la tupla (PLA_ORDEN, PLA_ID)).", f45.deduplicationKey);
        Assert.False(string.IsNullOrWhiteSpace(f45.excelSerialization));
        Assert.False(string.IsNullOrWhiteSpace(f45.pdfSerialization));
    }

    [Fact]
    public void Manifest_MonitoringControlsMustHaveAllControlsOfTypeSelection()
    {
        var fields = LoadManifest();
        for (int n = 72; n <= 80; n++)
        {
            var fCtrl = fields.First(f => f.number == n);
            Assert.Equal("ONE_TO_MANY_PER_CONTROL_TYPE", fCtrl.monitoringControlCardinality);
            Assert.StartsWith("ALL_CONTROLS_OF_TYPE", fCtrl.controlSelectionSemantics);
            Assert.DoesNotContain("MIN(CON_ID)", fCtrl.controlSelectionSemantics);
            Assert.DoesNotContain("MIN(CON_ID)", fCtrl.importTargetSemantics);
            Assert.StartsWith("MAP_BY_ORDINAL_OR_REJECT_AMBIGUOUS", fCtrl.importTargetSemantics);
            Assert.Contains("MULTILINE_ENUMERATED_PER_CONTROL", fCtrl.projectionSemantics);
        }
    }

    [Fact]
    public void Manifest_Field71MustHaveOperationalPendingDefaultSemantics()
    {
        var fields = LoadManifest();
        var f71 = fields.First(f => f.number == 71);

        Assert.Contains("OPERATIONAL_PENDING", f71.excelNullBehavior);
        Assert.DoesNotContain("asignar 'Vigente'", f71.excelNullBehavior);
    }

    [Fact]
    public void Manifest_EvidencesMustHaveExactNombreArchivoRepresentation()
    {
        var fields = LoadManifest();
        foreach (var n in new[] { 74, 77, 80 })
        {
            var fEvi = fields.First(f => f.number == n);
            Assert.StartsWith("NOMBRE_ARCHIVO_CON_EXTENSION", fEvi.evidenceProjection);
            Assert.False(string.IsNullOrWhiteSpace(fEvi.excelRepresentation));
            Assert.False(string.IsNullOrWhiteSpace(fEvi.pdfRepresentation));
        }
    }

    [Fact]
    public void Manifest_Fields41And43MustHaveEvaluationScope()
    {
        var fields = LoadManifest();
        var f41 = fields.First(f => f.number == 41);
        var f43 = fields.First(f => f.number == 43);

        Assert.Equal("POR_EVALUACION_RIESGO", f41.semanticScope);
        Assert.Contains("COUNT(RL_MR_PLANES)", f41.formulaDerivacion);

        Assert.Equal("POR_EVALUACION_RIESGO", f43.semanticScope);
        Assert.Contains("COUNT(RL_MR_ACTIVIDADES)", f43.formulaDerivacion);
    }

    [Fact]
    public void Manifest_MonitoringFieldsMustHaveRefinedImportRules()
    {
        var fields = LoadManifest();
        var monitoringFields = fields.Where(f => f.number is >= 70 and <= 82).ToList();

        Assert.Equal(13, monitoringFields.Count);
        foreach (var mf in monitoringFields)
        {
            Assert.False(string.IsNullOrWhiteSpace(mf.initialBaselineImportRule));
            Assert.False(string.IsNullOrWhiteSpace(mf.subsequentReconciliationRule));
            Assert.True(mf.preserveExistingOperationalValue);
            Assert.False(string.IsNullOrWhiteSpace(mf.excelNullBehavior));
        }
    }

    [Fact]
    public void Manifest_ControlAbsenceAndCombinedScaleSemantics_MustBeFrozen()
    {
        var fields = LoadManifest();

        foreach (var n in new[] { 20, 24, 28 })
        {
            var f = fields.First(x => x.number == n);
            Assert.Equal("ONE_TO_MANY_RENDERED", f.controlDescriptionCardinality);
            Assert.Equal("No hay", f.canonicalRepresentationWhenEmpty);
            Assert.Equal("NO_CONTROLS_OF_TYPE", f.absenceSemanticState);
        }

        foreach (var n in new[] { 21, 25, 29 })
        {
            var f = fields.First(x => x.number == n);
            Assert.Equal("ONE_COMBINED_SCALE_PER_CONTROL_TYPE", f.controlEffectivenessScaleCardinality);
            Assert.Equal("Inexistente", f.canonicalRepresentationWhenEmpty);
            Assert.Equal("NO_CONTROLS_OF_TYPE", f.absenceSemanticState);
        }

        foreach (var n in new[] { 22, 26, 30 })
        {
            var f = fields.First(x => x.number == n);
            Assert.Equal("0", f.canonicalRepresentationWhenEmpty);
            Assert.Equal("NO_CONTROLS_OF_TYPE", f.absenceSemanticState);
        }

        foreach (var n in new[] { 23, 27, 31 })
        {
            var f = fields.First(x => x.number == n);
            Assert.Equal("0%", f.canonicalRepresentationWhenEmpty);
            Assert.Equal("NO_CONTROLS_OF_TYPE", f.absenceSemanticState);
        }
    }
}
