export type EstadoConfiguracion = 'DRAFT' | 'IN_REVIEW' | 'APPROVED' | 'PUBLISHED' | 'RETIRED' | 'ARCHIVED';
export type EstadoMasterConfiguracion = 'ACTIVE' | 'INACTIVE' | 'RETIRED';

export interface FormulaDto {
  id: number;
  codigo: string;
  nombre: string;
  descripcion?: string | null;
  estado: string;
  fechaCreacion?: string;
  versionRow: number;
}

export interface FormulaVersionDto {
  id: number;
  formulaId: number;
  version: number;
  expresion: string;
  tipoResultado: string;
  estado: EstadoConfiguracion;
  hash: string;
  fechaInicio?: string | null;
  fechaFin?: string | null;
  fechaCreacion?: string;
  versionRow: number;
}

export interface FormulaUsageDto {
  id: number;
  versionFormularioId: number;
  campoClave: string;
  formulaVersionId: number;
  formulaVersion: number;
  formulaCodigo: string;
}

export interface CrearFormulaVersionDto {
  expresion: string;
  tipoResultado: string;
}

export interface CrearFormulaDto {
  codigo: string;
  nombre: string;
  descripcion?: string | null;
  versionInicial: CrearFormulaVersionDto;
}

export interface ActualizarFormulaBorradorDto extends CrearFormulaVersionDto {
  versionRow: number;
}

export interface CrearFormulaUsoDto {
  versionFormularioId: number;
  campoClave: string;
  formulaVersionId: number;
}

export interface FuncionDto {
  id: number;
  codigo: string;
  nombre: string;
  descripcion?: string | null;
  categoria: string;
  estado: string;
  versionRow: number;
}

export type TipoFuncion = 'NATIVE' | 'COMPOSITE';

export interface FuncionArgumentoDto {
  id: number;
  funcionVersionId: number;
  posicion: number;
  codigo: string;
  nombre: string;
  tipo: string;
  requerido: boolean;
  variadic: boolean;
  valorDefaultJson?: string | null;
  descripcion?: string | null;
}

export interface FuncionVersionDto {
  id: number;
  funcionId: number;
  version: number;
  tipo: TipoFuncion;
  tipoResultado: string;
  signatureJson?: string | null;
  definicionDsl?: string | null;
  handlerKey?: string | null;
  minArity: number;
  maxArity?: number | null;
  estado: EstadoConfiguracion;
  hash: string;
  versionRow: number;
}

export interface FuncionArgumentoGuardarDto {
  posicion: number;
  codigo: string;
  nombre: string;
  tipo: string;
  requerido: boolean;
  variadic: boolean;
  valorDefaultJson?: string | null;
  descripcion?: string | null;
}

export interface CrearFuncionVersionDto {
  tipo: TipoFuncion;
  tipoResultado: string;
  signatureJson?: string | null;
  definicionDsl?: string | null;
  handlerKey?: string | null;
  minArity: number;
  maxArity?: number | null;
  argumentos: FuncionArgumentoGuardarDto[];
}

export interface CrearFuncionDto {
  codigo: string;
  nombre: string;
  descripcion?: string | null;
  categoria?: string;
  versionInicial: CrearFuncionVersionDto;
}

export interface ActualizarFuncionBorradorDto extends CrearFuncionVersionDto {
  versionRow: number;
}

export interface ParametroDto {
  id: number;
  codigo: string;
  nombre: string;
  descripcion?: string | null;
  tipo: string;
  estado: string;
  versionRow: number;
}

export interface ParametroVersionDto {
  id: number;
  parametroId: number;
  version: number;
  tipo: string;
  valorEntero?: number | null;
  valorDecimal?: number | null;
  valorBooleano?: boolean | null;
  valorTexto?: string | null;
  valorFecha?: string | null;
  estado: EstadoConfiguracion;
  hash: string;
  versionRow: number;
}

export interface CrearParametroVersionDto {
  tipo: string;
  valorEntero?: number | null;
  valorDecimal?: number | null;
  valorBooleano?: boolean | null;
  valorTexto?: string | null;
  valorFecha?: string | null;
}

export interface CrearParametroDto {
  codigo: string;
  nombre: string;
  descripcion?: string | null;
  versionInicial: CrearParametroVersionDto;
}

export interface ActualizarParametroBorradorDto extends CrearParametroVersionDto {
  versionRow: number;
}

export interface CambiarEstadoConfiguracionDto {
  estado: string;
  versionRow: number;
}

export interface FormulaVersionSelectorOption {
  formulaId: number;
  formulaVersionId: number;
  codigo: string;
  nombre: string;
  version: number;
  estado: EstadoConfiguracion;
  tipoResultado: string;
  hash: string;
  expresion: string;
  descripcion?: string | null;
}

export interface ReglaCatalogoResumen {
  codigo: string;
  nombre: string;
  version?: string;
  algoritmoId?: string;
  elementos?: number;
}
