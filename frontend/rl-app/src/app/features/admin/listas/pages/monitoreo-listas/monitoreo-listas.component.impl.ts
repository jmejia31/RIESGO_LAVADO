import { ChangeDetectionStrategy, Component, OnDestroy, OnInit, signal, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ListasService } from '../../data-access/listas.service';
import { CoincidenciaJuridica, CoincidenciaNatural, CoincidenciaEmpleado, DetalleCoincidenciaNatural, DetalleCoincidenciaEmpleado, TipoDocumento, TipoListaCautela, RegistrarPositivoDto, Seguimiento, Evidencia, EvidenciaPolitica, MonitoreoTotales, ConsultaMonitoreoPaginada } from '../../models/listas.models';
import { ConfiguracionService } from '../../../../../core/configuration/configuracion.service';
import { jsPDF } from 'jspdf';
import * as XLSX from '../../../../../core/utils/excel-export.util';
import { COLORES_REPORTE_INSTITUCIONAL, agregarEncabezadoInstitucionalPdf, agregarPiesInstitucionalesPdf, asegurarEspacioSeccionPdf, autoTableInstitucional } from '../../../../../core/reporting/institutional-report.util';
import { getInstitutionalReportBranding } from '../../../../../core/reporting/institutional-report-standard';
import { EMPTY, of, forkJoin, Observable, Subject, Subscription } from 'rxjs';
import { catchError, map, switchMap } from 'rxjs/operators';
import { ActionIconComponent } from '../../../../../shared/components/action-icon/action-icon.component';
import { DataPaginationComponent } from '../../../../../shared/components/data-pagination/data-pagination.component';
import { PageSizeSelectorComponent } from '../../../../../shared/components/page-size-selector/page-size-selector.component';
import { InstitutionalFileUploadComponent } from '../../../../../shared/components/institutional-file-upload/institutional-file-upload.component';
import { INSTITUTIONAL_EVIDENCE_FILE_POLICY, InstitutionalFileUploadPolicy } from '../../../../../shared/components/institutional-file-upload/institutional-file-upload-policy';
import { ReportPreviewService } from '../../../../../shared/report-preview/report-preview.service';
import { InstitutionalConfirmationService } from '../../../../../shared/confirmation/institutional-confirmation.service';
import { InstitutionalNotificationService } from '../../../../../shared/notification/institutional-notification.service';
import { SortableTableHeaderComponent, SortDirection } from '../../../../../shared/components/sortable-table-header/sortable-table-header.component';

type FiltroTipo = 'juridica' | 'natural' | 'empleado';
type FiltroEstado = 'todos' | 'activos' | 'pendiente' | 'con_motivo' | 'manual' | 'cerrado_pasivo';
type RangoSeguimientoReporte = { desde?: string; hasta?: string; texto: string };
type MonitoreoRegistro = CoincidenciaJuridica | CoincidenciaNatural | CoincidenciaEmpleado;
type SolicitudMonitoreo = {
  tipo: FiltroTipo;
  consulta: ConsultaMonitoreoPaginada;
  contexto: string;
};

type RespuestaMonitoreo = {
  tipo: FiltroTipo;
  contexto: string;
  respuesta: { items: MonitoreoRegistro[]; pagina: number; totalPaginas: number; totalRegistros: number; totales: MonitoreoTotales };
};

@Component({
  selector: 'app-monitoreo-listas',
  standalone: true,
  imports: [ActionIconComponent, DataPaginationComponent, PageSizeSelectorComponent, InstitutionalFileUploadComponent, SortableTableHeaderComponent, CommonModule, FormsModule],
  changeDetection: ChangeDetectionStrategy.Eager,
  templateUrl: './monitoreo-listas.component.html',
})
export class MonitoreoListasComponent implements OnInit, OnDestroy {
  protected readonly reportPreview = inject(ReportPreviewService);
  private readonly confirmation = inject(InstitutionalConfirmationService);
  private readonly notification = inject(InstitutionalNotificationService);
  private configService = inject(ConfiguracionService);
  private filtroSeguimientoTimer: ReturnType<typeof setTimeout> | null = null;
  private readonly solicitudesMonitoreo$ = new Subject<SolicitudMonitoreo>();
  private monitoreoSubscription: Subscription | null = null;
  private busquedaTimer: ReturnType<typeof setTimeout> | null = null;
  private contextoMonitoreoCargado: string | null = null;
  private contextoMonitoreoEnCarga: string | null = null;
  private detalleSolicitud = 0;
  private seguimientoSolicitud = 0;
  readonly maxTextoTextarea = 1000;

  tipoActivo = signal<FiltroTipo>('juridica');
  cargando = signal(false);
  errorMonitoreo = signal<string | null>(null);
  busqueda = signal('');
  filtroEstado = signal<FiltroEstado>('todos');
  filtroFechaDesde = signal<string>('');
  filtroFechaHasta = signal<string>('');
  categoriaIdentidad = signal<'identificadas' | 'sin_identificacion'>('identificadas');
  periodoPlanilla = signal<string>('');
  periodoPlanillaDesde = signal<string>('');
  periodoPlanillaHasta = signal<string>('');
  rangoPlanillaInvalido = computed(() => !!this.periodoPlanillaDesde() && !!this.periodoPlanillaHasta()
    && this.periodoPlanillaDesde() > this.periodoPlanillaHasta());

  // Modal Detalle
  modalDetalleAbierto = signal(false);
  detalleCargando = signal(false);
  detalleError = signal<string | null>(null);
  detallesNatural = signal<DetalleCoincidenciaNatural[]>([]);
  detallesEmpleado = signal<DetalleCoincidenciaEmpleado[]>([]);
  personaSeleccionada = signal<CoincidenciaNatural | null>(null);
  personaSeleccionadaEmpleado = signal<CoincidenciaEmpleado | null>(null);

  // Modal Registrar Motivo
  modalMotivoAbierto = signal(false);
  guardandoMotivo = signal(false);
  listaTiposDocumento = signal<TipoDocumento[]>([]);
  entidadSeleccionada = signal<any | null>(null);
  formTipoDocId = signal<number | null>(null);
  formMotivo = signal<string>('');
  listaTiposListasCautela = signal<TipoListaCautela[]>([]);
  origenesRegistro = [
    { valor: 'DNP_LISTAS', etiqueta: 'Coincidencia DNP / Listas' },
    { valor: 'MANUAL_CUMPLIMIENTO', etiqueta: 'Registro manual Sección de Cumplimiento' },
    { valor: 'NOTICIA_PRENSA', etiqueta: 'Noticia / Prensa / Medio externo' },
    { valor: 'OTRO', etiqueta: 'Otro' }
  ];
  formOrigenRegistro = signal<string>('DNP_LISTAS');
  politicaEvidencias = signal<EvidenciaPolitica>({
    maximoMb: (INSTITUTIONAL_EVIDENCE_FILE_POLICY.maxFileSizeBytes ?? 10 * 1024 * 1024) / (1024 * 1024),
    maximoBytes: INSTITUTIONAL_EVIDENCE_FILE_POLICY.maxFileSizeBytes ?? 10 * 1024 * 1024,
    extensionesPermitidas: INSTITUTIONAL_EVIDENCE_FILE_POLICY.allowedExtensions.map(extension => `.${extension.replace(/^\./, '')}`),
    tiposPermitidosTexto: INSTITUTIONAL_EVIDENCE_FILE_POLICY.allowedTypesText
  });
  formTipoListaCautelaId = signal<number | null>(null);
  esRegistroManual = signal<boolean>(false);
  formManualNombre = signal<string>('');
  formManualNoDocumento = signal<string>('');
  formManualTipoPositivoId = signal<number | null>(null);
  formSeguimientoComentario = signal<string>('');
  archivosSeguimiento = signal<File[]>([]);

  // Modal Seguimiento
  modalSeguimientoAbierto = signal(false);
  cargandoSeguimiento = signal(false);
  seguimientoError = signal<string | null>(null);
  listaSeguimientos = signal<Seguimiento[]>([]);
  formComentarioSeguimiento = signal<string>('');
  filtroSeguimientoDesde = signal<string>('');
  filtroSeguimientoHasta = signal<string>('');
  reporteSeguimientoDocumento = signal<string>('');
  reporteSeguimientoDesde = signal<string>('');
  reporteSeguimientoHasta = signal<string>('');
  archivosSeleccionados = signal<File[]>([]);
  guardandoSeguimiento = signal(false);

  // Variables de Edición de Seguimiento
  modoEdicion = signal(false);
  seguimientoEditandoId = signal<number | null>(null);
  evidenciasExistentes = signal<Evidencia[]>([]);

  totalCoincidencias = computed(() => {
    return this.tipoActivo() === 'empleado' ? this.detallesEmpleado().length : this.detallesNatural().length;
  });
  
  coincidenciasPep = computed(() => {
    if (this.tipoActivo() === 'empleado') return 0;
    return this.detallesNatural().filter(det => det.esPep === 'SI' || det.esPep === 'S').length;
  });

  empresasUnicas = computed(() => {
    const list = this.tipoActivo() === 'empleado' 
      ? this.detallesEmpleado().map(det => det.nombreEmpresa?.trim() || '')
      : this.detallesNatural().map(det => det.nombreEmpresa?.trim() || '');
    return new Set(list.filter(x => x !== '')).size;
  });

  // Datos crudos de la API; cada colección conserva su origen para evitar mezclar reglas entre tipos de sujeto.
  juridicasRaw = signal<CoincidenciaJuridica[]>([]);
  naturalesRaw = signal<CoincidenciaNatural[]>([]);
  empleadosRaw = signal<CoincidenciaEmpleado[]>([]);
  private readonly totalesVacios: MonitoreoTotales = { totalRegistros: 0, pendientes: 0, conMotivo: 0, manuales: 0, cerradosPasivos: 0 };
  juridicasTotales = signal<MonitoreoTotales>(this.totalesVacios);
  naturalesTotales = signal<MonitoreoTotales>(this.totalesVacios);
  empleadosTotales = signal<MonitoreoTotales>(this.totalesVacios);
  juridicasPaginas = signal(0);
  naturalesPaginas = signal(0);
  empleadosPaginas = signal(0);
  juridicasTotalRegistros = signal(0);
  naturalesTotalRegistros = signal(0);
  empleadosTotalRegistros = signal(0);

  datosConsolidados = computed<MonitoreoRegistro[]>(() => [
    ...this.juridicasRaw(),
    ...this.naturalesRaw(),
    ...this.empleadosRaw()
  ]);

  datosActivos = computed<Array<CoincidenciaJuridica | CoincidenciaNatural | CoincidenciaEmpleado>>(() => {
    if (this.tipoActivo() === 'juridica') return this.juridicasRaw();
    if (this.tipoActivo() === 'natural') return this.naturalesRaw();
    return this.empleadosRaw();
  });

  etiquetaTipoActivo = computed(() => {
    if (this.tipoActivo() === 'juridica') return 'Personas jurídicas';
    if (this.tipoActivo() === 'natural') return 'Personas naturales';
    return 'Empleados';
  });

  totalesActuales = computed(() => this.tipoActivo() === 'juridica' ? this.juridicasTotales() : this.tipoActivo() === 'natural' ? this.naturalesTotales() : this.empleadosTotales());
  totalActual = computed(() => this.totalesActuales().totalRegistros);
  coincidenciasActivasActual = computed(() => this.totalesActuales().coincidenciasActivas ?? this.totalActual());
  totalCoincidenciasFuente = computed(() => this.totalesActuales().totalCoincidenciasFuente ?? this.totalActual());
  pendientesActual = computed(() => this.totalesActuales().pendientes);
  conMotivoActual = computed(() => this.totalesActuales().conMotivo);
  cerradosPasivosActual = computed(() => this.totalesActuales().cerradosPasivos);
  positivosManualesActual = computed(() => this.totalesActuales().manuales);
  totalGeneral = computed(() => this.totalActual());
  coincidenciasActivasGeneral = computed(() => this.coincidenciasActivasActual());
  pendientesGeneral = computed(() => this.pendientesActual());
  conMotivoGeneral = computed(() => this.conMotivoActual());
  positivosManualesGeneral = computed(() => this.positivosManualesActual());
  cerradosPasivosGeneral = computed(() => this.cerradosPasivosActual());
  totalRegistrosTabla = computed(() => {
    if (this.tipoActivo() === 'juridica') return this.juridicasTotalRegistros();
    if (this.tipoActivo() === 'natural') return this.naturalesTotalRegistros();
    return this.empleadosTotalRegistros();
  });

  hayFiltrosPrincipalesActivos = computed(() =>
    !!this.busqueda().trim() ||
    this.filtroEstado() !== 'todos' ||
    !!this.filtroFechaDesde() ||
    !!this.filtroFechaHasta() ||
    (this.tipoActivo() === 'natural' && this.categoriaIdentidad() === 'sin_identificacion') ||
    (this.tipoActivo() === 'empleado' && (!!this.periodoPlanilla() || !!this.periodoPlanillaDesde() || !!this.periodoPlanillaHasta()))
  );

  // Paginación
  paginaActual = signal(1);
  limite = signal(10);
  orden = signal('id');
  direccion = signal<SortDirection>('asc');

  constructor(private listasService: ListasService) {}

  ngOnInit() {
    this.iniciarPipelineMonitoreo();
    this.cargarDatos();
    this.listasService.getTiposDocumento().subscribe({
      next: (res) => {
        this.listaTiposDocumento.set(res);
        if (this.modalMotivoAbierto() && !this.formTipoDocId()) {
          this.formTipoDocId.set(this.deducirTipoDocumentoDefault(this.entidadSeleccionada(), this.tipoActivo()));
        }
      },
      error: (err) => console.error('Error al cargar tipos de documento:', err)
    });
    this.listasService.getTiposListasCautela().subscribe({
      next: (res) => {
        this.listaTiposListasCautela.set(res);
        if (this.modalMotivoAbierto() && !this.formTipoListaCautelaId()) {
          this.formTipoListaCautelaId.set(this.deducirTipoListaCautelaDefault(this.entidadSeleccionada()));
        }
      },
      error: (err) => console.error('Error al cargar tipos de listas de cautela:', err)
    });
    this.listasService.getPoliticaEvidencias().subscribe({
      next: (res) => this.politicaEvidencias.set({
        ...res,
        extensionesPermitidas: res.extensionesPermitidas?.length ? res.extensionesPermitidas : this.politicaEvidencias().extensionesPermitidas
      }),
      error: (err) => console.warn('No se pudo cargar la política de evidencias; se usará la política por defecto.', err)
    });
  }

  cambiarTipo(tipo: FiltroTipo) {
    this.tipoActivo.set(tipo);
    this.busqueda.set('');
    this.paginaActual.set(1);
    this.cargarDatos({ tipo, force: false });
  }

  ngOnDestroy(): void {
    if (this.busquedaTimer) clearTimeout(this.busquedaTimer);
    this.solicitudesMonitoreo$.complete();
    this.monitoreoSubscription?.unsubscribe();
  }

  cambiarBusqueda(valor: string): void {
    this.busqueda.set(valor);
    this.paginaActual.set(1);
    if (this.busquedaTimer) clearTimeout(this.busquedaTimer);
    this.busquedaTimer = setTimeout(() => this.cargarDatos(), 350);
  }

  cambiarEstado(valor: FiltroEstado): void {
    this.filtroEstado.set(valor);
    this.paginaActual.set(1);
    this.cargarDatos();
  }

  cambiarFechaDesde(valor: string): void {
    this.filtroFechaDesde.set(valor);
    this.paginaActual.set(1);
    this.cargarDatos();
  }

  cambiarFechaHasta(valor: string): void {
    this.filtroFechaHasta.set(valor);
    this.paginaActual.set(1);
    this.cargarDatos();
  }

  cambiarCategoriaIdentidad(valor: 'identificadas' | 'sin_identificacion'): void {
    if (valor !== 'identificadas' && valor !== 'sin_identificacion') return;
    this.categoriaIdentidad.set(valor);
    this.paginaActual.set(1);
    this.cargarDatos({ force: true });
  }

  cambiarPeriodoPlanilla(valor: string): void {
    // input[type=month]; el backend recibe una fecha dia 1 valida.
    if (valor && !/^\d{4}-(0[1-9]|1[0-2])$/.test(valor)) return;
    this.periodoPlanilla.set(valor);
    this.periodoPlanillaDesde.set('');
    this.periodoPlanillaHasta.set('');
    this.paginaActual.set(1);
    this.cargarDatos({ force: true });
  }

