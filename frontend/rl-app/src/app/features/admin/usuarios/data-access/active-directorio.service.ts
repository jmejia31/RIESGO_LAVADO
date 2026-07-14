import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../../environments/environment';
import { ResultadoValidacionAd } from '../models/active-directorio.models';

@Injectable({ providedIn: 'root' })
export class ActiveDirectorioService {

  constructor(private http: HttpClient) {}

  validarUsuario(usuario: string, dominio: string = ''): Observable<{ success: boolean; datos: ResultadoValidacionAd }> {
    const params = dominio
      ? `usuario=${encodeURIComponent(usuario)}&dominio=${encodeURIComponent(dominio)}`
      : `usuario=${encodeURIComponent(usuario)}`;
    return this.http.get<{ success: boolean; datos: ResultadoValidacionAd }>(
      `${environment.apiUrl}/auth/validar-dominio?${params}`
    );
  }
}
