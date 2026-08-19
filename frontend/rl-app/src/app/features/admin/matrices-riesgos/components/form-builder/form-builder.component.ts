import { Component, EventEmitter, Input, OnInit, Output, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
  CampoBuilderModel,
  CatalogoBuilderModel,
  ElementoCatalogoBuilderModel,
  FormBuilderModel,
  SeccionBuilderModel,
  TIPOS_CONTROLES_DISPONIBLES,
  TipoControlDefinicion,
  normalizarJsonABuilderModel,
  serializarBuilderModelAJson
} from '../../models/form-builder.models';
import { validarFormBuilderModel, FormBuilderValidationError } from '../../utils/form-builder-validator.util';

export interface CatalogoEdicionForm {
  codigoOriginal: string | null;
  codigo: string;
  nombre: string;
  esNuevo: boolean;
}

export interface ElementoEdicionForm {
  codigoOriginal: string | null;
  codigo: string;
  valor: string;
  orden: number;
  indice: number | null;
}

export interface FeedbackCatalogo {
  tipo: 'error' | 'exito' | 'info';
  mensaje: string;
}

@Component({
  selector: 'app-form-builder',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './form-builder.component.html',
  styleUrls: ['./form-builder.component.scss']
})
export class FormBuilderComponent implements OnInit {
  @Input() jsonDefinicion: string = '';
  @Input() soloLectura: boolean = false;
  @Input() esAdministrador: boolean = false;
  @Input() versionCodigo: string = 'V1.0';
  @Input() versionNumero: number = 1;
  @Input() catalogosDisponibles: Array<{ codigo: string; nombre: string }> = [
    { codigo: 'CAT_COINCIDENCIA_NIVEL', nombre: 'Nivel de Coincidencia' },
    { codigo: 'CAT_ESTADO_EVALUACION', nombre: 'Estado de Evaluación' },
    { codigo: 'CAT_TIPO_LISTA', nombre: 'Tipo de Lista de Riesgo' },
    { codigo: 'MR_PROBABILIDAD_1_5', nombre: 'Probabilidad (1 a 5)' },
    { codigo: 'MR_IMPACTO_1_5', nombre: 'Impacto (1 a 5)' }
  ];

  @Output() guardarJson = new EventEmitter<string>();
  @Output() cerrar = new EventEmitter<void>();

  readonly model = signal<FormBuilderModel>({
    codigoFormulario: 'FORM_DINAMICO',
    nombreFormulario: 'Formulario Dinámico',
    secciones: [],
    catalogos: []
  });

  readonly tiposControles = TIPOS_CONTROLES_DISPONIBLES;
  readonly seccionActivaId = signal<string | null>(null);
  readonly campoActivo = signal<CampoBuilderModel | null>(null);
  readonly mostrarJsonAvanzado = signal<boolean>(false);
  readonly jsonAvanzadoStr = signal<string>('');
  readonly erroresValidacion = signal<FormBuilderValidationError[]>([]);

  // Estado de Navegación y Vistas
  readonly vistaActiva = signal<'secciones' | 'catalogos'>('secciones');

  // Estado de Administración Visual de Catálogos
  readonly catalogoActivoCodigo = signal<string | null>(null);
  readonly busquedaCatalogos = signal<string>('');
  readonly catalogoEnEdicion = signal<CatalogoEdicionForm | null>(null);
  readonly elementoEnEdicion = signal<ElementoEdicionForm | null>(null);
  readonly feedbackCatalogo = signal<FeedbackCatalogo | null>(null);

  // Computados para Catálogos
  readonly catalogosList = computed<CatalogoBuilderModel[]>(() => {
    return this.model().catalogos ?? [];
  });

  readonly catalogosFiltrados = computed<CatalogoBuilderModel[]>(() => {
    const q = this.busquedaCatalogos().trim().toLowerCase();
    const list = this.catalogosList();
    if (!q) return list;
    return list.filter(c => c.codigo.toLowerCase().includes(q) || c.nombre.toLowerCase().includes(q));
  });

  readonly catalogoActivo = computed<CatalogoBuilderModel | null>(() => {
    const cod = this.catalogoActivoCodigo();
    if (!cod) return null;
    return this.catalogosList().find(c => c.codigo.toLowerCase() === cod.toLowerCase()) ?? null;
  });

