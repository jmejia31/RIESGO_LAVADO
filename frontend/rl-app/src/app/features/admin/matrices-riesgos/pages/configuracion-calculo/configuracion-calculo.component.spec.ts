import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { CalculoConfiguracionService } from '../../data-access/calculo-configuracion.service';
import { MatricesRiesgosService } from '../../data-access/matrices-riesgos.service';
import { ConfiguracionCalculoComponent } from './configuracion-calculo.component';

describe('ConfiguracionCalculoComponent', () => {
  let fixture: ComponentFixture<ConfiguracionCalculoComponent>;
  let component: ConfiguracionCalculoComponent;
  const config = {
    listarFormulas: vi.fn(() => of([{ id: 1, codigo: 'F_TOTAL', nombre: 'Total', estado: 'ACTIVE', versionRow: 1 }])),
    listarFunciones: vi.fn(() => of([{ id: 2, codigo: 'IF', nombre: 'Condicional', categoria: 'CALCULO', estado: 'ACTIVE', versionRow: 1 }])),
    listarParametros: vi.fn(() => of([{ id: 3, codigo: 'LIMITE', nombre: 'Límite', tipo: 'DECIMAL', estado: 'ACTIVE', versionRow: 1 }])),
    listarFormulaVersiones: vi.fn(() => of([])), listarFormulaUsages: vi.fn(() => of([])),
    listarFuncionVersiones: vi.fn(() => of([])), listarFuncionArgumentos: vi.fn(() => of([])),
    listarParametroVersiones: vi.fn(() => of([])),
    crearFormula: vi.fn(() => of(10)), crearFormulaVersion: vi.fn(() => of(11)), actualizarFormulaBorrador: vi.fn(() => of({ success: true })), cambiarEstadoFormula: vi.fn(() => of({ success: true })),
    crearFuncion: vi.fn(() => of(12)), crearFuncionVersion: vi.fn(() => of(13)), actualizarFuncionBorrador: vi.fn(() => of({ success: true })), cambiarEstadoFuncionVersion: vi.fn(() => of({ success: true })),
    crearParametro: vi.fn(() => of(14)), crearParametroVersion: vi.fn(() => of(15)), actualizarParametroBorrador: vi.fn(() => of({ success: true })), cambiarEstadoParametroVersion: vi.fn(() => of({ success: true }))
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [ConfiguracionCalculoComponent], providers: [
      { provide: CalculoConfiguracionService, useValue: config },
      { provide: MatricesRiesgosService, useValue: { metodologiaVigente: vi.fn(() => of({ versionFormularioId: 1, codigo: 'FORM', version: 1, secciones: [], catalogos: [{ codigo: 'CAT_RIESGO', nombre: 'Niveles de riesgo', elementos: [{ codigo: 'ALTO', valor: 'Alto', orden: 1 }] }], reglas: [{ codigo: 'RULE_VRI', version: '1.0', algoritmoId: 'ALG_VRI' }] })) } }
    ] }).compileComponents();
    fixture = TestBed.createComponent(ConfiguracionCalculoComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('carga los tres dominios mediante un único workspace', () => {
    expect(component.formulas()).toHaveLength(1);
    expect(component.funciones()).toHaveLength(1);
    expect(component.parametros()).toHaveLength(1);
    expect(fixture.nativeElement.textContent).toContain('Configuración de cálculo');
  });

  it('mantiene el contrato NATIVE/COMPOSITE fail-closed en la UX', () => {
    component.prepararNuevaFuncion();
    component.funcionForm.codigo = 'CLAMP';
    component.funcionForm.nombre = 'Clamp';
    component.funcionForm.handlerKey = '';
    component.guardarFuncion();
    expect(component.error()).toContain('contrato de función');
    component.funcionForm.tipo = 'COMPOSITE';
    component.funcionForm.definicionDsl = 'x * 2';
    component.funcionForm.handlerKey = '';
    component.guardarFuncion();
    expect(config.crearFuncion).toHaveBeenCalled();
  });

  it('no ofrece publicación directa de versiones', () => {
    component.cambiarEstadoVersion('function', { id: 2, versionRow: 1, estado: 'APPROVED' } as never, 'PUBLISHED');
    expect(config.cambiarEstadoFuncionVersion).not.toHaveBeenCalled();
    expect(component.error()).toContain('Publication Gate');
  });

  it('reserva el detalle solo cuando existe una selección', () => {
    const layout = () => fixture.nativeElement.querySelector('.config-data-layout') as HTMLElement;
    expect(layout().classList.contains('has-selection')).toBe(false);
    component.seleccionarFormula(component.formulas()[0]);
    fixture.detectChanges();
    expect(layout().classList.contains('has-selection')).toBe(true);
  });

  it('mantiene listas y detalle como panes independientes en las colecciones', () => {
    const tabs: Array<'formulas' | 'funciones' | 'parametros'> = ['formulas', 'funciones', 'parametros'];

    for (const tab of tabs) {
      component.seleccionarTab(tab);
      fixture.detectChanges();
      const layout = fixture.nativeElement.querySelector('.calculation-workspace > div.grid') as HTMLElement;

      expect(layout).not.toBeNull();
      expect(layout.querySelector('section:first-child > div.divide-y')).not.toBeNull();
      expect(layout.querySelector('section:nth-child(2)')).not.toBeNull();
    }
  });

  it('presenta catálogos como master/detail con sus elementos reales', () => {
    component.seleccionarTab('catalogos');
    component.seleccionarCatalogo(component.metodologia()!.catalogos[0]);
    fixture.detectChanges();
    expect(component.catalogoSeleccionado()?.codigo).toBe('CAT_RIESGO');
    expect(fixture.nativeElement.querySelector('.catalog-layout.has-selection')).not.toBeNull();
    expect(fixture.nativeElement.textContent).toContain('Alto');
  });

  it('navega dentro del resultado filtrado sin perder la selección lógica', () => {
    component.formulas.set([
      { id: 10, codigo: 'F_RIESGO_01', nombre: 'Riesgo uno', estado: 'ACTIVE', versionRow: 1 },
      { id: 11, codigo: 'F_RIESGO_02', nombre: 'Riesgo dos', estado: 'ACTIVE', versionRow: 1 },
      { id: 12, codigo: 'F_OTRA', nombre: 'Otra fórmula', estado: 'ACTIVE', versionRow: 1 }
    ]);
    component.busqueda.set('riesgo');
    component.seleccionarFormula(component.formulas()[0]);
    fixture.detectChanges();

    expect(component.formulaPosicion()).toBe('1 de 2');
    expect(component.puedeNavegar(component.formulasFiltradas(), component.formulaSeleccionada(), 1)).toBe(true);
    component.navegarFormula(1);
    fixture.detectChanges();

    expect(component.formulaSeleccionada()?.id).toBe(11);
    expect(component.formulaPosicion()).toBe('2 de 2');
    expect(component.puedeNavegar(component.formulasFiltradas(), component.formulaSeleccionada(), 1)).toBe(false);
    expect(fixture.nativeElement.querySelector('.config-detail-panel')).not.toBeNull();
  });

  it('mantiene navegación independiente para funciones y parámetros', () => {
    component.funciones.set([
      { id: 20, codigo: 'AND', nombre: 'AND', categoria: 'CALCULO', estado: 'ACTIVE', versionRow: 1 },
      { id: 21, codigo: 'OR', nombre: 'OR', categoria: 'CALCULO', estado: 'ACTIVE', versionRow: 1 }
    ]);
    component.parametros.set([
      { id: 30, codigo: 'P_ONE', nombre: 'Uno', tipo: 'DECIMAL', estado: 'ACTIVE', versionRow: 1 },
      { id: 31, codigo: 'P_TWO', nombre: 'Dos', tipo: 'DECIMAL', estado: 'ACTIVE', versionRow: 1 }
    ]);

    component.seleccionarFuncion(component.funciones()[0]);
    component.navegarFuncion(1);
    expect(component.funcionSeleccionada()?.id).toBe(21);
    expect(component.funcionPosicion()).toBe('2 de 2');

    component.seleccionarTab('parametros');
    component.seleccionarParametro(component.parametros()[0]);
    component.navegarParametro(1);
    expect(component.parametroSeleccionado()?.id).toBe(31);
    expect(component.parametroPosicion()).toBe('2 de 2');
  });
});
