import { TestBed } from '@angular/core/testing';
import { App } from './app';

describe('App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });

  it('should render the application router outlet', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('router-outlet')).not.toBeNull();
  });

  it('bloquea Escape globalmente mientras existe un dialog abierto', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    const dialog = document.createElement('dialog');
    dialog.setAttribute('open', '');
    document.body.appendChild(dialog);
    const escape = new KeyboardEvent('keydown', { key: 'Escape', bubbles: true, cancelable: true });

    dialog.dispatchEvent(escape);

    expect(escape.defaultPrevented).toBe(true);
    dialog.remove();
  });

  it('bloquea el evento cancel nativo del dialog para impedir cierre con Escape', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    const dialog = document.createElement('dialog');
    dialog.setAttribute('open', '');
    document.body.appendChild(dialog);
    const cancel = new Event('cancel', { bubbles: false, cancelable: true });

    dialog.dispatchEvent(cancel);

    expect(cancel.defaultPrevented).toBe(true);
    dialog.remove();
  });
});
