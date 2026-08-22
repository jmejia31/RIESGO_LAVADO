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
import { MatricesRiesgosService } from '../../data-access/matrices-riesgos.service';
import { FamiliaFormularioDto, VersionFormularioDto } from '../../models/matrices-riesgos.models';

@Component({
  selector: 'app-familia-detalle-modal',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './familia-detalle-modal.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class FamiliaDetalleModalComponent implements OnChanges, AfterViewInit, OnDestroy {
  private readonly service = inject(MatricesRiesgosService);
  private readonly authService = inject(AuthService);
  private readonly host = inject(ElementRef<HTMLElement>);

  private suscripcionDetalle: Subscription | null = null;
  private suscripcionVersiones: Subscription | null = null;
  private secuenciaCarga = 0;
  private focoAnterior: HTMLElement | null = null;

  @Input({ required: true }) familiaId = 0;
  @Input() familiaReferencia: FamiliaFormularioDto | null = null;

  @Output() readonly cerrar = new EventEmitter<void>();
  @Output() readonly gestionarVersiones = new EventEmitter<FamiliaFormularioDto>();
  @Output() readonly editarFamilia = new EventEmitter<FamiliaFormularioDto>();
  @Output() readonly nuevaVersion = new EventEmitter<FamiliaFormularioDto>();
  @Output() readonly verDefinicion = new EventEmitter<{ familia: FamiliaFormularioDto; version: VersionFormularioDto }>();

  @ViewChild('botonCerrar') botonCerrar?: ElementRef<HTMLButtonElement>;

  readonly detalle = signal<FamiliaFormularioDto | null>(null);
  readonly versiones = signal<VersionFormularioDto[]>([]);
  readonly cargando = signal(false);
  readonly cargandoVersiones = signal(false);
  readonly error = signal<string | null>(null);
  readonly errorVersiones = signal<string | null>(null);
  readonly noEncontrada = signal(false);
  readonly esAdministrador = computed(() => this.authService.tieneRol(['ADMIN', 'ADMINISTRADOR']));

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['familiaId'] && this.familiaId > 0) {
      this.cargarDetalle();
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

  reintentarVersiones(): void {
    const familia = this.detalle();
    if (!familia) return;
    this.cargarVersiones(familia, this.secuenciaCarga);
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

  solicitarVerDefinicion(version: VersionFormularioDto): void {
    const familia = this.detalle();
    if (familia) this.verDefinicion.emit({ familia, version });
  }

  manejarKeydown(event: KeyboardEvent): void {
    if (event.key !== 'Tab') return;

    const dialogo = this.host.nativeElement.querySelector('dialog');
    if (!dialogo) return;

    const elementos = Array.from(dialogo.querySelectorAll<HTMLElement>(
      'button:not([disabled]), [href], input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])'
    )).filter(elemento => !elemento.hasAttribute('hidden') && elemento.offsetParent !== null);

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
    this.error.set(null);
    this.errorVersiones.set(null);
    this.noEncontrada.set(false);
    this.cargando.set(true);
    this.cargandoVersiones.set(false);

    this.suscripcionDetalle = this.service.obtenerFamiliaFormularioPorId(this.familiaId).subscribe({
      next: familia => {
        if (!this.esSolicitudVigente(solicitudId)) return;

        if (!familia) {
          this.cargando.set(false);
          this.noEncontrada.set(true);
          return;
        }

        this.detalle.set(familia);
        this.cargando.set(false);
        this.cargarVersiones(familia, solicitudId);
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

  private cargarVersiones(familia: FamiliaFormularioDto, solicitudId: number): void {
    this.suscripcionVersiones?.unsubscribe();
    this.suscripcionVersiones = null;
    this.versiones.set([]);
    this.errorVersiones.set(null);
    this.cargandoVersiones.set(true);

    this.suscripcionVersiones = this.service.listarHistorialVersionesFormulario(familia.famCodigo).subscribe({
      next: versiones => {
        if (!this.esSolicitudVigente(solicitudId)) return;
        this.versiones.set(Array.isArray(versiones) ? versiones : []);
        this.cargandoVersiones.set(false);
      },
      error: error => {
        if (!this.esSolicitudVigente(solicitudId)) return;
        this.cargandoVersiones.set(false);
        this.errorVersiones.set(this.obtenerMensajeError(error, 'No se pudo cargar el historial de versiones de esta familia.'));
      }
    });
  }

  private esSolicitudVigente(solicitudId: number): boolean {
    return solicitudId === this.secuenciaCarga;
  }

  private cancelarSolicitudes(): void {
    this.suscripcionDetalle?.unsubscribe();
    this.suscripcionDetalle = null;
    this.suscripcionVersiones?.unsubscribe();
    this.suscripcionVersiones = null;
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
