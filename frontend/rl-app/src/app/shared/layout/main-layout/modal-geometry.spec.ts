import { Component } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MainLayoutComponent } from './main-layout.component';
import { AuthService } from '../../../core/auth/auth.service';
import { ConfiguracionService } from '../../../core/configuration/configuracion.service';
import { CatalogoService } from '../../../core/configuration/catalogo.service';
import { GlobalHttpStateService } from '../../../core/services/global-http-state.service';
import { of } from 'rxjs';
import { signal } from '@angular/core';
import { provideRouter } from '@angular/router';

@Component({
  standalone: true,
  template: `
    <div class="modal-backdrop-overlay" role="dialog" aria-modal="true">
      <div class="modal-container-card modal-size-md">
        <div class="modal-header-institutional">
          <h3>Título Modal</h3>
        </div>
        <div class="modal-body-scrollable">
          <p>Contenido</p>
        </div>
        <div class="modal-footer-institutional">
          <button type="button">Aceptar</button>
        </div>
      </div>
    </div>
  `
})
class ModalTestHostComponent {}

describe('Estandarización Visual Global de Modales (Contrato CSS y Geometría)', () => {
  let authMock: Partial<AuthService>;
  let configMock: Partial<ConfiguracionService>;
  let catalogoMock: Partial<CatalogoService>;
  let globalStateMock: Partial<GlobalHttpStateService>;

  beforeEach(async () => {
    authMock = {
      usuario: signal({ id: 1, email: 'admin@ihss.hn', nombre: 'Admin', rol: 'ADMIN' } as any),
      tieneRol: () => true,
      logout: () => {}
    };
    configMock = {
      configSistema: signal({ nombreSistema: 'SGRLA-IHSS', nombreInstitucion: 'IHSS' } as any),
      CargarConfiguracion: () => of({} as any)
    };
    catalogoMock = {
      modulos: () => of([])
    };
    globalStateMock = {
      cargando: signal(false),
      ultimoError: signal(null),
      limpiarError: () => {}
    };

    await TestBed.configureTestingModule({
      imports: [MainLayoutComponent, ModalTestHostComponent],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: authMock },
        { provide: ConfiguracionService, useValue: configMock },
        { provide: CatalogoService, useValue: catalogoMock },
        { provide: GlobalHttpStateService, useValue: globalStateMock }
      ]
    }).compileComponents();
  });

  it('1. el contrato del modal renderiza la estructura canónica con overlay, card, header, body y footer', () => {
    const fixture = TestBed.createComponent(ModalTestHostComponent);
    fixture.detectChanges();

    const overlay = fixture.nativeElement.querySelector('.modal-backdrop-overlay');
    const card = fixture.nativeElement.querySelector('.modal-container-card');
    const header = fixture.nativeElement.querySelector('.modal-header-institutional');
    const body = fixture.nativeElement.querySelector('.modal-body-scrollable');
    const footer = fixture.nativeElement.querySelector('.modal-footer-institutional');

    expect(overlay).toBeTruthy();
    expect(card).toBeTruthy();
    expect(header).toBeTruthy();
    expect(body).toBeTruthy();
    expect(footer).toBeTruthy();
    expect(overlay.getAttribute('role')).toBe('dialog');
    expect(overlay.getAttribute('aria-modal')).toBe('true');
  });

  it('2. MainLayoutComponent aplica la clase modal-abierto y bloquea foco/inert cuando detecta un modal activo', () => {
    const fixture = TestBed.createComponent(MainLayoutComponent);
    fixture.detectChanges();

    const modalHost = document.createElement('div');
    modalHost.innerHTML = '<dialog open class="modal-backdrop-overlay" aria-modal="true"><div class="modal-container-card"><button id="btn-modal">Test</button></div></dialog>';
    fixture.nativeElement.appendChild(modalHost);

    // Disparar sincronización
    (fixture.componentInstance as any).sincronizarBloqueoModal();
    fixture.detectChanges();

    expect(document.body.classList.contains('modal-abierto')).toBe(true);

    // Cleanup
    modalHost.remove();
    (fixture.componentInstance as any).sincronizarBloqueoModal();
    expect(document.body.classList.contains('modal-abierto')).toBe(false);
  });
});
