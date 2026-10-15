import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, ElementRef, OnDestroy, OnInit, computed, inject, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Observable } from 'rxjs';
import { MatricesRiesgosService } from '../../data-access/matrices-riesgos.service';
import { RiesgoDto, RiesgoGuardarDto } from '../../models/matrices-riesgos-fase11.models';

import { ActionIconComponent } from '../../../../../shared/components/action-icon/action-icon.component';
import { DataPaginationComponent } from '../../../../../shared/components/data-pagination/data-pagination.component';
import { PageSizeSelectorComponent } from '../../../../../shared/components/page-size-selector/page-size-selector.component';
import { normalizarMojibakeVisibleUtf8 } from '../../utils/text-encoding.util';
import { InstitutionalConfirmationService } from '../../../../../shared/confirmation/institutional-confirmation.service';

@Component({
  selector: 'app-matrices-riesgos-gestion',
  standalone: true,
  imports: [ActionIconComponent, DataPaginationComponent, PageSizeSelectorComponent, CommonModule, FormsModule],
  templateUrl: './matrices-riesgos-gestion.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class MatricesRiesgosGestionComponent implements OnInit, OnDestroy {
  private readonly service = inject(MatricesRiesgosService);
  private readonly confirmation = inject(InstitutionalConfirmationService);
  private readonly codigoInput = viewChild<ElementRef<HTMLInputElement>>('codigoInput');

  readonly riesgos = signal<RiesgoDto[]>([]);
  readonly cargando = signal(false);
  readonly guardando = signal(false);
  readonly error = signal<string | null>(null);
  readonly errorFormulario = signal<string | null>(null);
  readonly mensaje = signal<string | null>(null);
  readonly editandoId = signal(0);
  readonly Math = Math;
  readonly pagina = signal(1);
  readonly tamanoPagina = signal(10);
  readonly totalRegistros = signal(0);
  readonly filtroBuscar = signal('');
  readonly filtroEstado = signal<'TODOS' | 'ACTIVOS' | 'INACTIVOS'>('TODOS');
  readonly modalRiesgoAbierto = signal(false);
  readonly opcionesTamanoPagina = [10, 25, 50] as const;
  get etiquetaGuardarRiesgo(): string {
    return this.editandoId() > 0 ? 'Guardar cambios del riesgo' : 'Guardar nuevo riesgo';
  }

  private secuenciaCarga = 0;
  private temporizadorBusqueda: ReturnType<typeof setTimeout> | null = null;
  private focoRetorno: HTMLElement | null = null;
  private valoresIniciales = '';
  readonly totalPaginas = computed(() => this.totalRegistros() === 0 ? 0 : Math.ceil(this.totalRegistros() / this.tamanoPagina()));
  codigo = '';
  nombre = '';
  descripcion = '';
  activo = true;

  ngOnInit(): void {
    this.cargar();
  }

  ngOnDestroy(): void {
    this.cancelarBusquedaPendiente();
    this.secuenciaCarga++;
  }

  cargar(): void {
    const solicitudId = ++this.secuenciaCarga;
    this.cargando.set(true);
    this.error.set(null);
    const estado = this.filtroEstado();
    const activo = estado === 'ACTIVOS' ? true : estado === 'INACTIVOS' ? false : undefined;
    const incluirInactivos = estado !== 'ACTIVOS';
    this.service.listarRiesgosPaginados(
      incluirInactivos,
      this.pagina(),
      this.tamanoPagina(),
      this.filtroBuscar(),
      activo
    ).subscribe({
      next: resultado => {
        if (solicitudId !== this.secuenciaCarga) return;
        this.riesgos.set(resultado.items.map(item => ({
          ...item,
          rieNombre: normalizarMojibakeVisibleUtf8(item.rieNombre),
          rieDescripcion: item.rieDescripcion ? normalizarMojibakeVisibleUtf8(item.rieDescripcion) : item.rieDescripcion
        })));
        this.totalRegistros.set(resultado.totalRegistros);
        this.pagina.set(resultado.pagina);
        this.tamanoPagina.set(resultado.tamanoPagina);
        this.cargando.set(false);
      },
      error: (error: unknown) => { if (solicitudId === this.secuenciaCarga) this.finalizarError(error, 'No se pudieron cargar los riesgos.'); }
    });
  }

  cambiarPagina(pagina: number): void {
    if (pagina < 1 || pagina > this.totalPaginas() || pagina === this.pagina()) return;
    this.cancelarBusquedaPendiente();
    this.pagina.set(pagina);
    this.cargar();
  }

  cambiarTamanoPagina(tamano: number): void {
    const normalizado = Number(tamano);
    if (!this.opcionesTamanoPagina.includes(normalizado as 10 | 25 | 50)) return;
    this.cancelarBusquedaPendiente();
    this.tamanoPagina.set(normalizado);
    this.pagina.set(1);
    this.cargar();
  }

  cambiarBuscar(valor: string): void {
    this.filtroBuscar.set(valor ?? '');
    this.pagina.set(1);
    this.cancelarBusquedaPendiente();
    this.temporizadorBusqueda = setTimeout(() => {
      this.temporizadorBusqueda = null;
      this.cargar();
    }, 300);
  }

  cambiarEstadoFiltro(valor: string): void {
    const estado = valor === 'ACTIVOS' || valor === 'INACTIVOS' ? valor : 'TODOS';
    if (estado === this.filtroEstado()) return;
    this.cancelarBusquedaPendiente();
    this.filtroEstado.set(estado);
    this.pagina.set(1);
    this.cargar();
  }

  limpiarFiltros(): void {
    if (!this.filtroBuscar().trim() && this.filtroEstado() === 'TODOS') return;
    this.cancelarBusquedaPendiente();
    this.filtroBuscar.set('');
    this.filtroEstado.set('TODOS');
    this.pagina.set(1);
    this.cargar();
  }

  private cancelarBusquedaPendiente(): void {
    if (this.temporizadorBusqueda !== null) {
      clearTimeout(this.temporizadorBusqueda);
      this.temporizadorBusqueda = null;
    }
  }

  nuevo(event?: Event): void {
    if (event?.currentTarget instanceof HTMLElement) this.focoRetorno = event.currentTarget;
    this.editandoId.set(0);
    this.codigo = '';
    this.nombre = '';
    this.descripcion = '';
    this.activo = true;
    this.errorFormulario.set(null);
    this.mensaje.set(null);
    this.valoresIniciales = this.serializarValores();
    this.modalRiesgoAbierto.set(true);
    this.programarFocoInicial();
  }

  editar(riesgo: RiesgoDto, event?: Event): void {
    if (event?.currentTarget instanceof HTMLElement) this.focoRetorno = event.currentTarget;
    this.editandoId.set(riesgo.rieId);
    this.codigo = riesgo.rieCodigo;
    this.nombre = riesgo.rieNombre;
    this.descripcion = riesgo.rieDescripcion ?? '';
    this.activo = riesgo.rieActivo;
    this.errorFormulario.set(null);
    this.mensaje.set(null);
    this.valoresIniciales = this.serializarValores();
    this.modalRiesgoAbierto.set(true);
    this.programarFocoInicial();
  }

  async solicitarCierre(): Promise<void> {
    if (this.guardando()) return;
    if (this.formularioModificado()) {
      const confirmado = await this.confirmation.confirm({
        title: 'Descartar cambios del riesgo',
        message: 'Hay cambios sin guardar. ¿Desea descartarlos?',
        confirmLabel: 'Descartar cambios',
        cancelLabel: 'Continuar editando',
        confirmIcon: 'reject',
        confirmVariant: 'danger',
        icon: 'warning'
      });
      if (!confirmado) return;
    }
    this.cerrarFormulario();
  }

  private formularioModificado(): boolean {
    return this.serializarValores() !== this.valoresIniciales;
  }

  private serializarValores(): string {
    return JSON.stringify([this.codigo, this.nombre, this.descripcion, this.activo, this.editandoId()]);
  }

  private programarFocoInicial(): void {
    setTimeout(() => this.codigoInput()?.nativeElement.focus({ preventScroll: true }));
  }

  private cerrarFormulario(): void {
    this.modalRiesgoAbierto.set(false);
    this.editandoId.set(0);
    this.codigo = '';
    this.nombre = '';
    this.descripcion = '';
    this.activo = true;
    this.errorFormulario.set(null);
    this.valoresIniciales = this.serializarValores();
    const foco = this.focoRetorno;
    this.focoRetorno = null;
    setTimeout(() => {
      if (foco?.isConnected && !foco.closest('[inert]')) foco.focus({ preventScroll: true });
    });
  }

  guardar(): void {
    const codigo = this.codigo.trim();
    const nombre = this.nombre.trim();
    if (!codigo || !nombre) {
      this.errorFormulario.set('Código y nombre son obligatorios.');
      return;
    }

    if (codigo.length > 30 || nombre.length > 250 || this.descripcion.trim().length > 2000) {
      this.errorFormulario.set('Revise las longitudes máximas permitidas del riesgo.');
      return;
    }

    const dto: RiesgoGuardarDto = {
      rieCodigo: codigo,
      rieNombre: nombre,
      rieDescripcion: this.descripcion.trim() || null,
      rieActivo: this.activo
    };

    this.guardando.set(true);
    this.errorFormulario.set(null);
    const id = this.editandoId();
    const solicitud: Observable<unknown> = id > 0
      ? this.service.actualizarRiesgo(id, dto)
      : this.service.crearRiesgo(dto);
    solicitud.subscribe({
      next: () => {
        this.guardando.set(false);
        this.mensaje.set(id > 0 ? 'Riesgo actualizado correctamente.' : 'Riesgo creado correctamente.');
        this.cerrarFormulario();
        this.cargar();
      },
      error: (error: unknown) => {
        this.guardando.set(false);
        this.errorFormulario.set(this.mensajeError(error, 'No se pudo guardar el riesgo.'));
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
