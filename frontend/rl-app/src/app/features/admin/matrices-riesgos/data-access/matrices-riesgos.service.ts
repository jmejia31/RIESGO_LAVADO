import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { environment } from '../../../../../environments/environment';
import { CONFIRMACION_CAMBIOS_HEADER } from '../../../../core/interceptors/confirmacion-cambios.interceptor';
import { createHttpErrorScopeContext, HttpErrorScope } from '../../../../core/http/http-error-scope';
import {
  ActualizarFamiliaFormularioDto,
  ConsultaEvaluacionPaginadaDto,
  CrearFamiliaFormularioDto,
  EvidenciaDto,
  DesvincularEvidenciaDto,
  EvidenciaPoliticaDto,
  EvaluacionRiesgoDto,
  CalculoPreviewEvaluacionDto,
  InstitutionalEvaluationProjectionDto,
  EvaluacionesPaginadasDto,
  FamiliaFormularioDto,
  FamiliasFormularioPaginadasDto,
  ConsultaFamiliasFormularioPaginada,
  FamiliaPredeterminadaDto,
  FiltroReporteMatrices,
  FlujoEvaluacionDto,
  TransicionesEvaluacionDto,
  MetodologiaFormulario,
  ReporteMatricesPaginado,
  RiesgoReporteFila,
  VincularEvidenciaDto,
  VersionFormularioDto
} from '../models/matrices-riesgos.models';
import {
  ActividadPlanDto,
  ActividadPlanGuardarDto,
  AutomonitoreoDto,
  AutomonitoreoGuardarDto,
  ControlMonitoreoActualizarDto,
  MatrizBloque6Dto,
  ControlRiesgoDto,
  ControlRiesgoGuardarDto,
  EvaluacionControlDto,
  EvaluacionControlGuardarDto,
  MitigacionBloque4Dto,
  PlanMitigacionDto,
  PlanMitigacionGuardarDto,
  ResumenMatricesOperativoDto,
  RiesgoDto,
  RiesgoGuardarDto,
  RiesgosPaginadosDto,
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
      .get<ApiResponse<MetodologiaFormulario>>(`${this.apiUrl}/metodologia/vigente`, { context: createHttpErrorScopeContext('local') })
      .pipe(map(response => response.datos));
  }

  metodologiaPorVersion(versionId: number): Observable<MetodologiaFormulario> {
    return this.http
      .get<ApiResponse<MetodologiaFormulario>>(`${this.apiUrl}/metodologia/version/${versionId}`, { context: createHttpErrorScopeContext('local') })
      .pipe(map(response => response.datos));
  }

  obtenerConsolidadoPaginado(filtro: FiltroReporteMatrices): Observable<ReporteMatricesPaginado> {
    return this.http
      .get<ApiResponse<ReporteMatricesPaginado>>(`${this.apiUrl}/consolidado/paginado`, { params: this.parametrosReporte(filtro) , context: createHttpErrorScopeContext('local')})
      .pipe(map(response => response.datos));
  }

  listarFamiliasFormularioPaginadas(filtro: ConsultaFamiliasFormularioPaginada): Observable<FamiliasFormularioPaginadasDto> {
    let params = new HttpParams()
      .set('pagina', String(filtro.pagina ?? 1))
      .set('tamanoPagina', String(filtro.tamanoPagina ?? 10))
      .set('estado', filtro.estado ?? 'TODAS')
      .set('vigencia', filtro.vigencia ?? 'TODAS');
    if (filtro.buscar?.trim()) params = params.set('buscar', filtro.buscar.trim());
    return this.http
      .get<ApiResponse<FamiliasFormularioPaginadasDto>>(`${this.apiUrl}/familias/paginado`, { params , context: createHttpErrorScopeContext('local')})
      .pipe(map(response => response.datos));
  }

  obtenerFamiliaPredeterminada(): Observable<FamiliaPredeterminadaDto> {
    return this.http
      .get<ApiResponse<FamiliaPredeterminadaDto>>(`${this.apiUrl}/familias/predeterminada`, { context: createHttpErrorScopeContext('local') })
      .pipe(map(response => response.datos));
  }

  establecerFamiliaPredeterminada(id: number): Observable<boolean> {
    return this.http
      .put<ApiResponse<unknown>>(`${this.apiUrl}/familias/${id}/predeterminada`, {}, { ...this.confirmado, context: createHttpErrorScopeContext('local') })
      .pipe(map(response => response.success));
  }

  obtenerFamiliaFormularioPorId(id: number): Observable<FamiliaFormularioDto> {
    return this.http
      .get<ApiResponse<FamiliaFormularioDto>>(`${this.apiUrl}/familias/${id}`, {
        context: createHttpErrorScopeContext('local')
      })
      .pipe(map(response => response.datos));
  }

  crearFamiliaFormulario(dto: CrearFamiliaFormularioDto): Observable<number> {
    return this.http
      .post<ApiResponse<number>>(`${this.apiUrl}/familias`, dto, {
        ...this.confirmadoJson,
        context: createHttpErrorScopeContext('local')
      })
      .pipe(map(response => response.datos));
  }

  actualizarFamiliaFormulario(id: number, dto: ActualizarFamiliaFormularioDto): Observable<boolean> {
    return this.http
      .put<ApiResponse<unknown>>(`${this.apiUrl}/familias/${id}`, dto, {
        ...this.confirmadoJson,
        context: createHttpErrorScopeContext('local')
      })
      .pipe(map(response => response.success));
  }

  desactivarFamiliaFormulario(id: number, errorScope: HttpErrorScope = 'global'): Observable<boolean> {
    return this.http
      .put<ApiResponse<unknown>>(`${this.apiUrl}/familias/${id}/desactivar`, {}, {
        ...this.confirmado,
        context: createHttpErrorScopeContext(errorScope)
      })
      .pipe(map(response => response.success));
  }

  activarFamiliaFormulario(id: number, errorScope: HttpErrorScope = 'global'): Observable<boolean> {
    return this.http
      .put<ApiResponse<unknown>>(`${this.apiUrl}/familias/${id}/activar`, {}, {
        ...this.confirmado,
        context: createHttpErrorScopeContext(errorScope)
      })
      .pipe(map(response => response.success));
  }

  eliminarFamiliaFormulario(id: number): Observable<boolean> {
    return this.http
      .delete<ApiResponse<unknown>>(`${this.apiUrl}/familias/${id}`, {
        ...this.confirmado,
        context: createHttpErrorScopeContext('local')
      })
      .pipe(map(response => response.success));
  }

  obtenerVersionVigenteFormulario(familiaCodigo?: string): Observable<VersionFormularioDto> {
    const params = familiaCodigo?.trim()
      ? new HttpParams().set('familiaCodigo', familiaCodigo.trim())
      : undefined;
    return this.http
      .get<ApiResponse<VersionFormularioDto>>(`${this.apiUrl}/formulario/version-vigente`, { params, context: createHttpErrorScopeContext('local') })
      .pipe(map(response => response.datos));
  }

  obtenerVersionFormulario(id: number): Observable<VersionFormularioDto> {
    return this.http
      .get<ApiResponse<VersionFormularioDto>>(`${this.apiUrl}/formularios/${id}`, { context: createHttpErrorScopeContext('local') })
      .pipe(map(response => response.datos));
  }

  listarHistorialVersionesFormulario(
    familiaCodigo: string
  ): Observable<VersionFormularioDto[]> {
    const params = new HttpParams().set('familiaCodigo', familiaCodigo.trim());
    return this.http
      .get<ApiResponse<VersionFormularioDto[]>>(`${this.apiUrl}/formularios/historial`, {
        params,
        context: createHttpErrorScopeContext('local')
      })
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
    const cuerpo = JSON.parse(definicion) as unknown;

    return this.http
      .post<ApiResponse<number>>(`${this.apiUrl}/formularios/borrador`, cuerpo, {
        params,
        ...this.confirmadoJson
      , context: createHttpErrorScopeContext('local')})
      .pipe(map(response => response.datos));
  }

  clonarVersionFormulario(id: number): Observable<number> {
    return this.http
      .post<ApiResponse<number>>(`${this.apiUrl}/formularios/${id}/clonar`, {}, {
        ...this.confirmado,
        context: createHttpErrorScopeContext('local')
      })
      .pipe(map(response => response.datos));
  }

  actualizarBorradorFormulario(id: number, definicion: string): Observable<ApiMessage> {
    const cuerpo = JSON.parse(definicion) as unknown;
    return this.http.put<ApiMessage>(
      `${this.apiUrl}/formularios/${id}`,
      cuerpo,
      { ...this.confirmadoJson, context: createHttpErrorScopeContext('local') }
    );
  }

  publicarVersionFormulario(id: number): Observable<ApiMessage> {
    return this.http.post<ApiMessage>(
      `${this.apiUrl}/formularios/${id}/publicar`,
      {},
      { ...this.confirmado, context: createHttpErrorScopeContext('local') }
    );
  }

  cambiarVigenciaFormulario(id: number, vigente: boolean): Observable<ApiMessage> {
    const params = new HttpParams().set('vigente', String(vigente));
    return this.http.put<ApiMessage>(
      `${this.apiUrl}/formularios/${id}/estado`,
      {},
      { params, ...this.confirmado , context: createHttpErrorScopeContext('local')}
    );
  }

  eliminarVersionFormulario(id: number): Observable<ApiMessage> {
    return this.http.delete<ApiMessage>(
      `${this.apiUrl}/formularios/${id}`,
      { ...this.confirmado, context: createHttpErrorScopeContext('local') }
    );
  }

  listarRiesgosPaginados(
    incluirInactivos = false,
    pagina = 1,
    tamanoPagina = 25,
    buscar = '',
    activo?: boolean, errorScope: HttpErrorScope = 'global'
  ): Observable<RiesgosPaginadosDto> {
    let params = new HttpParams()
      .set('incluirInactivos', String(incluirInactivos))
      .set('pagina', String(pagina))
      .set('tamanoPagina', String(tamanoPagina));
    if (buscar.trim()) params = params.set('buscar', buscar.trim());
    if (activo !== undefined) params = params.set('activo', String(activo));
    return this.http
      .get<ApiResponse<RiesgosPaginadosDto>>(`${this.apiUrl}/riesgos/paginado`, { params , context: createHttpErrorScopeContext(errorScope)})
      .pipe(map(response => response.datos));
  }

  obtenerRiesgo(id: number): Observable<RiesgoDto> {
    return this.http
      .get<ApiResponse<RiesgoDto>>(`${this.apiUrl}/riesgos/${id}`, { context: createHttpErrorScopeContext('local') })
      .pipe(map(response => response.datos));
  }

  crearRiesgo(dto: RiesgoGuardarDto): Observable<number> {
    return this.http
      .post<ApiResponse<number>>(`${this.apiUrl}/riesgos`, dto, {
        ...this.confirmado,
        context: createHttpErrorScopeContext('local')
      })
      .pipe(map(response => response.datos));
  }

  actualizarRiesgo(id: number, dto: RiesgoGuardarDto): Observable<ApiMessage> {
    return this.http.put<ApiMessage>(`${this.apiUrl}/riesgos/${id}`, dto, {
      ...this.confirmado,
      context: createHttpErrorScopeContext('local')
    });
  }

  listarEvaluaciones(
    filtro: ConsultaEvaluacionPaginadaDto
  ): Observable<EvaluacionesPaginadasDto> {
    return this.http
      .get<ApiResponse<EvaluacionesPaginadasDto>>(`${this.apiUrl}/evaluaciones`, {
        params: this.construirParams(filtro)
      , context: createHttpErrorScopeContext('local')})
      .pipe(map(response => response.datos));
  }

  obtenerEvaluacion(id: number): Observable<EvaluacionRiesgoDto> {
    return this.http
      .get<ApiResponse<EvaluacionRiesgoDto>>(`${this.apiUrl}/evaluaciones/${id}`, { context: createHttpErrorScopeContext('local') })
      .pipe(map(response => response.datos));
  }

  crearEvaluacion(dto: EvaluacionRiesgoDto): Observable<ApiMessage> {
    return this.http.post<ApiMessage>(
      `${this.apiUrl}/evaluaciones`,
      dto,
      { ...this.confirmado, context: createHttpErrorScopeContext('local') }
    );
  }

  actualizarEvaluacion(id: number, dto: EvaluacionRiesgoDto): Observable<ApiMessage> {
    return this.http.put<ApiMessage>(
      `${this.apiUrl}/evaluaciones/${id}`,
      dto,
      { ...this.confirmado, context: createHttpErrorScopeContext('local') }
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
      { params, ...this.confirmado , context: createHttpErrorScopeContext('local')}
    );
  }

  obtenerFlujos(id: number): Observable<FlujoEvaluacionDto[]> {
    return this.http
      .get<ApiResponse<FlujoEvaluacionDto[]>>(`${this.apiUrl}/evaluaciones/${id}/flujos`, { context: createHttpErrorScopeContext('local') })
      .pipe(map(response => response.datos));
  }

  obtenerTransicionesEvaluacion(id: number): Observable<TransicionesEvaluacionDto> {
    return this.http
      .get<ApiResponse<TransicionesEvaluacionDto>>(`${this.apiUrl}/evaluaciones/${id}/transiciones-permitidas`, { context: createHttpErrorScopeContext('local') })
      .pipe(map(response => response.datos));
  }

  listarControles(evaluacionId: number): Observable<ControlRiesgoDto[]> {
    return this.http
      .get<ApiResponse<ControlRiesgoDto[]>>(`${this.apiUrl}/mitigacion/evaluaciones/${evaluacionId}/controles`, { context: createHttpErrorScopeContext('local') })
      .pipe(map(response => response.datos));
  }

  crearControl(dto: ControlRiesgoGuardarDto): Observable<number> {
    return this.http
      .post<ApiResponse<number>>(`${this.apiUrl}/mitigacion/controles`, dto, { ...this.confirmado, context: createHttpErrorScopeContext('local') })
      .pipe(map(response => response.datos));
  }

  actualizarControl(id: number, dto: ControlRiesgoGuardarDto): Observable<ApiMessage> {
    return this.http.put<ApiMessage>(`${this.apiUrl}/mitigacion/controles/${id}`, dto, { ...this.confirmado, context: createHttpErrorScopeContext('local') });
  }

  listarEvaluacionesControl(controlId: number): Observable<EvaluacionControlDto[]> {
    return this.http
      .get<ApiResponse<EvaluacionControlDto[]>>(`${this.apiUrl}/mitigacion/controles/${controlId}/evaluaciones`, { context: createHttpErrorScopeContext('local') })
      .pipe(map(response => response.datos));
  }

  evaluarControl(controlId: number, dto: EvaluacionControlGuardarDto): Observable<number> {
    return this.http
      .post<ApiResponse<number>>(`${this.apiUrl}/mitigacion/controles/${controlId}/evaluaciones`, dto, { ...this.confirmado, context: createHttpErrorScopeContext('local') })
      .pipe(map(response => response.datos));
  }

  listarPlanes(evaluacionId: number): Observable<PlanMitigacionDto[]> {
    return this.http
      .get<ApiResponse<PlanMitigacionDto[]>>(`${this.apiUrl}/mitigacion/evaluaciones/${evaluacionId}/planes`, { context: createHttpErrorScopeContext('local') })
      .pipe(map(response => response.datos));
  }

  guardarBorradorEvaluacion(dto: EvaluacionRiesgoDto): Observable<number> {
    return this.http
      .post<ApiResponse<number>>(`${this.apiUrl}/evaluaciones/borrador`, dto, { ...this.confirmado, context: createHttpErrorScopeContext('local') })
      .pipe(map(response => response.datos));
  }

  actualizarBorradorEvaluacion(id: number, dto: EvaluacionRiesgoDto): Observable<ApiMessage> {
    return this.http.put<ApiMessage>(`${this.apiUrl}/evaluaciones/${id}/borrador`, dto, { ...this.confirmado, context: createHttpErrorScopeContext('local') });
  }

  obtenerProyeccionInstitucional(id: number): Observable<InstitutionalEvaluationProjectionDto> {
    return this.http
      .get<ApiResponse<InstitutionalEvaluationProjectionDto>>(`${this.apiUrl}/evaluaciones/${id}/proyeccion-institucional`, { context: createHttpErrorScopeContext('local') })
      .pipe(map(response => response.datos));
  }

  previsualizarCalculoEvaluacion(dto: EvaluacionRiesgoDto): Observable<CalculoPreviewEvaluacionDto> {
    return this.http
      .post<ApiResponse<CalculoPreviewEvaluacionDto>>(`${this.apiUrl}/evaluaciones/calculo-preview`, dto, { ...this.confirmado, context: createHttpErrorScopeContext('local') })
      .pipe(map(response => response.datos));
  }

  obtenerMitigacionBloque4(evaluacionId: number): Observable<MitigacionBloque4Dto> {
    return this.http
      .get<ApiResponse<MitigacionBloque4Dto>>(`${this.apiUrl}/mitigacion/evaluaciones/${evaluacionId}/bloque4`, { context: createHttpErrorScopeContext('local') })
      .pipe(map(response => response.datos));
  }

  crearPlan(dto: PlanMitigacionGuardarDto): Observable<number> {
    return this.http
      .post<ApiResponse<number>>(`${this.apiUrl}/mitigacion/planes`, dto, { ...this.confirmado, context: createHttpErrorScopeContext('local') })
      .pipe(map(response => response.datos));
  }

  actualizarPlan(id: number, dto: PlanMitigacionGuardarDto): Observable<ApiMessage> {
    return this.http.put<ApiMessage>(`${this.apiUrl}/mitigacion/planes/${id}`, dto, { ...this.confirmado, context: createHttpErrorScopeContext('local') });
  }

  listarActividades(planId: number): Observable<ActividadPlanDto[]> {
    return this.http
      .get<ApiResponse<ActividadPlanDto[]>>(`${this.apiUrl}/mitigacion/planes/${planId}/actividades`, { context: createHttpErrorScopeContext('local') })
      .pipe(map(response => response.datos));
  }

  crearActividad(dto: ActividadPlanGuardarDto): Observable<number> {
    return this.http
      .post<ApiResponse<number>>(`${this.apiUrl}/mitigacion/actividades`, dto, { ...this.confirmado, context: createHttpErrorScopeContext('local') })
      .pipe(map(response => response.datos));
  }

  actualizarActividad(id: number, dto: ActividadPlanGuardarDto): Observable<ApiMessage> {
    return this.http.put<ApiMessage>(`${this.apiUrl}/mitigacion/actividades/${id}`, dto, { ...this.confirmado, context: createHttpErrorScopeContext('local') });
  }

  listarAlertas(evaluacionId: number): Observable<SenalAlertaDto[]> {
    return this.http
      .get<ApiResponse<SenalAlertaDto[]>>(`${this.apiUrl}/monitoreo/evaluaciones/${evaluacionId}/alertas`, { context: createHttpErrorScopeContext('local') })
      .pipe(map(response => response.datos));
  }

  crearAlerta(dto: SenalAlertaGuardarDto): Observable<number> {
    return this.http
      .post<ApiResponse<number>>(`${this.apiUrl}/monitoreo/alertas`, dto, { ...this.confirmado, context: createHttpErrorScopeContext('local') })
      .pipe(map(response => response.datos));
  }

  cambiarEstadoAlerta(id: number, estado: 'ACTIVO' | 'INACTIVO'): Observable<ApiMessage> {
    return this.http.put<ApiMessage>(
      `${this.apiUrl}/monitoreo/alertas/${id}/estado`,
      { aleEstado: estado },
      { ...this.confirmado, context: createHttpErrorScopeContext('local') }
    );
  }

  listarAutomonitoreo(evaluacionId: number): Observable<AutomonitoreoDto[]> {
    return this.http
      .get<ApiResponse<AutomonitoreoDto[]>>(`${this.apiUrl}/monitoreo/evaluaciones/${evaluacionId}/automonitoreo`, { context: createHttpErrorScopeContext('local') })
      .pipe(map(response => response.datos));
  }

  obtenerBloque6(evaluacionId: number): Observable<MatrizBloque6Dto> {
    return this.http
      .get<ApiResponse<MatrizBloque6Dto>>(`${this.apiUrl}/monitoreo/evaluaciones/${evaluacionId}/bloque6`, { context: createHttpErrorScopeContext('local') })
      .pipe(map(response => response.datos));
  }

  actualizarMonitoreoControl(controlId: number, dto: ControlMonitoreoActualizarDto): Observable<ApiMessage> {
    return this.http.put<ApiMessage>(`${this.apiUrl}/monitoreo/controles/${controlId}`, dto, { ...this.confirmado, context: createHttpErrorScopeContext('local') });
  }

  actualizarObservacionArea(evaluacionId: number, texto: string): Observable<ApiMessage> {
    return this.http.put<ApiMessage>(`${this.apiUrl}/monitoreo/evaluaciones/${evaluacionId}/observaciones/area`, { texto }, { ...this.confirmado, context: createHttpErrorScopeContext('local') });
  }

  actualizarObservacionUgr(evaluacionId: number, texto: string): Observable<ApiMessage> {
    return this.http.put<ApiMessage>(`${this.apiUrl}/monitoreo/evaluaciones/${evaluacionId}/observaciones/ugr`, { texto }, { ...this.confirmado, context: createHttpErrorScopeContext('local') });
  }

  registrarAutomonitoreo(dto: AutomonitoreoGuardarDto): Observable<number> {
    return this.http
      .post<ApiResponse<number>>(`${this.apiUrl}/monitoreo/automonitoreo`, dto, { ...this.confirmado, context: createHttpErrorScopeContext('local') })
      .pipe(map(response => response.datos));
  }

  obtenerResumenOperativo(): Observable<ResumenMatricesOperativoDto> {
    return this.http
      .get<ApiResponse<ResumenMatricesOperativoDto>>(`${this.apiUrl}/monitoreo/resumen`, { context: createHttpErrorScopeContext('local') })
      .pipe(map(response => response.datos));
  }

  descargarConsolidadoExcel(filtro?: FiltroReporteMatrices): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/reportes/consolidado.xlsx`, { responseType: 'blob', params: this.parametrosReporte(filtro), context: createHttpErrorScopeContext('local') });
  }

  descargarConsolidadoPdf(filtro?: FiltroReporteMatrices): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/reportes/consolidado.pdf`, { responseType: 'blob', params: this.parametrosReporte(filtro), context: createHttpErrorScopeContext('local') });
  }

  private parametrosReporte(filtro?: FiltroReporteMatrices): HttpParams {
    let params = new HttpParams();
    if (!filtro) return params;
    const valores: Record<string, string | number | undefined> = {
      buscar: filtro.buscar,
      area: filtro.area,
      duenoRiesgo: filtro.duenoRiesgo,
      estadoEvaluacion: filtro.estadoEvaluacion,
      nivelInherente: filtro.nivelInherente,
      nivelResidual: filtro.nivelResidual,
      respuestaRiesgo: filtro.respuestaRiesgo,
      fechaInicio: filtro.fechaInicio,
      fechaFin: filtro.fechaFin,
      pagina: filtro.pagina,
      tamanoPagina: filtro.tamanoPagina,
      ordenarPor: filtro.ordenarPor,
      orden: filtro.orden
    };
    for (const [clave, valor] of Object.entries(valores)) {
      if (valor !== undefined && valor !== null && String(valor).trim() !== '') params = params.set(clave, String(valor));
    }
    return params;
  }

  cargarEvidencia(archivo: File): Observable<EvidenciaDto> {
    const form = new FormData();
    form.append('archivo', archivo);
    return this.http
      .post<ApiResponse<EvidenciaDto>>(`${this.apiUrl}/evidencias/cargar`, form, { ...this.confirmado, context: createHttpErrorScopeContext('local') })
      .pipe(map(response => response.datos));
  }

  vincularEvidencia(dto: VincularEvidenciaDto): Observable<ApiMessage> {
    return this.http.post<ApiMessage>(`${this.apiUrl}/evidencias/vinculos`, dto, { ...this.confirmado, context: createHttpErrorScopeContext('local') });
  }

  desvincularEvidencia(dto: DesvincularEvidenciaDto): Observable<ApiMessage> {
    return this.http.delete<ApiMessage>(`${this.apiUrl}/evidencias/vinculos`, { ...this.confirmado, body: dto , context: createHttpErrorScopeContext('local')});
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
