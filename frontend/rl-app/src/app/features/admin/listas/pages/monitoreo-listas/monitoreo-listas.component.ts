import { ChangeDetectionStrategy, Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ListasService } from '../../data-access/listas.service';
import * as XLSX from '../../../../../core/utils/excel-export.util';
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

type InstanciaInternaMonitoreo = {
  configService: { configSistema(): { nombreInstitucion?: string } | null };
  listasService: ListasService;
  construirReporteListaPrincipalPdf(): ReporteListaPrincipal | null;
  obtenerResumenFiltrosPrincipales(): string;
  datosFiltrados(): RegistroMonitoreo[];
  esCerradoPasivo(item: RegistroMonitoreo): boolean;
  manejarErrorAuditoriaObligatoria(error: unknown, operacion: string): void;
};

/**
 * Adaptador del componente de Monitoreo de Listas.
 *
 * Conserva la implementación funcional aprobada y garantiza que Excel y PDF
 * consuman exactamente el mismo modelo de reporte: filtros, resumen,
 * encabezados y filas de detalle.
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

  private construirDatosExcelListaPrincipal(reporte: ReporteListaPrincipal): string[][] {
    const instancia = this as unknown as InstanciaInternaMonitoreo;
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
    const instancia = this as unknown as InstanciaInternaMonitoreo;
    const tipo = this.tipoActivo();
    const reporte = instancia.construirReporteListaPrincipalPdf();
    if (!reporte) return;

    const dataExcel = this.construirDatosExcelListaPrincipal(reporte);
    const ws = XLSX.utils.aoa_to_sheet(dataExcel);
    const maxLens = dataExcel.reduce((acc, row) => {
      row.forEach((val, colIdx) => {
        const len = val ? val.toString().length : 0;
        if (!acc[colIdx] || len > acc[colIdx]) {
          acc[colIdx] = len;
        }
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
      next: () => XLSX.writeFile(wb, fileName),
      error: (err: unknown) => instancia.manejarErrorAuditoriaObligatoria(err, 'exportación Excel')
    });
  }
}
