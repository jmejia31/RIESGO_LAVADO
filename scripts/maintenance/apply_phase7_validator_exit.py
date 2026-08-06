from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
VALIDATOR = ROOT / "scripts/validation/validate_matrices_dynamic_ddl_alignment.ps1"
WORKFLOW = ROOT / ".github/workflows/phase7-normalize-validator-exit.yml"
SELF = Path(__file__).resolve()

content = VALIDATOR.read_text(encoding="utf-8").rstrip()
if not content.endswith('Write-Host "Archivos del modulo revisados: $($moduleFiles.Count). Archivos sin trazas revisados: $($traceFiles.Count). Archivos de seguridad revisados: $($securityFiles.Count)." -ForegroundColor Green'):
    raise RuntimeError("El cierre esperado del validador cambió; no se aplicó la normalización.")
if content.endswith("exit 0"):
    raise RuntimeError("El validador ya contiene salida exitosa explícita.")

VALIDATOR.write_text(content + "\nexit 0\n", encoding="utf-8")

for temporary in (WORKFLOW, SELF):
    if temporary.exists():
        temporary.unlink()
