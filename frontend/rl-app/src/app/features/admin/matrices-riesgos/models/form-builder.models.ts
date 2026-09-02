import { ReglaCalculoMatrices } from './matrices-riesgos.models';

export type TipoControlBuilder =
  | 'texto'
  | 'numero'
  | 'fecha'
  | 'texto-largo'
  | 'selector-catalogo'
  | 'radio'
  | 'catalogo-multiple'
  | 'checkbox'
  | 'formula';

export type JsonObject = Record<string, unknown>;

export interface TipoControlDefinicion {
  tipo: TipoControlBuilder;
  etiqueta: string;
  descripcion: string;
  icono: string;
  categoria: 'basico' | 'seleccion' | 'avanzado';
  requiereCatalogo: boolean;
  requiereOpciones: boolean;
  requiereFormula: boolean;
}

export interface ElementoCatalogoBuilderModel {
  codigo: string;
  valor: string;
  orden: number;
  metadatosOriginales?: JsonObject;
}

export interface CatalogoBuilderModel {
  codigo: string;
  nombre: string;
  elementos: ElementoCatalogoBuilderModel[];
  metadatosOriginales?: JsonObject;
  elementosFuente?: 'elementos' | 'elementosRespaldo';
}

export interface CampoBuilderModel {
  id: string;
  clave: string;
  etiqueta: string;
  descripcion?: string;
  tipo: TipoControlBuilder;
  /** Tipo exacto recibido cuando se usó un alias o un tipo aún no editable en el Builder. */
  tipoOriginal?: string;
  claveFuente?: 'clave' | 'rutaDatos' | 'identificador';
  catalogoFuente?: 'codigoCatalogo' | 'catalogoCodigo' | 'catalogo';
  formulaFuente?: 'formula' | 'calculo' | 'referenciaCalculo';
  formulaId?: number;
  formulaVersionId?: number;
  formulaCodigo?: string;
  formulaVersion?: number;
  codigoCatalogo?: string;
  opciones?: string[];
  formula?: string;
  obligatorio: boolean;
  soloLectura: boolean;
  placeholder?: string;
  textoAyuda?: string;
  orden?: number;
  anchoColumnas: number; // 1 a 6
  metadatosOriginales?: JsonObject;
}

export interface SeccionBuilderModel {
  id: string;
  clave: string;
  claveFuente?: 'clave' | 'identificador';
  titulo: string;
  orden: number;
  columnasPorFila: number; // 1 a 6
  campos: CampoBuilderModel[];
  metadatosOriginales?: JsonObject;
}

export interface FormBuilderModel {
  codigoFormulario: string;
  nombreFormulario: string;
  descripcion?: string;
  secciones: SeccionBuilderModel[];
  catalogos?: CatalogoBuilderModel[];
  reglas?: ReglaCalculoMatrices[];
  metadatosOriginales?: JsonObject;
  definicionAnidada?: boolean;
  codigoFuente?: 'codigoFormulario' | 'identificador';
  nombreFuente?: 'nombreFormulario' | 'nombre';
  catalogosUbicacion?: 'definicion' | 'raiz';
  catalogosFormaOriginal?: 'array' | 'map';
  reglasFuente?: 'reglas' | 'reglasCalculo';
}

function siguienteIdentificador(base: string, usados: Set<string>, separador = '_copia'): string {
  const limpio = base.trim() || 'elemento';
  let candidato = `${limpio}${separador}`;
  let indice = 2;
  while (usados.has(candidato.toLowerCase())) {
    candidato = `${limpio}${separador}_${indice}`;
    indice += 1;
  }
  usados.add(candidato.toLowerCase());
  return candidato;
}

