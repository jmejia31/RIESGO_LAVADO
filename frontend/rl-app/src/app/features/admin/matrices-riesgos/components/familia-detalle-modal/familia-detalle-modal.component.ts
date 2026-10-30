import {
  AfterViewInit,
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  EventEmitter,
  Input,
  OnChanges,
  OnDestroy,
  Output,
  SimpleChanges,
  ViewChild,
  computed,
  inject,
  signal
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { Subscription } from 'rxjs';
import { AuthService } from '../../../../../core/auth/auth.service';
import { AuditoriaService } from '../../../bitacora/data-access/auditoria.service';
import { AuditoriaDto } from '../../../bitacora/models/auditoria.models';
import { MatricesRiesgosService } from '../../data-access/matrices-riesgos.service';
import { EstadoFormulario, FamiliaFormularioDto, VersionFormularioDto } from '../../models/matrices-riesgos.models';
import { InstitutionalConfirmationService } from '../../../../../shared/confirmation/institutional-confirmation.service';

interface ActividadFamiliaViewModel {
  id: string;
  titulo: string;
  descripcion: string;
  fecha: string;
  usuario: string;
  tono: 'green' | 'blue' | 'purple' | 'amber';
}

type FeedbackVersiones = { tipo: 'success' | 'error'; mensaje: string };

import { ActionIconComponent } from '../../../../../shared/components/action-icon/action-icon.component';

@Component({
  selector: 'app-familia-detalle-modal',
  standalone: true,
  imports: [ActionIconComponent, CommonModule],
  templateUrl: './familia-detalle-modal.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class FamiliaDetalleModalComponent implements OnChanges, AfterViewInit, OnDestroy {
  private readonly service = inject(MatricesRiesgosService);
  private readonly auditoriaService = inject(AuditoriaService);
  private readonly authService = inject(AuthService);
  private readonly confirmation = inject(InstitutionalConfirmationService);
  private readonly host = inject(ElementRef<HTMLElement>);

  private suscripcionDetalle: Subscription | null = null;
  private suscripcionVersiones: Subscription | null = null;
  private suscripcionActividad: Subscription | null = null;
  private secuenciaCarga = 0;
  private secuenciaActividad = 0;
  private focoAnterior: HTMLElement | null = null;

  @Input({ required: true }) familiaId = 0;
  @Input() familiaReferencia: FamiliaFormularioDto | null = null;
  @Input() origen: 'gestor-familias' | 'nueva-evaluacion' = 'gestor-familias';

  @Output() readonly cerrar = new EventEmitter<void>();
  @Output() readonly editarFamilia = new EventEmitter<FamiliaFormularioDto>();
  @Output() readonly nuevaVersion = new EventEmitter<FamiliaFormularioDto>();
  @Output() readonly verDefinicion = new EventEmitter<{ familia: FamiliaFormularioDto; version: VersionFormularioDto; modoEdicion: boolean }>();
  @Output() readonly publicarVersionSolicitada = new EventEmitter<VersionFormularioDto>();
  @Output() readonly cambiarVigenciaSolicitada = new EventEmitter<{ version: VersionFormularioDto; vigente: boolean }>();
  @Output() readonly eliminarVersionSolicitada = new EventEmitter<VersionFormularioDto>();

  @ViewChild('botonCerrar') botonCerrar?: ElementRef<HTMLButtonElement>;

  readonly detalle = signal<FamiliaFormularioDto | null>(null);
  readonly versiones = signal<VersionFormularioDto[]>([]);
  readonly auditoria = signal<AuditoriaDto[]>([]);
  readonly cargando = signal(false);
  readonly cargandoVersiones = signal(false);
  readonly cargandoActividad = signal(false);
  readonly operando = signal(false);
  readonly error = signal<string | null>(null);
  readonly errorVersiones = signal<string | null>(null);
  readonly feedbackVersiones = signal<FeedbackVersiones | null>(null);
  readonly errorActividad = signal<string | null>(null);
  readonly totalActividad = signal(0);
  private paginaActividad = 0;
  readonly mostrandoHistorialCompleto = signal(false);
  readonly noEncontrada = signal(false);
  readonly esAdministrador = computed(() => this.authService.tieneRol(['ADMINISTRADOR']));
  readonly familiaArchivada = computed(() => this.detalle()?.famArchivada === true);
  readonly puedeGestionarFamilia = computed(() => this.esAdministrador() && !this.familiaArchivada());
  readonly etiquetaRegresar = computed(() => this.origen === 'nueva-evaluacion' ? 'Regresar a Nueva Evaluación' : 'Regresar a Familias de Formularios');

  readonly versionesOrdenadas = computed(() =>
    [...this.versiones()].sort((a, b) => b.verVersion - a.verVersion)
  );

  readonly actividadReciente = computed<ActividadFamiliaViewModel[]>(() => {
    const fam = this.detalle();
    if (!fam) return [];

    const eventos: ActividadFamiliaViewModel[] = [];
    // El endpoint /auditoria/familia ya resolvió y autorizó la identidad canónica
    // (FAM_ID) y la histórica (FAM_CODIGO). No volver a filtrar aquí por ID:
    // hacerlo descartaría eventos legítimos registrados con el código histórico.
    const auditoriasFamilia = this.auditoria();

    for (const item of auditoriasFamilia) {
      const accion = (item.accion || '').trim().toUpperCase();
      const evento = this.mapearAuditoria(item, accion);
      if (evento) eventos.push(evento);
    }

    const tieneCreacionAuditada = auditoriasFamilia.some(item => {
      const accion = (item.accion || '').trim().toUpperCase();
      return accion.includes('CREATE') || accion.includes('INSERT') || accion.includes('CREAR');
    });

    if (!tieneCreacionAuditada && fam.famFechaCreacion) {
      eventos.push({
        id: `familia-creada-${fam.famId}`,
        titulo: 'Familia creada',
        descripcion: 'La familia fue creada en el sistema.',
        fecha: fam.famFechaCreacion,
        usuario: 'Registro institucional',
        tono: 'green'
      });
    }

    for (const version of this.versionesOrdenadas()) {
      if (version.verEstado !== 'PUBLISHED') continue;
      eventos.push({
        id: `version-publicada-${version.verId}`,
        titulo: `Versión v${version.verVersion} publicada`,
        descripcion: version.verVigente
          ? `La versión v${version.verVersion} fue publicada y se encuentra vigente.`
          : `La versión v${version.verVersion} fue publicada.`,
        fecha: version.verFechaInicio || version.verFechaCreacion,
        usuario: `Usuario #${version.verUsrCreacion}`,
        tono: 'purple'
      });
    }

    return eventos
      .filter(evento => Boolean(evento.fecha))
      .sort((a, b) => new Date(b.fecha).getTime() - new Date(a.fecha).getTime())
      .slice(0, this.mostrandoHistorialCompleto() ? undefined : 4);
  });

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['familiaId']) {
      if (this.familiaId > 0) {
        this.cargarDetalle();
      } else {
        this.cancelarSolicitudes();
        this.detalle.set(null);
        this.versiones.set([]);
        this.auditoria.set([]);
        this.paginaActividad = 0;
        this.cargando.set(false);
        this.error.set(null);
        this.noEncontrada.set(false);
      }
    }
  }

  ngAfterViewInit(): void {
    this.focoAnterior = document.activeElement instanceof HTMLElement ? document.activeElement : null;
    setTimeout(() => this.botonCerrar?.nativeElement.focus(), 0);
  }

  ngOnDestroy(): void {
    this.secuenciaCarga++;
    this.cancelarSolicitudes();
    const foco = this.focoAnterior;
    if (foco?.isConnected) {
      setTimeout(() => foco.focus(), 0);
    }
  }

  reintentarDetalle(): void {
    this.cargarDetalle();
  }

  refrescar(): void {
    this.cargarDetalle();
  }

  refrescarVersionesDespuesDeAccion(): void {
    const familia = this.detalle();
    if (!familia) return;
    this.cargarVersiones(familia);
    this.cargarActividad(familia);
    this.service.obtenerFamiliaFormularioPorId(this.familiaId).subscribe({
      next: familiaActualizada => this.detalle.set(familiaActualizada)
    });
  }

  mostrarFeedbackVersiones(tipo: FeedbackVersiones['tipo'], mensaje: string): void {
    this.feedbackVersiones.set({ tipo, mensaje });
  }

  cerrarFeedbackVersiones(): void {
    this.feedbackVersiones.set(null);
  }

  finalizarOperacionVersion(): void {
    this.operando.set(false);
  }

  enfocarContexto(): void {
    const intentarEnfocar = (framesRestantes = 30): void => {
      const control = this.botonCerrar?.nativeElement
        ?? this.host.nativeElement.querySelector('dialog') as HTMLElement | null;
      if (!control?.isConnected) return;

      const estilo = window.getComputedStyle(control);
      const disponible = estilo.display !== 'none'
        && estilo.visibility !== 'hidden'
        && control.getClientRects().length > 0
        && !control.closest('[inert], [aria-hidden="true"]');

      if (!disponible) {
        if (framesRestantes > 0) requestAnimationFrame(() => intentarEnfocar(framesRestantes - 1));
        return;
      }

      control.focus({ preventScroll: true });
      if (document.activeElement !== control && framesRestantes > 0) {
        requestAnimationFrame(() => intentarEnfocar(framesRestantes - 1));
      }
    };

    requestAnimationFrame(() => intentarEnfocar());
  }

  reintentarVersiones(): void {
    const familia = this.detalle();
    if (familia) this.cargarVersiones(familia);
  }

  solicitarEdicion(): void {
    const familia = this.detalle();
    if (familia && this.puedeGestionarFamilia()) this.editarFamilia.emit(familia);
  }

  solicitarNuevaVersion(): void {
    const familia = this.detalle();
    if (familia && this.puedeGestionarFamilia()) this.nuevaVersion.emit(familia);
  }

  solicitarVerVersion(version: VersionFormularioDto): void {
    const familia = this.detalle();
    if (familia) this.verDefinicion.emit({ familia, version, modoEdicion: false });
  }

  solicitarEditarVersion(version: VersionFormularioDto): void {
    const familia = this.detalle();
    if (familia && this.puedeGestionarFamilia() && version.verEstado === 'DRAFT' && !version.verVigente) {
      this.verDefinicion.emit({ familia, version, modoEdicion: true });
    }
  }

  solicitarPublicacion(version: VersionFormularioDto): void {
    if (!this.operando() && this.puedeGestionarFamilia() && version.verEstado === 'DRAFT' && !version.verVigente) {
      this.publicarVersionSolicitada.emit(version);
    }
  }

  solicitarCambioVigencia(version: VersionFormularioDto): void {
    if (!this.operando() && this.puedeGestionarFamilia() && version.verEstado === 'PUBLISHED') {
      this.cambiarVigenciaSolicitada.emit({ version, vigente: !version.verVigente });
    }
  }

  solicitarEliminacion(version: VersionFormularioDto): void {
    if (!this.operando() && this.puedeGestionarFamilia() && version.verEstado === 'DRAFT' && !version.verVigente) {
      this.eliminarVersionSolicitada.emit(version);
    }
  }

  clonarVersion(version: VersionFormularioDto): void {
    if (this.operando() || !this.puedeGestionarFamilia()) return;
    this.operando.set(true);
    this.errorVersiones.set(null);

    this.service.clonarVersionFormulario(version.verId).subscribe({
      next: () => {
        this.operando.set(false);
        const familia = this.detalle();
        if (familia) this.cargarVersiones(familia);
      },
      error: error => {
        this.operando.set(false);
        this.errorVersiones.set(this.obtenerMensajeError(error, 'No se pudo clonar la versión seleccionada.'));
      }
    });
  }

  async cambiarEstadoFamilia(): Promise<void> {
    const familia = this.detalle();
    if (!familia || !this.puedeGestionarFamilia() || this.operando()) return;

    const accion = familia.famActivo ? 'desactivar' : 'activar';
    const confirmed = await this.confirmation.confirm({
      title: familia.famActivo ? `¿Desactivar la familia ${familia.famNombre}?` : `¿Activar la familia ${familia.famNombre}?`,
      message: familia.famActivo
        ? 'Sus versiones e historial se conservarán.'
        : 'La familia volverá a estar disponible para nuevas operaciones.',
      icon: familia.famActivo ? 'warning' : 'question',
      confirmLabel: familia.famActivo ? 'Desactivar' : 'Sí, activar',
      cancelLabel: 'Cancelar',
      confirmIcon: familia.famActivo ? 'deactivate' : 'activate',
      confirmVariant: familia.famActivo ? 'danger' : 'success',
      allowEscapeKey: false
    });
    if (!confirmed) return;

    this.operando.set(true);
    this.error.set(null);
    const operacion = familia.famActivo
      ? this.service.desactivarFamiliaFormulario(familia.famId, 'local')
      : this.service.activarFamiliaFormulario(familia.famId, 'local');

    operacion.subscribe({
      next: () => {
        this.operando.set(false);
        this.cargarDetalle();
      },
      error: error => {
        this.operando.set(false);
        this.error.set(this.obtenerMensajeError(error, `No se pudo ${accion} la familia.`));
      }
    });
  }

  etiquetaEstado(estado: EstadoFormulario): string {
    const etiquetas: Record<EstadoFormulario, string> = {
      DRAFT: 'BORRADOR',
      IN_REVIEW: 'EN REVISIÓN',
      APPROVED: 'APROBADA',
      PUBLISHED: 'PUBLICADA',
      RETIRED: 'RETIRADA',
      ARCHIVED: 'ARCHIVADA'
    };
    return etiquetas[estado];
  }

  manejarKeydown(event: KeyboardEvent): void {
    if (event.key === 'Escape') {
      event.preventDefault();
      event.stopPropagation();
      return;
    }

    if (event.key !== 'Tab') return;

    const dialogo = this.host.nativeElement.querySelector('dialog') as HTMLDialogElement | null;
    if (!dialogo) return;

    const selectorFoco = 'button:not([disabled]), [href], input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])';
    const elementos = Array.from(dialogo.querySelectorAll(selectorFoco)).filter(
      (elemento): elemento is HTMLElement => elemento instanceof HTMLElement
        && !elemento.hasAttribute('hidden')
        && elemento.offsetParent !== null
    );

    if (elementos.length === 0) return;

    const primero = elementos[0];
    const ultimo = elementos[elementos.length - 1];
    const activo = document.activeElement;

    if (event.shiftKey && activo === primero) {
      event.preventDefault();
      ultimo.focus();
    } else if (!event.shiftKey && activo === ultimo) {
      event.preventDefault();
      primero.focus();
    }
  }

  private cargarDetalle(): void {
    if (this.familiaId <= 0) return;

    const solicitudId = ++this.secuenciaCarga;
    this.cancelarSolicitudes();
    this.detalle.set(null);
    this.versiones.set([]);
    this.auditoria.set([]);
    this.error.set(null);
    this.errorVersiones.set(null);
    this.errorActividad.set(null);
    this.noEncontrada.set(false);
    this.cargando.set(true);

    this.suscripcionDetalle = this.service.obtenerFamiliaFormularioPorId(this.familiaId).subscribe({
      next: familia => {
        if (!this.esSolicitudVigente(solicitudId)) return;
        this.cargando.set(false);

        if (!familia) {
          this.noEncontrada.set(true);
          return;
        }

        this.detalle.set(familia);
        this.cargarVersiones(familia);
        this.cargarActividad(familia);
      },
      error: error => {
        if (!this.esSolicitudVigente(solicitudId)) return;
        this.cargando.set(false);
        const status = (error as { status?: number })?.status;
        if (status === 404) {
          this.noEncontrada.set(true);
          return;
        }
        this.error.set(this.obtenerMensajeError(error, 'No se pudo cargar el detalle de la familia.'));
      }
    });
  }

  private cargarVersiones(familia: FamiliaFormularioDto): void {
    this.suscripcionVersiones?.unsubscribe();
    this.cargandoVersiones.set(true);
    this.errorVersiones.set(null);

    this.suscripcionVersiones = this.service.listarHistorialVersionesFormulario(familia.famCodigo).subscribe({
      next: versiones => {
        this.cargandoVersiones.set(false);
        this.versiones.set(Array.isArray(versiones) ? versiones : []);
      },
      error: error => {
        this.cargandoVersiones.set(false);
        this.versiones.set([]);
        this.errorVersiones.set(this.obtenerMensajeError(error, 'No se pudieron cargar las versiones de la familia.'));
      }
    });
  }

  cargarMasActividad(): void {
    const familia = this.detalle();
    if (!familia || this.cargandoActividad() || this.auditoria().length >= this.totalActividad()) return;
    this.cargarActividad(familia, this.paginaActividad + 1, true);
  }

  mostrarHistorialCompleto(): void {
    this.mostrandoHistorialCompleto.set(true);
    this.cargarMasActividad();
  }

  private cargarActividad(familia: FamiliaFormularioDto, pagina = 1, agregar = false): void {
    this.suscripcionActividad?.unsubscribe();
    const solicitudActividad = ++this.secuenciaActividad;
    if (!agregar) {
      this.auditoria.set([]);
      this.paginaActividad = 0;
    }
    this.errorActividad.set(null);

    if (!this.esAdministrador()) {
      this.cargandoActividad.set(false);
      return;
    }

    this.cargandoActividad.set(true);
    this.suscripcionActividad = this.auditoriaService.getHistorialFamilia({
      familiaId: familia.famId,
      pagina,
      limite: 25
    }, 'local').subscribe({
      next: respuesta => {
        if (solicitudActividad !== this.secuenciaActividad || familia.famId !== this.familiaId) return;
        this.cargandoActividad.set(false);
        this.totalActividad.set(respuesta?.totalRegistros || 0);
        this.paginaActividad = pagina;
        const actuales = agregar ? this.auditoria() : [];
        const porId = new Map(actuales.map(item => [item.audId, item]));
        for (const item of respuesta?.datos || []) porId.set(item.audId, item);
        this.auditoria.set([...porId.values()]);
      },
      error: () => {
        if (solicitudActividad !== this.secuenciaActividad || familia.famId !== this.familiaId) return;
        this.cargandoActividad.set(false);
        this.auditoria.set([]);
        this.errorActividad.set('La actividad de auditoría no está disponible para esta consulta.');
      }
    });
  }

  private mapearAuditoria(item: AuditoriaDto, accion: string): ActividadFamiliaViewModel | null {
    const usuario = item.usrEmail?.trim() || (item.usrId ? `Usuario #${item.usrId}` : 'Usuario institucional');
    let datosNvo: { Evento?: string; Justification?: string; NewActive?: boolean; NewArchived?: boolean } = {};
    try { datosNvo = item.datosNvo ? JSON.parse(item.datosNvo) as typeof datosNvo : {}; } catch { datosNvo = {}; }
    const evento = datosNvo.Evento?.toUpperCase();

    if (evento === 'FAMILY_ARCHIVED' || evento === 'FAMILY_RESTORED' || evento === 'FAMILY_ACTIVATED' || evento === 'FAMILY_DEACTIVATED') {
      const etiquetas: Record<string, [string, string, ActividadFamiliaViewModel['tono']]> = {
        FAMILY_ARCHIVED: ['Familia archivada', 'La familia fue archivada.', 'amber'],
        FAMILY_RESTORED: ['Familia restaurada', 'La familia fue restaurada como inactiva.', 'blue'],
        FAMILY_ACTIVATED: ['Familia activada', 'La familia fue activada.', 'green'],
        FAMILY_DEACTIVATED: ['Familia desactivada', 'La familia fue desactivada.', 'amber']
      };
      const [titulo, descripcion, tono] = etiquetas[evento];
      return { id: `audit-${item.audId}`, titulo, descripcion: datosNvo.Justification ? `${descripcion} Motivo: ${datosNvo.Justification}` : descripcion, fecha: item.fecha, usuario, tono };
    }

    if (accion.includes('CREATE') || accion.includes('INSERT') || accion.includes('CREAR')) {
      return {
        id: `audit-${item.audId}`,
        titulo: 'Familia creada',
        descripcion: 'La familia fue creada en el sistema.',
        fecha: item.fecha,
        usuario,
        tono: 'green'
      };
    }
    if (accion.includes('ACTIV')) {
      return {
        id: `audit-${item.audId}`,
        titulo: accion.includes('DES') ? 'Familia desactivada' : 'Familia activada',
        descripcion: accion.includes('DES') ? 'La familia fue desactivada.' : 'La familia fue activada.',
        fecha: item.fecha,
        usuario,
        tono: 'blue'
      };
    }
    if (accion.includes('UPDATE') || accion.includes('ACTUALIZ')) {
      return {
        id: `audit-${item.audId}`,
        titulo: 'Familia actualizada',
        descripcion: 'La información descriptiva de la familia fue actualizada.',
        fecha: item.fecha,
        usuario,
        tono: 'blue'
      };
    }

    return null;
  }

  private esSolicitudVigente(solicitudId: number): boolean {
    return solicitudId === this.secuenciaCarga;
  }

  private cancelarSolicitudes(): void {
    this.suscripcionDetalle?.unsubscribe();
    this.suscripcionDetalle = null;
    this.suscripcionVersiones?.unsubscribe();
    this.suscripcionVersiones = null;
    this.suscripcionActividad?.unsubscribe();
    this.suscripcionActividad = null;
    this.secuenciaActividad++;
  }

  private obtenerMensajeError(error: unknown, fallback: string): string {
    const respuesta = error as {
      error?: {
        detail?: string;
        mensaje?: string;
        title?: string;
      };
    };

    return respuesta?.error?.detail
      || respuesta?.error?.mensaje
      || respuesta?.error?.title
      || fallback;
  }
}
