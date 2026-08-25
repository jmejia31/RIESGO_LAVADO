import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormBuilderToolbarComponent } from './form-builder-toolbar.component';
import { EstadoFormulario } from '../../../models/matrices-riesgos.models';
import { describe, it, expect, beforeEach } from 'vitest';

describe('FormBuilderToolbarComponent (UI-FORM.5)', () => {
  let component: FormBuilderToolbarComponent;
  let fixture: ComponentFixture<FormBuilderToolbarComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [FormBuilderToolbarComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(FormBuilderToolbarComponent);
    component = fixture.componentInstance;
    component.versionCodigo = 'V1.0';
    component.versionNumero = 1;
    component.seccionesCount = 2;
    component.catalogosCount = 1;
  });

  describe('1. Renderizado de Badges y Estados Oficiales', () => {
    const matrizEstados: Array<{ estado: EstadoFormulario; soloLectura: boolean; etiquetaEsperada: string }> = [
      { estado: 'DRAFT', soloLectura: false, etiquetaEsperada: 'BORRADOR' },
      { estado: 'DRAFT', soloLectura: true, etiquetaEsperada: 'BORRADOR · SOLO LECTURA' },
      { estado: 'IN_REVIEW', soloLectura: true, etiquetaEsperada: 'EN REVISIÓN · SOLO LECTURA' },
      { estado: 'APPROVED', soloLectura: true, etiquetaEsperada: 'APROBADA · SOLO LECTURA' },
      { estado: 'PUBLISHED', soloLectura: true, etiquetaEsperada: 'PUBLICADA · SOLO LECTURA' },
      { estado: 'RETIRED', soloLectura: true, etiquetaEsperada: 'RETIRADA · SOLO LECTURA' },
      { estado: 'ARCHIVED', soloLectura: true, etiquetaEsperada: 'ARCHIVADA · SOLO LECTURA' }
    ];

    matrizEstados.forEach(({ estado, soloLectura, etiquetaEsperada }) => {
      it(`renderiza badge "${etiquetaEsperada}" para estado ${estado} (soloLectura: ${soloLectura})`, () => {
        component.estadoVersion = estado;
        component.soloLectura = soloLectura;
        fixture.detectChanges();

        expect(component.estadoEtiqueta).toBe(etiquetaEsperada);
        const el = fixture.nativeElement as HTMLElement;
        expect(el.textContent).toContain(etiquetaEsperada);
      });
    });

    it('usa fallback de modo borrador cuando no hay estadoVersion definido', () => {
      component.estadoVersion = undefined;
      component.soloLectura = false;
      fixture.detectChanges();
      expect(component.estadoEtiqueta).toBe('Modo Borrador');
    });

    it('usa fallback de modo solo lectura cuando no hay estadoVersion definido y soloLectura es true', () => {
      component.estadoVersion = undefined;
      component.soloLectura = true;
      fixture.detectChanges();
      expect(component.estadoEtiqueta).toBe('Modo Solo Lectura');
    });
  });

  describe('2. Modo Borrador Editable (Acciones y Botones)', () => {
    it('muestra botón Guardar Borrador y emite guardar al hacer click', () => {
      component.soloLectura = false;
      component.estadoVersion = 'DRAFT';
      component.puedePublicar = true;
      component.procesando = false;
      fixture.detectChanges();

      let emitido = false;
      component.guardar.subscribe(() => emitido = true);

      const el = fixture.nativeElement as HTMLElement;
      const btnGuardar = el.querySelector('#btn-guardar-builder') as HTMLButtonElement;
      expect(btnGuardar).toBeTruthy();
      expect(btnGuardar.textContent).toContain('Guardar Borrador');
      expect(btnGuardar.disabled).toBe(false);

      btnGuardar.click();
      expect(emitido).toBe(true);
    });

    it('muestra botón Publicar Versión cuando puedePublicar es true y emite publicar', () => {
      component.soloLectura = false;
      component.estadoVersion = 'DRAFT';
      component.puedePublicar = true;
      component.procesando = false;
      fixture.detectChanges();

      let emitido = false;
      component.publicar.subscribe(() => emitido = true);

      const el = fixture.nativeElement as HTMLElement;
      const btnPublicar = el.querySelector('#btn-publicar-builder') as HTMLButtonElement;
      expect(btnPublicar).toBeTruthy();
      expect(btnPublicar.textContent).toContain('Publicar Versión');
      expect(btnPublicar.disabled).toBe(false);

      btnPublicar.click();
      expect(emitido).toBe(true);
    });

    it('oculta botón Publicar Versión cuando puedePublicar es false', () => {
      component.soloLectura = false;
      component.estadoVersion = 'DRAFT';
      component.puedePublicar = false;
      component.procesando = false;
      fixture.detectChanges();

      const el = fixture.nativeElement as HTMLElement;
      expect(el.querySelector('#btn-publicar-builder')).toBeNull();
    });

    it('muestra botón de nueva sección en vista secciones', () => {
      component.soloLectura = false;
      component.vistaActiva = 'secciones';
      fixture.detectChanges();

      const el = fixture.nativeElement as HTMLElement;
      expect(el.querySelector('#btn-agregar-seccion')).toBeTruthy();
    });

    it('muestra botón de nuevo catálogo en vista catalogos', () => {
      component.soloLectura = false;
      component.vistaActiva = 'catalogos';
      fixture.detectChanges();

      const el = fixture.nativeElement as HTMLElement;
      expect(el.querySelector('#btn-nuevo-catalogo-header')).toBeTruthy();
    });
  });

  describe('3. Modo Solo Lectura (Bloqueo Estricto de Acciones)', () => {
    it('no renderiza botones de Guardar Borrador, Publicar, Nueva Sección ni Nuevo Catálogo', () => {
      component.soloLectura = true;
      component.estadoVersion = 'PUBLISHED';
      component.puedePublicar = false;
      fixture.detectChanges();

      const el = fixture.nativeElement as HTMLElement;
      expect(el.querySelector('#btn-guardar-builder')).toBeNull();
      expect(el.querySelector('#btn-publicar-builder')).toBeNull();
      expect(el.querySelector('#btn-agregar-seccion')).toBeNull();
      expect(el.querySelector('#btn-nuevo-catalogo-header')).toBeNull();
    });

    it('permite cerrar y alternar vistas en solo lectura', () => {
      component.soloLectura = true;
      component.estadoVersion = 'PUBLISHED';
      component.puedePublicar = false;
      fixture.detectChanges();

      let cerrado = false;
      component.cerrar.subscribe(() => cerrado = true);

      let vistaCambiada: string | null = null;
      component.cambiarVista.subscribe(v => vistaCambiada = v);

      const el = fixture.nativeElement as HTMLElement;
      const btnCerrar = el.querySelector('button[title="Cerrar constructor"]') as HTMLButtonElement;
      expect(btnCerrar).toBeTruthy();
      btnCerrar.click();
      expect(cerrado).toBe(true);

      const btnCatalogos = el.querySelector('#tab-vista-catalogos') as HTMLButtonElement;
      btnCatalogos.click();
      expect(vistaCambiada).toBe('catalogos');
    });
  });

  describe('4. Estado Procesando (Doble Envío)', () => {
    it('deshabilita botones Guardar Borrador y Publicar Versión cuando procesando es true', () => {
      component.soloLectura = false;
      component.estadoVersion = 'DRAFT';
      component.puedePublicar = true;
      component.procesando = true;
      fixture.detectChanges();

      const el = fixture.nativeElement as HTMLElement;
      const btnGuardar = el.querySelector('#btn-guardar-builder') as HTMLButtonElement;
      const btnPublicar = el.querySelector('#btn-publicar-builder') as HTMLButtonElement;

      expect(btnGuardar.disabled).toBe(true);
      expect(btnGuardar.textContent).toContain('Guardando...');

      expect(btnPublicar.disabled).toBe(true);
      expect(btnPublicar.textContent).toContain('Publicando...');
    });
  });
});
