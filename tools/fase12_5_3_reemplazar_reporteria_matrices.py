from pathlib import Path
import base64
import re
import zlib

root = Path(__file__).resolve().parents[1]
payload_dir = Path(__file__).resolve().parent / 'fase12_5_3_payload'
payload = ''.join(path.read_text(encoding='utf-8').strip() for path in sorted(payload_dir.glob('part*.txt')))
source = zlib.decompress(base64.b64decode(payload)).decode('utf-8')
exec(compile(source, 'fase12_5_3_payload.py', 'exec'))

component = root / 'frontend/rl-app/src/app/features/admin/matrices-riesgos/pages/matrices-riesgos/matrices-riesgos.component.ts'
text = component.read_text(encoding='utf-8')
pattern = r"\n  private generarExcelReporte\(\): void \{.*?\n  private descargarBlob\(blob: Blob, nombre: string\): void \{"
replacement = """

  obtenerMatricesReporte(reporte: MatricesRiesgoReporte | null = this.reporte()): MatrizRiesgoResumen[] {
    return reporte?.matricesFiltradas ?? [];
  }

  private descargarBlob(blob: Blob, nombre: string): void {"""
updated, count = re.subn(pattern, replacement, text, count=1, flags=re.S)
if count != 1:
    raise RuntimeError(f'No se retiró el bloque local heredado: {count}')
component.write_text(updated, encoding='utf-8')

spec = root / 'frontend/rl-app/src/app/features/admin/matrices-riesgos/pages/matrices-riesgos/matrices-riesgos.component.spec.ts'
spec_text = spec.read_text(encoding='utf-8')
old = r"/^Ficha_Matriz_Riesgo_88_\d{4}-\d{2}-\d{2}\.pdf$/"
new = r"/^Ficha_Matriz_Riesgo_88_\d{14}\.pdf$/"
if old not in spec_text:
    raise RuntimeError('No se encontró la expectativa de nombre de ficha a normalizar.')
spec.write_text(spec_text.replace(old, new, 1), encoding='utf-8')
