import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { CONFIRMACION_CAMBIOS_HEADER } from '../../../../core/interceptors/confirmacion-cambios.interceptor';
import { CalculoConfiguracionService } from './calculo-configuracion.service';

describe('CalculoConfiguracionService', () => {
  let service: CalculoConfiguracionService;
  let http: HttpTestingController;
  const base = 'http://localhost:5043/api/matrices-riesgos/configuracion-calculo';

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(CalculoConfiguracionService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => { http.verify(); TestBed.resetTestingModule(); });

  it('usa el contrato tipado de fórmulas, versiones y usos', () => {
    service.listarFormulas().subscribe();
    const formulas = http.expectOne(request => request.url === `${base}/formulas`);
    expect(formulas.request.params.get('incluirInactivas')).toBe('true');
    formulas.flush({ success: true, datos: [] });

    service.listarFormulaVersiones(4).subscribe();
    expect(http.expectOne(`${base}/formulas/4/versiones`).request.method).toBe('GET');
    service.listarFormulaUsages(4).subscribe();
    expect(http.expectOne(`${base}/formulas/4/usos`).request.method).toBe('GET');
  });

  it('envía mutaciones con confirmación y sin interpolar payloads', () => {
    const dto = { codigo: 'F_TOTAL', nombre: 'Total', versionInicial: { expresion: 'a + b', tipoResultado: 'DECIMAL' } };
    service.crearFormula(dto).subscribe();
    const request = http.expectOne(`${base}/formulas`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual(dto);
    expect(request.request.headers.get(CONFIRMACION_CAMBIOS_HEADER)).toBe('1');
    request.flush({ success: true, datos: 9 });
  });

  it('reemplaza usos únicamente mediante el endpoint de borrador', () => {
    const usos = [{ versionFormularioId: 21, campoClave: 'riesgo_total', formulaVersionId: 9 }];
    service.reemplazarFormulaUsos(21, usos).subscribe();
    const request = http.expectOne(`${base}/formula-usos/versiones-formulario/21`);
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual({ usos });
    expect(request.request.headers.get(CONFIRMACION_CAMBIOS_HEADER)).toBe('1');
    request.flush({ success: true, mensaje: 'Usos actualizados' });
  });
});
