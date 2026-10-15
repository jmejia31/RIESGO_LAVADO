import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { from, switchMap, throwError } from 'rxjs';
import { InstitutionalConfirmationService } from '../../shared/confirmation/institutional-confirmation.service';

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
  const confirmation = inject(InstitutionalConfirmationService);

  return from(confirmation.confirm({
      title: accion.titulo,
      message: accion.texto,
      confirmLabel: accion.confirmar,
      cancelLabel: 'Cancelar',
      confirmIcon: req.method.toUpperCase() === 'DELETE' ? 'delete' : 'save',
      confirmVariant: req.method.toUpperCase() === 'DELETE' ? 'danger' : 'primary',
      icon: accion.icono
    })).pipe(
    switchMap(confirmado => {
      if (confirmado) {
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
