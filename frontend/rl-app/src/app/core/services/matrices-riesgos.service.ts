import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CONFIRMACION_CAMBIOS_HEADER } from '../interceptors/confirmacion-cambios.interceptor';

type ApiResponse<T> = { success: boolean; datos: T; mensaje?: string };
type ApiMessage = { success: boolean; mensaje?: string };

export interface MatrizRiesgoFiltro {
  buscar?: string;
  estado?: string;
  sujetoTipo?: string;
  fechaInicio?: string;
  fechaFin?: string;
}

export interface MatrizRiesgoReporteFiltro extends MatrizRiesgoFiltro {
  nivelResidual?: string;
  modeloVersion?: string;
  responsable?: string;
}

export interface FactorInstitucional {
  codigo: string;
  nombre: string;
  pesoInstitucional: number;
  obligatorioGlobal: boolean;
}

export interface VariableMetodologia {
  variableId: number;
  factorId: number;
  factorCodigo: string;
  factorNombre: string;
  codigo: string;
  nombre: string;
  pesoInterno: number;
  obligatoria: boolean;
}

export interface EscalaRiesgo {
  escalaId: number;
  tipo: string;
  nivel: string;
  color: string;
  valorMinimo: number;
  valorMaximo: number;
  requierePlanAccion: boolean;
}

export interface CriterioCalculo {
  criterioId: number;
  factorId: number;
  factorCodigo: string;
  factorNombre: string;
  variableId: number;
  variableCodigo: string;
  variableNombre: string;
  escalaId?: number | null;
  valorDesde?: number | null;
  valorHasta?: number | null;
  puntaje: number;
  descripcion: string;
}

export interface MetodologiaMatrices {
  version: string;
  pesoTotalEsperado: number;
  puntajeMinimo: number;
  puntajeMaximo: number;
  mitigacionMaximaPct: number;
  decimalesCalculo: number;
  decimalesVisualizacion: number;
  factoresInstitucionales: FactorInstitucional[];
  variables: VariableMetodologia[];
  escalasRiesgo: EscalaRiesgo[];
  escalasCatalogo: EscalaRiesgo[];
  criterios: CriterioCalculo[];
  mitigacionesPermitidas: number[];
}

export interface MatrizRiesgoResumen {
  matrizId: number;
  modeloId: number;
  modeloVersion: string;
  sujetoTipo: string;
  sujetoIdExt?: string | null;
  documento?: string | null;
  nombreSujeto: string;
  estado: string;
  fechaEvaluacion: string;
  puntajeInherente?: number | null;
  nivelInherente?: string | null;
  puntajeResidual?: number | null;
  nivelResidual?: string | null;
  requierePlanAccion: boolean;
}

export interface MatrizRiesgoDetalle extends MatrizRiesgoResumen {
  origenDatos: string;
  motivoEstado?: string | null;
  snapshotMetodo?: string | null;
  detalles: MatrizRiesgoVariableDetalle[];
  controles: MatrizRiesgoControl[];
  resultados: MatrizRiesgoResultado[];
}

export interface MatrizRiesgoVariableDetalle {
  detalleId: number;
  variableId: number;
  factorId: number;
  factorCodigo: string;
  factorNombre: string;
  factorPesoInstitucional: number;
  variableCodigo: string;
  variableNombre: string;
  variablePesoInterno: number;
  valorCapturado?: string | null;
  puntaje?: number | null;
  puntajePonderado?: number | null;
  justificacion?: string | null;
  fuenteDato?: string | null;
  obligatoria: boolean;
}

export interface MatrizRiesgoControl {
  controlId: number;
  factorId?: number | null;
  factorCodigo?: string | null;
  nombre: string;
  descripcion?: string | null;
  efectividadPct: number;
  responsable?: string | null;
  estado: string;
  evidenciaObligatoria: boolean;
  tieneEvidencia: boolean;
}

export interface MatrizRiesgoResultado {
  resultadoId: number;
  factorId?: number | null;
  tipoResultado: string;
  versionCalculo: string;
  esVigente: boolean;
  puntajeInherente: number;
  nivelInherente: string;
  mitigacionPct: number;
  puntajeResidual: number;
  nivelResidual: string;
  requierePlanAccion: boolean;
  motivoRecalculo?: string | null;
  fechaCalculo: string;
}

export interface MatrizRiesgoDetalleRequest {
  variableId: number;
  valorCapturado?: string | null;
  puntaje: number;
  justificacion?: string | null;
  fuenteDato?: string | null;
}

export interface MatrizRiesgoControlRequest {
  factorId?: number | null;
  nombre: string;
  descripcion?: string | null;
  periodicidad?: string | null;
  oportunidad?: string | null;
  automatizacion?: string | null;
  procedimientos?: string | null;
  calidad?: string | null;
  efectividadPct: number;
  responsable?: string | null;
  evidenciaObligatoria: boolean;
}

export interface MatrizRiesgoCrearRequest {
  sujetoTipo: string;
  sujetoIdExt?: string | null;
  documento?: string | null;
  nombreSujeto: string;
  origenDatos: string;
  detalles: MatrizRiesgoDetalleRequest[];
  controles: MatrizRiesgoControlRequest[];
}

export interface MatrizRiesgoDashboard {
  totalMatrices: number;
  totalCalculadas: number;
  totalCerradas: number;
  totalConPlanAccion: number;
  porEstado: { nombre: string; total: number }[];
  porNivelResidual: { nombre: string; total: number }[];
}

export interface MatricesRiesgoReporte {
  fechaGeneracion: string;
  filtro: MatrizRiesgoReporteFiltro;
  totales: {
    totalMatrices: number;
    totalCalculadas: number;
    totalCerradas: number;
    totalAltoCritico: number;
    totalPlanAccionRequerido: number;
    totalPlanesVencidos: number;
  };
  porEstado: { nombre: string; total: number }[];
  porNivelResidual: { nombre: string; total: number }[];
  porSujetoTipo: { nombre: string; total: number }[];
  porFactor: {
    factorId: number;
    factorCodigo: string;
    factorNombre: string;
    totalMatrices: number;
    promedioInherente: number;
    promedioResidual: number;
    totalAltoCritico: number;
    totalPlanAccionRequerido: number;
  }[];
  mapaInherente: { nivel: string; total: number; promedio: number }[];
  mapaResidual: { nivel: string; total: number; promedio: number }[];
  matricesFiltradas: MatrizRiesgoResumen[];
  matricesCriticas: MatrizRiesgoResumen[];
  planesAccion: { estado: string; total: number; vencidos: number }[];
}

export interface MatrizRiesgoHistorial {
  historialId: number;
  matrizId?: number | null;
  tabla: string;
  registroId: string;
  accion: string;
  estadoAnterior?: string | null;
  estadoNuevo?: string | null;
  motivo?: string | null;
  usuarioEmail?: string | null;
  ip?: string | null;
  fecha: string;
}

export interface MatrizRiesgoCriterio {
  criterioId: number;
  factorId: number;
  factorCodigo: string;
  factorNombre: string;
  variableId: number;
  variableCodigo: string;
  variableNombre: string;
  escalaId?: number | null;
  escalaTipo?: string | null;
  escalaNivel?: string | null;
  valorDesde?: number | null;
  valorHasta?: number | null;
  puntaje: number;
  descripcion: string;
  activo: boolean;
  motivoInactivo?: string | null;
}

export interface MatrizRiesgoCriterioRequest {
  variableId: number;
  escalaId?: number | null;
  valorDesde?: number | null;
  valorHasta?: number | null;
  puntaje: number;
  descripcion: string;
}

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
