import { InstitutionalConfirmationDialog, INSTITUTIONAL_CONFIRMATION_DIALOG } from '../confirmation/institutional-confirmation.service';
import { InstitutionalNotificationService } from './institutional-notification.service';

describe('InstitutionalNotificationService', () => {
  let dialog: InstitutionalConfirmationDialog & { fire: ReturnType<typeof vi.fn> };
  let service: InstitutionalNotificationService;

  beforeEach(() => {
    dialog = { fire: vi.fn().mockResolvedValue({ isConfirmed: true }) };
    service = new InstitutionalNotificationService(dialog);
  });

  it('usa el shell institucional centrado y no el modo toast', async () => {
    await service.notify({
      title: 'Riesgo actualizado',
      message: 'El riesgo R-001 fue actualizado correctamente.',
      icon: 'success',
      confirmIcon: 'confirm',
      confirmLabel: 'Aceptar',
      confirmVariant: 'success'
    });

    expect(dialog.fire).toHaveBeenCalledWith(expect.objectContaining({
      showCancelButton: false,
      showConfirmButton: true,
      customClass: expect.objectContaining({
        container: expect.stringContaining('institutional-confirmation-overlay'),
        popup: expect.stringContaining('modal-size-md'),
        title: expect.stringContaining('modal-header-institutional'),
        actions: expect.stringContaining('modal-footer-institutional')
      })
    }));
    const options = dialog.fire.mock.calls[0][0] as { title: string };
    expect(options.title).toContain('Notificación institucional');
    expect(options).not.toHaveProperty('toast');
    expect(options.title).not.toContain('toast');
  });

  it('escapa texto dinámico antes de incrustarlo en el encabezado institucional', async () => {
    await service.notify({
      title: '<script>alert(1)</script>',
      message: 'Mensaje con <marcado>',
      icon: 'error'
    });

    const options = dialog.fire.mock.calls[0][0] as { title: string };
    expect(options.title).toContain('&lt;script&gt;alert(1)&lt;/script&gt;');
    expect(options.title).toContain('Mensaje con &lt;marcado&gt;');
    expect(options.title).not.toContain('<script>');
  });

  it('expone el token institucional como transporte compartido', () => {
    expect(INSTITUTIONAL_CONFIRMATION_DIALOG).toBeDefined();
  });
});
