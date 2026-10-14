export type RgbColor = { r: number; g: number; b: number };
type HslColor = { h: number; s: number; l: number };

export interface SystemThemeColors {
  primary: string;
  accent: string;
  properties: Record<string, string>;
}

export const DEFAULT_SYSTEM_PRIMARY = '#1e3a8a';
export const DEFAULT_SYSTEM_ACCENT = '#1d4ed8';

const IHSS_LEVELS = [50, 100, 200, 300, 400, 500, 600, 700, 800, 900, 950] as const;

export function parseHexColor(value: string | null | undefined): RgbColor | null {
  if (!value || !/^#?[\da-f]{6}$/i.test(value.trim())) return null;
  const hex = value.trim().replace(/^#/, '');
  return {
    r: Number.parseInt(hex.slice(0, 2), 16),
    g: Number.parseInt(hex.slice(2, 4), 16),
    b: Number.parseInt(hex.slice(4, 6), 16)
  };
}

export function relativeLuminance(color: RgbColor): number {
  const linear = [color.r, color.g, color.b].map(channel => {
    const normalized = channel / 255;
    return normalized <= 0.04045 ? normalized / 12.92 : ((normalized + 0.055) / 1.055) ** 2.4;
  });
  return 0.2126 * linear[0] + 0.7152 * linear[1] + 0.0722 * linear[2];
}

export function contrastRatio(first: RgbColor, second: RgbColor): number {
  const luminances = [relativeLuminance(first), relativeLuminance(second)].sort((a, b) => b - a);
  return (luminances[0] + 0.05) / (luminances[1] + 0.05);
}

export function bestContrastForeground(background: RgbColor): RgbColor {
  const white = { r: 255, g: 255, b: 255 };
  const black = { r: 0, g: 0, b: 0 };
  return contrastRatio(background, white) >= contrastRatio(background, black) ? white : black;
}

function mix(first: RgbColor, second: RgbColor, secondWeight: number): RgbColor {
  const weight = Math.min(1, Math.max(0, secondWeight));
  return {
    r: Math.round(first.r * (1 - weight) + second.r * weight),
    g: Math.round(first.g * (1 - weight) + second.g * weight),
    b: Math.round(first.b * (1 - weight) + second.b * weight)
  };
}

function darkSurfaceWithWhiteContrast(color: RgbColor): RgbColor {
  const white = { r: 255, g: 255, b: 255 };
  if (contrastRatio(color, white) >= 4.5) return color;
  let low = 0;
  let high = 1;
  for (let iteration = 0; iteration < 24; iteration += 1) {
    const middle = (low + high) / 2;
    if (contrastRatio(mix(color, { r: 0, g: 0, b: 0 }, middle), white) >= 4.5) high = middle;
    else low = middle;
  }
  return mix(color, { r: 0, g: 0, b: 0 }, high);
}

function toRgbToken(color: RgbColor): string {
  return `${color.r} ${color.g} ${color.b}`;
}

function toHex(color: RgbColor): string {
  return `#${[color.r, color.g, color.b].map(value => value.toString(16).padStart(2, '0')).join('')}`;
}

function primaryShade(color: RgbColor, level: typeof IHSS_LEVELS[number]): RgbColor {
  if (level < 700) {
    const towardWhite = ({ 50: 0.96, 100: 0.9, 200: 0.78, 300: 0.58, 400: 0.36, 500: 0.18, 600: 0.08 } as Record<number, number>)[level];
    return mix(color, { r: 255, g: 255, b: 255 }, towardWhite);
  }
  const towardBlack = ({ 700: 0, 800: 0.12, 900: 0.25, 950: 0.4 } as Record<number, number>)[level];
  return mix(color, { r: 0, g: 0, b: 0 }, towardBlack);
}

function rgbToHsl(color: RgbColor): HslColor {
  const channels = [color.r, color.g, color.b].map(channel => channel / 255);
  const maximum = Math.max(...channels);
  const minimum = Math.min(...channels);
  const lightness = (maximum + minimum) / 2;
  const difference = maximum - minimum;
  if (difference === 0) return { h: 0, s: 0, l: lightness };

  const saturation = difference / (1 - Math.abs(2 * lightness - 1));
  let hue: number;
  if (maximum === channels[0]) hue = ((channels[1] - channels[2]) / difference) % 6;
  else if (maximum === channels[1]) hue = (channels[2] - channels[0]) / difference + 2;
  else hue = (channels[0] - channels[1]) / difference + 4;
  hue *= 60;
  return { h: (hue + 360) % 360, s: saturation, l: lightness };
}

function hslToRgb(color: HslColor): RgbColor {
  const hue = ((color.h % 360) + 360) % 360;
  const chroma = (1 - Math.abs(2 * color.l - 1)) * color.s;
  const section = hue / 60;
  const intermediate = chroma * (1 - Math.abs((section % 2) - 1));
  const base = color.l - chroma / 2;
  let rgb: [number, number, number];
  if (section < 1) rgb = [chroma, intermediate, 0];
  else if (section < 2) rgb = [intermediate, chroma, 0];
  else if (section < 3) rgb = [0, chroma, intermediate];
  else if (section < 4) rgb = [0, intermediate, chroma];
  else if (section < 5) rgb = [intermediate, 0, chroma];
  else rgb = [chroma, 0, intermediate];
  return { r: Math.round((rgb[0] + base) * 255), g: Math.round((rgb[1] + base) * 255), b: Math.round((rgb[2] + base) * 255) };
}

function modalToneStops(color: RgbColor): [RgbColor, RgbColor, RgbColor] {
  const original = rgbToHsl(color);
  const white = { r: 255, g: 255, b: 255 };
  let baseLightness = original.l;
  if (contrastRatio(color, white) < 7) {
    let low = 0;
    let high = original.l;
    for (let iteration = 0; iteration < 24; iteration += 1) {
      const middle = (low + high) / 2;
      if (contrastRatio(hslToRgb({ ...original, l: middle }), white) >= 7) low = middle;
      else high = middle;
    }
    baseLightness = low;
  }

  let highlightLightness = Math.min(1, baseLightness + 0.12);
  if (contrastRatio(hslToRgb({ ...original, l: highlightLightness }), white) < 4.5) {
    let low = baseLightness;
    let high = highlightLightness;
    for (let iteration = 0; iteration < 24; iteration += 1) {
      const middle = (low + high) / 2;
      if (contrastRatio(hslToRgb({ ...original, l: middle }), white) >= 4.5) low = middle;
      else high = middle;
    }
    highlightLightness = low;
  }

  return [
    hslToRgb({ ...original, l: Math.max(0, baseLightness * 0.78) }),
    hslToRgb({ ...original, l: baseLightness }),
    hslToRgb({ ...original, l: highlightLightness })
  ];
}

function hueDistance(first: RgbColor, second: RgbColor): number {
  const firstHue = rgbToHsl(first);
  const secondHue = rgbToHsl(second);
  if (firstHue.s < 0.08 || secondHue.s < 0.08) return firstHue.s < 0.08 && secondHue.s < 0.08 ? 0 : 180;
  const difference = Math.abs(firstHue.h - secondHue.h);
  return Math.min(difference, 360 - difference);
}

export function createSystemTheme(primaryValue?: string | null, accentValue?: string | null): SystemThemeColors {
  const primary = parseHexColor(primaryValue) ?? parseHexColor(DEFAULT_SYSTEM_PRIMARY)!;
  const accent = parseHexColor(accentValue) ?? parseHexColor(DEFAULT_SYSTEM_ACCENT)!;
  const [modalPrimaryStart, modalPrimaryMid, modalPrimaryEnd] = modalToneStops(primary);
  const [modalSecondaryStart, modalSecondary, modalSecondaryEnd] = modalToneStops(accent);
  const modalPrimary = darkSurfaceWithWhiteContrast(primary);
  const modalAccent = darkSurfaceWithWhiteContrast(accent);
  const onPrimary = bestContrastForeground(primary);
  const onAccent = bestContrastForeground(accent);
  const modalForeground = { r: 255, g: 255, b: 255 };
  const properties: Record<string, string> = {
    '--ihss-primary-color': toHex(primary),
    '--ihss-accent-color': toHex(accent),
    '--ihss-primary-rgb': toRgbToken(primary),
    '--ihss-accent-rgb': toRgbToken(accent),
    '--ihss-on-primary-rgb': toRgbToken(onPrimary),
    '--ihss-on-accent-rgb': toRgbToken(onAccent),
    '--ihss-modal-primary-rgb': toRgbToken(modalPrimary),
    '--ihss-modal-accent-rgb': toRgbToken(modalAccent),
    '--ihss-modal-primary-start-rgb': toRgbToken(modalPrimaryStart),
    '--ihss-modal-primary-mid-rgb': toRgbToken(modalPrimaryMid),
    '--ihss-modal-primary-end-rgb': toRgbToken(modalPrimaryEnd),
    '--ihss-modal-secondary-start-rgb': toRgbToken(modalSecondaryStart),
    '--ihss-modal-secondary-mid-rgb': toRgbToken(modalSecondary),
    '--ihss-modal-secondary-end-rgb': toRgbToken(modalSecondaryEnd),
    '--ihss-modal-highlight-rgb': toRgbToken(hueDistance(primary, accent) <= 35 ? modalSecondaryEnd : modalPrimaryEnd),
    '--ihss-on-modal-rgb': toRgbToken(modalForeground),
    '--modal-close-bg': 'rgb(255 255 255 / 0.16)',
    '--modal-close-bg-hover': 'rgb(255 255 255 / 0.28)',
    '--modal-close-border': 'rgb(255 255 255 / 0.46)',
    '--modal-close-border-hover': 'rgb(255 255 255 / 0.78)',
    '--modal-close-color': 'rgb(255 255 255)',
    '--modal-close-color-hover': 'rgb(255 255 255)',
    '--modal-close-focus-rgb': toRgbToken(modalAccent)
  };
  for (const level of IHSS_LEVELS) {
    const shade = primaryShade(primary, level);
    properties[`--ihss-${level}-rgb`] = toRgbToken(shade);
    properties[`--ihss-on-${level}-rgb`] = toRgbToken(bestContrastForeground(shade));
  }
  properties['--ihss-on-primary-rgb'] = properties['--ihss-on-700-rgb'];

  return { primary: toHex(primary), accent: toHex(accent), properties };
}
