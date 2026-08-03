import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { environment } from '../../../../../environments/environment';
import { CONFIRMACION_CAMBIOS_HEADER } from '../../../../core/interceptors/confirmacion-cambios.interceptor';
import {
  AsociarEvidenciaActividadDto,
  AsociarEvidenciaAlertaDto,
  AsociarEvidenciaAprobacionDto,
  AsociarEvidenciaAutomonitoreoDto,
  AsociarEvidenciaControlDto,
  AsociarEvidenciaEvaluacionDto,
  AsociarEvidenciaPlanDto,
  AsociarEvidenciaRevisionDto,
  AsociarEvidenciaRiesgoDto,
  ConsultaEvaluacionPaginadaDto,
  EvidenciaDto,
  EvidenciaPoliticaDto,
  EvaluacionRiesgoDto,
  MetodologiaFormulario,
  RevisionEvaluacionDto,
  RiesgoReporteFila,
  VersionFormularioDto
} from '../models/matrices-riesgos.models';

export type ApiResponse<T> = { success: boolean; datos: T; mensaje?: string };
export type ApiMessage = { success: boolean; mensaje?: string; datos?: number };

@Injectable({ providedIn: 'root' })
export class MatricesRiesgosService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/matrices-riesgos`;
  private readonly confirmado = {
    headers: new HttpHeaders({ [CONFIRMACION_CAMBIOS_HEADER]: '1' })
  };

  metodologiaVigente(): Observable<MetodologiaFormulario> {
    return this.http
      .get<ApiResponse<MetodologiaFormulario>>(`${this.apiUrl}/metodologia/vigente`)
      .pipe(map(response => response.datos));
  }

  obtenerConsolidado(): Observable<RiesgoReporteFila[]> {
    return this.http
      .get<ApiResponse<RiesgoReporteFila[]>>(`${this.apiUrl}/consolidado`)
      .pipe(map(response => response.datos));
  }

  obtenerVersionVigenteFormulario(
    familiaCodigo = 'MATRIZ_RIESGOS_LAFT'
  ): Observable<VersionFormularioDto> {
    const params = new HttpParams().set('familiaCodigo', familiaCodigo);
    return this.http
      .get<ApiResponse<VersionFormularioDto>>(`${this.apiUrl}/formulario/version-vigente`, { params })
      .pipe(map(response => response.datos));
  }

  listarHistorialVersionesFormulario(
    familiaCodigo = 'MATRIZ_RIESGOS_LAFT'
  ): Observable<VersionFormularioDto[]> {
    const params = new HttpParams().set('familiaCodigo', familiaCodigo);
    return this.http
      .get<ApiResponse<VersionFormularioDto[]>>(`${this.apiUrl}/formularios/historial`, { params })
      .pipe(map(response => response.datos));
  }

  crearBorradorFormulario(
    familiaId: number,
    codigoFormulario: string,
    definicion: string
  ): Observable<number> {
    const params = new HttpParams()
      .set('familiaId', String(familiaId))
      .set('codigoFormulario', codigoFormulario);

    return this.http
      .post<ApiResponse<number>>(`${this.apiUrl}/formularios/borrador`, definicion, {
        params,
        ...this.confirmado
      })
      .pipe(map(response => response.datos));
  }

  clonarVersionFormulario(id: number): Observable<number> {
    return this.http
      .post<ApiResponse<number>>(`${this.apiUrl}/formularios/${id}/clonar`, {}, this.confirmado)
      .pipe(map(response => response.datos));
  }

  actualizarBorradorFormulario(id: number, definicion: string): Observable<ApiMessage> {
    return this.http.put<ApiMessage>(
      `${this.apiUrl}/formularios/${id}`,
      definicion,
      this.confirmado
    );
  }

  publicarVersionFormulario(id: number): Observable<ApiMessage> {
    return this.http.post<ApiMessage>(
      `${this.apiUrl}/formularios/${id}/publicar`,
      {},
      this.confirmado
    );
  }

  cambiarVigenciaFormulario(id: number, vigente: boolean): Observable<ApiMessage> {
    const params = new HttpParams().set('vigente', String(vigente));
    return this.http.put<ApiMessage>(
      `${this.apiUrl}/formularios/${id}/estado`,
      {},
      { params, ...this.confirmado }
    );
  }

  listarEvaluaciones(
    filtro: ConsultaEvaluacionPaginadaDto
  ): Observable<EvaluacionRiesgoDto[]> {
    return this.http
      .get<ApiResponse<EvaluacionRiesgoDto[]>>(`${this.apiUrl}/evaluaciones`, {
        params: this.construirParams(filtro)
      })
      .pipe(map(response => response.datos));
  }

  obtenerEvaluacion(id: number): Observable<EvaluacionRiesgoDto> {
    return this.http
      .get<ApiResponse<EvaluacionRiesgoDto>>(`${this.apiUrl}/evaluaciones/${id}`)
      .pipe(map(response => response.datos));
  }

  crearEvaluacion(dto: EvaluacionRiesgoDto): Observable<ApiMessage> {
    return this.http.post<ApiMessage>(
      `${this.apiUrl}/evaluaciones`,
      dto,
      this.confirmado
    );
  }

  actualizarEvaluacion(id: number, dto: EvaluacionRiesgoDto): Observable<ApiMessage> {
    return this.http.put<ApiMessage>(
      `${this.apiUrl}/evaluaciones/${id}`,
      dto,
      this.confirmado
    );
  }

  transicionarEvaluacion(
    id: number,
    nuevoEstado: string,
    motivo?: string
  ): Observable<ApiMessage> {
    let params = new HttpParams().set('nuevoEstado', nuevoEstado);
    if (motivo?.trim()) {
      params = params.set('motivo', motivo.trim());
    }

    return this.http.post<ApiMessage>(
      `${this.apiUrl}/evaluaciones/${id}/transiciones`,
      {},
      { params, ...this.confirmado }
    );
  }

  obtenerRevisiones(id: number): Observable<RevisionEvaluacionDto[]> {
    return this.http
      .get<ApiResponse<RevisionEvaluacionDto[]>>(`${this.apiUrl}/evaluaciones/${id}/revisiones`)
      .pipe(map(response => response.datos));
  }

  cargarEvidencia(archivo: File): Observable<EvidenciaDto> {
    const form = new FormData();
    form.append('archivo', archivo);
    return this.http
      .post<ApiResponse<EvidenciaDto>>(`${this.apiUrl}/evidencias/cargar`, form, this.confirmado)
      .pipe(map(response => response.datos));
  }

  vincularEvidenciaRiesgo(dto: AsociarEvidenciaRiesgoDto): Observable<ApiMessage> {
    return this.vincular('riesgo', dto);
  }

  vincularEvidenciaEvaluacion(dto: AsociarEvidenciaEvaluacionDto): Observable<ApiMessage> {
    return this.vincular('evaluacion', dto);
  }

  vincularEvidenciaControl(dto: AsociarEvidenciaControlDto): Observable<ApiMessage> {
    return this.vincular('control', dto);
  }

  vincularEvidenciaPlan(dto: AsociarEvidenciaPlanDto): Observable<ApiMessage> {
    return this.vincular('plan', dto);
  }

  vincularEvidenciaActividad(dto: AsociarEvidenciaActividadDto): Observable<ApiMessage> {
    return this.vincular('actividad', dto);
  }

  vincularEvidenciaAlerta(dto: AsociarEvidenciaAlertaDto): Observable<ApiMessage> {
    return this.vincular('alerta', dto);
  }

  vincularEvidenciaAutomonitoreo(dto: AsociarEvidenciaAutomonitoreoDto): Observable<ApiMessage> {
    return this.vincular('automonitoreo', dto);
  }

  vincularEvidenciaRevision(dto: AsociarEvidenciaRevisionDto): Observable<ApiMessage> {
    return this.vincular('revision', dto);
  }

  vincularEvidenciaAprobacion(dto: AsociarEvidenciaAprobacionDto): Observable<ApiMessage> {
    return this.vincular('aprobacion', dto);
  }

  eliminarEvidenciaHuerfana(id: number): Observable<ApiMessage> {
    return this.http.delete<ApiMessage>(`${this.apiUrl}/evidencias/${id}`, this.confirmado);
  }

  obtenerPoliticaEvidencias(): Observable<EvidenciaPoliticaDto> {
    return this.http
      .get<ApiResponse<EvidenciaPoliticaDto>>(`${environment.apiUrl}/listas/evidencias/politica`)
      .pipe(map(response => response.datos));
  }

  private vincular(tipo: string, dto: object): Observable<ApiMessage> {
    return this.http.post<ApiMessage>(
      `${this.apiUrl}/evidencias/vincular/${tipo}`,
      dto,
      this.confirmado
    );
  }

  private construirParams(filtro: object): HttpParams {
    let params = new HttpParams();
    for (const [clave, valor] of Object.entries(filtro)) {
      if (valor !== undefined && valor !== null && `${valor}`.trim() !== '') {
        params = params.set(clave, `${valor}`);
      }
    }
    return params;
  }
}
