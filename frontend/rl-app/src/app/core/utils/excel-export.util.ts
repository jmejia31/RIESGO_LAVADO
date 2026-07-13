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
    workbook.sheets.push({
      name: normalizarNombreHoja(name),
      sheet: worksheet
    });
  }
};

export function writeFile(workbook: WorkBook, fileName: string): void {
  const contenido = generarSpreadsheetXml(workbook);
  const blob = new Blob([contenido], {
    type: 'application/vnd.ms-excel;charset=utf-8'
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

function generarSpreadsheetXml(workbook: WorkBook): string {
  const hojas = workbook.sheets.length ? workbook.sheets : [{ name: 'Reporte', sheet: { data: [] } }];
  return `<?xml version="1.0" encoding="UTF-8"?>
<?mso-application progid="Excel.Sheet"?>
<Workbook xmlns="urn:schemas-microsoft-com:office:spreadsheet"
 xmlns:o="urn:schemas-microsoft-com:office:office"
 xmlns:x="urn:schemas-microsoft-com:office:excel"
 xmlns:ss="urn:schemas-microsoft-com:office:spreadsheet">
 <Styles>
  <Style ss:ID="Header"><Font ss:Bold="1"/><Interior ss:Color="#D9EAF7" ss:Pattern="Solid"/></Style>
  <Style ss:ID="Title"><Font ss:Bold="1" ss:Size="13"/></Style>
 </Styles>
 ${hojas.map(({ name, sheet }) => generarWorksheetXml(name, sheet)).join('\n')}
</Workbook>`;
}

function generarWorksheetXml(name: string, sheet: WorkSheet): string {
  const columnas = sheet['!cols'] || [];
  const columnasXml = columnas
    .map(col => `<Column ss:AutoFitWidth="0" ss:Width="${Math.max((col.wch || 12) * 6, 48)}"/>`)
    .join('');
  const filasXml = sheet.data.map((fila, index) => generarRowXml(fila, index)).join('\n');
  return `<Worksheet ss:Name="${escaparXml(name)}">
  <Table>
   ${columnasXml}
   ${filasXml}
  </Table>
 </Worksheet>`;
}

function generarRowXml(fila: unknown[], index: number): string {
  const style = index === 0 ? ' ss:StyleID="Title"' : '';
  const celdas = fila.map(valor => generarCellXml(valor, index)).join('');
  return `<Row${style}>${celdas}</Row>`;
}

function generarCellXml(valor: unknown, rowIndex: number): string {
  const tipo = typeof valor === 'number' && Number.isFinite(valor) ? 'Number' : 'String';
  const contenido = tipo === 'Number' ? String(valor) : escaparXml(String(valor ?? ''));
  const style = rowIndex > 0 && esEncabezado(String(valor ?? '')) ? ' ss:StyleID="Header"' : '';
  return `<Cell${style}><Data ss:Type="${tipo}">${contenido}</Data></Cell>`;
}

function esEncabezado(valor: string): boolean {
  return /^[A-ZÁÉÍÓÚÑ0-9 /()._-]+$/.test(valor) && valor.length <= 60;
}

function normalizarNombreHoja(name: string): string {
  const limpio = (name || 'Reporte').replace(/[\\/?*[\]:]/g, ' ').trim();
  return (limpio || 'Reporte').slice(0, 31);
}

function normalizarNombreArchivo(fileName: string): string {
  return fileName.replace(/\.xlsx$/i, '.xls');
}

function escaparXml(valor: string): string {
  return valor
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&apos;');
}
