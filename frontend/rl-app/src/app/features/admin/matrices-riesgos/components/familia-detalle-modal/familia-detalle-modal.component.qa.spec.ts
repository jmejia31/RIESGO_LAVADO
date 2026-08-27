import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { AuthService } from '../../../../../core/auth/auth.service';
import { AuditoriaService } from '../../../bitacora/data-access/auditoria.service';
import { MatricesRiesgosService } from '../../data-access/matrices-riesgos.service';
import { FamiliaFormularioDto } from '../../models/matrices-riesgos.models';
import { FamiliaDetalleModalComponent } from './familia-detalle-modal.component';

const familiaQa: FamiliaFormularioDto = {
  famId: 41,
  famCodigo: 'FAM_QA_RESPONSIVE',
  famNombre: 'Familia QA Responsive',
  famDescripcion: 'Familia para certificación transversal UI-FAM.QA.',
  famActivo: true,
  famFechaCreacion: '2026-08-23T00:00:00Z',
  totalVersiones: 0,
  tieneVersionVigente: false
};

describe('FamiliaDetalleModalComponent — UI-FAM.QA transversal', () => {
  it('mantiene contrato responsive desktop/reducido y cierre por teclado', async () => {
    const service = {
      obtenerFamiliaFormularioPorId: vi.fn(() => of(familiaQa)),
      listarHistorialVersionesFormulario: vi.fn(() => of([])),
      activarFamiliaFormulario: vi.fn(() => of(true)),
      desactivarFamiliaFormulario: vi.fn(() => of(true)),
      clonarVersionFormulario: vi.fn(() => of(1))
    };

    await TestBed.configureTestingModule({
      imports: [FamiliaDetalleModalComponent],
      providers: [
        { provide: MatricesRiesgosService, useValue: service },
        { provide: AuthService, useValue: { tieneRol: () => true } },
        { provide: AuditoriaService, useValue: { getBitacora: () => of({ datos: [], totalRegistros: 0 }) } }
      ]
    }).compileComponents();

    const fixture = TestBed.createComponent(FamiliaDetalleModalComponent);
    fixture.componentRef.setInput('familiaId', 41);
    fixture.componentRef.setInput('familiaReferencia', familiaQa);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const dialog = fixture.nativeElement.querySelector('[data-ui-fam-detail="modal"]') as HTMLElement | null;
    const card = dialog?.querySelector('.modal-container-card') as HTMLElement | null;
    const cerrarSpy = vi.spyOn(fixture.componentInstance.cerrar, 'emit');

    expect(card?.classList.contains('modal-container-card')).toBe(true);
    expect(card?.classList.contains('modal-size-workspace')).toBe(true);
    expect(dialog?.querySelector('.overflow-y-auto')).not.toBeNull();

    dialog?.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true, cancelable: true }));
    expect(cerrarSpy).not.toHaveBeenCalled();
  });
});
