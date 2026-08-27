import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { of } from 'rxjs';
import { MatricesRiesgosComponent } from './matrices-riesgos.component';
import { MatricesRiesgosService } from '../../data-access/matrices-riesgos.service';
import { AuthService } from '../../../../../core/auth/auth.service';
import { GlobalHttpStateService } from '../../../../../core/services/global-http-state.service';
import { FamiliaFormularioDto, VersionFormularioDto } from '../../models/matrices-riesgos.models';

describe('MatricesRiesgosComponent — UI-FAM.1 Gestor principal de Familias', () => {
  let component: MatricesRiesgosComponent;
  let fixture: ComponentFixture<MatricesRiesgosComponent>;
  let service: MatricesRiesgosService;

  const familias: FamiliaFormularioDto[] = [
    {
      famId: 1,
      famCodigo: 'EMPLEADOS',
      famNombre: 'Empleados',
      famDescripcion: 'Formulario institucional de empleados',
      famActivo: true,
      famFechaCreacion: '2026-08-19T00:00:00',
      totalVersiones: 2,
      tieneVersionVigente: true
    },
    {
      famId: 2,
      famCodigo: 'GTIC',
      famNombre: 'Matriz de Riesgos GTIC',
      famDescripcion: 'Matriz institucional del área GTIC',
      famActivo: true,
      famFechaCreacion: '2026-08-07T00:00:00',
      totalVersiones: 1,
      tieneVersionVigente: false
    },
    {
      famId: 3,
      famCodigo: 'MATRIZ_RIESGOS_LAFT',
      famNombre: 'Matriz de Riesgos LA/FT',
      famDescripcion: 'Matriz institucional LA/FT',
      famActivo: true,
      famFechaCreacion: '2026-08-07T00:00:00',
      totalVersiones: 5,
      tieneVersionVigente: true
    },
    {
      famId: 4,
      famCodigo: 'PRUEBA_FORMULARIO',
      famNombre: 'Prueba de Formulario',
      famDescripcion: 'Familia de pruebas',
      famActivo: false,
      famFechaCreacion: '2026-08-12T00:00:00',
      totalVersiones: 3,
      tieneVersionVigente: false
    }
  ];

  const versionVigente: VersionFormularioDto = {
    verId: 1,
    verFamiliaId: 1,
    verCodigo: 'EMPLEADOS',
    verVersion: 1,
    verJson: '{"secciones":[]}',
    verHash: 'hash-de-prueba',
    verEstado: 'PUBLISHED',
    verVigente: true,
    verFechaCreacion: '2026-08-19T00:00:00',
    verUsrCreacion: 1
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [MatricesRiesgosComponent, HttpClientTestingModule],
      providers: [
        MatricesRiesgosService,
        GlobalHttpStateService,
        {
          provide: AuthService,
          useValue: { tieneRol: () => true }
        }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(MatricesRiesgosComponent);
    component = fixture.componentInstance;
    service = TestBed.inject(MatricesRiesgosService);

    vi.spyOn(service, 'listarFamiliasFormulario').mockReturnValue(of(familias));
    vi.spyOn(service, 'listarHistorialVersionesFormulario').mockReturnValue(of([]));
    vi.spyOn(service, 'obtenerVersionVigenteFormulario').mockReturnValue(of(versionVigente));

    component.familias.set(familias);
    component.tab.set('plantillas');
    component.cargandoFamilias.set(false);
  });

  it('1. calcula KPI de familias exclusivamente desde los datos dinámicos cargados', () => {
    expect(component.totalFamilias()).toBe(4);
    expect(component.totalFamiliasActivas()).toBe(3);
    expect(component.totalFamiliasInactivas()).toBe(1);
    expect(component.totalVersionesFamilias()).toBe(11);
  });

  it('2. filtra por búsqueda de código o nombre sin hardcodear familias', () => {
    component.filtroBuscarFamilia.set('gtic');
    expect(component.familiasFiltradas().map(f => f.famCodigo)).toEqual(['GTIC']);

    component.filtroBuscarFamilia.set('prueba de formulario');
    expect(component.familiasFiltradas().map(f => f.famCodigo)).toEqual(['PRUEBA_FORMULARIO']);
  });

  it('3. combina filtro de estado y vigencia', () => {
    component.filtroEstadoFamilia.set('ACTIVAS');
    component.filtroVigenciaFamilia.set('VIGENTES');
    expect(component.familiasFiltradas().map(f => f.famCodigo)).toEqual(['EMPLEADOS', 'MATRIZ_RIESGOS_LAFT']);

    component.filtroVigenciaFamilia.set('SIN_VIGENTE');
    expect(component.familiasFiltradas().map(f => f.famCodigo)).toEqual(['GTIC']);
  });

  it('4. limpiar filtros restablece búsqueda, estado, vigencia y página', () => {
    component.filtroBuscarFamilia.set('MATRIZ');
    component.filtroEstadoFamilia.set('INACTIVAS');
    component.filtroVigenciaFamilia.set('SIN_VIGENTE');
    component.paginaFamilias.set(2);

    component.limpiarFiltrosFamilias();

    expect(component.filtroBuscarFamilia()).toBe('');
    expect(component.filtroEstadoFamilia()).toBe('TODAS');
    expect(component.filtroVigenciaFamilia()).toBe('TODAS');
    expect(component.paginaFamilias()).toBe(1);
  });

  it('5. pagina resultados dinámicos sin alterar la colección autoritativa', () => {
    const muchasFamilias = Array.from({ length: 12 }, (_, index): FamiliaFormularioDto => ({
      famId: index + 1,
      famCodigo: `FAM_${index + 1}`,
      famNombre: `Familia ${index + 1}`,
      famDescripcion: null,
      famActivo: true,
      famFechaCreacion: '2026-08-21T00:00:00',
      totalVersiones: index,
      tieneVersionVigente: index % 2 === 0
    }));
    component.familias.set(muchasFamilias);
    component.registrosPorPaginaFamilias.set(10);

    expect(component.totalPaginasFamilias()).toBe(2);
    expect(component.familiasPaginadas()).toHaveLength(10);

    component.cambiarPaginaFamilias(2);
    expect(component.familiasPaginadas()).toHaveLength(2);
    expect(component.familias()).toHaveLength(12);
  });

  it('6.1 renderiza Ãºnicamente el gestor principal de familias', () => {
    component.tab.set('plantillas');
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('[data-ui-fam="gestor-principal"]')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('[data-ui-fam="versiones-transicion"]')).toBeNull();
  });

  it('6.2 no renderiza la interfaz transitoria de versiones', () => {
    component.tab.set('plantillas');
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).not.toContain('Vista transitoria de versiones');
  });

  it('6.3 no renderiza el modal legacy de nuevo formulario', () => {
    component.tab.set('plantillas');
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).not.toContain('Nuevo Formulario de Matriz');
  });

  it('6.4 conserva el contexto de familia al seleccionarla en el flujo vigente', () => {
    component.seleccionarFamilia('GTIC');
    expect(component.familiaSeleccionada()).toBe('GTIC');
    expect(service.listarHistorialVersionesFormulario).toHaveBeenCalledWith('GTIC');
  });


  it('9. renderiza la pantalla principal con KPI y las ocho columnas aprobadas', () => {
    fixture.detectChanges();
    component.tab.set('plantillas');
    component.cargandoFamilias.set(false);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('[data-ui-fam="gestor-principal"]')).not.toBeNull();
    expect(compiled.textContent).toContain('Familias de Formularios');

    const headers = Array.from(compiled.querySelectorAll('[data-ui-fam-table="principal"] thead th'))
      .map(th => th.textContent?.trim());
    expect(headers).toEqual([
      'Código',
      'Nombre',
      'Descripción',
      'Estado',
      'Versiones',
      'Vigencia',
      'Fecha de creación',
      'Acciones'
    ]);
  });

  it('10. la acción Nueva familia abre el modal standalone UI-FAM.3 sin alterar el flujo de edición', () => {
    component.abrirModalCrearFamilia();
    fixture.detectChanges();

    expect(document.body.querySelector('[data-ui-fam-create="modal"]')).not.toBeNull();
    expect(component.modalFamiliaAbierto()).toBe(false);
    expect(component.modoEdicionFamilia()).toBe(false);

    component.cerrarModalCrearFamilia();
    expect(document.body.querySelector('[data-ui-fam-create="modal"]')).toBeNull();
  });
});
