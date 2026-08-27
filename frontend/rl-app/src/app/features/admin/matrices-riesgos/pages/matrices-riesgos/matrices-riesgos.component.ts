import { ApplicationRef, ChangeDetectionStrategy, Component, ComponentRef, EnvironmentInjector, HostListener, OnInit, OnDestroy, computed, createComponent, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subscription } from 'rxjs';
import { MatricesRiesgosService } from '../../data-access/matrices-riesgos.service';
import {
  CampoFormulario,
  DefinicionFormularioEditable,
  EvaluacionRiesgoDto,
  EvaluacionRiesgoResumenDto,
  EvaluacionesPaginadasDto,
  FamiliaFormularioDto,
  FlujoEvaluacionDto,
  MetodologiaFormulario,
  RespuestasFormulario,
  RiesgoReporteFila,
  ValorRespuestaFormulario,
  VersionFormularioDto
} from '../../models/matrices-riesgos.models';
import { RiesgoDto } from '../../models/matrices-riesgos-fase11.models';
import { GlobalHttpStateService } from '../../../../../core/services/global-http-state.service';

import { FormBuilderComponent } from '../../components/form-builder/form-builder.component';
import { DynamicFieldRendererComponent } from '../../components/dynamic-field-renderer/dynamic-field-renderer.component';
import { FamiliaDetalleModalComponent } from '../../components/familia-detalle-modal/familia-detalle-modal.component';
import { FamiliaCrearModalComponent } from '../../components/familia-crear-modal/familia-crear-modal.component';
import { FamiliaEditarModalComponent } from '../../components/familia-editar-modal/familia-editar-modal.component';
import { AuthService } from '../../../../../core/auth/auth.service';
import { recalcularFormulasEvaluacion } from '../../utils/dynamic-formula-evaluator.util';
import {
  normalizarDefinicionFormulario,
  normalizarMetodologiaFormulario,
  normalizarRespuestasFormulario,
  tieneValorRespuesta
} from '../../utils/dynamic-form-renderer.util';
import { sonJsonSemanticamenteEquivalentes } from '../../utils/form-builder-semantic-comparator.util';

type TabMatrices = 'evaluaciones' | 'consolidado' | 'plantillas';

