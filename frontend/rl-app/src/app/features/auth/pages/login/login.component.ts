import { ChangeDetectionStrategy, Component, signal, OnInit, OnDestroy, inject } from '@angular/core';
import { CommonModule }      from '@angular/common';
import { Router, ActivatedRoute } from '@angular/router';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { AuthService } from '../../../../core/auth/auth.service';
import { ConfiguracionService } from '../../../../core/configuration/configuracion.service';

@Component({
  selector:    'app-login',
  standalone:  true,
  imports:     [CommonModule, ReactiveFormsModule],
  templateUrl: './login.component.html',
  changeDetection: ChangeDetectionStrategy.Eager
})
export class LoginComponent implements OnInit, OnDestroy {
  private fb = inject(FormBuilder);
  private auth = inject(AuthService);
  private router = inject(Router);
  public configService = inject(ConfiguracionService);
  private route = inject(ActivatedRoute);

  form: FormGroup;
  cargando  = signal(false);
  error     = signal('');
  mostrarPw = signal(false);

  // Carrusel
  slideActual = signal(0);
  intervalo: any;
  slides: any[] = [];

  readonly currentYear = new Date().getFullYear();

  constructor() {
    this.form = this.fb.group({
      email:    ['', [Validators.required]],
      password: ['', [Validators.required, Validators.minLength(6)]],
      remember: [false]
    });
  }

  ngOnInit(): void {
    // Limpiar cualquier sesión previa para evitar tokens obsoletos
    localStorage.removeItem('access_token');
    localStorage.removeItem('refresh_token');
    localStorage.removeItem('token_expira');

    this.cargarSlides();
    this.configService.CargarConfiguracion().subscribe();
    this.verificarExpiracion();
  }

  verificarExpiracion() {
    const razon = this.route.snapshot.queryParamMap.get('razon');
    if (razon === 'expirada') {
      import('sweetalert2').then(Swal => {
        Swal.default.fire({
          allowOutsideClick: false,
          title: 'Sesión Expirada',
          text: 'Su sesión ha caducado por inactividad. Por favor, ingrese de nuevo.',
          icon: 'info',
          confirmButtonText: 'Entendido',
          confirmButtonColor: '#1e3a8a'
        });
      });
    } else if (razon === 'cambio-password') {
      import('sweetalert2').then(Swal => {
        Swal.default.fire({
          allowOutsideClick: false,
          title: 'Cambio de contraseña requerido',
          text: 'Debe iniciar sesión y completar el cambio de contraseña provisional antes de acceder al sistema.',
          icon: 'warning',
          confirmButtonText: 'Entendido',
          confirmButtonColor: '#1e3a8a'
        });
      });
    }
  }

  cargarSlides() {
    this.configService.ObtenerSlides().subscribe({
      next: (data) => {
        this.slides = data;
        if (this.slides.length > 0) this.iniciarCarrusel();
      },
      error: () => {
        // Fallback en caso de error
        this.slides = [
          { id: 1, imagenUrl: 'assets/login/slide1.png', titulo: 'Prevención de Lavado de Activos', descripcion: 'Gestión integral de riesgos y alertas para proteger la institución.', orden: 1, activo: true },
          { id: 2, imagenUrl: 'assets/login/slide2.png', titulo: 'Monitoreo de Listas', descripcion: 'Detección oportuna de personas expuestas políticamente o de interés.', orden: 2, activo: true },
          { id: 3, imagenUrl: 'assets/login/slide3.png', titulo: 'Cumplimiento Normativo IHSS', descripcion: 'Alineación institucional con regulaciones de transparencia y control interno.', orden: 3, activo: true }
        ];
      }
    });
  }

  ngOnDestroy(): void {
    if (this.intervalo) clearInterval(this.intervalo);
  }

  iniciarCarrusel() {
    this.intervalo = setInterval(() => {
      this.slideActual.update(val => (val + 1) % this.slides.length);
    }, 5000);
  }

  seleccionarSlide(idx: number) {
    this.slideActual.set(idx);
    if (this.intervalo) {
      clearInterval(this.intervalo);
      this.iniciarCarrusel();
    }
  }

