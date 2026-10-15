import { expect, test } from '@playwright/test';
import { createSystemTheme } from '../src/app/core/configuration/system-theme.util';

type Rgb = [number, number, number];

function parseRgb(value: string): Rgb | null {
  const match = value.match(/rgba?\(\s*([\d.]+)[,\s]+([\d.]+)[,\s]+([\d.]+)/i);
  return match ? [Number(match[1]), Number(match[2]), Number(match[3])] : null;
}

function luminance([r, g, b]: Rgb): number {
  const channel = (value: number) => {
    const normalized = value / 255;
    return normalized <= 0.04045 ? normalized / 12.92 : ((normalized + 0.055) / 1.055) ** 2.4;
  };
  return 0.2126 * channel(r) + 0.7152 * channel(g) + 0.0722 * channel(b);
}

function contrast(first: Rgb, second: Rgb): number {
  const values = [luminance(first), luminance(second)].sort((a, b) => b - a);
  return (values[0] + 0.05) / (values[1] + 0.05);
}

function composite(foreground: Rgb, background: Rgb, alpha: number): Rgb {
  return foreground.map((channel, index) => Math.round(channel * alpha + background[index] * (1 - alpha))) as Rgb;
}

const themes = [
  { name: 'blue', primary: '#1e3a8a', secondary: '#1d4ed8' },
  { name: 'red', primary: '#991b1b', secondary: '#dc2626' },
  { name: 'green', primary: '#14532d', secondary: '#22c55e' },
  { name: 'light', primary: '#fef3c7', secondary: '#fde68a' }
];

test('modal footer icon primitive stays visible, themed and accessible across variants', async ({ page }) => {
  await page.setViewportSize({ width: 1280, height: 900 });
  await page.goto('/login');
  await page.locator('body').evaluate(body => {
    const footer = document.createElement('div');
    footer.className = 'modal-footer-institutional';
    footer.setAttribute('data-testid', 'modal-action-primitive-fixture');
    const variants = ['primary', 'secondary', 'neutral', 'danger', 'success'];
    for (const variant of variants) {
      const button = document.createElement('button');
      button.type = 'button';
      button.className = `modal-action-icon modal-action-icon--${variant}`;
      button.setAttribute('aria-label', `${variant} action`);
      button.title = `${variant} action`;
      button.innerHTML = '<app-action-icon><svg aria-hidden="true" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.25"><path d="M5 12h14M12 5l7 7-7 7"/></svg></app-action-icon>';
      footer.append(button);
    }
    const disabled = document.createElement('button');
    disabled.type = 'button';
    disabled.disabled = true;
    disabled.className = 'modal-action-icon modal-action-icon--secondary';
    disabled.setAttribute('aria-label', 'previous disabled action');
    disabled.title = 'previous disabled action';
    disabled.innerHTML = '<app-action-icon><svg aria-hidden="true" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.25"><path d="M19 12H5m7 7-7-7 7-7"/></svg></app-action-icon>';
    footer.append(disabled);
    document.body.append(footer);
  });

  const footer = page.getByTestId('modal-action-primitive-fixture');
  await expect(footer.locator('button')).toHaveCount(6);
  for (const theme of themes) {
    const properties = createSystemTheme(theme.primary, theme.secondary).properties;
    await page.evaluate(values => {
      for (const [name, value] of Object.entries(values)) document.documentElement.style.setProperty(name, value);
    }, properties);
    const sample = await footer.locator('button').evaluateAll(buttons => {
      const footerStyle = getComputedStyle(buttons[0].parentElement!);
      const footerTokens = ['--ihss-modal-primary-start-rgb', '--ihss-modal-primary-mid-rgb'];
      return {
        backgroundImage: footerStyle.backgroundImage,
        baseColors: footerTokens.map(token => `rgb(${getComputedStyle(document.documentElement).getPropertyValue(token)})`),
        buttons: buttons.map(button => {
          const style = getComputedStyle(button);
          const rect = button.getBoundingClientRect();
          const icon = button.querySelector('app-action-icon');
          const iconRect = icon?.getBoundingClientRect();
          return { aria: button.getAttribute('aria-label'), title: button.getAttribute('title'), disabled: (button as HTMLButtonElement).disabled,
            color: style.color, backgroundColor: style.backgroundColor, backgroundImage: style.backgroundImage,
            borderColor: style.borderColor, borderWidth: style.borderWidth, opacity: style.opacity,
            width: rect.width, height: rect.height, iconWidth: iconRect?.width ?? 0, iconHeight: iconRect?.height ?? 0,
            visibility: style.visibility, display: style.display };
        })
      };
    });

    expect(sample.backgroundImage).toContain('linear-gradient');
    const footerStops = sample.baseColors.map(parseRgb);
    expect(footerStops.every(Boolean), `${theme.name}: footer theme stop colors resolve`).toBe(true);
    const primary = sample.buttons[0];
    const secondary = sample.buttons[1];
    const neutral = sample.buttons[2];
    const danger = sample.buttons[3];
    const success = sample.buttons[4];
    const disabled = sample.buttons[5];
    for (const button of sample.buttons) {
      expect(button.aria).toBeTruthy();
      expect(button.title).toBeTruthy();
      expect(button.visibility).toBe('visible');
      expect(button.display).not.toBe('none');
      expect(button.width).toBeGreaterThanOrEqual(44);
      expect(button.height).toBeGreaterThanOrEqual(44);
      expect(button.iconWidth).toBe(20);
      expect(button.iconHeight).toBe(20);
      if (!button.disabled) expect(button.opacity).toBe('1');
    }
    await expect(footer.locator('button')).toHaveText(['', '', '', '', '', '']);
    expect(primary.backgroundImage).toContain('linear-gradient');
    expect(primary.color).not.toBe(secondary.color);
    expect(danger.color).toBe('rgb(255, 255, 255)');
    expect(success.color).toBe('rgb(255, 255, 255)');
    expect(disabled.disabled).toBe(true);
    expect(Number(disabled.opacity)).toBeGreaterThan(0.7);
    expect(disabled.backgroundColor).not.toBe('rgba(0, 0, 0, 0)');
    expect(disabled.borderWidth).toBe('1px');

    const primaryColors = [...primary.backgroundImage.matchAll(/rgb\(\s*(\d+)[,\s]+(\d+)[,\s]+(\d+)\s*\)/g)]
      .map(match => [Number(match[1]), Number(match[2]), Number(match[3])] as Rgb);
    const primaryForeground = parseRgb(primary.color);
    const secondaryForeground = parseRgb(secondary.color);
    const neutralForeground = parseRgb(neutral.color);
    expect(primaryColors.length).toBeGreaterThanOrEqual(2);
    expect(primaryForeground).not.toBeNull();
    expect(secondaryForeground).not.toBeNull();
    expect(neutralForeground).not.toBeNull();
    for (const color of primaryColors) expect(contrast(primaryForeground!, color)).toBeGreaterThanOrEqual(3);
    for (const stop of footerStops as Rgb[]) {
      const secondarySurface = composite([255, 255, 255], stop, 0.16);
      expect(contrast(secondaryForeground!, secondarySurface)).toBeGreaterThanOrEqual(3);
      const secondaryBoundary = composite([255, 255, 255], stop, 0.62);
      expect(contrast(stop, secondaryBoundary)).toBeGreaterThanOrEqual(3);
      expect(contrast(neutralForeground!, [255, 255, 255])).toBeGreaterThanOrEqual(3);
    }
  }

  const primary = footer.getByRole('button', { name: 'primary action' });
  const beforeHover = await primary.evaluate(button => getComputedStyle(button).backgroundImage);
  await primary.hover();
  const afterHover = await primary.evaluate(button => getComputedStyle(button).backgroundImage);
  expect(afterHover).not.toBe(beforeHover);
  await primary.focus();
  await expect(primary).toBeFocused();
  const focus = await primary.evaluate(button => ({ style: getComputedStyle(button).outlineStyle, width: getComputedStyle(button).outlineWidth }));
  expect(focus.style).toBe('solid');
  expect(focus.width).toBe('3px');

  await page.setViewportSize({ width: 390, height: 844 });
  const mobile = await footer.evaluate(element => ({
    clientWidth: element.clientWidth,
    scrollWidth: element.scrollWidth,
    buttons: Array.from(element.querySelectorAll('button'), button => {
      const rect = button.getBoundingClientRect();
      return { width: rect.width, height: rect.height, right: rect.right };
    })
  }));
  expect(mobile.scrollWidth).toBeLessThanOrEqual(mobile.clientWidth);
  for (const button of mobile.buttons) {
    expect(button.width).toBeGreaterThanOrEqual(44);
    expect(button.height).toBeGreaterThanOrEqual(44);
    expect(button.right).toBeLessThanOrEqual(390);
  }
});
