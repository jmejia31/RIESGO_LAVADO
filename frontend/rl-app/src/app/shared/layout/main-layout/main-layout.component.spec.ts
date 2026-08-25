import '@angular/compiler';
import { ElementRef, signal } from '@angular/core';
import { Subject, of } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';
import { MainLayoutComponent } from './main-layout.component';

describe('MainLayoutComponent', () => {
  function crearComponente(markup = '<main id="contenido-principal"><button id="fuera">Fuera</button></main>') {
    const host = document.createElement('div');
    host.innerHTML = markup;
    document.body.appendChild(host);

    const usuario = signal<{ modulosIds: number[] } | null>(null);
    const auth = { usuario };
    const config = { CargarConfiguracion: vi.fn(() => of(undefined)) };
    const modulos = new Subject<Array<{ modId: number; modRuta: string; modSeccion: string; modNombre: string }>>();
    const catalogo = { modulos: vi.fn(() => modulos.asObservable()) };
    const globalState = {};
    const component = new MainLayoutComponent(
      auth as never,
      config as never,
      globalState as never,
      catalogo as never,
      new ElementRef(host)
    );

    return { component, host, usuario, config, catalogo, modulos };
  }

  it('carga módulos, filtra por permisos y agrupa el menú en orden institucional', () => {
    const { component, usuario, modulos, config } = crearComponente();
    component.ngOnInit();
    usuario.set({ modulosIds: [1, 2, 3, 4, 5, 6] });
    modulos.next([
      { modId: 1, modRuta: '/matrices-riesgos', modSeccion: '', modNombre: 'Matrices' },
      { modId: 2, modRuta: '/usuarios', modSeccion: '', modNombre: 'Usuarios' },
      { modId: 3, modRuta: '/tipo-listas', modSeccion: '', modNombre: 'Tipos' },
      { modId: 4, modRuta: '/monitoreo', modSeccion: '', modNombre: 'Monitoreo' },
      { modId: 5, modRuta: '/configuracion', modSeccion: '', modNombre: 'Configuración' },
      { modId: 6, modRuta: '/otro', modSeccion: 'Personalizado', modNombre: 'Otro' },
      { modId: 99, modRuta: '/oculto', modSeccion: 'Oculto', modNombre: 'Oculto' }
    ]);

    expect(config.CargarConfiguracion).toHaveBeenCalledTimes(1);
    expect(component.linksVisibles().map(item => item.modId)).toEqual([1, 2, 3, 4, 5, 6]);
    expect(component.menuAgrupado().map(item => item.nombre)).toEqual([
      'Riesgos LA/FT',
      'Monitoreo y Operación',
      'Listas de Cautela',
      'Seguridad y Accesos',
      'Configuración del Sistema',
      'Personalizado'
    ]);
  });

  it('deja el menú vacío cuando no existe usuario y maneja error de módulos', () => {
    const { component, modulos } = crearComponente();
    component.ngOnInit();
    modulos.error(new Error('fallo de catálogo'));

    expect(component.todosLosModulos()).toEqual([]);
    expect(component.linksVisibles()).toEqual([]);
    expect(component.menuAgrupado()).toEqual([]);
  });

  it('cierra sidebar solo en viewport móvil y alterna su estado', () => {
    const { component } = crearComponente();
    const originalWidth = window.innerWidth;
    Object.defineProperty(window, 'innerWidth', { configurable: true, value: 500 });

    component.ngOnInit();
    expect(component.sidebarAbierto()).toBe(false);
    component.toggleSidebar();
    expect(component.sidebarAbierto()).toBe(true);
    component.cerrarSidebarMovil();
    expect(component.sidebarAbierto()).toBe(false);

    Object.defineProperty(window, 'innerWidth', { configurable: true, value: originalWidth });
  });

  it('bloquea el fondo, contiene Tab y restaura foco al cerrar el diálogo', async () => {
    const { component, host } = crearComponente('<button id="fuera">Fuera</button><dialog open aria-modal="true"><button id="primero">Primero</button><button id="ultimo">Último</button></dialog>');
    const fuera = host.children[0] as HTMLElement;
    const dialogo = host.children[1] as HTMLElement;
    fuera.focus();

    component.ngAfterViewInit();
    expect(document.body.classList.contains('modal-abierto')).toBe(true);
    expect(fuera.inert).toBe(true);
    expect(document.activeElement?.id).toBe('primero');

    const ultimo = dialogo.querySelector('#ultimo') as HTMLElement;
    ultimo.focus();
    const tab = new KeyboardEvent('keydown', { key: 'Tab', cancelable: true });
    document.dispatchEvent(tab);
    expect(tab.defaultPrevented).toBe(true);
    expect(document.activeElement?.id).toBe('primero');

    dialogo.remove();
    component['sincronizarBloqueoModal']();
    await Promise.resolve();
    expect(document.body.classList.contains('modal-abierto')).toBe(false);
    expect(fuera.inert).not.toBe(true);
    expect(document.activeElement?.id).toBe('fuera');

    component.ngOnDestroy();
    host.remove();
  });
});
