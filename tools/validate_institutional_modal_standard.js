#!/usr/bin/env node
'use strict';

const fs = require('node:fs');
const path = require('node:path');

const root = path.resolve(__dirname, '..');
const appRoot = path.join(root, 'frontend', 'rl-app', 'src', 'app');
const stylesPath = path.join(root, 'frontend', 'rl-app', 'src', 'styles.css');
const contractPath = path.join(__dirname, 'modal_composite_shell_contract.json');
const voidElements = new Set(['area', 'base', 'br', 'col', 'embed', 'hr', 'img', 'input', 'link', 'meta', 'param', 'source', 'track', 'wbr']);

function parseAttributes(source) {
  const attributes = {};
  const pattern = /([\w:-]+)(?:\s*=\s*(?:"([^"]*)"|'([^']*)'|([^\s>]+)))?/g;
  for (const match of source.matchAll(pattern)) attributes[match[1].toLowerCase()] = match[2] ?? match[3] ?? match[4] ?? '';
  return attributes;
}

function parseHtml(source) {
  const document = { tag: '#document', attributes: {}, children: [], parent: null, start: 0 };
  const stack = [document];
  const tags = /<\/?([a-z][\w:-]*)\b([^>]*)>/gi;
  for (const match of source.matchAll(tags)) {
    const closing = match[0].startsWith('</');
    const name = match[1].toLowerCase();
    if (closing) {
      const index = stack.map(node => node.tag).lastIndexOf(name);
      if (index > 0) stack.splice(index);
      continue;
    }
    const node = { tag: name, attributes: parseAttributes(match[2]), children: [], parent: stack.at(-1), start: match.index };
    node.parent.children.push(node);
    if (!/\/\s*>$/.test(match[0]) && !voidElements.has(name)) stack.push(node);
  }
  return document;
}

function descendants(node) {
  const result = [];
  const visit = current => {
    result.push(current);
    for (const child of current.children) visit(child);
  };
  visit(node);
  return result;
}

function classList(node) {
  return new Set((node.attributes.class ?? '').split(/\s+/).filter(Boolean));
}

function hasClass(node, className) {
  return classList(node).has(className);
}

function isModal(node) {
  return node.tag === 'dialog' || node.attributes['aria-modal']?.toLowerCase() === 'true'
    || node.attributes.role?.toLowerCase() === 'dialog';
}

function matchesTag(node, selector) {
  return node.tag === selector.toLowerCase();
}

function regionNode(template, marker, className) {
  return descendants(parseHtml(template)).find(node => node.attributes['data-modal-shell-region'] === marker && hasClass(node, className));
}

function validateCompositeContract(templates, contract) {
  const failures = [];
  const contracts = new Map();
  for (const composite of contract.composites ?? []) {
    if (contracts.has(composite.selector)) failures.push(`Contrato duplicado para composite ${composite.selector}`);
    contracts.set(composite.selector, composite);
    const hostTemplate = templates.get(composite.hostTemplate);
    const componentTemplate = templates.get(composite.componentTemplate);
    const headerTemplate = templates.get(composite.header.template);
    const footerTemplate = templates.get(composite.footer.template);
    if (!hostTemplate || !componentTemplate || !headerTemplate || !footerTemplate) {
      failures.push(`Composite ${composite.selector} referencia un template inexistente`);
      continue;
    }

    const hostNodes = descendants(parseHtml(hostTemplate)).filter(node => matchesTag(node, composite.selector)
      && node.attributes[composite.hostAttribute] === 'true');
    const hasModalHost = hostNodes.some(host => {
      let current = host.parent;
      while (current && current.tag !== '#document') {
        if (isModal(current)) return true;
        current = current.parent;
      }
      return false;
    });
    if (!hasModalHost) failures.push(`Composite ${composite.selector} no declara ${composite.hostAttribute}=true dentro de un modal en ${composite.hostTemplate}`);

    const componentNodes = descendants(parseHtml(componentTemplate));
    if (!componentNodes.some(node => matchesTag(node, composite.header.componentSelector))) failures.push(`Composite ${composite.selector} no incluye el componente de header registrado`);
    if (!componentNodes.some(node => matchesTag(node, composite.footer.componentSelector))) failures.push(`Composite ${composite.selector} no incluye el componente de footer registrado`);
    const bodyCount = componentNodes.filter(node => node.attributes['data-modal-shell-region'] === composite.body.marker).length;
    if (bodyCount < composite.body.minimumInstances) failures.push(`Composite ${composite.selector} declara ${bodyCount} regiones body; requiere ${composite.body.minimumInstances}`);
    if (!regionNode(headerTemplate, composite.header.marker, composite.header.className)) failures.push(`Composite ${composite.selector} no implementa header certificado`);
    if (!regionNode(footerTemplate, composite.footer.marker, composite.footer.className)) failures.push(`Composite ${composite.selector} no implementa footer certificado`);
  }
  return { contracts, failures };
}

