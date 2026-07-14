export interface TipoDocumento {
  tipoDocumentoId: number;
  descripcion: string;
}

export interface TipoListaCautela {
  tipoListaCautelaId: number;
  descripcion: string;
  tipoArchivo?: string | null;
  cantidadColumnas?: number | null;
}

export interface ResumenLista {
  tipoListaCautelaId?: number;
  lista: string;
  usuario: string;
  fechaCreacion?: string;
  cantidadRegistros: number;
}

export interface RegistrarPositivoDto {
  tipoDocumentoId: number;
  tipoPositivoId: number;
  noDocumento: string;
  nombreCompleto: string;
  motivoIngreso: string;
  tipoListaCautelaId?: number | null;
  origenRegistro?: string | null;
}

export interface ExistingPositivo {
  tipoDocumentoId: number;
  motivoIngreso: string;
  tipoListaCautelaId?: number | null;
  origenRegistro?: string | null;
  fechaRegistroInterno?: string | null;
}

export interface EvidenciaPolitica {
  maximoMb: number;
  maximoBytes: number;
  extensionesPermitidas: string[];
  tiposPermitidosTexto: string;
}

export interface CoincidenciaJuridica {
  rtn: string;
  nombre: string;
  numeroPatrono: string;
  listaCoincidencia: string;
  fechaEncontro?: string;
  fechaCalifico?: string;
  fechaRegistroInterno?: string | null;
  esProveedorIhss?: string;
  tieneMotivo?: boolean;
  esManual?: boolean;
}

export interface CoincidenciaNatural {
  numeroIdentificacion: string;
  nombre: string;
  listaCoincidencia: string;
  totalRepetidos: number;
  fechaEncontro?: string | null;
  fechaCalifico?: string | null;
  fechaRegistroInterno?: string | null;
  tieneMotivo?: boolean;
  esManual?: boolean;
}

export interface CoincidenciaEmpleado {
  identidad: string;
  nombre: string;
  listaCoincidencia: string;
  totalRepetidos: number;
  fechaEncontro?: string | null;
  fechaCalifico?: string | null;
  fechaRegistroInterno?: string | null;
  tieneMotivo?: boolean;
  esManual?: boolean;
}

export interface DetalleCoincidenciaNatural {
  numeroIdentificacion: string;
  nombresPersona: string;
  tipoCondicionActuaDesc: string;
  numeroPatronal: string;
  nombreEmpresa: string;
  esPep: string;
  listaCoincidencia: string;
  fechaCalifico?: string;
  fechaCoincidencia?: string;
}

export interface DetalleCoincidenciaEmpleado {
  identidad: string;
  nombre: string;
  tipoCondicionActuaDesc: string;
  numeroPatrono: string;
  nombreEmpresa: string;
  razoSoci: string;
  listaCoincidencia: string;
  fechaCalifico?: string;
  fechaCoincidencia?: string;
}

export interface CoincidenciaPatronoResumen {
  fechaEncontro: string;
  cantidadRegistros: number;
}

export type CoincidenciaEmpleadoResumen = CoincidenciaPatronoResumen;

export interface CoincidenciaPatronoDetalle {
  reporteCoincidenciaId: number;
  dataId: number;
  dni: string;
  fechaEncontro: string;
  listaCoincidencia: string;
  nacionalidad: string;
  nombre: string;
  numeroPatrono: string;
  observacionLista: string;
  tipoPersona: string;
  usuarioEncontro: number;
  tipoCalificacion: string;
}

export type CoincidenciaEmpleadoDetalle = CoincidenciaPatronoDetalle;

export interface Evidencia {
  evidenciaId: number;
  nombreArchivo: string;
  tipoMime: string;
}

export interface Seguimiento {
  detalleListaId: number;
  positivoId: number;
  motivoIngreso: string;
  fechaCreacion: string;
  usrCreacionId: number;
  usrEmail: string;
  evidencias: Evidencia[];
}
