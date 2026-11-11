import { ComponentFixture, TestBed } from '@angular/core/testing';
import { SortableTableHeaderComponent } from './sortable-table-header.component';

describe('SortableTableHeaderComponent', () => {
  let fixture: ComponentFixture<SortableTableHeaderComponent>;
  let component: SortableTableHeaderComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [SortableTableHeaderComponent] }).compileComponents();
    fixture = TestBed.createComponent(SortableTableHeaderComponent);
    component = fixture.componentInstance;
    component.column = 'id';
    component.label = 'ID';
    component.ariaLabel = 'Ordenar por ID';
    fixture.detectChanges();
  });

  it('muestra ambas flechas y emite la columna al hacer clic', () => {
    const emitidos: string[] = [];
    component.sortChange.subscribe(value => emitidos.push(value));
    const button = fixture.nativeElement.querySelector('button') as HTMLButtonElement;
    expect(button.querySelectorAll('svg')).toHaveLength(1);
    expect(button.querySelectorAll('path')).toHaveLength(2);
    button.click();
    expect(emitidos).toEqual(['id']);
  });

  it('expone el estado activo y la dirección con un nombre accesible', () => {
    fixture.componentRef.setInput('activeColumn', 'id');
    fixture.componentRef.setInput('direction', 'desc');
    fixture.detectChanges();
    const button = fixture.nativeElement.querySelector('button') as HTMLButtonElement;
    expect(button.getAttribute('aria-label')).toBe('Ordenar por ID');
    expect(button.getAttribute('aria-pressed')).toBe('true');
    expect(button.getAttribute('aria-sort')).toBe('descending');
    expect(button.getAttribute('aria-sort')).toBe('descending');
    expect(fixture.nativeElement.querySelectorAll('path.text-ihss-700')).toHaveLength(1);
  });

  it('no permite activar una columna deshabilitada', () => {
    fixture.componentRef.setInput('disabled', true);
    fixture.detectChanges();
    const button = fixture.nativeElement.querySelector('button') as HTMLButtonElement;
    expect(button.disabled).toBe(true);
  });
});
