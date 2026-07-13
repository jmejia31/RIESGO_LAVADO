import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { from, switchMap, throwError } from 'rxjs';

const HEADER_CONFIRMADO = 'X-RL-Confirmado';
const HEADER_TITULO = 'X-RL-Confirmacion-Titulo';
const HEADER_TEXTO = 'X-RL-Confirmacion-Texto';
const HEADER_CONFIRMAR = 'X-RL-Confirmacion-Boton';
const HEADER_ICONO = 'X-RL-Confirmacion-Icono';

const metodosConCambio = new Set(['POST', 'PUT', 'PATCH', 'DELETE']);

const rutasExcluidas = [
  '/auth/login',
  '/auth/refresh',
  '/auth/logout'
];

function requiereConfirmacion(url: string, method: string): boolean {
  if (!metodosConCambio.has(method.toUpperCase())) return false;
  const normalizada = url.toLowerCase();
  return !rutasExcluidas.some(ruta => normalizada.includes(ruta));
}

function textoAccion(method: string): { titulo: string; texto: string; confirmar: string; icono: 'question' | 'warning' } {
  switch (method.toUpperCase()) {
    case 'DELETE':
      return {
        titulo: 'Confirmar eliminación',
        texto: '¿Desea eliminar este registro? Esta acción debe estar autorizada.',
        confirmar: 'Sí, eliminar',
        icono: 'warning'
      };
    case 'PUT':
    case 'PATCH':
      return {
        titulo: 'Confirmar actualización',
        texto: '¿Desea guardar los cambios realizados?',
        confirmar: 'Sí, guardar',
        icono: 'question'
      };
    default:
      return {
        titulo: 'Confirmar guardado',
        texto: '¿Desea guardar este registro?',
        confirmar: 'Sí, guardar',
        icono: 'question'
      };
  }
}

function limpiarCabecerasInternas(req: Parameters<HttpInterceptorFn>[0]) {
  return req.clone({
    headers: req.headers
      .delete(HEADER_CONFIRMADO)
      .delete(HEADER_TITULO)
      .delete(HEADER_TEXTO)
      .delete(HEADER_CONFIRMAR)
      .delete(HEADER_ICONO)
  });
}

export const confirmacionCambiosInterceptor: HttpInterceptorFn = (req, next) => {
  if (!requiereConfirmacion(req.url, req.method)) {
    return next(req);
  }

  const yaConfirmado = req.headers.get(HEADER_CONFIRMADO) === '1';
  const reqSinCabeceraInterna = limpiarCabecerasInternas(req);

  if (yaConfirmado) {
    return next(reqSinCabeceraInterna);
  }

  const accionBase = textoAccion(req.method);
  const iconoCabecera = req.headers.get(HEADER_ICONO);
  const accion = {
    titulo: req.headers.get(HEADER_TITULO) || accionBase.titulo,
    texto: req.headers.get(HEADER_TEXTO) || accionBase.texto,
    confirmar: req.headers.get(HEADER_CONFIRMAR) || accionBase.confirmar,
    icono: iconoCabecera === 'warning' ? 'warning' as const : accionBase.icono
  };

  return from(import('sweetalert2')).pipe(
    switchMap(Swal => from(Swal.default.fire({
      allowOutsideClick: false,
      allowEscapeKey: true,
      title: accion.titulo,
      text: accion.texto,
      icon: accion.icono,
      showCancelButton: true,
      showDenyButton: true,
      confirmButtonText: accion.confirmar,
      denyButtonText: 'No',
      cancelButtonText: 'Cancelar',
      confirmButtonColor: req.method.toUpperCase() === 'DELETE' ? '#dc2626' : '#0f766e',
      denyButtonColor: '#64748b',
      cancelButtonColor: '#d1d5db',
      reverseButtons: true
    }))),
    switchMap(result => {
      if (result.isConfirmed) {
        return next(reqSinCabeceraInterna);
      }

      return throwError(() => new HttpErrorResponse({
        status: 499,
        statusText: 'Operacion cancelada',
        url: req.url,
        error: { mensaje: 'Operación cancelada por el usuario.' }
      }));
    })
  );
};

export const CONFIRMACION_CAMBIOS_HEADER = HEADER_CONFIRMADO;
export const CONFIRMACION_TITULO_HEADER = HEADER_TITULO;
export const CONFIRMACION_TEXTO_HEADER = HEADER_TEXTO;
export const CONFIRMACION_BOTON_HEADER = HEADER_CONFIRMAR;
export const CONFIRMACION_ICONO_HEADER = HEADER_ICONO;
