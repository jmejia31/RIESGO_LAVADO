from __future__ import annotations

import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
COMPONENT = ROOT / 'frontend/rl-app/src/app/features/admin/listas/pages/monitoreo-listas/monitoreo-listas.component.ts'
EXCEL_UTIL = ROOT / 'frontend/rl-app/src/app/core/utils/excel-export.util.ts'
REPORT_UTIL = ROOT / 'frontend/rl-app/src/app/core/reporting/institutional-report.util.ts'
REPORT_SPEC = ROOT / 'frontend/rl-app/src/app/core/reporting/institutional-report.util.spec.ts'
DOC_ROOT = ROOT / 'docs/3. Módulo Matrices de Riesgos/Fase 12 - Mejora ejecutiva UXUI y mapa de calor'
EVIDENCE = DOC_ROOT / 'Evidencia_Fase_12_5_2'


def replace_once(text: str, old: str, new: str, label: str) -> str:
    if old not in text:
        raise RuntimeError(f'No se encontró el anclaje requerido: {label}')
    return text.replace(old, new, 1)


def replace_regex(text: str, pattern: str, replacement: str, label: str, count: int = 1) -> str:
    updated, total = re.subn(pattern, replacement, text, count=count, flags=re.S)
    if total != count:
        raise RuntimeError(f'Reemplazo incompleto para {label}: esperado {count}, aplicado {total}')
    return updated