  cambiarRangoPlanilla(extremo: 'desde' | 'hasta', valor: string): void {
    if (valor && !/^\d{4}-(0[1-9]|1[0-2])$/.test(valor)) return;
    if (extremo === 'desde') this.periodoPlanillaDesde.set(valor);
    else this.periodoPlanillaHasta.set(valor);
    this.periodoPlanilla.set('');
    this.paginaActual.set(1);
    this.cargarDatos({ force: true });
  }

  cambiarLimite(valor: number): void {
    if (![10, 25, 50].includes(valor)) return;
    this.limite.set(valor);
    this.paginaActual.set(1);
    this.cargarDatos();
  }

  cambiarPagina(pagina: number): void {
    const total = this.paginasTotales();
    if (!Number.isInteger(pagina) || total <= 0 || pagina < 1 || pagina > total) return;
    this.paginaActual.set(pagina);
    this.cargarDatos();
  }

  ordenar(columna: string): void {
    if (this.orden() === columna) this.direccion.set(this.direccion() === 'asc' ? 'desc' : 'asc');
    else { this.orden.set(columna); this.direccion.set('asc'); }
    this.paginaActual.set(1);
    this.cargarDatos({ force: true });
  }

  limpiarFiltrosPrincipales() {
    this.busqueda.set('');
    this.filtroEstado.set('todos');
    this.filtroFechaDesde.set('');
    this.filtroFechaHasta.set('');
    this.categoriaIdentidad.set('identificadas');
    this.periodoPlanilla.set('');
    this.periodoPlanillaDesde.set('');
    this.periodoPlanillaHasta.set('');
    this.paginaActual.set(1);
    this.cargarDatos();
  }

  private limitarTexto(valor: string | null | undefined): string {
    return (valor || '').slice(0, this.maxTextoTextarea);
  }

  actualizarMotivoIngreso(valor: string) {
    this.formMotivo.set(this.limitarTexto(valor));
  }

  actualizarComentarioSeguimientoInicial(valor: string) {
    this.formSeguimientoComentario.set(this.limitarTexto(valor));
  }

  actualizarComentarioSeguimiento(valor: string) {
    this.formComentarioSeguimiento.set(this.limitarTexto(valor));
  }

  cargarDatos(options: { tipo?: FiltroTipo; force?: boolean; mostrarLoader?: boolean } = {}) {
    this.iniciarPipelineMonitoreo();
    const tipo = options.tipo ?? this.tipoActivo();
    if (tipo === 'empleado' && this.rangoPlanillaInvalido()) {
      const mensaje = 'El mes inicial de planilla no puede ser posterior al mes final.';
      this.errorMonitoreo.set(mensaje);
      this.notificar('Rango de planilla inválido', mensaje, 'warning', 'view');
      this.cargando.set(false);
      this.contextoMonitoreoCargado = null;
      this.contextoMonitoreoEnCarga = null;
      return;
    }
    const forzar = options.force ?? false;
    const mostrarLoader = options.mostrarLoader ?? true;
    const consulta: ConsultaMonitoreoPaginada = { pagina: this.paginaActual(), tamanoPagina: this.limite(), buscar: this.busqueda(), estado: this.filtroEstado(), fechaDesde: this.filtroFechaDesde() || null, fechaHasta: this.filtroFechaHasta() || null, orden: this.orden(), direccion: this.direccion(),
      categoriaIdentidad: tipo === 'natural' ? this.categoriaIdentidad() : undefined,
      periodoPlanilla: tipo === 'empleado' && this.periodoPlanilla() ? `${this.periodoPlanilla()}-01` : null,
      periodoPlanillaDesde: tipo === 'empleado' && this.periodoPlanillaDesde() ? `${this.periodoPlanillaDesde()}-01` : null,
      periodoPlanillaHasta: tipo === 'empleado' && this.periodoPlanillaHasta() ? `${this.periodoPlanillaHasta()}-01` : null };
    const contexto = JSON.stringify([tipo, consulta]);
    if (!forzar && (contexto === this.contextoMonitoreoCargado || contexto === this.contextoMonitoreoEnCarga)) return;

    if (mostrarLoader && tipo === this.tipoActivo()) {
      this.cargando.set(true);
      this.errorMonitoreo.set(null);
    }
    this.contextoMonitoreoEnCarga = contexto;
    this.solicitudesMonitoreo$.next({ tipo, consulta, contexto });
  }

  private iniciarPipelineMonitoreo(): void {
    if (this.monitoreoSubscription) return;

    this.monitoreoSubscription = this.solicitudesMonitoreo$.pipe(
      switchMap(({ tipo, consulta, contexto }) => this.consultarMonitoreo(tipo, consulta).pipe(
        map(respuesta => ({ tipo, contexto, respuesta } as RespuestaMonitoreo)),
        catchError((err) => {
          if (tipo === this.tipoActivo() && this.contextoMonitoreoEnCarga === JSON.stringify([tipo, consulta])) {
            this.contextoMonitoreoEnCarga = null;
          }
          if (tipo === this.tipoActivo()) {
            const mensaje = this.mensajeErrorApi(err, 'No se pudieron cargar las coincidencias. Los datos mostrados anteriormente se conservaron.');
            this.errorMonitoreo.set(mensaje);
            this.notificar('No se pudieron cargar las coincidencias', mensaje, 'error', 'cancel');
            this.cargando.set(false);
          }
          return EMPTY;
        })
      ))
    ).subscribe(({ tipo, contexto, respuesta }) => {
      if (tipo !== this.tipoActivo()) return;
      if (tipo === 'juridica') {
        this.juridicasRaw.set(respuesta.items as CoincidenciaJuridica[]);
        this.juridicasTotales.set(respuesta.totales);
        this.juridicasPaginas.set(respuesta.totalPaginas);
        this.juridicasTotalRegistros.set(respuesta.totalRegistros);
      } else if (tipo === 'natural') {
        this.naturalesRaw.set(respuesta.items as CoincidenciaNatural[]);
        this.naturalesTotales.set(respuesta.totales);
        this.naturalesPaginas.set(respuesta.totalPaginas);
        this.naturalesTotalRegistros.set(respuesta.totalRegistros);
      } else {
        this.empleadosRaw.set(respuesta.items as CoincidenciaEmpleado[]);
        this.empleadosTotales.set(respuesta.totales);
        this.empleadosPaginas.set(respuesta.totalPaginas);
        this.empleadosTotalRegistros.set(respuesta.totalRegistros);
      }
      this.contextoMonitoreoEnCarga = null;
      this.contextoMonitoreoCargado = contexto;
      this.errorMonitoreo.set(null);
      this.paginaActual.set(respuesta.pagina);
      this.cargando.set(false);
    });
  }

  private consultarMonitoreo(tipo: FiltroTipo, consulta: ConsultaMonitoreoPaginada): Observable<RespuestaMonitoreo['respuesta']> {
    if (tipo === 'juridica') return this.listasService.getJuridicasPaginadas(consulta);
    if (tipo === 'natural') return this.listasService.getNaturalesPaginadas(consulta);
    return this.listasService.getEmpleadosPaginadas(consulta);
  }

  datosFiltrados = computed(() => this.datosActivos());

  private coincideTexto(item: unknown, query: string, valores: Array<string | null | undefined>): boolean {
    if (!query) return true;
    return valores.some(valor => (valor || '').toLowerCase().includes(query));
  }

  private coincideEstado(item: { tieneMotivo?: boolean; esManual?: boolean }, estado: FiltroEstado): boolean {
    const cerradoPasivo = this.esCerradoPasivo(item);
    if (estado === 'todos') return true;
    if (estado === 'activos') return !cerradoPasivo;
    if (estado === 'cerrado_pasivo') return cerradoPasivo;
    if (cerradoPasivo) return false;
    if (estado === 'con_motivo') return !!item.tieneMotivo;
    if (estado === 'pendiente') return !item.tieneMotivo;
    if (estado === 'manual') return !!item.esManual;
    return true;
  }

  private esCerradoPasivo(item: unknown): boolean {
    const estado = String(
      (item as { estadoRegistro?: unknown; estado?: unknown; estatus?: unknown })?.estadoRegistro ??
      (item as { estado?: unknown })?.estado ??
      (item as { estatus?: unknown })?.estatus ??
      ''
    ).trim().toUpperCase();

    const activo = (item as { activo?: unknown; esActivo?: unknown })?.activo ?? (item as { esActivo?: unknown })?.esActivo;
    return estado === 'CERRADO' || estado === 'CERRADA' || estado === 'PASIVO' || estado === 'PASIVA' || estado === 'SUSPENDIDO' || estado === 'SUSPENDIDA' || activo === false || activo === 0;
  }

  private coincideFecha(item: { fechaEncontro?: string | null; fechaRegistroInterno?: string | null }, desde: string, hasta: string): boolean {
    if (!desde && !hasta) return true;

    const fechaBase = item.fechaEncontro || item.fechaRegistroInterno;
    if (!fechaBase) return false;

    const fecha = new Date(fechaBase);
    if (Number.isNaN(fecha.getTime())) return false;

    if (desde) {
      const min = new Date(`${desde}T00:00:00`);
      if (fecha < min) return false;
    }

    if (hasta) {
      const max = new Date(`${hasta}T23:59:59`);
      if (fecha > max) return false;
    }

    return true;
  }

  obtenerEstadoMonitoreo(item: { tieneMotivo?: boolean; esManual?: boolean }): string {
    if (this.esCerradoPasivo(item)) return 'Cerrado / Pasivo';
    return item.tieneMotivo ? 'Con motivo' : 'Pendiente';
  }

  obtenerEstadoMonitoreoReporte(item: { tieneMotivo?: boolean; esManual?: boolean }): string {
    const estado = this.obtenerEstadoMonitoreo(item);
    if (estado === 'Con motivo') return 'CON MOTIVO REGISTRADO';
    if (estado === 'Cerrado / Pasivo') return 'CERRADO / PASIVO';
    return 'PENDIENTE DE REGISTRO';
  }

  obtenerClaseEstado(item: { tieneMotivo?: boolean; esManual?: boolean }): string {
    if (this.esCerradoPasivo(item))
      return 'inline-flex px-2.5 py-1 rounded-md text-xs font-semibold bg-slate-100 text-slate-700 ring-1 ring-slate-300';

    return item.tieneMotivo
      ? 'inline-flex px-2.5 py-1 rounded-md text-xs font-semibold bg-emerald-50 text-emerald-700 ring-1 ring-emerald-600/10'
      : 'inline-flex px-2.5 py-1 rounded-md text-xs font-semibold bg-amber-50 text-amber-700 ring-1 ring-amber-600/10';
  }

  puedeDarSeguimiento(item: { tieneMotivo?: boolean; esManual?: boolean; estadoIdentidad?: string; estadoAsociacion?: string }): boolean {
    return this.obtenerEstadoMonitoreo(item) === 'Con motivo';
  }

  obtenerTextoAccionSeguimiento(item: { tieneMotivo?: boolean; esManual?: boolean; estadoIdentidad?: string; estadoAsociacion?: string }): string {
    return this.obtenerEstadoMonitoreo(item) === 'Con motivo'
      ? 'Dar seguimiento'
      : 'Debe registrar un motivo antes del seguimiento';
  }

  private esIdentidadOperable(item: { estadoIdentidad?: string; estadoAsociacion?: string }): boolean {
    return item.estadoIdentidad === 'VERIFIED' && item.estadoAsociacion === 'VERIFIED';
  }

  obtenerEtiquetaIdentidad(item: { estadoIdentidad?: string; estadoAsociacion?: string; categoriaIdentidad?: string }): string {
    if (item.categoriaIdentidad === 'SIN_IDENTIFICACION') return 'Sin identificación atribuible';
    if (item.estadoIdentidad === 'VERIFIED' && item.estadoAsociacion === 'VERIFIED') return 'Identidad verificada';
    if (item.estadoIdentidad === 'VERIFIED' && item.estadoAsociacion === 'AMBIGUOUS') return 'Asociación ambigua';
    if (item.estadoIdentidad === 'AMBIGUOUS' || item.estadoAsociacion === 'AMBIGUOUS') return 'Identidad ambigua';
    if (item.estadoIdentidad === 'INVALID' || item.estadoAsociacion === 'INVALID') return 'Sin identificación';
    return 'Identidad no verificada';
  }

  obtenerClaseIdentidad(item: { estadoIdentidad?: string; estadoAsociacion?: string }): string {
    if (item.estadoIdentidad === 'VERIFIED' && item.estadoAsociacion === 'VERIFIED')
      return 'mt-1 inline-flex px-2 py-0.5 rounded-md text-[10px] font-semibold bg-emerald-50 text-emerald-700 ring-1 ring-emerald-600/10';
    if (item.estadoIdentidad === 'AMBIGUOUS' || item.estadoAsociacion === 'AMBIGUOUS')
      return 'mt-1 inline-flex px-2 py-0.5 rounded-md text-[10px] font-semibold bg-orange-50 text-orange-700 ring-1 ring-orange-600/10';
    return 'mt-1 inline-flex px-2 py-0.5 rounded-md text-[10px] font-semibold bg-slate-50 text-slate-600 ring-1 ring-slate-300';
  }

  private mensajeErrorApi(err: any, fallback: string): string {
    const codigo = err?.error?.codigo;
    if (codigo === 'AMBIGUOUS_IDENTITY') return 'No es posible identificar inequívocamente este registro. La operación quedó bloqueada.';
    if (codigo === 'UNVERIFIED_ASSOCIATION') return 'La asociación encontrada no está verificada. La operación quedó bloqueada.';
    return err?.error?.mensaje || err?.error?.message || fallback;
  }

  private notificar(title: string, message: string, icon: 'success' | 'error' | 'warning' | 'info', confirmIcon: 'confirm' | 'cancel' | 'view' | 'download' = 'confirm'): void {
    void this.notification.notify({
      title,
      message,
      icon,
      confirmIcon,
      confirmVariant: icon === 'error' ? 'danger' : icon === 'success' ? 'success' : 'primary'
    });
  }

  private mostrarCarga(title: string, message: string): void {
    void this.notification.showLoading(title, message);
  }

  private bloquearIdentidad(item: { estadoIdentidad?: string; estadoAsociacion?: string }): boolean {
    if (this.esIdentidadOperable(item)) return false;
    this.notificar(
      'Operación bloqueada',
      this.mensajeErrorApi({ error: { codigo: item.estadoIdentidad === 'AMBIGUOUS' ? 'AMBIGUOUS_IDENTITY' : 'UNVERIFIED_ASSOCIATION' } }, 'La identidad no está verificada.'),
      'warning',
      'view'
    );
    return true;
  }

  // Datos paginados reactivos por tipo
  juridicasPaginadas = computed(() => {
    if (this.tipoActivo() !== 'juridica') return [];
    return this.juridicasRaw();
  });

  naturalesPaginadas = computed(() => {
    if (this.tipoActivo() !== 'natural') return [];
    return this.naturalesRaw();
  });

  empleadosPaginadas = computed(() => {
    if (this.tipoActivo() !== 'empleado') return [];
    return this.empleadosRaw();
  });

  paginasTotales = computed(() => {
    if (this.tipoActivo() === 'juridica') return this.juridicasPaginas();
    if (this.tipoActivo() === 'natural') return this.naturalesPaginas();
    return this.empleadosPaginas();
  });

  abrirDetalle(row: CoincidenciaNatural) {
    const solicitud = ++this.detalleSolicitud;
    this.personaSeleccionada.set(row);
    this.modalDetalleAbierto.set(true);
    this.detalleCargando.set(true);
    this.detalleError.set(null);
    this.listasService.getDetalleNatural(row.numeroIdentificacion, 'local', row.idRegistroFuente, row.tipoFuente).subscribe({
      next: (res) => {
        if (solicitud !== this.detalleSolicitud) return;
        this.detallesNatural.set(res);
        this.detalleCargando.set(false);
      },
      error: (err) => {
        if (solicitud !== this.detalleSolicitud) return;
        this.detalleError.set(this.mensajeErrorApi(err, 'No se pudo cargar el detalle de la coincidencia.'));
        this.detalleCargando.set(false);
      }
    });
  }

