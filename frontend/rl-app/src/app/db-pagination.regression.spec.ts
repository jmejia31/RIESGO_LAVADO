// @ts-ignore: Vitest ejecuta este archivo en Node.js.
import { readdirSync, readFileSync, statSync } from 'node:fs';
// @ts-ignore: Vitest ejecuta este archivo en Node.js.
import { join } from 'node:path';
// @ts-ignore: Vitest ejecuta este archivo en Node.js.
import { cwd } from 'node:process';
import { describe, expect, it } from 'vitest';

const appRoot = join(cwd(), 'src', 'app');

describe('DB pagination regression contract', () => {
  it('does not reintroduce client-side pagination in certified business grids', () => {
    const targets = [
      'features/admin/bitacora/pages/bitacora/bitacora.component.ts',
      'features/admin/usuarios/pages/usuarios/usuarios.component.ts',
      'features/admin/listas/pages/monitoreo-listas/monitoreo-listas.component.impl.ts',
      'features/admin/listas/pages/coincidencias-empleado/coincidencias-empleado.component.ts',
      'features/admin/listas/pages/coincidencias-patrono/coincidencias-patrono.component.ts',
      'features/admin/matrices-riesgos/pages/matrices-riesgos/matrices-riesgos.component.ts',
      'features/admin/matrices-riesgos/components/matrices-riesgos-gestion/matrices-riesgos-gestion.component.ts'
    ];

    for (const relative of targets) {
      const source = readFileSync(join(appRoot, relative), 'utf8');
      expect(source, relative).not.toMatch(/(?:datosFiltrados|resumenFiltrado|detalleFiltrado|familiasFiltradas|consolidadoFiltrado)[\s\S]{0,500}\.slice\s*\(/);
    }
  });

  it('does not classify local report preview pagination as a persistent-grid violation', () => {
    const preview = readFileSync(join(appRoot, 'shared/report-preview/report-preview.component.ts'), 'utf8');
    expect(preview).toContain('rows.slice');
    expect(preview).toContain('pageSize');
  });

  it('keeps certified grids on explicit paged service contracts without full-list fallback', () => {
    const matrices = readFileSync(join(appRoot, 'features/admin/matrices-riesgos/pages/matrices-riesgos/matrices-riesgos.component.ts'), 'utf8');
    const monitoring = readFileSync(join(appRoot, 'features/admin/listas/pages/monitoreo-listas/monitoreo-listas.component.impl.ts'), 'utf8');
    const users = readFileSync(join(appRoot, 'features/admin/usuarios/pages/usuarios/usuarios.component.ts'), 'utf8');

    expect(matrices).toContain('listarFamiliasFormularioPaginadas');
    expect(matrices).toContain('obtenerConsolidadoPaginado');
    expect(matrices).toContain('listarRiesgosPaginados(false, 1, 200)');
    expect(matrices).not.toContain('this.service.listarRiesgos()');
    expect(matrices).not.toMatch(/this\.service\.obtenerConsolidado\(\)/);
    expect(monitoring).toContain('getJuridicasPaginadas');
    expect(monitoring).toContain('getNaturalesPaginadas');
    expect(monitoring).toContain('getEmpleadosPaginadas');
    expect(monitoring).toContain('getJuridicasParaExportar');
    expect(monitoring).toContain('getNaturalesParaExportar');
    expect(monitoring).toContain('getEmpleadosParaExportar');
    expect(monitoring).not.toContain('precargarTiposRestantes');
    expect(users).toContain('/auth/usuarios/paginado');
    expect(users).not.toContain('get(`${environment.apiUrl}/auth/usuarios`)');

    const riskGrid = readFileSync(join(appRoot, 'features/admin/matrices-riesgos/components/matrices-riesgos-gestion/matrices-riesgos-gestion.component.ts'), 'utf8');
    expect(riskGrid).toContain('listarRiesgosPaginados');
    expect(riskGrid).not.toContain('listarRiesgos(true)');
  });

  it('uses server metadata for totals instead of the visible page length', () => {
    const matricesTemplate = readFileSync(join(appRoot, 'features/admin/matrices-riesgos/pages/matrices-riesgos/matrices-riesgos.component.html'), 'utf8');
    const monitoringTemplate = readFileSync(join(appRoot, 'features/admin/listas/pages/monitoreo-listas/monitoreo-listas.component.html'), 'utf8');

    expect(matricesTemplate).toContain('totalRegistrosFamilias()');
    expect(matricesTemplate).not.toMatch(/de <strong[^>]*>\{\{ familiasFiltradas\(\)\.length \}\}/);
    expect(monitoringTemplate).toContain('juridicasTotales().totalRegistros');
    expect(monitoringTemplate).toContain('naturalesTotales().totalRegistros');
    expect(monitoringTemplate).toContain('empleadosTotales().totalRegistros');
  });

  it('keeps the local artifact exception explicit and prevents unbounded export pagination hacks', () => {
    const appFiles = collectFiles(appRoot, '.ts').filter(file => !file.endsWith('.spec.ts'));
    const productionSource = appFiles.map(file => readFileSync(file, 'utf8')).join('\n');
    expect(productionSource).not.toMatch(/(?:tamanoPagina|registrosPorPagina|pageSize)\s*[:=]\s*999999/i);
    expect(readFileSync(join(appRoot, 'shared/report-preview/report-preview.component.ts'), 'utf8')).toContain('rows.slice');
  });
});

function collectFiles(root: string, extension: string): string[] {
  const files: string[] = [];
  for (const entry of readdirSync(root)) {
    const path = join(root, entry);
    if (statSync(path).isDirectory()) files.push(...collectFiles(path, extension));
    else if (path.endsWith(extension)) files.push(path);
  }
  return files;
}
