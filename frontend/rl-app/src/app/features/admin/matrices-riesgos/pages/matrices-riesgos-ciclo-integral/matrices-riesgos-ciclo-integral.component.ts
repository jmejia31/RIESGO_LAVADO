import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { MatricesRiesgosService } from '../../data-access/matrices-riesgos.service';
import { EvaluacionRiesgoResumenDto } from '../../models/matrices-riesgos.models';
import { MatricesRiesgosGestionComponent } from '../../components/matrices-riesgos-gestion/matrices-riesgos-gestion.component';
import { MatricesRiesgosMitigacionComponent } from '../../components/matrices-riesgos-mitigacion/matrices-riesgos-mitigacion.component';
import { MatricesRiesgosMonitoreoOperativoComponent } from '../../components/matrices-riesgos-monitoreo-operativo/matrices-riesgos-monitoreo-operativo.component';
import { MatricesRiesgosComponent } from '../matrices-riesgos/matrices-riesgos.component';

type VistaCiclo = 'matriz' | 'riesgos' | 'mitigacion' | 'monitoreo';

@Component({
  selector: 'app-matrices-riesgos-ciclo-integral',
  standalone: true,
  imports: [
    MatricesRiesgosComponent,
    MatricesRiesgosGestionComponent,
    MatricesRiesgosMitigacionComponent,
    MatricesRiesgosMonitoreoOperativoComponent
  ],
  templateUrl: './matrices-riesgos-ciclo-integral.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class MatricesRiesgosCicloIntegralComponent implements OnInit {
  private readonly service = inject(MatricesRiesgosService);

  readonly vista = signal<VistaCiclo>('matriz');
  readonly evaluaciones = signal<EvaluacionRiesgoResumenDto[]>([]);
  readonly error = signal<string | null>(null);

  ngOnInit(): void {
    this.cargarEvaluacionesOperativas();
  }

  seleccionarVista(vista: VistaCiclo): void {
    this.vista.set(vista);
    this.error.set(null);
    if (vista === 'mitigacion' || vista === 'monitoreo') this.cargarEvaluacionesOperativas();
  }

  private cargarEvaluacionesOperativas(): void {
    this.service.listarEvaluaciones({ pagina: 1, registrosPorPagina: 200 }).subscribe({
      next: paginado => this.evaluaciones.set(paginado.items),
      error: error => {
        const respuesta = error as { error?: { mensaje?: string }; message?: string };
        this.error.set(respuesta?.error?.mensaje || respuesta?.message || 'No se pudieron cargar las evaluaciones operativas.');
      }
    });
  }
}
