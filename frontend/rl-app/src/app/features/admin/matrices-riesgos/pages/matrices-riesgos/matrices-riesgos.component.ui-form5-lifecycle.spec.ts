import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { MatricesRiesgosComponent } from './matrices-riesgos.component';
import { MatricesRiesgosService } from '../../data-access/matrices-riesgos.service';
import { EstadoFormulario, VersionFormularioDto } from '../../models/matrices-riesgos.models';
import { AuthService } from '../../../../../core/auth/auth.service';
import { CalculoConfiguracionService } from '../../data-access/calculo-configuracion.service';
import { describe, it, expect, beforeEach, vi } from 'vitest';

let mockSwalResult = { isConfirmed: true };

vi.mock('sweetalert2', () => ({
  default: {
    fire: vi.fn().mockImplementation(() => Promise.resolve(mockSwalResult))
  }
}));

describe('MatricesRiesgosComponent — UI-FORM.5 Estados y Ciclo de Edición', () => {
  let fixture: ComponentFixture<MatricesRiesgosComponent>;
  let component: MatricesRiesgosComponent;
  let authServiceMock: { tieneRol: ReturnType<typeof vi.fn> };
  let service: {
    obtenerVersionFormulario: ReturnType<typeof vi.fn>;
    obtenerVersionVigenteFormulario: ReturnType<typeof vi.fn>;
    listarHistorialVersionesFormulario: ReturnType<typeof vi.fn>;
    listarFamiliasFormulario: ReturnType<typeof vi.fn>;
    actualizarBorradorFormulario: ReturnType<typeof vi.fn>;
    publicarVersionFormulario: ReturnType<typeof vi.fn>;
    cambiarVigenciaFormulario: ReturnType<typeof vi.fn>;
    clonarVersionFormulario: ReturnType<typeof vi.fn>;
    eliminarVersionFormulario: ReturnType<typeof vi.fn>;
    crearBorradorFormulario: ReturnType<typeof vi.fn>;
    metodologiaVigente: ReturnType<typeof vi.fn>;
    metodologiaPorVersion: ReturnType<typeof vi.fn>;
    obtenerEvaluacion: ReturnType<typeof vi.fn>;
    listarRiesgos: ReturnType<typeof vi.fn>;
    listarEvaluaciones: ReturnType<typeof vi.fn>;
    obtenerConsolidado: ReturnType<typeof vi.fn>;
  };

  const baseVersion: VersionFormularioDto = {
    verId: 200,
    verFamiliaId: 1,
    verCodigo: 'FORM_TEST_CICLO',
    verVersion: 1,
    verJson: JSON.stringify({
      codigoFormulario: 'FORM_TEST_CICLO',
      nombreFormulario: 'Formulario Ciclo Test',
      secciones: [
        {
          clave: 'sec_1',
          titulo: 'Sección 1',
          orden: 1,
          campos: [{ clave: 'c1', etiqueta: 'Campo 1', tipo: 'texto', obligatorio: false, soloLectura: false }]
        }
      ],
      catalogos: []
    }),
    verHash: 'hash_test_200',
    verEstado: 'DRAFT',
    verVigente: false,
    verFechaCreacion: '2026-08-25T10:00:00Z',
    verUsrCreacion: 1
  };

  beforeEach(async () => {
    mockSwalResult = { isConfirmed: true };
    authServiceMock = {
      tieneRol: vi.fn().mockReturnValue(true)
    };

    service = {
      obtenerVersionFormulario: vi.fn().mockReturnValue(of(baseVersion)),
      obtenerVersionVigenteFormulario: vi.fn().mockReturnValue(of(baseVersion)),
      listarHistorialVersionesFormulario: vi.fn().mockReturnValue(of([baseVersion])),
      listarFamiliasFormulario: vi.fn().mockReturnValue(of([{ famId: 1, famCodigo: 'MATRIZ_LAFT', famNombre: 'Matriz LAFT', famActivo: true, totalVersiones: 1, tieneVersionVigente: false }])),
      actualizarBorradorFormulario: vi.fn().mockReturnValue(of({ success: true })),
      publicarVersionFormulario: vi.fn().mockReturnValue(of({ success: true })),
      cambiarVigenciaFormulario: vi.fn().mockReturnValue(of({ success: true })),
      clonarVersionFormulario: vi.fn().mockReturnValue(of({ verId: 201 })),
      eliminarVersionFormulario: vi.fn().mockReturnValue(of({ success: true })),
      crearBorradorFormulario: vi.fn().mockReturnValue(of(202)),
      metodologiaVigente: vi.fn().mockReturnValue(of(null)),
      metodologiaPorVersion: vi.fn().mockReturnValue(of(null)),
      obtenerEvaluacion: vi.fn().mockReturnValue(of(null)),
      listarRiesgos: vi.fn().mockReturnValue(of([])),
      listarEvaluaciones: vi.fn().mockReturnValue(of([])),
      obtenerConsolidado: vi.fn().mockReturnValue(of(null))
    };

    await TestBed.configureTestingModule({
      imports: [MatricesRiesgosComponent],
      providers: [
        { provide: AuthService, useValue: authServiceMock },
        { provide: MatricesRiesgosService, useValue: service },
        { provide: CalculoConfiguracionService, useValue: { listarFormulas: vi.fn().mockReturnValue(of([])), reemplazarFormulaUsos: vi.fn().mockReturnValue(of({ success: true })) } }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(MatricesRiesgosComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  describe('1. Matriz de Estados y Solo Lectura Autoritativo (Admin)', () => {
    const matrizCasos: Array<{ estado: EstadoFormulario; vigente: boolean; soloLecturaEsperado: boolean; descripcion: string }> = [
      { estado: 'DRAFT', vigente: false, soloLecturaEsperado: false, descripcion: 'DRAFT no vigente -> editable' },
      { estado: 'DRAFT', vigente: true, soloLecturaEsperado: true, descripcion: 'DRAFT vigente -> solo lectura' },
      { estado: 'IN_REVIEW', vigente: false, soloLecturaEsperado: true, descripcion: 'IN_REVIEW -> solo lectura' },
      { estado: 'APPROVED', vigente: false, soloLecturaEsperado: true, descripcion: 'APPROVED -> solo lectura' },
      { estado: 'PUBLISHED', vigente: false, soloLecturaEsperado: true, descripcion: 'PUBLISHED no vigente -> solo lectura' },
      { estado: 'PUBLISHED', vigente: true, soloLecturaEsperado: true, descripcion: 'PUBLISHED vigente -> solo lectura' },
      { estado: 'RETIRED', vigente: false, soloLecturaEsperado: true, descripcion: 'RETIRED -> solo lectura' },
      { estado: 'ARCHIVED', vigente: false, soloLecturaEsperado: true, descripcion: 'ARCHIVED -> solo lectura' }
    ];

    matrizCasos.forEach(({ estado, vigente, soloLecturaEsperado, descripcion }) => {
      it(descripcion, () => {
        const versionTest: VersionFormularioDto = {
          ...baseVersion,
          verEstado: estado,
          verVigente: vigente
        };
        service.obtenerVersionFormulario.mockReturnValue(of(versionTest));

        component.abrirDefinicion(versionTest);

        expect(service.obtenerVersionFormulario).toHaveBeenCalledWith(200);
        expect(component.versionEditando()?.verEstado).toBe(estado);
        expect(component.versionEditando()?.verVigente).toBe(vigente);
        expect(component.soloLecturaDefinicion()).toBe(soloLecturaEsperado);
      });
    });
  });

  describe('2. Permisos y No-Admin (Defensa en Profundidad)', () => {
    it('usuario no-admin abre DRAFT en modo solo lectura obligatoria', () => {
      authServiceMock.tieneRol.mockReturnValue(false);
      service.obtenerVersionFormulario.mockReturnValue(of(baseVersion));

      component.abrirDefinicion(baseVersion, false);

      expect(component.esAdministrador()).toBe(false);
      expect(component.soloLecturaDefinicion()).toBe(true);
    });

    it('usuario no-admin no puede ejecutar mutaciones administrativas (guardas defensivas)', () => {
      authServiceMock.tieneRol.mockReturnValue(false);
      component.versionEditando.set(baseVersion);
      component.soloLecturaDefinicion.set(false);

      component.guardarDefinicion();
      expect(service.actualizarBorradorFormulario).not.toHaveBeenCalled();

      component.publicarVersion(baseVersion);
      expect(service.publicarVersionFormulario).not.toHaveBeenCalled();

      component.clonarVersion(baseVersion);
      expect(service.clonarVersionFormulario).not.toHaveBeenCalled();

      component.cambiarVigenciaVersion(baseVersion, true);
      expect(service.cambiarVigenciaFormulario).not.toHaveBeenCalled();

      component.eliminarVersionFormulario(baseVersion);
      expect(service.eliminarVersionFormulario).not.toHaveBeenCalled();
    });

    it('retira el panel legacy y conserva el panel principal de familias', () => {
      component.tab.set('plantillas');
      fixture.detectChanges();
      expect(fixture.nativeElement.querySelector('[data-ui-fam="gestor-principal"]')).toBeTruthy();
      expect(fixture.nativeElement.querySelector('[data-ui-fam="versiones-transicion"]')).toBeNull();
      expect(fixture.nativeElement.textContent).not.toContain('Nuevo Formulario de Matriz');
    });
  });

  describe('3. Guardar Borrador (Persistencia y Verificación Semántica)', () => {
    it('guarda definición llamando a actualizarBorradorFormulario, verifica semánticamente y limpia operacionBuilderEnCurso', () => {
      service.obtenerVersionFormulario.mockReturnValue(of(baseVersion));
      component.abrirDefinicion(baseVersion);

      const jsonModificado = JSON.stringify({
        codigoFormulario: 'FORM_TEST_CICLO',
        nombreFormulario: 'Formulario Ciclo Test Modificado',
        secciones: [],
        catalogos: []
      });
      component.definicionTecnica = jsonModificado;

      service.actualizarBorradorFormulario.mockReturnValue(of({ success: true }));
      service.obtenerVersionFormulario.mockReturnValue(of({
        ...baseVersion,
        verJson: jsonModificado
      }));

      component.guardarDefinicion();

      expect(service.actualizarBorradorFormulario).toHaveBeenCalledWith(200, jsonModificado);
      expect(service.publicarVersionFormulario).not.toHaveBeenCalled();
      expect(component.mensaje()).toContain('verificada correctamente');
      expect(component.versionEditando()).toBeNull();
      expect(component.guardando()).toBe(false);
      expect(component.operacionBuilderEnCurso()).toBeNull();
    });

    it('bloquea cierre del builder si falla la verificación semántica post-save y resetea operacionBuilderEnCurso', () => {
      service.obtenerVersionFormulario.mockReturnValue(of(baseVersion));
      component.abrirDefinicion(baseVersion);

      const jsonModificado = JSON.stringify({
        codigoFormulario: 'FORM_TEST_CICLO',
        secciones: [{ clave: 's1', titulo: 'Sección 1', orden: 1, campos: [] }]
      });
      component.definicionTecnica = jsonModificado;

      service.actualizarBorradorFormulario.mockReturnValue(of({ success: true }));
      service.obtenerVersionFormulario.mockReturnValue(of({
        ...baseVersion,
        verJson: JSON.stringify({ codigoFormulario: 'OTRO', secciones: [] })
      }));

      component.guardarDefinicion();

      expect(component.versionEditando()).not.toBeNull();
      expect(component.error()).toContain('no coincide semánticamente');
      expect(component.guardando()).toBe(false);
      expect(component.operacionBuilderEnCurso()).toBeNull();
    });
  });

  describe('4. Publicación y Reconciliación Autoritativa', () => {
    it('publica versión y refresca el builder a modo solo lectura si la versión estaba abierta', async () => {
      service.obtenerVersionFormulario.mockReturnValue(of(baseVersion));
      component.abrirDefinicion(baseVersion);
      expect(component.soloLecturaDefinicion()).toBe(false);

      const versionPublicada: VersionFormularioDto = {
        ...baseVersion,
        verEstado: 'PUBLISHED',
        verVigente: true
      };

      service.publicarVersionFormulario.mockReturnValue(of({ success: true }));
      service.obtenerVersionFormulario.mockReturnValue(of(versionPublicada));

      component.publicarVersion(baseVersion);

      await new Promise(resolve => setTimeout(resolve, 50));

      expect(service.publicarVersionFormulario).toHaveBeenCalledWith(200);
      expect(service.obtenerVersionFormulario).toHaveBeenCalledWith(200);
      expect(component.versionEditando()?.verEstado).toBe('PUBLISHED');
      expect(component.versionEditando()?.verVigente).toBe(true);
      expect(component.soloLecturaDefinicion()).toBe(true);
      expect(component.guardando()).toBe(false);
      expect(component.operacionBuilderEnCurso()).toBeNull();
    });

    it('si el usuario cancela el diálogo de publicación, operacionBuilderEnCurso sigue en null y no hay requests', async () => {
      mockSwalResult = { isConfirmed: false };

      component.publicarVersion(baseVersion);
      await new Promise(resolve => setTimeout(resolve, 50));

      expect(service.publicarVersionFormulario).not.toHaveBeenCalled();
      expect(component.guardando()).toBe(false);
      expect(component.operacionBuilderEnCurso()).toBeNull();
    });

    it('maneja error de publicación mostrando el mensaje del backend y limpiando operacion', async () => {
      service.publicarVersionFormulario.mockReturnValue(throwError(() => ({
        status: 400,
        error: { mensaje: 'La versión no cumple con los requisitos para publicación.' }
      })));

      component.publicarVersion(baseVersion);
      await new Promise(resolve => setTimeout(resolve, 50));

      expect(component.error()).toContain('La versión no cumple con los requisitos');
      expect(component.guardando()).toBe(false);
      expect(component.operacionBuilderEnCurso()).toBeNull();
    });

    it('aplica fail-safe y cierra versión abierta si el re-fetch post-publicación falla', async () => {
      service.obtenerVersionFormulario.mockReturnValue(of(baseVersion));
      component.abrirDefinicion(baseVersion);

      service.publicarVersionFormulario.mockReturnValue(of({ success: true }));
      service.obtenerVersionFormulario.mockReturnValue(throwError(() => ({
        status: 500,
        error: { mensaje: 'Error al recuperar versión fresca' }
      })));

      component.publicarVersion(baseVersion);
      await new Promise(resolve => setTimeout(resolve, 50));

      expect(component.versionEditando()).toBeNull();
      expect(component.error()).toContain('Error al recuperar versión fresca');
      expect(component.guardando()).toBe(false);
      expect(component.operacionBuilderEnCurso()).toBeNull();
    });
  });
});
