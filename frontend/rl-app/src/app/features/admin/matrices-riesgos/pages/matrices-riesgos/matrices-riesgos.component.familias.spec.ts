import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { MatricesRiesgosComponent } from './matrices-riesgos.component';
import { MatricesRiesgosService } from '../../data-access/matrices-riesgos.service';
import { AuthService } from '../../../../../core/auth/auth.service';
import { GlobalHttpStateService } from '../../../../../core/services/global-http-state.service';
import { FamiliaFormularioDto } from '../../models/matrices-riesgos.models';
import { Subject, of, throwError } from 'rxjs';

describe('MatricesRiesgosComponent — F6.5.FAM.2 + UI-FAM.QA Gestor de Familias', () => {
  let component: MatricesRiesgosComponent;
  let fixture: ComponentFixture<MatricesRiesgosComponent>;
  let service: MatricesRiesgosService;

  const authMock = {
    tieneRol: vi.fn(() => true)
  };

  const mockFamilias: FamiliaFormularioDto[] = [
    {
      famId: 1,
      famCodigo: 'EMPLEADOS',
      famNombre: 'Familia Empleados',
      famDescripcion: 'Descripción de prueba para detalle',
      famActivo: true,
      famFechaCreacion: '2026-01-15T00:00:00',
      totalVersiones: 2,
      tieneVersionVigente: true
    },
    {
      famId: 2,
      famCodigo: 'QA_TEST',
      famNombre: 'Familia QA Test',
      famDescripcion: 'Familia sin versiones',
      famActivo: true,
      famFechaCreacion: '2026-02-20T00:00:00',
      totalVersiones: 0,
      tieneVersionVigente: false
    },
    {
      famId: 3,
      famCodigo: 'INACTIVA_FAM',
      famNombre: 'Familia Inactiva',
      famDescripcion: 'Familia inactiva de prueba',
      famActivo: false,
      famFechaCreacion: '2026-03-10T00:00:00',
      totalVersiones: 0,
      tieneVersionVigente: false
    }
  ];

  beforeEach(async () => {
    authMock.tieneRol.mockReturnValue(true);

    await TestBed.configureTestingModule({
      imports: [MatricesRiesgosComponent, HttpClientTestingModule],
      providers: [
        MatricesRiesgosService,
        GlobalHttpStateService,
        {
          provide: AuthService,
          useValue: authMock
        }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(MatricesRiesgosComponent);
    component = fixture.componentInstance;
    service = TestBed.inject(MatricesRiesgosService);

    vi.spyOn(service, 'listarFamiliasFormulario').mockReturnValue(of(mockFamilias));
    vi.spyOn(service, 'listarHistorialVersionesFormulario').mockReturnValue(of([]));
    vi.spyOn(service, 'obtenerVersionVigenteFormulario').mockReturnValue(of({
      verId: 1,
      verFamiliaId: 1,
      verCodigo: 'EMPLEADOS',
      verVersion: 1,
      verJson: '{}',
      verHash: 'hash',
      verEstado: 'PUBLISHED',
      verVigente: true,
      verFechaCreacion: '2026-01-01',
      verUsrCreacion: 1
    }));

    component.familias.set(mockFamilias);
    component.abrirModalGestorFamilias();
    fixture.detectChanges();
  });

  afterEach(() => {
    component.cerrarModalVerFamilia();
  });

  const mostrarGestorPrincipal = (): void => {
    component.cerrarModalGestorFamilias();
    component.tab.set('plantillas');
    component.mostrandoVersionesFamilia.set(false);
    fixture.detectChanges();
  };

  it('1. Renderiza el botón Administrar Familias y abre el modal gestor', () => {
    expect(component.modalGestorFamiliasAbierto()).toBe(true);
    const compiled = fixture.nativeElement as HTMLElement;
    const modalTitle = compiled.querySelector('#titulo-modal-gestor-familias');
    expect(modalTitle?.textContent).toContain('Administrar Familias de Formularios');
  });

  it('2. Verifica las columnas finales en la tabla: Código, Nombre, Estado, Versiones, Vigente, Fecha de Creación y Acciones', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    const headers = Array.from(compiled.querySelectorAll('dialog thead th')).map(th => th.textContent?.trim());
    expect(headers).toEqual(['Código', 'Nombre', 'Estado', 'Versiones', 'Vigente', 'Fecha de Creación', 'Acciones']);
    expect(headers).not.toContain('Descripción');
  });

  it('3. Renderiza botones iconográficos compactos con aria-label accesibles', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    const firstRowButtons = compiled.querySelectorAll('tbody tr:first-child button');
    const ariaLabels = Array.from(firstRowButtons).map(b => b.getAttribute('aria-label'));

    expect(ariaLabels).toContain('Ver detalle');
    expect(ariaLabels).toContain('Editar');
    expect(ariaLabels).toContain('Desactivar');
    expect(ariaLabels).toContain('Ver versiones');
  });

  it('4. Muestra la fecha formateada en español dd/MM/yyyy', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    const firstRowDate = compiled.querySelector('tbody tr:first-child td:nth-child(6)')?.textContent?.trim();
    expect(firstRowDate).toBe('15/01/2026');
  });

  it('5. Muestra el botón Eliminar únicamente para familias con totalVersiones === 0', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    const firstRowDeleteBtn = compiled.querySelector('tbody tr:first-child button[aria-label="Eliminar"]');
    const secondRowDeleteBtn = compiled.querySelector('tbody tr:nth-child(2) button[aria-label="Eliminar"]');

    expect(firstRowDeleteBtn).toBeNull();
    expect(secondRowDeleteBtn).not.toBeNull();
  });

  it('6. Búsqueda por texto, filtro por estado y botón Limpiar', () => {
    component.filtroBuscarFamilia.set('QA_TEST');
    fixture.detectChanges();
    expect(component.familiasFiltradas().length).toBe(1);

    component.limpiarFiltrosFamilias();
    fixture.detectChanges();
    expect(component.filtroBuscarFamilia()).toBe('');
    expect(component.filtroEstadoFamilia()).toBe('TODAS');
    expect(component.familiasFiltradas().length).toBe(3);
  });

  it('7. Ver detalle usa el nuevo modal UI-FAM.2 y consulta la familia por su famId', () => {
    vi.spyOn(service, 'obtenerFamiliaFormularioPorId').mockReturnValue(of(mockFamilias[0]));

    component.abrirModalVerFamilia(mockFamilias[0]);

    expect(component.detalleFamiliaDinamicoAbierto()).toBe(true);
    expect(component.modalVerFamiliaAbierto()).toBeNull();
    expect(service.obtenerFamiliaFormularioPorId).toHaveBeenCalledWith(1);
    expect(document.body.querySelector('[data-ui-fam-detail="modal"]')).not.toBeNull();
    expect(document.body.textContent).toContain('Descripción de prueba para detalle');

    component.cerrarModalVerFamilia();
    expect(component.detalleFamiliaDinamicoAbierto()).toBe(false);
    expect(document.body.querySelector('[data-ui-fam-detail="modal"]')).toBeNull();
  });

  it('8. Selección de Ver versiones selecciona la familia y cierra el gestor', () => {
    component.seleccionarFamiliaDesdeGestor('QA_TEST');
    expect(component.familiaSeleccionada()).toBe('QA_TEST');
    expect(component.modalGestorFamiliasAbierto()).toBe(false);
  });

  it('9. Confirmar activación y desactivación llaman al servicio Angular', () => {
    vi.spyOn(service, 'desactivarFamiliaFormulario').mockReturnValue(of(true));
    vi.spyOn(service, 'activarFamiliaFormulario').mockReturnValue(of(true));

    component.desactivarFamilia(mockFamilias[0]);
    expect(service.desactivarFamiliaFormulario).toHaveBeenCalledWith(1);

    component.activarFamilia(mockFamilias[2]);
    expect(service.activarFamiliaFormulario).toHaveBeenCalledWith(3);
  });

  it('10. Manejo de error cuando backend rechaza desactivar o eliminar', () => {
    vi.spyOn(service, 'desactivarFamiliaFormulario').mockReturnValue(throwError(() => ({
      error: { detail: 'La familia posee versiones vigentes.' }
    })));

    component.desactivarFamilia(mockFamilias[0]);
    expect(component.error()).toBe('La familia posee versiones vigentes.');
  });

  it('11. UI-FAM.QA carga familias desde backend y finaliza el estado loading', () => {
    const respuesta = new Subject<FamiliaFormularioDto[]>();
    vi.mocked(service.listarFamiliasFormulario).mockReturnValueOnce(respuesta);
    component.familias.set([]);

    component.cargarFamilias();
    expect(component.cargandoFamilias()).toBe(true);

    respuesta.next(mockFamilias);
    respuesta.complete();

    expect(service.listarFamiliasFormulario).toHaveBeenCalled();
    expect(component.familias()).toEqual(mockFamilias);
    expect(component.cargandoFamilias()).toBe(false);
    expect(component.errorFamilias()).toBeNull();
  });

  it('12. UI-FAM.QA calcula correctamente los cuatro KPI del gestor', () => {
    mostrarGestorPrincipal();

    expect(component.totalFamilias()).toBe(3);
    expect(component.totalFamiliasActivas()).toBe(2);
    expect(component.totalFamiliasInactivas()).toBe(1);
    expect(component.totalVersionesFamilias()).toBe(2);

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('[data-ui-fam-kpi="total"]')?.textContent?.trim()).toBe('3');
    expect(compiled.querySelector('[data-ui-fam-kpi="activas"]')?.textContent?.trim()).toBe('2');
    expect(compiled.querySelector('[data-ui-fam-kpi="inactivas"]')?.textContent?.trim()).toBe('1');
    expect(compiled.querySelector('[data-ui-fam-kpi="versiones"]')?.textContent?.trim()).toBe('2');
  });

  it('13. UI-FAM.QA filtra familias activas', () => {
    component.filtroEstadoFamilia.set('ACTIVAS');
    expect(component.familiasFiltradas().map(f => f.famId)).toEqual([1, 2]);
  });

  it('14. UI-FAM.QA filtra familias inactivas', () => {
    component.filtroEstadoFamilia.set('INACTIVAS');
    expect(component.familiasFiltradas().map(f => f.famId)).toEqual([3]);
  });

  it('15. UI-FAM.QA filtra familias con versión vigente', () => {
    component.filtroVigenciaFamilia.set('VIGENTES');
    expect(component.familiasFiltradas().map(f => f.famId)).toEqual([1]);
  });

  it('16. UI-FAM.QA filtra familias sin versión vigente', () => {
    component.filtroVigenciaFamilia.set('SIN_VIGENTE');
    expect(component.familiasFiltradas().map(f => f.famId)).toEqual([2, 3]);
  });

  it('17. UI-FAM.QA muestra estado vacío cuando búsqueda y filtros no tienen coincidencias', () => {
    mostrarGestorPrincipal();
    component.filtroBuscarFamilia.set('NO_EXISTE_999');
    fixture.detectChanges();

    expect(component.familiasFiltradas()).toEqual([]);
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('No hay familias que coincidan con los filtros.');
  });

  it('18. UI-FAM.QA oculta acciones administrativas cuando el usuario no está autorizado', () => {
    authMock.tieneRol.mockReturnValue(false);
    const componenteSinPermiso = TestBed.runInInjectionContext(() => new MatricesRiesgosComponent());
    expect(componenteSinPermiso.esAdministrador()).toBe(false);

    Object.defineProperty(component, 'esAdministrador', {
      configurable: true,
      value: () => false
    });
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('button[aria-label="Editar"]')).toBeNull();
    expect(compiled.querySelector('button[aria-label="Eliminar"]')).toBeNull();
    expect(compiled.textContent).not.toContain('Nueva familia');
  });

  it('19. UI-FAM.QA conserva contrato responsive para desktop y resolución reducida', () => {
    mostrarGestorPrincipal();
    const compiled = fixture.nativeElement as HTMLElement;
    const container = compiled.querySelector('[data-ui-fam="gestor-principal"]');
    const tablaResponsive = container?.querySelector('.overflow-x-auto');
    const indicadores = container?.querySelector('[aria-label="Indicadores de familias de formularios"]');

    expect(container).not.toBeNull();
    expect(tablaResponsive).not.toBeNull();
    expect(indicadores?.className).toContain('grid-cols-1');
    expect(indicadores?.className).toContain('sm:grid-cols-2');
    expect(indicadores?.className).toContain('xl:grid-cols-4');
  });
});
