import { ChangeDetectionStrategy, Component, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ListasService } from '../../data-access/listas.service';
import { TipoListaCautela } from '../../models/listas.models';
import { InstitutionalConfirmationService } from '../../../../../shared/confirmation/institutional-confirmation.service';

import { ActionIconComponent } from '../../../../../shared/components/action-icon/action-icon.component';
import { SortableTableHeaderComponent, SortDirection } from '../../../../../shared/components/sortable-table-header/sortable-table-header.component';

@Component({
  selector: 'app-tipo-listas',
  standalone: true,
  imports: [ActionIconComponent, SortableTableHeaderComponent, CommonModule, FormsModule, ReactiveFormsModule],
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
  orden = signal('id');
  direccion = signal<SortDirection>('asc');
  listasOrdenadas = computed(() => {
    const factor = this.direccion() === 'asc' ? 1 : -1;
    const campo = this.orden();
    return [...this.listas()].sort((a, b) => {
      const izquierda = campo === 'id' ? a.tipoListaCautelaId : campo === 'columnas' ? (a.cantidadColumnas ?? 0) : campo === 'archivo' ? (a.tipoArchivo ?? '') : a.descripcion;
      const derecha = campo === 'id' ? b.tipoListaCautelaId : campo === 'columnas' ? (b.cantidadColumnas ?? 0) : campo === 'archivo' ? (b.tipoArchivo ?? '') : b.descripcion;
      if (typeof izquierda === 'number' && typeof derecha === 'number') return (izquierda - derecha) * factor;
      return String(izquierda).localeCompare(String(derecha), 'es', { sensitivity: 'base' }) * factor;
    });
  });

  form!: FormGroup;

  constructor(
    private listasService: ListasService,
    private fb: FormBuilder,
    private confirmation: InstitutionalConfirmationService
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
    this.listasService.getTiposListasCautela('local').subscribe({
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

  ordenar(columna: string): void {
    if (this.orden() === columna) this.direccion.set(this.direccion() === 'asc' ? 'desc' : 'asc');
    else { this.orden.set(columna); this.direccion.set('asc'); }
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
    void this.confirmation.confirm({
      title: '¿Estás seguro?',
      message: `Se eliminará el tipo de lista "${item.descripcion}". Esta acción no se puede deshacer.`,
      icon: 'warning',
      confirmLabel: 'Sí, eliminar',
      cancelLabel: 'Cancelar',
      confirmIcon: 'delete',
      confirmVariant: 'danger'
    }).then(confirmed => {
      if (!confirmed) return;
      import('sweetalert2').then(Swal => {
        this.listasService.eliminarTipoListaCautela(item.tipoListaCautelaId).subscribe({
          next: (res) => {
            this.cargar();
            Swal.default.fire({
              allowOutsideClick: false,
              title: 'Eliminado',
              text: res.mensaje || 'El tipo de lista ha sido eliminado.',
              icon: 'success',
              confirmButtonColor: 'var(--ihss-primary-color)'
            });
          },
          error: (err) => {
            Swal.default.fire({
              allowOutsideClick: false,
              title: 'Error',
              text: err.error?.mensaje || 'No se pudo eliminar el tipo de lista.',
              icon: 'error',
              confirmButtonColor: 'var(--ihss-primary-color)'
            });
          }
        });
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