  readonly catalogosDisponiblesParaCampos = computed<Array<{ codigo: string; nombre: string; cantidadElementos: number }>>(() => {
    const locales = this.catalogosList().map(c => ({
      codigo: c.codigo,
      nombre: c.nombre,
      cantidadElementos: c.elementos?.length ?? 0
    }));

    const codigosVistos = new Set(locales.map(l => l.codigo.toLowerCase()));
    const globales = (this.catalogosDisponibles || [])
      .filter(g => !codigosVistos.has(g.codigo.toLowerCase()))
      .map(g => ({ codigo: g.codigo, nombre: g.nombre, cantidadElementos: 0 }));

    return [...locales, ...globales];
  });

  ngOnInit(): void {
    const parsed = normalizarJsonABuilderModel(this.jsonDefinicion, this.versionCodigo, 'Formulario Dinámico');
    if (!parsed.catalogos) {
      parsed.catalogos = [];
    }
    this.model.set(parsed);
    if (parsed.secciones.length > 0) {
      this.seccionActivaId.set(parsed.secciones[0].id);
    }
    if (parsed.catalogos && parsed.catalogos.length > 0) {
      this.catalogoActivoCodigo.set(parsed.catalogos[0].codigo);
    }
  }

  cambiarVista(vista: 'secciones' | 'catalogos'): void {
    this.vistaActiva.set(vista);
    this.feedbackCatalogo.set(null);
    this.catalogoEnEdicion.set(null);
    this.elementoEnEdicion.set(null);

    if (vista === 'catalogos') {
      const cats = this.catalogosList();
      if (!this.catalogoActivoCodigo() && cats.length > 0) {
        this.catalogoActivoCodigo.set(cats[0].codigo);
      }
    }
  }

  // ==========================================
  // ADMINISTRACIÓN DE SECCIONES Y CAMPOS
  // ==========================================

  agregarSeccion(): void {
    if (this.soloLectura) return;
    const current = this.model();
    const count = current.secciones.length + 1;
    const nuevaSeccion: SeccionBuilderModel = {
      id: `sec_${Date.now()}`,
      clave: `seccion_${count}`,
      titulo: `Nueva sección ${count}`,
      orden: count,
      columnasPorFila: 2,
      campos: []
    };
    this.model.set({
      ...current,
      secciones: [...current.secciones, nuevaSeccion]
    });
    this.seccionActivaId.set(nuevaSeccion.id);
  }

  eliminarSeccion(seccionId: string): void {
    if (this.soloLectura) return;
    const current = this.model();
    if (current.secciones.length <= 1) return;
    const filtradas = current.secciones.filter((s: SeccionBuilderModel) => s.id !== seccionId);
    this.model.set({
      ...current,
      secciones: filtradas
    });
    if (this.seccionActivaId() === seccionId && filtradas.length > 0) {
      this.seccionActivaId.set(filtradas[0].id);
    }
  }

  agregarCampoASeccion(seccionId: string, ctrlDef: TipoControlDefinicion): void {
    if (this.soloLectura) return;
    const current = this.model();
    const seccionIndex = current.secciones.findIndex((s: SeccionBuilderModel) => s.id === seccionId);
    if (seccionIndex === -1) return;

    const seccion = current.secciones[seccionIndex];
    const totalCampos = current.secciones.reduce((acc: number, s: SeccionBuilderModel) => acc + s.campos.length, 0) + 1;
    const claveNueva = `campo_${totalCampos}`;

    const catalogoPredeterminado = this.catalogosDisponiblesParaCampos()[0]?.codigo || 'MR_IMPACTO_1_5';

    const nuevoCampo: CampoBuilderModel = {
      id: `cmp_${Date.now()}_${totalCampos}`,
      clave: claveNueva,
      etiqueta: `${ctrlDef.etiqueta} ${totalCampos}`,
      tipo: ctrlDef.tipo,
      codigoCatalogo: ctrlDef.requiereCatalogo ? catalogoPredeterminado : undefined,
      obligatorio: true,
      soloLectura: ctrlDef.tipo === 'formula',
      anchoColumnas: 1
    };

    const seccionesActualizadas = [...current.secciones];
    seccionesActualizadas[seccionIndex] = {
      ...seccion,
      campos: [...seccion.campos, nuevoCampo]
    };

    this.model.set({
      ...current,
      secciones: seccionesActualizadas
    });

    this.campoActivo.set(nuevoCampo);
  }

