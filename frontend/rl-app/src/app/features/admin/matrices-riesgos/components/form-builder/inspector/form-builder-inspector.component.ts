import { ChangeDetectionStrategy, ChangeDetectorRef, Component, EventEmitter, Input, Output, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CAMPO_CONTROLES_VINCULADOS, CampoBuilderModel, FormulaCoincidenciaEstado, TipoControlBuilder, TipoControlDefinicion } from '../../../models/form-builder.models';
import { CampoFormulario, RespuestasFormulario } from '../../../models/matrices-riesgos.models';
import { FormulaVersionSelectorOption } from '../../../models/calculo-configuracion.models';
import { evaluarFormulaCampo, FormulaErrorCode, ResultadoEvaluacionFormula } from '../../../utils/dynamic-formula-evaluator.util';

export interface InspectorCatalogoOption {
  codigo: string;
  nombre: string;
  cantidadElementos: number;
}

import { ActionIconComponent } from '../../../../../../shared/components/action-icon/action-icon.component';
import { BoundedSelectComponent, BoundedSelectOption } from '../../../../../../shared/components/bounded-select/bounded-select.component';
import { InstitutionalTooltipDirective } from '../../../../../../shared/components/institutional-tooltip/institutional-tooltip.directive';
import { InstitutionalConfirmationService } from '../../../../../../shared/confirmation/institutional-confirmation.service';
import { etiquetaEstadoFormula, nombreFormulaLegible, normalizarBusquedaFormula } from '../../../utils/institutional-formula-label.util';