  onSubmit() {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }

    this.cargando.set(true);
    this.error.set('');

    this.auth.login(this.form.value).subscribe({
      next: (res) => {
        const usuario = res.datos.usuario;
        if (usuario?.debeCambiarPassword) {
          this.cargando.set(false);
          this.forzarCambioPassword(this.form.value.password);
        } else {
          this.cargando.set(false);
          this.router.navigate(['/home']);
        }
      },
      error: err => {
        if (err.status === 429) {
          this.error.set('Demasiados intentos. Su IP ha sido bloqueada temporalmente. Intente en 1 minuto.');
        } else {
          // La pantalla de acceso no debe mostrar mensajes recibidos de infraestructura.
          // El backend ya los sanitiza; este texto fijo es una segunda barrera de seguridad.
          this.error.set('No fue posible iniciar sesión. Verifique sus credenciales o contacte al administrador del sistema.');
        }
        this.cargando.set(false);
      }
    });
  }

  forzarCambioPassword(passwordActual: string) {
    import('sweetalert2').then(Swal => {
      Swal.default.fire({
        allowOutsideClick: false,
        title: 'Cambio de Contraseña Obligatorio',
        text: 'Por seguridad, debe cambiar la contraseña provisional que se le asignó.',
        icon: 'warning',
        html: `
          <div class="space-y-4 text-left">
            <div>
              <label class="block text-xs font-bold text-gray-500 uppercase mb-1">Nueva Contraseña</label>
              <div style="position: relative;">
                <input id="swal-new-password" type="password" class="w-full p-3 border rounded-xl focus:outline-none focus:ring-2 focus:ring-ihss-500" placeholder="Mínimo 8 caracteres" style="margin-bottom: 15px; padding-right: 40px; width: 100%;">
                <button type="button" id="toggle-new-pw" style="position: absolute; right: 10px; top: 12px; border: none; background: none; cursor: pointer; color: #9ca3af;">
                  <svg style="width: 20px; height: 20px;" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
                  </svg>
                </button>
              </div>
            </div>
            <div>
              <label class="block text-xs font-bold text-gray-500 uppercase mb-1">Confirmar Nueva Contraseña</label>
              <div style="position: relative;">
                <input id="swal-confirm-password" type="password" class="w-full p-3 border rounded-xl focus:outline-none focus:ring-2 focus:ring-ihss-500" placeholder="Repita la contraseña" style="padding-right: 40px; width: 100%;">
                <button type="button" id="toggle-confirm-pw" style="position: absolute; right: 10px; top: 12px; border: none; background: none; cursor: pointer; color: #9ca3af;">
                  <svg style="width: 20px; height: 20px;" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
                  </svg>
                </button>
              </div>
            </div>
          </div>
        `,
        focusConfirm: false,
        allowEscapeKey: false,
        confirmButtonText: 'Actualizar Contraseña',
        confirmButtonColor: '#1e3a8a',
        showLoaderOnConfirm: true,
        didOpen: () => {
          const toggleNew = document.getElementById('toggle-new-pw');
          const toggleConfirm = document.getElementById('toggle-confirm-pw');
          const newPwInput = document.getElementById('swal-new-password') as HTMLInputElement;
          const confirmPwInput = document.getElementById('swal-confirm-password') as HTMLInputElement;

          const eyeOpen = `<svg style="width: 20px; height: 20px;" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
          </svg>`;

          const eyeClosed = `<svg style="width: 20px; height: 20px;" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M13.875 18.825A10.05 10.05 0 0112 19c-4.478 0-8.268-2.943-9.543-7a9.97 9.97 0 011.563-3.029m5.858.908a3 3 0 114.243 4.243M9.878 9.878l4.242 4.242M9.88 9.88l-3.29-3.29m7.532 7.532l3.29 3.29M3 3l3.59 3.59m0 0A9.953 9.953 0 0112 5c4.478 0 8.268 2.943 9.543 7a10.025 10.025 0 01-4.132 5.411m0 0L21 21"/>
          </svg>`;

          toggleNew?.addEventListener('click', () => {
            const isPassword = newPwInput.type === 'password';
            newPwInput.type = isPassword ? 'text' : 'password';
            toggleNew.innerHTML = isPassword ? eyeClosed : eyeOpen;
          });

          toggleConfirm?.addEventListener('click', () => {
            const isPassword = confirmPwInput.type === 'password';
            confirmPwInput.type = isPassword ? 'text' : 'password';
            toggleConfirm.innerHTML = isPassword ? eyeClosed : eyeOpen;
          });
        },
        preConfirm: () => {
          const newPassword = (document.getElementById('swal-new-password') as HTMLInputElement).value;
          const confirmPassword = (document.getElementById('swal-confirm-password') as HTMLInputElement).value;

          if (!newPassword || newPassword.length < 8) {
            Swal.default.showValidationMessage('La nueva contraseña debe tener al menos 8 caracteres.');
            return false;
          }
          if (newPassword !== confirmPassword) {
            Swal.default.showValidationMessage('Las contraseñas no coinciden.');
            return false;
          }
          if (newPassword === passwordActual) {
            Swal.default.showValidationMessage('La nueva contraseña no puede ser igual a la provisional.');
            return false;
          }

          return this.auth.cambiarPassword(passwordActual, newPassword).toPromise()
            .then(
              response => {
                if (!response?.success) {
                  throw new Error(response?.mensaje || 'Error al actualizar la contraseña.');
                }
                return { success: true, newPassword };
              },
              error => {
                Swal.default.showValidationMessage(error.error?.mensaje || 'Error de red al actualizar la contraseña.');
                return false;
              }
            );
        }
      }).then((result) => {
        if (result.isConfirmed && result.value) {
          Swal.default.fire({
            allowOutsideClick: false,
            title: '¡Contraseña Actualizada!',
            text: 'Su contraseña ha sido cambiada con éxito. Iniciando sesión...',
            icon: 'success',
            timer: 2000,
            showConfirmButton: false
          }).then(() => {
            const nuevaPass = (result.value as any).newPassword;
            const email = this.form.value.email;

            this.cargando.set(true);
            this.auth.login({ email, password: nuevaPass }).subscribe({
              next: () => {
                this.cargando.set(false);
                this.router.navigate(['/home']);
              },
              error: () => {
                this.cargando.set(false);
                this.auth.logout();
              }
            });
          });
        }
      });
    });
  }

  abrirRecuperacionPassword() {
    import('sweetalert2').then(Swal => {
      Swal.default.fire({
        allowOutsideClick: false,
        title: 'Recuperar Contraseña',
        text: 'Ingrese su correo electrónico institucional para enviarle una clave provisional.',
        input: 'email',
        inputPlaceholder: 'ejemplo@ihss.hn',
        showCancelButton: true,
        confirmButtonText: 'Enviar Clave',
        cancelButtonText: 'Cancelar',
        confirmButtonColor: '#1e3a8a',
        showLoaderOnConfirm: true,
        preConfirm: (email) => {
          if (!email) {
            Swal.default.showValidationMessage('El correo electrónico es requerido.');
            return false;
          }
          return this.auth.recuperarPassword(email).toPromise()
            .then(
              response => {
                if (!response?.success) {
                  throw new Error(response?.mensaje || 'Error al enviar el correo.');
                }
                return response;
              },
              error => {
                Swal.default.showValidationMessage(error.error?.mensaje || 'Error al procesar la solicitud.');
                return false;
              }
            );
        }
      }).then((result) => {
        if (result.isConfirmed && result.value) {
          Swal.default.fire({
            allowOutsideClick: false,
            title: '¡Correo Enviado!',
            text: result.value.mensaje || 'Se ha enviado una clave provisional a su correo electrónico.',
            icon: 'success',
            confirmButtonColor: '#1e3a8a'
          });
        }
      });
    });
  }

  get emailCtrl()    { return this.form.get('email')!; }
  get passwordCtrl() { return this.form.get('password')!; }
}
