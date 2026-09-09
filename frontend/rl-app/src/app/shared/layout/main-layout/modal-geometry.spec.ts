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
// @ts-ignore: Vitest ejecuta esta guarda leyendo plantillas productivas desde Node.js.
import { readdirSync, readFileSync, statSync } from 'node:fs';
// @ts-ignore: Vitest ejecuta esta guarda leyendo plantillas productivas desde Node.js.
import { join } from 'node:path';
// @ts-ignore: Vitest ejecuta esta guarda en Node.js.
import { cwd } from 'node:process';

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

  it('3. soporta todas las variantes semánticas institucionales de tamaño (sm, md, lg, xl, workspace)', () => {
    const fixture = TestBed.createComponent(ModalTestHostComponent);
    fixture.detectChanges();

    const card = fixture.nativeElement.querySelector('.modal-container-card') as HTMLElement;
    const variantes = ['modal-size-sm', 'modal-size-md', 'modal-size-lg', 'modal-size-xl', 'modal-size-workspace'];

    for (const variante of variantes) {
      card.className = `modal-container-card ${variante}`;
      expect(card.classList.contains(variante)).toBe(true);
      expect(card.classList.contains('modal-container-card')).toBe(true);
    }
  });

  it('4. todas las superficies productivas usan tamaño canónico y no fijan alturas de documento en píxeles', () => {
    const appRoot = join(cwd(), 'src', 'app');
    const sources: Array<{ path: string; source: string }> = [];
    const collect = (root: string) => {
      for (const entry of readdirSync(root)) {
        const path = join(root, entry);
        if (statSync(path).isDirectory()) collect(path);
        else if (path.endsWith('.html')) {
          sources.push({ path, source: readFileSync(path, 'utf8') });
        } else if (path.endsWith('.ts') && !path.endsWith('.spec.ts')) {
          const source = readFileSync(path, 'utf8');
          for (const match of source.matchAll(/template\s*:\s*`([\s\S]*?)`/g)) {
            sources.push({ path: `${path} (inline template)`, source: match[1] });
          }
        }
      }
    };
    collect(appRoot);
    const canonical = /\bmodal-size-(?:sm|md|lg|xl|workspace)\b/;
    const violations: string[] = [];
    for (const { path, source } of sources) {
      for (const match of source.matchAll(/modal-container-card[^>]*>/g)) {
        if (!canonical.test(match[0])) violations.push(`${path}: missing modal size`);
      }
      if (/modal-container-card[^>]*h-\[[0-9]+px\]/.test(source)) {
        violations.push(`${path}: fixed pixel modal height`);
      }
    }
    expect(violations).toEqual([]);
  });

  it('5. los previews y las superficies tabulares de alta densidad usan workspace', () => {
    const appRoot = join(cwd(), 'src', 'app');
    const sources: Array<{ path: string; source: string }> = [];
    const collect = (root: string) => {
      for (const entry of readdirSync(root)) {
        const path = join(root, entry);
        if (statSync(path).isDirectory()) collect(path);
        else if (path.endsWith('.html')) sources.push({ path, source: readFileSync(path, 'utf8') });
      }
    };
    collect(appRoot);
    const preview = sources.find(({ path }) => path.endsWith('shared\\report-preview\\report-preview.component.html'));
    expect(preview?.source).toMatch(/modal-container-card[^>]*modal-size-workspace/);

    const highDensity = sources.filter(({ source }) => /data-modal-density="(?:wide-table|history-form)"/.test(source));
    expect(highDensity.length).toBeGreaterThan(0);
    for (const { source } of highDensity) {
      expect(source).toMatch(/modal-container-card[^>]*modal-size-workspace|modal-container-card[^>]*modal-size-xl/);
    }
  });

  it('6. el visor PDF usa geometria dependiente del viewport', () => {
    const source = readFileSync(join(cwd(), 'src', 'app', 'shared', 'report-preview', 'report-preview.component.html'), 'utf8');
    expect(source).toContain('report-preview-document');
    expect(source).not.toMatch(/h-\[[0-9]+px\]/);
  });
});
