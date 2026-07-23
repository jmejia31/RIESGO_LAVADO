from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]

xlsx_backend = ROOT / 'backend/RL.API/Infrastructure/Reporting/InstitutionalXlsxWorkbook.cs'
pdf_backend = ROOT / 'backend/RL.API/Infrastructure/Reporting/InstitutionalPdfDocument.cs'
renderer_tests = ROOT / 'backend/RL.API.Tests/Features/MatricesRiesgos/MatricesRiesgosReportRendererTests.cs'
excel_front = ROOT / 'frontend/rl-app/src/app/core/utils/excel-export.util.ts'
pdf_front = ROOT / 'frontend/rl-app/src/app/core/reporting/institutional-report.util.ts'
pdf_front_tests = ROOT / 'frontend/rl-app/src/app/core/reporting/institutional-report.util.spec.ts'
excel_front_tests = ROOT / 'frontend/rl-app/src/app/core/utils/excel-export.util.spec.ts'

# 1) Corrige el orden OpenXML exigido por Excel Desktop.
text = xlsx_backend.read_text(encoding='utf-8')
old = '''        return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
               "<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">" +
               "<sheetPr><pageSetUpPr fitToPage=\"1\" autoPageBreaks=\"0\"/></sheetPr>" +
               $"<sheetViews><sheetView workbookViewId=\"0\"><pane ySplit=\"4\" topLeftCell=\"A5\" activePane=\"bottomLeft\" state=\"frozen\"/></sheetView></sheetViews>" +
               $"<cols>{columns}</cols><sheetData>{rows}</sheetData>" +
               $"<mergeCells count=\"2\"><mergeCell ref=\"A1:{lastColumn}1\"/><mergeCell ref=\"A2:{lastColumn}2\"/></mergeCells>" +
               $"<autoFilter ref=\"A4:{lastColumn}{lastRow}\"/>" +
               "<printOptions horizontalCentered=\"1\"/><pageMargins left=\"0.35\" right=\"0.35\" top=\"0.65\" bottom=\"0.55\" header=\"0.2\" footer=\"0.2\"/>" +'''
new = '''        return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
               "<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">" +
               "<sheetPr><pageSetUpPr fitToPage=\"1\" autoPageBreaks=\"0\"/></sheetPr>" +
               $"<dimension ref=\"A1:{lastColumn}{lastRow}\"/>" +
               $"<sheetViews><sheetView workbookViewId=\"0\"><pane ySplit=\"4\" topLeftCell=\"A5\" activePane=\"bottomLeft\" state=\"frozen\"/></sheetView></sheetViews>" +
               "<sheetFormatPr defaultRowHeight=\"18\"/>" +
               $"<cols>{columns}</cols><sheetData>{rows}</sheetData>" +
               $"<autoFilter ref=\"A4:{lastColumn}{lastRow}\"/>" +
               $"<mergeCells count=\"2\"><mergeCell ref=\"A1:{lastColumn}1\"/><mergeCell ref=\"A2:{lastColumn}2\"/></mergeCells>" +
               "<printOptions horizontalCentered=\"1\"/><pageMargins left=\"0.35\" right=\"0.35\" top=\"0.65\" bottom=\"0.55\" header=\"0.2\" footer=\"0.2\"/>" +'''
if old not in text:
    raise RuntimeError('No se encontró el bloque WorksheetXml esperado.')
xlsx_backend.write_text(text.replace(old, new, 1), encoding='utf-8')

