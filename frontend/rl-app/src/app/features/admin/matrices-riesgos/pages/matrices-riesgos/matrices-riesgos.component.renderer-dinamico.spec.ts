import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { FormsModule } from '@angular/forms';
import { of } from 'rxjs';
import { describe, beforeEach, it, expect, vi } from 'vitest';

import { MatricesRiesgosComponent } from './matrices-riesgos.component';
import { MatricesRiesgosService } from '../../data-access/matrices-riesgos.service';
import { MetodologiaFormulario, VersionFormularioDto } from '../../models/matrices-riesgos.models';

const versionDinamica = (verJson: string): VersionFormularioDto => ({
  verId: 15,
  verFamiliaId: 1,
  verCodigo: 'MATRIZ_RENDERER',
  verVersion: 3,
  verJson,
  verHash: 'hash',
  verEstado: 'PUBLISHED',
  verVigente: true,
  verFechaInicio: null,
  verFechaFin: null,
  verFechaCreacion: '2026-08-18T10:00:00Z',
  verUsrCreacion: 1
});

describe('MatricesRiesgosComponent — integración del renderer dinámico', () => {
  let fixture: ComponentFixture<MatricesRiesgosComponent>;
  let component: MatricesRiesgosComponent;
  let serviceMock: Record<string, ReturnType<typeof vi.fn>>;

  const definicionTodosLosTipos = JSON.stringify({
    codigoFormulario: 'MATRIZ_RENDERER',
    nombreFormulario: 'Renderer integral',
    catalogos: [{
      codigo: 'NIVELES',
      nombre: 'Niveles publicados',
      elementos: [
        { codigo: 'A', valor: 'Alto publicado', orden: 1 },
        { codigo: 'B', valor: 'Bajo publicado', orden: 2 }
      ]
    }],
    secciones: [{
      clave: 'general',
      titulo: 'General',
      orden: 1,
      columnasPorFila: 3,
      campos: [
        { clave: 'texto', etiqueta: 'Texto', tipo: 'texto', obligatorio: true, soloLectura: false },
        { clave: 'numero', etiqueta: 'Número', tipo: 'numero', obligatorio: true, soloLectura: false },
        { clave: 'fecha', etiqueta: 'Fecha', tipo: 'fecha', obligatorio: false, soloLectura: false },
        { clave: 'largo', etiqueta: 'Largo', tipo: 'texto-largo', obligatorio: false, soloLectura: false },
        { clave: 'selector', etiqueta: 'Selector', tipo: 'selector-catalogo', codigoCatalogo: 'NIVELES', obligatorio: false, soloLectura: false },
        { clave: 'radio', etiqueta: 'Radio', tipo: 'radio', opciones: ['Sí', 'No'], obligatorio: false, soloLectura: false },
        { clave: 'multi', etiqueta: 'Múltiple', tipo: 'catalogo-multiple', codigoCatalogo: 'NIVELES', obligatorio: true, soloLectura: false },
        { clave: 'check', etiqueta: 'Check', tipo: 'checkbox', obligatorio: true, soloLectura: false },
        { clave: 'calc', etiqueta: 'Cálculo', tipo: 'formula', formula: 'numero * 2', obligatorio: false, soloLectura: false }
      ]
    }]
  });

  const metodologia: MetodologiaFormulario = {
    versionFormularioId: 15,
    codigo: 'MATRIZ_RENDERER',
    version: 3,
    secciones: [],
    catalogos: [{
      codigo: 'NIVELES',
      nombre: 'Niveles',
      elementos: [
        { codigo: 'A', valor: 'Alto', orden: 1 },
        { codigo: 'B', valor: 'Bajo', orden: 2 }
      ]
    }],
    reglas: []
  };

  beforeEach(async () => {
    serviceMock = {
      listarFamiliasFormulario: vi.fn().mockReturnValue(of([])),
      listarRiesgos: vi.fn().mockReturnValue(of([])),
      listarEvaluaciones: vi.fn().mockReturnValue(of({ items: [], pagina: 1, registrosPorPagina: 10, totalRegistros: 0, totalPaginas: 0 })),
      obtenerVersionVigenteFormulario: vi.fn().mockReturnValue(of(null)),
      metodologiaVigente: vi.fn().mockReturnValue(of(metodologia)),
      crearEvaluacion: vi.fn().mockReturnValue(of({})),
      actualizarEvaluacion: vi.fn().mockReturnValue(of({})),
      obtenerEvaluacion: vi.fn().mockReturnValue(of(null)),
      metodologiaPorVersion: vi.fn().mockReturnValue(of(metodologia)),
      obtenerFamiliaFormularioPorId: vi.fn().mockReturnValue(of({ famId: 1 }))
    };

    await TestBed.configureTestingModule({
      imports: [MatricesRiesgosComponent, HttpClientTestingModule, FormsModule],
      providers: [{ provide: MatricesRiesgosService, useValue: serviceMock }]
    }).compileComponents();

    fixture = TestBed.createComponent(MatricesRiesgosComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();

    component.versionVigente.set(versionDinamica(definicionTodosLosTipos));
    component.metodologia.set(metodologia);
    component.cargandoFormulario.set(false);
  });

  it('normaliza los nueve tipos del contrato vigente antes de renderizar', () => {
    const tipos = component.secciones()[0].campos.map(campo => campo.tipo);
    expect(tipos).toEqual([
      'texto',
      'numero',
      'fecha',
      'texto-largo',
      'selector-catalogo',
      'radio',
      'catalogo-multiple',
      'checkbox',
      'formula'
    ]);
    expect(component.secciones()[0].campos.at(-1)?.soloLectura).toBe(true);
  });

  it('usa el componente reusable para todos los campos del modal Nueva evaluación', () => {
    component.modalNuevaEvaluacionAbierto.set(true);
    fixture.detectChanges();

    const renderers = fixture.nativeElement.querySelectorAll('dialog app-dynamic-field-renderer');
    expect(renderers.length).toBe(9);
    expect(fixture.nativeElement.querySelector('#campo-new-radio input[type="radio"]')).not.toBeNull();
    expect(fixture.nativeElement.querySelectorAll('#campo-new-multi input[type="checkbox"]').length).toBe(2);
  });

  it('usa la definición y los catálogos de la versión publicada para Nueva Evaluación', () => {
    component.metodologia.set({ ...metodologia, catalogos: [] });
    component.modalNuevaEvaluacionAbierto.set(true);
    fixture.detectChanges();

    expect(component.seccionesModal()[0].columnasPorFila).toBe(3);
    expect(component.opcionesCatalogo(component.seccionesModal()[0].campos[4])).toEqual([
      { codigo: 'A', valor: 'Alto publicado', orden: 1 },
      { codigo: 'B', valor: 'Bajo publicado', orden: 2 }
    ]);
    expect(fixture.nativeElement.querySelector('[data-evaluation-section="general"]')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('[data-evaluation-field="selector"]')).not.toBeNull();
  });

  it('considera 0, false y una selección múltiple no vacía como respuestas presentes', () => {
    component.riesgoId.set(44);
    component.respuestas.set({
      texto: 'valor',
      numero: 0,
      multi: ['A'],
      check: false
    });

    expect(component.totalCompletados()).toBe(4);
    expect(component.puedeGuardar()).toBe(true);
  });

  it('bloquea guardado cuando un catálogo múltiple obligatorio queda vacío', () => {
    component.riesgoId.set(44);
    component.respuestas.set({
      texto: 'valor',
      numero: 0,
      multi: [],
      check: false
    });

    expect(component.puedeGuardar()).toBe(false);
  });

  it('serializa string[] sin convertir la selección múltiple a texto', () => {
    component.riesgoId.set(44);
    component.respuestas.set({
      texto: 'valor',
      numero: 2,
      multi: ['A', 'B'],
      check: false
    });

    component.guardarEvaluacion();

    expect(serviceMock['crearEvaluacion']).toHaveBeenCalledTimes(1);
    const dto = serviceMock['crearEvaluacion'].mock.calls[0][0];
    const data = JSON.parse(dto.evaDataJson);
    expect(data.multi).toEqual(['A', 'B']);
    expect(data.check).toBe(false);
  });

  it('degrada un tipo futuro a desconocido y lo mantiene bloqueado', () => {
    component.versionVigente.set(versionDinamica(JSON.stringify({
      secciones: [{
        clave: 'futuro',
        campos: [{ clave: 'nuevo', etiqueta: 'Nuevo', tipo: 'control-v9', obligatorio: false, soloLectura: false }]
      }]
    })));
    component.modalNuevaEvaluacionAbierto.set(true);
    fixture.detectChanges();

    const campo = component.secciones()[0].campos[0];
    expect(campo.tipo).toBe('desconocido');
    expect(campo.tipoOriginal).toBe('control-v9');
    expect(campo.soloLectura).toBe(true);
    expect(fixture.nativeElement.textContent).toContain('Tipo de campo no soportado: control-v9');
  });

  it('usa el mismo renderer en modo lectura y preserva false como No', () => {
    const metodologiaHistorica: MetodologiaFormulario = {
      ...metodologia,
      secciones: [{
        clave: 'hist',
        titulo: 'Histórico',
        orden: 1,
        campos: [{ clave: 'check', etiqueta: 'Confirmación', tipo: 'bool', obligatorio: false, soloLectura: false }]
      }]
    };

    component.metodologiaHistorica.set(metodologiaHistorica);
    component.respuestas.set({ check: false });
    component.modalVerAbierto.set(true);
    fixture.detectChanges();

    const renderer = fixture.nativeElement.querySelector('dialog app-dynamic-field-renderer');
    expect(renderer).not.toBeNull();
    expect(renderer.textContent).toContain('No');
  });

  it('normaliza una definición JSON malformada a formulario vacío sin romper el componente', () => {
    component.versionVigente.set(versionDinamica('{json-invalido'));
    component.modalNuevaEvaluacionAbierto.set(true);
    fixture.detectChanges();

    expect(component.secciones()).toEqual([]);
    expect(fixture.nativeElement.textContent).toContain('no contiene secciones configuradas');
  });
});