  seleccionarCampo(campo: CampoBuilderModel): void {
    this.campoActivo.set(campo);
  }

  alCambiarPropiedadCampo(): void {
    const activo = this.campoActivo();
    if (!activo || this.soloLectura) return;
    const current = this.model();
    const secciones = current.secciones.map(sec => ({
      ...sec,
      campos: sec.campos.map(c => c.id === activo.id ? { ...activo } : c)
    }));
    this.model.set({
      ...current,
      secciones
    });
  }

  eliminarCampo(seccionId: string, campoId: string): void {
    if (this.soloLectura) return;
    const current = this.model();
    const seccionIndex = current.secciones.findIndex((s: SeccionBuilderModel) => s.id === seccionId);
    if (seccionIndex === -1) return;

    const seccion = current.secciones[seccionIndex];
    const camposFiltrados = seccion.campos.filter((c: CampoBuilderModel) => c.id !== campoId);

    const seccionesActualizadas = [...current.secciones];
    seccionesActualizadas[seccionIndex] = {
      ...seccion,
      campos: camposFiltrados
    };

    this.model.set({
      ...current,
      secciones: seccionesActualizadas
    });

    if (this.campoActivo()?.id === campoId) {
      this.campoActivo.set(null);
    }
  }

  reordenarCampo(seccionId: string, campoIndex: number, direccion: 'subir' | 'bajar'): void {
    if (this.soloLectura) return;
    const current = this.model();
    const seccionIndex = current.secciones.findIndex((s: SeccionBuilderModel) => s.id === seccionId);
    if (seccionIndex === -1) return;

    const seccion = current.secciones[seccionIndex];
    const campos = [...seccion.campos];

    const targetIndex = direccion === 'subir' ? campoIndex - 1 : campoIndex + 1;
    if (targetIndex < 0 || targetIndex >= campos.length) return;

    const temp = campos[campoIndex];
    campos[campoIndex] = campos[targetIndex];
    campos[targetIndex] = temp;

    const seccionesActualizadas = [...current.secciones];
    seccionesActualizadas[seccionIndex] = {
      ...seccion,
      campos
    };

    this.model.set({
      ...current,
      secciones: seccionesActualizadas
    });
  }

  actualizarColumnasSeccion(seccionId: string, columnas: number): void {
    if (this.soloLectura) return;
    const current = this.model();
    const seccionIndex = current.secciones.findIndex((s: SeccionBuilderModel) => s.id === seccionId);
    if (seccionIndex === -1) return;

    const seccionesActualizadas = [...current.secciones];
    seccionesActualizadas[seccionIndex] = {
      ...seccionesActualizadas[seccionIndex],
      columnasPorFila: Number(columnas)
    };

    this.model.set({
      ...current,
      secciones: seccionesActualizadas
    });
  }

  // ==========================================
  // ADMINISTRACIÓN VISUAL DE CATÁLOGOS
  // ==========================================

  seleccionarCatalogo(codigo: string): void {
    this.catalogoActivoCodigo.set(codigo);
    this.catalogoEnEdicion.set(null);
    this.elementoEnEdicion.set(null);
    this.feedbackCatalogo.set(null);
  }

  camposQueUsanCatalogo(codigoCatalogo: string): Array<{ seccionTitulo: string; campoEtiqueta: string; campoClave: string }> {
    if (!codigoCatalogo) return [];
    const codLower = codigoCatalogo.trim().toLowerCase();
    const resultados: Array<{ seccionTitulo: string; campoEtiqueta: string; campoClave: string }> = [];

    const secciones = this.model().secciones ?? [];
    for (const sec of secciones) {
      for (const cmp of sec.campos ?? []) {
        if (cmp.codigoCatalogo && cmp.codigoCatalogo.trim().toLowerCase() === codLower) {
          resultados.push({
            seccionTitulo: sec.titulo || sec.clave,
            campoEtiqueta: cmp.etiqueta || cmp.clave,
            campoClave: cmp.clave
          });
        }
      }
    }
    return resultados;
  }

  catalogoEstaEnUso(codigoCatalogo: string): boolean {
    return this.camposQueUsanCatalogo(codigoCatalogo).length > 0;
  }

  iniciarNuevoCatalogo(): void {
    if (this.soloLectura) return;
    this.feedbackCatalogo.set(null);
    this.elementoEnEdicion.set(null);
    this.catalogoEnEdicion.set({
      codigoOriginal: null,
      codigo: '',
      nombre: '',
      esNuevo: true
    });
  }

