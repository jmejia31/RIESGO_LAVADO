import type { Type } from '@angular/core';
import { TestBed, ComponentFixture } from '@angular/core/testing';
import { FormsModule } from '@angular/forms';
import { FormBuilderCanvasV2Component } from './form-builder-canvas.component';
import { FormBuilderInspectorV2Component } from './form-builder-inspector.component';
import { FormBuilderPaletteV2Component } from './form-builder-palette.component';
import { FormBuilderStatusbarV2Component } from './form-builder-statusbar.component';
import { FormBuilderToolbarV2Component } from './form-builder-toolbar.component';
import { FormBuilderWorkspaceV2Component } from './form-builder-workspace.component';
import { crearEstadoInicialWorkspace, FORM_BUILDER_UI_V2_CONTRACT } from './form-builder-workspace.types';

describe('Form Builder UI v2 - scaffold de migración', () => {
  const componentes: Type<unknown>[] = [
    FormBuilderWorkspaceV2Component,
    FormBuilderToolbarV2Component,
    FormBuilderPaletteV2Component,
    FormBuilderCanvasV2Component,
    FormBuilderInspectorV2Component,
    FormBuilderStatusbarV2Component
  ];

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [...componentes, FormsModule]
    }).compileComponents();
  });

  it('mantiene el contrato UI separado del contrato JSON persistido', () => {
    const estado = crearEstadoInicialWorkspace();

    expect(FORM_BUILDER_UI_V2_CONTRACT).toBe('UI-FORM-V2');
    expect(estado).toEqual({
      view: 'editor',
      inspectorTab: 'general',
      paletteCollapsed: false,
      inspectorCollapsed: false,
      dirty: false,
      saving: false
    });
  });

  it('compila todos los shells presentacionales sin dependencias de HTTP ni backend', () => {
    for (const tipo of componentes) {
      const fixture = TestBed.createComponent(tipo);
      fixture.detectChanges();
      expect(fixture.componentInstance).toBeTruthy();
    }
  });

  describe('PaletteV2', () => {
    it('recibe controles via @Input y emite selección via @Output', () => {
      const fixture = TestBed.createComponent(FormBuilderPaletteV2Component);
      const component = fixture.componentInstance;
      const controles = [
        {
          tipo: 'texto' as const,
          etiqueta: 'Texto Libre',
          descripcion: 'Campo de texto',
          icono: 'T',
          categoria: 'basico' as const,
          requiereCatalogo: false,
          requiereOpciones: false,
          requiereFormula: false
        }
      ];
      component.tiposControles = controles;
      component.seccionActivaId = 'sec_1';
      component.soloLectura = false;
      let controlEmitido: unknown = null;
      component.agregarCampo.subscribe(control => controlEmitido = control);
      fixture.detectChanges();

      const boton = (fixture.nativeElement as HTMLElement).querySelector('button');
      boton?.click();

      expect(controlEmitido).toEqual(controles[0]);
    });
  });

  describe('InspectorV2', () => {
    it('muestra empty-state cuando campoActivo es null', () => {
      const fixture = TestBed.createComponent(FormBuilderInspectorV2Component);
      const component = fixture.componentInstance;
      component.campoActivo = null;
      fixture.detectChanges();

      const el = fixture.nativeElement as HTMLElement;
      expect(el.textContent).toContain('Seleccione un campo');
    });

    it('muestra propiedades cuando hay campo activo', () => {
      const fixture = TestBed.createComponent(FormBuilderInspectorV2Component);
      const component = fixture.componentInstance;
      component.campoActivo = {
        id: 'cmp_1',
        clave: 'campo_test',
        etiqueta: 'Campo Test',
        tipo: 'texto',
        obligatorio: true,
        soloLectura: false,
        anchoColumnas: 1
      };
      component.tiposControles = [
        {
          tipo: 'texto' as const,
          etiqueta: 'Texto Libre',
          descripcion: 'Campo de texto',
          icono: 'T',
          categoria: 'basico' as const,
          requiereCatalogo: false,
          requiereOpciones: false,
          requiereFormula: false
        }
      ];
      fixture.detectChanges();

      const el = fixture.nativeElement as HTMLElement;
      expect(el.textContent).toContain('Propiedades del Campo');
      expect(el.textContent).toContain('Clave Técnica');
    });
  });

  describe('CanvasV2', () => {
    it('muestra estado vacío sin secciones', () => {
      const fixture = TestBed.createComponent(FormBuilderCanvasV2Component);
      const component = fixture.componentInstance;
      component.secciones = [];
      fixture.detectChanges();

      const el = fixture.nativeElement as HTMLElement;
      expect(el.textContent).toContain('No hay secciones');
    });

    it('renderiza secciones con campos', () => {
      const fixture = TestBed.createComponent(FormBuilderCanvasV2Component);
      const component = fixture.componentInstance;
      component.secciones = [{
        id: 'sec_1',
        clave: 'seccion_1',
        titulo: 'Sección de Prueba',
        orden: 1,
        columnasPorFila: 2,
        campos: [{
          id: 'cmp_1',
          clave: 'campo_1',
          etiqueta: 'Campo Texto',
          tipo: 'texto',
          obligatorio: true,
          soloLectura: false,
          anchoColumnas: 1
        }]
      }];
      component.seccionActivaId = 'sec_1';
      fixture.detectChanges();

      const el = fixture.nativeElement as HTMLElement;
      expect(el.textContent).toContain('Campo Texto');
    });
  });

  describe('StatusbarV2', () => {
    it('muestra versión y modo borrador', () => {
      const fixture = TestBed.createComponent(FormBuilderStatusbarV2Component);
      const component = fixture.componentInstance;
      component.versionCodigo = 'V2.0';
      component.soloLectura = false;
      component.seccionesCount = 3;
      component.catalogosCount = 2;
      fixture.detectChanges();

      const el = fixture.nativeElement as HTMLElement;
      expect(el.textContent).toContain('V2.0');
      expect(el.textContent).toContain('Modo borrador');
      expect(el.textContent).toContain('3 sección(es)');
      expect(el.textContent).toContain('2 catálogo(s)');
    });

    it('muestra modo solo lectura', () => {
      const fixture = TestBed.createComponent(FormBuilderStatusbarV2Component);
      const component = fixture.componentInstance;
      component.soloLectura = true;
      fixture.detectChanges();

      const el = fixture.nativeElement as HTMLElement;
      expect(el.textContent).toContain('Solo lectura');
    });
  });

  describe('ToolbarV2', () => {
    it('emite evento cerrar', () => {
      const fixture = TestBed.createComponent(FormBuilderToolbarV2Component);
      const component = fixture.componentInstance;
      let cerrarEmitted = false;
      component.cerrar.subscribe(() => cerrarEmitted = true);

      component.cerrar.emit();
      expect(cerrarEmitted).toBe(true);
    });
  });

  describe('WorkspaceV2 shell', () => {
    it('contiene el contenedor con atributo de contrato UI-FORM-V2', () => {
      const fixture = TestBed.createComponent(FormBuilderWorkspaceV2Component);
      fixture.detectChanges();

      const el = fixture.nativeElement as HTMLElement;
      const section = el.querySelector('[data-ui-contract="UI-FORM-V2"]');
      expect(section).toBeTruthy();
    });

    it('delega la semántica modal al dialog contenedor para evitar diálogos anidados', () => {
      const fixture = TestBed.createComponent(FormBuilderWorkspaceV2Component);
      fixture.detectChanges();

      const el = fixture.nativeElement as HTMLElement;
      const section = el.querySelector('[data-ui-contract="UI-FORM-V2"]');
      expect(section).toBeTruthy();
      expect(section?.hasAttribute('aria-modal')).toBe(false);
      expect(section?.getAttribute('role')).not.toBe('dialog');
      expect(section?.getAttribute('aria-label')).toContain('Espacio de trabajo');
    });
  });
});
