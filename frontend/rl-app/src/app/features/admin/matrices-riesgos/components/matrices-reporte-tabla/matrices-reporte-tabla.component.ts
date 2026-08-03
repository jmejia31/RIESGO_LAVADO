import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RiesgoReporteFila } from '../../models/matrices-riesgos.models';

@Component({
  selector: 'app-matrices-reporte-tabla',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './matrices-reporte-tabla.component.html',
  styleUrl: './matrices-reporte-tabla.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class MatricesReporteTablaComponent {
  @Input({ required: true }) filas: RiesgoReporteFila[] = [];
}
