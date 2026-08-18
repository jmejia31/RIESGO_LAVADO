import {
  CampoFormulario,
  DefinicionFormularioEditable,
  MetodologiaFormulario,
  RespuestasFormulario,
  ValorRespuestaFormulario
} from '../models/matrices-riesgos.models';

export type TipoCampoRenderer =
  | 'texto'
  | 'numero'
  | 'fecha'
  | 'texto-largo'
  | 'selector-catalogo'
  | 'radio'
  | 'catalogo-multiple'
  | 'checkbox'
  | 'formula'
  | 'desconocido';

export interface TipoCampoNormalizado {
  tipo: TipoCampoRenderer;
  tipoOriginal: string | null;
}

const ALIASES_TIPO: Record<string, Exclude<TipoCampoRenderer, 'desconocido'>> = {
  texto: 'texto',
  text: 'texto',
  string: 'texto',
  cadena: 'texto',
  numero: 'numero',
  numerico: 'numero',
  'numérico': 'numero',
  number: 'numero',
  decimal: 'numero',
  integer: 'numero',
  int: 'numero',
  fecha: 'fecha',
  date: 'fecha',
  'texto-largo': 'texto-largo',
  textarea: 'texto-largo',
  'long-text': 'texto-largo',
  'selector-catalogo': 'selector-catalogo',
  catalogo: 'selector-catalogo',
  catálogo: 'selector-catalogo',
  select: 'selector-catalogo',
  dropdown: 'selector-catalogo',
  radio: 'radio',
  opciones: 'radio',
  'catalogo-multiple': 'catalogo-multiple',
  'catálogo-multiple': 'catalogo-multiple',
  multiselect: 'catalogo-multiple',
  'multi-select': 'catalogo-multiple',
  checkbox: 'checkbox',
  booleano: 'checkbox',
  boolean: 'checkbox',
  bool: 'checkbox',
  sino: 'checkbox',
  formula: 'formula',
  fórmula: 'formula',
  calculado: 'formula',
  calculada: 'formula'
};

function comoRegistro(valor: unknown): Record<string, unknown> | null {
  return valor !== null && typeof valor === 'object' && !Array.isArray(valor)
    ? valor as Record<string, unknown>
    : null;
}

function primerValor(registro: Record<string, unknown>, claves: readonly string[]): unknown {
  for (const clave of claves) {
    if (Object.prototype.hasOwnProperty.call(registro, clave)) {
      return registro[clave];
    }
  }
  return undefined;
}

function textoLimpio(valor: unknown): string {
  if (typeof valor === 'string') return valor.trim();
  if (typeof valor === 'number' && Number.isFinite(valor)) return String(valor);
  return '';
}

function numeroEnteroEnRango(valor: unknown, defecto: number, minimo: number, maximo: number): number {
  const numero = Number(valor);
  if (!Number.isFinite(numero)) return defecto;
  const entero = Math.trunc(numero);
  if (entero < minimo || entero > maximo) return defecto;
  return entero;
}

function booleanoSeguro(valor: unknown): boolean {
  if (typeof valor === 'boolean') return valor;
  if (typeof valor === 'number') return valor === 1;
  if (typeof valor === 'string') {
    const normalizado = valor.trim().toLowerCase();
    return ['true', '1', 'si', 'sí', 'yes'].includes(normalizado);
  }
  return false;
}

function opcionesSeguras(valor: unknown): string[] | null {
  if (!Array.isArray(valor)) return null;

  const opciones = valor
    .map(opcion => textoLimpio(opcion))
    .filter(Boolean);

  return opciones.length > 0 ? Array.from(new Set(opciones)) : null;
}

export function normalizarTipoCampoRenderer(tipo: unknown): TipoCampoNormalizado {
  const tipoOriginal = textoLimpio(tipo);
  if (!tipoOriginal) {
    return { tipo: 'desconocido', tipoOriginal: null };
  }

  const canonical = ALIASES_TIPO[tipoOriginal.toLowerCase()];
  return canonical
    ? { tipo: canonical, tipoOriginal }
    : { tipo: 'desconocido', tipoOriginal };
}

