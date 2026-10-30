import { ApplicationRef, ChangeDetectionStrategy, Component, ComponentRef, EnvironmentInjector, HostListener, OnInit, OnDestroy, computed, createComponent, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Observable, Subscription, forkJoin, map, of, switchMap, timer } from 'rxjs';
import { MatricesRiesgosService } from '../../data-access/matrices-riesgos.service';
import { CalculoConfiguracionService } from '../../data-access/calculo-configuracion.service';
import {
  CampoFormulario,
  DefinicionFormularioEditable,
  EvaluacionRiesgoDto,
  EvaluacionRiesgoResumenDto,
  EvaluacionesPaginadasDto,
  InstitutionalEvaluationProjectionDto,
  CalculoPreviewEvaluacionDto,
  FamiliaFormularioDto,
  FamiliaPredeterminadaDto,
  FiltroReporteMatrices,
  FlujoEvaluacionDto,
  EstadoEvaluacionCatalogoDto,
  TransicionesEvaluacionDto,
  MetodologiaFormulario,
  RespuestasFormulario,
  RiesgoReporteFila,
  ReporteMatricesPaginado,
  ReporteMatricesTotales,
  ValorRespuestaFormulario,
  VersionFormularioDto
} from '../../models/matrices-riesgos.models';
import { CrearFormulaUsoDto, FormulaVersionSelectorOption } from '../../models/calculo-configuracion.models';
import { normalizarJsonABuilderModel } from '../../models/form-builder.models';
import { ControlRiesgoDto, MatrizBloque6Dto, MitigacionBloque4Dto, PlanMitigacionDto, RiesgoDto } from '../../models/matrices-riesgos-fase11.models';
import { GlobalHttpStateService } from '../../../../../core/services/global-http-state.service';
import { InstitutionalConfirmationService } from '../../../../../shared/confirmation/institutional-confirmation.service';

import { FormBuilderComponent } from '../../components/form-builder/form-builder.component';
import { DynamicFieldRendererComponent } from '../../components/dynamic-field-renderer/dynamic-field-renderer.component';
import { FamiliaDetalleModalComponent } from '../../components/familia-detalle-modal/familia-detalle-modal.component';
import { FamiliaCrearModalComponent } from '../../components/familia-crear-modal/familia-crear-modal.component';
import { FamiliaEditarModalComponent } from '../../components/familia-editar-modal/familia-editar-modal.component';
import { AuthService } from '../../../../../core/auth/auth.service';
import {
  construirRespuestasFormularioPersistibles,
  normalizarDefinicionFormulario,
  normalizarMetodologiaFormulario,
  normalizarRespuestasFormulario,
  tieneValorRespuesta
} from '../../utils/dynamic-form-renderer.util';
import { sonJsonSemanticamenteEquivalentes } from '../../utils/form-builder-semantic-comparator.util';
import { normalizarMojibakeVisibleUtf8 } from '../../utils/text-encoding.util';
import { canonicalCatalogOptions, canonicalRiskLevelLabel, canonicalRiskResponseLabel } from '../../utils/matrices-riesgos-catalogos-canonicos';
import { MATRIX_BLOCK_1_FIELDS, MATRIX_BLOCK_2_FIELDS, MATRIX_BLOCK_3_FIELDS, MATRIX_BLOCK_4_FIELDS, MATRIX_BLOCK_5_FIELDS, MATRIX_BLOCK_6_FIELDS, MATRIX_BLOCK_TITLES, MATRIX_FIELDS, MatrixFieldContract } from '../../models/matriz-institucional.contract';

type TabMatrices = 'evaluaciones' | 'consolidado' | 'plantillas';
type AlertScope = 'page' | 'newEvaluation' | 'editEvaluation' | 'formBuilder';

import { ActionIconComponent } from '../../../../../shared/components/action-icon/action-icon.component';
import { BoundedSelectComponent, BoundedSelectOption } from '../../../../../shared/components/bounded-select/bounded-select.component';
import { DataPaginationComponent } from '../../../../../shared/components/data-pagination/data-pagination.component';
import { PageSizeSelectorComponent } from '../../../../../shared/components/page-size-selector/page-size-selector.component';
import { ReportPreviewService } from '../../../../../shared/report-preview/report-preview.service';
import { MatricesRiesgosMitigacionComponent } from '../../components/matrices-riesgos-mitigacion/matrices-riesgos-mitigacion.component';
import { MatricesRiesgosMonitoreoOperativoComponent } from '../../components/matrices-riesgos-monitoreo-operativo/matrices-riesgos-monitoreo-operativo.component';

type BloqueEvaluacion = 1 | 2 | 3 | 4 | 5 | 6;
type EstadoCargaWorkflow = 'LOADING' | 'SUCCESS_WITH_DATA' | 'SUCCESS_EMPTY' | 'ERROR';

