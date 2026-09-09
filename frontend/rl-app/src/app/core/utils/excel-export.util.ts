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

export type ExcelRowRole = 'TITLE' | 'HEADER' | 'SECTION' | 'KEY_VALUE' | 'PARAGRAPH' | 'DATA' | 'SPACER';

export interface ExcelPreviewSheet {
  name: string;
  rows: unknown[][];
  rowRoles: ExcelRowRole[];
  headerRow: unknown[] | null;
  contextRows: unknown[][];
  dataRows: unknown[][];
  dataRowRoles: ExcelRowRole[];
  totalRows: number;
  sourceRows: number;
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
  await downloadBlob(await workbookToBlob(workbook), normalizarNombreArchivoExcel(fileName));
}

/** Genera el mismo binario que se presenta en el visor, sin iniciar una descarga. */
export async function workbookToBlob(workbook: WorkBook): Promise<Blob> {
  const excel = construirLibroInstitucional(workbook);
  const buffer = await excel.xlsx.writeBuffer();
  return new Blob([buffer as BlobPart], {
    type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet'
  });
}

/** Descarga explícita desde un flujo que ya pasó por una vista previa. */
export function downloadBlob(blob: Blob, fileName: string): void {
  const url = URL.createObjectURL(blob);
  const enlace = document.createElement('a');
  enlace.href = url;
  enlace.download = normalizarNombreArchivoGeneral(fileName);
  document.body.appendChild(enlace);
  enlace.click();
  document.body.removeChild(enlace);
  URL.revokeObjectURL(url);
}

export function createExcelPreview(workbook: WorkBook): ExcelPreviewSheet[] {
  const sheets = workbook.sheets.length ? workbook.sheets : [{ name: 'Reporte', sheet: { data: [] } }];
  return sheets.map(({ name, sheet }) => createExcelPreviewSheet(name, sheet));
}

