import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { MatricesRiesgosComponent } from './matrices-riesgos.component';
import { MatricesRiesgosService } from '../../data-access/matrices-riesgos.service';
import { VersionFormularioDto } from '../../models/matrices-riesgos.models';
import { AuthService } from '../../../../../core/auth/auth.service';
import { CalculoConfiguracionService } from '../../data-access/calculo-configuracion.service';

describe('MatricesRiesgosComponent — Persistencia Bidireccional de Plantilla (F6.3)', () => {
  let fixture: ComponentFixture<MatricesRiesgosComponent>;
  let component: MatricesRiesgosComponent;
  let service: {
    obtenerVersionFormulario: ReturnType<typeof vi.fn>;
    obtenerVersionVigenteFormulario: ReturnType<typeof vi.fn>;
    listarHistorialVersionesFormulario: ReturnType<typeof vi.fn>;
    listarFamiliasFormulario: ReturnType<typeof vi.fn>;
    actualizarBorradorFormulario: ReturnType<typeof vi.fn>;
    metodologiaVigente: ReturnType<typeof vi.fn>;
    metodologiaPorVersion: ReturnType<typeof vi.fn>;
    obtenerEvaluacion: ReturnType<typeof vi.fn>;
    listarRiesgos: ReturnType<typeof vi.fn>;
    listarEvaluaciones: ReturnType<typeof vi.fn>;
    obtenerConsolidado: ReturnType<typeof vi.fn>;
  };

  const jsonRicoInicial = JSON.stringify({
    codigoFormulario: 'MATRIZ_LAFT_BIDIRECCIONAL',
    nombreFormulario: 'Matriz LAFT Bidireccional',
    extensionFutura: { deepMeta: 'seguro', flag: false, count: 0, ref: null },
    secciones: [
      {
        clave: 'identificacion',
        titulo: 'Identificación',
        orden: 1,
        campos: [
          { clave: 'area', etiqueta: 'Área', tipo: 'texto', obligatorio: true, soloLectura: false }
        ]
      }
    ],
    catalogos: [
      {
        codigo: 'CAT_SECTOR',
        nombre: 'Sectores Económicos',
        elementos: [
          { codigo: '001', valor: 'Sector Financiero', orden: 1 },
          { codigo: 'G-IVM', valor: 'Grupo IVM Especial', orden: 2 }
        ]
      }
    ]
  });

  const versionMock: VersionFormularioDto = {
    verId: 100,
    verFamiliaId: 1,
    verCodigo: 'MATRIZ_LAFT_BIDIRECCIONAL',
    verVersion: 1,
    verJson: jsonRicoInicial,
    verHash: 'sha_test_100',
    verEstado: 'DRAFT',
    verVigente: false,
    verFechaCreacion: '2026-08-20T10:00:00Z',
    verUsrCreacion: 1
  };

  beforeEach(async () => {
    service = {
      obtenerVersionFormulario: vi.fn().mockReturnValue(of(versionMock)),
      obtenerVersionVigenteFormulario: vi.fn().mockReturnValue(of(versionMock)),
      listarHistorialVersionesFormulario: vi.fn().mockReturnValue(of([versionMock])),
      listarFamiliasFormulario: vi.fn().mockReturnValue(of([{
        famId: 1,
        famCodigo: 'MATRIZ_LAFT_BIDIRECCIONAL',
        famNombre: 'Familia Test',
        famDescripcion: '',
        famActivo: true
      }])),
      actualizarBorradorFormulario: vi.fn().mockReturnValue(of({ success: true, mensaje: 'Guardado' })),
      metodologiaVigente: vi.fn().mockReturnValue(of({ versionFormularioId: 100, codigo: 'MATRIZ_LAFT_BIDIRECCIONAL', version: 1, secciones: [], catalogos: [], reglas: [] })),
      metodologiaPorVersion: vi.fn().mockReturnValue(of({ versionFormularioId: 100, codigo: 'MATRIZ_LAFT_BIDIRECCIONAL', version: 1, secciones: [], catalogos: [], reglas: [] })),
      obtenerEvaluacion: vi.fn().mockReturnValue(of(null)),
      listarRiesgos: vi.fn().mockReturnValue(of([])),
      listarEvaluaciones: vi.fn().mockReturnValue(of({ items: [], pagina: 1, registrosPorPagina: 10, totalRegistros: 0, totalPaginas: 0 })),
      obtenerConsolidado: vi.fn().mockReturnValue(of([]))
    };

    await TestBed.configureTestingModule({
      imports: [MatricesRiesgosComponent],
      providers: [
        { provide: AuthService, useValue: { tieneRol: vi.fn().mockReturnValue(true) } },
        { provide: MatricesRiesgosService, useValue: service },
        { provide: CalculoConfiguracionService, useValue: { listarFormulas: vi.fn().mockReturnValue(of([])), reemplazarFormulaUsos: vi.fn().mockReturnValue(of({ success: true })) } }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(MatricesRiesgosComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('1. Apertura autoritativa: consulta GET /formularios/{verId} y carga el verJson autoritativo en el Builder', () => {
    const versionSeleccionada: VersionFormularioDto = {
      ...versionMock,
      verId: 100,
      verJson: '{"versionVieja": true}' // Simulación de copia stale en historial
    };

    component.abrirDefinicion(versionSeleccionada);

    expect(service.obtenerVersionFormulario).toHaveBeenCalledWith(100);
    expect(component.versionEditando()?.verId).toBe(100);
    // Debe haber cargado la definición autoritativa retornada por el servicio, no la stale
    expect(component.definicionTecnica).toContain('MATRIZ_LAFT_BIDIRECCIONAL');
    expect(component.definicionTecnica).toContain('001');
    expect(component.definicionTecnica).toContain('G-IVM');
  });

  it('2. Apertura fail-closed: ante error HTTP en GET /formularios/{verId}, no abre el modal con datos dudosos', () => {
    service.obtenerVersionFormulario.mockReturnValueOnce(throwError(() => ({ status: 404, message: 'Not found' })));

    component.abrirDefinicion(versionMock);

    expect(component.versionEditando()).toBeNull();
    expect(component.error()).toContain('No se pudo cargar la versión autoritativa');
  });

  it('3. Flujo completo PUT -> GET mismo verId -> Equivalencia semántica -> Éxito y cierre normal', () => {
    component.abrirDefinicion(versionMock);
    expect(component.versionEditando()).not.toBeNull();

    // Modificación controlada que preserva metadatos lossless, '001', 'G-IVM', 0, false, null
    const jsonModificado = JSON.stringify({
      codigoFormulario: 'MATRIZ_LAFT_BIDIRECCIONAL',
      nombreFormulario: 'Matriz LAFT Modificada',
      extensionFutura: { deepMeta: 'seguro', flag: false, count: 0, ref: null },
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
        {
          codigo: 'CAT_SECTOR',
          nombre: 'Sectores Económicos',
          elementos: [
            { codigo: '001', valor: 'Sector Financiero', orden: 1 },
            { codigo: 'G-IVM', valor: 'Grupo IVM Especial', orden: 2 }
          ]
        }
      ]
    });

    component.definicionTecnica = jsonModificado;

    // Simular que el backend guardó y al releer retorna el mismo JSON con distinto orden de propiedades
    const jsonPersistidoConClavesEnOtroOrden = JSON.stringify({
      nombreFormulario: 'Matriz LAFT Modificada',
      codigoFormulario: 'MATRIZ_LAFT_BIDIRECCIONAL',
      catalogos: [
        {
          elementos: [
            { valor: 'Sector Financiero', codigo: '001', orden: 1 },
            { valor: 'Grupo IVM Especial', codigo: 'G-IVM', orden: 2 }
          ],
          nombre: 'Sectores Económicos',
          codigo: 'CAT_SECTOR'
        }
      ],
      secciones: [
        {
          campos: [
            { soloLectura: false, obligatorio: true, tipo: 'texto', etiqueta: 'Área principal', clave: 'area' }
          ],
          orden: 1,
          titulo: 'Identificación',
          clave: 'identificacion'
        }
      ],
      extensionFutura: { count: 0, ref: null, flag: false, deepMeta: 'seguro' }
    });

    service.obtenerVersionFormulario.mockReturnValue(of({
      ...versionMock,
      verJson: jsonPersistidoConClavesEnOtroOrden
    }));

    component.guardarDefinicion();

    expect(service.actualizarBorradorFormulario).toHaveBeenCalledWith(100, jsonModificado);
    expect(service.obtenerVersionFormulario).toHaveBeenCalledWith(100);
    // Cierre exitoso y mensaje
    expect(component.versionEditando()).toBeNull();
    expect(component.mensaje()).toContain('actualizada y verificada');
  });

  it('4. Discrepancia semántica post-save: bloquea el cierre (fail-closed), no muestra éxito y preserva el contexto de edición', () => {
    component.abrirDefinicion(versionMock);
    expect(component.versionEditando()).not.toBeNull();

    const jsonEnviado = JSON.stringify({
      codigoFormulario: 'MATRIZ_LAFT_BIDIRECCIONAL',
      secciones: [],
      catalogos: [{ codigo: 'CAT_SECTOR', elementos: [{ codigo: '001', valor: 'Financiero' }] }]
    });

    component.definicionTecnica = jsonEnviado;

    // Simular que el backend persistió algo diferente o incompleto (pérdida de catálogo)
    const jsonPersistidoDiscrepante = JSON.stringify({
      codigoFormulario: 'MATRIZ_LAFT_BIDIRECCIONAL',
      secciones: [],
      catalogos: [] // Catálogo ausente
    });

    service.obtenerVersionFormulario.mockReturnValue(of({
      ...versionMock,
      verJson: jsonPersistidoDiscrepante
    }));

    component.guardarDefinicion();

    expect(service.actualizarBorradorFormulario).toHaveBeenCalledWith(100, jsonEnviado);
    // No debe haber cerrado el modal
    expect(component.versionEditando()).not.toBeNull();
    expect(component.definicionTecnica).toBe(jsonEnviado);
    expect(component.error()).toContain('no coincide semánticamente');
    expect(component.mensaje()).toBeFalsy();
  });

  it('5. Discrepancia de orden en array: detecta que ["001", "G-IVM"] !== ["G-IVM", "001"] y bloquea cierre', () => {
    component.abrirDefinicion(versionMock);

    const jsonEnviado = JSON.stringify({
      codigoFormulario: 'FORM_ORDEN',
      catalogos: [{ codigo: 'CAT_A', elementos: [{ codigo: '001' }, { codigo: 'G-IVM' }] }]
    });
    component.definicionTecnica = jsonEnviado;

    // El servidor retorna el array con orden invertido
    const jsonPersistidoInvertido = JSON.stringify({
      codigoFormulario: 'FORM_ORDEN',
      catalogos: [{ codigo: 'CAT_A', elementos: [{ codigo: 'G-IVM' }, { codigo: '001' }] }]
    });

    service.obtenerVersionFormulario.mockReturnValue(of({
      ...versionMock,
      verJson: jsonPersistidoInvertido
    }));

    component.guardarDefinicion();

    expect(component.versionEditando()).not.toBeNull();
    expect(component.error()).toContain('no coincide semánticamente');
  });

  it('6. Error en GET post-save: falla de forma segura (fail-closed) sin cerrar el editor', () => {
    component.abrirDefinicion(versionMock);

    component.definicionTecnica = JSON.stringify({ codigoFormulario: 'FORM_TEST', secciones: [] });

    // PUT exitoso pero GET falla
    service.actualizarBorradorFormulario.mockReturnValue(of({ success: true }));
    service.obtenerVersionFormulario.mockReturnValue(throwError(() => ({ status: 500, message: 'Server error' })));

    component.guardarDefinicion();

    expect(component.versionEditando()).not.toBeNull();
    expect(component.error()).toContain('No se pudo verificar la persistencia');
  });

  it('7. Ciclo cerrar -> reabrir conserva la misma definición sin pérdida', () => {
    // 1. Abrir
    component.abrirDefinicion(versionMock);
    expect(component.definicionTecnica).toContain('MATRIZ_LAFT_BIDIRECCIONAL');

    // 2. Cerrar
    component.versionEditando.set(null);
    expect(component.versionEditando()).toBeNull();

    // 3. Reabrir misma versión
    component.abrirDefinicion(versionMock);
    expect(component.versionEditando()?.verId).toBe(100);
    expect(component.definicionTecnica).toContain('MATRIZ_LAFT_BIDIRECCIONAL');
    expect(component.definicionTecnica).toContain('CAT_SECTOR');
    expect(component.definicionTecnica).toContain('G-IVM');
  });
});
