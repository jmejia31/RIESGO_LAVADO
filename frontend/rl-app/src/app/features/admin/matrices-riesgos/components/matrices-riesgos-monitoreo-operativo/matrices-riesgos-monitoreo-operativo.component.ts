import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, ChangeDetectorRef, Component, EventEmitter, Input, OnInit, Output, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatricesRiesgosService } from '../../data-access/matrices-riesgos.service';
import { EvaluacionRiesgoDto, EvaluacionRiesgoResumenDto } from '../../models/matrices-riesgos.models';
import {
  AutomonitoreoDto,
  AutomonitoreoGuardarDto,
  ControlMonitoreoActualizarDto,
  ControlMonitoreoMatrizDto,
  MatrizBloque6Dto,
  ResumenMatricesOperativoDto,
  SenalAlertaDto,
  SenalAlertaGuardarDto
} from '../../models/matrices-riesgos-fase11.models';

import { ActionIconComponent } from '../../../../../shared/components/action-icon/action-icon.component';
import { InstitutionalFileUploadComponent } from '../../../../../shared/components/institutional-file-upload/institutional-file-upload.component';
import { INSTITUTIONAL_EVIDENCE_FILE_POLICY } from '../../../../../shared/components/institutional-file-upload/institutional-file-upload-policy';
import { BoundedSelectComponent, BoundedSelectOption } from '../../../../../shared/components/bounded-select/bounded-select.component';
import { normalizarMojibakeVisibleUtf8 } from '../../utils/text-encoding.util';
import { canonicalCatalogSelectOptions, canonicalMonitoringEffectivenessOptions } from '../../utils/matrices-riesgos-catalogos-canonicos';

