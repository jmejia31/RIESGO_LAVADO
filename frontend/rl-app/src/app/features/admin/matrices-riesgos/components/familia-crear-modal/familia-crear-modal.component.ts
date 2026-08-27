import { AfterViewInit, ChangeDetectionStrategy, Component, ElementRef, EventEmitter, OnDestroy, Output, ViewChild, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatricesRiesgosService } from '../../data-access/matrices-riesgos.service';

export interface FamiliaCreadaEvent {
  id: number;
  nombre: string;
}

@Component({
  selector: 'app-familia-crear-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './familia-crear-modal.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class FamiliaCrearModalComponent implements AfterViewInit, OnDestroy {
  private readonly service = inject(MatricesRiesgosService);
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
  private focoAnterior: HTMLElement | null = null;

  @Output() readonly cerrar = new EventEmitter<void>();
  @Output() readonly creada = new EventEmitter<FamiliaCreadaEvent>();

  @ViewChild('codigoInput') codigoInput?: ElementRef<HTMLInputElement>;

  codigo = '';
  nombre = '';
  descripcion = '';

  readonly guardando = signal(false);
  readonly error = signal<string | null>(null);

  ngAfterViewInit(): void {
    this.focoAnterior = document.activeElement instanceof HTMLElement ? document.activeElement : null;
    setTimeout(() => this.codigoInput?.nativeElement.focus(), 0);
  }

  ngOnDestroy(): void {
    const foco = this.focoAnterior;
    if (foco?.isConnected) {
      setTimeout(() => foco.focus(), 0);
    }
  }

  normalizarCampos(): void {
    this.codigo = this.codigo.trim();
    this.nombre = this.nombre.trim();
    this.descripcion = this.descripcion.trim();
  }

  solicitarCierre(): void {
    if (this.guardando()) return;
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

  guardar(): void {
    if (this.guardando()) return;

    this.normalizarCampos();
    this.error.set(null);

    if (!this.codigo) {
      this.error.set('El código de la familia es obligatorio.');
      return;
    }
    if (this.codigo.length > 50) {
      this.error.set('El código no puede superar los 50 caracteres.');
      return;
    }
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

    this.guardando.set(true);
    const nombreCreado = this.nombre;

    this.service.crearFamiliaFormulario({
      famCodigo: this.codigo,
      famNombre: this.nombre,
      famDescripcion: this.descripcion || null
    }).subscribe({
      next: id => {
        this.guardando.set(false);
        this.creada.emit({ id, nombre: nombreCreado });
      },
      error: error => {
        this.guardando.set(false);
        this.error.set(this.obtenerMensajeError(
          error,
          'No se pudo crear la familia. Verifique el código y los datos ingresados.'
        ));
      }
    });
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