  abrirDetalleEmpleado(row: CoincidenciaEmpleado) {
    const solicitud = ++this.detalleSolicitud;
    this.personaSeleccionadaEmpleado.set(row);
    this.modalDetalleAbierto.set(true);
    this.detalleCargando.set(true);
    this.detalleError.set(null);
    this.listasService.getDetalleEmpleado(row.identidad, 'local', row.idRegistroFuente, row.tipoFuente, this.periodoPlanilla() ? `${this.periodoPlanilla()}-01` : undefined).subscribe({
      next: (res) => {
        if (solicitud !== this.detalleSolicitud) return;
        this.detallesEmpleado.set(res);
        this.detalleCargando.set(false);
      },
      error: (err) => {
        if (solicitud !== this.detalleSolicitud) return;
        this.detalleError.set(this.mensajeErrorApi(err, 'No se pudo cargar el detalle de la coincidencia.'));
        this.detalleCargando.set(false);
      }
    });
  }

  cerrarModal() {
    ++this.detalleSolicitud;
    this.modalDetalleAbierto.set(false);
    this.personaSeleccionada.set(null);
    this.personaSeleccionadaEmpleado.set(null);
    this.detallesNatural.set([]);
    this.detallesEmpleado.set([]);
    this.detalleError.set(null);
  }

  // Normaliza la lectura del sujeto mostrado en el modal de detalle, sin mezclar reglas entre natural y empleado.
  detalleSujetoActual(): CoincidenciaNatural | CoincidenciaEmpleado | null {
    return this.tipoActivo() === 'empleado'
      ? this.personaSeleccionadaEmpleado()
      : this.personaSeleccionada();
  }

  detalleSujetoDocumento(): string {
    const sujeto = this.detalleSujetoActual();
    if (!sujeto) return 'N/D';
    return this.tipoActivo() === 'empleado'
      ? (sujeto as CoincidenciaEmpleado).identidad || 'N/D'
      : (sujeto as CoincidenciaNatural).numeroIdentificacion || 'N/D';
  }

  detalleSujetoFecha(): string {
    return this.detalleSujetoActual()?.fechaEncontro || '';
  }

  mostrarNombreVarianteDetalle(nombreDetalle?: string | null): string | null {
    const limpio = (nombreDetalle || '').replace(/\s+/g, ' ').trim();
    if (!limpio) return null;
    const sujeto = (this.detalleSujetoActual()?.nombre || '').replace(/\s+/g, ' ').trim();
    return limpio.toUpperCase() !== sujeto.toUpperCase() ? limpio : null;
  }

  verPdf() {
    const isEmpleado = this.tipoActivo() === 'empleado';
    const personaNatural = this.personaSeleccionada();
    const personaEmpleado = this.personaSeleccionadaEmpleado();
    
    if (isEmpleado ? !personaEmpleado : !personaNatural) return;

    const doc = new jsPDF({
      orientation: 'portrait',
      unit: 'mm',
      format: 'a4'
    });

    const headerStartY = this.agregarEncabezadoPdf(
      doc,
      isEmpleado
        ? 'REPORTE DETALLADO DE COINCIDENCIAS - EMPLEADO IHSS'
        : 'REPORTE DETALLADO DE COINCIDENCIAS - PERSONA NATURAL'
    );

    // Información de la persona
    doc.setFont('helvetica', 'bold');
    doc.setFontSize(10);
    doc.setTextColor(...COLORES_REPORTE_INSTITUCIONAL.body);
    const identityStartY = headerStartY;
    doc.text('Nombre:', 14, identityStartY);
    doc.setFont('helvetica', 'normal');
    doc.text(isEmpleado ? personaEmpleado!.nombre : personaNatural!.nombre, 32, identityStartY);

    doc.setFont('helvetica', 'bold');
    doc.text(isEmpleado ? 'Identidad:' : 'DNI:', 14, identityStartY + 6);
    doc.setFont('helvetica', 'normal');
    doc.text(isEmpleado ? personaEmpleado!.identidad : personaNatural!.numeroIdentificacion, 32, identityStartY + 6);

    // Tabla de coincidencias
    let tableHead: string[][];
    let tableBody: string[][];
    let colStyles: any;

    if (isEmpleado) {
      tableHead = [['Condición Actúa', 'Nro Patronal', 'Empresa', 'Razón Social', 'Lista', 'Fecha Coincidencia', 'Fecha Calificación']];
      tableBody = this.detallesEmpleado().map(det => [
        det.tipoCondicionActuaDesc,
        det.numeroPatrono,
        det.nombreEmpresa,
        det.razoSoci,
        det.listaCoincidencia,
        det.fechaCoincidencia ? this.formatDate(det.fechaCoincidencia) : '',
        det.fechaCalifico ? this.formatDate(det.fechaCalifico) : ''
      ]);
      colStyles = {
        2: { cellWidth: 35 },
        3: { cellWidth: 35 }
      };
    } else {
      tableHead = [['Condición Actúa', 'Nro Patronal', 'Empresa', 'Es PEP', 'Lista', 'Fecha Coincidencia', 'Fecha Calificación']];
      tableBody = this.detallesNatural().map(det => [
        det.tipoCondicionActuaDesc,
        det.numeroPatronal,
        det.nombreEmpresa,
        (det.esPep === 'SI' || det.esPep === 'S') ? 'SÍ' : 'NO',
        det.listaCoincidencia,
        det.fechaCoincidencia ? this.formatDate(det.fechaCoincidencia) : '',
        det.fechaCalifico ? this.formatDate(det.fechaCalifico) : ''
      ]);
      colStyles = {
        2: { cellWidth: 50 }
      };
    }

    // El encabezado institucional puede crecer por el logo configurado y por
    // títulos multilínea. La tabla debe iniciar después de la identidad, no
    // en una coordenada fija que pueda invadir el encabezado.
    const tableStartY = identityStartY + 16;
    autoTableInstitucional(doc, {
      startY: tableStartY,
      head: tableHead,
      body: tableBody,
      headStyles: {
        fillColor: getInstitutionalReportBranding().headerColor,
        textColor: getInstitutionalReportBranding().headerTextColor,
        fontSize: 8,
        fontStyle: 'bold'
      },
      bodyStyles: {
        fontSize: 8,
        textColor: COLORES_REPORTE_INSTITUCIONAL.body
      },
      columnStyles: colStyles,
      theme: 'striped',
      margin: { top: tableStartY },
      didParseCell: (data: any) => {
        if (data.row.section === 'body') {
          if (!isEmpleado) {
            // Columna Es PEP
            if (data.column.index === 3) {
              const rawVal = (data.row.raw as any)[3];
              if (rawVal === 'SÍ') {
                data.cell.styles.fillColor = [254, 226, 226]; // bg-red-100
                data.cell.styles.textColor = [153, 27, 27]; // text-red-800
                data.cell.styles.fontStyle = 'bold';
              } else {
                data.cell.styles.fillColor = [243, 244, 246]; // bg-gray-100
                data.cell.styles.textColor = [75, 85, 99]; // text-gray-600
              }
            }
            // Columna Lista
            if (data.column.index === 4) {
              data.cell.styles.fillColor = [254, 242, 242]; // bg-red-50
              data.cell.styles.textColor = [185, 28, 28]; // text-red-700
              data.cell.styles.fontStyle = 'bold';
            }
          } else {
            // Columna Lista
            if (data.column.index === 4) {
              data.cell.styles.fillColor = [254, 242, 242]; // bg-red-50
              data.cell.styles.textColor = [185, 28, 28]; // text-red-700
              data.cell.styles.fontStyle = 'bold';
            }
          }
        }
      }
    });

    // Resumen
    const finalY = (doc as any).lastAutoTable.finalY + 10;
    doc.setDrawColor(229, 231, 235);
    doc.line(14, finalY, 196, finalY);

    doc.setFont('helvetica', 'bold');
    doc.setFontSize(10);
    doc.text('Resumen del Reporte:', 14, finalY + 8);
    
    doc.setFont('helvetica', 'normal');
    doc.setTextColor(...COLORES_REPORTE_INSTITUCIONAL.body);
    doc.text(`Total de Coincidencias: ${this.totalCoincidencias()}`, 14, finalY + 14);
    
    if (!isEmpleado) {
      if (this.coincidenciasPep() > 0) {
        doc.setFont('helvetica', 'bold');
        doc.setTextColor(185, 28, 28); // red-700
        doc.text(`Coincidencias PEP: ${this.coincidenciasPep()}`, 14, finalY + 20);
        doc.setFont('helvetica', 'normal');
        doc.setTextColor(...COLORES_REPORTE_INSTITUCIONAL.body);
      } else {
        doc.text(`Coincidencias PEP: ${this.coincidenciasPep()}`, 14, finalY + 20);
      }
      doc.text(`Empresas Relacionadas: ${this.empresasUnicas()}`, 14, finalY + 26);
    } else {
      doc.text(`Empresas Relacionadas: ${this.empresasUnicas()}`, 14, finalY + 20);
    }

    // Generar Blob y abrir modal de visualización
    agregarPiesInstitucionalesPdf(doc);
    const blob = doc.output('blob');
    const tipo = isEmpleado ? 'empleado' : 'persona-natural';
    this.reportPreview.openPdf(
      blob,
      `Reporte_Detallado_Coincidencias_${tipo}.pdf`,
      'Vista previa del reporte detallado de coincidencias',
      'Revise el documento completo antes de descargarlo.'
    );
  }

  formatDate(dateStr: string): string {
    const date = new Date(dateStr);
    const day = String(date.getDate()).padStart(2, '0');
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const year = date.getFullYear();
    return `${day}/${month}/${year}`;
  }

  private formatDateOrNd(dateStr?: string | null): string {
    return dateStr ? this.formatDate(dateStr) : 'N/D';
  }

  private obtenerEtiquetaOrigenRegistro(origen?: string | null): string {
    return this.origenesRegistro.find(item => item.valor === origen)?.etiqueta || 'N/D';
  }

  // Fecha propia del alta en Riesgo Lavado; no reemplaza la fecha de coincidencia de DNP.
  private obtenerFechaRegistroInterno(rowFecha?: string | null, positivo?: { fechaRegistroInterno?: string | null } | null): string | null {
    return positivo?.fechaRegistroInterno || rowFecha || null;
  }

  private obtenerRangoSeguimientoReporte(noDocumento?: string): RangoSeguimientoReporte {
    const usaRangoAplicado = !noDocumento || this.reporteSeguimientoDocumento() === noDocumento;
    const desde = usaRangoAplicado ? this.reporteSeguimientoDesde() : '';
    const hasta = usaRangoAplicado ? this.reporteSeguimientoHasta() : '';

    if (desde && hasta) {
      return { desde, hasta, texto: `${this.formatDate(desde)} al ${this.formatDate(hasta)}` };
    }
    if (desde) {
      return { desde, texto: `Desde ${this.formatDate(desde)}` };
    }
    if (hasta) {
      return { hasta, texto: `Hasta ${this.formatDate(hasta)}` };
    }
    return { texto: 'Todos los seguimientos registrados' };
  }

  private agregarEncabezadoPdf(doc: jsPDF, titulo: string): number {
    const institucion = this.configService.configSistema()?.nombreInstitucion || 'Instituto Hondureño de Seguridad Social';
    const sistema = this.configService.configSistema()?.nombreSistema || 'SGRLA-IHSS';
    return agregarEncabezadoInstitucionalPdf(doc, titulo, institucion, sistema);
  }

  // Encabezado exclusivo del reporte principal en orientación horizontal; evita reutilizar el formato de ficha individual.
  private agregarEncabezadoReporteMonitoreoPdf(doc: jsPDF, titulo: string): number {
    const institucion = this.configService.configSistema()?.nombreInstitucion || 'Instituto Hondureño de Seguridad Social';
    const sistema = this.configService.configSistema()?.nombreSistema || 'SGRLA-IHSS';
    return agregarEncabezadoInstitucionalPdf(doc, titulo, institucion, sistema, this.obtenerResumenFiltrosPrincipales());
  }

  private agregarDatosMemo(doc: jsPDF, y: number, titulo: string, generalData: string[][]): number {
    y = asegurarEspacioSeccionPdf(doc, y, 30);
    doc.setFont('helvetica', 'bold');
    doc.setFontSize(12);
    doc.setTextColor(...COLORES_REPORTE_INSTITUCIONAL.body);
    doc.text(titulo, 14, y);
    y += 4;
    doc.setDrawColor(226, 232, 240);
    doc.line(14, y, 196, y);
    y += 6;

    autoTableInstitucional(doc, {
      startY: y,
      body: generalData,
      theme: 'plain',
      styles: {
        fontSize: 9,
        cellPadding: 2,
        textColor: COLORES_REPORTE_INSTITUCIONAL.body
      },
      columnStyles: {
        0: { fontStyle: 'bold', cellWidth: 40, textColor: COLORES_REPORTE_INSTITUCIONAL.body },
        1: { cellWidth: 55 },
        2: { fontStyle: 'bold', cellWidth: 45, textColor: COLORES_REPORTE_INSTITUCIONAL.body },
        3: { cellWidth: 50 }
      },
      margin: { left: 14, right: 14 }
    });

    return (doc as any).lastAutoTable.finalY + 10;
  }

  private agregarMotivoPdf(doc: jsPDF, y: number, titulo: string, motivoTexto: string): number {
    y = asegurarEspacioSeccionPdf(doc, y, 30);
    doc.setFont('helvetica', 'bold');
    doc.setFontSize(12);
    doc.setTextColor(...COLORES_REPORTE_INSTITUCIONAL.body);
    doc.text(titulo, 14, y);
    y += 4;
    doc.line(14, y, 196, y);
    y += 6;

    doc.setFont('helvetica', 'normal');
    doc.setFontSize(9.5);
    doc.setTextColor(...COLORES_REPORTE_INSTITUCIONAL.body);

    const splitMotivo = doc.splitTextToSize(motivoTexto, 180);
    doc.text(splitMotivo, 14, y);
    return y + (splitMotivo.length * 5) + 10;
  }

  private agregarSeguimientosPdf(doc: jsPDF, y: number, titulo: string, seguimientos: Seguimiento[], mensajeVacio: string, rango: RangoSeguimientoReporte): number {
    y = asegurarEspacioSeccionPdf(doc, y, 30);
    doc.setFont('helvetica', 'bold');
    doc.setFontSize(12);
    doc.setTextColor(...COLORES_REPORTE_INSTITUCIONAL.body);
    doc.text(titulo, 14, y);
    y += 4;
    doc.line(14, y, 196, y);
    y += 6;

    doc.setFont('helvetica', 'bold');
    doc.setFontSize(9);
    doc.setTextColor(...COLORES_REPORTE_INSTITUCIONAL.body);
    doc.text(`Rango de seguimientos: ${rango.texto}`, 14, y);
    y += 6;

    if (seguimientos && seguimientos.length > 0) {
      const seguimientosRows = seguimientos.map(seg => {
        const evidenciasTexto = seg.evidencias && seg.evidencias.length > 0
          ? seg.evidencias.map(e => e.nombreArchivo).join('\n')
          : 'Sin evidencias';
        return [
          this.formatDate(seg.fechaCreacion),
          seg.usrEmail || 'Sistema',
          seg.motivoIngreso || '',
          evidenciasTexto
        ];
      });

      autoTableInstitucional(doc, {
        startY: y,
        head: [['Fecha', 'Usuario', 'Comentario / Acción', 'Evidencias']],
        body: seguimientosRows,
        headStyles: {
          fillColor: getInstitutionalReportBranding().headerColor,
          textColor: getInstitutionalReportBranding().headerTextColor,
          fontSize: 8.5,
          fontStyle: 'bold'
        },
        bodyStyles: {
          fontSize: 8,
          textColor: COLORES_REPORTE_INSTITUCIONAL.body
        },
        columnStyles: {
          0: { cellWidth: 25 },
          1: { cellWidth: 35 },
          2: { cellWidth: 80 },
          3: { cellWidth: 42 }
        },
        theme: 'striped',
        margin: { left: 14, right: 14 }
      });
      return (doc as any).lastAutoTable.finalY + 10;
    }

    doc.setFont('helvetica', 'italic');
    doc.setFontSize(9.5);
    doc.setTextColor(...COLORES_REPORTE_INSTITUCIONAL.muted);
    doc.text(mensajeVacio, 14, y);
    return y + 8;
  }

  private abrirPdf(doc: jsPDF, fileName: string, title: string) {
    agregarPiesInstitucionalesPdf(doc);
    this.reportPreview.openPdf(
      doc.output('blob'),
      fileName,
      title,
      'Revise el documento completo antes de descargarlo.'
    );
  }

