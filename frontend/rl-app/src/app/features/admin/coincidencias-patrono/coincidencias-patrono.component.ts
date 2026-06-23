import { Component, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ListasService, CoincidenciaPatronoResumen, CoincidenciaPatronoDetalle } from '../../../core/services/listas.service';
import { AuthService } from '../../../core/services/auth.service';
import * as XLSX from 'xlsx';

@Component({
  selector: 'app-coincidencias-patrono',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './coincidencias-patrono.component.html'
})
export class CoincidenciasPatronoComponent implements OnInit {
  resumen = signal<CoincidenciaPatronoResumen[]>([]);
  cargando = signal(true);
  
  // Búsqueda y paginación
  buscarTerm = signal('');
  paginaActual = signal(1);
  registrosPorPagina = signal(25);
  
  // Detalle Modal
  mostrarModal = signal(false);
  cargandoDetalle = signal(false);
  fechaSeleccionada = signal('');
  detalleRegistros = signal<CoincidenciaPatronoDetalle[]>([]);
  
  // Búsqueda en el modal
  buscarTermDetalle = signal('');
  filtroCalificacion = signal<string>('Todas');

  protected readonly Math = Math;

  constructor(
    private listasService: ListasService,
    private authService: AuthService
  ) {}

  ngOnInit() {
    this.cargarResumen();
  }

  cargarResumen() {
    this.cargando.set(true);
    this.listasService.getResumenCoincidenciasPatrono().subscribe({
      next: (datos) => {
        this.resumen.set(datos);
        this.cargando.set(false);
      },
      error: (err) => {
        console.error('Error al cargar resumen:', err);
        this.cargando.set(false);
      }
    });
  }

  // Filtrado y paginación del resumen
  resumenFiltrado = computed(() => {
    const term = this.buscarTerm().toLowerCase().trim();
    const datos = this.resumen();
    if (!term) return datos;
    return datos.filter(item => {
      const fechaStr = this.formatearFechaSimple(item.fechaEncontro).toLowerCase();
      return fechaStr.includes(term) || item.cantidadRegistros.toString().includes(term);
    });
  });

  totalPaginas = computed(() => {
    const count = this.resumenFiltrado().length;
    const size = this.registrosPorPagina();
    return Math.ceil(count / size) || 1;
  });

  resumenPaginado = computed(() => {
    const datos = this.resumenFiltrado();
    const idx = (this.paginaActual() - 1) * this.registrosPorPagina();
    return datos.slice(idx, idx + this.registrosPorPagina());
  });

  cambiarPagina(p: number) {
    if (p >= 1 && p <= this.totalPaginas()) {
      this.paginaActual.set(p);
    }
  }

  // Formateador de fechas
  formatearFechaSimple(fechaStr: string): string {
    if (!fechaStr) return '';
    try {
      const d = new Date(fechaStr);
      if (isNaN(d.getTime())) return fechaStr;
      const dia = String(d.getDate()).padStart(2, '0');
      const mes = String(d.getMonth() + 1).padStart(2, '0');
      const anio = d.getFullYear();
      return `${dia}/${mes}/${anio}`;
    } catch {
      return fechaStr;
    }
  }

  formatearFechaCompleta(fechaStr: string): string {
    if (!fechaStr) return '';
    try {
      const d = new Date(fechaStr);
      if (isNaN(d.getTime())) return fechaStr;
      return d.toLocaleString('es-HN', { hour12: false });
    } catch {
      return fechaStr;
    }
  }

  verDetalles(item: CoincidenciaPatronoResumen) {
    const f = item.fechaEncontro.split('T')[0];
    this.fechaSeleccionada.set(f);
    this.mostrarModal.set(true);
    this.cargandoDetalle.set(true);
    this.detalleRegistros.set([]);
    this.buscarTermDetalle.set('');
    this.filtroCalificacion.set('Todas');
    
    this.listasService.getDetalleCoincidenciasPatrono(f).subscribe({
      next: (datos) => {
        this.detalleRegistros.set(datos);
        this.cargandoDetalle.set(false);
      },
      error: (err) => {
        console.error('Error al cargar detalle:', err);
        this.cargandoDetalle.set(false);
      }
    });
  }

