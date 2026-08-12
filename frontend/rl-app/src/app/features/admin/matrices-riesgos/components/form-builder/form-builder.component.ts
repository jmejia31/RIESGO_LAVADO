import { Component, EventEmitter, Input, OnInit, Output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
  CampoBuilderModel,
  FormBuilderModel,
  SeccionBuilderModel,
  TIPOS_CONTROLES_DISPONIBLES,
  TipoControlDefinicion,
  normalizarJsonABuilderModel,
  serializarBuilderModelAJson
} from '../../models/form-builder.models';
import { ElementoCatalogoMatrices } from '../../models/matrices-riesgos.models';

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
    secciones: []
  });

  readonly tiposControles = TIPOS_CONTROLES_DISPONIBLES;
  readonly seccionActivaId = signal<string | null>(null);
  readonly campoActivo = signal<CampoBuilderModel | null>(null);
  readonly mostrarJsonAvanzado = signal<boolean>(false);
  readonly jsonAvanzadoStr = signal<string>('');

  ngOnInit(): void {
    const parsed = normalizarJsonABuilderModel(this.jsonDefinicion, this.versionCodigo, 'Formulario Dinámico');
    this.model.set(parsed);
    if (parsed.secciones.length > 0) {
      this.seccionActivaId.set(parsed.secciones[0].id);
    }
  }

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

    const nuevoCampo: CampoBuilderModel = {
      id: `cmp_${Date.now()}_${totalCampos}`,
      clave: claveNueva,
      etiqueta: `${ctrlDef.etiqueta} ${totalCampos}`,
      tipo: ctrlDef.tipo,
      codigoCatalogo: ctrlDef.requiereCatalogo ? this.catalogosDisponibles[0]?.codigo || 'MR_IMPACTO_1_5' : undefined,
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

  toggleModoJson(): void {
    if (!this.mostrarJsonAvanzado()) {
      this.jsonAvanzadoStr.set(serializarBuilderModelAJson(this.model()));
    }
    this.mostrarJsonAvanzado.set(!this.mostrarJsonAvanzado());
  }

  aplicarJsonAvanzado(): void {
    if (this.soloLectura) return;
    try {
      const parsed = normalizarJsonABuilderModel(this.jsonAvanzadoStr(), this.versionCodigo, 'Formulario Dinámico');
      this.model.set(parsed);
      this.mostrarJsonAvanzado.set(false);
    } catch {
      alert('El formato JSON ingresado no es válido.');
    }
  }

  emitirGuardado(): void {
    if (this.soloLectura) return;
    const jsonOutput = serializarBuilderModelAJson(this.model());
    this.guardarJson.emit(jsonOutput);
  }
}