  imprimirReportePatrono(row: CoincidenciaJuridica) {
    this.cargando.set(true);
    const rangoSeguimiento = this.obtenerRangoSeguimientoReporte(row.numeroPatrono);

    const obsPositivo = row.tieneMotivo
      ? this.listasService.getPositivoPorDocumento(row.numeroPatrono, 1)
      : of(null);

    const obsSeguimientos = row.tieneMotivo
      ? this.listasService.getSeguimientos(row.numeroPatrono, rangoSeguimiento.desde, rangoSeguimiento.hasta, 1)
      : of([]);

    forkJoin([obsPositivo, obsSeguimientos]).subscribe({
      next: ([positivo, seguimientos]) => {
        const auditoriaData = {
          rtn: row.rtn,
          nombre: row.nombre,
          numeroPatrono: row.numeroPatrono,
          listaCoincidencia: row.listaCoincidencia,
          tieneMotivo: row.tieneMotivo,
          tipoReporte: 'PDF_PERFIL_FICHA',
          rangoSeguimientoDesde: rangoSeguimiento.desde || null,
          rangoSeguimientoHasta: rangoSeguimiento.hasta || null,
          rangoSeguimientoTexto: rangoSeguimiento.texto,
          fechaGeneracion: new Date().toISOString()
        };

        this.listasService.registrarAuditoriaImpresion(row.numeroPatrono, auditoriaData).subscribe({
          next: () => {
            this.generarPdfPatrono(row, positivo, seguimientos, rangoSeguimiento);
            this.cargando.set(false);
          },
          error: (err) => {
            this.manejarErrorAuditoriaObligatoria(err, 'generación de reporte PDF');
          }
        });
      },
      error: (err) => {
        this.manejarErrorReporte(err, 'generación de reporte PDF');
      }
    });
  }

  imprimirReporteNatural(row: CoincidenciaNatural) {
    this.cargando.set(true);
    const esMarcador = !row.numeroIdentificacion || row.numeroIdentificacion.toUpperCase().startsWith('N/A') || row.categoriaIdentidad === 'SIN_IDENTIFICACION';
    const docNum = (esMarcador && row.idRegistroFuente)
      ? `SRC:${row.idRegistroFuente}`
      : row.numeroIdentificacion;
    const rangoSeguimiento = this.obtenerRangoSeguimientoReporte(docNum);

    const obsDetalles = this.listasService.getDetalleNatural(row.numeroIdentificacion, 'global', row.idRegistroFuente, row.tipoFuente);
    const obsPositivo = row.tieneMotivo
      ? this.listasService.getPositivoPorDocumento(docNum, 2, row.idRegistroFuente)
      : of(null);
    const obsSeguimientos = row.tieneMotivo
      ? this.listasService.getSeguimientos(docNum, rangoSeguimiento.desde, rangoSeguimiento.hasta, 2, row.idRegistroFuente)
      : of([]);

    forkJoin([obsDetalles, obsPositivo, obsSeguimientos]).subscribe({
      next: ([detalles, positivo, seguimientos]) => {
        const auditoriaData = {
          numeroIdentificacion: row.numeroIdentificacion,
          nombre: row.nombre,
          listaCoincidencia: row.listaCoincidencia,
          totalRepetidos: row.totalRepetidos,
          tipoReporte: 'PDF_PERFIL_FICHA',
          rangoSeguimientoDesde: rangoSeguimiento.desde || null,
          rangoSeguimientoHasta: rangoSeguimiento.hasta || null,
          rangoSeguimientoTexto: rangoSeguimiento.texto,
          fechaGeneracion: new Date().toISOString()
        };

        this.listasService.registrarAuditoriaImpresion(row.numeroIdentificacion, auditoriaData).subscribe({
          next: () => {
            this.generarPdfNatural(row, detalles, positivo, seguimientos, rangoSeguimiento);
            this.cargando.set(false);
          },
          error: (err) => {
            this.manejarErrorAuditoriaObligatoria(err, 'generación de reporte PDF');
          }
        });
      },
      error: (err) => {
        this.manejarErrorReporte(err, 'generación de reporte PDF');
      }
    });
  }

  generarPdfNatural(row: CoincidenciaNatural, detalles: DetalleCoincidenciaNatural[], positivo: any, seguimientos: Seguimiento[], rangoSeguimiento: RangoSeguimientoReporte) {
    const doc = new jsPDF({
      orientation: 'portrait',
      unit: 'mm',
      format: 'a4'
    });

    let y = this.agregarEncabezadoPdf(doc, 'REPORTE INTEGRAL DE PERSONA NATURAL');

    // Información General
    doc.setFont('helvetica', 'bold');
    doc.setFontSize(12);
    doc.setTextColor(...COLORES_REPORTE_INSTITUCIONAL.body);
    doc.text('1. INFORMACIÓN GENERAL DE LA PERSONA', 14, y);
    y += 4;
    doc.setDrawColor(226, 232, 240);
    doc.line(14, y, 196, y);
    y += 6;

    const generalData = [
      ['DNI / Identificación:', row.numeroIdentificacion || 'N/D', 'Nombre Completo:', row.nombre || 'N/D'],
      ['Lista de Coincidencia:', row.listaCoincidencia || 'N/D', 'Total de Coincidencias:', String(row.totalRepetidos || 0)],
      ['Estado Monitoreo:', this.obtenerEstadoMonitoreoReporte(row), 'Registro Interno:', this.formatDateOrNd(positivo?.fechaRegistroInterno)],
      ['Origen del Registro:', this.obtenerEtiquetaOrigenRegistro(positivo?.origenRegistro), '', '']
    ];

    autoTableInstitucional(doc, {
      startY: y,
      body: generalData,
      theme: 'plain',
      styles: {
        fontSize: 9,
        cellPadding: 2,
        textColor: COLORES_REPORTE_INSTITUCIONAL.body
      },
      columnStyles: {
        0: { fontStyle: 'bold', cellWidth: 40, textColor: COLORES_REPORTE_INSTITUCIONAL.body },
        1: { cellWidth: 55 },
        2: { fontStyle: 'bold', cellWidth: 45, textColor: COLORES_REPORTE_INSTITUCIONAL.body },
        3: { cellWidth: 50 }
      },
      margin: { left: 14, right: 14 }
    });

    y = (doc as any).lastAutoTable.finalY + 10;

    // Sección 2: Motivo de ingreso a la lista
    doc.setFont('helvetica', 'bold');
    doc.setFontSize(12);
    doc.setTextColor(...COLORES_REPORTE_INSTITUCIONAL.body);
    doc.text('2. MOTIVO DE INGRESO A LISTA DE MONITOREO', 14, y);
    y += 4;
    doc.line(14, y, 196, y);
    y += 6;

    const motivoTexto = positivo?.motivoIngreso || 'No se ha registrado un motivo de ingreso inicial en el sistema para esta persona.';
    doc.setFont('helvetica', 'normal');
    doc.setFontSize(9.5);
    doc.setTextColor(...COLORES_REPORTE_INSTITUCIONAL.body);
    
    const splitMotivo = doc.splitTextToSize(motivoTexto, 180);
    doc.text(splitMotivo, 14, y);
    y += (splitMotivo.length * 5) + 10;

    // Sección 3: Detalle de Coincidencias
    doc.setFont('helvetica', 'bold');
    doc.setFontSize(12);
    doc.setTextColor(...COLORES_REPORTE_INSTITUCIONAL.body);
    doc.text('3. DETALLE DE COINCIDENCIAS ENCONTRADAS', 14, y);
    y += 4;
    doc.line(14, y, 196, y);
    y += 6;

    const tableHead = [['Condición Actúa', 'Nro Patronal', 'Empresa', 'Es PEP', 'Lista', 'Fecha Coincidencia', 'Fecha Calificación']];
    const tableBody = detalles.map(det => [
      det.tipoCondicionActuaDesc,
      det.numeroPatronal,
      det.nombreEmpresa,
      (det.esPep === 'SI' || det.esPep === 'S') ? 'SÍ' : 'NO',
      det.listaCoincidencia,
      det.fechaCoincidencia ? this.formatDate(det.fechaCoincidencia) : '',
      det.fechaCalifico ? this.formatDate(det.fechaCalifico) : ''
    ]);

    autoTableInstitucional(doc, {
      startY: y,
      head: tableHead,
      body: tableBody,
      headStyles: {
        fillColor: getInstitutionalReportBranding().headerColor,
        textColor: getInstitutionalReportBranding().headerTextColor,
        fontSize: 8,
        fontStyle: 'bold'
      },
      bodyStyles: {
        fontSize: 8,
        textColor: COLORES_REPORTE_INSTITUCIONAL.body
      },
      columnStyles: {
        2: { cellWidth: 50 }
      },
      theme: 'striped',
      margin: { left: 14, right: 14 },
      didParseCell: (data: any) => {
        if (data.row.section === 'body') {
          if (data.column.index === 3) {
            const rawVal = (data.row.raw as any)[3];
            if (rawVal === 'SÍ') {
              data.cell.styles.fillColor = [254, 226, 226];
              data.cell.styles.textColor = [153, 27, 27];
              data.cell.styles.fontStyle = 'bold';
            } else {
              data.cell.styles.fillColor = [243, 244, 246];
              data.cell.styles.textColor = [75, 85, 99];
            }
          }
          if (data.column.index === 4) {
            data.cell.styles.fillColor = [254, 242, 242];
            data.cell.styles.textColor = [185, 28, 28];
            data.cell.styles.fontStyle = 'bold';
          }
        }
      }
    });

    y = (doc as any).lastAutoTable.finalY + 10;

    this.agregarSeguimientosPdf(
      doc,
      y,
      '4. HISTORIAL DE SEGUIMIENTOS Y EVIDENCIAS',
      seguimientos,
      'No se registran acciones de seguimiento ni evidencias adicionales para esta persona.',
      rangoSeguimiento
    );

    this.abrirPdf(doc, `Reporte_Integral_Persona_Natural_${row.numeroIdentificacion}.pdf`, 'Vista previa del reporte integral de persona natural');
  }

  imprimirReporteEmpleado(row: CoincidenciaEmpleado) {
    this.cargando.set(true);
    const esMarcador = !row.identidad || row.identidad.toUpperCase().startsWith('N/A') || row.categoriaIdentidad === 'SIN_IDENTIFICACION';
    const docNum = (esMarcador && row.idRegistroFuente)
      ? `SRC:${row.idRegistroFuente}`
      : row.identidad;
    const rangoSeguimiento = this.obtenerRangoSeguimientoReporte(docNum);

    const obsDetalles = this.listasService.getDetalleEmpleado(row.identidad, 'global', row.idRegistroFuente, row.tipoFuente, this.periodoPlanilla() ? `${this.periodoPlanilla()}-01` : undefined);
    const obsPositivo = row.tieneMotivo
      ? this.listasService.getPositivoPorDocumento(docNum, 3, row.idRegistroFuente)
      : of(null);
    const obsSeguimientos = row.tieneMotivo
      ? this.listasService.getSeguimientos(docNum, rangoSeguimiento.desde, rangoSeguimiento.hasta, 3, row.idRegistroFuente)
      : of([]);

    forkJoin([obsDetalles, obsPositivo, obsSeguimientos]).subscribe({
      next: ([detalles, positivo, seguimientos]) => {
        const auditoriaData = {
          identidad: row.identidad,
          nombre: row.nombre,
          listaCoincidencia: row.listaCoincidencia,
          totalRepetidos: row.totalRepetidos,
          tipoReporte: 'PDF_PERFIL_FICHA',
          rangoSeguimientoDesde: rangoSeguimiento.desde || null,
          rangoSeguimientoHasta: rangoSeguimiento.hasta || null,
          rangoSeguimientoTexto: rangoSeguimiento.texto,
          fechaGeneracion: new Date().toISOString()
        };

        this.listasService.registrarAuditoriaImpresion(row.identidad, auditoriaData).subscribe({
          next: () => {
            this.generarPdfEmpleado(row, detalles, positivo, seguimientos, rangoSeguimiento);
            this.cargando.set(false);
          },
          error: (err) => {
            this.manejarErrorAuditoriaObligatoria(err, 'generación de reporte PDF');
          }
        });
      },
      error: (err) => {
        this.manejarErrorReporte(err, 'generación de reporte PDF');
      }
    });
  }

  generarPdfEmpleado(row: CoincidenciaEmpleado, detalles: DetalleCoincidenciaEmpleado[], positivo: any, seguimientos: Seguimiento[], rangoSeguimiento: RangoSeguimientoReporte) {
    const doc = new jsPDF({
      orientation: 'portrait',
      unit: 'mm',
      format: 'a4'
    });

    let y = this.agregarEncabezadoPdf(doc, 'REPORTE INTEGRAL DE EMPLEADO IHSS');

    // Información General
    doc.setFont('helvetica', 'bold');
    doc.setFontSize(12);
    doc.setTextColor(...COLORES_REPORTE_INSTITUCIONAL.body);
    doc.text('1. INFORMACIÓN GENERAL DEL EMPLEADO', 14, y);
    y += 4;
    doc.setDrawColor(226, 232, 240);
    doc.line(14, y, 196, y);
    y += 6;

    const generalData = [
      ['DNI / Identidad:', row.identidad || 'N/D', 'Nombre Completo:', row.nombre || 'N/D'],
      ['Lista de Coincidencia:', row.listaCoincidencia || 'N/D', 'Total de Coincidencias:', String(row.totalRepetidos || 0)],
      ['Estado Monitoreo:', this.obtenerEstadoMonitoreoReporte(row), 'Registro Interno:', this.formatDateOrNd(positivo?.fechaRegistroInterno)],
      ['Origen del Registro:', this.obtenerEtiquetaOrigenRegistro(positivo?.origenRegistro), '', '']
    ];

    autoTableInstitucional(doc, {
      startY: y,
      body: generalData,
      theme: 'plain',
      styles: {
        fontSize: 9,
        cellPadding: 2,
        textColor: COLORES_REPORTE_INSTITUCIONAL.body
      },
      columnStyles: {
        0: { fontStyle: 'bold', cellWidth: 40, textColor: COLORES_REPORTE_INSTITUCIONAL.body },
        1: { cellWidth: 55 },
        2: { fontStyle: 'bold', cellWidth: 45, textColor: COLORES_REPORTE_INSTITUCIONAL.body },
        3: { cellWidth: 50 }
      },
      margin: { left: 14, right: 14 }
    });

    y = (doc as any).lastAutoTable.finalY + 10;

    // Sección 2: Motivo de ingreso a la lista
    doc.setFont('helvetica', 'bold');
    doc.setFontSize(12);
    doc.setTextColor(...COLORES_REPORTE_INSTITUCIONAL.body);
    doc.text('2. MOTIVO DE INGRESO A LISTA DE MONITOREO', 14, y);
    y += 4;
    doc.line(14, y, 196, y);
    y += 6;

    const motivoTexto = positivo?.motivoIngreso || 'No se ha registrado un motivo de ingreso inicial en el sistema para este empleado.';
    doc.setFont('helvetica', 'normal');
    doc.setFontSize(9.5);
    doc.setTextColor(...COLORES_REPORTE_INSTITUCIONAL.body);
    
    const splitMotivo = doc.splitTextToSize(motivoTexto, 180);
    doc.text(splitMotivo, 14, y);
    y += (splitMotivo.length * 5) + 10;

    // Sección 3: Detalle de Coincidencias
    doc.setFont('helvetica', 'bold');
    doc.setFontSize(12);
    doc.setTextColor(...COLORES_REPORTE_INSTITUCIONAL.body);
    doc.text('3. DETALLE DE COINCIDENCIAS ENCONTRADAS', 14, y);
    y += 4;
    doc.line(14, y, 196, y);
    y += 6;

    const tableHead = [['Condición Actúa', 'Nro Patronal', 'Empresa', 'Razón Social', 'Lista', 'Fecha Coincidencia', 'Fecha Calificación']];
    const tableBody = detalles.map(det => [
      det.tipoCondicionActuaDesc,
      det.numeroPatrono,
      det.nombreEmpresa,
      det.razoSoci,
      det.listaCoincidencia,
      det.fechaCoincidencia ? this.formatDate(det.fechaCoincidencia) : '',
      det.fechaCalifico ? this.formatDate(det.fechaCalifico) : ''
    ]);

    autoTableInstitucional(doc, {
      startY: y,
      head: tableHead,
      body: tableBody,
      headStyles: {
        fillColor: getInstitutionalReportBranding().headerColor,
        textColor: getInstitutionalReportBranding().headerTextColor,
        fontSize: 8,
        fontStyle: 'bold'
      },
      bodyStyles: {
        fontSize: 8,
        textColor: COLORES_REPORTE_INSTITUCIONAL.body
      },
      columnStyles: {
        2: { cellWidth: 35 },
        3: { cellWidth: 35 }
      },
      theme: 'striped',
      margin: { left: 14, right: 14 },
      didParseCell: (data: any) => {
        if (data.row.section === 'body') {
          if (data.column.index === 4) {
            data.cell.styles.fillColor = [254, 242, 242];
            data.cell.styles.textColor = [185, 28, 28];
            data.cell.styles.fontStyle = 'bold';
          }
        }
      }
    });

    y = (doc as any).lastAutoTable.finalY + 10;

    this.agregarSeguimientosPdf(
      doc,
      y,
      '4. HISTORIAL DE SEGUIMIENTOS Y EVIDENCIAS',
      seguimientos,
      'No se registran acciones de seguimiento ni evidencias adicionales para este empleado.',
      rangoSeguimiento
    );

    this.abrirPdf(doc, `Reporte_Integral_Empleado_${row.identidad}.pdf`, 'Vista previa del reporte integral de empleado');
  }

