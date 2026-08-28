import { FormBuilderModel } from '../models/form-builder.models';
import { CampoFormulario, RespuestasFormulario } from '../models/matrices-riesgos.models';
import { evaluarFormulaCampo } from './dynamic-formula-evaluator.util';

export interface FormBuilderValidationError {
  campo: string;
  mensaje: string;
}

export function validarFormBuilderModel(model: FormBuilderModel): FormBuilderValidationError[] {
  const errores: FormBuilderValidationError[] = [];

  if (!model) {
    errores.push({ campo: 'Modelo', mensaje: 'La estructura del formulario no puede ser nula.' });
    return errores;
  }

  if (!model.secciones || model.secciones.length === 0) {
    errores.push({ campo: 'Secciones', mensaje: 'El formulario debe contener al menos una sección.' });
    return errores;
  }

  const codigosCatalogo = new Set<string>();
  (model.catalogos ?? []).forEach((catalogo, catalogoIdx) => {
    const posicion = `Catálogo ${catalogoIdx + 1}`;
    const codigo = catalogo.codigo?.trim();
    const nombre = catalogo.nombre?.trim();

    if (!codigo) {
      errores.push({ campo: posicion, mensaje: 'El código técnico del catálogo es obligatorio.' });
    } else {
      const codigoLower = codigo.toLowerCase();
      if (codigosCatalogo.has(codigoLower)) {
        errores.push({ campo: codigo, mensaje: `El código de catálogo "${catalogo.codigo}" está duplicado. Debe ser único sin distinguir mayúsculas/minúsculas.` });
      } else {
        codigosCatalogo.add(codigoLower);
      }
    }

    if (!nombre) {
      errores.push({ campo: codigo || posicion, mensaje: `El catálogo "${codigo || posicion}" requiere un nombre visible.` });
    }

    const codigosElementos = new Set<string>();
    (catalogo.elementos ?? []).forEach((elemento, elementoIdx) => {
      const elementoPosicion = `${codigo || posicion}, elemento ${elementoIdx + 1}`;
      const codigoElemento = elemento.codigo?.trim();
      const valorElemento = elemento.valor?.trim();

      if (!codigoElemento) {
        errores.push({ campo: elementoPosicion, mensaje: 'El código técnico del elemento de catálogo es obligatorio.' });
      } else {
        const codigoElementoLower = codigoElemento.toLowerCase();
        if (codigosElementos.has(codigoElementoLower)) {
          errores.push({ campo: codigoElemento, mensaje: `El elemento "${elemento.codigo}" está duplicado dentro del catálogo "${codigo || posicion}".` });
        } else {
          codigosElementos.add(codigoElementoLower);
        }
      }

      if (!valorElemento) {
        errores.push({ campo: codigoElemento || elementoPosicion, mensaje: `El elemento "${codigoElemento || elementoPosicion}" requiere una etiqueta/valor visible.` });
      }

      if (!Number.isInteger(elemento.orden) || elemento.orden < 1) {
        errores.push({ campo: codigoElemento || elementoPosicion, mensaje: `El elemento "${codigoElemento || elementoPosicion}" requiere un orden entero mayor o igual a 1.` });
      }
    });
  });

  const clavesCamposVistas = new Set<string>();

  model.secciones.forEach((sec, secIdx) => {
    if (!sec.titulo || sec.titulo.trim() === '') {
      errores.push({ campo: `Sección ${secIdx + 1}`, mensaje: `La sección ${secIdx + 1} requiere un título descriptivo.` });
    }

    if (!sec.campos || sec.campos.length === 0) {
      errores.push({ campo: sec.titulo || `Sección ${secIdx + 1}`, mensaje: `La sección "${sec.titulo || secIdx + 1}" debe tener al menos un campo configurado.` });
    } else {
      sec.campos.forEach((cmp, cmpIdx) => {
        const posicion = `Sección ${secIdx + 1}, Campo ${cmpIdx + 1}`;

        if (!cmp.clave || cmp.clave.trim() === '') {
          errores.push({ campo: posicion, mensaje: 'La clave técnica del campo es obligatoria.' });
        } else {
          const claveLower = cmp.clave.trim().toLowerCase();
          if (clavesCamposVistas.has(claveLower)) {
            errores.push({ campo: cmp.clave, mensaje: `La clave técnica "${cmp.clave}" está duplicada. Debe ser única en todo el formulario.` });
          } else {
            clavesCamposVistas.add(claveLower);
          }
        }

        if (!cmp.etiqueta || cmp.etiqueta.trim() === '') {
          errores.push({ campo: cmp.clave || posicion, mensaje: `El campo "${cmp.clave || posicion}" requiere una etiqueta visible.` });
        }

        if (cmp.tipo === 'selector-catalogo' || cmp.tipo === 'catalogo-multiple') {
          const codigoCatalogo = cmp.codigoCatalogo?.trim();
          if (!codigoCatalogo) {
            errores.push({ campo: cmp.clave || posicion, mensaje: `El campo "${cmp.etiqueta || cmp.clave}" requiere asociar un catálogo institucional.` });
          } else if (!codigosCatalogo.has(codigoCatalogo.toLowerCase())) {
            errores.push({ campo: cmp.clave || posicion, mensaje: `El campo "${cmp.etiqueta || cmp.clave}" referencia el catálogo inexistente "${cmp.codigoCatalogo}".` });
          }
        }

        if (cmp.tipo === 'formula' && !cmp.formula?.trim() && !cmp.tipoOriginal) {
          errores.push({ campo: cmp.clave || posicion, mensaje: `El campo calculado "${cmp.etiqueta || cmp.clave}" requiere una fórmula válida.` });
        }
      });
    }
  });

  // El Builder comparte el mismo parser seguro que el preview/runtime.
  const campos = model.secciones.flatMap(sec => sec.campos) as unknown as CampoFormulario[];
  const camposMap = new Map(campos.map(campo => [campo.clave.toLowerCase(), campo]));
  const valoresIniciales: RespuestasFormulario = Object.fromEntries(campos.map(campo => [campo.clave, 0]));
  campos.filter(campo => campo.formula?.trim()).forEach(campo => {
    const resultado = evaluarFormulaCampo(campo.formula, valoresIniciales, campo.clave, camposMap);
    // La validación del contrato comprueba sintaxis, referencias y ciclos. Los
    // errores dependientes de valores no deben invalidar un borrador con ceros
    // de previsualización; se validan al evaluar una respuesta real.
    if (!resultado.exito && resultado.codigo !== 'FORMULA_DIVISION_BY_ZERO'
      && resultado.codigo !== 'FORMULA_TYPE_MISMATCH') {
      errores.push({ campo: campo.clave, mensaje: resultado.codigo ?? 'FORMULA_SYNTAX_INVALID' });
    }
  });

  return errores;
}
