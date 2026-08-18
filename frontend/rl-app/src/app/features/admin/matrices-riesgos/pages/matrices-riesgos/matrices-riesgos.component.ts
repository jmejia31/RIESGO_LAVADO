import { ChangeDetectionStrategy, Component, HostListener, OnInit, OnDestroy, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
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
  VersionFormularioDto
} from '../../models/matrices-riesgos.models';
import { RiesgoDto } from '../../models/matrices-riesgos-fase11.models';
import { GlobalHttpStateService } from '../../../../../core/services/global-http-state.service';

import { FormBuilderComponent } from '../../components/form-builder/form-builder.component';
import { AuthService } from '../../../../../core/auth/auth.service';
import { recalcularFormulasEvaluacion } from '../../utils/dynamic-formula-evaluator.util';

type TabMatrices = 'evaluaciones' | 'captura' | 'consolidado' | 'plantillas';

@Component({
  selector: 'app-matrices-riesgos',
  standalone: true,
  imports: [CommonModule, FormsModule, FormBuilderComponent],
  templateUrl: './matrices-riesgos.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class MatricesRiesgosComponent implements OnInit, OnDestroy {
  private readonly service = inject(MatricesRiesgosService);
  private readonly globalState = inject(GlobalHttpStateService);
  private readonly authService = inject(AuthService);
  private autoDismissTimer: ReturnType<typeof setTimeout> | null = null;

  readonly esAdministrador = computed(() => this.authService.tieneRol(['ADMIN', 'ADMINISTRADOR']));

  readonly tab = signal<TabMatrices>('evaluaciones');
  readonly cargando = signal(false);
  readonly guardando = signal(false);
  readonly error = signal<string | null>(null);
  readonly mensaje = signal<string | null>(null);
  readonly errorModal = signal<string | null>(null);

  // Estados de Carga y Error Independientes por Sección (F2)
  readonly cargandoEvaluaciones = signal(false);
  readonly errorEvaluaciones = signal<string | null>(null);

  readonly cargandoFormulario = signal(false);
  readonly errorFormulario = signal<string | null>(null);

  readonly cargandoConsolidado = signal(false);
  readonly errorConsolidado = signal<string | null>(null);

  readonly cargandoPlantillas = signal(false);
  readonly errorPlantillas = signal<string | null>(null);

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
  readonly evaluacionSeleccionada = signal<EvaluacionRiesgoDto | null>(null);
  readonly evaluacionResumenSeleccionada = signal<EvaluacionRiesgoResumenDto | null>(null);
  readonly flujos = signal<FlujoEvaluacionDto[]>([]);
  readonly consolidado = signal<RiesgoReporteFila[]>([]);

  // Modales de Evaluaciones
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
    // 1. Si tenemos metodologiaHistorica con secciones, usar esas
    const metHist = this.metodologiaHistorica();
    if (metHist && metHist.secciones && metHist.secciones.length > 0) {
      return metHist.secciones.map(sec => ({
        clave: sec.clave,
        titulo: sec.titulo || sec.clave,
        orden: sec.orden,
        columnasPorFila: 2,
        campos: sec.campos.map(c => ({
          clave: c.clave,
          etiqueta: c.etiqueta,
          tipo: c.tipo as 'texto' | 'numero' | 'selector-catalogo' | 'calculado' | 'archivo',
          obligatorio: c.obligatorio,
          soloLectura: c.soloLectura,
          formula: c.formula,
          codigoCatalogo: c.codigoCatalogo
        }))
      })).sort((a, b) => a.orden - b.orden);
    }

    // 2. Si estamos en modal de ver/editar histórico, usar la versión histórica
    const vHist = this.versionHistorica();
    if (vHist?.verJson) {
      const def = this.extraerDefinicionVersion(vHist);
      if (def.secciones.length > 0) {
        return def.secciones.sort((a, b) => a.orden - b.orden);
      }
    }

    // 3. Fallback a la versión vigente
    const vVig = this.versionVigente();
    if (vVig?.verJson) {
      const def = this.extraerDefinicionVersion(vVig);
      if (def.secciones.length > 0) {
        return def.secciones.sort((a, b) => a.orden - b.orden);
      }
    }
    return [];
  });

  readonly secciones = computed(() => {
    const versionVigente = this.versionVigente();
    if (versionVigente?.verJson) {
      const def = this.extraerDefinicionVersion(versionVigente);
      if (def.secciones.length > 0) {
        return def.secciones.sort((a, b) => a.orden - b.orden);
      }
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
    if (this.modalVerAbierto()) {
      event.preventDefault();
      this.cerrarModalVer();
    } else if (this.modalEditarAbierto()) {
      event.preventDefault();
      this.cerrarModalEditar();
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
    this.limpiarAutoDismiss();
    if (this.timerDebounceBuscar) {
      clearTimeout(this.timerDebounceBuscar);
    }
  }

  seleccionarTab(tab: TabMatrices): void {
    this.tab.set(tab);
    this.limpiarAlertas();
    this.globalState.limpiarError();

    if (tab === 'consolidado') this.cargarConsolidado();
    if (tab === 'plantillas') {
      this.cargarFamilias();
    }
  }

  onKeydownTab(event: KeyboardEvent, tabActual: TabMatrices): void {
    const tabs: TabMatrices[] = ['evaluaciones', 'captura', 'consolidado', 'plantillas'];
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
      return;
    }

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
        if (this.tab() === 'plantillas') {
          this.cargarVersiones();
        }
      },
      error: () => this.familias.set([])
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

  abrirModalCrearFamilia(): void {
    this.modoEdicionFamilia.set(false);
    this.familiaIdEditando = 0;
    this.nuevaFamiliaCodigo = '';
    this.nuevaFamiliaNombre = '';
    this.nuevaFamiliaDescripcion = '';
    this.nuevaFamiliaActivo = true;
    this.errorModal.set(null);
    this.globalState.limpiarError();
    this.modalFamiliaAbierto.set(true);
  }

  abrirModalEditarFamilia(fam: FamiliaFormularioDto): void {
    this.modoEdicionFamilia.set(true);
    this.familiaIdEditando = fam.famId;
    this.nuevaFamiliaCodigo = fam.famCodigo;
    this.nuevaFamiliaNombre = fam.famNombre;
    this.nuevaFamiliaDescripcion = fam.famDescripcion ?? '';
    this.nuevaFamiliaActivo = fam.famActivo;
    this.errorModal.set(null);
    this.globalState.limpiarError();
    this.modalFamiliaAbierto.set(true);
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
          this.mostrarMensaje(`Familia «${this.nuevaFamiliaNombre}» actualizada correctamente.`);
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

  desactivarFamilia(fam: FamiliaFormularioDto): void {
    this.guardando.set(true);
    this.limpiarAlertas();
    this.service.desactivarFamiliaFormulario(fam.famId).subscribe({
      next: () => {
        this.guardando.set(false);
        this.globalState.limpiarError();
        this.mostrarMensaje(`Familia «${fam.famCodigo}» desactivada correctamente.`);
        this.cargarFamilias();
      },
      error: error => {
        this.guardando.set(false);
        this.mostrarError(this.obtenerMensajeError(error, 'No se pudo desactivar la familia. Verifique que no tenga versiones vigentes.'));
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

  alCambiarFiltroBuscar(valor: string): void {
    this.filtroBuscar.set(valor);
    this.pagina.set(1);
    if (this.timerDebounceBuscar) {
      clearTimeout(this.timerDebounceBuscar);
    }
    this.timerDebounceBuscar = setTimeout(() => {
      this.cargarEvaluaciones();
    }, 300);
  }

  alCambiarFiltroEstado(valor: string): void {
    this.filtroEstado.set(valor);
    this.pagina.set(1);
    this.cargarEvaluaciones();
  }

  cambiarRegistrosPorPagina(cantidad: number): void {
    this.registrosPorPagina.set(Number(cantidad));
    this.pagina.set(1);
    this.cargarEvaluaciones();
  }

  cambiarPagina(nuevaPagina: number): void {
    if (nuevaPagina < 1 || (this.totalPaginas() > 0 && nuevaPagina > this.totalPaginas())) {
      return;
    }
    this.pagina.set(nuevaPagina);
    this.cargarEvaluaciones();
  }

  limpiarFiltrosEvaluaciones(): void {
    if (this.timerDebounceBuscar) {
      clearTimeout(this.timerDebounceBuscar);
    }
    this.filtroBuscar.set('');
    this.filtroEstado.set('');
    this.pagina.set(1);
    this.cargarEvaluaciones();
  }

  cargarEvaluaciones(): void {
    this.cargandoEvaluaciones.set(true);
    this.errorEvaluaciones.set(null);
    this.service.listarEvaluaciones({
      buscar: this.filtroBuscar().trim() || undefined,
      estado: this.filtroEstado().trim() || undefined,
      pagina: this.pagina(),
      registrosPorPagina: this.registrosPorPagina()
    }).subscribe({
      next: paginado => {
        const items = Array.isArray(paginado?.items) ? paginado.items : [];
        this.evaluaciones.set(items);
        this.totalRegistros.set(Number.isFinite(paginado?.totalRegistros) ? Math.max(0, paginado.totalRegistros) : 0);
        this.totalPaginas.set(Number.isFinite(paginado?.totalPaginas) ? Math.max(0, paginado.totalPaginas) : 0);
        this.cargandoEvaluaciones.set(false);
      },
      error: error => {
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

  actualizarRespuesta(campo: CampoFormulario, valor: string | number | boolean | null): void {
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

  valorRespuesta(campo: CampoFormulario): string | number | boolean | null {
    return this.respuestas()[campo.clave] ?? null;
  }

  opcionesCatalogo(campo: CampoFormulario): Array<{ codigo: string; valor: string }> {
    if (!campo.codigoCatalogo) return [];

    const met = this.modalVerAbierto() || this.modalEditarAbierto()
      ? this.metodologiaHistorica() ?? this.metodologia()
      : this.metodologia();

    return met?.catalogos
      .find(catalogo => catalogo.codigo === campo.codigoCatalogo)
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

  // --- MODAL: NUEVA EVALUACIÓN ---
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

  // --- MODAL: VER DETALLE FRESCO ---
  abrirModalVer(resumen: EvaluacionRiesgoResumenDto): void {
    this.limpiarAlertas();
    this.cargando.set(true);
    this.evaluacionResumenSeleccionada.set(resumen);

    // 1. GET /evaluaciones/{id} para detalle fresco
    this.service.obtenerEvaluacion(resumen.evaId).subscribe({
      next: detalle => {
        this.evaluacionSeleccionada.set(detalle);
        this.riesgoId.set(detalle.evaRiesgoId);
        this.respuestas.set(this.parsearRespuestas(detalle.evaDataJson));

        // 2. Cargar metodología y versión exacta de evaVersionId
        this.service.metodologiaPorVersion(detalle.evaVersionId).subscribe({
          next: met => {
            this.metodologiaHistorica.set(met);
            this.service.obtenerFamiliaFormularioPorId(1).subscribe({
              next: () => {
                // Obtenemos historial o versión si es necesario
                this.cargando.set(false);
                this.modalVerAbierto.set(true);
              },
              error: () => {
                this.cargando.set(false);
                this.modalVerAbierto.set(true);
              }
            });
          },
          error: () => {
            // Fallback a metodología vigente si fallara
            this.metodologiaHistorica.set(this.metodologia());
            this.cargando.set(false);
            this.modalVerAbierto.set(true);
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

  // --- MODAL: EDITAR (SOLO BORRADOR) ---
  editarEvaluacion(evaluacion: EvaluacionRiesgoResumenDto): void {
    if ((evaluacion.estado || '').toUpperCase() !== 'BORRADOR') {
      this.mostrarError('Solo se permite editar evaluaciones en estado BORRADOR.');
      return;
    }

    this.limpiarAlertas();
    this.cargando.set(true);
    this.evaluacionResumenSeleccionada.set(evaluacion);

    // 1. GET /evaluaciones/{id} para detalle fresco
    this.service.obtenerEvaluacion(evaluacion.evaId).subscribe({
      next: detalle => {
        this.evaluacionSeleccionada.set(detalle);
        this.riesgoId.set(detalle.evaRiesgoId);
        this.respuestas.set(this.parsearRespuestas(detalle.evaDataJson));

        // 2. Cargar metodología histórica exacta
        this.service.metodologiaPorVersion(detalle.evaVersionId).subscribe({
          next: met => {
            this.metodologiaHistorica.set(met);
            this.cargando.set(false);
            this.modalEditarAbierto.set(true);
          },
          error: () => {
            this.metodologiaHistorica.set(this.metodologia());
            this.cargando.set(false);
            this.modalEditarAbierto.set(true);
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

  // --- MODAL: SEGUIMIENTO ---
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
        this.guardando.set(false);
        this.globalState.limpiarError();
        this.mostrarMensaje(esEdicion ? 'Evaluación actualizada correctamente.' : 'Evaluación creada correctamente.');
        this.modalEditarAbierto.set(false);
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
        // Actualizamos estado en el resumen seleccionado localmente
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
    this.versionEditando.set(version);
    this.soloLecturaDefinicion.set(soloLectura || version.verVigente);
    this.definicionTecnica = this.formatearDefinicion(version.verJson);
  }

  guardarDefinicion(): void {
    const version = this.versionEditando();
    if (!version) return;

    try {
      JSON.parse(this.definicionTecnica);
    } catch (error) {
      const detalle = error instanceof SyntaxError ? error.message : '';
      const sufijo = detalle ? `: ${detalle}` : '.';
      this.mostrarError(`La definición JSON no es válida${sufijo}`);
      return;
    }

    this.guardando.set(true);
    this.service.actualizarBorradorFormulario(version.verId, this.definicionTecnica).subscribe({
      next: () => {
        this.guardando.set(false);
        this.versionEditando.set(null);
        this.globalState.limpiarError();
        this.mostrarMensaje('Definición del formulario actualizada correctamente.');
        this.cargarVersiones();
      },
      error: error => {
        this.guardando.set(false);
        this.mostrarError(this.obtenerMensajeError(error, 'No se pudo actualizar la definición del formulario.'));
      }
    });
  }

  publicarVersion(version: VersionFormularioDto): void {
    import('sweetalert2').then(Swal => {
      Swal.default.fire({
        title: '¿Publicar versión?',
        text: `¿Desea publicar la versión ID #${version.verId} (${version.verCodigo} v${version.verVersion})? Esta acción creará la versión oficial de la plantilla.`,
        icon: 'question',
        showCancelButton: true,
        confirmButtonColor: '#2563eb',
        cancelButtonColor: '#6b7280',
        confirmButtonText: 'Sí, publicar',
        cancelButtonText: 'Cancelar'
      }).then((result) => {
        if (result.isConfirmed) {
          this.guardando.set(true);
          this.service.publicarVersionFormulario(version.verId).subscribe({
            next: () => {
              this.guardando.set(false);
              this.globalState.limpiarError();
              this.mostrarMensaje('Versión publicada correctamente.');
              this.cargarVersiones();
              this.cargarVersionVigentePorFamilia(this.familiaSeleccionada());
            },
            error: error => {
              this.guardando.set(false);
              this.mostrarError(this.obtenerMensajeError(error, 'No se pudo publicar la versión del formulario.'));
            }
          });
        }
      });
    });
  }

  cambiarVigenciaVersion(version: VersionFormularioDto, vigente: boolean): void {
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
    if (version.verVigente) {
      this.mostrarError('No se puede eliminar el formulario activo de la familia.');
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
  }

  guardarNuevoFormulario(): void {
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
    if (!version?.verJson) {
      return { codigoFormulario: '', nombreFormulario: '', secciones: [] };
    }

    try {
      const definicion = JSON.parse(version.verJson) as Partial<DefinicionFormularioEditable>;
      return {
        codigoFormulario: definicion.codigoFormulario ?? version.verCodigo,
        nombreFormulario: definicion.nombreFormulario ?? version.verCodigo,
        secciones: Array.isArray(definicion.secciones)
          ? definicion.secciones.map((seccion, indice) => ({
              clave: seccion.clave || `seccion_${indice + 1}`,
              titulo: seccion.titulo || `Sección ${indice + 1}`,
              orden: Number(seccion.orden ?? indice + 1),
              columnasPorFila: Number(seccion.columnasPorFila ?? 2),
              campos: Array.isArray(seccion.campos) ? seccion.campos.map(c => ({
                ...c,
                anchoColumnas: Number(c.anchoColumnas ?? 1),
                formula: c.formula || undefined
              })) : []
            }))
          : [],
        reglas: Array.isArray(definicion.reglas) ? definicion.reglas : []
      };
    } catch {
      return { codigoFormulario: version.verCodigo, nombreFormulario: version.verCodigo, secciones: [] };
    }
  }

  private parsearRespuestas(contenido: string): RespuestasFormulario {
    try {
      const valor = JSON.parse(contenido);
      return valor && typeof valor === 'object' && !Array.isArray(valor) ? valor : {};
    } catch {
      return {};
    }
  }

  private formatearDefinicion(contenido: string): string {
    try {
      return JSON.stringify(JSON.parse(contenido), null, 2);
    } catch {
      return contenido;
    }
  }

  private tieneValor(valor: unknown): boolean {
    if (valor === null || valor === undefined) return false;
    if (typeof valor === 'object') return Object.keys(valor).length > 0;
    if (typeof valor === 'string') return valor.trim() !== '';
    if (typeof valor === 'number') return !Number.isNaN(valor);
    return typeof valor === 'boolean';
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
