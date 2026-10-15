import { InstitutionalConfirmationDialog, InstitutionalConfirmationService } from './institutional-confirmation.service';

describe('InstitutionalConfirmationService', () => {
  let dialog: InstitutionalConfirmationDialog & { fire: ReturnType<typeof vi.fn> };
  let service: InstitutionalConfirmationService;

  beforeEach(() => {
    dialog = { fire: vi.fn().mockResolvedValue({ isConfirmed: false }) };
    service = new InstitutionalConfirmationService(dialog);
  });

  it('presenta una confirmación medium, institucional, icon-only y con nombres accesibles', async () => {
    await service.confirm({
      title: 'Hay cambios sin guardar',
      message: '¿Desea descartarlos?',
      confirmLabel: 'Descartar cambios',
      cancelLabel: 'Continuar editando',
      confirmIcon: 'reject',
      confirmVariant: 'danger',
      icon: 'warning'
    });

    expect(dialog.fire).toHaveBeenCalledWith(expect.objectContaining({
      showCancelButton: true,
      showDenyButton: false,
      allowOutsideClick: false,
      focusCancel: true,
      buttonsStyling: false,
      confirmButtonAriaLabel: 'Descartar cambios',
      cancelButtonAriaLabel: 'Continuar editando',
      customClass: expect.objectContaining({
        container: expect.stringContaining('modal-backdrop-overlay'),
        popup: expect.stringContaining('modal-size-md'),
        title: expect.stringContaining('modal-header-institutional'),
        actions: expect.stringContaining('modal-footer-institutional'),
        confirmButton: expect.stringContaining('modal-action-icon'),
        cancelButton: expect.stringContaining('modal-action-icon')
      })
    }));
  });

  it('devuelve true sólo cuando se confirma la acción', async () => {
    dialog.fire.mockResolvedValue({ isConfirmed: true });
    await expect(service.confirm({
      title: 'Confirmar',
      message: 'Mensaje',
      confirmLabel: 'Descartar cambios',
      cancelLabel: 'Continuar editando',
      confirmIcon: 'reject'
    })).resolves.toBe(true);
  });
});
