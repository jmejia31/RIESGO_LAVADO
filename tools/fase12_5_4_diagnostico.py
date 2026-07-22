from __future__ import annotations

import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUTPUT = ROOT / "docs/3. Módulo Matrices de Riesgos/Fase 12 - Mejora ejecutiva UXUI y mapa de calor/Evidencia_Fase_12_5_4/fase12_5_4_diagnostico.json"

TEXT_EXTENSIONS = {
    ".cs", ".ts", ".html", ".scss", ".css", ".json", ".md", ".txt", ".sql",
    ".yml", ".yaml", ".xml", ".ps1", ".py", ".csproj", ".sln"
}
EXCLUDED_PARTS = {
    ".git", "node_modules", "bin", "obj", "coverage", "dist", "test-results",
    "playwright-report", ".angular", ".vs", ".idea"
}

RECALCULAR_PATTERNS = {
    "ruta_http": re.compile(r"/recalcular\b", re.IGNORECASE),
    "metodo_frontend": re.compile(r"\brecalcular\s*\(", re.IGNORECASE),
    "simbolo_backend": re.compile(r"\bRecalcular\b"),
    "bandera_recalculo": re.compile(r"\besRecalculo\b")
}
MOJIBAKE_TOKENS = ["Ã", "Â", "â€", "â€™", "â€œ", "â€\u009d", "ï¿½", "�"]


def corregir_verificador_aplicacion() -> None:
    """Ajusta el control para detectar la ruta HTTP real, no su comentario documental."""
    path = ROOT / "tools/fase12_5_4_aplicar.py"
    text = path.read_text(encoding="utf-8")
    old = '"controller_endpoint_removed": "/recalcular" not in (ROOT / "backend/RL.API/Features/MatricesRiesgos/MatricesRiesgosController.cs").read_text(encoding="utf-8"),'
    new = '"controller_endpoint_removed": \'[HttpPost("{id:long}/recalcular")]\' not in (ROOT / "backend/RL.API/Features/MatricesRiesgos/MatricesRiesgosController.cs").read_text(encoding="utf-8"),'
    if old in text:
        path.write_text(text.replace(old, new, 1), encoding="utf-8")
    elif new not in text:
        raise RuntimeError("No se pudo localizar la verificación del endpoint público.")


def is_text_file(path: Path) -> bool:
    return path.is_file() and path.suffix.lower() in TEXT_EXTENSIONS and not any(part in EXCLUDED_PARTS for part in path.parts)


def relative(path: Path) -> str:
    return path.relative_to(ROOT).as_posix()


def classify(path: Path) -> str:
    rel = relative(path).lower()
    if rel.startswith("docs/"):
        return "documentacion_historica"
    if "/tests/" in rel or rel.endswith(".spec.ts") or rel.endswith(".tests.cs") or rel.endswith(".test.ts"):
        return "pruebas"
    return "codigo_activo"


def scan_occurrences(path: Path, text: str, patterns: dict[str, re.Pattern[str]]) -> list[dict[str, object]]:
    found: list[dict[str, object]] = []
    for line_number, line in enumerate(text.splitlines(), start=1):
        for name, pattern in patterns.items():
            if pattern.search(line):
                found.append({
                    "archivo": relative(path),
                    "clasificacion": classify(path),
                    "linea": line_number,
                    "tipo": name,
                    "texto": line.strip()[:500]
                })
    return found


def scan_mojibake(path: Path, text: str) -> list[dict[str, object]]:
    found: list[dict[str, object]] = []
    for line_number, line in enumerate(text.splitlines(), start=1):
        tokens = sorted({token for token in MOJIBAKE_TOKENS if token in line})
        if tokens:
            found.append({
                "archivo": relative(path),
                "clasificacion": classify(path),
                "linea": line_number,
                "tokens": tokens,
                "texto": line.strip()[:500]
            })
    return found


def main() -> None:
    corregir_verificador_aplicacion()

    recalcular: list[dict[str, object]] = []
    mojibake: list[dict[str, object]] = []
    todos: list[str] = []

    for path in ROOT.rglob("*"):
        if not is_text_file(path):
            continue
        todos.append(relative(path))
        try:
            text = path.read_text(encoding="utf-8")
        except UnicodeDecodeError:
            continue
        recalcular.extend(scan_occurrences(path, text, RECALCULAR_PATTERNS))
        mojibake.extend(scan_mojibake(path, text))

    consumidores_recalcular = [item for item in recalcular if item["clasificacion"] == "codigo_activo"]
    pruebas_recalcular = [item for item in recalcular if item["clasificacion"] == "pruebas"]
    historico_recalcular = [item for item in recalcular if item["clasificacion"] == "documentacion_historica"]
    mojibake_activo = [item for item in mojibake if item["clasificacion"] == "codigo_activo"]
    mojibake_pruebas = [item for item in mojibake if item["clasificacion"] == "pruebas"]
    mojibake_historico = [item for item in mojibake if item["clasificacion"] == "documentacion_historica"]

    result = {
        "fase": "12.5.4",
        "estado": "diagnostico_inicial",
        "archivos_texto_analizados": len(todos),
        "recalcular": {
            "codigo_activo": consumidores_recalcular,
            "pruebas": pruebas_recalcular,
            "documentacion_historica": historico_recalcular,
            "total": len(recalcular)
        },
        "caracteres_danados": {
            "codigo_activo": mojibake_activo,
            "pruebas": mojibake_pruebas,
            "documentacion_historica": mojibake_historico,
            "total": len(mojibake)
        },
        "criterios_decision": {
            "puede_retirarse_endpoint_publico": len(consumidores_recalcular) > 0,
            "requiere_revision_consumidores": True,
            "corregir_codigo_activo": len(mojibake_activo) > 0,
            "preservar_evidencia_historica_sin_cambio_masivo": True
        }
    }

    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    OUTPUT.write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({
        "archivos": len(todos),
        "recalcular_activo": len(consumidores_recalcular),
        "recalcular_pruebas": len(pruebas_recalcular),
        "mojibake_activo": len(mojibake_activo),
        "mojibake_historico": len(mojibake_historico),
        "salida": relative(OUTPUT)
    }, ensure_ascii=False))


if __name__ == "__main__":
    main()
