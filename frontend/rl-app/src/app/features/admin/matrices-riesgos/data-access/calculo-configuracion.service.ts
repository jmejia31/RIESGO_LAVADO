import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { environment } from '../../../../../environments/environment';
import { CONFIRMACION_CAMBIOS_HEADER } from '../../../../core/interceptors/confirmacion-cambios.interceptor';
import {
  ActualizarFormulaBorradorDto,
  ActualizarFuncionBorradorDto,
  ActualizarParametroBorradorDto,
  CambiarEstadoConfiguracionDto,
  CrearFormulaDto,
  CrearFormulaUsoDto,
  CrearFuncionDto,
  CrearFuncionVersionDto,
  CrearParametroDto,
  CrearParametroVersionDto,
  FormulaDto,
  FormulaUsageDto,
  FormulaVersionDto,
  FuncionArgumentoDto,
  FuncionDto,
  FuncionVersionDto,
  ParametroDto,
  ParametroVersionDto
} from '../models/calculo-configuracion.models';

interface ApiResponse<T> { success: boolean; datos: T; mensaje?: string; }
interface ApiMessage { success: boolean; mensaje?: string; }

@Injectable({ providedIn: 'root' })
export class CalculoConfiguracionService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/matrices-riesgos/configuracion-calculo`;
  private readonly confirmado = { headers: new HttpHeaders({ [CONFIRMACION_CAMBIOS_HEADER]: '1' }) };
  private readonly confirmadoJson = { headers: new HttpHeaders({ [CONFIRMACION_CAMBIOS_HEADER]: '1', 'Content-Type': 'application/json' }) };

  listarFormulas(incluirInactivas = true): Observable<FormulaDto[]> {
    return this.http.get<ApiResponse<FormulaDto[]>>(`${this.apiUrl}/formulas`, { params: this.allParams(incluirInactivas) }).pipe(map(r => r.datos ?? []));
  }

  obtenerFormula(id: number): Observable<FormulaDto> {
    return this.http.get<ApiResponse<FormulaDto>>(`${this.apiUrl}/formulas/${id}`).pipe(map(r => r.datos));
  }

  crearFormula(dto: CrearFormulaDto): Observable<number> {
    return this.http.post<ApiResponse<number>>(`${this.apiUrl}/formulas`, dto, this.confirmadoJson).pipe(map(r => r.datos));
  }

  crearFormulaVersion(formulaId: number, dto: { expresion: string; tipoResultado: string }): Observable<number> {
    return this.http.post<ApiResponse<number>>(`${this.apiUrl}/formulas/${formulaId}/versiones`, dto, this.confirmadoJson).pipe(map(r => r.datos));
  }

  actualizarFormulaBorrador(versionId: number, dto: ActualizarFormulaBorradorDto): Observable<ApiMessage> {
    return this.http.put<ApiMessage>(`${this.apiUrl}/formula-versiones/${versionId}`, dto, this.confirmadoJson);
  }

  listarFormulaVersiones(formulaId: number): Observable<FormulaVersionDto[]> {
    return this.http.get<ApiResponse<FormulaVersionDto[]>>(`${this.apiUrl}/formulas/${formulaId}/versiones`).pipe(map(r => r.datos ?? []));
  }

  listarFormulaUsages(formulaId: number): Observable<FormulaUsageDto[]> {
    return this.http.get<ApiResponse<FormulaUsageDto[]>>(`${this.apiUrl}/formulas/${formulaId}/usos`).pipe(map(r => r.datos ?? []));
  }

  crearFormulaUso(dto: CrearFormulaUsoDto): Observable<ApiMessage> {
    return this.http.post<ApiMessage>(`${this.apiUrl}/formula-usos`, dto, this.confirmadoJson);
  }

  reemplazarFormulaUsos(versionFormularioId: number, usos: CrearFormulaUsoDto[]): Observable<ApiMessage> {
    return this.http.put<ApiMessage>(`${this.apiUrl}/formula-usos/versiones-formulario/${versionFormularioId}`, { usos }, this.confirmadoJson);
  }

  cambiarEstadoFormula(id: number, dto: CambiarEstadoConfiguracionDto): Observable<ApiMessage> {
    return this.http.patch<ApiMessage>(`${this.apiUrl}/formulas/${id}/estado`, dto, this.confirmadoJson);
  }

  listarFunciones(incluirInactivas = true): Observable<FuncionDto[]> {
    return this.http.get<ApiResponse<FuncionDto[]>>(`${this.apiUrl}/funciones`, { params: this.allParams(incluirInactivas) }).pipe(map(r => r.datos ?? []));
  }

  obtenerFuncion(id: number): Observable<FuncionDto> {
    return this.http.get<ApiResponse<FuncionDto>>(`${this.apiUrl}/funciones/${id}`).pipe(map(r => r.datos));
  }

  crearFuncion(dto: CrearFuncionDto): Observable<number> {
    return this.http.post<ApiResponse<number>>(`${this.apiUrl}/funciones`, dto, this.confirmadoJson).pipe(map(r => r.datos));
  }

  crearFuncionVersion(funcionId: number, dto: CrearFuncionVersionDto): Observable<number> {
    return this.http.post<ApiResponse<number>>(`${this.apiUrl}/funciones/${funcionId}/versiones`, dto, this.confirmadoJson).pipe(map(r => r.datos));
  }

  actualizarFuncionBorrador(versionId: number, dto: ActualizarFuncionBorradorDto): Observable<ApiMessage> {
    return this.http.put<ApiMessage>(`${this.apiUrl}/funcion-versiones/${versionId}`, dto, this.confirmadoJson);
  }

  listarFuncionVersiones(funcionId: number): Observable<FuncionVersionDto[]> {
    return this.http.get<ApiResponse<FuncionVersionDto[]>>(`${this.apiUrl}/funciones/${funcionId}/versiones`).pipe(map(r => r.datos ?? []));
  }

  listarFuncionArgumentos(versionId: number): Observable<FuncionArgumentoDto[]> {
    return this.http.get<ApiResponse<FuncionArgumentoDto[]>>(`${this.apiUrl}/funcion-versiones/${versionId}/argumentos`).pipe(map(r => r.datos ?? []));
  }

  cambiarEstadoFuncionVersion(id: number, dto: CambiarEstadoConfiguracionDto): Observable<ApiMessage> {
    return this.http.patch<ApiMessage>(`${this.apiUrl}/funcion-versiones/${id}/estado`, dto, this.confirmadoJson);
  }

  listarParametros(incluirInactivas = true): Observable<ParametroDto[]> {
    return this.http.get<ApiResponse<ParametroDto[]>>(`${this.apiUrl}/parametros`, { params: this.allParams(incluirInactivas) }).pipe(map(r => r.datos ?? []));
  }

  obtenerParametro(id: number): Observable<ParametroDto> {
    return this.http.get<ApiResponse<ParametroDto>>(`${this.apiUrl}/parametros/${id}`).pipe(map(r => r.datos));
  }

  crearParametro(dto: CrearParametroDto): Observable<number> {
    return this.http.post<ApiResponse<number>>(`${this.apiUrl}/parametros`, dto, this.confirmadoJson).pipe(map(r => r.datos));
  }

  crearParametroVersion(parametroId: number, dto: CrearParametroVersionDto): Observable<number> {
    return this.http.post<ApiResponse<number>>(`${this.apiUrl}/parametros/${parametroId}/versiones`, dto, this.confirmadoJson).pipe(map(r => r.datos));
  }

  actualizarParametroBorrador(versionId: number, dto: ActualizarParametroBorradorDto): Observable<ApiMessage> {
    return this.http.put<ApiMessage>(`${this.apiUrl}/parametro-versiones/${versionId}`, dto, this.confirmadoJson);
  }

  listarParametroVersiones(parametroId: number): Observable<ParametroVersionDto[]> {
    return this.http.get<ApiResponse<ParametroVersionDto[]>>(`${this.apiUrl}/parametros/${parametroId}/versiones`).pipe(map(r => r.datos ?? []));
  }

  cambiarEstadoParametroVersion(id: number, dto: CambiarEstadoConfiguracionDto): Observable<ApiMessage> {
    return this.http.patch<ApiMessage>(`${this.apiUrl}/parametro-versiones/${id}/estado`, dto, this.confirmadoJson);
  }

  private allParams(incluirInactivas: boolean): HttpParams {
    return new HttpParams().set('incluirInactivas', String(incluirInactivas));
  }
}
