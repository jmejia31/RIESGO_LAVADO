import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ListasService, TipoListaCautela, ResumenLista } from '../../../core/services/listas.service';
import { AuthService } from '../../../core/services/auth.service';
import * as XLSX from 'xlsx';

@Component({
  selector: 'app-cargar-listas',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule],
  templateUrl: './cargar-listas.component.html'
})
export class CargarListasComponent implements OnInit {
  tiposListas = signal<TipoListaCautela[]>([]);
  resumenListas = signal<ResumenLista[]>([]);
  cargandoTipos = signal(true);
  cargandoResumen = signal(true);

  form!: FormGroup;

  constructor(
    private listasService: ListasService,
    private fb: FormBuilder,
    private authService: AuthService
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
    this.listasService.getTiposListasCautela().subscribe({
      next: (datos) => {
        this.tiposListas.set(datos);
        this.cargandoTipos.set(false);
      },
      error: (err) => {
        console.error('Error al cargar tipos de listas:', err);
        this.cargandoTipos.set(false);
      }
    });
  }

  cargarResumen() {
    this.cargandoResumen.set(true);
    this.listasService.getResumenListas().subscribe({
      next: (datos) => {
        this.resumenListas.set(datos);
        this.cargandoResumen.set(false);
      },
      error: (err) => {
        console.error('Error al cargar resumen de listas:', err);
        this.cargandoResumen.set(false);
      }
    });
  }

  archivoSeleccionado: File | null = null;

  onFileSelected(event: any) {
    const file = event.target.files[0];
    if (file) {
      this.archivoSeleccionado = file;
    } else {
      this.archivoSeleccionado = null;
    }
  }

  cargarArchivo() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    if (!this.archivoSeleccionado) {
      import('sweetalert2').then((Swal) => {
        Swal.default.fire({
          title: 'Archivo requerido',
          text: 'Por favor, seleccione un archivo para cargar.',
          icon: 'warning',
          confirmButtonColor: '#1e3a8a'
        });
      });
      return;
    }

    const tipoListaId = parseInt(this.form.get('tipoListaId')?.value, 10);

    import('sweetalert2').then((Swal) => {
      Swal.default.fire({
        title: 'Procesando archivo',
        text: 'Espere mientras se validan y cargan los registros...',
        allowOutsideClick: false,
        didOpen: () => {
          Swal.default.showLoading();
        }
      });

      this.listasService.uploadListaCautela(this.archivoSeleccionado!, tipoListaId).subscribe({
        next: (res) => {
          Swal.default.fire({
            title: 'Carga Exitosa',
            text: res.mensaje || 'Los registros fueron cargados exitosamente.',
            icon: 'success',
            confirmButtonColor: '#1e3a8a'
          });
          // Limpiar archivo seleccionado
          this.archivoSeleccionado = null;
          this.form.reset({ tipoListaId: '' });
          // Restablecer el input file visualmente si es necesario
          const fileInput = document.getElementById('fileUploadInput') as HTMLInputElement;
          if (fileInput) fileInput.value = '';

          // Actualizar la tabla de resumen
          this.cargarResumen();
        },
        error: (err) => {
          console.error('Error en validación:', err);
          const msg = err.error?.mensaje || 'Error al validar el archivo. Asegúrese de que tenga el formato y las columnas correctas.';
          Swal.default.fire({
            title: 'Error de Validación',
            text: msg,
            icon: 'error',
            confirmButtonColor: '#1e3a8a'
          });
        }
      });
    });
  }

  imprimir(item: ResumenLista) {
    if (!item.tipoListaCautelaId) {
      import('sweetalert2').then((Swal) => {
        Swal.default.fire({
          title: 'Error',
          text: 'No se puede exportar esta lista porque no tiene un ID de tipo válido.',
          icon: 'error',
          confirmButtonColor: '#1e3a8a'
        });
      });
      return;
    }

    import('sweetalert2').then((Swal) => {
      Swal.default.fire({
        title: 'Exportando',
        text: 'Obteniendo registros para la exportación a Excel...',
        allowOutsideClick: false,
        didOpen: () => {
          Swal.default.showLoading();
        }
      });

      this.listasService.exportarLista(item.tipoListaCautelaId!).subscribe({
        next: (registros) => {
          if (!registros || registros.length === 0) {
            Swal.default.fire({
              title: 'Información',
              text: 'No hay registros en esta lista para exportar.',
              icon: 'info',
              confirmButtonColor: '#1e3a8a'
            });
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

          const fileName = `${item.lista}_Export_${new Date().toISOString().split('T')[0]}.xlsx`;
          XLSX.writeFile(wb, fileName);

          Swal.default.fire({
            title: 'Éxito',
            text: `Se exportaron ${registros.length} registros exitosamente.`,
            icon: 'success',
            confirmButtonColor: '#1e3a8a'
          });
        },
        error: (err) => {
          console.error('Error al exportar lista:', err);
          Swal.default.fire({
            title: 'Error',
            text: 'Ocurrió un error al intentar exportar la lista de cautela.',
            icon: 'error',
            confirmButtonColor: '#1e3a8a'
          });
        }
      });
    });
  }

  esInvalido(name: string) {
    const c = this.form.get(name);
    return c ? c.invalid && c.touched : false;
  }
}
