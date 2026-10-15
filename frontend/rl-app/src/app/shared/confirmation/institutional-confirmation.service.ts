import { Inject, Injectable, InjectionToken } from '@angular/core';
import Swal, { SweetAlertIcon, SweetAlertOptions, SweetAlertResult } from 'sweetalert2';
import { ActionIconName, renderActionIconSvg } from '../components/action-icon/action-icon.component';

export interface InstitutionalConfirmationOptions {
  title: string;
  message: string;
  confirmLabel: string;
  cancelLabel: string;
  confirmIcon: ActionIconName;
  confirmVariant?: 'primary' | 'danger' | 'success';
  icon?: SweetAlertIcon;
}

export interface InstitutionalConfirmationDialog {
  fire(options: SweetAlertOptions): Promise<SweetAlertResult>;
}

export const INSTITUTIONAL_CONFIRMATION_DIALOG = new InjectionToken<InstitutionalConfirmationDialog>(
  'INSTITUTIONAL_CONFIRMATION_DIALOG',
  { providedIn: 'root', factory: () => ({ fire: options => Swal.fire(options) }) }
);

function escapeHtml(value: string): string {
  return value.replace(/[&<>"']/g, character => ({
    '&': '&amp;',
    '<': '&lt;',
    '>': '&gt;',
    '"': '&quot;',
    "'": '&#39;'
  })[character] ?? character);
}

@Injectable({ providedIn: 'root' })
export class InstitutionalConfirmationService {
  constructor(@Inject(INSTITUTIONAL_CONFIRMATION_DIALOG) private readonly dialog: InstitutionalConfirmationDialog) {}

  async confirm(options: InstitutionalConfirmationOptions): Promise<boolean> {
    const confirmVariant = options.confirmVariant ?? 'primary';
    const encabezado = options.icon === 'warning' ? 'Confirmación requerida' : 'Confirmación institucional';
    const result = await this.dialog.fire({
      title: `<span class="modal-header-icon" aria-hidden="true">${renderActionIconSvg(options.confirmIcon)}</span><span class="institutional-confirmation__copy"><span data-modal-eyebrow>${encabezado}</span><span class="institutional-confirmation__heading">${escapeHtml(options.title)}</span><span class="institutional-confirmation__message">${escapeHtml(options.message)}</span></span>`,
      showCancelButton: true,
      showDenyButton: false,
      showCloseButton: false,
      buttonsStyling: false,
      allowOutsideClick: false,
      allowEscapeKey: true,
      focusCancel: true,
      reverseButtons: true,
      confirmButtonText: renderActionIconSvg(options.confirmIcon),
      cancelButtonText: renderActionIconSvg('cancel'),
      confirmButtonAriaLabel: options.confirmLabel,
      cancelButtonAriaLabel: options.cancelLabel,
      customClass: {
        container: 'modal-backdrop-overlay institutional-confirmation-overlay',
        popup: 'modal-container-card modal-size-md institutional-confirmation',
        title: 'modal-header-institutional institutional-confirmation__header',
        actions: 'modal-footer-institutional institutional-confirmation__footer',
        confirmButton: `modal-action-icon modal-action-icon--${confirmVariant}`,
        cancelButton: 'modal-action-icon modal-action-icon--neutral'
      },
      didOpen: popup => {
        const confirmButton = popup.querySelector<HTMLButtonElement>('.swal2-confirm');
        const cancelButton = popup.querySelector<HTMLButtonElement>('.swal2-cancel');
        if (confirmButton) {
          confirmButton.type = 'button';
          confirmButton.title = options.confirmLabel;
        }
        if (cancelButton) {
          cancelButton.type = 'button';
          cancelButton.title = options.cancelLabel;
        }
      }
    });

    return result.isConfirmed;
  }
}
