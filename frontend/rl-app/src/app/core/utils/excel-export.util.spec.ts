import { describe, expect, it, vi } from 'vitest';
// @ts-ignore: Vitest ejecuta este archivo en Node.js.
import { mkdirSync, writeFileSync } from 'node:fs';
// @ts-ignore: Vitest ejecuta este archivo en Node.js.
import { resolve } from 'node:path';
import {
  construirLibroInstitucional,
  createExcelPreviewFromRows,
  detectarRolesFilasExcel,
  downloadBlob,
  normalizarNombreArchivoExcel,
  normalizarNombreArchivoGeneral,
  utils,
  workbookToBlob,
  writeFile
} from './excel-export.util';

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
    expect(fillColor(worksheet.getCell('A6'))).not.toBe('FF123B63');
    expect(worksheet.getCell('D6').font.color?.argb).not.toBe('FFFFFFFF');
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

  it('mantiene las filas clave/valor fuera del rol de cabecera tabular', () => {
    const data = [
      ['Reporte Integral de Patrono'],
      ['INFORMACION GENERAL DEL PATRONO'],
      ['Numero Patronal', '201200601751', 'RTN', '05019006500703'],
      ['Nombre/Razon Social', 'TECPROFIRE', 'Proveedor IHSS', 'No'],
      ['ID Reporte', 'Identidad', 'Nombre', 'Estado']
    ];
    const worksheet = utils.aoa_to_sheet(data);
    worksheet['!sectionRows'] = [2];
    worksheet['!headerRows'] = [5];
    const book = utils.book_new();
    utils.book_append_sheet(book, worksheet, 'Ficha Patrono');

    const output = construirLibroInstitucional(book).worksheets[0];
    expect(fillColor(output.getCell('A3'))).not.toBe('FF123B63');
    expect(fillColor(output.getCell('A4'))).not.toBe('FF123B63');
    expect(fillColor(output.getCell('A5'))).toBe('FF123B63');
  });

  it('separa la cabecera y conserva 125 filas de datos al paginar el preview', () => {
    const rows = [
      ['Codigo', 'Nombre', 'Estado'],
      ...Array.from({ length: 125 }, (_, index) => [`R-${String(index + 1).padStart(3, '0')}`, `Riesgo ${index + 1}`, 'ACTIVO'])
    ];
    const preview = createExcelPreviewFromRows('Coincidencias', rows);

    expect(preview.headerRow).toEqual(rows[0]);
    expect(preview.dataRows).toHaveLength(125);
    expect(preview.dataRows[0]).toEqual(rows[1]);
    expect(preview.dataRows[124]).toEqual(rows[125]);
    expect(new Set(preview.dataRows.map(row => row[0])).size).toBe(125);
    expect(preview.totalRows).toBe(125);
  });

  it('preserva nombre y MIME al descargar PDF y Excel', async () => {
    const pdf = new Blob(['pdf'], { type: 'application/pdf' });
    const xlsx = await workbookToBlob({ sheets: [{ name: 'Reporte', sheet: { data: [['Codigo'], ['R-001']] } }] });
    expect(normalizarNombreArchivoGeneral('Reporte.pdf')).toBe('Reporte.pdf');
    expect(normalizarNombreArchivoExcel('Reporte')).toBe('Reporte.xlsx');
    expect(pdf.type).toBe('application/pdf');
    expect(xlsx.type).toBe('application/vnd.openxmlformats-officedocument.spreadsheetml.sheet');

    const downloads: string[] = [];
    const link = {
      href: '',
      download: '',
      click: () => downloads.push(link.download)
    };
    vi.stubGlobal('document', {
      createElement: () => link,
      body: { appendChild: () => undefined, removeChild: () => undefined }
    });
    const blobUrl = vi.spyOn(URL, 'createObjectURL').mockReturnValue('blob:download');
    const revoke = vi.spyOn(URL, 'revokeObjectURL').mockImplementation(() => undefined);
    blobUrl.mockClear();
    revoke.mockClear();
    downloadBlob(pdf, 'Reporte.pdf');
    downloadBlob(xlsx, 'Reporte.xlsx');
    await writeFile({ sheets: [{ name: 'Reporte', sheet: { data: [['Codigo'], ['R-001']] } }] }, 'Reporte');
    expect(downloads).toEqual(['Reporte.pdf', 'Reporte.xlsx', 'Reporte.xlsx']);
    expect(blobUrl).toHaveBeenCalledTimes(3);
    expect(revoke).toHaveBeenCalledTimes(3);
    blobUrl.mockRestore();
    revoke.mockRestore();
    vi.unstubAllGlobals();
  });

  it('mantiene roles Excel mutuamente excluyentes y conserva spacers', () => {
    const rows = [
      ['Ficha de Patrono'],
      ['Numero Patronal', '201200601751', 'RTN', '05019006500703'],
      [],
      ['Codigo', 'Nombre', 'Estado'],
      ['R-001', 'Riesgo UAT 1', 'ACTIVO']
    ];
    const roles = detectarRolesFilasExcel(rows, {
      data: rows,
      '!keyValueRows': [2],
      '!headerRows': [2, 4]
    });
    expect(roles).toEqual(['TITLE', 'KEY_VALUE', 'SPACER', 'HEADER', 'DATA']);
    expect(roles.filter(role => role === 'HEADER')).toHaveLength(1);
    expect(roles.filter(role => role === 'KEY_VALUE')).toHaveLength(1);
  });

});
