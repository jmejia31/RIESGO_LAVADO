import { routes } from './app.routes';

describe('Application routes', () => {
  it('keeps every routed screen lazy-loaded', () => {
    const layout = routes.find(route => route.path === '');
    const routedScreens = [
      ...routes.filter(route => route.path !== '' && route.path !== '**'),
      ...(layout?.children ?? []).filter(route => route.path !== '')
    ];

    expect(layout?.loadComponent).toEqual(expect.any(Function));
    expect(routedScreens.length).toBe(12);
    expect(routedScreens.every(route => typeof route.loadComponent === 'function')).toBe(true);
    expect(routedScreens.every(route => route.component === undefined)).toBe(true);
  });

  it('preserves protected module paths and guards', () => {
    const layout = routes.find(route => route.path === '');
    const protectedPaths = [
      'usuarios',
      'configuracion',
      'monitoreo-listas',
      'bitacora',
      'tipo-listas',
      'cargar-listas',
      'coincidencias-patrono',
      'coincidencias-empleado',
      'matrices-riesgos'
    ];

    const protectedRoutes = (layout?.children ?? []).filter(route => protectedPaths.includes(route.path ?? ''));
    expect(protectedRoutes.map(route => route.path)).toEqual(protectedPaths);
    expect(protectedRoutes.every(route => route.canActivate?.length === 1)).toBe(true);
  });
});