  generarPdfPatrono(row: CoincidenciaJuridica, positivo: any, seguimientos: Seguimiento[], rangoSeguimiento: RangoSeguimientoReporte) {
    const doc = new jsPDF({
      orientation: 'portrait',
      unit: 'mm',
      format: 'a4'
    });

    let y = this.agregarEncabezadoPdf(doc, 'REPORTE INTEGRAL DE PATRONO');

    // Grid de Información General del Patrono
    doc.setFont('helvetica', 'bold');
    doc.setFontSize(12);
    doc.setTextColor(...COLORES_REPORTE_INSTITUCIONAL.body);
    doc.text('1. INFORMACIÓN GENERAL DEL PATRONO', 14, y);
    y += 4;
    doc.setDrawColor(226, 232, 240); // Slate 200
    doc.line(14, y, 196, y);
    y += 6;

    const generalData = [
      ['Número Patronal:', row.numeroPatrono || 'N/D', 'RTN:', row.rtn || 'N/D'],
      ['Nombre / Razón Social:', row.nombre || 'N/D', 'Proveedor IHSS:', row.esProveedorIhss || 'No'],
      ['Lista de Coincidencia:', row.listaCoincidencia || 'N/D', 'Estado Monitoreo:', this.obtenerEstadoMonitoreoReporte(row)],
      ['Fecha Coincidencia:', row.fechaEncontro ? this.formatDate(row.fechaEncontro) : 'N/D', 'Fecha Calificación:', row.fechaCalifico ? this.formatDate(row.fechaCalifico) : 'N/D'],
      ['Registro Interno:', this.formatDateOrNd(this.obtenerFechaRegistroInterno(row.fechaRegistroInterno, positivo)), 'Origen del Registro:', this.obtenerEtiquetaOrigenRegistro(positivo?.origenRegistro)]
    ];

    autoTableInstitucional(doc, {
      startY: y,
      body: generalData,
      theme: 'plain',
      styles: {
        fontSize: 9,
        cellPadding: 2,
        textColor: COLORES_REPORTE_INSTITUCIONAL.body
      },
      columnStyles: {
        0: { fontStyle: 'bold', cellWidth: 40, textColor: COLORES_REPORTE_INSTITUCIONAL.body },
        1: { cellWidth: 55 },
        2: { fontStyle: 'bold', cellWidth: 35, textColor: COLORES_REPORTE_INSTITUCIONAL.body },
        3: { cellWidth: 60 }
      },
      margin: { left: 14, right: 14 }
    });

    y = (doc as any).lastAutoTable.finalY + 10;

    // Sección 2: Clasificación y Motivo Inicial de Monitoreo
    doc.setFont('helvetica', 'bold');
    doc.setFontSize(12);
    doc.setTextColor(...COLORES_REPORTE_INSTITUCIONAL.body);
    doc.text('2. MOTIVO DE INGRESO A LISTA DE MONITOREO', 14, y);
    y += 4;
    doc.line(14, y, 196, y);
    y += 6;

    const motivoTexto = positivo?.motivoIngreso || 'No se ha registrado un motivo de ingreso inicial en el sistema para este patrono.';
    doc.setFont('helvetica', 'normal');
    doc.setFontSize(9.5);
    doc.setTextColor(...COLORES_REPORTE_INSTITUCIONAL.body);
    
    const splitMotivo = doc.splitTextToSize(motivoTexto, 180);
    doc.text(splitMotivo, 14, y);
    y += (splitMotivo.length * 5) + 10;

    this.agregarSeguimientosPdf(
      doc,
      y,
      '3. HISTORIAL DE SEGUIMIENTOS Y EVIDENCIAS',
      seguimientos,
      'No se registran acciones de seguimiento ni evidencias adicionales para este patrono.',
      rangoSeguimiento
    );

    this.abrirPdf(doc, `Reporte_Integral_Patrono_${row.numeroPatrono}.pdf`, 'Vista previa del reporte integral de patrono');
  }

  private ajustarColumnasExcel(ws: XLSX.WorkSheet, data: any[][]) {
    const maxLens = data.reduce((acc, row) => {
      row.forEach((val, colIdx) => {
        const len = val ? val.toString().length : 0;
        if (!acc[colIdx] || len > acc[colIdx]) {
          acc[colIdx] = len;
        }
      });
      return acc;
    }, [] as number[]);

    ws['!cols'] = maxLens.map(len => ({ wch: Math.min(Math.max(len + 2, 10), 45) }));
  }

  private agregarSeguimientosExcel(data: any[][], seguimientos: Seguimiento[], rangoSeguimiento: RangoSeguimientoReporte) {
    data.push([]);
    data.push(['Historial de Seguimientos y Evidencias']);
    data.push(['Rango de seguimientos:', rangoSeguimiento.texto]);
    data.push(['Fecha', 'Usuario', 'Comentario / Acción', 'Evidencias']);

    if (!seguimientos || seguimientos.length === 0) {
      data.push(['', '', 'No se registran acciones de seguimiento ni evidencias para el rango seleccionado.', '']);
      return;
    }

    seguimientos.forEach(seg => {
      data.push([
        seg.fechaCreacion ? this.formatDate(seg.fechaCreacion) : '',
        seg.usrEmail || 'Sistema',
        seg.motivoIngreso || '',
        seg.evidencias && seg.evidencias.length > 0
          ? seg.evidencias.map(e => e.nombreArchivo).join(', ')
          : 'Sin evidencias'
      ]);
    });
  }

  private escribirFichaExcel(
    data: any[][],
    sheetName: string,
    fileName: string,
    opciones: Partial<XLSX.WorkSheet> = {}
  ) {
    const ws = XLSX.utils.aoa_to_sheet(data);
    Object.assign(ws, opciones);
    this.ajustarColumnasExcel(ws, data);

    const wb = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(wb, ws, sheetName);
    void this.reportPreview.openExcel(
      wb,
      fileName,
      `Vista previa de ${sheetName.toLowerCase()}`,
      'Revise el contenido del reporte antes de descargar el archivo.'
    );
  }

  private manejarErrorAuditoriaObligatoria(err: unknown, operacion: string) {
    console.error(`No se pudo registrar auditoría de ${operacion}:`, err);
    this.cargando.set(false);
    this.notificar('Auditoría requerida', `No se pudo registrar la auditoría de ${operacion}. La operación fue cancelada.`, 'error', 'cancel');
  }

  public manejarErrorReporte(err: unknown, operacion: string): void {
    console.error(`No se pudo completar la ${operacion}:`, err);
    this.cargando.set(false);
    this.notificar('Reporte no generado', this.mensajeErrorApi(err, `No se pudo completar la ${operacion}. No se creó ningún archivo vacío.`), 'error', 'cancel');
  }

  private registrarAuditoriaFichaExcel(noDocumento: string, tipo: string, nombre: string, fileName: string, cantidadSeguimientos: number, rangoSeguimiento: RangoSeguimientoReporte) {
    return this.listasService.registrarAuditoriaExportacion(
      'RL_LISTA_POSITIVOS',
      noDocumento,
      'ExportacionFichaPerfil',
      {
        accion: 'EXPORTACION_EXCEL',
        tipoReporte: 'EXCEL_PERFIL_FICHA',
        tipo,
        nombre,
        cantidadSeguimientos,
        rangoSeguimientoDesde: rangoSeguimiento.desde || null,
        rangoSeguimientoHasta: rangoSeguimiento.hasta || null,
        rangoSeguimientoTexto: rangoSeguimiento.texto,
        archivo: fileName
      }
    );
  }

  exportarFichaExcelPatrono(row: CoincidenciaJuridica) {
    this.cargando.set(true);
    const rangoSeguimiento = this.obtenerRangoSeguimientoReporte(row.numeroPatrono);

    const obsPositivo = row.tieneMotivo
      ? this.listasService.getPositivoPorDocumento(row.numeroPatrono, 1)
      : of(null);
    const obsSeguimientos = row.tieneMotivo
      ? this.listasService.getSeguimientos(row.numeroPatrono, rangoSeguimiento.desde, rangoSeguimiento.hasta, 1)
      : of([]);

    forkJoin([obsPositivo, obsSeguimientos]).subscribe({
      next: ([positivo, seguimientos]) => {
        const data: any[][] = [
          ['Ficha de Perfil / Memorando'],
          [this.configService.configSistema()?.nombreInstitucion || 'Instituto Hondureño de Seguridad Social'],
          [`Fecha de Generación: ${new Date().toLocaleString()}`],
          [],
          ['Información General del Patrono'],
          ['Número Patronal:', row.numeroPatrono || 'N/D', 'RTN:', row.rtn || 'N/D'],
          ['Nombre / Razón Social:', row.nombre || 'N/D', 'Proveedor IHSS:', row.esProveedorIhss || 'No'],
          ['Lista de Coincidencia:', row.listaCoincidencia || 'N/D', 'Estado Monitoreo:', this.obtenerEstadoMonitoreoReporte(row)],
          ['Fecha Coincidencia:', row.fechaEncontro ? this.formatDate(row.fechaEncontro) : 'N/D', 'Fecha Calificación:', row.fechaCalifico ? this.formatDate(row.fechaCalifico) : 'N/D'],
          ['Registro Interno:', this.formatDateOrNd(this.obtenerFechaRegistroInterno(row.fechaRegistroInterno, positivo)), 'Origen del Registro:', this.obtenerEtiquetaOrigenRegistro(positivo?.origenRegistro)],
          [],
          ['Motivo de Ingreso a Lista de Monitoreo'],
          [positivo?.motivoIngreso || 'No se ha registrado un motivo de ingreso inicial en el sistema para este patrono.']
        ];

        this.agregarSeguimientosExcel(data, seguimientos, rangoSeguimiento);

        const fileName = `Ficha_Patrono_${row.numeroPatrono}.xlsx`;
        this.registrarAuditoriaFichaExcel(row.numeroPatrono, 'juridica', row.nombre, fileName, seguimientos.length, rangoSeguimiento).subscribe({
          next: () => {
            this.escribirFichaExcel(data, 'Ficha Patrono', fileName, {
              '!headerRows': [17],
              '!sectionRows': [5, 12, 15],
              '!keyValueRows': [6, 7, 8, 9, 10],
              '!paragraphRows': [13],
              '!autoFilterRow': 17
            });
            this.cargando.set(false);
          },
          error: err => this.manejarErrorAuditoriaObligatoria(err, 'exportación de ficha Excel')
        });
      },
      error: (err) => this.manejarErrorReporte(err, 'exportación de ficha Excel')
    });
  }

  exportarFichaExcelNatural(row: CoincidenciaNatural) {
    this.cargando.set(true);
    const esMarcador = !row.numeroIdentificacion || row.numeroIdentificacion.toUpperCase().startsWith('N/A') || row.categoriaIdentidad === 'SIN_IDENTIFICACION';
    const docNum = (esMarcador && row.idRegistroFuente)
      ? `SRC:${row.idRegistroFuente}`
      : row.numeroIdentificacion;
    const rangoSeguimiento = this.obtenerRangoSeguimientoReporte(docNum);

    const obsDetalles = this.listasService.getDetalleNatural(row.numeroIdentificacion, 'global', row.idRegistroFuente, row.tipoFuente);
    const obsPositivo = row.tieneMotivo
      ? this.listasService.getPositivoPorDocumento(docNum, 2, row.idRegistroFuente)
      : of(null);
    const obsSeguimientos = row.tieneMotivo
      ? this.listasService.getSeguimientos(docNum, rangoSeguimiento.desde, rangoSeguimiento.hasta, 2, row.idRegistroFuente)
      : of([]);

    forkJoin([obsDetalles, obsPositivo, obsSeguimientos]).subscribe({
      next: ([detalles, positivo, seguimientos]) => {
        const data: any[][] = [
          ['Ficha de Perfil / Memorando'],
          [this.configService.configSistema()?.nombreInstitucion || 'Instituto Hondureño de Seguridad Social'],
          [`Fecha de Generación: ${new Date().toLocaleString()}`],
          [],
          ['Información General de la Persona'],
          ['DNI / Identificación', row.numeroIdentificacion || 'N/D', 'Nombre Completo', row.nombre || 'N/D'],
          ['Lista Coincidencia', row.listaCoincidencia || 'N/D', 'Total de Coincidencias', String(row.totalRepetidos || 0)],
          ['Estado Monitoreo', this.obtenerEstadoMonitoreoReporte(row), 'Registro Interno', this.formatDateOrNd(positivo?.fechaRegistroInterno)],
          ['Origen del Registro', this.obtenerEtiquetaOrigenRegistro(positivo?.origenRegistro), '', ''],
          [],
          ['Motivo de Ingreso a Lista de Monitoreo'],
          [positivo?.motivoIngreso || 'No se ha registrado un motivo de ingreso inicial en el sistema para esta persona.'],
          [],
          ['Detalle de Coincidencias Encontradas'],
          ['Condición Actúa', 'Nro Patronal', 'Empresa', 'Es PEP', 'Lista', 'Fecha Coincidencia', 'Fecha Calificación']
        ];

        detalles.forEach(det => {
          data.push([
            det.tipoCondicionActuaDesc,
            det.numeroPatronal,
            det.nombreEmpresa,
            (det.esPep === 'SI' || det.esPep === 'S') ? 'SÍ' : 'NO',
            det.listaCoincidencia,
            det.fechaCoincidencia ? this.formatDate(det.fechaCoincidencia) : '',
            det.fechaCalifico ? this.formatDate(det.fechaCalifico) : ''
          ]);
        });

        this.agregarSeguimientosExcel(data, seguimientos, rangoSeguimiento);

        const fileName = `Ficha_Natural_${row.numeroIdentificacion}.xlsx`;
        this.registrarAuditoriaFichaExcel(row.numeroIdentificacion, 'natural', row.nombre, fileName, seguimientos.length, rangoSeguimiento).subscribe({
          next: () => {
            this.escribirFichaExcel(data, 'Ficha Natural', fileName);
            this.cargando.set(false);
          },
          error: err => this.manejarErrorAuditoriaObligatoria(err, 'exportación de ficha Excel')
        });
      },
      error: (err) => this.manejarErrorReporte(err, 'exportación de ficha Excel')
    });
  }

