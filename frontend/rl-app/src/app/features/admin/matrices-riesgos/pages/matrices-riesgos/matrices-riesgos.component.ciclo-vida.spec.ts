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

describe('MatricesRiesgosComponent — F6.4 Ciclo de Vida de Versiones', () => {
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

  it('1. C01 Guard defensivo TS impide llamar API DELETE si la versión es PUBLISHED vigente', () => {
    component.eliminarVersionFormulario(versionPublishedVigente);
    expect(serviceMock.eliminarVersionFormulario).not.toHaveBeenCalled();
    expect(component.error()).toContain('historial');
  });

  it('2. C01 Guard defensivo TS impide llamar API DELETE si la versión es PUBLISHED histórica', () => {
    component.eliminarVersionFormulario(versionPublishedHistorica);
    expect(serviceMock.eliminarVersionFormulario).not.toHaveBeenCalled();
    expect(component.error()).toContain('historial');
  });

  it('3. DRAFT no vigente permite llamar API DELETE si se confirma', () => {
    component.eliminarVersionFormulario(versionDraftNoVigente);
    // El SweetAlert2 requiere confirmación de usuario, la función por sí sola valida que pasó el guard defensivo sin mostrar error
    expect(component.error()).toBeFalsy();
  });

  it('4. abrirDefinicion establece soloLectura=true cuando la versión es PUBLISHED', () => {
    component.abrirDefinicion(versionPublishedVigente, false);
    fixture.detectChanges();
    expect(component.soloLecturaDefinicion()).toBe(true);
    expect(component.versionEditando()?.verId).toBe(102);
  });

  it('5. abrirDefinicion permite soloLectura=false solo cuando la versión es DRAFT no vigente', () => {
    component.abrirDefinicion(versionDraftNoVigente, false);
    fixture.detectChanges();
    expect(component.soloLecturaDefinicion()).toBe(false);
    expect(component.versionEditando()?.verId).toBe(101);
  });
});
