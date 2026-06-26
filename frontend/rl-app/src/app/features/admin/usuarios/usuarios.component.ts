import { ChangeDetectionStrategy, Component, OnInit, OnDestroy, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { Subscription } from 'rxjs';
import { UsuarioInfo as UsuarioInfoDto } from '../../../core/models/auth.models';
import { CatalogoService } from '../../../core/services/catalogo.service';
import { environment } from '../../../../environments/environment';
import { Rol, Dominio, Modulo } from '../../../core/models/catalogo.models';
import { ActiveDirectorioService, ResultadoValidacionAd } from '../../../core/services/active-directorio.service';

type EstadoAd = 'idle' | 'verificando' | 'valido' | 'no-existe' | 'bloqueado' | 'inactivo' | 'error';

@Component({
  selector:    'app-usuarios',
  standalone:  true,
  imports:     [CommonModule, FormsModule, ReactiveFormsModule],
  templateUrl: './usuarios.component.html',
  changeDetection: ChangeDetectionStrategy.Eager
})
export class UsuariosComponent implements OnInit, OnDestroy {
  usuarios     = signal<UsuarioInfoDto[]>([]);
  roles        = signal<Rol[]>([]);
  cargando     = signal(true);
  mostrarForm  = signal(false);
  guardando    = signal(false);
  errorGuardar = signal<string | null>(null);
  errorCatalogos = signal<string | null>(null);
  
  editando             = signal(false);
  usuarioUidParaEditar = signal<string | null>(null);

  listaDominios = signal<Dominio[]>([]);
  listaModulos = signal<Modulo[]>([]);

  // Estado de la validación AD
  estadoAd   = signal<EstadoAd>('idle');
  resultadoAd = signal<ResultadoValidacionAd | null>(null);

  form!: FormGroup;

  private subs = new Subscription();

  constructor(
    private http: HttpClient,
    private cat:  CatalogoService,
    private fb:   FormBuilder,
    private ad:   ActiveDirectorioService
  ) {}

  ngOnInit() {
    this.construirForm();
    this.cargar();
    this.cat.roles().subscribe(r => this.roles.set(r));
    this.cat.dominios().subscribe({
      next:  d => {
        this.listaDominios.set(d);
      },
      error: e => {
        console.error('Error cargando dominios:', e);
        this.errorCatalogos.set('Error al cargar la lista de dominios.');
      }
    });
    this.cat.modulos().subscribe({
      next: m => this.listaModulos.set(m),
      error: e => console.error('Error cargando modulos:', e)
    });
  }

  ngOnDestroy() {
    this.subs.unsubscribe();
  }

  construirForm() {
    this.form = this.fb.group({
      nombre:          ['', Validators.required],
      apellido:        ['', Validators.required],
      email:           ['', [Validators.required, Validators.email]],
      password:        [''],
      rolId:           [null, Validators.required],
      esUsuarioDominio: [0],
      usuarioDominio:  [''],
      dominioId:       [null],
      modulosIds:      [[]]
    });

    // Si el checkbox se marca: campo requerido + dispara validación AD al escribir
    this.form.get('esUsuarioDominio')?.valueChanges.subscribe(val => {
      const ctrl = this.form.get('usuarioDominio');
      const dominioCtrl = this.form.get('dominioId');
      const passwordCtrl = this.form.get('password');

      if (Number(val) === 1) {
        ctrl?.setValidators([Validators.required]);
        dominioCtrl?.setValidators([Validators.required]);
        passwordCtrl?.clearValidators();
        passwordCtrl?.setValue('');
      } else {
        ctrl?.clearValidators();
        ctrl?.setValue('');
        dominioCtrl?.clearValidators();
        dominioCtrl?.setValue(null);
        passwordCtrl?.clearValidators();
        this.estadoAd.set('idle');
        this.resultadoAd.set(null);
      }
      ctrl?.updateValueAndValidity();
      dominioCtrl?.updateValueAndValidity();
      passwordCtrl?.updateValueAndValidity();
    });
  }

  validarAd() {
    if (!this.esDominio) return;
    const valor = (this.form.get('usuarioDominio')?.value ?? '').trim();
    if (valor.length < 2) {
      this.estadoAd.set('idle');
      this.resultadoAd.set(null);
      return;
    }
    const domId = this.form.get('dominioId')?.value;
    const dom = this.listaDominios().find(d => d.domId === Number(domId));
    const dominioNombre = dom ? dom.domNombre : '';
    this.estadoAd.set('verificando');
    this.ad.validarUsuario(valor, dominioNombre).subscribe({
      next: resp => {
        const d = resp.datos;
        this.resultadoAd.set(d);
        if (!d.existe)        this.estadoAd.set('no-existe');
        else if (d.bloqueado) this.estadoAd.set('bloqueado');
        else if (!d.activo)   this.estadoAd.set('inactivo');
        else                  this.estadoAd.set('valido');

        const ctrl = this.form.get('usuarioDominio');
        if (d.existe && !d.bloqueado && d.activo) ctrl?.setErrors(null);
        else ctrl?.setErrors({ adInvalido: true });
      },
      error: () => {
        this.estadoAd.set('error');
        this.resultadoAd.set(null);
      }
    });
  }

  get esDominio(): boolean {
    return Number(this.form.get('esUsuarioDominio')?.value) === 1;
  }

  cargar() {
    this.cargando.set(true);
    this.http.get<{ success: boolean; datos: UsuarioInfoDto[] }>(`${environment.apiUrl}/auth/usuarios`)
      .subscribe({
        next:  r => { this.usuarios.set(r.datos); this.cargando.set(false); },
        error: () => this.cargando.set(false)
      });
  }

  cancelar() {
    this.mostrarForm.set(false);
    this.editando.set(false);
    this.usuarioUidParaEditar.set(null);
    this.form.reset({ esUsuarioDominio: 0, modulosIds: [] });
    this.estadoAd.set('idle');
    this.resultadoAd.set(null);
  }

  prepararEdicion(u: UsuarioInfoDto) {
    this.editando.set(true);
    this.usuarioUidParaEditar.set(u.uid);
    this.mostrarForm.set(true);

    this.form.patchValue({
      nombre:           u.nombre,
      apellido:         u.apellido,
      email:            u.email,
      rolId:            u.rolId,
      esUsuarioDominio: u.esUsuarioDominio,
      usuarioDominio:   u.usuarioDominio,
      dominioId:        u.dominioId,
      modulosIds:       u.modulosIds || []
    });

    this.form.get('password')?.clearValidators();
    this.form.get('password')?.updateValueAndValidity();

    if (u.esUsuarioDominio === 1) {
      this.estadoAd.set('valido');
    }
  }

  prepararNuevo() {
    this.editando.set(false);
    this.usuarioUidParaEditar.set(null);
    this.mostrarForm.set(true);
    this.form.reset({ esUsuarioDominio: 0, modulosIds: [] });
    
    this.form.get('password')?.clearValidators();
    this.form.get('password')?.updateValueAndValidity();
    
    this.estadoAd.set('idle');
  }

  guardar() {
    if (this.esDominio && (this.estadoAd() === 'verificando' || this.estadoAd() === 'no-existe' || this.estadoAd() === 'bloqueado' || this.estadoAd() === 'inactivo')) {
      return;
    }
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.errorGuardar.set(null);
    this.guardando.set(true);

    const obs = this.editando() 
        ? this.http.put<{ success: boolean; mensaje: string }>(`${environment.apiUrl}/auth/usuarios/${this.usuarioUidParaEditar()}`, this.form.value)
        : this.http.post<{ success: boolean; mensaje: string }>(`${environment.apiUrl}/auth/usuarios`, this.form.value);

    obs.subscribe({
      next:  () => {
        this.cancelar();
        this.cargar();
        this.guardando.set(false);
      },
      error: (err) => {
        this.guardando.set(false);
        const errorData = err?.error;
        let msg = 'Error al guardar el usuario.';

        if (errorData?.errores && Array.isArray(errorData.errores)) {
          msg = errorData.errores.join(', ');
        } else if (errorData?.mensaje) {
          msg = errorData.mensaje;
        }

        this.errorGuardar.set(msg);
      }
    });
  }

  reiniciarPassword(u: UsuarioInfoDto) {
    import('sweetalert2').then(Swal => {
      Swal.default.fire({
        title: '¿Reiniciar contraseña?',
        text: `Se generará una nueva contraseña provisional y se enviará por correo electrónico a ${u.email}.`,
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#1d4ed8',
        cancelButtonColor: '#d1d5db',
        confirmButtonText: 'Sí, reiniciar',
        cancelButtonText: 'Cancelar'
      }).then((result) => {
        if (result.isConfirmed) {
          Swal.default.fire({
            title: 'Procesando...',
            text: 'Por favor, espere.',
            allowOutsideClick: false,
            didOpen: () => {
              Swal.default.showLoading();
            }
          });

          this.http.post<{ success: boolean; mensaje: string }>(
            `${environment.apiUrl}/auth/recuperar-password`, 
            { email: u.email }
          ).subscribe({
            next: (resp) => {
              Swal.default.fire({
                allowOutsideClick: false,
                title: 'Contraseña Reiniciada',
                text: `Se ha enviado la nueva clave provisional al correo electrónico del usuario.`,
                icon: 'success',
                confirmButtonColor: '#1d4ed8'
              });
            },
            error: (err) => {
              Swal.default.fire({
                allowOutsideClick: false,
                title: 'Error',
                text: err.error?.mensaje || 'No se pudo reiniciar la contraseña.',
                icon: 'error',
                confirmButtonColor: '#1d4ed8'
              });
            }
          });
        }
      });
    });
  }

  ctrl(name: string) { return this.form.get(name)!; }
  esInvalido(name: string) { const c = this.ctrl(name); return c.invalid && c.touched; }

  onModuloChange(modId: number, event: Event) {
    const checked = (event.target as HTMLInputElement).checked;
    const ctrl = this.form.get('modulosIds');
    const currentIds: number[] = ctrl?.value || [];
    if (checked) {
      if (!currentIds.includes(modId)) {
        ctrl?.setValue([...currentIds, modId]);
      }
    } else {
      ctrl?.setValue(currentIds.filter(id => id !== modId));
    }
  }

  isModuloSelected(modId: number): boolean {
    const ids: number[] = this.form.get('modulosIds')?.value || [];
    return ids.includes(modId);
  }
}
