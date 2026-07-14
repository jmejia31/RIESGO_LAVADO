import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../../../environments/environment';
import { Observable, map } from 'rxjs';
import {
  CONFIRMACION_BOTON_HEADER,
  CONFIRMACION_CAMBIOS_HEADER,
  CONFIRMACION_TEXTO_HEADER,
  CONFIRMACION_TITULO_HEADER
} from '../../../../core/interceptors/confirmacion-cambios.interceptor';
import {
  CoincidenciaEmpleado,
  CoincidenciaJuridica,
  CoincidenciaNatural,
  CoincidenciaPatronoDetalle,
  CoincidenciaPatronoResumen,
  DetalleCoincidenciaEmpleado,
  DetalleCoincidenciaNatural,
  EvidenciaPolitica,
  ExistingPositivo,
  RegistrarPositivoDto,
  ResumenLista,
  Seguimiento,
  TipoDocumento,
  TipoListaCautela
} from '../models/listas.models';

@Injectable({
  providedIn: 'root'
})
export class ListasService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/listas`;
  private readonly sinConfirmacion = { [CONFIRMACION_CAMBIOS_HEADER]: '1' };

  // Fachada HTTP del módulo de monitoreo de listas: centraliza consultas, evidencias,
  // seguimientos, exportaciones y auditoría para que los componentes no construyan contratos REST.

  getJuridicas(): Observable<CoincidenciaJuridica[]> {
    return this.http.get<{ success: boolean; datos: CoincidenciaJuridica[] }>(`${this.apiUrl}/juridicas`)
      .pipe(map(res => res.datos));
  }

  getNaturales(): Observable<CoincidenciaNatural[]> {
    return this.http.get<{ success: boolean; datos: CoincidenciaNatural[] }>(`${this.apiUrl}/naturales`)
      .pipe(map(res => res.datos));
  }

  getEmpleados(): Observable<CoincidenciaEmpleado[]> {
    return this.http.get<{ success: boolean; datos: CoincidenciaEmpleado[] }>(`${this.apiUrl}/empleados`)
      .pipe(map(res => res.datos));
  }

  getDetalleNatural(numeroIdentificacion: string): Observable<DetalleCoincidenciaNatural[]> {
    return this.http.get<{ success: boolean; datos: DetalleCoincidenciaNatural[] }>(`${this.apiUrl}/naturales/${numeroIdentificacion}/detalle`)
      .pipe(map(res => res.datos));
  }

  getDetalleEmpleado(numeroIdentificacion: string): Observable<DetalleCoincidenciaEmpleado[]> {
    return this.http.get<{ success: boolean; datos: DetalleCoincidenciaEmpleado[] }>(`${this.apiUrl}/empleados/${numeroIdentificacion}/detalle`)
      .pipe(map(res => res.datos));
  }

  getTiposDocumento(): Observable<TipoDocumento[]> {
    return this.http.get<{ success: boolean; datos: TipoDocumento[] }>(`${this.apiUrl}/tipos-documento`)
      .pipe(map(res => res.datos));
  }

  getTiposListasCautela(): Observable<TipoListaCautela[]> {
    return this.http.get<{ success: boolean; datos: TipoListaCautela[] }>(`${this.apiUrl}/tipos-listas-cautela`)
      .pipe(map(res => res.datos));
  }

  registrarPositivo(dto: RegistrarPositivoDto): Observable<any> {
    return this.http.post<{ success: boolean; mensaje: string }>(`${this.apiUrl}/positivos`, dto, {
      headers: {
        [CONFIRMACION_TITULO_HEADER]: 'Confirmar registro en monitoreo',
        [CONFIRMACION_TEXTO_HEADER]: '¿Desea guardar el motivo de ingreso y registrar el control en lista de positivos?',
        [CONFIRMACION_BOTON_HEADER]: 'Sí, guardar motivo'
      }
    });
  }

  getPositivoPorDocumento(noDocumento: string): Observable<ExistingPositivo | null> {
    return this.http.get<{ success: boolean; datos: ExistingPositivo | null }>(`${this.apiUrl}/positivos/${noDocumento}`)
      .pipe(map(res => res.datos));
  }

  getPoliticaEvidencias(): Observable<EvidenciaPolitica> {
    return this.http.get<{ success: boolean; datos: EvidenciaPolitica }>(`${this.apiUrl}/evidencias/politica`)
      .pipe(map(res => res.datos));
  }

  getSeguimientos(noDocumento: string, desde?: string, hasta?: string): Observable<Seguimiento[]> {
    let params = new HttpParams();
    if (desde) params = params.set('desde', desde);
    if (hasta) params = params.set('hasta', hasta);

    return this.http.get<{ success: boolean; datos: Seguimiento[] }>(`${this.apiUrl}/positivos/${noDocumento}/seguimientos`, { params })
      .pipe(map(res => res.datos));
  }

  registrarSeguimiento(noDocumento: string, motivoIngreso: string, archivos: File[]): Observable<any> {
    // Envío documental: usa FormData para conservar archivos y campos validados por backend.
    const formData = new FormData();
    formData.append('motivoIngreso', motivoIngreso);
    if (archivos && archivos.length > 0) {
      archivos.forEach(file => {
        formData.append('archivos', file, file.name);
      });
    }
    return this.http.post<{ success: boolean; mensaje: string }>(`${this.apiUrl}/positivos/${noDocumento}/seguimientos`, formData, {
      headers: {
        [CONFIRMACION_TITULO_HEADER]: 'Confirmar seguimiento',
        [CONFIRMACION_TEXTO_HEADER]: '¿Desea guardar la nota de seguimiento y sus evidencias?',
        [CONFIRMACION_BOTON_HEADER]: 'Sí, guardar seguimiento'
      }
    });
  }

  getDescargaEvidenciaUrl(evidenciaId: number): string {
    return `${environment.apiUrl}/listas/evidencias/${evidenciaId}`;
  }

  actualizarSeguimiento(detalleId: number, motivoIngreso: string, nuevosArchivos: File[]): Observable<any> {
    const formData = new FormData();
    formData.append('motivoIngreso', motivoIngreso);
    if (nuevosArchivos && nuevosArchivos.length > 0) {
      nuevosArchivos.forEach(file => {
        formData.append('archivos', file, file.name);
      });
    }
    return this.http.put<{ success: boolean; mensaje: string }>(`${this.apiUrl}/seguimientos/${detalleId}`, formData, {
      headers: {
        [CONFIRMACION_TITULO_HEADER]: 'Confirmar cambios del seguimiento',
        [CONFIRMACION_TEXTO_HEADER]: '¿Desea actualizar la nota de seguimiento y conservar el historial de auditoría?',
        [CONFIRMACION_BOTON_HEADER]: 'Sí, actualizar'
      }
    });
  }

  eliminarEvidencia(evidenciaId: number, motivoEliminacion: string): Observable<any> {
    return this.http.delete<{ success: boolean; mensaje: string }>(`${this.apiUrl}/evidencias/${evidenciaId}`, {
      body: { motivoEliminacion },
      headers: this.sinConfirmacion
    });
  }

  descargarEvidenciaBlob(evidenciaId: number): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/evidencias/${evidenciaId}`, { responseType: 'blob' });
  }

  eliminarSeguimiento(detalleId: number, motivoEliminacion: string): Observable<any> {
    return this.http.delete<{ success: boolean; mensaje: string }>(`${this.apiUrl}/seguimientos/${detalleId}`, {
      body: { motivoEliminacion },
      headers: this.sinConfirmacion
    });
  }

  registrarAuditoriaImpresion(noDocumento: string, data: any): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/positivos/${noDocumento}/reporte-impreso`, data, {
      headers: this.sinConfirmacion
    });
  }

  registrarAuditoriaExportacion(tabla: string, registroId: string, modulo: string, detalle: any): Observable<any> {
    // Auditoría explícita de exportación generada desde frontend cuando la acción no modifica datos.
    return this.http.post<any>(`${environment.apiUrl}/auditoria/exportacion`, {
      tabla,
      registroId,
      modulo,
      detalle
    }, {
      headers: this.sinConfirmacion
    });
  }

  crearTipoListaCautela(descripcion: string, tipoArchivo: string | null, cantidadColumnas: number | null): Observable<any> {
    return this.http.post<{ success: boolean; mensaje: string; datos: any }>(`${this.apiUrl}/tipos-listas-cautela`, { descripcion, tipoArchivo, cantidadColumnas });
  }

  actualizarTipoListaCautela(id: number, descripcion: string, tipoArchivo: string | null, cantidadColumnas: number | null): Observable<any> {
    return this.http.put<{ success: boolean; mensaje: string }>(`${this.apiUrl}/tipos-listas-cautela/${id}`, { descripcion, tipoArchivo, cantidadColumnas });
  }

  eliminarTipoListaCautela(id: number): Observable<any> {
    return this.http.delete<{ success: boolean; mensaje: string }>(`${this.apiUrl}/tipos-listas-cautela/${id}`);
  }

  getResumenListas(): Observable<ResumenLista[]> {
    return this.http.get<{ success: boolean; datos: ResumenLista[] }>(`${this.apiUrl}/resumen`)
      .pipe(map(res => res.datos));
  }

  exportarLista(id: number): Observable<any[]> {
    return this.http.get<{ success: boolean; datos: any[] }>(`${this.apiUrl}/${id}/exportar`)
      .pipe(map(res => res.datos));
  }

  getResumenCoincidenciasPatrono(): Observable<CoincidenciaPatronoResumen[]> {
    return this.http.get<{ success: boolean; datos: CoincidenciaPatronoResumen[] }>(`${this.apiUrl}/coincidencias-patrono/resumen`)
      .pipe(map(res => res.datos));
  }

  getDetalleCoincidenciasPatrono(fecha: string): Observable<CoincidenciaPatronoDetalle[]> {
    return this.http.get<{ success: boolean; datos: CoincidenciaPatronoDetalle[] }>(`${this.apiUrl}/coincidencias-patrono/detalle?fecha=${fecha}`)
      .pipe(map(res => res.datos));
  }

  calificarCoincidencia(id: number, tipoCalificacionId: number): Observable<any> {
    return this.http.put<any>(`${this.apiUrl}/coincidencias-patrono/${id}/calificar`, { tipoCalificacionId });
  }

  getResumenMatchLista(dataId: number, nombre: string): Observable<string> {
    return this.http.get<{ success: boolean; detalle: string }>(`${this.apiUrl}/coincidencias-patrono/resumen-match?dataId=${dataId}&nombre=${encodeURIComponent(nombre)}`)
      .pipe(map(res => res.detalle));
  }

  getResumenMatchListaEmpleado(dataId: number, nombre: string): Observable<string> {
    return this.http.get<{ success: boolean; detalle: string }>(`${this.apiUrl}/coincidencias-empleado/resumen-match?dataId=${dataId}&nombre=${encodeURIComponent(nombre)}`)
      .pipe(map(res => res.detalle));
  }

  getResumenCoincidenciasEmpleado(): Observable<CoincidenciaPatronoResumen[]> {
    return this.http.get<{ success: boolean; datos: CoincidenciaPatronoResumen[] }>(`${this.apiUrl}/coincidencias-empleado/resumen`)
      .pipe(map(res => res.datos));
  }

  getDetalleCoincidenciasEmpleado(fecha: string): Observable<CoincidenciaPatronoDetalle[]> {
    return this.http.get<{ success: boolean; datos: CoincidenciaPatronoDetalle[] }>(`${this.apiUrl}/coincidencias-empleado/detalle?fecha=${fecha}`)
      .pipe(map(res => res.datos));
  }

  calificarCoincidenciaEmpleado(id: number, tipoCalificacionId: number): Observable<any> {
    return this.http.put<any>(`${this.apiUrl}/coincidencias-empleado/${id}/calificar`, { tipoCalificacionId });
  }

  uploadListaCautela(file: File, tipoListaCautelaId: number): Observable<any> {
    const formData = new FormData();
    formData.append('archivo', file);
    formData.append('tipoListaCautelaId', tipoListaCautelaId.toString());

    return this.http.post<any>(`${this.apiUrl}/cautela/upload`, formData);
  }
}
