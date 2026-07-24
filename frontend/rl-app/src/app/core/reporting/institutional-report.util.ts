import { jsPDF } from 'jspdf';
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
