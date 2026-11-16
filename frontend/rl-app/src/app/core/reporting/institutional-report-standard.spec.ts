import {
  INSTITUTIONAL_LOGO_ASPECT_RATIO,
  INSTITUTIONAL_LOGO_POLICY,
  INSTITUTIONAL_PDF_LOGO_WIDTH_MM
} from './institutional-report-standard';
import { bestContrastForeground, contrastRatio, parseHexColor } from '../configuration/system-theme.util';

describe('contrato institucional de identidad para reportes', () => {
  it('mantiene una escala uniforme de logo y conserva la proporción', () => {
    expect(INSTITUTIONAL_LOGO_POLICY.pdfWidthMm).toBe(INSTITUTIONAL_PDF_LOGO_WIDTH_MM);
    expect(INSTITUTIONAL_LOGO_POLICY.excelHeightPx).toBeCloseTo(
      INSTITUTIONAL_LOGO_POLICY.excelWidthPx / INSTITUTIONAL_LOGO_ASPECT_RATIO,
      8
    );
  });

  it.each(['#FFFFFF', '#000000', '#123B63', '#777777', '#767676', '#F3F6F9'])('elige un texto AA para %s', background => {
    const rgb = parseHexColor(background)!;
    const foreground = bestContrastForeground(rgb);
    expect(contrastRatio(rgb, foreground)).toBeGreaterThanOrEqual(4.5);
  });
});
