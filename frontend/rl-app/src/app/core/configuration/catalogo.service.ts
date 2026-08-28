import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map, of } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Rol, Dominio, Modulo, CatalogoMatrices, ElementoCatalogoMatrices } from './catalogo.models';

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

  matrices(incluirInactivos = false): Observable<CatalogoMatrices[]> {
    return this.http.get<CatalogoMatrices[]>(`${this.apiUrl}/matrices`, { params: { incluirInactivos } });
  }

  crearMatriz(codigo: string, nombre: string): Observable<number> {
    return this.http.post<number>(`${this.apiUrl}/matrices`, { codigo, nombre });
  }

  actualizarMatriz(id: number, nombre: string, activo: boolean): Observable<boolean> {
    return this.http.put<boolean>(`${this.apiUrl}/matrices/${id}`, { nombre, activo });
  }

  crearElemento(catalogoId: number, codigo: string, valor: string, orden: number): Observable<number> {
    return this.http.post<number>(`${this.apiUrl}/matrices/${catalogoId}/elementos`, { codigo, valor, orden });
  }

  actualizarElemento(id: number, valor: string, orden: number, activo: boolean): Observable<boolean> {
    return this.http.put<boolean>(`${this.apiUrl}/matrices/elementos/${id}`, { valor, orden, activo });
  }
}
