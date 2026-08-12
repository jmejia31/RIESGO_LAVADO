import { FormBuilderModel } from '../models/form-builder.models';

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

  const clavesCamposVistas = new Set<string>();

  model.secciones.forEach((sec, secIdx) => {
    if (!sec.titulo || sec.titulo.trim() === '') {
      errores.push({
        campo: `Sección ${secIdx + 1}`,
        mensaje: `La sección ${secIdx + 1} requiere un título descriptivo.`
      });
    }

    if (!sec.campos || sec.campos.length === 0) {
      errores.push({
        campo: sec.titulo || `Sección ${secIdx + 1}`,
        mensaje: `La sección "${sec.titulo || secIdx + 1}" debe tener al menos un campo configurado.`
      });
    } else {
      sec.campos.forEach((cmp, cmpIdx) => {
        const posicion = `Sección ${secIdx + 1}, Campo ${cmpIdx + 1}`;

        if (!cmp.clave || cmp.clave.trim() === '') {
          errores.push({
            campo: posicion,
            mensaje: 'La clave técnica del campo es obligatoria.'
          });
        } else {
          const claveLower = cmp.clave.trim().toLowerCase();
          if (clavesCamposVistas.has(claveLower)) {
            errores.push({
              campo: cmp.clave,
              mensaje: `La clave técnica "${cmp.clave}" está duplicada. Debe ser única en todo el formulario.`
            });
          } else {
            clavesCamposVistas.add(claveLower);
          }
        }

        if (!cmp.etiqueta || cmp.etiqueta.trim() === '') {
          errores.push({
            campo: cmp.clave || posicion,
            mensaje: `El campo "${cmp.clave || posicion}" requiere una etiqueta visible.`
          });
        }

        if (cmp.tipo === 'selector-catalogo' || cmp.tipo === 'catalogo-multiple') {
          if (!cmp.codigoCatalogo || cmp.codigoCatalogo.trim() === '') {
            errores.push({
              campo: cmp.clave || posicion,
              mensaje: `El campo "${cmp.etiqueta || cmp.clave}" requiere asociar un catálogo institucional.`
            });
          }
        }

        if (cmp.tipo === 'formula') {
          if (!cmp.formula || cmp.formula.trim() === '') {
            errores.push({
              campo: cmp.clave || posicion,
              mensaje: `El campo calculado "${cmp.etiqueta || cmp.clave}" requiere una fórmula válida.`
            });
          }
        }
      });
    }
  });

  return errores;
}