@Component({
  selector: 'app-matrices-riesgos',
  standalone: true,
  imports: [ActionIconComponent, BoundedSelectComponent, DataPaginationComponent, PageSizeSelectorComponent, CommonModule, FormsModule, FormBuilderComponent, DynamicFieldRendererComponent, MatricesRiesgosMitigacionComponent, MatricesRiesgosMonitoreoOperativoComponent],
  templateUrl: './matrices-riesgos.component.html',
  styleUrl: './matrices-riesgos.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class MatricesRiesgosComponent implements OnInit, OnDestroy {
  private readonly service = inject(MatricesRiesgosService);
  private readonly globalState = inject(GlobalHttpStateService);
  private readonly authService = inject(AuthService);
  private readonly calculoConfig = inject(CalculoConfiguracionService, { optional: true });
  private readonly reportPreview = inject(ReportPreviewService);
  private readonly confirmation = inject(InstitutionalConfirmationService);
  private readonly applicationRef = inject(ApplicationRef);
  private readonly environmentInjector = inject(EnvironmentInjector);
  private detalleFamiliaRef: ComponentRef<FamiliaDetalleModalComponent> | null = null;
  private crearFamiliaRef: ComponentRef<FamiliaCrearModalComponent> | null = null;
  private editarFamiliaRef: ComponentRef<FamiliaEditarModalComponent> | null = null;
  private readonly suscripcionesDetalleFamilia: Subscription[] = [];
  private readonly suscripcionesCrearFamilia: Subscription[] = [];
  private readonly suscripcionesEditarFamilia: Subscription[] = [];
  private readonly autoDismissTimers: Partial<Record<AlertScope, ReturnType<typeof setTimeout>>> = {};
  private focoRetornoEditarFamilia: HTMLElement | null = null;
  private focoRetornoPredeterminada: HTMLElement | null = null;
  private focoRetornoMatrizCompleta: HTMLElement | null = null;
  private detalleEnContexto = false;
  private secuenciaVersionNuevaEvaluacion = 0;
  private calculoPreviewSubscription: Subscription | null = null;
  private secuenciaContextoPredeterminado = 0;
  private secuenciaCargaMatrizCompleta = 0;

  readonly opcionesRegistrosPorPagina = [10, 20, 50] as const;
  private suscripcionEvaluaciones: Subscription | null = null;
  private secuenciaCargaEvaluaciones = 0;
  private secuenciaCargaFamilias = 0;
  private secuenciaCargaConsolidado = 0;

  readonly esAdministrador = computed(() => this.authService.tieneRol(['ADMINISTRADOR']));

  readonly tab = signal<TabMatrices>('evaluaciones');
  readonly cargando = signal(false);
  readonly guardando = signal(false);
  readonly cambiosSinGuardar = signal(false);
  readonly operacionBuilderEnCurso = signal<'guardar' | 'publicar' | null>(null);
  readonly error = signal<string | null>(null);
  readonly mensaje = signal<string | null>(null);
  readonly errorNuevaEvaluacion = signal<string | null>(null);
  readonly mensajeNuevaEvaluacion = signal<string | null>(null);
  readonly errorEdicionEvaluacion = signal<string | null>(null);
  readonly mensajeEdicionEvaluacion = signal<string | null>(null);
  readonly errorFormBuilder = signal<string | null>(null);
  readonly mensajeFormBuilder = signal<string | null>(null);
  readonly errorModal = signal<string | null>(null);

  readonly cargandoEvaluaciones = signal(false);
  readonly errorEvaluaciones = signal<string | null>(null);

  readonly cargandoFormulario = signal(false);
  readonly errorFormulario = signal<string | null>(null);

  readonly familiaPredeterminada = signal<FamiliaPredeterminadaDto | null>(null);
  readonly cargandoFamiliaPredeterminada = signal(false);
  readonly errorFamiliaPredeterminada = signal<string | null>(null);
  readonly operacionPredeterminadaEnCurso = signal(false);
  readonly familiaObjetivoPredeterminada = signal<FamiliaFormularioDto | null>(null);
  readonly modalConfirmarPredeterminadaAbierto = signal(false);
  readonly familiaArchivadoPendiente = signal<FamiliaFormularioDto | null>(null);
  readonly motivoArchivado = signal('');
  readonly familiaRestauracionPendiente = signal<FamiliaFormularioDto | null>(null);
  readonly motivoRestauracion = signal('');

  readonly cargandoConsolidado = signal(false);
  readonly errorConsolidado = signal<string | null>(null);

  readonly cargandoPlantillas = signal(false);
  readonly errorPlantillas = signal<string | null>(null);
  readonly cargandoFamilias = signal(false);
  readonly errorFamilias = signal<string | null>(null);

  readonly modalGestorFamiliasAbierto = signal<boolean>(false);
  readonly detalleFamiliaDinamicoAbierto = signal(false);
  readonly filtroBuscarFamilia = signal('');
  readonly filtroEstadoFamilia = signal('TODAS');
  readonly filtroArchivadoFamilia = signal<'NO_ARCHIVADAS' | 'ARCHIVADAS' | 'TODAS'>('NO_ARCHIVADAS');
  readonly filtroVigenciaFamilia = signal('TODAS');
  readonly filtroBuscarConsolidado = signal('');
  readonly filtroEstadoConsolidado = signal('TODOS');
  readonly paginaConsolidado = signal(1);
  readonly registrosPorPaginaConsolidado = signal(10);
  readonly paginaFamilias = signal(1);
  readonly registrosPorPaginaFamilias = signal(10);
  readonly totalRegistrosFamilias = signal(0);
  readonly totalPaginasFamiliasServidor = signal(0);
  readonly totalesFamiliasServidor = signal({ totalFamilias: 0, activas: 0, inactivas: 0, archivadas: 0, totalVersiones: 0 });
  readonly familiaNuevaSeleccionada = computed(() =>
    this.familias().find(familia => familia.famCodigo === this.familiaSeleccionada())
      ?? (() => {
        const predeterminada = this.familiaPredeterminada();
        if (!predeterminada?.configurada || predeterminada.familiaCodigo !== this.familiaSeleccionada()) return null;
        return {
          famId: predeterminada.familiaId ?? 0,
          famCodigo: predeterminada.familiaCodigo ?? '',
          famNombre: predeterminada.familiaNombre ?? '',
          famDescripcion: null,
          famActivo: true,
          famPredeterminada: true,
          famFechaCreacion: new Date(0).toISOString(),
          totalVersiones: predeterminada.tieneVersionVigente ? 1 : 0,
          tieneVersionVigente: predeterminada.tieneVersionVigente
        } satisfies FamiliaFormularioDto;
      })()
  );
  readonly familiasElegibles = computed(() =>
    this.familias().filter(familia => familia.famActivo && familia.tieneVersionVigente)
  );
  readonly configuracionFamiliaPredeterminadaPendiente = computed(() =>
    !this.cargandoFamiliaPredeterminada()
      && !this.errorFamiliaPredeterminada()
      && this.familiaPredeterminada()?.configurada !== true
  );

  readonly totalFamilias = computed(() => this.totalesFamiliasServidor().totalFamilias);
  readonly totalFamiliasActivas = computed(() => this.totalesFamiliasServidor().activas);
  readonly totalFamiliasInactivas = computed(() => this.totalesFamiliasServidor().inactivas);
  readonly totalVersionesFamilias = computed(() => this.totalesFamiliasServidor().totalVersiones);

  readonly familiasFiltradas = computed<FamiliaFormularioDto[]>(() => {
    return this.familias();
  });

  readonly totalPaginasFamilias = computed(() => {
    return this.totalPaginasFamiliasServidor();
  });

  readonly familiasPaginadas = computed<FamiliaFormularioDto[]>(() => {
    return this.familias();
  });

  readonly metodologia = signal<MetodologiaFormulario | null>(null);
  readonly versionVigente = signal<VersionFormularioDto | null>(null);
  readonly versiones = signal<VersionFormularioDto[]>([]);
  readonly familias = signal<FamiliaFormularioDto[]>([]);
  readonly familiaSeleccionada = signal<string>('');
  readonly modalFamiliaAbierto = signal<boolean>(false);
  readonly modoEdicionFamilia = signal<boolean>(false);
  familiaIdEditando = 0;
  nuevaFamiliaCodigo = '';
  nuevaFamiliaNombre = '';
  nuevaFamiliaDescripcion = '';
  nuevaFamiliaActivo = true;


  readonly riesgos = signal<RiesgoDto[]>([]);
  readonly opcionesRiesgoNuevaEvaluacion = computed<BoundedSelectOption[]>(() => this.riesgos().map(riesgo => ({
    value: riesgo.rieId,
    label: `${riesgo.rieCodigo} — ${riesgo.rieNombre}`,
    searchText: `${riesgo.rieCodigo} ${riesgo.rieNombre} ${riesgo.rieDescripcion ?? ''}`
  })));
  readonly evaluaciones = signal<EvaluacionRiesgoResumenDto[]>([]);
  readonly totalRegistros = signal(0);
  readonly totalPaginas = signal(0);

  readonly evaluacionSeleccionada = signal<EvaluacionRiesgoDto | null>(null);
  readonly evaluacionResumenSeleccionada = signal<EvaluacionRiesgoResumenDto | null>(null);
  readonly proyeccionMatrizCompleta = signal<InstitutionalEvaluationProjectionDto | null>(null);
  readonly proyeccionMatrizCompletaEnCarga = signal(false);
  readonly metodologiaMatrizCompleta = signal<MetodologiaFormulario | null>(null);
  readonly metodologiaMatrizCompletaEnCarga = signal(false);
  readonly metodologiaMatrizCompletaError = signal<string | null>(null);
  readonly matrizCompletaEnCarga = signal(false);
  readonly matrizCompletaError = signal<string | null>(null);
  readonly modalMatrizCompletaAbierto = signal(false);
  readonly controlesMatrizCompleta = signal<ControlRiesgoDto[]>([]);
  readonly controlesMatrizEnCarga = signal(false);
  readonly controlesMatrizError = signal<string | null>(null);
  readonly bloque4Matriz = signal<MitigacionBloque4Dto | null>(null);
  readonly bloque4MatrizEnCarga = signal(false);
  readonly bloque4MatrizError = signal<string | null>(null);
  readonly bloque6Matriz = signal<MatrizBloque6Dto | null>(null);
  readonly bloque6MatrizError = signal<string | null>(null);
  readonly bloque5DetalleExpandido = signal(false);
  readonly riesgoMaestroMatriz = signal<RiesgoDto | null>(null);
  readonly flujos = signal<FlujoEvaluacionDto[]>([]);
  readonly consolidado = signal<RiesgoReporteFila[]>([]);
  readonly totalRegistrosConsolidado = signal(0);
  readonly totalPaginasServidorConsolidado = signal(0);
  readonly totalesConsolidado = signal<ReporteMatricesTotales>({
    totalRiesgos: 0,
    totalConEvaluacionOficial: 0,
    totalSinEvaluacionOficial: 0,
    totalAltoCritico: 0
  });
  readonly ordenConsolidado = signal('fechaEvaluacion');
  readonly direccionConsolidado = signal<'asc' | 'desc'>('desc');

  private static readonly ESTADOS_EVALUACION_CANONICOS: EstadoEvaluacionCatalogoDto[] = [
    { codigo: 'APROBADA', etiqueta: 'Aprobada' },
    { codigo: 'BORRADOR', etiqueta: 'Borrador' },
    { codigo: 'CERRADA', etiqueta: 'Cerrada' },
    { codigo: 'EN_REVISION', etiqueta: 'En revisión' },
    { codigo: 'OBSERVADA', etiqueta: 'Observada' },
    { codigo: 'RECHAZADA', etiqueta: 'Rechazada' }
  ];

  readonly catalogoEstadosEvaluacion = signal<EstadoEvaluacionCatalogoDto[]>(MatricesRiesgosComponent.ESTADOS_EVALUACION_CANONICOS);
  readonly errorCatalogoEstadosEvaluacion = signal<string | null>(null);
  readonly estadosConsolidado = computed(() => this.catalogoEstadosEvaluacion().map(estado => estado.codigo));

  readonly totalPaginasConsolidado = computed(() => {
    return this.totalPaginasServidorConsolidado();
  });

  readonly consolidadoPaginado = computed<RiesgoReporteFila[]>(() => {
    return this.consolidado();
  });

  readonly modalVerAbierto = signal<boolean>(false);
  readonly modalEditarAbierto = signal<boolean>(false);
  readonly modalSeguimientoAbierto = signal<boolean>(false);
  readonly modalNuevaEvaluacionAbierto = signal<boolean>(false);
  readonly bloqueEvaluacionActual = signal<BloqueEvaluacion>(1);
  readonly erroresValidacionBloque = signal<Record<string, string>>({});
  readonly proyeccionEvaluacion = signal<InstitutionalEvaluationProjectionDto | null>(null);
  readonly cargandoProyeccionEvaluacion = signal(false);
  readonly detalleCalculosExpandido = signal(false);
  readonly calculoPreview = signal<CalculoPreviewEvaluacionDto | null>(null);
  readonly actualizandoCalculo = signal(false);
  readonly errorCalculoPreview = signal<string | null>(null);
  readonly metodologiaHistorica = signal<MetodologiaFormulario | null>(null);
  readonly versionHistorica = signal<VersionFormularioDto | null>(null);

  readonly pagina = signal(1);
  readonly registrosPorPagina = signal(10);
  readonly filtroBuscar = signal('');
  readonly filtroEstado = signal('');
  readonly riesgoId = signal(0);
  readonly respuestas = signal<RespuestasFormulario>({});

  readonly bloquesEvaluacion: readonly BloqueEvaluacion[] = [1, 2, 3, 4, 5, 6];

  readonly tituloBloqueEvaluacion = (bloque: BloqueEvaluacion): string => MATRIX_BLOCK_TITLES[bloque];

  valorProyeccion(ordinal: number): unknown {
    return this.proyeccionEvaluacion()?.fields.find(field => field.fieldNumber === ordinal)?.value ?? null;
  }

  etiquetaEstadoProyeccion(ordinal: number): string {
    return this.proyeccionEvaluacion()?.fields.find(field => field.fieldNumber === ordinal)?.state ?? 'LEGIT_BLANK';
  }

  motivoTransicion = '';
  nuevoEstado = '';
  readonly errorSeguimiento = signal<string | null>(null);
  readonly transicionesPermitidas = signal<string[]>([]);
  readonly estadoTransiciones = signal<EstadoCargaWorkflow>('LOADING');
  readonly errorTransiciones = signal<string | null>(null);
  readonly estadoFlujos = signal<EstadoCargaWorkflow>('LOADING');
  readonly errorFlujos = signal<string | null>(null);
  archivoEvidencia: File | null = null;

  readonly versionEditando = signal<VersionFormularioDto | null>(null);
  readonly soloLecturaDefinicion = signal<boolean>(false);
  readonly formulasCentralDisponibles = signal<FormulaVersionSelectorOption[]>([]);
  definicionTecnica = '';

  readonly seccionesModal = computed(() => {
    const metHist = this.metodologiaHistorica();
    if (metHist?.secciones?.length) {
      return normalizarMetodologiaFormulario(metHist).secciones;
    }

    const vHist = this.versionHistorica();
    if (vHist?.verJson) {
      const def = this.extraerDefinicionVersion(vHist);
      if (def.secciones.length > 0) return def.secciones;
    }

    const vVig = this.versionVigente();
    if (vVig?.verJson) {
      const def = this.extraerDefinicionVersion(vVig);
      if (def.secciones.length > 0) return def.secciones;
    }

    return [];
  });

  readonly secciones = computed(() => {
    const versionVigente = this.versionVigente();
    if (versionVigente?.verJson) {
      const def = this.extraerDefinicionVersion(versionVigente);
      if (def.secciones.length > 0) return def.secciones;
    }

    return [];
  });

  readonly totalCampos = computed(() =>
    this.secciones().reduce((total, seccion) => total + seccion.campos.length, 0)
  );

  contarEvaluacionesPorEstado(estado: string): number {
    const list = this.evaluaciones();
    if (!Array.isArray(list)) return 0;
    return list.filter(e => (e?.estado || '').toUpperCase() === estado.toUpperCase()).length;
  }

  readonly totalCompletados = computed(() => {
    const respuestas = this.respuestas();
    return this.secciones()
      .flatMap(seccion => seccion.campos)
      .filter(campo => this.tieneValor(respuestas[campo.clave]))
      .length;
  });

  readonly puedeGuardar = computed(() => {
    const respuestas = this.respuestas();
    const versionActiva = this.modalEditarAbierto()
      ? (this.versionHistorica() || this.metodologiaHistorica() || this.versionVigente())
      : this.versionVigente();

    if (this.riesgoId() <= 0 || !versionActiva) {
      return false;
    }

    const seccionesEval = this.modalEditarAbierto() || this.modalNuevaEvaluacionAbierto()
      ? this.seccionesModal()
      : this.secciones();

    return seccionesEval
      .flatMap(seccion => seccion.campos)
      .filter(campo => campo.obligatorio)
      .every(campo => this.tieneValor(respuestas[campo.clave]));
  });

  @HostListener('document:keydown.escape', ['$event'])
  manejarTeclaEscape(event: Event): void {
    if (this.modalConfirmarPredeterminadaAbierto()) {
      event.preventDefault();
      this.cancelarConfirmacionFamiliaPredeterminada();
    } else if (this.detalleFamiliaDinamicoAbierto()) {
      event.preventDefault();
      this.cerrarModalVerFamilia();
    } else if (this.modalMatrizCompletaAbierto()) {
      event.preventDefault();
      this.cerrarMatrizCompleta();
    } else if (this.modalVerAbierto()) {
      event.preventDefault();
      this.cerrarModalVer();
    } else if (this.modalEditarAbierto()) {
      event.preventDefault();
      return;
    } else if (this.modalSeguimientoAbierto()) {
      event.preventDefault();
      this.cerrarModalSeguimiento();
    } else if (this.modalNuevaEvaluacionAbierto()) {
      event.preventDefault();
      this.cerrarModalNuevaEvaluacion();
    } else if (this.versionEditando()) {
      event.preventDefault();
      this.versionEditando.set(null);
    } else if (this.modalFamiliaAbierto()) {
      event.preventDefault();
      this.cerrarModalFamilia();
    }
  }

  ngOnInit(): void {
    this.cargarCatalogoEstadosEvaluacion();
    this.cargarFamilias();
    this.cargarRiesgos();
    this.cargarEvaluaciones();
    this.cargarFormularioVigente();
  }

  cargarCatalogoEstadosEvaluacion(): void {
    this.errorCatalogoEstadosEvaluacion.set(null);
    const cargarCatalogo = this.service.obtenerCatalogoEstadosEvaluacion;
    if (typeof cargarCatalogo !== 'function') return;
    cargarCatalogo.call(this.service).subscribe({
      next: estados => {
        if (!Array.isArray(estados) || estados.length === 0) {
          this.catalogoEstadosEvaluacion.set(MatricesRiesgosComponent.ESTADOS_EVALUACION_CANONICOS);
          return;
        }
        const codigos = estados.map(estado => estado.codigo?.trim().toUpperCase());
        if (codigos.some(codigo => !codigo) || new Set(codigos).size !== codigos.length) {
          this.catalogoEstadosEvaluacion.set(MatricesRiesgosComponent.ESTADOS_EVALUACION_CANONICOS);
          this.errorCatalogoEstadosEvaluacion.set('El catálogo de estados de evaluación no es válido.');
          return;
        }
        this.catalogoEstadosEvaluacion.set(estados.map(estado => ({
          codigo: estado.codigo.trim().toUpperCase(),
          etiqueta: estado.etiqueta.trim()
        })));
      },
      error: () => {
        this.catalogoEstadosEvaluacion.set(MatricesRiesgosComponent.ESTADOS_EVALUACION_CANONICOS);
        this.errorCatalogoEstadosEvaluacion.set('No se pudo cargar el catálogo de estados de evaluación.');
      }
    });
  }

  ngOnDestroy(): void {
    this.secuenciaCargaEvaluaciones++;
    this.calculoPreviewSubscription?.unsubscribe();
    this.calculoPreviewSubscription = null;
    this.suscripcionEvaluaciones?.unsubscribe();
    this.suscripcionEvaluaciones = null;
    this.cerrarModalCrearFamilia();
    this.cerrarModalEditarFamilia();
    this.cerrarModalVerFamilia();
    this.secuenciaCargaMatrizCompleta++;
    this.modalMatrizCompletaAbierto.set(false);
    this.limpiarAutoDismiss();
    this.cancelarDebounceBuscarPendiente();
  }

  seleccionarTab(tab: TabMatrices): void {
    this.tab.set(tab);
    this.limpiarAlertas();
    this.globalState.limpiarError();

    if (tab === 'consolidado') this.cargarConsolidado();
    if (tab === 'plantillas') {
      this.paginaFamilias.set(1);
      this.cargarFamilias();
    }
  }

  onKeydownTab(event: KeyboardEvent, tabActual: TabMatrices): void {
    const tabs: TabMatrices[] = ['evaluaciones', 'consolidado', 'plantillas'];
    const indexActual = tabs.indexOf(tabActual);

    if (indexActual === -1) return;

    let nuevoIndex = -1;

    switch (event.key) {
      case 'ArrowRight':
      case 'ArrowDown':
        nuevoIndex = (indexActual + 1) % tabs.length;
        break;
      case 'ArrowLeft':
      case 'ArrowUp':
        nuevoIndex = (indexActual - 1 + tabs.length) % tabs.length;
        break;
      case 'Home':
        nuevoIndex = 0;
        break;
      case 'End':
        nuevoIndex = tabs.length - 1;
        break;
      default:
        return;
    }

    event.preventDefault();
    const nuevaTab = tabs[nuevoIndex];
    this.seleccionarTab(nuevaTab);

    setTimeout(() => {
      const el = document.getElementById('tab-' + nuevaTab);
      el?.focus();
    }, 0);
  }

  cargarFamilias(): void {
    const solicitudId = ++this.secuenciaCargaFamilias;
    this.cargandoFamilias.set(true);
    this.errorFamilias.set(null);
    this.service.listarFamiliasFormularioPaginadas({
      pagina: this.paginaFamilias(),
      tamanoPagina: this.registrosPorPaginaFamilias(),
      buscar: this.filtroBuscarFamilia(),
      estado: this.filtroEstadoFamilia() as 'TODAS' | 'ACTIVAS' | 'INACTIVAS',
      archivado: this.filtroArchivadoFamilia(),
      vigencia: this.filtroVigenciaFamilia() as 'TODAS' | 'VIGENTES' | 'SIN_VIGENTE'
    }).subscribe({
      next: resultado => {
        if (solicitudId !== this.secuenciaCargaFamilias) return;
        const items = Array.isArray(resultado?.items) ? resultado.items : [];
        this.familias.set(items.map(familia => ({
          ...familia,
          famNombre: normalizarMojibakeVisibleUtf8(familia.famNombre),
          famDescripcion: familia.famDescripcion ? normalizarMojibakeVisibleUtf8(familia.famDescripcion) : familia.famDescripcion
        })));
        this.paginaFamilias.set(resultado?.pagina ?? 1);
        this.totalRegistrosFamilias.set(resultado?.totalRegistros ?? 0);
        this.totalPaginasFamiliasServidor.set(resultado?.totalPaginas ?? 0);
        this.totalesFamiliasServidor.set({
          totalFamilias: resultado?.totales?.totalFamilias ?? 0,
          activas: resultado?.totales?.activas ?? 0,
          inactivas: resultado?.totales?.inactivas ?? 0,
          archivadas: resultado?.totales?.archivadas ?? 0,
          totalVersiones: resultado?.totales?.totalVersiones ?? 0
        });
        if (this.familiaSeleccionada() && !items.some(f => f.famCodigo === this.familiaSeleccionada())) {
          this.familiaSeleccionada.set('');
        }
        this.cargandoFamilias.set(false);
      },
      error: error => {
        if (solicitudId !== this.secuenciaCargaFamilias) return;
        this.familias.set([]);
        this.totalRegistrosFamilias.set(0);
        this.totalPaginasFamiliasServidor.set(0);
        this.errorFamilias.set(this.obtenerMensajeError(error, 'No se pudieron cargar las familias de formularios.'));
        this.cargandoFamilias.set(false);
      }
    });
  }

  seleccionarFamilia(codigo: string): void {
    if (!codigo) {
      ++this.secuenciaVersionNuevaEvaluacion;
      this.familiaSeleccionada.set('');
      this.versionVigente.set(null);
      this.metodologia.set(null);
      this.errorFormulario.set(null);
      this.cargandoFormulario.set(false);
      this.respuestas.set({});
      this.riesgoId.set(0);
      this.cerrarModalVerFamilia();
      return;
    }

    const familia = this.familias().find(item => item.famCodigo === codigo);
    if (this.modalNuevaEvaluacionAbierto() && familia && !this.familiaEsElegible(familia)) {
      this.familiaSeleccionada.set('');
      this.versionVigente.set(null);
      this.metodologia.set(null);
      this.errorFormulario.set(this.motivoFamiliaNoElegible(familia));
      this.cargandoFormulario.set(false);
      return;
    }

    const secuencia = ++this.secuenciaVersionNuevaEvaluacion;
    this.familiaSeleccionada.set(codigo);
    this.versionEditando.set(null);
    this.soloLecturaDefinicion.set(false);
    this.definicionTecnica = '';
    this.versionVigente.set(null);
    this.metodologia.set(null);
    this.errorFormulario.set(null);
    this.respuestas.set({});
    this.cargandoFormulario.set(true);
    this.cargarVersiones();
    this.service.obtenerVersionVigenteFormulario(codigo).subscribe({
      next: version => {
        if (secuencia !== this.secuenciaVersionNuevaEvaluacion || this.familiaSeleccionada() !== codigo) return;
        this.versionVigente.set(version);
        this.inicializarRespuestas();
        const cargarMetodologiaPorVersion = (this.service as MatricesRiesgosService & {
          metodologiaPorVersion?: (versionId: number) => Observable<MetodologiaFormulario>;
        }).metodologiaPorVersion;
        if (typeof cargarMetodologiaPorVersion !== 'function' || !version.verId) {
          this.cargandoFormulario.set(false);
          return;
        }
        cargarMetodologiaPorVersion.call(this.service, version.verId).subscribe({
          next: metodologia => {
            if (secuencia !== this.secuenciaVersionNuevaEvaluacion || this.familiaSeleccionada() !== codigo) return;
            this.metodologia.set(metodologia);
            this.inicializarRespuestas();
            this.cargandoFormulario.set(false);
          },
          error: error => {
            if (secuencia !== this.secuenciaVersionNuevaEvaluacion || this.familiaSeleccionada() !== codigo) return;
            this.cargandoFormulario.set(false);
            this.errorFormulario.set(this.obtenerMensajeError(error, 'No se pudo cargar la metodología de la versión seleccionada.'));
          }
        });
      },
      error: () => {
        if (secuencia !== this.secuenciaVersionNuevaEvaluacion || this.familiaSeleccionada() !== codigo) return;
        this.versionVigente.set(null);
        this.cargandoFormulario.set(false);
        this.errorFormulario.set('La familia seleccionada no dispone de una versión activa.');
        this.respuestas.set({});
      }
    });
  }

  abrirModalGestorFamilias(): void {
    this.cargarFamilias();
    this.modalGestorFamiliasAbierto.set(true);
  }

  cerrarModalGestorFamilias(): void {
    this.modalGestorFamiliasAbierto.set(false);
  }

  limpiarFiltrosFamilias(): void {
    this.filtroBuscarFamilia.set('');
    this.filtroEstadoFamilia.set('TODAS');
    this.filtroArchivadoFamilia.set('NO_ARCHIVADAS');
    this.filtroVigenciaFamilia.set('TODAS');
    this.paginaFamilias.set(1);
    this.cargarFamilias();
  }

  cambiarBuscarFamilia(valor: string): void {
    this.filtroBuscarFamilia.set(valor);
    this.paginaFamilias.set(1);
    this.cargarFamilias();
  }

  cambiarEstadoFamilia(valor: string): void {
    this.filtroEstadoFamilia.set(valor);
    this.paginaFamilias.set(1);
    this.cargarFamilias();
  }

  cambiarArchivadoFamilia(valor: string): void {
    if (valor !== 'NO_ARCHIVADAS' && valor !== 'ARCHIVADAS' && valor !== 'TODAS') return;
    this.filtroArchivadoFamilia.set(valor);
    this.paginaFamilias.set(1);
    this.cargarFamilias();
  }

  cambiarVigenciaFamilia(valor: string): void {
    this.filtroVigenciaFamilia.set(valor);
    this.paginaFamilias.set(1);
    this.cargarFamilias();
  }

  cambiarPaginaConsolidado(nuevaPagina: number): void {
    const total = this.totalPaginasConsolidado();
    if (!Number.isInteger(nuevaPagina) || total <= 0 || nuevaPagina < 1 || nuevaPagina > total) return;
    this.paginaConsolidado.set(nuevaPagina);
    this.cargarConsolidado();
  }

  cambiarRegistrosPorPaginaConsolidado(cantidad: number): void {
    const num = Number(cantidad);
    if (!Number.isInteger(num) || !this.opcionesRegistrosPorPagina.includes(num as 10 | 20 | 50)) return;
    this.registrosPorPaginaConsolidado.set(num);
    this.paginaConsolidado.set(1);
    this.cargarConsolidado();
  }

  ordenarConsolidado(campo: string): void {
    if (this.ordenConsolidado() === campo) {
      this.direccionConsolidado.update(actual => actual === 'asc' ? 'desc' : 'asc');
    } else {
      this.ordenConsolidado.set(campo);
      this.direccionConsolidado.set('asc');
    }
    this.paginaConsolidado.set(1);
    this.cargarConsolidado();
  }

  cambiarBuscarConsolidado(valor: string): void {
    this.filtroBuscarConsolidado.set(valor);
    this.paginaConsolidado.set(1);
    this.cargarConsolidado();
  }

  cambiarEstadoConsolidado(valor: string): void {
    this.filtroEstadoConsolidado.set(valor);
    this.paginaConsolidado.set(1);
    this.cargarConsolidado();
  }

  limpiarFiltrosConsolidado(): void {
    this.filtroBuscarConsolidado.set('');
    this.filtroEstadoConsolidado.set('TODOS');
    this.paginaConsolidado.set(1);
    this.cargarConsolidado();
  }

  limiteSuperiorConsolidado(): number {
    return Math.min(this.paginaConsolidado() * this.registrosPorPaginaConsolidado(), this.totalRegistrosConsolidado());
  }

  cambiarPaginaFamilias(nuevaPagina: number): void {
    const total = this.totalPaginasFamilias();
    if (!Number.isInteger(nuevaPagina) || total <= 0 || nuevaPagina < 1 || nuevaPagina > total) {
      return;
    }
    this.paginaFamilias.set(nuevaPagina);
    this.cargarFamilias();
  }

  cambiarRegistrosPorPaginaFamilias(cantidad: number): void {
    const num = Number(cantidad);
    if (!Number.isInteger(num) || !this.opcionesRegistrosPorPagina.includes(num as 10 | 20 | 50)) {
      return;
    }
    this.registrosPorPaginaFamilias.set(num);
    this.paginaFamilias.set(1);
    this.cargarFamilias();
  }

  abrirModalVerFamilia(fam: FamiliaFormularioDto, desdeNuevaEvaluacion = false): void {
    if (!fam || fam.famId <= 0) return;

    this.cerrarModalVerFamilia();
    this.cerrarModalGestorFamilias();
    const componentRef = createComponent(FamiliaDetalleModalComponent, {
      environmentInjector: this.environmentInjector
    });
    componentRef.setInput('familiaId', fam.famId);
    componentRef.setInput('familiaReferencia', fam);
    componentRef.setInput('origen', desdeNuevaEvaluacion ? 'nueva-evaluacion' : 'gestor-familias');

    this.suscripcionesDetalleFamilia.push(
      componentRef.instance.cerrar.subscribe(() => this.cerrarModalVerFamilia()),
      componentRef.instance.editarFamilia.subscribe(familia => {
        this.focoRetornoEditarFamilia = document.activeElement instanceof HTMLElement ? document.activeElement : null;
        this.abrirModalEditarFamilia(familia);
      }),
      componentRef.instance.nuevaVersion.subscribe(familia => {
        this.crearNuevaVersionDesdeDetalle(familia);
      }),
      componentRef.instance.verDefinicion.subscribe(({ familia, version, modoEdicion }) => {
        this.familiaSeleccionada.set(familia.famCodigo);
        this.abrirDefinicion(version, !modoEdicion);
      }),
      componentRef.instance.publicarVersionSolicitada.subscribe(version => this.publicarVersion(version)),
      componentRef.instance.cambiarVigenciaSolicitada.subscribe(({ version, vigente }) => this.cambiarVigenciaVersion(version, vigente)),
      componentRef.instance.eliminarVersionSolicitada.subscribe(version => this.eliminarVersionFormulario(version))
    );

    this.detalleFamiliaRef = componentRef;
    this.applicationRef.attachView(componentRef.hostView);
    document.body.appendChild(componentRef.location.nativeElement);
    this.detalleFamiliaDinamicoAbierto.set(true);
    componentRef.changeDetectorRef.detectChanges();
  }

  cerrarModalVerFamilia(): void {
    while (this.suscripcionesDetalleFamilia.length > 0) {
      this.suscripcionesDetalleFamilia.pop()?.unsubscribe();
    }

    const componentRef = this.detalleFamiliaRef;
    if (componentRef) {
      this.applicationRef.detachView(componentRef.hostView);
      componentRef.destroy();
      this.detalleFamiliaRef = null;
    }

    this.detalleFamiliaDinamicoAbierto.set(false);
  }

  abrirModalCrearFamilia(): void {
    this.cerrarModalCrearFamilia();
    this.cerrarModalGestorFamilias();
    this.errorModal.set(null);
    this.globalState.limpiarError();

    const componentRef = createComponent(FamiliaCrearModalComponent, {
      environmentInjector: this.environmentInjector
    });

    this.suscripcionesCrearFamilia.push(
      componentRef.instance.cerrar.subscribe(() => this.cerrarModalCrearFamilia()),
      componentRef.instance.creada.subscribe(({ nombre }) => {
        this.cerrarModalCrearFamilia();
        this.globalState.limpiarError();
        this.mostrarMensaje(`Familia «${nombre}» creada correctamente.`);
        this.cargarFamilias();
      })
    );

    this.crearFamiliaRef = componentRef;
    this.applicationRef.attachView(componentRef.hostView);
    document.body.appendChild(componentRef.location.nativeElement);
    componentRef.changeDetectorRef.detectChanges();
    this.ocultarDetalleComoContexto();
  }

  cerrarModalCrearFamilia(): void {
    while (this.suscripcionesCrearFamilia.length > 0) {
      this.suscripcionesCrearFamilia.pop()?.unsubscribe();
    }

    const componentRef = this.crearFamiliaRef;
    if (componentRef) {
      this.applicationRef.detachView(componentRef.hostView);
      componentRef.destroy();
      this.crearFamiliaRef = null;
    }
  }

  abrirModalEditarFamilia(fam: FamiliaFormularioDto): void {
    if (!fam || fam.famId <= 0 || !this.esAdministrador()) return;

    this.cerrarModalEditarFamilia();
    this.cerrarModalFamilia();
    this.cerrarModalGestorFamilias();
    this.errorModal.set(null);
    this.globalState.limpiarError();

    const componentRef = createComponent(FamiliaEditarModalComponent, {
      environmentInjector: this.environmentInjector
    });
    componentRef.setInput('familiaId', fam.famId);
    componentRef.setInput('familiaReferencia', fam);

    this.suscripcionesEditarFamilia.push(
      componentRef.instance.cerrar.subscribe(() => this.cerrarModalEditarFamilia()),
      componentRef.instance.guardada.subscribe(familia => {
        this.cerrarModalEditarFamilia();
        this.detalleFamiliaRef?.instance.refrescar();
        this.globalState.limpiarError();
        this.mostrarMensaje(`Familia «${familia.famNombre}» actualizada correctamente.`);
        this.cargarFamilias();
      }),
      componentRef.instance.estadoCambiado.subscribe(() => {
        this.detalleFamiliaRef?.instance.refrescar();
        this.globalState.limpiarError();
        this.cargarFamilias();
      }),
      componentRef.instance.eliminada.subscribe(familia => {
        this.cerrarModalEditarFamilia();
        this.globalState.limpiarError();
        this.mostrarMensaje(`Familia «${familia.famCodigo}» eliminada correctamente.`);
        this.cargarFamilias();
      })
    );

    this.editarFamiliaRef = componentRef;
    this.applicationRef.attachView(componentRef.hostView);
    document.body.appendChild(componentRef.location.nativeElement);
    componentRef.changeDetectorRef.detectChanges();
  }

  cerrarModalEditarFamilia(): void {
    while (this.suscripcionesEditarFamilia.length > 0) {
      this.suscripcionesEditarFamilia.pop()?.unsubscribe();
    }

    const componentRef = this.editarFamiliaRef;
    if (componentRef) {
      this.applicationRef.detachView(componentRef.hostView);
      componentRef.destroy();
      this.editarFamiliaRef = null;
    }

    this.mostrarDetalleComoContexto();
    const foco = this.focoRetornoEditarFamilia;
    this.focoRetornoEditarFamilia = null;
    if (foco?.isConnected) setTimeout(() => foco.focus(), 0);
  }

  private ocultarDetalleComoContexto(): void {
    const host = this.detalleFamiliaRef?.location.nativeElement as HTMLElement | undefined;
    if (!host) return;
    host.style.visibility = 'hidden';
    host.style.pointerEvents = 'none';
    host.setAttribute('aria-hidden', 'true');
    this.detalleEnContexto = true;
  }

  private mostrarDetalleComoContexto(): void {
    if (!this.detalleEnContexto) return;
    const host = this.detalleFamiliaRef?.location.nativeElement as HTMLElement | undefined;
    if (host) {
      host.style.visibility = '';
      host.style.pointerEvents = '';
      host.removeAttribute('aria-hidden');
    }
    this.detalleEnContexto = false;
  }

  cerrarModalFamilia(): void {
    this.modalFamiliaAbierto.set(false);
    this.errorModal.set(null);
    this.globalState.limpiarError();
  }

  guardarFamilia(): void {
    this.errorModal.set(null);
    this.globalState.limpiarError();

    if (this.modoEdicionFamilia()) {
      this.guardando.set(true);
      this.service.actualizarFamiliaFormulario(this.familiaIdEditando, {
        famNombre: this.nuevaFamiliaNombre,
        famDescripcion: this.nuevaFamiliaDescripcion,
        famActivo: this.nuevaFamiliaActivo
      }).subscribe({
        next: () => {
          this.guardando.set(false);
          this.modalFamiliaAbierto.set(false);
          this.errorModal.set(null);
          this.globalState.limpiarError();
          this.mostrarMensaje('Familia actualizada y verificada correctamente.');
          this.cargarFamilias();
        },
        error: error => {
          this.guardando.set(false);
          this.globalState.limpiarError();
          this.errorModal.set(this.obtenerMensajeError(error, 'No se pudo actualizar la familia. Verifique los datos e intente nuevamente.'));
        }
      });
    } else {
      this.guardando.set(true);
      this.service.crearFamiliaFormulario({
        famCodigo: this.nuevaFamiliaCodigo,
        famNombre: this.nuevaFamiliaNombre,
        famDescripcion: this.nuevaFamiliaDescripcion
      }).subscribe({
        next: () => {
          this.guardando.set(false);
          this.modalFamiliaAbierto.set(false);
          this.errorModal.set(null);
          this.globalState.limpiarError();
          this.mostrarMensaje(`Familia «${this.nuevaFamiliaNombre}» creada correctamente.`);
          this.cargarFamilias();
        },
        error: error => {
          this.guardando.set(false);
          this.globalState.limpiarError();
          this.errorModal.set(this.obtenerMensajeError(error, 'No se pudo crear la familia. Verifique el código y los datos ingresados.'));
        }
      });
    }
  }

  confirmarDesactivarFamilia(fam: FamiliaFormularioDto): void {
    void this.confirmation.confirm({
      title: `¿Desactivar la familia ${fam.famNombre}?`,
      message: 'La familia dejará de estar disponible para nuevas operaciones. No se eliminarán sus versiones ni información histórica.',
      icon: 'warning',
      confirmLabel: 'Desactivar',
      cancelLabel: 'Cancelar',
      confirmIcon: 'deactivate',
      confirmVariant: 'danger'
    }).then(confirmed => {
      if (confirmed) this.desactivarFamilia(fam);
    });
  }

  desactivarFamilia(fam: FamiliaFormularioDto): void {
    this.guardando.set(true);
    this.limpiarAlertas();
    this.service.desactivarFamiliaFormulario(fam.famId, 'local').subscribe({
      next: () => {
        this.guardando.set(false);
        this.globalState.limpiarError();
        this.mostrarMensaje('Familia desactivada correctamente.');
        this.cargarFamilias();
      },
      error: error => {
        this.guardando.set(false);
        this.mostrarError(this.obtenerMensajeError(error, 'No se pudo desactivar la familia. Verifique que no tenga versiones vigentes.'));
        this.cargarFamilias();
      }
    });
  }

  confirmarActivarFamilia(fam: FamiliaFormularioDto): void {
    void this.confirmation.confirm({
      title: `¿Activar nuevamente la familia ${fam.famNombre}?`,
      message: 'La familia volverá a estar disponible para nuevas operaciones.',
      icon: 'question',
      confirmLabel: 'Sí, activar',
      cancelLabel: 'Cancelar',
      confirmIcon: 'activate',
      confirmVariant: 'success'
    }).then(confirmed => {
      if (confirmed) this.activarFamilia(fam);
    });
  }

  activarFamilia(fam: FamiliaFormularioDto): void {
    this.guardando.set(true);
    this.limpiarAlertas();
    this.service.activarFamiliaFormulario(fam.famId, 'local').subscribe({
      next: () => {
        this.guardando.set(false);
        this.globalState.limpiarError();
        this.mostrarMensaje('Familia activada correctamente.');
        this.cargarFamilias();
      },
      error: (error: unknown) => {
        this.guardando.set(false);
        this.mostrarError(this.obtenerMensajeError(error, 'No se pudo activar la familia.'));
        this.cargarFamilias();
      }
    });
  }

  confirmarArchivarFamilia(fam: FamiliaFormularioDto): void {
    if (fam.famActivo || fam.famArchivada || fam.famPredeterminada) return;
    void this.confirmation.confirm({
      title: `¿Archivar la familia ${fam.famNombre}?`,
      message: 'Se retirará de los listados operativos y se conservarán versiones, evaluaciones y auditoría. La restauración devolverá la familia a estado inactivo.',
      icon: 'warning', confirmLabel: 'Archivar', cancelLabel: 'Cancelar', confirmIcon: 'deactivate', confirmVariant: 'danger'
    }).then(confirmed => {
      if (!confirmed) return;
      this.familiaArchivadoPendiente.set(fam);
      this.motivoArchivado.set('');
    });
  }

  cancelarArchivado(): void {
    this.familiaArchivadoPendiente.set(null);
    this.motivoArchivado.set('');
  }

  confirmarArchivado(): void {
    const familia = this.familiaArchivadoPendiente();
    const motivo = this.motivoArchivado().trim();
    if (!familia || !motivo) return;
    this.cancelarArchivado();
    this.archivarFamilia(familia, motivo);
  }

  archivarFamilia(fam: FamiliaFormularioDto, motivo: string): void {
    this.guardando.set(true);
    this.service.archivarFamiliaFormulario(fam.famId, motivo).subscribe({
      next: () => { this.guardando.set(false); this.mostrarMensaje('Familia archivada correctamente. El historial se conserva.'); this.cargarFamilias(); },
      error: error => { this.guardando.set(false); this.mostrarError(this.obtenerMensajeError(error, 'No se pudo archivar la familia.')); this.cargarFamilias(); }
    });
  }

  confirmarRestaurarFamilia(fam: FamiliaFormularioDto): void {
    if (!fam.famArchivada) return;
    void this.confirmation.confirm({
      title: `¿Restaurar la familia ${fam.famNombre}?`,
      message: 'La familia se restaurará como INACTIVA. No se publicarán ni activarán versiones automáticamente.',
      icon: 'question', confirmLabel: 'Restaurar', cancelLabel: 'Cancelar', confirmIcon: 'activate', confirmVariant: 'success'
    }).then(confirmed => {
      if (!confirmed) return;
      this.familiaRestauracionPendiente.set(fam);
      this.motivoRestauracion.set('');
    });
  }

  cancelarRestauracion(): void {
    this.familiaRestauracionPendiente.set(null);
    this.motivoRestauracion.set('');
  }

  confirmarRestauracion(): void {
    const familia = this.familiaRestauracionPendiente();
    const motivo = this.motivoRestauracion().trim();
    if (!familia || !motivo) return;
    this.cancelarRestauracion();
    this.restaurarFamilia(familia, motivo);
  }

  restaurarFamilia(fam: FamiliaFormularioDto, motivo: string): void {
    this.guardando.set(true);
    this.service.restaurarFamiliaFormulario(fam.famId, motivo).subscribe({
      next: () => { this.guardando.set(false); this.mostrarMensaje('Familia restaurada como inactiva.'); this.cargarFamilias(); },
      error: error => { this.guardando.set(false); this.mostrarError(this.obtenerMensajeError(error, 'No se pudo restaurar la familia.')); this.cargarFamilias(); }
    });
  }

  confirmarEliminarFamilia(fam: FamiliaFormularioDto): void {
    if (fam.famArchivada) return;
    void this.confirmation.confirm({
      title: `Eliminar familia ${fam.famCodigo}`,
      message: 'Esta operación eliminará permanentemente esta familia. Solo es posible porque actualmente no posee versiones asociadas.',
      icon: 'error',
      confirmLabel: 'Eliminar',
      cancelLabel: 'Cancelar',
      confirmIcon: 'delete',
      confirmVariant: 'danger'
    }).then(confirmed => {
      if (confirmed) this.eliminarFamilia(fam);
    });
  }

  eliminarFamilia(fam: FamiliaFormularioDto): void {
    this.guardando.set(true);
    this.limpiarAlertas();
    this.service.eliminarFamiliaFormulario(fam.famId).subscribe({
      next: () => {
        this.guardando.set(false);
        this.globalState.limpiarError();
        this.mostrarMensaje('Familia eliminada correctamente.');
        this.cargarFamilias();
      },
      error: (error: unknown) => {
        this.guardando.set(false);
        this.mostrarError(this.obtenerMensajeError(error, 'No se pudo eliminar la familia.'));
        this.cargarFamilias();
      }
    });
  }

  cargarVersiones(): void {
    this.cargandoPlantillas.set(true);
    this.errorPlantillas.set(null);
    const codigo = this.familiaSeleccionada();
    if (!codigo) {
      this.versiones.set([]);
      this.cargandoPlantillas.set(false);
      this.detalleFamiliaRef?.instance.refrescarVersionesDespuesDeAccion();
      return;
    }
    this.service.listarHistorialVersionesFormulario(codigo).subscribe({
      next: versiones => {
        this.versiones.set(versiones);
        this.cargandoPlantillas.set(false);
        this.detalleFamiliaRef?.instance.refrescarVersionesDespuesDeAccion();
      },
      error: error => {
        const msg = this.obtenerMensajeError(error, 'No se pudo cargar el historial de formularios.');
        this.errorPlantillas.set(msg);
        this.cargandoPlantillas.set(false);
      }
    });
  }

  cargarModulo(): void {
    this.cargarFormularioVigente();
    this.cargarEvaluaciones();
  }

  cargarFormularioVigente(): void {
    const obtenerDefault = (this.service as MatricesRiesgosService & {
      obtenerFamiliaPredeterminada?: () => Observable<FamiliaPredeterminadaDto>;
    }).obtenerFamiliaPredeterminada;
    if (typeof obtenerDefault === 'function') {
      this.cargarFamiliaPredeterminada();
      return;
    }

    this.cargandoFormulario.set(true);
    this.errorFormulario.set(null);

    this.service.obtenerVersionVigenteFormulario().subscribe({
      next: version => {
        this.versionVigente.set(version);
        this.inicializarRespuestas();
        this.cargarMetodologia();
      },
      error: error => {
        const msg = this.obtenerMensajeError(error, 'No se pudo cargar la versión vigente del formulario.');
        this.errorFormulario.set(msg);
        this.cargandoFormulario.set(false);
      }
    });
  }

  cargarRiesgos(): void {
    this.service.listarRiesgosPaginados(false, 1, 200).subscribe({
      next: respuesta => {
        const items = Array.isArray(respuesta?.items) ? respuesta.items : [];
        this.riesgos.set(items.map(riesgo => ({
          ...riesgo,
          rieNombre: normalizarMojibakeVisibleUtf8(riesgo.rieNombre),
          rieDescripcion: riesgo.rieDescripcion ? normalizarMojibakeVisibleUtf8(riesgo.rieDescripcion) : riesgo.rieDescripcion
        })));
      },
      error: () => this.riesgos.set([])
    });
  }

  seleccionarRiesgoNuevaEvaluacion(value: string | number | null): void {
    const riskId = Number(value);
    this.riesgoId.set(Number.isFinite(riskId) ? riskId : 0);
  }

  cargarMetodologia(): void {
    this.service.metodologiaVigente().subscribe({
      next: metodologia => {
        this.metodologia.set(metodologia);
        this.inicializarRespuestas();
        this.cargandoFormulario.set(false);
      },
      error: error => {
        const msg = this.obtenerMensajeError(error, 'No se pudo cargar la metodología dinámica vigente.');
        this.errorFormulario.set(msg);
        this.cargandoFormulario.set(false);
      }
    });
  }

  private timerDebounceBuscar: ReturnType<typeof setTimeout> | null = null;

  private cancelarDebounceBuscarPendiente(): void {
    if (this.timerDebounceBuscar) {
      clearTimeout(this.timerDebounceBuscar);
      this.timerDebounceBuscar = null;
    }
  }

  alCambiarFiltroBuscar(valor: string): void {
    this.filtroBuscar.set(valor);
    this.pagina.set(1);
    this.cancelarDebounceBuscarPendiente();
    this.timerDebounceBuscar = setTimeout(() => {
      this.timerDebounceBuscar = null;
      this.cargarEvaluaciones();
    }, 300);
  }

  alCambiarFiltroEstado(valor: string): void {
    this.cancelarDebounceBuscarPendiente();
    this.filtroEstado.set(valor);
    this.pagina.set(1);
    this.cargarEvaluaciones();
  }

  cambiarRegistrosPorPagina(cantidad: number): void {
    const num = Number(cantidad);
    if (!Number.isInteger(num) || !this.opcionesRegistrosPorPagina.includes(num as 10 | 20 | 50)) {
      return;
    }
    if (num === this.registrosPorPagina()) {
      return;
    }
    this.registrosPorPagina.set(num);
    this.pagina.set(1);
    this.cargarEvaluaciones();
  }

  cambiarPagina(nuevaPagina: number): void {
    const p = Number(nuevaPagina);
    if (!Number.isFinite(p) || !Number.isInteger(p)) {
      return;
    }
    const total = this.totalPaginas();
    if (total <= 0 || p < 1 || p > total || p === this.pagina()) {
      return;
    }
    this.pagina.set(p);
    this.cargarEvaluaciones();
  }

  limpiarFiltrosEvaluaciones(): void {
    this.cancelarDebounceBuscarPendiente();
    this.filtroBuscar.set('');
    this.filtroEstado.set('');
    this.pagina.set(1);
    this.cargarEvaluaciones();
  }

  cargarEvaluaciones(): void {
    const filtrosSnapshot = {
      buscar: this.filtroBuscar().trim() || undefined,
      estado: this.filtroEstado().trim() || undefined,
      pagina: this.pagina(),
      registrosPorPagina: this.registrosPorPagina()
    };

    const solicitudId = ++this.secuenciaCargaEvaluaciones;

    this.suscripcionEvaluaciones?.unsubscribe();
    this.suscripcionEvaluaciones = null;

    this.cargandoEvaluaciones.set(true);
    this.errorEvaluaciones.set(null);

    this.suscripcionEvaluaciones = this.service.listarEvaluaciones(filtrosSnapshot).subscribe({
      next: paginado => {
        if (solicitudId !== this.secuenciaCargaEvaluaciones) {
          return;
        }

        const items = Array.isArray(paginado?.items)
          ? paginado.items.map(item => ({
              ...item,
              riesgoNombre: normalizarMojibakeVisibleUtf8(item.riesgoNombre)
            }))
          : [];
        this.evaluaciones.set(items);

        const totalReg = Number.isFinite(paginado?.totalRegistros) && Math.floor(paginado.totalRegistros) >= 0
          ? Math.floor(paginado.totalRegistros)
          : 0;
        this.totalRegistros.set(totalReg);

        const totalPag = Number.isFinite(paginado?.totalPaginas) && Math.floor(paginado.totalPaginas) >= 0
          ? Math.floor(paginado.totalPaginas)
          : (filtrosSnapshot.registrosPorPagina > 0 ? Math.ceil(totalReg / filtrosSnapshot.registrosPorPagina) : 0);
        this.totalPaginas.set(totalPag);

        let pagEfectiva = Number.isFinite(paginado?.pagina) && Math.floor(paginado.pagina) >= 1
          ? Math.floor(paginado.pagina)
          : filtrosSnapshot.pagina;

        if (totalPag === 0) {
          pagEfectiva = 1;
        } else if (pagEfectiva > totalPag) {
          pagEfectiva = totalPag;
        }

        this.pagina.set(pagEfectiva);
        this.cargandoEvaluaciones.set(false);
      },
      error: error => {
        if (solicitudId !== this.secuenciaCargaEvaluaciones) {
          return;
        }

        const msg = this.obtenerMensajeError(error, 'No se pudieron consultar las evaluaciones.');
        this.errorEvaluaciones.set(msg);
        this.evaluaciones.set([]);
        this.totalRegistros.set(0);
        this.totalPaginas.set(0);
        this.cargandoEvaluaciones.set(false);
      }
    });
  }

  cargarConsolidado(): void {
    const solicitudId = ++this.secuenciaCargaConsolidado;
    this.cargandoConsolidado.set(true);
    this.errorConsolidado.set(null);
    const filtro = this.filtroReporteConsolidado();
    this.service.obtenerConsolidadoPaginado(filtro).subscribe({
      next: resultado => {
        if (solicitudId !== this.secuenciaCargaConsolidado) return;
        const items = Array.isArray(resultado?.items) ? resultado.items : [];
        this.consolidado.set(items.map(fila => this.normalizarFilaReporte(fila)));
        this.totalRegistrosConsolidado.set(resultado?.totalRegistros ?? 0);
        this.totalPaginasServidorConsolidado.set(resultado?.totalPaginas ?? 0);
        this.totalesConsolidado.set(resultado?.totales ?? {
          totalRiesgos: 0,
          totalConEvaluacionOficial: 0,
          totalSinEvaluacionOficial: 0,
          totalAltoCritico: 0
        });
        this.paginaConsolidado.set(resultado?.pagina ?? 1);
        const tamanoPagina = resultado?.tamanoPagina;
        if (tamanoPagina === 10 || tamanoPagina === 20 || tamanoPagina === 50) {
          this.registrosPorPaginaConsolidado.set(tamanoPagina);
        }
        this.cargandoConsolidado.set(false);
      },
      error: error => {
        if (solicitudId !== this.secuenciaCargaConsolidado) return;
        const msg = this.obtenerMensajeError(error, 'No se pudo cargar la matriz consolidada.');
        this.errorConsolidado.set(msg);
        this.cargandoConsolidado.set(false);
      }
    });
  }

  private normalizarFilaReporte(fila: RiesgoReporteFila): RiesgoReporteFila {
    return {
      ...fila,
      areaPrincipal: fila.areaPrincipal === null ? null : normalizarMojibakeVisibleUtf8(fila.areaPrincipal),
      duenoRiesgo: fila.duenoRiesgo === null ? null : normalizarMojibakeVisibleUtf8(fila.duenoRiesgo),
      nivelInherente: fila.nivelInherente === null ? null : normalizarMojibakeVisibleUtf8(fila.nivelInherente),
      nivelResidual: fila.nivelResidual === null ? null : normalizarMojibakeVisibleUtf8(fila.nivelResidual),
      respuestaRiesgo: fila.respuestaRiesgo === null ? null : normalizarMojibakeVisibleUtf8(fila.respuestaRiesgo),
      estadoEvaluacion: normalizarMojibakeVisibleUtf8(fila.estadoEvaluacion)
    };
  }

  cargarFamiliaPredeterminada(): void {
    const obtenerDefault = (this.service as MatricesRiesgosService & {
      obtenerFamiliaPredeterminada?: () => Observable<FamiliaPredeterminadaDto>;
    }).obtenerFamiliaPredeterminada;
    if (typeof obtenerDefault !== 'function') return;

    const secuencia = ++this.secuenciaContextoPredeterminado;
    this.cargandoFamiliaPredeterminada.set(true);
    this.errorFamiliaPredeterminada.set(null);
    obtenerDefault.call(this.service).subscribe({
      next: configuracion => {
        if (secuencia !== this.secuenciaContextoPredeterminado) return;
        this.familiaPredeterminada.set(configuracion);
        if (!configuracion.configurada) {
          this.versionVigente.set(null);
          this.metodologia.set(null);
          this.errorFormulario.set(null);
          this.cargandoFormulario.set(false);
          this.cargandoFamiliaPredeterminada.set(false);
          return;
        }

        const versionId = Number(configuracion.versionVigenteId);
        if (!Number.isInteger(versionId) || versionId <= 0) {
          this.versionVigente.set(null);
          this.metodologia.set(null);
          this.errorFamiliaPredeterminada.set('La configuración de la familia predeterminada no contiene una versión vigente válida.');
          this.cargandoFormulario.set(false);
          this.cargandoFamiliaPredeterminada.set(false);
          return;
        }

        this.cargandoFormulario.set(true);
        this.cargarContextoPorVersion(versionId, secuencia, false);
      },
      error: error => {
        if (secuencia !== this.secuenciaContextoPredeterminado) return;
        this.familiaPredeterminada.set(null);
        this.errorFamiliaPredeterminada.set(this.obtenerMensajeError(error, 'No se pudo consultar la familia predeterminada.'));
        this.versionVigente.set(null);
        this.metodologia.set(null);
        this.cargandoFormulario.set(false);
        this.cargandoFamiliaPredeterminada.set(false);
      }
    });
  }

  private cargarContextoPorVersion(versionId: number, secuencia: number, esNuevaEvaluacion: boolean): void {
    const obtenerVersion = (this.service as MatricesRiesgosService & {
      obtenerVersionFormulario?: (id: number) => Observable<VersionFormularioDto>;
    }).obtenerVersionFormulario;
    const obtenerMetodologia = (this.service as MatricesRiesgosService & {
      metodologiaPorVersion?: (id: number) => Observable<MetodologiaFormulario>;
    }).metodologiaPorVersion;

    if (typeof obtenerVersion !== 'function' || typeof obtenerMetodologia !== 'function') {
      this.cargandoFormulario.set(false);
      return;
    }

    forkJoin({
      version: obtenerVersion.call(this.service, versionId),
      metodologia: obtenerMetodologia.call(this.service, versionId)
    }).subscribe({
      next: resultado => {
        if ((!esNuevaEvaluacion && secuencia !== this.secuenciaContextoPredeterminado)
          || (esNuevaEvaluacion && secuencia !== this.secuenciaVersionNuevaEvaluacion)) return;
        this.versionVigente.set(resultado.version);
        this.metodologia.set(resultado.metodologia);
        this.inicializarRespuestas();
        this.errorFormulario.set(null);
        this.cargandoFormulario.set(false);
        if (!esNuevaEvaluacion) this.cargandoFamiliaPredeterminada.set(false);
      },
      error: error => {
        if ((!esNuevaEvaluacion && secuencia !== this.secuenciaContextoPredeterminado)
          || (esNuevaEvaluacion && secuencia !== this.secuenciaVersionNuevaEvaluacion)) return;
        this.versionVigente.set(null);
        this.metodologia.set(null);
        this.errorFormulario.set(this.obtenerMensajeError(error, 'No se pudo cargar la versión exacta de la familia seleccionada.'));
        this.cargandoFormulario.set(false);
        if (!esNuevaEvaluacion) this.cargandoFamiliaPredeterminada.set(false);
      }
    });
  }

  descargarConsolidado(formato: 'excel' | 'pdf'): void {
    this.limpiarAlertas();
    const solicitud = formato === 'excel'
      ? this.service.descargarConsolidadoExcel(this.filtroReporteConsolidado())
      : this.service.descargarConsolidadoPdf(this.filtroReporteConsolidado());

    solicitud.subscribe({
        next: blob => {
          const esExcel = formato === 'excel';
          if (esExcel) {
            void this.reportPreview.openExcelBlob(
              blob,
              'Matriz_Riesgos.xlsx',
              'Vista previa de la matriz consolidada de riesgos',
              'Revise los riesgos y filtros aplicados antes de descargar el libro Excel.'
            );
          } else {
            this.reportPreview.openPdf(
              blob,
              'Matriz_Riesgos.pdf',
              'Vista previa del reporte consolidado de riesgos',
              'Revise la proyección consolidada antes de descargar el documento.'
            );
          }
        },
      error: error => this.mostrarError(this.obtenerMensajeError(error, `No se pudo generar el reporte ${formato.toUpperCase()}.`))
    });
  }

  actualizarRespuesta(campo: CampoFormulario, valor: ValorRespuestaFormulario): void {
    this.respuestas.update(actuales => ({ ...actuales, [campo.clave]: valor }));
    const key = this.normalizarClaveContrato(campo.clave);
    if (this.erroresValidacionBloque()[key]) {
      this.erroresValidacionBloque.update(errors => {
        const next = { ...errors };
        delete next[key];
        return next;
      });
    }
    this.cambiosSinGuardar.set(true);
    if (this.modalNuevaEvaluacionAbierto() || this.modalEditarAbierto()) {
      this.solicitarCalculoPreview();
    }
  }

  @HostListener('window:beforeunload', ['$event'])
  protegerRecargaConCambiosPendientes(event: BeforeUnloadEvent): void {
    if (!this.cambiosSinGuardar()) return;
    event.preventDefault();
    event.returnValue = '';
  }

  private solicitarCalculoPreview(): void {
    const versionId = this.evaluacionSeleccionada()?.evaVersionId ?? this.versionVigente()?.verId ?? 0;
    const riskId = this.riesgoId();
    if (versionId <= 0 || riskId <= 0) return;

    const current = this.evaluacionSeleccionada();
    const request: EvaluacionRiesgoDto = {
      evaId: current?.evaId ?? 0,
      evaRiesgoId: riskId,
      evaVersionId: versionId,
      evaEstado: current?.evaEstado ?? 'BORRADOR',
      evaDataJson: JSON.stringify(this.construirRespuestasFormularioPersistibles()),
      evaDataCalcJson: null,
      evaVri: null,
      evaVrr: null,
      evaFechaEval: current?.evaFechaEval ?? new Date().toISOString(),
      evaUsrEval: current?.evaUsrEval ?? 0,
      evaVersionRow: current?.evaVersionRow ?? 1,
      evaActivo: current?.evaActivo ?? true
    };

    this.calculoPreviewSubscription?.unsubscribe();
    this.actualizandoCalculo.set(true);
    this.errorCalculoPreview.set(null);
    this.calculoPreviewSubscription = timer(350).pipe(
      switchMap(() => this.service.previsualizarCalculoEvaluacion(request))
    ).subscribe({
      next: result => {
        this.calculoPreview.set(result);
        this.actualizandoCalculo.set(false);
      },
      error: error => {
        this.calculoPreview.set(null);
        this.actualizandoCalculo.set(false);
        this.errorCalculoPreview.set(this.obtenerMensajeError(error, 'No se pudo actualizar el cálculo en el servidor.'));
      }
    });
  }

  camposDeBloqueActual(): CampoFormulario[] {
    const bloque = this.bloqueEvaluacionActual();
    const contract = MATRIX_FIELDS.filter(field => field.block === bloque && field.key);
    const keys = new Set(contract.map(field => this.normalizarClaveContrato(field.key!)));
    return this.seccionesModal().flatMap(section => section.campos).filter(field => {
      const key = this.normalizarClaveContrato(field.clave);
      return keys.has(key) || (!MATRIX_FIELDS.some(item => item.key && this.normalizarClaveContrato(item.key) === key) && bloque === 1);
    }).map(field => {
      const contractField = MATRIX_FIELDS.find(item => item.key && this.normalizarClaveContrato(item.key) === this.normalizarClaveContrato(field.clave));
      return {
        ...field,
        soloLectura: field.soloLectura || (contractField !== undefined && !contractField.editable),
        obligatorio: this.esGTICCondicional(field.clave) ? false : field.obligatorio
      };
    });
  }

  seccionesDelBloqueActual(): Array<{ clave: string; titulo: string; columnasPorFila?: number; campos: CampoFormulario[] }> {
    const allowed = new Set(this.camposDeBloqueActual().map(field => field.clave));
    return this.seccionesModal()
      .map(section => ({ ...section, campos: section.campos.filter(field => allowed.has(field.clave)) }))
      .filter(section => section.campos.length > 0);
  }

  private normalizarClaveContrato(value: string): string {
    return value.toLocaleLowerCase('es-HN').replace(/[^a-z0-9]/g, '');
  }

  irABloqueEvaluacion(bloque: BloqueEvaluacion): void {
    if (!this.bloquesEvaluacion.includes(bloque)) return;
    this.bloqueEvaluacionActual.set(bloque);
  }

  volverBloqueEvaluacion(): void {
    if (this.bloqueEvaluacionActual() > 1) {
      this.bloqueEvaluacionActual.update(current => (current - 1) as BloqueEvaluacion);
    }
  }

  continuarBloqueEvaluacion(): void {
    const bloqueActual = this.bloqueEvaluacionActual();
    if (bloqueActual < 6) this.irABloqueEvaluacion((bloqueActual + 1) as BloqueEvaluacion);
  }

  errorCampoEditor(campo: CampoFormulario): string | null {
    return this.erroresValidacionBloque()[this.normalizarClaveContrato(campo.clave)] ?? null;
  }

  private idCampoEditor(clave: string, prefix: 'campo-new' | 'campo-edit'): string {
    const safe = clave.replace(/[^A-Za-z0-9_-]+/g, '-').replace(/^-+|-+$/g, '') || 'sin-clave';
    return `${prefix}-${safe}`;
  }

  private esGTICCondicional(clave: string): boolean {
    return ['amenazasgtic', 'vulnerabilidadesgtic', 'activosinformaciongtic']
      .includes(this.normalizarClaveContrato(clave));
  }

  guardarBorradorEvaluacion(): void {
    const actual = this.evaluacionSeleccionada();
    const versionId = actual?.evaVersionId ?? this.versionVigente()?.verId ?? 0;
    if (this.riesgoId() <= 0 || versionId <= 0 || this.guardando()) {
      this.mostrarError('Seleccione un riesgo y una versión vigente antes de guardar el borrador.');
      return;
    }

    this.guardando.set(true);
    this.limpiarAlertas();
    const dto: EvaluacionRiesgoDto = {
      evaId: actual?.evaId ?? 0,
      evaRiesgoId: this.riesgoId(),
      evaVersionId: versionId,
      evaEstado: 'BORRADOR',
      evaDataJson: JSON.stringify(this.construirRespuestasFormularioPersistibles()),
      evaDataCalcJson: '{}',
      evaVri: null,
      evaVrr: null,
      evaFechaEval: actual?.evaFechaEval ?? new Date().toISOString(),
      evaUsrEval: actual?.evaUsrEval ?? 0,
      evaVersionRow: actual?.evaVersionRow ?? 1,
      evaActivo: true
    };
    const save = actual
      ? this.service.actualizarBorradorEvaluacion(actual.evaId, dto).pipe(map(() => actual.evaId))
      : this.service.guardarBorradorEvaluacion(dto);

    save.subscribe({
      next: evaluationId => this.recargarBorradorGuardado(evaluationId),
      error: error => {
        this.guardando.set(false);
        this.mostrarError(this.obtenerMensajeError(error, 'No se pudo guardar el borrador.'));
      }
    });
  }

  private recargarBorradorGuardado(evaluationId: number): void {
    // Los endpoints de borrador solo devuelven el ID/mensaje; esta lectura explícita
    // obtiene la versión canónica calculada después de Guardar.
    this.service.obtenerEvaluacion(evaluationId).subscribe({
      next: detalle => {
        this.evaluacionSeleccionada.set(detalle);
        this.riesgoId.set(detalle.evaRiesgoId);
        this.respuestas.set(this.parsearRespuestas(detalle.evaDataJson));
        this.cambiosSinGuardar.set(false);
        if (!this.evaluacionResumenSeleccionada()) {
          const riesgo = this.riesgos().find(item => item.rieId === detalle.evaRiesgoId);
          const version = this.versionVigente();
          this.evaluacionResumenSeleccionada.set({
            evaId: detalle.evaId,
            evaRiesgoId: detalle.evaRiesgoId,
            riesgoCodigo: riesgo?.rieCodigo ?? '',
            riesgoNombre: riesgo?.rieNombre ?? '',
            evaVersionId: detalle.evaVersionId,
            versionCodigo: version?.verCodigo ?? '',
            versionNumero: version?.verVersion ?? 0,
            estado: detalle.evaEstado,
            evaEstado: detalle.evaEstado,
            fechaEval: detalle.evaFechaEval,
            evaFechaEval: detalle.evaFechaEval
          });
          this.versionHistorica.set(version ?? null);
          this.metodologiaHistorica.set(this.metodologia());
        }
        this.modalNuevaEvaluacionAbierto.set(false);
        this.modalEditarAbierto.set(true);
        this.cargandoProyeccionEvaluacion.set(true);
        const projectionRequest = this.solicitarProyeccion(evaluationId);
        if (!projectionRequest) {
          this.cargandoProyeccionEvaluacion.set(false);
          this.guardando.set(false);
          this.mostrarMensaje(`Borrador #${evaluationId} guardado.`);
          this.cargarEvaluaciones();
          return;
        }
        projectionRequest.subscribe({
          next: projection => {
            this.proyeccionEvaluacion.set(projection);
            this.calculoPreview.set({
              listo: true,
              evaDataCalcJson: this.evaluacionSeleccionada()?.evaDataCalcJson ?? null,
              evaVri: this.evaluacionSeleccionada()?.evaVri ?? null,
              evaEtp: null,
              evaVrr: this.evaluacionSeleccionada()?.evaVrr ?? null,
              obligatoriosPendientes: []
            });
            this.cargandoProyeccionEvaluacion.set(false);
            this.guardando.set(false);
            this.mostrarMensaje(`Borrador #${evaluationId} guardado.`);
            this.cargarEvaluaciones();
          },
          error: error => {
            this.cargandoProyeccionEvaluacion.set(false);
            this.guardando.set(false);
            this.mostrarError(this.obtenerMensajeError(error, 'El borrador se guardó, pero no se pudo actualizar la proyección institucional.'));
          }
        });
      },
      error: error => {
        this.guardando.set(false);
        this.mostrarError(this.obtenerMensajeError(error, 'El borrador se guardó, pero no se pudo recargar desde el servidor.'));
      }
    });
  }

  refrescarProyeccionEvaluacion(): void {
    const evaluationId = this.evaluacionSeleccionada()?.evaId;
    if (!evaluationId) return;
    this.cargandoProyeccionEvaluacion.set(true);
    const projectionRequest = this.solicitarProyeccion(evaluationId);
    if (!projectionRequest) {
      this.cargandoProyeccionEvaluacion.set(false);
      return;
    }
    projectionRequest.subscribe({
      next: projection => {
        this.proyeccionEvaluacion.set(projection);
        this.cargandoProyeccionEvaluacion.set(false);
        this.solicitarCalculoPreview();
      },
      error: error => {
        this.cargandoProyeccionEvaluacion.set(false);
        this.mostrarError(this.obtenerMensajeError(error, 'Los datos se guardaron, pero no se pudo refrescar la proyección institucional.'));
      }
    });
  }

  private solicitarProyeccion(evaluationId: number): Observable<InstitutionalEvaluationProjectionDto> | null {
    const service = this.service as MatricesRiesgosService & {
      obtenerProyeccionInstitucional?: (id: number) => Observable<InstitutionalEvaluationProjectionDto>;
    };
    return typeof service.obtenerProyeccionInstitucional === 'function'
      ? service.obtenerProyeccionInstitucional(evaluationId)
      : null;
  }

  valorRespuesta(campo: CampoFormulario): ValorRespuestaFormulario {
    if (campo.tipo === 'formula' || campo.soloLectura) {
      const contractField = MATRIX_FIELDS.find(field => field.key && this.normalizarClaveContrato(field.key) === this.normalizarClaveContrato(campo.clave));
      const projectionValue = contractField ? this.valorProyeccion(contractField.ordinal) : null;
      if (projectionValue !== null && projectionValue !== undefined) return projectionValue as ValorRespuestaFormulario;

      const calcJson = this.calculoPreview()?.evaDataCalcJson ?? this.evaluacionSeleccionada()?.evaDataCalcJson;
      if (calcJson && contractField?.key) {
        try {
          const calculated = JSON.parse(calcJson) as Record<string, unknown>;
          const value = calculated[contractField.key];
          if (value !== null && value !== undefined && ['string', 'number', 'boolean'].includes(typeof value)) {
            return value as ValorRespuestaFormulario;
          }
        } catch {
          // A malformed persisted calculation is not shown as a user-entered answer.
        }
      }
    }
    return this.respuestas()[campo.clave] ?? null;
  }

  opcionesCatalogo(campo: CampoFormulario): Array<{ codigo: string; valor: string }> {
    if (!campo.codigoCatalogo) return [];
    if (campo.codigoCatalogo.toUpperCase() === 'MR_RESPUESTA_RIESGO') {
      return canonicalCatalogOptions('RISK_RESPONSE').map(item => ({ codigo: item.codigo, valor: item.valor }));
    }

    const version = this.modalVerAbierto() || this.modalEditarAbierto()
      ? this.versionHistorica()
      : this.versionVigente();
    const catalogosVersion = version?.verJson
      ? this.extraerDefinicionVersion(version).catalogos
      : undefined;
    const catalogos = catalogosVersion
      ?? (this.modalVerAbierto() || this.modalEditarAbierto()
        ? this.metodologiaHistorica()?.catalogos
        : this.metodologia()?.catalogos);

    return catalogos
      ?.find(catalogo => catalogo.codigo.toLowerCase() === campo.codigoCatalogo!.toLowerCase())
      ?.elementos
      .slice()
      .sort((a, b) => a.orden - b.orden) ?? [];
  }

  obtenerEtiquetaCatalogo(campo: CampoFormulario, valor: unknown): string {
    if (valor === null || valor === undefined || valor === '') return '-';
    const opciones = this.opcionesCatalogo(campo);
    const item = opciones.find(o => String(o.codigo) === String(valor));
    return item ? `${item.valor} (${item.codigo})` : String(valor);
  }

  nuevaEvaluacion(): void {
    this.calculoPreviewSubscription?.unsubscribe();
    this.calculoPreviewSubscription = null;
    this.calculoPreview.set(null);
    this.errorCalculoPreview.set(null);
    this.actualizandoCalculo.set(false);
    this.limpiarAlertasEditor('newEvaluation');
    this.evaluacionSeleccionada.set(null);
    this.proyeccionMatrizCompleta.set(null);
    this.proyeccionMatrizCompletaEnCarga.set(false);
    this.evaluacionResumenSeleccionada.set(null);
    this.riesgoId.set(0);
    ++this.secuenciaVersionNuevaEvaluacion;
    this.familiaSeleccionada.set('');
    this.versionVigente.set(null);
    this.metodologia.set(null);
    this.errorFormulario.set(null);
    this.cargandoFormulario.set(false);
    this.versionHistorica.set(null);
    this.metodologiaHistorica.set(null);
    this.respuestas.set({});
    this.bloqueEvaluacionActual.set(1);
    this.proyeccionEvaluacion.set(null);
    // La publicación o reactivación puede haber ocurrido después de la carga inicial.
    // Refrescar el contrato servidor antes de evaluar evita reutilizar una elegibilidad obsoleta.
    const codigoPredeterminado = this.familiaPredeterminada()?.familiaCodigo;
    const familiaPredeterminadaActual = codigoPredeterminado
      ? this.familias().find(familia => familia.famCodigo === codigoPredeterminado)
      : null;
    if (!familiaPredeterminadaActual?.tieneVersionVigente) this.cargarFamilias();
    this.modalNuevaEvaluacionAbierto.set(true);

    if (this.familiaPredeterminada()?.configurada && codigoPredeterminado) {
      this.seleccionarFamilia(codigoPredeterminado);
    }
  }

  private filtroReporteConsolidado(): FiltroReporteMatrices {
    return {
      buscar: this.filtroBuscarConsolidado().trim() || undefined,
      estadoEvaluacion: this.filtroEstadoConsolidado() === 'TODOS' ? undefined : this.filtroEstadoConsolidado(),
      pagina: this.paginaConsolidado(),
      tamanoPagina: this.registrosPorPaginaConsolidado() as 10 | 20 | 50,
      ordenarPor: this.ordenConsolidado(),
      orden: this.direccionConsolidado()
    };
  }

  cerrarModalNuevaEvaluacion(): void {
    if (this.cambiosSinGuardar()) {
      void this.confirmarDescarteCambios().then(confirmado => {
        if (confirmado) this.completarCierreNuevaEvaluacion();
      });
      return;
    }
    this.completarCierreNuevaEvaluacion();
  }

  private completarCierreNuevaEvaluacion(): void {
    this.calculoPreviewSubscription?.unsubscribe();
    this.calculoPreviewSubscription = null;
    this.actualizandoCalculo.set(false);
    ++this.secuenciaVersionNuevaEvaluacion;
    this.modalNuevaEvaluacionAbierto.set(false);
    this.cambiosSinGuardar.set(false);
    this.limpiarAlertasEditor('newEvaluation');
    this.familiaSeleccionada.set('');
    this.versionVigente.set(null);
    this.metodologia.set(null);
    this.respuestas.set({});
    if (typeof (this.service as MatricesRiesgosService & {
      obtenerFamiliaPredeterminada?: () => Observable<FamiliaPredeterminadaDto>;
    }).obtenerFamiliaPredeterminada === 'function') {
      this.cargarFamiliaPredeterminada();
    }
  }

  familiaEsElegible(familia: FamiliaFormularioDto | null | undefined): boolean {
    // Fixtures legacy pueden omitir el campo; el contrato HTTP real siempre lo informa.
    return !!familia?.famActivo && familia.tieneVersionVigente !== false;
  }

  motivoFamiliaNoElegible(familia: FamiliaFormularioDto | null | undefined): string {
    if (!familia) return 'Seleccione una familia para continuar.';
    if (!familia.famActivo) return 'La familia está inactiva.';
    if (familia.tieneVersionVigente === false) return 'La familia no tiene una versión vigente publicada.';
    return '';
  }

  etiquetaFamiliaNueva(familia: FamiliaFormularioDto): string {
    const motivo = this.motivoFamiliaNoElegible(familia);
    return motivo ? `${familia.famNombre} — ${motivo}` : familia.famNombre;
  }

  abrirConfirmacionFamiliaPredeterminada(familia: FamiliaFormularioDto, event?: Event): void {
    if (!this.usuarioEsAdministrador() || familia.famPredeterminada || !this.familiaEsElegible(familia)) return;
    this.familiaObjetivoPredeterminada.set(familia);
    this.focoRetornoPredeterminada = event?.currentTarget instanceof HTMLElement
      ? event.currentTarget
      : document.activeElement instanceof HTMLElement ? document.activeElement : null;
    this.modalConfirmarPredeterminadaAbierto.set(true);
    setTimeout(() => document.getElementById('confirmar-familia-predeterminada')?.focus(), 0);
  }

  usuarioEsAdministrador(): boolean {
    return this.authService.tieneRol(['ADMINISTRADOR']);
  }

  cancelarConfirmacionFamiliaPredeterminada(): void {
    this.modalConfirmarPredeterminadaAbierto.set(false);
    this.familiaObjetivoPredeterminada.set(null);
    const foco = this.focoRetornoPredeterminada;
    this.focoRetornoPredeterminada = null;
    setTimeout(() => foco?.focus(), 0);
  }

  confirmarFamiliaPredeterminada(): void {
    const familia = this.familiaObjetivoPredeterminada();
    const establecer = (this.service as MatricesRiesgosService & {
      establecerFamiliaPredeterminada?: (id: number) => Observable<boolean>;
    }).establecerFamiliaPredeterminada;
    if (!familia || typeof establecer !== 'function' || this.operacionPredeterminadaEnCurso()) return;

    this.operacionPredeterminadaEnCurso.set(true);
    establecer.call(this.service, familia.famId).subscribe({
      next: success => {
        this.operacionPredeterminadaEnCurso.set(false);
        if (!success) {
          this.mostrarError('No se pudo establecer la familia predeterminada.');
          return;
        }
        this.cancelarConfirmacionFamiliaPredeterminada();
        this.mostrarMensaje(`La familia «${familia.famNombre}» se estableció como predeterminada.`);
        this.cargarFamilias();
        this.cargarFamiliaPredeterminada();
      },
      error: error => {
        this.operacionPredeterminadaEnCurso.set(false);
        const status = Number((error as { status?: number })?.status);
        if (status === 409) {
          this.mostrarError('La configuración cambió mientras realizabas la operación. Se actualizó el estado actual; inténtalo nuevamente.');
        } else if (status === 400) {
          this.mostrarError(this.obtenerMensajeError(error, 'La familia debe estar activa y contar con una versión publicada y vigente antes de establecerla como predeterminada.'));
        } else {
          this.mostrarError(this.obtenerMensajeError(error, 'No se pudo establecer la familia predeterminada.'));
        }
        this.cargarFamilias();
        this.cargarFamiliaPredeterminada();
      }
    });
  }

  abrirModalVer(resumen: EvaluacionRiesgoResumenDto): void {
    this.limpiarAlertas();
    this.cargando.set(true);
    this.evaluacionResumenSeleccionada.set(resumen);

    this.service.obtenerEvaluacion(resumen.evaId).subscribe({
      next: detalle => {
        this.evaluacionSeleccionada.set(detalle);
        this.riesgoId.set(detalle.evaRiesgoId);
        this.respuestas.set(this.parsearRespuestas(detalle.evaDataJson));

        this.service.metodologiaPorVersion(detalle.evaVersionId).subscribe({
          next: met => {
            this.metodologiaHistorica.set(met);
            this.cargando.set(false);
            this.modalVerAbierto.set(true);
          },
          error: error => {
            this.cargando.set(false);
            this.mostrarError(this.obtenerMensajeError(error, `No se pudo recuperar la metodología histórica para la versión ID ${detalle.evaVersionId}.`));
          }
        });
      },
      error: error => {
        this.cargando.set(false);
        this.mostrarError(this.obtenerMensajeError(error, 'No se pudo obtener el detalle de la evaluación.'));
      }
    });
  }

  abrirMatrizCompleta(resumen: EvaluacionRiesgoResumenDto): void {
    const solicitudId = ++this.secuenciaCargaMatrizCompleta;
    this.bloque5DetalleExpandido.set(false);
    this.limpiarAlertas();
    this.focoRetornoMatrizCompleta = document.activeElement instanceof HTMLElement ? document.activeElement : null;
    this.evaluacionResumenSeleccionada.set(resumen);
    this.evaluacionSeleccionada.set(null);
    this.proyeccionMatrizCompleta.set(null);
    this.proyeccionMatrizCompletaEnCarga.set(true);
    this.metodologiaMatrizCompleta.set(null);
    this.metodologiaMatrizCompletaEnCarga.set(false);
    this.metodologiaMatrizCompletaError.set(null);
    this.riesgoMaestroMatriz.set(null);
    this.matrizCompletaError.set(null);
    this.controlesMatrizCompleta.set([]);
    this.controlesMatrizError.set(null);
    this.controlesMatrizEnCarga.set(false);
    this.bloque4Matriz.set(null);
    this.bloque4MatrizError.set(null);
    this.bloque4MatrizEnCarga.set(false);
    this.bloque6Matriz.set(null);
    this.bloque6MatrizError.set(null);
    this.matrizCompletaEnCarga.set(true);
    this.modalMatrizCompletaAbierto.set(true);

    const projectionRequest = this.solicitarProyeccion(resumen.evaId);
    if (!projectionRequest) {
      this.proyeccionMatrizCompletaEnCarga.set(false);
      this.matrizCompletaError.set('La proyección institucional canónica no está disponible.');
    } else {
      projectionRequest.subscribe({
        next: projection => {
          if (solicitudId !== this.secuenciaCargaMatrizCompleta) return;
          const fieldNumbers = projection.fields.map(field => field.fieldNumber);
          const expected = Array.from({ length: 82 }, (_, index) => index + 1);
          if (projection.evaluationId !== resumen.evaId || fieldNumbers.length !== 82 || fieldNumbers.some((number, index) => number !== expected[index])) {
            this.matrizCompletaError.set('La proyección institucional no corresponde a la evaluación seleccionada o no contiene los 82 campos.');
          } else {
            this.proyeccionMatrizCompleta.set(projection);
          }
          this.proyeccionMatrizCompletaEnCarga.set(false);
        },
        error: error => {
          if (solicitudId !== this.secuenciaCargaMatrizCompleta) return;
          this.matrizCompletaError.set(this.obtenerMensajeErrorLectura(error, 'No se pudo cargar la proyección institucional canónica.'));
          this.proyeccionMatrizCompletaEnCarga.set(false);
        }
      });
    }

    this.service.obtenerEvaluacion(resumen.evaId).subscribe({
      next: detalle => {
        if (solicitudId !== this.secuenciaCargaMatrizCompleta) return;
        this.evaluacionSeleccionada.set(detalle);
        this.evaluacionResumenSeleccionada.update(actual => actual
          ? {
            ...actual,
            estado: detalle.evaEstado,
            evaEstado: detalle.evaEstado,
            evaVersionId: detalle.evaVersionId,
            fechaEval: detalle.evaFechaEval,
            evaFechaEval: detalle.evaFechaEval,
          }
          : actual);
        this.riesgoId.set(detalle.evaRiesgoId);
        this.respuestas.set(this.parsearRespuestas(detalle.evaDataJson));
        this.cargarControlesMatrizCompleta(detalle.evaId, solicitudId);
        this.cargarBloque4Matriz(detalle.evaId, solicitudId);
        this.service.obtenerBloque6(detalle.evaId).subscribe({
          next: response => {
            if (solicitudId === this.secuenciaCargaMatrizCompleta) {
              const safe = {
                ...response,
                senalesAlerta: Array.isArray(response?.senalesAlerta) ? response.senalesAlerta : [],
                controles: Array.isArray(response?.controles) ? response.controles : [],
                estadoRiesgo: typeof response?.estadoRiesgo === 'string' ? response.estadoRiesgo : null,
                observacionesArea: typeof response?.observacionesArea === 'string' ? response.observacionesArea : null,
                observacionesUgr: typeof response?.observacionesUgr === 'string' ? response.observacionesUgr : null,
                puedeEditarObservacionesArea: response?.puedeEditarObservacionesArea === true,
                puedeEditarObservacionesUgr: response?.puedeEditarObservacionesUgr === true
              } as MatrizBloque6Dto;
              this.bloque6Matriz.set(safe);
            }
          },
          error: error => {
            if (solicitudId === this.secuenciaCargaMatrizCompleta)
              this.bloque6MatrizError.set(this.obtenerMensajeErrorLectura(error, 'No se pudo cargar el monitoreo y las observaciones.'));
          }
        });
        this.cargarMetodologiaMatrizCompleta(detalle.evaVersionId, solicitudId);
        forkJoin({
          riesgo: this.service.obtenerRiesgo(detalle.evaRiesgoId),
          version: this.service.obtenerVersionFormulario(detalle.evaVersionId)
        }).subscribe({
          next: ({ riesgo, version }) => {
            if (solicitudId !== this.secuenciaCargaMatrizCompleta) return;
            this.riesgoMaestroMatriz.set(riesgo);
            this.evaluacionResumenSeleccionada.update(actual => actual
              ? { ...actual, versionCodigo: version.verCodigo, versionNumero: version.verVersion, evaVersionId: version.verId }
              : actual);
            this.matrizCompletaEnCarga.set(false);
          },
          error: error => {
            if (solicitudId !== this.secuenciaCargaMatrizCompleta) return;
            this.matrizCompletaError.set(this.obtenerMensajeErrorLectura(error, 'No se pudo recuperar el riesgo maestro vinculado a la evaluación.'));
            this.matrizCompletaEnCarga.set(false);
          }
        });
      },
      error: error => {
        if (solicitudId !== this.secuenciaCargaMatrizCompleta) return;
        this.matrizCompletaError.set(this.obtenerMensajeErrorLectura(error, 'No se pudo obtener la evaluación seleccionada.'));
        this.matrizCompletaEnCarga.set(false);
      }
    });
  }

  cerrarMatrizCompleta(): void {
    ++this.secuenciaCargaMatrizCompleta;
    this.bloque5DetalleExpandido.set(false);
    this.modalMatrizCompletaAbierto.set(false);
    this.proyeccionMatrizCompleta.set(null);
    this.proyeccionMatrizCompletaEnCarga.set(false);
    this.matrizCompletaEnCarga.set(false);
    this.metodologiaMatrizCompletaEnCarga.set(false);
    this.controlesMatrizEnCarga.set(false);
    this.bloque4MatrizEnCarga.set(false);
    this.bloque6Matriz.set(null);
    this.bloque6MatrizError.set(null);
    this.globalState.limpiarError();

    const foco = this.focoRetornoMatrizCompleta;
    this.focoRetornoMatrizCompleta = null;
    if (foco?.isConnected) setTimeout(() => foco.focus(), 0);
  }

  valorCampoMatriz(field: MatrixFieldContract): string {
    const canonical = this.valorProyeccionMatrizCompleta(field.ordinal);
    if (canonical !== null) return canonical;
    const evaluacion = this.evaluacionSeleccionada();
    const resumen = this.evaluacionResumenSeleccionada();
    if (!evaluacion || !resumen) return '—';

    if (field.ordinal === 1) {
      const numeroPresentacion = this.numeroPresentacionEvaluacion(resumen.evaId);
      return numeroPresentacion === null ? '—' : String(numeroPresentacion);
    }
    if (field.ordinal === 2) return this.riesgoMaestroMatriz()?.rieCodigo || '—';
    if (field.ordinal === 8) return this.riesgoMaestroMatriz()?.rieNombre || '—';
    if (field.ordinal === 9) return this.riesgoMaestroMatriz()?.rieDescripcion || '—';
    if (field.ordinal === 12) return evaluacion.evaVri === null || evaluacion.evaVri === undefined ? '—' : String(evaluacion.evaVri);
    if (field.ordinal === 13) {
      const calculados = this.parsearRespuestas(evaluacion.evaDataCalcJson ?? '{}');
      const nivel = calculados['nivel_riesgo_inherente'];
      return nivel === null || nivel === undefined || nivel === '' ? '—' : Array.isArray(nivel) ? nivel.join(', ') : String(nivel);
    }

    if (!field.key) return '—';
    const value = this.respuestas()[field.key];
    if (value === null || value === undefined || value === '') {
      return 'Sin dato';
    }
    return Array.isArray(value) ? value.join(', ') : String(value);
  }

  private valorProyeccionMatrizCompleta(ordinal: number): string | null {
    const field = this.proyeccionMatrizCompleta()?.fields.find(item => item.fieldNumber === ordinal);
    if (!field) return null;
    if (field.value === null || field.value === undefined) {
      switch (field.state) {
        case 'NOT_APPLICABLE':
        case 'ABSENT_CONTROL_PROJECTED':
          return 'No aplica';
        case 'LEGIT_BLANK':
        case 'LEGIT_FORMULA_BLANK':
        case 'OPERATIONAL_PENDING':
        case 'CALCULATION_PENDING':
          return 'Sin dato';
        default:
          return 'Estado no reconocido';
      }
    }
    if (Array.isArray(field.value)) {
      return field.value.map((item, index) => {
        if (item === null || item === undefined || item === '') return `${index + 1}. [Sin dato]`;
        if (typeof item !== 'object') return `${index + 1}. ${String(item)}`;
        const record = item as Record<string, unknown>;
        const ordinalIndex = record['planOrdinalIndex'] ?? record['ordinalIndex'] ?? record['activityOrdinalIndex'];
        const value = record['value'] ?? record['displayValue'] ?? record['description'] ?? record['text'];
        const display = value === null || value === undefined || value === '' ? '[Sin dato]' : String(value);
        return `${ordinalIndex ?? index + 1}. ${display}`;
      }).join('\n');
    }
    if (typeof field.value === 'object') {
      const record = field.value as Record<string, unknown>;
      const value = record['value'] ?? record['displayValue'] ?? record['text'];
      return value === null || value === undefined ? JSON.stringify(field.value) ?? '—' : String(value);
    }
    return String(field.value);
  }

  readonly camposMatrizCompleta = MATRIX_BLOCK_1_FIELDS;
  readonly camposControlesMatriz = MATRIX_BLOCK_2_FIELDS;
  readonly camposResidualMatriz = MATRIX_BLOCK_3_FIELDS;
  readonly camposBloque4Matriz = MATRIX_BLOCK_4_FIELDS;
  readonly camposMatrizPlan = MATRIX_BLOCK_4_FIELDS.slice(2);
  readonly camposBloque5Matriz = MATRIX_BLOCK_5_FIELDS;
  readonly camposBloque6Matriz = MATRIX_BLOCK_6_FIELDS;
  readonly bloquesMatrizPendientes = [] as const;
  readonly etiquetaBloquePendiente = (block: 4 | 5 | 6): string => MATRIX_BLOCK_TITLES[block];
  camposMetadataBloque(block: 4 | 5 | 6): readonly MatrixFieldContract[] {
    return MATRIX_FIELDS.filter(field => field.block === block);
  }

  valorCampoResidual(field: MatrixFieldContract): string {
    const canonical = this.valorProyeccionMatrizCompleta(field.ordinal);
    if (canonical !== null) return canonical;
    const evaluacion = this.evaluacionSeleccionada();
    const resumen = this.evaluacionResumenSeleccionada();
    if (!evaluacion || !resumen) return '—';

    const calculados = this.parsearRespuestas(evaluacion.evaDataCalcJson ?? '{}');

    if (field.ordinal === 34) {
      const desc = calculados['riesgo_residual_descripcion'];
      if (desc !== null && desc !== undefined && desc !== '') return String(desc);
      return this.riesgoMaestroMatriz()?.rieNombre || this.riesgoMaestroMatriz()?.rieDescripcion || '—';
    }

    if (field.ordinal === 35) {
      const fRes = calculados['frecuencia_residual'];
      if (fRes !== null && fRes !== undefined && fRes !== '') return String(fRes);
      if (resumen.versionNumero === 1 && this.respuestas()['frecuencia_residual']) {
        return String(this.respuestas()['frecuencia_residual']);
      }
      return '—';
    }

    if (field.ordinal === 36) {
      const iRes = calculados['impacto_residual'];
      if (iRes !== null && iRes !== undefined && iRes !== '') return String(iRes);
      if (resumen.versionNumero === 1 && this.respuestas()['impacto_residual']) {
        return String(this.respuestas()['impacto_residual']);
      }
      return '—';
    }

    if (field.ordinal === 37) {
      const vrr = evaluacion.evaVrr ?? calculados['valor_riesgo_residual'] ?? calculados['vrr'];
      if (vrr !== null && vrr !== undefined && vrr !== '') return String(vrr);
      return '—';
    }

    if (field.ordinal === 38) {
      const vrr = evaluacion.evaVrr ?? calculados['valor_riesgo_residual'] ?? calculados['vrr'];
      return canonicalRiskLevelLabel(vrr) ?? '—';
    }

    if (field.ordinal === 39) {
      const respuestas = this.respuestas();
      const respuesta = respuestas['respuesta_riesgo'] || respuestas['respuestaRiesgo'];
      return canonicalRiskResponseLabel(respuesta) ?? '—';
    }

    return '—';
  }

  etiquetaNivelRiesgo(value: unknown): string {
    return canonicalRiskLevelLabel(value) ?? '—';
  }

  etiquetaRespuestaRiesgo(value: unknown): string {
    return canonicalRiskResponseLabel(value) ?? '—';
  }

  valorCampoBloque4(field: MatrixFieldContract, plan: PlanMitigacionDto): string {
    switch (field.ordinal) {
      case 43: return Number.isInteger(plan.cantidadActividades) && (plan.cantidadActividades ?? -1) >= 0 ? String(plan.cantidadActividades) : '—';
      case 44: return this.textoMatriz(plan.plaMonitoreoSeguimiento);
      case 45: return this.textoMatriz(plan.plaResponsables);
      case 46: return this.fechaMatrizPublica(plan.plaFechaInicio);
      case 47: return this.fechaMatrizPublica(plan.plaFechaFin);
      case 48: return this.textoMatriz(plan.plaRecursos);
      case 49: return this.presupuestoMatriz(plan.plaPresupuesto);
      default: return '—';
    }
  }

  valorCampoBloque5(field: MatrixFieldContract): string {
    const canonical = this.valorProyeccionMatrizCompleta(field.ordinal);
    if (canonical !== null) return canonical;
    const evaluacion = this.evaluacionSeleccionada();
    if (!evaluacion || !field.key) return '—';
    const value = this.parsearRespuestas(evaluacion.evaDataCalcJson ?? '{}')[field.key];
    if (value === null || value === undefined || value === '') return '—';
    if (Array.isArray(value)) return value.join(', ');
    if (typeof value === 'object') return '—';
    return String(value);
  }

  estadoVerificacionBloque5(field: MatrixFieldContract): 'PASS' | 'Esperado' | 'Revisar' | 'Sin dato' {
    if (![64, 66, 67, 68, 69].includes(field.ordinal)) return 'Sin dato';
    const evaluacion = this.evaluacionSeleccionada();
    if (!evaluacion || !field.key) return 'Sin dato';
    const value = this.parsearRespuestas(evaluacion.evaDataCalcJson ?? '{}')[field.key];
    const numero = this.numeroCalculadoEstricto(value);
    if (numero === null) return 'Sin dato';
    if (field.ordinal === 64 || field.ordinal === 66) return numero === 0 ? 'PASS' : 'Revisar';
    return numero >= 0 ? 'Esperado' : 'Revisar';
  }

  private numeroCalculadoEstricto(value: unknown): number | null {
    if (typeof value === 'number') return Number.isFinite(value) ? value : null;
    if (typeof value !== 'string' || value.trim() === '') return null;
    const parsed = Number(value.trim());
    return Number.isFinite(parsed) ? parsed : null;
  }

  private textoMatriz(value: string | null | undefined): string {
    return typeof value === 'string' && value.trim() ? normalizarMojibakeVisibleUtf8(value.trim()) : '—';
  }

  fechaMatrizPublica(value: string | null | undefined): string {
    if (!value) return '—';
    const date = new Date(value);
    return Number.isNaN(date.getTime()) ? '—' : new Intl.DateTimeFormat('es-HN', { day: '2-digit', month: '2-digit', year: 'numeric', timeZone: 'UTC' }).format(date);
  }

  private presupuestoMatriz(value: number | null | undefined): string {
    if (typeof value !== 'number' || !Number.isFinite(value) || value < 0) return '—';
    return new Intl.NumberFormat('es-HN', { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(value);
  }

  reintentarCargaBloque4Matriz(): void {
    const evaluacionId = this.evaluacionSeleccionada()?.evaId;
    if (evaluacionId) this.cargarBloque4Matriz(evaluacionId, this.secuenciaCargaMatrizCompleta);
  }

  private cargarBloque4Matriz(evaluacionId: number, solicitudId: number): void {
    this.bloque4MatrizEnCarga.set(true);
    this.bloque4MatrizError.set(null);
    this.service.obtenerMitigacionBloque4(evaluacionId).subscribe({
      next: response => {
        if (solicitudId !== this.secuenciaCargaMatrizCompleta) return;
        if (!response || !Array.isArray(response.planes) || !Number.isInteger(response.cantidadAcciones)
          || response.cantidadAcciones < 0 || response.cantidadAcciones !== response.planes.length
          || response.planes.some(plan => !Number.isSafeInteger(plan.plaId) || !Array.isArray(plan.actividades)
            || !Number.isInteger(plan.cantidadActividades) || plan.cantidadActividades !== plan.actividades.length)) {
          this.bloque4Matriz.set(null);
          this.bloque4MatrizError.set('La proyección de planes y actividades no tiene un contrato válido.');
        } else {
          this.bloque4Matriz.set(response);
        }
        this.bloque4MatrizEnCarga.set(false);
      },
      error: error => {
        if (solicitudId !== this.secuenciaCargaMatrizCompleta) return;
        this.bloque4Matriz.set(null);
        this.bloque4MatrizError.set(this.obtenerMensajeErrorLectura(error, 'No se pudo cargar el plan de mitigación de la evaluación.'));
        this.bloque4MatrizEnCarga.set(false);
      }
    });
  }

  controlesPorTipo(tipo: ControlRiesgoDto['conTipo']): ControlRiesgoDto[] {
    return this.controlesMatrizCompleta().filter(control => control.conTipo === tipo);
  }

  valorCampoControl(field: MatrixFieldContract): string {
    const canonical = this.valorProyeccionMatrizCompleta(field.ordinal);
    if (canonical !== null) {
      if (![23, 27, 31, 33].includes(field.ordinal)) return canonical;
      if (canonical === 'Sin dato' || canonical === 'No aplica' || canonical === 'Estado no reconocido') return canonical;
      const versionNumero = this.evaluacionResumenSeleccionada()?.versionNumero;
      const esV1 = versionNumero === 1;
      return esV1 ? this.formatearPorcentajeHistorico0A100(canonical) : this.formatearPorcentajeRuntime0A1(canonical);
    }
    const key = field.key;
    if (!key) return '—';
    const versionNumero = this.evaluacionResumenSeleccionada()?.versionNumero;
    const esV1 = versionNumero === 1;
    if ([21, 22, 25, 26, 29, 30].includes(field.ordinal)
      && (esV1 || !this.catalogoEscalaEfectividad())) return 'Sin dato';
    if (field.ordinal === 23 || field.ordinal === 27 || field.ordinal === 31) {
      const source = esV1
        ? this.respuestas()[field.ordinal === 23 ? 'controles_preventivo' : field.ordinal === 27 ? 'controles_detectivo' : 'controles_correctivo']
        : this.parsearRespuestas(this.evaluacionSeleccionada()?.evaDataCalcJson ?? '{}')[key];
      return esV1 ? this.formatearPorcentajeHistorico0A100(source) : this.formatearPorcentajeRuntime0A1(source);
    }
    if (field.ordinal === 21 || field.ordinal === 25 || field.ordinal === 29) {
      const scale = this.respuestas()[key];
      const selected = this.catalogoEscalaEfectividad()?.elementos.find(option => option.codigo === String(scale));
      return selected?.valor ?? '—';
    }
    const calculos = this.parsearRespuestas(this.evaluacionSeleccionada()?.evaDataCalcJson ?? '{}');
    const value = esV1 && field.ordinal === 33 ? calculos['etp'] : calculos[key];
    if (value === null || value === undefined || value === '') return '—';
    if (field.ordinal === 33) return esV1 ? this.formatearPorcentajeHistorico0A100(value) : this.formatearPorcentajeRuntime0A1(value);
    return String(value);
  }

  catalogoEscalaEfectividad(): MetodologiaFormulario['catalogos'][number] | undefined {
    return (this.metodologiaMatrizCompleta()?.catalogos ?? []).find(catalog => catalog.codigo === 'CAT_EFECTIVIDAD_ESCALA');
  }

  formatearPorcentajeHistorico0A100(value: unknown): string {
    const percentagePoints = typeof value === 'number' ? value : typeof value === 'string' && value.trim() !== '' ? Number(value) : Number.NaN;
    if (!Number.isFinite(percentagePoints) || percentagePoints < 0 || percentagePoints > 100) return '—';
    return `${new Intl.NumberFormat('es-HN', { maximumFractionDigits: 2 }).format(percentagePoints)}%`;
  }

  formatearPorcentajeRuntime0A1(value: unknown): string {
    const ratio = typeof value === 'number' ? value : typeof value === 'string' && value.trim() !== '' ? Number(value) : Number.NaN;
    if (!Number.isFinite(ratio) || ratio < 0 || ratio > 1) return '—';
    return `${new Intl.NumberFormat('es-HN', { maximumFractionDigits: 2 }).format(ratio * 100)}%`;
  }

  reintentarCargaMetodologiaMatriz(): void {
    const detalle = this.evaluacionSeleccionada();
    if (detalle) this.cargarMetodologiaMatrizCompleta(detalle.evaVersionId, this.secuenciaCargaMatrizCompleta);
  }

  private cargarMetodologiaMatrizCompleta(versionId: number, solicitudId: number): void {
    this.metodologiaMatrizCompletaEnCarga.set(true);
    this.metodologiaMatrizCompletaError.set(null);
    this.service.metodologiaPorVersion(versionId).subscribe({
      next: metodologia => {
        if (solicitudId !== this.secuenciaCargaMatrizCompleta
          || (metodologia.versionFormularioId !== undefined && metodologia.versionFormularioId !== versionId)) return;
        this.metodologiaMatrizCompleta.set(metodologia);
        this.metodologiaMatrizCompletaEnCarga.set(false);
      },
      error: error => {
        if (solicitudId !== this.secuenciaCargaMatrizCompleta) return;
        this.metodologiaMatrizCompleta.set(null);
        this.metodologiaMatrizCompletaError.set(this.obtenerMensajeErrorLectura(error, `No se pudo cargar la metodología de la versión ${versionId}.`));
        this.metodologiaMatrizCompletaEnCarga.set(false);
      }
    });
  }

  reintentarCargaControlesMatriz(): void {
    const evaluacionId = this.evaluacionSeleccionada()?.evaId;
    if (evaluacionId) this.cargarControlesMatrizCompleta(evaluacionId, this.secuenciaCargaMatrizCompleta);
  }

  private cargarControlesMatrizCompleta(evaluacionId: number, solicitudId: number): void {
    this.controlesMatrizEnCarga.set(true);
    this.controlesMatrizError.set(null);
    this.service.listarControles(evaluacionId).subscribe({
      next: controles => {
        if (solicitudId !== this.secuenciaCargaMatrizCompleta) return;
        this.controlesMatrizCompleta.set(controles);
        this.controlesMatrizEnCarga.set(false);
      },
      error: error => {
        if (solicitudId !== this.secuenciaCargaMatrizCompleta) return;
        this.controlesMatrizCompleta.set([]);
        this.controlesMatrizError.set(this.obtenerMensajeErrorLectura(error, 'No se pudieron cargar los controles asociados.'));
        this.controlesMatrizEnCarga.set(false);
      }
    });
  }

  abrirMatrizCompletaDesdeConsolidado(fila: RiesgoReporteFila): void {
    this.abrirMatrizCompleta({
      evaId: fila.evaluacionId,
      evaRiesgoId: fila.riesgoId,
      riesgoCodigo: fila.codigoRiesgo,
      riesgoNombre: fila.codigoRiesgo,
      evaVersionId: fila.versionFormularioId,
      versionCodigo: `VERSION_${fila.versionFormularioId}`,
      versionNumero: 0,
      estado: fila.estadoEvaluacion,
      evaEstado: fila.estadoEvaluacion,
      vri: fila.vri,
      vrr: fila.vrr,
      nivelResidual: fila.nivelResidual,
      fechaEval: fila.fechaEvaluacion ?? '',
      evaFechaEval: fila.fechaEvaluacion ?? undefined
    });
  }

  readonly totalCamposContratoMatriz = MATRIX_FIELDS.length;

  private numeroPresentacionEvaluacion(evaluacionId: number): number | null {
    const indexListado = this.evaluaciones().findIndex(item => item.evaId === evaluacionId);
    if (indexListado >= 0) return (this.pagina() - 1) * this.registrosPorPagina() + indexListado + 1;

    const indexConsolidado = this.consolidado().findIndex(item => item.evaluacionId === evaluacionId);
    return indexConsolidado >= 0
      ? (this.paginaConsolidado() - 1) * this.registrosPorPaginaConsolidado() + indexConsolidado + 1
      : null;
  }

  cerrarModalVer(): void {
    this.modalVerAbierto.set(false);
    this.globalState.limpiarError();
  }

  editarEvaluacion(evaluacion: EvaluacionRiesgoResumenDto): void {
    if ((evaluacion.estado || '').toUpperCase() !== 'BORRADOR') {
      this.mostrarError('Solo se permite editar evaluaciones en estado Borrador.');
      return;
    }

    this.limpiarAlertasEditor('editEvaluation');
    this.cargando.set(true);
    this.evaluacionResumenSeleccionada.set(evaluacion);

    this.service.obtenerEvaluacion(evaluacion.evaId).subscribe({
      next: detalle => {
        this.evaluacionSeleccionada.set(detalle);
        this.riesgoId.set(detalle.evaRiesgoId);
        this.respuestas.set(this.parsearRespuestas(detalle.evaDataJson));
        this.cambiosSinGuardar.set(false);
        this.bloqueEvaluacionActual.set(1);
        this.cargandoProyeccionEvaluacion.set(true);
        const projectionRequest = this.solicitarProyeccion(detalle.evaId);
        if (!projectionRequest) this.cargandoProyeccionEvaluacion.set(false);
        projectionRequest?.subscribe({
          next: projection => {
            this.proyeccionEvaluacion.set(projection);
            this.cargandoProyeccionEvaluacion.set(false);
          },
          error: error => {
            this.cargandoProyeccionEvaluacion.set(false);
            this.mostrarError(this.obtenerMensajeError(error, 'No se pudo cargar la proyección institucional de la evaluación.'));
          }
        });

        this.service.metodologiaPorVersion(detalle.evaVersionId).subscribe({
          next: met => {
            this.metodologiaHistorica.set(met);
            this.cargando.set(false);
            this.modalEditarAbierto.set(true);
          },
          error: error => {
            this.cargando.set(false);
            this.mostrarError(this.obtenerMensajeError(error, `No se pudo recuperar la metodología histórica para la versión ID ${detalle.evaVersionId}.`));
          }
        });
      },
      error: error => {
        this.cargando.set(false);
        this.mostrarError(this.obtenerMensajeError(error, 'No se pudo obtener el detalle fresco para editar la evaluación.'));
      }
    });
  }

  cerrarModalEditar(): void {
    if (this.cambiosSinGuardar()) {
      void this.confirmarDescarteCambios().then(confirmado => {
        if (confirmado) this.completarCierreEditar();
      });
      return;
    }
    this.completarCierreEditar();
  }

  private completarCierreEditar(): void {
    this.modalEditarAbierto.set(false);
    this.limpiarAlertasEditor('editEvaluation');
    this.cambiosSinGuardar.set(false);
  }

  private confirmarDescarteCambios(): Promise<boolean> {
    return this.confirmation.confirm({
      title: 'Hay cambios sin guardar',
      message: '¿Desea descartar los cambios y cerrar la evaluación?',
      confirmLabel: 'Descartar cambios',
      cancelLabel: 'Continuar editando',
      confirmIcon: 'reject',
      confirmVariant: 'danger',
      icon: 'warning'
    });
  }

  abrirModalSeguimiento(evaluacion: EvaluacionRiesgoResumenDto): void {
    this.limpiarAlertas();
    this.errorSeguimiento.set(null);
    this.errorTransiciones.set(null);
    this.errorFlujos.set(null);
    this.evaluacionResumenSeleccionada.set(evaluacion);
    this.motivoTransicion = '';
    this.transicionesPermitidas.set([]);
    this.flujos.set([]);
    this.nuevoEstado = '';
    this.cargarTransiciones(evaluacion.evaId);
    this.cargarFlujos(evaluacion.evaId);
    this.modalSeguimientoAbierto.set(true);
  }

  cerrarModalSeguimiento(): void {
    this.modalSeguimientoAbierto.set(false);
    this.globalState.limpiarError();
  }

  obtenerTransicionesValidas(_estadoActual?: string): string[] {
    return this.transicionesPermitidas();
  }

  etiquetaEstadoEvaluacion(estado: string | null | undefined): string {
    const codigo = (estado ?? '').trim().toUpperCase();
    const catalogado = this.catalogoEstadosEvaluacion().find(item => item.codigo === codigo)?.etiqueta;
    if (catalogado) return catalogado;
    const etiquetas: Record<string, string> = {
      BORRADOR: 'Borrador',
      EN_REVISION: 'En revisión',
      OBSERVADA: 'Observada',
      APROBADA: 'Aprobada',
      RECHAZADA: 'Rechazada',
      CERRADA: 'Cerrada'
    };
    return etiquetas[codigo] ?? 'Estado no reconocido';
  }

  cargarTransiciones(evaluacionId: number): void {
    this.estadoTransiciones.set('LOADING');
    this.errorTransiciones.set(null);
    this.transicionesPermitidas.set([]);
    this.nuevoEstado = '';
    this.service.obtenerTransicionesEvaluacion(evaluacionId).subscribe({
      next: (transiciones: TransicionesEvaluacionDto) => {
        if (this.evaluacionResumenSeleccionada()?.evaId !== evaluacionId) return;
        if (!Array.isArray(transiciones?.destinosPermitidos) || !transiciones.estadoActual) {
          this.errorTransiciones.set('La respuesta de transiciones permitidas no tiene el formato esperado.');
          this.estadoTransiciones.set('ERROR');
          return;
        }

        const destinos = [...transiciones.destinosPermitidos];
        this.transicionesPermitidas.set(destinos);
        this.estadoTransiciones.set(destinos.length > 0 ? 'SUCCESS_WITH_DATA' : 'SUCCESS_EMPTY');
        this.nuevoEstado = '';
        const actual = this.evaluacionResumenSeleccionada();
        if (actual?.evaId === evaluacionId) {
          this.evaluacionResumenSeleccionada.set({ ...actual, estado: transiciones.estadoActual });
        }
      },
      error: error => {
        if (this.evaluacionResumenSeleccionada()?.evaId !== evaluacionId) return;
        this.transicionesPermitidas.set([]);
        this.nuevoEstado = '';
        this.errorTransiciones.set(this.obtenerMensajeError(error, 'No se pudieron consultar las transiciones permitidas.'));
        this.estadoTransiciones.set('ERROR');
      }
    });
  }

  guardarEvaluacion(): void {
    const esEdicion = this.modalEditarAbierto();
    const actual = this.evaluacionSeleccionada();

    const versionIdFinal = esEdicion && actual
      ? actual.evaVersionId
      : this.versionVigente()?.verId ?? 0;

    if (versionIdFinal <= 0 || !this.puedeGuardar()) {
      this.mostrarError('Complete el riesgo y todos los campos obligatorios antes de guardar.');
      return;
    }

    this.guardando.set(true);
    this.limpiarAlertas();

    const dto: EvaluacionRiesgoDto = {
      evaId: actual?.evaId ?? 0,
      evaRiesgoId: this.riesgoId(),
      evaVersionId: versionIdFinal,
      evaEstado: actual?.evaEstado ?? 'BORRADOR',
      evaDataJson: JSON.stringify(this.construirRespuestasFormularioPersistibles()),
      evaDataCalcJson: actual?.evaDataCalcJson ?? null,
      evaVri: actual?.evaVri ?? null,
      evaVrr: actual?.evaVrr ?? null,
      evaFechaEval: actual?.evaFechaEval ?? new Date().toISOString(),
      evaUsrEval: actual?.evaUsrEval ?? 0,
      evaVersionRow: actual?.evaVersionRow ?? 1,
      evaActivo: true
    };

    const solicitud = esEdicion && actual
      ? this.service.actualizarEvaluacion(actual.evaId, dto)
      : this.service.crearEvaluacion(dto);

    solicitud.subscribe({
      next: () => {
        if (esEdicion && actual) {
          this.service.obtenerEvaluacion(actual.evaId).subscribe({
            next: detallePersistido => {
              this.guardando.set(false);

              if (!sonJsonSemanticamenteEquivalentes(dto.evaDataJson, detallePersistido.evaDataJson)) {
                this.mostrarError('El servidor respondió al guardado, pero la evaluación recuperada no coincide con los cambios enviados. No se cerró el modal para evitar ocultar la inconsistencia.');
                return;
              }

              this.evaluacionSeleccionada.set(detallePersistido);
              this.respuestas.set(this.parsearRespuestas(detallePersistido.evaDataJson));
              this.mostrarMensaje(`Cambios de la evaluación #${actual.evaId} guardados y verificados correctamente.`);
              this.cargarEvaluaciones();
            },
            error: errorVerificacion => {
              this.guardando.set(false);
              this.mostrarError(this.obtenerMensajeError(
                errorVerificacion,
                'Los cambios fueron enviados, pero no se pudo verificar la persistencia recuperando nuevamente la evaluación.'
              ));
            }
          });
          return;
        }

        this.guardando.set(false);
        this.mostrarMensaje('Evaluación creada correctamente.');
        this.modalNuevaEvaluacionAbierto.set(false);
        this.cargarEvaluaciones();
      },
      error: error => {
        this.guardando.set(false);
        if (error?.status === 409) {
          this.mostrarError('La evaluación fue modificada por otro usuario. Recargue los datos antes de continuar.');
        } else {
          this.mostrarError(this.obtenerMensajeError(error, 'No se pudo guardar la evaluación. Verifique los datos e intente nuevamente.'));
        }
      }
    });
  }

  transicionarEvaluacionModal(): void {
    const resumen = this.evaluacionResumenSeleccionada();
    if (!resumen) return;

    this.errorSeguimiento.set(null);
    const destino = this.nuevoEstado.trim().toUpperCase();
    const destinoCatalogado = this.catalogoEstadosEvaluacion().some(estado => estado.codigo === destino)
      || ['BORRADOR', 'EN_REVISION', 'OBSERVADA', 'APROBADA', 'RECHAZADA', 'CERRADA'].includes(destino);
    if (!destino || !destinoCatalogado || !this.transicionesPermitidas().includes(destino)) {
      this.errorSeguimiento.set('Seleccione un estado de destino válido.');
      return;
    }

    this.guardando.set(true);
    this.service.transicionarEvaluacion(resumen.evaId, destino, this.motivoTransicion).subscribe({
      next: () => {
        this.guardando.set(false);
        this.globalState.limpiarError();
        this.mostrarMensaje(`Estado de la evaluación #${resumen.evaId} actualizado a ${this.etiquetaEstadoEvaluacion(this.nuevoEstado)} correctamente.`);
        this.motivoTransicion = '';
        this.cargarEvaluaciones();
        this.cargarFlujos(resumen.evaId);
        this.cargarTransiciones(resumen.evaId);
      },
      error: error => {
        this.guardando.set(false);
        this.errorSeguimiento.set(this.obtenerMensajeError(error, 'No se pudo realizar la transición de estado.'));
      }
    });
  }

  cargarFlujos(evaluacionId: number): void {
    this.estadoFlujos.set('LOADING');
    this.errorFlujos.set(null);
    this.flujos.set([]);
    this.service.obtenerFlujos(evaluacionId).subscribe({
      next: flujos => {
        if (this.evaluacionResumenSeleccionada()?.evaId !== evaluacionId) return;
        if (!Array.isArray(flujos)) {
          this.errorFlujos.set('La respuesta del historial de estados no tiene el formato esperado.');
          this.estadoFlujos.set('ERROR');
          return;
        }

        this.flujos.set(flujos);
        this.estadoFlujos.set(flujos.length > 0 ? 'SUCCESS_WITH_DATA' : 'SUCCESS_EMPTY');
      },
      error: error => {
        if (this.evaluacionResumenSeleccionada()?.evaId !== evaluacionId) return;
        this.flujos.set([]);
        this.errorFlujos.set(this.obtenerMensajeError(error, 'No se pudo cargar el historial de estados.'));
        this.estadoFlujos.set('ERROR');
      }
    });
  }

  seleccionarArchivo(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.archivoEvidencia = input.files?.item(0) ?? null;
  }

  cargarYVincularEvidencia(evaluacion: EvaluacionRiesgoDto): void {
    if (!this.archivoEvidencia) {
      this.mostrarError('Seleccione un archivo de evidencia antes de continuar.');
      return;
    }

    this.guardando.set(true);
    this.limpiarAlertas();
    this.service.cargarEvidencia(this.archivoEvidencia).subscribe({
      next: evidencia => {
        this.service.vincularEvidencia({
          entidadId: evaluacion.evaId,
          evidenciaId: evidencia.eviId,
          tipoEntidad: 'evaluacion'
        }).subscribe({
          next: () => {
            this.archivoEvidencia = null;
            this.guardando.set(false);
            this.globalState.limpiarError();
            this.mostrarMensaje('Evidencia cargada y vinculada correctamente.');
          },
          error: error => {
            this.service.eliminarEvidenciaHuerfana(evidencia.eviId).subscribe();
            this.guardando.set(false);
            this.mostrarError(this.obtenerMensajeError(error, 'No se pudo vincular la evidencia al expediente.'));
          }
        });
      },
      error: error => {
        this.guardando.set(false);
        this.mostrarError(this.obtenerMensajeError(error, 'No se pudo cargar el archivo de evidencia.'));
      }
    });
  }

  clonarVersion(version: VersionFormularioDto): void {
    if (!this.esAdministrador()) return;
    this.guardando.set(true);
    this.service.clonarVersionFormulario(version.verId).subscribe({
      next: () => {
        this.guardando.set(false);
        this.globalState.limpiarError();
        this.mostrarMensaje('Versión clonada como borrador exitosamente.');
        this.cargarVersiones();
      },
      error: error => {
        this.guardando.set(false);
        this.mostrarError(this.obtenerMensajeError(error, 'No se pudo clonar la versión del formulario.'));
      }
    });
  }

  abrirDefinicion(version: VersionFormularioDto, soloLectura = false): void {
    this.cargando.set(true);
    this.service.obtenerVersionFormulario(version.verId).subscribe({
      next: versionAutoritativa => {
        this.cargando.set(false);
        this.versionEditando.set(versionAutoritativa);
        this.ocultarDetalleComoContexto();
        this.soloLecturaDefinicion.set(
          soloLectura ||
          !this.esAdministrador() ||
          versionAutoritativa.verVigente ||
          versionAutoritativa.verEstado !== 'DRAFT'
        );
        this.definicionTecnica = this.formatearDefinicion(versionAutoritativa.verJson);
        this.cargarFormulasCentrales();
      },
      error: error => {
        this.cargando.set(false);
        this.versionEditando.set(null);
        this.mostrarError(this.obtenerMensajeError(error, 'No se pudo cargar la versión autoritativa del formulario.'));
      }
    });
  }

  private cargarFormulasCentrales(): void {
    if (!this.calculoConfig) return;
    this.calculoConfig.listarFormulas(false).subscribe({
      next: formulas => {
        if (formulas.length === 0) { this.formulasCentralDisponibles.set([]); return; }
        forkJoin(formulas.map(formula => this.calculoConfig!.listarFormulaVersiones(formula.id))).subscribe({
          next: versiones => this.formulasCentralDisponibles.set(versiones.flatMap((lista, index) => lista
            .filter(versionItem => versionItem.estado !== 'ARCHIVED' && versionItem.estado !== 'RETIRED')
            .map(versionItem => ({
              formulaId: formulas[index].id,
              formulaVersionId: versionItem.id,
              codigo: formulas[index].codigo,
              nombre: formulas[index].nombre,
              version: versionItem.version,
              estado: versionItem.estado,
              tipoResultado: versionItem.tipoResultado,
              hash: versionItem.hash,
              expresion: versionItem.expresion,
              descripcion: formulas[index].descripcion
            })))),
          error: () => this.formulasCentralDisponibles.set([])
        });
      },
      error: () => this.formulasCentralDisponibles.set([])
    });
  }

  guardarDefinicion(): void {
    if (!this.esAdministrador() || this.soloLecturaDefinicion()) return;
    const version = this.versionEditando();
    if (!version) return;

    const jsonEnviado = this.definicionTecnica;
    try {
      JSON.parse(jsonEnviado);
    } catch (error) {
      const detalle = error instanceof SyntaxError ? error.message : '';
      const sufijo = detalle ? `: ${detalle}` : '.';
      this.mostrarError(`La definición JSON no es válida${sufijo}`);
      return;
    }

    this.guardando.set(true);
    this.operacionBuilderEnCurso.set('guardar');
    this.service.actualizarBorradorFormulario(version.verId, jsonEnviado).subscribe({
      next: () => {
        this.sincronizarUsosFormula(version.verId, jsonEnviado).subscribe({
          next: () => this.verificarDefinicionPersistida(version, jsonEnviado),
          error: error => {
            this.guardando.set(false);
            this.operacionBuilderEnCurso.set(null);
            this.mostrarError(this.obtenerMensajeErrorPublicacion(error, 'No se pudieron actualizar los usos de fórmula del borrador; la versión permanece en borrador.'));
          }
        });
      },
      error: error => {
        this.guardando.set(false);
        this.operacionBuilderEnCurso.set(null);
        this.mostrarError(this.obtenerMensajeErrorPublicacion(error, 'No se pudo actualizar la definición del formulario; la versión permanece en borrador.'));
      }
    });
  }

  private verificarDefinicionPersistida(version: VersionFormularioDto, jsonEnviado: string): void {
    this.service.obtenerVersionFormulario(version.verId).subscribe({
      next: versionPersistida => {
        this.guardando.set(false);
        this.operacionBuilderEnCurso.set(null);
        const sonEquivalentes = sonJsonSemanticamenteEquivalentes(jsonEnviado, versionPersistida.verJson);

        if (!sonEquivalentes) {
          this.mostrarError('La persistencia recuperada del servidor no coincide semánticamente con la definición enviada.');
          return;
        }

        this.versionEditando.set(versionPersistida);
        this.soloLecturaDefinicion.set(
          !this.esAdministrador() ||
          versionPersistida.verVigente ||
          versionPersistida.verEstado !== 'DRAFT'
        );
        this.globalState.limpiarError();
        this.mostrarMensaje('Definición guardada correctamente y verificada.');
        this.cargarVersiones();
      },
      error: getError => {
        this.guardando.set(false);
        this.operacionBuilderEnCurso.set(null);
        this.mostrarError(this.obtenerMensajeError(getError, 'No se pudo verificar la persistencia de la versión del formulario tras guardar.'));
      }
    });
  }

  private sincronizarUsosFormula(versionFormularioId: number, json: string): Observable<unknown> {
    if (!this.calculoConfig || typeof this.calculoConfig.reemplazarFormulaUsos !== 'function') return of(null);
    return this.calculoConfig.reemplazarFormulaUsos(versionFormularioId, this.extraerUsosFormula(versionFormularioId, json));
  }

  private extraerUsosFormula(versionFormularioId: number, json: string): CrearFormulaUsoDto[] {
    const modelo = normalizarJsonABuilderModel(json);
    return modelo.secciones.flatMap(seccion => seccion.campos)
      .filter((campo): campo is typeof campo & { formulaVersionId: number } => campo.tipo === 'formula' && Number.isInteger(campo.formulaVersionId) && (campo.formulaVersionId ?? 0) > 0)
      .map(campo => ({
        versionFormularioId,
        campoClave: campo.clave.trim(),
        formulaVersionId: campo.formulaVersionId
      }));
  }

  publicarVersionDesdeBuilder(version: VersionFormularioDto, definicionActual: string): void {
    if (!this.esAdministrador() || !definicionActual.trim()) return;
    void this.confirmation.confirm({
      title: 'Publicar versión oficial?',
      message: `Se validará, guardará y publicará la definición visible de la versión ID #${version.verId}.`,
      icon: 'question',
      confirmLabel: 'Sí, publicar versión',
      cancelLabel: 'Cancelar',
      confirmIcon: 'publish',
      confirmVariant: 'primary'
    }).then(confirmed => {
      if (!confirmed) return;
      this.guardando.set(true);
      this.operacionBuilderEnCurso.set('publicar');
      this.limpiarAlertas();
      const publicar = () => this.ejecutarPublicacionDesdeBuilder(version);
      if (sonJsonSemanticamenteEquivalentes(version.verJson, definicionActual)) {
        publicar();
        return;
      }
      this.service.actualizarBorradorFormulario(version.verId, definicionActual).subscribe({
        next: () => this.sincronizarUsosFormula(version.verId, definicionActual).subscribe({
          next: () => this.service.obtenerVersionFormulario(version.verId).subscribe({
            next: persistida => {
              if (!sonJsonSemanticamenteEquivalentes(definicionActual, persistida.verJson)) {
                this.finalizarOperacionBuilder('La definición visible no coincide con la definición persistida. No se publicó la versión.');
                return;
              }
              publicar();
            },
            error: error => this.finalizarOperacionBuilder(this.obtenerMensajeErrorPublicacion(error, 'No se pudo verificar la definición antes de publicarla.'))
          }),
          error: error => this.finalizarOperacionBuilder(this.obtenerMensajeErrorPublicacion(error, 'La definición se guardó, pero no se pudieron sincronizar las dependencias de fórmula. La versión no se publicó; revise la configuración y vuelva a intentar.'))
        }),
        error: error => this.finalizarOperacionBuilder(this.obtenerMensajeErrorPublicacion(error, 'No se pudo guardar la definición actual; la versión no se publicó.'))
      });
    });
  }

  private ejecutarPublicacionDesdeBuilder(version: VersionFormularioDto): void {
    this.service.publicarVersionFormulario(version.verId).subscribe({
      next: () => this.service.obtenerVersionFormulario(version.verId).subscribe({
        next: fresca => {
          this.guardando.set(false);
          this.operacionBuilderEnCurso.set(null);
          this.versionEditando.set(fresca);
          this.soloLecturaDefinicion.set(true);
          this.definicionTecnica = this.formatearDefinicion(fresca.verJson);
          this.cargarVersiones();
          this.cargarVersionVigentePorFamilia(this.familiaSeleccionada());
          if (fresca.verEstado === 'PUBLISHED' && fresca.verVigente) {
            this.globalState.limpiarError();
            this.mostrarMensaje('Versión publicada correctamente. La versión quedó publicada y vigente; su definición histórica permanece protegida contra modificaciones.');
          } else {
            this.mostrarError('La publicación fue aceptada, pero la versión no quedó confirmada como PUBLICADA y VIGENTE. No se reporta éxito.');
          }
        },
        error: () => {
          this.guardando.set(false);
          this.operacionBuilderEnCurso.set(null);
          this.mostrarError('La solicitud de publicación fue aceptada, pero no se pudo verificar el estado final. Consulte nuevamente la versión; no se reporta rechazo del backend.');
        }
      }),
      error: error => this.finalizarOperacionBuilder(this.obtenerMensajeErrorPublicacion(error, 'La definición se guardó, pero el servidor rechazó la publicación. La versión no se publicó.'))
    });
  }

  private finalizarOperacionBuilder(mensaje: string): void {
    this.guardando.set(false);
    this.operacionBuilderEnCurso.set(null);
    this.mostrarError(mensaje);
  }

  publicarVersion(version: VersionFormularioDto): void {
    if (!this.esAdministrador()) return;
    void this.confirmation.confirm({
      title: '¿Publicar versión oficial?',
      message: `Al publicar la versión ID #${version.verId} (${version.verCodigo} v${version.verVersion}) de la familia ${this.familiaSeleccionada()}, se convertirá en la versión vigente, la versión vigente anterior quedará como histórica, esta versión quedará bloqueada en solo lectura y para cambios futuros deberá clonarla a un nuevo borrador.`,
      icon: 'question',
      confirmLabel: 'Sí, publicar versión',
      cancelLabel: 'Cancelar',
      confirmIcon: 'publish',
      confirmVariant: 'primary'
    }).then(confirmed => {
      if (confirmed) {
          this.guardando.set(true);
          this.operacionBuilderEnCurso.set('publicar');
          this.service.publicarVersionFormulario(version.verId).subscribe({
            next: () => {
              this.globalState.limpiarError();
              this.mostrarMensaje('Versión publicada y establecida como vigente correctamente.');
              this.cargarVersiones();
              this.cargarVersionVigentePorFamilia(this.familiaSeleccionada());

              if (this.versionEditando()?.verId === version.verId) {
                this.service.obtenerVersionFormulario(version.verId).subscribe({
                  next: versionFresca => {
                    this.guardando.set(false);
                    this.operacionBuilderEnCurso.set(null);
                    this.versionEditando.set(versionFresca);
                    this.soloLecturaDefinicion.set(
                      !this.esAdministrador() || versionFresca.verVigente || versionFresca.verEstado !== 'DRAFT'
                    );
                    this.definicionTecnica = this.formatearDefinicion(versionFresca.verJson);
                  },
                  error: err => {
                    this.guardando.set(false);
                    this.operacionBuilderEnCurso.set(null);
                    this.mostrarError(this.obtenerMensajeError(err, 'No se pudo refrescar el estado de la versión tras la publicación.'));
                    this.versionEditando.set(null);
                  }
                });
              } else {
                this.guardando.set(false);
                this.operacionBuilderEnCurso.set(null);
              }
            },
            error: error => {
              this.guardando.set(false);
              this.operacionBuilderEnCurso.set(null);
              this.mostrarError(this.obtenerMensajeError(error, 'No se pudo publicar la versión del formulario.'));
            }
          });
      } else {
          this.guardando.set(false);
          this.operacionBuilderEnCurso.set(null);
      }
    });
  }

  cambiarVigenciaVersion(version: VersionFormularioDto, vigente: boolean): void {
    if (!this.esAdministrador()) return;
    const accion = vigente ? 'activar' : 'desactivar';
    void this.confirmation.confirm({
      title: vigente ? '¿Activar versión?' : '¿Desactivar versión?',
      message: `¿Está seguro de ${accion} la versión ID #${version.verId} (${version.verCodigo} v${version.verVersion})?`,
      icon: 'warning',
      confirmLabel: vigente ? 'Sí, activar' : 'Sí, desactivar',
      cancelLabel: 'Cancelar',
      confirmIcon: vigente ? 'activate' : 'deactivate',
      confirmVariant: vigente ? 'success' : 'danger'
    }).then(confirmed => {
      if (confirmed) {
          this.guardando.set(true);
          this.service.cambiarVigenciaFormulario(version.verId, vigente).subscribe({
            next: () => {
              this.guardando.set(false);
              this.globalState.limpiarError();
              this.mostrarMensaje(vigente ? 'Versión establecida como activa exitosamente.' : 'Versión desactivada.');
              this.cargarVersiones();
              this.cargarVersionVigentePorFamilia(this.familiaSeleccionada());
            },
            error: error => {
              this.guardando.set(false);
              this.mostrarError(this.obtenerMensajeError(error, 'No se pudo actualizar la vigencia de la versión.'));
            }
          });
      }
    });
  }

  eliminarVersionFormulario(version: VersionFormularioDto): void {
    if (!this.esAdministrador()) return;
    if (version.verEstado !== 'DRAFT' || version.verVigente) {
      this.mostrarError('Las versiones publicadas forman parte del historial y no pueden eliminarse. Para modificar, clone la versión a un nuevo borrador.');
      return;
    }

    void this.confirmation.confirm({
      title: '¿Eliminar versión?',
      message: `¿Está seguro de eliminar permanentemente la versión ID #${version.verId} (${version.verCodigo} v${version.verVersion})?`,
      icon: 'warning',
      confirmLabel: 'Sí, eliminar',
      cancelLabel: 'Cancelar',
      confirmIcon: 'delete',
      confirmVariant: 'danger'
    }).then(confirmed => {
      if (confirmed) {
          this.guardando.set(true);
          this.service.eliminarVersionFormulario(version.verId).subscribe({
            next: () => {
              this.guardando.set(false);
              this.globalState.limpiarError();
              this.confirmarEliminacionVersion(version);
              this.cargarVersiones();
            },
            error: error => {
              this.guardando.set(false);
              this.mostrarError(this.obtenerMensajeError(error, 'No se pudo eliminar la versión del formulario.'));
            }
          });
      }
    });
  }

  cerrarDefinicion(): void {
    this.limpiarAlertasPorScope('formBuilder');
    this.versionEditando.set(null);
    this.mostrarDetalleComoContexto();
    // Return focus after Angular's change-detection turn removes the builder.
    // Animation frames can run while a nested dialog is still being torn down
    // in headless Chromium, leaving focus on BODY after that dialog disappears.
    setTimeout(() => {
      this.detalleFamiliaRef?.instance.enfocarContexto();
    }, 0);
  }

  crearNuevaVersionDesdeDetalle(familia: FamiliaFormularioDto): void {
    if (!this.esAdministrador()) return;
    if (!familia || familia.famId <= 0) {
      this.mostrarError('No se encontró la familia para crear la nueva versión.');
      return;
    }

    const plantillaBase = {
      codigoFormulario: familia.famCodigo.trim().toUpperCase(),
      nombreFormulario: familia.famNombre.trim(),
      version: "1.0",
      secciones: [
        {
          id: "identificacion",
          clave: "identificacion",
          titulo: "Identificación del riesgo",
          orden: 1,
          campos: [
            {
              id: "area_principal",
              clave: "area_principal",
              etiqueta: "Área principal",
              tipo: "selector-catalogo",
              codigoCatalogo: "MR_AREA_PRINCIPAL",
              permiteValorManual: true,
              obligatorio: true,
              soloLectura: false,
              placeholder: "Escriba el área principal",
              textoAyuda: "Seleccione un área administrada o use la opción manual si aún no está catalogada."
            },
            {
              id: "dueno_riesgo",
              clave: "dueno_riesgo",
              etiqueta: "Responsable o dueño del riesgo (área responsable)",
              tipo: "selector-catalogo",
              codigoCatalogo: "MR_AREA_RESPONSABLE",
              permiteValorManual: true,
              obligatorio: true,
              soloLectura: false,
              placeholder: "Escriba el área responsable",
              textoAyuda: "Entrada manual con sugerencias administrables; se permite escribir un área que no esté catalogada."
            }
          ]
        }
      ],
      catalogos: [
        {
          codigo: "MR_AREA_PRINCIPAL",
          nombre: "Áreas principales",
          elementos: []
        },
        {
          codigo: "MR_AREA_RESPONSABLE",
          nombre: "Áreas responsables",
          elementos: []
        }
      ],
      reglas: []
    };

    this.guardando.set(true);
    this.globalState.limpiarError();

    this.service.crearBorradorFormulario(
      familia.famId,
      familia.famCodigo.trim().toUpperCase(),
      JSON.stringify(plantillaBase)
    ).subscribe({
      next: verId => {
        this.guardando.set(false);
        this.globalState.limpiarError();
        this.mostrarMensaje(`Formulario borrador creado exitosamente con ID #${verId}.`);
        this.cargarVersiones();
      },
      error: error => {
        this.guardando.set(false);
        this.globalState.limpiarError();
        this.mostrarError(this.obtenerMensajeError(error, 'No se pudo crear el borrador del formulario.'));
      }
    });
  }

  cargarVersionVigentePorFamilia(familiaCodigo: string): void {
    this.service.obtenerVersionVigenteFormulario(familiaCodigo).subscribe({
      next: version => {
        this.versionVigente.set(version);
        this.inicializarRespuestas();
      },
      error: () => {
        this.versionVigente.set(null);
        this.globalState.limpiarError();
      }
    });
  }

  private inicializarRespuestas(): void {
    const actuales = this.respuestas();
    const iniciales: RespuestasFormulario = {};
    for (const campo of this.secciones().flatMap(seccion => seccion.campos)) {
      iniciales[campo.clave] = actuales[campo.clave] ?? null;
    }
    this.respuestas.set(iniciales);
  }

  private extraerDefinicionVersion(version: VersionFormularioDto | null): DefinicionFormularioEditable {
    if (!version) {
      return { codigoFormulario: '', nombreFormulario: '', secciones: [] };
    }

    return normalizarDefinicionFormulario(version.verJson, version.verCodigo, version.verCodigo);
  }

  private parsearRespuestas(contenido: string): RespuestasFormulario {
    return normalizarRespuestasFormulario(contenido);
  }

  private construirRespuestasFormularioPersistibles(): RespuestasFormulario {
    const secciones = this.modalEditarAbierto() || this.modalNuevaEvaluacionAbierto()
      ? this.seccionesModal()
      : this.secciones();
    return construirRespuestasFormularioPersistibles(this.respuestas(), secciones);
  }

  private formatearDefinicion(contenido: string): string {
    try {
      return JSON.stringify(JSON.parse(contenido), null, 2);
    } catch {
      return contenido;
    }
  }

  private tieneValor(valor: unknown): boolean {
    return tieneValorRespuesta(valor);
  }

  private finalizarConError(error: unknown, mensaje: string): void {
    this.cargando.set(false);
    this.mostrarError(this.obtenerMensajeError(error, mensaje));
  }

  private confirmarEliminacionVersion(version: VersionFormularioDto): void {
    const obtenerVersion = (this.service as MatricesRiesgosService & {
      obtenerVersionFormulario?: (id: number) => Observable<VersionFormularioDto>;
    }).obtenerVersionFormulario;
    if (typeof obtenerVersion !== 'function') {
      this.mostrarMensaje(`Formulario ID #${version.verId} eliminado correctamente.`);
      return;
    }

    obtenerVersion.call(this.service, version.verId).subscribe({
      next: () => this.mostrarError('La API confirmó la eliminación, pero la lectura aún encontró la versión. No se reporta éxito.'),
      error: error => {
        if ((error as { status?: number })?.status === 404) {
          this.mostrarMensaje(`Formulario ID #${version.verId} eliminado correctamente.`);
          return;
        }
        this.mostrarError(this.obtenerMensajeError(error, 'No se pudo verificar la eliminación de la versión del formulario.'));
      }
    });
  }

  private mostrarMensaje(texto: string): void {
    const detalle = this.detalleFamiliaRef?.instance;
    if (this.detalleFamiliaDinamicoAbierto() && detalle) {
      detalle.mostrarFeedbackVersiones('success', texto);
      this.detalleFamiliaRef?.changeDetectorRef.detectChanges();
      return;
    }
    const scope = this.obtenerScopeAlerta();
    this.establecerMensaje(scope, texto);
    this.programarAutoDismiss(5000, scope);
  }

  private mostrarError(texto: string): void {
    const detalle = this.detalleFamiliaRef?.instance;
    if (this.detalleFamiliaDinamicoAbierto() && detalle) {
      detalle.mostrarFeedbackVersiones('error', texto);
      this.detalleFamiliaRef?.changeDetectorRef.detectChanges();
      return;
    }
    const scope = this.obtenerScopeAlerta();
    this.establecerError(scope, texto);
    this.programarAutoDismiss(8000, scope);
  }

  private limpiarAlertas(): void {
    this.limpiarAlertasPorScope(this.obtenerScopeAlerta());
  }

  private mostrarErrorEnEditor(texto: string): void {
    const scope = this.modalEditarAbierto() ? 'editEvaluation' : 'newEvaluation';
    this.establecerError(scope, texto);
    this.programarAutoDismiss(8000, scope);
  }

  private obtenerScopeAlerta(): AlertScope {
    if (this.versionEditando()) return 'formBuilder';
    if (this.modalEditarAbierto()) return 'editEvaluation';
    if (this.modalNuevaEvaluacionAbierto()) return 'newEvaluation';
    return 'page';
  }

  private establecerError(scope: AlertScope, texto: string): void {
    this.establecerAlerta(scope, texto, null);
  }

  private establecerMensaje(scope: AlertScope, texto: string): void {
    this.establecerAlerta(scope, null, texto);
  }

  private establecerAlerta(scope: AlertScope, error: string | null, mensaje: string | null): void {
    if (scope === 'newEvaluation') {
      this.errorNuevaEvaluacion.set(error);
      this.mensajeNuevaEvaluacion.set(mensaje);
    } else if (scope === 'editEvaluation') {
      this.errorEdicionEvaluacion.set(error);
      this.mensajeEdicionEvaluacion.set(mensaje);
    } else if (scope === 'formBuilder') {
      this.errorFormBuilder.set(error);
      this.mensajeFormBuilder.set(mensaje);
    } else {
      this.error.set(error);
      this.mensaje.set(mensaje);
    }
  }

  private limpiarAlertasEditor(scope: 'newEvaluation' | 'editEvaluation'): void {
    this.limpiarAlertasPorScope(scope);
  }

  private limpiarAlertasPorScope(scope: AlertScope): void {
    this.establecerAlerta(scope, null, null);
    this.limpiarAutoDismiss(scope);
  }

  private programarAutoDismiss(ms: number, scope: AlertScope): void {
    this.limpiarAutoDismiss(scope);
    this.autoDismissTimers[scope] = setTimeout(() => {
      this.establecerAlerta(scope, null, null);
      delete this.autoDismissTimers[scope];
      if (scope === 'page') this.globalState.limpiarError();
    }, ms);
  }

  private limpiarAutoDismiss(scope?: AlertScope): void {
    const scopes: AlertScope[] = scope ? [scope] : ['page', 'newEvaluation', 'editEvaluation', 'formBuilder'];
    for (const current of scopes) {
      const timer = this.autoDismissTimers[current];
      if (timer) clearTimeout(timer);
      delete this.autoDismissTimers[current];
    }
  }

  private obtenerMensajeErrorLectura(error: unknown, mensaje: string): string {
    const respuesta = error as {
      error?: {
        detail?: string;
        mensaje?: string;
        title?: string;
        errors?: Record<string, string[]>;
      };
    };
    const errorValidacion = Object.values(respuesta?.error?.errors ?? {}).flat().find(Boolean);
    return respuesta?.error?.detail
      || respuesta?.error?.mensaje
      || errorValidacion
      || respuesta?.error?.title
      || mensaje;
  }

  private obtenerMensajeErrorPublicacion(error: unknown, mensaje: string): string {
    const respuesta = error as { error?: { detail?: string; mensaje?: string; title?: string; errors?: Record<string, string[]> } };
    const errorValidacion = Object.values(respuesta?.error?.errors ?? {}).flat().find(Boolean);
    const detalle = respuesta?.error?.detail || respuesta?.error?.mensaje || errorValidacion || respuesta?.error?.title;
    return detalle && detalle !== mensaje ? `${mensaje} Detalle del servidor: ${detalle}` : mensaje;
  }
  private obtenerMensajeError(error: unknown, mensaje: string): string {
    const respuesta = error as {
      status?: number;
      error?: {
        detail?: string;
        mensaje?: string;
        title?: string;
        errors?: Record<string, string[]>;
      };
    };
    const errorValidacion = Object.values(respuesta?.error?.errors ?? {}).flat().find(Boolean);

    if (respuesta?.status === 409) {
      return 'La evaluación fue modificada por otra sesión o usuario. Recargue los datos antes de continuar; sus cambios actuales siguen en pantalla.';
    }

    return respuesta?.error?.detail
      || respuesta?.error?.mensaje
      || errorValidacion
      || (respuesta?.error?.title === 'One or more validation errors occurred.'
        ? 'La definición enviada contiene errores de validación.'
        : respuesta?.error?.title)
      || mensaje;
  }
}
