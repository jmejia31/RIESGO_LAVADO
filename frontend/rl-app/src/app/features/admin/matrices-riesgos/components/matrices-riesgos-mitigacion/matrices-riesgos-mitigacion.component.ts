import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, ChangeDetectorRef, Component, EventEmitter, Input, OnChanges, OnInit, Output, SimpleChanges, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Observable } from 'rxjs';
import { MatricesRiesgosService } from '../../data-access/matrices-riesgos.service';
import { normalizarMojibakeVisibleUtf8 } from '../../utils/text-encoding.util';
import { EstadoEvaluacionCatalogoDto, EvaluacionRiesgoDto, EvaluacionRiesgoResumenDto } from '../../models/matrices-riesgos.models';
import {
  ActividadPlanDto,
  ActividadPlanGuardarDto,
  ControlRiesgoDto,
  ControlRiesgoGuardarDto,
  EvaluacionControlDto,
  EvaluacionControlGuardarDto,
  PlanMitigacionDto,
  PlanMitigacionGuardarDto
} from '../../models/matrices-riesgos-fase11.models';

import { ActionIconComponent } from '../../../../../shared/components/action-icon/action-icon.component';
import { BoundedSelectComponent, BoundedSelectOption } from '../../../../../shared/components/bounded-select/bounded-select.component';
import { InstitutionalTooltipDirective } from '../../../../../shared/components/institutional-tooltip/institutional-tooltip.directive';
import { canonicalCatalogSelectOptions } from '../../utils/matrices-riesgos-catalogos-canonicos';
import { InstitutionalConfirmationService } from '../../../../../shared/confirmation/institutional-confirmation.service';

