import { ChangeDetectionStrategy, Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { forkJoin, of } from 'rxjs';
import { ListasService } from '../../data-access/listas.service';
import {
  CoincidenciaJuridica,
  CoincidenciaNatural,
  CoincidenciaEmpleado,
  DetalleCoincidenciaNatural,
  DetalleCoincidenciaEmpleado,
  Seguimiento
} from '../../models/listas.models';
import * as XLSX from '../../../../../core/utils/excel-export.util';
import {
  crearLibroExcelInstitucionalDesdeReporte,
  InstitutionalReportDefinition
} from '../../../../../core/reporting/institutional-report-parity.util';
import { MonitoreoListasComponent as MonitoreoListasComponentBase } from './monitoreo-listas.component.impl';

type ReporteListaPrincipal = {
  title: string;
  headers: string[];
  rows: string[][];
};

type RegistroMonitoreo = {
  tieneMotivo?: boolean;
  esManual?: boolean;
};

type RangoSeguimientoReporte = {
  desde?: string;
  hasta?: string;
  texto: string;
};

type PositivoReporte = {
  motivoIngreso?: string | null;
  origenRegistro?: string | null;
  fechaRegistroInterno?: string | null;
} | null;

type InstanciaInternaMonitoreo = {
  configService: {
    configSistema(): {
      nombreInstitucion?: string;
      nombreSistema?: string;
    } | null;
  };
  listasService: ListasService;
  cargando: { set(value: boolean): void };
  construirReporteListaPrincipalPdf(): ReporteListaPrincipal | null;
  obtenerResumenFiltrosPrincipales(): string;
  datosFiltrados(): RegistroMonitoreo[];
  esCerradoPasivo(item: RegistroMonitoreo): boolean;
  obtenerRangoSeguimientoReporte(noDocumento?: string): RangoSeguimientoReporte;
  obtenerEstadoMonitoreoReporte(item: RegistroMonitoreo): string;
  obtenerEtiquetaOrigenRegistro(origen?: string | null): string;
  obtenerFechaRegistroInterno(
    rowFecha?: string | null,
    positivo?: { fechaRegistroInterno?: string | null } | null
  ): string | null;
  registrarAuditoriaFichaExcel(
    noDocumento: string,
    tipo: string,
    nombre: string,
    fileName: string,
    cantidadSeguimientos: number,
    rangoSeguimiento: RangoSeguimientoReporte
  ): ReturnType<ListasService['registrarAuditoriaExportacion']>;
  manejarErrorAuditoriaObligatoria(error: unknown, operacion: string): void;
};

/**
 * Adaptador del componente de Monitoreo de Listas.
 *
 * El PDF aprobado permanece sin cambios. Todas las exportaciones Excel de
 * Coincidencias reutilizan un contrato institucional único que replica el
 * título, secciones, orden, campos, tablas, mensajes vacíos y resúmenes del PDF.
 */
@Component({
  selector: 'app-monitoreo-listas',
  standalone: true,
  imports: [CommonModule, FormsModule],
  changeDetection: ChangeDetectionStrategy.Eager,
  templateUrl: './monitoreo-listas.component.html'
})
export class MonitoreoListasComponent extends MonitoreoListasComponentBase {
  constructor(listasService: ListasService) {
    super(listasService);
  }

  private interna(): InstanciaInternaMonitoreo {
    return this as unknown as InstanciaInternaMonitoreo;
  }

  private datosInstitucionales(): Pick<
    InstitutionalReportDefinition,
    'institution' | 'systemName' | 'generatedAt'
  > {
    const config = this.interna().configService.configSistema();
    return {
      institution: config?.nombreInstitucion || 'Instituto Hondureño de Seguridad Social',
      systemName: config?.nombreSistema || 'SGRLA-IHSS',
      generatedAt: new Date()
    };
  }

  private filasSeguimientos(seguimientos: Seguimiento[]): string[][] {
    return (seguimientos || []).map(seg => [
      seg.fechaCreacion ? this.formatDate(seg.fechaCreacion) : '',
      seg.usrEmail || 'Sistema',
      seg.motivoIngreso || '',
      seg.evidencias && seg.evidencias.length > 0
        ? seg.evidencias.map(evidencia => evidencia.nombreArchivo).join(', ')
        : 'Sin evidencias'
    ]);
  }

  private construirReporteIntegralPatrono(
    row: CoincidenciaJuridica,
    positivo: PositivoReporte,
    seguimientos: Seguimiento[],
    rangoSeguimiento: RangoSeguimientoReporte
  ): InstitutionalReportDefinition {
    const interna = this.interna();
    const fechaRegistro = interna.obtenerFechaRegistroInterno(row.fechaRegistroInterno, positivo);
    return {
      ...this.datosInstitucionales(),
      title: 'REPORTE INTEGRAL DE PATRONO',
      sections: [
        {
          kind: 'keyValue',
          title: '1. INFORMACIÓN GENERAL DEL PATRONO',
          rows: [
            ['Número Patronal:', row.numeroPatrono || 'N/D', 'RTN:', row.rtn || 'N/D'],
            ['Nombre / Razón Social:', row.nombre || 'N/D', 'Proveedor IHSS:', row.esProveedorIhss || 'No'],
            ['Lista de Coincidencia:', row.listaCoincidencia || 'N/D', 'Estado Monitoreo:', interna.obtenerEstadoMonitoreoReporte(row)],
            [
              'Fecha Coincidencia:',
              row.fechaEncontro ? this.formatDate(row.fechaEncontro) : 'N/D',
              'Fecha Calificación:',
              row.fechaCalifico ? this.formatDate(row.fechaCalifico) : 'N/D'
            ],
            [
              'Registro Interno:',
              fechaRegistro ? this.formatDate(fechaRegistro) : 'N/D',
              'Origen del Registro:',
              interna.obtenerEtiquetaOrigenRegistro(positivo?.origenRegistro)
            ]
          ]
        },
        {
          kind: 'paragraph',
          title: '2. MOTIVO DE INGRESO A LISTA DE MONITOREO',
          text: positivo?.motivoIngreso ||
            'No se ha registrado un motivo de ingreso inicial en el sistema para este patrono.'
        },
        {
          kind: 'history',
          title: '3. HISTORIAL DE SEGUIMIENTOS Y EVIDENCIAS',
          rangeText: rangoSeguimiento.texto,
          headers: ['Fecha', 'Usuario', 'Comentario / Acción', 'Evidencias'],
          rows: this.filasSeguimientos(seguimientos),
          emptyText: 'No se registran acciones de seguimiento ni evidencias adicionales para este patrono.'
        }
      ]
    };
  }

  private construirReporteIntegralNatural(
    row: CoincidenciaNatural,
    detalles: DetalleCoincidenciaNatural[],
    positivo: PositivoReporte,
    seguimientos: Seguimiento[],
    rangoSeguimiento: RangoSeguimientoReporte
  ): InstitutionalReportDefinition {
    const interna = this.interna();
    return {
      ...this.datosInstitucionales(),
      title: 'REPORTE INTEGRAL DE PERSONA NATURAL',
      sections: [
        {
          kind: 'keyValue',
          title: '1. INFORMACIÓN GENERAL DE LA PERSONA',
          rows: [
            ['DNI / Identificación:', row.numeroIdentificacion || 'N/D', 'Nombre Completo:', row.nombre || 'N/D'],
            ['Lista de Coincidencia:', row.listaCoincidencia || 'N/D', 'Total de Coincidencias:', String(row.totalRepetidos || 0)],
            [
              'Estado Monitoreo:',
              interna.obtenerEstadoMonitoreoReporte(row),
              'Registro Interno:',
              positivo?.fechaRegistroInterno ? this.formatDate(positivo.fechaRegistroInterno) : 'N/D'
            ],
            ['Origen del Registro:', interna.obtenerEtiquetaOrigenRegistro(positivo?.origenRegistro), '', '']
          ]
        },
        {
          kind: 'paragraph',
          title: '2. MOTIVO DE INGRESO A LISTA DE MONITOREO',
          text: positivo?.motivoIngreso ||
            'No se ha registrado un motivo de ingreso inicial en el sistema para esta persona.'
        },
        {
          kind: 'table',
          title: '3. DETALLE DE COINCIDENCIAS ENCONTRADAS',
          headers: ['Condición Actúa', 'Nro Patronal', 'Empresa', 'Es PEP', 'Lista', 'Fecha Coincidencia', 'Fecha Calificación'],
          rows: (detalles || []).map(detalle => [
            detalle.tipoCondicionActuaDesc,
            detalle.numeroPatronal,
            detalle.nombreEmpresa,
            detalle.esPep === 'SI' || detalle.esPep === 'S' ? 'SÍ' : 'NO',
            detalle.listaCoincidencia,
            detalle.fechaCoincidencia ? this.formatDate(detalle.fechaCoincidencia) : '',
            detalle.fechaCalifico ? this.formatDate(detalle.fechaCalifico) : ''
          ]),
          emptyText: 'No se encontraron coincidencias detalladas para esta persona.'
        },
        {
          kind: 'history',
          title: '4. HISTORIAL DE SEGUIMIENTOS Y EVIDENCIAS',
          rangeText: rangoSeguimiento.texto,
          headers: ['Fecha', 'Usuario', 'Comentario / Acción', 'Evidencias'],
          rows: this.filasSeguimientos(seguimientos),
          emptyText: 'No se registran acciones de seguimiento ni evidencias adicionales para esta persona.'
        }
      ]
    };
  }

  private construirReporteIntegralEmpleado(
    row: CoincidenciaEmpleado,
    detalles: DetalleCoincidenciaEmpleado[],
    positivo: PositivoReporte,
    seguimientos: Seguimiento[],
    rangoSeguimiento: RangoSeguimientoReporte
  ): InstitutionalReportDefinition {
    const interna = this.interna();
    return {
      ...this.datosInstitucionales(),
      title: 'REPORTE INTEGRAL DE EMPLEADO IHSS',
      sections: [
        {
          kind: 'keyValue',
          title: '1. INFORMACIÓN GENERAL DEL EMPLEADO',
          rows: [
            ['DNI / Identidad:', row.identidad || 'N/D', 'Nombre Completo:', row.nombre || 'N/D'],
            ['Lista de Coincidencia:', row.listaCoincidencia || 'N/D', 'Total de Coincidencias:', String(row.totalRepetidos || 0)],
            [
              'Estado Monitoreo:',
              interna.obtenerEstadoMonitoreoReporte(row),
              'Registro Interno:',
              positivo?.fechaRegistroInterno ? this.formatDate(positivo.fechaRegistroInterno) : 'N/D'
            ],
            ['Origen del Registro:', interna.obtenerEtiquetaOrigenRegistro(positivo?.origenRegistro), '', '']
          ]
        },
        {
          kind: 'paragraph',
          title: '2. MOTIVO DE INGRESO A LISTA DE MONITOREO',
          text: positivo?.motivoIngreso ||
            'No se ha registrado un motivo de ingreso inicial en el sistema para este empleado.'
        },
        {
          kind: 'table',
          title: '3. DETALLE DE COINCIDENCIAS ENCONTRADAS',
          headers: ['Condición Actúa', 'Nro Patronal', 'Empresa', 'Razón Social', 'Lista', 'Fecha Coincidencia', 'Fecha Calificación'],
          rows: (detalles || []).map(detalle => [
            detalle.tipoCondicionActuaDesc,
            detalle.numeroPatrono,
            detalle.nombreEmpresa,
            detalle.razoSoci,
            detalle.listaCoincidencia,
            detalle.fechaCoincidencia ? this.formatDate(detalle.fechaCoincidencia) : '',
            detalle.fechaCalifico ? this.formatDate(detalle.fechaCalifico) : ''
          ]),
          emptyText: 'No se encontraron coincidencias detalladas para este empleado.'
        },
        {
          kind: 'history',
          title: '4. HISTORIAL DE SEGUIMIENTOS Y EVIDENCIAS',
          rangeText: rangoSeguimiento.texto,
          headers: ['Fecha', 'Usuario', 'Comentario / Acción', 'Evidencias'],
          rows: this.filasSeguimientos(seguimientos),
          emptyText: 'No se registran acciones de seguimiento ni evidencias adicionales para este empleado.'
        }
      ]
    };
  }

  private construirReporteDetallado(): InstitutionalReportDefinition | null {
    const esEmpleado = this.tipoActivo() === 'empleado';
    const personaNatural = this.personaSeleccionada();
    const personaEmpleado = this.personaSeleccionadaEmpleado();

    if (esEmpleado && !personaEmpleado) return null;
    if (!esEmpleado && !personaNatural) return null;

    const filasDetalle = esEmpleado
      ? this.detallesEmpleado().map(detalle => [
          detalle.tipoCondicionActuaDesc,
          detalle.numeroPatrono,
          detalle.nombreEmpresa,
          detalle.razoSoci,
          detalle.listaCoincidencia,
          detalle.fechaCoincidencia ? this.formatDate(detalle.fechaCoincidencia) : '',
          detalle.fechaCalifico ? this.formatDate(detalle.fechaCalifico) : ''
        ])
      : this.detallesNatural().map(detalle => [
          detalle.tipoCondicionActuaDesc,
          detalle.numeroPatronal,
          detalle.nombreEmpresa,
          detalle.esPep === 'SI' || detalle.esPep === 'S' ? 'SÍ' : 'NO',
          detalle.listaCoincidencia,
          detalle.fechaCoincidencia ? this.formatDate(detalle.fechaCoincidencia) : '',
          detalle.fechaCalifico ? this.formatDate(detalle.fechaCalifico) : ''
        ]);

    return {
      ...this.datosInstitucionales(),
      title: esEmpleado
        ? 'REPORTE DETALLADO DE COINCIDENCIAS - EMPLEADO IHSS'
        : 'REPORTE DETALLADO DE COINCIDENCIAS - PERSONA NATURAL',
      sections: [
        {
          kind: 'keyValue',
          rows: [[
            'Nombre:',
            esEmpleado ? personaEmpleado!.nombre : personaNatural!.nombre,
            esEmpleado ? 'Identidad:' : 'DNI:',
            esEmpleado ? personaEmpleado!.identidad : personaNatural!.numeroIdentificacion
          ]]
        },
        {
          kind: 'table',
          headers: esEmpleado
            ? ['Condición Actúa', 'Nro Patronal', 'Empresa', 'Razón Social', 'Lista', 'Fecha Coincidencia', 'Fecha Calificación']
            : ['Condición Actúa', 'Nro Patronal', 'Empresa', 'Es PEP', 'Lista', 'Fecha Coincidencia', 'Fecha Calificación'],
          rows: filasDetalle,
          emptyText: 'No existen coincidencias detalladas para mostrar.'
        },
        {
          kind: 'keyValue',
          title: 'RESUMEN DEL REPORTE',
          rows: esEmpleado
            ? [['Total de Coincidencias:', String(this.totalCoincidencias()), 'Empresas Relacionadas:', String(this.empresasUnicas())]]
            : [
                ['Total de Coincidencias:', String(this.totalCoincidencias()), 'Coincidencias PEP:', String(this.coincidenciasPep())],
                ['Empresas Relacionadas:', String(this.empresasUnicas()), '', '']
              ]
        }
      ]
    };
  }

  private escribirReporteExcel(
    report: InstitutionalReportDefinition,
    sheetName: string,
    fileName: string
  ): void {
    const workbook = crearLibroExcelInstitucionalDesdeReporte(report, sheetName);
    void XLSX.writeFile(workbook, fileName);
  }

  private construirDatosExcelListaPrincipal(reporte: ReporteListaPrincipal): string[][] {
    const instancia = this.interna();
    const datosFiltrados = instancia.datosFiltrados();
    const pendientes = datosFiltrados.filter(item =>
      !instancia.esCerradoPasivo(item) && (!item.tieneMotivo || !!item.esManual)
    ).length;
    const conMotivo = datosFiltrados.filter(item =>
      !instancia.esCerradoPasivo(item) && !!item.tieneMotivo && !item.esManual
    ).length;
    const cerradosPasivos = datosFiltrados.filter(item => instancia.esCerradoPasivo(item)).length;

    return [
      [reporte.title],
      [instancia.configService.configSistema()?.nombreInstitucion || 'Instituto Hondureño de Seguridad Social'],
      [`SGRLA-IHSS | Generado: ${new Date().toLocaleString()}`],
      [],
      [`Filtros aplicados: ${instancia.obtenerResumenFiltrosPrincipales()}`],
      [],
      [
        'Registros filtrados', String(reporte.rows.length), 'Coincidencias visibles en la vista actual',
        'Pendientes', String(pendientes), 'Requieren motivo o revisión'
      ],
      [
        'Con motivo', String(conMotivo), 'Con sustento registrado',
        'Cerrados / pasivos', String(cerradosPasivos), 'Registros no activos'
      ],
      [],
      ['Detalle de coincidencias filtradas'],
      reporte.headers,
      ...reporte.rows
    ];
  }

  override exportarListaPrincipal(): void {
    const instancia = this.interna();
    const tipo = this.tipoActivo();
    const reporte = instancia.construirReporteListaPrincipalPdf();
    if (!reporte) return;

    const dataExcel = this.construirDatosExcelListaPrincipal(reporte);
    const ws = XLSX.utils.aoa_to_sheet(dataExcel);
    ws['!headerRows'] = [11];
    ws['!sectionRows'] = [10];
    ws['!paragraphRows'] = [2, 3, 5];
    ws['!keyValueRows'] = [7, 8];
    ws['!autoFilterRow'] = 11;

    const maxLens = dataExcel.reduce((acc, row) => {
      row.forEach((val, colIdx) => {
        const len = val ? val.toString().length : 0;
        if (!acc[colIdx] || len > acc[colIdx]) acc[colIdx] = len;
      });
      return acc;
    }, [] as number[]);
    ws['!cols'] = maxLens.map(len => ({ wch: Math.min(Math.max(len + 2, 10), 40) }));

    const wb = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(wb, ws, 'Coincidencias');

    const fileName = `Reporte_${tipo.charAt(0).toUpperCase() + tipo.slice(1)}s_${new Date().toISOString().split('T')[0]}.xlsx`;
    instancia.listasService.registrarAuditoriaExportacion(
      'RL_LISTA_POSITIVOS',
      tipo,
      'ExportacionMonitoreoListas',
      {
        accion: 'EXPORTACION_EXCEL',
        tipo,
        titulo: reporte.title,
        filtros: instancia.obtenerResumenFiltrosPrincipales(),
        cantidadRegistros: reporte.rows.length,
        archivo: fileName
      }
    ).subscribe({
      next: () => void XLSX.writeFile(wb, fileName),
      error: (err: unknown) => instancia.manejarErrorAuditoriaObligatoria(err, 'exportación Excel')
    });
  }

  override exportarFichaExcelPatrono(row: CoincidenciaJuridica): void {
    const instancia = this.interna();
    instancia.cargando.set(true);
    const rango = instancia.obtenerRangoSeguimientoReporte(row.numeroPatrono);
    const positivo$ = row.tieneMotivo
      ? instancia.listasService.getPositivoPorDocumento(row.numeroPatrono)
      : of(null);
    const seguimientos$ = row.tieneMotivo
      ? instancia.listasService.getSeguimientos(row.numeroPatrono, rango.desde, rango.hasta)
      : of([]);

    forkJoin([positivo$, seguimientos$]).subscribe({
      next: ([positivo, seguimientos]) => {
        const fileName = `Ficha_Patrono_${row.numeroPatrono}.xlsx`;
        const report = this.construirReporteIntegralPatrono(row, positivo, seguimientos, rango);
        instancia.registrarAuditoriaFichaExcel(
          row.numeroPatrono, 'juridica', row.nombre, fileName, seguimientos.length, rango
        ).subscribe({
          next: () => {
            this.escribirReporteExcel(report, 'Ficha Patrono', fileName);
            instancia.cargando.set(false);
          },
          error: err => instancia.manejarErrorAuditoriaObligatoria(err, 'exportación de ficha Excel')
        });
      },
      error: err => {
        console.error('Error al exportar ficha Excel de patrono:', err);
        instancia.cargando.set(false);
      }
    });
  }

  override exportarFichaExcelNatural(row: CoincidenciaNatural): void {
    const instancia = this.interna();
    instancia.cargando.set(true);
    const rango = instancia.obtenerRangoSeguimientoReporte(row.numeroIdentificacion);
    const detalles$ = instancia.listasService.getDetalleNatural(row.numeroIdentificacion);
    const positivo$ = row.tieneMotivo
      ? instancia.listasService.getPositivoPorDocumento(row.numeroIdentificacion)
      : of(null);
    const seguimientos$ = row.tieneMotivo
      ? instancia.listasService.getSeguimientos(row.numeroIdentificacion, rango.desde, rango.hasta)
      : of([]);

    forkJoin([detalles$, positivo$, seguimientos$]).subscribe({
      next: ([detalles, positivo, seguimientos]) => {
        const fileName = `Ficha_Natural_${row.numeroIdentificacion}.xlsx`;
        const report = this.construirReporteIntegralNatural(row, detalles, positivo, seguimientos, rango);
        instancia.registrarAuditoriaFichaExcel(
          row.numeroIdentificacion, 'natural', row.nombre, fileName, seguimientos.length, rango
        ).subscribe({
          next: () => {
            this.escribirReporteExcel(report, 'Ficha Natural', fileName);
            instancia.cargando.set(false);
          },
          error: err => instancia.manejarErrorAuditoriaObligatoria(err, 'exportación de ficha Excel')
        });
      },
      error: err => {
        console.error('Error al exportar ficha Excel de persona natural:', err);
        instancia.cargando.set(false);
      }
    });
  }

  override exportarFichaExcelEmpleado(row: CoincidenciaEmpleado): void {
    const instancia = this.interna();
    instancia.cargando.set(true);
    const rango = instancia.obtenerRangoSeguimientoReporte(row.identidad);
    const detalles$ = instancia.listasService.getDetalleEmpleado(row.identidad);
    const positivo$ = row.tieneMotivo
      ? instancia.listasService.getPositivoPorDocumento(row.identidad)
      : of(null);
    const seguimientos$ = row.tieneMotivo
      ? instancia.listasService.getSeguimientos(row.identidad, rango.desde, rango.hasta)
      : of([]);

    forkJoin([detalles$, positivo$, seguimientos$]).subscribe({
      next: ([detalles, positivo, seguimientos]) => {
        const fileName = `Ficha_Empleado_${row.identidad}.xlsx`;
        const report = this.construirReporteIntegralEmpleado(row, detalles, positivo, seguimientos, rango);
        instancia.registrarAuditoriaFichaExcel(
          row.identidad, 'empleado', row.nombre, fileName, seguimientos.length, rango
        ).subscribe({
          next: () => {
            this.escribirReporteExcel(report, 'Ficha Empleado', fileName);
            instancia.cargando.set(false);
          },
          error: err => instancia.manejarErrorAuditoriaObligatoria(err, 'exportación de ficha Excel')
        });
      },
      error: err => {
        console.error('Error al exportar ficha Excel de empleado:', err);
        instancia.cargando.set(false);
      }
    });
  }

  override exportarExcel(): void {
    const instancia = this.interna();
    const report = this.construirReporteDetallado();
    if (!report) return;

    const esEmpleado = this.tipoActivo() === 'empleado';
    const persona = esEmpleado ? this.personaSeleccionadaEmpleado() : this.personaSeleccionada();
    if (!persona) return;

    const documento = esEmpleado
      ? (persona as CoincidenciaEmpleado).identidad
      : (persona as CoincidenciaNatural).numeroIdentificacion;
    const nombre = persona.nombre;
    const cantidadRegistros = esEmpleado ? this.detallesEmpleado().length : this.detallesNatural().length;
    const fileName = esEmpleado
      ? `Reporte_Coincidencias_Empleado_${documento}.xlsx`
      : `Reporte_Coincidencias_${documento}.xlsx`;

    instancia.listasService.registrarAuditoriaExportacion(
      'RL_LISTA_POSITIVOS',
      documento,
      'ExportacionMonitoreoListas',
      {
        accion: 'EXPORTACION_EXCEL',
        tipo: esEmpleado ? 'empleado' : 'natural',
        nombre,
        cantidadRegistros,
        archivo: fileName
      }
    ).subscribe({
      next: () => this.escribirReporteExcel(report, 'Detalle Coincidencias', fileName),
      error: err => instancia.manejarErrorAuditoriaObligatoria(err, 'exportación Excel')
    });
  }
}
