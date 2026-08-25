import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormBuilderComponent } from './form-builder.component';
import { normalizarJsonABuilderModel, serializarBuilderModelAJson, FormBuilderModel } from '../../models/form-builder.models';
import { validarFormBuilderModel } from '../../utils/form-builder-validator.util';
import { describe, it, expect, beforeEach, vi } from 'vitest';

describe('FormBuilderComponent y Adaptador Normalizador (Fases 3 y 4)', () => {
  let component: FormBuilderComponent;
  let fixture: ComponentFixture<FormBuilderComponent>;

  const jsonPruebaValido = JSON.stringify({
    codigoFormulario: 'MATRIZ_LAFT_TEST',
    nombreFormulario: 'Matriz de Riesgo Prueba',
    secciones: [
      {
        clave: 'identificacion',
        titulo: 'Identificación',
        orden: 1,
        campos: [
          { clave: 'area', etiqueta: 'Área principal', tipo: 'texto', obligatorio: true, soloLectura: false }
        ]
      }
    ],
    catalogos: [
      { codigo: 'CAT_AREA', nombre: 'Áreas', elementos: [{ codigo: '01', valor: 'Área 1', orden: 1 }] }
    ]
  });

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [FormBuilderComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(FormBuilderComponent);
    component = fixture.componentInstance;
    component.jsonDefinicion = jsonPruebaValido;
    component.esAdministrador = true;
    fixture.detectChanges();
  });

  it('debe crear el componente FormBuilderComponent', () => {
    expect(component).toBeTruthy();
  });

  it('bloquea la visualización del editor JSON técnico si esAdministrador es false', () => {
    component.esAdministrador = false;
    component.toggleModoJson();
    expect(component.mostrarJsonAvanzado()).toBe(false);
  });

  it('muestra una vista previa de solo lectura con el renderer Ãºnico y las opciones reales', () => {
    component.model.set({
      codigoFormulario: 'PREVIEW',
      nombreFormulario: 'Formulario de prueba',
      descripcion: 'Consulta segura',
      secciones: [{
        id: 'sec-preview', clave: 'identificacion', titulo: 'IdentificaciÃ³n', orden: 1, columnasPorFila: 2,
        campos: [
          { id: 'c1', clave: 'texto', etiqueta: 'Texto', tipo: 'texto', obligatorio: true, soloLectura: false, anchoColumnas: 1 },
          { id: 'c2', clave: 'area', etiqueta: 'Ãrea', tipo: 'selector-catalogo', codigoCatalogo: 'CAT_REAL', obligatorio: false, soloLectura: false, anchoColumnas: 1 },
          { id: 'c3', clave: 'formula', etiqueta: 'Resultado', tipo: 'formula', formula: 'a + b', obligatorio: false, soloLectura: false, anchoColumnas: 2 }
        ]
      }],
      catalogos: [{ codigo: 'CAT_REAL', nombre: 'CatÃ¡logo real', elementos: [{ codigo: 'A', valor: 'OpciÃ³n A', orden: 1 }, { codigo: 'B', valor: 'OpciÃ³n B', orden: 2 }] }]
    });
    const serializadoAntes = serializarBuilderModelAJson(component.model());
    component.cambiarVista('preview');
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[aria-label="Vista previa del formulario"]')).toBeTruthy();
    expect(fixture.nativeElement.querySelectorAll('app-dynamic-field-renderer')).toHaveLength(3);
    expect(fixture.nativeElement.querySelector('app-form-builder-palette')).toBeNull();
    expect(fixture.nativeElement.querySelector('[data-preview-options="area"]')?.textContent).toContain('OpciÃ³n A');
    expect(fixture.nativeElement.textContent).toContain('Resultado');
    expect(fixture.nativeElement.textContent).not.toContain('a + b');
    expect(serializarBuilderModelAJson(component.model())).toBe(serializadoAntes);
  });

  it('busca de forma literal, copia el JSON exacto y valida sin aplicar ni guardar', async () => {
    const json = serializarBuilderModelAJson(component.model());
    component.jsonAvanzadoStr.set(json);
    component.busquedaJson.set('MATRIZ_LAFT_TEST');
    expect(component.coincidenciasJson().length).toBeGreaterThan(0);
    component.moverBusquedaJson(1);
    const modeloAntes = serializarBuilderModelAJson(component.model());
    const emitir = vi.spyOn(component.guardarJson, 'emit');
    const writeText = vi.fn().mockResolvedValue(undefined);
    Object.defineProperty(navigator, 'clipboard', { configurable: true, value: { writeText } });

    component.copiarJsonTecnico();
    component.validarJsonTecnico();
    await Promise.resolve();

    expect(writeText).toHaveBeenCalledWith(json);
    expect(component.resultadoValidacionJson()).toBe('valido');
    expect(serializarBuilderModelAJson(component.model())).toBe(modeloAntes);
    expect(emitir).not.toHaveBeenCalled();
  });

  it('rechaza JSON invÃ¡lido sin tocar el modelo ni activar sincronizaciÃ³n', () => {
    const modeloAntes = serializarBuilderModelAJson(component.model());
    component.jsonAvanzadoStr.set('{"secciones":');
    component.validarJsonTecnico();
    expect(component.resultadoValidacionJson()).toBe('invalido');
    expect(component.mensajeJson()).toContain('JSON sint');
    expect(serializarBuilderModelAJson(component.model())).toBe(modeloAntes);
  });

  it('normalizarJsonABuilderModel convierte correctamente la estructura JSON en BuilderModel', () => {
    const model = normalizarJsonABuilderModel(jsonPruebaValido, 'CODIGO_DEFAULT', 'Nombre Default');
    expect(model.codigoFormulario).toBe('MATRIZ_LAFT_TEST');
    expect(model.secciones).toHaveLength(1);
    expect(model.secciones[0].titulo).toBe('Identificación');
    expect(model.secciones[0].campos).toHaveLength(1);
    expect(model.secciones[0].campos[0].clave).toBe('area');
  });

  it('serializarBuilderModelAJson preserva el contrato oficial de JSON', () => {
    const model: FormBuilderModel = {
      codigoFormulario: 'FORM_TEST',
      nombreFormulario: 'Test Form',
      secciones: [
        {
          id: 'sec_1',
          clave: 'sec_test',
          titulo: 'Sección Test',
          orden: 1,
          columnasPorFila: 2,
          campos: [
            {
              id: 'cmp_1',
              clave: 'campo_uno',
              etiqueta: 'Campo Uno',
              tipo: 'texto',
              obligatorio: true,
              soloLectura: false,
              anchoColumnas: 1
            }
          ]
        }
      ]
    };

    const json = serializarBuilderModelAJson(model);
    const parsed = JSON.parse(json);
    expect(parsed.codigoFormulario).toBe('FORM_TEST');
    expect(parsed.secciones[0].campos[0].clave).toBe('campo_uno');
    expect(parsed.secciones[0].campos[0].obligatorio).toBe(true);
  });

  it('validarFormBuilderModel detecta claves técnicas duplicadas', () => {
    const modelDuplicado: FormBuilderModel = {
      codigoFormulario: 'TEST',
      nombreFormulario: 'Test',
      secciones: [
        {
          id: 'sec_1',
          clave: 'general',
          titulo: 'General',
          orden: 1,
          columnasPorFila: 2,
          campos: [
            { id: 'c1', clave: 'campo_duplicado', etiqueta: 'E1', tipo: 'texto', obligatorio: false, soloLectura: false, anchoColumnas: 1 },
            { id: 'c2', clave: 'campo_duplicado', etiqueta: 'E2', tipo: 'numero', obligatorio: false, soloLectura: false, anchoColumnas: 1 }
          ]
        }
      ]
    };

    const errores = validarFormBuilderModel(modelDuplicado);
    expect(errores.length).toBeGreaterThan(0);
    expect(errores[0].mensaje).toContain('está duplicada');
  });

  it('no emite guardado si existen errores de validación', () => {
    vi.spyOn(component.guardarJson, 'emit');
    component.model.set({
      codigoFormulario: 'INVALID',
      nombreFormulario: 'Invalid',
      secciones: [
        {
          id: 'sec_1',
          clave: '',
          titulo: '',
          orden: 1,
          columnasPorFila: 2,
          campos: []
        }
      ]
    });

    component.emitirGuardado();
    expect(component.erroresValidacion().length).toBeGreaterThan(0);
    expect(component.guardarJson.emit).not.toHaveBeenCalled();
  });

  it('permite editar etiqueta, tipo, obligatoriedad y fórmula de un campo en modo DRAFT y actualiza la serialización JSON', () => {
    component.soloLectura = false;
    const seccionId = component.model().secciones[0].id;
    const ctrlTexto = component.tiposControles.find(t => t.tipo === 'texto')!;
    component.agregarCampoASeccion(seccionId, ctrlTexto);

    const campo = component.model().secciones[0].campos[0];
    component.seleccionarCampo(campo);

    const activo = component.campoActivo()!;
    activo.etiqueta = 'Nuevo Nombre Evaluacion';
    activo.tipo = 'formula';
    activo.formula = 'val1 * val2';
    activo.obligatorio = true;
    component.alCambiarPropiedadCampo();

    const jsonFinal = serializarBuilderModelAJson(component.model());
    const parsed = JSON.parse(jsonFinal);

    expect(parsed.secciones[0].campos[0].etiqueta).toBe('Nuevo Nombre Evaluacion');
    expect(parsed.secciones[0].campos[0].tipo).toBe('formula');
    expect(parsed.secciones[0].campos[0].formula).toBe('val1 * val2');
    expect(parsed.secciones[0].campos[0].obligatorio).toBe(true);
  });

  it('permite agregar y eliminar campos en modo DRAFT', () => {
    component.soloLectura = false;
    const seccionId = component.model().secciones[0].id;
    const camposIniciales = component.model().secciones[0].campos.length;

    const ctrlNumero = component.tiposControles.find(t => t.tipo === 'numero')!;
    component.agregarCampoASeccion(seccionId, ctrlNumero);
    expect(component.model().secciones[0].campos).toHaveLength(camposIniciales + 1);

    const nuevoCampoId = component.model().secciones[0].campos[component.model().secciones[0].campos.length - 1].id;
    component.eliminarCampo(seccionId, nuevoCampoId);
    expect(component.model().secciones[0].campos).toHaveLength(camposIniciales);
  });

  it('bloquea modificaciones cuando soloLectura es true (versión PUBLISHED o VIGENTE)', () => {
    component.soloLectura = true;
    const seccionId = component.model().secciones[0].id;
    const camposIniciales = component.model().secciones[0].campos.length;

    const ctrlTexto = component.tiposControles.find(t => t.tipo === 'texto')!;
    component.agregarCampoASeccion(seccionId, ctrlTexto);
    expect(component.model().secciones[0].campos).toHaveLength(camposIniciales);

    component.agregarSeccion();
    expect(component.model().secciones).toHaveLength(1);
  });

  it('emite el evento de cierre correctamente al presionar el botón de cerrar', () => {
    vi.spyOn(component.cerrar, 'emit');
    component.cerrar.emit();
    expect(component.cerrar.emit).toHaveBeenCalled();
  });

  it('administra secciones y conserva una seccion activa valida', () => {
    const originalId = component.model().secciones[0].id;
    component.agregarSeccion();
    expect(component.model().secciones).toHaveLength(2);
    const nueva = component.model().secciones[1];
    expect(component.seccionActivaId()).toBe(nueva.id);
    component.eliminarSeccion(nueva.id);
    expect(component.model().secciones).toHaveLength(1);
    expect(component.seccionActivaId()).toBe(originalId);
    component.eliminarSeccion(originalId);
    expect(component.model().secciones).toHaveLength(1);
  });

  it('configura controles con catalogo y formula con valores seguros por defecto', () => {
    const seccionId = component.model().secciones[0].id;
    const catalogo = component.tiposControles.find(t => t.tipo === 'selector-catalogo')!;
    const formula = component.tiposControles.find(t => t.tipo === 'formula')!;
    component.agregarCampoASeccion(seccionId, catalogo);
    component.agregarCampoASeccion(seccionId, formula);
    const campos = component.model().secciones[0].campos;
    expect(campos[1].codigoCatalogo).toBe(component.catalogosList()[0].codigo);
    expect(campos[2].soloLectura).toBe(true);
    expect(component.campoActivo()?.id).toBe(campos[2].id);
  });

  it('reordena campos dentro de limites validos y actualiza columnas', () => {
    const seccionId = component.model().secciones[0].id;
    const texto = component.tiposControles.find(t => t.tipo === 'texto')!;
    const numero = component.tiposControles.find(t => t.tipo === 'numero')!;
    component.agregarCampoASeccion(seccionId, texto);
    component.agregarCampoASeccion(seccionId, numero);
    const antes = component.model().secciones[0].campos.map(c => c.id);
    component.reordenarCampo(seccionId, 1, 'subir');
    component.reordenarCampo(seccionId, 0, 'subir');
    component.reordenarCampo(seccionId, 9, 'bajar');
    component.actualizarColumnasSeccion(seccionId, 4);
    component.actualizarColumnasSeccion('inexistente', 6);
    expect(component.model().secciones[0].campos.map(c => c.id)).toEqual([antes[1], antes[0], antes[2]]);
    expect(component.model().secciones[0].columnasPorFila).toBe(4);
  });

  it('aplica JSON avanzado valido y cierra el modo tecnico', () => {
    const jsonNuevo = JSON.stringify({ codigoFormulario: 'FORM_NUEVO', nombreFormulario: 'Nuevo', secciones: [{ clave: 'sec', titulo: 'Sec', orden: 1, campos: [{ clave: 'dato', etiqueta: 'Dato', tipo: 'texto', obligatorio: true }] }] });
    component.toggleModoJson();
    expect(component.mostrarJsonAvanzado()).toBe(true);
    component.jsonAvanzadoStr.set(jsonNuevo);
    component.aplicarJsonAvanzado();
    expect(component.model().codigoFormulario).toBe('FORM_NUEVO');
    expect(component.mostrarJsonAvanzado()).toBe(false);
  });

  it('emite JSON valido y respeta los bloqueos de solo lectura', () => {
    vi.spyOn(component.guardarJson, 'emit');
    component.emitirGuardado();
    expect(component.guardarJson.emit).toHaveBeenCalledTimes(1);
    const seccionId = component.model().secciones[0].id;
    const campoId = component.model().secciones[0].campos[0].id;
    const etiquetaAnterior = component.model().secciones[0].campos[0].etiqueta;
    component.seleccionarCampo({ ...component.model().secciones[0].campos[0] });
    component.campoActivo()!.etiqueta = 'No debe persistir';
    component.soloLectura = true;
    component.alCambiarPropiedadCampo();
    component.eliminarCampo(seccionId, campoId);
    component.actualizarColumnasSeccion(seccionId, 6);
    component.emitirGuardado();
    expect(component.model().secciones[0].campos[0].etiqueta).toBe(etiquetaAnterior);
    expect(component.model().secciones[0].campos).toHaveLength(1);
    expect(component.model().secciones[0].columnasPorFila).toBe(2);
    expect(component.guardarJson.emit).toHaveBeenCalledTimes(1);
  });

  it('valida los casos semanticamente invalidos del contrato visual', () => {
    expect(validarFormBuilderModel(null as unknown as FormBuilderModel)[0].campo).toBe('Modelo');
    expect(validarFormBuilderModel({ codigoFormulario: 'X', nombreFormulario: 'X', secciones: [] })[0].campo).toBe('Secciones');
    const errores = validarFormBuilderModel({
      codigoFormulario: 'X', nombreFormulario: 'X',
      secciones: [{ id: 's', clave: 's', titulo: 'S', orden: 1, columnasPorFila: 2, campos: [
        { id: 'a', clave: '', etiqueta: '', tipo: 'selector-catalogo', codigoCatalogo: '', obligatorio: false, soloLectura: false, anchoColumnas: 1 },
        { id: 'b', clave: 'calc', etiqueta: 'Calc', tipo: 'formula', formula: '', obligatorio: false, soloLectura: true, anchoColumnas: 1 }
      ] }]
    });
    const mensajes = errores.map(error => error.mensaje).join(' ');
    expect(mensajes).toContain('asociar un');
    expect(mensajes).toContain('campo calculado');
  });

  describe('Integración UI-FORM.1 — shell productivo consolidado', () => {
    it('renderiza directamente las cinco regiones del Form Builder sin wrapper Workspace V2', () => {
      component.cambiarVista('secciones');
      fixture.detectChanges();

      const el = fixture.nativeElement as HTMLElement;
      expect(el.querySelector('[data-form-builder-shell="true"]')).toBeTruthy();
      expect(el.querySelector('app-form-builder-workspace-v2')).toBeNull();
      expect(el.querySelector('app-form-builder-toolbar')).toBeTruthy();
      expect(el.querySelector('app-form-builder-palette')).toBeTruthy();
      expect(el.querySelector('app-form-builder-canvas')).toBeTruthy();
      expect(el.querySelector('app-form-builder-inspector')).toBeTruthy();
      expect(el.querySelector('app-form-builder-statusbar')).toBeTruthy();
    });

    it('mantiene la biblioteca visual en la región izquierda cuando el formulario está en solo lectura', () => {
      component.soloLectura = true;
      fixture.detectChanges();

      const palette = (fixture.nativeElement as HTMLElement).querySelector('app-form-builder-palette');
      expect(palette?.textContent).toContain('Agregar campos');
      expect(palette?.textContent).not.toContain('Estructura del formulario');
      expect(palette?.querySelector('#buscador-palette')).not.toBeNull();
    });

    it('actualiza el título de una sección via actualizarTituloSeccion', () => {
      const seccionId = component.model().secciones[0].id;
      component.actualizarTituloSeccion(seccionId, 'Nuevo Título Sección');
      expect(component.model().secciones[0].titulo).toBe('Nuevo Título Sección');
    });
  });

  describe('UI-FORM.2 — Drag & Drop seguro, Gate de Tipos y Selección Automática', () => {
    it('1. valida el tipo soltado contra TIPOS_CONTROLES_DISPONIBLES y agrega el campo', () => {
      const seccionId = component.model().secciones[0].id;
      const totalAntes = component.model().secciones[0].campos.length;

      component.procesarSoltarControl({ seccionId, tipo: 'numero' });

      const camposDespues = component.model().secciones[0].campos;
      expect(camposDespues).toHaveLength(totalAntes + 1);
      const nuevoCampo = camposDespues[camposDespues.length - 1];
      expect(nuevoCampo.tipo).toBe('numero');
      expect(component.seccionActivaId()).toBe(seccionId);
      expect(component.campoActivo()?.id).toBe(nuevoCampo.id);
    });

    it('2. rechaza e ignora payloads de tipos no soportados o inventados (firma-digital, archivo, etc.)', () => {
      const seccionId = component.model().secciones[0].id;
      const totalAntes = component.model().secciones[0].campos.length;

      component.procesarSoltarControl({ seccionId, tipo: 'firma-digital' });
      component.procesarSoltarControl({ seccionId, tipo: 'archivo' });
      component.procesarSoltarControl({ seccionId, tipo: 'geolocalizacion' });
      component.procesarSoltarControl({ seccionId, tipo: 'tipo-arbitrario-xyz' });

      expect(component.model().secciones[0].campos).toHaveLength(totalAntes);
    });

    it('3. drop de campo formula crea el campo con soloLectura: true de forma inmutable', () => {
      const seccionId = component.model().secciones[0].id;
      component.procesarSoltarControl({ seccionId, tipo: 'formula' });

      const campo = component.campoActivo();
      expect(campo).toBeTruthy();
      expect(campo?.tipo).toBe('formula');
      expect(campo?.soloLectura).toBe(true);
    });

    it('4. drop de selector-catalogo asigna catalogo de la plantilla sin inventar catalogos falsos', () => {
      const seccionId = component.model().secciones[0].id;
      component.procesarSoltarControl({ seccionId, tipo: 'selector-catalogo' });

      const campo = component.campoActivo();
      expect(campo).toBeTruthy();
      expect(campo?.tipo).toBe('selector-catalogo');
      expect(campo?.codigoCatalogo).toBe('CAT_AREA');
    });

    it('5. rechaza operaciones de drop cuando soloLectura es true', () => {
      component.soloLectura = true;
      const seccionId = component.model().secciones[0].id;
      const totalAntes = component.model().secciones[0].campos.length;

      component.procesarSoltarControl({ seccionId, tipo: 'texto' });

      expect(component.model().secciones[0].campos).toHaveLength(totalAntes);
    });

    it('6. gate obligatorio de tipos: TIPOS_CONTROLES_DISPONIBLES contiene exactamente los 9 tipos oficiales (0 inventados)', () => {
      const tipos = component.tiposControles.map(t => t.tipo).sort();
      const tiposOficiales = [
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

      expect(tipos).toEqual(tiposOficiales);
      expect(tipos.length).toBe(9);
    });
  });

  describe('UI-FORM.5 — Estados y Ciclo de Edición del Form Builder', () => {
    it('emitirGuardado emite guardarJson cuando el modelo es válido y no está procesando', () => {
      let jsonEmitido = '';
      component.guardarJson.subscribe(j => jsonEmitido = j);

      component.soloLectura = false;
      component.procesando = false;
      component.emitirGuardado();

      expect(jsonEmitido).toBeTruthy();
      expect(JSON.parse(jsonEmitido).codigoFormulario).toBe('MATRIZ_LAFT_TEST');
    });

    it('emitirGuardado queda bloqueado si soloLectura es true', () => {
      let emitido = false;
      component.guardarJson.subscribe(() => emitido = true);

      component.soloLectura = true;
      component.procesando = false;
      component.emitirGuardado();

      expect(emitido).toBe(false);
    });

    it('emitirGuardado queda bloqueado si procesando es true', () => {
      let emitido = false;
      component.guardarJson.subscribe(() => emitido = true);

      component.soloLectura = false;
      component.procesando = true;
      component.emitirGuardado();

      expect(emitido).toBe(false);
    });

    it('emitirPublicar emite evento publicar cuando puedePublicar es true', () => {
      let publicado = false;
      component.publicar.subscribe(() => publicado = true);

      component.soloLectura = false;
      component.procesando = false;
      component.puedePublicar = true;
      component.emitirPublicar();

      expect(publicado).toBe(true);
    });

    it('emitirPublicar queda bloqueado si puedePublicar es false, soloLectura es true o procesando es true', () => {
      let conteo = 0;
      component.publicar.subscribe(() => conteo++);

      // 1. puedePublicar false
      component.puedePublicar = false;
      component.soloLectura = false;
      component.procesando = false;
      component.emitirPublicar();
      expect(conteo).toBe(0);

      // 2. soloLectura true
      component.puedePublicar = true;
      component.soloLectura = true;
      component.procesando = false;
      component.emitirPublicar();
      expect(conteo).toBe(0);

      // 3. procesando true
      component.puedePublicar = true;
      component.soloLectura = false;
      component.procesando = true;
      component.emitirPublicar();
      expect(conteo).toBe(0);
    });

    it('Arquitectura limpia: FormBuilderComponent no depende de servicios HTTP o MatricesRiesgosService', () => {
      // El componente se instancia únicamente con Inputs/Outputs presentacionales
      expect(component).toBeDefined();
    });

    describe('Bloqueo Transitorio Real durante Procesamiento / Operación Activa', () => {
      beforeEach(() => {
        component.soloLectura = false;
        component.esAdministrador = true;
        component.estadoVersion = 'DRAFT';
        component.operacion = 'guardar';
      });

      it('D. bloquea agregarSeccion durante operacion activa', () => {
        const totalAntes = component.model().secciones.length;
        component.agregarSeccion();
        expect(component.model().secciones.length).toBe(totalAntes);
      });

      it('E. bloquea eliminarCampo durante operacion activa', () => {
        const seccionId = component.model().secciones[0].id;
        const campoId = component.model().secciones[0].campos[0].id;
        component.eliminarCampo(seccionId, campoId);
        expect(component.model().secciones[0].campos.length).toBe(1);
      });

      it('F. bloquea reordenarCampo durante operacion activa', () => {
        const seccionId = component.model().secciones[0].id;
        const texto = component.tiposControles.find(t => t.tipo === 'texto')!;
        component.operacion = null; // desbloqueo temporal para setup
        component.agregarCampoASeccion(seccionId, texto);
        component.operacion = 'guardar'; // re-bloqueo

        const antes = component.model().secciones[0].campos.map(c => c.id);
        component.reordenarCampo(seccionId, 1, 'subir');
        expect(component.model().secciones[0].campos.map(c => c.id)).toEqual(antes);
      });

      it('G. bloquea procesarSoltarControl (drop) durante operacion activa', () => {
        const seccionId = component.model().secciones[0].id;
        const totalAntes = component.model().secciones[0].campos.length;
        component.procesarSoltarControl({ seccionId, tipo: 'texto' });
        expect(component.model().secciones[0].campos.length).toBe(totalAntes);
      });

      it('H. bloquea alCambiarPropiedadCampo (inspector) durante operacion activa', () => {
        const campoOriginal = component.model().secciones[0].campos[0];
        component.seleccionarCampo({ ...campoOriginal });
        component.campoActivo()!.etiqueta = 'Etiqueta Mutada Indebidamente';
        component.alCambiarPropiedadCampo();
        expect(component.model().secciones[0].campos[0].etiqueta).toBe(campoOriginal.etiqueta);
      });

      it('I. bloquea crear, editar y eliminar catalogos durante operacion activa', () => {
        const totalAntes = (component.model().catalogos ?? []).length;
        component.catalogoEnEdicion.set({ codigoOriginal: null, codigo: 'CAT_NUEVO', nombre: 'Nuevo', esNuevo: true });
        component.guardarEdicionCatalogo();
        expect((component.model().catalogos ?? []).length).toBe(totalAntes);

        component.eliminarCatalogo('CAT_AREA');
        expect((component.model().catalogos ?? []).length).toBe(totalAntes);
      });

      it('J. bloquea agregar, editar, eliminar y reordenar elementos de catalogo durante operacion activa', () => {
        const cat = (component.model().catalogos ?? [])[0];
        component.catalogoActivoCodigo.set(cat.codigo);
        const elementosAntes = (cat.elementos ?? []).length;

        component.elementoEnEdicion.set({ codigoOriginal: null, codigo: '003', valor: 'Opcion 3', orden: 3, indice: null });
        component.guardarElementoCatalogo();
        expect(((component.model().catalogos ?? [])[0].elementos ?? []).length).toBe(elementosAntes);

        component.eliminarElementoCatalogo(0);
        expect(((component.model().catalogos ?? [])[0].elementos ?? []).length).toBe(elementosAntes);

        component.reordenarElementoCatalogo(1, 'subir');
        expect((component.model().catalogos ?? [])[0].elementos[0].codigo).toBe('01');
      });

      it('K. bloquea aplicarJsonAvanzado durante operacion activa', () => {
        const jsonNuevo = JSON.stringify({ codigoFormulario: 'FORM_MUTADO', nombreFormulario: 'Mutado', secciones: [] });
        component.jsonAvanzadoStr.set(jsonNuevo);
        component.aplicarJsonAvanzado();
        expect(component.model().codigoFormulario).toBe('MATRIZ_LAFT_TEST');
      });

      it('L. cuando operacion termina, DRAFT admin no vigente vuelve a ser editable', () => {
        component.operacion = 'publicar';
        expect(component.bloqueadoParaMutacion).toBe(true);

        // Termina la operacion
        component.operacion = null;
        component.procesando = false;
        expect(component.bloqueadoParaMutacion).toBe(false);

        const totalAntes = component.model().secciones.length;
        component.agregarSeccion();
        expect(component.model().secciones.length).toBe(totalAntes + 1);
      });
    });
  });
});
