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
    .replace(/<app-action-icon\b[\s\S]*?<\/app-action-icon>/gi, ' ')
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

type SemanticAction = 'edit' | 'view' | 'delete' | 'create' | 'save' | 'close' | 'cancel' | 'refresh' | 'retry' | 'clear-filters' | 'filter' | 'export-excel' | 'export-pdf' | 'upload' | 'download' | 'print' | 'sync';

const SEMANTIC_ACTION_RULES: Array<{ action: SemanticAction; pattern: RegExp }> = [
  { action: 'close', pattern: /^(?:cerrar|close)\b/i },
  { action: 'cancel', pattern: /^(?:cancelar|cancel)\b/i },
  { action: 'retry', pattern: /^(?:reintentar|retry)\b/i },
  { action: 'refresh', pattern: /^(?:actualizar|recargar|refrescar|refresh)\b/i },
  { action: 'sync', pattern: /^(?:sincronizar|sync)\b/i },
  { action: 'clear-filters', pattern: /^(?:limpiar(?: filtros)?|restablecer filtros|mostrar todos|show all|clear filters?)\b/i },
  { action: 'filter', pattern: /^(?:filtrar|filter|mostrar documentos eliminados)\b/i },
  { action: 'edit', pattern: /^(?:editar|modificar|edit)\b/i },
  { action: 'view', pattern: /^(?:ver|consultar|visualizar|mostrar|view)\b/i },
  { action: 'delete', pattern: /^(?:eliminar|borrar|remover|delete)\b/i },
  { action: 'create', pattern: /^(?:crear|nuevo|nueva|agregar|añadir|add|new)\b/i },
  { action: 'save', pattern: /^(?:guardar|aplicar cambios|save)\b/i },
  { action: 'export-excel', pattern: /^(?:exportar|descargar) (?:a |en )?(?:excel|xlsx)\b|^descargar coincidencias\b/i },
  { action: 'export-pdf', pattern: /^(?:generar|exportar|descargar|ver) (?:el |un )?(?:reporte )?pdf\b/i },
  { action: 'upload', pattern: /^(?:subir|cargar|upload)\b/i },
  { action: 'download', pattern: /^(?:descargar|download)\b/i },
  { action: 'print', pattern: /^(?:imprimir|print)\b/i }
];

function accessibleActionText(attrs: string): string[] {
  const values = [...attrs.matchAll(/(?:\[attr\.)?(?:title|aria-label)\]?\s*=\s*["']([^"']+)["']/gi)].flatMap(match => {
    const value = match[1].trim();
    const literals = [...value.matchAll(/['"]([^'"]+)['"]/g)].map(literal => literal[1].trim());
    return [value, ...literals];
  });
  return [...new Set(values.filter(Boolean))];
}

function expectedSemanticActions(attrs: string): SemanticAction[] {
  const expected = new Set<SemanticAction>();
  for (const text of accessibleActionText(attrs)) {
    const normalized = text.replace(/\s+/g, ' ').trim();
    if (/^mostrar u ocultar contrase/i.test(normalized)) continue;
    if (/^ver versiones\b/i.test(normalized)) continue;
    const rule = SEMANTIC_ACTION_RULES.find(candidate => candidate.pattern.test(normalized));
    if (rule) expected.add(rule.action);
  }
  return [...expected];
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

function canonicalActionNames(appRoot: string): Set<string> {
  const registryPath = join(appRoot, 'shared', 'components', 'action-icon', 'action-icon.component.ts');
  const registry = readFileSync(registryPath, 'utf8');
  const start = registry.indexOf('export const ACTION_ICON_PATHS');
  const end = registry.indexOf('\n};', start);
  const body = registry.slice(start, end < 0 ? registry.length : end);
  return new Set([...body.matchAll(/^\s*(?:'([^']+)'|([a-z-]+)):\s*\[/gm)].map(match => match[1] ?? match[2]));
}

function declaredActionIcons(body: string): string[] {
  const tags = [...body.matchAll(/<app-action-icon\b[^>]*>/gi)].map(match => match[0]);
  return tags.flatMap(tag => {
    const binding = tag.match(/\[action\]\s*=\s*["']([^"']+)["']/i);
    if (binding) return [...binding[1].matchAll(/['"]([a-z-]+)['"]/gi)].map(match => match[1]);
    const input = tag.match(/\baction\s*=\s*["']([^"']+)["']/i);
    return input ? [input[1]] : [];
  });
}

describe('regla global de acciones icon-only', () => {
  it('mantiene acciones productivas sin texto visible y con nombre/tooltip accesibles', () => {
    const workingDirectory = cwd();
    const appRoot = join(workingDirectory, 'src', 'app');
    const canonicalActions = canonicalActionNames(appRoot);
    const violations: string[] = [];

    for (const { path, source } of productionSources(appRoot)) {
      for (const button of extractButtons(source)) {
        if (!isAction(button.attrs) || isNonActionControl(button.attrs, source, button.offset)) continue;
        const text = visibleButtonText(button.body);
        const hasAriaLabel = /aria-label|attr\.aria-label/i.test(button.attrs);
        const hasTooltip = /\btitle\s*=|\[title\]|attr\.title/i.test(button.attrs);
        const actionIconCount = (button.body.match(/<app-action-icon\b/gi) ?? []).length;
        const actionIcons = declaredActionIcons(button.body);
        const hasIcon = /<app-action-icon\b/i.test(button.body);
        const location = `${relative(workingDirectory, path)}:${lineAt(source, button.offset)}`;
        if (text) violations.push(`${location}: visible text "${text}"`);
        if (!hasAriaLabel) violations.push(`${location}: missing aria-label`);
        if (!hasTooltip) violations.push(`${location}: missing title/tooltip`);
        if (!hasIcon) violations.push(`${location}: missing semantic icon`);
        if (actionIconCount !== 1) violations.push(`${location}: expected exactly one canonical action icon`);
        for (const action of actionIcons) {
          if (!canonicalActions.has(action)) violations.push(`${location}: action "${action}" is not registered in the canonical icon catalog`);
        }
        if (/<svg\b/i.test(button.body)) violations.push(`${location}: local SVG action icon bypasses the canonical catalog`);
      }
    }

    expect(violations).toEqual([]);
  });

  it('alinea el icono canónico con la operación descrita por cada acción', () => {
    const workingDirectory = cwd();
    const appRoot = join(workingDirectory, 'src', 'app');
    const violations: string[] = [];

    for (const { path, source } of productionSources(appRoot)) {
      for (const button of extractButtons(source)) {
        if (!isAction(button.attrs) || isNonActionControl(button.attrs, source, button.offset)) continue;
        const expected = expectedSemanticActions(button.attrs);
        if (expected.length === 0) continue;
        const actions = declaredActionIcons(button.body);
        const location = `${relative(workingDirectory, path)}:${lineAt(source, button.offset)}`;
        const expectedSet = new Set(expected);
        const actionSet = new Set(actions);
        if (actions.length !== expected.length || [...expectedSet].some(action => !actionSet.has(action))) {
          violations.push(`${location}: expected actions="${expected.join(',')}" for ${accessibleActionText(button.attrs).join(' / ')}, found actions="${actions.join(',')}"`);
        }
      }
    }

    expect(violations).toEqual([]);
  });
});
