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
      listarEvaluaciones: vi.fn().mockReturnValue(of([{
        evaId: 20,
        evaRiesgoId: 7,
        evaVersionId: 10,
        evaEstado: 'BORRADOR',
        evaDataJson: '{}',
        evaDataCalcJson: '{}',
        evaVri: 7,
        evaVrr: 4,
        evaFechaEval: '2026-08-07',
        evaUsrEval: 1,
        evaVersionRow: 1,
        evaActivo: true
      }]))
    };
    await TestBed.configureTestingModule({
      imports: [MatricesRiesgosCicloIntegralComponent],
      providers: [{ provide: MatricesRiesgosService, useValue: service }]
    }).overrideComponent(MatricesRiesgosCicloIntegralComponent, {
      set: { template: '<div>{{ vista() }}</div>' }
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

  it('recarga evaluaciones al entrar a mitigación y monitoreo', () => {
    component.seleccionarVista('mitigacion');
    component.seleccionarVista('monitoreo');
    expect(service.listarEvaluaciones).toHaveBeenCalledTimes(3);
  });

  it('conserva error funcional cuando falla la carga operativa', () => {
    service.listarEvaluaciones.mockReturnValue(throwError(() => ({ error: { mensaje: 'Consulta fallida' } })));
    component.seleccionarVista('mitigacion');
    expect(component.error()).toBe('Consulta fallida');
  });
});
