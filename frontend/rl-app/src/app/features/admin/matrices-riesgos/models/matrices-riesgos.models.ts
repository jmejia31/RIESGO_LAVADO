export interface MatrizRiesgoFiltro {
  buscar?: string;
  estado?: string;
  sujetoTipo?: string;
  fechaInicio?: string;
  fechaFin?: string;
}

export interface MatrizRiesgoReporteFiltro extends MatrizRiesgoFiltro {
  nivelInherente?: string;
  nivelResidual?: string;
  modeloVersion?: string;
  responsable?: string;
}

export interface FactorInstitucional {
  codigo: string;
  nombre: string;
  pesoInstitucional: number;
  obligatorioGlobal: boolean;
}

export interface VariableMetodologia {
  variableId: number;
  factorId: number;
  factorCodigo: string;
  factorNombre: string;
  codigo: string;
  nombre: string;
  pesoInterno: number;
  obligatoria: boolean;
}

export interface EscalaRiesgo {
  escalaId: number;
  tipo: string;
  nivel: string;
  color: string;
  valorMinimo: number;
  valorMaximo: number;
  requierePlanAccion: boolean;
}

export interface CriterioCalculo {
  criterioId: number;
  factorId: number;
  factorCodigo: string;
  factorNombre: string;
  variableId: number;
  variableCodigo: string;
  variableNombre: string;
  escalaId?: number | null;
  valorDesde?: number | null;
  valorHasta?: number | null;
  puntaje: number;
  descripcion: string;
}

export interface MetodologiaMatrices {
  version: string;
  pesoTotalEsperado: number;
  puntajeMinimo: number;
  puntajeMaximo: number;
  mitigacionMaximaPct: number;
  decimalesCalculo: number;
  decimalesVisualizacion: number;
  factoresInstitucionales: FactorInstitucional[];
  variables: VariableMetodologia[];
  escalasRiesgo: EscalaRiesgo[];
  escalasCatalogo: EscalaRiesgo[];
  criterios: CriterioCalculo[];
  mitigacionesPermitidas: number[];
}

export interface MatrizRiesgoResumen {
  matrizId: number;
  modeloId: number;
  modeloVersion: string;
  sujetoTipo: string;
  sujetoIdExt?: string | null;
  documento?: string | null;
  nombreSujeto: string;
  estado: string;
  fechaEvaluacion: string;
  puntajeInherente?: number | null;
  nivelInherente?: string | null;
  puntajeResidual?: number | null;
  nivelResidual?: string | null;
  requierePlanAccion: boolean;
}

export interface MatrizRiesgoDetalle extends MatrizRiesgoResumen {
  origenDatos: string;
  motivoEstado?: string | null;
  snapshotMetodo?: string | null;
  detalles: MatrizRiesgoVariableDetalle[];
  controles: MatrizRiesgoControl[];
  resultados: MatrizRiesgoResultado[];
  planesAccion: MatrizRiesgoPlanAccion[];
  evidencias: MatrizRiesgoEvidencia[];
}

export interface MatrizRiesgoVariableDetalle {
  detalleId: number;
  variableId: number;
  factorId: number;
  factorCodigo: string;
  factorNombre: string;
  factorPesoInstitucional: number;
  variableCodigo: string;
  variableNombre: string;
  variablePesoInterno: number;
  valorCapturado?: string | null;
  puntaje?: number | null;
  puntajePonderado?: number | null;
  justificacion?: string | null;
  fuenteDato?: string | null;
  obligatoria: boolean;
}

export interface MatrizRiesgoControl {
  controlId: number;
  factorId?: number | null;
  factorCodigo?: string | null;
  nombre: string;
  descripcion?: string | null;
  efectividadPct: number;
  responsable?: string | null;
  estado: string;
  evidenciaObligatoria: boolean;
  tieneEvidencia: boolean;
}

export interface MatrizRiesgoResultado {
  resultadoId: number;
  factorId?: number | null;
  tipoResultado: string;
  versionCalculo: string;
  esVigente: boolean;
  puntajeInherente: number;
  nivelInherente: string;
  mitigacionPct: number;
  puntajeResidual: number;
  nivelResidual: string;
  requierePlanAccion: boolean;
  motivoRecalculo?: string | null;
  fechaCalculo: string;
}

export interface MatrizRiesgoDetalleRequest {
  variableId: number;
  valorCapturado?: string | null;
  puntaje: number;
  justificacion?: string | null;
  fuenteDato?: string | null;
}

export interface MatrizRiesgoControlRequest {
  factorId?: number | null;
  nombre: string;
  descripcion?: string | null;
  periodicidad?: string | null;
  oportunidad?: string | null;
  automatizacion?: string | null;
  procedimientos?: string | null;
  calidad?: string | null;
  efectividadPct: number;
  responsable?: string | null;
  evidenciaObligatoria: boolean;
}

