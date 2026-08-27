import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { Observable, Subject, of, throwError } from 'rxjs';
import { AuthService } from '../../../../../core/auth/auth.service';
import { AuditoriaService } from '../../../bitacora/data-access/auditoria.service';
import { MatricesRiesgosService } from '../../data-access/matrices-riesgos.service';
import { FamiliaFormularioDto, VersionFormularioDto } from '../../models/matrices-riesgos.models';
import { FamiliaDetalleModalComponent } from './familia-detalle-modal.component';

describe('FamiliaDetalleModalComponent — UI-FAM.2', () => {
  let service: MatricesRiesgosService;
  let auditoriaService: AuditoriaService;

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
    totalVersiones: 3,
    tieneVersionVigente: true
  };

  const versiones: VersionFormularioDto[] = [
    {
      verId: 15,
      verFamiliaId: 7,
      verCodigo: 'FAMILIA_AUTORITATIVA',
      verVersion: 5,
      verJson: '{}',
      verHash: 'hash-v5',
      verEstado: 'DRAFT',
      verVigente: false,
      verFechaInicio: null,
      verFechaFin: null,
      verFechaCreacion: '2026-08-21T10:00:00',
      verUsrCreacion: 11
    },
    {
      verId: 14,
      verFamiliaId: 7,
      verCodigo: 'FAMILIA_AUTORITATIVA',
      verVersion: 4,
      verJson: '{}',
      verHash: 'hash-v4',
      verEstado: 'PUBLISHED',
      verVigente: true,
      verFechaInicio: '2026-08-19T09:00:00',
      verFechaFin: null,
      verFechaCreacion: '2026-08-19T08:00:00',
      verUsrCreacion: 10
    },
    {
      verId: 13,
      verFamiliaId: 7,
      verCodigo: 'FAMILIA_AUTORITATIVA',
      verVersion: 3,
      verJson: '{}',
      verHash: 'hash-v3',
      verEstado: 'RETIRED',
      verVigente: false,
      verFechaInicio: '2026-08-12T09:00:00',
      verFechaFin: '2026-08-18T18:00:00',
      verFechaCreacion: '2026-08-12T08:00:00',
      verUsrCreacion: 9
    }
  ];

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [FamiliaDetalleModalComponent, HttpClientTestingModule],
      providers: [
        MatricesRiesgosService,
        AuditoriaService,
        {
          provide: AuthService,
          useValue: { tieneRol: () => true }
        }
      ]
    }).compileComponents();

    service = TestBed.inject(MatricesRiesgosService);
    auditoriaService = TestBed.inject(AuditoriaService);
    vi.spyOn(service, 'listarHistorialVersionesFormulario').mockReturnValue(of(versiones));
    vi.spyOn(auditoriaService, 'getBitacora').mockReturnValue(of({ datos: [], totalRegistros: 0 }));
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

  it('bloquea Escape sin cerrar el detalle', () => {
    vi.spyOn(service, 'obtenerFamiliaFormularioPorId').mockReturnValue(of(familia));
    const fixture = crearComponente();
    const event = new KeyboardEvent('keydown', { key: 'Escape', cancelable: true });
    fixture.componentInstance.manejarKeydown(event);

    expect(event.defaultPrevented).toBe(true);
    expect((fixture.nativeElement as HTMLElement).querySelector('[data-ui-fam-detail="modal"]')).not.toBeNull();
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
    expect(fixture.componentInstance.error()).toBe('Falla temporal controlada.');
    fixture.componentInstance.reintentarDetalle();
    fixture.detectChanges();
    expect(spyDetalle).toHaveBeenCalledTimes(2);
    expect(fixture.componentInstance.detalle()).toEqual(familia);
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
    const pendiente = new Observable<FamiliaFormularioDto>(() => () => { cancelada = true; });
    vi.spyOn(service, 'obtenerFamiliaFormularioPorId').mockReturnValue(pendiente);
    const fixture = crearComponente();
    fixture.destroy();
    expect(cancelada).toBe(true);
  });

  it('8. carga dinámicamente el historial de versiones usando el famCodigo autoritativo', () => {
    const spyVersiones = vi.mocked(service.listarHistorialVersionesFormulario);
    vi.spyOn(service, 'obtenerFamiliaFormularioPorId').mockReturnValue(of(familia));
    const fixture = crearComponente();
    fixture.detectChanges();
    expect(spyVersiones).toHaveBeenCalledWith('FAMILIA_AUTORITATIVA');
    expect(fixture.componentInstance.versionesOrdenadas().map(v => v.verVersion)).toEqual([5, 4, 3]);
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Versiones del formulario');
  });

  it('9. construye actividad reciente únicamente con datos reales de auditoría, creación y versiones', () => {
    vi.spyOn(service, 'obtenerFamiliaFormularioPorId').mockReturnValue(of(familia));
    vi.mocked(auditoriaService.getBitacora).mockReturnValue(of({
      datos: [{
        audId: 91,
        tabla: 'RL_MR_FAMILIAS_FORMULARIO',
        registroId: '7',
        accion: 'UPDATE',
        usrEmail: 'admin@ihss.hn',
        fecha: '2026-08-20T10:00:00',
        modulo: 'Matrices'
      }],
      totalRegistros: 1
    }));
    const fixture = crearComponente();
    fixture.detectChanges();
    const actividad = fixture.componentInstance.actividadReciente();
    expect(actividad.some(item => item.titulo === 'Familia actualizada')).toBe(true);
    expect(actividad.some(item => item.titulo === 'Versión v4 publicada')).toBe(true);
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Actividad reciente');
  });

  it('10. emite Editar familia, Nueva versión y Ver definición con la familia autoritativa', () => {
    vi.spyOn(service, 'obtenerFamiliaFormularioPorId').mockReturnValue(of(familia));
    const fixture = crearComponente();
    const editar = vi.spyOn(fixture.componentInstance.editarFamilia, 'emit');
    const nuevaVersion = vi.spyOn(fixture.componentInstance.nuevaVersion, 'emit');
    const ver = vi.spyOn(fixture.componentInstance.verDefinicion, 'emit');
    fixture.componentInstance.solicitarEdicion();
    fixture.componentInstance.solicitarNuevaVersion();
    fixture.componentInstance.solicitarVerVersion(versiones[1]);
    expect(editar).toHaveBeenCalledWith(familia);
    expect(nuevaVersion).toHaveBeenCalledWith(familia);
    expect(ver).toHaveBeenCalledWith({ familia, version: versiones[1] });
  });

  it('11. clona una versión publicada y recarga el historial', () => {
    vi.spyOn(service, 'obtenerFamiliaFormularioPorId').mockReturnValue(of(familia));
    const spyClonar = vi.spyOn(service, 'clonarVersionFormulario').mockReturnValue(of(99));
    const spyHistorial = vi.mocked(service.listarHistorialVersionesFormulario);
    const fixture = crearComponente();
    fixture.componentInstance.clonarVersion(versiones[1]);
    expect(spyClonar).toHaveBeenCalledWith(14);
    expect(spyHistorial).toHaveBeenCalledTimes(2);
  });

  it('12. activa o desactiva mediante la operación explícita de ciclo de vida', () => {
    vi.spyOn(service, 'obtenerFamiliaFormularioPorId').mockReturnValue(of(familia));
    const spyDesactivar = vi.spyOn(service, 'desactivarFamiliaFormulario').mockReturnValue(of(true));
    vi.spyOn(window, 'confirm').mockReturnValue(true);
    const fixture = crearComponente();
    fixture.componentInstance.cambiarEstadoFamilia();
    expect(spyDesactivar).toHaveBeenCalledWith(7);
  });

  it('13. muestra los estados contractuales sin inventar estados nuevos', () => {
    vi.spyOn(service, 'obtenerFamiliaFormularioPorId').mockReturnValue(of(familia));
    const fixture = crearComponente();
    expect(fixture.componentInstance.etiquetaEstado('DRAFT')).toBe('BORRADOR');
    expect(fixture.componentInstance.etiquetaEstado('IN_REVIEW')).toBe('EN REVISIÓN');
    expect(fixture.componentInstance.etiquetaEstado('APPROVED')).toBe('APROBADA');
    expect(fixture.componentInstance.etiquetaEstado('PUBLISHED')).toBe('PUBLICADA');
    expect(fixture.componentInstance.etiquetaEstado('RETIRED')).toBe('RETIRADA');
    expect(fixture.componentInstance.etiquetaEstado('ARCHIVED')).toBe('ARCHIVADA');
  });

  it('14. conserva un único dialog accesible con aria-modal', () => {
    vi.spyOn(service, 'obtenerFamiliaFormularioPorId').mockReturnValue(of(familia));
    const fixture = crearComponente();
    const dialog = (fixture.nativeElement as HTMLElement).querySelector('dialog');
    expect(dialog?.getAttribute('role')).toBe('dialog');
    expect(dialog?.getAttribute('aria-modal')).toBe('true');
    expect((fixture.nativeElement as HTMLElement).querySelectorAll('dialog[open]').length).toBe(1);
  });

  it('15. limpia el detalle y las suscripciones para un ID inválido', () => {
    const fixture = crearComponente();
    fixture.componentRef.setInput('familiaId', 0);
    fixture.detectChanges();

    expect(fixture.componentInstance.detalle()).toBeNull();
    expect(fixture.componentInstance.versiones()).toEqual([]);
    expect(fixture.componentInstance.auditoria()).toEqual([]);
    expect(fixture.componentInstance.error()).toBeNull();
  });

  it('16. muestra error al clonar y al cambiar el estado de la familia', () => {
    vi.spyOn(service, 'obtenerFamiliaFormularioPorId').mockReturnValue(of(familia));
    const fixture = crearComponente();
    vi.spyOn(service, 'clonarVersionFormulario').mockReturnValue(throwError(() => ({ error: { detail: 'No se puede clonar.' } })));
    fixture.componentInstance.clonarVersion(versiones[1]);
    expect(fixture.componentInstance.errorVersiones()).toBe('No se puede clonar.');

    vi.spyOn(service, 'desactivarFamiliaFormulario').mockReturnValue(throwError(() => ({ error: { detail: 'No se puede desactivar.' } })));
    fixture.componentInstance.cambiarEstadoFamilia();
    expect(fixture.componentInstance.error()).toBe('No se puede desactivar.');
    expect(fixture.componentInstance.operando()).toBe(false);
  });

  it('17. maneja errores de historial y auditoría y permite reintentar historial', () => {
    vi.spyOn(service, 'obtenerFamiliaFormularioPorId').mockReturnValue(of(familia));
    const versionesSpy = vi.spyOn(service, 'listarHistorialVersionesFormulario')
      .mockReturnValueOnce(throwError(() => ({ error: { detail: 'Historial no disponible.' } })))
      .mockReturnValueOnce(of(versiones));
    vi.mocked(auditoriaService.getBitacora).mockReturnValue(throwError(() => new Error('auditoría')));
    const fixture = crearComponente();

    expect(fixture.componentInstance.errorVersiones()).toBe('Historial no disponible.');
    fixture.componentInstance.reintentarVersiones();
    expect(versionesSpy).toHaveBeenCalledTimes(2);
    expect(fixture.componentInstance.versiones()).toEqual(versiones);
    expect(fixture.componentInstance.errorActividad()).toBe('La actividad de auditoría no está disponible para esta consulta.');
  });

  it('18. mapea creación, actualización y acciones no mapeadas de auditoría', () => {
    vi.spyOn(service, 'obtenerFamiliaFormularioPorId').mockReturnValue(of(familia));
    vi.mocked(auditoriaService.getBitacora).mockReturnValue(of({ datos: [
      { audId: 1, tabla: 'RL_MR_FAMILIAS_FORMULARIO', registroId: String(familia.famId), accion: 'CREATE', usrId: 8, fecha: '2026-08-22T10:00:00' },
      { audId: 2, tabla: 'RL_MR_FAMILIAS_FORMULARIO', registroId: String(familia.famId), accion: 'UPDATE', usrEmail: 'admin@ihss.hn', fecha: '2026-08-21T10:00:00' },
      { audId: 3, tabla: 'RL_MR_FAMILIAS_FORMULARIO', registroId: String(familia.famId), accion: 'OTHER', fecha: '2026-08-20T10:00:00' }
    ], totalRegistros: 3 }));
    const fixture = crearComponente();

    const titulos = fixture.componentInstance.actividadReciente().map(item => item.titulo);
    expect(titulos).toContain('Familia creada');
    expect(titulos).toContain('Familia actualizada');
    expect(fixture.componentInstance.actividadReciente()[0].usuario).toBe('Usuario #8');
  });
});