  iniciarEdicionCatalogo(cat: CatalogoBuilderModel): void {
    if (this.soloLectura) return;
    this.feedbackCatalogo.set(null);
    this.elementoEnEdicion.set(null);
    this.catalogoEnEdicion.set({
      codigoOriginal: cat.codigo,
      codigo: cat.codigo,
      nombre: cat.nombre,
      esNuevo: false
    });
  }

  cancelarEdicionCatalogo(): void {
    this.catalogoEnEdicion.set(null);
  }

  guardarEdicionCatalogo(): void {
    if (this.soloLectura) return;
    const edicion = this.catalogoEnEdicion();
    if (!edicion) return;

    const codigoLimpio = edicion.codigo.trim();
    const nombreLimpio = edicion.nombre.trim();

    if (!codigoLimpio) {
      this.feedbackCatalogo.set({ tipo: 'error', mensaje: 'El código del catálogo es obligatorio.' });
      return;
    }

    if (!nombreLimpio) {
      this.feedbackCatalogo.set({ tipo: 'error', mensaje: 'El nombre del catálogo es obligatorio.' });
      return;
    }

    const current = this.model();
    const catalogosActuales = current.catalogos ? [...current.catalogos] : [];
    const codigoLower = codigoLimpio.toLowerCase();

    if (edicion.esNuevo) {
      const existe = catalogosActuales.some(c => c.codigo.trim().toLowerCase() === codigoLower);
      if (existe) {
        this.feedbackCatalogo.set({
          tipo: 'error',
          mensaje: `Ya existe un catálogo con el código "${codigoLimpio}". Los códigos deben ser únicos sin distinguir mayúsculas/minúsculas.`
        });
        return;
      }

      const nuevoCatalogo: CatalogoBuilderModel = {
        codigo: codigoLimpio,
        nombre: nombreLimpio,
        elementos: []
      };

      this.model.set({
        ...current,
        catalogos: [...catalogosActuales, nuevoCatalogo]
      });

      this.catalogoActivoCodigo.set(codigoLimpio);
      this.catalogoEnEdicion.set(null);
      this.feedbackCatalogo.set({ tipo: 'exito', mensaje: `Catálogo "${nombreLimpio}" (${codigoLimpio}) creado exitosamente.` });
    } else {
      const codOriginal = edicion.codigoOriginal || edicion.codigo;
      const codOriginalLower = codOriginal.trim().toLowerCase();

      // Si cambió de código, validar unicidad
      if (codOriginalLower !== codigoLower) {
        const existeOtro = catalogosActuales.some(
          c => c.codigo.trim().toLowerCase() === codigoLower && c.codigo.trim().toLowerCase() !== codOriginalLower
        );
        if (existeOtro) {
          this.feedbackCatalogo.set({
            tipo: 'error',
            mensaje: `Ya existe otro catálogo con el código "${codigoLimpio}".`
          });
          return;
        }
      }

      // Actualizar catálogo preservando metadatos originales
      const idx = catalogosActuales.findIndex(c => c.codigo.trim().toLowerCase() === codOriginalLower);
      if (idx === -1) return;

      const catActual = catalogosActuales[idx];
      const catActualizado: CatalogoBuilderModel = {
        ...catActual,
        codigo: codigoLimpio,
        nombre: nombreLimpio
      };

      catalogosActuales[idx] = catActualizado;

      // Si el código cambió, actualizar referencias en los campos para preservar integridad referencial
      let seccionesActualizadas = current.secciones;
      if (codOriginalLower !== codigoLower) {
        seccionesActualizadas = current.secciones.map(sec => ({
          ...sec,
          campos: sec.campos.map(cmp => {
            if (cmp.codigoCatalogo && cmp.codigoCatalogo.trim().toLowerCase() === codOriginalLower) {
              return { ...cmp, codigoCatalogo: codigoLimpio };
            }
            return cmp;
          })
        }));
      }

      this.model.set({
        ...current,
        catalogos: catalogosActuales,
        secciones: seccionesActualizadas
      });

      this.catalogoActivoCodigo.set(codigoLimpio);
      this.catalogoEnEdicion.set(null);
      this.feedbackCatalogo.set({ tipo: 'exito', mensaje: `Catálogo "${nombreLimpio}" (${codigoLimpio}) actualizado exitosamente.` });
    }
  }

