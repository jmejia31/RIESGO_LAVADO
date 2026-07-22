import * as ExcelJS from 'exceljs';

export interface ColInfo {
  wch?: number;
}

export interface WorkSheet {
  data: unknown[][];
  '!cols'?: ColInfo[];
}

export interface WorkBook {
  sheets: Array<{ name: string; sheet: WorkSheet }>;
}

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

export async function writeFile(workbook: WorkBook, fileName: string): Promise<void> {
  const excel = new ExcelJS.Workbook();
  excel.creator = 'SGRLA-IHSS';
  excel.company = 'Instituto Hondureño de Seguridad Social';
  excel.created = new Date();
  excel.modified = new Date();

  const hojas = workbook.sheets.length ? workbook.sheets : [{ name: 'Reporte', sheet: { data: [] } }];
  for (const { name, sheet } of hojas) {
    crearHojaInstitucional(excel, name, sheet);
  }

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
  const data = source.data ?? [];
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

  const firstRow = worksheet.getRow(1);
  if (maxColumns > 1) worksheet.mergeCells(1, 1, 1, maxColumns);
  firstRow.height = 28;
  firstRow.font = { name: 'Arial', size: 14, bold: true, color: { argb: 'FFFFFFFF' } };
  firstRow.fill = { type: 'pattern', pattern: 'solid', fgColor: { argb: 'FF123B63' } };
  firstRow.alignment = { vertical: 'middle', horizontal: 'left', wrapText: true };

  const headerRowIndex = detectarFilaEncabezado(data);
  if (headerRowIndex > 0) {
    const header = worksheet.getRow(headerRowIndex);
    header.font = { name: 'Arial', size: 10, bold: true, color: { argb: 'FFFFFFFF' } };
    header.fill = { type: 'pattern', pattern: 'solid', fgColor: { argb: 'FF123B63' } };
    header.alignment = { vertical: 'middle', horizontal: 'center', wrapText: true };
    header.height = 24;
    worksheet.views = [{ state: 'frozen', ySplit: headerRowIndex }];
    if (worksheet.rowCount >= headerRowIndex) {
      worksheet.autoFilter = {
        from: { row: headerRowIndex, column: 1 },
        to: { row: Math.max(headerRowIndex, worksheet.rowCount), column: maxColumns }
      };
    }
    worksheet.pageSetup.printTitlesRow = `${headerRowIndex}:${headerRowIndex}`;
  } else {
    worksheet.views = [{ state: 'frozen', ySplit: 1 }];
  }

  for (let rowIndex = 2; rowIndex <= worksheet.rowCount; rowIndex++) {
    const row = worksheet.getRow(rowIndex);
    row.font = { name: 'Arial', size: 10, color: { argb: 'FF1F2937' } };
    row.alignment = { vertical: 'top', wrapText: true };
    if (rowIndex !== headerRowIndex && rowIndex % 2 === 0) {
      row.fill = { type: 'pattern', pattern: 'solid', fgColor: { argb: 'FFF3F6F9' } };
    }
    row.eachCell({ includeEmpty: true }, cell => {
      cell.border = {
        top: { style: 'thin', color: { argb: 'FFD8E0E8' } },
        left: { style: 'thin', color: { argb: 'FFD8E0E8' } },
        bottom: { style: 'thin', color: { argb: 'FFD8E0E8' } },
        right: { style: 'thin', color: { argb: 'FFD8E0E8' } }
      };
    });
  }

  const configured = source['!cols'] ?? [];
  worksheet.columns = Array.from({ length: maxColumns }, (_, index) => ({
    width: Math.min(Math.max(configured[index]?.wch ?? calcularAncho(data, index), 10), 48)
  }));

  worksheet.headerFooter.oddHeader = '&C&"Arial,Bold"&10 INSTITUTO HONDUREÑO DE SEGURIDAD SOCIAL';
  worksheet.headerFooter.oddFooter = '&LSGRLA-IHSS&CGenerado: &D &T&RPágina &P de &N';
  worksheet.properties.defaultRowHeight = 18;
}

function detectarFilaEncabezado(data: unknown[][]): number {
  for (let index = 1; index < data.length; index++) {
    const values = data[index].filter(value => String(value ?? '').trim() !== '');
    if (values.length >= 3) return index + 1;
  }
  return 0;
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
