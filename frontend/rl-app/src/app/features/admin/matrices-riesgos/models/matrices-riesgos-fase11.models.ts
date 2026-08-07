export interface RiesgoDto {
  rieId: number;
  rieCodigo: string;
  rieNombre: string;
  rieDescripcion?: string | null;
  rieActivo: boolean;
  rieUsrCreacion: number;
  rieFechaCreacion: string;
}

export interface RiesgoGuardarDto {
  rieCodigo: string;
  rieNombre: string;
  rieDescripcion?: string | null;
  rieActivo: boolean;
}

export interface ControlRiesgoDto {
  conId: number;
  conEvaluacionId: number;
  conTipo: 'PREVENTIVO' | 'DETECTIVO' | 'CORRECTIVO';
  conDescripcion: string;
  conAutomatizacion: 'MANUAL' | 'SEMIAUTOMATICO' | 'AUTOMATICO';
  conEstado: string;
}

export type ControlRiesgoGuardarDto = Omit<ControlRiesgoDto, 'conId'>;

export interface EvaluacionControlDto {
  ecoId: number;
  ecoControlId: number;
  ecoEfectividad: number;
  ecoComentario?: string | null;
}

export interface EvaluacionControlGuardarDto {
  ecoEfectividad: number;
  ecoComentario?: string | null;
}

export interface PlanMitigacionDto {
  plaId: number;
  plaEvaluacionId: number;
  plaDescripcion: string;
  plaAvance: number;
  plaPresupuesto: number;
  plaFechaInicio: string;
  plaFechaFin: string;
  plaEstado: string;
}

export type PlanMitigacionGuardarDto = Omit<PlanMitigacionDto, 'plaId'>;

export interface ActividadPlanDto {
  actId: number;
  actPlanId: number;
  actDescripcion: string;
  actResponsable: string;
  actAvance: number;
  actFechaInicio: string;
  actFechaFin: string;
  actEstado: string;
}

export type ActividadPlanGuardarDto = Omit<ActividadPlanDto, 'actId'>;

export interface SenalAlertaDto {
  aleId: number;
  aleEvaluacionId: number;
  aleCodigo: string;
  aleIndicador: string;
  aleEstado: 'ACTIVO' | 'INACTIVO';
  aleFechaDisparo?: string | null;
}

export interface SenalAlertaGuardarDto {
  aleEvaluacionId: number;
  aleCodigo: string;
  aleIndicador: string;
  aleEstado: 'ACTIVO' | 'INACTIVO';
}

export interface AutomonitoreoDto {
  monId: number;
  monEvaluacionId: number;
  monEstadoRiesgo: string;
  monEstadoContr: string;
  monResultado: string;
  monUsrId: number;
  monFecha: string;
}

export interface AutomonitoreoGuardarDto {
  monEvaluacionId: number;
  monEstadoRiesgo: string;
  monEstadoContr: string;
  monResultado: string;
}

export interface ResumenMatricesOperativoDto {
  fechaGeneracion: string;
  riesgosActivos: number;
  evaluacionesActivas: number;
  evaluacionesAprobadas: number;
  riesgosAltoCritico: number;
  alertasActivas: number;
  planesAbiertos: number;
  actividadesVencidas: number;
  automonitoreosUltimos30Dias: number;
}
