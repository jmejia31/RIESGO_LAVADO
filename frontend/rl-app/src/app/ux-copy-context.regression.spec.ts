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

function productionSources(appRoot: string): Array<{ path: string; source: string }> {
  const sources = collectFiles(appRoot, '.html').map(path => ({ path, source: readFileSync(path, 'utf8') }));
  for (const path of collectFiles(appRoot, '.ts').filter(path => !path.endsWith('.spec.ts'))) {
    const source = readFileSync(path, 'utf8');
    for (const match of source.matchAll(/\btemplate\s*:\s*`([\s\S]*?)`/g)) {
      sources.push({ path: `${path}#inline-template`, source: match[1] });
    }
  }
  return sources;
}

const APP_ROOT = join(cwd(), 'src', 'app');
const PRODUCTIVE_SOURCES = productionSources(APP_ROOT);

const GENERIC_COPY_RULES: Array<{ name: string; pattern: RegExp }> = [
  { name: 'generic empty state', pattern: /\bno hay registros disponibles\b|\bno hay datos\b|\bsin informaci(?:ón|Ã³n)\b|\bsin registros\s*[.!]?\s*(?:<\/|$)/gi },
  { name: 'generic detail tooltip', pattern: /\bver detalle del registro\b|\bver detalle completo\b/gi },
  { name: 'generic record description', pattern: /\binformaci(?:ón|Ã³n) del registro\b|\bdetalles del registro\b|\blistado de registros\b/gi }
];

describe('copy UX contextualizado', () => {
  it('no reintroduce descripciones productivas genéricas conocidas', () => {
    const genericPhrases = [
      'Gestione la información del sistema.',
      'Administre los registros.',
      'Información del módulo.',
      'Consulte la información disponible.',
      'Realice las operaciones necesarias.',
      'Listado de registros.',
      'Detalles del registro.',
      'Complete los campos.',
      'Ingrese la información.',
      'Edite la información seleccionada.',
      'Buscar...'
    ];
    const violations: string[] = [];
    for (const { path, source } of PRODUCTIVE_SOURCES) {
      for (const phrase of genericPhrases) {
        if (source.includes(phrase)) violations.push(`${relative(cwd(), path)}: ${phrase}`);
      }
    }
    expect(violations).toEqual([]);
  });

  it('rechaza estados, tooltips y ayudas genéricas cuando el contexto funcional está disponible', () => {
    const violations: string[] = [];
    for (const { path, source } of PRODUCTIVE_SOURCES) {
      for (const rule of GENERIC_COPY_RULES) {
        rule.pattern.lastIndex = 0;
        if (rule.pattern.test(source)) violations.push(`${relative(cwd(), path)}: ${rule.name}`);
      }
    }
    expect(violations).toEqual([]);
  });

  it('conserva copy contextual en las superficies UX principales', () => {
    const read = (relativePath: string) => readFileSync(join(APP_ROOT, relativePath), 'utf8');
    const matrices = read('features/admin/matrices-riesgos/pages/matrices-riesgos/matrices-riesgos.component.html');
    const familia = read('features/admin/matrices-riesgos/components/familia-detalle-modal/familia-detalle-modal.component.html');
    const monitoreo = read('features/admin/listas/pages/monitoreo-listas/monitoreo-listas.component.html');
    expect(matrices).toContain('Gestione las familias institucionales de formularios, su estado y acceso a versiones.');
    expect(matrices).toContain('Seleccione la familia y el riesgo institucional para generar la evaluación.');
    expect(familia).toContain('Identificación y descripción');
    expect(monitoreo).toContain('Revise la información, coincidencias y trazabilidad del sujeto.');
  });

  it('usa placeholders de búsqueda específicos para las coincidencias', () => {
    for (const file of [
      'features/admin/listas/pages/coincidencias-empleado/coincidencias-empleado.component.html',
      'features/admin/listas/pages/coincidencias-patrono/coincidencias-patrono.component.html'
    ]) {
      const source = readFileSync(join(APP_ROOT, file), 'utf8');
      expect(source).toContain('Buscar por nombre o número de identificación...');
      expect(source).toContain('Buscar en el detalle de la coincidencia...');
    }
  });
});
