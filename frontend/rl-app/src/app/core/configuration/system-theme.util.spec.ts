import { bestContrastForeground, contrastRatio, createSystemTheme, parseHexColor } from './system-theme.util';

describe('system-theme.util', () => {
  it('genera de forma determinista los once niveles dinámicos de la paleta IHSS', () => {
    const theme = createSystemTheme('#1e3a8a', '#1d4ed8');
    const levels = [50, 100, 200, 300, 400, 500, 600, 700, 800, 900, 950];
    expect(levels.every(level => /^\d+ \d+ \d+$/.test(theme.properties[`--ihss-${level}-rgb`]))).toBe(true);
    expect(theme).toEqual(createSystemTheme('#1e3a8a', '#1d4ed8'));
  });

  it.each([
    ['#991b1b', '#dc2626'],
    ['#166534', '#22c55e'],
    ['#fff8b5', '#fef08a'],
    ['#111827', '#0f172a'],
    ['not-a-color', undefined]
  ])('mantiene colores válidos y superficies de modal legibles para %s', (primary, accent) => {
    const theme = createSystemTheme(primary, accent);
    const modalPrimary = parseHexColor(`#${theme.properties['--ihss-modal-primary-rgb'].split(' ').map(n => Number(n).toString(16).padStart(2, '0')).join('')}`)!;
    const modalAccent = parseHexColor(`#${theme.properties['--ihss-modal-accent-rgb'].split(' ').map(n => Number(n).toString(16).padStart(2, '0')).join('')}`)!;
    const white = { r: 255, g: 255, b: 255 };
    expect(contrastRatio(modalPrimary, white)).toBeGreaterThanOrEqual(4.5);
    expect(contrastRatio(modalAccent, white)).toBeGreaterThanOrEqual(4.5);
  });

  it('elige texto de alto contraste para colores claros y oscuros', () => {
    const light = parseHexColor('#fff8b5')!;
    const dark = parseHexColor('#111827')!;
    expect(bestContrastForeground(light)).toEqual({ r: 0, g: 0, b: 0 });
    expect(bestContrastForeground(dark)).toEqual({ r: 255, g: 255, b: 255 });
    expect(contrastRatio(light, bestContrastForeground(light))).toBeGreaterThanOrEqual(4.5);
  });

  it('rechaza HEX inválido sin producir propiedades CSS malformadas', () => {
    expect(parseHexColor('#12xz00')).toBeNull();
    const theme = createSystemTheme('#12xz00', 'invalid');
    expect(theme.primary).toBe('#1e3a8a');
    expect(theme.accent).toBe('#1d4ed8');
    expect(Object.values(theme.properties).every(value => !value.includes('NaN'))).toBe(true);
  });
});