/** Construye la vista previa desde filas ya leídas de un XLSX remoto. */
export function createExcelPreviewFromRows(name: string, rows: unknown[][]): ExcelPreviewSheet {
  return createExcelPreviewSheet(name, { data: rows });
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

  const explicitKeyValueRows = normalizarFilas(source['!keyValueRows'] ?? [], worksheet.rowCount);
  const keyValueRows = explicitKeyValueRows.length ? explicitKeyValueRows : detectarFilasClaveValor(data);
  const keyValueSet = new Set(keyValueRows);
  const explicitParagraphRows = normalizarFilas(source['!paragraphRows'] ?? [], worksheet.rowCount);
  const paragraphRows = explicitParagraphRows;
  const paragraphSet = new Set(paragraphRows);
  const headerRows = normalizarFilas(
    source['!headerRows']?.length ? source['!headerRows'] : detectarFilasEncabezado(data, new Set([...keyValueSet, ...paragraphSet])),
    worksheet.rowCount
  ).filter(row => !keyValueSet.has(row) && !paragraphSet.has(row));
  const headerSet = new Set(headerRows);
  const sectionRows = normalizarFilas(
    source['!sectionRows']?.length ? source['!sectionRows'] : detectarFilasSeccion(data, new Set([...headerSet, ...keyValueSet, ...paragraphSet])),
    worksheet.rowCount
  ).filter(row => !headerSet.has(row) && !keyValueSet.has(row) && !paragraphSet.has(row));
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

export function detectarFilasEncabezado(data: unknown[][], excludedRows = new Set<number>()): number[] {
  const rows: number[] = [];
  const conocidas = new Set([
    'condición actúa', 'condicion actua', 'número patronal', 'numero patronal',
    'número identificación', 'numero identificacion', 'identidad',
    'dni / identificación', 'dni / identidad', 'fecha'
  ]);

  const firstRow = data[0] ?? [];
  if (esCabeceraTabular(firstRow)) rows.push(1);

  for (let index = 1; index < data.length; index++) {
    if (excludedRows.has(index + 1)) continue;
    const row = data[index];
    const values = row.map(value => String(value ?? '').trim()).filter(Boolean);
    if (values.length < 3) continue;
    const previousValues = (data[index - 1] ?? []).filter(value => String(value ?? '').trim() !== '');
    const first = values[0].toLocaleLowerCase('es-HN');
    const knownHeader = conocidas.has(first) || (first === 'fecha' && values.some(value => value.toLocaleLowerCase('es-HN') === 'usuario'));
    if ((previousValues.length <= 1 || knownHeader) && !pareceFilaClaveValor(row)) rows.push(index + 1);
  }

  return rows;
}

function detectarFilasSeccion(data: unknown[][], protectedRows: Set<number>): number[] {
  const rows: number[] = [];
  for (let index = 3; index < data.length; index++) {
    const rowNumber = index + 1;
    if (protectedRows.has(rowNumber)) continue;
    const values = data[index].map(value => String(value ?? '').trim()).filter(Boolean);
    if (values.length !== 1) continue;
    const text = values[0].toLocaleLowerCase('es-HN');
    if (text.startsWith('fecha de generación') || text.startsWith('fecha de generacion')) continue;
    rows.push(rowNumber);
  }
  return rows;
}

function detectarFilasClaveValor(data: unknown[][]): number[] {
  return data
    .map((row, index) => pareceFilaClaveValor(row) ? index + 1 : 0)
    .filter((row): row is number => row > 0);
}

function pareceFilaClaveValor(row: unknown[]): boolean {
  const values = row.map(value => String(value ?? '').trim()).filter(Boolean);
  if (values.length < 4 || values.length % 2 !== 0) return false;

  const etiquetasConocidas = new Set([
    'número patronal', 'numero patronal', 'rtn', 'nombre / razón social', 'nombre / razon social',
    'proveedor ihss', 'lista de coincidencia', 'lista coincidencia', 'estado monitoreo',
    'fecha coincidencia', 'fecha calificación', 'fecha calificacion', 'registro interno',
    'origen del registro', 'dni / identificación', 'dni / identificacion', 'nombre completo'
  ]);
  const etiquetas = values.filter((_, index) => index % 2 === 0);
  const valores = values.filter((_, index) => index % 2 === 1);
  const etiquetasReconocidas = etiquetas.filter(value => {
    const normalizada = value.replace(/:$/, '').toLocaleLowerCase('es-HN');
    return etiquetasConocidas.has(normalizada);
  }).length;
  const valoresNoEtiquetas = valores.filter(value => {
    const normalizada = value.replace(/:$/, '').toLocaleLowerCase('es-HN');
    return !etiquetasConocidas.has(normalizada);
  }).length;
  return etiquetasReconocidas >= 2 && valoresNoEtiquetas >= 2;
}

function esCabeceraTabular(row: unknown[]): boolean {
  const values = row.map(value => String(value ?? '').trim()).filter(Boolean);
  return values.length >= 2 && !pareceFilaClaveValor(row);
}

export function detectarRolesFilasExcel(data: unknown[][], source: WorkSheet = { data }): ExcelRowRole[] {
  const maxRow = data.length;
  const explicitKeyValue = new Set(normalizarFilas(source['!keyValueRows'] ?? [], maxRow));
  const keyValueRows = explicitKeyValue.size ? explicitKeyValue : new Set(detectarFilasClaveValor(data));
  const paragraphRows = new Set(normalizarFilas(source['!paragraphRows'] ?? [], maxRow));
  const explicitHeaders = new Set(normalizarFilas(source['!headerRows'] ?? [], maxRow));
  const detectedHeaders = new Set(detectarFilasEncabezado(data, new Set([...keyValueRows, ...paragraphRows])));
  const headers = explicitHeaders.size ? explicitHeaders : detectedHeaders;
  const sections = new Set(normalizarFilas(source['!sectionRows'] ?? [], maxRow));

  return data.map((row, index) => {
    const rowNumber = index + 1;
    if (row.every(value => String(value ?? '').trim() === '')) return 'SPACER';
    if (keyValueRows.has(rowNumber)) return 'KEY_VALUE';
    if (paragraphRows.has(rowNumber)) return 'PARAGRAPH';
    if (sections.has(rowNumber)) return 'SECTION';
    if (headers.has(rowNumber) && !keyValueRows.has(rowNumber) && !paragraphRows.has(rowNumber)) return 'HEADER';
    if (rowNumber === 1) return 'TITLE';
    return 'DATA';
  });
}

function createExcelPreviewSheet(name: string, source: WorkSheet): ExcelPreviewSheet {
  const rows = (source.data ?? []).map(row => [...row]);
  const rowRoles = detectarRolesFilasExcel(rows, source);
  const headerIndex = rowRoles.findIndex(role => role === 'HEADER');
  const headerRow = headerIndex >= 0 ? rows[headerIndex] : null;
  const contextRows = headerIndex > 0 ? rows.slice(0, headerIndex) : [];
  const dataStart = headerIndex >= 0 ? headerIndex + 1 : 0;
  const dataRows = rows.slice(dataStart);
  const dataRowRoles = rowRoles.slice(dataStart);

  return {
    name,
    rows,
    rowRoles,
    headerRow,
    contextRows,
    dataRows,
    dataRowRoles,
    totalRows: dataRows.length,
    sourceRows: rows.length
  };
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

export function normalizarNombreArchivoGeneral(fileName: string): string {
  const limpio = (fileName || 'Reporte').replace(/[\\/:*?"<>|]/g, '_').trim();
  return limpio || 'Reporte';
}

export function normalizarNombreArchivoExcel(fileName: string): string {
  const limpio = normalizarNombreArchivoGeneral(fileName || 'Reporte');
  return limpio.replace(/\.(xls|xlsx)$/i, '') + '.xlsx';
}