export interface MatrizRiesgoCrearRequest {
  sujetoTipo: string;
  sujetoIdExt?: string | null;
  documento?: string | null;
  nombreSujeto: string;
  origenDatos: string;
  detalles: MatrizRiesgoDetalleRequest[];
  controles: MatrizRiesgoControlRequest[];
}

export interface MatrizRiesgoMapaTransicion {
  nivelInherente: string;
  nivelResidual: string;
  total: number;
  promedioInherente: number;
  promedioResidual: number;
}

export interface MatrizRiesgoDashboard {
  fechaGeneracion: string;
  filtro: MatrizRiesgoReporteFiltro;
  totalMatrices: number;
  totalCalculadas: number;
  totalSinCalculo: number;
  totalCerradas: number;
  totalConPlanAccion: number;
  totalAltoCritico: number;
  totalPlanesVencidos: number;
  porEstado: { nombre: string; total: number }[];
  porSujetoTipo: { nombre: string; total: number }[];
  porNivelInherente: { nombre: string; total: number }[];
  porNivelResidual: { nombre: string; total: number }[];
  mapaTransicion: MatrizRiesgoMapaTransicion[];
  matricesCriticas: MatrizRiesgoResumen[];
  matricesFiltradas: MatrizRiesgoResumen[];
  planesAccion: { estado: string; total: number; vencidos: number }[];
}

export interface MatricesRiesgoReporte {
  fechaGeneracion: string;
  filtro: MatrizRiesgoReporteFiltro;
  totales: {
    totalMatrices: number;
    totalCalculadas: number;
    totalCerradas: number;
    totalAltoCritico: number;
    totalPlanAccionRequerido: number;
    totalPlanesVencidos: number;
  };
  porEstado: { nombre: string; total: number }[];
  porNivelResidual: { nombre: string; total: number }[];
  porSujetoTipo: { nombre: string; total: number }[];
  porFactor: {
    factorId: number;
    factorCodigo: string;
    factorNombre: string;
    totalMatrices: number;
    promedioInherente: number;
    promedioResidual: number;
    totalAltoCritico: number;
    totalPlanAccionRequerido: number;
  }[];
  mapaInherente: { nivel: string; total: number; promedio: number }[];
  mapaResidual: { nivel: string; total: number; promedio: number }[];
  matricesFiltradas: MatrizRiesgoResumen[];
  matricesCriticas: MatrizRiesgoResumen[];
  planesAccion: { estado: string; total: number; vencidos: number }[];
}

export interface MatrizRiesgoHistorial {
  historialId: number;
  matrizId?: number | null;
  tabla: string;
  registroId: string;
  accion: string;
  estadoAnterior?: string | null;
  estadoNuevo?: string | null;
  motivo?: string | null;
  usuarioEmail?: string | null;
  ip?: string | null;
  fecha: string;
}

export interface MatrizRiesgoCriterio {
  criterioId: number;
  factorId: number;
  factorCodigo: string;
  factorNombre: string;
  variableId: number;
  variableCodigo: string;
  variableNombre: string;
  escalaId?: number | null;
  escalaTipo?: string | null;
  escalaNivel?: string | null;
  valorDesde?: number | null;
  valorHasta?: number | null;
  puntaje: number;
  descripcion: string;
  activo: boolean;
  motivoInactivo?: string | null;
}

export interface MatrizRiesgoCriterioRequest {
  variableId: number;
  escalaId?: number | null;
  valorDesde?: number | null;
  valorHasta?: number | null;
  puntaje: number;
  descripcion: string;
}

export interface MatrizRiesgoPlanAccion {
  planId: number;
  matrizId: number;
  resultadoId?: number | null;
  actividad: string;
  responsable: string;
  periodicidad?: string | null;
  fechaInicio?: string | null;
  fechaFin?: string | null;
  medioPrueba?: string | null;
  observaciones?: string | null;
  estado: string;
  motivoCierre?: string | null;
  fechaCreacion: string;
  fechaCierre?: string | null;
  vencido: boolean;
}

export interface MatrizRiesgoPlanAccionRequest {
  resultadoId?: number | null;
  actividad: string;
  responsable: string;
  periodicidad?: string | null;
  fechaInicio?: string | null;
  fechaFin?: string | null;
  medioPrueba?: string | null;
  observaciones?: string | null;
}

export interface MatrizRiesgoEvidencia {
  evidenciaId: number;
  matrizId: number;
  controlId?: number | null;
  planId?: number | null;
  nombreOriginal: string;
  nombreFisico: string;
  tipoMime?: string | null;
  extension?: string | null;
  tamanoBytes: number;
  hashSha256?: string | null;
  activa: boolean;
  motivoInactivo?: string | null;
  fechaCreacion: string;
}
