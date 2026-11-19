import { describe, expect, it } from 'vitest';
import {
  INSTITUTIONAL_LOGO_POLICY,
  INSTITUTIONAL_PDF_LOGO_WIDTH_MM,
  INSTITUTIONAL_REPORT_COLORS,
  INSTITUTIONAL_REPORT_EXCEL_COLORS,
  getInstitutionalReportBranding,
  setInstitutionalReportBranding
} from './institutional-report-standard';
import { bestContrastForeground, contrastRatio, parseHexColor } from '../configuration/system-theme.util';

describe('contrato institucional de identidad para reportes', () => {
  it('mantiene una escala uniforme de logo y conserva la proporción', () => {
    expect(INSTITUTIONAL_LOGO_POLICY.pdfWidthMm).toBe(INSTITUTIONAL_PDF_LOGO_WIDTH_MM);
    expect(INSTITUTIONAL_LOGO_POLICY.excelWidthPx).toBe(156);
  });

  it.each(['#FFFFFF', '#000000', '#123B63', '#777777', '#767676', '#F3F6F9'])('elige un texto AA para %s', background => {
    const rgb = parseHexColor(background)!;
    const foreground = bestContrastForeground(rgb);
    expect(contrastRatio(rgb, foreground)).toBeGreaterThanOrEqual(4.5);
  });

  it('define tokens explícitos de color negro para cuerpo, secciones y etiquetas en PDF y Excel', () => {
    expect(INSTITUTIONAL_REPORT_COLORS.black).toEqual([0, 0, 0]);
    expect(INSTITUTIONAL_REPORT_COLORS.body).toEqual([0, 0, 0]);
    expect(INSTITUTIONAL_REPORT_COLORS.sectionTitle).toEqual([0, 0, 0]);
    expect(INSTITUTIONAL_REPORT_COLORS.label).toEqual([0, 0, 0]);
    expect(INSTITUTIONAL_REPORT_COLORS.tableBodyForeground).toEqual([0, 0, 0]);

    expect(INSTITUTIONAL_REPORT_EXCEL_COLORS.black).toBe('FF000000');
    expect(INSTITUTIONAL_REPORT_EXCEL_COLORS.body).toBe('FF000000');
    expect(INSTITUTIONAL_REPORT_EXCEL_COLORS.sectionTitle).toBe('FF000000');
    expect(INSTITUTIONAL_REPORT_EXCEL_COLORS.label).toBe('FF000000');
    expect(INSTITUTIONAL_REPORT_EXCEL_COLORS.tableBodyForeground).toBe('FF000000');
  });

  it.each([
    { nombre: 'Blanco', header: '#FFFFFF', headerRgb: [255, 255, 255] as [number, number, number] },
    { nombre: 'Azul Institucional', header: '#123B63', headerRgb: [18, 59, 99] as [number, number, number] },
    { nombre: 'Rosa', header: '#F9A8D4', headerRgb: [249, 168, 212] as [number, number, number] },
    { nombre: 'Turquesa/Menta', header: '#A7F3D0', headerRgb: [167, 243, 208] as [number, number, number] },
    { nombre: 'Negro', header: '#000000', headerRgb: [0, 0, 0] as [number, number, number] }
  ])('mantiene independencia de color para tema $nombre ($header)', ({ headerRgb }) => {
    setInstitutionalReportBranding({ headerColor: headerRgb });
    const branding = getInstitutionalReportBranding();
    expect(branding.headerColor).toEqual(headerRgb);
    // Los colores de contenido en fondo blanco permanecen en negro absoluto, inmunes al headerColor
    expect(INSTITUTIONAL_REPORT_COLORS.sectionTitle).toEqual([0, 0, 0]);
    expect(INSTITUTIONAL_REPORT_COLORS.body).toEqual([0, 0, 0]);
    expect(INSTITUTIONAL_REPORT_COLORS.label).toEqual([0, 0, 0]);
    expect(INSTITUTIONAL_REPORT_EXCEL_COLORS.sectionTitle).toBe('FF000000');
    expect(INSTITUTIONAL_REPORT_EXCEL_COLORS.body).toBe('FF000000');
  });
});