@Component({
  selector: 'app-form-builder-inspector',
  standalone: true,
  imports: [ActionIconComponent, BoundedSelectComponent, InstitutionalTooltipDirective, CommonModule, FormsModule],
  templateUrl: './form-builder-inspector.component.html',
  styleUrls: ['./form-builder-inspector.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class FormBuilderInspectorComponent {
  private cdr = inject(ChangeDetectorRef);
  private readonly confirmation = inject(InstitutionalConfirmationService);
  readonly nombreFormulaLegible = nombreFormulaLegible;

  private _campoActivo: CampoBuilderModel | null = null;
  @Input()
  set campoActivo(val: CampoBuilderModel | null) {
    this._campoActivo = val;
    this.cdr.markForCheck();
  }
  get campoActivo(): CampoBuilderModel | null {
    return this._campoActivo;
  }

  @Input() soloLectura: boolean = false;
  @Input() tiposControles: TipoControlDefinicion[] = [];
  @Input() catalogosDisponibles: InspectorCatalogoOption[] = [];
  @Input() formulaVersionesDisponibles: FormulaVersionSelectorOption[] = [];
  @Input() camposFormulario: CampoBuilderModel[] = [];
  @Input() formulaCoincidencias: Readonly<Record<string, FormulaCoincidenciaEstado>> = {};

  @Output() propiedadCambiada = new EventEmitter<void>();
  @Output() formulaCoincidenciaInvalidada = new EventEmitter<string>();
  @Output() navegarCatalogos = new EventEmitter<void>();

  // Estado UI de acordeones colapsables (exclusivo de UI, nunca persistido)
  seccionesAbiertas: Record<'general' | 'reglas' | 'datos' | 'presentacion' | 'ayuda', boolean> = {
    general: true,
    reglas: true,
    datos: true,
    presentacion: true,
    ayuda: true
  };

  nuevaOpcionTexto = '';

  toggleSeccion(seccion: 'general' | 'reglas' | 'datos' | 'presentacion' | 'ayuda'): void {
    this.seccionesAbiertas[seccion] = !this.seccionesAbiertas[seccion];
    this.cdr.markForCheck();
  }

  get definicionTipoActual(): TipoControlDefinicion | undefined {
    if (!this.campoActivo) return undefined;
    return this.tiposControles.find(t => t.tipo === this.campoActivo?.tipo);
  }

  get requiereCatalogo(): boolean {
    if (!this.campoActivo) return false;
    return this.definicionTipoActual?.requiereCatalogo ?? false;
  }

  get requiereOpciones(): boolean {
    if (!this.campoActivo) return false;
    return this.definicionTipoActual?.requiereOpciones ?? false;
  }

  get requiereFormula(): boolean {
    if (!this.campoActivo) return false;
    return this.definicionTipoActual?.requiereFormula ?? false;
  }

  get esFormula(): boolean {
    return this.campoActivo?.tipo === 'formula';
  }

  get esControlesVinculados(): boolean {
    return this.campoActivo?.tipo === CAMPO_CONTROLES_VINCULADOS.tipo;
  }

  get formulaModoActual(): 'central' | 'manual' {
    return this.campoActivo?.formulaModo
      ?? (this.campoActivo?.formulaVersionId ? 'central' : 'manual');
  }

  /**
   * Datos sintéticos controlados para que el editor muestre el resultado sin
   * ejecutar fórmulas arbitrarias ni depender de respuestas institucionales.
   */
  get formulaRespuestasSinteticas(): RespuestasFormulario {
    const respuestas: RespuestasFormulario = {
      frecuencia_inherente: 2,
      impacto_inherente: 3,
      control_preventivo: 1,
      control_detectivo: 1,
      control_correctivo: 1
    };
    const campos = [...this.camposFormulario, ...(this.campoActivo ? [this.campoActivo] : [])];
    for (const campo of campos) {
      const clave = campo.clave.trim().toLowerCase();
      if (!clave || clave in respuestas) continue;
      respuestas[clave] = campo.tipo === 'checkbox' ? true : campo.tipo === 'numero' || campo.tipo === 'formula' ? 2 : 'VALOR_SINTETICO';
    }
    return respuestas;
  }

  get formulaCamposMap(): Map<string, CampoFormulario> {
    const campos = [...this.camposFormulario, ...(this.campoActivo ? [this.campoActivo] : [])];
    return new Map(campos.filter(campo => !!campo.clave.trim()).map(campo => [campo.clave.trim().toLowerCase(), campo as CampoFormulario]));
  }

  get formulaPreviewResult(): ResultadoEvaluacionFormula | null {
    if (this.formulaModoActual !== 'manual' || !this.campoActivo?.formula?.trim()) return null;
    return evaluarFormulaCampo(
      this.campoActivo.formula,
      this.formulaRespuestasSinteticas,
      this.campoActivo.clave,
      this.formulaCamposMap
    );
  }

  get formulaCoincidenciaActual(): FormulaCoincidenciaEstado {
    const clave = this.campoActivo?.clave.trim().toLowerCase();
    if (!clave) return 'pendiente';
    return this.formulaCoincidencias[clave] ?? 'pendiente';
  }

  get formulaCoincidenciaTexto(): string {
    switch (this.formulaCoincidenciaActual) {
      case 'reutilizada': return 'Coincidencia central elegible detectada; la fórmula sigue manual hasta seleccionar la versión exacta.';
      case 'creada': return 'Nueva fórmula central registrada como DRAFT; requiere aprobación antes de ser ejecutable.';
      default: return 'Pendiente: se resolverá al guardar y confirmar el registro de la fórmula manual.';
    }
  }

  mensajeFormulaPreview(result: ResultadoEvaluacionFormula | null): string {
    if (!result || result.exito) return '';
    const mensajes: Partial<Record<FormulaErrorCode, string>> = {
      FORMULA_SYNTAX_INVALID: 'La sintaxis DSL no es válida.',
      FORMULA_OPERATOR_UNSUPPORTED: 'La expresión contiene un operador no permitido.',
      FORMULA_FUNCTION_UNSUPPORTED: 'La expresión contiene una función no autorizada.',
      FORMULA_REFERENCE_UNKNOWN: 'La expresión referencia un campo inexistente.',
      FORMULA_SELF_REFERENCE: 'La fórmula no puede referenciarse a sí misma.',
      FORMULA_CYCLE: 'La expresión contiene una dependencia circular.',
      FORMULA_TYPE_MISMATCH: 'Los tipos de los operandos no son compatibles.',
      FORMULA_DIVISION_BY_ZERO: 'La vista previa detectó una división por cero.',
      FORMULA_ARGUMENT_INVALID: 'La cantidad o tipo de argumentos no es válida.',
      FORMULA_LIMIT_EXCEEDED: 'La expresión excede los límites institucionales.'
    };
    return mensajes[result.codigo ?? 'FORMULA_SYNTAX_INVALID'] ?? result.error ?? 'La expresión no pudo validarse.';
  }

  readonly tiposResultadoFormula = ['DECIMAL', 'INTEGER', 'BOOLEAN', 'TEXT', 'DATE'];

  get formulaVersionSeleccionada(): FormulaVersionSelectorOption | null {
    const id = this.campoActivo?.formulaVersionId;
    return id ? this.formulaVersionesDisponibles.find(item => item.formulaVersionId === id) ?? null : null;
  }

  get formulaVersionSelectOptions(): readonly BoundedSelectOption[] {
    return this.formulaVersionesDisponibles.map(option => ({
      value: option.formulaVersionId,
      label: `${nombreFormulaLegible(option)} · v${option.version} · ${etiquetaEstadoFormula(option.estado)}`,
      searchText: normalizarBusquedaFormula(`${nombreFormulaLegible(option)} ${option.codigo} ${option.version} ${etiquetaEstadoFormula(option.estado)} ${option.descripcion ?? ''} ${option.expresion}`)
    }));
  }

  seleccionarFormulaCentral(versionId: string | number | null): void {
    if (this.soloLectura || !this.campoActivo) return;
    const normalizedVersionId = versionId === null || versionId === '' ? null : Number(versionId);
    if (normalizedVersionId !== null && !Number.isInteger(normalizedVersionId)) return;
    if (normalizedVersionId === null) {
      this.campoActivo.formulaId = undefined;
      this.campoActivo.formulaVersionId = undefined;
      this.campoActivo.formulaCodigo = undefined;
      this.campoActivo.formulaVersion = undefined;
      this.campoActivo.formula = undefined;
      this.campoActivo.formulaFuente = undefined;
      this.campoActivo.formulaModo = undefined;
      this.campoActivo.formulaManualRespaldo = undefined;
      this.formulaCoincidenciaInvalidada.emit(this.campoActivo.clave.trim().toLowerCase());
      this.cdr.markForCheck();
      this.propiedadCambiada.emit();
      return;
    }
    const option = this.formulaVersionesDisponibles.find(item => item.formulaVersionId === normalizedVersionId);
    if (!option) return;
    if (this.campoActivo.formulaModo === 'manual' && this.campoActivo.formula?.trim()) {
      this.campoActivo.formulaManualRespaldo = {
        expresion: this.campoActivo.formula,
        nombre: this.campoActivo.formulaNombre,
        tipoResultado: this.campoActivo.formulaTipoResultado
      };
    }
    this.campoActivo.formulaId = option.formulaId;
    this.campoActivo.formulaVersionId = option.formulaVersionId;
    this.campoActivo.formulaCodigo = option.codigo;
    this.campoActivo.formulaVersion = option.version;
    this.campoActivo.formula = option.expresion;
    this.campoActivo.formulaFuente = 'formula';
    this.campoActivo.formulaModo = 'central';
    this.campoActivo.formulaTipoResultado = option.tipoResultado;
    this.formulaCoincidenciaInvalidada.emit(this.campoActivo.clave.trim().toLowerCase());
    this.cdr.markForCheck();
    this.propiedadCambiada.emit();
  }

  cambiarModoFormula(modo: 'central' | 'manual'): void {
    if (this.soloLectura || !this.campoActivo || this.formulaModoActual === modo) return;
    const campo = this.campoActivo;
    if (modo === 'manual') {
      campo.formulaModo = 'manual';
      campo.formulaFuente = 'formula';
      campo.formulaNombre ??= campo.etiqueta;
      campo.formulaTipoResultado ??= 'DECIMAL';
      const respaldo = campo.formulaManualRespaldo;
      campo.formula = campo.formula ?? respaldo?.expresion ?? '';
      campo.formulaNombre = campo.formulaNombre ?? respaldo?.nombre;
      campo.formulaTipoResultado = campo.formulaTipoResultado ?? respaldo?.tipoResultado;
      campo.formulaId = undefined;
      campo.formulaVersionId = undefined;
      campo.formulaCodigo = undefined;
      campo.formulaVersion = undefined;
      campo.formulaValidacion = 'sin-validar';
      this.formulaCoincidenciaInvalidada.emit(campo.clave.trim().toLowerCase());
      this.cdr.markForCheck();
      this.propiedadCambiada.emit();
      return;
    }

    const tieneFormulaManual = !!campo.formula?.trim() && !campo.formulaVersionId;
    const confirmarCambio = tieneFormulaManual
      ? this.confirmation.confirm({
        title: 'Cambiar fuente de fórmula',
        message: 'La expresión manual quedará conservada como intención del editor, pero no se vinculará hasta seleccionar una versión central.',
        icon: 'warning',
        confirmLabel: 'Continuar',
        cancelLabel: 'Cancelar',
        confirmIcon: 'configure',
        confirmVariant: 'primary'
      })
      : Promise.resolve(true);
    void confirmarCambio.then(confirmado => {
      if (!confirmado || !this.campoActivo) return;
      const actual = this.campoActivo;
      if (actual.formula?.trim()) actual.formulaManualRespaldo = {
        expresion: actual.formula,
        nombre: actual.formulaNombre,
        tipoResultado: actual.formulaTipoResultado
      };
      actual.formula = undefined;
      actual.formulaModo = 'central';
      actual.formulaValidacion = 'sin-validar';
      this.formulaCoincidenciaInvalidada.emit(actual.clave.trim().toLowerCase());
      this.cdr.markForCheck();
      this.propiedadCambiada.emit();
    });
  }

  validarFormulaManual(): void {
    if (this.soloLectura || !this.campoActivo) return;
    const expression = this.campoActivo.formula?.trim() ?? '';
    const result = expression.length > 0 && expression.length <= 4096
      ? this.formulaPreviewResult
      : { exito: false } as ResultadoEvaluacionFormula;
    this.campoActivo.formulaValidacion = result?.exito ? 'valida' : 'invalida';
    this.cdr.markForCheck();
  }

  alCambiarFormulaManual(): void {
    if (this.soloLectura || !this.campoActivo) return;
    this.campoActivo.formulaValidacion = 'sin-validar';
    this.formulaCoincidenciaInvalidada.emit(this.campoActivo.clave.trim().toLowerCase());
    this.alCambiarPropiedad();
  }

  get soportaPlaceholder(): boolean {
    if (!this.campoActivo) return false;
    return ['texto', 'numero', 'texto-largo'].includes(this.campoActivo.tipo)
      || (this.campoActivo.tipo === 'selector-catalogo' && !!this.campoActivo.permiteValorManual);
  }

  alCambiarTipo(nuevoTipo: TipoControlBuilder): void {
    if (this.soloLectura || !this.campoActivo) return;
    this.campoActivo.tipo = nuevoTipo;

    if (nuevoTipo === CAMPO_CONTROLES_VINCULADOS.tipo) {
      this.aplicarInvariantesControlesVinculados();
    }

    // Invariante formula soloLectura
    if (nuevoTipo === 'formula') {
      this.campoActivo.soloLectura = true;
    }

    // Regla Hidden != Delete: no borrar propiedades contractuales al cambiar de tipo
    this.cdr.markForCheck();
    this.propiedadCambiada.emit();
  }

  alCambiarPropiedad(): void {
    if (this.soloLectura || !this.campoActivo) return;
    if (this.esControlesVinculados) this.aplicarInvariantesControlesVinculados();
    if (this.campoActivo.tipo === 'formula') {
      this.campoActivo.soloLectura = true;
      if (!this.campoActivo.formulaModo && !this.campoActivo.formulaVersionId) this.campoActivo.formulaModo = 'manual';
    }
    this.cdr.markForCheck();
    this.propiedadCambiada.emit();
  }

  private aplicarInvariantesControlesVinculados(): void {
    if (!this.campoActivo) return;
    this.campoActivo.clave = CAMPO_CONTROLES_VINCULADOS.clave;
    this.campoActivo.etiqueta = CAMPO_CONTROLES_VINCULADOS.etiqueta;
    this.campoActivo.campoCanonicoId = CAMPO_CONTROLES_VINCULADOS.campoCanonicoId;
    this.campoActivo.fuenteVinculada = CAMPO_CONTROLES_VINCULADOS.fuenteVinculada;
    this.campoActivo.cardinalidad = CAMPO_CONTROLES_VINCULADOS.cardinalidad;
    this.campoActivo.persistirRespuesta = false;
    this.campoActivo.obligatorio = false;
    this.campoActivo.soloLectura = true;
  }

  agregarOpcion(): void {
    if (this.soloLectura || !this.campoActivo) return;
    const texto = this.nuevaOpcionTexto.trim();
    if (!texto) return;

    this.campoActivo.opciones = [...(this.campoActivo.opciones || []), texto];
    this.nuevaOpcionTexto = '';
    this.cdr.markForCheck();
    this.propiedadCambiada.emit();
  }

  eliminarOpcion(index: number): void {
    if (this.soloLectura || !this.campoActivo || !this.campoActivo.opciones) return;
    this.campoActivo.opciones = this.campoActivo.opciones.filter((_, i) => i !== index);
    this.cdr.markForCheck();
    this.propiedadCambiada.emit();
  }
}
