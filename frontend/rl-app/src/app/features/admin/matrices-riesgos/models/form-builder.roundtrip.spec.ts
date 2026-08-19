import { normalizarJsonABuilderModel, serializarBuilderModelAJson, FormBuilderModel } from './form-builder.models';
import { validarFormBuilderModel } from '../utils/form-builder-validator.util';

describe('Contrato bidireccional del Form Builder', () => {
  it('preserva metadatos conocidos y futuros al editar una propiedad soportada', () => {
    const original = {
      codigoFormulario: 'FORM_LOSSLESS',
      nombreFormulario: 'Formulario Lossless',
      descripcion: 'Descripción raíz',
      propiedadFuturaRaiz: { habilitada: true, valorCero: 0, valorNulo: null },
      secciones: [
        {
          clave: 'general',
          titulo: 'General',
          orden: 1,
          columnasPorFila: 2,
          visible: false,
          metadataSeccionFutura: 'conservar',
          campos: [
            {
              clave: 'nivel',
              etiqueta: 'Nivel original',
              descripcion: 'Descripción del campo',
              tipo: 'selector-catalogo',
              codigoCatalogo: 'CAT_NIVEL',
              obligatorio: true,
              soloLectura: false,
              placeholder: 'Seleccione',
              textoAyuda: 'Ayuda visible',
              anchoColumnas: 1,
              visibleCuando: { operador: 'IGUAL_A', izquierda: { referencia: 'activo' }, derecha: { literal: true } },
              propiedadFuturaCampo: { conservar: false }
            }
          ]
        }
      ],
      catalogos: [
        {
          codigo: 'CAT_NIVEL',
          nombre: 'Niveles',
          origen: 'EMBEBIDO',
          elementos: [
            { codigo: 'A', valor: 'Alto', orden: 1, activo: true },
            { codigo: 'B', valor: 'Bajo', orden: 2, activo: false }
          ]
        }
      ],
      reglas: [{ codigo: 'R1', version: '1', algoritmoId: 'ALG-1', parametros: { cero: 0 } }]
    };

    const model = normalizarJsonABuilderModel(JSON.stringify(original));
    expect(model.descripcion).toBe('Descripción raíz');
    expect(model.secciones[0].campos[0].placeholder).toBe('Seleccione');
    expect(model.secciones[0].campos[0].textoAyuda).toBe('Ayuda visible');
    expect(model.catalogos?.[0].elementos[0].codigo).toBe('A');

    model.secciones[0].campos[0].etiqueta = 'Nivel editado';
    const serializado = JSON.parse(serializarBuilderModelAJson(model));

    expect(serializado.secciones[0].campos[0].etiqueta).toBe('Nivel editado');
    expect(serializado.secciones[0].campos[0].placeholder).toBe('Seleccione');
    expect(serializado.secciones[0].campos[0].textoAyuda).toBe('Ayuda visible');
    expect(serializado.secciones[0].campos[0].visibleCuando.derecha.literal).toBe(true);
    expect(serializado.secciones[0].campos[0].propiedadFuturaCampo.conservar).toBe(false);
    expect(serializado.secciones[0].metadataSeccionFutura).toBe('conservar');
    expect(serializado.propiedadFuturaRaiz.habilitada).toBe(true);
    expect(serializado.propiedadFuturaRaiz.valorCero).toBe(0);
    expect(serializado.propiedadFuturaRaiz.valorNulo).toBeNull();
    expect(serializado.catalogos[0].origen).toBe('EMBEBIDO');
    expect(serializado.catalogos[0].elementos[1].activo).toBe(false);
    expect(serializado.reglas[0].parametros.cero).toBe(0);
  });

  it('preserva envelope, dialecto v3 y catálogos en forma de mapa', () => {
    const contratoV3Reducido = {
      tipoDocumento: 'CONTRATO_FORMULARIO_DINAMICO',
      versionContrato: '1.0.0',
      arquitecturaLogica: { versionPublicadaInmutable: true },
      definicionFormulario: {
        identificador: 'MATRIZ_RIESGOS_IHSS',
        nombre: 'Matriz Consolidada',
        version: 1,
        configuracion: { guardadoAutomatico: true },
        secciones: [
          {
            identificador: 'IDENTIFICACION',
            titulo: 'Identificación',
            orden: 1,
            activo: true,
            visible: true,
            campos: [
              {
                identificador: 'CAMPO_TIPORIESGO',
                rutaDatos: 'tipoRiesgo',
                etiqueta: 'Tipo de Riesgo',
                tipo: 'SELECCION',
                orden: 1,
                activo: true,
                visible: true,
                obligatorio: true,
                soloLectura: false,
                catalogo: 'TIPOS_RIESGO',
                valorAlmacenado: 'codigo',
                etiquetaPresentada: 'etiqueta',
                restricciones: { longitudMaxima: 50 }
              }
            ]
          }
        ]
      },
      catalogos: {
        TIPOS_RIESGO: {
          identificador: 'TIPOS_RIESGO',
          tipoOrigen: 'EMBEBIDO',
          version: 1,
          activo: true,
          elementos: [
            { codigo: 'TEC', valor: 'Tecnológico', etiqueta: 'Tecnológico', activo: true },
            { codigo: 'OP', valor: 'Operativo', etiqueta: 'Operativo', activo: true }
          ]
        }
      }
    };

    const model = normalizarJsonABuilderModel(JSON.stringify(contratoV3Reducido));
    expect(model.definicionAnidada).toBe(true);
    expect(model.codigoFormulario).toBe('MATRIZ_RIESGOS_IHSS');
    expect(model.secciones[0].clave).toBe('IDENTIFICACION');
    expect(model.secciones[0].campos[0].clave).toBe('tipoRiesgo');
    expect(model.secciones[0].campos[0].tipo).toBe('selector-catalogo');
    expect(model.secciones[0].campos[0].tipoOriginal).toBe('SELECCION');
    expect(model.secciones[0].campos[0].codigoCatalogo).toBe('TIPOS_RIESGO');
    expect(model.catalogosFormaOriginal).toBe('map');
    expect(model.catalogos?.[0].elementos[0].codigo).toBe('TEC');

    model.secciones[0].campos[0].etiqueta = 'Tipo de Riesgo editado';
    const serializado = JSON.parse(serializarBuilderModelAJson(model));

    expect(serializado.tipoDocumento).toBe('CONTRATO_FORMULARIO_DINAMICO');
    expect(serializado.arquitecturaLogica.versionPublicadaInmutable).toBe(true);
    expect(serializado.definicionFormulario.identificador).toBe('MATRIZ_RIESGOS_IHSS');
    expect(serializado.definicionFormulario.configuracion.guardadoAutomatico).toBe(true);
    expect(serializado.definicionFormulario.secciones[0].identificador).toBe('IDENTIFICACION');
    expect(serializado.definicionFormulario.secciones[0].campos[0].rutaDatos).toBe('tipoRiesgo');
    expect(serializado.definicionFormulario.secciones[0].campos[0].etiqueta).toBe('Tipo de Riesgo editado');
    expect(serializado.definicionFormulario.secciones[0].campos[0].tipo).toBe('SELECCION');
    expect(serializado.definicionFormulario.secciones[0].campos[0].valorAlmacenado).toBe('codigo');
    expect(serializado.definicionFormulario.secciones[0].campos[0].restricciones.longitudMaxima).toBe(50);
    expect(serializado.catalogos.TIPOS_RIESGO.tipoOrigen).toBe('EMBEBIDO');
    expect(serializado.catalogos.TIPOS_RIESGO.elementos[0].activo).toBe(true);
    expect(serializado.catalogos.TIPOS_RIESGO.elementos[0].codigo).toBe('TEC');
  });

  it('no degrada tipos aún no editables cuando no fueron modificados', () => {
    const original = {
      codigoFormulario: 'FORM_RICO',
      nombreFormulario: 'Rico',
      secciones: [{
        clave: 'sec', titulo: 'Sec', orden: 1, campos: [
          { clave: 'controles', etiqueta: 'Controles', tipo: 'REPETIDOR', obligatorio: false, soloLectura: false, camposElemento: [{ rutaRelativa: 'descripcion', tipo: 'AREA_TEXTO' }] }
        ]
      }]
    };

    const model = normalizarJsonABuilderModel(JSON.stringify(original));
    expect(model.secciones[0].campos[0].tipo).toBe('texto');
    expect(model.secciones[0].campos[0].tipoOriginal).toBe('REPETIDOR');

    const serializado = JSON.parse(serializarBuilderModelAJson(model));
    expect(serializado.secciones[0].campos[0].tipo).toBe('REPETIDOR');
    expect(serializado.secciones[0].campos[0].camposElemento[0].rutaRelativa).toBe('descripcion');
  });

  it('valida unicidad de catálogos, elementos e integridad de referencias', () => {
    const model: FormBuilderModel = {
      codigoFormulario: 'FORM_VALIDACION',
      nombreFormulario: 'Validación',
      catalogos: [
        { codigo: 'CAT_A', nombre: 'Catálogo A', elementos: [
          { codigo: 'X', valor: 'Uno', orden: 1 },
          { codigo: 'x', valor: 'Duplicado', orden: 0 }
        ] },
        { codigo: 'cat_a', nombre: 'Duplicado', elementos: [] }
      ],
      secciones: [{ id: 's', clave: 's', titulo: 'S', orden: 1, columnasPorFila: 2, campos: [
        { id: 'c', clave: 'seleccion', etiqueta: 'Selección', tipo: 'selector-catalogo', codigoCatalogo: 'NO_EXISTE', obligatorio: true, soloLectura: false, anchoColumnas: 1 }
      ] }]
    };

    const mensajes = validarFormBuilderModel(model).map(error => error.mensaje).join(' | ');
    expect(mensajes).toContain('código de catálogo');
    expect(mensajes).toContain('está duplicado dentro del catálogo');
    expect(mensajes).toContain('orden entero');
    expect(mensajes).toContain('catálogo inexistente');
  });

  it('acepta códigos alfanuméricos y referencias válidas sin convertirlos a números', () => {
    const model: FormBuilderModel = {
      codigoFormulario: 'FORM_OK',
      nombreFormulario: 'OK',
      catalogos: [{ codigo: 'AREAS', nombre: 'Áreas', elementos: [
        { codigo: 'G-IVM', valor: 'Gerencia IVM', orden: 1 },
        { codigo: 'GTIC', valor: 'Tecnología', orden: 2 }
      ] }],
      secciones: [{ id: 's', clave: 's', titulo: 'S', orden: 1, columnasPorFila: 2, campos: [
        { id: 'c1', clave: 'area', etiqueta: 'Área', tipo: 'selector-catalogo', codigoCatalogo: 'AREAS', obligatorio: true, soloLectura: false, anchoColumnas: 1 },
        { id: 'c2', clave: 'areasRelacionadas', etiqueta: 'Áreas relacionadas', tipo: 'catalogo-multiple', codigoCatalogo: 'AREAS', obligatorio: false, soloLectura: false, anchoColumnas: 1 }
      ] }]
    };

    expect(validarFormBuilderModel(model)).toEqual([]);
    const serializado = JSON.parse(serializarBuilderModelAJson(model));
    expect(serializado.catalogos[0].elementos.map((item: { codigo: string }) => item.codigo)).toEqual(['G-IVM', 'GTIC']);
  });
});
