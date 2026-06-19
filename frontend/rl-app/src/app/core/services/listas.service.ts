import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { Observable, map } from 'rxjs';

export interface TipoDocumento {
  tipoDocumentoId: number;
  descripcion: string;
}

export interface TipoListaCautela {
  tipoListaCautelaId: number;
  descripcion: string;
  tipoArchivo?: string | null;
  cantidadColumnas?: number | null;
}

export interface ResumenLista {
  tipoListaCautelaId?: number;
  lista: string;
  usuario: string;
  fechaCreacion?: string;
  cantidadRegistros: number;
}

export interface RegistrarPositivoDto {
  tipoDocumentoId: number;
  tipoPositivoId: number;
  noDocumento: string;
  nombreCompleto: string;
  motivoIngreso: string;
  tipoListaCautelaId?: number | null;
}

export interface CoincidenciaJuridica {
  rtn: string;
  nombre: string;
  numeroPatrono: string;
  listaCoincidencia: string;
  fechaEncontro?: string;
  fechaCalifico?: string;
  esProveedorIhss?: string;
  tieneMotivo?: boolean;
  esManual?: boolean;
}

export interface CoincidenciaNatural {
  numeroIdentificacion: string;
  nombre: string;
  listaCoincidencia: string;
  totalRepetidos: number;
  tieneMotivo?: boolean;
  esManual?: boolean;
}

export interface CoincidenciaEmpleado {
  identidad: string;
  nombre: string;
  listaCoincidencia: string;
  totalRepetidos: number;
  tieneMotivo?: boolean;
  esManual?: boolean;
}

export interface DetalleCoincidenciaNatural {
  numeroIdentificacion: string;
  nombresPersona: string;
  tipoCondicionActuaDesc: string;
  numeroPatronal: string;
  nombreEmpresa: string;
  esPep: string;
  listaCoincidencia: string;
  fechaCalifico?: string;
  fechaCoincidencia?: string;
}

export interface DetalleCoincidenciaEmpleado {
  identidad: string;
  nombre: string;
  tipoCondicionActuaDesc: string;
  numeroPatrono: string;
  nombreEmpresa: string;
  razoSoci: string;
  listaCoincidencia: string;
  fechaCalifico?: string;
  fechaCoincidencia?: string;
}

@Injectable({
  providedIn: 'root'
})
export class ListasService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/listas`;

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
    return this.http.post<{ success: boolean; mensaje: string }>(`${this.apiUrl}/positivos`, dto);
  }

  getPositivoPorDocumento(noDocumento: string): Observable<{ tipoDocumentoId: number; motivoIngreso: string; tipoListaCautelaId?: number | null } | null> {
    return this.http.get<{ success: boolean; datos: { tipoDocumentoId: number; motivoIngreso: string; tipoListaCautelaId?: number | null } | null }>(`${this.apiUrl}/positivos/${noDocumento}`)
      .pipe(map(res => res.datos));
  }

  getSeguimientos(noDocumento: string): Observable<Seguimiento[]> {
    return this.http.get<{ success: boolean; datos: Seguimiento[] }>(`${this.apiUrl}/positivos/${noDocumento}/seguimientos`)
      .pipe(map(res => res.datos));
  }

  registrarSeguimiento(noDocumento: string, motivoIngreso: string, archivos: File[]): Observable<any> {
    const formData = new FormData();
    formData.append('motivoIngreso', motivoIngreso);
    if (archivos && archivos.length > 0) {
      archivos.forEach(file => {
        formData.append('archivos', file, file.name);
      });
    }
    return this.http.post<{ success: boolean; mensaje: string }>(`${this.apiUrl}/positivos/${noDocumento}/seguimientos`, formData);
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
    return this.http.put<{ success: boolean; mensaje: string }>(`${this.apiUrl}/seguimientos/${detalleId}`, formData);
  }

  eliminarEvidencia(evidenciaId: number): Observable<any> {
    return this.http.delete<{ success: boolean; mensaje: string }>(`${this.apiUrl}/evidencias/${evidenciaId}`);
  }

  descargarEvidenciaBlob(evidenciaId: number): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/evidencias/${evidenciaId}`, { responseType: 'blob' });
  }

  eliminarSeguimiento(detalleId: number): Observable<any> {
    return this.http.delete<{ success: boolean; mensaje: string }>(`${this.apiUrl}/seguimientos/${detalleId}`);
  }

  registrarAuditoriaImpresion(noDocumento: string, data: any): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/positivos/${noDocumento}/reporte-impreso`, data);
  }

  registrarAuditoriaExportacion(tabla: string, registroId: string, modulo: string, detalle: any): Observable<any> {
    return this.http.post<any>(`${environment.apiUrl}/auditoria/exportacion`, {
      tabla,
      registroId,
      modulo,
      detalle
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

export interface CoincidenciaPatronoResumen {
  fechaEncontro: string;
  cantidadRegistros: number;
}
export type CoincidenciaEmpleadoResumen = CoincidenciaPatronoResumen;

export interface CoincidenciaPatronoDetalle {
  reporteCoincidenciaId: number;
  dataId: number;
  dni: string;
  fechaEncontro: string;
  listaCoincidencia: string;
  nacionalidad: string;
  nombre: string;
  numeroPatrono: string;
  observacionLista: string;
  tipoPersona: string;
  usuarioEncontro: number;
  tipoCalificacion: string;
}
export type CoincidenciaEmpleadoDetalle = CoincidenciaPatronoDetalle;

export interface Evidencia {
  evidenciaId: number;
  nombreArchivo: string;
  tipoMime: string;
}

export interface Seguimiento {
  detalleListaId: number;
  positivoId: number;
  motivoIngreso: string;
  fechaCreacion: string;
  usrCreacionId: number;
  usrEmail: string;
  evidencias: Evidencia[];
}
