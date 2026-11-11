import { HttpErrorResponse } from '@angular/common/http';
import { GlobalHttpStateService } from './global-http-state.service';

describe('GlobalHttpStateService', () => {
  it('mantiene el contador tecnico sin mostrar feedback global para refrescos silenciosos', () => {
    const service = new GlobalHttpStateService();

    service.iniciarPeticion('SILENT_REFRESH');

    expect(service.cargando()).toBe(true);
    expect(service.indicadorVisible()).toBe(false);

    service.finalizarPeticion('SILENT_REFRESH');

    expect(service.cargando()).toBe(false);
    expect(service.indicadorVisible()).toBe(false);
  });

  it('expone feedback global solo para operaciones visibles y lo libera al finalizar', () => {
    const service = new GlobalHttpStateService();

    service.iniciarPeticion('USER_ACTION');
    service.iniciarPeticion('CRITICAL_OPERATION');
    expect(service.indicadorVisible()).toBe(true);

    service.finalizarPeticion('USER_ACTION');
    expect(service.indicadorVisible()).toBe(true);

    service.finalizarPeticion('CRITICAL_OPERATION');
    expect(service.indicadorVisible()).toBe(false);
    expect(service.cargando()).toBe(false);
  });

  it('conserva los errores globales independientemente de la presentación de carga', () => {
    const service = new GlobalHttpStateService();
    service.notificarError(new HttpErrorResponse({
      status: 500,
      error: { detail: 'Fallo controlado' }
    }));

    expect(service.ultimoError()).toBe('Fallo controlado');
  });
});
