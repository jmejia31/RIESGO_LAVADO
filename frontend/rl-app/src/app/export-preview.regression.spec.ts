import { describe, expect, it } from 'vitest';
// @ts-ignore: Vitest ejecuta este archivo en Node.js.
import { readdirSync, readFileSync, statSync } from 'node:fs';
// @ts-ignore: Vitest ejecuta este archivo en Node.js.
import { join, relative } from 'node:path';
// @ts-ignore: Vitest ejecuta este archivo en Node.js.
import { cwd } from 'node:process';

function collectFiles(root: string, extension: string): string[] {
  const files: string[] = [];
  for (const entry of readdirSync(root)) {
    const path = join(root, entry);
    if (statSync(path).isDirectory()) files.push(...collectFiles(path, extension));
    else if (path.endsWith(extension)) files.push(path);
  }
  return files;
}

describe('contrato global de vista previa de exportaciones', () => {
  it('no permite descargas directas desde productores de reportes', () => {
    const appRoot = join(cwd(), 'src', 'app');
    const allowed = new Set([
      join(appRoot, 'core', 'utils', 'excel-export.util.ts'),
      join(appRoot, 'shared', 'report-preview', 'report-preview.service.ts')
    ]);
    const violations: string[] = [];
    for (const path of collectFiles(appRoot, '.ts').filter(file => !file.endsWith('.spec.ts'))) {
      if (allowed.has(path)) continue;
      const source = readFileSync(path, 'utf8');
      for (const pattern of [/\bXLSX\.writeFile\s*\(/g, /\bdoc\.save\s*\(/g, /\benlace\.click\s*\(/g]) {
        if (pattern.test(source)) violations.push(`${relative(cwd(), path)}: ${pattern}`);
        pattern.lastIndex = 0;
      }
    }
    expect(violations).toEqual([]);
  });

  it('mantiene un único contrato de preview para los productores de reportes', () => {
    const appRoot = join(cwd(), 'src', 'app');
    const producers = collectFiles(appRoot, '.ts')
      .filter(file => !file.endsWith('.spec.ts'))
      .filter(file => /exportar|imprimir|descargarConsolidado/.test(readFileSync(file, 'utf8')));
    const reportProducers = producers.filter(file => /features[\\/]admin[\\/].*[\\/]pages[\\/]/.test(file));
    const missing = reportProducers.filter(file => {
      const source = readFileSync(file, 'utf8');
      return /XLSX\.utils|jsPDF|descargarConsolidado/.test(source) && !/reportPreview\.open(?:Pdf|Excel|ExcelBlob)/.test(source);
    });
    expect(missing.map(file => relative(cwd(), file))).toEqual([]);
  });
});
