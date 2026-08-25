import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { EstadoFormulario } from '../../../models/matrices-riesgos.models';

@Component({
  selector: 'app-form-builder-toolbar',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './form-builder-toolbar.component.html',
  styleUrls: ['./form-builder-toolbar.component.scss'],
  changeDetection: ChangeDetectionStrategy.Default
})
export class FormBuilderToolbarComponent {
  @Input() title: string = 'Constructor de Formularios Dinámicos';
  @Input() versionCodigo: string = '';
  @Input() versionNumero: number = 1;
  @Input() estadoVersion?: EstadoFormulario;
  @Input() seccionesCount: number = 0;
  @Input() catalogosCount: number = 0;
  @Input() soloLectura: boolean = false;
  @Input() esAdministrador: boolean = false;
  @Input() vistaActiva: 'secciones' | 'catalogos' = 'secciones';
  @Input() mostrarJsonAvanzado: boolean = false;
  @Input() puedePublicar: boolean = false;
  @Input() procesando: boolean = false;

  @Output() cambiarVista = new EventEmitter<'secciones' | 'catalogos'>();
  @Output() toggleJson = new EventEmitter<void>();
  @Output() agregarSeccion = new EventEmitter<void>();
  @Output() agregarCatalogo = new EventEmitter<void>();
  @Output() guardar = new EventEmitter<void>();
  @Output() publicar = new EventEmitter<void>();
  @Output() cerrar = new EventEmitter<void>();

  get estadoEtiqueta(): string {
    const mapaEstados: Record<EstadoFormulario, string> = {
      DRAFT: 'BORRADOR',
      IN_REVIEW: 'EN REVISIÓN',
      APPROVED: 'APROBADA',
      PUBLISHED: 'PUBLICADA',
      RETIRED: 'RETIRADA',
      ARCHIVED: 'ARCHIVADA'
    };

    if (this.estadoVersion && mapaEstados[this.estadoVersion]) {
      const texto = mapaEstados[this.estadoVersion];
      return this.soloLectura ? `${texto} · SOLO LECTURA` : texto;
    }
    return this.soloLectura ? 'Modo Solo Lectura' : 'Modo Borrador';
  }
}
