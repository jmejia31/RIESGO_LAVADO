import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { describe, expect, it, beforeEach, vi } from 'vitest';
import { AuthService } from '../../../../../core/auth/auth.service';
import { MatricesRiesgosService } from '../../data-access/matrices-riesgos.service';
import { FamiliaFormularioDto } from '../../models/matrices-riesgos.models';
import { FamiliaEditarModalComponent } from './familia-editar-modal.component';

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

describe('FamiliaEditarModalComponent — UI-FAM.4', () => {
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
    const codigo = fixture.nativeElement.querySelector('#editar-fam-codigo') as HTMLInputElement;
    expect(codigo).not.toBeNull();
    expect(codigo.readOnly).toBe(true);
    expect(codigo.value).toBe('PRUEBA_FORMULARIO');
    expect(fixture.nativeElement.textContent).not.toContain('Última actividad');
  });

  it('guarda solo datos descriptivos y conserva el estado actual en el DTO', async () => {
    component.nombre = 'Nombre actualizado';
    component.descripcion = 'Descripción actualizada';
    service.obtenerFamiliaFormularioPorId.mockReturnValue(of({
      ...familiaBase,
      famNombre: 'Nombre actualizado',
      famDescripcion: 'Descripción actualizada'
    }));

    component.guardarCambios();
    await estabilizarVista();

    expect(service.actualizarFamiliaFormulario).toHaveBeenCalledWith(7, {
      famNombre: 'Nombre actualizado',
      famDescripcion: 'Descripción actualizada',
      famActivo: true
    });
    expect(service.activarFamiliaFormulario).not.toHaveBeenCalled();
    expect(service.desactivarFamiliaFormulario).not.toHaveBeenCalled();
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
    const boton = fixture.nativeElement.querySelector('[data-ui-fam-edit-action="eliminar"]') as HTMLButtonElement;
    expect(boton).not.toBeNull();
    expect(boton.disabled).toBe(true);
    expect(fixture.nativeElement.textContent).toContain('La familia contiene versiones y no puede eliminarse.');
  });

  it('habilita eliminación segura cuando no existen versiones', async () => {
    service.obtenerFamiliaFormularioPorId.mockReturnValue(of({ ...familiaBase, totalVersiones: 0 }));
    await cambiarFamilia(9);

    expect(component.puedeEliminar()).toBe(true);
  });

  it('habilita activar solo para una familia inactiva', async () => {
    service.obtenerFamiliaFormularioPorId.mockReturnValue(of({ ...familiaBase, famActivo: false }));
    await cambiarFamilia(10);

    expect(component.puedeActivar()).toBe(true);
    expect(component.puedeDesactivar()).toBe(false);
  });

  it('ejecuta la desactivación mediante su endpoint dedicado y conserva el borrador descriptivo', async () => {
    component.nombre = 'Nombre aún no guardado';
    component.descripcion = 'Borrador local';
    service.obtenerFamiliaFormularioPorId.mockReturnValue(of({ ...familiaBase, famActivo: false }));

    component['cambiarEstado']('DESACTIVADA');
    await estabilizarVista();

    expect(service.desactivarFamiliaFormulario).toHaveBeenCalledWith(7);
    expect(service.actualizarFamiliaFormulario).not.toHaveBeenCalled();
    expect(component.detalle()?.famActivo).toBe(false);
    expect(component.nombre).toBe('Nombre aún no guardado');
    expect(component.descripcion).toBe('Borrador local');
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
});