/** Duplica una sección dentro del modelo sin compartir referencias ni identificadores técnicos. */
export function duplicarSeccionBuilderModel(model: FormBuilderModel, seccionId: string): { model: FormBuilderModel; seccion: SeccionBuilderModel } | null {
  const indiceOrigen = model.secciones.findIndex(seccion => seccion.id === seccionId);
  if (indiceOrigen < 0) return null;

  const clavesSeccion = new Set(model.secciones.map(seccion => seccion.clave.toLowerCase()));
  const idsSeccion = new Set(model.secciones.map(seccion => seccion.id.toLowerCase()));
  const origen = model.secciones[indiceOrigen];
  const clave = siguienteIdentificador(origen.clave, clavesSeccion);
  const id = siguienteIdentificador(origen.id, idsSeccion, '_copy');
  const clavesCampo = new Set(model.secciones.flatMap(seccion => seccion.campos.map(campo => campo.clave.toLowerCase())));
  const idsCampo = new Set(model.secciones.flatMap(seccion => seccion.campos.map(campo => campo.id.toLowerCase())));
  const campos = origen.campos.map((campo, indice) => ({
    ...clonarJson(campo),
    id: siguienteIdentificador(campo.id || `field_${indice + 1}`, idsCampo, '_copy'),
    clave: siguienteIdentificador(campo.clave || `campo_${indice + 1}`, clavesCampo),
    orden: campo.orden ?? indice + 1
  }));
  const seccion: SeccionBuilderModel = {
    ...clonarJson(origen),
    id,
    clave,
    titulo: `${origen.titulo} (copia)`,
    orden: indiceOrigen + 2,
    campos
  };
  const secciones = [...model.secciones];
  secciones.splice(indiceOrigen + 1, 0, seccion);
  const seccionesOrdenadas = secciones.map((item, indice) => ({ ...item, orden: indice + 1 }));
  return { model: { ...model, secciones: seccionesOrdenadas }, seccion: seccionesOrdenadas[indiceOrigen + 1] };
}

export const TIPOS_CONTROLES_DISPONIBLES: TipoControlDefinicion[] = [
  { tipo: 'texto', etiqueta: 'Texto', descripcion: 'Entrada de texto corto', icono: 'font-size', categoria: 'basico', requiereCatalogo: false, requiereOpciones: false, requiereFormula: false },
  { tipo: 'numero', etiqueta: 'Número', descripcion: 'Valores numéricos enteros o decimales', icono: 'hashtag', categoria: 'basico', requiereCatalogo: false, requiereOpciones: false, requiereFormula: false },
  { tipo: 'fecha', etiqueta: 'Fecha', descripcion: 'Selector de fecha (dd/mm/aaaa)', icono: 'calendar', categoria: 'basico', requiereCatalogo: false, requiereOpciones: false, requiereFormula: false },
  { tipo: 'texto-largo', etiqueta: 'Texto largo', descripcion: 'Área de texto de múltiples líneas', icono: 'align-left', categoria: 'basico', requiereCatalogo: false, requiereOpciones: false, requiereFormula: false },
  { tipo: 'selector-catalogo', etiqueta: 'Lista desplegable', descripcion: 'Selección única desde un catálogo', icono: 'chevron-down-circle', categoria: 'seleccion', requiereCatalogo: true, requiereOpciones: false, requiereFormula: false },
  { tipo: 'radio', etiqueta: 'Opciones de radio', descripcion: 'Botones de selección exclusiva', icono: 'radio-button-checked', categoria: 'seleccion', requiereCatalogo: false, requiereOpciones: true, requiereFormula: false },
  { tipo: 'catalogo-multiple', etiqueta: 'Lista de checkbox', descripcion: 'Selección múltiple de elementos', icono: 'check-square-list', categoria: 'seleccion', requiereCatalogo: true, requiereOpciones: false, requiereFormula: false },
  { tipo: 'checkbox', etiqueta: 'Checkbox simple', descripcion: 'Casilla de verificación Sí/No', icono: 'check-box', categoria: 'seleccion', requiereCatalogo: false, requiereOpciones: false, requiereFormula: false },
  { tipo: 'formula', etiqueta: 'Fórmula', descripcion: 'Campo calculado automáticamente', icono: 'function', categoria: 'avanzado', requiereCatalogo: false, requiereOpciones: false, requiereFormula: true }
];

function esObjeto(value: unknown): value is JsonObject {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
}

function clonarJson<T>(value: T): T {
  if (value === undefined || value === null) return value;
  return JSON.parse(JSON.stringify(value)) as T;
}

function texto(value: unknown): string | undefined {
  return typeof value === 'string' && value.trim() !== '' ? value : undefined;
}

function numeroEnteroPositivo(value: unknown, fallback: number): number {
  return typeof value === 'number' && Number.isInteger(value) && value > 0 ? value : fallback;
}

