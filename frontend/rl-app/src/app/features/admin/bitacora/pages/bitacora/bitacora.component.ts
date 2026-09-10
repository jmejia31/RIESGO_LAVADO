import { ChangeDetectionStrategy, Component, OnInit, signal, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuditoriaService } from '../../data-access/auditoria.service';
import { AuditoriaDto } from '../../models/auditoria.models';

import { ActionIconComponent } from '../../../../../shared/components/action-icon/action-icon.component';
import { DataPaginationComponent } from '../../../../../shared/components/data-pagination/data-pagination.component';
import { PageSizeSelectorComponent } from '../../../../../shared/components/page-size-selector/page-size-selector.component';

@Component({
  selector: 'app-bitacora',
  standalone: true,
  imports: [ActionIconComponent, DataPaginationComponent, PageSizeSelectorComponent, CommonModule, FormsModule],
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

  showingRange = computed(() => {
    const total = this.totalRegistros();
    const start = total === 0 ? 0 : (this.paginaActual() - 1) * this.limite() + 1;
    const end = total === 0 ? 0 : Math.min(this.paginaActual() * this.limite(), total);
    return { start, end, total };
  });

  detalleAuditoria = computed(() => {
    const row = this.registroSeleccionado();
    if (!row) {
      return {
        descripcion: '',
        resultado: 'N/D',
        regional: 'N/D',
        metodoHttp: 'N/D',
        endpoint: 'N/D',
        correlationId: 'N/D',
        userAgent: 'N/D',
        identificador: 'N/D'
      };
    }

    const payloads = [this.parseAuditObject(row.datosNvo), this.parseAuditObject(row.datosAnt)]
      .filter((value): value is Record<string, unknown> => value !== null);
    const read = (...aliases: string[]) => {
      for (const payload of payloads) {
        const value = this.findAuditValue(payload, aliases);
        if (value !== undefined && value !== null && String(value).trim() !== '') {
          return this.serializeAuditMetadataValue(value);
        }
      }
      return 'N/D';
    };

    const resultado = read('Resultado', 'Result', 'EstadoResultado', 'Status');
    const identificador = read('Identificador', 'Usuario', 'Username', 'Email');
    return {
      descripcion: this.buildDetalleDescripcion(row, resultado, identificador),
      resultado,
      regional: read('Regional', 'RegionalId', 'Region', 'RegionId'),
      metodoHttp: read('MetodoHttp', 'MetodoHTTP', 'HttpMethod', 'Method', 'Metodo'),
      endpoint: read('Endpoint', 'Ruta', 'Path', 'Url'),
      correlationId: read('CorrelationId', 'CorrelationID', 'RequestId', 'TraceId'),
      userAgent: read('UserAgent', 'User-Agent', 'Browser', 'Navegador'),
      identificador
    };
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
      
      const stringAnt = hasAnt ? this.serializeAuditValue(valAnt) : null;
      const stringNvo = hasNvo ? this.serializeAuditValue(valNvo) : null;
      
      const changed = stringAnt !== stringNvo;

      if (hasAnt) {
        antList.push({
          key,
          value: this.serializeAuditValue(valAnt),
          changed
        });
      }
      if (hasNvo) {
        nvoList.push({
          key,
          value: this.serializeAuditValue(valNvo),
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
        this.errorCarga.set(err?.error?.mensaje || 'No se pudo cargar la bitácora con los filtros indicados.');
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
      case 'UPDATE': return 'bg-ihss-50 text-ihss-700 ring-1 ring-ihss-600/10';
      case 'DELETE': return 'bg-red-50 text-red-700 ring-1 ring-red-600/10';
      case 'VER': return 'bg-amber-50 text-amber-700 ring-1 ring-amber-600/10';
      case 'UPLOAD': return 'bg-sky-50 text-sky-700 ring-1 ring-sky-600/10';
      case 'LOGIN': return 'bg-indigo-50 text-indigo-700 ring-1 ring-indigo-600/10';
      case 'LOGOUT': return 'bg-slate-50 text-slate-700 ring-1 ring-slate-600/10';
      default: return 'bg-gray-50 text-gray-700 ring-1 ring-gray-600/10';
    }
  }

  getModuloLabel(modulo?: string): string {
    switch (modulo) {
      case 'Auth': return 'Autenticación';
      case 'AdminUsuarios': return 'Gestión de Usuarios';
      case 'MonitoreoListas': return 'Monitoreo de Listas';
      case 'CargaListas': return 'Carga de Listas';
      case 'ExportacionListas': return 'Listas de Cautela';
      case 'ExportacionMonitoreoListas': return 'Monitoreo de Listas';
      case 'ExportacionCoincidenciasPatrono': return 'Coincidencias Patrono';
      case 'ExportacionCoincidenciasEmpleado': return 'Coincidencias Empleado';
      case 'ExportacionFichaPerfil': return 'Ficha de Perfil';
      default: return modulo || 'No indicado';
    }
  }

  getIpDisplay(ip?: string): string {
    if (!ip?.trim()) return '-';
    const normalized = ip.trim().replace(/^\[|\]$/g, '');
    const mapped = normalized.match(/^::ffff:(\d{1,3}(?:\.\d{1,3}){3})$/i)?.[1];
    const value = mapped || normalized;
    if (value === '::1' || value === '0:0:0:0:0:0:0:1') return 'Local (127.0.0.1)';
    return value;
  }

  getDescripcionEvento(row: AuditoriaDto): string {
    const entidad = row.tabla || 'la entidad auditada';
    const registro = row.registroId ? ` #${row.registroId}` : '';

    switch (row.accion.toUpperCase()) {
      case 'LOGIN': return 'Inicio de sesión registrado para el usuario.';
      case 'LOGOUT': return 'Cierre de sesión registrado para el usuario.';
      case 'INSERT': return `Creación de registro en ${entidad}${registro}.`;
      case 'UPDATE': return `Modificación del registro${registro} en ${entidad}.`;
      case 'DELETE': return `Eliminación o inactivación registrada para ${entidad}${registro}.`;
      case 'VER': return `Consulta registrada sobre ${entidad}${registro}.`;
      case 'UPLOAD': return `Carga registrada en ${entidad}${registro}.`;
      default: return `Evento ${row.accion} registrado en ${entidad}${registro}.`;
    }
  }

  getRegistroDetalleLabel(row: AuditoriaDto | null): string {
    if (!row) return 'N/D';
    const tabla = row.tabla?.trim() || 'Registro';
    const entidad = tabla === 'RL_USUARIOS'
      ? 'Usuario'
      : tabla === 'RL_LISTA_POSITIVOS'
        ? 'Registro de Lista Positiva'
        : tabla;
    return row.registroId ? `${entidad} (#${row.registroId})` : entidad;
  }

  getResultadoBadgeClass(resultado: string): string {
    const value = (resultado || '').toUpperCase();
    if (value.includes('EXITO') || value.includes('SUCCESS') || value === 'OK') {
      return 'bg-emerald-50 text-emerald-700 ring-1 ring-emerald-600/10';
    }
    if (value.includes('FALL') || value.includes('ERROR') || value.includes('FAIL')) {
      return 'bg-red-50 text-red-700 ring-1 ring-red-600/10';
    }
    return 'bg-gray-50 text-gray-700 ring-1 ring-gray-600/10';
  }

  private buildDetalleDescripcion(row: AuditoriaDto, resultado: string, identificador: string): string {
    const action = row.accion.toUpperCase();
    const normalizedResult = resultado.toUpperCase();
    const actor = identificador !== 'N/D' ? identificador : (row.usrEmail || 'usuario');

    if (action === 'LOGIN') {
      if (normalizedResult.includes('EXITO') || normalizedResult.includes('SUCCESS')) {
        return `Inicio de sesión exitoso para usuario: ${actor}`;
      }
      if (normalizedResult.includes('FALL') || normalizedResult.includes('ERROR') || normalizedResult.includes('FAIL')) {
        return `Intento de inicio de sesión fallido para usuario: ${actor}`;
      }
    }

    return this.getDescripcionEvento(row);
  }

  private parseAuditObject(value?: string | null): Record<string, unknown> | null {
    if (!value) return null;
    try {
      const parsed = JSON.parse(value);
      return parsed && typeof parsed === 'object' && !Array.isArray(parsed)
        ? parsed as Record<string, unknown>
        : null;
    } catch {
      return null;
    }
  }

  private findAuditValue(source: Record<string, unknown>, aliases: string[]): unknown {
    const normalize = (value: string) => value
      .normalize('NFD')
      .replace(/[\u0300-\u036f]/g, '')
      .replace(/[\s_\-]/g, '')
      .toLowerCase();
    const expected = new Set(aliases.map(normalize));

    for (const [key, value] of Object.entries(source)) {
      if (expected.has(normalize(key))) return value;
    }

    for (const value of Object.values(source)) {
      if (value && typeof value === 'object' && !Array.isArray(value)) {
        const nested = this.findAuditValue(value as Record<string, unknown>, aliases);
        if (nested !== undefined) return nested;
      }
    }

    return undefined;
  }

  private serializeAuditMetadataValue(value: unknown): string {
    if (value === null || value === undefined) return 'N/D';
    if (typeof value === 'string') return value;
    if (typeof value === 'object') return JSON.stringify(value);
    return String(value);
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

  private serializeAuditValue(value: unknown): string {
    if (value === null) return 'null';
    if (typeof value === 'string') return `"${value}"`;
    if (typeof value === 'object') return JSON.stringify(value, null, 2) ?? 'null';
    return String(value);
  }

}