@Component({
  selector: 'app-matrices-riesgos-monitoreo-operativo',
  standalone: true,
  imports: [ActionIconComponent, BoundedSelectComponent, InstitutionalFileUploadComponent, CommonModule, FormsModule],
  templateUrl: './matrices-riesgos-monitoreo-operativo.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class MatricesRiesgosMonitoreoOperativoComponent implements OnInit {
  private readonly service = inject(MatricesRiesgosService);
  private readonly cdr = inject(ChangeDetectorRef);

  @Input() evaluaciones: Array<EvaluacionRiesgoDto | EvaluacionRiesgoResumenDto> = [];
  @Input() evaluacionIdInicial: number | null = null;
  @Output() persisted = new EventEmitter<void>();

  readonly resumen = signal<ResumenMatricesOperativoDto | null>(null);
  readonly alertas = signal<SenalAlertaDto[]>([]);
  readonly automonitoreos = signal<AutomonitoreoDto[]>([]);
  readonly bloque6 = signal<MatrizBloque6Dto | null>(null);
  readonly cargando = signal(false);
  readonly guardando = signal(false);
  readonly error = signal<string | null>(null);
  readonly mensaje = signal<string | null>(null);

  evaluacionId = 0;
  readonly estadosRiesgo = canonicalCatalogSelectOptions('MONITORING_RISK_STATUS');
  readonly estadosControl = canonicalCatalogSelectOptions('MONITORING_CONTROL_STATUS');
  readonly efectividadesMonitoreoCatalogo = canonicalMonitoringEffectivenessOptions();

  obtenerEstadoEvaluacion(evaluacion: EvaluacionRiesgoDto | EvaluacionRiesgoResumenDto): string {
    if ('evaEstado' in evaluacion && evaluacion.evaEstado) return evaluacion.evaEstado;
    if ('estado' in evaluacion && evaluacion.estado) return evaluacion.estado;
    return '';
  }

  etiquetaEstadoEvaluacion(estado: string): string {
    const etiquetas: Record<string, string> = {
      BORRADOR: 'Borrador',
      EN_REVISION: 'En revisión',
      OBSERVADA: 'Observada',
      APROBADA: 'Aprobada',
      RECHAZADA: 'Rechazada',
      CERRADA: 'Cerrada'
    };
    return etiquetas[estado.trim().toUpperCase()] ?? estado;
  }

  get opcionesEvaluacion(): BoundedSelectOption[] {
    return this.evaluaciones.map(evaluacion => ({
      value: evaluacion.evaId,
      label: `#${evaluacion.evaId} · Riesgo ${evaluacion.evaRiesgoId} · ${this.etiquetaEstadoEvaluacion(this.obtenerEstadoEvaluacion(evaluacion))}`,
      searchText: this.textoBusquedaEvaluacion(evaluacion)
    }));
  }

  private textoBusquedaEvaluacion(evaluacion: EvaluacionRiesgoDto | EvaluacionRiesgoResumenDto): string {
    if ('riesgoCodigo' in evaluacion) {
      return `${evaluacion.riesgoCodigo} ${evaluacion.riesgoNombre} ${this.obtenerEstadoEvaluacion(evaluacion)}`;
    }
    return `${evaluacion.evaRiesgoId} ${this.obtenerEstadoEvaluacion(evaluacion)}`;
  }

  alertaCodigo = '';
  alertaIndicador = '';
  alertaEstado: 'ACTIVO' | 'INACTIVO' = 'ACTIVO';
  monEstadoRiesgo = '';
  monEstadoControles = '';
  monResultado = '';
  observacionesArea = '';
  observacionesUgr = '';
  readonly estadoMonitoreoDrafts: Record<number, string> = {};
  readonly efectividadMonitoreoDrafts: Record<number, number | null> = {};
  readonly archivosPendientesPorControl = signal<Record<number, File[]>>({});

  ngOnInit(): void {
    if (this.evaluacionIdInicial && this.evaluacionIdInicial > 0) {
      this.seleccionarEvaluacion(this.evaluacionIdInicial);
    } else {
      this.cargarResumen();
    }
  }

  cargarResumen(): void {
    this.service.obtenerResumenOperativo().subscribe({
      next: resumen => this.resumen.set(this.normalizarResumen(resumen)),
      error: error => this.error.set(this.mensajeError(error, 'No se pudo cargar el resumen operativo.'))
    });
  }

  seleccionarEvaluacion(valor: number | string): void {
    this.evaluacionId = Number(valor) || 0;
    this.alertas.set([]);
    this.automonitoreos.set([]);
    this.bloque6.set(null);
    this.error.set(null);
    this.mensaje.set(null);
    this.cdr.markForCheck();
    if (this.evaluacionId > 0) this.cargarSeguimiento();
  }

  cargarSeguimiento(): void {
    if (this.evaluacionId <= 0) return;
    this.cargando.set(true);
    this.service.listarAlertas(this.evaluacionId).subscribe({
      next: alertas => {
        this.alertas.set(alertas.map(alerta => ({ ...alerta, aleIndicador: normalizarMojibakeVisibleUtf8(alerta.aleIndicador) })));
        this.service.listarAutomonitoreo(this.evaluacionId).subscribe({
          next: automonitoreos => {
            this.automonitoreos.set(automonitoreos.map(item => ({ ...item, monEstadoRiesgo: normalizarMojibakeVisibleUtf8(item.monEstadoRiesgo), monEstadoContr: normalizarMojibakeVisibleUtf8(item.monEstadoContr), monResultado: normalizarMojibakeVisibleUtf8(item.monResultado) })));
            this.cargarBloque6();
          },
          error: error => this.finalizarError(error, 'No se pudo cargar el automonitoreo.')
        });
      },
      error: error => this.finalizarError(error, 'No se pudieron cargar las señales de alerta.')
    });
  }

  private cargarBloque6(): void {
    this.service.obtenerBloque6(this.evaluacionId).subscribe({
      next: datos => {
        this.bloque6.set(datos);
        this.alertas.set(datos.senalesAlerta.map(alerta => ({ ...alerta, aleIndicador: normalizarMojibakeVisibleUtf8(alerta.aleIndicador) })));
        this.observacionesArea = datos.observacionesArea ?? '';
        this.observacionesUgr = datos.observacionesUgr ?? '';
        for (const control of datos.controles) {
          this.estadoMonitoreoDrafts[control.controlId] = control.estadoMonitoreo ?? '';
          this.efectividadMonitoreoDrafts[control.controlId] = control.efectividadMonitoreo;
        }
        this.cargando.set(false);
        this.cdr.markForCheck();
      },
      error: error => this.finalizarError(error, 'No se pudo cargar el estado, controles y evidencias del Bloque 6.')
    });
  }

  controlesDeTipo(controles: ControlMonitoreoMatrizDto[], tipo: string): ControlMonitoreoMatrizDto[] {
    return controles.filter(control => control.tipo === tipo);
  }

  guardarMonitoreoControl(control: ControlMonitoreoMatrizDto): void {
    const dto: ControlMonitoreoActualizarDto = {
      evaluacionId: this.evaluacionId,
      estadoMonitoreo: this.estadoMonitoreoDrafts[control.controlId]?.trim() || null,
      efectividadMonitoreo: this.efectividadMonitoreoDrafts[control.controlId]
    };
    this.guardando.set(true);
    this.service.actualizarMonitoreoControl(control.controlId, dto).subscribe({
      next: () => {
        this.guardando.set(false);
        this.mensaje.set('Seguimiento del control actualizado.');
        this.cargarBloque6();
        this.service.obtenerProyeccionInstitucional(this.evaluacionId).subscribe({ next: () => this.persisted.emit(), error: error => this.finalizarGuardadoError(error, 'No se pudo confirmar la proyección institucional.') });
      },
      error: error => this.finalizarGuardadoError(error, 'No se pudo actualizar el seguimiento del control.')
    });
  }

  guardarObservacion(tipo: 'area' | 'ugr'): void {
    const data = this.bloque6();
    const permitido = tipo === 'area' ? data?.puedeEditarObservacionesArea : data?.puedeEditarObservacionesUgr;
    if (!permitido) {
      this.error.set(`No tiene permiso para editar Observaciones ${tipo === 'area' ? 'del Área' : 'UGR'}.`);
      return;
    }
    this.guardando.set(true);
    const request = tipo === 'area'
      ? this.service.actualizarObservacionArea(this.evaluacionId, this.observacionesArea)
      : this.service.actualizarObservacionUgr(this.evaluacionId, this.observacionesUgr);
    request.subscribe({
      next: () => {
        this.guardando.set(false);
        this.mensaje.set(`Observaciones ${tipo === 'area' ? 'del Área' : 'UGR'} guardadas.`);
        this.cargarBloque6();
        this.service.obtenerProyeccionInstitucional(this.evaluacionId).subscribe({ next: () => this.persisted.emit(), error: error => this.finalizarGuardadoError(error, 'No se pudo confirmar la proyección institucional.') });
      },
      error: error => {
        if ((error as { status?: number })?.status === 403) this.error.set(`No tiene permiso para editar Observaciones ${tipo === 'area' ? 'del Área' : 'UGR'}.`);
        else this.finalizarGuardadoError(error, `No se pudieron guardar Observaciones ${tipo === 'area' ? 'del Área' : 'UGR'}.`);
      }
    });
  }

  cargarEvidencia(control: ControlMonitoreoMatrizDto, archivos: File[]): void {
    const archivo = archivos[0];
    if (!archivo) return;
    this.archivosPendientesPorControl.update(pendientes => ({ ...pendientes, [control.controlId]: [archivo] }));
    this.guardando.set(true);
    this.service.cargarEvidencia(archivo).subscribe({
      next: evidencia => this.service.vincularEvidencia({ evidenciaId: evidencia.eviId, tipoEntidad: 'control', entidadId: control.controlId }).subscribe({
        next: () => {
          this.guardando.set(false);
          this.archivosPendientesPorControl.update(pendientes => {
            const actualizados = { ...pendientes };
            delete actualizados[control.controlId];
            return actualizados;
          });
          this.mensaje.set('Evidencia cargada y vinculada al control.');
          this.cargarBloque6();
          this.service.obtenerProyeccionInstitucional(this.evaluacionId).subscribe({ next: () => this.persisted.emit(), error: error => this.finalizarGuardadoError(error, 'No se pudo confirmar la proyección institucional.') });
        },
        error: error => this.finalizarGuardadoError(error, 'El archivo se cargó, pero no se pudo vincular al control.')
      }),
      error: error => this.finalizarGuardadoError(error, 'No se pudo cargar la evidencia.')
    });
  }

  get politicaEvidencias() {
    return INSTITUTIONAL_EVIDENCE_FILE_POLICY;
  }

  desvincularEvidencia(control: ControlMonitoreoMatrizDto, evidenciaId: number): void {
    this.guardando.set(true);
    this.service.desvincularEvidencia({ evidenciaId, tipoEntidad: 'control', entidadId: control.controlId }).subscribe({
      next: () => {
        this.guardando.set(false);
        this.mensaje.set('Evidencia desvinculada. El archivo se conserva según su ciclo de vida.');
        this.cargarBloque6();
        this.service.obtenerProyeccionInstitucional(this.evaluacionId).subscribe({ next: () => this.persisted.emit(), error: error => this.finalizarGuardadoError(error, 'No se pudo confirmar la proyección institucional.') });
      },
      error: error => this.finalizarGuardadoError(error, 'No se pudo desvincular la evidencia.')
    });
  }

  private normalizarResumen(resumen: ResumenMatricesOperativoDto): ResumenMatricesOperativoDto {
    return { ...resumen };
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
        this.persisted.emit();
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
        this.persisted.emit();
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
        this.persisted.emit();
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