function normalizarTipoBuilder(value: unknown): { tipo: TipoControlBuilder; tipoOriginal?: string } {
  const original = texto(value) ?? 'texto';
  const normalizado = original.trim().toLowerCase().replace(/_/g, '-');
  let tipo: TipoControlBuilder;

  switch (normalizado) {
    case 'texto': tipo = 'texto'; break;
    case 'numero':
    case 'numérico':
    case 'numerico':
    case 'entero':
    case 'decimal': tipo = 'numero'; break;
    case 'fecha': tipo = 'fecha'; break;
    case 'texto-largo':
    case 'textarea':
    case 'area-texto': tipo = 'texto-largo'; break;
    case 'selector-catalogo':
    case 'catalogo':
    case 'select':
    case 'seleccion': tipo = 'selector-catalogo'; break;
    case 'radio':
    case 'opciones': tipo = 'radio'; break;
    case 'catalogo-multiple':
    case 'multiselect':
    case 'seleccion-multiple': tipo = 'catalogo-multiple'; break;
    case 'checkbox':
    case 'sino':
    case 'bool':
    case 'booleano': tipo = 'checkbox'; break;
    case 'formula':
    case 'calculado':
    case 'calculo-sistema':
    case 'texto-calculado': tipo = 'formula'; break;
    default: tipo = 'texto'; break;
  }

  return original === tipo ? { tipo } : { tipo, tipoOriginal: original };
}

function serializarTipo(campo: CampoBuilderModel): string {
  if (!campo.tipoOriginal) return campo.tipo;
  return normalizarTipoBuilder(campo.tipoOriginal).tipo === campo.tipo ? campo.tipoOriginal : campo.tipo;
}

function normalizarCatalogos(rawCatalogos: unknown): { catalogos?: CatalogoBuilderModel[]; forma?: 'array' | 'map' } {
  if (Array.isArray(rawCatalogos)) {
    return {
      forma: 'array',
      catalogos: rawCatalogos
        .map((raw, index) => normalizarCatalogo(raw, undefined, index))
        .filter((catalogo): catalogo is CatalogoBuilderModel => catalogo !== null)
    };
  }

  if (esObjeto(rawCatalogos)) {
    return {
      forma: 'map',
      catalogos: Object.entries(rawCatalogos)
        .map(([codigo, raw], index) => normalizarCatalogo(raw, codigo, index))
        .filter((catalogo): catalogo is CatalogoBuilderModel => catalogo !== null)
    };
  }

  return {};
}

function normalizarCatalogo(raw: unknown, codigoMapa: string | undefined, index: number): CatalogoBuilderModel | null {
  if (!esObjeto(raw)) return null;

  const codigo = texto(raw['codigo']) ?? texto(raw['identificador']) ?? codigoMapa ?? `CATALOGO_${index + 1}`;
  const nombre = texto(raw['nombre']) ?? texto(raw['etiqueta']) ?? texto(raw['descripcion']) ?? codigo;
  const elementosFuente: 'elementos' | 'elementosRespaldo' = Array.isArray(raw['elementos']) ? 'elementos' : 'elementosRespaldo';
  const rawElementos = Array.isArray(raw[elementosFuente]) ? raw[elementosFuente] as unknown[] : [];
  const elementos: ElementoCatalogoBuilderModel[] = rawElementos
    .filter(esObjeto)
    .map((elemento, elementoIndex) => ({
      codigo: texto(elemento['codigo']) ?? `ELEMENTO_${elementoIndex + 1}`,
      valor: texto(elemento['valor']) ?? texto(elemento['etiqueta']) ?? texto(elemento['codigo']) ?? `Elemento ${elementoIndex + 1}`,
      orden: numeroEnteroPositivo(elemento['orden'], elementoIndex + 1),
      metadatosOriginales: clonarJson(elemento)
    }));

  return {
    codigo,
    nombre,
    elementos,
    elementosFuente,
    metadatosOriginales: clonarJson(raw)
  };
}

