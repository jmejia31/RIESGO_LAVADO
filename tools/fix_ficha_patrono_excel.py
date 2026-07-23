from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
EXCEL_UTIL = ROOT / 'frontend/rl-app/src/app/core/utils/excel-export.util.ts'
EXCEL_TEST = ROOT / 'frontend/rl-app/src/app/core/utils/excel-export.util.spec.ts'
COMPONENT = ROOT / 'frontend/rl-app/src/app/features/admin/listas/pages/monitoreo-listas/monitoreo-listas.component.ts'

# -----------------------------------------------------------------------------
# 1. Extender el estándar Excel para fichas con pares etiqueta/valor y párrafos.
# -----------------------------------------------------------------------------
text = EXCEL_UTIL.read_text(encoding='utf-8')
old = """  '!headerRows'?: number[];
  '!sectionRows'?: number[];
  '!autoFilterRow'?: number;
}"""
new = """  '!headerRows'?: number[];
  '!sectionRows'?: number[];
  '!keyValueRows'?: number[];
  '!paragraphRows'?: number[];
  '!autoFilterRow'?: number;
}"""
if old not in text:
    raise RuntimeError('No se encontró la interfaz WorkSheet esperada.')
text = text.replace(old, new, 1)

old = """  const sectionSet = new Set(sectionRows);

  // Cuerpo: sólo las celdas que realmente existen reciben formato. Esto evita"""
new = """  const sectionSet = new Set(sectionRows);
  const keyValueRows = normalizarFilas(source['!keyValueRows'] ?? [], worksheet.rowCount);
  const paragraphRows = normalizarFilas(source['!paragraphRows'] ?? [], worksheet.rowCount);

  // Cuerpo: sólo las celdas que realmente existen reciben formato. Esto evita"""
if old not in text:
    raise RuntimeError('No se encontró la inicialización de sectionSet esperada.')
text = text.replace(old, new, 1)

anchor = """  // Todas las cabeceras tabulares usan el mismo azul y texto blanco, pero sólo
  // hasta la última columna con contenido de esa cabecera.
"""
insert = """  // Filas de información general: mantienen el aspecto de ficha del PDF.
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

""" + anchor
if anchor not in text:
    raise RuntimeError('No se encontró el anclaje de cabeceras tabulares.')
text = text.replace(anchor, insert, 1)
EXCEL_UTIL.write_text(text, encoding='utf-8')

# -----------------------------------------------------------------------------
# 2. Configurar explícitamente la Ficha Patrono para evitar detecciones erróneas.
# -----------------------------------------------------------------------------
text = COMPONENT.read_text(encoding='utf-8')
old = """  private escribirFichaExcel(data: any[][], sheetName: string, fileName: string) {
    const ws = XLSX.utils.aoa_to_sheet(data);
    this.ajustarColumnasExcel(ws, data);

    const wb = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(wb, ws, sheetName);
    XLSX.writeFile(wb, fileName);
  }"""
new = """  private escribirFichaExcel(
    data: any[][],
    sheetName: string,
    fileName: string,
    opciones: Partial<XLSX.WorkSheet> = {}
  ) {
    const ws = XLSX.utils.aoa_to_sheet(data);
    Object.assign(ws, opciones);
    this.ajustarColumnasExcel(ws, data);

    const wb = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(wb, ws, sheetName);
    XLSX.writeFile(wb, fileName);
  }"""
if old not in text:
    raise RuntimeError('No se encontró escribirFichaExcel esperado.')
text = text.replace(old, new, 1)

# Igualar etiquetas de la información general con el PDF.
replacements = {
    "['Número Patronal', row.numeroPatrono || 'N/D', 'RTN', row.rtn || 'N/D'],": "['Número Patronal:', row.numeroPatrono || 'N/D', 'RTN:', row.rtn || 'N/D'],",
    "['Nombre / Razón Social', row.nombre || 'N/D', 'Proveedor IHSS', row.esProveedorIhss || 'No'],": "['Nombre / Razón Social:', row.nombre || 'N/D', 'Proveedor IHSS:', row.esProveedorIhss || 'No'],",
    "['Lista Coincidencia', row.listaCoincidencia || 'N/D', 'Estado Monitoreo', this.obtenerEstadoMonitoreoReporte(row)],": "['Lista de Coincidencia:', row.listaCoincidencia || 'N/D', 'Estado Monitoreo:', this.obtenerEstadoMonitoreoReporte(row)],",
    "['Fecha Coincidencia', row.fechaEncontro ? this.formatDate(row.fechaEncontro) : 'N/D', 'Fecha Calificación', row.fechaCalifico ? this.formatDate(row.fechaCalifico) : 'N/D'],": "['Fecha Coincidencia:', row.fechaEncontro ? this.formatDate(row.fechaEncontro) : 'N/D', 'Fecha Calificación:', row.fechaCalifico ? this.formatDate(row.fechaCalifico) : 'N/D'],",
    "['Registro Interno', this.formatDateOrNd(this.obtenerFechaRegistroInterno(row.fechaRegistroInterno, positivo)), 'Origen del Registro', this.obtenerEtiquetaOrigenRegistro(positivo?.origenRegistro)],": "['Registro Interno:', this.formatDateOrNd(this.obtenerFechaRegistroInterno(row.fechaRegistroInterno, positivo)), 'Origen del Registro:', this.obtenerEtiquetaOrigenRegistro(positivo?.origenRegistro)],"
}
for old_value, new_value in replacements.items():
    if old_value not in text:
        raise RuntimeError(f'No se encontró la fila esperada: {old_value}')
    text = text.replace(old_value, new_value, 1)

old = """            this.escribirFichaExcel(data, 'Ficha Patrono', fileName);"""
new = """            this.escribirFichaExcel(data, 'Ficha Patrono', fileName, {
              '!headerRows': [17],
              '!sectionRows': [5, 12, 15],
              '!keyValueRows': [6, 7, 8, 9, 10],
              '!paragraphRows': [13],
              '!autoFilterRow': 17
            });"""
if old not in text:
    raise RuntimeError('No se encontró la llamada escribirFichaExcel de Patrono.')
text = text.replace(old, new, 1)
COMPONENT.write_text(text, encoding='utf-8')

# -----------------------------------------------------------------------------
# 3. Prueba de regresión con el mismo escenario del archivo reportado.
# -----------------------------------------------------------------------------
text = EXCEL_TEST.read_text(encoding='utf-8')
closing = '\n});\n'
if not text.endswith(closing):
    raise RuntimeError('No se encontró el cierre del describe de Excel.')
new_test = r'''

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
'''
text = text[:-len(closing)] + new_test + closing
EXCEL_TEST.write_text(text, encoding='utf-8')

print('Corrección de Ficha Patrono aplicada correctamente.')
