import { describe, expect, it } from 'vitest';
// @ts-ignore: Vitest ejecuta este archivo en Node.js.
import { readdirSync, readFileSync, statSync } from 'node:fs';
// @ts-ignore: Vitest ejecuta este archivo en Node.js.
import { join, relative } from 'node:path';
// @ts-ignore: Vitest ejecuta este archivo en Node.js.
import { cwd } from 'node:process';

type ButtonBlock = { attrs: string; body: string; offset: number };

function collectFiles(root: string, extension: string): string[] {
  const files: string[] = [];
  for (const entry of readdirSync(root)) {
    const path = join(root, entry);
    if (statSync(path).isDirectory()) files.push(...collectFiles(path, extension));
    else if (path.endsWith(extension)) files.push(path);
  }
  return files;
}

function extractButtons(source: string): ButtonBlock[] {
  const result: ButtonBlock[] = [];
  let cursor = 0;
  while (true) {
    const start = source.indexOf('<button', cursor);
    if (start < 0) break;
    let endTag = start + '<button'.length;
    let quote: string | undefined;
    while (endTag < source.length) {
      const char = source[endTag];
      if (quote) {
        if (char === quote) quote = undefined;
      } else if (char === '"' || char === "'") {
        quote = char;
      } else if (char === '>') {
        break;
      }
      endTag++;
    }
    const end = source.indexOf('</button>', endTag);
    if (end < 0) break;
    result.push({
      attrs: source.slice(start, endTag + 1),
      body: source.slice(endTag + 1, end),
      offset: start
    });
    cursor = end + '</button>'.length;
  }
  return result;
}

function visibleButtonText(body: string): string {
  return body
    .replace(/<!--[\s\S]*?-->/g, ' ')
    .replace(/<svg\b[\s\S]*?<\/svg>/gi, ' ')
    .replace(/<[^>]+>/g, ' ')
    .replace(/\{\{[\s\S]*?\}\}/g, ' ')
    .replace(/@(?:if|else|for|empty)\b[^{}]*/g, ' ')
    .replace(/[{}]/g, ' ')
    .replace(/&nbsp;/gi, ' ')
    .replace(/\s+/g, ' ')
    .trim();
}

function isNonActionControl(attrs: string, source: string, offset: number): boolean {
  const nearbyMarkup = source.slice(Math.max(0, offset - 500), offset);
  return /role\s*=\s*["']tab["']|data-ui-control\s*=\s*["'](?:navigation|selection)["']|\[?routerLink|inspector-group-toggle|id\s*=\s*["']tab-|catalog-record|seleccionar(?:Tab|Vista|Formula|Funcion|Parametro|Catalogo)\s*\(|cambiarTipo\s*\(|ordenarConsolidado\s*\(|cambiarPagina(?:Detalle|Consolidado|Familias)?\s*\(|paginaActual\.set\s*\(/i.test(attrs) || /<nav\b/i.test(nearbyMarkup);
}

function isAction(attrs: string): boolean {
  return /\(click\)|\(ngSubmit\)|type\s*=\s*["']submit["']/i.test(attrs);
}

function lineAt(source: string, offset: number): number {
  return source.slice(0, offset).split('\n').length;
}

function productionSources(appRoot: string): Array<{ path: string; source: string }> {
  const sources = collectFiles(appRoot, '.html').map(path => ({ path, source: readFileSync(path, 'utf8') }));
  for (const path of collectFiles(appRoot, '.ts').filter(path => !path.endsWith('.spec.ts'))) {
    const source = readFileSync(path, 'utf8');
    const inlineTemplate = /\btemplate\s*:\s*`([\s\S]*?)`/g;
    let match: RegExpExecArray | null;
    while ((match = inlineTemplate.exec(source))) {
      sources.push({ path: `${path}#inline-template`, source: match[1] });
    }
  }
  return sources;
}

describe('regla global de acciones icon-only', () => {
  it('mantiene acciones productivas sin texto visible y con nombre/tooltip accesibles', () => {
    const workingDirectory = cwd();
    const appRoot = join(workingDirectory, 'src', 'app');
    const violations: string[] = [];

    for (const { path, source } of productionSources(appRoot)) {
      for (const button of extractButtons(source)) {
        if (!isAction(button.attrs) || isNonActionControl(button.attrs, source, button.offset)) continue;
        const text = visibleButtonText(button.body);
        const hasAriaLabel = /aria-label|attr\.aria-label/i.test(button.attrs);
        const hasTooltip = /\btitle\s*=|\[title\]|attr\.title/i.test(button.attrs);
        const hasIcon = /<svg\b|animate-spin/i.test(button.body);
        const location = `${relative(workingDirectory, path)}:${lineAt(source, button.offset)}`;
        if (text) violations.push(`${location}: visible text "${text}"`);
        if (!hasAriaLabel) violations.push(`${location}: missing aria-label`);
        if (!hasTooltip) violations.push(`${location}: missing title/tooltip`);
        if (!hasIcon) violations.push(`${location}: missing semantic icon`);
      }
    }

    expect(violations).toEqual([]);
  });
});
