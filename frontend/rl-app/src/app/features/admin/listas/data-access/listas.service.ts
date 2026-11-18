import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../../../environments/environment';
import { Observable, map, of } from 'rxjs';
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
  TipoListaCautela,
  ConsultaMonitoreoPaginada,
  ConsultaCoincidenciasPaginada,
  Paginado,
  MonitoreoPaginado
} from '../models/listas.models';
import { createHttpErrorScopeContext, HttpErrorScope } from '../../../../core/http/http-error-scope';

@Injectable({
  providedIn: 'root'
})
export class ListasService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/listas`;
  private readonly sinConfirmacion = { [CONFIRMACION_CAMBIOS_HEADER]: '1' };
  private readonly marcadoresSinIdentificacion = new Set([
    'N/A',
    'N/A(3)',
    'NULL',
    'VACIO',
    'VACÍO',
    'SIN IDENTIFICACION',
    'SIN IDENTIFICACIÓN'
  ]);

  // Fachada HTTP del módulo de monitoreo de listas: centraliza consultas, evidencias,
  // seguimientos, exportaciones y auditoría para que los componentes no construyan contratos REST.

  getJuridicasPaginadas(consulta: ConsultaMonitoreoPaginada): Observable<MonitoreoPaginado<CoincidenciaJuridica>> {
    return this.http.get<{ success: boolean; datos: MonitoreoPaginado<CoincidenciaJuridica> }>(`${this.apiUrl}/juridicas/paginado`, { params: this.toMonitoringParams(consulta) })
      .pipe(map(res => res.datos));
  }

  getJuridicasParaExportar(consulta: ConsultaMonitoreoPaginada): Observable<CoincidenciaJuridica[]> {
    return this.http.get<{ success: boolean; datos: CoincidenciaJuridica[] }>(`${this.apiUrl}/juridicas/exportar`, { params: this.toMonitoringParams(consulta), context: createHttpErrorScopeContext('local') })
      .pipe(map(res => res.datos));
  }

  getNaturalesPaginadas(consulta: ConsultaMonitoreoPaginada): Observable<MonitoreoPaginado<CoincidenciaNatural>> {
    return this.http.get<{ success: boolean; datos: MonitoreoPaginado<CoincidenciaNatural> }>(`${this.apiUrl}/naturales/paginado`, { params: this.toMonitoringParams(consulta) })
      .pipe(map(res => res.datos));
  }

  getNaturalesParaExportar(consulta: ConsultaMonitoreoPaginada): Observable<CoincidenciaNatural[]> {
    return this.http.get<{ success: boolean; datos: CoincidenciaNatural[] }>(`${this.apiUrl}/naturales/exportar`, { params: this.toMonitoringParams(consulta), context: createHttpErrorScopeContext('local') })
      .pipe(map(res => res.datos));
  }

  getEmpleadosPaginadas(consulta: ConsultaMonitoreoPaginada): Observable<MonitoreoPaginado<CoincidenciaEmpleado>> {
    return this.http.get<{ success: boolean; datos: MonitoreoPaginado<CoincidenciaEmpleado> }>(`${this.apiUrl}/empleados/paginado`, { params: this.toMonitoringParams(consulta) })
      .pipe(map(res => res.datos));
  }

  getEmpleadosParaExportar(consulta: ConsultaMonitoreoPaginada): Observable<CoincidenciaEmpleado[]> {
    return this.http.get<{ success: boolean; datos: CoincidenciaEmpleado[] }>(`${this.apiUrl}/empleados/exportar`, { params: this.toMonitoringParams(consulta), context: createHttpErrorScopeContext('local') })
      .pipe(map(res => res.datos));
  }

  getResumenCoincidenciasPatronoPaginado(consulta: ConsultaCoincidenciasPaginada): Observable<Paginado<CoincidenciaPatronoResumen>> {
    return this.http.get<{ success: boolean; datos: Paginado<CoincidenciaPatronoResumen> }>(`${this.apiUrl}/coincidencias-patrono/resumen/paginado`, { params: this.toCoincidenciasParams(consulta) , context: createHttpErrorScopeContext('local')}).pipe(map(res => res.datos));
  }

  getDetalleCoincidenciasPatronoPaginado(consulta: ConsultaCoincidenciasPaginada): Observable<Paginado<CoincidenciaPatronoDetalle>> {
    return this.http.get<{ success: boolean; datos: Paginado<CoincidenciaPatronoDetalle> }>(`${this.apiUrl}/coincidencias-patrono/detalle/paginado`, { params: this.toCoincidenciasParams(consulta) , context: createHttpErrorScopeContext('local')}).pipe(map(res => res.datos));
  }

  getResumenCoincidenciasEmpleadoPaginado(consulta: ConsultaCoincidenciasPaginada): Observable<Paginado<CoincidenciaPatronoResumen>> {
    return this.http.get<{ success: boolean; datos: Paginado<CoincidenciaPatronoResumen> }>(`${this.apiUrl}/coincidencias-empleado/resumen/paginado`, { params: this.toCoincidenciasParams(consulta) , context: createHttpErrorScopeContext('local')}).pipe(map(res => res.datos));
  }

  getDetalleCoincidenciasEmpleadoPaginado(consulta: ConsultaCoincidenciasPaginada): Observable<Paginado<CoincidenciaPatronoDetalle>> {
    return this.http.get<{ success: boolean; datos: Paginado<CoincidenciaPatronoDetalle> }>(`${this.apiUrl}/coincidencias-empleado/detalle/paginado`, { params: this.toCoincidenciasParams(consulta) , context: createHttpErrorScopeContext('local')}).pipe(map(res => res.datos));
  }

  private toMonitoringParams(consulta: ConsultaMonitoreoPaginada): HttpParams {
    let params = new HttpParams().set('pagina', consulta.pagina).set('tamanoPagina', consulta.tamanoPagina);
    if (consulta.buscar) params = params.set('buscar', consulta.buscar);
    if (consulta.estado) params = params.set('estado', consulta.estado);
    if (consulta.fechaDesde) params = params.set('fechaDesde', consulta.fechaDesde);
    if (consulta.fechaHasta) params = params.set('fechaHasta', consulta.fechaHasta);
    if (consulta.categoriaIdentidad) params = params.set('categoriaIdentidad', consulta.categoriaIdentidad);
    if (consulta.periodoPlanilla) params = params.set('periodoPlanilla', consulta.periodoPlanilla);
    if (consulta.periodoPlanillaDesde) params = params.set('periodoPlanillaDesde', consulta.periodoPlanillaDesde);
    if (consulta.periodoPlanillaHasta) params = params.set('periodoPlanillaHasta', consulta.periodoPlanillaHasta);
    if (consulta.orden) params = params.set('orden', consulta.orden);
    if (consulta.direccion) params = params.set('direccion', consulta.direccion);
    return params;
  }

  private toCoincidenciasParams(consulta: ConsultaCoincidenciasPaginada): HttpParams {
    let params = new HttpParams().set('pagina', consulta.pagina).set('tamanoPagina', consulta.tamanoPagina);
    if (consulta.buscar) params = params.set('buscar', consulta.buscar);
    if (consulta.calificacion) params = params.set('calificacion', consulta.calificacion);
    if (consulta.fecha) params = params.set('fecha', consulta.fecha);
    if (consulta.orden) params = params.set('orden', consulta.orden);
    if (consulta.direccion) params = params.set('direccion', consulta.direccion);
    return params;
  }

  // M1: el ID de coincidencia/positivo es la referencia de lectura.
  // Las rutas por DNI se conservan solo como compatibilidad; una identidad
  // ambigua sin ID es rechazada por el backend.
  getDetalleNatural(
    numeroIdentificacion: string,
    errorScope: HttpErrorScope = 'global',
    idRegistroFuente?: number,
    tipoFuente?: 'DNP_COINCIDENCIA' | 'MANUAL_CUMPLIMIENTO' | 'POSITIVO_DNP'
  ): Observable<DetalleCoincidenciaNatural[]> {
    const context = createHttpErrorScopeContext(errorScope);
    if (idRegistroFuente && tipoFuente) {
      const params = new HttpParams()
        .set('numeroIdentificacion', numeroIdentificacion)
        .set('idRegistroFuente', idRegistroFuente)
        .set('tipoFuente', tipoFuente);
      return this.http.get<{ success: boolean; datos: DetalleCoincidenciaNatural[] }>(
        `${this.apiUrl}/naturales/detalle`, { params, context }).pipe(map(res => res.datos));
    }
    if (this.esMarcadorSinIdentificacion(numeroIdentificacion)) {
      const params = new HttpParams().set('numeroIdentificacion', numeroIdentificacion);
      return this.http.get<{ success: boolean; datos: DetalleCoincidenciaNatural[] }>(
        `${this.apiUrl}/naturales/detalle`, { params, context }).pipe(map(res => res.datos));
    }
    return this.http.get<{ success: boolean; datos: DetalleCoincidenciaNatural[] }>(
      `${this.apiUrl}/naturales/${encodeURIComponent(numeroIdentificacion)}/detalle`, { context })
      .pipe(map(res => res.datos));
  }

  getDetalleEmpleado(
    numeroIdentificacion: string,
    errorScope: HttpErrorScope = 'global',
    idRegistroFuente?: number,
    tipoFuente?: 'DNP_COINCIDENCIA' | 'MANUAL_CUMPLIMIENTO' | 'POSITIVO_DNP',
    periodoPlanilla?: string
  ): Observable<DetalleCoincidenciaEmpleado[]> {
    const context = createHttpErrorScopeContext(errorScope);
    if (idRegistroFuente && tipoFuente) {
      let params = new HttpParams()
        .set('numeroIdentificacion', numeroIdentificacion)
        .set('idRegistroFuente', idRegistroFuente)
        .set('tipoFuente', tipoFuente);
      if (periodoPlanilla) params = params.set('periodoPlanilla', periodoPlanilla);
      return this.http.get<{ success: boolean; datos: DetalleCoincidenciaEmpleado[] }>(
        `${this.apiUrl}/empleados/detalle`, { params, context }).pipe(map(res => res.datos));
    }
    if (this.esMarcadorSinIdentificacion(numeroIdentificacion)) {
      const params = new HttpParams().set('numeroIdentificacion', numeroIdentificacion);
      return this.http.get<{ success: boolean; datos: DetalleCoincidenciaEmpleado[] }>(
        `${this.apiUrl}/empleados/detalle`, { params, context }).pipe(map(res => res.datos));
    }
    return this.http.get<{ success: boolean; datos: DetalleCoincidenciaEmpleado[] }>(
      `${this.apiUrl}/empleados/${encodeURIComponent(numeroIdentificacion)}/detalle`, { context })
      .pipe(map(res => res.datos));
  }

  getTiposDocumento(): Observable<TipoDocumento[]> {
    return this.http.get<{ success: boolean; datos: TipoDocumento[] }>(`${this.apiUrl}/tipos-documento`)
      .pipe(map(res => res.datos));
  }

  getTiposListasCautela(errorScope: HttpErrorScope = 'global'): Observable<TipoListaCautela[]> {
    return this.http.get<{ success: boolean; datos: TipoListaCautela[] }>(`${this.apiUrl}/tipos-listas-cautela`, { context: createHttpErrorScopeContext(errorScope) })
      .pipe(map(res => res.datos));
  }

  registrarPositivo(dto: RegistrarPositivoDto): Observable<any> {
    return this.http.post<{ success: boolean; mensaje: string }>(`${this.apiUrl}/positivos`, dto, {
      headers: {
        [CONFIRMACION_TITULO_HEADER]: 'Confirmar registro en monitoreo',
        [CONFIRMACION_TEXTO_HEADER]: '¿Desea guardar el motivo de ingreso y registrar el control en lista de positivos?',
        [CONFIRMACION_BOTON_HEADER]: 'Sí, guardar motivo'
      }
    , context: createHttpErrorScopeContext('local')});
  }

  getPositivoPorDocumento(noDocumento: string): Observable<ExistingPositivo | null> {
    if (this.esMarcadorSinIdentificacion(noDocumento)) return of(null);

    return this.http.get<{ success: boolean; datos: ExistingPositivo | null }>(`${this.apiUrl}/positivos/${encodeURIComponent(noDocumento)}`)
      .pipe(map(res => res.datos));
  }

  getPoliticaEvidencias(): Observable<EvidenciaPolitica> {
    return this.http.get<{ success: boolean; datos: EvidenciaPolitica }>(`${this.apiUrl}/evidencias/politica`)
      .pipe(map(res => res.datos));
  }

  getSeguimientos(noDocumento: string, desde?: string, hasta?: string): Observable<Seguimiento[]> {
    if (this.esMarcadorSinIdentificacion(noDocumento)) return of([]);

    let params = new HttpParams();
    if (desde) params = params.set('desde', desde);
    if (hasta) params = params.set('hasta', hasta);

    return this.http.get<{ success: boolean; datos: Seguimiento[] }>(`${this.apiUrl}/positivos/${encodeURIComponent(noDocumento)}/seguimientos`, { params })
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
    return this.http.post<{ success: boolean; mensaje: string }>(`${this.apiUrl}/positivos/${encodeURIComponent(noDocumento)}/seguimientos`, formData, {
      headers: {
        [CONFIRMACION_TITULO_HEADER]: 'Confirmar seguimiento',
        [CONFIRMACION_TEXTO_HEADER]: '¿Desea guardar la nota de seguimiento y sus evidencias?',
        [CONFIRMACION_BOTON_HEADER]: 'Sí, guardar seguimiento'
      }
    , context: createHttpErrorScopeContext('local')});
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
      },
      context: createHttpErrorScopeContext('local')
    });
  }

  eliminarEvidencia(evidenciaId: number, motivoEliminacion: string): Observable<any> {
    return this.http.delete<{ success: boolean; mensaje: string }>(`${this.apiUrl}/evidencias/${evidenciaId}`, {
      body: { motivoEliminacion },
      headers: this.sinConfirmacion
    , context: createHttpErrorScopeContext('local')});
  }

  descargarEvidenciaBlob(evidenciaId: number): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/evidencias/${evidenciaId}`, { responseType: 'blob' , context: createHttpErrorScopeContext('local')});
  }

  eliminarSeguimiento(detalleId: number, motivoEliminacion: string): Observable<any> {
    return this.http.delete<{ success: boolean; mensaje: string }>(`${this.apiUrl}/seguimientos/${detalleId}`, {
      body: { motivoEliminacion },
      headers: this.sinConfirmacion
    , context: createHttpErrorScopeContext('local')});
  }

  registrarAuditoriaImpresion(noDocumento: string, data: any): Observable<any> {
    const options = {
      headers: this.sinConfirmacion
    , context: createHttpErrorScopeContext('local')};

    if (this.esMarcadorSinIdentificacion(noDocumento)) {
      const params = new HttpParams().set('noDocumento', noDocumento);
      return this.http.post<any>(`${this.apiUrl}/reporte-impreso`, data, { ...options, params });
    }

    return this.http.post<any>(`${this.apiUrl}/positivos/${encodeURIComponent(noDocumento)}/reporte-impreso`, data, options);
  }

  private esMarcadorSinIdentificacion(value: string | null | undefined): boolean {
    const normalizado = (value ?? '').trim().toUpperCase();
    return normalizado.startsWith('N/A') || this.marcadoresSinIdentificacion.has(normalizado);
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
    , context: createHttpErrorScopeContext('local')});
  }

  crearTipoListaCautela(descripcion: string, tipoArchivo: string | null, cantidadColumnas: number | null): Observable<any> {
    return this.http.post<{ success: boolean; mensaje: string; datos: any }>(`${this.apiUrl}/tipos-listas-cautela`, { descripcion, tipoArchivo, cantidadColumnas }, { context: createHttpErrorScopeContext('local') });
  }

  actualizarTipoListaCautela(id: number, descripcion: string, tipoArchivo: string | null, cantidadColumnas: number | null): Observable<any> {
    return this.http.put<{ success: boolean; mensaje: string }>(`${this.apiUrl}/tipos-listas-cautela/${id}`, { descripcion, tipoArchivo, cantidadColumnas }, { context: createHttpErrorScopeContext('local') });
  }

  eliminarTipoListaCautela(id: number): Observable<any> {
    return this.http.delete<{ success: boolean; mensaje: string }>(`${this.apiUrl}/tipos-listas-cautela/${id}`, { context: createHttpErrorScopeContext('local') });
  }

  getResumenListas(): Observable<ResumenLista[]> {
    return this.http.get<{ success: boolean; datos: ResumenLista[] }>(`${this.apiUrl}/resumen`, { context: createHttpErrorScopeContext('local') })
      .pipe(map(res => res.datos));
  }

  exportarLista(id: number): Observable<any[]> {
    return this.http.get<{ success: boolean; datos: any[] }>(`${this.apiUrl}/${id}/exportar`, { context: createHttpErrorScopeContext('local') })
      .pipe(map(res => res.datos));
  }

  getDetalleCoincidenciasPatronoParaExportar(fecha: string): Observable<CoincidenciaPatronoDetalle[]> {
    return this.http.get<{ success: boolean; datos: CoincidenciaPatronoDetalle[] }>(`${this.apiUrl}/coincidencias-patrono/detalle/exportar?fecha=${fecha}`, { context: createHttpErrorScopeContext('local') })
      .pipe(map(res => res.datos));
  }

  calificarCoincidencia(id: number, tipoCalificacionId: number): Observable<any> {
    return this.http.put<any>(`${this.apiUrl}/coincidencias-patrono/${id}/calificar`, { tipoCalificacionId }, { context: createHttpErrorScopeContext('local') });
  }

  getResumenMatchLista(dataId: number, nombre: string): Observable<string> {
    return this.http.get<{ success: boolean; detalle: string }>(`${this.apiUrl}/coincidencias-patrono/resumen-match?dataId=${dataId}&nombre=${encodeURIComponent(nombre)}`, { context: createHttpErrorScopeContext('local') })
      .pipe(map(res => res.detalle));
  }

  getResumenMatchListaEmpleado(dataId: number, nombre: string): Observable<string> {
    return this.http.get<{ success: boolean; detalle: string }>(`${this.apiUrl}/coincidencias-empleado/resumen-match?dataId=${dataId}&nombre=${encodeURIComponent(nombre)}`, { context: createHttpErrorScopeContext('local') })
      .pipe(map(res => res.detalle));
  }

  getDetalleCoincidenciasEmpleadoParaExportar(fecha: string): Observable<CoincidenciaPatronoDetalle[]> {
    return this.http.get<{ success: boolean; datos: CoincidenciaPatronoDetalle[] }>(`${this.apiUrl}/coincidencias-empleado/detalle/exportar?fecha=${fecha}`, { context: createHttpErrorScopeContext('local') })
      .pipe(map(res => res.datos));
  }

  calificarCoincidenciaEmpleado(id: number, tipoCalificacionId: number): Observable<any> {
    return this.http.put<any>(`${this.apiUrl}/coincidencias-empleado/${id}/calificar`, { tipoCalificacionId }, { context: createHttpErrorScopeContext('local') });
  }

  uploadListaCautela(file: File, tipoListaCautelaId: number): Observable<any> {
    const formData = new FormData();
    formData.append('archivo', file);
    formData.append('tipoListaCautelaId', tipoListaCautelaId.toString());

    return this.http.post<any>(`${this.apiUrl}/cautela/upload`, formData, { context: createHttpErrorScopeContext('local') });
  }
}
