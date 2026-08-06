from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[2]
VALIDATOR = ROOT / "scripts/validation/validate_matrices_dynamic_ddl_alignment.ps1"
WORKFLOW = ROOT / ".github/workflows/phase7-fix-validator.yml"
SELF = Path(__file__).resolve()

content = VALIDATOR.read_text(encoding="utf-8")

old_trace = "$traceFiles = Get-SourceFiles -Roots $traceScanRoots -Extensions $moduleExtensions"
new_trace = """$traceFiles = @(
    Get-SourceFiles -Roots $traceScanRoots -Extensions $moduleExtensions | Where-Object {
        $_.FullName -ne $oracleIntegrationTest
    }
)"""
if content.count(old_trace) != 1:
    raise RuntimeError("No se encontró exactamente una asignación de traceFiles.")
content = content.replace(old_trace, new_trace, 1)

new_oracle_block = r'''if (Test-Path -LiteralPath $oracleIntegrationTest) {
    $content = Get-Content -LiteralPath $oracleIntegrationTest -Raw
    foreach ($token in @(
        'RL_ORACLE_INTEGRATION_REQUIRED',
        'TipoEntidadEvidencia.Evaluacion',
        'RL_MR_EVIDENCIAS_VINCULOS',
        'RL_AUDITORIA',
        'SEQ_RL_AUDITORIA',
        'AuditoriaFallaDespuesDeInsertar',
        'TablasModelo17',
        'SecuenciasModelo17',
        'IndicesPrincipales',
        'RestriccionesPrincipales',
        'RIE_NOMBRE',
        'RIE_USR_CREACION',
        'EsquemaModelo17_InventarioIndicesRestriccionesYAusencias_CumplenContrato',
        'CicloCompleto_Commit_PersisteFamiliaVersionRiesgoEvaluacionProyeccionFlujoEvidenciaVinculoYAuditoria',
        'CicloCompleto_Rollback_NoPersisteRegistrosBase')) {
        if (-not $content.Contains($token)) {
            $errors.Add("La suite Oracle del modelo reducido no contiene el control obligatorio '$token'.")
        }
    }

    $retiredOracleObjects = @(
        'RL_MR_EVI_APROBACION',
        'RL_MR_EVI_REVISION',
        'RL_MR_EVI_AUTOMONITOREO',
        'RL_MR_EVI_ALERTA',
        'RL_MR_EVI_ACTIVIDAD',
        'RL_MR_EVI_PLAN',
        'RL_MR_EVI_CONTROL',
        'RL_MR_EVI_EVALUACION',
        'RL_MR_EVI_RIESGO',
        'RL_MR_DETALLES_IMPORTACION',
        'RL_MR_LOTES_IMPORTACION',
        'RL_MR_TRAZAS_CALCULO',
        'RL_MR_AUDITORIA',
        'RL_MR_PERMISOS_FORMULARIO',
        'RL_MR_APROBACIONES_FORMULARIO',
        'RL_MR_CAMPOS_FORMULARIO',
        'RL_MR_RELACIONES_RIESGO',
        'RL_MR_REVISIONES_EVALUACION',
        'SEQ_RL_MR_AUDITORIA',
        'SEQ_RL_MR_TRAZAS',
        'SEQ_RL_MR_REVISIONES')

    foreach ($token in $retiredOracleObjects) {
        $escaped = [regex]::Escape($token)
        $activeSqlPattern = "(?im)\b(?:INSERT\s+INTO|UPDATE|DELETE\s+FROM|MERGE\s+INTO|FROM)\s+$escaped\b"
        if ($content -match $activeSqlPattern) {
            $errors.Add("La suite Oracle ejecuta SQL activo contra el objeto heredado '$token'.")
        }
    }

    if ($content.Contains('TRA_REGLA_ID')) {
        $errors.Add("La suite Oracle reintroduce la columna heredada 'TRA_REGLA_ID'.")
    }
}

'''
pattern = re.compile(
    r"if \(Test-Path -LiteralPath \$oracleIntegrationTest\) \{.*?\n\}\n\nif \(Test-Path -LiteralPath \$programFile\) \{",
    re.S,
)
match = pattern.search(content)
if not match:
    raise RuntimeError("No se encontró el bloque Oracle del validador.")
content = content[:match.start()] + new_oracle_block + "if (Test-Path -LiteralPath $programFile) {" + content[match.end():]

VALIDATOR.write_text(content, encoding="utf-8")

for temporary in (WORKFLOW, SELF):
    if temporary.exists():
        temporary.unlink()
