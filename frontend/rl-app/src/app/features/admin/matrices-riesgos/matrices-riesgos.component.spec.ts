import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { ConfiguracionService } from '../../../core/configuration/configuracion.service';
import { MatricesRiesgosService } from './data-access/matrices-riesgos.service';
import { MatricesRiesgosComponent } from './matrices-riesgos.component';

describe('MatricesRiesgosComponent', () => {
  let fixture: ComponentFixture<MatricesRiesgosComponent>;
  let component: MatricesRiesgosComponent;
  let service: Record<string, ReturnType<typeof vi.fn>>;

  beforeEach(async () => {
    service = {
      metodologiaVigente: vi.fn(() => of({ variables: [], escalasCatalogo: [], escalasRiesgo: [] })),
      dashboard: vi.fn(() => of({})),
      reporte: vi.fn(() => of({})),
      listar: vi.fn(() => of([])),
      listarCriterios: vi.fn(() => of([])),
      obtener: vi.fn(() => of({})),
      historial: vi.fn(() => of([])),
      crear: vi.fn(() => of({ matrizId: 1, sujetoTipo: 'PROVEEDOR' })),
      actualizar: vi.fn(() => of({ matrizId: 1, sujetoTipo: 'PROVEEDOR' })),
      calcular: vi.fn(() => of({})),
      recalcular: vi.fn(() => of({})),
      crearCriterio: vi.fn(() => of({ criterioId: 1 })),
      actualizarCriterio: vi.fn(() => of({ criterioId: 1 })),
      cambiarEstado: vi.fn(() => of({ success: true })),
      eliminarMatriz: vi.fn(() => of({ success: true })),
      inactivarCriterio: vi.fn(() => of({ success: true })),
      eliminarCriterio: vi.fn(() => of({ success: true }))
    };

    await TestBed.configureTestingModule({
      imports: [MatricesRiesgosComponent],
      providers: [
        { provide: MatricesRiesgosService, useValue: service },
        { provide: ConfiguracionService, useValue: { configSistema: vi.fn(() => null) } }
      ]
    })
      .overrideComponent(MatricesRiesgosComponent, { set: { template: '' } })
      .compileComponents();

    fixture = TestBed.createComponent(MatricesRiesgosComponent);
    component = fixture.componentInstance;
  });

  afterEach(() => {
    fixture.destroy();
    TestBed.resetTestingModule();
  });

  it('lista matrices con los filtros activos y finaliza la carga', () => {
    const matrices = [{ matrizId: 7, nombreSujeto: 'Proveedor Uno' }];
    component.filtroBuscar.set('Proveedor');
    component.filtroEstado.set('CALCULADA');
    component.filtroSujetoTipo.set('PROVEEDOR');
    service['listar'].mockReturnValue(of(matrices));

    component.cargarMatrices();

    expect(service['listar']).toHaveBeenCalledWith({
      buscar: 'Proveedor', estado: 'CALCULADA', sujetoTipo: 'PROVEEDOR'
    });
    expect(component.matrices()).toEqual(matrices);
    expect(component.cargando()).toBe(false);
  });

  it('muestra el mensaje devuelto por la API y recupera el indicador al fallar el listado', () => {
    service['listar'].mockReturnValue(throwError(() => ({ error: { mensaje: 'Consulta rechazada' } })));

    component.cargarMatrices();

    expect(component.error()).toBe('Consulta rechazada');
    expect(component.cargando()).toBe(false);
  });

  it('carga el reporte usando el filtro vigente', () => {
    const reporte = { totales: { totalMatrices: 3 } };
    component.reporteFiltro.set({ estado: 'ACTIVA' });
    service['reporte'].mockReturnValue(of(reporte));

    component.cargarReporte();

    expect(service['reporte']).toHaveBeenCalledWith({ estado: 'ACTIVA' });
    expect(component.reporte()).toEqual(reporte);
    expect(component.cargandoReporte()).toBe(false);
  });

  it('conserva un error controlado y detiene la carga si falla el reporte', () => {
    service['reporte'].mockReturnValue(throwError(() => new Error('Servicio temporalmente no disponible')));

    component.cargarReporte();

    expect(component.error()).toBe('Servicio temporalmente no disponible');
    expect(component.cargandoReporte()).toBe(false);
  });

  it('selecciona una matriz y solicita su historial', () => {
    const matriz = { matrizId: 21, nombreSujeto: 'Institucional' };
    const historial = [{ historialId: 5, accion: 'CREAR' }];
    service['obtener'].mockReturnValue(of(matriz));
    service['historial'].mockReturnValue(of(historial));

    component.seleccionarMatriz(21);

    expect(service['obtener']).toHaveBeenCalledWith(21);
    expect(service['historial']).toHaveBeenCalledWith(21);
    expect(component.matrizSeleccionada()).toEqual(matriz);
    expect(component.historial()).toEqual(historial);
    expect(component.cargando()).toBe(false);
  });

  it('detiene la carga inicial y no consulta dependencias si falla la metodologia', () => {
    service['metodologiaVigente'].mockReturnValue(throwError(() => ({ error: { detalle: 'Metodologia no configurada' } })));

    component.cargarTodo();

    expect(component.error()).toBe('Metodologia no configurada');
    expect(component.cargando()).toBe(false);
    expect(service['dashboard']).not.toHaveBeenCalled();
    expect(service['reporte']).not.toHaveBeenCalled();
    expect(service['listar']).not.toHaveBeenCalled();
  });

  it('bloquea la creacion cuando no existen variables configuradas', () => {
    component.nuevaMatriz.nombreSujeto = 'Proveedor Uno';

    component.crearMatriz();

    expect(component.error()).toBe('No existen variables configuradas para el tipo de sujeto seleccionado.');
    expect(service['crear']).not.toHaveBeenCalled();
    expect(component.guardando()).toBe(false);
  });

  it('bloquea una matriz duplicada antes de escribir', () => {
    component.nuevaMatriz.nombreSujeto = 'Proveedor Uno';
    component.capturasVariables.set([{
      variableId: 1, criterioId: null, puntaje: 4, valorCapturado: '',
      justificacion: 'Evaluacion', fuenteDato: 'CAPTURA'
    }] as never);
    component.matricesDuplicadas.set([{ matrizId: 9, nombreSujeto: 'Proveedor Uno' }] as never);

    component.crearMatriz();

    expect(component.error()).toContain('Ya existe una matriz activa');
    expect(service['crear']).not.toHaveBeenCalled();
  });

  it('crea una matriz valida y solicita su calculo automatico', () => {
    const matrizCreada = { matrizId: 31, sujetoTipo: 'PROVEEDOR', nombreSujeto: 'Proveedor Uno' };
    service['crear'].mockReturnValue(of(matrizCreada));
    component.nuevaMatriz.nombreSujeto = 'Proveedor Uno';
    component.nuevaMatriz.documento = 'RTN-01';
    component.capturasVariables.set([{
      variableId: 1, criterioId: null, puntaje: 3, valorCapturado: '3',
      justificacion: 'Evaluacion inicial', fuenteDato: 'CAPTURA'
    }] as never);

    component.crearMatriz();

    expect(service['crear']).toHaveBeenCalledWith(expect.objectContaining({
      nombreSujeto: 'Proveedor Uno', documento: 'RTN-01'
    }));
    expect(service['calcular']).toHaveBeenCalledWith(31, 'FACTOR');
    expect(component.mensaje()).toBe('Matriz creada y calculada automáticamente.');
    expect(component.guardando()).toBe(false);
  });

  it('recupera el formulario cuando la creacion de la matriz falla', () => {
    service['crear'].mockReturnValue(throwError(() => ({ error: { mensaje: 'Documento duplicado' } })));
    component.nuevaMatriz.nombreSujeto = 'Proveedor Uno';
    component.capturasVariables.set([{
      variableId: 1, criterioId: null, puntaje: 3, valorCapturado: '',
      justificacion: '', fuenteDato: 'CAPTURA'
    }] as never);

    component.crearMatriz();

    expect(component.error()).toBe('Documento duplicado');
    expect(component.guardando()).toBe(false);
  });

  it('valida los campos obligatorios de un criterio antes de escribir', () => {
    component.criteriosForm = {
      variableId: 0, escalaId: null, valorDesde: null, valorHasta: null,
      puntaje: 2, descripcion: ''
    };

    component.guardarCriterio();

    expect(component.error()).toBe('La variable y la descripción del criterio son obligatorias.');
    expect(service['crearCriterio']).not.toHaveBeenCalled();
  });

  it('exige motivo para recalcular y ejecuta la operacion al completarlo', () => {
    const matriz = { matrizId: 18, sujetoTipo: 'PROVEEDOR' } as never;
    component.recalcularMatriz(matriz);

    component.confirmarModal();

    expect(component.modalError()).toBe('El motivo es obligatorio para completar esta acción.');
    expect(service['recalcular']).not.toHaveBeenCalled();

    component.actualizarModalMotivo('Actualizacion anual');
    component.confirmarModal();

    expect(service['recalcular']).toHaveBeenCalledWith(18, 'Actualizacion anual', 'FACTOR');
    expect(component.mensaje()).toBe('Matriz recalculada correctamente.');
    expect(component.guardando()).toBe(false);
  });

  it('rechaza un rango de criterio cuyo valor inicial supera al final', () => {
    component.criteriosForm = {
      variableId: 3, escalaId: null, valorDesde: 10, valorHasta: 5,
      puntaje: 4, descripcion: 'Rango invalido'
    };

    component.guardarCriterio();

    expect(component.error()).toBe('El valor desde no puede ser mayor que el valor hasta.');
    expect(service['crearCriterio']).not.toHaveBeenCalled();
  });

  it('impide reutilizar el mismo motivo en un cambio de estado', () => {
    const matriz = { matrizId: 18, sujetoTipo: 'PROVEEDOR' } as never;
    component.historial.set([{
      historialId: 1, accion: 'CAMBIO_ESTADO', motivo: 'Revision anual'
    }] as never);
    component.cambiarEstado(matriz, 'APROBADA');
    component.actualizarModalMotivo(' revision anual ');

    component.confirmarModal();

    expect(component.modalError()).toContain('Este motivo ya fue utilizado');
    expect(service['cambiarEstado']).not.toHaveBeenCalled();
  });

  it('cambia el estado con un motivo nuevo y refresca la matriz', () => {
    const matriz = { matrizId: 18, sujetoTipo: 'PROVEEDOR' } as never;
    component.cambiarEstado(matriz, 'APROBADA');
    component.actualizarModalMotivo('Aprobacion del comite');

    component.confirmarModal();

    expect(service['cambiarEstado']).toHaveBeenCalledWith(18, 'APROBADA', 'Aprobacion del comite');
    expect(component.mensaje()).toBe('Estado actualizado correctamente.');
    expect(component.modalOperacion()).toBeNull();
    expect(component.guardando()).toBe(false);
  });

  it('elimina logicamente una matriz y limpia su seleccion', () => {
    const matriz = { matrizId: 25, sujetoTipo: 'INSTITUCIONAL' } as never;
    component.matrizSeleccionada.set({ matrizId: 25 } as never);
    component.historial.set([{ historialId: 3 }] as never);
    component.eliminarMatriz(matriz);
    component.actualizarModalMotivo('Registro creado por error');

    component.confirmarModal();

    expect(service['eliminarMatriz']).toHaveBeenCalledWith(25, 'Registro creado por error');
    expect(component.matrizSeleccionada()).toBeNull();
    expect(component.historial()).toEqual([]);
    expect(component.mensaje()).toBe('Matriz eliminada correctamente.');
    expect(component.modalOperacion()).toBeNull();
  });

  it('inactiva un criterio con motivo y recarga el catalogo', () => {
    const criterio = { criterioId: 7, variableId: 3, activo: true } as never;
    component.inactivarCriterio(criterio);
    component.actualizarModalMotivo('Criterio fuera de vigencia');

    component.confirmarModal();

    expect(service['inactivarCriterio']).toHaveBeenCalledWith(7, 'Criterio fuera de vigencia');
    expect(service['listarCriterios']).toHaveBeenCalled();
    expect(component.mensaje()).toBe('Criterio inactivado correctamente.');
    expect(component.modalOperacion()).toBeNull();
  });

  it('mantiene abierto el modal si falla la eliminacion de un criterio', () => {
    service['eliminarCriterio'].mockReturnValue(throwError(() => ({ error: { mensaje: 'Criterio en uso' } })));
    const criterio = { criterioId: 9, variableId: 3, activo: false } as never;
    component.eliminarCriterio(criterio);
    component.actualizarModalMotivo('Depuracion del catalogo');

    component.confirmarModal();

    expect(service['eliminarCriterio']).toHaveBeenCalledWith(9, 'Depuracion del catalogo');
    expect(component.modalError()).toBe('Criterio en uso');
    expect(component.modalOperacion()?.tipo).toBe('eliminarCriterio');
    expect(component.guardando()).toBe(false);
  });
});
