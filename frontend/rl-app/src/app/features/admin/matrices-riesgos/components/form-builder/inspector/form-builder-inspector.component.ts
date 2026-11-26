import { ChangeDetectionStrategy, ChangeDetectorRef, Component, EventEmitter, Input, Output, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CampoBuilderModel, TipoControlBuilder, TipoControlDefinicion } from '../../../models/form-builder.models';
import { FormulaVersionSelectorOption } from '../../../models/calculo-configuracion.models';

export interface InspectorCatalogoOption {
  codigo: string;
  nombre: string;
  cantidadElementos: number;
}

import { ActionIconComponent } from '../../../../../../shared/components/action-icon/action-icon.component';
import { BoundedSelectComponent, BoundedSelectOption } from '../../../../../../shared/components/bounded-select/bounded-select.component';
import { InstitutionalTooltipDirective } from '../../../../../../shared/components/institutional-tooltip/institutional-tooltip.directive';
import { InstitutionalConfirmationService } from '../../../../../../shared/confirmation/institutional-confirmation.service';

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

  @Output() propiedadCambiada = new EventEmitter<void>();
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

  get formulaModoActual(): 'central' | 'manual' {
    return this.campoActivo?.formulaModo
      ?? (this.campoActivo?.formulaVersionId ? 'central' : 'manual');
  }

  readonly tiposResultadoFormula = ['DECIMAL', 'INTEGER', 'BOOLEAN', 'TEXT', 'DATE'];

  get formulaVersionSelectOptions(): readonly BoundedSelectOption[] {
    return this.formulaVersionesDisponibles.map(option => ({
      value: option.formulaVersionId,
      label: `${option.codigo} · v${option.version} · ${option.estado}`,
      searchText: `${option.codigo} ${option.nombre} ${option.version} ${option.estado} ${option.expresion}`
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
      this.cdr.markForCheck();
      this.propiedadCambiada.emit();
    });
  }

  validarFormulaManual(): void {
    if (this.soloLectura || !this.campoActivo) return;
    const expression = this.campoActivo.formula?.trim() ?? '';
    this.campoActivo.formulaValidacion = expression.length > 0 && expression.length <= 4096 ? 'valida' : 'invalida';
    this.cdr.markForCheck();
  }

  get soportaPlaceholder(): boolean {
    if (!this.campoActivo) return false;
    return ['texto', 'numero', 'texto-largo'].includes(this.campoActivo.tipo)
      || (this.campoActivo.tipo === 'selector-catalogo' && !!this.campoActivo.permiteValorManual);
  }

  alCambiarTipo(nuevoTipo: TipoControlBuilder): void {
    if (this.soloLectura || !this.campoActivo) return;
    this.campoActivo.tipo = nuevoTipo;

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
    if (this.campoActivo.tipo === 'formula') {
      this.campoActivo.soloLectura = true;
      if (!this.campoActivo.formulaModo && !this.campoActivo.formulaVersionId) this.campoActivo.formulaModo = 'manual';
    }
    this.cdr.markForCheck();
    this.propiedadCambiada.emit();
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