@Component({
  selector: 'app-matrices-riesgos',
  standalone: true,
  imports: [CommonModule, FormsModule, FormBuilderComponent, DynamicFieldRendererComponent],
  templateUrl: './matrices-riesgos.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class MatricesRiesgosComponent implements OnInit, OnDestroy {
  private readonly service = inject(MatricesRiesgosService);
  private readonly globalState = inject(GlobalHttpStateService);
  private readonly authService = inject(AuthService);
  private readonly applicationRef = inject(ApplicationRef);
  private readonly environmentInjector = inject(EnvironmentInjector);
  private detalleFamiliaRef: ComponentRef<FamiliaDetalleModalComponent> | null = null;
  private crearFamiliaRef: ComponentRef<FamiliaCrearModalComponent> | null = null;
  private editarFamiliaRef: ComponentRef<FamiliaEditarModalComponent> | null = null;
  private readonly suscripcionesDetalleFamilia: Subscription[] = [];
  private readonly suscripcionesCrearFamilia: Subscription[] = [];
  private readonly suscripcionesEditarFamilia: Subscription[] = [];
  private autoDismissTimer: ReturnType<typeof setTimeout> | null = null;
  private focoRetornoEditarFamilia: HTMLElement | null = null;
  private detalleEnContexto = false;

  readonly opcionesRegistrosPorPagina = [10, 20, 50] as const;
  private suscripcionEvaluaciones: Subscription | null = null;
  private secuenciaCargaEvaluaciones = 0;

  readonly esAdministrador = computed(() => this.authService.tieneRol(['ADMIN', 'ADMINISTRADOR']));

  readonly tab = signal<TabMatrices>('evaluaciones');
  readonly cargando = signal(false);
  readonly guardando = signal(false);
  readonly operacionBuilderEnCurso = signal<'guardar' | 'publicar' | null>(null);
  readonly error = signal<string | null>(null);
  readonly mensaje = signal<string | null>(null);
  readonly errorModal = signal<string | null>(null);

  readonly cargandoEvaluaciones = signal(false);
  readonly errorEvaluaciones = signal<string | null>(null);

  readonly cargandoFormulario = signal(false);
  readonly errorFormulario = signal<string | null>(null);

  readonly cargandoConsolidado = signal(false);
  readonly errorConsolidado = signal<string | null>(null);

  readonly cargandoPlantillas = signal(false);
  readonly errorPlantillas = signal<string | null>(null);
  readonly cargandoFamilias = signal(false);
  readonly errorFamilias = signal<string | null>(null);

  readonly modalGestorFamiliasAbierto = signal<boolean>(false);
  readonly modalVerFamiliaAbierto = signal<FamiliaFormularioDto | null>(null);
  readonly detalleFamiliaDinamicoAbierto = signal(false);
  readonly filtroBuscarFamilia = signal('');
  readonly filtroEstadoFamilia = signal('TODAS');
  readonly filtroVigenciaFamilia = signal('TODAS');
  readonly paginaFamilias = signal(1);
  readonly registrosPorPaginaFamilias = signal(10);
  readonly mostrandoVersionesFamilia = signal(false);

  readonly totalFamilias = computed(() => this.familias().length);
  readonly totalFamiliasActivas = computed(() => this.familias().filter(f => f.famActivo).length);
  readonly totalFamiliasInactivas = computed(() => this.familias().filter(f => !f.famActivo).length);
  readonly totalVersionesFamilias = computed(() => this.familias().reduce((total, f) => total + Math.max(0, Number(f.totalVersiones) || 0), 0));

  readonly familiasFiltradas = computed<FamiliaFormularioDto[]>(() => {
    const lista = this.familias();
    const buscar = this.filtroBuscarFamilia().trim().toLowerCase();
    const estado = this.filtroEstadoFamilia();
    const vigencia = this.filtroVigenciaFamilia();

    return lista.filter(f => {
      const cumpleBuscar = !buscar
        || f.famCodigo.toLowerCase().includes(buscar)
        || f.famNombre.toLowerCase().includes(buscar);
      const cumpleEstado = estado === 'TODAS'
        || (estado === 'ACTIVAS' && f.famActivo)
        || (estado === 'INACTIVAS' && !f.famActivo);
      const cumpleVigencia = vigencia === 'TODAS'
        || (vigencia === 'VIGENTES' && f.tieneVersionVigente)
        || (vigencia === 'SIN_VIGENTE' && !f.tieneVersionVigente);
      return cumpleBuscar && cumpleEstado && cumpleVigencia;
    });
  });

  readonly totalPaginasFamilias = computed(() => {
    const porPagina = this.registrosPorPaginaFamilias();
    return porPagina > 0 ? Math.ceil(this.familiasFiltradas().length / porPagina) : 0;
  });

  readonly familiasPaginadas = computed<FamiliaFormularioDto[]>(() => {
    const lista = this.familiasFiltradas();
    const porPagina = this.registrosPorPaginaFamilias();
    const totalPaginas = this.totalPaginasFamilias();
    const paginaEfectiva = totalPaginas === 0 ? 1 : Math.min(Math.max(this.paginaFamilias(), 1), totalPaginas);
    const inicio = (paginaEfectiva - 1) * porPagina;
    return lista.slice(inicio, inicio + porPagina);
  });

  readonly metodologia = signal<MetodologiaFormulario | null>(null);
  readonly versionVigente = signal<VersionFormularioDto | null>(null);
  readonly versiones = signal<VersionFormularioDto[]>([]);
  readonly familias = signal<FamiliaFormularioDto[]>([]);
  readonly familiaSeleccionada = signal<string>('MATRIZ_RIESGOS_LAFT');
  readonly modalFamiliaAbierto = signal<boolean>(false);
  readonly modoEdicionFamilia = signal<boolean>(false);
  familiaIdEditando = 0;
  nuevaFamiliaCodigo = '';
  nuevaFamiliaNombre = '';
  nuevaFamiliaDescripcion = '';
  nuevaFamiliaActivo = true;

  readonly modalFormularioAbierto = signal<boolean>(false);
  nuevoFormularioCodigo = '';
  nuevoFormularioNombre = '';

  readonly riesgos = signal<RiesgoDto[]>([]);
  readonly evaluaciones = signal<EvaluacionRiesgoResumenDto[]>([]);
  readonly totalRegistros = signal(0);
  readonly totalPaginas = signal(0);

  readonly paginasVisibles = computed<number[]>(() => {
    const total = this.totalPaginas();
    const actual = this.pagina();

    if (total <= 0) return [];

    const candidatos = [
      1,
      actual - 2,
      actual - 1,
      actual,
      actual + 1,
      actual + 2,
      total
    ];

    const validos = candidatos.filter(p => Number.isInteger(p) && p >= 1 && p <= total);
    const unicos = Array.from(new Set(validos));
    return unicos.sort((a, b) => a - b);
  });

  readonly evaluacionSeleccionada = signal<EvaluacionRiesgoDto | null>(null);
  readonly evaluacionResumenSeleccionada = signal<EvaluacionRiesgoResumenDto | null>(null);
  readonly flujos = signal<FlujoEvaluacionDto[]>([]);
  readonly consolidado = signal<RiesgoReporteFila[]>([]);

  readonly modalVerAbierto = signal<boolean>(false);
  readonly modalEditarAbierto = signal<boolean>(false);
  readonly modalSeguimientoAbierto = signal<boolean>(false);
  readonly modalNuevaEvaluacionAbierto = signal<boolean>(false);
  readonly metodologiaHistorica = signal<MetodologiaFormulario | null>(null);
  readonly versionHistorica = signal<VersionFormularioDto | null>(null);

  readonly pagina = signal(1);
  readonly registrosPorPagina = signal(10);
  readonly filtroBuscar = signal('');
  readonly filtroEstado = signal('');
  readonly riesgoId = signal(0);
  readonly respuestas = signal<RespuestasFormulario>({});

  motivoTransicion = '';
  nuevoEstado = 'EN_REVISION';
  archivoEvidencia: File | null = null;

  readonly versionEditando = signal<VersionFormularioDto | null>(null);
  readonly soloLecturaDefinicion = signal<boolean>(false);
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
    if (this.detalleFamiliaDinamicoAbierto()) {
      event.preventDefault();
      this.cerrarModalVerFamilia();
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
    } else if (this.modalFormularioAbierto()) {
      event.preventDefault();
      this.cerrarModalFormulario();
    }
  }

  ngOnInit(): void {
    this.cargarFamilias();
    this.cargarRiesgos();
    this.cargarEvaluaciones();
    this.cargarFormularioVigente();
  }

  ngOnDestroy(): void {
    this.secuenciaCargaEvaluaciones++;
    this.suscripcionEvaluaciones?.unsubscribe();
    this.suscripcionEvaluaciones = null;
    this.cerrarModalCrearFamilia();
    this.cerrarModalEditarFamilia();
    this.cerrarModalVerFamilia();
    this.limpiarAutoDismiss();
    this.cancelarDebounceBuscarPendiente();
  }

  seleccionarTab(tab: TabMatrices): void {
    this.tab.set(tab);
    this.limpiarAlertas();
    this.globalState.limpiarError();

    if (tab === 'consolidado') this.cargarConsolidado();
    if (tab === 'plantillas') {
      this.mostrandoVersionesFamilia.set(false);
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
    if (typeof this.service.listarFamiliasFormulario !== 'function') {
      this.familias.set([]);
      this.cargandoFamilias.set(false);
      return;
    }

    this.cargandoFamilias.set(true);
    this.errorFamilias.set(null);
    this.service.listarFamiliasFormulario().subscribe({
      next: familias => {
        this.familias.set(familias);
        if (familias.length > 0) {
          const actualValida = familias.some(f => f.famCodigo === this.familiaSeleccionada());
          if (!actualValida) {
            const activa = familias.find(f => f.famActivo) ?? familias[0];
            this.familiaSeleccionada.set(activa.famCodigo);
          }
        }
        const totalPaginas = this.totalPaginasFamilias();
        if (totalPaginas > 0 && this.paginaFamilias() > totalPaginas) {
          this.paginaFamilias.set(totalPaginas);
        }
        this.cargandoFamilias.set(false);
      },
      error: error => {
        this.familias.set([]);
        this.errorFamilias.set(this.obtenerMensajeError(error, 'No se pudieron cargar las familias de formularios.'));
        this.cargandoFamilias.set(false);
      }
    });
  }

  seleccionarFamilia(codigo: string): void {
    this.familiaSeleccionada.set(codigo);
    this.versionEditando.set(null);
    this.soloLecturaDefinicion.set(false);
    this.definicionTecnica = '';
    this.cargarVersiones();
    this.cargarVersionVigentePorFamilia(codigo);
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
    this.filtroVigenciaFamilia.set('TODAS');
    this.paginaFamilias.set(1);
  }

  cambiarPaginaFamilias(nuevaPagina: number): void {
    const total = this.totalPaginasFamilias();
    if (!Number.isInteger(nuevaPagina) || total <= 0 || nuevaPagina < 1 || nuevaPagina > total) {
      return;
    }
    this.paginaFamilias.set(nuevaPagina);
  }

  cambiarRegistrosPorPaginaFamilias(cantidad: number): void {
    const num = Number(cantidad);
    if (!Number.isInteger(num) || !this.opcionesRegistrosPorPagina.includes(num as 10 | 20 | 50)) {
      return;
    }
    this.registrosPorPaginaFamilias.set(num);
    this.paginaFamilias.set(1);
  }

  seleccionarFamiliaDesdeGestor(famCodigo: string): void {
    this.seleccionarFamilia(famCodigo);
    this.mostrandoVersionesFamilia.set(true);
    this.cerrarModalGestorFamilias();
  }

  volverAGestorFamilias(): void {
    this.mostrandoVersionesFamilia.set(false);
    this.versionEditando.set(null);
    this.errorPlantillas.set(null);
    this.cargarFamilias();
  }

  abrirModalVerFamilia(fam: FamiliaFormularioDto): void {
    if (!fam || fam.famId <= 0) return;

    this.cerrarModalVerFamilia();
    this.cerrarModalGestorFamilias();
    this.modalVerFamiliaAbierto.set(null);

    const componentRef = createComponent(FamiliaDetalleModalComponent, {
      environmentInjector: this.environmentInjector
    });
    componentRef.setInput('familiaId', fam.famId);
    componentRef.setInput('familiaReferencia', fam);

    this.suscripcionesDetalleFamilia.push(
      componentRef.instance.cerrar.subscribe(() => this.cerrarModalVerFamilia()),
      componentRef.instance.gestionarVersiones.subscribe(familia => {
        this.cerrarModalVerFamilia();
        this.seleccionarFamiliaDesdeGestor(familia.famCodigo);
      }),
      componentRef.instance.editarFamilia.subscribe(familia => {
        this.focoRetornoEditarFamilia = document.activeElement instanceof HTMLElement ? document.activeElement : null;
        this.ocultarDetalleComoContexto();
        this.abrirModalEditarFamilia(familia);
      }),
      componentRef.instance.nuevaVersion.subscribe(familia => {
        this.ocultarDetalleComoContexto();
        this.seleccionarFamilia(familia.famCodigo);
        this.mostrandoVersionesFamilia.set(true);
        this.abrirModalCrearFormulario();
      }),
      componentRef.instance.verDefinicion.subscribe(({ familia, version }) => {
        this.ocultarDetalleComoContexto();
        this.familiaSeleccionada.set(familia.famCodigo);
        this.abrirDefinicion(version, true);
      })
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
    this.modalVerFamiliaAbierto.set(null);
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
    import('sweetalert2').then(Swal => {
      Swal.default.fire({
        title: `¿Desactivar la familia ${fam.famNombre}?`,
        html: `<p class="text-sm text-gray-700 mb-2">La familia dejará de estar disponible para nuevas operaciones.</p>
               <p class="text-xs text-gray-500">No se eliminarán sus versiones ni información histórica.</p>`,
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#d97706',
        cancelButtonColor: '#6b7280',
        confirmButtonText: 'Desactivar',
        cancelButtonText: 'Cancelar'
      }).then(result => {
        if (result.isConfirmed) {
          this.desactivarFamilia(fam);
        }
      });
    });
  }

  desactivarFamilia(fam: FamiliaFormularioDto): void {
    this.guardando.set(true);
    this.limpiarAlertas();
    this.service.desactivarFamiliaFormulario(fam.famId).subscribe({
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
    import('sweetalert2').then(Swal => {
      Swal.default.fire({
        title: `¿Activar nuevamente la familia ${fam.famNombre}?`,
        icon: 'question',
        showCancelButton: true,
        confirmButtonColor: '#059669',
        cancelButtonColor: '#6b7280',
        confirmButtonText: 'Sí, activar',
        cancelButtonText: 'Cancelar'
      }).then(result => {
        if (result.isConfirmed) {
          this.activarFamilia(fam);
        }
      });
    });
  }

  activarFamilia(fam: FamiliaFormularioDto): void {
    this.guardando.set(true);
    this.limpiarAlertas();
    this.service.activarFamiliaFormulario(fam.famId).subscribe({
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

  confirmarEliminarFamilia(fam: FamiliaFormularioDto): void {
    import('sweetalert2').then(Swal => {
      Swal.default.fire({
        title: `Eliminar familia ${fam.famCodigo}`,
        html: `<p class="text-sm text-gray-700 mb-2">Esta operación eliminará permanentemente esta familia.</p>
               <p class="text-xs text-gray-500">Solo es posible porque actualmente no posee versiones asociadas.</p>`,
        icon: 'error',
        showCancelButton: true,
        confirmButtonColor: '#dc2626',
        cancelButtonColor: '#6b7280',
        confirmButtonText: 'Eliminar',
        cancelButtonText: 'Cancelar'
      }).then(result => {
        if (result.isConfirmed) {
          this.eliminarFamilia(fam);
        }
      });
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
    const codigo = this.familiaSeleccionada() || 'MATRIZ_RIESGOS_LAFT';
    this.service.listarHistorialVersionesFormulario(codigo).subscribe({
      next: versiones => {
        this.versiones.set(versiones);
        this.cargandoPlantillas.set(false);
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
    this.service.listarRiesgos().subscribe({
      next: riesgos => this.riesgos.set(riesgos),
      error: () => this.riesgos.set([])
    });
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

        const items = Array.isArray(paginado?.items) ? paginado.items : [];
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
    this.cargandoConsolidado.set(true);
    this.errorConsolidado.set(null);
    this.service.obtenerConsolidado().subscribe({
      next: filas => {
        this.consolidado.set(filas);
        this.cargandoConsolidado.set(false);
      },
      error: error => {
        const msg = this.obtenerMensajeError(error, 'No se pudo cargar la matriz consolidada.');
        this.errorConsolidado.set(msg);
        this.cargandoConsolidado.set(false);
      }
    });
  }

  descargarConsolidado(formato: 'excel' | 'pdf'): void {
    this.limpiarAlertas();
    const solicitud = formato === 'excel'
      ? this.service.descargarConsolidadoExcel()
      : this.service.descargarConsolidadoPdf();

    solicitud.subscribe({
      next: blob => {
        const url = URL.createObjectURL(blob);
        const enlace = document.createElement('a');
        enlace.href = url;
        enlace.download = formato === 'excel' ? 'Matriz_Riesgos.xlsx' : 'Matriz_Riesgos.pdf';
        enlace.click();
        URL.revokeObjectURL(url);
      },
      error: error => this.mostrarError(this.obtenerMensajeError(error, `No se pudo generar el reporte ${formato.toUpperCase()}.`))
    });
  }

  actualizarRespuesta(campo: CampoFormulario, valor: ValorRespuestaFormulario): void {
    this.respuestas.update(actuales => {
      const nuevas = { ...actuales, [campo.clave]: valor };
      const seccionesActuales = this.modalEditarAbierto() || this.modalNuevaEvaluacionAbierto()
        ? this.seccionesModal()
        : this.secciones();
      const todosLosCampos = seccionesActuales.flatMap(s => s.campos);
      const { respuestasActualizadas } = recalcularFormulasEvaluacion(todosLosCampos, nuevas);
      return respuestasActualizadas;
    });
  }

  valorRespuesta(campo: CampoFormulario): ValorRespuestaFormulario {
    return this.respuestas()[campo.clave] ?? null;
  }

  opcionesCatalogo(campo: CampoFormulario): Array<{ codigo: string; valor: string }> {
    if (!campo.codigoCatalogo) return [];

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
    this.limpiarAlertas();
    this.evaluacionSeleccionada.set(null);
    this.evaluacionResumenSeleccionada.set(null);
    this.riesgoId.set(0);
    this.versionHistorica.set(null);
    this.metodologiaHistorica.set(null);
    this.inicializarRespuestas();
    this.modalNuevaEvaluacionAbierto.set(true);
  }

  cerrarModalNuevaEvaluacion(): void {
    this.modalNuevaEvaluacionAbierto.set(false);
    this.globalState.limpiarError();
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
            this.service.obtenerFamiliaFormularioPorId(1).subscribe({
              next: () => {
                this.cargando.set(false);
                this.modalVerAbierto.set(true);
              },
              error: () => {
                this.cargando.set(false);
                this.modalVerAbierto.set(true);
              }
            });
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

  cerrarModalVer(): void {
    this.modalVerAbierto.set(false);
    this.globalState.limpiarError();
  }

  editarEvaluacion(evaluacion: EvaluacionRiesgoResumenDto): void {
    if ((evaluacion.estado || '').toUpperCase() !== 'BORRADOR') {
      this.mostrarError('Solo se permite editar evaluaciones en estado BORRADOR.');
      return;
    }

    this.limpiarAlertas();
    this.cargando.set(true);
    this.evaluacionResumenSeleccionada.set(evaluacion);

    this.service.obtenerEvaluacion(evaluacion.evaId).subscribe({
      next: detalle => {
        this.evaluacionSeleccionada.set(detalle);
        this.riesgoId.set(detalle.evaRiesgoId);
        this.respuestas.set(this.parsearRespuestas(detalle.evaDataJson));

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
    this.modalEditarAbierto.set(false);
    this.globalState.limpiarError();
  }

  abrirModalSeguimiento(evaluacion: EvaluacionRiesgoResumenDto): void {
    this.limpiarAlertas();
    this.evaluacionResumenSeleccionada.set(evaluacion);
    this.motivoTransicion = '';
    this.nuevoEstado = this.obtenerTransicionPorDefecto(evaluacion.estado);
    this.cargarFlujos(evaluacion.evaId);
    this.modalSeguimientoAbierto.set(true);
  }

  cerrarModalSeguimiento(): void {
    this.modalSeguimientoAbierto.set(false);
    this.globalState.limpiarError();
  }

  obtenerTransicionesValidas(estadoActual: string): string[] {
    switch ((estadoActual || '').toUpperCase()) {
      case 'BORRADOR':
        return ['EN_REVISION'];
      case 'EN_REVISION':
        return ['OBSERVADA', 'APROBADA', 'RECHAZADA'];
      case 'OBSERVADA':
        return ['BORRADOR'];
      case 'APROBADA':
        return ['CERRADA'];
      default:
        return [];
    }
  }

  obtenerTransicionPorDefecto(estadoActual: string): string {
    const validas = this.obtenerTransicionesValidas(estadoActual);
    return validas.length > 0 ? validas[0] : '';
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

    const seccionesEval = esEdicion || this.modalNuevaEvaluacionAbierto()
      ? this.seccionesModal()
      : this.secciones();

    const todosLosCampos = seccionesEval.flatMap(s => s.campos);
    const { respuestasActualizadas, calculosJson } = recalcularFormulasEvaluacion(todosLosCampos, this.respuestas());

    const dto: EvaluacionRiesgoDto = {
      evaId: actual?.evaId ?? 0,
      evaRiesgoId: this.riesgoId(),
      evaVersionId: versionIdFinal,
      evaEstado: actual?.evaEstado ?? 'BORRADOR',
      evaDataJson: JSON.stringify(respuestasActualizadas),
      evaDataCalcJson: JSON.stringify(calculosJson),
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
              this.globalState.limpiarError();
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
        this.globalState.limpiarError();
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

    this.limpiarAlertas();
    if (!this.nuevoEstado.trim()) {
      this.mostrarError('Seleccione un estado de destino válido.');
      return;
    }

    this.guardando.set(true);
    this.service.transicionarEvaluacion(resumen.evaId, this.nuevoEstado, this.motivoTransicion).subscribe({
      next: () => {
        this.guardando.set(false);
        this.globalState.limpiarError();
        this.mostrarMensaje(`Estado de la evaluación #${resumen.evaId} actualizado a ${this.nuevoEstado} correctamente.`);
        this.motivoTransicion = '';
        this.cargarEvaluaciones();
        this.cargarFlujos(resumen.evaId);
        this.evaluacionResumenSeleccionada.set({
          ...resumen,
          estado: this.nuevoEstado
        });
        this.nuevoEstado = this.obtenerTransicionPorDefecto(this.nuevoEstado);
      },
      error: error => {
        this.guardando.set(false);
        this.mostrarError(this.obtenerMensajeError(error, 'No se pudo realizar la transición de estado.'));
      }
    });
  }

  cargarFlujos(evaluacionId: number): void {
    this.service.obtenerFlujos(evaluacionId).subscribe({
      next: flujos => this.flujos.set(flujos),
      error: () => this.flujos.set([])
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
        this.soloLecturaDefinicion.set(
          soloLectura ||
          !this.esAdministrador() ||
          versionAutoritativa.verVigente ||
          versionAutoritativa.verEstado !== 'DRAFT'
        );
        this.definicionTecnica = this.formatearDefinicion(versionAutoritativa.verJson);
      },
      error: error => {
        this.cargando.set(false);
        this.versionEditando.set(null);
        this.mostrarError(this.obtenerMensajeError(error, 'No se pudo cargar la versión autoritativa del formulario.'));
      }
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
        this.service.obtenerVersionFormulario(version.verId).subscribe({
          next: versionPersistida => {
            this.guardando.set(false);
            this.operacionBuilderEnCurso.set(null);
            const sonEquivalentes = sonJsonSemanticamenteEquivalentes(
              jsonEnviado,
              versionPersistida.verJson
            );

            if (!sonEquivalentes) {
              this.mostrarError('La persistencia recuperada del servidor no coincide semánticamente con la definición enviada.');
              return;
            }

            this.versionEditando.set(null);
            this.globalState.limpiarError();
            this.mostrarMensaje('Definición del formulario actualizada y verificada correctamente.');
            this.cargarVersiones();
          },
          error: getError => {
            this.guardando.set(false);
            this.operacionBuilderEnCurso.set(null);
            this.mostrarError(
              this.obtenerMensajeError(
                getError,
                'No se pudo verificar la persistencia de la versión del formulario tras guardar.'
              )
            );
          }
        });
      },
      error: error => {
        this.guardando.set(false);
        this.operacionBuilderEnCurso.set(null);
        this.mostrarError(this.obtenerMensajeError(error, 'No se pudo actualizar la definición del formulario.'));
      }
    });
  }

  publicarVersion(version: VersionFormularioDto): void {
    if (!this.esAdministrador()) return;
    import('sweetalert2').then(Swal => {
      Swal.default.fire({
        title: '¿Publicar versión oficial?',
        html: `<p class="text-sm text-gray-700 mb-2">Al publicar la versión <strong>ID #${version.verId}</strong> (${version.verCodigo} v${version.verVersion}) de la familia <strong>${this.familiaSeleccionada()}</strong>:</p>
               <ul class="text-xs text-gray-600 text-left list-disc pl-5 space-y-1 mb-2">
                 <li>Se convertirá en la versión <strong>vigente</strong> de la familia.</li>
                 <li>La versión vigente anterior quedará como <strong>histórica</strong>.</li>
                 <li>Esta versión quedará bloqueada en <strong>solo lectura</strong>.</li>
                 <li>Para cambios futuros deberá clonarla a un nuevo borrador.</li>
               </ul>`,
        icon: 'question',
        showCancelButton: true,
        confirmButtonColor: '#2563eb',
        cancelButtonColor: '#6b7280',
        confirmButtonText: 'Sí, publicar versión',
        cancelButtonText: 'Cancelar'
      }).then((result) => {
        if (result.isConfirmed) {
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
    });
  }

  cambiarVigenciaVersion(version: VersionFormularioDto, vigente: boolean): void {
    if (!this.esAdministrador()) return;
    const accion = vigente ? 'activar' : 'desactivar';
    import('sweetalert2').then(Swal => {
      Swal.default.fire({
        title: vigente ? '¿Activar versión?' : '¿Desactivar versión?',
        text: `¿Está seguro de ${accion} la versión ID #${version.verId} (${version.verCodigo} v${version.verVersion})?`,
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: vigente ? '#059669' : '#d97706',
        cancelButtonColor: '#6b7280',
        confirmButtonText: vigente ? 'Sí, activar' : 'Sí, desactivar',
        cancelButtonText: 'Cancelar'
      }).then((result) => {
        if (result.isConfirmed) {
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
    });
  }

  eliminarVersionFormulario(version: VersionFormularioDto): void {
    if (!this.esAdministrador()) return;
    if (version.verEstado !== 'DRAFT' || version.verVigente) {
      this.mostrarError('Las versiones publicadas forman parte del historial y no pueden eliminarse. Para modificar, clone la versión a un nuevo borrador.');
      return;
    }

    import('sweetalert2').then(Swal => {
      Swal.default.fire({
        title: '¿Eliminar versión?',
        text: `¿Está seguro de eliminar permanentemente la versión ID #${version.verId} (${version.verCodigo} v${version.verVersion})?`,
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#dc2626',
        cancelButtonColor: '#6b7280',
        confirmButtonText: 'Sí, eliminar',
        cancelButtonText: 'Cancelar',
        focusCancel: true
      }).then((result) => {
        if (result.isConfirmed) {
          this.guardando.set(true);
          this.service.eliminarVersionFormulario(version.verId).subscribe({
            next: () => {
              this.guardando.set(false);
              this.globalState.limpiarError();
              this.mostrarMensaje(`Formulario ID #${version.verId} eliminado correctamente.`);
              this.cargarVersiones();
            },
            error: error => {
              this.guardando.set(false);
              this.mostrarError(this.obtenerMensajeError(error, 'No se pudo eliminar la versión del formulario.'));
            }
          });
        }
      });
    });
  }

  abrirModalCrearFormulario(): void {
    if (!this.esAdministrador()) return;
    const famObj = this.familias().find(f => f.famCodigo === this.familiaSeleccionada());
    this.nuevoFormularioCodigo = famObj?.famCodigo || 'MATRIZ_NUEVA';
    this.nuevoFormularioNombre = famObj?.famNombre || 'Nueva Matriz de Riesgos';
    this.errorModal.set(null);
    this.globalState.limpiarError();
    this.modalFormularioAbierto.set(true);
  }

  cerrarModalFormulario(): void {
    this.modalFormularioAbierto.set(false);
    this.errorModal.set(null);
    this.globalState.limpiarError();
    this.mostrarDetalleComoContexto();
  }

  cerrarDefinicion(): void {
    this.versionEditando.set(null);
    this.mostrarDetalleComoContexto();
    setTimeout(() => {
      this.detalleFamiliaRef?.instance.enfocarContexto();
    }, 0);
  }

  guardarNuevoFormulario(): void {
    if (!this.esAdministrador()) return;
    const famObj = this.familias().find(f => f.famCodigo === this.familiaSeleccionada());
    if (!famObj) {
      this.errorModal.set('Seleccione una familia válida para crear el formulario.');
      return;
    }

    const plantillaBase = {
      codigoFormulario: this.nuevoFormularioCodigo.trim().toUpperCase(),
      nombreFormulario: this.nuevoFormularioNombre.trim(),
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
              tipo: "texto",
              obligatorio: true,
              soloLectura: false
            },
            {
              id: "dueno_riesgo",
              clave: "dueno_riesgo",
              etiqueta: "Dueño del riesgo",
              tipo: "texto",
              obligatorio: true,
              soloLectura: false
            }
          ]
        }
      ],
      catalogos: [],
      reglas: []
    };

    this.guardando.set(true);
    this.errorModal.set(null);
    this.globalState.limpiarError();

    this.service.crearBorradorFormulario(
      famObj.famId,
      this.nuevoFormularioCodigo.trim().toUpperCase(),
      JSON.stringify(plantillaBase)
    ).subscribe({
      next: verId => {
        this.guardando.set(false);
        this.modalFormularioAbierto.set(false);
        this.errorModal.set(null);
        this.globalState.limpiarError();
        this.mostrarMensaje(`Formulario borrador creado exitosamente con ID #${verId}.`);
        this.cargarVersiones();
      },
      error: error => {
        this.guardando.set(false);
        this.globalState.limpiarError();
        this.errorModal.set(this.obtenerMensajeError(error, 'No se pudo crear el borrador del formulario.'));
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

  private mostrarMensaje(texto: string): void {
    this.error.set(null);
    this.mensaje.set(texto);
    this.programarAutoDismiss(5000);
  }

  private mostrarError(texto: string): void {
    this.mensaje.set(null);
    this.error.set(texto);
    this.programarAutoDismiss(8000);
  }

  private limpiarAlertas(): void {
    this.error.set(null);
    this.mensaje.set(null);
    this.limpiarAutoDismiss();
  }

  private programarAutoDismiss(ms: number): void {
    this.limpiarAutoDismiss();
    this.autoDismissTimer = setTimeout(() => {
      this.error.set(null);
      this.mensaje.set(null);
      this.globalState.limpiarError();
    }, ms);
  }

  private limpiarAutoDismiss(): void {
    if (this.autoDismissTimer) {
      clearTimeout(this.autoDismissTimer);
      this.autoDismissTimer = null;
    }
  }

  private obtenerMensajeError(error: unknown, mensaje: string): string {
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
      || (respuesta?.error?.title === 'One or more validation errors occurred.'
        ? 'La definición enviada contiene errores de validación.'
        : respuesta?.error?.title)
      || mensaje;
  }
}
