import { Inject, Injectable } from '@angular/core';
import { SweetAlertIcon, SweetAlertOptions } from 'sweetalert2';
import {
  INSTITUTIONAL_CONFIRMATION_DIALOG,
  InstitutionalConfirmationDialog
} from '../confirmation/institutional-confirmation.service';
import { ActionIconName, renderActionIconSvg } from '../components/action-icon/action-icon.component';

export interface InstitutionalNotificationOptions {
  title: string;
  message: string;
  icon: SweetAlertIcon;
  confirmLabel?: string;
  confirmIcon?: ActionIconName;
  confirmVariant?: 'primary' | 'danger' | 'success';
}

export interface InstitutionalHtmlDialogOptions {
  title: string;
  html: string;
  confirmLabel?: string;
  size?: 'md' | 'lg';
}

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
export class InstitutionalNotificationService {
  constructor(@Inject(INSTITUTIONAL_CONFIRMATION_DIALOG) private readonly dialog: InstitutionalConfirmationDialog) {}

  async notify(options: InstitutionalNotificationOptions): Promise<void> {
    const confirmLabel = options.confirmLabel ?? 'Aceptar';
    const confirmIcon = options.confirmIcon ?? 'confirm';
    const confirmVariant = options.confirmVariant ?? 'primary';
    const encabezado = options.icon === 'error' ? 'Error institucional' : 'Notificación institucional';
    const dialogOptions: SweetAlertOptions = {
      title: `<span class="modal-header-icon" aria-hidden="true">${renderActionIconSvg(confirmIcon)}</span><span class="institutional-confirmation__copy"><span data-modal-eyebrow>${encabezado}</span><span class="institutional-confirmation__heading">${escapeHtml(options.title)}</span><span class="institutional-confirmation__message">${escapeHtml(options.message)}</span></span>`,
      showConfirmButton: true,
      showCancelButton: false,
      showDenyButton: false,
      showCloseButton: false,
      buttonsStyling: false,
      allowOutsideClick: false,
      allowEscapeKey: true,
      returnFocus: true,
      confirmButtonText: renderActionIconSvg(confirmIcon),
      confirmButtonAriaLabel: confirmLabel,
      customClass: {
        container: 'modal-backdrop-overlay institutional-confirmation-overlay',
        popup: 'modal-container-card modal-size-md institutional-confirmation',
        title: 'modal-header-institutional institutional-confirmation__header',
        actions: 'modal-footer-institutional institutional-confirmation__footer',
        confirmButton: `modal-action-icon modal-action-icon--${confirmVariant}`
      },
      didOpen: popup => {
        popup.querySelector('.swal2-actions')?.classList.remove('swal2-loading');
        const confirmButton = popup.querySelector<HTMLButtonElement>('.swal2-confirm');
        if (confirmButton) {
          confirmButton.type = 'button';
          confirmButton.disabled = false;
          confirmButton.title = confirmLabel;
        }
      }
    };

    await this.dialog.fire(dialogOptions);
  }

  async showLoading(title: string, message: string): Promise<void> {
    const dialogOptions: SweetAlertOptions = {
      title: `<span class="modal-header-icon institutional-loading-icon" aria-hidden="true">${renderActionIconSvg('refresh')}</span><span class="institutional-confirmation__copy"><span data-modal-eyebrow>Operación en curso</span><span class="institutional-confirmation__heading">${escapeHtml(title)}</span><span class="institutional-confirmation__message">${escapeHtml(message)}</span></span>`,
      showConfirmButton: false,
      showCancelButton: false,
      showDenyButton: false,
      showCloseButton: false,
      buttonsStyling: false,
      allowOutsideClick: false,
      allowEscapeKey: false,
      returnFocus: true,
      customClass: {
        container: 'modal-backdrop-overlay institutional-confirmation-overlay',
        popup: 'modal-container-card modal-size-md institutional-confirmation',
        title: 'modal-header-institutional institutional-confirmation__header',
        actions: 'modal-footer-institutional institutional-confirmation__footer'
      }
    };

    await this.dialog.fire(dialogOptions);
  }

  closeLoading(): void {
    this.dialog.close?.();
  }

  async showHtml(options: InstitutionalHtmlDialogOptions): Promise<void> {
    const confirmLabel = options.confirmLabel ?? 'Cerrar';
    const size = options.size ?? 'lg';
    await this.dialog.fire({
      title: `<span class="modal-header-icon" aria-hidden="true">${renderActionIconSvg('view')}</span><span class="institutional-confirmation__copy"><span data-modal-eyebrow>Información institucional</span><span class="institutional-confirmation__heading">${escapeHtml(options.title)}</span></span>`,
      html: options.html,
      showConfirmButton: true,
      showCancelButton: false,
      showDenyButton: false,
      showCloseButton: false,
      buttonsStyling: false,
      allowOutsideClick: false,
      allowEscapeKey: true,
      returnFocus: true,
      confirmButtonText: renderActionIconSvg('close'),
      confirmButtonAriaLabel: confirmLabel,
      customClass: {
        container: 'modal-backdrop-overlay institutional-confirmation-overlay',
        popup: `modal-container-card modal-size-${size} institutional-confirmation`,
        title: 'modal-header-institutional institutional-confirmation__header',
        actions: 'modal-footer-institutional institutional-confirmation__footer',
        confirmButton: 'modal-action-icon modal-action-icon--neutral'
      },
      didOpen: popup => {
        popup.querySelector('.swal2-actions')?.classList.remove('swal2-loading');
        const confirmButton = popup.querySelector<HTMLButtonElement>('.swal2-confirm');
        if (confirmButton) {
          confirmButton.type = 'button';
          confirmButton.disabled = false;
          confirmButton.title = confirmLabel;
        }
      }
    });
  }
}
