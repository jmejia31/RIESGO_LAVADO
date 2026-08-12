import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { environment } from '../../../../../environments/environment';
import { CONFIRMACION_CAMBIOS_HEADER } from '../../../../core/interceptors/confirmacion-cambios.interceptor';
import {
  ConsultaEvaluacionPaginadaDto,
  EvidenciaDto,
  EvidenciaPoliticaDto,
  EvaluacionRiesgoDto,
  FlujoEvaluacionDto,
  MetodologiaFormulario,
  RiesgoReporteFila,
  VincularEvidenciaDto,
  VersionFormularioDto
} from '../models/matrices-riesgos.models';
import {
  ActividadPlanDto,
  ActividadPlanGuardarDto,
  AutomonitoreoDto,
  AutomonitoreoGuardarDto,
  ControlRiesgoDto,
  ControlRiesgoGuardarDto,
  EvaluacionControlDto,
  EvaluacionControlGuardarDto,
  PlanMitigacionDto,
  PlanMitigacionGuardarDto,
  ResumenMatricesOperativoDto,
  RiesgoDto,
  RiesgoGuardarDto,
  SenalAlertaDto,
  SenalAlertaGuardarDto
} from '../models/matrices-riesgos-fase11.models';

export type ApiResponse<T> = { success: boolean; datos: T; mensaje?: string };
export type ApiMessage = { success: boolean; mensaje?: string; datos?: number };

