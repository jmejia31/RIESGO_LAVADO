import { AfterViewInit, ChangeDetectionStrategy, Component, ElementRef, OnDestroy, OnInit, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../../core/auth/auth.service';
import { ConfiguracionService } from '../../../core/configuration/configuracion.service';
import { CatalogoService } from '../../../core/configuration/catalogo.service';
import { Modulo } from '../../../core/configuration/catalogo.models';
import { GlobalHttpStateService } from '../../../core/services/global-http-state.service';

@Component({
  selector: 'app-main-layout',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './main-layout.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class MainLayoutComponent implements OnInit, AfterViewInit, OnDestroy {
  sidebarAbierto = signal(true);

  /** Lista completa de módulos activos cargados desde la API */
  readonly todosLosModulos = signal<Modulo[]>([]);

  private observadorDialogos?: MutationObserver;
  private dialogoActivo: HTMLElement | null = null;
  private focoPrevio: HTMLElement | null = null;
  private dialogoConTabindexTemporal: HTMLElement | null = null;
  private readonly estadoInertOriginal = new Map<HTMLElement, boolean>();

  constructor(
    readonly auth: AuthService,
    readonly configService: ConfiguracionService,
    readonly globalState: GlobalHttpStateService,
    private catalogoService: CatalogoService,
    private readonly host: ElementRef<HTMLElement>
  ) {}

  ngOnInit() {
    if (typeof window !== 'undefined' && window.innerWidth < 768) {
      this.sidebarAbierto.set(false);
    }

    this.configService.CargarConfiguracion().subscribe();
    this.catalogoService.modulos().subscribe({
      next:  mods => this.todosLosModulos.set(mods),
      error: ()   => this.todosLosModulos.set([])
    });
  }

  ngAfterViewInit(): void {
    if (typeof document === 'undefined' || typeof MutationObserver === 'undefined') return;

    this.observadorDialogos = new MutationObserver(() => this.sincronizarBloqueoModal());
    this.observadorDialogos.observe(this.host.nativeElement, { childList: true, subtree: true });
    document.addEventListener('keydown', this.mantenerFocoEnDialogo, true);
    this.sincronizarBloqueoModal();
  }

  ngOnDestroy(): void {
    this.observadorDialogos?.disconnect();
    this.observadorDialogos = undefined;

    if (typeof document !== 'undefined') {
      document.removeEventListener('keydown', this.mantenerFocoEnDialogo, true);
      document.body.classList.remove('modal-abierto');
    }

    this.restaurarElementosInert();
    this.limpiarTabindexTemporal();
    this.dialogoActivo = null;
    this.focoPrevio = null;
  }

  toggleSidebar() {
    this.sidebarAbierto.update(v => !v);
  }

  cerrarSidebarMovil() {
    if (typeof window !== 'undefined' && window.innerWidth < 768) {
      this.sidebarAbierto.set(false);
    }
  }

  enfocarContenido(): void {
    if (typeof document === 'undefined') return;

    setTimeout(() => {
      document.getElementById('contenido-principal')?.focus({ preventScroll: true });
    });
  }

  private sincronizarBloqueoModal(): void {
    if (typeof document === 'undefined') return;

    const dialogos = Array.from(
      this.host.nativeElement.querySelectorAll<HTMLElement>('dialog[open][aria-modal="true"], [role="dialog"][aria-modal="true"]')
    );
    const dialogo = dialogos.at(-1) ?? null;

    if (dialogo === this.dialogoActivo) return;

    this.restaurarElementosInert();
    this.limpiarTabindexTemporal();

    if (!dialogo) {
      this.dialogoActivo = null;
      document.body.classList.remove('modal-abierto');

      const focoARestaurar = this.focoPrevio;
      this.focoPrevio = null;
      queueMicrotask(() => {
        if (focoARestaurar?.isConnected && !focoARestaurar.inert) {
          focoARestaurar.focus({ preventScroll: true });
        }
      });
      return;
    }

    const focoActual = document.activeElement;
    if (!this.focoPrevio && focoActual instanceof HTMLElement && !dialogo.contains(focoActual)) {
      this.focoPrevio = focoActual;
    }

    this.dialogoActivo = dialogo;
    document.body.classList.add('modal-abierto');
    this.enfocarDialogo(dialogo);
    this.aplicarInertFueraDelDialogo(dialogo);
  }

  private aplicarInertFueraDelDialogo(dialogo: HTMLElement): void {
    const limite = this.host.nativeElement;
    let actual: HTMLElement | null = dialogo;

    while (actual && actual !== limite) {
      const padre: HTMLElement | null = actual.parentElement;
      if (!padre) break;

      for (const hermano of Array.from(padre.children)) {
        if (!(hermano instanceof HTMLElement) || hermano === actual) continue;

        if (!this.estadoInertOriginal.has(hermano)) {
          this.estadoInertOriginal.set(hermano, hermano.inert);
        }
        hermano.inert = true;
      }

      actual = padre;
    }
  }

  private restaurarElementosInert(): void {
    for (const [elemento, estadoOriginal] of this.estadoInertOriginal.entries()) {
      if (elemento.isConnected) {
        elemento.inert = estadoOriginal;
      }
    }
    this.estadoInertOriginal.clear();
  }

  private enfocarDialogo(dialogo: HTMLElement): void {
    const elementosEnfocables = this.obtenerElementosEnfocables(dialogo);
    const destino = elementosEnfocables[0] ?? dialogo;

    if (destino === dialogo && !dialogo.hasAttribute('tabindex')) {
      dialogo.setAttribute('tabindex', '-1');
      this.dialogoConTabindexTemporal = dialogo;
    }

    destino.focus({ preventScroll: true });
  }

  private limpiarTabindexTemporal(): void {
    if (this.dialogoConTabindexTemporal?.isConnected) {
      this.dialogoConTabindexTemporal.removeAttribute('tabindex');
    }
    this.dialogoConTabindexTemporal = null;
  }

  private obtenerElementosEnfocables(dialogo: HTMLElement): HTMLElement[] {
    if (typeof window === 'undefined') return [];

    const selector = [
      'a[href]:not([tabindex="-1"])',
      'button:not([disabled]):not([tabindex="-1"])',
      'input:not([disabled]):not([type="hidden"]):not([tabindex="-1"])',
      'select:not([disabled]):not([tabindex="-1"])',
      'textarea:not([disabled]):not([tabindex="-1"])',
      '[contenteditable="true"]:not([tabindex="-1"])',
      '[tabindex]:not([tabindex="-1"])'
    ].join(',');

    return Array.from(dialogo.querySelectorAll<HTMLElement>(selector)).filter(elemento => {
      if (elemento.inert || elemento.closest('[inert]')) return false;
      const estilo = window.getComputedStyle(elemento);
      return estilo.display !== 'none' && estilo.visibility !== 'hidden';
    });
  }

  private readonly mantenerFocoEnDialogo = (event: KeyboardEvent): void => {
    if (event.key !== 'Tab' || !this.dialogoActivo || typeof document === 'undefined') return;

    const dialogo = this.dialogoActivo;
    const elementosEnfocables = this.obtenerElementosEnfocables(dialogo);

    if (elementosEnfocables.length === 0) {
      event.preventDefault();
      this.enfocarDialogo(dialogo);
      return;
    }

    const primero = elementosEnfocables[0];
    const ultimo = elementosEnfocables.at(-1);
    if (!ultimo) return;
    const activo = document.activeElement;

    if (event.shiftKey) {
      if (activo === primero || !dialogo.contains(activo)) {
        event.preventDefault();
        ultimo.focus({ preventScroll: true });
      }
      return;
    }

    if (activo === ultimo || !dialogo.contains(activo)) {
      event.preventDefault();
      primero.focus({ preventScroll: true });
    }
  };

  /** Módulos que el usuario logueado tiene asignados */
  linksVisibles = computed(() => {
    const usr   = this.auth.usuario();
    const todos = this.todosLosModulos();
    if (!usr || todos.length === 0) return [];
    const ids = usr.modulosIds ?? [];
    return todos.filter(m => ids.includes(m.modId));
  });

  /** Agrupación por sección para el menú lateral */
  menuAgrupado = computed(() => {
    const links = this.linksVisibles();

    const getSeccion = (ruta: string, defaultSec: string): string => {
      const r = ruta.toLowerCase();
      if (r.includes('matrices-riesgos')) return 'Riesgos LA/FT';
      if (r.includes('tipo-listas') || r.includes('cargar-listas') || r.includes('coincidencias-patrono') || r.includes('coincidencias-empleado')) return 'Listas de Cautela';
      if (r.includes('monitoreo') || r.includes('listas')) return 'Monitoreo y Operación';
      if (r.includes('usuarios') || r.includes('roles') || r.includes('accesos')) return 'Seguridad y Accesos';
      if (r.includes('configuracion') || r.includes('configuració')) return 'Configuración del Sistema';
      return defaultSec || 'General';
    };

    const seccionesMap = new Map<string, Modulo[]>();

    links.forEach(item => {
      const secNombre = getSeccion(item.modRuta, item.modSeccion);
      if (!seccionesMap.has(secNombre)) {
        seccionesMap.set(secNombre, []);
      }
      seccionesMap.get(secNombre)!.push(item);
    });

    const ordenSecciones = ['Riesgos LA/FT', 'Monitoreo y Operación', 'Listas de Cautela', 'Seguridad y Accesos', 'Configuración del Sistema'];

    return Array.from(seccionesMap.keys())
      .sort((a, b) => {
        const idxA = ordenSecciones.indexOf(a);
        const idxB = ordenSecciones.indexOf(b);
        if (idxA !== -1 && idxB !== -1) return idxA - idxB;
        if (idxA !== -1) return -1;
        if (idxB !== -1) return 1;
        return a.localeCompare(b);
      })
      .map(nombre => ({
        nombre,
        items: seccionesMap.get(nombre) || []
      }));
  });
}