function serializarCatalogo(catalogo: CatalogoBuilderModel, forma: 'array' | 'map'): JsonObject {
  const raw = clonarJson(catalogo.metadatosOriginales ?? {});

  if ('identificador' in raw && !('codigo' in raw)) raw['identificador'] = catalogo.codigo;
  else raw['codigo'] = catalogo.codigo;

  if ('nombre' in raw || forma === 'array') raw['nombre'] = catalogo.nombre;
  else if ('etiqueta' in raw) raw['etiqueta'] = catalogo.nombre;
  else if ('descripcion' in raw && raw['descripcion'] !== null) raw['descripcion'] = catalogo.nombre;

  const fuente = catalogo.elementosFuente ?? 'elementos';
  raw[fuente] = catalogo.elementos.map((elemento, index) => {
    const original = clonarJson(elemento.metadatosOriginales ?? {});
    original['codigo'] = elemento.codigo;
    if ('valor' in original || !('etiqueta' in original)) original['valor'] = elemento.valor;
    if ('etiqueta' in original) original['etiqueta'] = elemento.valor;
    if ('orden' in original || forma === 'array') original['orden'] = elemento.orden || index + 1;
    return original;
  });

  return raw;
}

function construirModeloVacio(defaultCodigo: string, defaultNombre: string): FormBuilderModel {
  return {
    codigoFormulario: defaultCodigo,
    nombreFormulario: defaultNombre,
    secciones: [{ id: 'sec_1', clave: 'general', titulo: 'Datos Generales', orden: 1, columnasPorFila: 2, campos: [] }]
  };
}

