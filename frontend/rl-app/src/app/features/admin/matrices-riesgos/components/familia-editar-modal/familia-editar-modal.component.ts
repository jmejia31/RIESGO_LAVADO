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
import { FormsModule } from '@angular/forms';
import { Subscription } from 'rxjs';
import { AuthService } from '../../../../../core/auth/auth.service';
import { MatricesRiesgosService } from '../../data-access/matrices-riesgos.service';
import { FamiliaFormularioDto } from '../../models/matrices-riesgos.models';

export interface FamiliaEstadoCambiadoEvent {
  familia: FamiliaFormularioDto;
  accion: 'ACTIVADA' | 'DESACTIVADA';
}

@Component({
  selector: 'app-familia-editar-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './familia-editar-modal.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class FamiliaEditarModalComponent implements OnChanges, AfterViewInit, OnDestroy {
  private readonly service = inject(MatricesRiesgosService);
  private readonly authService = inject(AuthService);
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);

  private suscripcionDetalle: Subscription | null = null;
  private suscripcionOperacion: Subscription | null = null;
  private secuenciaCarga = 0;
  private focoAnterior: HTMLElement | null = null;
  private nombrePersistido = '';
  private descripcionPersistida = '';

  @Input({ required: true }) familiaId = 0;
  @Input() familiaReferencia: FamiliaFormularioDto | null = null;

  @Output() readonly cerrar = new EventEmitter<void>();
  @Output() readonly guardada = new EventEmitter<FamiliaFormularioDto>();
  @Output() readonly estadoCambiado = new EventEmitter<FamiliaEstadoCambiadoEvent>();
  @Output() readonly eliminada = new EventEmitter<FamiliaFormularioDto>();

  @ViewChild('nombreInput') nombreInput?: ElementRef<HTMLInputElement>;

  readonly detalle = signal<FamiliaFormularioDto | null>(null);
  readonly cargando = signal(false);
  readonly guardando = signal(false);
  readonly operando = signal(false);
  readonly error = signal<string | null>(null);
  readonly mensaje = signal<string | null>(null);
  readonly noEncontrada = signal(false);
  readonly esAdministrador = computed(() => this.authService.tieneRol(['ADMIN', 'ADMINISTRADOR']));

  nombre = '';
  descripcion = '';

  hayCambios(): boolean {
    return this.nombre.trim() !== this.nombrePersistido
      || this.descripcion.trim() !== this.descripcionPersistida;
  }

  readonly puedeDesactivar = computed(() => {
    const familia = this.detalle();
    return Boolean(
      familia
      && this.esAdministrador()
      && familia.famActivo
      && !familia.tieneVersionVigente
      && !this.operando()
      && !this.guardando()
    );
  });

  readonly puedeActivar = computed(() => {
    const familia = this.detalle();
    return Boolean(
      familia
      && this.esAdministrador()
      && !familia.famActivo
      && !this.operando()
      && !this.guardando()
    );
  });

  readonly puedeEliminar = computed(() => {
    const familia = this.detalle();
    return Boolean(
      familia
      && this.esAdministrador()
      && Number(familia.totalVersiones) === 0
      && !this.operando()
      && !this.guardando()
    );
  });

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['familiaId']) {
      if (this.familiaId > 0) {
        this.cargarDetalle(false);
      } else {
        this.cancelarSolicitudes();
        this.detalle.set(null);
        this.cargando.set(false);
        this.noEncontrada.set(false);
        this.error.set(null);
      }
    }
  }

  ngAfterViewInit(): void {
    this.focoAnterior = document.activeElement instanceof HTMLElement ? document.activeElement : null;
    setTimeout(() => this.nombreInput?.nativeElement.focus(), 0);
  }

  ngOnDestroy(): void {
    this.secuenciaCarga++;
    this.cancelarSolicitudes();
    const foco = this.focoAnterior;
    if (foco?.isConnected) {
      setTimeout(() => foco.focus(), 0);
    }
  }

  reintentar(): void {
    this.cargarDetalle(false);
  }

  solicitarCierre(): void {
    if (this.guardando() || this.operando()) return;
    this.cerrar.emit();
  }

  manejarTecladoDialogo(event: KeyboardEvent): void {
    if (event.key === 'Escape') {
      event.preventDefault();
      event.stopPropagation();
      return;
    }

    if (event.key !== 'Tab') return;

    const elementos = Array.from(
      this.host.nativeElement.querySelectorAll<HTMLElement>(
        'button:not([disabled]), input:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])'
      )
    ).filter(elemento => elemento.offsetParent !== null);

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

  guardarCambios(): void {
    const familia = this.detalle();
    if (!familia || this.guardando() || this.operando() || !this.esAdministrador()) return;

    this.nombre = this.nombre.trim();
    this.descripcion = this.descripcion.trim();
    this.error.set(null);
    this.mensaje.set(null);

    if (!this.nombre) {
      this.error.set('El nombre de la familia es obligatorio.');
      return;
    }
    if (this.nombre.length > 150) {
      this.error.set('El nombre no puede superar los 150 caracteres.');
      return;
    }
    if (this.descripcion.length > 500) {
      this.error.set('La descripción no puede superar los 500 caracteres.');
      return;
    }
    if (!this.hayCambios()) return;

    const nombreEnviado = this.nombre;
    const descripcionEnviada = this.descripcion;
    const estadoEnviado = familia.famActivo;

    this.guardando.set(true);
    this.service.actualizarFamiliaFormulario(familia.famId, {
      famNombre: nombreEnviado,
      famDescripcion: descripcionEnviada || null,
      famActivo: estadoEnviado
    }).subscribe({
      next: () => {
        this.suscripcionDetalle?.unsubscribe();
        this.suscripcionDetalle = this.service.obtenerFamiliaFormularioPorId(familia.famId).subscribe({
          next: persistida => {
            this.guardando.set(false);
            const descripcionPersistida = (persistida.famDescripcion ?? '').trim();
            if (
              persistida.famNombre.trim() !== nombreEnviado
              || descripcionPersistida !== descripcionEnviada
              || persistida.famActivo !== estadoEnviado
            ) {
              this.error.set('El servidor respondió al guardado, pero la información recuperada no coincide con los cambios enviados.');
              this.aplicarDetalle(persistida, false);
              return;
            }

            this.aplicarDetalle(persistida, false);
            this.guardada.emit(persistida);
          },
          error: error => {
            this.guardando.set(false);
            this.error.set(this.obtenerMensajeError(
              error,
              'Los cambios fueron enviados, pero no se pudo verificar la persistencia de la familia.'
            ));
          }
        });
      },
      error: error => {
        this.guardando.set(false);
        this.error.set(this.obtenerMensajeError(
          error,
          'No se pudo actualizar la información descriptiva de la familia.'
        ));
      }
    });
  }

  confirmarActivar(): void {
    const familia = this.detalle();
    if (!familia || !this.puedeActivar()) return;

    import('sweetalert2').then(Swal => {
      Swal.default.fire({
        title: 'Activar familia',
        text: `¿Desea activar la familia ${familia.famNombre}?`,
        icon: 'question',
        showCancelButton: true,
        confirmButtonColor: '#059669',
        cancelButtonColor: '#6b7280',
        confirmButtonText: 'Sí, activar',
        cancelButtonText: 'Cancelar',
        focusCancel: true
      }).then(result => {
        if (result.isConfirmed) this.cambiarEstado('ACTIVADA');
      });
    });
  }

  confirmarDesactivar(): void {
    const familia = this.detalle();
    if (!familia) return;

    if (familia.tieneVersionVigente) {
      this.error.set('La familia no puede desactivarse mientras tenga una versión publicada vigente.');
      return;
    }
    if (!this.puedeDesactivar()) return;

    import('sweetalert2').then(Swal => {
      Swal.default.fire({
        title: 'Desactivar familia',
        text: `¿Desea desactivar la familia ${familia.famNombre}? Sus versiones e historial se conservarán.`,
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#d97706',
        cancelButtonColor: '#6b7280',
        confirmButtonText: 'Sí, desactivar',
        cancelButtonText: 'Cancelar',
        focusCancel: true
      }).then(result => {
        if (result.isConfirmed) this.cambiarEstado('DESACTIVADA');
      });
    });
  }

  confirmarEliminar(): void {
    const familia = this.detalle();
    if (!familia) return;

    if (Number(familia.totalVersiones) > 0) {
      this.error.set('La familia contiene versiones y no puede eliminarse.');
      return;
    }
    if (!this.puedeEliminar()) return;

    import('sweetalert2').then(Swal => {
      Swal.default.fire({
        title: 'Eliminar familia',
        text: `¿Desea eliminar permanentemente la familia ${familia.famCodigo}? Esta acción no se puede deshacer.`,
        icon: 'error',
        showCancelButton: true,
        confirmButtonColor: '#dc2626',
        cancelButtonColor: '#6b7280',
        confirmButtonText: 'Eliminar',
        cancelButtonText: 'Cancelar',
        focusCancel: true
      }).then(result => {
        if (!result.isConfirmed) return;

        this.operando.set(true);
        this.error.set(null);
        this.suscripcionOperacion?.unsubscribe();
        this.suscripcionOperacion = this.service.eliminarFamiliaFormulario(familia.famId).subscribe({
          next: () => {
            this.operando.set(false);
            this.eliminada.emit(familia);
          },
          error: error => {
            this.operando.set(false);
            const mensajeError = this.obtenerMensajeError(error, 'No se pudo eliminar la familia.');
            this.cargarDetalle(true);
            this.error.set(mensajeError);
          }
        });
      });
    });
  }

  private cambiarEstado(accion: 'ACTIVADA' | 'DESACTIVADA'): void {
    const familia = this.detalle();
    if (!familia || this.operando() || this.guardando() || !this.esAdministrador()) return;

    this.operando.set(true);
    this.error.set(null);
    this.mensaje.set(null);
    this.suscripcionOperacion?.unsubscribe();
    const solicitud = accion === 'ACTIVADA'
      ? this.service.activarFamiliaFormulario(familia.famId)
      : this.service.desactivarFamiliaFormulario(familia.famId);

    this.suscripcionOperacion = solicitud.subscribe({
      next: () => {
        this.suscripcionDetalle?.unsubscribe();
        this.suscripcionDetalle = this.service.obtenerFamiliaFormularioPorId(familia.famId).subscribe({
          next: actualizada => {
            this.operando.set(false);
            const nombreBorrador = this.nombre;
            const descripcionBorrador = this.descripcion;
            this.aplicarDetalle(actualizada, true, nombreBorrador, descripcionBorrador);
            this.mensaje.set(accion === 'ACTIVADA'
              ? 'Familia activada correctamente.'
              : 'Familia desactivada correctamente.');
            this.estadoCambiado.emit({ familia: actualizada, accion });
          },
          error: error => {
            this.operando.set(false);
            this.error.set(this.obtenerMensajeError(
              error,
              `La familia fue ${accion === 'ACTIVADA' ? 'activada' : 'desactivada'}, pero no se pudo recargar su estado.`
            ));
          }
        });
      },
      error: error => {
        this.operando.set(false);
        const mensajeError = this.obtenerMensajeError(
          error,
          accion === 'ACTIVADA'
            ? 'No se pudo activar la familia.'
            : 'No se pudo desactivar la familia. Verifique que no tenga una versión publicada vigente.'
        );
        this.cargarDetalle(true);
        this.error.set(mensajeError);
      }
    });
  }

  private cargarDetalle(preservarBorrador: boolean): void {
    if (this.familiaId <= 0) return;

    const nombreBorrador = this.nombre;
    const descripcionBorrador = this.descripcion;
    const solicitudId = ++this.secuenciaCarga;
    this.suscripcionDetalle?.unsubscribe();
    this.error.set(null);
    this.mensaje.set(null);
    this.noEncontrada.set(false);
    this.cargando.set(true);

    this.suscripcionDetalle = this.service.obtenerFamiliaFormularioPorId(this.familiaId).subscribe({
      next: familia => {
        if (solicitudId !== this.secuenciaCarga) return;
        this.cargando.set(false);
        if (!familia) {
          this.detalle.set(null);
          this.noEncontrada.set(true);
          return;
        }
        this.aplicarDetalle(familia, preservarBorrador, nombreBorrador, descripcionBorrador);
      },
      error: error => {
        if (solicitudId !== this.secuenciaCarga) return;
        this.cargando.set(false);
        const status = (error as { status?: number })?.status;
        if (status === 404) {
          this.detalle.set(null);
          this.noEncontrada.set(true);
          return;
        }
        this.error.set(this.obtenerMensajeError(error, 'No se pudo cargar la familia para edición.'));
      }
    });
  }

  private aplicarDetalle(
    familia: FamiliaFormularioDto,
    preservarBorrador: boolean,
    nombreBorrador = this.nombre,
    descripcionBorrador = this.descripcion
  ): void {
    this.detalle.set(familia);
    this.nombrePersistido = familia.famNombre.trim();
    this.descripcionPersistida = (familia.famDescripcion ?? '').trim();
    this.nombre = preservarBorrador ? nombreBorrador : this.nombrePersistido;
    this.descripcion = preservarBorrador ? descripcionBorrador : this.descripcionPersistida;
  }

  private cancelarSolicitudes(): void {
    this.suscripcionDetalle?.unsubscribe();
    this.suscripcionDetalle = null;
    this.suscripcionOperacion?.unsubscribe();
    this.suscripcionOperacion = null;
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
        ? 'Los datos de la familia contienen errores de validación.'
        : respuesta?.error?.title)
      || mensaje;
  }
}