  exportarFichaExcelEmpleado(row: CoincidenciaEmpleado) {
    this.cargando.set(true);
    const esMarcador = !row.identidad || row.identidad.toUpperCase().startsWith('N/A') || row.categoriaIdentidad === 'SIN_IDENTIFICACION';
    const docNum = (esMarcador && row.idRegistroFuente)
      ? `SRC:${row.idRegistroFuente}`
      : row.identidad;
    const rangoSeguimiento = this.obtenerRangoSeguimientoReporte(docNum);

    const obsDetalles = this.listasService.getDetalleEmpleado(row.identidad, 'global', row.idRegistroFuente, row.tipoFuente, this.periodoPlanilla() ? `${this.periodoPlanilla()}-01` : undefined);
    const obsPositivo = row.tieneMotivo
      ? this.listasService.getPositivoPorDocumento(docNum, 3, row.idRegistroFuente)
      : of(null);
    const obsSeguimientos = row.tieneMotivo
      ? this.listasService.getSeguimientos(docNum, rangoSeguimiento.desde, rangoSeguimiento.hasta, 3, row.idRegistroFuente)
      : of([]);

    forkJoin([obsDetalles, obsPositivo, obsSeguimientos]).subscribe({
      next: ([detalles, positivo, seguimientos]) => {
        const data: any[][] = [
          ['Ficha de Perfil / Memorando'],
          [this.configService.configSistema()?.nombreInstitucion || 'Instituto Hondureño de Seguridad Social'],
          [`Fecha de Generación: ${new Date().toLocaleString()}`],
          [],
          ['Información General del Empleado'],
          ['DNI / Identidad', row.identidad || 'N/D', 'Nombre Completo', row.nombre || 'N/D'],
          ['Lista Coincidencia', row.listaCoincidencia || 'N/D', 'Total de Coincidencias', String(row.totalRepetidos || 0)],
          ['Estado Monitoreo', this.obtenerEstadoMonitoreoReporte(row), 'Registro Interno', this.formatDateOrNd(positivo?.fechaRegistroInterno)],
          ['Origen del Registro', this.obtenerEtiquetaOrigenRegistro(positivo?.origenRegistro), '', ''],
          [],
          ['Motivo de Ingreso a Lista de Monitoreo'],
          [positivo?.motivoIngreso || 'No se ha registrado un motivo de ingreso inicial en el sistema para este empleado.'],
          [],
          ['Detalle de Coincidencias Encontradas'],
          ['Condición Actúa', 'Nro Patronal', 'Empresa', 'Razón Social', 'Lista', 'Fecha Coincidencia', 'Fecha Calificación']
        ];

        detalles.forEach(det => {
          data.push([
            det.tipoCondicionActuaDesc,
            det.numeroPatrono,
            det.nombreEmpresa,
            det.razoSoci,
            det.listaCoincidencia,
            det.fechaCoincidencia ? this.formatDate(det.fechaCoincidencia) : '',
            det.fechaCalifico ? this.formatDate(det.fechaCalifico) : ''
          ]);
        });

        this.agregarSeguimientosExcel(data, seguimientos, rangoSeguimiento);

        const fileName = `Ficha_Empleado_${row.identidad}.xlsx`;
        this.registrarAuditoriaFichaExcel(row.identidad, 'empleado', row.nombre, fileName, seguimientos.length, rangoSeguimiento).subscribe({
          next: () => {
            this.escribirFichaExcel(data, 'Ficha Empleado', fileName);
            this.cargando.set(false);
          },
          error: err => this.manejarErrorAuditoriaObligatoria(err, 'exportación de ficha Excel')
        });
      },
      error: (err) => this.manejarErrorReporte(err, 'exportación de ficha Excel')
    });
  }

  exportarExcel() {
    if (this.tipoActivo() === 'empleado') {
      const persona = this.personaSeleccionadaEmpleado();
      if (!persona) return;

      const data = [
        ['Monitoreo de Listas de Riesgo'],
        [this.configService.configSistema()?.nombreInstitucion || 'Instituto Hondureño de Seguridad Social'],
        ['Reporte Detallado de Coincidencias - Empleado IHSS'],
        [],
        ['Nombre:', persona.nombre],
        ['Identidad:', persona.identidad],
        [],
        ['Condición Actúa', 'Nro Patronal', 'Empresa', 'Razón Social', 'Lista', 'Fecha Coincidencia', 'Fecha Calificación']
      ];

      this.detallesEmpleado().forEach(det => {
        data.push([
          det.tipoCondicionActuaDesc,
          det.numeroPatrono,
          det.nombreEmpresa,
          det.razoSoci,
          det.listaCoincidencia,
          det.fechaCoincidencia ? this.formatDate(det.fechaCoincidencia) : '',
          det.fechaCalifico ? this.formatDate(det.fechaCalifico) : ''
        ]);
      });

      data.push([]);
      data.push(['Resumen del Reporte']);
      data.push(['Total de Coincidencias', this.totalCoincidencias().toString()]);
      data.push(['Empresas Relacionadas', this.empresasUnicas().toString()]);

      const ws = XLSX.utils.aoa_to_sheet(data);
      
      // Auto-ajustar ancho de columnas básico
      const maxLens = data.reduce((acc, row) => {
        row.forEach((val, colIdx) => {
          const len = val ? val.toString().length : 0;
          if (!acc[colIdx] || len > acc[colIdx]) {
            acc[colIdx] = len;
          }
        });
        return acc;
      }, [] as number[]);
      ws['!cols'] = maxLens.map(len => ({ wch: Math.min(Math.max(len + 2, 10), 40) }));

      const wb = XLSX.utils.book_new();
      XLSX.utils.book_append_sheet(wb, ws, 'Detalle Coincidencias');

      const fileName = `Reporte_Coincidencias_Empleado_${persona.identidad}.xlsx`;
      this.listasService.registrarAuditoriaExportacion(
        'RL_LISTA_POSITIVOS',
        persona.identidad,
        'ExportacionMonitoreoListas',
        {
          accion: 'EXPORTACION_EXCEL',
          tipo: 'empleado',
          nombre: persona.nombre,
          cantidadRegistros: this.detallesEmpleado().length,
          archivo: fileName
        }
      ).subscribe({
        next: () => void this.reportPreview.openExcel(wb, fileName, 'Vista previa del reporte detallado de coincidencias', 'Revise los registros antes de descargar el archivo.'),
        error: err => this.manejarErrorAuditoriaObligatoria(err, 'exportación Excel')
      });
    } else {
      const persona = this.personaSeleccionada();
      if (!persona) return;

      const data = [
        ['Monitoreo de Listas de Riesgo'],
        [this.configService.configSistema()?.nombreInstitucion || 'Instituto Hondureño de Seguridad Social'],
        ['Reporte Detallado de Coincidencias - Persona Natural'],
        [],
        ['Nombre:', persona.nombre],
        ['DNI:', persona.numeroIdentificacion],
        [],
        ['Condición Actúa', 'Nro Patronal', 'Empresa', 'Es PEP', 'Lista', 'Fecha Coincidencia', 'Fecha Calificación']
      ];

      this.detallesNatural().forEach(det => {
        data.push([
          det.tipoCondicionActuaDesc,
          det.numeroPatronal,
          det.nombreEmpresa,
          (det.esPep === 'SI' || det.esPep === 'S') ? 'SÍ' : 'NO',
          det.listaCoincidencia,
          det.fechaCoincidencia ? this.formatDate(det.fechaCoincidencia) : '',
          det.fechaCalifico ? this.formatDate(det.fechaCalifico) : ''
        ]);
      });

      data.push([]);
      data.push(['Resumen del Reporte']);
      data.push(['Total de Coincidencias', this.totalCoincidencias().toString()]);
      data.push(['Coincidencias PEP', this.coincidenciasPep().toString()]);
      data.push(['Empresas Relacionadas', this.empresasUnicas().toString()]);

      const ws = XLSX.utils.aoa_to_sheet(data);
      
      // Auto-ajustar ancho de columnas básico
      const maxLens = data.reduce((acc, row) => {
        row.forEach((val, colIdx) => {
          const len = val ? val.toString().length : 0;
          if (!acc[colIdx] || len > acc[colIdx]) {
            acc[colIdx] = len;
          }
        });
        return acc;
      }, [] as number[]);
      ws['!cols'] = maxLens.map(len => ({ wch: Math.min(Math.max(len + 2, 10), 40) }));

      const wb = XLSX.utils.book_new();
      XLSX.utils.book_append_sheet(wb, ws, 'Detalle Coincidencias');

      const fileName = `Reporte_Coincidencias_${persona.numeroIdentificacion}.xlsx`;
      this.listasService.registrarAuditoriaExportacion(
        'RL_LISTA_POSITIVOS',
        persona.numeroIdentificacion,
        'ExportacionMonitoreoListas',
        {
          accion: 'EXPORTACION_EXCEL',
          tipo: 'natural',
          nombre: persona.nombre,
          cantidadRegistros: this.detallesNatural().length,
          archivo: fileName
        }
      ).subscribe({
        next: () => void this.reportPreview.openExcel(wb, fileName, 'Vista previa del reporte detallado de coincidencias', 'Revise los registros antes de descargar el archivo.'),
        error: err => this.manejarErrorAuditoriaObligatoria(err, 'exportación Excel')
      });
    }
  }

  exportarListaPrincipal() {
    if (this.errorMonitoreo()) return;
    this.cargando.set(true);
    this.obtenerDatosMonitoreoParaExportar().subscribe({
      next: dataFiltrada => {
        this.cargando.set(false);
        this.exportarListaPrincipalConDatos(dataFiltrada);
      },
      error: err => {
        this.cargando.set(false);
        this.manejarErrorAuditoriaObligatoria(err, 'preparación de exportación Excel');
      }
    });
  }

  private exportarListaPrincipalConDatos(dataFiltrada: MonitoreoRegistro[]) {
    const tipo = this.tipoActivo();

    let headers: string[] = [];
    let title = '';
    let rows: any[][] = [];

    if (tipo === 'juridica') {
      title = 'Reporte de Coincidencias Jurídicas';
      headers = ['Número Patronal', 'RTN', 'Nombre Empresa', 'Lista Coincidencia', 'Proveedor IHSS', 'Fecha Coincidencia', 'Fecha Calificación', 'Registro Interno'];
      rows = (dataFiltrada as CoincidenciaJuridica[]).map(item => [
        item.numeroPatrono,
        item.rtn,
        item.nombre,
        item.listaCoincidencia,
        item.esProveedorIhss || 'No',
        item.fechaEncontro ? this.formatDate(item.fechaEncontro) : '',
        item.fechaCalifico ? this.formatDate(item.fechaCalifico) : '',
        item.fechaRegistroInterno ? this.formatDate(item.fechaRegistroInterno) : ''
      ]);
    } else if (tipo === 'natural') {
      title = 'Reporte de Coincidencias Naturales (Agrupado por DNI)';
      headers = ['Número Identificación', 'Nombre Completo', 'Lista Coincidencia', 'Coincidencias', 'Estado', 'Identidad / Asociación', 'Fecha Coincidencia', 'Fecha Calificación', 'Registro Interno'];
      rows = (dataFiltrada as CoincidenciaNatural[]).map(item => [
        item.numeroIdentificacion,
        item.nombre,
        item.listaCoincidencia,
        item.totalRepetidos || (item.esManual ? 0 : 1),
        this.obtenerEstadoMonitoreo(item),
        this.obtenerEtiquetaIdentidad(item),
        item.fechaEncontro ? this.formatDate(item.fechaEncontro) : '',
        item.fechaCalifico ? this.formatDate(item.fechaCalifico) : '',
        item.fechaRegistroInterno ? this.formatDate(item.fechaRegistroInterno) : ''
      ]);
    } else {
      title = 'Reporte de Coincidencias Empleados (Agrupado por Identidad)';
      headers = ['Identidad', 'Nombre Empleado', 'Lista Coincidencia', 'Coincidencias', 'Estado', 'Identidad / Asociación', 'Fecha Coincidencia', 'Fecha Calificación', 'Registro Interno'];
      rows = (dataFiltrada as CoincidenciaEmpleado[]).map(item => [
        item.identidad,
        item.nombre,
        item.listaCoincidencia,
        item.totalRepetidos || (item.esManual ? 0 : 1),
        this.obtenerEstadoMonitoreo(item),
        this.obtenerEtiquetaIdentidad(item),
        item.fechaEncontro ? this.formatDate(item.fechaEncontro) : '',
        item.fechaCalifico ? this.formatDate(item.fechaCalifico) : '',
        item.fechaRegistroInterno ? this.formatDate(item.fechaRegistroInterno) : ''
      ]);
    }

    const dataExcel = [
      [title],
      [this.configService.configSistema()?.nombreInstitucion || 'Instituto Hondureño de Seguridad Social'],
      [`Fecha de Generación: ${this.formatDate(new Date().toISOString())}`],
      [],
      headers,
      ...rows
    ];

    const ws = XLSX.utils.aoa_to_sheet(dataExcel);
    
    // Auto-ajustar ancho de columnas
    const maxLens = dataExcel.reduce((acc, row) => {
      row.forEach((val, colIdx) => {
        const len = val ? val.toString().length : 0;
        if (!acc[colIdx] || len > acc[colIdx]) {
          acc[colIdx] = len;
        }
      });
      return acc;
    }, [] as number[]);
    ws['!cols'] = maxLens.map(len => ({ wch: Math.min(Math.max(len + 2, 10), 40) }));

    const wb = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(wb, ws, 'Coincidencias');

    const fileName = `Reporte_${tipo.charAt(0).toUpperCase() + tipo.slice(1)}s_${new Date().toISOString().split('T')[0]}.xlsx`;
    this.listasService.registrarAuditoriaExportacion(
      'RL_LISTA_POSITIVOS',
      tipo,
      'ExportacionMonitoreoListas',
      {
        accion: 'EXPORTACION_EXCEL',
        tipo,
        titulo: title,
        cantidadRegistros: rows.length,
        archivo: fileName
      }
    ).subscribe({
      next: () => void this.reportPreview.openExcel(wb, fileName, `Vista previa del reporte de coincidencias ${tipo}`, 'Revise los registros filtrados antes de descargar el archivo.'),
      error: err => this.manejarErrorAuditoriaObligatoria(err, 'exportación Excel')
    });
  }

  private obtenerResumenFiltrosPrincipales(): string {
    const filtros = [
      `Tipo: ${this.etiquetaTipoActivo()}`,
      `Estado: ${this.obtenerEtiquetaEstadoFiltro(this.filtroEstado())}`,
      this.busqueda().trim() ? `Búsqueda: ${this.busqueda().trim()}` : 'Búsqueda: Sin texto',
      this.filtroFechaDesde() ? `Desde: ${this.formatDate(this.filtroFechaDesde())}` : 'Desde: Sin fecha',
      this.filtroFechaHasta() ? `Hasta: ${this.formatDate(this.filtroFechaHasta())}` : 'Hasta: Sin fecha',
      ...(this.tipoActivo() === 'empleado'
        ? [this.periodoPlanilla() ? `Planilla: ${this.periodoPlanilla()}` : 'Planilla: Todos los meses',
           this.periodoPlanillaDesde() ? `Planilla desde: ${this.periodoPlanillaDesde()}` : 'Planilla desde: Sin límite',
           this.periodoPlanillaHasta() ? `Planilla hasta: ${this.periodoPlanillaHasta()}` : 'Planilla hasta: Sin límite']
        : [])
    ];
    return filtros.join(' | ');
  }

  private obtenerEtiquetaEstadoFiltro(estado: FiltroEstado): string {
    if (estado === 'activos') return 'Coincidencias activas';
    if (estado === 'pendiente') return 'Pendientes de motivo';
    if (estado === 'con_motivo') return 'Con motivo registrado';
    if (estado === 'manual') return 'Positivos manuales';
    if (estado === 'cerrado_pasivo') return 'Cerrados / Pasivos';
    return 'Todos los registros';
  }

  public obtenerConsultaMonitoreoActual(): ConsultaMonitoreoPaginada {
    return {
      pagina: 1,
      tamanoPagina: 200,
      buscar: this.busqueda(),
      estado: this.filtroEstado(),
      fechaDesde: this.filtroFechaDesde() || null,
      fechaHasta: this.filtroFechaHasta() || null,
      categoriaIdentidad: this.tipoActivo() === 'natural' ? this.categoriaIdentidad() : undefined,
      periodoPlanilla: this.tipoActivo() === 'empleado' && this.periodoPlanilla() ? `${this.periodoPlanilla()}-01` : null,
      periodoPlanillaDesde: this.tipoActivo() === 'empleado' && this.periodoPlanillaDesde() ? `${this.periodoPlanillaDesde()}-01` : null,
      periodoPlanillaHasta: this.tipoActivo() === 'empleado' && this.periodoPlanillaHasta() ? `${this.periodoPlanillaHasta()}-01` : null
    };
  }

  public obtenerDatosMonitoreoParaExportar(): Observable<MonitoreoRegistro[]> {
    const consulta = this.obtenerConsultaMonitoreoActual();
    if (this.tipoActivo() === 'juridica') return this.listasService.getJuridicasParaExportar(consulta);
    if (this.tipoActivo() === 'natural') return this.listasService.getNaturalesParaExportar(consulta);
    return this.listasService.getEmpleadosParaExportar(consulta);
  }

