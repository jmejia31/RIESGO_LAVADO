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

/**
 * Adaptador del componente de Monitoreo de Listas.
 *
 * Conserva la implementación funcional aprobada y garantiza que Excel y PDF
 * consuman exactamente el mismo modelo de encabezados y filas del reporte
 * principal, evitando divergencias entre formatos.
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

  override exportarListaPrincipal(): void {
    const instancia = this as unknown as {
      configService: { configSistema(): { nombreInstitucion?: string } | null };
      listasService: ListasService;
      construirReporteListaPrincipalPdf(): ReporteListaPrincipal | null;
      manejarErrorAuditoriaObligatoria(error: unknown, operacion: string): void;
    };

    const tipo = this.tipoActivo();
    const reporte = instancia.construirReporteListaPrincipalPdf();
    if (!reporte) return;

    const dataExcel = [
      [reporte.title],
      [instancia.configService.configSistema()?.nombreInstitucion || 'Instituto Hondureño de Seguridad Social'],
      [`Fecha de Generación: ${this.formatDate(new Date().toISOString())}`],
      [],
      reporte.headers,
      ...reporte.rows
    ];

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
        cantidadRegistros: reporte.rows.length,
        archivo: fileName
      }
    ).subscribe({
      next: () => XLSX.writeFile(wb, fileName),
      error: (err: unknown) => instancia.manejarErrorAuditoriaObligatoria(err, 'exportación Excel')
    });
  }
}
