import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { Subject, of, throwError } from 'rxjs';
import { MatricesRiesgosService } from '../../data-access/matrices-riesgos.service';
import { FamiliaCrearModalComponent } from './familia-crear-modal.component';

describe('FamiliaCrearModalComponent — UI-FAM.3', () => {
  let fixture: ComponentFixture<FamiliaCrearModalComponent>;
  let component: FamiliaCrearModalComponent;
  let service: MatricesRiesgosService;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [FamiliaCrearModalComponent, HttpClientTestingModule],
      providers: [MatricesRiesgosService]
    }).compileComponents();

    fixture = TestBed.createComponent(FamiliaCrearModalComponent);
    component = fixture.componentInstance;
    service = TestBed.inject(MatricesRiesgosService);
    fixture.detectChanges();
  });

  it('1. renderiza el prototipo aprobado con resumen, identificación y reglas', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    const texto = compiled.textContent ?? '';

    expect(compiled.querySelector('[data-ui-fam-create="modal"]')).not.toBeNull();
    expect(texto).toContain('Nueva Familia de Formularios');
    expect(texto).toContain('Paso actual:');
    expect(texto).toContain('Identificación');
    expect(texto).toContain('Requiere:');
    expect(texto).toContain('Código y nombre');
    expect(texto).toContain('Auditoría:');
    expect(texto).toContain('Activa');
    expect(compiled.querySelector('[data-ui-fam-create="reglas"]')).not.toBeNull();
  });

  it('2. contiene exclusivamente los tres campos del contrato de creación', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    const controles = Array.from(compiled.querySelectorAll('input, textarea'));
    const nombres = controles.map(control => control.getAttribute('name'));

    expect(nombres).toEqual(['codigo', 'nombre', 'descripcion']);
    expect(compiled.querySelector('#fam-activo')).toBeNull();
  });

  it('3. aplica límites UX de 50, 150 y 500 caracteres', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('#crear-fam-codigo')?.getAttribute('maxlength')).toBe('50');
    expect(compiled.querySelector('#crear-fam-nombre')?.getAttribute('maxlength')).toBe('150');
    expect(compiled.querySelector('#crear-fam-descripcion')?.getAttribute('maxlength')).toBe('500');
  });

  it('4. no envía si código o nombre quedan vacíos después de trim', () => {
    const spyCrear = vi.spyOn(service, 'crearFamiliaFormulario').mockReturnValue(of(1));
    component.codigo = '   ';
    component.nombre = '   ';

    component.guardar();

    expect(spyCrear).not.toHaveBeenCalled();
    expect(component.error()).toBe('El código de la familia es obligatorio.');
  });

  it('5. normaliza con trim y envía únicamente FamCodigo, FamNombre y FamDescripcion', () => {
    const spyCrear = vi.spyOn(service, 'crearFamiliaFormulario').mockReturnValue(of(25));
    const spyCreada = vi.spyOn(component.creada, 'emit');
    component.codigo = '  PRUEBA_FORMULARIO  ';
    component.nombre = '  Prueba de Formulario  ';
    component.descripcion = '  Familia institucional de prueba.  ';

    component.guardar();

    expect(spyCrear).toHaveBeenCalledWith({
      famCodigo: 'PRUEBA_FORMULARIO',
      famNombre: 'Prueba de Formulario',
      famDescripcion: 'Familia institucional de prueba.'
    });
    expect(spyCreada).toHaveBeenCalledWith({ id: 25, nombre: 'Prueba de Formulario' });
  });

  it('6. evita doble submit mientras la creación está pendiente', () => {
    const pendiente = new Subject<number>();
    const spyCrear = vi.spyOn(service, 'crearFamiliaFormulario').mockReturnValue(pendiente);
    component.codigo = 'FAMILIA_UNICA';
    component.nombre = 'Familia única';

    component.guardar();
    component.guardar();

    expect(component.guardando()).toBe(true);
    expect(spyCrear).toHaveBeenCalledTimes(1);
    pendiente.next(10);
    pendiente.complete();
    expect(component.guardando()).toBe(false);
  });

  it('7. conserva el modal y presenta el detalle cuando backend rechaza la creación', () => {
    vi.spyOn(service, 'crearFamiliaFormulario').mockReturnValue(throwError(() => ({
      error: { detail: 'Ya existe una familia con ese código.' }
    })));
    component.codigo = 'DUPLICADA';
    component.nombre = 'Familia duplicada';

    component.guardar();
    fixture.detectChanges();

    expect(component.guardando()).toBe(false);
    expect(component.error()).toBe('Ya existe una familia con ese código.');
    expect((fixture.nativeElement as HTMLElement).querySelector('[data-ui-fam-create-state="error"]')).not.toBeNull();
  });

  it('8. Escape cierra cuando es seguro y no cierra durante el submit', () => {
    const spyCerrar = vi.spyOn(component.cerrar, 'emit');
    const evento = new KeyboardEvent('keydown', { key: 'Escape' });

    component.manejarTecladoDialogo(evento);
    expect(spyCerrar).toHaveBeenCalledTimes(1);

    component.guardando.set(true);
    component.manejarTecladoDialogo(new KeyboardEvent('keydown', { key: 'Escape' }));
    expect(spyCerrar).toHaveBeenCalledTimes(1);
  });
});
