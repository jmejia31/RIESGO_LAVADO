import { describe, expect, it, vi } from 'vitest';
// @ts-ignore: Vitest ejecuta este archivo en Node.js.
import { mkdirSync, writeFileSync } from 'node:fs';
// @ts-ignore: Vitest ejecuta este archivo en Node.js.
import { resolve } from 'node:path';
import { jsPDF } from 'jspdf';
import {
  agregarEncabezadoInstitucionalPdf,
  agregarPiesInstitucionalesPdf,
  dibujarLogoInstitucionalPdf,
  asegurarEspacioSeccionPdf,
  autoTableInstitucional,
  COLORES_REPORTE_INSTITUCIONAL,
  MARGEN_SUPERIOR_TABLA_CONTINUACION_MM,
  resolverAnchoTablaInstitucional,
  resolverAnchoUtilPdf,
  resolverMargenesTablaInstitucional
} from './institutional-report.util';
import { INSTITUTIONAL_LOGO_ASSET_PATH, INSTITUTIONAL_LOGO_ASPECT_RATIO } from './institutional-report-standard';

describe('estándar institucional de reportería', () => {
  it('usa la paleta institucional aprobada', () => {
    expect(COLORES_REPORTE_INSTITUCIONAL.navy).toEqual([18, 59, 99]);
    expect(COLORES_REPORTE_INSTITUCIONAL.alternate).toEqual([243, 246, 249]);
  });

  it('usa el recurso canónico y conserva las proporciones del logo PDF', () => {
    const doc = {
      setFillColor: vi.fn(),
      rect: vi.fn(),
      addImage: vi.fn()
    } as any;
    vi.stubGlobal('window', {
      document: { createElement: vi.fn() },
      navigator: { userAgent: 'Mozilla/5.0' }
    });
    vi.stubGlobal('document', { createElement: vi.fn() });

    expect(dibujarLogoInstitucionalPdf(doc, 210, 4, 48)).toBe(true);
    expect(doc.addImage).toHaveBeenCalledWith(
      INSTITUTIONAL_LOGO_ASSET_PATH,
      'PNG',
      148,
      4,
      48,
      48 / INSTITUTIONAL_LOGO_ASPECT_RATIO,
      'IHSS_LOGO',
      'FAST'
    );
    vi.unstubAllGlobals();
  });

  it('mantiene el mismo encabezado en orientación vertical y horizontal', () => {
    const portrait = new jsPDF({ orientation: 'portrait', unit: 'mm', format: 'a4' });
    const landscape = new jsPDF({ orientation: 'landscape', unit: 'mm', format: 'a4' });
    expect(agregarEncabezadoInstitucionalPdf(portrait, 'REPORTE VERTICAL')).toBeGreaterThan(34);
    expect(agregarEncabezadoInstitucionalPdf(landscape, 'REPORTE HORIZONTAL', undefined, undefined, 'Tipo: Jurídica')).toBeGreaterThan(44);
  });

  it('expande el encabezado y conserva filtros extensos sin truncarlos', () => {
    const doc = new jsPDF({ orientation: 'portrait', unit: 'mm', format: 'a4' });
    const title = 'Matriz Consolidada de Riesgos — Matriz completa 82 campos';
    const filters = 'Tipo: Personas jurídicas | Estado: Todos los registros | Búsqueda: Sin texto | Desde: Sin fecha | Hasta: Sin fecha';
    const startY = agregarEncabezadoInstitucionalPdf(doc, title, undefined, undefined, filters);
    const output = String(doc.output());

    expect(startY).toBeGreaterThan(64);
    expect(output).toContain('Matriz Consolidada de Riesgos');
    expect(output).toContain('Hasta: Sin');
    expect(output).toContain('(fecha) Tj');
  });

  it('mueve una sección completa cuando no existe espacio suficiente', () => {
    const doc = new jsPDF({ orientation: 'portrait', unit: 'mm', format: 'a4' });
    agregarEncabezadoInstitucionalPdf(doc, 'REPORTE');
    const y = asegurarEspacioSeccionPdf(doc, 280, 30);
    expect(doc.getNumberOfPages()).toBe(2);
    expect(y).toBe(24);
  });

  it('impide que un margen de primera página genere espacios gigantes en páginas continuas', () => {
    expect(resolverMargenesTablaInstitucional({ top: 50, left: 12 })).toEqual({
      bottom: 19,
      left: 12,
      right: 14,
      top: MARGEN_SUPERIOR_TABLA_CONTINUACION_MM
    });
  });

  it('repite encabezados y evita dividir filas en tablas de varias páginas', () => {
    const doc = new jsPDF({ orientation: 'portrait', unit: 'mm', format: 'a4' });
    agregarEncabezadoInstitucionalPdf(doc, 'REPORTE EXTENSO');
    autoTableInstitucional(doc, {
      startY: 50,
      margin: { top: 50 },
      head: [['Identificación', 'Nombre', 'Observación']],
      body: Array.from({ length: 80 }, (_, index) => [`ID-${index}`, `Persona ${index}`, 'Texto institucional de prueba'])
    });
    expect(doc.getNumberOfPages()).toBeGreaterThan(1);
    agregarPiesInstitucionalesPdf(doc);

    const outputDir = (globalThis as any).process?.env?.['REPORTERIA_REGRESION_OUTPUT_DIR'];
    if (outputDir) {
      mkdirSync(outputDir, { recursive: true });
      const bytes = new Uint8Array(doc.output('arraybuffer'));
      writeFileSync(resolve(outputDir, 'monitoreo_detalle_continuacion.pdf'), bytes);
    }
  });

  it('agrega numeración una sola vez aunque se finalice dos veces', () => {
    const doc = new jsPDF({ orientation: 'portrait', unit: 'mm', format: 'a4' });
    agregarEncabezadoInstitucionalPdf(doc, 'REPORTE');
    agregarPiesInstitucionalesPdf(doc);
    agregarPiesInstitucionalesPdf(doc);
    expect((doc as any).__reporteInstitucionalFinalizado).toBe(true);
  });

  it('mantiene el ancho efectivo de las tablas dentro de los margenes imprimibles', () => {
    const doc = new jsPDF({ orientation: 'portrait', unit: 'mm', format: 'a4' });
    const margin = resolverMargenesTablaInstitucional({ left: 14, right: 14 });
    const usableWidth = resolverAnchoUtilPdf(doc, margin);
    expect(resolverAnchoTablaInstitucional(doc, usableWidth + 100, margin)).toBe(usableWidth);
    autoTableInstitucional(doc, {
      startY: 50,
      margin,
      tableWidth: usableWidth + 100,
      columnStyles: {
        0: { cellWidth: 90 },
        1: { cellWidth: 90 },
        2: { cellWidth: 90 }
      },
      head: [['A', 'B', 'C']],
      body: [['Texto', 'Texto', 'Texto']]
    });
    expect((doc as any).lastAutoTable).toBeTruthy();
  });
});
