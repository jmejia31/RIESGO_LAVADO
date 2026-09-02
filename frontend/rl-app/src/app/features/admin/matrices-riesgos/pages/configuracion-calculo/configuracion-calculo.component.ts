import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { forkJoin } from 'rxjs';
import Swal from 'sweetalert2';
import { MatricesRiesgosService } from '../../data-access/matrices-riesgos.service';
import { CalculoConfiguracionService } from '../../data-access/calculo-configuracion.service';
import {
  ActualizarFormulaBorradorDto,
  ActualizarFuncionBorradorDto,
  ActualizarParametroBorradorDto,
  CambiarEstadoConfiguracionDto,
  CrearFormulaDto,
  CrearFuncionDto,
  CrearFuncionVersionDto,
  CrearParametroDto,
  CrearParametroVersionDto,
  EstadoConfiguracion,
  FormulaDto,
  FormulaUsageDto,
  FormulaVersionDto,
  FuncionArgumentoDto,
  FuncionArgumentoGuardarDto,
  FuncionDto,
  FuncionVersionDto,
  ParametroDto,
  ParametroVersionDto
} from '../../models/calculo-configuracion.models';
import { MetodologiaFormulario } from '../../models/matrices-riesgos.models';

type ConfigTab = 'formulas' | 'funciones' | 'parametros' | 'reglas' | 'catalogos';

interface FormulaDraftForm {
  codigo: string;
  nombre: string;
  descripcion: string;
  expresion: string;
  tipoResultado: string;
}

interface FunctionDraftForm {
  codigo: string;
  nombre: string;
  descripcion: string;
  categoria: string;
  tipo: 'NATIVE' | 'COMPOSITE';
  tipoResultado: string;
  handlerKey: string;
  definicionDsl: string;
  minArity: number;
  maxArity: number | null;
}

interface ParameterDraftForm {
  codigo: string;
  nombre: string;
  descripcion: string;
  tipo: string;
  valorEntero: number | null;
  valorDecimal: number | null;
  valorBooleano: boolean | null;
  valorTexto: string;
  valorFecha: string;
}

