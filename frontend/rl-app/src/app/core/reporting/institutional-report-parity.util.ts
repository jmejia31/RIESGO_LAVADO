import * as XLSX from '../utils/excel-export.util';

export type InstitutionalReportCell = string | number | boolean | null | undefined;

export type InstitutionalReportSection =
  | {
      kind: 'keyValue';
      title?: string;
      rows: InstitutionalReportCell[][];
    }
  | {
      kind: 'paragraph';
      title?: string;
      text: string;
    }
  | {
      kind: 'table';
      title?: string;
      headers: string[];
      rows: InstitutionalReportCell[][];
      emptyText?: string;
    }
  | {
      kind: 'history';
      title?: string;
      rangeText: string;
      headers: string[];
      rows: InstitutionalReportCell[][];
      emptyText: string;
    };

export interface InstitutionalReportDefinition {
  title: string;
  institution: string;
  systemName: string;
  generatedAt: Date;
  sections: InstitutionalReportSection[];
}

export interface InstitutionalExcelReport {
  data: InstitutionalReportCell[][];
  worksheet: XLSX.WorkSheet;
}

/**
 * Construye un XLSX institucional a partir de la misma estructura lógica que
 * presenta el PDF: encabezado, secciones numeradas, datos generales, tablas,
 * textos explicativos e historial. Este contrato es reutilizable por cualquier
 * módulo que exponga el mismo reporte en PDF y Excel.
 */
export function construirExcelInstitucionalDesdeReporte(
  report: InstitutionalReportDefinition
): InstitutionalExcelReport {
  const data: InstitutionalReportCell[][] = [
    [report.title],
    [report.institution],
    [`${report.systemName} | Generado: ${report.generatedAt.toLocaleString()}`],
    []
  ];

  const headerRows: number[] = [];
  const sectionRows: number[] = [];
  const keyValueRows: number[] = [];
  const paragraphRows: number[] = [2, 3];
  let autoFilterRow = 0;

  const pushRow = (row: InstitutionalReportCell[]): number => {
    data.push(row);
    return data.length;
  };

  const pushSectionTitle = (title?: string): void => {
    if (!title) return;
    sectionRows.push(pushRow([title]));
  };

  for (const section of report.sections) {
    pushSectionTitle(section.title);

    if (section.kind === 'keyValue') {
      for (const row of section.rows) {
        keyValueRows.push(pushRow(row));
      }
    }

    if (section.kind === 'paragraph') {
      paragraphRows.push(pushRow([section.text]));
    }

    if (section.kind === 'table') {
      if (section.rows.length > 0) {
        const headerRow = pushRow(section.headers);
        headerRows.push(headerRow);
        if (!autoFilterRow) autoFilterRow = headerRow;
        section.rows.forEach(row => pushRow(row));
      } else {
        paragraphRows.push(pushRow([section.emptyText || 'No existen registros para mostrar.']));
      }
    }

    if (section.kind === 'history') {
      keyValueRows.push(pushRow(['Rango de seguimientos:', section.rangeText]));
      if (section.rows.length > 0) {
        const headerRow = pushRow(section.headers);
        headerRows.push(headerRow);
        if (!autoFilterRow) autoFilterRow = headerRow;
        section.rows.forEach(row => pushRow(row));
      } else {
        paragraphRows.push(pushRow([section.emptyText]));
      }
    }

    pushRow([]);
  }

  while (
    data.length &&
    data[data.length - 1].every(value => value === null || value === undefined || value === '')
  ) {
    data.pop();
  }

  const worksheet = XLSX.utils.aoa_to_sheet(data);
  worksheet['!headerRows'] = headerRows;
  worksheet['!sectionRows'] = sectionRows;
  worksheet['!keyValueRows'] = keyValueRows;
  worksheet['!paragraphRows'] = paragraphRows;
  worksheet['!autoFilterRow'] = autoFilterRow;

  const maxColumns = Math.max(1, ...data.map(row => row.length));
  worksheet['!cols'] = Array.from({ length: maxColumns }, (_, index) => {
    const maxLength = data.reduce((max, row) => {
      const value = row[index];
      return Math.max(max, value === null || value === undefined ? 0 : String(value).length);
    }, 0);
    return { wch: Math.min(Math.max(maxLength + 2, 12), 45) };
  });

  return { data, worksheet };
}

export function crearLibroExcelInstitucionalDesdeReporte(
  report: InstitutionalReportDefinition,
  sheetName: string
): XLSX.WorkBook {
  const workbook = XLSX.utils.book_new();
  const { worksheet } = construirExcelInstitucionalDesdeReporte(report);
  XLSX.utils.book_append_sheet(workbook, worksheet, sheetName);
  return workbook;
}
