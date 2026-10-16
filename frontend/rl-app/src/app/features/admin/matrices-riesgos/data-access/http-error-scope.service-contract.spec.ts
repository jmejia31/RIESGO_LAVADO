import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { HTTP_ERROR_SCOPE } from '../../../../core/http/http-error-scope';
import { createHttpErrorScopeContext } from '../../../../core/http/http-error-scope';
import { CatalogoService } from '../../../../core/configuration/catalogo.service';
import { ConfiguracionService } from '../../../../core/configuration/configuracion.service';
import { ListasService } from '../../listas/data-access/listas.service';
import { CalculoConfiguracionService } from './calculo-configuracion.service';
import { MatricesRiesgosService } from './matrices-riesgos.service';

describe('HTTP error scope service contracts', () => {
  let http: HttpTestingController;
  let catalogos: CatalogoService;
  let configuracion: ConfiguracionService;
  let listas: ListasService;
  let calculo: CalculoConfiguracionService;
  let matrices: MatricesRiesgosService;
  const root = 'http://localhost:5043/api';

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    http = TestBed.inject(HttpTestingController);
    catalogos = TestBed.inject(CatalogoService);
    configuracion = TestBed.inject(ConfiguracionService);
    listas = TestBed.inject(ListasService);
    calculo = TestBed.inject(CalculoConfiguracionService);
    matrices = TestBed.inject(MatricesRiesgosService);
  });

  afterEach(() => {
    http.verify();
    TestBed.resetTestingModule();
  });

  it('preserves global configuration and catalog defaults while allowing local call-site scope', () => {
    configuracion.CargarConfiguracion().subscribe();
    const globalConfig = http.expectOne(`${root}/configuracion/sistema`);
    expect(globalConfig.request.context.get(HTTP_ERROR_SCOPE)).toBe('global');
    globalConfig.flush({ success: true, datos: { nombreInstitucion: 'IHSS', nombreSistema: 'SGRLA', timeoutSesion: 30 } });

    configuracion.CargarConfiguracion('local').subscribe();
    const localConfig = http.expectOne(`${root}/configuracion/sistema`);
    expect(localConfig.request.context.get(HTTP_ERROR_SCOPE)).toBe('local');
    localConfig.flush({ success: true, datos: { nombreInstitucion: 'IHSS', nombreSistema: 'SGRLA', timeoutSesion: 30 } });

    catalogos.modulos().subscribe();
    const globalModules = http.expectOne(`${root}/catalogos/modulos`);
    expect(globalModules.request.context.get(HTTP_ERROR_SCOPE)).toBe('global');
    globalModules.flush({ success: true, datos: [] });

    catalogos.modulos('local').subscribe();
    const localModules = http.expectOne(`${root}/catalogos/modulos`);
    expect(localModules.request.context.get(HTTP_ERROR_SCOPE)).toBe('local');
    localModules.flush({ success: true, datos: [] });
  });

  it('keeps optional login slides on local fallback scope', () => {
    configuracion.ObtenerSlides().subscribe();
    const request = http.expectOne(`${root}/configuracion/login`);
    expect(request.request.context.get(HTTP_ERROR_SCOPE)).toBe('local');
    request.flush({ success: true, datos: [] });
  });

  it('preserves global default and local override for shared list details', () => {
    listas.getDetalleNatural('0801').subscribe();
    const globalDetail = http.expectOne(`${root}/listas/naturales/0801/detalle`);
    expect(globalDetail.request.context.get(HTTP_ERROR_SCOPE)).toBe('global');
    globalDetail.flush({ success: true, datos: [] });

    listas.getDetalleNatural('0801', 'local').subscribe();
    const localDetail = http.expectOne(`${root}/listas/naturales/0801/detalle`);
    expect(localDetail.request.context.get(HTTP_ERROR_SCOPE)).toBe('local');
    localDetail.flush({ success: true, datos: [] });

    listas.getDetalleEmpleado('0801', 'local').subscribe();
    const localEmployeeDetail = http.expectOne(`${root}/listas/empleados/0801/detalle`);
    expect(localEmployeeDetail.request.context.get(HTTP_ERROR_SCOPE)).toBe('local');
    localEmployeeDetail.flush({ success: true, datos: [] });

    listas.getTiposListasCautela().subscribe();
    const globalTypes = http.expectOne(`${root}/listas/tipos-listas-cautela`);
    expect(globalTypes.request.context.get(HTTP_ERROR_SCOPE)).toBe('global');
    globalTypes.flush({ success: true, datos: [] });

    listas.getTiposListasCautela('local').subscribe();
    const localTypes = http.expectOne(`${root}/listas/tipos-listas-cautela`);
    expect(localTypes.request.context.get(HTTP_ERROR_SCOPE)).toBe('local');
    localTypes.flush({ success: true, datos: [] });
  });

  it('marks contextual follow-up writes local without changing their payload', () => {
    listas.registrarSeguimiento('0801', 'Revision mensual', []).subscribe();
    const request = http.expectOne(`${root}/listas/positivos/0801/seguimientos`);
    expect(request.request.method).toBe('POST');
    expect(request.request.context.get(HTTP_ERROR_SCOPE)).toBe('local');
    expect((request.request.body as FormData).get('motivoIngreso')).toBe('Revision mensual');
    request.flush({ success: true, mensaje: 'Registrado' });
  });

  it('keeps shared calculation reads global by default and honors local overrides', () => {
    calculo.listarFormulas(false).subscribe();
    const globalFormulas = http.expectOne(request => request.url === `${root}/matrices-riesgos/configuracion-calculo/formulas`);
    expect(globalFormulas.request.params.get('incluirInactivas')).toBe('false');
    expect(globalFormulas.request.context.get(HTTP_ERROR_SCOPE)).toBe('global');
    globalFormulas.flush({ success: true, datos: [] });

    calculo.listarFormulas(false, 'local').subscribe();
    const localFormulas = http.expectOne(request => request.url === `${root}/matrices-riesgos/configuracion-calculo/formulas`);
    expect(localFormulas.request.context.get(HTTP_ERROR_SCOPE)).toBe('local');
    localFormulas.flush({ success: true, datos: [] });

    calculo.listarFormulaVersiones(4).subscribe();
    const globalVersions = http.expectOne(`${root}/matrices-riesgos/configuracion-calculo/formulas/4/versiones`);
    expect(globalVersions.request.context.get(HTTP_ERROR_SCOPE)).toBe('global');
    globalVersions.flush({ success: true, datos: [] });

    calculo.listarFormulaVersiones(4, 'local').subscribe();
    const localVersions = http.expectOne(`${root}/matrices-riesgos/configuracion-calculo/formulas/4/versiones`);
    expect(localVersions.request.context.get(HTTP_ERROR_SCOPE)).toBe('local');
    localVersions.flush({ success: true, datos: [] });
  });

  it('keeps evaluation details local and family status scope explicit per consumer', () => {
    matrices.obtenerEvaluacion(42).subscribe();
    const evaluation = http.expectOne(`${root}/matrices-riesgos/evaluaciones/42`);
    expect(evaluation.request.context.get(HTTP_ERROR_SCOPE)).toBe('local');
    evaluation.flush({ success: true, datos: { evaId: 42 } });

    matrices.activarFamiliaFormulario(12).subscribe();
    const globalActivate = http.expectOne(`${root}/matrices-riesgos/familias/12/activar`);
    expect(globalActivate.request.context.get(HTTP_ERROR_SCOPE)).toBe('global');
    globalActivate.flush({ success: true, datos: true });

    matrices.activarFamiliaFormulario(12, 'local').subscribe();
    const localActivate = http.expectOne(`${root}/matrices-riesgos/familias/12/activar`);
    expect(localActivate.request.context.get(HTTP_ERROR_SCOPE)).toBe('local');
    localActivate.flush({ success: true, datos: true });
  });

  it('creates independent HttpContext instances for every request scope', () => {
    const first = createHttpErrorScopeContext('local');
    const second = createHttpErrorScopeContext('global');
    expect(first).not.toBe(second);
    expect(first.get(HTTP_ERROR_SCOPE)).toBe('local');
    expect(second.get(HTTP_ERROR_SCOPE)).toBe('global');
    first.set(HTTP_ERROR_SCOPE, 'global');
    expect(second.get(HTTP_ERROR_SCOPE)).toBe('global');
  });
});
