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

const PAGINATED_SURFACES = [
  ['features/admin/bitacora/pages/bitacora/bitacora.component.html', 1],
  ['features/admin/usuarios/pages/usuarios/usuarios.component.html', 1],
  ['features/admin/listas/pages/coincidencias-empleado/coincidencias-empleado.component.html', 2],
  ['features/admin/listas/pages/coincidencias-patrono/coincidencias-patrono.component.html', 2],
  ['features/admin/listas/pages/monitoreo-listas/monitoreo-listas.component.html', 1],
  ['features/admin/matrices-riesgos/pages/matrices-riesgos/matrices-riesgos.component.html', 3],
  ['features/admin/matrices-riesgos/components/matrices-riesgos-gestion/matrices-riesgos-gestion.component.html', 1],
  ['shared/report-preview/report-preview.component.html', 1]
] as const;

describe('GLOBAL_PAGINATION_DESIGN_SYSTEM_GUARD', () => {
  const appRoot = join(cwd(), 'src', 'app');

  it('audita todos los templates y certifica una única superficie visual por grilla', () => {
    const htmlFiles = collectFiles(appRoot, '.html');
    expect(htmlFiles.length).toBe(28);

    let paginationSurfaces = 0;
    let pageSizeSelectors = 0;
    const violations: string[] = [];

    for (const [relativePath, expectedCount] of PAGINATED_SURFACES) {
      const path = join(appRoot, relativePath);
      const source = readFileSync(path, 'utf8');
      const paginationCount = (source.match(/<app-data-pagination\b/g) ?? []).length;
      const pageSizeCount = (source.match(/<app-page-size-selector\b/g) ?? []).length;
      paginationSurfaces += paginationCount;
      pageSizeSelectors += pageSizeCount;

      if (paginationCount !== expectedCount) violations.push(`${relativePath}: expected ${expectedCount} shared paginator(s), found ${paginationCount}`);
      if (pageSizeCount !== expectedCount) violations.push(`${relativePath}: expected ${expectedCount} shared page-size selector(s), found ${pageSizeCount}`);
      if (/<(?:button|div)[^>]*>\s*(?:Anterior|Siguiente|Pág\.?|Pag\.?)\s*</i.test(source)) {
        violations.push(`${relativePath}: raw textual paginator control`);
      }
      if (/>\s*Por página\s*</i.test(source)) {
        violations.push(`${relativePath}: legacy page-size label`);
      }
      if (/<select\b[^>]*(?:registrosPorPagina|tamanoPagina|pageSize|limite)[^>]*>/i.test(source)) {
        violations.push(`${relativePath}: raw page-size select`);
      }
      const selectorPositions = [...source.matchAll(/<app-page-size-selector\b/g)].map(match => match.index ?? 0);
      const paginatorPositions = [...source.matchAll(/<app-data-pagination\b/g)].map(match => match.index ?? 0);
      if (selectorPositions.some(position => !paginatorPositions.some(paginatorPosition => paginatorPosition > position))) {
        violations.push(`${relativePath}: page-size selector is located after the pagination footer`);
      }
    }

    expect(paginationSurfaces).toBe(12);
    expect(pageSizeSelectors).toBe(12);
    expect(violations).toEqual([]);
  });

  it('mantiene el contrato canónico de iconos, ventana numérica y selector Mostrar', () => {
    const paginator = readFileSync(join(appRoot, 'shared/components/data-pagination/data-pagination.component.ts'), 'utf8');
    const pageSize = readFileSync(join(appRoot, 'shared/components/page-size-selector/page-size-selector.component.ts'), 'utf8');

    expect(paginator).toContain('action="previous"');
    expect(paginator).toContain('action="next"');
    expect(paginator).toContain('aria-current');
    expect(paginator).toContain('type: \'ellipsis\'');
    expect(paginator).not.toContain('overflow-x-auto');
    expect(pageSize).toContain('<span>{{ label }}</span>');
    expect(pageSize).toContain("label = 'Mostrar'");
    expect(pageSize).toContain('focus:ring-ihss-500/20');
  });

  it('no deja plantillas inline productivas con paginadores manuales', () => {
    const inlineTemplates: string[] = [];
    for (const path of collectFiles(appRoot, '.ts').filter(path => !path.endsWith('.spec.ts'))) {
      const source = readFileSync(path, 'utf8');
      for (const match of source.matchAll(/\btemplate\s*:\s*`([\s\S]*?)`/g)) inlineTemplates.push(`${relative(appRoot, path)}:${match[1]}`);
    }
    expect(inlineTemplates.length).toBeGreaterThan(0);
    for (const template of inlineTemplates) {
      expect(template).not.toMatch(/>\s*(?:Anterior|Siguiente|Pág\.?|Pag\.?)\s*</i);
      expect(template).not.toMatch(/>\s*Por página\s*</i);
    }
  });
});
