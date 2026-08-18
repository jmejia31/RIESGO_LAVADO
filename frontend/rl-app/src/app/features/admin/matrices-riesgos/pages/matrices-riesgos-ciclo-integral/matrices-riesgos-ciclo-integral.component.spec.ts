import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { MatricesRiesgosService } from '../../data-access/matrices-riesgos.service';
import { MatricesRiesgosCicloIntegralComponent } from './matrices-riesgos-ciclo-integral.component';

describe('MatricesRiesgosCicloIntegralComponent', () => {
  let fixture: ComponentFixture<MatricesRiesgosCicloIntegralComponent>;
  let component: MatricesRiesgosCicloIntegralComponent;
  let service: { listarEvaluaciones: ReturnType<typeof vi.fn> };

  const paginadoOperativo = {
    items: [{
      evaId: 20,
      evaRiesgoId: 7,
      riesgoCodigo: 'RIE-007',
      riesgoNombre: 'Riesgo 7',
      evaVersionId: 10,
      versionCodigo: 'FORM_A',
      versionNumero: 2,
      estado: 'BORRADOR',
      vri: 7,
      vrr: 4,
      nivelResidual: 'MEDIO',
      fechaEval: '2026-08-07'
    }],
    pagina: 1,
    registrosPorPagina: 200,
    totalRegistros: 1,
    totalPaginas: 1
  };

  beforeEach(async () => {
    service = {
      listarEvaluaciones: vi.fn().mockReturnValue(of(paginadoOperativo))
    };
    await TestBed.configureTestingModule({
      imports: [MatricesRiesgosCicloIntegralComponent],
      providers: [{ provide: MatricesRiesgosService, useValue: service }]
    }).overrideComponent(MatricesRiesgosCicloIntegralComponent, {
      set: { template: '<div class="vista">{{ vista() }}</div>' }
    }).compileComponents();
    fixture = TestBed.createComponent(MatricesRiesgosCicloIntegralComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('inicia en matriz sin precargar 200 evaluaciones operativas', () => {
    expect(component.vista()).toBe('matriz');
    expect(component.evaluaciones()).toEqual([]);
    expect(service.listarEvaluaciones).not.toHaveBeenCalled();
  });

  it('cambia a vista riesgos sin consultar evaluaciones operativas', () => {
    component.seleccionarVista('riesgos');
    expect(component.vista()).toBe('riesgos');
    expect(component.error()).toBeNull();
    expect(service.listarEvaluaciones).not.toHaveBeenCalled();
  });

  it('carga evaluaciones solo al entrar a mitigacion y monitoreo', () => {
    component.seleccionarVista('mitigacion');
    expect(component.vista()).toBe('mitigacion');
    expect(component.evaluaciones()).toHaveLength(1);

    component.seleccionarVista('monitoreo');
    expect(component.vista()).toBe('monitoreo');
    expect(service.listarEvaluaciones).toHaveBeenCalledTimes(2);
    expect(service.listarEvaluaciones).toHaveBeenNthCalledWith(1, { pagina: 1, registrosPorPagina: 200 });
    expect(service.listarEvaluaciones).toHaveBeenNthCalledWith(2, { pagina: 1, registrosPorPagina: 200 });
  });

  it('normaliza items no-array a arreglo vacio', () => {
    service.listarEvaluaciones.mockReturnValue(of({ ...paginadoOperativo, items: null }));
    component.seleccionarVista('mitigacion');
    expect(component.evaluaciones()).toEqual([]);
    expect(component.error()).toBeNull();
  });

  it('limpia datos operativos obsoletos y conserva error.mensaje al fallar', () => {
    component.seleccionarVista('mitigacion');
    expect(component.evaluaciones()).toHaveLength(1);

    service.listarEvaluaciones.mockReturnValue(throwError(() => ({ error: { mensaje: 'Consulta fallida' } })));
    component.seleccionarVista('monitoreo');

    expect(component.evaluaciones()).toEqual([]);
    expect(component.error()).toBe('Consulta fallida');
  });

  it('conserva error funcional con propiedad message cuando falla la carga', () => {
    service.listarEvaluaciones.mockReturnValue(throwError(() => ({ message: 'Fallo de red al listar evaluaciones' })));
    component.seleccionarVista('monitoreo');
    expect(component.error()).toBe('Fallo de red al listar evaluaciones');
  });

  it('usa mensaje fallback cuando el objeto de error esta vacio', () => {
    service.listarEvaluaciones.mockReturnValue(throwError(() => ({})));
    component.seleccionarVista('mitigacion');
    expect(component.error()).toBe('No se pudieron cargar las evaluaciones operativas.');
  });
});