# 2) Espaciado institucional uniforme entre tablas y secciones PDF backend.
text = pdf_backend.read_text(encoding='utf-8')
old = '''    public void AddSection(string title)
    {
        AsegurarEspacio(28m);
        DrawText(Sanitizar(title).ToUpperInvariant(), MarginLeft, _current.CursorY, 11m, bold: true,
            InstitutionalReportStandard.Palette.Navy);
        _current.CursorY += 15m;
        DrawLine(MarginLeft, _current.CursorY, _pageWidth - MarginRight, _current.CursorY,
            InstitutionalReportStandard.Palette.Border, 0.7m);
        _current.CursorY += 8m;
    }
'''
new = '''    public void AddSection(string title)
    {
        const decimal topSpacing = 12m;
        const decimal bottomSpacing = 10m;
        var requiereSeparacion = _current.CursorY > BodyTop + 0.5m;
        AsegurarEspacio((requiereSeparacion ? topSpacing : 0m) + 30m);

        // Si AsegurarEspacio abrió una página nueva, el título inicia en el margen
        // institucional. En caso contrario se conserva una sangría vertical clara
        // respecto de la tabla o bloque anterior.
        if (_current.CursorY > BodyTop + 0.5m)
            _current.CursorY += topSpacing;

        DrawText(Sanitizar(title).ToUpperInvariant(), MarginLeft, _current.CursorY, 11m, bold: true,
            InstitutionalReportStandard.Palette.Navy);
        _current.CursorY += 15m;
        DrawLine(MarginLeft, _current.CursorY, _pageWidth - MarginRight, _current.CursorY,
            InstitutionalReportStandard.Palette.Border, 0.7m);
        _current.CursorY += bottomSpacing;
    }
'''
if old not in text:
    raise RuntimeError('No se encontró AddSection esperado.')
pdf_backend.write_text(text.replace(old, new, 1), encoding='utf-8')

