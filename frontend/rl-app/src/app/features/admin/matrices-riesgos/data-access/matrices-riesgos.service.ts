import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { environment } from '../../../../../environments/environment';
import { CONFIRMACION_CAMBIOS_HEADER } from '../../../../core/interceptors/confirmacion-cambios.interceptor';
import {
  MatricesRiesgoReporte,
  MatrizRiesgoCrearRequest,
  MatrizRiesgoCriterio,
  MatrizRiesgoCriterioRequest,
  MatrizRiesgoDashboard,
  MatrizRiesgoDetalle,
  MatrizRiesgoFiltro,
  MatrizRiesgoHistorial,
  MatrizRiesgoReporteFiltro,
  MatrizRiesgoResumen,
  MetodologiaMatrices
} from '../models/matrices-riesgos.models';

type ApiResponse<T> = { success: boolean; datos: T; mensaje?: string };
type ApiMessage = { success: boolean; mensaje?: string };

@Injectable({ providedIn: 'root' })
export class MatricesRiesgosService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/matrices-riesgos`;
  private readonly confirmado = {
    headers: new HttpHeaders({ [CONFIRMACION_CAMBIOS_HEADER]: '1' })
  };

  metodologiaVigente(): Observable<MetodologiaMatrices> {
    return this.http.get<ApiResponse<MetodologiaMatrices>>(`${this.apiUrl}/metodologia/vigente`)
      .pipe(map(res => res.datos));
  }

  dashboard(): Observable<MatrizRiesgoDashboard> {
    return this.http.get<ApiResponse<MatrizRiesgoDashboard>>(`${this.apiUrl}/dashboard`)
      .pipe(map(res => res.datos));
  }

  reporte(filtro: MatrizRiesgoReporteFiltro): Observable<MatricesRiesgoReporte> {
    return this.http.get<ApiResponse<MatricesRiesgoReporte>>(`${this.apiUrl}/reportes`, { params: this.construirParams(filtro) })
      .pipe(map(res => res.datos));
  }

  exportarReporte(filtro: MatrizRiesgoReporteFiltro, formato: 'EXCEL' | 'PDF'): Observable<Blob> {
    const params = this.construirParams({ ...filtro, formato });
    return this.http.get(`${this.apiUrl}/reportes/exportar`, {
      params,
      responseType: 'blob',
      headers: this.confirmado.headers
    });
  }

  listar(filtro: MatrizRiesgoFiltro): Observable<MatrizRiesgoResumen[]> {
    const params = this.construirParams(filtro);
    return this.http.get<ApiResponse<MatrizRiesgoResumen[]>>(this.apiUrl, { params })
      .pipe(map(res => res.datos));
  }

  obtener(id: number): Observable<MatrizRiesgoDetalle> {
    return this.http.get<ApiResponse<MatrizRiesgoDetalle>>(`${this.apiUrl}/${id}`)
      .pipe(map(res => res.datos));
  }

  crear(dto: MatrizRiesgoCrearRequest): Observable<MatrizRiesgoDetalle> {
    return this.http.post<ApiResponse<MatrizRiesgoDetalle>>(this.apiUrl, dto)
      .pipe(map(res => res.datos));
  }

  actualizar(id: number, dto: MatrizRiesgoCrearRequest): Observable<MatrizRiesgoDetalle> {
    return this.http.put<ApiResponse<MatrizRiesgoDetalle>>(`${this.apiUrl}/${id}`, dto, this.confirmado)
      .pipe(map(res => res.datos));
  }

  calcular(id: number, tipoCalculo = 'GLOBAL'): Observable<unknown> {
    return this.http.post<ApiResponse<unknown>>(`${this.apiUrl}/${id}/calcular`, { tipoCalculo }, this.confirmado)
      .pipe(map(res => res.datos));
  }

  recalcular(id: number, motivoCalculo: string, tipoCalculo = 'GLOBAL'): Observable<unknown> {
    return this.http.post<ApiResponse<unknown>>(`${this.apiUrl}/${id}/recalcular`, { tipoCalculo, motivoCalculo }, this.confirmado)
      .pipe(map(res => res.datos));
  }

  cambiarEstado(id: number, estado: string, motivo: string): Observable<ApiMessage> {
    return this.http.put<ApiMessage>(`${this.apiUrl}/${id}/estado`, { estado, motivo }, this.confirmado);
  }

  eliminarMatriz(id: number, motivo: string): Observable<ApiMessage> {
    return this.http.put<ApiMessage>(`${this.apiUrl}/${id}/eliminar`, { motivo }, this.confirmado);
  }

  historial(id: number): Observable<MatrizRiesgoHistorial[]> {
    return this.http.get<ApiResponse<MatrizRiesgoHistorial[]>>(`${this.apiUrl}/${id}/historial`)
      .pipe(map(res => res.datos));
  }

  listarCriterios(incluirInactivos = false): Observable<MatrizRiesgoCriterio[]> {
    const params = new HttpParams().set('incluirInactivos', incluirInactivos);
    return this.http.get<ApiResponse<MatrizRiesgoCriterio[]>>(`${this.apiUrl}/criterios`, { params })
      .pipe(map(res => res.datos));
  }

  crearCriterio(dto: MatrizRiesgoCriterioRequest): Observable<MatrizRiesgoCriterio> {
    return this.http.post<ApiResponse<MatrizRiesgoCriterio>>(`${this.apiUrl}/criterios`, dto)
      .pipe(map(res => res.datos));
  }

  actualizarCriterio(id: number, dto: MatrizRiesgoCriterioRequest): Observable<MatrizRiesgoCriterio> {
    return this.http.put<ApiResponse<MatrizRiesgoCriterio>>(`${this.apiUrl}/criterios/${id}`, dto)
      .pipe(map(res => res.datos));
  }

  inactivarCriterio(id: number, motivo: string): Observable<ApiMessage> {
    return this.http.put<ApiMessage>(`${this.apiUrl}/criterios/${id}/inactivar`, { motivo }, this.confirmado);
  }

  eliminarCriterio(id: number, motivo: string): Observable<ApiMessage> {
    return this.http.put<ApiMessage>(`${this.apiUrl}/criterios/${id}/eliminar`, { motivo }, this.confirmado);
  }

  private construirParams(filtro: object): HttpParams {
    let params = new HttpParams();
    Object.entries(filtro).forEach(([key, value]) => {
      if (value !== undefined && value !== null && `${value}`.trim() !== '') {
        params = params.set(key, `${value}`);
      }
    });
    return params;
  }
}
