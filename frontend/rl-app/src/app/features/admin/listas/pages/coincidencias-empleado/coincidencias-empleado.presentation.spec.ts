import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { AuthService } from '../../../../../core/auth/auth.service';
import { ListasService } from '../../data-access/listas.service';
import { ReportPreviewService } from '../../../../../shared/report-preview/report-preview.service';
import { InstitutionalConfirmationService } from '../../../../../shared/confirmation/institutional-confirmation.service';
import { InstitutionalNotificationService } from '../../../../../shared/notification/institutional-notification.service';
import { CoincidenciasEmpleadoComponent } from './coincidencias-empleado.component';

describe('CoincidenciasEmpleadoComponent — superficie de tabla', () => {
  let fixture: ComponentFixture<CoincidenciasEmpleadoComponent>;

  beforeEach(async () => {
    const listasService = {
      getResumenCoincidenciasEmpleadoPaginado: vi.fn(() => of({
        items: [],
        pagina: 1,
        tamanoPagina: 25,
        totalRegistros: 0,
        totalPaginas: 0
      })),
      getDetalleCoincidenciasEmpleadoPaginado: vi.fn(() => of({
        items: [],
        pagina: 1,
        tamanoPagina: 25,
        totalRegistros: 0,
        totalPaginas: 0
      }))
    };

    await TestBed.configureTestingModule({
      imports: [CoincidenciasEmpleadoComponent],
      providers: [
        { provide: ListasService, useValue: listasService },
        { provide: AuthService, useValue: {} },
        { provide: ReportPreviewService, useValue: {} },
        { provide: InstitutionalConfirmationService, useValue: {} },
        { provide: InstitutionalNotificationService, useValue: {} }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(CoincidenciasEmpleadoComponent);
    fixture.detectChanges();
  });

  it('mantiene la tabla dentro de una superficie con altura y scroll propios', () => {
    const root = fixture.nativeElement as HTMLElement;
    const superficieTabla = root.querySelector('[aria-label="Tabla de coincidencias de empleados"]') as HTMLElement;

    expect(superficieTabla).not.toBeNull();
    expect(superficieTabla.className).toContain('h-[55vh]');
    expect(superficieTabla.className).toContain('min-h-[22rem]');
    expect(superficieTabla.className).toContain('max-h-[36rem]');
    expect(superficieTabla.className).toContain('overflow-y-scroll');
    expect(superficieTabla.className).toContain('overflow-x-hidden');
    expect(superficieTabla.className).toContain('overscroll-contain');
    expect(superficieTabla.querySelector('.overflow-x-auto')).not.toBeNull();
    expect(superficieTabla.querySelector('table')).not.toBeNull();
    expect(root.querySelector('app-data-pagination')).not.toBeNull();
  });
});