def write_report_utility() -> None:
    REPORT_UTIL.parent.mkdir(parents=True, exist_ok=True)
    REPORT_UTIL.write_text(r'''import { jsPDF } from 'jspdf';
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

export function autoTableInstitucional(doc: jsPDF, options: Record<string, any>): void {
  const originalDidDrawPage = options.didDrawPage;
  const margin = { top: 24, bottom: 19, left: 14, right: 14, ...(options.margin ?? {}) };

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
      ...(options.styles ?? {})
    },
    headStyles: {
      fillColor: COLORES_REPORTE_INSTITUCIONAL.navy,
      textColor: COLORES_REPORTE_INSTITUCIONAL.white,
      fontStyle: 'bold',
      ...(options.headStyles ?? {})
    },
    alternateRowStyles: {
      fillColor: COLORES_REPORTE_INSTITUCIONAL.alternate,
      ...(options.alternateRowStyles ?? {})
    },
    didDrawPage: (data: any) => {
      if (data.pageNumber > 1) dibujarEncabezadoCompacto(doc);
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


def write_excel_utility() -> None:
    EXCEL_UTIL.write_text(r'''import * as ExcelJS from 'exceljs';

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
''', encoding='utf-8')


def transform_component() -> dict:
    text = COMPONENT.read_text(encoding='utf-8')
    original_auto_tables = text.count('autoTable(doc, {')

    text = text.replace("import autoTable from 'jspdf-autotable';\n", '')
    import_anchor = "import * as XLSX from '../../../../../core/utils/excel-export.util';\n"
    import_block = import_anchor + (
        "import { agregarEncabezadoInstitucionalPdf, agregarPiesInstitucionalesPdf, "
        "asegurarEspacioSeccionPdf, autoTableInstitucional } from "
        "'../../../../../core/reporting/institutional-report.util';\n"
    )
    text = replace_once(text, import_anchor, import_block, 'importación de utilitario institucional')
    text = text.replace('autoTable(doc, {', 'autoTableInstitucional(doc, {')

    text = replace_regex(
        text,
        r"  private agregarEncabezadoPdf\(doc: jsPDF, titulo: string\) \{.*?\n  \}\n\n  // Encabezado exclusivo",
        "  private agregarEncabezadoPdf(doc: jsPDF, titulo: string): number {\n"
        "    const institucion = this.configService.configSistema()?.nombreInstitucion || 'Instituto Hondureño de Seguridad Social';\n"
        "    const sistema = this.configService.configSistema()?.nombreSistema || 'SGRLA-IHSS';\n"
        "    return agregarEncabezadoInstitucionalPdf(doc, titulo, institucion, sistema);\n"
        "  }\n\n  // Encabezado exclusivo",
        'encabezado vertical compartido'
    )

    text = replace_regex(
        text,
        r"  private agregarEncabezadoReporteMonitoreoPdf\(doc: jsPDF, titulo: string\): number \{.*?\n  \}\n\n  private agregarDatosMemo",
        "  private agregarEncabezadoReporteMonitoreoPdf(doc: jsPDF, titulo: string): number {\n"
        "    const institucion = this.configService.configSistema()?.nombreInstitucion || 'Instituto Hondureño de Seguridad Social';\n"
        "    const sistema = this.configService.configSistema()?.nombreSistema || 'SGRLA-IHSS';\n"
        "    return agregarEncabezadoInstitucionalPdf(doc, titulo, institucion, sistema, this.obtenerResumenFiltrosPrincipales());\n"
        "  }\n\n  private agregarDatosMemo",
        'encabezado horizontal compartido'
    )

    for signature in (
        "  private agregarDatosMemo(doc: jsPDF, y: number, titulo: string, generalData: string[][]): number {\n",
        "  private agregarMotivoPdf(doc: jsPDF, y: number, titulo: string, motivoTexto: string): number {\n",
        "  private agregarSeguimientosPdf(doc: jsPDF, y: number, titulo: string, seguimientos: Seguimiento[], mensajeVacio: string, rango: RangoSeguimientoReporte): number {\n",
    ):
        text = replace_once(text, signature, signature + '    y = asegurarEspacioSeccionPdf(doc, y, 30);\n', f'espacio de sección {signature.strip()}')

    text = replace_regex(
        text,
        r"  private abrirPdf\(doc: jsPDF\) \{.*?\n  \}",
        "  private abrirPdf(doc: jsPDF) {\n"
        "    agregarPiesInstitucionalesPdf(doc);\n"
        "    const blob = doc.output('blob');\n"
        "    const url = URL.createObjectURL(blob);\n"
        "    this.pdfUrl.set(this.sanitizer.bypassSecurityTrustResourceUrl(url));\n"
        "    this.pdfModalAbierto.set(true);\n"
        "  }",
        'finalización común de PDF'
    )

    header_patterns = {
        'REPORTE INTEGRAL DE PERSONA NATURAL': r"    const institucion = this\.configService\.configSistema\(\)\?\.nombreInstitucion \|\| 'Instituto Hondureño de Seguridad Social';\n    const sistema = this\.configService\.configSistema\(\)\?\.nombreSistema \|\| 'Sistema de Monitoreo RIESGO IHSS';\n\n    // Banner de encabezado.*?    doc\.text\(`\$\{sistema\}  \|  Fecha de Generación: \$\{new Date\(\)\.toLocaleString\(\)\}`, 14, 30\);",
        'REPORTE INTEGRAL DE EMPLEADO IHSS': r"    const institucion = this\.configService\.configSistema\(\)\?\.nombreInstitucion \|\| 'Instituto Hondureño de Seguridad Social';\n    const sistema = this\.configService\.configSistema\(\)\?\.nombreSistema \|\| 'Sistema de Monitoreo RIESGO IHSS';\n\n    // Banner de encabezado.*?    doc\.text\(`\$\{sistema\}  \|  Fecha de Generación: \$\{new Date\(\)\.toLocaleString\(\)\}`, 14, 30\);",
        'REPORTE INTEGRAL DE PATRONO': r"    const institucion = this\.configService\.configSistema\(\)\?\.nombreInstitucion \|\| 'Instituto Hondureño de Seguridad Social';\n    const sistema = this\.configService\.configSistema\(\)\?\.nombreSistema \|\| 'Sistema de Monitoreo RIESGO IHSS';\n\n    // Banner de encabezado.*?    doc\.text\(`\$\{sistema\}  \|  Fecha de Generación: \$\{new Date\(\)\.toLocaleString\(\)\}`, 14, 30\);",
    }

    # Los tres bloques son estructuralmente iguales; se sustituyen por orden de aparición y título conocido.
    generic_header = r"    const institucion = this\.configService\.configSistema\(\)\?\.nombreInstitucion \|\| 'Instituto Hondureño de Seguridad Social';\n    const sistema = this\.configService\.configSistema\(\)\?\.nombreSistema \|\| 'Sistema de Monitoreo RIESGO IHSS';\n\n    // Banner de encabezado.*?    doc\.text\(`\$\{sistema\}  \|  Fecha de Generación: \$\{new Date\(\)\.toLocaleString\(\)\}`, 14, 30\);"
    titles = ['REPORTE INTEGRAL DE PERSONA NATURAL', 'REPORTE INTEGRAL DE EMPLEADO IHSS', 'REPORTE INTEGRAL DE PATRONO']
    for title in titles:
        text = replace_regex(text, generic_header, f"    this.agregarEncabezadoPdf(doc, '{title}');", f'encabezado {title}', count=1)

    text = replace_regex(
        text,
        r"    // Encabezado\n.*?    // Información de la persona",
        "    this.agregarEncabezadoPdf(\n"
        "      doc,\n"
        "      isEmpleado\n"
        "        ? 'REPORTE DETALLADO DE COINCIDENCIAS - EMPLEADO IHSS'\n"
        "        : 'REPORTE DETALLADO DE COINCIDENCIAS - PERSONA NATURAL'\n"
        "    );\n\n    // Información de la persona",
        'vista previa de detalle'
    )

    # Toda salida PDF, incluida la vista previa antigua, recibe pie y numeración. Es idempotente.
    text = text.replace("    const blob = doc.output('blob');", "    agregarPiesInstitucionalesPdf(doc);\n    const blob = doc.output('blob');")

    COMPONENT.write_text(text, encoding='utf-8')
    return {
        'auto_tables_originales': original_auto_tables,
        'auto_tables_institucionales': text.count('autoTableInstitucional(doc, {'),
        'encabezados_compartidos': text.count('agregarEncabezadoInstitucionalPdf('),
        'pies_compartidos': text.count('agregarPiesInstitucionalesPdf(doc)')
    }


def write_tests() -> None:
    REPORT_SPEC.write_text(r'''import { describe, expect, it } from 'vitest';
import { jsPDF } from 'jspdf';
import {
  agregarEncabezadoInstitucionalPdf,
  agregarPiesInstitucionalesPdf,
  asegurarEspacioSeccionPdf,
  autoTableInstitucional,
  COLORES_REPORTE_INSTITUCIONAL
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

  it('repite encabezados y evita dividir filas en tablas de varias páginas', () => {
    const doc = new jsPDF({ orientation: 'portrait', unit: 'mm', format: 'a4' });
    agregarEncabezadoInstitucionalPdf(doc, 'REPORTE EXTENSO');
    autoTableInstitucional(doc, {
      startY: 44,
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


def write_documentation(metrics: dict) -> None:
    EVIDENCE.mkdir(parents=True, exist_ok=True)
    evidence = {
        'fase': '12.5.2',
        'objetivo': 'Normalización institucional de Monitoreo de Listas',
        'estado': 'generado_pendiente_ci',
        'cambios': {
            'pdf_estandar_compartido': True,
            'pdf_vertical_horizontal_mismo_encabezado': True,
            'filas_indivisibles': True,
            'encabezados_repetidos': True,
            'pies_y_numeracion': True,
            'excel_openxml_real': True,
            'excel_autofiltro': True,
            'excel_panel_congelado': True,
            'excel_configuracion_impresion': True,
            'vista_previa_mismo_formato': True
        },
        'metricas_transformacion': metrics,
        'dependencia_excel': 'exceljs@4.4.0',
        'restricciones': [
            'No modificar DNP',
            'No tocar CONTROL_ALMACEN.PROVEEDOR',
            'No integrar Monitoreo de Listas con Matrices de Riesgos',
            'No fusionar a main'
        ]
    }
    (EVIDENCE / 'fase12_5_2_normalizacion_monitoreo.json').write_text(
        json.dumps(evidence, ensure_ascii=False, indent=2) + '\n', encoding='utf-8'
    )

    (DOC_ROOT / 'Fase_12_5_2_Normalizacion_Monitoreo_Listas.md').write_text(f'''# Fase 12.5.2 — Normalización de Monitoreo de Listas

## Objetivo

Convertir Monitoreo de Listas en el patrón institucional definitivo para PDF y Excel antes de migrar Matrices de Riesgos.

## PDF institucional

- Encabezado único azul marino para orientación vertical y horizontal.
- Nombre institucional, título propio, sistema y fecha/hora.
- Caja de filtros en reportes generales.
- Encabezado compacto en páginas de continuación.
- Encabezados de tabla repetidos.
- `rowPageBreak: avoid` para impedir que un registro se divida entre páginas.
- Pie institucional con fecha y `Página X de Y`.
- Vista previa y descarga utilizan la misma composición visual.

## Excel institucional

Se sustituyó el SpreadsheetML/XML renombrado como `.xls` por un libro OpenXML `.xlsx` real mediante `exceljs@4.4.0`.

Cada hoja aplica:

- Propiedades institucionales del libro.
- Encabezado azul marino.
- Texto blanco y tipografía Arial.
- Filas alternadas.
- Bordes y ajuste de texto.
- Autofiltro.
- Panel congelado.
- Orientación vertical u horizontal según columnas.
- Ajuste a una página de ancho.
- Encabezado y pie de impresión.
- Extensión `.xlsx` real.

## Alcance técnico

- Tablas normalizadas detectadas: **{metrics['auto_tables_institucionales']}**.
- Encabezados compartidos en el componente: **{metrics['encabezados_compartidos']}**.
- Finalizaciones institucionales de PDF: **{metrics['pies_compartidos']}**.

## Restricciones

No se modifica DNP, `CONTROL_ALMACEN.PROVEEDOR`, el cálculo de Matrices de Riesgos ni `main`.

## Criterios de salida

- Backend, frontend, build y E2E aprobados.
- Pruebas del estándar PDF aprobadas.
- Excel real generado sin conversión a `.xls`.
- Sin scripts, workflows ni activadores temporales en el commit funcional.
''', encoding='utf-8')


def main() -> None:
    write_report_utility()
    write_excel_utility()
    metrics = transform_component()
    write_tests()
    write_documentation(metrics)
    print(json.dumps(metrics, ensure_ascii=False))


if __name__ == '__main__':
    main()
