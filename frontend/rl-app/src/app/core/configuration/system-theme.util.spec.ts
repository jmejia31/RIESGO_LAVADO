import { bestContrastForeground, contrastRatio, createSystemTheme, parseHexColor, relativeLuminance } from './system-theme.util';

function colorFromToken(value: string) {
  const [r, g, b] = value.split(' ').map(Number);
  return { r, g, b };
}

function hue(color: { r: number; g: number; b: number }): number {
  const [r, g, b] = [color.r, color.g, color.b].map(channel => channel / 255);
  const maximum = Math.max(r, g, b);
  const minimum = Math.min(r, g, b);
  const difference = maximum - minimum;
  if (!difference) return 0;
  if (maximum === r) return (((g - b) / difference) % 6 + 6) % 6 * 60;
  if (maximum === g) return ((b - r) / difference + 2) * 60;
  return ((r - g) / difference + 4) * 60;
}

function hueDistance(first: number, second: number): number {
  const difference = Math.abs(first - second);
  return Math.min(difference, 360 - difference);
}

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

  it.each([
    ['#1e3a8a', '#1d4ed8'],
    ['#991b1b', '#dc2626'],
    ['#14532d', '#22c55e'],
    ['#fff8b5', '#fef08a'],
    ['#111827', '#0f172a']
  ])('genera gradientes de modal monofamiliares y con contraste para %s', (primary, accent) => {
    const theme = createSystemTheme(primary, accent);
    const white = { r: 255, g: 255, b: 255 };
    for (const family of ['primary', 'secondary']) {
      const stops = ['start', 'mid', 'end'].map(stop => colorFromToken(theme.properties[`--ihss-modal-${family}-${stop}-rgb`]));
      expect(hueDistance(hue(stops[0]), hue(stops[1]))).toBeLessThanOrEqual(3);
      expect(hueDistance(hue(stops[1]), hue(stops[2]))).toBeLessThanOrEqual(3);
      expect(relativeLuminance(stops[0])).toBeLessThan(relativeLuminance(stops[1]));
      expect(relativeLuminance(stops[1])).toBeLessThan(relativeLuminance(stops[2]));
      expect(relativeLuminance(stops[2]) - relativeLuminance(stops[0])).toBeGreaterThanOrEqual(0.08);
      expect(stops.every(stop => contrastRatio(stop, white) >= 4.5)).toBe(true);
    }
  });

  it('mantiene el primario dominante cuando primario y secundario no comparten familia cromatica', () => {
    const unrelated = createSystemTheme('#1e3a8a', '#dc2626');
    expect(unrelated.properties['--ihss-modal-highlight-rgb']).toBe(unrelated.properties['--ihss-modal-primary-end-rgb']);
    const harmonious = createSystemTheme('#991b1b', '#dc2626');
    expect(harmonious.properties['--ihss-modal-highlight-rgb']).toBe(harmonious.properties['--ihss-modal-secondary-end-rgb']);
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