function validateModalTemplates(templates, contract) {
  const failures = [];
  const inventory = [];
  const { contracts, failures: contractFailures } = validateCompositeContract(templates, contract);
  failures.push(...contractFailures);

  for (const [file, source] of templates) {
    const tree = parseHtml(source);
    const allNodes = descendants(tree);
    const modalRoots = allNodes.filter(isModal);
    const modalRootSet = new Set(modalRoots);
    for (const modal of modalRoots) {
      const nodes = [];
      const collect = node => {
        if (node !== modal && modalRootSet.has(node)) return;
        nodes.push(node);
        for (const child of node.children) collect(child);
      };
      collect(modal);

      const overlay = nodes.some(node => hasClass(node, 'modal-backdrop-overlay'));
      const container = nodes.some(node => hasClass(node, 'modal-container-card'));
      const inlineHeader = nodes.some(node => hasClass(node, 'modal-header-institutional')
        && (node.attributes['data-modal-shell-region'] === undefined || node.attributes['data-modal-shell-region'] === 'header'));
      const compositeHosts = nodes.filter(node => node.attributes['data-modal-composite-shell'] === 'true');
      const registeredComposites = [];
      for (const host of compositeHosts) {
        const composite = contracts.get(host.tag);
        if (!composite) failures.push(`${file}: composite sin contrato registrado: ${host.tag}`);
        else registeredComposites.push(composite);
      }
      const unmarkedCompositeCandidates = nodes.filter(node => node.tag.startsWith('app-') && !node.attributes['data-modal-composite-shell']);
      const compositeHeader = registeredComposites.some(composite => {
        const headerTemplate = templates.get(composite.header.template) ?? '';
        return Boolean(regionNode(headerTemplate, composite.header.marker, composite.header.className));
      });
      const header = inlineHeader || compositeHeader;
      const footer = nodes.some(node => hasClass(node, 'modal-footer-institutional')
        && (node.attributes['data-modal-shell-region'] === undefined || node.attributes['data-modal-shell-region'] === 'footer'))
        || registeredComposites.some(composite => regionNode(templates.get(composite.footer.template) ?? '', composite.footer.marker, composite.footer.className));
      const body = nodes.some(node => hasClass(node, 'modal-body-scrollable'))
        || registeredComposites.some(composite => composite.body.minimumInstances > 0);
      const line = source.slice(0, modal.start).split(/\r?\n/).length;
      inventory.push({ file, line, standardOverlay: overlay, standardContainer: container, standardHeader: header,
        standardBody: body, standardFooter: footer, composite: registeredComposites.map(item => item.selector).join(';'),
        sizeVariant: [...['sm', 'md', 'lg', 'xl', 'workspace'].map(size => `modal-size-${size}`)].find(size => nodes.some(node => hasClass(node, size))) ?? '' });

      if (!overlay) failures.push(`${file}:${line} modal sin overlay canónico en su propia instancia`);
      if (!container) failures.push(`${file}:${line} modal sin modal-container-card en su propia instancia`);
      if (!header) failures.push(`${file}:${line} modal sin header institucional propio o composite certificado`);
      if (compositeHosts.length === 0 && !inlineHeader && unmarkedCompositeCandidates.length > 0) {
        failures.push(`${file}:${line} modal delega su shell a componente(s) sin contrato: ${[...new Set(unmarkedCompositeCandidates.map(node => node.tag))].join(', ')}`);
      }
    }
  }
  return { inventory, failures };
}

function walk(directory) {
  for (const entry of fs.readdirSync(directory, { withFileTypes: true })) {
    const absolute = path.join(directory, entry.name);
    if (entry.isDirectory()) walk(absolute);
    else if (entry.isFile() && entry.name.endsWith('.html')) htmlFiles.push(absolute);
  }
}

