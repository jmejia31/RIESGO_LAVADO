import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Subject, of, throwError } from 'rxjs';
import { describe, expect, it, beforeEach, vi } from 'vitest';
import { AuthService } from '../../../../../core/auth/auth.service';
import { MatricesRiesgosService } from '../../data-access/matrices-riesgos.service';
import { FamiliaFormularioDto } from '../../models/matrices-riesgos.models';
import { FamiliaEditarModalComponent } from './familia-editar-modal.component';

vi.mock('sweetalert2', () => ({
  default: {
    fire: vi.fn(() => Promise.resolve({ isConfirmed: true }))
  }
}));

const familiaBase: FamiliaFormularioDto = {
  famId: 7,
  famCodigo: 'PRUEBA_FORMULARIO',
  famNombre: 'Prueba de Formulario',
  famDescripcion: 'Descripción institucional',
  famActivo: true,
  famFechaCreacion: '2026-08-12T00:00:00Z',
  totalVersiones: 3,
  tieneVersionVigente: false
};

describe('FamiliaEditarModalComponent — UI-FAM.4 + UI-FAM.QA', () => {
  let fixture: ComponentFixture<FamiliaEditarModalComponent>;
  let component: FamiliaEditarModalComponent;

  const service = {
    obtenerFamiliaFormularioPorId: vi.fn(),
    actualizarFamiliaFormulario: vi.fn(),
    activarFamiliaFormulario: vi.fn(),
    desactivarFamiliaFormulario: vi.fn(),
    eliminarFamiliaFormulario: vi.fn()
  };

  const auth = {
    tieneRol: vi.fn(() => true)
  };

  async function estabilizarVista(): Promise<void> {
    await fixture.whenStable();
    fixture.detectChanges();
  }

  async function cambiarFamilia(id: number): Promise<void> {
    fixture.componentRef.setInput('familiaId', id);
    fixture.detectChanges();
    await estabilizarVista();
  }

  beforeEach(async () => {
    vi.clearAllMocks();
    auth.tieneRol.mockReturnValue(true);
    service.obtenerFamiliaFormularioPorId.mockReturnValue(of({ ...familiaBase }));
    service.actualizarFamiliaFormulario.mockReturnValue(of(true));
    service.activarFamiliaFormulario.mockReturnValue(of(true));
    service.desactivarFamiliaFormulario.mockReturnValue(of(true));
    service.eliminarFamiliaFormulario.mockReturnValue(of(true));

    await TestBed.configureTestingModule({
      imports: [FamiliaEditarModalComponent],
      providers: [
        { provide: MatricesRiesgosService, useValue: service },
        { provide: AuthService, useValue: auth }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(FamiliaEditarModalComponent);
    component = fixture.componentInstance;
    await cambiarFamilia(7);
  });

  it('carga la familia por ID y mantiene el código bloqueado', async () => {
    await estabilizarVista();

    expect(service.obtenerFamiliaFormularioPorId).toHaveBeenCalledWith(7);
    expect(component.detalle()?.famCodigo).toBe('PRUEBA_FORMULARIO');

    component.detalle.set({ ...familiaBase });
    component.cargando.set(false);
    fixture.detectChanges();

    const codigo = fixture.nativeElement.querySelector('#editar-fam-codigo') as HTMLInputElement | null;
    expect(codigo).not.toBeNull();
    expect(codigo?.readOnly).toBe(true);
    expect(codigo?.value).toBe('PRUEBA_FORMULARIO');
    expect(fixture.nativeElement.textContent).not.toContain('Última actividad');
  });

  it('guarda solo datos descriptivos, conserva el estado actual y emite la familia persistida', async () => {
    const persistida = {
      ...familiaBase,
      famNombre: 'Nombre actualizado',
      famDescripcion: 'Descripción actualizada'
    };
    const emitSpy = vi.spyOn(component.guardada, 'emit');
    component.nombre = 'Nombre actualizado';
    component.descripcion = 'Descripción actualizada';
    service.obtenerFamiliaFormularioPorId.mockReturnValue(of(persistida));

    component.guardarCambios();
    await estabilizarVista();

    expect(service.actualizarFamiliaFormulario).toHaveBeenCalledWith(7, {
      famNombre: 'Nombre actualizado',
      famDescripcion: 'Descripción actualizada',
      famActivo: true
    });
    expect(service.activarFamiliaFormulario).not.toHaveBeenCalled();
    expect(service.desactivarFamiliaFormulario).not.toHaveBeenCalled();
    expect(emitSpy).toHaveBeenCalledWith(persistida);
  });

  it('bloquea la desactivación en UI cuando existe versión vigente', async () => {
    service.obtenerFamiliaFormularioPorId.mockReturnValue(of({ ...familiaBase, tieneVersionVigente: true }));
    await cambiarFamilia(8);

    expect(component.puedeDesactivar()).toBe(false);
    component.confirmarDesactivar();
    expect(service.desactivarFamiliaFormulario).not.toHaveBeenCalled();
    expect(component.error()).toContain('versión publicada vigente');
  });

  it('bloquea eliminación cuando la familia contiene versiones', async () => {
    await estabilizarVista();

    expect(component.puedeEliminar()).toBe(false);
    component.detalle.set({ ...familiaBase });
    component.cargando.set(false);
    fixture.detectChanges();

    const boton = fixture.nativeElement.querySelector('[data-ui-fam-edit-action="eliminar"]') as HTMLButtonElement | null;
    expect(boton).not.toBeNull();
    expect(boton?.disabled).toBe(true);
    expect(fixture.nativeElement.textContent).toContain('La familia contiene versiones y no puede eliminarse.');
  });

  it('habilita eliminación segura cuando no existen versiones', async () => {
    service.obtenerFamiliaFormularioPorId.mockReturnValue(of({ ...familiaBase, totalVersiones: 0 }));
    await cambiarFamilia(9);

    expect(component.puedeEliminar()).toBe(true);
  });

  it('elimina una familia sin versiones mediante confirmación y emite el evento de cierre de ciclo', async () => {
    const eliminable: FamiliaFormularioDto = {
      ...familiaBase,
      famId: 9,
      totalVersiones: 0
    };
    service.obtenerFamiliaFormularioPorId.mockReturnValue(of(eliminable));
    await cambiarFamilia(9);
    const emitSpy = vi.spyOn(component.eliminada, 'emit');

    component.confirmarEliminar();

    await vi.waitFor(() => {
      expect(service.eliminarFamiliaFormulario).toHaveBeenCalledWith(9);
    });
    expect(component.operando()).toBe(false);
    expect(emitSpy).toHaveBeenCalledWith(eliminable);
  });

  it('habilita activar solo para una familia inactiva', async () => {
    service.obtenerFamiliaFormularioPorId.mockReturnValue(of({ ...familiaBase, famActivo: false }));
    await cambiarFamilia(10);

    expect(component.puedeActivar()).toBe(true);
    expect(component.puedeDesactivar()).toBe(false);
  });

  it('activa mediante su endpoint dedicado y emite el estado ACTIVADA', async () => {
    const inactiva: FamiliaFormularioDto = {
      ...familiaBase,
      famId: 10,
      famActivo: false
    };
    const activada: FamiliaFormularioDto = {
      ...inactiva,
      famActivo: true
    };
    service.obtenerFamiliaFormularioPorId.mockReturnValue(of(inactiva));
    await cambiarFamilia(10);
    service.obtenerFamiliaFormularioPorId.mockReturnValue(of(activada));
    const emitSpy = vi.spyOn(component.estadoCambiado, 'emit');

    component['cambiarEstado']('ACTIVADA');
    await estabilizarVista();

    expect(service.activarFamiliaFormulario).toHaveBeenCalledWith(10);
    expect(service.desactivarFamiliaFormulario).not.toHaveBeenCalled();
    expect(component.detalle()?.famActivo).toBe(true);
    expect(component.mensaje()).toBe('Familia activada correctamente.');
    expect(emitSpy).toHaveBeenCalledWith({ familia: activada, accion: 'ACTIVADA' });
  });

  it('ejecuta la desactivación mediante su endpoint dedicado, conserva el borrador y emite DESACTIVADA', async () => {
    const desactivada = { ...familiaBase, famActivo: false };
    const emitSpy = vi.spyOn(component.estadoCambiado, 'emit');
    component.nombre = 'Nombre aún no guardado';
    component.descripcion = 'Borrador local';
    service.obtenerFamiliaFormularioPorId.mockReturnValue(of(desactivada));

    component['cambiarEstado']('DESACTIVADA');
    await estabilizarVista();

    expect(service.desactivarFamiliaFormulario).toHaveBeenCalledWith(7);
    expect(service.actualizarFamiliaFormulario).not.toHaveBeenCalled();
    expect(component.detalle()?.famActivo).toBe(false);
    expect(component.nombre).toBe('Nombre aún no guardado');
    expect(component.descripcion).toBe('Borrador local');
    expect(emitSpy).toHaveBeenCalledWith({ familia: desactivada, accion: 'DESACTIVADA' });
  });

  it('conserva el modal abierto al pulsar Escape', () => {
    const emitSpy = vi.spyOn(component.cerrar, 'emit');
    const event = new KeyboardEvent('keydown', { key: 'Escape', cancelable: true });

    component.manejarTecladoDialogo(event);

    expect(event.defaultPrevented).toBe(true);
    expect(emitSpy).not.toHaveBeenCalled();
  });

  it('impide mutaciones cuando el usuario no es Administrador', async () => {
    auth.tieneRol.mockReturnValue(false);
    fixture.destroy();
    fixture = TestBed.createComponent(FamiliaEditarModalComponent);
    component = fixture.componentInstance;
    await cambiarFamilia(7);

    component.nombre = 'Intento';
    component.guardarCambios();

    expect(service.actualizarFamiliaFormulario).not.toHaveBeenCalled();
    expect(component.puedeActivar()).toBe(false);
    expect(component.puedeDesactivar()).toBe(false);
    expect(component.puedeEliminar()).toBe(false);
  });

  it('expone el mensaje del backend cuando falla el guardado', async () => {
    service.actualizarFamiliaFormulario.mockReturnValue(throwError(() => ({
      error: { detail: 'Conflicto de actualización.' }
    })));
    component.nombre = 'Nombre con cambio';

    component.guardarCambios();
    await estabilizarVista();

    expect(component.error()).toBe('Conflicto de actualización.');
  });

  it('UI-FAM.QA trata una familia ya activa como no activable y no duplica la operación', () => {
    expect(component.detalle()?.famActivo).toBe(true);
    expect(component.puedeActivar()).toBe(false);

    component.confirmarActivar();

    expect(service.activarFamiliaFormulario).not.toHaveBeenCalled();
  });

  it('UI-FAM.QA detecta por separado cambios de nombre y descripción', async () => {
    expect(component.hayCambios()).toBe(false);

    component.nombre = 'Solo nombre actualizado';
    expect(component.hayCambios()).toBe(true);

    await cambiarFamilia(7);
    component.descripcion = 'Solo descripción actualizada';
    expect(component.hayCambios()).toBe(true);
  });

  it('UI-FAM.QA protege doble clic durante una operación de ciclo de vida', async () => {
    const inactiva = { ...familiaBase, famActivo: false };
    const pendiente = new Subject<boolean>();
    service.obtenerFamiliaFormularioPorId.mockReturnValue(of(inactiva));
    await cambiarFamilia(10);
    service.activarFamiliaFormulario.mockReturnValue(pendiente);

    component['cambiarEstado']('ACTIVADA');
    component['cambiarEstado']('ACTIVADA');

    expect(component.operando()).toBe(true);
    expect(service.activarFamiliaFormulario).toHaveBeenCalledTimes(1);

    service.obtenerFamiliaFormularioPorId.mockReturnValue(of({ ...inactiva, famActivo: true }));
    pendiente.next(true);
    pendiente.complete();
    await estabilizarVista();

    expect(component.operando()).toBe(false);
  });

  it('UI-FAM.QA conserva contrato responsive desktop y resolución reducida', async () => {
    component.detalle.set({ ...familiaBase });
    component.cargando.set(false);
    fixture.detectChanges();
    await estabilizarVista();

    const dialog = fixture.nativeElement.querySelector('[data-ui-fam-edit="modal"]') as HTMLElement | null;
    const card = dialog?.querySelector('.modal-container-card') as HTMLElement | null;
    const grid = dialog?.querySelector('div.grid.grid-cols-1') as HTMLElement | null;

    expect(card?.classList.contains('modal-container-card')).toBe(true);
    expect(card?.classList.contains('modal-size-xl')).toBe(true);
    expect(grid).not.toBeNull();
    expect(grid?.className).toContain('grid-cols-1');
    expect(grid?.className).toContain('lg:grid-cols-');
  });

  it('limpia el estado cuando se recibe un identificador inválido', async () => {
    fixture.componentRef.setInput('familiaId', 0);
    fixture.detectChanges();
    await estabilizarVista();

    expect(component.detalle()).toBeNull();
    expect(component.cargando()).toBe(false);
    expect(component.noEncontrada()).toBe(false);
    expect(component.error()).toBeNull();
  });

  it('valida nombre y descripción antes de enviar cambios', () => {
    component.nombre = '   ';
    component.guardarCambios();
    expect(component.error()).toBe('El nombre de la familia es obligatorio.');

    component.nombre = 'x'.repeat(151);
    component.guardarCambios();
    expect(component.error()).toBe('El nombre no puede superar los 150 caracteres.');

    component.nombre = 'Nombre válido';
    component.descripcion = 'x'.repeat(501);
    component.guardarCambios();
    expect(component.error()).toBe('La descripción no puede superar los 500 caracteres.');
    expect(service.actualizarFamiliaFormulario).not.toHaveBeenCalled();
  });

  it('rechaza una respuesta persistida que no coincide con el cambio enviado', async () => {
    component.nombre = 'Nombre nuevo';
    service.actualizarFamiliaFormulario.mockReturnValue(of(true));
    service.obtenerFamiliaFormularioPorId.mockReturnValue(of({ ...familiaBase, famNombre: 'Nombre antiguo' }));

    component.guardarCambios();
    await estabilizarVista();

    expect(component.error()).toContain('no coincide con los cambios enviados');
    expect(component.guardando()).toBe(false);
  });

  it('expone el error cuando falla la verificación posterior al guardado', async () => {
    component.nombre = 'Nombre nuevo';
    service.actualizarFamiliaFormulario.mockReturnValue(of(true));
    service.obtenerFamiliaFormularioPorId.mockReturnValue(throwError(() => ({ error: { detail: 'No se pudo leer.' } })));

    component.guardarCambios();
    await estabilizarVista();

    expect(component.error()).toBe('No se pudo leer.');
    expect(component.guardando()).toBe(false);
  });

  it('maneja familia inexistente y error 404 al recargar por ID', async () => {
    service.obtenerFamiliaFormularioPorId.mockReturnValue(of(null));
    await cambiarFamilia(20);
    expect(component.noEncontrada()).toBe(true);
    expect(component.detalle()).toBeNull();

    service.obtenerFamiliaFormularioPorId.mockReturnValue(throwError(() => ({ status: 404 })));
    await cambiarFamilia(21);
    expect(component.noEncontrada()).toBe(true);
    expect(component.detalle()).toBeNull();
  });

  it('recarga y reporta el error cuando falla una operación de ciclo de vida', async () => {
    service.activarFamiliaFormulario.mockReturnValue(throwError(() => ({ error: { detail: 'No autorizado.' } })));
    component['cambiarEstado']('ACTIVADA');
    await estabilizarVista();

    expect(component.operando()).toBe(false);
    expect(component.error()).toBe('No autorizado.');
    expect(service.obtenerFamiliaFormularioPorId).toHaveBeenCalledWith(7);
  });
});
