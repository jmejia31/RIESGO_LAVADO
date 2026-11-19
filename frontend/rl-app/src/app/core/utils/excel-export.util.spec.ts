import { describe, expect, it, vi } from 'vitest';
import * as ExcelJS from 'exceljs';
// @ts-ignore: Vitest ejecuta este archivo en Node.js.
import { mkdirSync, readFileSync, writeFileSync } from 'node:fs';
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
  writeFile,
  WorkBook
} from './excel-export.util';
import { setInstitutionalReportBranding } from '../reporting/institutional-report-standard';

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

  it('conserva roles del contexto para renderizar títulos y pares clave/valor sin concatenarlos', () => {
    const rows = [
      ['Reporte Integral de Patrono'],
      ['Instituto Hondureño de Seguridad Social'],
      ['Fecha de Generación: 13/11/2026'],
      [],
      ['INFORMACION GENERAL DEL PATRONO'],
      ['Numero Patronal', '201200601751', 'RTN', '05019006500703'],
      [],
      ['Codigo', 'Nombre', 'Estado'],
      ['R-001', 'Riesgo UAT 1', 'ACTIVO']
    ];
    const preview = createExcelPreviewFromRows('Ficha', rows);

    expect(preview.contextRows).toEqual(rows.slice(0, 7));
    expect(preview.contextRowRoles).toEqual(['TITLE', 'DATA', 'DATA', 'SPACER', 'SECTION', 'KEY_VALUE', 'SPACER']);
    expect(preview.contextRowRoles.filter(role => role === 'DATA')).toHaveLength(2);
    expect(preview.headerRow).toEqual(rows[7]);
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

  it('no marca el logo como embebido fuera de un navegador real', async () => {
    const workbook: WorkBook = { sheets: [{ name: 'Reporte', sheet: { data: [['Título'], ['Código'], ['R-001']] } }] };
    await workbookToBlob(workbook);
    expect(workbook.logoEmbedded).toBe(false);
    expect(construirLibroInstitucional(workbook).worksheets[0].getCell('A1').value).toBe('Título');
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

  it.each([
    { nombre: 'Blanco', rgb: [255, 255, 255] as [number, number, number] },
    { nombre: 'Azul', rgb: [18, 59, 99] as [number, number, number] },
    { nombre: 'Rosa', rgb: [249, 168, 212] as [number, number, number] },
    { nombre: 'Turquesa', rgb: [167, 243, 208] as [number, number, number] },
    { nombre: 'Negro', rgb: [0, 0, 0] as [number, number, number] }
  ])('mantiene títulos de sección y etiquetas en negro puro (FF000000) para tema $nombre', ({ rgb }) => {
    setInstitutionalReportBranding({ headerColor: rgb });
    const data = [
      ['Título del Reporte'],
      ['SECCIÓN DE PRUEBA'],
      ['Etiqueta Uno:', 'Valor Uno', 'Etiqueta Dos:', 'Valor Dos'],
      ['Col A', 'Col B'],
      ['Dato A', 'Dato B']
    ];
    const sheet = utils.aoa_to_sheet(data);
    sheet['!sectionRows'] = [2];
    sheet['!keyValueRows'] = [3];
    sheet['!headerRows'] = [4];
    const book = utils.book_new();
    utils.book_append_sheet(book, sheet, 'Prueba');

    const ws = construirLibroInstitucional(book).worksheets[0];
    expect(ws.getCell('A2').font.color?.argb).toBe('FF000000');
    expect(ws.getCell('A2').font.bold).toBe(true);

    expect(ws.getCell('A3').font.color?.argb).toBe('FF000000');
    expect(ws.getCell('A3').font.bold).toBe(true);
    expect(ws.getCell('B3').font.color?.argb).toBe('FF000000');
    expect(ws.getCell('B3').font.bold ?? false).toBe(false);
    expect(ws.getCell('C3').font.color?.argb).toBe('FF000000');
    expect(ws.getCell('C3').font.bold).toBe(true);
  });

  it('incrusta logotipo institucional en el libro Excel tanto en formato PNG como JPEG', async () => {
    const pngLogo = 'data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==';
    const jpegLogo = 'data:image/jpeg;base64,/9j/4AAQSkZJRgABAQEASABIAAD/2wBDAP//////////////////////////////////////////////////////////////////////////////////////wgALCAABAAEBAREA/8QAFBABAAAAAAAAAAAAAAAAAAAAAP/aAAgBAQABPxA=';

    // Prueba con PNG
    setInstitutionalReportBranding({ logoUrl: pngLogo, logoAspectRatio: 2.82 });
    const bookPng = utils.book_new();
    utils.book_append_sheet(bookPng, utils.aoa_to_sheet([['Reporte'], ['Datos']]), 'Hoja1');
    const workbookPng = construirLibroInstitucional(bookPng);
    expect(workbookPng.worksheets[0].getImages().length).toBe(1);
    expect((workbookPng as any).model?.media?.length).toBe(1);

    // Prueba con JPEG
    setInstitutionalReportBranding({ logoUrl: jpegLogo, logoAspectRatio: 2.82 });
    const bookJpeg = utils.book_new();
    utils.book_append_sheet(bookJpeg, utils.aoa_to_sheet([['Reporte'], ['Datos']]), 'Hoja1');
    const workbookJpeg = construirLibroInstitucional(bookJpeg);
    expect(workbookJpeg.worksheets[0].getImages().length).toBe(1);
    expect((workbookJpeg as any).model?.media?.length).toBe(1);

    // Prueba sin logo
    setInstitutionalReportBranding({ logoUrl: '' });
    const bookSinLogo = utils.book_new();
    utils.book_append_sheet(bookSinLogo, utils.aoa_to_sheet([['Reporte'], ['Datos']]), 'Hoja1');
    const workbookSinLogo = construirLibroInstitucional(bookSinLogo);
    expect(workbookSinLogo.worksheets[0].getImages().length).toBe(0);
  });
});
