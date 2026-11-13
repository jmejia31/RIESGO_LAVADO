export const INSTITUTIONAL_REPORT_COLORS = {
  navy: [18, 59, 99] as [number, number, number],
  navyDark: [11, 46, 79] as [number, number, number],
  white: [255, 255, 255] as [number, number, number],
  body: [31, 41, 55] as [number, number, number],
  muted: [100, 116, 139] as [number, number, number],
  alternate: [243, 246, 249] as [number, number, number],
  border: [216, 224, 232] as [number, number, number],
  filterBackground: [239, 246, 255] as [number, number, number],
  filterText: [30, 64, 175] as [number, number, number]
} as const;

export const INSTITUTIONAL_REPORT_EXCEL_COLORS = {
  navy: 'FF123B63',
  white: 'FFFFFFFF',
  body: 'FF1F2937',
  alternate: 'FFF3F6F9',
  border: 'FFD8E0E8'
} as const;

export const INSTITUTIONAL_REPORT_LAYOUT = {
  pdfMargin: 14,
  pdfFooterMargin: 13,
  pdfContinuationTop: 20,
  pdfHeaderPadding: 14,
  pdfHeaderMinimumHeight: 34
} as const;
