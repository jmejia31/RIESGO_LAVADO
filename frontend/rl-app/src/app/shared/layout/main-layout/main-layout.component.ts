import { ChangeDetectionStrategy, Component, OnInit, signal, computed } from '@angular/core';
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
export class MainLayoutComponent implements OnInit {
  sidebarAbierto = signal(true);

  /** Lista completa de módulos activos cargados desde la API */
  todosLosModulos = signal<Modulo[]>([]);

  constructor(
    readonly auth: AuthService,
    readonly configService: ConfiguracionService,
    readonly globalState: GlobalHttpStateService,
    private catalogoService: CatalogoService
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