# 3) Generador Excel frontend estándar: estilos sólo sobre columnas reales,
# múltiples cabeceras, texto blanco y filtro acotado a la tabla principal.
excel_front.write_text(r'''import * as ExcelJS from 'exceljs';

export interface ColInfo {
  wch?: number;
}

export interface WorkSheet {
  data: unknown[][];
  '!cols'?: ColInfo[];
  '!headerRows'?: number[];
  '!sectionRows'?: number[];
  '!autoFilterRow'?: number;
}

export interface WorkBook {
  sheets: Array<{ name: string; sheet: WorkSheet }>;
}

const NAVY = 'FF123B63';
const WHITE = 'FFFFFFFF';
const BODY = 'FF1F2937';
const ALTERNATE = 'FFF3F6F9';
const BORDER = 'FFD8E0E8';

export const utils = {
  aoa_to_sheet(data: unknown[][]): WorkSheet {
    return { data };
  },

  book_new(): WorkBook {
    return { sheets: [] };
  },

  book_append_sheet(workbook: WorkBook, worksheet: WorkSheet, name: string): void {
    workbook.sheets.push({ name: normalizarNombreHoja(name), sheet: worksheet });
  }
};

export function construirLibroInstitucional(workbook: WorkBook): ExcelJS.Workbook {
  const excel = new ExcelJS.Workbook();
  excel.creator = 'SGRLA-IHSS';
  excel.company = 'Instituto Hondureño de Seguridad Social';
  excel.created = new Date();
  excel.modified = new Date();

  const hojas = workbook.sheets.length ? workbook.sheets : [{ name: 'Reporte', sheet: { data: [] } }];
  for (const { name, sheet } of hojas) {
    crearHojaInstitucional(excel, name, sheet);
  }

  return excel;
}

export async function writeFile(workbook: WorkBook, fileName: string): Promise<void> {
  const excel = construirLibroInstitucional(workbook);
  const buffer = await excel.xlsx.writeBuffer();
  const blob = new Blob([buffer as BlobPart], {
    type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet'
  });
  const url = URL.createObjectURL(blob);
  const enlace = document.createElement('a');
  enlace.href = url;
  enlace.download = normalizarNombreArchivo(fileName);
  document.body.appendChild(enlace);
  enlace.click();
  document.body.removeChild(enlace);
  URL.revokeObjectURL(url);
}

function crearHojaInstitucional(workbook: ExcelJS.Workbook, name: string, source: WorkSheet): void {
  const data = (source.data ?? []).map(recortarFilaVaciaFinal);
  const maxColumns = Math.max(1, ...data.map(row => row.length));
  const worksheet = workbook.addWorksheet(normalizarNombreHoja(name), {
    pageSetup: {
      paperSize: 9,
      orientation: maxColumns > 8 ? 'landscape' : 'portrait',
      fitToPage: true,
      fitToWidth: 1,
      fitToHeight: 0,
      margins: { left: 0.35, right: 0.35, top: 0.65, bottom: 0.55, header: 0.2, footer: 0.2 }
    }
  });

  data.forEach(row => worksheet.addRow(row.map(normalizarValor)));
  if (worksheet.rowCount === 0) worksheet.addRow(['Sin información']);

  const headerRows = normalizarFilas(
    source['!headerRows']?.length ? source['!headerRows'] : detectarFilasEncabezado(data),
    worksheet.rowCount
  );
  const headerSet = new Set(headerRows);
  const sectionRows = normalizarFilas(
    source['!sectionRows']?.length ? source['!sectionRows'] : detectarFilasSeccion(data, headerSet),
    worksheet.rowCount
  );
  const sectionSet = new Set(sectionRows);

  // Cuerpo: sólo las celdas que realmente existen reciben formato. Esto evita
  // que Excel pinte columnas vacías hasta XFD o más allá del documento real.
  for (let rowIndex = 1; rowIndex <= worksheet.rowCount; rowIndex++) {
    if (rowIndex === 1 || headerSet.has(rowIndex) || sectionSet.has(rowIndex)) continue;
    const row = worksheet.getRow(rowIndex);
    const usedColumns = Math.max(1, data[rowIndex - 1]?.length ?? row.actualCellCount);
    for (let column = 1; column <= usedColumns; column++) {
      const cell = row.getCell(column);
      cell.font = { name: 'Arial', size: 10, color: { argb: BODY } };
      cell.alignment = { vertical: 'top', wrapText: true };
      cell.border = bordeInstitucional();
      if (rowIndex > 1 && rowIndex % 2 === 0) {
        cell.fill = relleno(ALTERNATE);
      }
    }
  }

  // Título institucional limitado al ancho efectivo de la hoja.
  const firstRow = worksheet.getRow(1);
  firstRow.height = 28;
  for (let column = 1; column <= maxColumns; column++) {
    const cell = firstRow.getCell(column);
    cell.font = { name: 'Arial', size: 14, bold: true, color: { argb: WHITE } };
    cell.fill = relleno(NAVY);
    cell.alignment = { vertical: 'middle', horizontal: 'left', wrapText: true };
  }
  if (maxColumns > 1) worksheet.mergeCells(1, 1, 1, maxColumns);

  // Secciones descriptivas: énfasis sin extender una franja a columnas vacías.
  for (const rowIndex of sectionRows) {
    const row = worksheet.getRow(rowIndex);
    const usedColumns = Math.max(1, data[rowIndex - 1]?.length ?? 1);
    for (let column = 1; column <= usedColumns; column++) {
      const cell = row.getCell(column);
      cell.font = { name: 'Arial', size: 10, bold: true, color: { argb: NAVY } };
      cell.alignment = { vertical: 'middle', wrapText: true };
      cell.border = {
        bottom: { style: 'thin', color: { argb: BORDER } }
      };
    }
    row.height = 21;
  }

  // Todas las cabeceras tabulares usan el mismo azul y texto blanco, pero sólo
  // hasta la última columna con contenido de esa cabecera.
  for (const rowIndex of headerRows) {
    const row = worksheet.getRow(rowIndex);
    const usedColumns = Math.max(1, data[rowIndex - 1]?.length ?? 1);
    for (let column = 1; column <= usedColumns; column++) {
      const cell = row.getCell(column);
      cell.font = { name: 'Arial', size: 10, bold: true, color: { argb: WHITE } };
      cell.fill = relleno(NAVY);
      cell.alignment = { vertical: 'middle', horizontal: 'center', wrapText: true };
      cell.border = bordeInstitucional();
    }
    row.height = 24;
  }

  const primaryHeader = resolverCabeceraPrincipal(data, headerRows, source['!autoFilterRow']);
  if (primaryHeader > 0) {
    const lastHeaderColumn = Math.max(1, data[primaryHeader - 1]?.length ?? maxColumns);
    const lastDataRow = resolverFinTabla(data, primaryHeader, headerSet, sectionSet);
    if (lastDataRow > primaryHeader) {
      worksheet.autoFilter = {
        from: { row: primaryHeader, column: 1 },
        to: { row: lastDataRow, column: lastHeaderColumn }
      };
    }
    worksheet.views = [{ state: 'frozen', ySplit: primaryHeader }];
    worksheet.pageSetup.printTitlesRow = `${primaryHeader}:${primaryHeader}`;
  } else {
    worksheet.views = [{ state: 'frozen', ySplit: Math.min(4, worksheet.rowCount) }];
  }

  const configured = source['!cols'] ?? [];
  worksheet.columns = Array.from({ length: maxColumns }, (_, index) => ({
    width: Math.min(Math.max(configured[index]?.wch ?? calcularAncho(data, index), 10), 48)
  }));

  const lastColumn = worksheet.getColumn(maxColumns).letter;
  worksheet.pageSetup.printArea = `A1:${lastColumn}${worksheet.rowCount}`;
  worksheet.headerFooter.oddHeader = '&C&"Arial,Bold"&10 INSTITUTO HONDUREÑO DE SEGURIDAD SOCIAL';
  worksheet.headerFooter.oddFooter = '&LSGRLA-IHSS&CGenerado: &D &T&RPágina &P de &N';
  worksheet.properties.defaultRowHeight = 18;
}

function detectarFilasEncabezado(data: unknown[][]): number[] {
  const rows: number[] = [];
  const conocidas = new Set([
    'condición actúa', 'condicion actua', 'número patronal', 'numero patronal',
    'número identificación', 'numero identificacion', 'identidad',
    'dni / identificación', 'dni / identidad', 'fecha'
  ]);

  for (let index = 1; index < data.length; index++) {
    const row = data[index];
    const values = row.map(value => String(value ?? '').trim()).filter(Boolean);
    if (values.length < 3) continue;
    const previousValues = (data[index - 1] ?? []).filter(value => String(value ?? '').trim() !== '');
    const first = values[0].toLocaleLowerCase('es-HN');
    const knownHeader = conocidas.has(first) || (first === 'fecha' && values.some(value => value.toLocaleLowerCase('es-HN') === 'usuario'));
    if (previousValues.length <= 1 || knownHeader) rows.push(index + 1);
  }

  return rows;
}

function detectarFilasSeccion(data: unknown[][], headerRows: Set<number>): number[] {
  const rows: number[] = [];
  for (let index = 3; index < data.length; index++) {
    const rowNumber = index + 1;
    if (headerRows.has(rowNumber)) continue;
    const values = data[index].map(value => String(value ?? '').trim()).filter(Boolean);
    if (values.length !== 1) continue;
    const text = values[0].toLocaleLowerCase('es-HN');
    if (text.startsWith('fecha de generación') || text.startsWith('fecha de generacion')) continue;
    rows.push(rowNumber);
  }
  return rows;
}

function resolverCabeceraPrincipal(data: unknown[][], headerRows: number[], explicit?: number): number {
  if (explicit === 0) return 0;
  if (explicit && headerRows.includes(explicit)) return explicit;
  return [...headerRows]
    .sort((a, b) => (data[b - 1]?.length ?? 0) - (data[a - 1]?.length ?? 0) || a - b)[0] ?? 0;
}

function resolverFinTabla(data: unknown[][], headerRow: number, headerRows: Set<number>, sectionRows: Set<number>): number {
  let end = headerRow;
  for (let rowNumber = headerRow + 1; rowNumber <= data.length; rowNumber++) {
    const values = data[rowNumber - 1] ?? [];
    const nonEmpty = values.some(value => String(value ?? '').trim() !== '');
    if (!nonEmpty || headerRows.has(rowNumber) || sectionRows.has(rowNumber)) break;
    end = rowNumber;
  }
  return end;
}

function normalizarFilas(rows: number[], maxRow: number): number[] {
  return [...new Set(rows.filter(row => Number.isInteger(row) && row > 0 && row <= maxRow))].sort((a, b) => a - b);
}

function recortarFilaVaciaFinal(row: unknown[]): unknown[] {
  let end = row.length;
  while (end > 0 && String(row[end - 1] ?? '').trim() === '') end--;
  return row.slice(0, end);
}

function bordeInstitucional(): Partial<ExcelJS.Borders> {
  return {
    top: { style: 'thin', color: { argb: BORDER } },
    left: { style: 'thin', color: { argb: BORDER } },
    bottom: { style: 'thin', color: { argb: BORDER } },
    right: { style: 'thin', color: { argb: BORDER } }
  };
}

function relleno(color: string): ExcelJS.Fill {
  return { type: 'pattern', pattern: 'solid', fgColor: { argb: color } };
}

function calcularAncho(data: unknown[][], columnIndex: number): number {
  const maxLength = data.reduce((max, row) => {
    const length = String(row[columnIndex] ?? '').length;
    return Math.max(max, length);
  }, 0);
  return Math.min(Math.max(maxLength + 2, 10), 45);
}

function normalizarValor(value: unknown): string | number | boolean | Date {
  if (value instanceof Date) return value;
  if (typeof value === 'number' || typeof value === 'boolean') return value;
  return String(value ?? '');
}

function normalizarNombreHoja(name: string): string {
  const limpio = (name || 'Reporte').replace(/[\\/?*[\]:]/g, ' ').trim();
  return (limpio || 'Reporte').slice(0, 31);
}

function normalizarNombreArchivo(fileName: string): string {
  const limpio = (fileName || 'Reporte.xlsx').replace(/[\\/:*?"<>|]/g, '_');
  return limpio.replace(/\.(xls|xlsx)$/i, '') + '.xlsx';
}
''', encoding='utf-8')

