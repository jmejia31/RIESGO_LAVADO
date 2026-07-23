import * as ExcelJS from 'exceljs';

export interface ColInfo {
  wch?: number;
}

export interface WorkSheet {
  data: unknown[][];
  '!cols'?: ColInfo[];
  '!headerRows'?: number[];
  '!sectionRows'?: number[];
  '!keyValueRows'?: number[];
  '!paragraphRows'?: number[];
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
  const keyValueRows = normalizarFilas(source['!keyValueRows'] ?? [], worksheet.rowCount);
  const paragraphRows = normalizarFilas(source['!paragraphRows'] ?? [], worksheet.rowCount);

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

  // Filas de información general: mantienen el aspecto de ficha del PDF.
  // Las columnas impares son etiquetas en negrita y las pares son valores normales.
  for (const rowIndex of keyValueRows) {
    const row = worksheet.getRow(rowIndex);
    const usedColumns = Math.max(1, data[rowIndex - 1]?.length ?? 1);
    for (let column = 1; column <= usedColumns; column++) {
      const cell = row.getCell(column);
      const esEtiqueta = column % 2 === 1;
      cell.font = {
        name: 'Arial',
        size: 10,
        bold: esEtiqueta,
        color: { argb: esEtiqueta ? NAVY : BODY }
      };
      cell.alignment = { vertical: 'middle', horizontal: 'left', wrapText: true };
      cell.border = bordeInstitucional();
    }
    row.height = 22;
  }

  // Los textos explicativos se muestran como párrafos, no como títulos o cabeceras.
  for (const rowIndex of paragraphRows) {
    if (maxColumns > 1) worksheet.mergeCells(rowIndex, 1, rowIndex, maxColumns);
    const row = worksheet.getRow(rowIndex);
    const cell = row.getCell(1);
    cell.font = { name: 'Arial', size: 10, bold: false, color: { argb: BODY } };
    cell.alignment = { vertical: 'top', horizontal: 'left', wrapText: true };
    cell.border = {};
    row.height = 30;
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
