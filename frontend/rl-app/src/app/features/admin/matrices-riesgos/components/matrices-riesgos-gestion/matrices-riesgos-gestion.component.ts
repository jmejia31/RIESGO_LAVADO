import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatricesRiesgosService } from '../../data-access/matrices-riesgos.service';
import { RiesgoDto, RiesgoGuardarDto } from '../../models/matrices-riesgos-fase11.models';

@Component({
  selector: 'app-matrices-riesgos-gestion',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './matrices-riesgos-gestion.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class MatricesRiesgosGestionComponent implements OnInit {
  private readonly service = inject(MatricesRiesgosService);

  readonly riesgos = signal<RiesgoDto[]>([]);
  readonly cargando = signal(false);
  readonly guardando = signal(false);
  readonly error = signal<string | null>(null);
  readonly mensaje = signal<string | null>(null);
  readonly editandoId = signal(0);

  codigo = '';
  nombre = '';
  descripcion = '';
  activo = true;

  ngOnInit(): void {
    this.cargar();
  }

  cargar(): void {
    this.cargando.set(true);
    this.error.set(null);
    this.service.listarRiesgos(true).subscribe({
      next: riesgos => {
        this.riesgos.set(riesgos);
        this.cargando.set(false);
      },
      error: error => this.finalizarError(error, 'No se pudieron cargar los riesgos.')
    });
  }

  nuevo(): void {
    this.editandoId.set(0);
    this.codigo = '';
    this.nombre = '';
    this.descripcion = '';
    this.activo = true;
    this.error.set(null);
    this.mensaje.set(null);
  }

  editar(riesgo: RiesgoDto): void {
    this.editandoId.set(riesgo.rieId);
    this.codigo = riesgo.rieCodigo;
    this.nombre = riesgo.rieNombre;
    this.descripcion = riesgo.rieDescripcion ?? '';
    this.activo = riesgo.rieActivo;
    this.error.set(null);
    this.mensaje.set(null);
  }

  guardar(): void {
    const codigo = this.codigo.trim();
    const nombre = this.nombre.trim();
    if (!codigo || !nombre) {
      this.error.set('Código y nombre son obligatorios.');
      return;
    }

    if (codigo.length > 30 || nombre.length > 250 || this.descripcion.trim().length > 2000) {
      this.error.set('Revise las longitudes máximas permitidas del riesgo.');
      return;
    }

    const dto: RiesgoGuardarDto = {
      rieCodigo: codigo,
      rieNombre: nombre,
      rieDescripcion: this.descripcion.trim() || null,
      rieActivo: this.activo
    };

    this.guardando.set(true);
    this.error.set(null);
    const id = this.editandoId();
    const solicitud = id > 0 ? this.service.actualizarRiesgo(id, dto) : this.service.crearRiesgo(dto);
    solicitud.subscribe({
      next: () => {
        this.guardando.set(false);
        this.mensaje.set(id > 0 ? 'Riesgo actualizado correctamente.' : 'Riesgo creado correctamente.');
        this.nuevo();
        this.cargar();
      },
      error: error => {
        this.guardando.set(false);
        this.error.set(this.mensajeError(error, 'No se pudo guardar el riesgo.'));
      }
    });
  }

  private finalizarError(error: unknown, mensaje: string): void {
    this.cargando.set(false);
    this.error.set(this.mensajeError(error, mensaje));
  }

  private mensajeError(error: unknown, mensaje: string): string {
    const respuesta = error as { error?: { mensaje?: string }; message?: string };
    return respuesta?.error?.mensaje || respuesta?.message || mensaje;
  }
}
