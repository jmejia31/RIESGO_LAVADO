import { ChangeDetectionStrategy, Component, OnInit, computed, effect, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { OnDestroy } from '@angular/core';
import { jsPDF } from 'jspdf';
import autoTable from 'jspdf-autotable';
import * as XLSX from '../../../../../core/utils/excel-export.util';
import { MatricesRiesgosService } from '../../data-access/matrices-riesgos.service';
import { MatricesReporteTablaComponent } from '../../components/matrices-reporte-tabla/matrices-reporte-tabla.component';
import {
  EscalaRiesgo,
  MatrizRiesgoCriterio,
  MatrizRiesgoCriterioRequest,
  MatrizRiesgoCrearRequest,
  MatrizRiesgoDetalle,
  MatrizRiesgoDashboard,
  MatrizRiesgoEvidencia,
  MatrizRiesgoHistorial,
  MatrizRiesgoPlanAccion,
  MatrizRiesgoPlanAccionRequest,
  MatrizRiesgoReporteFiltro,
  MatrizRiesgoResumen,
  MatricesRiesgoReporte,
  MetodologiaMatrices,
  VariableMetodologia
} from '../../models/matrices-riesgos.models';
import { ConfiguracionService } from '../../../../../core/configuration/configuracion.service';

type TabMatrices = 'dashboard' | 'matrices' | 'nueva' | 'criterios' | 'planes' | 'reportes';
type ModalTipo = 'calcular' | 'estado' | 'eliminarMatriz' | 'inactivarCriterio' | 'eliminarCriterio' | 'estadoPlan' | 'inactivarPlan' | 'reactivarPlan' | 'inactivarEvidencia';

interface CapturaVariable {
  variableId: number;
  criterioId: number | null;
  puntaje: number | null;
  valorCapturado: string;
  justificacion: string;
  fuenteDato: string;
}

interface ModalOperacion {
  tipo: ModalTipo;
  titulo: string;
  descripcion: string;
  textoConfirmar: string;
  requiereMotivo: boolean;
  estado?: string;
  matriz?: MatrizRiesgoResumen;
  criterio?: MatrizRiesgoCriterio;
  plan?: MatrizRiesgoPlanAccion;
  evidencia?: MatrizRiesgoEvidencia;
  tono: 'normal' | 'advertencia' | 'peligro';
}

interface EvidenciaPreview {
  nombre: string;
  tipoMime: string;
  tamanoBytes: number;
  url: string | null;
  urlSegura: SafeResourceUrl | null;
  tipoVista: 'imagen' | 'pdf' | 'texto' | 'office' | 'generico';
  texto?: string;
  cargando: boolean;
  error?: string;
}

@Component({
  selector: 'app-matrices-riesgos',
  standalone: true,
  imports: [CommonModule, FormsModule, MatricesReporteTablaComponent],
  templateUrl: './matrices-riesgos.component.html',
  styles: [`
    :host {
      display: block;
      min-width: 0;
    }

    .rl-table {
      width: 100%;
      min-width: 920px;
      table-layout: fixed;
    }

    .rl-clamp-2,
    .rl-clamp-3 {
      display: -webkit-box;
      -webkit-box-orient: vertical;
      overflow: hidden;
      overflow-wrap: anywhere;
      word-break: normal;
    }

    .rl-clamp-2 {
      -webkit-line-clamp: 2;
      line-clamp: 2;
    }

    .rl-clamp-3 {
      -webkit-line-clamp: 3;
      line-clamp: 3;
    }

    @media (max-width: 768px) {
      .rl-table {
        min-width: 760px;
      }
    }
  `],
  changeDetection: ChangeDetectionStrategy.Default
})
export class MatricesRiesgosComponent implements OnInit, OnDestroy {
  private readonly service = inject(MatricesRiesgosService);
  private readonly configService = inject(ConfiguracionService);
  private readonly sanitizer = inject(DomSanitizer);
  private reporteFiltroTimer: ReturnType<typeof setTimeout> | null = null;
  private matricesFiltroTimer: ReturnType<typeof setTimeout> | null = null;
  private duplicadosTimer: ReturnType<typeof setTimeout> | null = null;

  readonly tab = signal<TabMatrices>('dashboard');
  readonly cargando = signal(false);
  readonly cargandoReporte = signal(false);
  readonly guardando = signal(false);
  readonly error = signal<string | null>(null);
  readonly mensaje = signal<string | null>(null);
  readonly modalOperacion = signal<ModalOperacion | null>(null);
  readonly evidenciaPreview = signal<EvidenciaPreview | null>(null);
  readonly modalMotivo = signal('');
  readonly modalError = signal<string | null>(null);
  modalMotivoTexto = '';

  readonly dashboard = signal<MatrizRiesgoDashboard | null>(null);
  readonly reporte = signal<MatricesRiesgoReporte | null>(null);
  readonly metodologia = signal<MetodologiaMatrices | null>(null);
  readonly matrices = signal<MatrizRiesgoResumen[]>([]);
  readonly matrizSeleccionada = signal<MatrizRiesgoDetalle | null>(null);
  readonly historial = signal<MatrizRiesgoHistorial[]>([]);
  readonly criterios = signal<MatrizRiesgoCriterio[]>([]);
  readonly planesAccion = signal<MatrizRiesgoPlanAccion[]>([]);
  readonly evidencias = signal<MatrizRiesgoEvidencia[]>([]);
  readonly matrizEditandoId = signal<number | null>(null);
  readonly matricesDuplicadas = signal<MatrizRiesgoResumen[]>([]);
  readonly buscandoDuplicados = signal(false);

  readonly filtroBuscar = signal('');
  readonly filtroEstado = signal('');
  readonly filtroSujetoTipo = signal('');
  readonly incluirCriteriosInactivos = signal(false);
  readonly reporteFiltro = signal<MatrizRiesgoReporteFiltro>({});
  readonly reporteFiltrosActivos = computed(() =>
    Object.values(this.reporteFiltro()).some(valor => `${valor ?? ''}`.trim() !== '')
  );

  nuevaMatriz = {
    sujetoTipo: 'PROVEEDOR',
    sujetoIdExt: '',
    documento: '',
    nombreSujeto: '',
    origenDatos: 'CAPTURA'
  };

  nuevoControl = {
    factorId: null as number | null,
    nombre: '',
    descripcion: '',
    periodicidad: '',
    oportunidad: '',
    automatizacion: '',
    procedimientos: '',
    calidad: '',
    efectividadPct: 0,
    responsable: '',
    evidenciaObligatoria: false
  };

  criteriosForm: MatrizRiesgoCriterioRequest = {
    variableId: 0,
    escalaId: null,
    valorDesde: null,
    valorHasta: null,
    puntaje: 0,
    descripcion: ''
  };

  planForm: MatrizRiesgoPlanAccionRequest = {
    resultadoId: null,
    actividad: '',
    responsable: '',
    periodicidad: '',
    fechaInicio: '',
    fechaFin: '',
    medioPrueba: '',
    observaciones: ''
  };

  readonly planEditandoId = signal<number | null>(null);
  evidenciaArchivo: File | null = null;
  evidenciaPlanId: number | null = null;
  evidenciaControlId: number | null = null;

  readonly criterioEditandoId = signal<number | null>(null);
  readonly capturasVariables = signal<CapturaVariable[]>([]);

  // Estados visibles para operación diaria. Los estados técnicos de cálculo
  // quedan fuera de filtros y botones porque el sistema recalcula al guardar.
  readonly estadosDisponibles = ['EN_REVISION', 'APROBADA', 'CERRADA', 'INACTIVA'];
  readonly estadosGestionables = ['EN_REVISION', 'APROBADA', 'CERRADA', 'INACTIVA'];
  readonly estadosPlan = ['PENDIENTE', 'EN_PROCESO', 'CERRADO', 'VENCIDO'];
  readonly tiposSujeto = [
    { valor: 'PROVEEDOR', texto: 'Proveedor' },
    { valor: 'CLIENTE_PATRONO', texto: 'Cliente / Patrono' },
    { valor: 'EMPLEADO', texto: 'Empleado' },
    { valor: 'INSTITUCIONAL', texto: 'Institucional' }
  ];

  constructor() {
    effect(onCleanup => {
      if (!this.error()) return;
      const timer = setTimeout(() => this.error.set(null), 6500);
      onCleanup(() => clearTimeout(timer));
    });

    effect(onCleanup => {
      if (!this.mensaje()) return;
      const timer = setTimeout(() => this.mensaje.set(null), 4200);
      onCleanup(() => clearTimeout(timer));
    });
  }

  readonly variablesPorFactor = computed(() => {
    const grupos = new Map<string, { factorId: number; factorCodigo: string; factorNombre: string; variables: VariableMetodologia[] }>();
    for (const variable of this.metodologia()?.variables ?? []) {
      const key = variable.factorCodigo;
      if (!grupos.has(key)) {
        grupos.set(key, {
          factorId: variable.factorId,
          factorCodigo: variable.factorCodigo,
          factorNombre: variable.factorNombre,
          variables: []
        });
      }
      grupos.get(key)!.variables.push(variable);
    }
    return Array.from(grupos.values());
  });

  readonly progresoCaptura = computed(() => {
    const capturas = this.capturasVariables();
    const total = capturas.length;
    const completas = capturas.filter(c => c.puntaje !== null && c.puntaje !== undefined).length;
    return { total, completas, pendiente: Math.max(0, total - completas) };
  });

  readonly escalasCriterio = computed(() =>
    (this.metodologia()?.escalasCatalogo ?? []).filter(e => e.tipo === 'VARIABLE' || e.tipo === 'INHERENTE' || e.tipo === 'RESIDUAL')
  );

  readonly escalasRiesgoOrdenadas = computed(() =>
    [...(this.metodologia()?.escalasRiesgo ?? [])].sort((a, b) => a.valorMinimo - b.valorMinimo)
  );

  readonly mitigacionesPermitidasOrdenadas = computed(() => {
    const valores = this.metodologia()?.mitigacionesPermitidas?.length
      ? this.metodologia()?.mitigacionesPermitidas ?? []
      : [0, 10, 25, 40, 55];
    return [...new Set(valores.map(valor => Number(valor)))].sort((a, b) => a - b);
  });

  readonly resumenNiveles = computed(() => {
    const totalMatrices = this.dashboard()?.totalMatrices ?? 0;
    const conteos = new Map((this.dashboard()?.porNivelResidual ?? []).map((x: { nombre: string; total: number }) => [x.nombre.toUpperCase(), x.total]));
    return this.escalasRiesgoOrdenadas().map(e => ({
      ...e,
      total: conteos.get(e.nivel.toUpperCase()) ?? 0,
      porcentaje: totalMatrices > 0 ? ((conteos.get(e.nivel.toUpperCase()) ?? 0) / totalMatrices) * 100 : 0
    }));
  });

  readonly heatmapFilas = computed(() => {
    const niveles = this.escalasRiesgoOrdenadas();
    const colores = niveles.length > 0 ? niveles.map(n => n.color || this.colorNivel(n.nivel)) : ['#4caf50', '#8bc34a', '#ffc107', '#ff9800', '#f44336'];
    const etiquetas = ['Frecuente', 'Probable', 'Ocasional', 'Posible', 'Improbable'];
    return etiquetas.map((frecuencia, fila) => ({
      frecuencia,
      celdas: [0, 1, 2, 3, 4].map(col => {
        const idx = Math.min(4, Math.max(0, fila + col - 2));
        return { color: colores[idx] ?? '#e5e7eb', nivel: niveles[idx]?.nivel ?? 'Sin escala' };
      })
    }));
  });

  readonly mostrarHistorialDebajoListado = computed(() => {
    // Con pocos registros, el historial se coloca bajo el listado para aprovechar el espacio central.
    // Cuando la lista crece, permanece en el panel de detalle para evitar desplazamientos largos.
    return !!this.matrizSeleccionada() && this.matrices().length > 0 && this.matrices().length <= 4;
  });

  ngOnInit(): void {
    this.cargarTodo();
  }

  ngOnDestroy(): void {
    this.cerrarVistaPreviaEvidencia();
  }

  actualizarModulo(): void {
    this.error.set(null);
    this.mensaje.set(null);
    this.matricesDuplicadas.set([]);
    this.matrizSeleccionada.set(null);
    this.historial.set([]);
    this.limpiarFormularioMatriz();
    this.cargarTodo();
  }

  cargarTodo(): void {
    this.cargando.set(true);
    this.error.set(null);
    this.mensaje.set(null);

    // Carga base del módulo: la metodología define variables, criterios,
    // escalas y mitigaciones antes de consultar matrices, reportes y catálogos.
    this.service.metodologiaVigente().subscribe({
      next: metodologia => {
        this.metodologia.set(metodologia);
        this.prepararCapturaVariables();
        this.cargarDashboard();
        this.cargarReporte();
        this.cargarMatrices();
        this.cargarCriterios();
      },
      error: err => {
        this.error.set(this.obtenerMensajeError(err, 'No se pudo cargar la metodología vigente.'));
        this.cargando.set(false);
      }
    });
  }

  iniciarNuevaMatriz(): void {
    this.limpiarFormularioMatriz();
    this.tab.set('nueva');
  }

  cargarDashboard(): void {
    this.service.dashboard().subscribe({
      next: datos => this.dashboard.set(datos),
      error: err => this.error.set(this.obtenerMensajeError(err, 'No se pudo cargar el dashboard.'))
    });
  }

  cargarReporte(): void {
    this.cargandoReporte.set(true);
    this.service.reporte(this.reporteFiltro()).subscribe({
      next: datos => {
        this.reporte.set(datos);
        this.cargandoReporte.set(false);
      },
      error: err => {
        this.error.set(this.obtenerMensajeError(err, 'No se pudo cargar la reportería.'));
        this.cargandoReporte.set(false);
      }
    });
  }

  actualizarFiltroReporte(campo: keyof MatrizRiesgoReporteFiltro, valor: string): void {
    this.reporteFiltro.set({ ...this.reporteFiltro(), [campo]: valor?.trim?.() ?? valor });
    this.programarCargaReporte();
  }

  actualizarFiltroMatrices(campo: 'buscar' | 'estado' | 'sujetoTipo', valor: string): void {
    if (campo === 'buscar') this.filtroBuscar.set(valor);
    if (campo === 'estado') this.filtroEstado.set(valor);
    if (campo === 'sujetoTipo') this.filtroSujetoTipo.set(valor);
    this.programarCargaMatrices();
  }

  limpiarFiltrosReporte(): void {
    this.reporteFiltro.set({});
    this.cargarReporte();
  }

  private programarCargaReporte(): void {
    // Evita llamadas repetidas mientras el usuario escribe o cambia filtros.
    if (this.reporteFiltroTimer) clearTimeout(this.reporteFiltroTimer);
    this.reporteFiltroTimer = setTimeout(() => this.cargarReporte(), 350);
  }

  private programarCargaMatrices(): void {
    // Mantiene la búsqueda automática sin saturar el API por cada pulsación.
    if (this.matricesFiltroTimer) clearTimeout(this.matricesFiltroTimer);
    this.matricesFiltroTimer = setTimeout(() => this.cargarMatrices(), 300);
  }

  private programarBusquedaDuplicadosMatriz(): void {
    // En edición no se valida contra sí misma; la duplicidad solo bloquea altas nuevas.
    if (this.matrizEditandoId()) {
      this.matricesDuplicadas.set([]);
      return;
    }

    if (this.duplicadosTimer) clearTimeout(this.duplicadosTimer);
    this.duplicadosTimer = setTimeout(() => this.buscarDuplicadosMatriz(), 320);
  }

  private buscarDuplicadosMatriz(): void {
    const termino = this.terminoBusquedaDuplicadoMatriz();
    if (!termino || termino.length < 3) {
      this.matricesDuplicadas.set([]);
      this.buscandoDuplicados.set(false);
      return;
    }

    this.buscandoDuplicados.set(true);
    this.service.listar({ buscar: termino }).subscribe({
      next: datos => {
        this.matricesDuplicadas.set(datos.filter(matriz => this.esDuplicadoMatriz(matriz)));
        this.buscandoDuplicados.set(false);
      },
      error: () => {
        this.matricesDuplicadas.set([]);
        this.buscandoDuplicados.set(false);
      }
    });
  }

  private terminoBusquedaDuplicadoMatriz(): string {
    return [
      this.nuevaMatriz.documento,
      this.nuevaMatriz.sujetoIdExt
    ].map(x => x.trim()).find(x => x.length >= 3) ?? '';
  }

  private esDuplicadoMatriz(matriz: MatrizRiesgoResumen): boolean {
    if (matriz.matrizId === this.matrizEditandoId()) return false;

    const documento = this.normalizarComparacion(this.nuevaMatriz.documento);
    const identificador = this.normalizarComparacion(this.nuevaMatriz.sujetoIdExt);

    return (!!documento && documento === this.normalizarComparacion(matriz.documento || ''))
      || (!!identificador && identificador === this.normalizarComparacion(matriz.sujetoIdExt || ''));
  }

  private normalizarComparacion(valor: string): string {
    return `${valor ?? ''}`.trim().replace(/\s+/g, ' ').toUpperCase();
  }

  exportarReporte(formato: 'EXCEL' | 'PDF'): void {
    this.guardando.set(true);
    this.service.exportarReporte(this.reporteFiltro(), formato).subscribe({
      next: blob => {
        if (formato === 'EXCEL') {
          this.generarExcelReporte();
        } else {
          this.generarPdfReporte();
        }
        this.mensaje.set(`Reporte ${formato} generado correctamente.`);
        this.guardando.set(false);
      },
      error: err => {
        this.error.set(this.obtenerMensajeError(err, 'No se pudo exportar el reporte.'));
        this.guardando.set(false);
      }
    });
  }

  cargarMatrices(): void {
    this.cargando.set(true);
    this.service.listar({
      buscar: this.filtroBuscar(),
      estado: this.filtroEstado(),
      sujetoTipo: this.filtroSujetoTipo()
    }).subscribe({
      next: datos => {
        this.matrices.set(datos);
        this.cargando.set(false);
      },
      error: err => {
        this.error.set(this.obtenerMensajeError(err, 'No se pudo listar matrices.'));
        this.cargando.set(false);
      }
    });
  }

  cargarCriterios(): void {
    this.service.listarCriterios(this.incluirCriteriosInactivos()).subscribe({
      next: datos => {
        this.criterios.set(datos);
        this.aplicarCriteriosAutomaticos();
      },
      error: err => this.error.set(this.obtenerMensajeError(err, 'No se pudieron cargar los criterios.'))
    });
  }

  seleccionarMatriz(id: number): void {
    this.cargando.set(true);
    this.service.obtener(id).subscribe({
      next: matriz => {
        this.matrizSeleccionada.set(matriz);
        this.planesAccion.set(matriz.planesAccion ?? []);
        this.evidencias.set(matriz.evidencias ?? []);
        this.cargarHistorial(id);
        this.cargando.set(false);
      },
      error: err => {
        this.error.set(this.obtenerMensajeError(err, 'No se pudo consultar la matriz.'));
        this.cargando.set(false);
      }
    });
  }

  cargarHistorial(id: number): void {
    this.service.historial(id).subscribe({
      next: datos => this.historial.set(this.deduplicarHistorial(datos)),
      error: () => this.historial.set([])
    });
  }

  seleccionarMatrizParaPlanes(id: number | string | null): void {
    const matrizId = id === null || id === '' ? null : Number(id);
    if (!matrizId) {
      this.matrizSeleccionada.set(null);
      this.planesAccion.set([]);
      this.evidencias.set([]);
      this.limpiarFormularioPlan();
      return;
    }

    this.seleccionarMatriz(matrizId);
    this.cargarPlanesYEvidencias(matrizId);
  }

  cargarPlanesYEvidencias(matrizId?: number): void {
    const id = matrizId ?? this.matrizSeleccionada()?.matrizId;
    if (!id) return;

    this.service.listarPlanes(id).subscribe({
      next: datos => this.planesAccion.set(datos),
      error: err => this.error.set(this.obtenerMensajeError(err, 'No se pudieron cargar los planes de acción.'))
    });

    this.service.listarEvidencias(id).subscribe({
      next: datos => this.evidencias.set(datos),
      error: err => this.error.set(this.obtenerMensajeError(err, 'No se pudieron cargar las evidencias.'))
    });
  }

  crearMatriz(): void {
    const dto = this.construirDtoMatriz();
    if (!dto) return;

    // Control preventivo de duplicidad: identificador externo o documento no deben
    // crear una segunda matriz activa para el mismo sujeto evaluado.
    if (!this.matrizEditandoId() && this.matricesDuplicadas().length > 0) {
      this.error.set('Ya existe una matriz activa con el mismo identificador externo o documento. Revise el registro existente antes de crear otro.');
      return;
    }

    this.guardando.set(true);
    const matrizId = this.matrizEditandoId();
    const request = matrizId
      ? this.service.actualizar(matrizId, dto)
      : this.service.crear(dto);

    request.subscribe({
      next: matriz => this.calcularAutomaticamenteDespuesDeGuardar(matriz, !!matrizId),
      error: err => {
        this.error.set(this.obtenerMensajeError(err, matrizId ? 'No se pudo actualizar la matriz.' : 'No se pudo crear la matriz.'));
        this.guardando.set(false);
      }
    });
  }

  actualizarCampoMatriz(campo: 'sujetoTipo' | 'sujetoIdExt' | 'documento' | 'nombreSujeto', valor: string): void {
    this.nuevaMatriz = { ...this.nuevaMatriz, [campo]: valor };
    if (campo === 'sujetoTipo') {
      this.prepararCapturaVariables();
      this.ajustarFactorControlAlTipoSujeto();
    }
    this.programarBusquedaDuplicadosMatriz();
  }

  cancelarEdicionMatriz(): void {
    this.limpiarFormularioMatriz();
    this.matricesDuplicadas.set([]);
    this.tab.set('matrices');
  }

  private construirDtoMatriz(): MatrizRiesgoCrearRequest | null {
    const progreso = this.progresoCaptura();
    if (progreso.total === 0) {
      this.error.set('No existen variables configuradas para el tipo de sujeto seleccionado.');
      return null;
    }

    if (progreso.pendiente > 0) {
      this.error.set(`Debe completar las ${progreso.total} variables de ${this.etiquetaTipoSujeto(this.nuevaMatriz.sujetoTipo)} antes de guardar y calcular. Faltan ${progreso.pendiente}.`);
      return null;
    }

    // Solo se envían las variables que corresponden al tipo de sujeto seleccionado.
    // La ponderación y clasificación final se conservan en el proceso de cálculo del API.
    const detalles = this.capturasVariables()
      .map(x => ({
        variableId: x.variableId,
        puntaje: Number(x.puntaje),
        valorCapturado: x.valorCapturado || null,
        justificacion: x.justificacion || null,
        fuenteDato: x.fuenteDato || null
      }));

    if (!this.nuevaMatriz.nombreSujeto.trim()) {
      this.error.set('El nombre del sujeto evaluado es obligatorio.');
      return null;
    }

    const errorCriterios = this.validarCapturasContraCriterios();
    if (errorCriterios) {
      this.error.set(errorCriterios);
      return null;
    }

    return {
      ...this.nuevaMatriz,
      sujetoIdExt: this.nuevaMatriz.sujetoIdExt || null,
      documento: this.nuevaMatriz.documento || null,
      detalles,
      controles: this.nuevoControl.nombre.trim()
        ? [{
            factorId: this.nuevoControl.factorId ?? this.factorCapturaActual()?.factorId ?? null,
            nombre: this.nuevoControl.nombre,
            descripcion: this.nuevoControl.descripcion || null,
            periodicidad: this.nuevoControl.periodicidad || null,
            oportunidad: this.nuevoControl.oportunidad || null,
            automatizacion: this.nuevoControl.automatizacion || null,
            procedimientos: this.nuevoControl.procedimientos || null,
            calidad: this.nuevoControl.calidad || null,
            efectividadPct: Number(this.nuevoControl.efectividadPct),
            responsable: this.nuevoControl.responsable || null,
            evidenciaObligatoria: this.nuevoControl.evidenciaObligatoria
          }]
        : []
    };
  }

  editarMatriz(matriz: MatrizRiesgoResumen | MatrizRiesgoDetalle): void {
    this.cargando.set(true);
    this.service.obtener(matriz.matrizId).subscribe({
      next: detalle => {
        // La edición reconstruye la captura con las variables vigentes del tipo
        // de sujeto, conserva valores registrados y recalcula al guardar.
        this.matrizEditandoId.set(detalle.matrizId);
        this.nuevaMatriz = {
          sujetoTipo: detalle.sujetoTipo,
          sujetoIdExt: detalle.sujetoIdExt || '',
          documento: detalle.documento || '',
          nombreSujeto: detalle.nombreSujeto,
          origenDatos: detalle.origenDatos || 'CAPTURA'
        };
        const variablesEdicion = this.variablesParaTipoSujeto(detalle.sujetoTipo);
        this.capturasVariables.set(variablesEdicion.map(variable => {
          const valor = detalle.detalles.find(d => d.variableId === variable.variableId);
          return {
            variableId: variable.variableId,
            criterioId: null,
            puntaje: valor?.puntaje ?? null,
            valorCapturado: valor?.valorCapturado ?? '',
            justificacion: valor?.justificacion ?? '',
            fuenteDato: valor?.fuenteDato ?? 'CAPTURA'
          };
        }));
        this.aplicarCriteriosAutomaticos();
        this.matricesDuplicadas.set([]);
        this.tab.set('nueva');
        this.matrizSeleccionada.set(detalle);
        this.cargando.set(false);
      },
      error: err => {
        this.error.set(this.obtenerMensajeError(err, 'No se pudo cargar la matriz para edición.'));
        this.cargando.set(false);
      }
    });
  }

  calcularMatriz(matriz: MatrizRiesgoResumen): void {
    this.abrirModal({
      tipo: 'calcular',
      titulo: 'Calcular matriz',
      descripcion: `Se evaluará la matriz ${matriz.matrizId} con los criterios vigentes. Revise que la información capturada esté completa antes de continuar.`,
      textoConfirmar: 'Calcular',
      requiereMotivo: false,
      matriz,
      tono: 'normal'
    });
  }

  cambiarEstado(matriz: MatrizRiesgoResumen, estado: string): void {
    const activandoMatriz = matriz.estado === 'INACTIVA' && estado === 'EN_REVISION';
    const inactivandoMatriz = estado === 'INACTIVA';
    this.abrirModal({
      tipo: 'estado',
      titulo: activandoMatriz ? 'Activar matriz' : inactivandoMatriz ? 'Inactivar matriz' : 'Cambiar estado',
      descripcion: activandoMatriz
        ? `Ingrese el motivo obligatorio para activar la matriz ${matriz.matrizId}. El estado volverá a En Revisión.`
        : `Ingrese el motivo obligatorio para cambiar la matriz ${matriz.matrizId} al estado ${this.estadoEtiqueta(estado)}.`,
      textoConfirmar: activandoMatriz ? 'Activar' : inactivandoMatriz ? 'Inactivar' : 'Cambiar estado',
      requiereMotivo: true,
      matriz,
      estado,
      tono: inactivandoMatriz ? 'peligro' : activandoMatriz ? 'normal' : 'advertencia'
    });
  }

  eliminarMatriz(matriz: MatrizRiesgoResumen): void {
    this.abrirModal({
      tipo: 'eliminarMatriz',
      titulo: 'Eliminar matriz',
      descripcion: `Ingrese el motivo obligatorio para retirar la matriz ${matriz.matrizId} de la operación diaria. La información se conservará para consulta histórica.`,
      textoConfirmar: 'Eliminar matriz',
      requiereMotivo: true,
      matriz,
      tono: 'peligro'
    });
  }

  abrirModal(operacion: ModalOperacion): void {
    this.modalOperacion.set(operacion);
    this.modalMotivo.set('');
    this.modalMotivoTexto = '';
    this.modalError.set(null);
  }

  cerrarModal(): void {
    if (this.guardando()) return;
    this.modalOperacion.set(null);
    this.modalMotivo.set('');
    this.modalMotivoTexto = '';
    this.modalError.set(null);
  }

  actualizarModalMotivo(valorIngresado: string): void {
    const valor = (valorIngresado ?? '').slice(0, 1000);
    this.modalMotivoTexto = valor;
    this.modalMotivo.set(valor);
    this.modalError.set(null);
  }

  contadorModalMotivo(): number {
    return this.modalMotivoTexto.length;
  }

  confirmarModal(): void {
    const operacion = this.modalOperacion();
    if (!operacion) return;

    const motivo = this.modalMotivoTexto.trim();
    if (operacion.requiereMotivo && !motivo) {
      this.modalError.set('El motivo es obligatorio para completar esta acción.');
      return;
    }

    if (operacion.tipo === 'estado' && this.existeMotivoCambioEstado(motivo)) {
      this.modalError.set('Este motivo ya fue utilizado en un cambio de estado de esta matriz. Ingrese un motivo diferente.');
      return;
    }

    this.modalError.set(null);
    switch (operacion.tipo) {
      case 'calcular':
        this.ejecutarCalculo(operacion.matriz!);
        break;
      case 'estado':
        this.ejecutarCambioEstado(operacion.matriz!, operacion.estado!, motivo);
        break;
      case 'eliminarMatriz':
        this.ejecutarEliminacionMatriz(operacion.matriz!, motivo);
        break;
      case 'inactivarCriterio':
        this.ejecutarInactivacionCriterio(operacion.criterio!, motivo);
        break;
      case 'eliminarCriterio':
        this.ejecutarEliminacionCriterio(operacion.criterio!, motivo);
        break;
      case 'estadoPlan':
        this.ejecutarCambioEstadoPlan(operacion.plan!, operacion.estado!, motivo);
        break;
      case 'inactivarPlan':
        this.ejecutarInactivacionPlan(operacion.plan!, motivo);
        break;
      case 'reactivarPlan':
        this.ejecutarReactivacionPlan(operacion.plan!, motivo);
        break;
      case 'inactivarEvidencia':
        this.ejecutarInactivacionEvidencia(operacion.evidencia!, motivo);
        break;
    }
  }

  guardarPlanAccion(): void {
    const matriz = this.matrizSeleccionada();
    if (!matriz) {
      this.error.set('Seleccione una matriz antes de registrar el plan de acción.');
      return;
    }

    if (!this.planForm.actividad.trim() || !this.planForm.responsable.trim()) {
      this.error.set('La actividad y el responsable del plan son obligatorios.');
      return;
    }

    this.guardando.set(true);
    const dto: MatrizRiesgoPlanAccionRequest = {
      resultadoId: this.planForm.resultadoId || null,
      actividad: this.planForm.actividad.trim(),
      responsable: this.planForm.responsable.trim(),
      periodicidad: this.planForm.periodicidad || null,
      fechaInicio: this.planForm.fechaInicio || null,
      fechaFin: this.planForm.fechaFin || null,
      medioPrueba: this.planForm.medioPrueba || null,
      observaciones: this.planForm.observaciones || null
    };

    const planId = this.planEditandoId();
    const request = planId
      ? this.service.actualizarPlan(matriz.matrizId, planId, dto)
      : this.service.crearPlan(matriz.matrizId, dto);

    request.subscribe({
      next: () => {
        this.mensaje.set(planId ? 'Plan de acción actualizado correctamente.' : 'Plan de acción registrado correctamente.');
        this.limpiarFormularioPlan();
        this.cargarPlanesYEvidencias(matriz.matrizId);
        this.cargarReporte();
        this.guardando.set(false);
      },
      error: err => {
        this.error.set(this.obtenerMensajeError(err, 'No se pudo guardar el plan de acción.'));
        this.guardando.set(false);
      }
    });
  }

  editarPlan(plan: MatrizRiesgoPlanAccion): void {
    this.planEditandoId.set(plan.planId);
    this.planForm = {
      resultadoId: plan.resultadoId ?? null,
      actividad: plan.actividad,
      responsable: plan.responsable,
      periodicidad: plan.periodicidad || '',
      fechaInicio: plan.fechaInicio ? plan.fechaInicio.substring(0, 10) : '',
      fechaFin: plan.fechaFin ? plan.fechaFin.substring(0, 10) : '',
      medioPrueba: plan.medioPrueba || '',
      observaciones: plan.observaciones || ''
    };
  }

  limpiarFormularioPlan(): void {
    this.planEditandoId.set(null);
    this.planForm = {
      resultadoId: null,
      actividad: '',
      responsable: '',
      periodicidad: '',
      fechaInicio: '',
      fechaFin: '',
      medioPrueba: '',
      observaciones: ''
    };
  }

  cambiarEstadoPlan(plan: MatrizRiesgoPlanAccion, estado: string): void {
    this.abrirModal({
      tipo: 'estadoPlan',
      titulo: 'Cambiar estado del plan',
      descripcion: `Ingrese el motivo obligatorio para cambiar el plan ${plan.planId} al estado ${estado}.`,
      textoConfirmar: 'Cambiar estado',
      requiereMotivo: true,
      estado,
      plan,
      tono: estado === 'CERRADO' ? 'normal' : 'advertencia'
    });
  }

  inactivarPlan(plan: MatrizRiesgoPlanAccion): void {
    this.abrirModal({
      tipo: 'inactivarPlan',
      titulo: 'Inactivar plan',
      descripcion: `Ingrese el motivo obligatorio para inactivar el plan ${plan.planId}.`,
      textoConfirmar: 'Inactivar',
      requiereMotivo: true,
      plan,
      tono: 'peligro'
    });
  }

  reactivarPlan(plan: MatrizRiesgoPlanAccion): void {
    this.abrirModal({
      tipo: 'reactivarPlan',
      titulo: 'Reactivar plan',
      descripcion: `Ingrese el motivo obligatorio para reactivar el plan ${plan.planId}. El estado volverá a PENDIENTE.`,
      textoConfirmar: 'Reactivar',
      requiereMotivo: true,
      plan,
      tono: 'normal'
    });
  }

  seleccionarArchivoEvidencia(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.evidenciaArchivo = input.files?.item(0) ?? null;
  }

  vistaPreviaArchivoSeleccionado(): void {
    if (!this.evidenciaArchivo) {
      this.error.set('Seleccione un archivo para visualizar.');
      return;
    }
    if (!this.validarTamanoVistaPrevia(this.evidenciaArchivo.size)) return;
    void this.crearVistaPreviaDesdeBlob(this.evidenciaArchivo, this.evidenciaArchivo.name, this.evidenciaArchivo.type);
  }

  cargarEvidencia(): void {
    const matriz = this.matrizSeleccionada();
    if (!matriz || !this.evidenciaArchivo) {
      this.error.set('Seleccione una matriz y un archivo de evidencia.');
      return;
    }

    this.guardando.set(true);
    this.service.cargarEvidencia(matriz.matrizId, this.evidenciaArchivo, this.evidenciaControlId, this.evidenciaPlanId).subscribe({
      next: () => {
        this.mensaje.set('Evidencia registrada correctamente.');
        this.evidenciaArchivo = null;
        this.evidenciaPlanId = null;
        this.evidenciaControlId = null;
        this.cargarPlanesYEvidencias(matriz.matrizId);
        this.seleccionarMatriz(matriz.matrizId);
        this.guardando.set(false);
      },
      error: err => {
        this.error.set(this.obtenerMensajeError(err, 'No se pudo cargar la evidencia.'));
        this.guardando.set(false);
      }
    });
  }

  descargarEvidencia(evidencia: MatrizRiesgoEvidencia): void {
    const matriz = this.matrizSeleccionada();
    if (!matriz) return;

    this.guardando.set(true);
    this.service.descargarEvidencia(matriz.matrizId, evidencia.evidenciaId).subscribe({
      next: blob => {
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = evidencia.nombreOriginal;
        link.click();
        URL.revokeObjectURL(url);
        this.mensaje.set('Evidencia descargada correctamente.');
        this.guardando.set(false);
      },
      error: err => {
        this.error.set(this.obtenerMensajeError(err, 'No se pudo descargar la evidencia.'));
        this.guardando.set(false);
      }
    });
  }

  vistaPreviaEvidencia(evidencia: MatrizRiesgoEvidencia): void {
    const matriz = this.matrizSeleccionada();
    if (!matriz || !evidencia.activa) return;
    if (!this.validarTamanoVistaPrevia(evidencia.tamanoBytes)) return;

    this.cerrarVistaPreviaEvidencia();
    this.evidenciaPreview.set({
      nombre: evidencia.nombreOriginal,
      tipoMime: evidencia.tipoMime || 'application/octet-stream',
      tamanoBytes: evidencia.tamanoBytes,
      url: null,
      urlSegura: null,
      tipoVista: this.tipoVistaPorMime(evidencia.tipoMime || '', evidencia.nombreOriginal),
      cargando: true
    });

    this.service.descargarEvidencia(matriz.matrizId, evidencia.evidenciaId).subscribe({
      next: blob => {
        void this.crearVistaPreviaDesdeBlob(blob, evidencia.nombreOriginal, blob.type || evidencia.tipoMime || 'application/octet-stream');
      },
      error: err => {
        this.evidenciaPreview.update(actual => actual ? {
          ...actual,
          cargando: false,
          error: this.obtenerMensajeError(err, 'No se pudo generar la vista previa de la evidencia.')
        } : actual);
      }
    });
  }

  cerrarVistaPreviaEvidencia(): void {
    const actual = this.evidenciaPreview();
    if (actual?.url) {
      URL.revokeObjectURL(actual.url);
    }
    this.evidenciaPreview.set(null);
  }

  descargarVistaPreviaActual(): void {
    const actual = this.evidenciaPreview();
    if (!actual?.url) return;
    const link = document.createElement('a');
    link.href = actual.url;
    link.download = actual.nombre;
    link.click();
  }

  inactivarEvidencia(evidencia: MatrizRiesgoEvidencia): void {
    this.abrirModal({
      tipo: 'inactivarEvidencia',
      titulo: 'Eliminar evidencia',
      descripcion: `Ingrese el motivo obligatorio para eliminar lógicamente la evidencia ${evidencia.evidenciaId}. El archivo físico se conserva.`,
      textoConfirmar: 'Eliminar',
      requiereMotivo: true,
      evidencia,
      tono: 'peligro'
    });
  }

  private ejecutarCambioEstadoPlan(plan: MatrizRiesgoPlanAccion, estado: string, motivo: string): void {
    const matrizId = this.matrizSeleccionada()?.matrizId;
    if (!matrizId) return;

    this.guardando.set(true);
    this.service.cambiarEstadoPlan(matrizId, plan.planId, estado, motivo).subscribe({
      next: () => {
        this.mensaje.set('Estado del plan actualizado correctamente.');
        this.cargarPlanesYEvidencias(matrizId);
        this.cargarReporte();
        this.guardando.set(false);
        this.cerrarModal();
      },
      error: err => this.finalizarAccionConError(err, 'No se pudo cambiar el estado del plan.')
    });
  }

  private ejecutarInactivacionPlan(plan: MatrizRiesgoPlanAccion, motivo: string): void {
    const matrizId = this.matrizSeleccionada()?.matrizId;
    if (!matrizId) return;

    this.guardando.set(true);
    this.service.inactivarPlan(matrizId, plan.planId, motivo).subscribe({
      next: () => {
        this.mensaje.set('Plan de acción inactivado correctamente.');
        this.cargarPlanesYEvidencias(matrizId);
        this.cargarReporte();
        this.guardando.set(false);
        this.cerrarModal();
      },
      error: err => this.finalizarAccionConError(err, 'No se pudo inactivar el plan.')
    });
  }

  private ejecutarReactivacionPlan(plan: MatrizRiesgoPlanAccion, motivo: string): void {
    const matrizId = this.matrizSeleccionada()?.matrizId;
    if (!matrizId) return;

    this.guardando.set(true);
    this.service.reactivarPlan(matrizId, plan.planId, motivo).subscribe({
      next: () => {
        this.mensaje.set('Plan de acción reactivado correctamente.');
        this.cargarPlanesYEvidencias(matrizId);
        this.cargarReporte();
        this.guardando.set(false);
        this.cerrarModal();
      },
      error: err => this.finalizarAccionConError(err, 'No se pudo reactivar el plan.')
    });
  }

  private ejecutarInactivacionEvidencia(evidencia: MatrizRiesgoEvidencia, motivo: string): void {
    const matrizId = this.matrizSeleccionada()?.matrizId;
    if (!matrizId) return;

    this.guardando.set(true);
    this.service.inactivarEvidencia(matrizId, evidencia.evidenciaId, motivo).subscribe({
      next: () => {
        this.mensaje.set('Evidencia inactivada correctamente.');
        this.cargarPlanesYEvidencias(matrizId);
        this.seleccionarMatriz(matrizId);
        this.guardando.set(false);
        this.cerrarModal();
      },
      error: err => this.finalizarAccionConError(err, 'No se pudo inactivar la evidencia.')
    });
  }

  guardarCriterio(): void {
    if (!this.criteriosForm.variableId || !this.criteriosForm.descripcion.trim()) {
      this.error.set('La variable y la descripción del criterio son obligatorias.');
      return;
    }

    if (this.criteriosForm.valorDesde !== null && this.criteriosForm.valorHasta !== null
      && Number(this.criteriosForm.valorDesde) > Number(this.criteriosForm.valorHasta)) {
      this.error.set('El valor desde no puede ser mayor que el valor hasta.');
      return;
    }

    this.guardando.set(true);
    const dto: MatrizRiesgoCriterioRequest = {
      variableId: Number(this.criteriosForm.variableId),
      escalaId: this.criteriosForm.escalaId ? Number(this.criteriosForm.escalaId) : null,
      valorDesde: this.criteriosForm.valorDesde === null || this.criteriosForm.valorDesde === undefined ? null : Number(this.criteriosForm.valorDesde),
      valorHasta: this.criteriosForm.valorHasta === null || this.criteriosForm.valorHasta === undefined ? null : Number(this.criteriosForm.valorHasta),
      puntaje: Number(this.criteriosForm.puntaje),
      descripcion: this.criteriosForm.descripcion.trim()
    };

    const id = this.criterioEditandoId();
    const request = id
      ? this.service.actualizarCriterio(id, dto)
      : this.service.crearCriterio(dto);

    request.subscribe({
      next: () => {
        this.mensaje.set(id ? 'Criterio actualizado correctamente.' : 'Criterio registrado correctamente.');
        this.limpiarFormularioCriterio();
        this.cargarCriterios();
        this.aplicarCriteriosAutomaticos();
        this.guardando.set(false);
      },
      error: err => {
        this.error.set(this.obtenerMensajeError(err, 'No se pudo guardar el criterio.'));
        this.guardando.set(false);
      }
    });
  }

  editarCriterio(criterio: MatrizRiesgoCriterio): void {
    this.criterioEditandoId.set(criterio.criterioId);
    this.criteriosForm = {
      variableId: criterio.variableId,
      escalaId: criterio.escalaId ?? null,
      valorDesde: criterio.valorDesde ?? null,
      valorHasta: criterio.valorHasta ?? null,
      puntaje: criterio.puntaje,
      descripcion: criterio.descripcion
    };
  }

  inactivarCriterio(criterio: MatrizRiesgoCriterio): void {
    this.abrirModal({
      tipo: 'inactivarCriterio',
      titulo: 'Inactivar criterio',
      descripcion: `Ingrese el motivo obligatorio para inactivar el criterio ${criterio.criterioId}.`,
      textoConfirmar: 'Inactivar',
      requiereMotivo: true,
      criterio,
      tono: 'peligro'
    });
  }

  eliminarCriterio(criterio: MatrizRiesgoCriterio): void {
    this.abrirModal({
      tipo: 'eliminarCriterio',
      titulo: 'Eliminar criterio',
      descripcion: `Ingrese el motivo obligatorio para eliminar definitivamente el criterio ${criterio.criterioId}. Esta acción retira el criterio del catálogo.`,
      textoConfirmar: 'Eliminar',
      requiereMotivo: true,
      criterio,
      tono: 'peligro'
    });
  }
  limpiarFormularioCriterio(): void {
    this.criterioEditandoId.set(null);
    this.criteriosForm = {
      variableId: 0,
      escalaId: null,
      valorDesde: null,
      valorHasta: null,
      puntaje: 0,
      descripcion: ''
    };
  }

  variableNombre(variableId: number): string {
    const variable = this.metodologia()?.variables.find(v => v.variableId === Number(variableId));
    return variable ? `${variable.factorCodigo} - ${variable.nombre}` : 'Variable';
  }

  criteriosVariable(variableId: number): MatrizRiesgoCriterio[] {
    return this.criterios()
      .filter(c => c.activo && c.variableId === Number(variableId))
      .sort((a, b) => (a.valorDesde ?? -999999) - (b.valorDesde ?? -999999));
  }

  criterioSeleccionado(captura: CapturaVariable): MatrizRiesgoCriterio | null {
    if (!captura.criterioId) return null;
    return this.criterios().find(c => c.criterioId === Number(captura.criterioId)) ?? null;
  }

  criterioSugerido(captura: CapturaVariable): MatrizRiesgoCriterio | null {
    const criterios = this.criteriosVariable(captura.variableId);
    if (criterios.length === 0) return null;

    const valor = this.numeroSeguro(captura.valorCapturado);
    if (valor === null) return null;

    return criterios.find(c => {
      const desde = c.valorDesde ?? Number.NEGATIVE_INFINITY;
      const hasta = c.valorHasta ?? Number.POSITIVE_INFINITY;
      return valor >= desde && valor <= hasta;
    }) ?? null;
  }

  actualizarValorCapturado(variableId: number, valor: string): void {
    const captura = this.capturasVariables().find(x => x.variableId === variableId);
    if (!captura) return;

    const sugerido = this.criterioSugerido({ ...captura, valorCapturado: valor });
    this.actualizarCaptura(variableId, {
      valorCapturado: valor,
      criterioId: sugerido?.criterioId ?? captura.criterioId,
      puntaje: sugerido ? Number(sugerido.puntaje) : captura.puntaje,
      justificacion: sugerido && !captura.justificacion.trim() ? sugerido.descripcion : captura.justificacion
    });
  }

  actualizarPuntaje(variableId: number, valor: number | string | null): void {
    const puntaje = valor === null || valor === '' ? null : Number(valor);
    this.actualizarCaptura(variableId, { puntaje });
  }

  actualizarJustificacion(variableId: number, valor: string): void {
    this.actualizarCaptura(variableId, { justificacion: valor });
  }

  actualizarFuente(variableId: number, valor: string): void {
    this.actualizarCaptura(variableId, { fuenteDato: valor });
  }

  seleccionarCriterio(variableId: number, criterioId: number | string | null): void {
    const id = criterioId === null || criterioId === '' ? null : Number(criterioId);
    const criterio = id ? this.criterios().find(c => c.criterioId === id) : null;
    if (!criterio) {
      this.actualizarCaptura(variableId, { criterioId: null });
      return;
    }

    const captura = this.capturasVariables().find(x => x.variableId === variableId);
    this.actualizarCaptura(variableId, {
      criterioId: criterio.criterioId,
      puntaje: Number(criterio.puntaje),
      justificacion: captura?.justificacion.trim() ? captura.justificacion : criterio.descripcion
    });
  }

  advertenciaCriterio(captura: CapturaVariable): string | null {
    const criterios = this.criteriosVariable(captura.variableId);
    if (criterios.length === 0 || captura.puntaje === null || captura.puntaje === undefined) return null;

    const seleccionado = this.criterioSeleccionado(captura);
    if (seleccionado && Number(captura.puntaje) !== Number(seleccionado.puntaje)) {
      return `El puntaje debe coincidir con el criterio seleccionado: ${seleccionado.puntaje}.`;
    }

    const sugerido = this.criterioSugerido(captura);
    if (sugerido && Number(captura.puntaje) !== Number(sugerido.puntaje)) {
      return `Según el valor capturado, el criterio aplicable sugiere ${sugerido.puntaje}.`;
    }

    if (!seleccionado && !sugerido) {
      return 'La variable tiene criterios activos; seleccione uno o capture un valor dentro de un rango definido.';
    }

    return null;
  }

  colorNivel(nivel?: string | null): string {
    const escala = this.escalasRiesgoOrdenadas().find(e => e.nivel.toUpperCase() === (nivel ?? '').toUpperCase());
    if (escala?.color) return escala.color;
    const normalizado = (nivel ?? '').toUpperCase();
    if (normalizado.includes('CRIT')) return '#dc2626';
    if (normalizado.includes('ALTO')) return '#f97316';
    if (normalizado.includes('MEDIO')) return '#facc15';
    if (normalizado.includes('BAJO')) return '#22c55e';
    return '#94a3b8';
  }

  estadoEtiqueta(estado?: string | null): string {
    const normalizado = (estado ?? '').trim().toUpperCase();
    const etiquetas: Record<string, string> = {
      BORRADOR: 'Borrador',
      EN_EVALUACION: 'En evaluación',
      CALCULADA: 'En Revisión',
      EN_REVISION: 'En Revisión',
      OBSERVADA: 'Observada',
      APROBADA: 'Aprobada',
      CERRADA: 'Cerrada',
      INACTIVA: 'Inactiva'
    };
    return etiquetas[normalizado] ?? (estado || '-');
  }

  estadosGestionablesParaMatriz(estadoActual?: string | null): string[] {
    // Si la matriz está inactiva, la única acción visible es activarla.
    // El estado operativo de reactivación es En Revisión para obligar revisión posterior.
    return (estadoActual ?? '').toUpperCase() === 'INACTIVA'
      ? ['EN_REVISION']
      : this.estadosGestionables;
  }

  puedeEliminarMatriz(matriz: MatrizRiesgoResumen | MatrizRiesgoDetalle): boolean {
    const estado = (matriz.estado ?? '').toUpperCase();
    // La eliminación lógica solo queda disponible antes del cierre operativo.
    // Una matriz aprobada, cerrada o ya inactiva forma parte del expediente y debe conservarse.
    return !['APROBADA', 'CERRADA', 'INACTIVA'].includes(estado);
  }

  mensajeBloqueoEliminarMatriz(matriz: MatrizRiesgoResumen | MatrizRiesgoDetalle): string {
    return this.puedeEliminarMatriz(matriz)
      ? 'Eliminar matriz'
      : 'La matriz no puede eliminarse porque ya fue aprobada, cerrada o se encuentra inactiva.';
  }

  textoBotonEstado(matriz: MatrizRiesgoResumen | MatrizRiesgoDetalle, estado: string): string {
    return matriz.estado === 'INACTIVA' && estado === 'EN_REVISION'
      ? 'Activar'
      : this.estadoEtiqueta(estado);
  }

  claseBotonModal(operacion: ModalOperacion | null): string {
    if (operacion?.tono === 'peligro') return 'bg-red-600 hover:bg-red-700 focus:ring-red-500';
    if (operacion?.tono === 'advertencia') return 'bg-amber-500 hover:bg-amber-600 focus:ring-amber-500';
    return 'bg-ihss-900 hover:bg-ihss-800 focus:ring-ihss-600';
  }

  puedeConfirmarModal(): boolean {
    const operacion = this.modalOperacion();
    if (!operacion || this.guardando()) return false;
    const motivo = this.modalMotivoTexto.trim();
    if (operacion.requiereMotivo && !motivo) return false;
    if (operacion.tipo === 'estado' && this.existeMotivoCambioEstado(motivo)) return false;
    return true;
  }

  private ejecutarCalculo(matriz: MatrizRiesgoResumen): void {
    this.guardando.set(true);
    this.service.calcular(matriz.matrizId, this.tipoCalculoParaSujeto(matriz.sujetoTipo)).subscribe({
      next: () => this.refrescarDespuesAccion(matriz.matrizId, 'Matriz calculada correctamente.'),
      error: err => this.finalizarAccionConError(err, 'No se pudo calcular la matriz.')
    });
  }

  private ejecutarCambioEstado(matriz: MatrizRiesgoResumen, estado: string, motivo: string): void {
    this.guardando.set(true);
    this.service.cambiarEstado(matriz.matrizId, estado, motivo).subscribe({
      next: () => this.refrescarDespuesAccion(matriz.matrizId, 'Estado actualizado correctamente.'),
      error: err => this.finalizarAccionConError(err, 'No se pudo cambiar el estado.')
    });
  }

  private ejecutarEliminacionMatriz(matriz: MatrizRiesgoResumen, motivo: string): void {
    this.guardando.set(true);
    this.service.eliminarMatriz(matriz.matrizId, motivo).subscribe({
      next: () => {
        this.mensaje.set('Matriz eliminada correctamente.');
        this.matrizSeleccionada.set(null);
        this.historial.set([]);
        this.limpiarFormularioMatriz();
        this.cargarDashboard();
        this.cargarReporte();
        this.cargarMatrices();
        this.guardando.set(false);
        this.cerrarModal();
      },
      error: err => this.finalizarAccionConError(err, 'No se pudo eliminar la matriz.')
    });
  }

  private ejecutarInactivacionCriterio(criterio: MatrizRiesgoCriterio, motivo: string): void {
    this.guardando.set(true);
    this.service.inactivarCriterio(criterio.criterioId, motivo).subscribe({
      next: () => {
        this.mensaje.set('Criterio inactivado correctamente.');
        this.cargarCriterios();
        this.guardando.set(false);
        this.cerrarModal();
      },
      error: err => this.finalizarAccionConError(err, 'No se pudo inactivar el criterio.')
    });
  }

  private ejecutarEliminacionCriterio(criterio: MatrizRiesgoCriterio, motivo: string): void {
    this.guardando.set(true);
    this.service.eliminarCriterio(criterio.criterioId, motivo).subscribe({
      next: () => {
        this.mensaje.set('Criterio eliminado correctamente.');
        this.limpiarFormularioCriterio();
        this.cargarCriterios();
        this.aplicarCriteriosAutomaticos();
        this.guardando.set(false);
        this.cerrarModal();
      },
      error: err => this.finalizarAccionConError(err, 'No se pudo eliminar el criterio.')
    });
  }

  private prepararCapturaVariables(): void {
    const existentes = new Map(this.capturasVariables().map(captura => [captura.variableId, captura]));
    this.capturasVariables.set(this.variablesParaTipoSujeto(this.nuevaMatriz.sujetoTipo).map(variable => ({
      variableId: variable.variableId,
      criterioId: existentes.get(variable.variableId)?.criterioId ?? null,
      puntaje: existentes.get(variable.variableId)?.puntaje ?? null,
      valorCapturado: existentes.get(variable.variableId)?.valorCapturado ?? '',
      justificacion: existentes.get(variable.variableId)?.justificacion ?? '',
      fuenteDato: existentes.get(variable.variableId)?.fuenteDato ?? 'CAPTURA'
    })));
  }

  private calcularAutomaticamenteDespuesDeGuardar(matriz: MatrizRiesgoDetalle, fueEdicion: boolean): void {
    const tipoCalculo = this.tipoCalculoParaSujeto(matriz.sujetoTipo);
    this.service.calcular(matriz.matrizId, tipoCalculo).subscribe({
      next: () => {
        this.mensaje.set(fueEdicion
          ? 'Matriz actualizada y recalculada automáticamente.'
          : 'Matriz creada y calculada automáticamente.');
        this.matrizSeleccionada.set(matriz);
        this.limpiarFormularioMatriz();
        this.cargarDashboard();
        this.cargarReporte();
        this.cargarMatrices();
        this.seleccionarMatriz(matriz.matrizId);
        this.tab.set('matrices');
        this.guardando.set(false);
      },
      error: err => {
        this.error.set(this.obtenerMensajeError(err, 'La matriz fue guardada, pero no se pudo calcular automáticamente.'));
        this.matrizSeleccionada.set(matriz);
        this.cargarMatrices();
        this.tab.set('matrices');
        this.guardando.set(false);
      }
    });
  }

  private aplicarCriteriosAutomaticos(): void {
    this.capturasVariables.set(this.capturasVariables().map(captura => {
      const sugerido = this.criterioSugerido(captura);
      if (!sugerido) return captura;

      return {
        ...captura,
        criterioId: sugerido.criterioId,
        puntaje: captura.puntaje ?? Number(sugerido.puntaje),
        justificacion: captura.justificacion.trim() ? captura.justificacion : sugerido.descripcion
      };
    }));
  }

  private refrescarDespuesAccion(matrizId: number, mensaje: string): void {
    this.mensaje.set(mensaje);
    this.cargarDashboard();
    this.cargarReporte();
    this.cargarMatrices();
    this.seleccionarMatriz(matrizId);
    this.guardando.set(false);
    this.cerrarModal();
  }

  private finalizarAccionConError(err: unknown, mensajeDefault: string): void {
    const mensaje = this.obtenerMensajeError(err, mensajeDefault);
    if (this.modalOperacion()) {
      this.modalError.set(mensaje);
    } else {
      this.error.set(mensaje);
    }
    this.guardando.set(false);
  }

  private validarCapturasContraCriterios(): string | null {
    // Si el usuario selecciona un criterio, el puntaje debe coincidir con ese
    // rango para evitar evaluaciones manuales inconsistentes.
    for (const captura of this.capturasVariables()) {
      if (captura.puntaje === null || captura.puntaje === undefined) continue;

      const advertencia = this.advertenciaCriterio(captura);
      if (advertencia) {
        return `${this.variableNombre(captura.variableId)}: ${advertencia}`;
      }
    }

    return null;
  }

  private actualizarCaptura(variableId: number, cambios: Partial<CapturaVariable>): void {
    this.capturasVariables.set(this.capturasVariables().map(captura =>
      captura.variableId === variableId ? { ...captura, ...cambios } : captura
    ));
  }

  private numeroSeguro(valor: string): number | null {
    const normalizado = `${valor ?? ''}`.trim().replace(',', '.');
    if (!normalizado) return null;
    const numero = Number(normalizado);
    return Number.isFinite(numero) ? numero : null;
  }

  existeMotivoCambioEstado(motivo: string): boolean {
    const motivoNormalizado = motivo.trim().toUpperCase();
    if (!motivoNormalizado) return false;

    return this.historial().some(item =>
      item.accion?.toUpperCase() === 'CAMBIO_ESTADO'
      && (item.motivo ?? '').trim().toUpperCase() === motivoNormalizado
    );
  }

  private deduplicarHistorial(datos: MatrizRiesgoHistorial[]): MatrizRiesgoHistorial[] {
    // Limpia repeticiones visuales causadas por reintentos rápidos sin eliminar
    // trazabilidad real en base de datos.
    const vistos = new Set<string>();
    return datos.filter(item => {
      const fecha = item.fecha ? new Date(item.fecha) : null;
      const fechaMinuto = fecha && !Number.isNaN(fecha.getTime())
        ? `${fecha.getFullYear()}-${fecha.getMonth()}-${fecha.getDate()} ${fecha.getHours()}:${fecha.getMinutes()}`
        : item.fecha;
      const key = [
        item.accion,
        item.tabla,
        item.registroId,
        item.estadoAnterior ?? '',
        item.estadoNuevo ?? '',
        (item.motivo ?? '').trim().toUpperCase(),
        fechaMinuto
      ].join('|');
      if (vistos.has(key)) return false;
      vistos.add(key);
      return true;
    });
  }

  private generarExcelReporte(): void {
    const reporte = this.reporte();
    if (!reporte) {
      this.error.set('No hay datos de reportería para exportar.');
      return;
    }
    // La exportación usa exactamente el reporte vigente para que coincida con
    // lo que el usuario filtró en pantalla.
    const matricesFiltradas = this.obtenerMatricesReporte(reporte);

    const wb = XLSX.utils.book_new();
    this.agregarHojaExcel(wb, 'Reporte', [
      ['INSTITUTO HONDUREÑO DE SEGURIDAD SOCIAL'],
      ['REPORTE DE MATRICES DE RIESGOS'],
      ['SGRLA-IHSS'],
      ['Fecha de generación', this.formatearFechaHora(reporte.fechaGeneracion)],
      [],
      ['1. FILTROS APLICADOS'],
      ['Búsqueda general', reporte.filtro?.buscar || 'Todos'],
      ['Estado', reporte.filtro?.estado || 'Todos'],
      ['Tipo de sujeto', reporte.filtro?.sujetoTipo || 'Todos'],
      ['Nivel residual', reporte.filtro?.nivelResidual || 'Todos'],
      ['Responsable', reporte.filtro?.responsable || 'Todos'],
      ['Fecha inicio', reporte.filtro?.fechaInicio || 'Todos'],
      ['Fecha fin', reporte.filtro?.fechaFin || 'Todos'],
      [],
      ['2. RESUMEN EJECUTIVO'],
      ['Indicador', 'Valor'],
      ['Total matrices', reporte.totales.totalMatrices],
      ['Calculadas', reporte.totales.totalCalculadas],
      ['Cerradas', reporte.totales.totalCerradas],
      ['Alto / Crítico', reporte.totales.totalAltoCritico],
      ['Plan requerido', reporte.totales.totalPlanAccionRequerido],
      ['Planes vencidos', reporte.totales.totalPlanesVencidos],
      [],
      ['3. DISTRIBUCIÓN POR ESTADO'],
      ['Estado', 'Total'],
      ...reporte.porEstado.map(x => [x.nombre, x.total]),
      [],
      ['4. DISTRIBUCIÓN POR NIVEL RESIDUAL'],
      ['Nivel', 'Total'],
      ...reporte.porNivelResidual.map(x => [x.nombre, x.total]),
      [],
      ['5. MATRICES FILTRADAS'],
      ['ID', 'Sujeto', 'Documento', 'Tipo', 'Estado', 'Inherente', 'Residual', 'Plan requerido', 'Fecha'],
      ...matricesFiltradas.map(x => [
        x.matrizId,
        x.nombreSujeto,
        x.documento || '',
        x.sujetoTipo,
        x.estado,
        this.formatearResultado(x.puntajeInherente, x.nivelInherente),
        this.formatearResultado(x.puntajeResidual, x.nivelResidual),
        x.requierePlanAccion ? 'Sí' : 'No',
        this.formatearFecha(x.fechaEvaluacion)
      ]),
      [],
      ['6. RESULTADOS POR FACTOR'],
      ['Factor', 'Matrices', 'Promedio inherente', 'Promedio residual', 'Alto / Crítico', 'Plan requerido'],
      ...reporte.porFactor.map(x => [
        `${x.factorCodigo} - ${x.factorNombre}`,
        x.totalMatrices,
        x.promedioInherente,
        x.promedioResidual,
        x.totalAltoCritico,
        x.totalPlanAccionRequerido
      ]),
      [],
      ['7. MATRICES ALTO / CRÍTICO'],
      ['ID', 'Sujeto', 'Documento', 'Tipo', 'Estado', 'Inherente', 'Residual', 'Plan requerido', 'Fecha'],
      ...reporte.matricesCriticas.map(x => [
        x.matrizId,
        x.nombreSujeto,
        x.documento || '',
        x.sujetoTipo,
        x.estado,
        this.formatearResultado(x.puntajeInherente, x.nivelInherente),
        this.formatearResultado(x.puntajeResidual, x.nivelResidual),
        x.requierePlanAccion ? 'Sí' : 'No',
        this.formatearFecha(x.fechaEvaluacion)
      ]),
      [],
      ['8. PLANES DE ACCIÓN'],
      ['Estado', 'Total', 'Vencidos'],
      ...reporte.planesAccion.map(x => [x.estado, x.total, x.vencidos])
    ]);

    this.agregarHojaExcel(wb, 'Distribuciones', [
      ['Distribución por estado'],
      ['Estado', 'Total'],
      ...reporte.porEstado.map(x => [x.nombre, x.total]),
      [],
      ['Distribución por nivel residual'],
      ['Nivel', 'Total'],
      ...reporte.porNivelResidual.map(x => [x.nombre, x.total]),
      [],
      ['Distribución por sujeto'],
      ['Tipo de sujeto', 'Total'],
      ...reporte.porSujetoTipo.map(x => [x.nombre, x.total]),
      [],
      ['Riesgo inherente por nivel'],
      ['Nivel', 'Total', 'Promedio'],
      ...reporte.mapaInherente.map(x => [x.nivel, x.total, x.promedio]),
      [],
      ['Riesgo residual por nivel'],
      ['Nivel', 'Total', 'Promedio'],
      ...reporte.mapaResidual.map(x => [x.nivel, x.total, x.promedio])
    ]);

    this.agregarHojaExcel(wb, 'Factores', [
      ['Factor', 'Matrices', 'Promedio inherente', 'Promedio residual', 'Alto / Crítico', 'Plan requerido'],
      ...reporte.porFactor.map(x => [
        `${x.factorCodigo} - ${x.factorNombre}`,
        x.totalMatrices,
        x.promedioInherente,
        x.promedioResidual,
        x.totalAltoCritico,
        x.totalPlanAccionRequerido
      ])
    ]);

    this.agregarHojaExcel(wb, 'Matrices Filtradas', [
      ['ID', 'Sujeto', 'Documento', 'Tipo', 'Estado', 'Inherente', 'Residual', 'Plan requerido', 'Fecha'],
      ...matricesFiltradas.map(x => [
        x.matrizId,
        x.nombreSujeto,
        x.documento || '',
        x.sujetoTipo,
        x.estado,
        this.formatearResultado(x.puntajeInherente, x.nivelInherente),
        this.formatearResultado(x.puntajeResidual, x.nivelResidual),
        x.requierePlanAccion ? 'Sí' : 'No',
        this.formatearFecha(x.fechaEvaluacion)
      ])
    ]);

    this.agregarHojaExcel(wb, 'Matrices Alto Critico', [
      ['ID', 'Sujeto', 'Documento', 'Tipo', 'Estado', 'Inherente', 'Residual', 'Plan requerido', 'Fecha'],
      ...reporte.matricesCriticas.map(x => [
        x.matrizId,
        x.nombreSujeto,
        x.documento || '',
        x.sujetoTipo,
        x.estado,
        `${x.puntajeInherente ?? '-'} ${x.nivelInherente ?? ''}`.trim(),
        `${x.puntajeResidual ?? '-'} ${x.nivelResidual ?? ''}`.trim(),
        x.requierePlanAccion ? 'Sí' : 'No',
        this.formatearFecha(x.fechaEvaluacion)
      ])
    ]);

    this.agregarHojaExcel(wb, 'Planes Accion', [
      ['Estado', 'Total', 'Vencidos'],
      ...reporte.planesAccion.map(x => [x.estado, x.total, x.vencidos])
    ]);

    XLSX.writeFile(wb, `Reporte_Matrices_Riesgos_${this.fechaArchivo()}.xlsx`);
  }

  private generarPdfReporte(): void {
    const reporte = this.reporte();
    if (!reporte) {
      this.error.set('No hay datos de reportería para generar el PDF.');
      return;
    }
    const matricesFiltradas = this.obtenerMatricesReporte(reporte);

    const doc = new jsPDF({ orientation: 'portrait', unit: 'mm', format: 'a4' });
    let y = this.agregarEncabezadoReportePdf(doc, 'REPORTE DE MATRICES DE RIESGOS');

    y = this.agregarTablaReportePdf(doc, y, '1. FILTROS APLICADOS', [
      ['Búsqueda general', reporte.filtro?.buscar || 'Todos', 'Estado', reporte.filtro?.estado || 'Todos'],
      ['Tipo de sujeto', reporte.filtro?.sujetoTipo || 'Todos', 'Nivel residual', reporte.filtro?.nivelResidual || 'Todos'],
      ['Responsable', reporte.filtro?.responsable || 'Todos', 'Rango fecha', `${reporte.filtro?.fechaInicio || 'Todos'} - ${reporte.filtro?.fechaFin || 'Todos'}`]
    ]);

    y = this.agregarTablaReportePdf(doc, y, '2. RESUMEN EJECUTIVO', [
      ['Total matrices', `${reporte.totales.totalMatrices}`, 'Calculadas', `${reporte.totales.totalCalculadas}`],
      ['Cerradas', `${reporte.totales.totalCerradas}`, 'Alto / Crítico', `${reporte.totales.totalAltoCritico}`],
      ['Plan requerido', `${reporte.totales.totalPlanAccionRequerido}`, 'Planes vencidos', `${reporte.totales.totalPlanesVencidos}`]
    ]);

    y = this.agregarAutoTablaReportePdf(doc, y, '3. MATRICES FILTRADAS', ['ID', 'Sujeto', 'Documento', 'Tipo', 'Estado', 'Residual', 'Plan', 'Fecha'],
      this.filasMatrizReporteCompleta(matricesFiltradas));

    y = this.agregarAutoTablaReportePdf(doc, y, '4. RESULTADOS POR FACTOR', ['Factor', 'Matrices', 'Inherente', 'Residual', 'Alto/Crítico', 'Plan'],
      reporte.porFactor.map(x => [
        `${x.factorCodigo} - ${x.factorNombre}`,
        `${x.totalMatrices}`,
        `${x.promedioInherente}`,
        `${x.promedioResidual}`,
        `${x.totalAltoCritico}`,
        `${x.totalPlanAccionRequerido}`
      ]));

    y = this.agregarAutoTablaReportePdf(doc, y, '5. MATRICES ALTO / CRÍTICO', ['ID', 'Sujeto', 'Estado', 'Residual', 'Plan', 'Fecha'],
      this.filasMatrizReporte(reporte.matricesCriticas));

    y = this.agregarAutoTablaReportePdf(doc, y, '6. MAPA INHERENTE PERSISTIDO', ['Nivel', 'Total', 'Promedio'],
      reporte.mapaInherente.map(x => [x.nivel, `${x.total}`, `${x.promedio}`]));

    y = this.agregarAutoTablaReportePdf(doc, y, '7. MAPA RESIDUAL PERSISTIDO', ['Nivel', 'Total', 'Promedio'],
      reporte.mapaResidual.map(x => [x.nivel, `${x.total}`, `${x.promedio}`]));

    this.agregarPiePaginaPdf(doc);
    doc.save(`Reporte_Matrices_Riesgos_${this.fechaArchivo()}.pdf`);
  }

  private agregarEncabezadoReportePdf(doc: jsPDF, titulo: string): number {
    const institucion = this.configService.configSistema()?.nombreInstitucion || 'Instituto Hondureño de Seguridad Social';
    const sistema = this.configService.configSistema()?.nombreSistema || 'SGRLA-IHSS';

    doc.setFillColor(15, 23, 42);
    doc.rect(0, 0, 210, 38, 'F');
    doc.setFont('helvetica', 'bold');
    doc.setFontSize(13);
    doc.setTextColor(255, 255, 255);
    doc.text(institucion.toUpperCase(), 14, 14);
    doc.setFontSize(17);
    doc.text(titulo, 14, 23);
    doc.setFont('helvetica', 'normal');
    doc.setFontSize(9);
    doc.setTextColor(203, 213, 225);
    doc.text(`${sistema} | Fecha de Generación: ${new Date().toLocaleString('es-HN')}`, 14, 30);
    return 48;
  }

  private agregarTablaReportePdf(doc: jsPDF, y: number, titulo: string, filas: string[][]): number {
    y = this.asegurarEspacioSeccionPdf(doc, y, filas.length);
    this.agregarTituloSeccionPdf(doc, y, titulo);
    autoTable(doc, {
      startY: y + 6,
      body: filas,
      theme: 'plain',
      styles: { fontSize: 8.5, cellPadding: 2, textColor: [51, 65, 85] },
      columnStyles: {
        0: { fontStyle: 'bold', cellWidth: 35, textColor: [30, 41, 59] },
        1: { cellWidth: 55 },
        2: { fontStyle: 'bold', cellWidth: 35, textColor: [30, 41, 59] },
        3: { cellWidth: 55 }
      },
      margin: { left: 14, right: 14 }
    });
    return (doc as any).lastAutoTable.finalY + 9;
  }

  private agregarAutoTablaReportePdf(doc: jsPDF, y: number, titulo: string, encabezados: string[], filas: string[][]): number {
    y = this.asegurarEspacioSeccionPdf(doc, y, filas.length);
    this.agregarTituloSeccionPdf(doc, y, titulo);
    autoTable(doc, {
      startY: y + 6,
      head: [encabezados],
      body: filas.length ? filas : [[`Sin registros para mostrar.`, ...Array(encabezados.length - 1).fill('')]],
      theme: 'grid',
      headStyles: { fillColor: [31, 63, 145], textColor: [255, 255, 255], fontSize: 8, fontStyle: 'bold' },
      bodyStyles: { fontSize: 7.8, textColor: [15, 23, 42] },
      alternateRowStyles: { fillColor: [248, 250, 252] },
      margin: { left: 14, right: 14 },
      rowPageBreak: 'avoid',
      styles: { overflow: 'linebreak', cellPadding: 2 }
    });
    return (doc as any).lastAutoTable.finalY + 9;
  }

  private asegurarEspacioSeccionPdf(doc: jsPDF, y: number, filas: number): number {
    const altoMinimo = filas <= 4 ? 18 + Math.max(filas, 1) * 9 : 36;
    if (y + altoMinimo > 278) {
      doc.addPage();
      return 18;
    }
    return y;
  }

  private agregarTituloSeccionPdf(doc: jsPDF, y: number, titulo: string): void {
    doc.setFont('helvetica', 'bold');
    doc.setFontSize(11);
    doc.setTextColor(30, 41, 59);
    doc.text(titulo, 14, y);
    doc.setDrawColor(226, 232, 240);
    doc.line(14, y + 2, 196, y + 2);
  }

  private agregarPiePaginaPdf(doc: jsPDF): void {
    const paginas = doc.getNumberOfPages();
    for (let i = 1; i <= paginas; i++) {
      doc.setPage(i);
      doc.setFont('helvetica', 'normal');
      doc.setFontSize(8);
      doc.setTextColor(100, 116, 139);
      doc.text(`Página ${i} de ${paginas}`, 176, 287);
    }
  }

  private filasMatrizReporte(matrices: MatrizRiesgoResumen[]): string[][] {
    return matrices.map(x => [
      `${x.matrizId}`,
      x.nombreSujeto || '',
      x.estado || '',
      this.formatearResultado(x.puntajeResidual, x.nivelResidual),
      x.requierePlanAccion ? 'Sí' : 'No',
      this.formatearFecha(x.fechaEvaluacion)
    ]);
  }

  private filasMatrizReporteCompleta(matrices: MatrizRiesgoResumen[]): string[][] {
    return matrices.map(x => [
      `${x.matrizId}`,
      x.nombreSujeto || '',
      x.documento || '-',
      x.sujetoTipo || '-',
      x.estado || '',
      this.formatearResultado(x.puntajeResidual, x.nivelResidual),
      x.requierePlanAccion ? 'Sí' : 'No',
      this.formatearFecha(x.fechaEvaluacion)
    ]);
  }

  obtenerMatricesReporte(reporte: MatricesRiesgoReporte | null = this.reporte()): MatrizRiesgoResumen[] {
    return reporte?.matricesFiltradas ?? [];
  }

  private agregarHojaExcel(wb: XLSX.WorkBook, nombre: string, data: unknown[][]): void {
    const ws = XLSX.utils.aoa_to_sheet(data);
    ws['!cols'] = this.calcularAnchosExcel(data);
    XLSX.utils.book_append_sheet(wb, ws, nombre);
  }

  private calcularAnchosExcel(data: unknown[][]): XLSX.ColInfo[] {
    const columnas = data.reduce((max, fila) => Math.max(max, fila.length), 0);
    return Array.from({ length: columnas }, (_, index) => {
      const ancho = data.reduce((max, fila) => Math.max(max, `${fila[index] ?? ''}`.length), 10);
      return { wch: Math.min(Math.max(ancho + 2, 12), 48) };
    });
  }

  private descargarBlob(blob: Blob, nombre: string): void {
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = nombre;
    a.click();
    URL.revokeObjectURL(url);
  }

  private async crearVistaPreviaDesdeBlob(blob: Blob, nombre: string, tipoMime: string): Promise<void> {
    this.cerrarVistaPreviaEvidencia();
    if (!blob.size) {
      this.evidenciaPreview.set({
        nombre,
        tipoMime: tipoMime || 'application/octet-stream',
        tamanoBytes: 0,
        url: null,
        urlSegura: null,
        tipoVista: 'generico',
        cargando: false,
        error: 'El archivo no contiene datos para mostrar en vista previa.'
      });
      return;
    }

    const mimeDetectado = await this.detectarMimeVistaPrevia(blob, nombre, tipoMime || blob.type);
    const mime = mimeDetectado || tipoMime || blob.type || 'application/octet-stream';
    const url = URL.createObjectURL(blob);
    const tipoVista = this.tipoVistaPorMime(mime, nombre);
    const preview: EvidenciaPreview = {
      nombre,
      tipoMime: mime,
      tamanoBytes: blob.size,
      url,
      urlSegura: this.sanitizer.bypassSecurityTrustResourceUrl(url),
      tipoVista,
      cargando: false
    };

    if (tipoVista === 'texto') {
      try {
        preview.texto = await blob.text();
      } catch {
        preview.error = 'No se pudo leer el contenido de texto del archivo.';
      }
    }

    this.evidenciaPreview.set(preview);
  }

  private tipoVistaPorMime(tipoMime: string, nombre: string): EvidenciaPreview['tipoVista'] {
    const mime = `${tipoMime || ''}`.toLowerCase();
    const extension = nombre.split('.').pop()?.toLowerCase() ?? '';
    if (mime.startsWith('image/') || ['png', 'jpg', 'jpeg', 'gif', 'bmp', 'webp'].includes(extension)) return 'imagen';
    if (mime === 'application/pdf' || extension === 'pdf') return 'pdf';
    if (mime.startsWith('text/') || ['txt', 'csv', 'json', 'xml', 'log'].includes(extension)) return 'texto';
    if ([
      'doc', 'docx', 'xls', 'xlsx', 'ppt', 'pptx', 'odt', 'ods', 'odp'
    ].includes(extension) || mime.includes('word') || mime.includes('excel') || mime.includes('spreadsheet') || mime.includes('presentation') || mime.includes('officedocument')) {
      return 'office';
    }
    return 'generico';
  }

  private async detectarMimeVistaPrevia(blob: Blob, nombre: string, tipoMime?: string): Promise<string> {
    const mime = `${tipoMime || blob.type || ''}`.toLowerCase();
    if (mime && mime !== 'application/octet-stream') return mime;

    const extension = nombre.split('.').pop()?.toLowerCase() ?? '';
    const porExtension: Record<string, string> = {
      png: 'image/png',
      jpg: 'image/jpeg',
      jpeg: 'image/jpeg',
      gif: 'image/gif',
      webp: 'image/webp',
      bmp: 'image/bmp',
      pdf: 'application/pdf',
      txt: 'text/plain',
      csv: 'text/csv',
      json: 'application/json',
      xml: 'text/xml',
      log: 'text/plain',
      doc: 'application/msword',
      docx: 'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
      xls: 'application/vnd.ms-excel',
      xlsx: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
      ppt: 'application/vnd.ms-powerpoint',
      pptx: 'application/vnd.openxmlformats-officedocument.presentationml.presentation'
    };
    if (porExtension[extension]) return porExtension[extension];

    const bytes = new Uint8Array(await blob.slice(0, 16).arrayBuffer());
    const firma = Array.from(bytes).map(b => b.toString(16).padStart(2, '0')).join(' ');
    if (firma.startsWith('89 50 4e 47')) return 'image/png';
    if (firma.startsWith('ff d8 ff')) return 'image/jpeg';
    if (firma.startsWith('47 49 46 38')) return 'image/gif';
    if (firma.startsWith('25 50 44 46')) return 'application/pdf';
    if (firma.startsWith('50 4b 03 04')) return 'application/zip';

    return mime || 'application/octet-stream';
  }

  private validarTamanoVistaPrevia(tamanoBytes: number): boolean {
    const maxBytes = 10 * 1024 * 1024;
    if (tamanoBytes <= maxBytes) return true;
    this.error.set('La vista previa solo está disponible para archivos de hasta 10 MB.');
    return false;
  }

  private fechaArchivo(): string {
    return new Date().toISOString().slice(0, 19).replace(/-/g, '').replace(/:/g, '').replace('T', '');
  }

  private formatearFecha(valor: string | Date | null | undefined): string {
    if (!valor) return '';
    const fecha = new Date(valor);
    return Number.isNaN(fecha.getTime()) ? `${valor}` : fecha.toLocaleDateString('es-HN');
  }

  private formatearFechaHora(valor: string | Date | null | undefined): string {
    if (!valor) return '';
    const fecha = new Date(valor);
    return Number.isNaN(fecha.getTime()) ? `${valor}` : fecha.toLocaleString('es-HN');
  }

  private formatearResultado(puntaje: number | null | undefined, nivel: string | null | undefined): string {
    if (puntaje === null || puntaje === undefined) return '-';
    return `${puntaje} ${nivel || ''}`.trim();
  }

  private limpiarFormularioMatriz(): void {
    this.matrizEditandoId.set(null);
    this.capturasVariables.set([]);
    this.matricesDuplicadas.set([]);
    this.nuevaMatriz = {
      sujetoTipo: 'PROVEEDOR',
      sujetoIdExt: '',
      documento: '',
      nombreSujeto: '',
      origenDatos: 'CAPTURA'
    };
    this.nuevoControl = {
      factorId: null,
      nombre: '',
      descripcion: '',
      periodicidad: '',
      oportunidad: '',
      automatizacion: '',
      procedimientos: '',
      calidad: '',
      efectividadPct: this.mitigacionesPermitidasOrdenadas()[0] ?? 0,
      responsable: '',
      evidenciaObligatoria: false
    };
    this.prepararCapturaVariables();
  }

  private factorCodigoPorTipoSujeto(tipo: string): string | null {
    const normalizado = `${tipo ?? ''}`.trim().toUpperCase();
    if (normalizado === 'PROVEEDOR') return 'PROVEEDORES';
    if (normalizado === 'CLIENTE_PATRONO') return 'CLIENTES_PATRONOS';
    if (normalizado === 'EMPLEADO') return 'EMPLEADOS';
    return null;
  }

  factorCapturaActual(): { factorId: number; factorCodigo: string; factorNombre: string; variables: VariableMetodologia[] } | null {
    const factorCodigo = this.factorCodigoPorTipoSujeto(this.nuevaMatriz.sujetoTipo);
    if (!factorCodigo) return null;
    return this.variablesPorFactor().find(g => g.factorCodigo === factorCodigo) ?? null;
  }

  factoresControlDisponibles(): { factorId: number; factorCodigo: string; factorNombre: string; variables: VariableMetodologia[] }[] {
    const factor = this.factorCapturaActual();
    return factor ? [factor] : this.variablesPorFactor();
  }

  private variablesParaTipoSujeto(tipo: string): VariableMetodologia[] {
    const variables = this.metodologia()?.variables ?? [];
    const factorCodigo = this.factorCodigoPorTipoSujeto(tipo);
    if (!factorCodigo) return variables;
    return variables.filter(variable => variable.factorCodigo === factorCodigo);
  }

  tipoCalculoParaSujeto(tipo: string): 'GLOBAL' | 'FACTOR' {
    return this.factorCodigoPorTipoSujeto(tipo) ? 'FACTOR' : 'GLOBAL';
  }

  etiquetaTipoSujeto(tipo: string): string {
    return this.tiposSujeto.find(x => x.valor === tipo)?.texto ?? tipo;
  }

  private ajustarFactorControlAlTipoSujeto(): void {
    const factorId = this.factorCapturaActual()?.factorId ?? null;
    this.nuevoControl = { ...this.nuevoControl, factorId };
  }

  private obtenerMensajeError(err: unknown, mensajeDefault: string): string {
    const error = err as { error?: { mensaje?: string; detalle?: string }; message?: string };
    return error?.error?.mensaje || error?.error?.detalle || error?.message || mensajeDefault;
  }
}
