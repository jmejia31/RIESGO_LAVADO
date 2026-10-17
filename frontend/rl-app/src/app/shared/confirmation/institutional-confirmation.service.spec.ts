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

  it('mantiene la política de Escape explícita sólo cuando la opción lo solicita', async () => {
    await service.confirm({
      title: 'Desactivar familia',
      message: 'Se conservará el historial.',
      confirmLabel: 'Desactivar',
      cancelLabel: 'Cancelar',
      confirmIcon: 'deactivate',
      allowEscapeKey: false
    });

    expect(dialog.fire).toHaveBeenCalledWith(expect.objectContaining({
      allowEscapeKey: false,
      allowOutsideClick: false,
      focusCancel: true
    }));
  });

  it('devuelve el motivo recortado sólo tras confirmar y valida dentro del adaptador', async () => {
    dialog.fire.mockResolvedValue({ isConfirmed: true, value: '  Motivo aprobado  ' });
    await expect(service.confirmWithInput({
      title: 'Eliminar evidencia',
      message: 'La evidencia se eliminará lógicamente.',
      confirmLabel: 'Sí, eliminar',
      cancelLabel: 'Cancelar',
      confirmIcon: 'delete',
      confirmVariant: 'danger',
      inputLabel: 'Motivo de eliminación',
      inputPlaceholder: 'Escriba el motivo',
      inputMaxLength: 1000,
      inputValidator: value => value.trim() ? null : 'Debe ingresar un motivo.'
    })).resolves.toBe('Motivo aprobado');

    expect(dialog.fire).toHaveBeenCalledWith(expect.objectContaining({
      input: 'textarea',
      inputLabel: 'Motivo de eliminación',
      inputAttributes: expect.objectContaining({ maxlength: '1000', 'aria-label': 'Motivo de eliminación' }),
      confirmButtonAriaLabel: 'Sí, eliminar'
    }));
  });

  it('no entrega valor cuando se cancela una confirmación con entrada', async () => {
    dialog.fire.mockResolvedValue({ isConfirmed: false, value: 'No debe continuar' });
    await expect(service.confirmWithInput({
      title: 'Eliminar',
      message: 'Motivo requerido.',
      confirmLabel: 'Eliminar',
      cancelLabel: 'Cancelar',
      confirmIcon: 'delete',
      inputLabel: 'Motivo',
      inputPlaceholder: 'Escriba el motivo',
      inputMaxLength: 1000,
      inputValidator: () => null
    })).resolves.toBeNull();
  });
});