@Component({
  selector: 'app-configuracion-calculo',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './configuracion-calculo.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ConfiguracionCalculoComponent implements OnInit {
  private readonly config = inject(CalculoConfiguracionService);
  private readonly matrices = inject(MatricesRiesgosService);

  readonly tab = signal<ConfigTab>('formulas');
  readonly cargando = signal(true);
  readonly guardando = signal(false);
  readonly error = signal<string | null>(null);
  readonly mensaje = signal<string | null>(null);
  readonly busqueda = signal('');
  readonly incluirInactivas = signal(true);
  readonly formulas = signal<FormulaDto[]>([]);
  readonly funciones = signal<FuncionDto[]>([]);
  readonly parametros = signal<ParametroDto[]>([]);
  readonly metodologia = signal<MetodologiaFormulario | null>(null);

  readonly formulaSeleccionada = signal<FormulaDto | null>(null);
  readonly formulaVersiones = signal<FormulaVersionDto[]>([]);
  readonly formulaUsages = signal<FormulaUsageDto[]>([]);
  readonly funcionSeleccionada = signal<FuncionDto | null>(null);
  readonly funcionVersiones = signal<FuncionVersionDto[]>([]);
  readonly funcionArgumentos = signal<FuncionArgumentoDto[]>([]);
  readonly parametroSeleccionado = signal<ParametroDto | null>(null);
  readonly parametroVersiones = signal<ParametroVersionDto[]>([]);
  readonly handlerKeys = ['IF_V1', 'IFERROR_V1', 'ROUND_V1', 'ROUNDDOWN_V1', 'MAX_V1', 'MIN_V1', 'MOD_V1', 'OR_V1', 'AND_V1', 'LOOKUP_V1'];

  readonly modoFormula = signal<'none' | 'create' | 'version' | 'edit'>('none');
  readonly modoFuncion = signal<'none' | 'create' | 'version' | 'edit'>('none');
  readonly modoParametro = signal<'none' | 'create' | 'version' | 'edit'>('none');
  readonly formulaVersionEditando = signal<FormulaVersionDto | null>(null);
  readonly funcionVersionEditando = signal<FuncionVersionDto | null>(null);
  readonly parametroVersionEditando = signal<ParametroVersionDto | null>(null);

  formulaForm: FormulaDraftForm = this.nuevaFormulaForm();
  funcionForm: FunctionDraftForm = this.nuevaFuncionForm();
  parametroForm: ParameterDraftForm = this.nuevoParametroForm();
  funcionArgumentosForm: FuncionArgumentoGuardarDto[] = [];

  ngOnInit(): void { this.cargar(); }

  cargar(): void {
    this.cargando.set(true);
    this.error.set(null);
    forkJoin({
      formulas: this.config.listarFormulas(this.incluirInactivas()),
      funciones: this.config.listarFunciones(this.incluirInactivas()),
      parametros: this.config.listarParametros(this.incluirInactivas()),
      metodologia: this.matrices.metodologiaVigente()
    }).subscribe({
      next: data => {
        this.formulas.set(data.formulas);
        this.funciones.set(data.funciones);
        this.parametros.set(data.parametros);
        this.metodologia.set(data.metodologia);
        this.cargando.set(false);
      },
      error: error => { this.cargando.set(false); this.error.set(this.mensajeError(error, 'No se pudo cargar la configuración de cálculo.')); }
    });
  }

  seleccionarTab(tab: ConfigTab): void {
    this.tab.set(tab);
    this.busqueda.set('');
    this.mensaje.set(null);
    this.error.set(null);
  }

  cambiarInactivas(): void { this.cargar(); }

  formulasFiltradas(): FormulaDto[] { return this.filtrar(this.formulas()); }
  funcionesFiltradas(): FuncionDto[] { return this.filtrar(this.funciones()); }
  parametrosFiltrados(): ParametroDto[] { return this.filtrar(this.parametros()); }

  seleccionarFormula(item: FormulaDto): void {
    this.formulaSeleccionada.set(item);
    this.modoFormula.set('none');
    forkJoin({ versiones: this.config.listarFormulaVersiones(item.id), usos: this.config.listarFormulaUsages(item.id) }).subscribe({
      next: data => { this.formulaVersiones.set(data.versiones); this.formulaUsages.set(data.usos); },
      error: error => this.error.set(this.mensajeError(error, 'No se pudo cargar el detalle de la fórmula.'))
    });
  }

  seleccionarFuncion(item: FuncionDto): void {
    this.funcionSeleccionada.set(item);
    this.modoFuncion.set('none');
    this.config.listarFuncionVersiones(item.id).subscribe({
      next: versiones => { this.funcionVersiones.set(versiones); this.funcionArgumentos.set([]); },
      error: error => this.error.set(this.mensajeError(error, 'No se pudo cargar el detalle de la función.'))
    });
  }

  seleccionarFuncionVersion(item: FuncionVersionDto): void {
    this.funcionVersionEditando.set(item.estado === 'DRAFT' ? item : null);
    this.config.listarFuncionArgumentos(item.id).subscribe({
      next: argumentos => {
        this.funcionArgumentos.set(argumentos);
        if (item.estado === 'DRAFT') {
          this.funcionForm = {
            codigo: this.funcionSeleccionada()?.codigo ?? '',
            nombre: this.funcionSeleccionada()?.nombre ?? '',
            descripcion: this.funcionSeleccionada()?.descripcion ?? '',
            categoria: 'CALCULO',
            tipo: item.tipo,
            tipoResultado: item.tipoResultado,
            handlerKey: item.handlerKey ?? '',
            definicionDsl: item.definicionDsl ?? '',
            minArity: item.minArity,
            maxArity: item.maxArity ?? null
          };
          this.funcionArgumentosForm = argumentos.map(argumento => ({
            posicion: argumento.posicion,
            codigo: argumento.codigo,
            nombre: argumento.nombre,
            tipo: argumento.tipo,
            requerido: argumento.requerido,
            variadic: argumento.variadic,
            valorDefaultJson: argumento.valorDefaultJson,
            descripcion: argumento.descripcion
          }));
          this.modoFuncion.set('edit');
        }
      },
      error: error => this.error.set(this.mensajeError(error, 'No se pudieron cargar los argumentos de la función.'))
    });
  }

  seleccionarParametro(item: ParametroDto): void {
    this.parametroSeleccionado.set(item);
    this.modoParametro.set('none');
    this.config.listarParametroVersiones(item.id).subscribe({
      next: versiones => this.parametroVersiones.set(versiones),
      error: error => this.error.set(this.mensajeError(error, 'No se pudo cargar el detalle del parámetro.'))
    });
  }

  cargarParametroVersion(version: ParametroVersionDto): void {
    if (version.estado !== 'DRAFT') return;
    this.parametroVersionEditando.set(version);
    this.parametroForm = {
      codigo: this.parametroSeleccionado()?.codigo ?? '',
      nombre: this.parametroSeleccionado()?.nombre ?? '',
      descripcion: this.parametroSeleccionado()?.descripcion ?? '',
      tipo: version.tipo,
      valorEntero: version.valorEntero ?? null,
      valorDecimal: version.valorDecimal ?? null,
      valorBooleano: version.valorBooleano ?? null,
      valorTexto: version.valorTexto ?? '',
      valorFecha: version.valorFecha ?? ''
    };
    this.modoParametro.set('edit');
  }

  cargarFormulaVersion(version: FormulaVersionDto): void {
    this.formulaForm = { codigo: this.formulaSeleccionada()?.codigo ?? '', nombre: this.formulaSeleccionada()?.nombre ?? '', descripcion: this.formulaSeleccionada()?.descripcion ?? '', expresion: version.expresion, tipoResultado: version.tipoResultado };
    this.formulaVersionEditando.set(version);
    this.modoFormula.set('edit');
  }

  prepararNuevaFormula(): void { this.formulaForm = this.nuevaFormulaForm(); this.modoFormula.set('create'); this.formulaSeleccionada.set(null); }
  prepararVersionFormula(): void { this.formulaForm = this.nuevaFormulaForm(); this.formulaVersionEditando.set(null); this.modoFormula.set('version'); }
  prepararNuevaFuncion(): void { this.funcionForm = this.nuevaFuncionForm(); this.funcionArgumentosForm = []; this.modoFuncion.set('create'); this.funcionSeleccionada.set(null); }
  prepararVersionFuncion(): void { this.funcionForm = this.nuevaFuncionForm(); this.funcionArgumentosForm = []; this.funcionVersionEditando.set(null); this.modoFuncion.set('version'); }
  prepararNuevoParametro(): void { this.parametroForm = this.nuevoParametroForm(); this.modoParametro.set('create'); this.parametroSeleccionado.set(null); }
  prepararVersionParametro(): void { this.parametroForm = this.nuevoParametroForm(); this.parametroVersionEditando.set(null); this.modoParametro.set('version'); }

  guardarFormula(): void {
    if (!this.validarFormula()) return;
    const versionDto = { expresion: this.formulaForm.expresion.trim(), tipoResultado: this.formulaForm.tipoResultado };
    const request = this.modoFormula() === 'create'
      ? this.config.crearFormula(this.crearFormulaDto())
      : this.modoFormula() === 'edit' && this.formulaVersionEditando()
        ? this.config.actualizarFormulaBorrador(this.formulaVersionEditando()!.id, { ...versionDto, versionRow: this.formulaVersionEditando()!.versionRow })
        : this.config.crearFormulaVersion(this.formulaSeleccionada()!.id, versionDto);
    this.ejecutar(request)
      .subscribe({ next: () => this.finalizar('Fórmula guardada correctamente.'), error: error => this.fallar(error, 'No se pudo guardar la fórmula.') });
  }

  guardarFormulaBorrador(version: FormulaVersionDto): void {
    if (version.estado !== 'DRAFT' || !this.formulaForm.expresion.trim()) return;
    const dto: ActualizarFormulaBorradorDto = { expresion: this.formulaForm.expresion.trim(), tipoResultado: this.formulaForm.tipoResultado, versionRow: version.versionRow };
    this.ejecutar(this.config.actualizarFormulaBorrador(version.id, dto)).subscribe({ next: () => { this.finalizar('Borrador de fórmula actualizado.'); if (this.formulaSeleccionada()) this.seleccionarFormula(this.formulaSeleccionada()!); }, error: error => this.fallar(error, 'No se pudo actualizar el borrador de fórmula.') });
  }

  cambiarEstadoFormula(item: FormulaDto, estado: 'INACTIVE' | 'ACTIVE' | 'RETIRED'): void {
    void this.confirmar(`¿Cambiar el estado de ${item.codigo}?`).then(ok => {
      if (!ok) return;
      const dto: CambiarEstadoConfiguracionDto = { estado, versionRow: item.versionRow };
      this.ejecutar(this.config.cambiarEstadoFormula(item.id, dto)).subscribe({ next: () => { this.finalizar('Estado de fórmula actualizado.'); this.cargar(); }, error: error => this.fallar(error, 'No se pudo cambiar el estado de la fórmula.') });
    });
  }

  guardarFuncion(): void {
    if (!this.validarFuncion()) return;
    const version: CrearFuncionVersionDto = this.crearFuncionVersionDto();
    const observable = this.modoFuncion() === 'create'
      ? this.config.crearFuncion({ codigo: this.funcionForm.codigo.trim().toUpperCase(), nombre: this.funcionForm.nombre.trim(), descripcion: this.funcionForm.descripcion.trim() || null, categoria: this.funcionForm.categoria.trim() || 'CALCULO', versionInicial: version })
      : this.modoFuncion() === 'edit' && this.funcionVersionEditando()
        ? this.config.actualizarFuncionBorrador(this.funcionVersionEditando()!.id, { ...version, versionRow: this.funcionVersionEditando()!.versionRow })
        : this.config.crearFuncionVersion(this.funcionSeleccionada()!.id, version);
    this.ejecutar(observable).subscribe({ next: () => this.finalizar('Función guardada correctamente.'), error: error => this.fallar(error, 'No se pudo guardar la función.') });
  }

  guardarParametro(): void {
    if (!this.validarParametro()) return;
    const version = this.crearParametroVersionDto();
    const observable = this.modoParametro() === 'create'
      ? this.config.crearParametro({ codigo: this.parametroForm.codigo.trim().toUpperCase(), nombre: this.parametroForm.nombre.trim(), descripcion: this.parametroForm.descripcion.trim() || null, versionInicial: version })
      : this.modoParametro() === 'edit' && this.parametroVersionEditando()
        ? this.config.actualizarParametroBorrador(this.parametroVersionEditando()!.id, { ...version, versionRow: this.parametroVersionEditando()!.versionRow })
        : this.config.crearParametroVersion(this.parametroSeleccionado()!.id, version);
    this.ejecutar(observable).subscribe({ next: () => this.finalizar('Parámetro guardado correctamente.'), error: error => this.fallar(error, 'No se pudo guardar el parámetro.') });
  }

  actualizarFuncionBorrador(version: FuncionVersionDto): void {
    if (version.estado !== 'DRAFT' || !this.validarFuncion()) return;
    const dto: ActualizarFuncionBorradorDto = { ...this.crearFuncionVersionDto(), versionRow: version.versionRow };
    this.ejecutar(this.config.actualizarFuncionBorrador(version.id, dto)).subscribe({ next: () => { this.finalizar('Borrador de función actualizado.'); if (this.funcionSeleccionada()) this.seleccionarFuncion(this.funcionSeleccionada()!); }, error: error => this.fallar(error, 'No se pudo actualizar el borrador de función.') });
  }

  actualizarParametroBorrador(version: ParametroVersionDto): void {
    if (version.estado !== 'DRAFT' || !this.validarParametro()) return;
    const dto: ActualizarParametroBorradorDto = { ...this.crearParametroVersionDto(), versionRow: version.versionRow };
    this.ejecutar(this.config.actualizarParametroBorrador(version.id, dto)).subscribe({ next: () => { this.finalizar('Borrador de parámetro actualizado.'); if (this.parametroSeleccionado()) this.seleccionarParametro(this.parametroSeleccionado()!); }, error: error => this.fallar(error, 'No se pudo actualizar el borrador de parámetro.') });
  }

  cambiarEstadoVersion(kind: 'function' | 'parameter', item: FuncionVersionDto | ParametroVersionDto, estado: EstadoConfiguracion): void {
    if (estado === 'PUBLISHED') { this.error.set('La publicación requiere la validación del Publication Gate único.'); return; }
    const dto: CambiarEstadoConfiguracionDto = { estado, versionRow: item.versionRow };
    const request = kind === 'function' ? this.config.cambiarEstadoFuncionVersion(item.id, dto) : this.config.cambiarEstadoParametroVersion(item.id, dto);
    this.ejecutar(request).subscribe({ next: () => { this.finalizar('Estado actualizado.'); if (kind === 'function' && this.funcionSeleccionada()) this.seleccionarFuncion(this.funcionSeleccionada()!); if (kind === 'parameter' && this.parametroSeleccionado()) this.seleccionarParametro(this.parametroSeleccionado()!); }, error: error => this.fallar(error, 'No se pudo cambiar el estado.') });
  }

  agregarArgumento(): void { this.funcionArgumentosForm = [...this.funcionArgumentosForm, { posicion: this.funcionArgumentosForm.length + 1, codigo: `ARG_${this.funcionArgumentosForm.length + 1}`, nombre: '', tipo: 'DECIMAL', requerido: true, variadic: false, descripcion: null }]; }
  quitarArgumento(index: number): void { this.funcionArgumentosForm = this.funcionArgumentosForm.filter((_, i) => i !== index).map((item, i) => ({ ...item, posicion: i + 1 })); }

  valorParametro(version: ParametroVersionDto): string {
    const value = version.valorEntero ?? version.valorDecimal ?? version.valorBooleano ?? version.valorTexto ?? version.valorFecha;
    return value === null || value === undefined ? '—' : String(value);
  }

  estadoLabel(estado: string): string { return ({ DRAFT: 'Borrador', IN_REVIEW: 'En revisión', APPROVED: 'Aprobada', PUBLISHED: 'Publicada', RETIRED: 'Retirada', ARCHIVED: 'Archivada', ACTIVE: 'Activa', INACTIVE: 'Inactiva' } as Record<string, string>)[estado] ?? estado; }
  esPublicado(estado: string): boolean { return estado === 'PUBLISHED'; }

  private crearFormulaDto(): CrearFormulaDto { return { codigo: this.formulaForm.codigo.trim().toUpperCase(), nombre: this.formulaForm.nombre.trim(), descripcion: this.formulaForm.descripcion.trim() || null, versionInicial: { expresion: this.formulaForm.expresion.trim(), tipoResultado: this.formulaForm.tipoResultado } }; }
  private crearFuncionVersionDto(): CrearFuncionVersionDto { return { tipo: this.funcionForm.tipo, tipoResultado: this.funcionForm.tipoResultado, handlerKey: this.funcionForm.tipo === 'NATIVE' ? this.funcionForm.handlerKey.trim() : null, definicionDsl: this.funcionForm.tipo === 'COMPOSITE' ? this.funcionForm.definicionDsl.trim() : null, minArity: Number(this.funcionForm.minArity), maxArity: this.funcionForm.maxArity === null ? null : Number(this.funcionForm.maxArity), argumentos: this.funcionArgumentosForm }; }
  private crearParametroVersionDto(): CrearParametroVersionDto { const f = this.parametroForm; return { tipo: f.tipo, valorEntero: f.tipo === 'INTEGER' ? f.valorEntero : null, valorDecimal: f.tipo === 'DECIMAL' ? f.valorDecimal : null, valorBooleano: f.tipo === 'BOOLEAN' ? f.valorBooleano : null, valorTexto: f.tipo === 'TEXT' ? f.valorTexto : null, valorFecha: f.tipo === 'DATE' ? f.valorFecha || null : null }; }
  private validarFormula(): boolean { if (!this.formulaForm.expresion.trim() || (this.modoFormula() === 'create' && (!this.formulaForm.codigo.trim() || !this.formulaForm.nombre.trim()))) { this.error.set('Código, nombre y expresión son obligatorios.'); return false; } return true; }
  private validarFuncion(): boolean { if ((this.modoFuncion() === 'create' && (!this.funcionForm.codigo.trim() || !this.funcionForm.nombre.trim())) || (this.funcionForm.tipo === 'NATIVE' && !this.funcionForm.handlerKey.trim()) || (this.funcionForm.tipo === 'COMPOSITE' && !this.funcionForm.definicionDsl.trim())) { this.error.set('Completa los datos requeridos del contrato de función.'); return false; } return true; }
  private validarParametro(): boolean { if ((this.modoParametro() === 'create' && (!this.parametroForm.codigo.trim() || !this.parametroForm.nombre.trim())) || !this.parametroForm.tipo) { this.error.set('Código, nombre y tipo son obligatorios.'); return false; } return true; }
  private filtrar<T extends { codigo: string; nombre: string }>(items: T[]): T[] { const q = this.busqueda().trim().toLowerCase(); return q ? items.filter(item => item.codigo.toLowerCase().includes(q) || item.nombre.toLowerCase().includes(q)) : items; }
  private ejecutar(observable: import('rxjs').Observable<unknown>): import('rxjs').Observable<unknown> { this.guardando.set(true); this.error.set(null); return observable; }
  private finalizar(texto: string): void { this.guardando.set(false); this.mensaje.set(texto); this.cargar(); }
  private fallar(error: unknown, fallback: string): void { this.guardando.set(false); this.error.set(this.mensajeError(error, fallback)); }
  private mensajeError(error: unknown, fallback: string): string { const value = error as { error?: { mensaje?: string }; message?: string }; return value?.error?.mensaje || value?.message || fallback; }
  private confirmar(text: string): Promise<boolean> { return Swal.fire({ title: 'Confirmar cambio', text, icon: 'warning', showCancelButton: true, confirmButtonText: 'Continuar', cancelButtonText: 'Cancelar', reverseButtons: true, focusCancel: true }).then(result => result.isConfirmed); }
  private nuevaFormulaForm(): FormulaDraftForm { return { codigo: '', nombre: '', descripcion: '', expresion: '', tipoResultado: 'DECIMAL' }; }
  private nuevaFuncionForm(): FunctionDraftForm { return { codigo: '', nombre: '', descripcion: '', categoria: 'CALCULO', tipo: 'NATIVE', tipoResultado: 'DECIMAL', handlerKey: '', definicionDsl: '', minArity: 1, maxArity: 1 }; }
  private nuevoParametroForm(): ParameterDraftForm { return { codigo: '', nombre: '', descripcion: '', tipo: 'DECIMAL', valorEntero: null, valorDecimal: null, valorBooleano: null, valorTexto: '', valorFecha: '' }; }
}
