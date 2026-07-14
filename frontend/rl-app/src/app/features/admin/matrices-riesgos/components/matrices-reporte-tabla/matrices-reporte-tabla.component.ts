import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { EscalaRiesgo, MatrizRiesgoResumen } from '../../models/matrices-riesgos.models';

@Component({
  selector: 'app-matrices-reporte-tabla',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './matrices-reporte-tabla.component.html',
  styleUrl: './matrices-reporte-tabla.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class MatricesReporteTablaComponent {
  @Input({ required: true }) matrices: MatrizRiesgoResumen[] = [];
  @Input() escalas: EscalaRiesgo[] = [];

  colorNivel(nivel?: string | null): string {
    const escala = this.escalas.find(item => item.nivel.toUpperCase() === (nivel ?? '').toUpperCase());
    if (escala?.color) return escala.color;
    const normalizado = (nivel ?? '').toUpperCase();
    if (normalizado.includes('CRIT')) return '#dc2626';
    if (normalizado.includes('ALTO')) return '#f97316';
    if (normalizado.includes('MEDIO')) return '#facc15';
    if (normalizado.includes('BAJO')) return '#22c55e';
    return '#94a3b8';
  }
}