  public construirReporteListaPrincipalPdf(dataFiltrada: MonitoreoRegistro[] = this.datosFiltrados()): { title: string; headers: string[]; rows: string[][] } | null {
    const tipo = this.tipoActivo();

    if (tipo === 'juridica') {
      return {
        title: 'Reporte de Coincidencias Jurídicas',
        headers: ['Número Patronal', 'RTN', 'Nombre Empresa', 'Lista Coincidencia', 'Proveedor IHSS', 'Estado', 'Fecha Coincidencia', 'Fecha Calificación', 'Registro Interno'],
        rows: (dataFiltrada as CoincidenciaJuridica[]).map(item => [
          item.numeroPatrono || '',
          item.rtn || '',
          item.nombre || '',
          item.listaCoincidencia || '',
          item.esProveedorIhss || 'No',
          this.obtenerEstadoMonitoreo(item),
          item.fechaEncontro ? this.formatDate(item.fechaEncontro) : '',
          item.fechaCalifico ? this.formatDate(item.fechaCalifico) : '',
          item.fechaRegistroInterno ? this.formatDate(item.fechaRegistroInterno) : ''
        ])
      };
    }

    if (tipo === 'natural') {
      return {
        title: 'Reporte de Coincidencias Naturales (Agrupado por DNI)',
        headers: ['Número Identificación', 'Nombre Completo', 'Lista Coincidencia', 'Coincidencias', 'Estado', 'Identidad / Asociación', 'Fecha Coincidencia', 'Fecha Calificación', 'Registro Interno'],
        rows: (dataFiltrada as CoincidenciaNatural[]).map(item => [
          item.numeroIdentificacion || '',
          item.nombre || '',
          item.listaCoincidencia || '',
          String(item.totalRepetidos || (item.esManual ? 0 : 1)),
          this.obtenerEstadoMonitoreo(item),
          this.obtenerEtiquetaIdentidad(item),
          item.fechaEncontro ? this.formatDate(item.fechaEncontro) : '',
          item.fechaCalifico ? this.formatDate(item.fechaCalifico) : '',
          item.fechaRegistroInterno ? this.formatDate(item.fechaRegistroInterno) : ''
        ])
      };
    }

    return {
      title: 'Reporte de Coincidencias Empleados (Agrupado por Identidad)',
      headers: ['Identidad', 'Nombre Empleado', 'Lista Coincidencia', 'Coincidencias', 'Estado', 'Identidad / Asociación', 'Fecha Coincidencia', 'Fecha Calificación', 'Registro Interno'],
      rows: (dataFiltrada as CoincidenciaEmpleado[]).map(item => [
        item.identidad || '',
        item.nombre || '',
        item.listaCoincidencia || '',
        String(item.totalRepetidos || (item.esManual ? 0 : 1)),
        this.obtenerEstadoMonitoreo(item),
        this.obtenerEtiquetaIdentidad(item),
        item.fechaEncontro ? this.formatDate(item.fechaEncontro) : '',
        item.fechaCalifico ? this.formatDate(item.fechaCalifico) : '',
        item.fechaRegistroInterno ? this.formatDate(item.fechaRegistroInterno) : ''
      ])
    };
  }

  exportarPdfListaPrincipal() {
    if (this.errorMonitoreo()) return;
    this.cargando.set(true);
    this.obtenerDatosMonitoreoParaExportar().subscribe({
      next: dataFiltrada => {
        this.cargando.set(false);
        this.exportarPdfListaPrincipalConDatos(dataFiltrada);
      },
      error: err => {
        this.cargando.set(false);
        this.manejarErrorAuditoriaObligatoria(err, 'preparación de exportación PDF');
      }
    });
  }

  private exportarPdfListaPrincipalConDatos(dataFiltrada: MonitoreoRegistro[]) {
    const reporte = this.construirReporteListaPrincipalPdf(dataFiltrada);
    if (!reporte) return;

    const doc = new jsPDF({ orientation: 'landscape', unit: 'mm', format: 'a4' });
    const pageWidth = doc.internal.pageSize.getWidth();
    const pageHeight = doc.internal.pageSize.getHeight();
    const startY = this.agregarEncabezadoReporteMonitoreoPdf(doc, reporte.title);

    const resumen = [
      ['Registros filtrados', String(reporte.rows.length), 'Coincidencias visibles en la vista actual'],
      ['Pendientes', String(dataFiltrada.filter(item => !this.esCerradoPasivo(item) && !item.tieneMotivo).length), 'Requieren motivo o revisión'],
      ['Con motivo', String(dataFiltrada.filter(item => !this.esCerradoPasivo(item) && !!item.tieneMotivo).length), 'Con sustento registrado'],
      ['Cerrados / pasivos', String(dataFiltrada.filter(item => this.esCerradoPasivo(item)).length), 'Registros no activos']
    ];
    const resumenTabla = [
      [...resumen[0], ...resumen[1]],
      [...resumen[2], ...resumen[3]]
    ];

    autoTableInstitucional(doc, {
      startY,
      body: resumenTabla,
      theme: 'plain',
      styles: {
        fontSize: 8,
        cellPadding: 2.5,
        valign: 'middle',
        lineColor: [226, 232, 240],
        lineWidth: 0.1
      },
      columnStyles: {
        0: { cellWidth: 66, fillColor: [248, 250, 252], textColor: COLORES_REPORTE_INSTITUCIONAL.body, fontStyle: 'bold' },
        1: { cellWidth: 14, fillColor: [248, 250, 252], textColor: COLORES_REPORTE_INSTITUCIONAL.body, fontStyle: 'bold', halign: 'center' },
        2: { cellWidth: 58, fillColor: [248, 250, 252], textColor: COLORES_REPORTE_INSTITUCIONAL.body },
        3: { cellWidth: 66, fillColor: [248, 250, 252], textColor: COLORES_REPORTE_INSTITUCIONAL.body, fontStyle: 'bold' },
        4: { cellWidth: 14, fillColor: [248, 250, 252], textColor: COLORES_REPORTE_INSTITUCIONAL.body, fontStyle: 'bold', halign: 'center' },
        5: { cellWidth: 58, fillColor: [248, 250, 252], textColor: COLORES_REPORTE_INSTITUCIONAL.body }
      },
      margin: { left: 14, right: 14 }
    });

    const tableStartY = ((doc as any).lastAutoTable?.finalY || startY) + 10;

    doc.setFont('helvetica', 'bold');
    doc.setFontSize(11);
    doc.setTextColor(...COLORES_REPORTE_INSTITUCIONAL.body);
    doc.text('Detalle de coincidencias filtradas', 14, tableStartY - 3);

    autoTableInstitucional(doc, {
      startY: tableStartY,
      head: [reporte.headers],
      body: reporte.rows,
      theme: 'striped',
      headStyles: {
        fillColor: getInstitutionalReportBranding().headerColor,
        textColor: getInstitutionalReportBranding().headerTextColor,
        fontStyle: 'bold',
        fontSize: 7.4,
        halign: 'left',
        minCellHeight: 7
      },
      styles: {
        fontSize: 7.2,
        cellPadding: 2.2,
        overflow: 'linebreak',
        valign: 'middle',
        textColor: COLORES_REPORTE_INSTITUCIONAL.body,
        lineColor: [226, 232, 240],
        lineWidth: 0.1
      },
      alternateRowStyles: { fillColor: [248, 250, 252] },
      margin: { left: 14, right: 14 },
      tableWidth: pageWidth - 28
    });
    agregarPiesInstitucionalesPdf(doc);

    const fileName = `Reporte_Monitoreo_Listas_${this.tipoActivo()}_${new Date().toISOString().split('T')[0]}.pdf`;
    this.listasService.registrarAuditoriaExportacion(
      'RL_LISTA_POSITIVOS',
      this.tipoActivo(),
      'ExportacionMonitoreoListas',
      {
        accion: 'EXPORTACION_PDF',
        tipo: this.tipoActivo(),
        titulo: reporte.title,
        filtros: this.obtenerResumenFiltrosPrincipales(),
        cantidadRegistros: reporte.rows.length,
        archivo: fileName
      }
    ).subscribe({
      next: () => this.reportPreview.openPdf(
        doc.output('blob'),
        fileName,
        'Vista previa del reporte de monitoreo de listas',
        'Revise las coincidencias filtradas antes de descargar el documento.'
      ),
      error: err => this.manejarErrorAuditoriaObligatoria(err, 'exportación PDF')
    });
  }

  mathMin(a: number, b: number): number {
    return Math.min(a, b);
  }

  deducirTipoDocumentoDefault(row: any, tipo: FiltroTipo): number | null {
    const tipos = this.listaTiposDocumento();
    if (!tipos.length) return 1;

    if (tipo === 'natural' || tipo === 'empleado') {
      const match = tipos.find(t => {
        const desc = (t.descripcion || '').toUpperCase();
        return desc.includes('DNI') || desc.includes('IDENTIDAD') || desc.includes('CEDULA');
      });
      return match ? match.tipoDocumentoId : tipos[0].tipoDocumentoId;
    }

    if (tipo === 'juridica') {
      const match = tipos.find(t => {
        const desc = (t.descripcion || '').toUpperCase();
        return desc.includes('RTN') || desc.includes('PATRONAL') || desc.includes('RUC');
      });
      return match ? match.tipoDocumentoId : tipos[0].tipoDocumentoId;
    }

    return tipos[0].tipoDocumentoId;
  }

  deducirTipoListaCautelaDefault(row: any): number | null {
    const cautelas = this.listaTiposListasCautela();
    if (!cautelas.length) return 1;

    const listaTexto = (row?.listaCoincidencia || '').toUpperCase().trim();
    if (listaTexto) {
      const match = cautelas.find(c => {
        const desc = (c.descripcion || '').toUpperCase().trim();
        return desc === listaTexto || listaTexto.includes(desc) || desc.includes(listaTexto);
      });
      if (match) return match.tipoListaCautelaId;
    }

    const peps = cautelas.find(c => (c.descripcion || '').toUpperCase().includes('PEP'));
    return peps ? peps.tipoListaCautelaId : cautelas[0].tipoListaCautelaId;
  }

  registrarMotivo(row: any) {
    this.esRegistroManual.set(false);
    this.formManualNombre.set('');
    this.formManualNoDocumento.set('');
    this.formManualTipoPositivoId.set(null);
    this.formSeguimientoComentario.set('');
    this.archivosSeguimiento.set([]);

    let tipoPosId = 1; // 1 = JURÍDICO, 2 = NATURAL, 3 = EMPLEADO
    const tipo = this.tipoActivo();
    let docNum = '';
    let documentoReal = '';

    if (tipo === 'juridica') {
      tipoPosId = 1;
      docNum = row.numeroPatrono || row.rtn;
      documentoReal = docNum;
    } else if (tipo === 'natural') {
      tipoPosId = 2;
      documentoReal = row.numeroIdentificacion || 'Sin identificación';
      docNum = (row.categoriaIdentidad === 'SIN_IDENTIFICACION' && row.idRegistroFuente)
        ? `SRC:${row.idRegistroFuente}`
        : (row.numeroIdentificacion || (row.idRegistroFuente ? `SRC:${row.idRegistroFuente}` : ''));
    } else if (tipo === 'empleado') {
      tipoPosId = 3;
      documentoReal = row.identidad || 'Sin identificación';
      docNum = (row.categoriaIdentidad === 'SIN_IDENTIFICACION' && row.idRegistroFuente)
        ? `SRC:${row.idRegistroFuente}`
        : (row.identidad || (row.idRegistroFuente ? `SRC:${row.idRegistroFuente}` : ''));
    }

    const defaultTipoDoc = this.deducirTipoDocumentoDefault(row, tipo);
    const defaultTipoCautela = this.deducirTipoListaCautelaDefault(row);
    const defaultOrigen = row.esManual ? 'MANUAL_CUMPLIMIENTO' : 'DNP_LISTAS';

    this.formTipoDocId.set(defaultTipoDoc);
    this.formTipoListaCautelaId.set(defaultTipoCautela);
    this.formOrigenRegistro.set(defaultOrigen);
    this.formMotivo.set('');

    this.entidadSeleccionada.set({
      nombreCompleto: row.nombre,
      noDocumento: docNum,
      documentoReal: documentoReal,
      idRegistroFuente: row.idRegistroFuente ?? null,
      tipoPositivoId: tipoPosId,
      tipoListaText: tipo === 'juridica' ? 'Jurídica' : tipo === 'natural' ? 'Natural' : 'Empleado',
      totalRepetidos: row.totalRepetidos || 1,
      estadoIdentidad: row.estadoIdentidad,
      estadoAsociacion: row.estadoAsociacion,
      categoriaIdentidad: row.categoriaIdentidad,
      listaCoincidencia: row.listaCoincidencia || ''
    });

    this.listasService.getPositivoPorDocumento(docNum, tipoPosId, row.idRegistroFuente).subscribe({
      next: (existing) => {
        if (existing) {
          this.formTipoDocId.set(existing.tipoDocumentoId || defaultTipoDoc);
          this.formMotivo.set(existing.motivoIngreso || '');
          this.formTipoListaCautelaId.set(existing.tipoListaCautelaId || defaultTipoCautela);
          this.formOrigenRegistro.set(existing.origenRegistro || defaultOrigen);
        } else {
          this.formTipoDocId.set(defaultTipoDoc);
          this.formTipoListaCautelaId.set(defaultTipoCautela);
          this.formOrigenRegistro.set(defaultOrigen);
        }
        this.modalMotivoAbierto.set(true);
      },
      error: (err) => {
        console.error('Error al obtener datos existentes de la lista de positivos:', err);
        this.formTipoDocId.set(defaultTipoDoc);
        this.formTipoListaCautelaId.set(defaultTipoCautela);
        this.formOrigenRegistro.set(defaultOrigen);
        this.modalMotivoAbierto.set(true);
      }
    });
  }

  agregarPositivoManual() {
    this.esRegistroManual.set(true);
    this.formManualNombre.set('');
    this.formManualNoDocumento.set('');
    this.formManualTipoPositivoId.set(null);
    this.formTipoDocId.set(this.deducirTipoDocumentoDefault(null, 'natural'));
    this.formMotivo.set('');
    this.formTipoListaCautelaId.set(this.deducirTipoListaCautelaDefault(null));
    this.formOrigenRegistro.set('MANUAL_CUMPLIMIENTO');
    this.formSeguimientoComentario.set('');
    this.archivosSeguimiento.set([]);
    this.entidadSeleccionada.set(null);
    this.modalMotivoAbierto.set(true);
  }

  cerrarModalMotivo() {
    this.modalMotivoAbierto.set(false);
    this.entidadSeleccionada.set(null);
    this.formTipoDocId.set(null);
    this.formMotivo.set('');
    this.formTipoListaCautelaId.set(null);
    this.formOrigenRegistro.set('DNP_LISTAS');
    this.esRegistroManual.set(false);
    this.formManualNombre.set('');
    this.formManualNoDocumento.set('');
    this.formManualTipoPositivoId.set(null);
    this.formSeguimientoComentario.set('');
    this.archivosSeguimiento.set([]);
  }

  get politicaCargaArchivos(): InstitutionalFileUploadPolicy {
    const politica = this.politicaEvidencias();
    return {
      allowedExtensions: politica.extensionesPermitidas,
      allowedTypesText: politica.tiposPermitidosTexto,
      maxFileSizeBytes: politica.maximoBytes || politica.maximoMb * 1024 * 1024,
    };
  }

  onManualSeguimientoFilesSelected(files: File[]): void {
    this.archivosSeguimiento.set(files);
  }

