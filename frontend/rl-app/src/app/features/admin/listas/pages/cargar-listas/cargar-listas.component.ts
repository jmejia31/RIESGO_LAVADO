import { ChangeDetectionStrategy, Component, OnInit, signal, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ListasService } from '../../data-access/listas.service';
import { ResumenLista, TipoListaCautela } from '../../models/listas.models';
import { AuthService } from '../../../../../core/auth/auth.service';
import * as XLSX from '../../../../../core/utils/excel-export.util';

import { ActionIconComponent } from '../../../../../shared/components/action-icon/action-icon.component';
import { ReportPreviewService } from '../../../../../shared/report-preview/report-preview.service';
import { InstitutionalFileUploadComponent } from '../../../../../shared/components/institutional-file-upload/institutional-file-upload.component';
import { LIST_IMPORT_FILE_POLICY } from '../../../../../shared/components/institutional-file-upload/institutional-file-upload-policy';
import { InstitutionalTooltipDirective } from '../../../../../shared/components/institutional-tooltip/institutional-tooltip.directive';
import { SortableTableHeaderComponent, SortDirection } from '../../../../../shared/components/sortable-table-header/sortable-table-header.component';
import { InstitutionalNotificationService } from '../../../../../shared/notification/institutional-notification.service';

@Component({
  selector: 'app-cargar-listas',
  standalone: true,
  imports: [ActionIconComponent, InstitutionalFileUploadComponent, SortableTableHeaderComponent, InstitutionalTooltipDirective, CommonModule, FormsModule, ReactiveFormsModule],
  templateUrl: './cargar-listas.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class CargarListasComponent implements OnInit {
  private readonly reportPreview = inject(ReportPreviewService);
  tiposListas = signal<TipoListaCautela[]>([]);
  resumenListas = signal<ResumenLista[]>([]);
  cargandoTipos = signal(true);
  cargandoResumen = signal(true);
  procesandoCarga = signal(false);
  exportandoTipoId = signal<number | null>(null);
  errorTipos = signal<string | null>(null);
  errorResumen = signal<string | null>(null);
  orden = signal('id');
  direccion = signal<SortDirection>('asc');
  resumenOrdenado = computed(() => {
    const factor = this.direccion() === 'asc' ? 1 : -1;
    const campo = this.orden();
    return [...this.resumenListas()].sort((a, b) => {
      const izquierda = campo === 'id' ? (a.tipoListaCautelaId ?? 0) : campo === 'cantidad' ? a.cantidadRegistros : campo === 'fecha' ? (a.fechaCreacion ?? '') : campo === 'usuario' ? a.usuario : a.lista;
      const derecha = campo === 'id' ? (b.tipoListaCautelaId ?? 0) : campo === 'cantidad' ? b.cantidadRegistros : campo === 'fecha' ? (b.fechaCreacion ?? '') : campo === 'usuario' ? b.usuario : b.lista;
      if (typeof izquierda === 'number' && typeof derecha === 'number') return (izquierda - derecha) * factor;
      return String(izquierda).localeCompare(String(derecha), 'es', { sensitivity: 'base' }) * factor;
    });
  });

  readonly politicaCargaListas = LIST_IMPORT_FILE_POLICY;

  form!: FormGroup;

  constructor(
    private listasService: ListasService,
    private fb: FormBuilder,
    private authService: AuthService,
    private readonly notification: InstitutionalNotificationService
  ) {}

  ngOnInit() {
    this.construirForm();
    this.cargarTipos();
    this.cargarResumen();
  }

  construirForm() {
    this.form = this.fb.group({
      tipoListaId: ['', [Validators.required]]
    });
  }

  cargarTipos() {
    this.cargandoTipos.set(true);
    this.errorTipos.set(null);
    this.listasService.getTiposListasCautela('local').subscribe({
      next: (datos) => {
        this.tiposListas.set(datos);
        this.cargandoTipos.set(false);
      },
      error: (err) => {
        console.error('Error al cargar tipos de listas:', err);
        this.tiposListas.set([]);
        const mensaje = err?.error?.mensaje || 'No se pudieron cargar los tipos de listas disponibles.';
        this.errorTipos.set(mensaje);
        void this.notification.notify({ title: 'No se pudieron cargar los tipos de listas', message: mensaje, icon: 'error', confirmIcon: 'cancel', confirmVariant: 'danger' });
        this.cargandoTipos.set(false);
      }
    });
  }

  cargarResumen() {
    this.cargandoResumen.set(true);
    this.errorResumen.set(null);
    this.listasService.getResumenListas().subscribe({
      next: (datos) => {
        this.resumenListas.set(datos);
        this.cargandoResumen.set(false);
      },
      error: (err) => {
        console.error('Error al cargar resumen de listas:', err);
        this.resumenListas.set([]);
        const mensaje = err?.error?.mensaje || 'No se pudo cargar el resumen de listas.';
        this.errorResumen.set(mensaje);
        void this.notification.notify({ title: 'No se pudo cargar el resumen de listas', message: mensaje, icon: 'error', confirmIcon: 'cancel', confirmVariant: 'danger' });
        this.cargandoResumen.set(false);
      }
    });
  }

  ordenar(columna: string): void {
    if (this.orden() === columna) this.direccion.set(this.direccion() === 'asc' ? 'desc' : 'asc');
    else { this.orden.set(columna); this.direccion.set('asc'); }
  }

  readonly archivoSeleccionado = signal<File | null>(null);

  onArchivoSeleccionado(files: File[]) {
    this.archivoSeleccionado.set(files[0] ?? null);
  }

  cargarArchivo() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const archivoSeleccionado = this.archivoSeleccionado();
    if (!archivoSeleccionado) {
      void this.notification.notify({ title: 'Archivo requerido', message: 'Por favor, seleccione un archivo para cargar.', icon: 'warning', confirmIcon: 'view', confirmVariant: 'primary' });
      return;
    }

    const tipoListaId = parseInt(this.form.get('tipoListaId')?.value, 10);
    this.procesandoCarga.set(true);

    void this.notification.showLoading('Procesando archivo', 'Espere mientras se validan y cargan los registros...');
    this.listasService.uploadListaCautela(archivoSeleccionado, tipoListaId).subscribe({
        next: (res) => {
          this.procesandoCarga.set(false);
          this.notification.closeLoading();
          void this.notification.notify({ title: 'Carga exitosa', message: res.mensaje || 'Los registros fueron cargados exitosamente.', icon: 'success', confirmIcon: 'confirm', confirmVariant: 'success' });
          // Limpiar archivo seleccionado
          this.archivoSeleccionado.set(null);
          this.form.reset({ tipoListaId: '' });
          // Actualizar la tabla de resumen
          this.cargarResumen();
        },
        error: (err) => {
          this.procesandoCarga.set(false);
          console.error('Error en validación:', err);
          const msg = err.error?.mensaje || 'Error al validar el archivo. Asegúrese de que tenga el formato y las columnas correctas.';
          this.notification.closeLoading();
          void this.notification.notify({ title: 'Error de validación', message: msg, icon: 'error', confirmIcon: 'cancel', confirmVariant: 'danger' });
        }
      });
  }

  imprimir(item: ResumenLista) {
    if (!item.tipoListaCautelaId) {
      void this.notification.notify({ title: 'No se puede exportar la lista', message: 'La lista no tiene un ID de tipo válido.', icon: 'error', confirmIcon: 'cancel', confirmVariant: 'danger' });
      return;
    }

    if (this.exportandoTipoId()) return;
    this.exportandoTipoId.set(item.tipoListaCautelaId);

    void this.notification.showLoading('Exportando lista', 'Obteniendo registros para la exportación a Excel...');
    this.listasService.exportarLista(item.tipoListaCautelaId!).subscribe({
        next: (registros) => {
          this.exportandoTipoId.set(null);
          if (!registros || registros.length === 0) {
            this.notification.closeLoading();
            void this.notification.notify({ title: 'Sin registros para exportar', message: 'No hay registros en esta lista para exportar.', icon: 'info', confirmIcon: 'view', confirmVariant: 'primary' });
            return;
          }

          // Mapear claves a nombres legibles/amigables
          const registrosFormateados = registros.map(r => {
            const formatted: any = {};
            for (const key of Object.keys(r)) {
              let newKey = key;
              if (key === 'LISTA_CAUTELA_ID') newKey = 'ID Registro';
              else if (key === 'DATA_ID') newKey = 'ID Data';
              else if (key === 'OBSERVACIONES') newKey = 'Observaciones';
              else if (key === 'TIPO_LISTA') newKey = 'Tipo Lista';
              else if (key === 'USUARIO') newKey = 'Usuario';
              else if (key === 'FECHA_CREACION') newKey = 'Fecha Creación';
              else if (key.startsWith('TEXTO') || key.startsWith('TEXT')) {
                const num = key.replace(/\D/g, '');
                newKey = `Texto ${num}`;
              } else {
                newKey = key.toLowerCase().split('_').map(w => w.charAt(0).toUpperCase() + w.slice(1)).join(' ');
              }
              formatted[newKey] = r[key];
            }
            return formatted;
          });

          // Obtener información para el encabezado del reporte
          const institucion = 'Instituto Hondureño de Seguridad Social';
          const usuario = this.authService.usuario();
          const nombreUsuario = usuario ? `${usuario.nombre} ${usuario.apellido}` : 'Sistema';
          const fechaExport = new Date().toLocaleString('es-HN', { hour12: false });

          // Encabezados de tabla
          const headers = Object.keys(registrosFormateados[0]);

          // Construcción de la matriz AOA (Array of Arrays)
          const excelData = [
            ['REPORTE DETALLADO DE REGISTROS - LISTA DE CAUTELA'],
            ['Institución:', institucion],
            ['Lista de Cautela:', item.lista],
            ['Fecha de Exportación:', fechaExport],
            ['Exportado por:', nombreUsuario],
            [], // Fila vacía de separación
            headers // Encabezados reales de la tabla
          ];

          // Agregar filas de datos a la matriz
          registrosFormateados.forEach(row => {
            const rowData = headers.map(h => row[h]);
            excelData.push(rowData);
          });

          // Crear hoja de Excel desde AOA
          const ws = XLSX.utils.aoa_to_sheet(excelData);
          
          // Auto-ajustar ancho de columnas considerando solo la tabla (fila 7 en adelante, índice 6)
          const colWidths: any[] = [];
          headers.forEach((h, colIdx) => {
            let maxLen = h.length;
            for (let rowIdx = 6; rowIdx < excelData.length; rowIdx++) {
              const val = excelData[rowIdx][colIdx];
              const len = val ? val.toString().length : 0;
              if (len > maxLen) {
                maxLen = len;
              }
            }
            colWidths.push({ wch: Math.min(Math.max(maxLen + 2, 10), 55) });
          });
          ws['!cols'] = colWidths;

          const wb = XLSX.utils.book_new();
          XLSX.utils.book_append_sheet(wb, ws, item.lista);

          const fileName = `${this.nombreArchivoSeguro(item.lista)}_Export_${new Date().toISOString().split('T')[0]}.xlsx`;
          this.notification.closeLoading();
          void this.reportPreview.openExcel(
              wb,
              fileName,
              `Vista previa de la lista ${item.lista}`,
              'Revise los registros de la lista antes de descargar el archivo.'
            );

        },
        error: (err) => {
          this.exportandoTipoId.set(null);
          console.error('Error al exportar lista:', err);
          this.notification.closeLoading();
          void this.notification.notify({ title: 'No se pudo exportar la lista', message: err?.error?.mensaje || 'Ocurrió un error al intentar exportar la lista de cautela.', icon: 'error', confirmIcon: 'cancel', confirmVariant: 'danger' });
        }
      });
  }

  esInvalido(name: string) {
    const c = this.form.get(name);
    return c ? c.invalid && c.touched : false;
  }

  private nombreArchivoSeguro(nombre: string): string {
    return (nombre || 'Lista_Cautela')
      .normalize('NFD')
      .replace(/[\u0300-\u036f]/g, '')
      .replace(/[^a-zA-Z0-9_-]+/g, '_')
      .replace(/^_+|_+$/g, '')
      .slice(0, 80) || 'Lista_Cautela';
  }
}
