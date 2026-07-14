export interface AuditoriaDto {
  audId: number;
  tabla: string;
  registroId: string;
  accion: string;
  datosAnt?: string;
  datosNvo?: string;
  usrId?: number;
  usrEmail?: string;
  ip?: string;
  fecha: string;
  modulo?: string;
}

export interface AuditoriaPaginado {
  datos: AuditoriaDto[];
  totalRegistros: number;
}