# 4) Utilitario PDF frontend: margen compacto obligatorio en páginas continuas
# y cabeceras tabulares con paleta institucional no sobreescribible.
pdf_front.write_text(r'''import { jsPDF } from 'jspdf';
import autoTable from 'jspdf-autotable';

export const COLORES_REPORTE_INSTITUCIONAL = {
  navy: [18, 59, 99] as [number, number, number],
  navyDark: [11, 46, 79] as [number, number, number],
  white: [255, 255, 255] as [number, number, number],
  body: [31, 41, 55] as [number, number, number],
  muted: [100, 116, 139] as [number, number, number],
  alternate: [243, 246, 249] as [number, number, number],
  border: [216, 224, 232] as [number, number, number],
  filterBackground: [239, 246, 255] as [number, number, number],
  filterText: [30, 64, 175] as [number, number, number]
};

export const MARGEN_SUPERIOR_TABLA_CONTINUACION_MM = 20;

interface ReporteMetaInterna {
  institucion: string;
  sistema: string;
  titulo: string;
  fecha: string;
}

function obtenerMeta(doc: jsPDF): ReporteMetaInterna {
  return (doc as any).__reporteInstitucional ?? {
    institucion: 'INSTITUTO HONDUREÑO DE SEGURIDAD SOCIAL',
    sistema: 'SGRLA-IHSS',
    titulo: 'REPORTE INSTITUCIONAL',
    fecha: new Date().toLocaleString('es-HN')
  };
}

function dibujarEncabezadoCompacto(doc: jsPDF): void {
  const meta = obtenerMeta(doc);
  const width = doc.internal.pageSize.getWidth();
  doc.setFillColor(...COLORES_REPORTE_INSTITUCIONAL.navyDark);
  doc.rect(0, 0, width, 14, 'F');
  doc.setFont('helvetica', 'bold');
  doc.setFontSize(8.5);
  doc.setTextColor(...COLORES_REPORTE_INSTITUCIONAL.white);
  doc.text(meta.institucion, 14, 6.5);
  doc.setFont('helvetica', 'normal');
  doc.setFontSize(7.5);
  doc.text(meta.titulo, 14, 11);
  doc.text(meta.sistema, width - 14, 9, { align: 'right' });
}

export function agregarEncabezadoInstitucionalPdf(
  doc: jsPDF,
  titulo: string,
  institucion = 'Instituto Hondureño de Seguridad Social',
  sistema = 'SGRLA-IHSS',
  filtros?: string
): number {
  const width = doc.internal.pageSize.getWidth();
  const fecha = new Date().toLocaleString('es-HN');
  const meta: ReporteMetaInterna = {
    institucion: institucion.toUpperCase(),
    sistema,
    titulo,
    fecha
  };
  (doc as any).__reporteInstitucional = meta;
  (doc as any).__reporteInstitucionalFinalizado = false;

  doc.setFillColor(...COLORES_REPORTE_INSTITUCIONAL.navy);
  doc.rect(0, 0, width, 34, 'F');
  doc.setFont('helvetica', 'bold');
  doc.setFontSize(12);
  doc.setTextColor(...COLORES_REPORTE_INSTITUCIONAL.white);
  doc.text(meta.institucion, 14, 11.5);
  doc.setFontSize(17);
  doc.text(titulo, 14, 22);
  doc.setFont('helvetica', 'normal');
  doc.setFontSize(8.5);
  doc.setTextColor(220, 230, 240);
  doc.text(`${sistema} | Generado: ${fecha}`, 14, 29);

  if (!filtros) return 44;

  doc.setFillColor(...COLORES_REPORTE_INSTITUCIONAL.filterBackground);
  doc.roundedRect(14, 41, width - 28, 14, 2, 2, 'F');
  doc.setFont('helvetica', 'normal');
  doc.setFontSize(8.5);
  doc.setTextColor(...COLORES_REPORTE_INSTITUCIONAL.filterText);
  const lineas = doc.splitTextToSize(`Filtros aplicados: ${filtros}`, width - 38);
  doc.text(lineas.slice(0, 2), 19, 48.5);
  return 64;
}

export function resolverMargenesTablaInstitucional(margin?: Record<string, number>): Record<string, number> {
  return {
    bottom: 19,
    left: 14,
    right: 14,
    ...(margin ?? {}),
    // startY gobierna la primera página. En páginas siguientes el margen superior
    // siempre queda junto al encabezado compacto y nunca hereda valores como 50 mm.
    top: MARGEN_SUPERIOR_TABLA_CONTINUACION_MM
  };
}

export function autoTableInstitucional(doc: jsPDF, options: Record<string, any>): void {
  const originalDidDrawPage = options['didDrawPage'];
  const margin = resolverMargenesTablaInstitucional(options['margin']);

  autoTable(doc, {
    ...options,
    showHead: 'everyPage',
    rowPageBreak: 'avoid',
    pageBreak: 'auto',
    margin,
    styles: {
      overflow: 'linebreak',
      valign: 'middle',
      font: 'helvetica',
      textColor: COLORES_REPORTE_INSTITUCIONAL.body,
      cellPadding: 2,
      ...(options['styles'] ?? {})
    },
    headStyles: {
      ...(options['headStyles'] ?? {}),
      fillColor: COLORES_REPORTE_INSTITUCIONAL.navy,
      textColor: COLORES_REPORTE_INSTITUCIONAL.white,
      fontStyle: 'bold'
    },
    alternateRowStyles: {
      ...(options['alternateRowStyles'] ?? {}),
      fillColor: COLORES_REPORTE_INSTITUCIONAL.alternate
    },
    didDrawPage: (data: any) => {
      const actualPage = doc.getCurrentPageInfo().pageNumber;
      if (actualPage > 1) dibujarEncabezadoCompacto(doc);
      if (typeof originalDidDrawPage === 'function') originalDidDrawPage(data);
    }
  } as any);
}

export function asegurarEspacioSeccionPdf(doc: jsPDF, y: number, altoMinimo = 28): number {
  const limite = doc.internal.pageSize.getHeight() - 20;
  if (y + altoMinimo <= limite) return y;
  doc.addPage();
  dibujarEncabezadoCompacto(doc);
  return 24;
}

export function agregarPiesInstitucionalesPdf(doc: jsPDF): void {
  if ((doc as any).__reporteInstitucionalFinalizado) return;
  const meta = obtenerMeta(doc);
  const total = doc.getNumberOfPages();

  for (let page = 1; page <= total; page++) {
    doc.setPage(page);
    if (page > 1) dibujarEncabezadoCompacto(doc);
    const width = doc.internal.pageSize.getWidth();
    const height = doc.internal.pageSize.getHeight();
    doc.setDrawColor(...COLORES_REPORTE_INSTITUCIONAL.border);
    doc.line(14, height - 13, width - 14, height - 13);
    doc.setFont('helvetica', 'normal');
    doc.setFontSize(7.5);
    doc.setTextColor(...COLORES_REPORTE_INSTITUCIONAL.muted);
    doc.text(meta.sistema, 14, height - 7.5);
    doc.text(`Generado: ${meta.fecha}`, width / 2, height - 7.5, { align: 'center' });
    doc.text(`Página ${page} de ${total}`, width - 14, height - 7.5, { align: 'right' });
  }

  (doc as any).__reporteInstitucionalFinalizado = true;
}
''', encoding='utf-8')

