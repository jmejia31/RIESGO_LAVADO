export interface Rol {
  rolId: number;
  rolNombre: string;
}


export interface Dominio {
  domId: number;
  domNombre: string;
  domDescripcion?: string;
}

export interface Modulo {
  modId: number;
  modNombre: string;
  modDescripcion?: string;
  modRuta: string;
  modIcono: string;
  modSeccion: string;
}

export interface ElementoCatalogoMatrices {
  id: number;
  codigo: string;
  valor: string;
  orden: number;
  activo: boolean;
}

export interface CatalogoMatrices {
  id: number;
  codigo: string;
  nombre: string;
  activo: boolean;
  elementos: ElementoCatalogoMatrices[];
}
