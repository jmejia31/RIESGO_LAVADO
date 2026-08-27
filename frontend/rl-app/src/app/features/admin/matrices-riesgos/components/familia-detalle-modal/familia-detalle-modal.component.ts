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

interface ActividadFamiliaViewModel {
  id: string;
  titulo: string;
  descripcion: string;
  fecha: string;
  usuario: string;
  tono: 'green' | 'blue' | 'purple' | 'amber';
}

@Component({
  selector: 'app-familia-detalle-modal',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './familia-detalle-modal.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class FamiliaDetalleModalComponent implements OnChanges, AfterViewInit, OnDestroy {
  private readonly service = inject(MatricesRiesgosService);
  private readonly auditoriaService = inject(AuditoriaService);
  private readonly authService = inject(AuthService);
  private readonly host = inject(ElementRef<HTMLElement>);

  private suscripcionDetalle: Subscription | null = null;
  private suscripcionVersiones: Subscription | null = null;
  private suscripcionActividad: Subscription | null = null;
  private secuenciaCarga = 0;
  private focoAnterior: HTMLElement | null = null;

  @Input({ required: true }) familiaId = 0;
  @Input() familiaReferencia: FamiliaFormularioDto | null = null;

  @Output() readonly cerrar = new EventEmitter<void>();
  @Output() readonly gestionarVersiones = new EventEmitter<FamiliaFormularioDto>();
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
  readonly errorActividad = signal<string | null>(null);
  readonly noEncontrada = signal(false);
  readonly esAdministrador = computed(() => this.authService.tieneRol(['ADMIN', 'ADMINISTRADOR']));

  readonly versionesOrdenadas = computed(() =>
    [...this.versiones()].sort((a, b) => b.verVersion - a.verVersion)
  );

  readonly actividadReciente = computed<ActividadFamiliaViewModel[]>(() => {
    const fam = this.detalle();
    if (!fam) return [];

    const eventos: ActividadFamiliaViewModel[] = [];
    const auditoriasFamilia = this.auditoria()
      .filter(item => String(item.registroId) === String(fam.famId));

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
      .slice(0, 4);
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

  enfocarContexto(): void {
    this.botonCerrar?.nativeElement.focus();
  }

  reintentarVersiones(): void {
    const familia = this.detalle();
    if (familia) this.cargarVersiones(familia);
  }

  solicitarGestionVersiones(): void {
    const familia = this.detalle();
    if (familia) this.gestionarVersiones.emit(familia);
  }

  solicitarEdicion(): void {
    const familia = this.detalle();
    if (familia) this.editarFamilia.emit(familia);
  }

  solicitarNuevaVersion(): void {
    const familia = this.detalle();
    if (familia) this.nuevaVersion.emit(familia);
  }

  solicitarVerVersion(version: VersionFormularioDto): void {
    const familia = this.detalle();
    if (familia) this.verDefinicion.emit({ familia, version, modoEdicion: false });
  }

  solicitarEditarVersion(version: VersionFormularioDto): void {
    const familia = this.detalle();
    if (familia) this.verDefinicion.emit({ familia, version, modoEdicion: true });
  }

  solicitarPublicacion(version: VersionFormularioDto): void {
    if (!this.operando() && version.verEstado === 'DRAFT' && !version.verVigente) {
      this.publicarVersionSolicitada.emit(version);
    }
  }

  solicitarCambioVigencia(version: VersionFormularioDto): void {
    if (!this.operando() && version.verEstado === 'PUBLISHED') {
      this.cambiarVigenciaSolicitada.emit({ version, vigente: !version.verVigente });
    }
  }

  solicitarEliminacion(version: VersionFormularioDto): void {
    if (!this.operando() && version.verEstado === 'DRAFT' && !version.verVigente) {
      this.eliminarVersionSolicitada.emit(version);
    }
  }

  clonarVersion(version: VersionFormularioDto): void {
    if (this.operando()) return;
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

  cambiarEstadoFamilia(): void {
    const familia = this.detalle();
    if (!familia || !this.esAdministrador() || this.operando()) return;

    const accion = familia.famActivo ? 'desactivar' : 'activar';
    const confirmacion = window.confirm(
      familia.famActivo
        ? `¿Desactivar la familia ${familia.famNombre}? Sus versiones e historial se conservarán.`
        : `¿Activar la familia ${familia.famNombre}?`
    );
    if (!confirmacion) return;

    this.operando.set(true);
    this.error.set(null);
    const operacion = familia.famActivo
      ? this.service.desactivarFamiliaFormulario(familia.famId)
      : this.service.activarFamiliaFormulario(familia.famId);

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

  private cargarActividad(familia: FamiliaFormularioDto): void {
    this.suscripcionActividad?.unsubscribe();
    this.auditoria.set([]);
    this.errorActividad.set(null);

    if (!this.esAdministrador()) {
      this.cargandoActividad.set(false);
      return;
    }

    this.cargandoActividad.set(true);
    this.suscripcionActividad = this.auditoriaService.getBitacora({
      pagina: 1,
      limite: 100,
      tabla: 'RL_MR_FAMILIAS_FORMULARIO'
    }).subscribe({
      next: respuesta => {
        this.cargandoActividad.set(false);
        this.auditoria.set((respuesta?.datos || []).filter(item => String(item.registroId) === String(familia.famId)));
      },
      error: () => {
        this.cargandoActividad.set(false);
        this.auditoria.set([]);
        this.errorActividad.set('La actividad de auditoría no está disponible para esta consulta.');
      }
    });
  }

  private mapearAuditoria(item: AuditoriaDto, accion: string): ActividadFamiliaViewModel | null {
    const usuario = item.usrEmail?.trim() || (item.usrId ? `Usuario #${item.usrId}` : 'Usuario institucional');

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
