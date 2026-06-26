import { ChangeDetectionStrategy, Component, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { ConfiguracionService } from '../../../core/services/configuracion.service';
import { CatalogoService } from '../../../core/services/catalogo.service';
import { Modulo } from '../../../core/models/catalogo.models';

@Component({
  selector: 'app-main-layout',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './main-layout.component.html',
  changeDetection: ChangeDetectionStrategy.Eager
})
export class MainLayoutComponent implements OnInit {
  sidebarAbierto = signal(true);

  /** Lista completa de módulos activos cargados desde la API */
  todosLosModulos = signal<Modulo[]>([]);

  constructor(
    readonly auth: AuthService,
    readonly configService: ConfiguracionService,
    private catalogoService: CatalogoService
  ) {}

  ngOnInit() {
    this.configService.CargarConfiguracion().subscribe();
    this.catalogoService.modulos().subscribe({
      next:  mods => this.todosLosModulos.set(mods),
      error: ()   => this.todosLosModulos.set([])
    });
  }

  toggleSidebar() {
    this.sidebarAbierto.update(v => !v);
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
    
    // Mapeo local de rutas a secciones recomendadas para asegurar orden y limpieza visual
    const getSeccion = (ruta: string, defaultSec: string): string => {
      const r = ruta.toLowerCase();
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

    // Definir un orden de prioridad para las secciones en el menú
    const ordenSecciones = ['Monitoreo y Operación', 'Listas de Cautela', 'Seguridad y Accesos', 'Configuración del Sistema'];

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
