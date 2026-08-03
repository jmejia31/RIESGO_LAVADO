export type EstadoFormulario =
  | 'DRAFT'
  | 'IN_REVIEW'
  | 'APPROVED'
  | 'PUBLISHED'
  | 'RETIRED'
  | 'ARCHIVED';

export interface VersionFormularioDto {
  verId: number;
  verFamiliaId: number;
  verCodigo: string;
  verVersion: number;
  verJson: string;
  verHash: string;
  verEstado: EstadoFormulario;
  verVigente: boolean;
  verFechaInicio?: string | null;
  verFechaFin?: string | null;
  verFechaCreacion: string;
  verUsrCreacion: number;
}

export interface CampoFormulario {
  campoCanonicoId?: number | null;
  clave: string;
  etiqueta: string;
  tipo: string;
  codigoCatalogo?: string | null;
  obligatorio: boolean;
  soloLectura: boolean;
}

export interface SeccionFormulario {
  clave: string;
  titulo: string;
  orden: number;
  campos: CampoFormulario[];
}

export interface ElementoCatalogoMatrices {
  codigo: string;
  valor: string;
  orden: number;
}

export interface CatalogoMatrices {
  codigo: string;
  nombre: string;
  elementos: ElementoCatalogoMatrices[];
}

export interface ReglaCalculoMatrices {
  codigo: string;
  version: string;
  algoritmoId: string;
  parametros?: unknown;
}

export interface MetodologiaFormulario {
  versionFormularioId: number;
  codigo: string;
  version: number;
  secciones: SeccionFormulario[];
  catalogos: CatalogoMatrices[];
  reglas: ReglaCalculoMatrices[];
}

export interface EvaluacionRiesgoDto {
  evaId: number;
  evaRiesgoId: number;
  evaVersionId: number;
  evaEstado: string;
  evaDataJson: string;
  evaDataCalcJson?: string | null;
  evaVri?: number | null;
  evaVrr?: number | null;
  evaFechaEval: string;
  evaUsrEval: number;
  evaVersionRow: number;
  evaActivo: boolean;
}

export interface ConsultaEvaluacionPaginadaDto {
  riesgoId?: number;
  buscar?: string;
  estado?: string;
  pagina: number;
  registrosPorPagina: number;
}

export interface RevisionEvaluacionDto {
  revId: number;
  revEvaluacionId: number;
  revEstadoAnterior: string;
  revEstadoNuevo: string;
  revMotivo?: string | null;
  revFecha: string;
  revUsrId: number;
}

export interface EvidenciaDto {
  eviId: number;
  eviNombreArchivo: string;
  eviExtension: string;
  eviTamano: number;
  eviHash: string;
  eviRuta: string;
  eviUsrCreacion: number;
  eviFechaCreacion: string;
}

export interface AsociarEvidenciaRiesgoDto {
  evrRiesgoId: number;
  evrEvidenciaId: number;
}

export interface AsociarEvidenciaEvaluacionDto {
  eveEvaluacionId: number;
  eveEvidenciaId: number;
}

export interface AsociarEvidenciaControlDto {
  evcControlId: number;
  evcEvidenciaId: number;
}

export interface AsociarEvidenciaPlanDto {
  evpPlanId: number;
  evpEvidenciaId: number;
}

export interface AsociarEvidenciaActividadDto {
  evaActividadId: number;
  evaEvidenciaId: number;
}

export interface AsociarEvidenciaAlertaDto {
  evaAlertaId: number;
  evaEvidenciaId: number;
}

export interface AsociarEvidenciaAutomonitoreoDto {
  evmMonitoreoId: number;
  evmEvidenciaId: number;
}

export interface AsociarEvidenciaRevisionDto {
  evvRevisionId: number;
  evvEvidenciaId: number;
}

export interface AsociarEvidenciaAprobacionDto {
  evapAprobacionId: number;
  evapEvidenciaId: number;
}

export interface RiesgoReporteFila {
  riesgoId: number;
  evaluacionId: number;
  versionFormularioId: number;
  codigoRiesgo: string;
  areaPrincipal: string;
  duenoRiesgo: string;
  vri: number;
  nivelInherente: string;
  vrr: number;
  nivelResidual: string;
  respuestaRiesgo: string;
  estadoEvaluacion: string;
  fechaEvaluacion: string;
}

export interface ReporteMatricesTotales {
  totalRiesgos: number;
  totalConEvaluacionOficial: number;
  totalSinEvaluacionOficial: number;
  totalAltoCritico: number;
}

export interface ReporteMatricesPaginado {
  items: RiesgoReporteFila[];
  pagina: number;
  tamanoPagina: number;
  totalRegistros: number;
  totalPaginas: number;
  totales: ReporteMatricesTotales;
}

export interface MapaTransicionCelda {
  nivelInherente: string;
  nivelResidual: string;
  total: number;
  promedioInherente: number;
  promedioResidual: number;
}

export interface MatrizRiesgoDashboardDinamico {
  fechaGeneracion: string;
  totalRiesgos: number;
  totalConEvaluacionOficial: number;
  totalSinEvaluacionOficial: number;
  mapaTransicion: MapaTransicionCelda[];
  pendientesOperativos: RiesgoReporteFila[];
}

export interface EvidenciaPoliticaDto {
  maximoMb: number;
  maximoBytes: number;
  extensionesPermitidas: string;
  tiposPermitidosTexto: string;
}

export type RespuestasFormulario = Record<string, string | number | boolean | null>;

export interface DefinicionFormularioEditable {
  codigoFormulario: string;
  nombreFormulario: string;
  secciones: Array<{
    clave: string;
    titulo: string;
    orden: number;
    campos: CampoFormulario[];
  }>;
  reglas?: Array<{ codigo: string; version: string }>;
}
