import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CampoFormulario, ValorRespuestaFormulario } from '../../models/matrices-riesgos.models';
import {
  TipoCampoRenderer,
  formatearValorRespuesta,
  normalizarTipoCampoRenderer,
  tieneValorRespuesta
} from '../../utils/dynamic-form-renderer.util';

export interface OpcionCampoRenderer {
  codigo: string;
  valor: string;
}

@Component({
  selector: 'app-dynamic-field-renderer',
  standalone: true,
  imports: [CommonModule, FormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="space-y-1" [attr.data-renderer-type]="tipo">
      @if (modoLectura) {
        <span class="block text-xs font-bold text-gray-600">
          {{ campo.etiqueta }}
          @if (campo.obligatorio) { <span class="text-red-500" aria-hidden="true">*</span> }
        </span>
        <div class="min-h-[38px] whitespace-pre-wrap px-3 py-2 bg-gray-50 border border-gray-200 rounded-lg text-xs font-medium text-gray-800"
             [attr.data-field-key]="campo.clave">
          {{ textoLectura }}
        </div>
        @if (tipo === 'desconocido') {
          <p class="mt-1 text-[11px] font-semibold text-amber-700" role="status">
            Tipo de campo no soportado: {{ tipoOriginalVisible }}. Se muestra en modo seguro de solo lectura.
          </p>
        }
      } @else if (tipo === 'radio') {
        <fieldset [id]="idControl"
                  class="space-y-2"
                  [attr.aria-required]="campo.obligatorio ? 'true' : null"
                  [attr.data-field-key]="campo.clave">
          <legend class="block text-xs font-bold text-gray-700">
            {{ campo.etiqueta }}
            @if (campo.obligatorio) { <span class="text-red-500" aria-hidden="true">*</span> }
          </legend>
          @if (opcionesDisponibles.length > 0) {
            <div class="flex flex-wrap gap-x-4 gap-y-2">
              @for (opcion of opcionesDisponibles; track opcion.codigo; let indice = $index) {
                <label class="inline-flex items-center gap-2 text-xs font-medium text-gray-700"
                       [for]="idOpcion(indice)">
                  <input type="radio"
                         class="h-4 w-4 border-gray-300 text-blue-600 focus:ring-blue-500"
                         [id]="idOpcion(indice)"
                         [name]="idControl"
                         [value]="opcion.codigo"
                         [required]="campo.obligatorio"
                         [disabled]="campo.soloLectura"
                         [ngModel]="valorEscalar"
                         (ngModelChange)="emitirSeleccion($event)" />
                  <span>{{ opcion.valor }}</span>
                </label>
              }
            </div>
          } @else {
            <p class="rounded-lg border border-amber-200 bg-amber-50 px-3 py-2 text-xs font-semibold text-amber-800" role="status">
              No hay opciones configuradas; ingrese un valor de forma segura.
            </p>
            <input [id]="idControl + '-fallback'"
                   type="text"
                   class="w-full rounded-xl border border-gray-200 bg-white px-3 py-2 text-xs font-medium text-gray-800 shadow-sm focus:ring-2 focus:ring-ihss-600 focus:outline-none"
                   [required]="campo.obligatorio"
                   [attr.aria-label]="campo.etiqueta + ' valor'"
                   [readOnly]="campo.soloLectura"
                   [value]="valorEscalar ?? ''"
                   (input)="emitirTexto($any($event.target).value)" />
          }
        </fieldset>
      } @else if (tipo === 'catalogo-multiple') {
        <fieldset [id]="idControl"
                  class="space-y-2"
                  [attr.aria-required]="campo.obligatorio ? 'true' : null"
                  [attr.data-field-key]="campo.clave">
          <legend class="block text-xs font-bold text-gray-700">
            {{ campo.etiqueta }}
            @if (campo.obligatorio) { <span class="text-red-500" aria-hidden="true">*</span> }
          </legend>
          @if (opcionesDisponibles.length > 0) {
            <div class="grid grid-cols-1 gap-2 sm:grid-cols-2">
              @for (opcion of opcionesDisponibles; track opcion.codigo; let indice = $index) {
                <label class="flex items-center gap-2 rounded-lg border border-gray-100 bg-gray-50/70 px-3 py-2 text-xs font-medium text-gray-700"
                       [for]="idOpcion(indice)">
                  <input type="checkbox"
                         class="h-4 w-4 rounded border-gray-300 text-blue-600 focus:ring-blue-500"
                         [id]="idOpcion(indice)"
                         [disabled]="campo.soloLectura"
                         [checked]="opcionSeleccionada(opcion.codigo)"
                         (change)="alternarOpcion(opcion.codigo, $event)" />
                  <span>{{ opcion.valor }}</span>
                </label>
              }
            </div>
          } @else {
            <p class="rounded-lg border border-amber-200 bg-amber-50 px-3 py-2 text-xs font-semibold text-amber-800" role="status">
              No hay opciones configuradas para este campo.
            </p>
          }
        </fieldset>
      } @else {
        <label [for]="idControl" class="block text-xs font-bold text-gray-700">
          {{ campo.etiqueta }}
          @if (campo.obligatorio) { <span class="text-red-500" aria-hidden="true">*</span> }
        </label>

        @switch (tipo) {
          @case ('texto') {
            <input [id]="idControl"
                   type="text"
                   class="w-full rounded-xl border border-gray-200 bg-white px-3 py-2 text-xs font-medium text-gray-800 shadow-sm focus:ring-2 focus:ring-ihss-600 focus:outline-none disabled:bg-gray-100 disabled:text-gray-500"
                   [required]="campo.obligatorio"
                   [attr.aria-required]="campo.obligatorio ? 'true' : null"
                   [readOnly]="campo.soloLectura"
                   [value]="valorEscalar ?? ''"
                   (input)="emitirTexto($any($event.target).value)" />
          }
          @case ('numero') {
            <input [id]="idControl"
                   type="number"
                   class="w-full rounded-xl border border-gray-200 bg-white px-3 py-2 text-xs font-medium text-gray-800 shadow-sm focus:ring-2 focus:ring-ihss-600 focus:outline-none disabled:bg-gray-100 disabled:text-gray-500"
                   [required]="campo.obligatorio"
                   [attr.aria-required]="campo.obligatorio ? 'true' : null"
                   [readOnly]="campo.soloLectura"
                   [value]="valorEscalar ?? ''"
                   (input)="emitirNumero($any($event.target).value)" />
          }
          @case ('fecha') {
            <input [id]="idControl"
                   type="date"
                   class="w-full rounded-xl border border-gray-200 bg-white px-3 py-2 text-xs font-medium text-gray-800 shadow-sm focus:ring-2 focus:ring-ihss-600 focus:outline-none disabled:bg-gray-100 disabled:text-gray-500"
                   [required]="campo.obligatorio"
                   [attr.aria-required]="campo.obligatorio ? 'true' : null"
                   [readOnly]="campo.soloLectura"
                   [value]="valorEscalar ?? ''"
                   (input)="emitirTexto($any($event.target).value)" />
          }
          @case ('texto-largo') {
            <textarea [id]="idControl"
                      rows="3"
                      class="min-h-24 w-full rounded-xl border border-gray-200 bg-white p-2.5 text-xs font-medium text-gray-800 shadow-sm focus:ring-2 focus:ring-ihss-600 focus:outline-none disabled:bg-gray-100 disabled:text-gray-500"
                      [required]="campo.obligatorio"
                      [attr.aria-required]="campo.obligatorio ? 'true' : null"
                      [readOnly]="campo.soloLectura"
                      [value]="valorEscalar ?? ''"
                      (input)="emitirTexto($any($event.target).value)"></textarea>
          }
          @case ('selector-catalogo') {
            <select [id]="idControl"
                    class="w-full rounded-xl border border-gray-200 bg-white px-3 py-2 text-xs font-medium text-gray-800 shadow-sm focus:ring-2 focus:ring-ihss-600 focus:outline-none disabled:bg-gray-100 disabled:text-gray-500"
                    [required]="campo.obligatorio"
                    [attr.aria-required]="campo.obligatorio ? 'true' : null"
                    [disabled]="campo.soloLectura || opcionesDisponibles.length === 0"
                    [value]="valorEscalar ?? ''"
                    (change)="emitirSeleccion($any($event.target).value)">
              <option value="" [selected]="!valorEscalar">Seleccione una opción</option>
              @for (opcion of opcionesDisponibles; track opcion.codigo) {
                <option [value]="opcion.codigo" [selected]="opcion.codigo === valorEscalar">{{ opcion.valor }}</option>
              }
            </select>
            @if (opcionesDisponibles.length === 0) {
              <p class="mt-1 text-[11px] font-semibold text-amber-700" role="status">No hay opciones disponibles.</p>
            }
          }
          @case ('checkbox') {
            <div class="mt-2 flex items-center gap-2">
              <input [id]="idControl"
                     type="checkbox"
                     class="h-4 w-4 rounded border-gray-300 text-blue-600 focus:ring-blue-500"
                     [required]="campo.obligatorio"
                     [attr.aria-required]="campo.obligatorio ? 'true' : null"
                     [disabled]="campo.soloLectura"
                     [checked]="valorBooleano"
                     (change)="emitirBooleano($any($event.target).checked)" />
              <span class="text-xs font-medium text-gray-600">Marcar como afirmativo</span>
            </div>
          }
          @case ('formula') {
            <div [id]="idControl"
                 class="flex items-center justify-between gap-3 rounded-xl border border-gray-200 bg-gray-50 px-3 py-2 text-xs font-bold text-ihss-900 shadow-inner"
                 aria-readonly="true">
              <span>{{ textoLectura === '-' ? 'Calculado automáticamente' : textoLectura }}</span>
              <span class="rounded-md border border-blue-100 bg-blue-50 px-2 py-0.5 text-[10px] font-semibold text-blue-700">
                {{ campo.formula || 'Fórmula calculada' }}
              </span>
            </div>
          }
          @case ('desconocido') {
            <div [id]="idControl"
                 class="rounded-xl border border-amber-200 bg-amber-50 px-3 py-2 text-xs text-amber-900"
                 role="status"
                 aria-readonly="true">
              <p class="font-bold">Tipo de campo no soportado: {{ tipoOriginalVisible }}</p>
              <p class="mt-1">El control permanece bloqueado para evitar guardar información con una semántica incorrecta.</p>
              @if (tieneValorActual) {
                <p class="mt-1 font-medium">Valor existente: {{ textoLectura }}</p>
              }
            </div>
          }
        }
      }
    </div>
  `
})
export class DynamicFieldRendererComponent {
  @Input() campo: CampoFormulario = {
    clave: '',
    etiqueta: '',
    tipo: 'desconocido',
    obligatorio: false,
    soloLectura: true
  };
  @Input() valor: ValorRespuestaFormulario = null;
  @Input() opcionesCatalogo: ReadonlyArray<OpcionCampoRenderer> = [];
  @Input() modoLectura = false;
  @Input() idPrefix = 'campo';
  @Output() readonly valorChange = new EventEmitter<ValorRespuestaFormulario>();

  get tipo(): TipoCampoRenderer {
    return normalizarTipoCampoRenderer(this.campo?.tipo).tipo;
  }

  get tipoOriginalVisible(): string {
    return this.campo?.tipoOriginal?.trim()
      || normalizarTipoCampoRenderer(this.campo?.tipo).tipoOriginal
      || 'sin tipo';
  }

  get idControl(): string {
    const clave = (this.campo?.clave || 'sin-clave')
      .replace(/[^A-Za-z0-9_-]+/g, '-')
      .replace(/^-+|-+$/g, '') || 'sin-clave';
    const prefijo = (this.idPrefix || 'campo')
      .replace(/[^A-Za-z0-9_-]+/g, '-')
      .replace(/^-+|-+$/g, '') || 'campo';
    return `${prefijo}-${clave}`;
  }

  get valorEscalar(): string | number | boolean | null {
    return Array.isArray(this.valor) ? null : this.valor;
  }

  get valorBooleano(): boolean {
    return this.valor === true;
  }

  get valorMultiple(): string[] {
    if (Array.isArray(this.valor)) return this.valor.map(valor => String(valor));
    if (!tieneValorRespuesta(this.valor)) return [];
    return [String(this.valor)];
  }

  get opcionesDisponibles(): OpcionCampoRenderer[] {
    const opcionesCatalogo = this.opcionesCatalogo
      .map(opcion => ({ codigo: String(opcion.codigo), valor: String(opcion.valor) }))
      .filter(opcion => opcion.codigo.trim() !== '');

    if (opcionesCatalogo.length > 0) {
      return this.deduplicarOpciones(opcionesCatalogo);
    }

    const opcionesInline = (this.campo?.opciones ?? [])
      .map(opcion => String(opcion).trim())
      .filter(Boolean)
      .map(opcion => ({ codigo: opcion, valor: opcion }));

    return this.deduplicarOpciones(opcionesInline);
  }

  get textoLectura(): string {
    if (this.tipo === 'checkbox') {
      return this.valor === null || this.valor === undefined
        ? '-'
        : this.valor === true ? 'Sí' : 'No';
    }

    if (this.tipo === 'selector-catalogo' || this.tipo === 'radio') {
      if (!tieneValorRespuesta(this.valor) || Array.isArray(this.valor)) return '-';
      return this.etiquetaOpcion(String(this.valor));
    }

    if (this.tipo === 'catalogo-multiple') {
      const valores = this.valorMultiple;
      return valores.length > 0
        ? valores.map(valor => this.etiquetaOpcion(valor)).join(', ')
        : '-';
    }

    return formatearValorRespuesta(this.valor);
  }

  get tieneValorActual(): boolean {
    return tieneValorRespuesta(this.valor);
  }

  idOpcion(indice: number): string {
    return `${this.idControl}-opcion-${indice + 1}`;
  }

  opcionSeleccionada(codigo: string): boolean {
    return this.valorMultiple.includes(String(codigo));
  }

  emitirTexto(valor: unknown): void {
    this.valorChange.emit(valor === null || valor === undefined ? null : String(valor));
  }

  emitirNumero(valor: unknown): void {
    if (valor === null || valor === undefined || valor === '') {
      this.valorChange.emit(null);
      return;
    }

    const numero = Number(valor);
    this.valorChange.emit(Number.isFinite(numero) ? numero : null);
  }

  emitirBooleano(valor: unknown): void {
    this.valorChange.emit(Boolean(valor));
  }

  emitirSeleccion(valor: unknown): void {
    this.valorChange.emit(valor === null || valor === undefined || valor === '' ? null : String(valor));
  }

  alternarOpcion(codigo: string, event: Event): void {
    const input = event.target as HTMLInputElement | null;
    if (!input || this.campo.soloLectura) return;

    const codigoNormalizado = String(codigo);
    const actuales = this.valorMultiple.filter(valor => valor !== codigoNormalizado);
    const nuevos = input.checked ? [...actuales, codigoNormalizado] : actuales;
    this.valorChange.emit(Array.from(new Set(nuevos)));
  }

  private etiquetaOpcion(codigo: string): string {
    const opcion = this.opcionesDisponibles.find(item => item.codigo === codigo);
    return opcion ? opcion.valor : codigo;
  }

  private deduplicarOpciones(opciones: OpcionCampoRenderer[]): OpcionCampoRenderer[] {
    const vistos = new Set<string>();
    return opciones.filter(opcion => {
      if (vistos.has(opcion.codigo)) return false;
      vistos.add(opcion.codigo);
      return true;
    });
  }
}
