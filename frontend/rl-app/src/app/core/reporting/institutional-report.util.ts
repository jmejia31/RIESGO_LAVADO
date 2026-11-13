import { jsPDF } from 'jspdf';
import autoTable from 'jspdf-autotable';
import {
  INSTITUTIONAL_LOGO_ASSET_PATH,
  INSTITUTIONAL_LOGO_ASPECT_RATIO,
  INSTITUTIONAL_REPORT_COLORS,
  INSTITUTIONAL_REPORT_LAYOUT
} from './institutional-report-standard';

export const COLORES_REPORTE_INSTITUCIONAL = INSTITUTIONAL_REPORT_COLORS;

export const MARGEN_SUPERIOR_TABLA_CONTINUACION_MM = INSTITUTIONAL_REPORT_LAYOUT.pdfContinuationTop;

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
  const leftWidth = width * 0.62 - INSTITUTIONAL_REPORT_LAYOUT.pdfMargin;
  const titleLines = doc.splitTextToSize(meta.titulo, leftWidth);
  const height = Math.max(18, 8 + titleLines.length * 4.5);
  doc.setFillColor(...COLORES_REPORTE_INSTITUCIONAL.navyDark);
  doc.rect(0, 0, width, height, 'F');
  doc.setFont('helvetica', 'bold');
  doc.setFontSize(8.5);
  doc.setTextColor(...COLORES_REPORTE_INSTITUCIONAL.white);
  doc.text(meta.institucion, INSTITUTIONAL_REPORT_LAYOUT.pdfMargin, 5.5);
  doc.setFont('helvetica', 'normal');
  doc.setFontSize(7.5);
  doc.text(titleLines, INSTITUTIONAL_REPORT_LAYOUT.pdfMargin, 10.5);
  dibujarLogoInstitucionalPdf(doc, width, 4, 32, 12);
  doc.text(meta.sistema, width - INSTITUTIONAL_REPORT_LAYOUT.pdfMargin, 22, { align: 'right' });
}

/** Inserta el PNG canónico en el documento real; no agrega una capa HTML de marca. */
export function dibujarLogoInstitucionalPdf(
  doc: jsPDF,
  pageWidth: number,
  top: number,
  requestedWidth: number,
  requestedHeight?: number
): boolean {
  // jsPDF carga la ruta same-origin de forma síncrona; en las pruebas Node no
  // existe un navegador y se verifica el contrato de la utilidad sin I/O web.
  if (!esNavegadorReal()) return false;
  const width = requestedWidth;
  const height = requestedHeight ?? width / INSTITUTIONAL_LOGO_ASPECT_RATIO;
  const x = pageWidth - INSTITUTIONAL_REPORT_LAYOUT.pdfMargin - width;
  doc.setFillColor(...INSTITUTIONAL_REPORT_COLORS.white);
  doc.rect(x, top, width, height, 'F');
  doc.addImage(INSTITUTIONAL_LOGO_ASSET_PATH, 'PNG', x, top, width, height, 'IHSS_LOGO', 'FAST');
  return true;
}

function esNavegadorReal(): boolean {
  if (typeof window === 'undefined') return false;
  const browserWindow = window as Window & { document?: Document };
  const userAgent = browserWindow.navigator?.userAgent ?? '';
  return typeof browserWindow.document?.createElement === 'function' && !/jsdom/i.test(userAgent);
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

  const margin = INSTITUTIONAL_REPORT_LAYOUT.pdfMargin;
  const leftWidth = width * 0.62 - margin;
  const rightWidth = width - margin * 2 - width * 0.66;
  doc.setFont('helvetica', 'bold');
  doc.setFontSize(12);
  const institutionLines = doc.splitTextToSize(meta.institucion, leftWidth);
  doc.setFontSize(17);
  const titleLines = doc.splitTextToSize(meta.titulo, leftWidth);
  doc.setFont('helvetica', 'normal');
  doc.setFontSize(8.5);
  const metadataLines = doc.splitTextToSize(`${sistema} | Generado: ${fecha}`, rightWidth);
  const logoWidth = Math.min(48, Math.max(34, width * 0.16));
  const logoHeight = logoWidth / INSTITUTIONAL_LOGO_ASPECT_RATIO;
  const headerHeight = Math.max(
    INSTITUTIONAL_REPORT_LAYOUT.pdfHeaderMinimumHeight,
    8 + institutionLines.length * 5 + 5 + titleLines.length * 7,
    12 + metadataLines.length * 4.5,
    logoHeight + 28
  );
  doc.setFillColor(...COLORES_REPORTE_INSTITUCIONAL.navy);
  doc.rect(0, 0, width, headerHeight, 'F');
  doc.setFont('helvetica', 'bold');
  doc.setFontSize(12);
  doc.setTextColor(...COLORES_REPORTE_INSTITUCIONAL.white);
  doc.text(institutionLines, margin, 9);
  doc.setFontSize(17);
  doc.text(titleLines, margin, 9 + institutionLines.length * 5 + 6);
  doc.setFont('helvetica', 'normal');
  doc.setFontSize(8.5);
  doc.setTextColor(220, 230, 240);
  dibujarLogoInstitucionalPdf(doc, width, 4, logoWidth, logoHeight);
  doc.text(metadataLines, width - margin - rightWidth, Math.max(24, logoHeight + 9), { align: 'left' });

  if (!filtros) return headerHeight + 10;

  const filterLines = doc.splitTextToSize(`Filtros aplicados: ${filtros}`, width - 38);
  const filterHeight = Math.max(14, 7 + filterLines.length * 4.2);
  doc.setFillColor(...COLORES_REPORTE_INSTITUCIONAL.filterBackground);
  doc.roundedRect(margin, headerHeight + 7, width - margin * 2, filterHeight, 2, 2, 'F');
  doc.setFont('helvetica', 'normal');
  doc.setFontSize(8.5);
  doc.setTextColor(...COLORES_REPORTE_INSTITUCIONAL.filterText);
  doc.text(filterLines, margin + 5, headerHeight + 7 + 7);
  return headerHeight + filterHeight + 17;
}

