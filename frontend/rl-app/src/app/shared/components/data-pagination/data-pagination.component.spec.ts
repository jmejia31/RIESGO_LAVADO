import { describe, expect, it } from 'vitest';
import { DataPaginationComponent } from './data-pagination.component';

describe('DataPaginationComponent', () => {
  const create = (page: number, pageSize: number, totalRecords: number) => {
    const component = new DataPaginationComponent();
    component.page = page;
    component.pageSize = pageSize;
    component.totalRecords = totalRecords;
    return component;
  };

  it('calcula un rango vacío sin producir 1 a 0', () => {
    const component = create(1, 10, 0);
    expect(component.startRecord).toBe(0);
    expect(component.endRecord).toBe(0);
    expect(component.visibleItems).toEqual([]);
  });

  it('mantiene la primera y última página con una ventana compacta', () => {
    const component = create(50, 10, 1310);
    expect(component.totalPages).toBe(131);
    expect(component.visibleItems).toEqual([
      { type: 'page', page: 1 },
      { type: 'ellipsis', key: '1-48' },
      { type: 'page', page: 48 },
      { type: 'page', page: 49 },
      { type: 'page', page: 50 },
      { type: 'page', page: 51 },
      { type: 'page', page: 52 },
      { type: 'ellipsis', key: '52-131' },
      { type: 'page', page: 131 }
    ]);
  });

  it('no renderiza más de siete botones numéricos', () => {
    const component = create(1, 10, 100000);
    expect(component.visibleItems.filter(item => item.type === 'page')).toHaveLength(4);
  });

  it('conserva la primera y última página al inicio y al final del conjunto', () => {
    const first = create(1, 10, 1310);
    const last = create(131, 10, 1310);

    expect(first.visibleItems.filter(item => item.type === 'page').map(item => item.page)).toEqual([1, 2, 3, 131]);
    expect(last.visibleItems.filter(item => item.type === 'page').map(item => item.page)).toEqual([1, 129, 130, 131]);
  });

  it('mantiene la ventana alrededor de la página intermedia', () => {
    const component = create(50, 10, 1310);
    expect(component.visibleItems.filter(item => item.type === 'page').map(item => item.page)).toEqual([1, 48, 49, 50, 51, 52, 131]);
  });

  it('calcula correctamente el resumen de un único registro', () => {
    const component = create(1, 10, 1);
    component.itemLabel = 'familias';
    expect(component.startRecord).toBe(1);
    expect(component.endRecord).toBe(1);
    expect(component.totalPages).toBe(1);
  });
});
