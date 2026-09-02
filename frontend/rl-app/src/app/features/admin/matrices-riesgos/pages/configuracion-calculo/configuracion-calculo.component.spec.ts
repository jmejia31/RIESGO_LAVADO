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
      { provide: MatricesRiesgosService, useValue: { metodologiaVigente: vi.fn(() => of({ versionFormularioId: 1, codigo: 'FORM', version: 1, secciones: [], catalogos: [], reglas: [] })) } }
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
});