export function resolverMargenesTablaInstitucional(margin?: Record<string, number>): Record<string, number> {
  return {
    bottom: 19,
    left: INSTITUTIONAL_REPORT_LAYOUT.pdfMargin,
    right: INSTITUTIONAL_REPORT_LAYOUT.pdfMargin,
    ...(margin ?? {}),
    // startY gobierna la primera página. En páginas siguientes el margen superior
    // siempre queda junto al encabezado compacto y nunca hereda valores como 50 mm.
    top: MARGEN_SUPERIOR_TABLA_CONTINUACION_MM
  };
}

export function resolverAnchoUtilPdf(doc: jsPDF, margin: Record<string, number>): number {
  return Math.max(1, doc.internal.pageSize.getWidth() - margin['left'] - margin['right']);
}

export function resolverAnchoTablaInstitucional(doc: jsPDF, requestedWidth: number | undefined, margin: Record<string, number>): number {
  const usableWidth = resolverAnchoUtilPdf(doc, margin);
  return Math.min(typeof requestedWidth === 'number' ? requestedWidth : usableWidth, usableWidth);
}

function limitarAnchosColumnas(columnStyles: Record<string, any> | undefined, usableWidth: number): Record<string, any> | undefined {
  if (!columnStyles) return undefined;
  const entries = Object.entries(columnStyles);
  const explicitWidth = entries.reduce((total, [, style]) => total + (typeof style?.cellWidth === 'number' ? style.cellWidth : 0), 0);
  if (explicitWidth <= usableWidth || explicitWidth === 0) return columnStyles;
  const ratio = usableWidth / explicitWidth;
  const scaled: Array<[string, any]> = entries.map(([key, style]) => [
    key,
    typeof style?.cellWidth === 'number'
      ? { ...style, cellWidth: Math.max(4, style.cellWidth * ratio) }
      : style
  ]);
  return Object.fromEntries(scaled);
}

export function autoTableInstitucional(doc: jsPDF, options: Record<string, any>): void {
  const originalDidDrawPage = options['didDrawPage'];
  const margin = resolverMargenesTablaInstitucional(options['margin']);
  const usableWidth = resolverAnchoUtilPdf(doc, margin);
  const tableWidth = resolverAnchoTablaInstitucional(doc, options['tableWidth'], margin);

  autoTable(doc, {
    ...options,
    tableWidth,
    columnStyles: limitarAnchosColumnas(options['columnStyles'], usableWidth),
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
    doc.line(INSTITUTIONAL_REPORT_LAYOUT.pdfMargin, height - INSTITUTIONAL_REPORT_LAYOUT.pdfFooterMargin, width - INSTITUTIONAL_REPORT_LAYOUT.pdfMargin, height - INSTITUTIONAL_REPORT_LAYOUT.pdfFooterMargin);
    doc.setFont('helvetica', 'normal');
    doc.setFontSize(7.5);
    doc.setTextColor(...COLORES_REPORTE_INSTITUCIONAL.muted);
    doc.text(meta.sistema, INSTITUTIONAL_REPORT_LAYOUT.pdfMargin, height - 7.5);
    doc.text(`Generado: ${meta.fecha}`, width / 2, height - 7.5, { align: 'center' });
    doc.text(`Página ${page} de ${total}`, width - INSTITUTIONAL_REPORT_LAYOUT.pdfMargin, height - 7.5, { align: 'right' });
  }

  (doc as any).__reporteInstitucionalFinalizado = true;
}
