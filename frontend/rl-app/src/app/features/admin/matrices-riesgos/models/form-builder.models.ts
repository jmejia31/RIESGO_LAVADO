import { CampoFormulario, DefinicionFormularioEditable } from './matrices-riesgos.models';

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

export interface CampoBuilderModel {
  id: string;
  clave: string;
  etiqueta: string;
  tipo: TipoControlBuilder;
  codigoCatalogo?: string;
  opciones?: string[];
  formula?: string;
  obligatorio: boolean;
  soloLectura: boolean;
  placeholder?: string;
  textoAyuda?: string;
  anchoColumnas: number; // 1 a 6
}

export interface SeccionBuilderModel {
  id: string;
  clave: string;
  titulo: string;
  orden: number;
  columnasPorFila: number; // 1 a 6
  campos: CampoBuilderModel[];
}

export interface FormBuilderModel {
  codigoFormulario: string;
  nombreFormulario: string;
  descripcion?: string;
  secciones: SeccionBuilderModel[];
}

export const TIPOS_CONTROLES_DISPONIBLES: TipoControlDefinicion[] = [
  {
    tipo: 'texto',
    etiqueta: 'Texto',
    descripcion: 'Entrada de texto corto',
    icono: 'font-size',
    categoria: 'basico',
    requiereCatalogo: false,
    requiereOpciones: false,
    requiereFormula: false
  },
  {
    tipo: 'numero',
    etiqueta: 'Número',
    descripcion: 'Valores numéricos enteros o decimales',
    icono: 'hashtag',
    categoria: 'basico',
    requiereCatalogo: false,
    requiereOpciones: false,
    requiereFormula: false
  },
  {
    tipo: 'fecha',
    etiqueta: 'Fecha',
    descripcion: 'Selector de fecha (dd/mm/aaaa)',
    icono: 'calendar',
    categoria: 'basico',
    requiereCatalogo: false,
    requiereOpciones: false,
    requiereFormula: false
  },
  {
    tipo: 'texto-largo',
    etiqueta: 'Texto largo',
    descripcion: 'Área de texto de múltiples líneas',
    icono: 'align-left',
    categoria: 'basico',
    requiereCatalogo: false,
    requiereOpciones: false,
    requiereFormula: false
  },
  {
    tipo: 'selector-catalogo',
    etiqueta: 'Lista desplegable',
    descripcion: 'Selección única desde un catálogo',
    icono: 'chevron-down-circle',
    categoria: 'seleccion',
    requiereCatalogo: true,
    requiereOpciones: false,
    requiereFormula: false
  },
  {
    tipo: 'radio',
    etiqueta: 'Opciones de radio',
    descripcion: 'Botones de selección exclusiva',
    icono: 'radio-button-checked',
    categoria: 'seleccion',
    requiereCatalogo: false,
    requiereOpciones: true,
    requiereFormula: false
  },
  {
    tipo: 'catalogo-multiple',
    etiqueta: 'Lista de checkbox',
    descripcion: 'Selección múltiple de elementos',
    icono: 'check-square-list',
    categoria: 'seleccion',
    requiereCatalogo: true,
    requiereOpciones: false,
    requiereFormula: false
  },
  {
    tipo: 'checkbox',
    etiqueta: 'Checkbox simple',
    descripcion: 'Casilla de verificación Sí/No',
    icono: 'check-box',
    categoria: 'seleccion',
    requiereCatalogo: false,
    requiereOpciones: false,
    requiereFormula: false
  },
  {
    tipo: 'formula',
    etiqueta: 'Fórmula',
    descripcion: 'Campo calculado automáticamente',
    icono: 'function',
    categoria: 'avanzado',
    requiereCatalogo: false,
    requiereOpciones: false,
    requiereFormula: true
  }
];

