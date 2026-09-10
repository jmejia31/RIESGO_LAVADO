import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Observable } from 'rxjs';
import { MatricesRiesgosService } from '../../data-access/matrices-riesgos.service';
import { RiesgoDto, RiesgoGuardarDto } from '../../models/matrices-riesgos-fase11.models';

import { ActionIconComponent } from '../../../../../shared/components/action-icon/action-icon.component';
import { DataPaginationComponent } from '../../../../../shared/components/data-pagination/data-pagination.component';
import { PageSizeSelectorComponent } from '../../../../../shared/components/page-size-selector/page-size-selector.component';

@Component({
  selector: 'app-matrices-riesgos-gestion',
  standalone: true,
  imports: [ActionIconComponent, DataPaginationComponent, PageSizeSelectorComponent, CommonModule, FormsModule],
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
  readonly Math = Math;
  readonly pagina = signal(1);
  readonly tamanoPagina = signal(25);
  readonly totalRegistros = signal(0);
  private secuenciaCarga = 0;
  readonly totalPaginas = computed(() => this.totalRegistros() === 0 ? 0 : Math.ceil(this.totalRegistros() / this.tamanoPagina()));
  codigo = '';
  nombre = '';
  descripcion = '';
  activo = true;

  ngOnInit(): void {
    this.cargar();
  }

  cargar(): void {
    const solicitudId = ++this.secuenciaCarga;
    this.cargando.set(true);
    this.error.set(null);
    this.service.listarRiesgosPaginados(true, this.pagina(), this.tamanoPagina()).subscribe({
      next: resultado => {
        if (solicitudId !== this.secuenciaCarga) return;
        this.riesgos.set(resultado.items);
        this.totalRegistros.set(resultado.totalRegistros);
        this.pagina.set(resultado.pagina);
        this.cargando.set(false);
      },
      error: (error: unknown) => { if (solicitudId === this.secuenciaCarga) this.finalizarError(error, 'No se pudieron cargar los riesgos.'); }
    });
  }

  cambiarPagina(pagina: number): void {
    if (pagina < 1 || pagina > this.totalPaginas() || pagina === this.pagina()) return;
    this.pagina.set(pagina);
    this.cargar();
  }

  cambiarTamanoPagina(tamano: number): void {
    if (![10, 25, 50].includes(Number(tamano))) return;
    this.tamanoPagina.set(Number(tamano));
    this.pagina.set(1);
    this.cargar();
  }

  nuevo(limpiarMensajes = true): void {
    this.editandoId.set(0);
    this.codigo = '';
    this.nombre = '';
    this.descripcion = '';
    this.activo = true;
    this.error.set(null);
    if (limpiarMensajes) this.mensaje.set(null);
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
    const solicitud: Observable<unknown> = id > 0
      ? this.service.actualizarRiesgo(id, dto)
      : this.service.crearRiesgo(dto);
    solicitud.subscribe({
      next: () => {
        this.guardando.set(false);
        this.mensaje.set(id > 0 ? 'Riesgo actualizado correctamente.' : 'Riesgo creado correctamente.');
        this.nuevo(false);
        this.cargar();
      },
      error: (error: unknown) => {
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
