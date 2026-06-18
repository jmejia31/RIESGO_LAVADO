import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { Observable } from 'rxjs';

export interface AuditoriaDto {
  audId: number;
  tabla: string;
  registroId: string;
  accion: string;
  datosAnt?: string;
  datosNvo?: string;
  usrId?: number;
  usrEmail?: string;
  ip?: string;
  fecha: string;
  modulo?: string;
}

export interface AuditoriaPaginado {
  datos: AuditoriaDto[];
  totalRegistros: number;
}

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
    fechaInicio?: string;
    fechaFin?: string;
  }): Observable<AuditoriaPaginado> {
    let httpParams = new HttpParams()
      .set('pagina', params.pagina.toString())
      .set('limite', params.limite.toString());

    if (params.buscar) httpParams = httpParams.set('buscar', params.buscar);
    if (params.accion) httpParams = httpParams.set('accion', params.accion);
    if (params.modulo) httpParams = httpParams.set('modulo', params.modulo);
    if (params.fechaInicio) httpParams = httpParams.set('fechaInicio', params.fechaInicio);
    if (params.fechaFin) httpParams = httpParams.set('fechaFin', params.fechaFin);

    return this.http.get<AuditoriaPaginado>(this.apiUrl, { params: httpParams });
  }
}
