import type { Type } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { FormsModule } from '@angular/forms';
import { FormBuilderCanvasComponent } from './canvas/form-builder-canvas.component';
import { FormBuilderInspectorComponent } from './inspector/form-builder-inspector.component';
import { FormBuilderPaletteComponent } from './palette/form-builder-palette.component';
import { FormBuilderStatusbarComponent } from './statusbar/form-builder-statusbar.component';
import { FormBuilderToolbarComponent } from './toolbar/form-builder-toolbar.component';

describe('Form Builder — componentes presentacionales productivos', () => {
  const componentes: Type<unknown>[] = [
    FormBuilderToolbarComponent,
    FormBuilderPaletteComponent,
    FormBuilderCanvasComponent,
    FormBuilderInspectorComponent,
    FormBuilderStatusbarComponent
  ];

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [...componentes, FormsModule] }).compileComponents();
  });

  it('compila las cinco regiones sin dependencias HTTP ni un Workspace paralelo', () => {
    for (const tipo of componentes) {
      const fixture = TestBed.createComponent(tipo);
      fixture.detectChanges();
      expect(fixture.componentInstance).toBeTruthy();
    }
  });

  it('la biblioteca emite la selección de un control en edición', () => {
    const fixture = TestBed.createComponent(FormBuilderPaletteComponent);
    const component = fixture.componentInstance;
    const controles = [{
      tipo: 'texto' as const,
      etiqueta: 'Texto Libre',
      descripcion: 'Campo de texto',
      icono: 'T',
      categoria: 'basico' as const,
      requiereCatalogo: false,
      requiereOpciones: false,
      requiereFormula: false
    }];
    component.tiposControles = controles;
    component.seccionActivaId = 'sec_1';
    let controlEmitido: unknown = null;
    component.agregarCampo.subscribe(control => controlEmitido = control);
    fixture.detectChanges();

    (fixture.nativeElement as HTMLElement).querySelector('.palette-card')?.dispatchEvent(new MouseEvent('click', { bubbles: true }));
    expect(controlEmitido).toEqual(controles[0]);
  });

  it('en solo lectura la región izquierda muestra estructura y no controles para agregar', () => {
    const fixture = TestBed.createComponent(FormBuilderPaletteComponent);
    const component = fixture.componentInstance;
    component.soloLectura = true;
    component.secciones = [{
      id: 'sec_1', clave: 'general', titulo: 'Información general', orden: 1, columnasPorFila: 2,
      campos: [{ id: 'cmp_1', clave: 'nombre', etiqueta: 'Nombre', tipo: 'texto', obligatorio: true, soloLectura: false, anchoColumnas: 1 }]
    }];
    fixture.detectChanges();

    const texto = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(texto).toContain('Estructura');
    expect(texto).toContain('Solo lectura');
    expect(texto).toContain('Información general');
    expect(texto).toContain('Nombre');
    expect(texto).not.toContain('Biblioteca de campos');
  });

  it('el inspector muestra empty-state cuando no existe campo activo', () => {
    const fixture = TestBed.createComponent(FormBuilderInspectorComponent);
    fixture.componentInstance.campoActivo = null;
    fixture.detectChanges();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Seleccione un campo');
  });

  it('el inspector muestra únicamente propiedades existentes cuando hay selección', () => {
    const fixture = TestBed.createComponent(FormBuilderInspectorComponent);
    const component = fixture.componentInstance;
    component.campoActivo = {
      id: 'cmp_1', clave: 'campo_test', etiqueta: 'Campo Test', tipo: 'texto', obligatorio: true, soloLectura: false, anchoColumnas: 1
    };
    component.tiposControles = [{
      tipo: 'texto' as const,
      etiqueta: 'Texto Libre', descripcion: 'Campo de texto', icono: 'T', categoria: 'basico' as const,
      requiereCatalogo: false, requiereOpciones: false, requiereFormula: false
    }];
    fixture.detectChanges();

    const texto = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(texto).toContain('Propiedades del campo');
    expect(texto).toContain('Clave Técnica');
    expect(texto).toContain('Etiqueta Visible');
  });

  it('el canvas conserva el estado vacío', () => {
    const fixture = TestBed.createComponent(FormBuilderCanvasComponent);
    fixture.componentInstance.secciones = [];
    fixture.detectChanges();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('No hay secciones');
  });

  it('el canvas renderiza secciones y campos existentes', () => {
    const fixture = TestBed.createComponent(FormBuilderCanvasComponent);
    fixture.componentInstance.secciones = [{
      id: 'sec_1', clave: 'seccion_1', titulo: 'Sección de Prueba', orden: 1, columnasPorFila: 2,
      campos: [{ id: 'cmp_1', clave: 'campo_1', etiqueta: 'Campo Texto', tipo: 'texto', obligatorio: true, soloLectura: false, anchoColumnas: 1 }]
    }];
    fixture.componentInstance.seccionActivaId = 'sec_1';
    fixture.detectChanges();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Campo Texto');
  });

  it('la statusbar muestra solo metadatos reales de la versión y modo', () => {
    const fixture = TestBed.createComponent(FormBuilderStatusbarComponent);
    const component = fixture.componentInstance;
    component.versionCodigo = 'V2.0';
    component.soloLectura = false;
    component.seccionesCount = 3;
    component.catalogosCount = 2;
    fixture.detectChanges();

    const texto = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(texto).toContain('V2.0');
    expect(texto).toContain('Modo borrador');
    expect(texto).toContain('3 sección(es)');
    expect(texto).toContain('2 catálogo(s)');
    expect(texto).not.toContain('Todos los cambios guardados');
  });

  it('la toolbar conserva el evento accesible de cierre', () => {
    const fixture = TestBed.createComponent(FormBuilderToolbarComponent);
    let emitido = false;
    fixture.componentInstance.cerrar.subscribe(() => emitido = true);
    fixture.detectChanges();

    const cerrar = (fixture.nativeElement as HTMLElement).querySelector('[aria-label="Cerrar modal de constructor"]') as HTMLButtonElement | null;
    expect(cerrar).toBeTruthy();
    cerrar?.click();
    expect(emitido).toBe(true);
  });
});
