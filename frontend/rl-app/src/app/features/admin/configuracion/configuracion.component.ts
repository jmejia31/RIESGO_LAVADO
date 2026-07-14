import { ChangeDetectionStrategy, Component, OnInit, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ConfiguracionService } from '../../../core/configuration/configuracion.service';
import { ConfigSistema, LoginSlide } from '../../../core/configuration/configuracion.models';

@Component({
  selector: 'app-configuracion',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule],
  changeDetection: ChangeDetectionStrategy.Eager,
  templateUrl: './configuracion.component.html',
})
export class ConfiguracionComponent implements OnInit {
  private fb = inject(FormBuilder);
  public configService = inject(ConfiguracionService);

  activeTab = signal<'sistema' | 'slides'>('sistema');
  readonly maxTextoTextarea = 1000;

  // Estado pestaña Configuración
  form!: FormGroup;
  guardando = signal(false);
  mensajeExito = signal<string | null>(null);
  mensajeError = signal<string | null>(null);

  // Estado pestaña Slides
  slides = signal<LoginSlide[]>([]);
  cargandoSlides = signal(false);
  modalSlideAbierto = signal(false);
  slideForm!: FormGroup;
  slideEditando = signal<LoginSlide | null>(null);
  guardandoSlide = signal(false);
  subiendoImagen = signal(false);

  ngOnInit() {
    this.construirForm();
    this.construirSlideForm();
    this.cargarConfiguracion();
    this.cargarSlides();
  }