pdf_front_tests.write_text(r'''import { describe, expect, it } from 'vitest';
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
  });

  it('agrega numeración una sola vez aunque se finalice dos veces', () => {
    const doc = new jsPDF({ orientation: 'portrait', unit: 'mm', format: 'a4' });
    agregarEncabezadoInstitucionalPdf(doc, 'REPORTE');
    agregarPiesInstitucionalesPdf(doc);
    agregarPiesInstitucionalesPdf(doc);
    expect((doc as any).__reporteInstitucionalFinalizado).toBe(true);
  });
});
''', encoding='utf-8')

excel_front_tests.write_text(r'''import { describe, expect, it } from 'vitest';
import { construirLibroInstitucional, utils } from './excel-export.util';

function fillColor(cell: any): string | undefined {
  return cell.fill?.type === 'pattern' ? cell.fill.fgColor?.argb : undefined;
}

describe('generador Excel institucional', () => {
  it('limita títulos y cabeceras a las columnas realmente utilizadas', () => {
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
  });

  it('reconoce múltiples cabeceras de una ficha sin pintar columnas vacías', () => {
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
  });
});
''', encoding='utf-8')

# 5) Fortalece la prueba backend con validación del orden de nodos OpenXML.
text = renderer_tests.read_text(encoding='utf-8')
if 'using System.Xml.Linq;' not in text:
    text = text.replace('using System.Text.Json;\n', 'using System.Text.Json;\nusing System.Xml.Linq;\n', 1)
