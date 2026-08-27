import { ChangeDetectionStrategy, ChangeDetectorRef, Component, EventEmitter, Input, Output, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CampoBuilderModel, TipoControlBuilder, TipoControlDefinicion } from '../../../models/form-builder.models';

export interface InspectorCatalogoOption {
  codigo: string;
  nombre: string;
  cantidadElementos: number;
}

@Component({
  selector: 'app-form-builder-inspector',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './form-builder-inspector.component.html',
  styleUrls: ['./form-builder-inspector.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class FormBuilderInspectorComponent {
  private cdr = inject(ChangeDetectorRef);

  private _campoActivo: CampoBuilderModel | null = null;
  @Input()
  set campoActivo(val: CampoBuilderModel | null) {
    this._campoActivo = val;
    this.cdr.markForCheck();
  }
  get campoActivo(): CampoBuilderModel | null {
    return this._campoActivo;
  }

  @Input() soloLectura: boolean = false;
  @Input() tiposControles: TipoControlDefinicion[] = [];
  @Input() catalogosDisponibles: InspectorCatalogoOption[] = [];

  @Output() propiedadCambiada = new EventEmitter<void>();
  @Output() navegarCatalogos = new EventEmitter<void>();

  // Estado UI de acordeones colapsables (exclusivo de UI, nunca persistido)
  seccionesAbiertas: Record<'general' | 'reglas' | 'datos' | 'presentacion' | 'ayuda', boolean> = {
    general: true,
    reglas: true,
    datos: true,
    presentacion: true,
    ayuda: true
  };

  nuevaOpcionTexto = '';

  toggleSeccion(seccion: 'general' | 'reglas' | 'datos' | 'presentacion' | 'ayuda'): void {
    this.seccionesAbiertas[seccion] = !this.seccionesAbiertas[seccion];
    this.cdr.markForCheck();
  }

  get definicionTipoActual(): TipoControlDefinicion | undefined {
    if (!this.campoActivo) return undefined;
    return this.tiposControles.find(t => t.tipo === this.campoActivo?.tipo);
  }

  get requiereCatalogo(): boolean {
    if (!this.campoActivo) return false;
    return this.definicionTipoActual?.requiereCatalogo ?? false;
  }

  get requiereOpciones(): boolean {
    if (!this.campoActivo) return false;
    return this.definicionTipoActual?.requiereOpciones ?? false;
  }

  get requiereFormula(): boolean {
    if (!this.campoActivo) return false;
    return this.definicionTipoActual?.requiereFormula ?? false;
  }

  get esFormula(): boolean {
    return this.campoActivo?.tipo === 'formula';
  }

  get soportaPlaceholder(): boolean {
    if (!this.campoActivo) return false;
    return ['texto', 'numero', 'texto-largo'].includes(this.campoActivo.tipo);
  }

  alCambiarTipo(nuevoTipo: TipoControlBuilder): void {
    if (this.soloLectura || !this.campoActivo) return;
    this.campoActivo.tipo = nuevoTipo;

    // Invariante formula soloLectura
    if (nuevoTipo === 'formula') {
      this.campoActivo.soloLectura = true;
    }

    // Regla Hidden != Delete: no borrar propiedades contractuales al cambiar de tipo
    this.cdr.markForCheck();
    this.propiedadCambiada.emit();
  }

  alCambiarPropiedad(): void {
    if (this.soloLectura || !this.campoActivo) return;
    if (this.campoActivo.tipo === 'formula') {
      this.campoActivo.soloLectura = true;
    }
    this.cdr.markForCheck();
    this.propiedadCambiada.emit();
  }

  agregarOpcion(): void {
    if (this.soloLectura || !this.campoActivo) return;
    const texto = this.nuevaOpcionTexto.trim();
    if (!texto) return;

    this.campoActivo.opciones = [...(this.campoActivo.opciones || []), texto];
    this.nuevaOpcionTexto = '';
    this.cdr.markForCheck();
    this.propiedadCambiada.emit();
  }

  eliminarOpcion(index: number): void {
    if (this.soloLectura || !this.campoActivo || !this.campoActivo.opciones) return;
    this.campoActivo.opciones = this.campoActivo.opciones.filter((_, i) => i !== index);
    this.cdr.markForCheck();
    this.propiedadCambiada.emit();
  }
}