  construirForm() {
    this.form = this.fb.group({
      nombreInstitucion: ['', Validators.required],
      nombreSistema: ['', Validators.required],
      logoUrl: [''],
      iconoUrl: [''],
      colorPrimario: ['#1e3a8a', [Validators.required, Validators.pattern(/^#[0-9a-fA-F]{6}$/)]],
      colorSecundario: ['#1d4ed8', [Validators.required, Validators.pattern(/^#[0-9a-fA-F]{6}$/)]],
      timeoutSesion: [30, [Validators.required, Validators.min(1), Validators.max(1440)]],
      acuerdoLegal: ['', [Validators.maxLength(this.maxTextoTextarea)]],
      maxIntentos: [5, [Validators.required, Validators.min(1), Validators.max(20)]]
    });
  }

  construirSlideForm() {
    this.slideForm = this.fb.group({
      imagenUrl: ['', Validators.required],
      titulo: [''],
      descripcion: ['', [Validators.maxLength(this.maxTextoTextarea)]],
      orden: [1, [Validators.required, Validators.min(1)]],
      activo: [true],
      imagenIcono: ['']
    });
  }

  longitudCampo(form: FormGroup, control: string): number {
    return String(form.get(control)?.value || '').length;
  }

  cargarConfiguracion() {
    const config = this.configService.configSistema();
    if (config) {
      this.form.patchValue(config);
    } else {
      this.configService.CargarConfiguracion().subscribe({
        next: (cfg) => this.form.patchValue(cfg),
        error: () => this.mostrarError('Error al cargar la configuración desde el servidor.')
      });
    }
  }

  cargarSlides() {
    this.cargandoSlides.set(true);
    this.configService.getTodosSlides().subscribe({
      next: (data) => {
        this.slides.set(data);
        this.cargandoSlides.set(false);
      },
      error: () => {
        this.slides.set([]);
        this.cargandoSlides.set(false);
      }
    });
  }

  guardar() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.mensajeExito.set(null);
    this.mensajeError.set(null);
    this.guardando.set(true);

    const data: ConfigSistema = this.form.value;

    this.configService.GuardarConfiguracion(data).subscribe({
      next: () => {
        this.guardando.set(false);
        this.mostrarExito('¡Configuración guardada y aplicada exitosamente!');
        this.configService.CargarConfiguracion().subscribe();
      },
      error: (err) => {
        this.guardando.set(false);
        const msg = err?.error?.mensaje || 'Error al guardar los cambios en la base de datos.';
        this.mostrarError(msg);
      }
    });
  }

  abrirModalSlide(slide?: LoginSlide) {
    this.slideEditando.set(slide || null);
    if (slide) {
      this.slideForm.patchValue({
        imagenUrl: slide.imagenUrl,
        titulo: slide.titulo,
        descripcion: slide.descripcion,
        orden: slide.orden,
        activo: slide.activo,
        imagenIcono: slide.imagenIcono
      });
    } else {
      this.slideForm.reset({
        imagenUrl: '',
        titulo: '',
        descripcion: '',
        orden: this.slides().length + 1,
        activo: true,
        imagenIcono: ''
      });
    }
    this.modalSlideAbierto.set(true);
  }

  cerrarModalSlide() {
    this.modalSlideAbierto.set(false);
    this.slideEditando.set(null);
    this.slideForm.reset();
  }

  guardarSlide() {
    if (this.slideForm.invalid) return;

    this.guardandoSlide.set(true);
    const body: LoginSlide = {
      ...this.slideForm.value,
      id: this.slideEditando()?.id || 0
    };

    const request = this.slideEditando() 
      ? this.configService.actualizarSlide(body.id, body)
      : this.configService.crearSlide(body);

    request.subscribe({
      next: () => {
        this.guardandoSlide.set(false);
        this.mostrarExito(this.slideEditando() ? 'Slide actualizado exitosamente' : 'Slide creado exitosamente');
        this.cerrarModalSlide();
        this.cargarSlides();
      },
      error: (err) => {
        this.guardandoSlide.set(false);
        const msg = err?.error?.mensaje || 'Error al procesar la solicitud.';
        this.mostrarError(msg);
      }
    });
  }

  eliminarSlide(id: number) {
    import('sweetalert2').then(Swal => {
      Swal.default.fire({
        allowOutsideClick: false,
        title: '¿Está seguro de eliminar el slide?',
        text: 'Esta acción no se puede deshacer y el slide se removerá del carrusel de login.',
        icon: 'warning',
        showCancelButton: true,
        confirmButtonText: 'Sí, eliminar',
        cancelButtonText: 'Cancelar',
        confirmButtonColor: '#dc2626',
        cancelButtonColor: '#6b7280'
      }).then(res => {
        if (res.isConfirmed) {
          this.configService.eliminarSlide(id).subscribe({
            next: () => {
              this.mostrarExito('Slide eliminado de la base de datos');
              this.cargarSlides();
            },
            error: (err) => {
              const msg = err?.error?.mensaje || 'No se pudo eliminar el slide.';
              this.mostrarError(msg);
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

  private mostrarExito(msg: string) {
    this.mensajeExito.set(msg);
    setTimeout(() => this.mensajeExito.set(null), 5000);
  }

  private mostrarError(msg: string) {
    this.mensajeError.set(msg);
    setTimeout(() => this.mensajeError.set(null), 7000);
  }

  onArchivoSeleccionado(event: any) {
    const file = event.target.files?.[0];
    if (!file) return;

    this.subiendoImagen.set(true);
    this.configService.subirImagen(file).subscribe({
      next: (res) => {
        this.subiendoImagen.set(false);
        this.slideForm.patchValue({ imagenUrl: res.url });
        this.slideForm.get('imagenUrl')?.markAsTouched();
        
        import('sweetalert2').then(Swal => {
          Swal.default.fire({
            allowOutsideClick: false,
            title: '¡Imagen Subida!',
            text: 'La imagen ha sido cargada y procesada exitosamente en el servidor.',
            icon: 'success',
            timer: 2000,
            showConfirmButton: false
          });
        });
      },
      error: (err) => {
        this.subiendoImagen.set(false);
        const msg = err?.error?.mensaje || 'Error al subir el archivo de imagen.';
        import('sweetalert2').then(Swal => {
          Swal.default.fire({
            allowOutsideClick: false,
            title: 'Error de carga',
            text: msg,
            icon: 'error',
            confirmButtonText: 'Entendido',
            confirmButtonColor: '#1e3a8a'
          });
        });
      }
    });
  }
}