export function normalizarJsonABuilderModel(jsonStr: string, defaultCodigo: string = 'FORM_DINAMICO', defaultNombre: string = 'Formulario Dinámico'): FormBuilderModel {
  if (!jsonStr || jsonStr.trim() === '') {
    return {
      codigoFormulario: defaultCodigo,
      nombreFormulario: defaultNombre,
      secciones: [
        {
          id: 'sec_1',
          clave: 'general',
          titulo: 'Datos Generales',
          orden: 1,
          columnasPorFila: 2,
          campos: []
        }
      ]
    };
  }

  try {
    const raw = JSON.parse(jsonStr) as DefinicionFormularioEditable;
    const seccionesRaw = Array.isArray(raw.secciones) ? raw.secciones : [];

    const secciones: SeccionBuilderModel[] = seccionesRaw.map((sec, secIdx) => {
      const camposRaw = Array.isArray(sec.campos) ? sec.campos : [];
      const campos: CampoBuilderModel[] = camposRaw.map((cmp, cmpIdx) => {
        let tipoMapeado: TipoControlBuilder = 'texto';
        const t = (cmp.tipo || 'texto').toLowerCase();
        if (t === 'numero' || t === 'numérico') tipoMapeado = 'numero';
        else if (t === 'fecha') tipoMapeado = 'fecha';
        else if (t === 'texto-largo' || t === 'textarea') tipoMapeado = 'texto-largo';
        else if (t === 'selector-catalogo' || t === 'catalogo' || t === 'select') tipoMapeado = 'selector-catalogo';
        else if (t === 'radio' || t === 'opciones') tipoMapeado = 'radio';
        else if (t === 'catalogo-multiple' || t === 'multiselect') tipoMapeado = 'catalogo-multiple';
        else if (t === 'checkbox' || t === 'sino' || t === 'bool') tipoMapeado = 'checkbox';
        else if (t === 'formula' || t === 'calculado') tipoMapeado = 'formula';

        return {
          id: `field_${cmp.clave || 'cmp_' + (cmpIdx + 1)}`,
          clave: cmp.clave || `campo_${cmpIdx + 1}`,
          etiqueta: cmp.etiqueta || `Campo ${cmpIdx + 1}`,
          tipo: tipoMapeado,
          codigoCatalogo: cmp.codigoCatalogo || undefined,
          obligatorio: !!cmp.obligatorio,
          soloLectura: !!cmp.soloLectura || tipoMapeado === 'formula',
          anchoColumnas: 1
        };
      });

      return {
        id: `sec_${secIdx + 1}`,
        clave: sec.clave || `seccion_${secIdx + 1}`,
        titulo: sec.titulo || `Sección ${secIdx + 1}`,
        orden: sec.orden || secIdx + 1,
        columnasPorFila: 2,
        campos
      };
    });

    return {
      codigoFormulario: raw.codigoFormulario || defaultCodigo,
      nombreFormulario: raw.nombreFormulario || defaultNombre,
      secciones: secciones.length > 0 ? secciones : [
        {
          id: 'sec_1',
          clave: 'general',
          titulo: 'Datos Generales',
          orden: 1,
          columnasPorFila: 2,
          campos: []
        }
      ]
    };
  } catch {
    return {
      codigoFormulario: defaultCodigo,
      nombreFormulario: defaultNombre,
      secciones: [
        {
          id: 'sec_1',
          clave: 'general',
          titulo: 'Datos Generales',
          orden: 1,
          columnasPorFila: 2,
          campos: []
        }
      ]
    };
  }
}

export function serializarBuilderModelAJson(model: FormBuilderModel): string {
  const definicion: DefinicionFormularioEditable = {
    codigoFormulario: model.codigoFormulario,
    nombreFormulario: model.nombreFormulario,
    secciones: model.secciones.map((sec, secIdx) => ({
      clave: sec.clave || `seccion_${secIdx + 1}`,
      titulo: sec.titulo || `Sección ${secIdx + 1}`,
      orden: secIdx + 1,
      campos: sec.campos.map(cmp => {
        const campoForm: CampoFormulario = {
          clave: cmp.clave,
          etiqueta: cmp.etiqueta,
          tipo: cmp.tipo,
          codigoCatalogo: cmp.codigoCatalogo || null,
          obligatorio: cmp.obligatorio,
          soloLectura: cmp.soloLectura
        };
        return campoForm;
      })
    }))
  };

  return JSON.stringify(definicion, null, 2);
}
