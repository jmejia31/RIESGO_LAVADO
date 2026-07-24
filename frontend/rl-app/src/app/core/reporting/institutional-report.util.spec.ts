import { describe, expect, it } from 'vitest';
// @ts-ignore: Vitest ejecuta este archivo en Node.js.
import { mkdirSync, writeFileSync } from 'node:fs';
// @ts-ignore: Vitest ejecuta este archivo en Node.js.
import { resolve } from 'node:path';
import { jsPDF } from 'jspdf';
import {
  agregarEncabezadoInstitucionalPdf,
  agregarPiesInstitucionalesPdf,
  asegurarEspacioSeccionPdf,
  autoTableInstitucional,
  COLORES_REPORTE_INSTITUCIONAL,
  MARGEN_SUPERIOR_TABLA_CONTINUACION_MM,
  resolverMargenesTablaInstitucional
} from './institutional-report.util';

describe('estándar institucional de reportería', () => {
  it('usa la paleta institucional aprobada', () => {
    expect(COLORES_REPORTE_INSTITUCIONAL.navy).toEqual([18, 59, 99]);
    expect(COLORES_REPORTE_INSTITUCIONAL.alternate).toEqual([243, 246, 249]);
  });

  it('mantiene el mismo encabezado en orientación vertical y horizontal', () => {
    const portrait = new jsPDF({ orientation: 'portrait', unit: 'mm', format: 'a4' });
    const landscape = new jsPDF({ orientation: 'landscape', unit: 'mm', format: 'a4' });
    expect(agregarEncabezadoInstitucionalPdf(portrait, 'REPORTE VERTICAL')).toBe(44);
    expect(agregarEncabezadoInstitucionalPdf(landscape, 'REPORTE HORIZONTAL', undefined, undefined, 'Tipo: Jurídica')).toBe(64);
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
});
