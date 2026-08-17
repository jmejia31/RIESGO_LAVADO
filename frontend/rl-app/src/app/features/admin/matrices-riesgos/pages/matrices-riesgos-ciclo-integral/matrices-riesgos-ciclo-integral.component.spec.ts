import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { MatricesRiesgosService } from '../../data-access/matrices-riesgos.service';
import { MatricesRiesgosCicloIntegralComponent } from './matrices-riesgos-ciclo-integral.component';

describe('MatricesRiesgosCicloIntegralComponent', () => {
  let fixture: ComponentFixture<MatricesRiesgosCicloIntegralComponent>;
  let component: MatricesRiesgosCicloIntegralComponent;
  let service: { listarEvaluaciones: ReturnType<typeof vi.fn> };

  beforeEach(async () => {
    service = {
      listarEvaluaciones: vi.fn().mockReturnValue(of({
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
      }))
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

  it('inicia en la matriz y precarga evaluaciones operativas', () => {
    expect(component.vista()).toBe('matriz');
    expect(component.evaluaciones()).toHaveLength(1);
    expect(service.listarEvaluaciones).toHaveBeenCalledWith({ pagina: 1, registrosPorPagina: 200 });
  });

  it('cambia a vista riesgos sin invocar recarga adicional de evaluaciones', () => {
    const llamadasPrevias = service.listarEvaluaciones.mock.calls.length;
    component.seleccionarVista('riesgos');
    expect(component.vista()).toBe('riesgos');
    expect(component.error()).toBeNull();
    expect(service.listarEvaluaciones.mock.calls.length).toBe(llamadasPrevias);
  });

  it('recarga evaluaciones al entrar a mitigación y monitoreo', () => {
    component.seleccionarVista('mitigacion');
    expect(component.vista()).toBe('mitigacion');
    component.seleccionarVista('monitoreo');
    expect(component.vista()).toBe('monitoreo');
    expect(service.listarEvaluaciones).toHaveBeenCalledTimes(3);
  });

  it('conserva error funcional cuando falla la carga operativa con estructura error.mensaje', () => {
    service.listarEvaluaciones.mockReturnValue(throwError(() => ({ error: { mensaje: 'Consulta fallida' } })));
    component.seleccionarVista('mitigacion');
    expect(component.error()).toBe('Consulta fallida');
  });

  it('conserva error funcional con propiedad message cuando falla la carga', () => {
    service.listarEvaluaciones.mockReturnValue(throwError(() => ({ message: 'Fallo de red al listar evaluaciones' })));
    component.seleccionarVista('monitoreo');
    expect(component.error()).toBe('Fallo de red al listar evaluaciones');
  });

  it('usa mensaje fallback cuando el objeto de error está vacío', () => {
    service.listarEvaluaciones.mockReturnValue(throwError(() => ({})));
    component.seleccionarVista('mitigacion');
    expect(component.error()).toBe('No se pudieron cargar las evaluaciones operativas.');
  });
});
