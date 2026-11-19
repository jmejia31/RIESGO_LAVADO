import { ChangeDetectionStrategy, Component, OnInit, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ConfiguracionService } from '../../../../../core/configuration/configuracion.service';
import { ConfigSistema, LoginSlide } from '../../../../../core/configuration/configuracion.models';
import { DEFAULT_SYSTEM_ACCENT, DEFAULT_SYSTEM_PRIMARY } from '../../../../../core/configuration/system-theme.util';

import { ActionIconComponent } from '../../../../../shared/components/action-icon/action-icon.component';
import { InstitutionalFileUploadComponent } from '../../../../../shared/components/institutional-file-upload/institutional-file-upload.component';
import { INSTITUTIONAL_FAVICON_FILE_POLICY, INSTITUTIONAL_LOGO_FILE_POLICY, LOGIN_SLIDE_IMAGE_FILE_POLICY } from '../../../../../shared/components/institutional-file-upload/institutional-file-upload-policy';
import { InstitutionalConfirmationService } from '../../../../../shared/confirmation/institutional-confirmation.service';
import { InstitutionalTooltipDirective } from '../../../../../shared/components/institutional-tooltip/institutional-tooltip.directive';

@Component({
  selector: 'app-configuracion',
  standalone: true,
  imports: [ActionIconComponent, InstitutionalFileUploadComponent, InstitutionalTooltipDirective, CommonModule, FormsModule, ReactiveFormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './configuracion.component.html',
})
export class ConfiguracionComponent implements OnInit {
  private fb = inject(FormBuilder);
  public configService = inject(ConfiguracionService);
  private readonly confirmation = inject(InstitutionalConfirmationService);

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
  readonly politicaImagenSlide = LOGIN_SLIDE_IMAGE_FILE_POLICY;
  readonly politicaLogoInstitucional = INSTITUTIONAL_LOGO_FILE_POLICY;
  archivosLogoPendientes = signal<File[]>([]);
  subiendoLogo = signal(false);
  readonly politicaFaviconInstitucional = INSTITUTIONAL_FAVICON_FILE_POLICY;
  archivosFaviconPendientes = signal<File[]>([]);
  subiendoFavicon = signal(false);
  readonly estadoFavicon = signal<'IDLE' | 'FAVICON_SELECTED' | 'FAVICON_VALIDATING' | 'FAVICON_CONVERTING' | 'FAVICON_UPLOADING' | 'FAVICON_READY_TO_SAVE' | 'FAVICON_SAVING' | 'FAVICON_PUBLISHED' | 'FAVICON_SAVE_FAILED'>('IDLE');
  faviconVistaPreviaUrl = signal<string | null>(null);
  readonly estadoLogo = signal<'IDLE' | 'LOGO_SELECTED' | 'LOGO_VALIDATING' | 'LOGO_UPLOADING' | 'LOGO_UPLOADED_PENDING_SAVE' | 'LOGO_SAVING' | 'LOGO_PUBLISHED' | 'LOGO_SAVE_FAILED'>('IDLE');
  archivosImagenPendientes = signal<File[]>([]);

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
      colorPrimario: [DEFAULT_SYSTEM_PRIMARY, [Validators.required, Validators.pattern(/^#[0-9a-fA-F]{6}$/)]],
      colorSecundario: [DEFAULT_SYSTEM_ACCENT, [Validators.required, Validators.pattern(/^#[0-9a-fA-F]{6}$/)]],
      colorEncabezadoReportes: ['#123B63', [Validators.required, Validators.pattern(/^#[0-9a-fA-F]{6}$/)]],
      timeoutSesion: [30, [Validators.required, Validators.min(1), Validators.max(1440)]],
      acuerdoLegal: ['', [Validators.maxLength(this.maxTextoTextarea)]],
      maxIntentos: [5, [Validators.required, Validators.min(1), Validators.max(20)]],
      validezClaveTemp: [15, [Validators.required, Validators.min(1), Validators.max(1440)]]
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
      this.configService.CargarConfiguracion('local').subscribe({
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
    if (this.subiendoLogo() || this.subiendoFavicon()) return;
    this.guardando.set(true);
    this.estadoLogo.set('LOGO_SAVING');
    this.estadoFavicon.set('FAVICON_SAVING');

    const data: ConfigSistema = this.form.value;

    this.configService.GuardarConfiguracion(data).subscribe({
      next: () => {
        this.configService.CargarConfiguracion('local').subscribe({
          next: persisted => {
            this.form.patchValue(persisted);
            // La vista previa debe resolver el favicon efectivamente publicado.
            this.faviconVistaPreviaUrl.set(null);
            this.estadoLogo.set('LOGO_PUBLISHED');
            this.estadoFavicon.set('FAVICON_PUBLISHED');
            this.guardando.set(false);
            this.mostrarExito('¡Configuración guardada y aplicada exitosamente!');
          },
          error: () => {
            this.guardando.set(false);
            this.estadoLogo.set('LOGO_SAVE_FAILED');
            this.estadoFavicon.set('FAVICON_SAVE_FAILED');
            this.mostrarError('No se pudo verificar la configuracion persistida en el servidor.');
          }
        });
      },
      error: (err) => {
        this.guardando.set(false);
        this.estadoLogo.set('LOGO_SAVE_FAILED');
        this.estadoFavicon.set('FAVICON_SAVE_FAILED');
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
    void this.confirmation.confirm({
      title: '¿Está seguro de eliminar el slide?',
      message: 'Esta acción no se puede deshacer y el slide se removerá del carrusel de login.',
      icon: 'warning',
      confirmLabel: 'Sí, eliminar',
      cancelLabel: 'Cancelar',
      confirmIcon: 'delete',
      confirmVariant: 'danger'
    }).then(confirmed => {
      if (confirmed) {
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

  onArchivoSeleccionado(files: File[]) {
    const file = files[0];
    if (!file) return;

    this.archivosImagenPendientes.set([file]);
    this.subiendoImagen.set(true);
    this.configService.subirImagen(file).subscribe({
      next: (res) => {
        this.subiendoImagen.set(false);
        this.archivosImagenPendientes.set([]);
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
            confirmButtonColor: 'var(--ihss-primary-color)'
          });
        });
      }
    });
  }

  onLogoSeleccionado(files: File[]) {
    const file = files[0];
    if (!file) return;
    this.archivosLogoPendientes.set([file]);
    this.estadoLogo.set('LOGO_SELECTED');
    this.subiendoLogo.set(true);
    this.estadoLogo.set('LOGO_VALIDATING');
    void this.normalizarLogoPng(file).then(normalized => {
      this.estadoLogo.set('LOGO_UPLOADING');
      this.configService.subirImagen(normalized).subscribe({
        next: response => {
          this.subiendoLogo.set(false);
          this.archivosLogoPendientes.set([]);
          // La URL queda en el formulario y no se vuelve activa hasta pulsar Guardar.
          this.form.patchValue({ logoUrl: response.url });
          this.estadoLogo.set('LOGO_UPLOADED_PENDING_SAVE');
          this.mostrarExito('Logo cargado. Guarde la configuración para publicarlo.');
        },
        error: error => {
          this.subiendoLogo.set(false);
          this.estadoLogo.set('LOGO_SAVE_FAILED');

          this.archivosLogoPendientes.set([]);
          this.mostrarError(error?.error?.mensaje || 'No se pudo cargar el logo institucional.');
        }
      });
    }).catch(() => {
      this.subiendoLogo.set(false);
      this.estadoLogo.set('LOGO_SAVE_FAILED');
      this.archivosLogoPendientes.set([]);
      this.mostrarError('No se pudo leer el logo. Use un PNG, JPG, JPEG o WEBP válido.');
    });
  }

  private async normalizarLogoPng(file: File): Promise<File> {
    const objectUrl = URL.createObjectURL(file);
    try {
      const image = await new Promise<HTMLImageElement>((resolve, reject) => {
        const element = new Image();
        element.onload = () => resolve(element);
        element.onerror = () => reject(new Error('invalid-image'));
        element.src = objectUrl;
      });
      const canvas = document.createElement('canvas');
      canvas.width = image.naturalWidth;
      canvas.height = image.naturalHeight;
      const context = canvas.getContext('2d');
      if (!context || canvas.width <= 0 || canvas.height <= 0) throw new Error('invalid-image');
      context.drawImage(image, 0, 0);
      const blob = await new Promise<Blob>((resolve, reject) => canvas.toBlob(value => value ? resolve(value) : reject(new Error('encode-failed')), 'image/png'));
      return new File([blob], `${file.name.replace(/\.[^.]+$/, '')}.png`, { type: 'image/png', lastModified: file.lastModified });
    } finally {
      URL.revokeObjectURL(objectUrl);
    }
  }

  urlFaviconVistaPrevia(): string {
    // Un path /uploads/... pertenece al backend, no al servidor Angular (:4200).
    // El favicon publicado siempre se recupera desde su endpoint validado.
    return this.faviconVistaPreviaUrl() ?? this.configService.obtenerUrlFaviconPublicado();
  }

  onFaviconSeleccionado(files: File[]) {
    const file = files[0];
    if (!file) return;

    this.archivosFaviconPendientes.set([file]);
    this.estadoFavicon.set('FAVICON_SELECTED');
    this.subiendoFavicon.set(true);
    this.estadoFavicon.set('FAVICON_VALIDATING');
    this.faviconVistaPreviaUrl.set(null);

    // El servidor solo decodifica píxeles PNG/ICO de forma real. JPEG y WebP
    // se decodifican con el navegador y se envían como PNG RGBA canónico.
    const normalized = file.name.toLowerCase().endsWith('.ico')
      ? Promise.resolve(file)
      : this.normalizarFaviconPng(file);

    void normalized.then(uploadFile => {
      this.estadoFavicon.set('FAVICON_UPLOADING');
      this.configService.subirFavicon(uploadFile).subscribe({
        next: response => {
          this.subiendoFavicon.set(false);
          this.archivosFaviconPendientes.set([]);
          if (!response.success || !/^\/uploads\/[A-Za-z0-9._-]+\.ico$/i.test(response.url)) {
            this.estadoFavicon.set('FAVICON_SAVE_FAILED');
            this.mostrarError('El servidor no devolvió una referencia de favicon válida.');
            return;
          }
          // URL absoluta del backend; /uploads/... relativo apunta erróneamente a Angular.
          this.faviconVistaPreviaUrl.set(this.configService.resolverUrlImagen(response.url));
          this.form.patchValue({ iconoUrl: response.url });
          this.estadoFavicon.set('FAVICON_READY_TO_SAVE');
          this.mostrarExito('Favicon convertido correctamente. Guarde la configuración para publicarlo.');
        },
        error: error => {
          this.subiendoFavicon.set(false);
          this.estadoFavicon.set('FAVICON_SAVE_FAILED');
          this.archivosFaviconPendientes.set([]);
          this.faviconVistaPreviaUrl.set(null);
          this.mostrarError(error?.error?.mensaje || 'No se pudo cargar el favicon institucional.');
        }
      });
    }).catch(() => {
      this.subiendoFavicon.set(false);
      this.estadoFavicon.set('FAVICON_SAVE_FAILED');
      this.archivosFaviconPendientes.set([]);
      this.faviconVistaPreviaUrl.set(null);
      this.mostrarError('No se pudo decodificar la imagen original. Use un PNG, JPG, JPEG o WebP válido.');
    });
  }

  private async normalizarFaviconPng(file: File): Promise<File> {
    const objectUrl = URL.createObjectURL(file);
    try {
      const image = await new Promise<HTMLImageElement>((resolve, reject) => {
        const element = new Image();
        element.onload = () => resolve(element);
        element.onerror = () => reject(new Error('invalid-favicon-image'));
        element.src = objectUrl;
      });
      const { naturalWidth: width, naturalHeight: height } = image;
      if (width < 16 || height < 16 || width > 2048 || height > 2048 || width * height > 4194304) {
        throw new Error('invalid-favicon-dimensions');
      }

      // PNG RGBA estandarizado; siempre cuadrado y transparente, sin deformación.
      // No modifica ni utiliza la normalización del logo documental.
      const size = 256;
      const canvas = document.createElement('canvas');
      canvas.width = size;
      canvas.height = size;
      const context = canvas.getContext('2d');
      if (!context) throw new Error('canvas-not-supported');
      context.clearRect(0, 0, size, size);
      context.imageSmoothingEnabled = true;
      context.imageSmoothingQuality = 'high';
      const scale = Math.min(size / width, size / height);
      const targetWidth = width * scale;
      const targetHeight = height * scale;
      context.drawImage(
        image,
        (size - targetWidth) / 2,
        (size - targetHeight) / 2,
        targetWidth,
        targetHeight
      );
      const blob = await new Promise<Blob>((resolve, reject) => {
        canvas.toBlob(value => value ? resolve(value) : reject(new Error('png-encode-failed')), 'image/png');
      });
      if (blob.size === 0 || blob.size > 2 * 1024 * 1024) throw new Error('favicon-size-limit');
      return new File([blob], `${file.name.replace(/\.[^.]+$/, '')}.png`, {
        type: 'image/png',
        lastModified: file.lastModified
      });
    } finally {
      URL.revokeObjectURL(objectUrl);
    }
  }
}