@Injectable({ providedIn: 'root' })
export class MatricesRiesgosService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/matrices-riesgos`;
  private readonly confirmado = {
    headers: new HttpHeaders({ [CONFIRMACION_CAMBIOS_HEADER]: '1' })
  };
  private readonly confirmadoJson = {
    headers: new HttpHeaders({
      [CONFIRMACION_CAMBIOS_HEADER]: '1',
      'Content-Type': 'application/json'
    })
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
        ...this.confirmadoJson
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
      this.confirmadoJson
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

  listarRiesgos(incluirInactivos = false): Observable<RiesgoDto[]> {
    const params = new HttpParams().set('incluirInactivos', String(incluirInactivos));
    return this.http
      .get<ApiResponse<RiesgoDto[]>>(`${this.apiUrl}/riesgos`, { params })
      .pipe(map(response => response.datos));
  }

  obtenerRiesgo(id: number): Observable<RiesgoDto> {
    return this.http
      .get<ApiResponse<RiesgoDto>>(`${this.apiUrl}/riesgos/${id}`)
      .pipe(map(response => response.datos));
  }

  crearRiesgo(dto: RiesgoGuardarDto): Observable<number> {
    return this.http
      .post<ApiResponse<number>>(`${this.apiUrl}/riesgos`, dto, this.confirmado)
      .pipe(map(response => response.datos));
  }

  actualizarRiesgo(id: number, dto: RiesgoGuardarDto): Observable<ApiMessage> {
    return this.http.put<ApiMessage>(`${this.apiUrl}/riesgos/${id}`, dto, this.confirmado);
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

  obtenerFlujos(id: number): Observable<FlujoEvaluacionDto[]> {
    return this.http
      .get<ApiResponse<FlujoEvaluacionDto[]>>(`${this.apiUrl}/evaluaciones/${id}/flujos`)
      .pipe(map(response => response.datos));
  }

  listarControles(evaluacionId: number): Observable<ControlRiesgoDto[]> {
    return this.http
      .get<ApiResponse<ControlRiesgoDto[]>>(`${this.apiUrl}/mitigacion/evaluaciones/${evaluacionId}/controles`)
      .pipe(map(response => response.datos));
  }

  crearControl(dto: ControlRiesgoGuardarDto): Observable<number> {
    return this.http
      .post<ApiResponse<number>>(`${this.apiUrl}/mitigacion/controles`, dto, this.confirmado)
      .pipe(map(response => response.datos));
  }

  actualizarControl(id: number, dto: ControlRiesgoGuardarDto): Observable<ApiMessage> {
    return this.http.put<ApiMessage>(`${this.apiUrl}/mitigacion/controles/${id}`, dto, this.confirmado);
  }

  listarEvaluacionesControl(controlId: number): Observable<EvaluacionControlDto[]> {
    return this.http
      .get<ApiResponse<EvaluacionControlDto[]>>(`${this.apiUrl}/mitigacion/controles/${controlId}/evaluaciones`)
      .pipe(map(response => response.datos));
  }

  evaluarControl(controlId: number, dto: EvaluacionControlGuardarDto): Observable<number> {
    return this.http
      .post<ApiResponse<number>>(`${this.apiUrl}/mitigacion/controles/${controlId}/evaluaciones`, dto, this.confirmado)
      .pipe(map(response => response.datos));
  }

  listarPlanes(evaluacionId: number): Observable<PlanMitigacionDto[]> {
    return this.http
      .get<ApiResponse<PlanMitigacionDto[]>>(`${this.apiUrl}/mitigacion/evaluaciones/${evaluacionId}/planes`)
      .pipe(map(response => response.datos));
  }

  crearPlan(dto: PlanMitigacionGuardarDto): Observable<number> {
    return this.http
      .post<ApiResponse<number>>(`${this.apiUrl}/mitigacion/planes`, dto, this.confirmado)
      .pipe(map(response => response.datos));
  }

  actualizarPlan(id: number, dto: PlanMitigacionGuardarDto): Observable<ApiMessage> {
    return this.http.put<ApiMessage>(`${this.apiUrl}/mitigacion/planes/${id}`, dto, this.confirmado);
  }

  listarActividades(planId: number): Observable<ActividadPlanDto[]> {
    return this.http
      .get<ApiResponse<ActividadPlanDto[]>>(`${this.apiUrl}/mitigacion/planes/${planId}/actividades`)
      .pipe(map(response => response.datos));
  }

  crearActividad(dto: ActividadPlanGuardarDto): Observable<number> {
    return this.http
      .post<ApiResponse<number>>(`${this.apiUrl}/mitigacion/actividades`, dto, this.confirmado)
      .pipe(map(response => response.datos));
  }

  actualizarActividad(id: number, dto: ActividadPlanGuardarDto): Observable<ApiMessage> {
    return this.http.put<ApiMessage>(`${this.apiUrl}/mitigacion/actividades/${id}`, dto, this.confirmado);
  }

  listarAlertas(evaluacionId: number): Observable<SenalAlertaDto[]> {
    return this.http
      .get<ApiResponse<SenalAlertaDto[]>>(`${this.apiUrl}/monitoreo/evaluaciones/${evaluacionId}/alertas`)
      .pipe(map(response => response.datos));
  }

  crearAlerta(dto: SenalAlertaGuardarDto): Observable<number> {
    return this.http
      .post<ApiResponse<number>>(`${this.apiUrl}/monitoreo/alertas`, dto, this.confirmado)
      .pipe(map(response => response.datos));
  }

  cambiarEstadoAlerta(id: number, estado: 'ACTIVO' | 'INACTIVO'): Observable<ApiMessage> {
    return this.http.put<ApiMessage>(
      `${this.apiUrl}/monitoreo/alertas/${id}/estado`,
      { aleEstado: estado },
      this.confirmado
    );
  }

  listarAutomonitoreo(evaluacionId: number): Observable<AutomonitoreoDto[]> {
    return this.http
      .get<ApiResponse<AutomonitoreoDto[]>>(`${this.apiUrl}/monitoreo/evaluaciones/${evaluacionId}/automonitoreo`)
      .pipe(map(response => response.datos));
  }

  registrarAutomonitoreo(dto: AutomonitoreoGuardarDto): Observable<number> {
    return this.http
      .post<ApiResponse<number>>(`${this.apiUrl}/monitoreo/automonitoreo`, dto, this.confirmado)
      .pipe(map(response => response.datos));
  }

  obtenerResumenOperativo(): Observable<ResumenMatricesOperativoDto> {
    return this.http
      .get<ApiResponse<ResumenMatricesOperativoDto>>(`${this.apiUrl}/monitoreo/resumen`)
      .pipe(map(response => response.datos));
  }

  descargarConsolidadoExcel(): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/reportes/consolidado.xlsx`, { responseType: 'blob' });
  }

  descargarConsolidadoPdf(): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/reportes/consolidado.pdf`, { responseType: 'blob' });
  }

  cargarEvidencia(archivo: File): Observable<EvidenciaDto> {
    const form = new FormData();
    form.append('archivo', archivo);
    return this.http
      .post<ApiResponse<EvidenciaDto>>(`${this.apiUrl}/evidencias/cargar`, form, this.confirmado)
      .pipe(map(response => response.datos));
  }

  vincularEvidencia(dto: VincularEvidenciaDto): Observable<ApiMessage> {
    return this.http.post<ApiMessage>(`${this.apiUrl}/evidencias/vinculos`, dto, this.confirmado);
  }

  eliminarEvidenciaHuerfana(id: number): Observable<ApiMessage> {
    return this.http.delete<ApiMessage>(`${this.apiUrl}/evidencias/${id}`, this.confirmado);
  }

  obtenerPoliticaEvidencias(): Observable<EvidenciaPoliticaDto> {
    return this.http
      .get<ApiResponse<EvidenciaPoliticaDto>>(`${environment.apiUrl}/listas/evidencias/politica`)
      .pipe(map(response => response.datos));
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
