import { ChangeDetectionStrategy, Component, OnInit, signal, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuditoriaService } from '../../data-access/auditoria.service';
import { AuditoriaDto } from '../../models/auditoria.models';

@Component({
  selector: 'app-bitacora',
  standalone: true,
  imports: [CommonModule, FormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './bitacora.component.html',
})
export class BitacoraComponent implements OnInit {
  private auditoriaService = inject(AuditoriaService);
  private busquedaTimer: ReturnType<typeof setTimeout> | null = null;

  datos = signal<AuditoriaDto[]>([]);
  totalRegistros = signal(0);
  cargando = signal(false);
  errorCarga = signal<string | null>(null);

  // Filtros vinculados
  filtroBuscar = '';
  filtroAccion = '';
  filtroModulo = '';
  filtroTabla = '';
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
    if (!this.validarRangoFechas()) return;

    this.cargando.set(true);
    this.errorCarga.set(null);
    this.auditoriaService.getBitacora({
      pagina: this.paginaActual(),
      limite: this.limite(),
      buscar: this.filtroBuscar || undefined,
      accion: this.filtroAccion || undefined,
      modulo: this.filtroModulo || undefined,
      tabla: this.filtroTabla || undefined,
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
        this.errorCarga.set(err?.error?.mensaje || 'No se pudo cargar la bitacora con los filtros indicados.');
        this.cargando.set(false);
      }
    });
  }

  aplicarFiltros() {
    this.paginaActual.set(1);
    this.cargarDatos();
  }

  programarBusquedaAutomatica() {
    if (this.busquedaTimer) clearTimeout(this.busquedaTimer);
    this.busquedaTimer = setTimeout(() => this.aplicarFiltros(), 350);
  }

  onFiltroAccionChange() {
    if (this.filtroAccion !== 'DELETE') {
      this.filtroTabla = '';
    }
    this.programarBusquedaAutomatica();
  }

  onFiltroModuloChange() {
    if (this.filtroModulo !== 'MonitoreoListas') {
      this.filtroTabla = '';
    }
    this.programarBusquedaAutomatica();
  }

  // Filtro rápido para evidencias inactivadas lógicamente y auditadas como DELETE.
  filtrarDocumentosEliminados() {
    this.filtroBuscar = '';
    this.filtroAccion = 'DELETE';
    this.filtroModulo = 'MonitoreoListas';
    this.filtroTabla = 'RL_DETALLE_EVIDENCIA';
    this.paginaActual.set(1);
    this.cargarDatos();
  }

  filtroDocumentosEliminadosActivo(): boolean {
    return this.filtroAccion === 'DELETE'
      && this.filtroModulo === 'MonitoreoListas'
      && this.filtroTabla === 'RL_DETALLE_EVIDENCIA';
  }

  limpiarFiltros() {
    this.filtroBuscar = '';
    this.filtroAccion = '';
    this.filtroModulo = '';
    this.filtroTabla = '';
    this.filtroFechaInicio = '';
    this.filtroFechaFin = '';
    this.errorCarga.set(null);
    this.paginaActual.set(1);
    this.cargarDatos();
  }

  private validarRangoFechas(): boolean {
    if (this.filtroFechaInicio && this.filtroFechaFin && this.filtroFechaInicio > this.filtroFechaFin) {
      this.errorCarga.set('La fecha "Desde" no puede ser mayor que la fecha "Hasta".');
      this.datos.set([]);
      this.totalRegistros.set(0);
      return false;
    }
    return true;
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
      case 'UPLOAD': return 'bg-purple-50 text-purple-700 ring-1 ring-purple-600/10';
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