function normalizarCampo(
  valor: unknown,
  indiceSeccion: number,
  indiceCampo: number,
  clavesUsadas: Set<string>
): CampoFormulario | null {
  const raw = comoRegistro(valor);
  if (!raw) return null;

  const clave = textoLimpio(primerValor(raw, ['clave', 'key', 'id']));
  if (!clave) return null;

  const claveUnica = clave.toLowerCase();
  if (clavesUsadas.has(claveUnica)) return null;
  clavesUsadas.add(claveUnica);

  const tipoNormalizado = normalizarTipoCampoRenderer(primerValor(raw, ['tipo', 'type']));
  const etiqueta = textoLimpio(primerValor(raw, ['etiqueta', 'label'])) || clave;
  const codigoCatalogo = textoLimpio(primerValor(raw, ['codigoCatalogo', 'catalogoCodigo', 'catalogCode'])) || null;
  const formula = textoLimpio(primerValor(raw, ['formula'])) || null;
  const campoCanonicoIdRaw = Number(primerValor(raw, ['campoCanonicoId']));
  const campoCanonicoId = Number.isFinite(campoCanonicoIdRaw) && campoCanonicoIdRaw > 0
    ? Math.trunc(campoCanonicoIdRaw)
    : null;

  const soloLecturaConfigurado = booleanoSeguro(primerValor(raw, ['soloLectura', 'readOnly']));
  const soloLectura = soloLecturaConfigurado
    || tipoNormalizado.tipo === 'formula'
    || tipoNormalizado.tipo === 'desconocido';

  return {
    campoCanonicoId,
    clave,
    etiqueta,
    tipo: tipoNormalizado.tipo,
    tipoOriginal: tipoNormalizado.tipo === 'desconocido' ? tipoNormalizado.tipoOriginal : null,
    codigoCatalogo,
    opciones: opcionesSeguras(primerValor(raw, ['opciones', 'options'])),
    formula,
    obligatorio: booleanoSeguro(primerValor(raw, ['obligatorio', 'required'])),
    soloLectura,
    anchoColumnas: numeroEnteroEnRango(
      primerValor(raw, ['anchoColumnas', 'ancho', 'width']),
      1,
      1,
      6
    )
  };
}

function normalizarSecciones(valor: unknown): DefinicionFormularioEditable['secciones'] {
  if (!Array.isArray(valor)) return [];

  const clavesUsadas = new Set<string>();

  return valor
    .map((seccion, indiceSeccion) => {
      const raw = comoRegistro(seccion);
      if (!raw) return null;

      const clave = textoLimpio(primerValor(raw, ['clave', 'key', 'id'])) || `seccion_${indiceSeccion + 1}`;
      const titulo = textoLimpio(primerValor(raw, ['titulo', 'title'])) || `Sección ${indiceSeccion + 1}`;
      const camposRaw = primerValor(raw, ['campos', 'fields']);
      const campos = Array.isArray(camposRaw)
        ? camposRaw
          .map((campo, indiceCampo) => normalizarCampo(campo, indiceSeccion, indiceCampo, clavesUsadas))
          .filter((campo): campo is CampoFormulario => campo !== null)
        : [];

      return {
        clave,
        titulo,
        orden: numeroEnteroEnRango(primerValor(raw, ['orden', 'order']), indiceSeccion + 1, 0, 100000),
        columnasPorFila: numeroEnteroEnRango(
          primerValor(raw, ['columnasPorFila', 'columnas', 'columns']),
          2,
          1,
          6
        ),
        campos
      };
    })
    .filter((seccion): seccion is DefinicionFormularioEditable['secciones'][number] => seccion !== null)
    .sort((a, b) => a.orden - b.orden);
}