  async guardarMotivo(): Promise<void> {
    const manual = this.esRegistroManual();
    let docId = this.formTipoDocId();
    const motivo = this.formMotivo().trim();
    let cautelaId = this.formTipoListaCautelaId();
    let origenRegistro = this.formOrigenRegistro();
    const comentarioSeg = this.formSeguimientoComentario().trim();
    const archivosSeg = this.archivosSeguimiento();

    let noDocumento = '';
    let nombreCompleto = '';
    let tipoPositivoId = 1;
    let idRegistroFuente: number | null = null;

    if (manual) {
      noDocumento = this.formManualNoDocumento().trim();
      nombreCompleto = this.formManualNombre().trim();
      tipoPositivoId = this.formManualTipoPositivoId() || 1;
      if (!noDocumento || !nombreCompleto || !tipoPositivoId) {
        this.notificar('Campos requeridos', 'Por favor complete todos los campos obligatorios del registro manual.', 'warning', 'view');
        return;
      }
    } else {
      const entidad = this.entidadSeleccionada();
      if (!entidad) return;
      noDocumento = entidad.noDocumento;
      nombreCompleto = entidad.nombreCompleto;
      tipoPositivoId = entidad.tipoPositivoId;
      idRegistroFuente = entidad.idRegistroFuente ?? null;
    }

    if (!docId) {
      docId = this.deducirTipoDocumentoDefault(this.entidadSeleccionada(), this.tipoActivo());
      this.formTipoDocId.set(docId);
    }
    if (!cautelaId) {
      cautelaId = this.deducirTipoListaCautelaDefault(this.entidadSeleccionada());
      this.formTipoListaCautelaId.set(cautelaId);
    }
    if (!origenRegistro) {
      origenRegistro = manual ? 'MANUAL_CUMPLIMIENTO' : 'DNP_LISTAS';
      this.formOrigenRegistro.set(origenRegistro);
    }

    if (!docId || !cautelaId || !origenRegistro || !motivo) {
      this.notificar('Campos requeridos', 'Por favor ingrese el motivo de ingreso a la lista de positivos.', 'warning', 'view');
      return;
    }

    const notaSegFinal = comentarioSeg || (archivosSeg.length > 0 ? 'Evidencia documental adjunta en registro inicial.' : '');

    const confirmado = await this.confirmation.confirm({
      title: 'Registrar motivo de coincidencia',
      message: `Se registrará el motivo para ${nombreCompleto} y quedará disponible para auditoría y seguimiento.`,
      confirmLabel: 'Sí, registrar',
      cancelLabel: 'Cancelar',
      confirmIcon: 'save',
      confirmVariant: 'primary'
    });
    if (!confirmado) return;

    this.guardandoMotivo.set(true);

    const dto: RegistrarPositivoDto = {
      tipoDocumentoId: Number(docId),
      tipoPositivoId: tipoPositivoId,
      noDocumento: noDocumento,
      nombreCompleto: nombreCompleto,
      motivoIngreso: motivo,
      tipoListaCautelaId: Number(cautelaId),
      origenRegistro: origenRegistro,
      ...(idRegistroFuente ? { idRegistroFuente } : {})
    };

    this.listasService.registrarPositivo(dto).subscribe({
        next: (resp) => {
          if (notaSegFinal) {
            this.listasService.registrarSeguimiento(noDocumento, notaSegFinal, archivosSeg, tipoPositivoId, idRegistroFuente ?? undefined).subscribe({
              next: () => {
                this.guardandoMotivo.set(false);
                this.cerrarModalMotivo();
                this.notificar('Registro completo', 'Se ha registrado el motivo y el primer seguimiento correctamente.', 'success');
                this.cargarDatos({ force: true, mostrarLoader: false });
              },
              error: (errSeg) => {
                this.guardandoMotivo.set(false);
                this.cerrarModalMotivo();
                this.notificar('Registro parcial', 'El motivo se registró con éxito, pero hubo un error al registrar el seguimiento: ' + (errSeg.error?.mensaje || 'Error desconocido'), 'warning', 'view');
                this.cargarDatos({ force: true, mostrarLoader: false });
              }
            });
          } else {
            this.guardandoMotivo.set(false);
            this.cerrarModalMotivo();
            this.notificar('Registro exitoso', resp.mensaje || 'Se ha registrado el motivo correctamente.', 'success');
            this.cargarDatos({ force: true, mostrarLoader: false });
          }
        },
        error: (err) => {
          this.guardandoMotivo.set(false);
          if (err?.status === 499) {
            return;
          }
          this.notificar('No se pudo guardar el registro', err.error?.mensaje || 'No se pudo guardar el registro.', 'error', 'cancel');
        }
      });
  }

  darSeguimiento(row: any) {
    let tipoPosId = 1;
    const tipo = this.tipoActivo();
    let docNum = '';
    let documentoReal = '';

    if (tipo === 'juridica') {
      tipoPosId = 1;
      docNum = row.numeroPatrono || row.rtn;
      documentoReal = docNum;
    } else if (tipo === 'natural') {
      tipoPosId = 2;
      documentoReal = row.numeroIdentificacion || 'Sin identificación';
      docNum = (row.categoriaIdentidad === 'SIN_IDENTIFICACION' && row.idRegistroFuente)
        ? `SRC:${row.idRegistroFuente}`
        : (row.numeroIdentificacion || (row.idRegistroFuente ? `SRC:${row.idRegistroFuente}` : ''));
    } else if (tipo === 'empleado') {
      tipoPosId = 3;
      documentoReal = row.identidad || 'Sin identificación';
      docNum = (row.categoriaIdentidad === 'SIN_IDENTIFICACION' && row.idRegistroFuente)
        ? `SRC:${row.idRegistroFuente}`
        : (row.identidad || (row.idRegistroFuente ? `SRC:${row.idRegistroFuente}` : ''));
    }

    this.entidadSeleccionada.set({
      nombreCompleto: row.nombre,
      noDocumento: docNum,
      documentoReal: documentoReal,
      idRegistroFuente: row.idRegistroFuente ?? null,
      tipoPositivoId: tipoPosId,
      tipoListaText: tipo === 'juridica' ? 'Jurídica' : tipo === 'natural' ? 'Natural' : 'Empleado',
      totalRepetidos: row.totalRepetidos || 1,
      estadoIdentidad: row.estadoIdentidad,
      estadoAsociacion: row.estadoAsociacion,
      categoriaIdentidad: row.categoriaIdentidad
    });

    this.modoEdicion.set(false);
    this.seguimientoEditandoId.set(null);
    this.evidenciasExistentes.set([]);
    this.formComentarioSeguimiento.set('');
    if (this.reporteSeguimientoDocumento() === docNum) {
      this.filtroSeguimientoDesde.set(this.reporteSeguimientoDesde());
      this.filtroSeguimientoHasta.set(this.reporteSeguimientoHasta());
    } else {
      this.filtroSeguimientoDesde.set('');
      this.filtroSeguimientoHasta.set('');
    }
    this.archivosSeleccionados.set([]);
    this.modalSeguimientoAbierto.set(true);

    this.cargarSeguimientos(docNum, tipoPosId, row.idRegistroFuente);
  }

  cargarSeguimientos(noDocumento: string, tipoPositivoId?: number, idRegistroFuente?: number) {
    const solicitud = ++this.seguimientoSolicitud;
    this.cargandoSeguimiento.set(true);
    this.seguimientoError.set(null);
    const effectiveTipo = tipoPositivoId ?? this.entidadSeleccionada()?.tipoPositivoId;
    const effectiveFuente = idRegistroFuente ?? this.entidadSeleccionada()?.idRegistroFuente;
    const request$ = (effectiveTipo !== undefined && effectiveTipo !== null)
      ? this.listasService.getSeguimientos(noDocumento, this.filtroSeguimientoDesde(), this.filtroSeguimientoHasta(), effectiveTipo, effectiveFuente ?? undefined)
      : this.listasService.getSeguimientos(noDocumento, this.filtroSeguimientoDesde(), this.filtroSeguimientoHasta());
    request$.subscribe({
      next: (res) => {
        if (solicitud !== this.seguimientoSolicitud) return;
        this.listaSeguimientos.set(res);
        this.cargandoSeguimiento.set(false);
      },
      error: (err) => {
        if (solicitud !== this.seguimientoSolicitud) return;
        console.error('Error al obtener historial de seguimientos:', err);
        this.seguimientoError.set(this.mensajeErrorApi(err, 'No se pudo cargar el historial de seguimientos.'));
        this.cargandoSeguimiento.set(false);
      }
    });
  }

  aplicarFiltroSeguimientos() {
    const entidad = this.entidadSeleccionada();
    if (!entidad) return;

    const desde = this.filtroSeguimientoDesde();
    const hasta = this.filtroSeguimientoHasta();
    if (desde && hasta && desde > hasta) {
      this.notificar('Rango no válido', 'La fecha desde no puede ser mayor que la fecha hasta.', 'warning', 'view');
      return;
    }

    this.reporteSeguimientoDocumento.set(entidad.noDocumento);
    this.reporteSeguimientoDesde.set(desde);
    this.reporteSeguimientoHasta.set(hasta);
    this.cargarSeguimientos(entidad.noDocumento);
  }

  programarFiltroSeguimientos() {
    if (this.filtroSeguimientoTimer) clearTimeout(this.filtroSeguimientoTimer);
    this.filtroSeguimientoTimer = setTimeout(() => this.aplicarFiltroSeguimientos(), 350);
  }

  limpiarFiltroSeguimientos() {
    const entidad = this.entidadSeleccionada();
    this.filtroSeguimientoDesde.set('');
    this.filtroSeguimientoHasta.set('');
    if (entidad && this.reporteSeguimientoDocumento() === entidad.noDocumento) {
      this.reporteSeguimientoDocumento.set('');
      this.reporteSeguimientoDesde.set('');
      this.reporteSeguimientoHasta.set('');
    }
    if (entidad) {
      this.cargarSeguimientos(entidad.noDocumento);
    }
  }

  cerrarModalSeguimiento() {
    ++this.seguimientoSolicitud;
    this.modalSeguimientoAbierto.set(false);
    this.entidadSeleccionada.set(null);
    this.listaSeguimientos.set([]);
    this.formComentarioSeguimiento.set('');
    this.filtroSeguimientoDesde.set('');
    this.filtroSeguimientoHasta.set('');
    this.archivosSeleccionados.set([]);
    this.modoEdicion.set(false);
    this.seguimientoEditandoId.set(null);
    this.evidenciasExistentes.set([]);
    this.seguimientoError.set(null);
  }

  onSeguimientoFilesSelected(files: File[]): void {
    this.archivosSeleccionados.set(files);
  }

  iniciarEdicionSeguimiento(seg: Seguimiento) {
    this.modoEdicion.set(true);
    this.seguimientoEditandoId.set(seg.detalleListaId);
    this.formComentarioSeguimiento.set(seg.motivoIngreso);
    this.evidenciasExistentes.set(seg.evidencias || []);
    this.archivosSeleccionados.set([]);
  }

  cancelarEdicion() {
    this.modoEdicion.set(false);
    this.seguimientoEditandoId.set(null);
    this.formComentarioSeguimiento.set('');
    this.evidenciasExistentes.set([]);
    this.archivosSeleccionados.set([]);
  }

  // Centraliza el motivo obligatorio para eliminaciones lógicas de monitoreo.
  private solicitarMotivoEliminacion(title: string, text: string): Promise<string | null> {
    return this.confirmation.confirmWithInput({
      title,
      message: text,
      icon: 'warning',
      confirmLabel: 'Sí, eliminar',
      cancelLabel: 'Cancelar',
      confirmIcon: 'delete',
      confirmVariant: 'danger',
      inputLabel: 'Motivo de eliminación',
      inputPlaceholder: 'Escriba el motivo de la eliminación...',
      inputMaxLength: this.maxTextoTextarea,
      inputValidator: value => {
        const motivo = value?.trim();
        if (!motivo) return 'Debe ingresar un motivo de eliminación.';
        if (motivo.length > this.maxTextoTextarea) return `El motivo no debe superar los ${this.maxTextoTextarea} caracteres.`;
        return null;
      }
    });
  }

  eliminarEvidenciaExistente(evi: Evidencia) {
    void (async () => {
      const motivoEliminacion = await this.solicitarMotivoEliminacion(
        '¿Eliminar evidencia?',
        `Se eliminará lógicamente el archivo ${evi.nombreArchivo}.`
      );

      if (motivoEliminacion) {
          this.mostrarCarga('Eliminando evidencia', 'Espere mientras se actualiza la evidencia documental.');

          this.listasService.eliminarEvidencia(evi.evidenciaId, motivoEliminacion).subscribe({
            next: (resp) => {
              this.evidenciasExistentes.set(
                this.evidenciasExistentes().filter(e => e.evidenciaId !== evi.evidenciaId)
              );

              this.listaSeguimientos.set(
                this.listaSeguimientos().map(s => {
                  if (s.evidencias) {
                    s.evidencias = s.evidencias.filter(e => e.evidenciaId !== evi.evidenciaId);
                  }
                  return s;
                })
              );

              this.notification.closeLoading();
              this.notificar('Evidencia eliminada', resp.mensaje || 'Evidencia eliminada correctamente.', 'success');
            },
            error: (err) => {
              this.notification.closeLoading();
              this.notificar('No se pudo eliminar la evidencia', err.error?.mensaje || 'No se pudo eliminar la evidencia.', 'error', 'cancel');
            }
          });
      }
    })();
  }

  async guardarSeguimiento(): Promise<void> {
    const entidad = this.entidadSeleccionada();
    const motivo = this.formComentarioSeguimiento().trim();
    const archivos = this.archivosSeleccionados();

    if (!entidad || !motivo) return;

    const confirmado = await this.confirmation.confirm({
      title: this.modoEdicion() ? 'Guardar cambios del seguimiento' : 'Registrar seguimiento',
      message: this.modoEdicion()
        ? 'Se actualizará el seguimiento seleccionado y se conservará su trazabilidad.'
        : 'Se registrará el nuevo seguimiento y las evidencias adjuntas.',
      confirmLabel: 'Sí, guardar',
      cancelLabel: 'Cancelar',
      confirmIcon: 'save',
      confirmVariant: 'primary'
    });
    if (!confirmado) return;

    this.guardandoSeguimiento.set(true);

    const request$ = this.modoEdicion()
        ? this.listasService.actualizarSeguimiento(this.seguimientoEditandoId()!, motivo, archivos)
        : this.listasService.registrarSeguimiento(entidad.noDocumento, motivo, archivos, entidad.tipoPositivoId, entidad.idRegistroFuente);

      request$.subscribe({
        next: (resp) => {
          this.guardandoSeguimiento.set(false);
          const editMode = this.modoEdicion();
          
          this.cancelarEdicion();
          
          this.notificar('Seguimiento guardado', resp.mensaje || (editMode ? 'Seguimiento actualizado exitosamente.' : 'Seguimiento registrado exitosamente.'), 'success');

          this.cargarSeguimientos(entidad.noDocumento, entidad.tipoPositivoId, entidad.idRegistroFuente);
        },
        error: (err) => {
          this.guardandoSeguimiento.set(false);
          if (err?.status === 499) {
            return;
          }
          this.notificar('No se pudo guardar el seguimiento', err.error?.mensaje || 'No se pudo guardar el seguimiento.', 'error', 'cancel');
        }
      });
  }

  descargarEvidencia(evi: Evidencia) {
    this.mostrarCarga('Cargando archivo', 'Por favor, espere mientras se recupera la evidencia.');
    this.listasService.descargarEvidenciaBlob(evi.evidenciaId).subscribe({
        next: (blob) => {
          this.notification.closeLoading();
          const mimeType = blob.type || evi.tipoMime;
          const blobUrl = URL.createObjectURL(blob);

          const esVisualizable = mimeType.includes('pdf') || mimeType.includes('image');
          if (esVisualizable) {
            window.open(blobUrl, '_blank');
          } else {
            const a = document.createElement('a');
            a.href = blobUrl;
            a.download = evi.nombreArchivo;
            document.body.appendChild(a);
            a.click();
            document.body.removeChild(a);
          }

          setTimeout(() => URL.revokeObjectURL(blobUrl), 15000);
        },
        error: (err) => {
          this.notification.closeLoading();
          this.notificar('No se pudo cargar la evidencia', 'No se pudo cargar el archivo de evidencia.', 'error', 'cancel');
        }
      });
  }

  eliminarSeguimiento(seg: Seguimiento) {
    void (async () => {
      const motivoEliminacion = await this.solicitarMotivoEliminacion(
        '¿Eliminar seguimiento?',
        'Esta acción realizará una eliminación lógica de la nota de seguimiento.'
      );

      if (motivoEliminacion) {
          this.mostrarCarga('Eliminando seguimiento', 'Espere mientras se actualiza el historial.');

          this.listasService.eliminarSeguimiento(seg.detalleListaId, motivoEliminacion).subscribe({
            next: (resp) => {
              if (this.modoEdicion() && this.seguimientoEditandoId() === seg.detalleListaId) {
                this.cancelarEdicion();
              }

              this.notification.closeLoading();
              this.notificar('Seguimiento eliminado', resp.mensaje || 'El seguimiento ha sido eliminado correctamente.', 'success');

              const entidad = this.entidadSeleccionada();
              if (entidad) {
                this.cargarSeguimientos(entidad.noDocumento);
              }
            },
            error: (err) => {
              this.notification.closeLoading();
              this.notificar('No se pudo eliminar el seguimiento', err.error?.mensaje || 'No se pudo eliminar el seguimiento.', 'error', 'cancel');
            }
          });
      }
    })();
  }

  obtenerIconoArchivo(mime: string): string {
    const m = mime.toLowerCase();
    if (m.includes('pdf')) return 'application/pdf';
    if (m.includes('image') || m.includes('png') || m.includes('jpeg') || m.includes('gif')) return 'image';
    if (m.includes('word') || m.includes('officedocument.word') || m.includes('msword')) return 'word';
    if (m.includes('excel') || m.includes('officedocument.spreadsheet') || m.includes('csv')) return 'excel';
    return 'default';
  }
}
