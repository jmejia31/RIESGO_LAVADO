export const INSTITUTIONAL_REPORT_COLORS = {
  navy: [18, 59, 99] as [number, number, number],
  navyDark: [11, 46, 79] as [number, number, number],
  white: [255, 255, 255] as [number, number, number],
  black: [0, 0, 0] as [number, number, number],
  body: [0, 0, 0] as [number, number, number],
  sectionTitle: [0, 0, 0] as [number, number, number],
  label: [0, 0, 0] as [number, number, number],
  tableBodyForeground: [0, 0, 0] as [number, number, number],
  muted: [100, 116, 139] as [number, number, number],
  alternate: [243, 246, 249] as [number, number, number],
  border: [216, 224, 232] as [number, number, number],
  filterBackground: [239, 246, 255] as [number, number, number],
  filterText: [0, 0, 0] as [number, number, number]
} as const;

export const INSTITUTIONAL_REPORT_EXCEL_COLORS = {
  navy: 'FF123B63',
  white: 'FFFFFFFF',
  black: 'FF000000',
  body: 'FF000000',
  sectionTitle: 'FF000000',
  label: 'FF000000',
  tableBodyForeground: 'FF000000',
  alternate: 'FFF3F6F9',
  border: 'FFD8E0E8'
} as const;

export const INSTITUTIONAL_REPORT_LAYOUT = {
  pdfMargin: 14,
  pdfFooterMargin: 13,
  // Reserva el encabezado compacto y una separación visual antes de tablas
  // o secciones que continúan en páginas posteriores.
  pdfContinuationHeaderHeight: 34,
  pdfContinuationTop: 40,
  pdfHeaderPadding: 14,
  pdfHeaderMinimumHeight: 34
} as const;

/** Medidas de presentación B06; se utiliza siempre el ratio del logo configurado. */
/** Ancho físico canónico del logo en todos los encabezados PDF (milímetros). */
export const INSTITUTIONAL_PDF_LOGO_WIDTH_MM = 72.644;
export const INSTITUTIONAL_LOGO_POLICY = {
  pdfWidthMm: INSTITUTIONAL_PDF_LOGO_WIDTH_MM,
  pdfMaxHeightMm: 32,
  excelWidthPx: 156,
  excelMaxHeightPx: 56
} as const;

export interface InstitutionalReportBranding {
  institutionName: string;
  systemName: string;
  logoUrl: string;
  logoAspectRatio: number;
  headerColor: [number, number, number];
  headerTextColor: [number, number, number];
}

const DEFAULT_BRANDING: InstitutionalReportBranding = {
  institutionName: 'INSTITUTO HONDUREÑO DE SEGURIDAD SOCIAL',
  systemName: 'SGRLA-IHSS',
  logoUrl: '',
  logoAspectRatio: 1,
  headerColor: INSTITUTIONAL_REPORT_COLORS.navy,
  headerTextColor: INSTITUTIONAL_REPORT_COLORS.white
};

let currentBranding: InstitutionalReportBranding = DEFAULT_BRANDING;

export function getInstitutionalReportBranding(): InstitutionalReportBranding {
  return currentBranding;
}

export function setInstitutionalReportBranding(value: Partial<InstitutionalReportBranding>): void {
  currentBranding = { ...currentBranding, ...value };
}