  eliminarCatalogo(codigo: string): void {
    if (this.soloLectura) return;
    const camposEnUso = this.camposQueUsanCatalogo(codigo);
    if (camposEnUso.length > 0) {
      const listaCampos = camposEnUso.map(c => `"${c.campoEtiqueta}" (en sección ${c.seccionTitulo})`).join(', ');
      this.feedbackCatalogo.set({
        tipo: 'error',
        mensaje: `No se puede eliminar el catálogo "${codigo}" porque está en uso por ${camposEnUso.length} campo(s): ${listaCampos}. Reasigna o elimina los campos antes de eliminar el catálogo.`
      });
      return;
    }

    const current = this.model();
    const catalogosFiltrados = (current.catalogos ?? []).filter(
      c => c.codigo.trim().toLowerCase() !== codigo.trim().toLowerCase()
    );

    this.model.set({
      ...current,
      catalogos: catalogosFiltrados
    });

    if (this.catalogoActivoCodigo()?.trim().toLowerCase() === codigo.trim().toLowerCase()) {
      this.catalogoActivoCodigo.set(catalogosFiltrados.length > 0 ? catalogosFiltrados[0].codigo : null);
    }

    this.catalogoEnEdicion.set(null);
    this.elementoEnEdicion.set(null);
    this.feedbackCatalogo.set({ tipo: 'exito', mensaje: `Catálogo "${codigo}" eliminado correctamente.` });
  }

  // ==========================================
  // ADMINISTRACIÓN DE ELEMENTOS DE CATÁLOGO
  // ==========================================

  iniciarNuevoElemento(): void {
    if (this.soloLectura || !this.catalogoActivo()) return;
    const cat = this.catalogoActivo()!;
    const ordenSiguiente = (cat.elementos?.length ?? 0) + 1;
    this.feedbackCatalogo.set(null);
    this.elementoEnEdicion.set({
      codigoOriginal: null,
      codigo: '',
      valor: '',
      orden: ordenSiguiente,
      indice: null
    });
  }

  iniciarEdicionElemento(elem: ElementoCatalogoBuilderModel, indice: number): void {
    if (this.soloLectura) return;
    this.feedbackCatalogo.set(null);
    this.elementoEnEdicion.set({
      codigoOriginal: elem.codigo,
      codigo: elem.codigo,
      valor: elem.valor,
      orden: elem.orden,
      indice
    });
  }

  cancelarEdicionElemento(): void {
    this.elementoEnEdicion.set(null);
  }

  guardarElementoCatalogo(): void {
    if (this.soloLectura) return;
    const cat = this.catalogoActivo();
    const elemEdicion = this.elementoEnEdicion();
    if (!cat || !elemEdicion) return;

    const codigoLimpio = String(elemEdicion.codigo).trim();
    const valorLimpio = String(elemEdicion.valor).trim();
    const ordenNum = Number(elemEdicion.orden);

    if (!codigoLimpio) {
      this.feedbackCatalogo.set({ tipo: 'error', mensaje: 'El código del elemento es obligatorio.' });
      return;
    }

    if (!valorLimpio) {
      this.feedbackCatalogo.set({ tipo: 'error', mensaje: 'La etiqueta/valor del elemento es obligatorio.' });
      return;
    }

    if (!Number.isInteger(ordenNum) || ordenNum < 1) {
      this.feedbackCatalogo.set({ tipo: 'error', mensaje: 'El orden debe ser un número entero mayor o igual a 1.' });
      return;
    }

    const elementosActuales = cat.elementos ? [...cat.elementos] : [];
    const codigoLower = codigoLimpio.toLowerCase();

    // Validar unicidad de código de elemento en este catálogo
    const duplicado = elementosActuales.some((el, idx) => {
      if (elemEdicion.indice !== null && idx === elemEdicion.indice) return false;
      return el.codigo.trim().toLowerCase() === codigoLower;
    });

    if (duplicado) {
      this.feedbackCatalogo.set({
        tipo: 'error',
        mensaje: `El código "${codigoLimpio}" ya existe dentro de este catálogo. Los códigos de elementos deben ser únicos.`
      });
      return;
    }

    if (elemEdicion.indice === null) {
      // Nuevo elemento
      const nuevoElemento: ElementoCatalogoBuilderModel = {
        codigo: codigoLimpio,
        valor: valorLimpio,
        orden: ordenNum
      };
      elementosActuales.push(nuevoElemento);
    } else {
      // Edición de elemento existente
      const idx = elemEdicion.indice;
      const elemExistente = elementosActuales[idx];
      elementosActuales[idx] = {
        ...elemExistente,
        codigo: codigoLimpio,
        valor: valorLimpio,
        orden: ordenNum
      };
    }

    // Actualizar catálogo en el modelo
    this.actualizarElementosDeCatalogoActivo(elementosActuales);
    this.elementoEnEdicion.set(null);
    this.feedbackCatalogo.set({ tipo: 'exito', mensaje: `Elemento "${valorLimpio}" (${codigoLimpio}) guardado correctamente.` });
  }