  cerrarModal() {
    this.mostrarModal.set(false);
  }

  detalleFiltrado = computed(() => {
    const term = this.buscarTermDetalle().toLowerCase().trim();
    const calif = this.filtroCalificacion();
    const datos = this.detalleRegistros();
    
    return datos.filter(r => {
      const matchTerm = !term || 
        (r.nombre && r.nombre.toLowerCase().includes(term)) ||
        (r.dni && r.dni.toLowerCase().includes(term)) ||
        (r.numeroPatrono && r.numeroPatrono.toLowerCase().includes(term)) ||
        (r.listaCoincidencia && r.listaCoincidencia.toLowerCase().includes(term)) ||
        (r.tipoPersona && r.tipoPersona.toLowerCase().includes(term));
        
      let matchCalif = true;
      if (calif !== 'Todas') {
        const c = r.tipoCalificacion || 'Primera Vez';
        matchCalif = (c === calif);
      }

      return matchTerm && matchCalif;
    });
  });

  imprimir(item: CoincidenciaPatronoResumen) {
    const f = item.fechaEncontro.split('T')[0];
    import('sweetalert2').then((Swal) => {
      Swal.default.fire({
        title: 'Exportando',
        text: 'Obteniendo registros detallados para la exportación a Excel...',
        allowOutsideClick: false,
        didOpen: () => {
          Swal.default.showLoading();
        }
      });

      this.listasService.getDetalleCoincidenciasPatrono(f).subscribe({
        next: (registros) => {
          if (!registros || registros.length === 0) {
            Swal.default.fire({
              allowOutsideClick: false,
              title: 'Información',
              text: 'No hay registros de coincidencias para esta fecha para exportar.',
              icon: 'info',
              confirmButtonColor: '#1e3a8a'
            });
            return;
          }

          // Obtener información para el encabezado del reporte
          const institucion = 'Instituto Hondureño de Seguridad Social';
          const usuario = this.authService.usuario();
          const nombreUsuario = usuario ? `${usuario.nombre} ${usuario.apellido}` : 'Sistema';
          const fechaExport = new Date().toLocaleString('es-HN', { hour12: false });
          const fechaCoincidencia = this.formatearFechaSimple(item.fechaEncontro);

          // Columnas a exportar
          const headers = [
            'ID Reporte',
            'Identidad/DNI',
            'Nombre',
            'Número Patrono',
            'Tipo Persona',
            'Lista de Coincidencia',
            'Calificación',
            'Observación de Lista',
            'Nacionalidad',
            'Fecha Coincidencia'
          ];

          // Construcción de la matriz AOA (Array of Arrays)
          const excelData = [
            ['REPORTE DIARIO DE COINCIDENCIAS - COINCIDENCIAS PATRONO'],
            ['Institución:', institucion],
            ['Fecha de Coincidencias:', fechaCoincidencia],
            ['Fecha de Exportación:', fechaExport],
            ['Exportado por:', nombreUsuario],
            [], // Fila vacía de separación
            headers // Encabezados de la tabla
          ];

          // Agregar registros formateados
          registros.forEach(r => {
            excelData.push([
              r.reporteCoincidenciaId ? r.reporteCoincidenciaId.toString() : '',
              r.dni || '',
              r.nombre || '',
              r.numeroPatrono || '',
              r.tipoPersona || '',
              r.listaCoincidencia || '',
              r.tipoCalificacion || '',
              r.observacionLista || '',
              r.nacionalidad || '',
              this.formatearFechaCompleta(r.fechaEncontro)
            ]);
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
          XLSX.utils.book_append_sheet(wb, ws, 'Coincidencias');

          const fileName = `Coincidencias_Patrono_${fechaCoincidencia.replace(/\//g, '-')}_Export.xlsx`;
          XLSX.writeFile(wb, fileName);
          this.listasService.registrarAuditoriaExportacion(
            'DNP_IHSS.REPORTE_COINCIDENCIAS',
            f,
            'ExportacionCoincidenciasPatrono',
            {
              accion: 'EXPORTACION_EXCEL',
              fechaCoincidencia,
              cantidadRegistros: registros.length,
              archivo: fileName
            }
          ).subscribe({ error: err => console.warn('No se pudo registrar auditoria de exportacion', err) });

          Swal.default.fire({
            allowOutsideClick: false,
            title: 'Éxito',
            text: `Se exportaron ${registros.length} registros exitosamente.`,
            icon: 'success',
            confirmButtonColor: '#1e3a8a'
          });
        },
        error: (err) => {
          console.error('Error al exportar:', err);
          Swal.default.fire({
            allowOutsideClick: false,
            title: 'Error',
            text: 'Ocurrió un error al intentar exportar las coincidencias.',
            icon: 'error',
            confirmButtonColor: '#1e3a8a'
          });
        }
      });
    });
  }

  calificar(r: CoincidenciaPatronoDetalle, tipoCalificacionId: number) {
    const desc = tipoCalificacionId === 1 ? 'Positivo' : 'Falso Positivo';
    import('sweetalert2').then((Swal) => {
      Swal.default.fire({
        title: '¿Está seguro?',
        text: `Desea calificar este registro con ID ${r.reporteCoincidenciaId} como "${desc}"?`,
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#1e3a8a',
        cancelButtonColor: '#d33',
        confirmButtonText: 'Sí, calificar',
        cancelButtonText: 'Cancelar'
      }).then((result) => {
        if (result.isConfirmed) {
          Swal.default.fire({
            title: 'Procesando',
            text: 'Guardando calificación...',
            allowOutsideClick: false,
            didOpen: () => {
              Swal.default.showLoading();
            }
          });

          this.listasService.calificarCoincidencia(r.reporteCoincidenciaId, tipoCalificacionId).subscribe({
            next: () => {
              // Actualizar localmente el tipoCalificacion
              r.tipoCalificacion = desc;
              // También actualizar la lista de resumen general en background
              this.cargarResumen();
              
              Swal.default.fire({
                allowOutsideClick: false,
                title: 'Calificado',
                text: `El registro ha sido calificado como ${desc}.`,
                icon: 'success',
                confirmButtonColor: '#1e3a8a'
              });
            },
            error: (err) => {
              console.error('Error al calificar:', err);
              Swal.default.fire({
                allowOutsideClick: false,
                title: 'Error',
                text: 'No se pudo guardar la calificación del registro.',
                icon: 'error',
                confirmButtonColor: '#1e3a8a'
              });
            }
          });
        }
      });
    });
  }

  /**
   * Extrae los tokens significativos del nombre (palabras de más de 2 chars),
   * replicando la misma lógica que RebuildWhereClause en el backend C#.
   */
  private extraerTokens(nombre: string): string[] {
    return nombre
      .trim()
      .split(/\s+/)
      .filter(t => t.length > 2)
      .map(t => t.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')); // escapar regex
  }

  /**
   * Resalta en el HTML del detalle los tokens que coincidieron con el nombre buscado.
   * Solo resalta dentro de nodos de texto (evita romper etiquetas HTML como <br>, <b>).
   */
  private resaltarCoincidencias(html: string, tokens: string[]): string {
    if (!tokens.length || !html) return html;

    // Dividimos por etiquetas HTML para no alterar la estructura
    const partes = html.split(/(<[^>]+>)/g);
    return partes.map(parte => {
      if (parte.startsWith('<')) return parte; // es una etiqueta HTML, no tocar
      // Aplicar resaltado para cada token (case-insensitive)
      let resultado = parte;
      tokens.forEach(token => {
        const regex = new RegExp(`(${token})`, 'gi');
        resultado = resultado.replace(
          regex,
          '<mark style="background:linear-gradient(120deg,#fde68a,#fbbf24);color:#1e1e1e;border-radius:3px;padding:0 3px;font-weight:700;box-shadow:0 1px 3px rgba(251,191,36,.4)">$1</mark>'
        );
      });
      return resultado;
    }).join('');
  }

  mostrarDetalleRegistro(r: CoincidenciaPatronoDetalle) {
    import('sweetalert2').then((Swal) => {
      Swal.default.fire({
        title: 'Obteniendo detalles...',
        text: 'Consultando la base de datos de listas...',
        allowOutsideClick: false,
        didOpen: () => {
          Swal.default.showLoading();
        }
      });

      this.listasService.getResumenMatchLista(r.dataId, r.nombre).subscribe({
        next: (detalleHtml) => {
          // Extraer tokens del nombre del patrono (misma lógica que el backend)
          const tokens = this.extraerTokens(r.nombre || '');

          // Resaltar las coincidencias en el detalle
          const detalleResaltado = this.resaltarCoincidencias(
            detalleHtml || 'No se encontró detalle adicional en la lista de origen.',
            tokens
          );

          // Construir badge con tokens encontrados
          const tokensBadges = tokens.map(t =>
            `<span style="display:inline-block;background:#dbeafe;color:#1e40af;border:1px solid #bfdbfe;border-radius:12px;padding:1px 8px;font-size:10px;font-weight:600;margin:1px">${t}</span>`
          ).join(' ');

          const htmlContent = `
            <style>
              .det-grid { display:grid; grid-template-columns:1fr 1fr; gap:6px; margin-bottom:10px; }
              .det-field { text-align:left; font-size:11px; }
              .det-field strong { color:#374151; }
              .det-tokens { display:flex; flex-wrap:wrap; gap:3px; margin:6px 0 10px; align-items:center; }
              .det-tokens-label { font-size:10px; color:#6b7280; margin-right:4px; }
              .det-resumen { max-height:220px; overflow-y:auto; text-align:left; font-size:11px;
                             line-height:1.7; background:#f9fafb; border:1px solid #e5e7eb;
                             border-radius:10px; padding:10px 12px; }
              .det-footer { font-size:10px; color:#9ca3af; text-align:right; margin-top:8px; }
              .det-nombre { font-size:13px; font-weight:700; color:#111827; text-align:left; margin:6px 0; }
            </style>
            <div>
              <div class="det-grid">
                <div class="det-field"><strong>ID Reporte:</strong> ${r.reporteCoincidenciaId}</div>
                <div class="det-field"><strong>DNI / Identidad:</strong> ${r.dni || '—'}</div>
                <div class="det-field"><strong>N° Patrono:</strong> ${r.numeroPatrono || '—'}</div>
                <div class="det-field"><strong>Nacionalidad:</strong> ${r.nacionalidad || '—'}</div>
                <div class="det-field"><strong>Tipo Persona:</strong> ${r.tipoPersona || '—'}</div>
                <div class="det-field"><strong>Calificación:</strong> ${r.tipoCalificacion || 'Sin calificar'}</div>
              </div>
              <div class="det-nombre">📋 ${r.nombre || '—'}</div>
              <div class="det-tokens">
                <span class="det-tokens-label">🔍 Términos buscados:</span>
                ${tokensBadges || '<span style="font-size:10px;color:#9ca3af">—</span>'}
              </div>
              <p style="text-align:left;font-size:11px;font-weight:600;color:#374151;border-bottom:1px solid #e5e7eb;padding-bottom:4px;margin-bottom:6px">
                Detalle en lista de origen:
              </p>
              <div class="det-resumen">
                ${detalleResaltado}
              </div>
              <div class="det-footer">Fecha Coincidencia: ${this.formatearFechaCompleta(r.fechaEncontro)}</div>
            </div>
          `;

          Swal.default.fire({
            allowOutsideClick: false,
            title: '🔎 Detalle de Coincidencia',
            html: htmlContent,
            width: 600,
            confirmButtonColor: '#1e3a8a',
            confirmButtonText: 'Cerrar',
            customClass: {
              popup: 'rounded-2xl',
              title: 'text-base'
            }
          });
        },
        error: (err) => {
          console.error('Error al obtener detalle dinámico:', err);
          Swal.default.fire({
            allowOutsideClick: false,
            title: 'Error',
            text: 'Ocurrió un error al intentar consultar el detalle de coincidencias en la base de datos.',
            icon: 'error',
            confirmButtonColor: '#1e3a8a'
          });
        }
      });
    });
  }
}
