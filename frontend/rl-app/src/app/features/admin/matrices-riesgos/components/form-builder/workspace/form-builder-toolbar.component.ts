import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-form-builder-toolbar-v2',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './form-builder-toolbar.component.html',
  styleUrls: ['./form-builder-toolbar.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class FormBuilderToolbarV2Component {
  @Input() title: string = 'Constructor de Formularios Dinámicos';
  @Input() versionCodigo: string = '';
  @Input() versionNumero: number = 1;
  @Input() seccionesCount: number = 0;
  @Input() catalogosCount: number = 0;
  @Input() soloLectura: boolean = false;
  @Input() esAdministrador: boolean = false;
  @Input() vistaActiva: 'secciones' | 'catalogos' = 'secciones';
  @Input() mostrarJsonAvanzado: boolean = false;
  @Input() activeTab: string = 'editor';

  @Output() cambiarVista = new EventEmitter<'secciones' | 'catalogos'>();
  @Output() toggleJson = new EventEmitter<void>();
  @Output() agregarSeccion = new EventEmitter<void>();
  @Output() agregarCatalogo = new EventEmitter<void>();
  @Output() guardar = new EventEmitter<void>();
  @Output() cerrar = new EventEmitter<void>();
  @Output() tabChange = new EventEmitter<string>();
}
