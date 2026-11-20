import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, ChangeDetectorRef, Component, EventEmitter, Input, OnInit, Output, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatricesRiesgosService } from '../../data-access/matrices-riesgos.service';
import { EstadoEvaluacionCatalogoDto, EvaluacionRiesgoDto, EvaluacionRiesgoResumenDto } from '../../models/matrices-riesgos.models';
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
import { InstitutionalTooltipDirective } from '../../../../../shared/components/institutional-tooltip/institutional-tooltip.directive';
import { INSTITUTIONAL_EVIDENCE_FILE_POLICY } from '../../../../../shared/components/institutional-file-upload/institutional-file-upload-policy';
import { BoundedSelectComponent, BoundedSelectOption } from '../../../../../shared/components/bounded-select/bounded-select.component';
import { normalizarMojibakeVisibleUtf8 } from '../../utils/text-encoding.util';
import { canonicalCatalogSelectOptions, canonicalMonitoringEffectivenessOptions } from '../../utils/matrices-riesgos-catalogos-canonicos';
import { InstitutionalConfirmationService } from '../../../../../shared/confirmation/institutional-confirmation.service';
import { InstitutionalNotificationService } from '../../../../../shared/notification/institutional-notification.service';

@Component({
  selector: 'app-matrices-riesgos-monitoreo-operativo',
  standalone: true,
  imports: [ActionIconComponent, BoundedSelectComponent, InstitutionalFileUploadComponent, InstitutionalTooltipDirective, CommonModule, FormsModule],
  templateUrl: './matrices-riesgos-monitoreo-operativo.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class MatricesRiesgosMonitoreoOperativoComponent implements OnInit {
  private readonly service = inject(MatricesRiesgosService);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly confirmation = inject(InstitutionalConfirmationService);
  private readonly notification = inject(InstitutionalNotificationService);

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
  readonly catalogoEstadosEvaluacion = signal<EstadoEvaluacionCatalogoDto[]>([]);
  readonly modalAlertaAbierto = signal(false);
  readonly modalAutomonitoreoAbierto = signal(false);
  readonly confirmando = signal(false);

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
    const codigo = estado.trim().toUpperCase();
    const catalogo = this.catalogoEstadosEvaluacion();
    return (Array.isArray(catalogo) ? catalogo : []).find(item => item.codigo === codigo)?.etiqueta ?? estado;
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
  private alertaSnapshot = '';
  private automonitoreoSnapshot = '';

  ngOnInit(): void {
    const cargarCatalogo = this.service.obtenerCatalogoEstadosEvaluacion;
    if (typeof cargarCatalogo === 'function') {
      cargarCatalogo.call(this.service).subscribe({
        next: estados => {
          this.catalogoEstadosEvaluacion.set(Array.isArray(estados) ? estados : []);
          this.cdr.markForCheck();
        }
      });
    }
    if (this.evaluacionIdInicial && this.evaluacionIdInicial > 0) {
      this.seleccionarEvaluacion(this.evaluacionIdInicial);
    } else {
      this.cargarResumen();
    }
  }

  cargarResumen(): void {
    this.service.obtenerResumenOperativo().subscribe({
      next: resumen => this.resumen.set(this.normalizarResumen(resumen)),
      error: error => this.notificarError(this.mensajeError(error, 'No se pudo cargar el resumen operativo.'))
    });
  }

  seleccionarEvaluacion(valor: number | string): void {
    this.evaluacionId = Number(valor) || 0;
    this.modalAlertaAbierto.set(false);
    this.modalAutomonitoreoAbierto.set(false);
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

  async guardarMonitoreoControl(control: ControlMonitoreoMatrizDto): Promise<void> {
    if (this.operacionEnCurso()) return;
    const dto: ControlMonitoreoActualizarDto = {
      evaluacionId: this.evaluacionId,
      estadoMonitoreo: this.estadoMonitoreoDrafts[control.controlId]?.trim() || null,
      efectividadMonitoreo: this.efectividadMonitoreoDrafts[control.controlId]
    };
    const confirmado = await this.confirmarGuardado(
      'Guardar seguimiento del control',
      `¿Desea guardar el estado y la efectividad del control #${control.controlId}?`,
      'Guardar seguimiento'
    );
    if (!confirmado) return;
    this.guardando.set(true);
    this.service.actualizarMonitoreoControl(control.controlId, dto).subscribe({
      next: () => {
        this.guardando.set(false);
        this.notificarExito('Seguimiento del control actualizado.');
        this.cargarBloque6();
        this.service.obtenerProyeccionInstitucional(this.evaluacionId).subscribe({ next: () => this.persisted.emit(), error: error => this.finalizarGuardadoError(error, 'No se pudo confirmar la proyección institucional.') });
      },
      error: error => this.finalizarGuardadoError(error, 'No se pudo actualizar el seguimiento del control.')
    });
  }

  async guardarObservacion(tipo: 'area' | 'ugr'): Promise<void> {
    if (this.operacionEnCurso()) return;
    const data = this.bloque6();
    const permitido = tipo === 'area' ? data?.puedeEditarObservacionesArea : data?.puedeEditarObservacionesUgr;
    if (!permitido) {
      this.notificarError(`No tiene permiso para editar Observaciones ${tipo === 'area' ? 'del Área' : 'UGR'}.`);
      return;
    }
    const etiqueta = tipo === 'area' ? 'del Área' : 'UGR';
    const confirmado = await this.confirmarGuardado(
      `Guardar Observaciones ${etiqueta}`,
      `¿Desea guardar las Observaciones ${etiqueta}?`,
      'Guardar observaciones'
    );
    if (!confirmado) return;
    this.guardando.set(true);
    const request = tipo === 'area'
      ? this.service.actualizarObservacionArea(this.evaluacionId, this.observacionesArea)
      : this.service.actualizarObservacionUgr(this.evaluacionId, this.observacionesUgr);
    request.subscribe({
      next: () => {
        this.guardando.set(false);
        this.notificarExito(`Observaciones ${tipo === 'area' ? 'del Área' : 'UGR'} guardadas.`);
        this.cargarBloque6();
        this.service.obtenerProyeccionInstitucional(this.evaluacionId).subscribe({ next: () => this.persisted.emit(), error: error => this.finalizarGuardadoError(error, 'No se pudo confirmar la proyección institucional.') });
      },
      error: error => {
        if ((error as { status?: number })?.status === 403) this.notificarError(`No tiene permiso para editar Observaciones ${tipo === 'area' ? 'del Área' : 'UGR'}.`);
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
          this.notificarExito('Evidencia cargada y vinculada al control.');
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

  async desvincularEvidencia(control: ControlMonitoreoMatrizDto, evidenciaId: number): Promise<void> {
    if (this.operacionEnCurso()) return;
    const evidencia = control.evidencias.find(item => item.id === evidenciaId);
    const nombreArchivo = evidencia?.nombreArchivo || `evidencia #${evidenciaId}`;
    const confirmado = await this.confirmarGuardado(
      'Desvincular evidencia',
      `¿Desea desvincular “${nombreArchivo}” del control #${control.controlId}? El archivo no será eliminado.`,
      'Desvincular'
    );
    if (!confirmado) return;
    this.guardando.set(true);
    this.service.desvincularEvidencia({ evidenciaId, tipoEntidad: 'control', entidadId: control.controlId }).subscribe({
      next: () => {
        this.guardando.set(false);
        this.notificarExito('Evidencia desvinculada. El archivo se conserva según su ciclo de vida.');
        this.cargarBloque6();
        this.service.obtenerProyeccionInstitucional(this.evaluacionId).subscribe({ next: () => this.persisted.emit(), error: error => this.finalizarGuardadoError(error, 'No se pudo confirmar la proyección institucional.') });
      },
      error: error => this.finalizarGuardadoError(error, 'No se pudo desvincular la evidencia.')
    });
  }

  private normalizarResumen(resumen: ResumenMatricesOperativoDto): ResumenMatricesOperativoDto {
    return { ...resumen };
  }

  nuevoAlerta(): void {
    this.alertaCodigo = '';
    this.alertaIndicador = '';
    this.alertaEstado = 'ACTIVO';
    this.alertaSnapshot = this.estadoAlertaFormulario();
    this.modalAlertaAbierto.set(true);
  }

  async crearAlerta(): Promise<void> {
    if (this.operacionEnCurso()) return;
    if (this.evaluacionId <= 0 || !this.alertaCodigo.trim() || !this.alertaIndicador.trim()) {
      this.notificarError('Seleccione una evaluación y complete código e indicador de la alerta.');
      return;
    }
    if (this.alertaCodigo.trim().length > 50 || this.alertaIndicador.trim().length > 150) {
      this.notificarError('Revise las longitudes máximas permitidas de la alerta.');
      return;
    }

    const confirmado = await this.confirmarGuardado(
      'Registrar señal de alerta',
      '¿Desea registrar esta señal de alerta para la evaluación seleccionada?',
      'Registrar alerta'
    );
    if (!confirmado) return;

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
        this.notificarExito('Señal de alerta registrada correctamente.');
        this.cerrarModalAlertaForzado();
        this.cargarSeguimiento();
        this.cargarResumen();
        this.persisted.emit();
      },
      error: error => this.finalizarGuardadoError(error, 'No se pudo registrar la señal de alerta.')
    });
  }

  async cambiarEstado(alerta: SenalAlertaDto): Promise<void> {
    if (this.operacionEnCurso()) return;
    const nuevoEstado: 'ACTIVO' | 'INACTIVO' = alerta.aleEstado === 'ACTIVO' ? 'INACTIVO' : 'ACTIVO';
    const confirmado = await this.confirmarGuardado(
      `${nuevoEstado === 'ACTIVO' ? 'Activar' : 'Inactivar'} alerta`,
      `¿Desea ${nuevoEstado === 'ACTIVO' ? 'activar' : 'inactivar'} la alerta ${alerta.aleCodigo}?`,
      nuevoEstado === 'ACTIVO' ? 'Activar alerta' : 'Inactivar alerta'
    );
    if (!confirmado) return;
    this.guardando.set(true);
    this.service.cambiarEstadoAlerta(alerta.aleId, nuevoEstado).subscribe({
      next: () => {
        this.guardando.set(false);
        this.notificarExito(`Alerta ${nuevoEstado === 'ACTIVO' ? 'activada' : 'inactivada'} correctamente.`);
        this.cargarSeguimiento();
        this.cargarResumen();
        this.persisted.emit();
      },
      error: error => this.finalizarGuardadoError(error, 'No se pudo cambiar el estado de la alerta.')
    });
  }

  nuevoAutomonitoreo(): void {
    this.monEstadoRiesgo = '';
    this.monEstadoControles = '';
    this.monResultado = '';
    this.automonitoreoSnapshot = this.estadoAutomonitoreoFormulario();
    this.modalAutomonitoreoAbierto.set(true);
  }

  async registrarAutomonitoreo(): Promise<void> {
    if (this.operacionEnCurso()) return;
    if (this.evaluacionId <= 0 || !this.monEstadoRiesgo.trim() || !this.monEstadoControles.trim() || !this.monResultado.trim()) {
      this.notificarError('Seleccione una evaluación y complete todos los campos del automonitoreo.');
      return;
    }

    const confirmado = await this.confirmarGuardado(
      'Registrar automonitoreo',
      '¿Desea registrar este resultado de automonitoreo?',
      'Registrar automonitoreo'
    );
    if (!confirmado) return;

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
        this.notificarExito('Automonitoreo registrado correctamente.');
        this.cerrarModalAutomonitoreoForzado();
        this.cargarSeguimiento();
        this.cargarResumen();
        this.persisted.emit();
      },
      error: error => this.finalizarGuardadoError(error, 'No se pudo registrar el automonitoreo.')
    });
  }

  private finalizarError(error: unknown, mensaje: string): void {
    this.cargando.set(false);
    this.notificarError(this.mensajeError(error, mensaje));
  }

  private finalizarGuardadoError(error: unknown, mensaje: string): void {
    this.guardando.set(false);
    this.notificarError(this.mensajeError(error, mensaje));
  }

  private notificarError(mensaje: string): void {
    this.error.set(mensaje);
    this.mensaje.set(null);
    void this.notification.notify({
      title: 'No se pudo completar la operación',
      message: mensaje,
      icon: 'error',
      confirmIcon: 'cancel',
      confirmLabel: 'Aceptar',
      confirmVariant: 'danger'
    });
  }

  private notificarExito(mensaje: string): void {
    this.mensaje.set(mensaje);
    this.error.set(null);
    void this.notification.notify({
      title: 'Operación completada',
      message: mensaje,
      icon: 'success',
      confirmIcon: 'confirm',
      confirmLabel: 'Aceptar',
      confirmVariant: 'success'
    });
  }

  private mensajeError(error: unknown, mensaje: string): string {
    const respuesta = error as { error?: { mensaje?: string }; message?: string };
    return respuesta?.error?.mensaje || respuesta?.message || mensaje;
  }

  operacionEnCurso(): boolean {
    return this.guardando() || this.confirmando();
  }

  alertaFormularioValido(): boolean {
    return this.evaluacionId > 0
      && this.alertaCodigo.trim().length > 0
      && this.alertaCodigo.trim().length <= 50
      && this.alertaIndicador.trim().length > 0
      && this.alertaIndicador.trim().length <= 150;
  }

  automonitoreoFormularioValido(): boolean {
    return this.evaluacionId > 0
      && this.monEstadoRiesgo.trim().length > 0
      && this.monEstadoControles.trim().length > 0
      && this.monResultado.trim().length > 0;
  }

  observacionValida(tipo: 'area' | 'ugr'): boolean {
    return (tipo === 'area' ? this.observacionesArea : this.observacionesUgr).trim().length > 0;
  }

  async solicitarCierreAlerta(): Promise<void> {
    if (this.operacionEnCurso()) return;
    if (this.estadoAlertaFormulario() !== this.alertaSnapshot) {
      const confirmado = await this.confirmation.confirm({
        title: 'Descartar señal de alerta',
        message: '¿Desea descartar los datos de la señal de alerta?',
        confirmLabel: 'Descartar cambios',
        cancelLabel: 'Continuar editando',
        confirmIcon: 'delete',
        confirmVariant: 'danger',
        icon: 'warning'
      });
      if (!confirmado) return;
    }
    this.cerrarModalAlertaForzado();
  }

  async solicitarCierreAutomonitoreo(): Promise<void> {
    if (this.operacionEnCurso()) return;
    if (this.estadoAutomonitoreoFormulario() !== this.automonitoreoSnapshot) {
      const confirmado = await this.confirmation.confirm({
        title: 'Descartar automonitoreo',
        message: '¿Desea descartar los datos del automonitoreo?',
        confirmLabel: 'Descartar cambios',
        cancelLabel: 'Continuar editando',
        confirmIcon: 'delete',
        confirmVariant: 'danger',
        icon: 'warning'
      });
      if (!confirmado) return;
    }
    this.cerrarModalAutomonitoreoForzado();
  }

  private async confirmarGuardado(title: string, message: string, confirmLabel: string): Promise<boolean> {
    if (this.confirmando()) return false;
    this.confirmando.set(true);
    try {
      return await this.confirmation.confirm({
        title,
        message,
        confirmLabel,
        cancelLabel: 'Continuar editando',
        confirmIcon: 'save',
        confirmVariant: 'primary'
      });
    } finally {
      this.confirmando.set(false);
    }
  }

  private estadoAlertaFormulario(): string {
    return JSON.stringify([this.evaluacionId, this.alertaCodigo, this.alertaIndicador, this.alertaEstado]);
  }

  private estadoAutomonitoreoFormulario(): string {
    return JSON.stringify([this.evaluacionId, this.monEstadoRiesgo, this.monEstadoControles, this.monResultado]);
  }

  cerrarModalAlertaForzado(): void {
    this.modalAlertaAbierto.set(false);
    this.alertaCodigo = '';
    this.alertaIndicador = '';
    this.alertaEstado = 'ACTIVO';
    this.alertaSnapshot = this.estadoAlertaFormulario();
  }

  cerrarModalAutomonitoreoForzado(): void {
    this.modalAutomonitoreoAbierto.set(false);
    this.monEstadoRiesgo = '';
    this.monEstadoControles = '';
    this.monResultado = '';
    this.automonitoreoSnapshot = this.estadoAutomonitoreoFormulario();
  }
}
