import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map, of } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Rol, Dominio, Modulo } from './catalogo.models';

@Injectable({
  providedIn: 'root'
})
export class CatalogoService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/catalogos`;

  roles(): Observable<Rol[]> {
    return this.http.get<{ success: boolean, datos: Rol[] }>(`${this.apiUrl}/roles`).pipe(
      map(res => res.datos)
    );
  }
  dominios(): Observable<Dominio[]> {
    return this.http.get<{ success: boolean, datos: Dominio[] }>(`${this.apiUrl}/dominios`).pipe(
      map(res => res.datos)
    );
  }
  modulos(): Observable<Modulo[]> {
    return this.http.get<{ success: boolean, datos: Modulo[] }>(`${this.apiUrl}/modulos`).pipe(
      map(res => res.datos)
    );
  }
}
