import { ComponentFixture, TestBed } from '@angular/core/testing';
import { PageSizeSelectorComponent } from './page-size-selector.component';

describe('PageSizeSelectorComponent', () => {
  let fixture: ComponentFixture<PageSizeSelectorComponent>;
  let component: PageSizeSelectorComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [PageSizeSelectorComponent] }).compileComponents();
    fixture = TestBed.createComponent(PageSizeSelectorComponent);
    component = fixture.componentInstance;
    component.options = [10, 25, 50];
  });

  it('refleja siempre el valor autoritativo recibido, incluido el render inicial', () => {
    component.value = 25;
    fixture.detectChanges();
    const select = fixture.nativeElement.querySelector('select') as HTMLSelectElement;
    expect(select.value).toBe('25');

    component.value = 10;
    fixture.detectChanges();
    expect(select.value).toBe('10');
  });

  it('emite el tamaño seleccionado como número', () => {
    component.value = 10;
    fixture.detectChanges();
    const emitidos: number[] = [];
    component.valueChange.subscribe(value => emitidos.push(value));

    const select = fixture.nativeElement.querySelector('select') as HTMLSelectElement;
    select.value = '50';
    select.dispatchEvent(new Event('change'));
    fixture.detectChanges();

    expect(emitidos).toEqual([50]);
  });
});
