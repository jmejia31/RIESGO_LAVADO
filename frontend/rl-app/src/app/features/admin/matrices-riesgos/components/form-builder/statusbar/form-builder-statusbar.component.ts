import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { EstadoFormulario } from '../../../models/matrices-riesgos.models';

@Component({
  selector: 'app-form-builder-statusbar',
  standalone: true,
  templateUrl: './form-builder-statusbar.component.html',
  styleUrls: ['./form-builder-statusbar.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class FormBuilderStatusbarComponent {
  @Input() versionCodigo: string = '';
  @Input() estadoVersion?: EstadoFormulario;
  @Input() soloLectura: boolean = false;
  @Input() seccionesCount: number = 0;
  @Input() catalogosCount: number = 0;
  @Input() puedeGuardar: boolean = false;
  @Output() cancelar = new EventEmitter<void>();
  @Output() guardar = new EventEmitter<void>();

  get estadoEtiqueta(): string {
    const mapa: Record<EstadoFormulario, string> = {
      DRAFT: 'Borrador', IN_REVIEW: 'En revision', APPROVED: 'Aprobada',
      PUBLISHED: 'Publicada', RETIRED: 'Retirada', ARCHIVED: 'Archivada'
    };
    const estado = this.estadoVersion ? mapa[this.estadoVersion] : 'Modo borrador';
    return this.soloLectura && this.estadoVersion ? `${estado} \u00b7 Solo lectura` : (this.soloLectura ? 'Solo lectura' : estado);
  }
}
