import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { MatricesRiesgosComponent } from './matrices-riesgos.component';
import { MatricesRiesgosService } from '../../data-access/matrices-riesgos.service';
import { VersionFormularioDto } from '../../models/matrices-riesgos.models';
import { GlobalHttpStateService } from '../../../../../core/services/global-http-state.service';
import { AuthService } from '../../../../../core/auth/auth.service';

vi.mock('sweetalert2', () => ({
  default: {
    fire: vi.fn().mockResolvedValue({ isConfirmed: true })
  }
}));

describe('MatricesRiesgosComponent — ciclo de vida de versiones', () => {
  let component: MatricesRiesgosComponent;
  let fixture: ComponentFixture<MatricesRiesgosComponent>;
  let serviceMock: any;
  let globalStateMock: any;
  let authServiceMock: any;

  const versionDraftNoVigente: VersionFormularioDto = {
    verId: 101,
    verFamiliaId: 1,
    verCodigo: 'FORM_A',
    verVersion: 1,
    verEstado: 'DRAFT',
    verVigente: false,
    verJson: '{"secciones":[]}',
    verHash: 'hash1',
    verFechaCreacion: '2026-08-20',
    verUsrCreacion: 1
  };

  const versionPublishedVigente: VersionFormularioDto = {
    verId: 102,
    verFamiliaId: 1,
    verCodigo: 'FORM_A',
    verVersion: 2,
    verEstado: 'PUBLISHED',
    verVigente: true,
    verJson: '{"secciones":[]}',
    verHash: 'hash2',
    verFechaCreacion: '2026-08-20',
    verUsrCreacion: 1
  };

  const versionPublishedHistorica: VersionFormularioDto = {
    verId: 100,
    verFamiliaId: 1,
    verCodigo: 'FORM_A',
    verVersion: 0,
    verEstado: 'PUBLISHED',
    verVigente: false,
    verJson: '{"secciones":[]}',
    verHash: 'hash0',
    verFechaCreacion: '2026-08-20',
    verUsrCreacion: 1
  };

  beforeEach(async () => {
    vi.clearAllMocks();

    serviceMock = {
      obtenerVersionVigenteFormulario: vi.fn().mockReturnValue(of(versionPublishedVigente)),
      obtenerVersionVigentePorFamilia: vi.fn().mockReturnValue(of(versionPublishedVigente)),
      metodologiaVigente: vi.fn().mockReturnValue(of({ secciones: [] })),
      listarEvaluaciones: vi.fn().mockReturnValue(of({ items: [], totalRegistros: 0, totalPaginas: 0, pagina: 1 })),
      listarRiesgos: vi.fn().mockReturnValue(of([])),
      listarHistorialVersionesFormulario: vi.fn().mockReturnValue(of([versionDraftNoVigente, versionPublishedVigente, versionPublishedHistorica])),
      listarFamiliasFormulario: vi.fn().mockReturnValue(of([])),
      obtenerVersionFormulario: vi.fn().mockImplementation((id: number) => {
        if (id === 101) return of(versionDraftNoVigente);
        if (id === 102) return of(versionPublishedVigente);
        return of(versionPublishedHistorica);
      }),
      eliminarVersionFormulario: vi.fn().mockReturnValue(of({})),
      publicarVersionFormulario: vi.fn().mockReturnValue(of({})),
      cambiarVigenciaFormulario: vi.fn().mockReturnValue(of({})),
      clonarVersionFormulario: vi.fn().mockReturnValue(of(103))
    };

    globalStateMock = {
      limpiarError: vi.fn()
    };

    authServiceMock = {
      tieneRol: vi.fn().mockReturnValue(true)
    };

    await TestBed.configureTestingModule({
      imports: [MatricesRiesgosComponent],
      providers: [
        { provide: MatricesRiesgosService, useValue: serviceMock },
        { provide: GlobalHttpStateService, useValue: globalStateMock },
        { provide: AuthService, useValue: authServiceMock }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(MatricesRiesgosComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('impide DELETE de una versión PUBLISHED vigente', () => {
    component.eliminarVersionFormulario(versionPublishedVigente);

    expect(serviceMock.eliminarVersionFormulario).not.toHaveBeenCalled();
    expect(component.error()).toContain('historial');
  });

  it('impide DELETE de una versión PUBLISHED histórica', () => {
    component.eliminarVersionFormulario(versionPublishedHistorica);

    expect(serviceMock.eliminarVersionFormulario).not.toHaveBeenCalled();
    expect(component.error()).toContain('historial');
  });

  it('permite que un DRAFT no vigente alcance la confirmación de eliminación', () => {
    component.eliminarVersionFormulario(versionDraftNoVigente);

    expect(component.error()).toBeFalsy();
  });

  it('fuerza solo lectura al abrir una versión PUBLISHED', () => {
    component.abrirDefinicion(versionPublishedVigente, false);
    fixture.detectChanges();

    expect(component.soloLecturaDefinicion()).toBe(true);
    expect(component.versionEditando()?.verId).toBe(102);
  });

  it('permite edición solo para DRAFT no vigente', () => {
    component.abrirDefinicion(versionDraftNoVigente, false);
    fixture.detectChanges();

    expect(component.soloLecturaDefinicion()).toBe(false);
    expect(component.versionEditando()?.verId).toBe(101);
  });

  it('renderiza la matriz exacta de acciones según estado y vigencia', () => {
    component.tab.set('plantillas');
    component.versiones.set([versionDraftNoVigente, versionPublishedVigente, versionPublishedHistorica]);
    fixture.detectChanges();

    const panel = fixture.nativeElement.querySelector('#panel-plantillas') as HTMLElement;
    expect(panel).toBeTruthy();

    const botonesDeVersion = (versionId: number): string[] => {
      const tarjetas = Array.from(panel.querySelectorAll('article')) as HTMLElement[];
      const tarjeta = tarjetas.find(item => item.textContent?.includes(`ID #${versionId}`));
      expect(tarjeta).toBeTruthy();
      return Array.from(tarjeta!.querySelectorAll('button'))
        .map(boton => (boton.textContent ?? '').replace(/\s+/g, ' ').trim());
    };

    const draft = botonesDeVersion(101);
    expect(draft).toEqual(expect.arrayContaining(['Ver definición', 'Editar definición', 'Clonar', 'Publicar', 'Eliminar']));
    expect(draft).not.toContain('Activar');
    expect(draft).not.toContain('Desactivar');

    const vigente = botonesDeVersion(102);
    expect(vigente).toEqual(expect.arrayContaining(['Ver definición', 'Clonar', 'Desactivar']));
    expect(vigente).not.toContain('Editar definición');
    expect(vigente).not.toContain('Publicar');
    expect(vigente).not.toContain('Eliminar');
    expect(vigente).not.toContain('Activar');

    const historica = botonesDeVersion(100);
    expect(historica).toEqual(expect.arrayContaining(['Ver definición', 'Clonar', 'Activar']));
    expect(historica).not.toContain('Editar definición');
    expect(historica).not.toContain('Publicar');
    expect(historica).not.toContain('Eliminar');
    expect(historica).not.toContain('Desactivar');
  });

  it('clona una PUBLISHED como nuevo borrador y refresca el historial', () => {
    const refrescarHistorial = vi.spyOn(component, 'cargarVersiones');

    component.clonarVersion(versionPublishedVigente);

    expect(serviceMock.clonarVersionFormulario).toHaveBeenCalledWith(102);
    expect(component.mensaje()).toBe('Versión clonada como borrador exitosamente.');
    expect(refrescarHistorial).toHaveBeenCalledTimes(1);
  });

  it('conserva el estado de pantalla y reporta error si falla la clonación', () => {
    serviceMock.clonarVersionFormulario.mockReturnValueOnce(
      throwError(() => ({ error: { detail: 'No fue posible clonar la versión.' } }))
    );
    const refrescarHistorial = vi.spyOn(component, 'cargarVersiones');

    component.clonarVersion(versionPublishedVigente);

    expect(component.guardando()).toBe(false);
    expect(component.error()).toBe('No fue posible clonar la versión.');
    expect(refrescarHistorial).not.toHaveBeenCalled();
  });

  it('advierte sustitución, historial e inmutabilidad antes de publicar y refresca ambas fuentes', async () => {
    const Swal = await import('sweetalert2');
    const fireMock = vi.mocked(Swal.default.fire);
    fireMock.mockClear();
    const refrescarHistorial = vi.spyOn(component, 'cargarVersiones');
    const refrescarVigente = vi.spyOn(component, 'cargarVersionVigentePorFamilia');

    component.publicarVersion(versionDraftNoVigente);
    await new Promise(resolve => setTimeout(resolve, 50));

    expect(fireMock).toHaveBeenCalledTimes(1);
    const opciones = fireMock.mock.calls[0][0];
    const html = String(opciones.html ?? '');
    expect(html).toContain('versión <strong>vigente</strong>');
    expect(html).toContain('vigente anterior quedará como <strong>histórica</strong>');
    expect(html).toContain('bloqueada en <strong>solo lectura</strong>');
    expect(html).toContain('clonarla a un nuevo borrador');

    expect(serviceMock.publicarVersionFormulario).toHaveBeenCalledWith(101);
    expect(component.mensaje()).toBe('Versión publicada y establecida como vigente correctamente.');
    expect(refrescarHistorial).toHaveBeenCalledTimes(1);
    expect(refrescarVigente).toHaveBeenCalledWith(component.familiaSeleccionada());
  });

  it('reactiva una PUBLISHED histórica y refresca historial y vigente de la familia', async () => {
    const refrescarHistorial = vi.spyOn(component, 'cargarVersiones');
    const refrescarVigente = vi.spyOn(component, 'cargarVersionVigentePorFamilia');

    component.cambiarVigenciaVersion(versionPublishedHistorica, true);
    await new Promise(resolve => setTimeout(resolve, 50));

    expect(serviceMock.cambiarVigenciaFormulario).toHaveBeenCalledWith(100, true);
    expect(component.mensaje()).toBe('Versión establecida como activa exitosamente.');
    expect(refrescarHistorial).toHaveBeenCalledTimes(1);
    expect(refrescarVigente).toHaveBeenCalledWith(component.familiaSeleccionada());
  });

  it('desactiva la PUBLISHED vigente y refresca historial y vigente de la familia', async () => {
    const refrescarHistorial = vi.spyOn(component, 'cargarVersiones');
    const refrescarVigente = vi.spyOn(component, 'cargarVersionVigentePorFamilia');

    component.cambiarVigenciaVersion(versionPublishedVigente, false);
    await new Promise(resolve => setTimeout(resolve, 50));

    expect(serviceMock.cambiarVigenciaFormulario).toHaveBeenCalledWith(102, false);
    expect(component.mensaje()).toBe('Versión desactivada.');
    expect(refrescarHistorial).toHaveBeenCalledTimes(1);
    expect(refrescarVigente).toHaveBeenCalledWith(component.familiaSeleccionada());
  });
});