@Component({
  selector: 'app-matrices-riesgos-mitigacion',
  standalone: true,
  imports: [ActionIconComponent, BoundedSelectComponent, InstitutionalTooltipDirective, CommonModule, FormsModule],
  templateUrl: './matrices-riesgos-mitigacion.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class MatricesRiesgosMitigacionComponent implements OnChanges, OnInit {
  private readonly service = inject(MatricesRiesgosService);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly confirmation = inject(InstitutionalConfirmationService);

  @Input() evaluaciones: Array<EvaluacionRiesgoDto | EvaluacionRiesgoResumenDto> = [];
  @Input() evaluacionIdInicial: number | null = null;
  @Input() modo: 'completo' | 'controles' | 'planes' = 'completo';
  @Output() persisted = new EventEmitter<void>();

  readonly controles = signal<ControlRiesgoDto[]>([]);
  readonly evaluacionesControl = signal<EvaluacionControlDto[]>([]);
  readonly planes = signal<PlanMitigacionDto[]>([]);
  readonly actividades = signal<ActividadPlanDto[]>([]);
  readonly cargando = signal(false);
  readonly guardando = signal(false);
  readonly error = signal<string | null>(null);
  readonly mensaje = signal<string | null>(null);
  readonly catalogoEstadosEvaluacion = signal<EstadoEvaluacionCatalogoDto[]>([]);
  readonly modalControlAbierto = signal(false);
  readonly modalEfectividadAbierto = signal(false);
  readonly modalPlanAbierto = signal(false);
  readonly modalActividadAbierto = signal(false);
  readonly confirmando = signal(false);

  evaluacionId = 0;
  readonly tiposControlCatalogo = canonicalCatalogSelectOptions('CONTROL_TYPE');
  readonly automatizacionesCatalogo = canonicalCatalogSelectOptions('CONTROL_AUTOMATION');

  ngOnInit(): void {
    const cargarCatalogo = this.service.obtenerCatalogoEstadosEvaluacion;
    if (typeof cargarCatalogo !== 'function') return;
    cargarCatalogo.call(this.service).subscribe({
      next: estados => {
        this.catalogoEstadosEvaluacion.set(Array.isArray(estados) ? estados : []);
        this.cdr.markForCheck();
      }
    });
  }

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
  controlSeleccionadoId = 0;
  planSeleccionadoId = 0;

  controlEditandoId = 0;
  controlTipo: ControlRiesgoGuardarDto['conTipo'] = 'PREVENTIVO';
  controlDescripcion = '';
  controlAutomatizacion: ControlRiesgoGuardarDto['conAutomatizacion'] = 'MANUAL';
  readonly estadosControl = [
    { valor: 'ACTIVO', texto: 'Activo' },
    { valor: 'INACTIVO', texto: 'Inactivo' }
  ] as const;
  readonly estadosPlan = [
    { valor: 'PENDIENTE', texto: 'Pendiente' },
    { valor: 'EN_PROCESO', texto: 'En proceso' },
    { valor: 'CERRADO', texto: 'Cerrado' },
    { valor: 'VENCIDO', texto: 'Vencido' },
    { valor: 'INACTIVO', texto: 'Inactivo' }
  ] as const;
  readonly estadosActividad = [
    { valor: 'PENDIENTE', texto: 'Pendiente' },
    { valor: 'EN_PROCESO', texto: 'En proceso' },
    { valor: 'FINALIZADA', texto: 'Finalizada' },
    { valor: 'CANCELADA', texto: 'Cancelada' }
  ] as const;

  controlEstado = 'ACTIVO';
  efectividad = 0;
  comentarioEfectividad = '';

  planEditandoId = 0;
  planDescripcion = '';
  planAvance = 0;
  planPresupuesto = 0;
  planFechaInicio = this.hoy();
  planFechaFin = this.hoy();
  planEstado = 'PENDIENTE';
  planMonitoreoSeguimiento = '';
  planResponsables = '';
  planRecursos = '';

  actividadEditandoId = 0;
  actividadDescripcion = '';
  actividadResponsable = '';
  actividadAvance = 0;
  actividadFechaInicio = this.hoy();
  actividadFechaFin = this.hoy();
  actividadEstado = 'PENDIENTE';

  private controlSnapshot = '';
  private efectividadSnapshot = '';
  private planSnapshot = '';
  private actividadSnapshot = '';

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['evaluacionIdInicial'] && this.evaluacionIdInicial && this.evaluacionIdInicial > 0
      && this.evaluacionId !== this.evaluacionIdInicial) {
      this.seleccionarEvaluacion(this.evaluacionIdInicial);
    }
  }

  seleccionarEvaluacion(valor: number | string): void {
    this.evaluacionId = Number(valor) || 0;
    this.controlSeleccionadoId = 0;
    this.planSeleccionadoId = 0;
    this.controles.set([]);
    this.evaluacionesControl.set([]);
    this.planes.set([]);
    this.actividades.set([]);
    this.error.set(null);
    this.mensaje.set(null);
    this.cdr.markForCheck();
    if (this.evaluacionId > 0) this.cargarMitigacion();
  }

  cargarMitigacion(): void {
    if (this.evaluacionId <= 0) return;
    this.cargando.set(true);
    this.service.listarControles(this.evaluacionId).subscribe({
      next: controles => {
        this.controles.set(controles);
        this.service.listarPlanes(this.evaluacionId).subscribe({
          next: planes => {
            this.planes.set(planes);
            this.cargando.set(false);
            this.cdr.markForCheck();
          },
          error: (error: unknown) => this.finalizarError(error, 'No se pudieron cargar los planes de mitigación.')
        });
      },
      error: (error: unknown) => this.finalizarError(error, 'No se pudieron cargar los controles.')
    });
  }

  editarControl(control: ControlRiesgoDto): void {
    this.controlEditandoId = control.conId;
    this.controlSeleccionadoId = control.conId;
    this.controlTipo = control.conTipo;
    this.controlDescripcion = control.conDescripcion;
    this.controlAutomatizacion = control.conAutomatizacion;
    this.controlEstado = control.conEstado;
    this.controlSnapshot = this.estadoControlFormulario();
    this.modalControlAbierto.set(true);
    this.cargarEvaluacionesControl(control.conId);
  }

  nuevoControl(): void {
    this.resetControlForm();
    this.controlSnapshot = this.estadoControlFormulario();
    this.modalControlAbierto.set(true);
  }

  async guardarControl(): Promise<void> {
    if (this.operacionEnCurso()) return;
    if (this.evaluacionId <= 0 || !this.controlDescripcion.trim()) {
      this.error.set('Seleccione una evaluación y describa el control.');
      return;
    }

    const confirmado = await this.confirmarGuardado(
      this.controlEditandoId > 0 ? 'Editar Control' : 'Crear Control',
      this.controlEditandoId > 0
        ? '¿Desea guardar los cambios del control seleccionado?'
        : '¿Desea guardar este nuevo control?',
      this.controlEditandoId > 0 ? 'Guardar cambios' : 'Confirmar guardado'
    );
    if (!confirmado) return;

    const dto: ControlRiesgoGuardarDto = {
      conEvaluacionId: this.evaluacionId,
      conTipo: this.controlTipo,
      conDescripcion: this.controlDescripcion.trim(),
      conAutomatizacion: this.controlAutomatizacion,
      conEstado: this.controlEstado.trim() || 'ACTIVO',
      conEstadoMonitoreo: null,
      conEfectividadMonitoreo: null
    };
    this.guardando.set(true);
    const solicitud: Observable<unknown> = this.controlEditandoId > 0
      ? this.service.actualizarControl(this.controlEditandoId, dto)
      : this.service.crearControl(dto);
    solicitud.subscribe({
      next: () => {
        this.guardando.set(false);
        this.mensaje.set(this.controlEditandoId > 0 ? 'Control actualizado correctamente.' : 'Control creado correctamente.');
        this.cerrarModalControlForzado();
        this.cargarMitigacion();
        this.persisted.emit();
      },
      error: (error: unknown) => this.finalizarGuardadoError(error, 'No se pudo guardar el control.')
    });
  }

  cargarEvaluacionesControl(controlId: number): void {
    this.controlSeleccionadoId = controlId;
    this.service.listarEvaluacionesControl(controlId).subscribe({
      next: items => this.evaluacionesControl.set(items.map(item => ({
        ...item,
        ecoComentario: item.ecoComentario ? normalizarMojibakeVisibleUtf8(item.ecoComentario) : item.ecoComentario
      }))),
      error: (error: unknown) => this.error.set(this.mensajeError(error, 'No se pudo cargar la efectividad del control.'))
    });
  }

  async evaluarControl(): Promise<void> {
    await this.guardarEfectividad();
  }

  async guardarEfectividad(): Promise<void> {
    if (this.operacionEnCurso()) return;
    if (this.controlSeleccionadoId <= 0 || this.efectividad < 0 || this.efectividad > 100) {
      this.error.set('Seleccione un control y registre una efectividad entre 0 y 100.');
      return;
    }
    const confirmado = await this.confirmarGuardado(
      'Registrar efectividad',
      '¿Desea registrar la efectividad del control seleccionado?',
      'Registrar efectividad'
    );
    if (!confirmado) return;
    const dto: EvaluacionControlGuardarDto = {
      ecoEfectividad: Number(this.efectividad),
      ecoComentario: this.comentarioEfectividad.trim() || null
    };
    this.guardando.set(true);
    this.service.evaluarControl(this.controlSeleccionadoId, dto).subscribe({
      next: () => {
        this.guardando.set(false);
        this.efectividad = 0;
        this.comentarioEfectividad = '';
        this.mensaje.set('Efectividad del control registrada correctamente.');
        this.cargarEvaluacionesControl(this.controlSeleccionadoId);
        this.modalEfectividadAbierto.set(false);
      },
      error: (error: unknown) => this.finalizarGuardadoError(error, 'No se pudo registrar la efectividad del control.')
    });
  }

  editarPlan(plan: PlanMitigacionDto): void {
    this.planEditandoId = plan.plaId;
    this.planSeleccionadoId = plan.plaId;
    this.planDescripcion = plan.plaDescripcion;
    this.planAvance = plan.plaAvance;
    this.planPresupuesto = plan.plaPresupuesto;
    this.planFechaInicio = this.fechaInput(plan.plaFechaInicio);
    this.planFechaFin = this.fechaInput(plan.plaFechaFin);
    this.planEstado = plan.plaEstado;
    this.planMonitoreoSeguimiento = plan.plaMonitoreoSeguimiento ?? '';
    this.planResponsables = plan.plaResponsables ?? '';
    this.planRecursos = plan.plaRecursos ?? '';
    this.planSnapshot = this.estadoPlanFormulario();
    this.modalPlanAbierto.set(true);
    this.cargarActividades(plan.plaId);
  }

  nuevoPlan(): void {
    this.resetPlanForm();
    this.planSnapshot = this.estadoPlanFormulario();
    this.modalPlanAbierto.set(true);
  }

  async guardarPlan(): Promise<void> {
    if (this.operacionEnCurso()) return;
    if (this.evaluacionId <= 0 || !this.planDescripcion.trim()) {
      this.error.set('Seleccione una evaluación y describa el plan.');
      return;
    }
    if (this.planAvance < 0 || this.planAvance > 100 || this.planPresupuesto < 0 || this.planFechaFin < this.planFechaInicio) {
      this.error.set('Revise avance, presupuesto y rango de fechas del plan.');
      return;
    }
    const confirmado = await this.confirmarGuardado(
      this.planEditandoId > 0 ? 'Editar Plan de Mitigación' : 'Crear Plan de Mitigación',
      this.planEditandoId > 0
        ? '¿Desea guardar los cambios del plan seleccionado?'
        : '¿Desea guardar este nuevo plan de mitigación?',
      this.planEditandoId > 0 ? 'Guardar cambios' : 'Confirmar guardado'
    );
    if (!confirmado) return;
    const dto: PlanMitigacionGuardarDto = {
      plaEvaluacionId: this.evaluacionId,
      plaDescripcion: this.planDescripcion.trim(),
      plaAvance: Number(this.planAvance),
      plaPresupuesto: Number(this.planPresupuesto),
      plaFechaInicio: this.fechaIso(this.planFechaInicio),
      plaFechaFin: this.fechaIso(this.planFechaFin),
      plaEstado: this.planEstado.trim() || 'PENDIENTE',
      plaMonitoreoSeguimiento: this.planMonitoreoSeguimiento.trim() || null,
      plaResponsables: this.planResponsables.trim() || null,
      plaRecursos: this.planRecursos.trim() || null
    };
    this.guardando.set(true);
    const solicitud: Observable<unknown> = this.planEditandoId > 0
      ? this.service.actualizarPlan(this.planEditandoId, dto)
      : this.service.crearPlan(dto);
    solicitud.subscribe({
      next: () => {
        this.guardando.set(false);
        this.mensaje.set(this.planEditandoId > 0 ? 'Plan actualizado correctamente.' : 'Plan creado correctamente.');
        this.cerrarModalPlanForzado();
        this.cargarMitigacion();
        this.persisted.emit();
      },
      error: (error: unknown) => this.finalizarGuardadoError(error, 'No se pudo guardar el plan de mitigación.')
    });
  }

  cargarActividades(planId: number): void {
    this.planSeleccionadoId = planId;
    this.service.listarActividades(planId).subscribe({
      next: actividades => this.actividades.set(actividades),
      error: (error: unknown) => this.error.set(this.mensajeError(error, 'No se pudieron cargar las actividades.'))
    });
  }

  editarActividad(actividad: ActividadPlanDto): void {
    this.actividadEditandoId = actividad.actId;
    this.planSeleccionadoId = actividad.actPlanId;
    this.actividadDescripcion = actividad.actDescripcion;
    this.actividadResponsable = actividad.actResponsable;
    this.actividadAvance = actividad.actAvance;
    this.actividadFechaInicio = this.fechaInput(actividad.actFechaInicio);
    this.actividadFechaFin = this.fechaInput(actividad.actFechaFin);
    this.actividadEstado = actividad.actEstado;
    this.actividadSnapshot = this.estadoActividadFormulario();
    this.modalActividadAbierto.set(true);
  }

  nuevaActividad(): void {
    if (this.planSeleccionadoId <= 0) {
      this.error.set('Seleccione un plan antes de crear una actividad.');
      return;
    }
    this.resetActividadForm();
    this.actividadSnapshot = this.estadoActividadFormulario();
    this.modalActividadAbierto.set(true);
  }

  async guardarActividad(): Promise<void> {
    if (this.operacionEnCurso()) return;
    if (this.planSeleccionadoId <= 0 || !this.actividadDescripcion.trim() || !this.actividadResponsable.trim()) {
      this.error.set('Seleccione un plan y complete descripción y responsable de la actividad.');
      return;
    }
    if (this.actividadAvance < 0 || this.actividadAvance > 100 || this.actividadFechaFin < this.actividadFechaInicio) {
      this.error.set('Revise avance y rango de fechas de la actividad.');
      return;
    }
    const confirmado = await this.confirmarGuardado(
      this.actividadEditandoId > 0 ? 'Editar Actividad' : 'Crear Actividad',
      this.actividadEditandoId > 0
        ? '¿Desea guardar los cambios de la actividad seleccionada?'
        : '¿Desea guardar esta nueva actividad?',
      this.actividadEditandoId > 0 ? 'Guardar cambios' : 'Confirmar guardado'
    );
    if (!confirmado) return;
    const dto: ActividadPlanGuardarDto = {
      actPlanId: this.planSeleccionadoId,
      actDescripcion: this.actividadDescripcion.trim(),
      actResponsable: this.actividadResponsable.trim(),
      actAvance: Number(this.actividadAvance),
      actFechaInicio: this.fechaIso(this.actividadFechaInicio),
      actFechaFin: this.fechaIso(this.actividadFechaFin),
      actEstado: this.actividadEstado.trim() || 'PENDIENTE'
    };
    this.guardando.set(true);
    const solicitud: Observable<unknown> = this.actividadEditandoId > 0
      ? this.service.actualizarActividad(this.actividadEditandoId, dto)
      : this.service.crearActividad(dto);
    solicitud.subscribe({
      next: () => {
        this.guardando.set(false);
        this.mensaje.set(this.actividadEditandoId > 0 ? 'Actividad actualizada correctamente.' : 'Actividad creada correctamente.');
        this.cerrarModalActividadForzado();
        this.cargarActividades(this.planSeleccionadoId);
        this.persisted.emit();
      },
      error: (error: unknown) => this.finalizarGuardadoError(error, 'No se pudo guardar la actividad.')
    });
  }

  private hoy(): string {
    return new Date().toISOString().slice(0, 10);
  }

  private fechaIso(valor: string): string {
    return new Date(`${valor}T00:00:00Z`).toISOString();
  }

  private fechaInput(valor: string): string {
    return valor?.slice(0, 10) || this.hoy();
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

  operacionEnCurso(): boolean {
    return this.guardando() || this.confirmando();
  }

  controlFormularioValido(): boolean {
    return this.evaluacionId > 0 && this.controlDescripcion.trim().length > 0;
  }

  efectividadFormularioValida(): boolean {
    return this.controlSeleccionadoId > 0 && Number.isFinite(Number(this.efectividad))
      && this.efectividad >= 0 && this.efectividad <= 100;
  }

  planFormularioValido(): boolean {
    return this.evaluacionId > 0
      && this.planDescripcion.trim().length > 0
      && this.planAvance >= 0 && this.planAvance <= 100
      && this.planPresupuesto >= 0
      && !!this.planFechaInicio && !!this.planFechaFin
      && this.planFechaFin >= this.planFechaInicio;
  }

  actividadFormularioValida(): boolean {
    return this.planSeleccionadoId > 0
      && this.actividadDescripcion.trim().length > 0
      && this.actividadResponsable.trim().length > 0
      && this.actividadAvance >= 0 && this.actividadAvance <= 100
      && !!this.actividadFechaInicio && !!this.actividadFechaFin
      && this.actividadFechaFin >= this.actividadFechaInicio;
  }

  abrirModalEfectividad(): void {
    if (this.controlSeleccionadoId <= 0) return;
    this.efectividadSnapshot = this.estadoEfectividadFormulario();
    this.modalEfectividadAbierto.set(true);
  }

  async solicitarCierreControl(): Promise<void> {
    if (this.operacionEnCurso()) return;
    if (this.estadoControlFormulario() !== this.controlSnapshot) {
      const confirmado = await this.confirmation.confirm({
        title: 'Descartar cambios del control',
        message: '¿Desea descartar los cambios realizados?',
        confirmLabel: 'Descartar cambios',
        cancelLabel: 'Continuar editando',
        confirmIcon: 'delete',
        confirmVariant: 'danger',
        icon: 'warning'
      });
      if (!confirmado) return;
    }
    this.cerrarModalControlForzado();
  }

  async solicitarCierreEfectividad(): Promise<void> {
    if (this.operacionEnCurso()) return;
    if (this.estadoEfectividadFormulario() !== this.efectividadSnapshot) {
      const confirmado = await this.confirmation.confirm({
        title: 'Descartar efectividad',
        message: '¿Desea descartar la efectividad introducida?',
        confirmLabel: 'Descartar',
        cancelLabel: 'Continuar editando',
        confirmIcon: 'delete',
        confirmVariant: 'danger',
        icon: 'warning'
      });
      if (!confirmado) return;
    }
    this.modalEfectividadAbierto.set(false);
  }

  async solicitarCierrePlan(): Promise<void> {
    if (this.operacionEnCurso()) return;
    if (this.estadoPlanFormulario() !== this.planSnapshot) {
      const confirmado = await this.confirmation.confirm({
        title: 'Descartar cambios del plan',
        message: '¿Desea descartar los cambios realizados?',
        confirmLabel: 'Descartar cambios',
        cancelLabel: 'Continuar editando',
        confirmIcon: 'delete',
        confirmVariant: 'danger',
        icon: 'warning'
      });
      if (!confirmado) return;
    }
    this.cerrarModalPlanForzado();
  }

  async solicitarCierreActividad(): Promise<void> {
    if (this.operacionEnCurso()) return;
    if (this.estadoActividadFormulario() !== this.actividadSnapshot) {
      const confirmado = await this.confirmation.confirm({
        title: 'Descartar cambios de la actividad',
        message: '¿Desea descartar los cambios realizados?',
        confirmLabel: 'Descartar cambios',
        cancelLabel: 'Continuar editando',
        confirmIcon: 'delete',
        confirmVariant: 'danger',
        icon: 'warning'
      });
      if (!confirmado) return;
    }
    this.cerrarModalActividadForzado();
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

  private estadoControlFormulario(): string {
    return JSON.stringify([this.evaluacionId, this.controlEditandoId, this.controlTipo, this.controlDescripcion, this.controlAutomatizacion, this.controlEstado]);
  }

  private estadoEfectividadFormulario(): string {
    return JSON.stringify([this.controlSeleccionadoId, this.efectividad, this.comentarioEfectividad]);
  }

  private estadoPlanFormulario(): string {
    return JSON.stringify([this.evaluacionId, this.planEditandoId, this.planDescripcion, this.planAvance, this.planPresupuesto, this.planFechaInicio, this.planFechaFin, this.planEstado, this.planMonitoreoSeguimiento, this.planResponsables, this.planRecursos]);
  }

  private estadoActividadFormulario(): string {
    return JSON.stringify([this.planSeleccionadoId, this.actividadEditandoId, this.actividadDescripcion, this.actividadResponsable, this.actividadAvance, this.actividadFechaInicio, this.actividadFechaFin, this.actividadEstado]);
  }

  private resetControlForm(): void {
    this.controlEditandoId = 0;
    this.controlSeleccionadoId = 0;
    this.controlTipo = 'PREVENTIVO';
    this.controlDescripcion = '';
    this.controlAutomatizacion = 'MANUAL';
    this.controlEstado = 'ACTIVO';
    this.efectividad = 0;
    this.comentarioEfectividad = '';
    this.evaluacionesControl.set([]);
  }

  private resetPlanForm(): void {
    this.planEditandoId = 0;
    this.planSeleccionadoId = 0;
    this.planDescripcion = '';
    this.planAvance = 0;
    this.planPresupuesto = 0;
    this.planFechaInicio = this.hoy();
    this.planFechaFin = this.hoy();
    this.planEstado = 'PENDIENTE';
    this.planMonitoreoSeguimiento = '';
    this.planResponsables = '';
    this.planRecursos = '';
    this.actividades.set([]);
  }

  private resetActividadForm(): void {
    this.actividadEditandoId = 0;
    this.actividadDescripcion = '';
    this.actividadResponsable = '';
    this.actividadAvance = 0;
    this.actividadFechaInicio = this.hoy();
    this.actividadFechaFin = this.hoy();
    this.actividadEstado = 'PENDIENTE';
  }

  cerrarModalControlForzado(): void {
    this.modalControlAbierto.set(false);
    this.resetControlForm();
  }

  cerrarModalPlanForzado(): void {
    this.modalPlanAbierto.set(false);
    this.resetPlanForm();
  }

  cerrarModalActividadForzado(): void {
    this.modalActividadAbierto.set(false);
    this.resetActividadForm();
  }
}
