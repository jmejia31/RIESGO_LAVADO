import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatricesRiesgosService } from '../../data-access/matrices-riesgos.service';
import {
  CampoFormulario,
  DefinicionFormularioEditable,
  EvaluacionRiesgoDto,
  FlujoEvaluacionDto,
  MetodologiaFormulario,
  RespuestasFormulario,
  RiesgoReporteFila,
  VersionFormularioDto
} from '../../models/matrices-riesgos.models';
import { RiesgoDto } from '../../models/matrices-riesgos-fase11.models';

type TabMatrices = 'evaluaciones' | 'captura' | 'consolidado' | 'plantillas';

@Component({
  selector: 'app-matrices-riesgos',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './matrices-riesgos.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class MatricesRiesgosComponent implements OnInit {
  private readonly service = inject(MatricesRiesgosService);

  readonly tab = signal<TabMatrices>('evaluaciones');
  readonly cargando = signal(false);
  readonly guardando = signal(false);
  readonly error = signal<string | null>(null);
  readonly mensaje = signal<string | null>(null);

  readonly metodologia = signal<MetodologiaFormulario | null>(null);
  readonly versionVigente = signal<VersionFormularioDto | null>(null);
  readonly versiones = signal<VersionFormularioDto[]>([]);
  readonly riesgos = signal<RiesgoDto[]>([]);
  readonly evaluaciones = signal<EvaluacionRiesgoDto[]>([]);
  readonly evaluacionSeleccionada = signal<EvaluacionRiesgoDto | null>(null);
  readonly flujos = signal<FlujoEvaluacionDto[]>([]);
  readonly consolidado = signal<RiesgoReporteFila[]>([]);

  readonly pagina = signal(1);
  readonly registrosPorPagina = signal(20);
  readonly filtroBuscar = signal('');
  readonly filtroEstado = signal('');
  readonly riesgoId = signal(0);
  readonly respuestas = signal<RespuestasFormulario>({});

  motivoTransicion = '';
  nuevoEstado = 'EN_REVISION';
  archivoEvidencia: File | null = null;

  readonly versionEditando = signal<VersionFormularioDto | null>(null);
  definicionTecnica = '';

  readonly secciones = computed(() => {
    const metodologia = this.metodologia();
    if (metodologia?.secciones?.length) {
      return [...metodologia.secciones].sort((a, b) => a.orden - b.orden);
    }

    return this.extraerDefinicionVersion(this.versionVigente()).secciones;
  });

  readonly totalCampos = computed(() =>
    this.secciones().reduce((total, seccion) => total + seccion.campos.length, 0)
  );

  readonly totalCompletados = computed(() => {
    const respuestas = this.respuestas();
    return this.secciones()
      .flatMap(seccion => seccion.campos)
      .filter(campo => this.tieneValor(respuestas[campo.clave]))
      .length;
  });

  readonly puedeGuardar = computed(() => {
    const respuestas = this.respuestas();
    if (this.riesgoId() <= 0 || !this.versionVigente()) {
      return false;
    }

    return this.secciones()
      .flatMap(seccion => seccion.campos)
      .filter(campo => campo.obligatorio)
      .every(campo => this.tieneValor(respuestas[campo.clave]));
  });

  ngOnInit(): void {
    this.cargarRiesgos();
    this.cargarModulo();
  }

  seleccionarTab(tab: TabMatrices): void {
    this.tab.set(tab);
    this.error.set(null);
    this.mensaje.set(null);

    if (tab === 'consolidado') this.cargarConsolidado();
    if (tab === 'plantillas') this.cargarVersiones();
  }

  cargarModulo(): void {
    this.cargando.set(true);
    this.error.set(null);

    this.service.obtenerVersionVigenteFormulario().subscribe({
      next: version => {
        this.versionVigente.set(version);
        this.inicializarRespuestas();
        this.cargarMetodologia();
        this.cargarEvaluaciones();
      },
      error: error => this.finalizarConError(error, 'No se pudo cargar la versión vigente del formulario.')
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
        this.cargando.set(false);
      },
      error: error => this.finalizarConError(error, 'No se pudo cargar la metodología dinámica vigente.')
    });
  }

  cargarEvaluaciones(): void {
    this.cargando.set(true);
    this.service.listarEvaluaciones({
      buscar: this.filtroBuscar().trim() || undefined,
      estado: this.filtroEstado().trim() || undefined,
      pagina: this.pagina(),
      registrosPorPagina: this.registrosPorPagina()
    }).subscribe({
      next: evaluaciones => {
        this.evaluaciones.set(evaluaciones);
        this.cargando.set(false);
      },
      error: error => this.finalizarConError(error, 'No se pudieron consultar las evaluaciones.')
    });
  }

  cargarConsolidado(): void {
    this.cargando.set(true);
    this.service.obtenerConsolidado().subscribe({
      next: filas => {
        this.consolidado.set(filas);
        this.cargando.set(false);
      },
      error: error => this.finalizarConError(error, 'No se pudo cargar la matriz consolidada.')
    });
  }

  descargarConsolidado(formato: 'excel' | 'pdf'): void {
    this.error.set(null);
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
      error: error => this.error.set(this.obtenerMensajeError(error, `No se pudo generar el reporte ${formato.toUpperCase()}.`))
    });
  }

  cargarVersiones(): void {
    this.cargando.set(true);
    this.service.listarHistorialVersionesFormulario().subscribe({
      next: versiones => {
        this.versiones.set(versiones);
        this.cargando.set(false);
      },
      error: error => this.finalizarConError(error, 'No se pudo cargar el historial de formularios.')
    });
  }

  actualizarRespuesta(campo: CampoFormulario, valor: string | number | boolean | null): void {
    this.respuestas.update(actuales => ({ ...actuales, [campo.clave]: valor }));
  }

  valorRespuesta(campo: CampoFormulario): string | number | boolean | null {
    return this.respuestas()[campo.clave] ?? null;
  }

  opcionesCatalogo(campo: CampoFormulario): Array<{ codigo: string; valor: string }> {
    if (!campo.codigoCatalogo) return [];

    return this.metodologia()?.catalogos
      .find(catalogo => catalogo.codigo === campo.codigoCatalogo)
      ?.elementos
      .slice()
      .sort((a, b) => a.orden - b.orden) ?? [];
  }

  nuevaEvaluacion(): void {
    this.evaluacionSeleccionada.set(null);
    this.riesgoId.set(0);
    this.inicializarRespuestas();
    this.tab.set('captura');
  }

  editarEvaluacion(evaluacion: EvaluacionRiesgoDto): void {
    this.evaluacionSeleccionada.set(evaluacion);
    this.riesgoId.set(evaluacion.evaRiesgoId);
    this.respuestas.set(this.parsearRespuestas(evaluacion.evaDataJson));
    this.tab.set('captura');
    this.cargarFlujos(evaluacion.evaId);
  }

  guardarEvaluacion(): void {
    const version = this.versionVigente();
    if (!version || !this.puedeGuardar()) {
      this.error.set('Complete el riesgo y todos los campos obligatorios antes de guardar.');
      return;
    }

    this.guardando.set(true);
    this.error.set(null);
    const actual = this.evaluacionSeleccionada();
    const dto: EvaluacionRiesgoDto = {
      evaId: actual?.evaId ?? 0,
      evaRiesgoId: this.riesgoId(),
      evaVersionId: version.verId,
      evaEstado: actual?.evaEstado ?? 'BORRADOR',
      evaDataJson: JSON.stringify(this.respuestas()),
      evaDataCalcJson: actual?.evaDataCalcJson ?? '{}',
      evaVri: actual?.evaVri ?? null,
      evaVrr: actual?.evaVrr ?? null,
      evaFechaEval: actual?.evaFechaEval ?? new Date().toISOString(),
      evaUsrEval: actual?.evaUsrEval ?? 0,
      evaVersionRow: actual?.evaVersionRow ?? 1,
      evaActivo: true
    };

    const solicitud = actual
      ? this.service.actualizarEvaluacion(actual.evaId, dto)
      : this.service.crearEvaluacion(dto);

    solicitud.subscribe({
      next: () => {
        this.mensaje.set(actual ? 'Evaluación actualizada correctamente.' : 'Evaluación creada correctamente.');
        this.guardando.set(false);
        this.tab.set('evaluaciones');
        this.cargarEvaluaciones();
      },
      error: error => {
        this.guardando.set(false);
        this.error.set(this.obtenerMensajeError(error, 'No se pudo guardar la evaluación.'));
      }
    });
  }

  transicionarEvaluacion(evaluacion: EvaluacionRiesgoDto): void {
    if (!this.nuevoEstado.trim()) {
      this.error.set('Seleccione un estado de destino.');
      return;
    }

    this.guardando.set(true);
    this.service.transicionarEvaluacion(evaluacion.evaId, this.nuevoEstado, this.motivoTransicion).subscribe({
      next: () => {
        this.mensaje.set('Estado actualizado correctamente.');
        this.guardando.set(false);
        this.motivoTransicion = '';
        this.cargarEvaluaciones();
        this.cargarFlujos(evaluacion.evaId);
      },
      error: error => {
        this.guardando.set(false);
        this.error.set(this.obtenerMensajeError(error, 'No se pudo realizar la transición.'));
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
      this.error.set('Seleccione un archivo de evidencia.');
      return;
    }

    this.guardando.set(true);
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
            this.mensaje.set('Evidencia cargada y vinculada correctamente.');
          },
          error: error => {
            this.service.eliminarEvidenciaHuerfana(evidencia.eviId).subscribe();
            this.guardando.set(false);
            this.error.set(this.obtenerMensajeError(error, 'No se pudo vincular la evidencia.'));
          }
        });
      },
      error: error => {
        this.guardando.set(false);
        this.error.set(this.obtenerMensajeError(error, 'No se pudo cargar la evidencia.'));
      }
    });
  }

  clonarVersion(version: VersionFormularioDto): void {
    this.guardando.set(true);
    this.service.clonarVersionFormulario(version.verId).subscribe({
      next: () => {
        this.guardando.set(false);
        this.mensaje.set('Versión clonada como borrador.');
        this.cargarVersiones();
      },
      error: error => {
        this.guardando.set(false);
        this.error.set(this.obtenerMensajeError(error, 'No se pudo clonar la versión.'));
      }
    });
  }

  abrirDefinicion(version: VersionFormularioDto): void {
    this.versionEditando.set(version);
    this.definicionTecnica = this.formatearDefinicion(version.verJson);
  }

  guardarDefinicion(): void {
    const version = this.versionEditando();
    if (!version) return;

    try {
      JSON.parse(this.definicionTecnica);
    } catch {
      this.error.set('La definición técnica no tiene una estructura válida.');
      return;
    }

    this.guardando.set(true);
    this.service.actualizarBorradorFormulario(version.verId, this.definicionTecnica).subscribe({
      next: () => {
        this.guardando.set(false);
        this.versionEditando.set(null);
        this.mensaje.set('Definición del formulario actualizada.');
        this.cargarVersiones();
      },
      error: error => {
        this.guardando.set(false);
        this.error.set(this.obtenerMensajeError(error, 'No se pudo actualizar la definición.'));
      }
    });
  }

  publicarVersion(version: VersionFormularioDto): void {
    this.guardando.set(true);
    this.service.publicarVersionFormulario(version.verId).subscribe({
      next: () => {
        this.guardando.set(false);
        this.mensaje.set('Versión publicada correctamente.');
        this.cargarVersiones();
        this.cargarModulo();
      },
      error: error => {
        this.guardando.set(false);
        this.error.set(this.obtenerMensajeError(error, 'No se pudo publicar la versión.'));
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
              campos: Array.isArray(seccion.campos) ? seccion.campos : []
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
    return valor !== null && valor !== undefined && `${valor}`.trim() !== '';
  }

  private finalizarConError(error: unknown, mensaje: string): void {
    this.cargando.set(false);
    this.error.set(this.obtenerMensajeError(error, mensaje));
  }

  private obtenerMensajeError(error: unknown, mensaje: string): string {
    const respuesta = error as { error?: { mensaje?: string }; message?: string };
    return respuesta?.error?.mensaje || respuesta?.message || mensaje;
  }
}