old = '''            var sheetXml = sheetReader.ReadToEnd();
            Assert.Contains("<pageSetUpPr fitToPage=\"1\" autoPageBreaks=\"0\"/>", sheetXml);
            Assert.Contains("fitToWidth=\"1\"", sheetXml);
'''
new = '''            var sheetXml = sheetReader.ReadToEnd();
            Assert.Contains("<pageSetUpPr fitToPage=\"1\" autoPageBreaks=\"0\"/>", sheetXml);
            Assert.Contains("fitToWidth=\"1\"", sheetXml);

            var document = XDocument.Parse(sheetXml);
            var children = document.Root!.Elements().Select(element => element.Name.LocalName).ToList();
            var autoFilterIndex = children.IndexOf("autoFilter");
            var mergeCellsIndex = children.IndexOf("mergeCells");
            Assert.True(autoFilterIndex >= 0, $"{worksheet.FullName} no contiene autoFilter.");
            Assert.True(mergeCellsIndex >= 0, $"{worksheet.FullName} no contiene mergeCells.");
            Assert.True(autoFilterIndex < mergeCellsIndex,
                $"{worksheet.FullName} no respeta el orden OpenXML: autoFilter debe preceder a mergeCells.");
            Assert.NotEmpty(document.Root.Descendants().Where(element => element.Name.LocalName == "row"));
'''
if old not in text:
    raise RuntimeError('No se encontró el bloque de validación Excel en las pruebas backend.')
renderer_tests.write_text(text.replace(old, new, 1), encoding='utf-8')

print('Correcciones de reportería aplicadas.')
