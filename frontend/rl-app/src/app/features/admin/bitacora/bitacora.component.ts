import { Component, OnInit, signal, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuditoriaService, AuditoriaDto } from '../../../core/services/auditoria.service';

@Component({
  selector: 'app-bitacora',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="w-full space-y-6">
      
      <!-- Encabezado -->
      <div class="bg-white p-6 rounded-2xl shadow-sm border border-gray-100 flex flex-col md:flex-row justify-between items-start md:items-center gap-4">
        <div>
          <h2 class="text-2xl font-bold text-gray-800">Bitácora de Sistema</h2>
          <p class="text-sm text-gray-500">Auditoría y registro de acciones realizadas en la plataforma.</p>
        </div>
      </div>

      <!-- Filtros Avanzados -->
      <div class="bg-white p-6 rounded-2xl shadow-sm border border-gray-100 space-y-4">
        <div class="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-5 gap-4">
          
          <!-- Búsqueda General -->
          <div class="flex flex-col">
            <label class="text-xs font-semibold text-gray-500 uppercase tracking-wider mb-1">Buscar</label>
            <input type="text" [(ngModel)]="filtroBuscar" (keyup.enter)="aplicarFiltros()"
              placeholder="Usuario, tabla, IP..."
              class="px-3 py-2 border border-gray-200 rounded-xl focus:outline-none focus:ring-2 focus:ring-ihss-600 transition-colors text-sm" />
          </div>

          <!-- Acción -->
          <div class="flex flex-col">
            <label class="text-xs font-semibold text-gray-500 uppercase tracking-wider mb-1">Acción</label>
            <select [(ngModel)]="filtroAccion" (change)="aplicarFiltros()"
              class="px-3 py-2 border border-gray-200 rounded-xl focus:outline-none focus:ring-2 focus:ring-ihss-600 transition-colors text-sm bg-white font-medium text-gray-700">
              <option value="">Todas</option>
              <option value="INSERT">INSERT (Crear)</option>
              <option value="UPDATE">UPDATE (Modificar)</option>
              <option value="DELETE">DELETE (Eliminar)</option>
              <option value="VER">VER (Visualizar)</option>
              <option value="LOGIN">LOGIN</option>
              <option value="LOGOUT">LOGOUT</option>
            </select>
          </div>

          <!-- Módulo -->
          <div class="flex flex-col">
            <label class="text-xs font-semibold text-gray-500 uppercase tracking-wider mb-1">Módulo</label>
            <select [(ngModel)]="filtroModulo" (change)="aplicarFiltros()"
              class="px-3 py-2 border border-gray-200 rounded-xl focus:outline-none focus:ring-2 focus:ring-ihss-600 transition-colors text-sm bg-white font-medium text-gray-700">
              <option value="">Todos</option>
              <option value="Auth">Autenticación (Auth)</option>
              <option value="AdminUsuarios">Gestión de Usuarios</option>
              <option value="MonitoreoListas">Monitoreo de Listas</option>
            </select>
          </div>

          <!-- Fecha Inicio -->
          <div class="flex flex-col">
            <label class="text-xs font-semibold text-gray-500 uppercase tracking-wider mb-1">Desde</label>
            <input type="date" [(ngModel)]="filtroFechaInicio" (change)="aplicarFiltros()"
              class="px-3 py-2 border border-gray-200 rounded-xl focus:outline-none focus:ring-2 focus:ring-ihss-600 transition-colors text-sm bg-white" />
          </div>

          <!-- Fecha Fin -->
          <div class="flex flex-col">
            <label class="text-xs font-semibold text-gray-500 uppercase tracking-wider mb-1">Hasta</label>
            <input type="date" [(ngModel)]="filtroFechaFin" (change)="aplicarFiltros()"
              class="px-3 py-2 border border-gray-200 rounded-xl focus:outline-none focus:ring-2 focus:ring-ihss-600 transition-colors text-sm bg-white" />
          </div>

        </div>

        <div class="flex justify-end gap-2">
          <button (click)="limpiarFiltros()"
            class="px-4 py-2 border border-gray-200 rounded-xl bg-white text-gray-700 hover:bg-gray-50 font-semibold text-xs transition-colors">
            Limpiar Filtros
          </button>
          <button (click)="aplicarFiltros()"
            class="px-4 py-2 bg-ihss-900 text-white rounded-xl hover:bg-ihss-800 font-semibold text-xs transition-colors shadow-sm">
            Buscar
          </button>
        </div>
      </div>

      <!-- Tabla y Paginación -->
      <div class="bg-white p-6 rounded-2xl shadow-sm border border-gray-100 space-y-4">
        
        <div class="overflow-x-auto rounded-xl border border-gray-200">
          @if (cargando()) {
            <div class="py-20 flex flex-col items-center justify-center gap-3">
              <svg class="animate-spin h-8 w-8 text-ihss-900" fill="none" viewBox="0 0 24 24">
                <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
              </svg>
              <p class="text-sm font-medium text-gray-500">Cargando registros...</p>
            </div>
          } @else {
            @if (datos().length === 0) {
              <div class="py-20 flex flex-col items-center justify-center gap-2">
                <svg class="w-12 h-12 text-gray-300" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9.172 16.172a4 4 0 015.656 0M9 10h.01M15 10h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
                <p class="text-sm font-medium text-gray-500">No se encontraron registros en la bitácora.</p>
              </div>
            } @else {
              <table class="min-w-full divide-y divide-gray-200">
                <thead class="bg-gray-50 text-[10px] font-bold text-gray-500 uppercase tracking-wider">
                  <tr>
                    <th class="px-6 py-3 text-left">Fecha y Hora</th>
                    <th class="px-6 py-3 text-left">Usuario</th>
                    <th class="px-6 py-3 text-left">Acción</th>
                    <th class="px-6 py-3 text-left">Tabla Afectada</th>
                    <th class="px-6 py-3 text-left">ID Registro</th>
                    <th class="px-6 py-3 text-left">Módulo</th>
                    <th class="px-6 py-3 text-left">IP</th>
                    <th class="px-6 py-3 text-center">Detalle</th>
                  </tr>
                </thead>
                <tbody class="bg-white divide-y divide-gray-200 text-sm text-gray-700">
                  @for (row of datos(); track row.audId) {
                    <tr class="hover:bg-gray-50/50 transition-colors">
                      <td class="px-6 py-4 whitespace-nowrap text-xs text-gray-500">
                        {{ row.fecha | date:'dd/MM/yyyy HH:mm:ss' }}
                      </td>
                      <td class="px-6 py-4">
                        <span class="font-medium text-gray-900 block">{{ row.usrEmail || 'Sistema' }}</span>
                        @if (row.usrId) {
                          <span class="text-[10px] text-gray-400">ID: {{ row.usrId }}</span>
                        }
                      </td>
                      <td class="px-6 py-4 whitespace-nowrap">
                        <span [class]="getBadgeClass(row.accion)" class="inline-flex px-2.5 py-1 rounded text-xs font-semibold">
                          {{ row.accion }}
                        </span>
                      </td>
                      <td class="px-6 py-4 font-mono text-xs text-gray-600">
                        {{ row.tabla }}
                      </td>
                      <td class="px-6 py-4 text-xs font-semibold text-gray-700">
                        {{ row.registroId }}
                      </td>
                      <td class="px-6 py-4 text-xs text-gray-500">
                        {{ row.modulo || '-' }}
                      </td>
                      <td class="px-6 py-4 text-xs text-gray-500 font-mono">
                        {{ row.ip || '-' }}
                      </td>
                      <td class="px-6 py-4 text-center whitespace-nowrap">
                        @if (row.datosAnt || row.datosNvo) {
                          <button (click)="verDetalle(row)"
                            class="inline-flex items-center justify-center p-1.5 text-ihss-900 bg-ihss-50 hover:bg-ihss-100 rounded-lg transition-colors border border-ihss-200">
                            <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
                            </svg>
                          </button>
                        } @else {
                          <span class="text-xs text-gray-400">-</span>
                        }
                      </td>
                    </tr>
                  }
                </tbody>
              </table>

              <!-- Paginación -->
              <div class="px-6 py-4 flex items-center justify-between border-t border-gray-150 bg-gray-50/30">
                <div class="text-xs text-gray-500">
                  Mostrando {{ (paginaActual() - 1) * limite() + 1 }} a {{ mathMin(paginaActual() * limite(), totalRegistros()) }} de {{ totalRegistros() }} registros
                </div>
                <div class="flex items-center gap-3 text-xs text-gray-500">
                  <span>Mostrar</span>
                  <select [ngModel]="limite()" (ngModelChange)="limite.set(+$event); paginaActual.set(1); cargarDatos()"
                    class="border border-gray-200 rounded-xl pl-3 pr-8 py-1 focus:outline-none bg-white">
                    <option [value]="10">10</option>
                    <option [value]="25">25</option>
                    <option [value]="50">50</option>
                  </select>

                  <div class="flex items-center gap-1">
                    <button (click)="cambiarPagina(paginaActual() - 1)" [disabled]="paginaActual() === 1"
                      class="p-2 border border-gray-200 rounded-lg bg-white text-gray-600 hover:bg-gray-50 disabled:opacity-40 transition-all">
                      <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" />
                      </svg>
                    </button>
                    
                    @for (page of paginasArray(); track page) {
                      <button (click)="cambiarPagina(page)"
                        [class]="paginaActual() === page 
                          ? 'bg-ihss-900 text-white font-bold' 
                          : 'bg-white border border-gray-200 text-gray-600 hover:bg-gray-50'"
                        class="w-8 h-8 rounded-lg flex items-center justify-center text-xs transition-all">
                        {{ page }}
                      </button>
                    }

                    <button (click)="cambiarPagina(paginaActual() + 1)" [disabled]="paginaActual() === paginasTotales()"
                      class="p-2 border border-gray-200 rounded-lg bg-white text-gray-600 hover:bg-gray-50 disabled:opacity-40 transition-all">
                      <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
                      </svg>
                    </button>
                  </div>
                </div>
              </div>
            }
          }
        </div>
      </div>

      <!-- Modal de Visor de Cambios JSON -->
      @if (modalDetalleAbierto()) {
        <div class="fixed inset-0 z-50 flex items-center justify-center p-4 overflow-y-auto" role="dialog" aria-modal="true">
          
          <div class="fixed inset-0 bg-gray-500/75 transition-opacity" (click)="cerrarModalDetalle()"></div>

          <div class="relative bg-white rounded-2xl text-left overflow-hidden shadow-xl transform transition-all max-w-4xl w-full border border-gray-100 flex flex-col max-h-[90vh] z-50">
              
              <div class="bg-gray-50 px-6 py-4 flex justify-between items-center border-b border-gray-200">
                <div>
                  <h3 class="text-lg font-bold text-gray-900">Detalle de Valores Auditados</h3>
                  <p class="text-xs text-gray-500">
                    Registro ID: {{ registroSeleccionado()?.audId }} | Tabla: {{ registroSeleccionado()?.tabla }}
                    <span class="mx-2 text-gray-300">|</span>
                    Usuario: <span class="font-semibold text-gray-700">{{ registroSeleccionado()?.usrEmail || 'Sistema' }}</span>
                    <span class="mx-2 text-gray-300">|</span>
                    Fecha: <span class="font-semibold text-gray-700">{{ registroSeleccionado()?.fecha | date:'dd/MM/yyyy HH:mm:ss' }}</span>
                  </p>
                </div>
                <button (click)="cerrarModalDetalle()" class="text-gray-400 hover:text-gray-600 transition-colors">
                  <svg class="w-6 h-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
                  </svg>
                </button>
              </div>

              <div class="p-6 overflow-y-auto space-y-4">
                <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
                  
                  <!-- Datos Anteriores -->
                  <div class="flex flex-col space-y-2">
                    <span class="text-xs font-bold text-gray-500 uppercase tracking-wider">Valores Anteriores</span>
                    @if (propiedadesComparadas().esJson) {
                      <div class="border border-gray-250 rounded-xl overflow-hidden bg-gray-50 divide-y divide-gray-200 max-h-[400px] overflow-y-auto">
                        @for (item of propiedadesComparadas().ant; track item.key) {
                          <div class="p-3 flex justify-between items-start gap-4 text-xs font-mono transition-colors"
                               [class]="item.changed ? 'bg-red-50 text-red-800 border-l-4 border-red-500 pl-2' : 'text-gray-700'">
                            <span class="font-semibold text-gray-500 shrink-0" [class.text-red-700]="item.changed">"{{ item.key }}":</span>
                            <span class="text-right break-all">{{ item.value }}</span>
                          </div>
                        }
                        @if (propiedadesComparadas().ant.length === 0) {
                          <div class="p-4 text-xs text-gray-400 italic text-center">Sin valores anteriores</div>
                        }
                      </div>
                    } @else {
                      <pre class="bg-gray-50 border border-gray-200 rounded-xl p-4 text-xs font-mono text-gray-800 overflow-x-auto min-h-[150px] max-h-[400px]">
{{ formatJson(registroSeleccionado()?.datosAnt) }}
                      </pre>
                    }
                  </div>

                  <!-- Datos Nuevos -->
                  <div class="flex flex-col space-y-2">
                    <span class="text-xs font-bold text-gray-500 uppercase tracking-wider">Valores Nuevos</span>
                    @if (propiedadesComparadas().esJson) {
                      <div class="border border-gray-250 rounded-xl overflow-hidden bg-gray-50 divide-y divide-gray-200 max-h-[400px] overflow-y-auto">
                        @for (item of propiedadesComparadas().nvo; track item.key) {
                          <div class="p-3 flex justify-between items-start gap-4 text-xs font-mono transition-colors"
                               [class]="item.changed ? 'bg-emerald-50 text-emerald-800 border-l-4 border-emerald-500 pl-2' : 'text-gray-700'">
                            <span class="font-semibold text-gray-500 shrink-0" [class.text-emerald-700]="item.changed">"{{ item.key }}":</span>
                            <span class="text-right break-all" [class.font-bold]="item.changed">{{ item.value }}</span>
                          </div>
                        }
                        @if (propiedadesComparadas().nvo.length === 0) {
                          <div class="p-4 text-xs text-gray-400 italic text-center">Sin valores nuevos</div>
                        }
                      </div>
                    } @else {
                      <pre class="bg-gray-50 border border-gray-200 rounded-xl p-4 text-xs font-mono text-gray-800 overflow-x-auto min-h-[150px] max-h-[400px]">
{{ formatJson(registroSeleccionado()?.datosNvo) }}
                      </pre>
                    }
                  </div>

                </div>
              </div>

              <div class="bg-gray-50 px-6 py-3 flex justify-end border-t border-gray-200">
                <button (click)="cerrarModalDetalle()" class="px-4 py-2 border border-gray-200 rounded-xl bg-white text-gray-700 hover:bg-gray-50 font-semibold text-xs transition-colors">
                  Cerrar
                </button>
              </div>

          </div>
        </div>
      }

    </div>
  `
})
export class BitacoraComponent implements OnInit {
  private auditoriaService = inject(AuditoriaService);

  datos = signal<AuditoriaDto[]>([]);
  totalRegistros = signal(0);
  cargando = signal(false);

  // Filtros vinculados
  filtroBuscar = '';
  filtroAccion = '';
  filtroModulo = '';
  filtroFechaInicio = '';
  filtroFechaFin = '';

  // Paginación
  paginaActual = signal(1);
  limite = signal(10);

  // Modal Detalle
  modalDetalleAbierto = signal(false);
  registroSeleccionado = signal<AuditoriaDto | null>(null);

  paginasTotales = computed(() => {
    return Math.ceil(this.totalRegistros() / this.limite()) || 1;
  });

  paginasArray = computed(() => {
    const total = this.paginasTotales();
    return Array.from({ length: total }, (_, i) => i + 1);
  });

  propiedadesComparadas = computed(() => {
    const row = this.registroSeleccionado();
    if (!row) return { esJson: false, ant: [], nvo: [] };

    let objAnt: any = null;
    let objNvo: any = null;
    let esJson = true;

    try {
      if (row.datosAnt) objAnt = JSON.parse(row.datosAnt);
    } catch {
      esJson = false;
    }
    try {
      if (row.datosNvo) objNvo = JSON.parse(row.datosNvo);
    } catch {
      esJson = false;
    }

    if ((row.datosAnt && objAnt === null) || (row.datosNvo && objNvo === null)) {
      esJson = false;
    }

    if (!esJson) {
      return { esJson: false, ant: [], nvo: [] };
    }

    objAnt = objAnt || {};
    objNvo = objNvo || {};

    const keys = Array.from(new Set([...Object.keys(objAnt), ...Object.keys(objNvo)]));

    const antList: { key: string; value: string; changed: boolean }[] = [];
    const nvoList: { key: string; value: string; changed: boolean }[] = [];

    keys.forEach(key => {
      const valAnt = objAnt[key];
      const valNvo = objNvo[key];
      
      const hasAnt = key in objAnt;
      const hasNvo = key in objNvo;
      
      const stringAnt = hasAnt ? (typeof valAnt === 'object' ? JSON.stringify(valAnt) : String(valAnt)) : null;
      const stringNvo = hasNvo ? (typeof valNvo === 'object' ? JSON.stringify(valNvo) : String(valNvo)) : null;
      
      const changed = stringAnt !== stringNvo;

      if (hasAnt) {
        antList.push({
          key,
          value: valAnt === null ? 'null' : (typeof valAnt === 'string' ? `"${valAnt}"` : String(valAnt)),
          changed
        });
      }
      if (hasNvo) {
        nvoList.push({
          key,
          value: valNvo === null ? 'null' : (typeof valNvo === 'string' ? `"${valNvo}"` : String(valNvo)),
          changed
        });
      }
    });

    return { esJson: true, ant: antList, nvo: nvoList };
  });

  ngOnInit() {
    this.cargarDatos();
  }

  cargarDatos() {
    this.cargando.set(true);
    this.auditoriaService.getBitacora({
      pagina: this.paginaActual(),
      limite: this.limite(),
      buscar: this.filtroBuscar || undefined,
      accion: this.filtroAccion || undefined,
      modulo: this.filtroModulo || undefined,
      fechaInicio: this.filtroFechaInicio || undefined,
      fechaFin: this.filtroFechaFin || undefined
    }).subscribe({
      next: (res) => {
        this.datos.set(res.datos);
        this.totalRegistros.set(res.totalRegistros);
        this.cargando.set(false);
      },
      error: (err) => {
        console.error('Error al cargar bitácora:', err);
        this.datos.set([]);
        this.totalRegistros.set(0);
        this.cargando.set(false);
      }
    });
  }

  aplicarFiltros() {
    this.paginaActual.set(1);
    this.cargarDatos();
  }

  limpiarFiltros() {
    this.filtroBuscar = '';
    this.filtroAccion = '';
    this.filtroModulo = '';
    this.filtroFechaInicio = '';
    this.filtroFechaFin = '';
    this.paginaActual.set(1);
    this.cargarDatos();
  }

  cambiarPagina(pagina: number) {
    if (pagina < 1 || pagina > this.paginasTotales()) return;
    this.paginaActual.set(pagina);
    this.cargarDatos();
  }

  getBadgeClass(accion: string): string {
    switch (accion) {
      case 'INSERT': return 'bg-emerald-50 text-emerald-700 ring-1 ring-emerald-600/10';
      case 'UPDATE': return 'bg-blue-50 text-blue-700 ring-1 ring-blue-600/10';
      case 'DELETE': return 'bg-red-50 text-red-700 ring-1 ring-red-600/10';
      case 'VER': return 'bg-amber-50 text-amber-700 ring-1 ring-amber-600/10';
      case 'LOGIN': return 'bg-indigo-50 text-indigo-700 ring-1 ring-indigo-600/10';
      case 'LOGOUT': return 'bg-slate-50 text-slate-700 ring-1 ring-slate-600/10';
      default: return 'bg-gray-50 text-gray-700 ring-1 ring-gray-600/10';
    }
  }

  verDetalle(row: AuditoriaDto) {
    this.registroSeleccionado.set(row);
    this.modalDetalleAbierto.set(true);
  }

  cerrarModalDetalle() {
    this.modalDetalleAbierto.set(false);
    this.registroSeleccionado.set(null);
  }

  formatJson(val: string | undefined | null): string {
    if (!val) return 'Sin datos o valores vacíos';
    try {
      const parsed = JSON.parse(val);
      return JSON.stringify(parsed, null, 2);
    } catch {
      return val;
    }
  }

  mathMin(a: number, b: number): number {
    return Math.min(a, b);
  }
}