function objetoDefinicion(valor: unknown): Record<string, unknown> | null {
  if (typeof valor === 'string') {
    if (!valor.trim()) return null;
    try {
      return comoRegistro(JSON.parse(valor));
    } catch {
      return null;
    }
  }

  return comoRegistro(valor);
}

export function normalizarDefinicionFormulario(
  valor: unknown,
  codigoDefecto = '',
  nombreDefecto = ''
): DefinicionFormularioEditable {
  const raw = objetoDefinicion(valor);
  if (!raw) {
    return { codigoFormulario: codigoDefecto, nombreFormulario: nombreDefecto || codigoDefecto, secciones: [] };
  }

  const seccionesRaw = primerValor(raw, ['secciones', 'sections']);
  const catalogosRaw = primerValor(raw, ['catalogos', 'catalogs']);
  const reglasRaw = primerValor(raw, ['reglas', 'reglasCalculo', 'rules']);

  return {
    codigoFormulario: textoLimpio(primerValor(raw, ['codigoFormulario', 'formCode'])) || codigoDefecto,
    nombreFormulario: textoLimpio(primerValor(raw, ['nombreFormulario', 'formName'])) || nombreDefecto || codigoDefecto,
    secciones: normalizarSecciones(seccionesRaw),
    catalogos: Array.isArray(catalogosRaw)
      ? catalogosRaw as DefinicionFormularioEditable['catalogos']
      : undefined,
    reglas: Array.isArray(reglasRaw)
      ? reglasRaw as DefinicionFormularioEditable['reglas']
      : undefined
  };
}

export function normalizarMetodologiaFormulario(
  metodologia: MetodologiaFormulario | null | undefined
): DefinicionFormularioEditable {
  if (!metodologia) {
    return { codigoFormulario: '', nombreFormulario: '', secciones: [] };
  }

  return normalizarDefinicionFormulario({
    codigoFormulario: metodologia.codigo,
    nombreFormulario: metodologia.codigo,
    secciones: metodologia.secciones,
    catalogos: metodologia.catalogos,
    reglas: metodologia.reglas
  }, metodologia.codigo, metodologia.codigo);
}

export function normalizarRespuestasFormulario(valor: unknown): RespuestasFormulario {
  const raw = typeof valor === 'string'
    ? (() => {
        try {
          return comoRegistro(JSON.parse(valor));
        } catch {
          return null;
        }
      })()
    : comoRegistro(valor);

  if (!raw) return {};

  const respuestas: RespuestasFormulario = {};
  for (const [clave, respuesta] of Object.entries(raw)) {
    if (respuesta === null || typeof respuesta === 'string' || typeof respuesta === 'boolean') {
      respuestas[clave] = respuesta;
      continue;
    }

    if (typeof respuesta === 'number') {
      respuestas[clave] = Number.isFinite(respuesta) ? respuesta : null;
      continue;
    }

    if (Array.isArray(respuesta)) {
      respuestas[clave] = Array.from(new Set(
        respuesta
          .map(item => textoLimpio(item))
          .filter(Boolean)
      ));
      continue;
    }

    respuestas[clave] = null;
  }

  return respuestas;
}

export function tieneValorRespuesta(valor: unknown): boolean {
  if (valor === null || valor === undefined) return false;
  if (Array.isArray(valor)) return valor.length > 0;
  if (typeof valor === 'string') return valor.trim() !== '';
  if (typeof valor === 'number') return Number.isFinite(valor);
  if (typeof valor === 'boolean') return true;
  return false;
}

export function formatearValorRespuesta(valor: ValorRespuestaFormulario | undefined): string {
  if (valor === null || valor === undefined) return '-';
  if (Array.isArray(valor)) return valor.length > 0 ? valor.join(', ') : '-';
  if (typeof valor === 'boolean') return valor ? 'Sí' : 'No';
  if (typeof valor === 'number') return Number.isFinite(valor) ? String(valor) : '-';
  return valor.trim() || '-';
}
