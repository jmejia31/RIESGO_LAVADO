import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { describe, beforeEach, it, expect, vi } from 'vitest';

import { AuthService } from '../../../../../core/auth/auth.service';
import { GlobalHttpStateService } from '../../../../../core/services/global-http-state.service';
import { MatricesRiesgosService } from '../../data-access/matrices-riesgos.service';
import { EvaluacionRiesgoDto, EvaluacionRiesgoResumenDto, MetodologiaFormulario, VersionFormularioDto } from '../../models/matrices-riesgos.models';
import { MatricesRiesgosComponent } from './matrices-riesgos.component';

describe('MatricesRiesgosComponent — regresiones UAT F6.5', () => {
  let fixture: ComponentFixture<MatricesRiesgosComponent>;
  let component: MatricesRiesgosComponent;
  let service: Record<string, ReturnType<typeof vi.fn>>;

  const version: VersionFormularioDto = {
    verId: 10,
    verFamiliaId: 1,
    verCodigo: 'MATRIZ_RIESGOS_LAFT_V1',
    verVersion: 1,
    verJson: JSON.stringify({
      codigoFormulario: 'MATRIZ_RIESGOS_LAFT_V1',
      nombreFormulario: 'Matriz QA',
      secciones: [{
        clave: 'identificacion',
        titulo: 'Identificación',
        orden: 1,
        campos: [{
          clave: 'area_principal',
          etiqueta: 'Área principal',
          tipo: 'texto',
          obligatorio: true,
          soloLectura: false
        }]
      }]
    }),
    verHash: 'hash',
    verEstado: 'PUBLISHED',
    verVigente: false,
    verFechaCreacion: '2026-08-21T00:00:00Z',
    verUsrCreacion: 1
  };

  const metodologia: MetodologiaFormulario = {
    versionFormularioId: 10,
    codigo: 'MATRIZ_RIESGOS_LAFT_V1',
    version: 1,
    secciones: [{
      clave: 'identificacion',
      titulo: 'Identificación',
      orden: 1,
      campos: [{
        clave: 'area_principal',
        etiqueta: 'Área principal',
        tipo: 'texto',
        obligatorio: true,
        soloLectura: false
      }]
    }],
    catalogos: [],
    reglas: []
  };

  const resumen: EvaluacionRiesgoResumenDto = {
    evaId: 12,
    evaRiesgoId: 5,
    riesgoCodigo: 'F11_TEST_5518',
    riesgoNombre: 'Riesgo de Certificación Fase 11 Actualizado',
    evaVersionId: 10,
    versionCodigo: 'MATRIZ_RIESGOS_LAFT_V1',
    versionNumero: 1,
    estado: 'BORRADOR',
    vri: 6,
    vrr: 5,
    nivelResidual: 'ALTO',
    fechaEval: '2026-08-21T14:00:00Z'
  };

  const detalleOriginal: EvaluacionRiesgoDto = {
    evaId: 12,
    evaRiesgoId: 5,
    evaVersionId: 10,
    evaEstado: 'BORRADOR',
    evaDataJson: JSON.stringify({ area_principal: 'Gerencia Riesgos' }),
    evaDataCalcJson: '{}',
    evaVri: 6,
    evaVrr: 5,
    evaFechaEval: '2026-08-21T14:00:00Z',
    evaUsrEval: 1,
    evaVersionRow: 1,
    evaActivo: true
  };

  beforeEach(async () => {
    service = {
      listarFamiliasFormulario: vi.fn().mockReturnValue(of([])),
      listarRiesgos: vi.fn().mockReturnValue(of([])),
      listarEvaluaciones: vi.fn().mockReturnValue(of({
        items: [resumen], pagina: 1, registrosPorPagina: 10, totalRegistros: 1, totalPaginas: 1
      })),
      obtenerVersionVigenteFormulario: vi.fn().mockReturnValue(of({ ...version, verVigente: true })),
      metodologiaVigente: vi.fn().mockReturnValue(of(metodologia)),
      obtenerEvaluacion: vi.fn().mockReturnValue(of(detalleOriginal)),
      metodologiaPorVersion: vi.fn().mockReturnValue(of(metodologia)),
      actualizarEvaluacion: vi.fn().mockReturnValue(of({ success: true }))
    };

    await TestBed.configureTestingModule({
      imports: [MatricesRiesgosComponent],
      providers: [
        { provide: MatricesRiesgosService, useValue: service },
        { provide: AuthService, useValue: { tieneRol: vi.fn().mockReturnValue(true) } },
        { provide: GlobalHttpStateService, useValue: { limpiarError: vi.fn() } }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(MatricesRiesgosComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('guarda un BORRADOR, relee la evaluación y conserva el modal abierto con datos verificados', () => {
    const valorActualizado = 'Gerencia Riesgos UAT-F6.5-02 — Edición BORRADOR';
    const detallePersistido: EvaluacionRiesgoDto = {
      ...detalleOriginal,
      evaDataJson: JSON.stringify({ area_principal: valorActualizado }),
      evaVersionRow: 2
    };

    service['obtenerEvaluacion']
      .mockReturnValueOnce(of(detalleOriginal))
      .mockReturnValueOnce(of(detallePersistido));

    component.editarEvaluacion(resumen);
    component.actualizarRespuesta(component.seccionesModal()[0].campos[0], valorActualizado);
    component.guardarEvaluacion();

    expect(service['actualizarEvaluacion']).toHaveBeenCalledWith(
      12,
      expect.objectContaining({
        evaId: 12,
        evaVersionId: 10,
        evaVersionRow: 1
      })
    );
    expect(service['obtenerEvaluacion']).toHaveBeenCalledTimes(2);
    expect(component.modalEditarAbierto()).toBe(true);
    expect(component.mensaje()).toContain('guardados y verificados correctamente');
    expect(component.evaluacionSeleccionada()?.evaVersionRow).toBe(2);
    expect(component.respuestas()['area_principal']).toBe(valorActualizado);
  });

  it('mantiene el modal abierto y falla cerrado si el GET posterior no coincide con el PUT', () => {
    service['obtenerEvaluacion']
      .mockReturnValueOnce(of(detalleOriginal))
      .mockReturnValueOnce(of(detalleOriginal));

    component.editarEvaluacion(resumen);
    component.actualizarRespuesta(component.seccionesModal()[0].campos[0], 'Cambio que el servidor no devolvió');
    component.guardarEvaluacion();

    expect(component.modalEditarAbierto()).toBe(true);
    expect(component.error()).toContain('no coincide con los cambios enviados');
  });

  it('Escape no cierra el modal de edición para evitar pérdida accidental de cambios', () => {
    component.editarEvaluacion(resumen);
    expect(component.modalEditarAbierto()).toBe(true);

    const event = new Event('keydown', { cancelable: true });
    const preventDefault = vi.spyOn(event, 'preventDefault');
    component.manejarTeclaEscape(event);

    expect(preventDefault).toHaveBeenCalledOnce();
    expect(component.modalEditarAbierto()).toBe(true);
  });
});
