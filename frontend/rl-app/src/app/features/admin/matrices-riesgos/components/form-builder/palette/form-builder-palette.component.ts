import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CampoBuilderModel, SeccionBuilderModel, TipoControlDefinicion } from '../../../models/form-builder.models';

function normalizarTexto(valor: string | undefined | null): string {
  if (!valor) return '';
  return valor
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .toLowerCase()
    .trim();
}

@Component({
  selector: 'app-form-builder-palette',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './form-builder-palette.component.html',
  styleUrls: ['./form-builder-palette.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class FormBuilderPaletteComponent {
  @Input() tiposControles: TipoControlDefinicion[] = [];
  @Input() secciones: SeccionBuilderModel[] = [];
  @Input() soloLectura: boolean = false;
  @Input() seccionActivaId: string | null = null;
  @Input() campoActivoId: string | null = null;

  @Output() agregarCampo = new EventEmitter<TipoControlDefinicion>();
  @Output() seleccionarSeccion = new EventEmitter<string>();
  @Output() seleccionarCampo = new EventEmitter<CampoBuilderModel>();

  readonly terminoBusqueda = signal<string>('');

  readonly controlesFiltrados = computed(() => {
    const query = normalizarTexto(this.terminoBusqueda());
    if (!query) {
      return this.tiposControles;
    }
    return this.tiposControles.filter(ctrl => {
      const matchEtiqueta = normalizarTexto(ctrl.etiqueta).includes(query);
      const matchDescripcion = normalizarTexto(ctrl.descripcion).includes(query);
      const matchTipo = normalizarTexto(ctrl.tipo).includes(query);
      const matchCategoria = normalizarTexto(ctrl.categoria).includes(query);
      return matchEtiqueta || matchDescripcion || matchTipo || matchCategoria;
    });
  });

  readonly controlesBasicos = computed(() =>
    this.controlesFiltrados().filter(c => c.categoria === 'basico')
  );

  readonly controlesSeleccion = computed(() =>
    this.controlesFiltrados().filter(c => c.categoria === 'seleccion')
  );

  readonly controlesAvanzados = computed(() =>
    this.controlesFiltrados().filter(c => c.categoria === 'avanzado')
  );

  readonly totalFiltrados = computed(() => this.controlesFiltrados().length);

  readonly grupos = computed(() => [
    { clave: 'basico', etiqueta: 'Básicos', controles: this.controlesBasicos() },
    { clave: 'seleccion', etiqueta: 'Selección', controles: this.controlesSeleccion() },
    { clave: 'avanzado', etiqueta: 'Avanzados', controles: this.controlesAvanzados() }
  ].filter(grupo => grupo.controles.length > 0));

  limpiarBusqueda(): void {
    this.terminoBusqueda.set('');
  }

  onDragStart(event: DragEvent, ctrl: TipoControlDefinicion): void {
    if (this.soloLectura || !this.seccionActivaId) {
      event.preventDefault();
      return;
    }
    if (event.dataTransfer) {
      event.dataTransfer.setData('text/plain', ctrl.tipo);
      event.dataTransfer.setData('application/x-form-builder-control', ctrl.tipo);
      event.dataTransfer.effectAllowed = 'copy';
    }
  }

  onCardClick(ctrl: TipoControlDefinicion): void {
    if (!this.soloLectura && this.seccionActivaId) this.agregarCampo.emit(ctrl);
  }

  onCardKeydown(event: KeyboardEvent, ctrl: TipoControlDefinicion): void {
    if (event.key !== 'Enter' && event.key !== ' ') return;
    event.preventDefault();
    this.onCardClick(ctrl);
  }
}
