import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { MatricesRiesgosComponent } from './matrices-riesgos.component';
import { MatricesRiesgosService } from '../../data-access/matrices-riesgos.service';
import { AuthService } from '../../../../../core/auth/auth.service';
import { GlobalHttpStateService } from '../../../../../core/services/global-http-state.service';
import { FamiliaFormularioDto } from '../../models/matrices-riesgos.models';
import { of, throwError } from 'rxjs';

describe('MatricesRiesgosComponent — F6.5.FAM.2 Refinamiento Gestor de Familias', () => {
  let component: MatricesRiesgosComponent;
  let fixture: ComponentFixture<MatricesRiesgosComponent>;
  let service: MatricesRiesgosService;

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
    await TestBed.configureTestingModule({
      imports: [MatricesRiesgosComponent, HttpClientTestingModule],
      providers: [
        MatricesRiesgosService,
        GlobalHttpStateService,
        {
          provide: AuthService,
          useValue: {
            tieneRol: () => true
          }
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

  it('7. Abrir modal Ver detalle contiene la descripción que no está en la tabla', () => {
    component.abrirModalVerFamilia(mockFamilias[0]);
    fixture.detectChanges();

    expect(component.modalVerFamiliaAbierto()).toEqual(mockFamilias[0]);
    const compiled = fixture.nativeElement as HTMLElement;
    const verModalDesc = compiled.querySelector('#titulo-modal-ver-familia');
    expect(verModalDesc?.textContent).toContain('Familia: Familia Empleados');

    component.cerrarModalVerFamilia();
    expect(component.modalVerFamiliaAbierto()).toBeNull();
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
});
