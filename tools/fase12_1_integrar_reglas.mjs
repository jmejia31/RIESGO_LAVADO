import fs from 'node:fs';
import path from 'node:path';

const root = process.cwd();
const componentPath = path.join(
  root,
  'frontend/rl-app/src/app/features/admin/matrices-riesgos/pages/matrices-riesgos/matrices-riesgos.component.ts'
);
const templatePath = path.join(
  root,
  'frontend/rl-app/src/app/features/admin/matrices-riesgos/pages/matrices-riesgos/matrices-riesgos.component.html'
);

function replaceOnce(content, search, replacement, label) {
  if (!content.includes(search)) {
    if (content.includes(replacement)) return content;
    throw new Error(`No se encontró el bloque requerido: ${label}`);
  }
  return content.replace(search, replacement);
}

function replaceRegex(content, regex, replacement, label) {
  if (!regex.test(content)) {
    if (typeof replacement === 'string' && replacement && content.includes(replacement.trim())) return content;
    throw new Error(`No se encontró el patrón requerido: ${label}`);
  }
  return content.replace(regex, replacement);
}

let component = fs.readFileSync(componentPath, 'utf8');
let template = fs.readFileSync(templatePath, 'utf8');

const policyImport = `import {
  ESTADOS_MATRIZ_VISIBLES,
  etiquetaEstadoMatriz,
  puedeEditarMatriz as puedeEditarMatrizPorEstado,
  puedeEliminarMatriz as puedeEliminarMatrizPorEstado,
  transicionesPermitidasMatriz
} from '../../domain/matrices-riesgos-estados.policy';`;

if (!component.includes("from '../../domain/matrices-riesgos-estados.policy'")) {
  component = replaceOnce(
    component,
    "import { ConfiguracionService } from '../../../../../core/configuration/configuracion.service';",
    `import { ConfiguracionService } from '../../../../../core/configuration/configuracion.service';\n${policyImport}`,
    'importación de política de estados'
  );
}

component = replaceOnce(
  component,
  "type ModalTipo = 'calcular' | 'estado' | 'eliminarMatriz' | 'inactivarCriterio' | 'eliminarCriterio' | 'estadoPlan' | 'inactivarPlan' | 'reactivarPlan' | 'inactivarEvidencia';",
  "type ModalTipo = 'estado' | 'eliminarMatriz' | 'inactivarCriterio' | 'eliminarCriterio' | 'estadoPlan' | 'inactivarPlan' | 'reactivarPlan' | 'inactivarEvidencia';",
  'tipo modal sin cálculo manual'
);

component = replaceRegex(
  component,
  /  \/\/ Estados visibles para operación diaria\.[\s\S]*?  readonly estadosGestionables = \['EN_REVISION', 'APROBADA', 'CERRADA', 'INACTIVA'\];\n/,
  `  // Estados funcionales aprobados para la operación diaria.\n  // Los estados técnicos se normalizan mediante una única política de dominio.\n  readonly estadosDisponibles = [...ESTADOS_MATRIZ_VISIBLES];\n`,
  'catálogo centralizado de estados'
);

component = replaceRegex(
  component,
  /\n  calcularMatriz\(matriz: MatrizRiesgoResumen\): void \{[\s\S]*?\n  \}\n\n  cambiarEstado/,
  '\n  cambiarEstado',
  'retiro del método de cálculo manual'
);

component = component.replace(
  /\n      case 'calcular':\n        this\.ejecutarCalculo\(operacion\.matriz!\);\n        break;/,
  ''
);

component = replaceRegex(
  component,
  /  estadoEtiqueta\(estado\?: string \| null\): string \{[\s\S]*?\n  \}\n\n  estadosGestionablesParaMatriz/,
  `  estadoEtiqueta(estado?: string | null): string {\n    return etiquetaEstadoMatriz(estado);\n  }\n\n  estadosGestionablesParaMatriz`,
  'etiqueta centralizada de estado'
);

component = replaceRegex(
  component,
  /  estadosGestionablesParaMatriz\(estadoActual\?: string \| null\): string\[\] \{[\s\S]*?\n  \}\n\n  puedeEliminarMatriz/,
  `  estadosGestionablesParaMatriz(estadoActual?: string | null): readonly string[] {\n    return transicionesPermitidasMatriz(estadoActual);\n  }\n\n  puedeEditarMatriz(matriz: MatrizRiesgoResumen | MatrizRiesgoDetalle): boolean {\n    return puedeEditarMatrizPorEstado(matriz.estado);\n  }\n\n  puedeEliminarMatriz`,
  'transiciones operativas por estado'
);

component = replaceRegex(
  component,
  /  puedeEliminarMatriz\(matriz: MatrizRiesgoResumen \| MatrizRiesgoDetalle\): boolean \{[\s\S]*?\n  \}\n\n  mensajeBloqueoEliminarMatriz/,
  `  puedeEliminarMatriz(matriz: MatrizRiesgoResumen | MatrizRiesgoDetalle): boolean {\n    return puedeEliminarMatrizPorEstado(matriz.estado);\n  }\n\n  mensajeBloqueoEliminarMatriz`,
  'regla de eliminación por estado'
);

component = replaceOnce(
  component,
  "      : 'La matriz no puede eliminarse porque ya fue aprobada, cerrada o se encuentra inactiva.';",
  "      : 'La matriz solo puede eliminarse mientras se encuentra En Revisión.';",
  'mensaje de bloqueo de eliminación'
);

component = replaceRegex(
  component,
  /\n  private ejecutarCalculo\(matriz: MatrizRiesgoResumen\): void \{[\s\S]*?\n  \}\n\n  private ejecutarCambioEstado/,
  '\n  private ejecutarCambioEstado',
  'retiro de ejecución manual de cálculo'
);

template = replaceOnce(
  template,
  '<p class="text-xs font-bold uppercase text-gray-400">Calculadas</p>',
  '<p class="text-xs font-bold uppercase text-gray-400">Evaluadas</p>',
  'KPI sin estado técnico visible'
);

template = replaceOnce(
  template,
  '[disabled]="guardando() || matriz.estado === \'CERRADA\' || matriz.estado === \'INACTIVA\'">\n                        Editar',
  '[disabled]="guardando() || !puedeEditarMatriz(matriz)">\n                        Editar',
  'edición controlada por estado'
);

template = replaceRegex(
  template,
  /\n\s*<button \*ngIf="!matriz\.puntajeResidual" type="button" \(click\)="calcularMatriz\(matriz\)"[\s\S]*?\n\s*<\/button>/,
  '',
  'botón de cálculo manual'
);

if (component.includes("type ModalTipo = 'calcular'")) {
  throw new Error('Persistió el modal de cálculo manual.');
}
if (template.includes('(click)="calcularMatriz(matriz)"')) {
  throw new Error('Persistió el botón de cálculo manual.');
}
if (!component.includes('transicionesPermitidasMatriz(estadoActual)')) {
  throw new Error('No se integraron las transiciones operativas.');
}
if (!template.includes('>Evaluadas</p>')) {
  throw new Error('No se retiró el estado técnico del KPI.');
}

fs.writeFileSync(componentPath, component, 'utf8');
fs.writeFileSync(templatePath, template, 'utf8');

console.log('Integración Fase 12.1 aplicada correctamente.');
