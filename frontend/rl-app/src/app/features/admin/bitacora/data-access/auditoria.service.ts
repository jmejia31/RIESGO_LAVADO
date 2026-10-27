import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../../../environments/environment';
import { Observable } from 'rxjs';
import { AuditoriaDto, AuditoriaPaginado } from '../models/auditoria.models';
import { createHttpErrorScopeContext, HttpErrorScope } from '../../../../core/http/http-error-scope';

@Injectable({
  providedIn: 'root'
})
export class AuditoriaService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/auditoria`;

  getBitacora(params: {
    pagina: number;
    limite: number;
    buscar?: string;
    accion?: string;
    modulo?: string;
    tabla?: string;
    registroId?: string | number;
    registroAlternativo?: string;
    fechaInicio?: string;
    fechaFin?: string;
  }, errorScope: HttpErrorScope = 'global'): Observable<AuditoriaPaginado> {
    let httpParams = new HttpParams()
      .set('pagina', params.pagina.toString())
      .set('limite', params.limite.toString());

    if (params.buscar) httpParams = httpParams.set('buscar', params.buscar);
    if (params.accion) httpParams = httpParams.set('accion', params.accion);
    if (params.modulo) httpParams = httpParams.set('modulo', params.modulo);
    if (params.tabla) httpParams = httpParams.set('tabla', params.tabla);
    if (params.registroId !== undefined) httpParams = httpParams.set('registroId', String(params.registroId));
    if (params.registroAlternativo) httpParams = httpParams.set('registroAlternativo', params.registroAlternativo);
    if (params.fechaInicio) httpParams = httpParams.set('fechaInicio', params.fechaInicio);
    if (params.fechaFin) httpParams = httpParams.set('fechaFin', params.fechaFin);

    const url = params.registroId !== undefined ? `${this.apiUrl}/entidad` : this.apiUrl;
    return this.http.get<AuditoriaPaginado>(url, {
      params: httpParams,
      context: createHttpErrorScopeContext(errorScope)
    });
  }

  getHistorialEntidad(params: {
    tabla: string;
    registroId: string | number;
    registroAlternativo?: string;
    pagina?: number;
    limite?: number;
  }, errorScope: HttpErrorScope = 'global'): Observable<AuditoriaPaginado> {
    let httpParams = new HttpParams()
      .set('tabla', params.tabla)
      .set('registroId', String(params.registroId))
      .set('pagina', String(params.pagina ?? 1))
      .set('limite', String(params.limite ?? 25));
    if (params.registroAlternativo) httpParams = httpParams.set('registroAlternativo', params.registroAlternativo);

    return this.http.get<AuditoriaPaginado>(`${this.apiUrl}/entidad`, {
      params: httpParams,
      context: createHttpErrorScopeContext(errorScope)
    });
  }

  getHistorialFamilia(params: { familiaId: number; pagina?: number; limite?: number }, errorScope: HttpErrorScope = 'global'): Observable<AuditoriaPaginado> {
    const httpParams = new HttpParams()
      .set('familiaId', String(params.familiaId))
      .set('pagina', String(params.pagina ?? 1))
      .set('limite', String(params.limite ?? 25));

    return this.http.get<AuditoriaPaginado>(`${this.apiUrl}/familia`, {
      params: httpParams,
      context: createHttpErrorScopeContext(errorScope)
    });
  }
}