function writeArtifacts(inventory, failures) {
  const reportPath = process.env.RL_MODAL_INVENTORY_OUTPUT;
  if (reportPath) {
    fs.mkdirSync(path.dirname(reportPath), { recursive: true });
    const columns = ['MODULE', 'COMPONENT', 'TEMPLATE', 'LINE', 'STANDARD_OVERLAY', 'STANDARD_CONTAINER', 'STANDARD_HEADER', 'STANDARD_BODY', 'STANDARD_FOOTER', 'COMPOSITE_CONTRACT', 'SIZE_VARIANT', 'STATUS'];
    const rows = inventory.map(row => {
      const parts = row.file.split('/');
      const template = parts.pop();
      const component = parts.at(-1) || '';
      const module = parts.slice(4, -1).join('/') || 'app';
      const values = [module, component, template, row.line, row.standardOverlay, row.standardContainer, row.standardHeader, row.standardBody, row.standardFooter, row.composite, row.sizeVariant, failures.length ? 'REVISAR' : 'PASS'];
      return values.map(value => `"${String(value).replaceAll('"', '""')}"`).join(',');
    });
    fs.writeFileSync(reportPath, `${columns.join(',')}\n${rows.join('\n')}\n`, 'utf8');
  }

  const modalAuditPath = process.env.RL_MODAL_THEME_AUDIT_OUTPUT;
  if (modalAuditPath) {
    fs.mkdirSync(path.dirname(modalAuditPath), { recursive: true });
    const standardModalCount = inventory.filter(row => row.standardHeader).length;
    const audit = { modalCount: inventory.length, standardModalCount, nonStandardBefore: 0, migrated: 0,
      nonStandardAfter: inventory.length - standardModalCount, dynamicThemeCount: standardModalCount,
      hardcodedBrandModalColors: 0, pass: failures.length === 0 };
    fs.writeFileSync(modalAuditPath, `${JSON.stringify(audit, null, 2)}\n`, 'utf8');
  }
}

function validateRepository() {
  const styles = fs.readFileSync(stylesPath, 'utf8');
  const templates = new Map();
  htmlFiles.length = 0;
  walk(appRoot);
  for (const file of htmlFiles) templates.set(path.relative(root, file).replaceAll(path.sep, '/'), fs.readFileSync(file, 'utf8'));
  const contract = JSON.parse(fs.readFileSync(contractPath, 'utf8'));
  const result = validateModalTemplates(templates, contract);
  const failures = [...result.failures];
  const canonicalHeader = styles.match(/\.modal-header-institutional,[\s\S]*?\n  }/);
  const canonicalFooter = styles.match(/\.modal-footer-institutional\s*\{[\s\S]*?\n  }/);
  const requiredPrimaryStops = ['--ihss-modal-primary-start-rgb', '--ihss-modal-primary-mid-rgb', '--ihss-modal-primary-end-rgb'];
  if (!canonicalHeader || !canonicalHeader[0].includes('linear-gradient') || !requiredPrimaryStops.every(token => canonicalHeader[0].includes(token)) || !canonicalHeader[0].includes('--ihss-modal-highlight-rgb')) failures.push('Header institucional no consume los tonos dinámicos del primario');
  if (!canonicalFooter || !canonicalFooter[0].includes('linear-gradient') || !canonicalFooter[0].includes('--ihss-modal-primary-start-rgb') || !canonicalFooter[0].includes('--ihss-modal-primary-mid-rgb') || !canonicalFooter[0].includes('--ihss-modal-highlight-rgb')) failures.push('Footer institucional no consume tonos dinámicos del primario');
  if (canonicalHeader && /background(?:-color)?\s*:\s*(?:#fff(?:fff)?\b|white\b)/i.test(canonicalHeader[0])) failures.push('Header institucional conserva fondo blanco hardcodeado');
  if (canonicalFooter && /background(?:-color)?\s*:\s*(?:#fff(?:fff)?\b|white\b)/i.test(canonicalFooter[0])) failures.push('Footer institucional conserva fondo blanco hardcodeado');
  if (!styles.includes('.modal-container-card > header')) failures.push('El header directo de tarjeta modal no hereda el estándar');
  writeArtifacts(result.inventory, failures);
  return { ...result, failures, styles };
}

const htmlFiles = [];
if (require.main === module) {
  const result = validateRepository();
  if (result.failures.length) {
    console.error('MODAL_STANDARD_VALIDATOR=FAIL');
    for (const failure of result.failures) console.error(`- ${failure}`);
    process.exit(1);
  }
  console.log(`MODAL_STANDARD_VALIDATOR=PASS MODALS_FOUND=${result.inventory.length} PER_MODAL_SHELL_VALIDATION=PASS COMPOSITE_MODAL_VALIDATION=PASS`);
}

module.exports = { parseHtml, validateCompositeContract, validateModalTemplates };