  eliminarElementoCatalogo(indice: number): void {
    if (this.soloLectura) return;
    const cat = this.catalogoActivo();
    if (!cat || !cat.elementos) return;

    const elementos = cat.elementos.filter((_, idx) => idx !== indice);
    // Reindexar orden secuencialmente
    const elementosReordenados = elementos.map((el, idx) => ({
      ...el,
      orden: idx + 1
    }));

    this.actualizarElementosDeCatalogoActivo(elementosReordenados);
    this.elementoEnEdicion.set(null);
    this.feedbackCatalogo.set({ tipo: 'exito', mensaje: 'Elemento eliminado correctamente.' });
  }

  reordenarElementoCatalogo(indice: number, direccion: 'subir' | 'bajar'): void {
    if (this.soloLectura) return;
    const cat = this.catalogoActivo();
    if (!cat || !cat.elementos) return;

    const targetIdx = direccion === 'subir' ? indice - 1 : indice + 1;
    if (targetIdx < 0 || targetIdx >= cat.elementos.length) return;

    const elementos = [...cat.elementos];
    const temp = elementos[indice];
    elementos[indice] = elementos[targetIdx];
    elementos[targetIdx] = temp;

    // Actualizar orden (1-based)
    const elementosActualizados = elementos.map((el, idx) => ({
      ...el,
      orden: idx + 1
    }));

    this.actualizarElementosDeCatalogoActivo(elementosActualizados);
  }

  private actualizarElementosDeCatalogoActivo(elementos: ElementoCatalogoBuilderModel[]): void {
    const codActivo = this.catalogoActivoCodigo();
    if (!codActivo) return;

    const current = this.model();
    const catalogos = (current.catalogos ?? []).map(c => {
      if (c.codigo.trim().toLowerCase() === codActivo.trim().toLowerCase()) {
        return { ...c, elementos };
      }
      return c;
    });

    this.model.set({
      ...current,
      catalogos
    });
  }

  // ==========================================
  // VISTA JSON AVANZADA Y GUARDADO
  // ==========================================

  toggleModoJson(): void {
    if (!this.esAdministrador) return;
    if (!this.mostrarJsonAvanzado()) {
      this.jsonAvanzadoStr.set(serializarBuilderModelAJson(this.model()));
    }
    this.mostrarJsonAvanzado.set(!this.mostrarJsonAvanzado());
  }

  aplicarJsonAvanzado(): void {
    if (this.soloLectura || !this.esAdministrador) return;
    try {
      const parsed = normalizarJsonABuilderModel(this.jsonAvanzadoStr(), this.versionCodigo, 'Formulario Dinámico');
      if (!parsed.catalogos) parsed.catalogos = [];
      this.model.set(parsed);
      this.mostrarJsonAvanzado.set(false);
      this.erroresValidacion.set([]);
      if (parsed.catalogos.length > 0 && !this.catalogoActivoCodigo()) {
        this.catalogoActivoCodigo.set(parsed.catalogos[0].codigo);
      }
    } catch {
      this.erroresValidacion.set([{ campo: 'JSON', mensaje: 'El formato JSON ingresado no es válido.' }]);
    }
  }

  validarYObtenerErrores(): boolean {
    const errs = validarFormBuilderModel(this.model());
    this.erroresValidacion.set(errs);
    return errs.length === 0;
  }

  emitirGuardado(): void {
    if (this.soloLectura) return;
    if (!this.validarYObtenerErrores()) {
      return;
    }
    const jsonOutput = serializarBuilderModelAJson(this.model());
    this.guardarJson.emit(jsonOutput);
  }
}
