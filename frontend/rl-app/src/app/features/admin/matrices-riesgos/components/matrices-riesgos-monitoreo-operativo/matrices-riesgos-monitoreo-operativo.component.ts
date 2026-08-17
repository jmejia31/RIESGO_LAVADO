import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, Input, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatricesRiesgosService } from '../../data-access/matrices-riesgos.service';
import { EvaluacionRiesgoDto, EvaluacionRiesgoResumenDto } from '../../models/matrices-riesgos.models';
import {
  AutomonitoreoDto,
  AutomonitoreoGuardarDto,
  ResumenMatricesOperativoDto,
  SenalAlertaDto,
  SenalAlertaGuardarDto
} from '../../models/matrices-riesgos-fase11.models';

@Component({
  selector: 'app-matrices-riesgos-monitoreo-operativo',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './matrices-riesgos-monitoreo-operativo.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class MatricesRiesgosMonitoreoOperativoComponent implements OnInit {
  private readonly service = inject(MatricesRiesgosService);

  @Input() evaluaciones: Array<EvaluacionRiesgoDto | EvaluacionRiesgoResumenDto> = [];

  readonly resumen = signal<ResumenMatricesOperativoDto | null>(null);
  readonly alertas = signal<SenalAlertaDto[]>([]);
  readonly automonitoreos = signal<AutomonitoreoDto[]>([]);
  readonly cargando = signal(false);
  readonly guardando = signal(false);
  readonly error = signal<string | null>(null);
  readonly mensaje = signal<string | null>(null);

  evaluacionId = 0;

  obtenerEstadoEvaluacion(evaluacion: EvaluacionRiesgoDto | EvaluacionRiesgoResumenDto): string {
    if ('evaEstado' in evaluacion && evaluacion.evaEstado) return evaluacion.evaEstado;
    if ('estado' in evaluacion && evaluacion.estado) return evaluacion.estado;
    return '';
  }

  alertaCodigo = '';
  alertaIndicador = '';
  alertaEstado: 'ACTIVO' | 'INACTIVO' = 'ACTIVO';
  monEstadoRiesgo = '';
  monEstadoControles = '';
  monResultado = '';

  ngOnInit(): void {
    this.cargarResumen();
  }

  cargarResumen(): void {
    this.service.obtenerResumenOperativo().subscribe({
      next: resumen => this.resumen.set(resumen),
      error: error => this.error.set(this.mensajeError(error, 'No se pudo cargar el resumen operativo.'))
    });
  }

  seleccionarEvaluacion(valor: number | string): void {
    this.evaluacionId = Number(valor) || 0;
    this.alertas.set([]);
    this.automonitoreos.set([]);
    this.error.set(null);
    this.mensaje.set(null);
    if (this.evaluacionId > 0) this.cargarSeguimiento();
  }

  cargarSeguimiento(): void {
    if (this.evaluacionId <= 0) return;
    this.cargando.set(true);
    this.service.listarAlertas(this.evaluacionId).subscribe({
      next: alertas => {
        this.alertas.set(alertas);
        this.service.listarAutomonitoreo(this.evaluacionId).subscribe({
          next: automonitoreos => {
            this.automonitoreos.set(automonitoreos);
            this.cargando.set(false);
          },
          error: error => this.finalizarError(error, 'No se pudo cargar el automonitoreo.')
        });
      },
      error: error => this.finalizarError(error, 'No se pudieron cargar las señales de alerta.')
    });
  }

  crearAlerta(): void {
    if (this.evaluacionId <= 0 || !this.alertaCodigo.trim() || !this.alertaIndicador.trim()) {
      this.error.set('Seleccione una evaluación y complete código e indicador de la alerta.');
      return;
    }
    if (this.alertaCodigo.trim().length > 50 || this.alertaIndicador.trim().length > 150) {
      this.error.set('Revise las longitudes máximas permitidas de la alerta.');
      return;
    }

    const dto: SenalAlertaGuardarDto = {
      aleEvaluacionId: this.evaluacionId,
      aleCodigo: this.alertaCodigo.trim(),
      aleIndicador: this.alertaIndicador.trim(),
      aleEstado: this.alertaEstado
    };
    this.guardando.set(true);
    this.service.crearAlerta(dto).subscribe({
      next: () => {
        this.guardando.set(false);
        this.alertaCodigo = '';
        this.alertaIndicador = '';
        this.alertaEstado = 'ACTIVO';
        this.mensaje.set('Señal de alerta registrada correctamente.');
        this.cargarSeguimiento();
        this.cargarResumen();
      },
      error: error => this.finalizarGuardadoError(error, 'No se pudo registrar la señal de alerta.')
    });
  }

  cambiarEstado(alerta: SenalAlertaDto): void {
    const nuevoEstado: 'ACTIVO' | 'INACTIVO' = alerta.aleEstado === 'ACTIVO' ? 'INACTIVO' : 'ACTIVO';
    this.guardando.set(true);
    this.service.cambiarEstadoAlerta(alerta.aleId, nuevoEstado).subscribe({
      next: () => {
        this.guardando.set(false);
        this.mensaje.set(`Alerta ${nuevoEstado === 'ACTIVO' ? 'activada' : 'inactivada'} correctamente.`);
        this.cargarSeguimiento();
        this.cargarResumen();
      },
      error: error => this.finalizarGuardadoError(error, 'No se pudo cambiar el estado de la alerta.')
    });
  }

  registrarAutomonitoreo(): void {
    if (this.evaluacionId <= 0 || !this.monEstadoRiesgo.trim() || !this.monEstadoControles.trim() || !this.monResultado.trim()) {
      this.error.set('Seleccione una evaluación y complete todos los campos del automonitoreo.');
      return;
    }

    const dto: AutomonitoreoGuardarDto = {
      monEvaluacionId: this.evaluacionId,
      monEstadoRiesgo: this.monEstadoRiesgo.trim(),
      monEstadoContr: this.monEstadoControles.trim(),
      monResultado: this.monResultado.trim()
    };
    this.guardando.set(true);
    this.service.registrarAutomonitoreo(dto).subscribe({
      next: () => {
        this.guardando.set(false);
        this.monEstadoRiesgo = '';
        this.monEstadoControles = '';
        this.monResultado = '';
        this.mensaje.set('Automonitoreo registrado correctamente.');
        this.cargarSeguimiento();
        this.cargarResumen();
      },
      error: error => this.finalizarGuardadoError(error, 'No se pudo registrar el automonitoreo.')
    });
  }

  private finalizarError(error: unknown, mensaje: string): void {
    this.cargando.set(false);
    this.error.set(this.mensajeError(error, mensaje));
  }

  private finalizarGuardadoError(error: unknown, mensaje: string): void {
    this.guardando.set(false);
    this.error.set(this.mensajeError(error, mensaje));
  }

  private mensajeError(error: unknown, mensaje: string): string {
    const respuesta = error as { error?: { mensaje?: string }; message?: string };
    return respuesta?.error?.mensaje || respuesta?.message || mensaje;
  }
}
