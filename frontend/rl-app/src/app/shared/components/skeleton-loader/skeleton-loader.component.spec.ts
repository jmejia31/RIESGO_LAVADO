import { ComponentFixture, TestBed } from '@angular/core/testing';
import { SkeletonLoaderComponent } from './skeleton-loader.component';

describe('SkeletonLoaderComponent', () => {
  let fixture: ComponentFixture<SkeletonLoaderComponent>;
  let component: SkeletonLoaderComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SkeletonLoaderComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(SkeletonLoaderComponent);
    component = fixture.componentInstance;
  });

  it('expone un unico estado accesible y oculta la geometria decorativa', () => {
    component.label = 'Cargando auditoría';
    fixture.detectChanges();

    const host = fixture.nativeElement as HTMLElement;
    const status = host.querySelector('[role="status"]');
    const visual = status?.querySelector('[aria-hidden="true"]');

    expect(status?.getAttribute('aria-live')).toBe('polite');
    expect(status?.getAttribute('aria-busy')).toBe('true');
    expect(status?.getAttribute('aria-label')).toBe('Cargando auditoría');
    expect(visual).not.toBeNull();
  });

  it('renderiza la cantidad solicitada de filas de tabla', () => {
    component.variant = 'table';
    component.rows = 3;
    fixture.detectChanges();

    const rows = (fixture.nativeElement as HTMLElement).querySelectorAll('[data-skeleton-row]');
    expect(rows.length).toBe(3);
  });

  it('limita filas a un rango seguro', () => {
    component.rows = 99;
    expect(component.filasArray.length).toBe(12);

    component.rows = 0;
    expect(component.filasArray.length).toBe(1);
  });
});
