export interface ResultadoValidacionAd {
  existe: boolean;
  bloqueado: boolean;
  activo: boolean;
  nombreCompleto?: string;
  mensaje: string;
}