export function normalizarJsonABuilderModel(jsonStr: string, defaultCodigo: string = 'FORM_DINAMICO', defaultNombre: string = 'Formulario Dinámico'): FormBuilderModel {
  if (!jsonStr || jsonStr.trim() === '') return construirModeloVacio(defaultCodigo, defaultNombre);

  try {
    const parsed = JSON.parse(jsonStr) as unknown;
    if (!esObjeto(parsed)) return construirModeloVacio(defaultCodigo, defaultNombre);

    const rawRaiz = parsed;
    const definicionAnidada = esObjeto(rawRaiz['definicionFormulario']);
    const rawDefinicion = definicionAnidada ? rawRaiz['definicionFormulario'] as JsonObject : rawRaiz;
    const seccionesRaw = Array.isArray(rawDefinicion['secciones']) ? rawDefinicion['secciones'] as unknown[] : [];

    const secciones: SeccionBuilderModel[] = seccionesRaw.filter(esObjeto).map((sec, secIdx) => {
      const camposRaw = Array.isArray(sec['campos']) ? sec['campos'] as unknown[] : [];
      const campos: CampoBuilderModel[] = camposRaw.filter(esObjeto).map((cmp, cmpIdx) => {
        const claveFuente: CampoBuilderModel['claveFuente'] = texto(cmp['clave']) ? 'clave' : texto(cmp['rutaDatos']) ? 'rutaDatos' : 'identificador';
        const clave = texto(cmp[claveFuente]) ?? `campo_${cmpIdx + 1}`;
        const tipoResuelto = normalizarTipoBuilder(cmp['tipo']);
        const catalogoFuente: CampoBuilderModel['catalogoFuente'] | undefined = texto(cmp['codigoCatalogo']) ? 'codigoCatalogo' : texto(cmp['catalogoCodigo']) ? 'catalogoCodigo' : texto(cmp['catalogo']) ? 'catalogo' : undefined;
        const formulaFuente: CampoBuilderModel['formulaFuente'] | undefined = texto(cmp['formula']) ? 'formula' : texto(cmp['calculo']) ? 'calculo' : texto(cmp['referenciaCalculo']) ? 'referenciaCalculo' : undefined;

        return {
          id: `field_${texto(cmp['identificador']) ?? clave}`,
          clave,
          claveFuente,
          etiqueta: texto(cmp['etiqueta']) ?? `Campo ${cmpIdx + 1}`,
          descripcion: texto(cmp['descripcion']),
          tipo: tipoResuelto.tipo,
          tipoOriginal: tipoResuelto.tipoOriginal,
          catalogoFuente,
          formulaFuente,
          codigoCatalogo: catalogoFuente ? texto(cmp[catalogoFuente]) : undefined,
          opciones: Array.isArray(cmp['opciones']) ? (cmp['opciones'] as unknown[]).filter((opcion): opcion is string => typeof opcion === 'string') : undefined,
          formula: formulaFuente ? texto(cmp[formulaFuente]) : undefined,
          formulaId: typeof cmp['formulaId'] === 'number' ? cmp['formulaId'] : undefined,
          formulaVersionId: typeof cmp['formulaVersionId'] === 'number' ? cmp['formulaVersionId'] : undefined,
          formulaCodigo: texto(cmp['formulaCodigo']),
          formulaVersion: typeof cmp['formulaVersion'] === 'number' ? cmp['formulaVersion'] : undefined,
          obligatorio: !!cmp['obligatorio'],
          soloLectura: !!cmp['soloLectura'] || tipoResuelto.tipo === 'formula',
          placeholder: texto(cmp['placeholder']),
          textoAyuda: texto(cmp['textoAyuda']),
          orden: numeroEnteroPositivo(cmp['orden'], cmpIdx + 1),
          anchoColumnas: typeof cmp['anchoColumnas'] === 'number' && cmp['anchoColumnas'] >= 1 && cmp['anchoColumnas'] <= 6 ? cmp['anchoColumnas'] : 1,
          metadatosOriginales: clonarJson(cmp)
        };
      });

      const claveFuente: SeccionBuilderModel['claveFuente'] = texto(sec['clave']) ? 'clave' : 'identificador';
      const clave = texto(sec[claveFuente]) ?? `seccion_${secIdx + 1}`;
      return {
        id: `sec_${texto(sec['identificador']) ?? clave}`,
        clave,
        claveFuente,
        titulo: texto(sec['titulo']) ?? `Sección ${secIdx + 1}`,
        orden: numeroEnteroPositivo(sec['orden'], secIdx + 1),
        columnasPorFila: typeof sec['columnasPorFila'] === 'number' && sec['columnasPorFila'] >= 1 && sec['columnasPorFila'] <= 6 ? sec['columnasPorFila'] : 2,
        campos,
        metadatosOriginales: clonarJson(sec)
      };
    });

    const catalogosUbicacion: FormBuilderModel['catalogosUbicacion'] = rawDefinicion['catalogos'] !== undefined ? 'definicion' : rawRaiz['catalogos'] !== undefined ? 'raiz' : undefined;
    const rawCatalogos = catalogosUbicacion === 'definicion' ? rawDefinicion['catalogos'] : catalogosUbicacion === 'raiz' ? rawRaiz['catalogos'] : undefined;
    const catalogosNormalizados = normalizarCatalogos(rawCatalogos);

    const reglasFuente: FormBuilderModel['reglasFuente'] = Array.isArray(rawDefinicion['reglas']) ? 'reglas' : Array.isArray(rawDefinicion['reglasCalculo']) ? 'reglasCalculo' : undefined;
    const reglas = reglasFuente ? clonarJson(rawDefinicion[reglasFuente] as ReglaCalculoMatrices[]) : undefined;
    const codigoFuente: FormBuilderModel['codigoFuente'] = texto(rawDefinicion['codigoFormulario']) ? 'codigoFormulario' : 'identificador';
    const nombreFuente: FormBuilderModel['nombreFuente'] = texto(rawDefinicion['nombreFormulario']) ? 'nombreFormulario' : 'nombre';

    return {
      codigoFormulario: texto(rawDefinicion[codigoFuente]) ?? defaultCodigo,
      nombreFormulario: texto(rawDefinicion[nombreFuente]) ?? defaultNombre,
      descripcion: texto(rawDefinicion['descripcion']),
      secciones: secciones.length > 0 ? secciones : construirModeloVacio(defaultCodigo, defaultNombre).secciones,
      catalogos: catalogosNormalizados.catalogos,
      reglas,
      metadatosOriginales: clonarJson(rawRaiz),
      definicionAnidada,
      codigoFuente,
      nombreFuente,
      catalogosUbicacion,
      catalogosFormaOriginal: catalogosNormalizados.forma,
      reglasFuente
    };
  } catch {
    return construirModeloVacio(defaultCodigo, defaultNombre);
  }
}

