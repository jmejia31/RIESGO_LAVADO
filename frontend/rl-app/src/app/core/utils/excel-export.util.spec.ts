import { describe, expect, it } from 'vitest';
// @ts-ignore: Vitest ejecuta este archivo en Node.js.
import { mkdirSync, writeFileSync } from 'node:fs';
// @ts-ignore: Vitest ejecuta este archivo en Node.js.
import { resolve } from 'node:path';
import { construirLibroInstitucional, utils } from './excel-export.util';

function fillColor(cell: any): string | undefined {
  return cell.fill?.type === 'pattern' ? cell.fill.fgColor?.argb : undefined;
}

describe('generador Excel institucional', () => {
  it('limita títulos y cabeceras a las columnas realmente utilizadas', async () => {
    const data = [
      ['Reporte de Coincidencias Jurídicas'],
      ['Instituto Hondureño de Seguridad Social'],
      ['Fecha de Generación: 23/07/2026'],
      [],
      ['Número Patronal', 'RTN', 'Nombre Empresa', 'Lista', 'Proveedor', 'Fecha Coincidencia', 'Fecha Calificación', 'Registro Interno'],
      ['201200601751', '05019006500073', 'Empresa', 'OFAC', 'No', '21/05/2026', '14/06/2026', '']
    ];
    const sheet = utils.aoa_to_sheet(data);
    const book = utils.book_new();
    utils.book_append_sheet(book, sheet, 'Coincidencias');

    const worksheet = construirLibroInstitucional(book).worksheets[0];
    expect(fillColor(worksheet.getCell('H1'))).toBe('FF123B63');
    expect(fillColor(worksheet.getCell('I1'))).not.toBe('FF123B63');
    expect(worksheet.getCell('A5').font.color?.argb).toBe('FFFFFFFF');
    expect(worksheet.getCell('H5').font.color?.argb).toBe('FFFFFFFF');
    expect(fillColor(worksheet.getCell('I5'))).not.toBe('FF123B63');
    expect((worksheet.autoFilter as any).to.column).toBe(8);

    const outputDir = (globalThis as any).process?.env?.['REPORTERIA_REGRESION_OUTPUT_DIR'];
    if (outputDir) {
      mkdirSync(outputDir, { recursive: true });
      const buffer = await worksheet.workbook.xlsx.writeBuffer();
      writeFileSync(resolve(outputDir, 'monitoreo_juridicas_estandar.xlsx'), new Uint8Array(buffer as ArrayBuffer));
    }
  });

  it('reconoce múltiples cabeceras de una ficha sin pintar columnas vacías', async () => {
    const data = [
      ['Ficha de Perfil / Memorando'],
      ['Instituto Hondureño de Seguridad Social'],
      ['Fecha de Generación: 23/07/2026'],
      [],
      ['Información General de la Persona'],
      ['DNI / Identificación', '0703197300189', 'Nombre Completo', 'JORGE GUSTAVO MEDINA'],
      ['Lista Coincidencia', 'PEPS', 'Total de Coincidencias', '30'],
      [],
      ['Detalle de Coincidencias Encontradas'],
      ['Condición Actúa', 'Nro Patronal', 'Empresa', 'Es PEP', 'Lista', 'Fecha Coincidencia', 'Fecha Calificación'],
      ['REPRESENTANTE LEGAL', '101196100761', 'TRIBUNAL SUPERIOR DE CUENTAS', 'SÍ', 'PEPS', '01/07/2025', '07/11/2025']
    ];
    const sheet = utils.aoa_to_sheet(data);
    const book = utils.book_new();
    utils.book_append_sheet(book, sheet, 'Ficha Natural');

    const worksheet = construirLibroInstitucional(book).worksheets[0];
    expect(fillColor(worksheet.getCell('A6'))).toBe('FF123B63');
    expect(worksheet.getCell('D6').font.color?.argb).toBe('FFFFFFFF');
    expect(fillColor(worksheet.getCell('E6'))).not.toBe('FF123B63');
    expect(fillColor(worksheet.getCell('A10'))).toBe('FF123B63');
    expect(worksheet.getCell('G10').font.color?.argb).toBe('FFFFFFFF');
    expect(fillColor(worksheet.getCell('H10'))).not.toBe('FF123B63');
    expect((worksheet.autoFilter as any).from.row).toBe(10);
    expect((worksheet.autoFilter as any).to.column).toBe(7);

    const outputDir = (globalThis as any).process?.env?.['REPORTERIA_REGRESION_OUTPUT_DIR'];
    if (outputDir) {
      mkdirSync(outputDir, { recursive: true });
      const buffer = await worksheet.workbook.xlsx.writeBuffer();
      writeFileSync(resolve(outputDir, 'monitoreo_ficha_natural_estandar.xlsx'), new Uint8Array(buffer as ArrayBuffer));
    }
  });

  it('presenta la ficha de patrono con información general y motivo al nivel correcto', async () => {
    const data = [
      ['Ficha de Perfil / Memorando'],
      ['Instituto Hondureño de Seguridad Social'],
      ['Fecha de Generación: 23/07/2026'],
      [],
      ['Información General del Patrono'],
      ['Número Patronal:', '101202303441', 'RTN:', '08019021289810'],
      ['Nombre / Razón Social:', 'HELIOS S A', 'Proveedor IHSS:', 'No'],
      ['Lista de Coincidencia:', 'OFAC', 'Estado Monitoreo:', 'CON MOTIVO REGISTRADO'],
      ['Fecha Coincidencia:', '19/12/2023', 'Fecha Calificación:', '01/06/2026'],
      ['Registro Interno:', '07/06/2026', 'Origen del Registro:', 'N/D'],
      [],
      ['Motivo de Ingreso a Lista de Monitoreo'],
      ['prueba de motivo, este fue modificado por otro usuario'],
      [],
      ['Historial de Seguimientos y Evidencias'],
      ['Rango de seguimientos:', 'Todos los seguimientos registrados'],
      ['Fecha', 'Usuario', 'Comentario / Acción', 'Evidencias'],
      ['10/07/2026', 'francisco.perez@ihss.hn', 'Tercer seguimiento', 'Sin evidencias']
    ];
    const sheet = utils.aoa_to_sheet(data);
    sheet['!headerRows'] = [17];
    sheet['!sectionRows'] = [5, 12, 15];
    sheet['!keyValueRows'] = [6, 7, 8, 9, 10];
    sheet['!paragraphRows'] = [13];
    sheet['!autoFilterRow'] = 17;

    const book = utils.book_new();
    utils.book_append_sheet(book, sheet, 'Ficha Patrono');
    const worksheet = construirLibroInstitucional(book).worksheets[0];

    expect(fillColor(worksheet.getCell('A6'))).not.toBe('FF123B63');
    expect(worksheet.getCell('A6').font.bold).toBe(true);
    expect(worksheet.getCell('B6').font.bold ?? false).toBe(false);
    expect(worksheet.getCell('C6').font.bold).toBe(true);
    expect(worksheet.getCell('A13').font.bold ?? false).toBe(false);
    expect(worksheet.getCell('A13').isMerged).toBe(true);
    expect(worksheet.getCell('D13').isMerged).toBe(true);
    expect(fillColor(worksheet.getCell('A17'))).toBe('FF123B63');
    expect(worksheet.getCell('A17').font.color?.argb).toBe('FFFFFFFF');
    expect((worksheet.autoFilter as any).from.row).toBe(17);
    expect((worksheet.autoFilter as any).to.column).toBe(4);

    const outputDir = (globalThis as any).process?.env?.['REPORTERIA_REGRESION_OUTPUT_DIR'];
    if (outputDir) {
      mkdirSync(outputDir, { recursive: true });
      const buffer = await worksheet.workbook.xlsx.writeBuffer();
      writeFileSync(resolve(outputDir, 'monitoreo_ficha_patrono_corregida.xlsx'), new Uint8Array(buffer as ArrayBuffer));
    }
  });

});
