import { ChangeDetectionStrategy, Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ListasService, TipoListaCautela } from '../../../core/services/listas.service';

@Component({
  selector: 'app-tipo-listas',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule],
  templateUrl: './tipo-listas.component.html',
  changeDetection: ChangeDetectionStrategy.Eager
})
export class TipoListasComponent implements OnInit {
  listas = signal<TipoListaCautela[]>([]);
  cargando = signal(true);
  mostrarForm = signal(false);
  guardando = signal(false);
  editando = signal(false);
  errorGuardar = signal<string | null>(null);
  errorCarga = signal<string | null>(null);
  idParaEditar = signal<number | null>(null);

  form!: FormGroup;

  constructor(
    private listasService: ListasService,
    private fb: FormBuilder
  ) {}

  ngOnInit() {
    this.construirForm();
    this.cargar();
  }

  construirForm() {
    this.form = this.fb.group({
      descripcion: ['', [Validators.required, Validators.maxLength(250)]],
      tipoArchivo: [{ value: 'xlsx', disabled: true }, [Validators.required, Validators.maxLength(50)]],
      cantidadColumnas: [null, [Validators.required, Validators.min(1)]]
    });
  }

  cargar() {
    this.cargando.set(true);
    this.errorCarga.set(null);
    this.listasService.getTiposListasCautela().subscribe({
      next: (datos) => {
        this.listas.set(datos);
        this.cargando.set(false);
      },
      error: (err) => {
        console.error('Error al cargar tipos de listas:', err);
        this.listas.set([]);
        this.errorCarga.set(err?.error?.mensaje || 'No se pudieron cargar los tipos de listas de cautela.');
        this.cargando.set(false);
      }
    });
  }

  prepararNuevo() {
    this.editando.set(false);
    this.idParaEditar.set(null);
    this.errorGuardar.set(null);
    this.form.reset({ tipoArchivo: 'xlsx' });
    this.mostrarForm.set(true);
  }

  prepararEdicion(item: TipoListaCautela) {
    this.editando.set(true);
    this.idParaEditar.set(item.tipoListaCautelaId);
    this.errorGuardar.set(null);
    this.form.patchValue({
      descripcion: item.descripcion,
      tipoArchivo: item.tipoArchivo || 'xlsx',
      cantidadColumnas: item.cantidadColumnas
    });
    this.mostrarForm.set(true);
  }

  cancelar() {
    this.mostrarForm.set(false);
    this.editando.set(false);
    this.idParaEditar.set(null);
    this.errorGuardar.set(null);
    this.form.reset();
  }

  guardar() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.errorGuardar.set(null);
    this.guardando.set(true);
    const rawVal = this.form.getRawValue();
    const descripcion = rawVal.descripcion;
    const tipoArchivo = rawVal.tipoArchivo;
    const cantidadColumnas = rawVal.cantidadColumnas;
 
    const obs = this.editando()
      ? this.listasService.actualizarTipoListaCautela(this.idParaEditar()!, descripcion, tipoArchivo, cantidadColumnas)
      : this.listasService.crearTipoListaCautela(descripcion, tipoArchivo, cantidadColumnas);

    obs.subscribe({
      next: (res) => {
        this.guardando.set(false);
        this.cancelar();
        this.cargar();
        
        import('sweetalert2').then((Swal) => {
          Swal.default.fire({
            allowOutsideClick: false,
            title: 'Éxito',
            text: res.mensaje || 'Registro guardado correctamente.',
            icon: 'success',
            toast: true,
            position: 'top-end',
            showConfirmButton: false,
            timer: 3000,
            timerProgressBar: true
          });
        });
      },
      error: (err) => {
        this.guardando.set(false);
        const msg = err.error?.mensaje || 'Ocurrió un error al guardar.';
        this.errorGuardar.set(msg);
      }
    });
  }

  eliminar(item: TipoListaCautela) {
    import('sweetalert2').then((Swal) => {
      Swal.default.fire({
        allowOutsideClick: false,
        title: '¿Estás seguro?',
        text: `Se eliminará el tipo de lista "${item.descripcion}". Esta acción no se puede deshacer.`,
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#ef4444',
        cancelButtonColor: '#6b7280',
        confirmButtonText: 'Sí, eliminar',
        cancelButtonText: 'Cancelar'
      }).then((result) => {
        if (result.isConfirmed) {
          this.listasService.eliminarTipoListaCautela(item.tipoListaCautelaId).subscribe({
            next: (res) => {
              this.cargar();
              Swal.default.fire({
                allowOutsideClick: false,
                title: 'Eliminado',
                text: res.mensaje || 'El tipo de lista ha sido eliminado.',
                icon: 'success',
                confirmButtonColor: '#1e3a8a'
              });
            },
            error: (err) => {
              Swal.default.fire({
                allowOutsideClick: false,
                title: 'Error',
                text: err.error?.mensaje || 'No se pudo eliminar el tipo de lista.',
                icon: 'error',
                confirmButtonColor: '#1e3a8a'
              });
            }
          });
        }
      });
    });
  }

  ctrl(name: string) {
    return this.form.get(name)!;
  }

  esInvalido(name: string) {
    const c = this.ctrl(name);
    return c.invalid && c.touched;
  }
}