function serializarCampo(campo: CampoBuilderModel, index: number): JsonObject {
  const esNuevo = !campo.metadatosOriginales;
  const raw = clonarJson(campo.metadatosOriginales ?? {});
  const claveFuente = campo.claveFuente ?? 'clave';
  raw[claveFuente] = campo.clave;
  raw['etiqueta'] = campo.etiqueta;
  raw['tipo'] = serializarTipo(campo);
  raw['obligatorio'] = campo.obligatorio;
  raw['soloLectura'] = campo.soloLectura;

  if ('orden' in raw || campo.orden !== undefined) raw['orden'] = campo.orden ?? index + 1;
  if (esNuevo || 'anchoColumnas' in raw || campo.anchoColumnas !== 1) raw['anchoColumnas'] = campo.anchoColumnas;
  if (campo.descripcion !== undefined || 'descripcion' in raw) raw['descripcion'] = campo.descripcion ?? raw['descripcion'];
  if (campo.placeholder !== undefined || 'placeholder' in raw) raw['placeholder'] = campo.placeholder ?? raw['placeholder'];
  if (campo.textoAyuda !== undefined || 'textoAyuda' in raw) raw['textoAyuda'] = campo.textoAyuda ?? raw['textoAyuda'];

  const catalogoFuente = campo.catalogoFuente ?? ('codigoCatalogo' in raw ? 'codigoCatalogo' : 'catalogo' in raw ? 'catalogo' : 'codigoCatalogo');
  if (campo.codigoCatalogo) raw[catalogoFuente] = campo.codigoCatalogo;
  else if (esNuevo || catalogoFuente in raw) raw[catalogoFuente] = null;

  if (campo.opciones && campo.opciones.length > 0) raw['opciones'] = campo.opciones;
  else if (esNuevo || 'opciones' in raw) raw['opciones'] = null;

  const formulaFuente = campo.formulaFuente ?? ('formula' in raw ? 'formula' : 'calculo' in raw ? 'calculo' : 'formula');
  if (campo.formula) raw[formulaFuente] = campo.formula;
  else if (esNuevo || formulaFuente in raw) raw[formulaFuente] = null;
  const centralFormulaKeys: Array<[string, unknown]> = [
    ['formulaId', campo.formulaId],
    ['formulaVersionId', campo.formulaVersionId],
    ['formulaCodigo', campo.formulaCodigo],
    ['formulaVersion', campo.formulaVersion]
  ];
  for (const [key, value] of centralFormulaKeys) {
    if (value !== undefined) raw[key] = value;
    else delete raw[key];
  }

  return raw;
}

function serializarSeccion(seccion: SeccionBuilderModel, index: number): JsonObject {
  const esNueva = !seccion.metadatosOriginales;
  const raw = clonarJson(seccion.metadatosOriginales ?? {});
  raw[seccion.claveFuente ?? 'clave'] = seccion.clave;
  raw['titulo'] = seccion.titulo;
  raw['orden'] = seccion.orden || index + 1;
  if (esNueva || 'columnasPorFila' in raw || seccion.columnasPorFila !== 2) raw['columnasPorFila'] = seccion.columnasPorFila;
  raw['campos'] = seccion.campos.map((campo, index) => serializarCampo(campo, index));
  return raw;
}

export function serializarBuilderModelAJson(model: FormBuilderModel): string {
  const raiz = clonarJson(model.metadatosOriginales ?? {});
  const definicion = model.definicionAnidada && esObjeto(raiz['definicionFormulario'])
    ? clonarJson(raiz['definicionFormulario'] as JsonObject)
    : model.definicionAnidada ? {} : raiz;

  definicion[model.codigoFuente ?? 'codigoFormulario'] = model.codigoFormulario;
  definicion[model.nombreFuente ?? 'nombreFormulario'] = model.nombreFormulario;
  if (model.descripcion !== undefined || 'descripcion' in definicion) definicion['descripcion'] = model.descripcion ?? definicion['descripcion'];
  definicion['secciones'] = model.secciones.map((seccion, index) => serializarSeccion(seccion, index));

  if (model.catalogos) {
    const forma = model.catalogosFormaOriginal ?? 'array';
    const serializados = model.catalogos.map(catalogo => serializarCatalogo(catalogo, forma));
    const valorCatalogos: unknown = forma === 'map'
      ? Object.fromEntries(model.catalogos.map((catalogo, index) => [catalogo.codigo, serializados[index]]))
      : serializados;

    if (model.catalogosUbicacion === 'raiz' && model.definicionAnidada) raiz['catalogos'] = valorCatalogos;
    else definicion['catalogos'] = valorCatalogos;
  }

  if (model.reglas) definicion[model.reglasFuente ?? 'reglas'] = clonarJson(model.reglas);

  if (model.definicionAnidada) raiz['definicionFormulario'] = definicion;
  return JSON.stringify(model.definicionAnidada ? raiz : definicion, null, 2);
}
