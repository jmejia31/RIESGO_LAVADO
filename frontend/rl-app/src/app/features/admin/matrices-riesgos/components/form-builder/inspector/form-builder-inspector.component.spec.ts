import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormsModule } from '@angular/forms';
import { FormBuilderInspectorComponent, InspectorCatalogoOption } from './form-builder-inspector.component';
import { CampoBuilderModel, TIPOS_CONTROLES_DISPONIBLES } from '../../../models/form-builder.models';

describe('FormBuilderInspectorComponent — UI-FORM.4 Inspector Profesional', () => {
  let component: FormBuilderInspectorComponent;
  let fixture: ComponentFixture<FormBuilderInspectorComponent>;

  const mockCatalogos: InspectorCatalogoOption[] = [
    { codigo: 'CAT_AREAS', nombre: 'Áreas Institucionales', cantidadElementos: 3 },
    { codigo: 'CAT_RIESGO', nombre: 'Niveles de Riesgo', cantidadElementos: 2 }
  ];

  const crearCampoMock = (tipo: any = 'texto'): CampoBuilderModel => ({
    id: 'cmp_test_1',
    clave: 'campo_uno',
    etiqueta: 'Campo Uno',
    descripcion: 'Descripción del campo',
    tipo,
    obligatorio: false,
    soloLectura: false,
    placeholder: 'Ingrese valor...',
    textoAyuda: 'Ayuda para el usuario',
    anchoColumnas: 2,
    codigoCatalogo: 'CAT_AREAS',
    opciones: ['Opción A', 'Opción B'],
    formula: 'val1 * val2'
  });

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [FormBuilderInspectorComponent, FormsModule]
    }).compileComponents();

    fixture = TestBed.createComponent(FormBuilderInspectorComponent);
    component = fixture.componentInstance;
    component.tiposControles = [...TIPOS_CONTROLES_DISPONIBLES];
    component.catalogosDisponibles = [...mockCatalogos];
    component.soloLectura = false;
  });

  describe('1. Empty State', () => {
    it('muestra empty-state cuando campoActivo es null y no renderiza el formulario de edición', () => {
      component.campoActivo = null;
      fixture.detectChanges();

      const el = fixture.nativeElement as HTMLElement;
      expect(el.textContent).toContain('Seleccione un campo en el lienzo para editar sus propiedades');
      expect(el.querySelector('input')).toBeNull();
      expect(el.querySelector('select')).toBeNull();
    });
  });

  describe('2. Grupo: GENERAL', () => {
    beforeEach(() => {
      component.campoActivo = crearCampoMock('texto');
      fixture.detectChanges();
    });

    it('renderiza etiqueta, clave, tipo y descripción', () => {
      const el = fixture.nativeElement as HTMLElement;
      const inputEtiqueta = el.querySelector('#prop-etiqueta-cmp_test_1') as HTMLInputElement;
      const inputClave = el.querySelector('#prop-clave-cmp_test_1') as HTMLInputElement;
      const selectTipo = el.querySelector('#prop-tipo-cmp_test_1') as HTMLSelectElement;
      const textareaDesc = el.querySelector('#prop-desc-cmp_test_1') as HTMLTextAreaElement;

      expect(inputEtiqueta.value).toBe('Campo Uno');
      expect(inputClave.value).toBe('campo_uno');
      expect(selectTipo.value).toBe('texto');
      expect(textareaDesc.value).toBe('Descripción del campo');
    });

    it('el selector de tipos contiene exactamente las 9 opciones oficiales', () => {
      const el = fixture.nativeElement as HTMLElement;
      const selectTipo = el.querySelector('#prop-tipo-cmp_test_1') as HTMLSelectElement;
      const opciones = Array.from(selectTipo.options).map(o => o.value).sort();
      const esperados = [
        'catalogo-multiple',
        'checkbox',
        'fecha',
        'formula',
        'numero',
        'radio',
        'selector-catalogo',
        'texto',
        'texto-largo'
      ].sort();

      expect(opciones).toEqual(esperados);
      expect(opciones.length).toBe(9);
    });

    it('cambiar tipo a "formula" asegura soloLectura = true de forma inmutable', () => {
      component.campoActivo!.soloLectura = false;
      let emitido = false;
      component.propiedadCambiada.subscribe(() => emitido = true);

      component.alCambiarTipo('formula');

      expect(component.campoActivo!.tipo).toBe('formula');
      expect(component.campoActivo!.soloLectura).toBe(true);
      expect(emitido).toBe(true);
    });

    it('cambiar tipo desde "formula" hacia otro no fuerza soloLectura a false', () => {
      component.campoActivo = crearCampoMock('formula');
      component.campoActivo.soloLectura = true;

      component.alCambiarTipo('texto');

      expect(component.campoActivo.tipo).toBe('texto');
      expect(component.campoActivo.soloLectura).toBe(true);
    });

    it('regla Hidden != Delete: cambiar de tipo no elimina propiedades contractuales previas', () => {
      component.campoActivo = crearCampoMock('selector-catalogo');
      component.campoActivo.codigoCatalogo = 'CAT_AREAS';
      component.campoActivo.opciones = ['Opc 1'];
      component.campoActivo.formula = 'x + y';

      component.alCambiarTipo('texto');

      expect(component.campoActivo.codigoCatalogo).toBe('CAT_AREAS');
      expect(component.campoActivo.opciones).toEqual(['Opc 1']);
      expect(component.campoActivo.formula).toBe('x + y');
    });
  });

  describe('3. Grupo: REGLAS', () => {
    it('permite alternar obligatorio y emite propiedadCambiada', () => {
      component.campoActivo = crearCampoMock('texto');
      let emitido = false;
      component.propiedadCambiada.subscribe(() => emitido = true);

      component.campoActivo.obligatorio = true;
      component.alCambiarPropiedad();

      expect(emitido).toBe(true);
    });

    it('en tipo "formula", soloLectura permanece bloqueado en true', () => {
      const mock = crearCampoMock('formula');
      mock.soloLectura = true;
      fixture.componentRef.setInput('campoActivo', mock);
      fixture.detectChanges();

      const el = fixture.nativeElement as HTMLElement;
      const chkSoloLectura = el.querySelector('#prop-sololectura-cmp_test_1') as HTMLInputElement;

      expect(chkSoloLectura.disabled || chkSoloLectura.getAttribute('disabled') !== null).toBe(true);
      expect(chkSoloLectura.checked).toBe(true);
    });

    it('muestra el campo formula únicamente cuando el tipo es formula', () => {
      component.campoActivo = crearCampoMock('formula');
      fixture.detectChanges();
      let el = fixture.nativeElement as HTMLElement;
      expect(el.querySelector('#prop-formula-cmp_test_1')).toBeTruthy();

      component.campoActivo = crearCampoMock('texto');
      fixture.detectChanges();
      el = fixture.nativeElement as HTMLElement;
      expect(el.querySelector('#prop-formula-cmp_test_1')).toBeNull();
    });
  });

  describe('4. Grupo: DATOS', () => {
    it('muestra selector de catálogo cuando requiereCatalogo es true (selector-catalogo y catalogo-multiple)', () => {
      component.campoActivo = crearCampoMock('selector-catalogo');
      fixture.detectChanges();
      let el = fixture.nativeElement as HTMLElement;
      expect(el.querySelector('#prop-catalogo-cmp_test_1')).toBeTruthy();

      component.campoActivo = crearCampoMock('catalogo-multiple');
      fixture.detectChanges();
      el = fixture.nativeElement as HTMLElement;
      expect(el.querySelector('#prop-catalogo-cmp_test_1')).toBeTruthy();
    });

    it('click en "Administrar Catálogos" emite navegarCatalogos', () => {
      component.campoActivo = crearCampoMock('selector-catalogo');
      fixture.detectChanges();

      let emitido = false;
      component.navegarCatalogos.subscribe(() => emitido = true);

      const el = fixture.nativeElement as HTMLElement;
      const btnAdmin = Array.from(el.querySelectorAll('button')).find(b => b.textContent?.includes('Administrar Catálogos'));
      btnAdmin?.click();

      expect(emitido).toBe(true);
    });

    it('muestra editor de opciones cuando requiereOpciones es true (radio)', () => {
      component.campoActivo = crearCampoMock('radio');
      component.campoActivo.opciones = ['Opción 1', 'Opción 2'];
      fixture.detectChanges();

      const el = fixture.nativeElement as HTMLElement;
      expect(el.textContent).toContain('Opciones Configuradas');
      expect(el.textContent).toContain('Opción 1');
      expect(el.textContent).toContain('Opción 2');
    });

    it('permite agregar y eliminar opciones como string[]', () => {
      component.campoActivo = crearCampoMock('radio');
      component.campoActivo.opciones = ['Opción A'];
      let emitidoCount = 0;
      component.propiedadCambiada.subscribe(() => emitidoCount++);

      // Agregar opción
      component.nuevaOpcionTexto = 'Opción B';
      component.agregarOpcion();

      expect(component.campoActivo.opciones).toEqual(['Opción A', 'Opción B']);
      expect(component.nuevaOpcionTexto).toBe('');
      expect(emitidoCount).toBe(1);

      // Eliminar primera opción
      component.eliminarOpcion(0);
      expect(component.campoActivo.opciones).toEqual(['Opción B']);
      expect(emitidoCount).toBe(2);
    });

    it('ignora adición de opciones vacías o con solo espacios', () => {
      component.campoActivo = crearCampoMock('radio');
      component.campoActivo.opciones = ['Opción Existente'];

      component.nuevaOpcionTexto = '   ';
      component.agregarOpcion();

      expect(component.campoActivo.opciones).toEqual(['Opción Existente']);
    });

    it('muestra nota informativa cuando el tipo no requiere datos adicionales (texto, numero, fecha, etc.)', () => {
      component.campoActivo = crearCampoMock('texto');
      fixture.detectChanges();

      const el = fixture.nativeElement as HTMLElement;
      expect(el.textContent).toContain('Este tipo de campo no requiere una fuente de datos adicional');
    });
  });

  describe('5. Grupo: PRESENTACIÓN', () => {
    it('muestra placeholder únicamente en tipos soportados (texto, numero, texto-largo)', () => {
      component.campoActivo = crearCampoMock('texto');
      fixture.detectChanges();
      let el = fixture.nativeElement as HTMLElement;
      expect(el.querySelector('#prop-placeholder-cmp_test_1')).toBeTruthy();

      component.campoActivo = crearCampoMock('checkbox');
      fixture.detectChanges();
      el = fixture.nativeElement as HTMLElement;
      expect(el.querySelector('#prop-placeholder-cmp_test_1')).toBeNull();
    });

    it('renderiza texto de ayuda y selector de ancho en columnas (1 a 6)', () => {
      component.campoActivo = crearCampoMock('texto');
      fixture.detectChanges();

      const el = fixture.nativeElement as HTMLElement;
      const inputAyuda = el.querySelector('#prop-textoayuda-cmp_test_1') as HTMLInputElement;
      const selectAncho = el.querySelector('#prop-ancho-cmp_test_1') as HTMLSelectElement;

      expect(inputAyuda).toBeTruthy();
      expect(selectAncho).toBeTruthy();
      expect(selectAncho.options.length).toBe(6);
    });
  });

  describe('6. Modo Solo Lectura', () => {
    beforeEach(() => {
      component.campoActivo = crearCampoMock('selector-catalogo');
      component.soloLectura = true;
      fixture.detectChanges();
    });

    it('deshabilita inputs, selects y checkboxes en modo soloLectura', () => {
      const el = fixture.nativeElement as HTMLElement;
      const inputEtiqueta = el.querySelector('#prop-etiqueta-cmp_test_1') as HTMLInputElement;
      const inputClave = el.querySelector('#prop-clave-cmp_test_1') as HTMLInputElement;
      const selectTipo = el.querySelector('#prop-tipo-cmp_test_1') as HTMLSelectElement;
      const selectCatalogo = el.querySelector('#prop-catalogo-cmp_test_1') as HTMLSelectElement;

      expect(inputEtiqueta.disabled).toBe(true);
      expect(inputClave.disabled).toBe(true);
      expect(selectTipo.disabled).toBe(true);
      expect(selectCatalogo.disabled).toBe(true);
    });

    it('no emite cambios en modo soloLectura', () => {
      let emitido = false;
      component.propiedadCambiada.subscribe(() => emitido = true);

      component.alCambiarTipo('numero');
      component.alCambiarPropiedad();
      component.agregarOpcion();
      component.eliminarOpcion(0);

      expect(emitido).toBe(false);
    });
  });

  describe('7. Matriz de Propiedades por Tipo (9 Tipos Oficiales)', () => {
    const tipos = [
      { tipo: 'texto', requiereCat: false, requiereOpc: false, requiereForm: false, placeholder: true },
      { tipo: 'numero', requiereCat: false, requiereOpc: false, requiereForm: false, placeholder: true },
      { tipo: 'fecha', requiereCat: false, requiereOpc: false, requiereForm: false, placeholder: false },
      { tipo: 'texto-largo', requiereCat: false, requiereOpc: false, requiereForm: false, placeholder: true },
      { tipo: 'selector-catalogo', requiereCat: true, requiereOpc: false, requiereForm: false, placeholder: false },
      { tipo: 'radio', requiereCat: false, requiereOpc: true, requiereForm: false, placeholder: false },
      { tipo: 'catalogo-multiple', requiereCat: true, requiereOpc: false, requiereForm: false, placeholder: false },
      { tipo: 'checkbox', requiereCat: false, requiereOpc: false, requiereForm: false, placeholder: false },
      { tipo: 'formula', requiereCat: false, requiereOpc: false, requiereForm: true, placeholder: false }
    ];

    tipos.forEach(t => {
      it(`verifica visibilidad exacta para tipo "${t.tipo}"`, () => {
        component.campoActivo = crearCampoMock(t.tipo);
        fixture.detectChanges();

        expect(component.requiereCatalogo).toBe(t.requiereCat);
        expect(component.requiereOpciones).toBe(t.requiereOpc);
        expect(component.requiereFormula).toBe(t.requiereForm);
        expect(component.soportaPlaceholder).toBe(t.placeholder);
      });
    });
  });

  describe('8. Acordeones (Estado UI no persistido)', () => {
    it('alterna el estado colapsado/expandido de las 4 secciones sin tocar el modelo', () => {
      const campo = crearCampoMock('texto');
      component.campoActivo = campo;
      fixture.detectChanges();

      expect(component.seccionesAbiertas.general).toBe(true);
      expect(component.seccionesAbiertas.reglas).toBe(true);
      expect(component.seccionesAbiertas.datos).toBe(true);
      expect(component.seccionesAbiertas.presentacion).toBe(true);

      component.toggleSeccion('general');
      expect(component.seccionesAbiertas.general).toBe(false);

      component.toggleSeccion('reglas');
      expect(component.seccionesAbiertas.reglas).toBe(false);

      // El modelo permanece intacto sin propiedades UI inyectadas
      const keys = Object.keys(component.campoActivo);
      expect(keys.includes('generalAbierto')).toBe(false);
      expect(keys.includes('reglasAbiertas')).toBe(false);
      expect(keys.includes('seccionesAbiertas')).toBe(false);
      expect(keys.includes('uiState')).toBe(false);
    });
  });

  describe('9. Cero Propiedades Inventadas (HARD GATE)', () => {
    it('no contiene propiedades prohibidas como min, max, pattern, cssClass, etc.', () => {
      const campo = crearCampoMock('texto');
      component.campoActivo = campo;
      fixture.detectChanges();

      const propiedadesProhibidas = [
        'min', 'max', 'step', 'pattern', 'regex', 'maxlength', 'minlength',
        'mask', 'icon', 'iconoVisual', 'color', 'background', 'alignment',
        'cssClass', 'className', 'visible', 'hidden', 'conditional', 'condition',
        'dependsOn', 'apiSource', 'endpoint', 'query', 'defaultValue', 'valorDefault',
        'precision', 'currency', 'format', 'layout', 'orientation', 'selected',
        'expanded', 'activeTab', 'accordionState', 'uiState', 'visualState'
      ];

      const keysEnCampo = Object.keys(campo);
      for (const prop of propiedadesProhibidas) {
        expect(keysEnCampo.includes(prop)).toBe(false);
      }
    });
  });
});
