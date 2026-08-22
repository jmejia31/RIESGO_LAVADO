import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { Subject, of, throwError } from 'rxjs';
import { AuthService } from '../../../../../core/auth/auth.service';
import { MatricesRiesgosService } from '../../data-access/matrices-riesgos.service';
import { FamiliaFormularioDto, VersionFormularioDto } from '../../models/matrices-riesgos.models';
import { FamiliaDetalleModalComponent } from './familia-detalle-modal.component';

describe('FamiliaDetalleModalComponent — UI-FAM.2', () => {
  let service: MatricesRiesgosService;

  const referencia: FamiliaFormularioDto = {
    famId: 7,
    famCodigo: 'FAM_REFERENCIA',
    famNombre: 'Nombre desde listado',
    famDescripcion: 'Descripción resumida del listado',
    famActivo: true,
    famFechaCreacion: '2026-08-01T00:00:00',
    totalVersiones: 1,
    tieneVersionVigente: false
  };

  const familia: FamiliaFormularioDto = {
    famId: 7,
    famCodigo: 'MATRIZ_RIESGOS_LAFT',
    famNombre: 'Matriz de Riesgos LA/FT',
    famDescripcion: 'Detalle autoritativo recuperado por identificador.',
    famActivo: true,
    famFechaCreacion: '2026-08-07T00:00:00',
    totalVersiones: 2,
    tieneVersionVigente: true
  };

  const versiones: VersionFormularioDto[] = [
    {
      verId: 71,
      verFamiliaId: 7,
      verCodigo: 'MATRIZ_RIESGOS_LAFT',
      verVersion: 2,
      verJson: '{"secciones":[]}',
      verHash: 'hash-v2',
      verEstado: 'PUBLISHED',
      verVigente: true,
      verFechaInicio: '2026-08-20T00:00:00',
      verFechaFin: null,
      verFechaCreacion: '2026-08-20T00:00:00',
      verUsrCreacion: 1
    },
    {
      verId: 70,
      verFamiliaId: 7,
      verCodigo: 'MATRIZ_RIESGOS_LAFT',
      verVersion: 1,
      verJson: '{"secciones":[]}',
      verHash: 'hash-v1',
      verEstado: 'PUBLISHED',
      verVigente: false,
      verFechaInicio: '2026-08-07T00:00:00',
      verFechaFin: '2026-08-19T00:00:00',
      verFechaCreacion: '2026-08-07T00:00:00',
      verUsrCreacion: 1
    }
  ];

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [FamiliaDetalleModalComponent, HttpClientTestingModule],
      providers: [
        MatricesRiesgosService,
        {
          provide: AuthService,
          useValue: { tieneRol: () => true }
        }
      ]
    }).compileComponents();

    service = TestBed.inject(MatricesRiesgosService);
  });

  function crearComponente(id = 7): ComponentFixture<FamiliaDetalleModalComponent> {
    const fixture = TestBed.createComponent(FamiliaDetalleModalComponent);
    fixture.componentRef.setInput('familiaReferencia', referencia);
    fixture.componentRef.setInput('familiaId', id);
    fixture.detectChanges();
    return fixture;
  }

  it('1. consulta el detalle por famId exacto y después el historial por código autoritativo', () => {
    vi.spyOn(service, 'obtenerFamiliaFormularioPorId').mockReturnValue(of(familia));
    vi.spyOn(service, 'listarHistorialVersionesFormulario').mockReturnValue(of(versiones));

    const fixture = crearComponente(7);
    const component = fixture.componentInstance;

    expect(service.obtenerFamiliaFormularioPorId).toHaveBeenCalledWith(7);
    expect(service.listarHistorialVersionesFormulario).toHaveBeenCalledWith('MATRIZ_RIESGOS_LAFT');
    expect(component.detalle()).toEqual(familia);
    expect(component.versiones()).toEqual(versiones);
  });

  it('2. mantiene un estado loading independiente mientras la respuesta de detalle está pendiente', () => {
    const detallePendiente = new Subject<FamiliaFormularioDto>();
    vi.spyOn(service, 'obtenerFamiliaFormularioPorId').mockReturnValue(detallePendiente);
    vi.spyOn(service, 'listarHistorialVersionesFormulario').mockReturnValue(of([]));

    const fixture = crearComponente();
    fixture.detectChanges();

    expect(fixture.componentInstance.cargando()).toBe(true);
    expect((fixture.nativeElement as HTMLElement).querySelector('[data-ui-fam-detail-state="loading"]')).not.toBeNull();
  });

  it('3. renderiza únicamente los datos autoritativos devueltos por GET de familia', () => {
    vi.spyOn(service, 'obtenerFamiliaFormularioPorId').mockReturnValue(of(familia));
    vi.spyOn(service, 'listarHistorialVersionesFormulario').mockReturnValue(of(versiones));

    const fixture = crearComponente();
    fixture.detectChanges();
    const texto = (fixture.nativeElement as HTMLElement).textContent ?? '';

    expect(texto).toContain('Matriz de Riesgos LA/FT');
    expect(texto).toContain('Detalle autoritativo recuperado por identificador.');
    expect(texto).not.toContain('Descripción resumida del listado');
  });

  it('4. separa 404 como estado no encontrada y no lo presenta como error genérico', () => {
    vi.spyOn(service, 'obtenerFamiliaFormularioPorId').mockReturnValue(throwError(() => ({ status: 404 })));
    vi.spyOn(service, 'listarHistorialVersionesFormulario').mockReturnValue(of([]));

    const fixture = crearComponente();
    fixture.detectChanges();

    expect(fixture.componentInstance.noEncontrada()).toBe(true);
    expect(fixture.componentInstance.error()).toBeNull();
    expect((fixture.nativeElement as HTMLElement).querySelector('[data-ui-fam-detail-state="not-found"]')).not.toBeNull();
  });

  it('5. presenta error recuperable y reintenta el GET por el mismo id', () => {
    const spyDetalle = vi.spyOn(service, 'obtenerFamiliaFormularioPorId')
      .mockReturnValueOnce(throwError(() => ({ status: 500, error: { detail: 'Falla temporal controlada.' } })))
      .mockReturnValueOnce(of(familia));
    vi.spyOn(service, 'listarHistorialVersionesFormulario').mockReturnValue(of([]));

    const fixture = crearComponente();
    fixture.detectChanges();
    expect(fixture.componentInstance.error()).toBe('Falla temporal controlada.');

    fixture.componentInstance.reintentarDetalle();
    fixture.detectChanges();

    expect(spyDetalle).toHaveBeenCalledTimes(2);
    expect(spyDetalle).toHaveBeenNthCalledWith(2, 7);
    expect(fixture.componentInstance.detalle()).toEqual(familia);
    expect(fixture.componentInstance.error()).toBeNull();
  });

  it('6. un error del historial no oculta el detalle de familia y permite reintentar solo versiones', () => {
    vi.spyOn(service, 'obtenerFamiliaFormularioPorId').mockReturnValue(of(familia));
    const spyVersiones = vi.spyOn(service, 'listarHistorialVersionesFormulario')
      .mockReturnValueOnce(throwError(() => ({ error: { detail: 'Historial no disponible.' } })))
      .mockReturnValueOnce(of(versiones));

    const fixture = crearComponente();
    fixture.detectChanges();

    expect(fixture.componentInstance.detalle()).toEqual(familia);
    expect(fixture.componentInstance.errorVersiones()).toBe('Historial no disponible.');

    fixture.componentInstance.reintentarVersiones();
    fixture.detectChanges();

    expect(spyVersiones).toHaveBeenCalledTimes(2);
    expect(fixture.componentInstance.versiones()).toEqual(versiones);
    expect(fixture.componentInstance.errorVersiones()).toBeNull();
  });

  it('7. al cambiar rápidamente de familia A a B cancela A e impide que una respuesta tardía sobrescriba B', () => {
    const respuestaA = new Subject<FamiliaFormularioDto>();
    const familiaB: FamiliaFormularioDto = { ...familia, famId: 8, famCodigo: 'GTIC', famNombre: 'Familia GTIC' };
    const spyDetalle = vi.spyOn(service, 'obtenerFamiliaFormularioPorId')
      .mockReturnValueOnce(respuestaA)
      .mockReturnValueOnce(of(familiaB));
    vi.spyOn(service, 'listarHistorialVersionesFormulario').mockReturnValue(of([]));

    const fixture = crearComponente(7);
    expect(respuestaA.observed).toBe(true);

    fixture.componentRef.setInput('familiaId', 8);
    fixture.detectChanges();

    expect(spyDetalle).toHaveBeenNthCalledWith(2, 8);
    expect(respuestaA.observed).toBe(false);
    expect(fixture.componentInstance.detalle()?.famCodigo).toBe('GTIC');

    respuestaA.next(familia);
    expect(fixture.componentInstance.detalle()?.famCodigo).toBe('GTIC');
  });

  it('8. destruir el modal durante una carga cancela la suscripción pendiente', () => {
    const respuestaPendiente = new Subject<FamiliaFormularioDto>();
    vi.spyOn(service, 'obtenerFamiliaFormularioPorId').mockReturnValue(respuestaPendiente);
    vi.spyOn(service, 'listarHistorialVersionesFormulario').mockReturnValue(of([]));

    const fixture = crearComponente();
    expect(respuestaPendiente.observed).toBe(true);

    fixture.destroy();
    expect(respuestaPendiente.observed).toBe(false);
  });

  it('9. emite cerrar sin mutar datos de familia', () => {
    vi.spyOn(service, 'obtenerFamiliaFormularioPorId').mockReturnValue(of(familia));
    vi.spyOn(service, 'listarHistorialVersionesFormulario').mockReturnValue(of([]));
    const fixture = crearComponente();
    const emit = vi.spyOn(fixture.componentInstance.cerrar, 'emit');

    fixture.componentInstance.cerrar.emit();
    expect(emit).toHaveBeenCalledTimes(1);
    expect(fixture.componentInstance.detalle()).toEqual(familia);
  });

  it('10. emite gestionar versiones y editar usando la familia autoritativa, no la referencia de listado', () => {
    vi.spyOn(service, 'obtenerFamiliaFormularioPorId').mockReturnValue(of(familia));
    vi.spyOn(service, 'listarHistorialVersionesFormulario').mockReturnValue(of([]));
    const fixture = crearComponente();
    const gestionar = vi.spyOn(fixture.componentInstance.gestionarVersiones, 'emit');
    const editar = vi.spyOn(fixture.componentInstance.editarFamilia, 'emit');

    fixture.componentInstance.solicitarGestionVersiones();
    fixture.componentInstance.solicitarEdicion();

    expect(gestionar).toHaveBeenCalledWith(familia);
    expect(editar).toHaveBeenCalledWith(familia);
  });

  it('11. emite nueva versión y ver definición con el contexto autoritativo de familia', () => {
    vi.spyOn(service, 'obtenerFamiliaFormularioPorId').mockReturnValue(of(familia));
    vi.spyOn(service, 'listarHistorialVersionesFormulario').mockReturnValue(of(versiones));
    const fixture = crearComponente();
    const nuevaVersion = vi.spyOn(fixture.componentInstance.nuevaVersion, 'emit');
    const verDefinicion = vi.spyOn(fixture.componentInstance.verDefinicion, 'emit');

    fixture.componentInstance.solicitarNuevaVersion();
    fixture.componentInstance.solicitarVerDefinicion(versiones[0]);

    expect(nuevaVersion).toHaveBeenCalledWith(familia);
    expect(verDefinicion).toHaveBeenCalledWith({ familia, version: versiones[0] });
  });

  it('12. renderiza el historial con campos reales de VersionFormularioDto', () => {
    vi.spyOn(service, 'obtenerFamiliaFormularioPorId').mockReturnValue(of(familia));
    vi.spyOn(service, 'listarHistorialVersionesFormulario').mockReturnValue(of(versiones));
    const fixture = crearComponente();
    fixture.detectChanges();
    const texto = (fixture.nativeElement as HTMLElement).textContent ?? '';

    expect(texto).toContain('v2');
    expect(texto).toContain('PUBLISHED');
    expect(texto).toContain('Vigente');
    expect(texto).toContain('Histórica');
    expect(texto).toContain('Ver definición');
  });

  it('13. no inventa actividad reciente, usuario de actualización ni última actividad', () => {
    vi.spyOn(service, 'obtenerFamiliaFormularioPorId').mockReturnValue(of(familia));
    vi.spyOn(service, 'listarHistorialVersionesFormulario').mockReturnValue(of(versiones));
    const fixture = crearComponente();
    fixture.detectChanges();
    const texto = (fixture.nativeElement as HTMLElement).textContent ?? '';

    expect(texto).not.toContain('Actividad reciente');
    expect(texto).not.toContain('Última actividad');
    expect(texto).not.toContain('Actualizado por');
  });

  it('14. restaura el foco al disparador cuando el modal se destruye', async () => {
    vi.spyOn(service, 'obtenerFamiliaFormularioPorId').mockReturnValue(of(familia));
    vi.spyOn(service, 'listarHistorialVersionesFormulario').mockReturnValue(of([]));

    const disparador = document.createElement('button');
    disparador.textContent = 'Abrir detalle';
    document.body.appendChild(disparador);
    disparador.focus();

    const fixture = crearComponente();
    await new Promise(resolve => setTimeout(resolve, 0));
    expect(document.activeElement?.getAttribute('aria-label')).toBe('Cerrar detalle de familia');

    fixture.destroy();
    await new Promise(resolve => setTimeout(resolve, 0));
    expect(document.activeElement).toBe(disparador);
    disparador.remove();
  });
});
