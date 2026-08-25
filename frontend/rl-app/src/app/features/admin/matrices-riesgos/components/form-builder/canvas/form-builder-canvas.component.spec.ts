import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormBuilderCanvasComponent } from './form-builder-canvas.component';
import { SeccionBuilderModel, CampoBuilderModel } from '../../../models/form-builder.models';

describe('FormBuilderCanvasComponent — UI-FORM.3 Lienzo, Secciones y Field Cards', () => {
  let component: FormBuilderCanvasComponent;
  let fixture: ComponentFixture<FormBuilderCanvasComponent>;

  const mockSecciones: SeccionBuilderModel[] = [
    {
      id: 'sec_1',
      clave: 'datos_principales',
      titulo: 'Datos Principales',
      orden: 1,
      columnasPorFila: 2,
      campos: [
        { id: 'cmp_1', clave: 'nombre', etiqueta: 'Nombre Completo', tipo: 'texto', obligatorio: true, soloLectura: false, anchoColumnas: 1 },
        { id: 'cmp_2', clave: 'edad', etiqueta: 'Edad', tipo: 'numero', obligatorio: false, soloLectura: false, anchoColumnas: 1 },
        { id: 'cmp_3', clave: 'area', etiqueta: 'Área', tipo: 'selector-catalogo', codigoCatalogo: 'CAT_AREAS', obligatorio: true, soloLectura: false, anchoColumnas: 1 }
      ]
    },
    {
      id: 'sec_2',
      clave: 'evaluacion_riesgo',
      titulo: 'Evaluación de Riesgo',
      orden: 2,
      columnasPorFila: 1,
      campos: []
    }
  ];

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [FormBuilderCanvasComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(FormBuilderCanvasComponent);
    component = fixture.componentInstance;
    component.secciones = JSON.parse(JSON.stringify(mockSecciones));
    component.soloLectura = false;
    component.seccionActivaId = 'sec_1';
    component.campoActivoId = 'cmp_1';
    fixture.detectChanges();
  });

  describe('1. Field Cards y Selección Visual', () => {
    it('renderiza todos los campos de la sección con clave y tipo visible', () => {
      const el = fixture.nativeElement as HTMLElement;
      const cards = el.querySelectorAll('article');
      expect(cards.length).toBe(3);
      expect(cards[0].textContent).toContain('nombre · texto');
      expect(cards[0].textContent).toContain('Nombre Completo');
      expect(cards[0].textContent).toContain('*'); // obligatorio
    });

    it('aplica estado activo (borde azul y ring) únicamente al campo seleccionado', () => {
      const el = fixture.nativeElement as HTMLElement;
      const cards = el.querySelectorAll('article');

      expect(cards[0].classList.contains('border-blue-500')).toBe(true);
      expect(cards[0].classList.contains('ring-2')).toBe(true);

      expect(cards[1].classList.contains('border-blue-500')).toBe(false);
      expect(cards[2].classList.contains('border-blue-500')).toBe(false);
    });

    it('click en una tarjeta emite seleccionarCampo con el modelo exacto', () => {
      let seleccionado: CampoBuilderModel | undefined;
      component.seleccionarCampo.subscribe((c: CampoBuilderModel) => seleccionado = c);

      const el = fixture.nativeElement as HTMLElement;
      const cards = el.querySelectorAll('article');
      cards[1].click();

      expect(seleccionado).toBeTruthy();
      expect(seleccionado?.id).toBe('cmp_2');
      expect(seleccionado?.clave).toBe('edad');
    });

    it('renderiza previews apropiados para los diferentes tipos soportados', () => {
      const el = fixture.nativeElement as HTMLElement;
      const inputTexto = el.querySelector('#preview-cmp_1');
      const inputNumero = el.querySelector('#preview-cmp_2');
      const selectCatalogo = el.querySelector('#preview-cmp_3');

      expect(inputTexto).toBeTruthy();
      expect(inputNumero).toBeTruthy();
      expect(selectCatalogo).toBeTruthy();
    });
  });

  describe('2. Secciones y Columnas', () => {
    it('renderiza el encabezado de sección con orden, título y selector de columnas', () => {
      const el = fixture.nativeElement as HTMLElement;
      const inputTitulo = el.querySelector('#seccion-titulo-sec_1') as HTMLInputElement;
      const selectColumnas = el.querySelector('#seccion-columnas-sec_1') as HTMLSelectElement;

      expect(inputTitulo.value).toBe('Datos Principales');
      expect(selectColumnas.value).toBe('2');
    });

    it('cambiar título emite tituloSeccionChange con seccionId y nuevo título', () => {
      let emitido: { seccionId: string; titulo: string } | null = null;
      component.tituloSeccionChange.subscribe(e => emitido = e);

      const el = fixture.nativeElement as HTMLElement;
      const inputTitulo = el.querySelector('#seccion-titulo-sec_1') as HTMLInputElement;
      inputTitulo.value = 'Nuevo Título Sección';
      inputTitulo.dispatchEvent(new Event('input'));

      expect(emitido).toEqual({ seccionId: 'sec_1', titulo: 'Nuevo Título Sección' });
    });

    it('cambiar columnas emite columnasSeccionChange con seccionId y cantidad de columnas', () => {
      let emitido: { seccionId: string; columnas: number } | null = null;
      component.columnasSeccionChange.subscribe(e => emitido = e);

      const el = fixture.nativeElement as HTMLElement;
      const selectColumnas = el.querySelector('#seccion-columnas-sec_1') as HTMLSelectElement;
      selectColumnas.value = '3';
      selectColumnas.dispatchEvent(new Event('change'));

      expect(emitido).toEqual({ seccionId: 'sec_1', columnas: 3 });
    });
  });

  describe('3. Acciones Agrupadas y Boundaries de Reordenamiento', () => {
    it('primer campo tiene el botón subir deshabilitado y bajar habilitado', () => {
      const el = fixture.nativeElement as HTMLElement;
      const cards = el.querySelectorAll('article');
      const botonesPrimerCampo = cards[0].querySelectorAll('button[title]');

      const btnSubir = Array.from(botonesPrimerCampo).find(b => b.getAttribute('title') === 'Mover arriba') as HTMLButtonElement;
      const btnBajar = Array.from(botonesPrimerCampo).find(b => b.getAttribute('title') === 'Mover abajo') as HTMLButtonElement;

      expect(btnSubir?.disabled).toBe(true);
      expect(btnBajar?.disabled).toBe(false);
    });

    it('último campo tiene el botón bajar deshabilitado y subir habilitado', () => {
      const el = fixture.nativeElement as HTMLElement;
      const cards = el.querySelectorAll('article');
      const botonesUltimoCampo = cards[2].querySelectorAll('button[title]');

      const btnSubir = Array.from(botonesUltimoCampo).find(b => b.getAttribute('title') === 'Mover arriba') as HTMLButtonElement;
      const btnBajar = Array.from(botonesUltimoCampo).find(b => b.getAttribute('title') === 'Mover abajo') as HTMLButtonElement;

      expect(btnSubir?.disabled).toBe(false);
      expect(btnBajar?.disabled).toBe(true);
    });

    it('click en botón subir emite reordenarCampo con dirección "subir"', () => {
      let emitido: { seccionId: string; index: number; direccion: 'subir' | 'bajar' } | null = null;
      component.reordenarCampo.subscribe(e => emitido = e);

      const el = fixture.nativeElement as HTMLElement;
      const cards = el.querySelectorAll('article');
      const btnSubir = cards[1].querySelector('button[title="Mover arriba"]') as HTMLButtonElement;
      btnSubir.click();

      expect(emitido).toEqual({ seccionId: 'sec_1', index: 1, direccion: 'subir' });
    });

    it('click en botón bajar emite reordenarCampo con dirección "bajar"', () => {
      let emitido: { seccionId: string; index: number; direccion: 'subir' | 'bajar' } | null = null;
      component.reordenarCampo.subscribe(e => emitido = e);

      const el = fixture.nativeElement as HTMLElement;
      const cards = el.querySelectorAll('article');
      const btnBajar = cards[1].querySelector('button[title="Mover abajo"]') as HTMLButtonElement;
      btnBajar.click();

      expect(emitido).toEqual({ seccionId: 'sec_1', index: 1, direccion: 'bajar' });
    });

    it('click en botón eliminar emite eliminarCampo con seccionId y campoId', () => {
      let emitido: { seccionId: string; campoId: string } | null = null;
      component.eliminarCampo.subscribe(e => emitido = e);

      const el = fixture.nativeElement as HTMLElement;
      const cards = el.querySelectorAll('article');
      const btnEliminar = cards[0].querySelector('button[title="Eliminar campo"]') as HTMLButtonElement;
      btnEliminar.click();

      expect(emitido).toEqual({ seccionId: 'sec_1', campoId: 'cmp_1' });
    });
  });

  describe('4. Drop Zones Compactas y Drag & Drop', () => {
    it('sección vacía muestra mensaje orientativo para arrastrar o hacer click', () => {
      const el = fixture.nativeElement as HTMLElement;
      const secciones = el.querySelectorAll('section');
      expect(secciones[1].textContent).toContain('Arrastra un campo desde el panel izquierdo');
    });

    it('dragover sobre sección activa el drop-zone visual destacado', () => {
      const preventDefaultSpy = vi.fn();
      const dragEvent = {
        type: 'dragover',
        preventDefault: preventDefaultSpy,
        dataTransfer: { dropEffect: '' }
      } as unknown as DragEvent;

      component.onDragOver(dragEvent, 'sec_1');
      fixture.detectChanges();

      expect(preventDefaultSpy).toHaveBeenCalled();
      expect(component.seccionArrastreSobreId()).toBe('sec_1');
      const el = fixture.nativeElement as HTMLElement;
      expect(el.querySelector('.drop-zone')).toBeTruthy();
      expect(el.textContent).toContain('Suelta el campo en «Datos Principales»');
    });

    it('dragleave desactiva el estado visual de recepción', () => {
      component.seccionArrastreSobreId.set('sec_1');
      fixture.detectChanges();

      const dragEvent = {
        type: 'dragleave',
        currentTarget: null,
        relatedTarget: null
      } as unknown as DragEvent;

      component.onDragLeave(dragEvent, 'sec_1');
      fixture.detectChanges();

      expect(component.seccionArrastreSobreId()).toBeNull();
    });

    it('drop emite soltarControl con seccionId y tipo de control', () => {
      let eventoSoltado: { seccionId: string; tipo: string } | null = null;
      component.soltarControl.subscribe(e => eventoSoltado = e);

      const mockDataTransfer = {
        getData: (format: string) => format === 'application/x-form-builder-control' ? 'numero' : ''
      } as unknown as DataTransfer;

      const dropEvent = {
        type: 'drop',
        preventDefault: vi.fn(),
        dataTransfer: mockDataTransfer
      } as unknown as DragEvent;

      component.onDrop(dropEvent, 'sec_2');

      expect(eventoSoltado).toEqual({ seccionId: 'sec_2', tipo: 'numero' });
      expect(component.seccionArrastreSobreId()).toBeNull();
    });
  });

  describe('5. Modo Solo Lectura', () => {
    beforeEach(() => {
      component.soloLectura = true;
      fixture.detectChanges();
    });

    it('deshabilita edición de título y cambio de columnas', () => {
      const el = fixture.nativeElement as HTMLElement;
      const inputTitulo = el.querySelector('#seccion-titulo-sec_1') as HTMLInputElement;
      const selectColumnas = el.querySelector('#seccion-columnas-sec_1') as HTMLSelectElement;

      expect(inputTitulo.disabled).toBe(true);
      expect(selectColumnas.disabled).toBe(true);
    });

    it('oculta acciones destructivas y de reordenamiento de campos y secciones', () => {
      const el = fixture.nativeElement as HTMLElement;
      expect(el.querySelectorAll('button[title="Mover arriba"]').length).toBe(0);
      expect(el.querySelectorAll('button[title="Mover abajo"]').length).toBe(0);
      expect(el.querySelectorAll('button[title="Eliminar campo"]').length).toBe(0);
      expect(el.querySelectorAll('button[title="Eliminar sección"]').length).toBe(0);
    });

    it('drop en modo soloLectura no emite soltarControl', () => {
      let emitido = false;
      component.soltarControl.subscribe(() => emitido = true);

      const mockDataTransfer = { getData: () => 'texto' } as unknown as DataTransfer;
      const dropEvent = { type: 'drop', preventDefault: vi.fn(), dataTransfer: mockDataTransfer } as unknown as DragEvent;

      component.onDrop(dropEvent, 'sec_1');
      expect(emitido).toBe(false);
    });
  });

  describe('6. Previews de Presentación y Opciones (UI-FORM.4)', () => {
    it('texto, numero y texto-largo usan cmp.placeholder cuando está definido', () => {
      component.secciones = [
        {
          id: 'sec_preview',
          clave: 'sec_preview',
          titulo: 'Sección Previews',
          orden: 1,
          columnasPorFila: 3,
          campos: [
            { id: 'c_txt', clave: 'txt', etiqueta: 'Texto', tipo: 'texto', orden: 1, anchoColumnas: 1, obligatorio: false, soloLectura: false, placeholder: 'Mi placeholder personalizado' },
            { id: 'c_num', clave: 'num', etiqueta: 'Número', tipo: 'numero', orden: 2, anchoColumnas: 1, obligatorio: false, soloLectura: false, placeholder: 'Ej. 99.99' },
            { id: 'c_lng', clave: 'lng', etiqueta: 'Largo', tipo: 'texto-largo', orden: 3, anchoColumnas: 1, obligatorio: false, soloLectura: false, placeholder: 'Detalle extenso...' }
          ]
        }
      ];
      fixture.detectChanges();

      const el = fixture.nativeElement as HTMLElement;
      const inputTexto = el.querySelector('#preview-c_txt') as HTMLInputElement;
      const inputNumero = el.querySelector('#preview-c_num') as HTMLInputElement;
      const textareaLargo = el.querySelector('#preview-c_lng') as HTMLTextAreaElement;

      expect(inputTexto.placeholder).toBe('Mi placeholder personalizado');
      expect(inputNumero.placeholder).toBe('Ej. 99.99');
      expect(textareaLargo.placeholder).toBe('Detalle extenso...');
    });

    it('radio muestra opciones reales cuando cmp.opciones está definido sin mutar el modelo', () => {
      component.secciones = [
        {
          id: 'sec_radio',
          clave: 'sec_radio',
          titulo: 'Sección Radio',
          orden: 1,
          columnasPorFila: 1,
          campos: [
            { id: 'c_rad', clave: 'rad', etiqueta: 'Nivel', tipo: 'radio', orden: 1, anchoColumnas: 1, obligatorio: false, soloLectura: false, opciones: ['Alto', 'Medio', 'Bajo'] }
          ]
        }
      ];
      fixture.detectChanges();

      const el = fixture.nativeElement as HTMLElement;
      expect(el.textContent).toContain('Alto');
      expect(el.textContent).toContain('Medio');
      expect(el.textContent).toContain('Bajo');
    });

    it('radio sin opciones muestra fallback visual sin inventar opciones en el modelo', () => {
      const campoSinOpciones: CampoBuilderModel = {
        id: 'c_rad_vacio',
        clave: 'rad_vacio',
        etiqueta: 'Sin Opciones',
        tipo: 'radio',
        orden: 1,
        anchoColumnas: 1,
        obligatorio: false,
        soloLectura: false
      };
      component.secciones = [
        {
          id: 'sec_radio_vacio',
          clave: 'sec_radio_vacio',
          titulo: 'Sección Radio Vacío',
          orden: 1,
          columnasPorFila: 1,
          campos: [campoSinOpciones]
        }
      ];
      fixture.detectChanges();

      const el = fixture.nativeElement as HTMLElement;
      expect(el.textContent).toContain('Opción 1');
      expect(el.textContent).toContain('Opción 2');

      // No se inyectan opciones al modelo
      expect(campoSinOpciones.opciones).toBeUndefined();
    });

    it('la drop-zone poblada es compacta, visible y no muta el modelo', () => {
      const antes = JSON.stringify(component.secciones);
      const el = fixture.nativeElement as HTMLElement;
      const zone = el.querySelector('.drop-zone') as HTMLElement;

      expect(zone).toBeTruthy();
      expect(zone.classList.contains('min-h-[66px]')).toBe(true);
      expect(zone.textContent).toContain('Arrastra un campo desde el panel izquierdo');
      expect(JSON.stringify(component.secciones)).toBe(antes);
    });

    it('aplica anchoColumnas al layout visual sin agregar propiedades al modelo', () => {
      component.secciones = [{
        ...mockSecciones[0],
        campos: [{ ...mockSecciones[0].campos[0], anchoColumnas: 2 }, mockSecciones[0].campos[1]]
      }];
      const antes = JSON.stringify(component.secciones);
      fixture.detectChanges();

      const card = (fixture.nativeElement as HTMLElement).querySelector('article') as HTMLElement;
      expect(card.style.gridColumn).toBe('span 2 / span 2');
      expect(JSON.stringify(component.secciones)).toBe(antes);
    });
  });
});
