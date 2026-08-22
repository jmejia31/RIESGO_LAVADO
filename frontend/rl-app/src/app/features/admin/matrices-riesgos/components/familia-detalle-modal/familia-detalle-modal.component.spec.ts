import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { Observable, Subject, of, throwError } from 'rxjs';
import { AuthService } from '../../../../../core/auth/auth.service';
import { MatricesRiesgosService } from '../../data-access/matrices-riesgos.service';
import { FamiliaFormularioDto } from '../../models/matrices-riesgos.models';
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
    famCodigo: 'FAMILIA_AUTORITATIVA',
    famNombre: 'Familia autoritativa',
    famDescripcion: 'Detalle autoritativo recuperado por identificador.',
    famActivo: true,
    famFechaCreacion: '2026-08-07T00:00:00',
    totalVersiones: 2,
    tieneVersionVigente: true
  };

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

  it('1. consulta el detalle por el famId exacto', () => {
    const spyDetalle = vi.spyOn(service, 'obtenerFamiliaFormularioPorId').mockReturnValue(of(familia));

    const fixture = crearComponente(7);

    expect(spyDetalle).toHaveBeenCalledWith(7);
    expect(fixture.componentInstance.detalle()).toEqual(familia);
  });

  it('2. mantiene estado loading mientras la respuesta está pendiente', () => {
    const detallePendiente = new Subject<FamiliaFormularioDto>();
    vi.spyOn(service, 'obtenerFamiliaFormularioPorId').mockReturnValue(detallePendiente);

    const fixture = crearComponente();
    fixture.detectChanges();

    expect(fixture.componentInstance.cargando()).toBe(true);
    expect((fixture.nativeElement as HTMLElement).querySelector('[data-ui-fam-detail-state="loading"]')).not.toBeNull();
  });

  it('3. renderiza los datos autoritativos devueltos por GET y no la referencia obsoleta', () => {
    vi.spyOn(service, 'obtenerFamiliaFormularioPorId').mockReturnValue(of(familia));

    const fixture = crearComponente();
    fixture.detectChanges();
    const texto = (fixture.nativeElement as HTMLElement).textContent ?? '';

    expect(texto).toContain('Familia autoritativa');
    expect(texto).toContain('Detalle autoritativo recuperado por identificador.');
    expect(texto).not.toContain('Descripción resumida del listado');
  });

  it('4. trata 404 como no encontrada sin mezclarlo con error genérico', () => {
    vi.spyOn(service, 'obtenerFamiliaFormularioPorId').mockReturnValue(throwError(() => ({ status: 404 })));

    const fixture = crearComponente();
    fixture.detectChanges();

    expect(fixture.componentInstance.noEncontrada()).toBe(true);
    expect(fixture.componentInstance.error()).toBeNull();
    expect((fixture.nativeElement as HTMLElement).querySelector('[data-ui-fam-detail-state="not-found"]')).not.toBeNull();
  });

  it('5. presenta un error recuperable y reintenta el GET del mismo id', () => {
    const spyDetalle = vi.spyOn(service, 'obtenerFamiliaFormularioPorId')
      .mockReturnValueOnce(throwError(() => ({ status: 500, error: { detail: 'Falla temporal controlada.' } })))
      .mockReturnValueOnce(of(familia));

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

  it('6. al cambiar rápidamente de A a B impide que una respuesta tardía de A sobrescriba B', () => {
    const respuestaA = new Subject<FamiliaFormularioDto>();
    const familiaB: FamiliaFormularioDto = { ...familia, famId: 8, famCodigo: 'FAMILIA_B', famNombre: 'Familia B' };
    const spyDetalle = vi.spyOn(service, 'obtenerFamiliaFormularioPorId')
      .mockReturnValueOnce(respuestaA)
      .mockReturnValueOnce(of(familiaB));

    const fixture = crearComponente(7);

    fixture.componentRef.setInput('familiaId', 8);
    fixture.detectChanges();

    expect(spyDetalle).toHaveBeenNthCalledWith(2, 8);
    expect(fixture.componentInstance.detalle()?.famCodigo).toBe('FAMILIA_B');

    respuestaA.next(familia);
    fixture.detectChanges();
    expect(fixture.componentInstance.detalle()?.famCodigo).toBe('FAMILIA_B');
  });

  it('7. destruir el modal durante la carga cancela la suscripción pendiente', () => {
    let cancelada = false;
    const pendiente = new Observable<FamiliaFormularioDto>(() => () => {
      cancelada = true;
    });
    vi.spyOn(service, 'obtenerFamiliaFormularioPorId').mockReturnValue(pendiente);

    const fixture = crearComponente();
    expect(fixture.componentInstance.cargando()).toBe(true);

    fixture.destroy();
    expect(cancelada).toBe(true);
  });

  it('8. emite Ver versiones y Editar usando la familia autoritativa', () => {
    vi.spyOn(service, 'obtenerFamiliaFormularioPorId').mockReturnValue(of(familia));
    const fixture = crearComponente();
    const gestionar = vi.spyOn(fixture.componentInstance.gestionarVersiones, 'emit');
    const editar = vi.spyOn(fixture.componentInstance.editarFamilia, 'emit');

    fixture.componentInstance.solicitarGestionVersiones();
    fixture.componentInstance.solicitarEdicion();

    expect(gestionar).toHaveBeenCalledWith(familia);
    expect(editar).toHaveBeenCalledWith(familia);
  });

  it('9. mantiene UI-FAM.2 limitada al detalle y no integra todavía historial de versiones ni actividad ficticia', () => {
    vi.spyOn(service, 'obtenerFamiliaFormularioPorId').mockReturnValue(of(familia));
    const fixture = crearComponente();
    fixture.detectChanges();
    const host = fixture.nativeElement as HTMLElement;
    const texto = host.textContent ?? '';

    expect(texto).toContain('Ver versiones');
    expect(texto).not.toContain('Historial de versiones');
    expect(texto).not.toContain('Versiones del formulario');
    expect(texto).not.toContain('Actividad reciente');
    expect(texto).not.toContain('Última actividad');
  });

  it('10. restaura el foco al disparador cuando el modal se destruye', async () => {
    vi.spyOn(service, 'obtenerFamiliaFormularioPorId').mockReturnValue(of(familia));

    const disparador = document.createElement('button');
    disparador.textContent = 'Abrir detalle';
    document.body.appendChild(disparador);
    disparador.focus();

    const fixture = crearComponente();
    document.body.appendChild(fixture.nativeElement);
    await new Promise(resolve => setTimeout(resolve, 0));
    expect(document.activeElement?.getAttribute('aria-label')).toBe('Cerrar detalle de familia');

    fixture.destroy();
    await new Promise(resolve => setTimeout(resolve, 0));
    expect(document.activeElement).toBe(disparador);
    fixture.nativeElement.remove();
    disparador.remove();
  });

  it('11. conserva un único dialog accesible con aria-modal', () => {
    vi.spyOn(service, 'obtenerFamiliaFormularioPorId').mockReturnValue(of(familia));
    const fixture = crearComponente();
    const dialog = (fixture.nativeElement as HTMLElement).querySelector('dialog');

    expect(dialog?.getAttribute('role')).toBe('dialog');
    expect(dialog?.getAttribute('aria-modal')).toBe('true');
    expect((fixture.nativeElement as HTMLElement).querySelectorAll('dialog[open]').length).toBe(1);
  });
});
