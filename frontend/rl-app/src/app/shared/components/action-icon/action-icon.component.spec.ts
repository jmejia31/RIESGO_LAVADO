import { describe, expect, it } from 'vitest';
import { ACTION_ICON_PATHS, ActionIconName } from './action-icon.component';

describe('catálogo global de iconos de acción', () => {
  it('define una geometría no vacía para cada acción registrada', () => {
    const acciones = Object.keys(ACTION_ICON_PATHS) as ActionIconName[];
    expect(acciones.length).toBeGreaterThan(0);
    for (const accion of acciones) {
      expect(ACTION_ICON_PATHS[accion].length, accion).toBeGreaterThan(0);
      expect(ACTION_ICON_PATHS[accion].every(path => path.trim().length > 0), accion).toBe(true);
    }
  });

  it('mantiene edit y view en la geometría canónica de Matrices', () => {
    expect(ACTION_ICON_PATHS.edit).toEqual([
      'M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z'
    ]);
    expect(ACTION_ICON_PATHS.view).toEqual([
      'M15 12a3 3 0 11-6 0 3 3 0 016 0z',
      'M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z'
    ]);
  });
});
