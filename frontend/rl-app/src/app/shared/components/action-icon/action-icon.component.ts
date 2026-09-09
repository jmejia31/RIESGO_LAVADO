import { ChangeDetectionStrategy, Component, Input } from '@angular/core';

export type ActionIconName =
  | 'activate'
  | 'approve'
  | 'back'
  | 'cancel'
  | 'clear-filters'
  | 'close'
  | 'collapse'
  | 'configure'
  | 'confirm'
  | 'copy'
  | 'create'
  | 'deactivate'
  | 'delete'
  | 'download'
  | 'duplicate'
  | 'edit'
  | 'expand'
  | 'export-excel'
  | 'export-pdf'
  | 'filter'
  | 'follow-up'
  | 'history'
  | 'lock'
  | 'login'
  | 'logout'
  | 'mark-false-positive'
  | 'mark-positive'
  | 'menu'
  | 'more-actions'
  | 'move-down'
  | 'move-up'
  | 'next'
  | 'pause'
  | 'play'
  | 'preview'
  | 'previous'
  | 'print'
  | 'publish'
  | 'refresh'
  | 'reject'
  | 'reset-password'
  | 'resume'
  | 'retry'
  | 'save'
  | 'search'
  | 'show-password'
  | 'sync'
  | 'hide-password'
  | 'unlock'
  | 'upload'
  | 'validate'
  | 'versions'
  | 'view';

/** Registro único de geometrías para acciones del sistema. */
export const ACTION_ICON_PATHS: Readonly<Record<ActionIconName, readonly string[]>> = {
  activate: ['M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z'],
  approve: ['M5 12l4 4L19 6'],
  back: ['M15 19l-7-7 7-7'],
  cancel: ['M6 18 18 6M6 6l12 12'],
  'clear-filters': ['M3 4h18M6 8h12M9 12h6m-3 0v8'],
  close: ['M6 18 18 6M6 6l12 12'],
  collapse: ['m6 9 6 6 6-6'],
  configure: ['M4 6h16M4 12h16M4 18h16', 'M8 4v4M16 10v4M10 16v4'],
  confirm: ['M5 12l4 4L19 6'],
  copy: ['M16 8V6a2 2 0 0 0-2-2H6a2 2 0 0 0-2 2v8a2 2 0 0 0 2 2h2', 'M10 8h8a2 2 0 0 1 2 2v8a2 2 0 0 1-2 2h-8a2 2 0 0 1-2-2v-8a2 2 0 0 1 2-2z'],
  create: ['M12 4v16m8-8H4'],
  deactivate: ['M10 9v6m4-6v6m7-3a9 9 0 11-18 0 9 9 0 0118 0z'],
  delete: ['M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16'],
  download: ['M12 10v6m0 0l-3-3m3 3l3-3m2 8H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414A1 1 0 0119 9.414V19a2 2 0 01-2 2z'],
  duplicate: ['M8 16H6a2 2 0 01-2-2V6a2 2 0 012-2h8a2 2 0 012 2v2', 'M10 8h8a2 2 0 012 2v8a2 2 0 01-2 2h-8a2 2 0 01-2-2v-8a2 2 0 012-2z'],
  edit: ['M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z'],
  expand: ['m9 5 7 7-7 7'],
  'export-excel': ['M12 10v6m0 0l-3-3m3 3l3-3m2 8H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414A1 1 0 0119 9.414V19a2 2 0 01-2 2z'],
  'export-pdf': ['M7 7h10M7 11h10M7 15h6M5 3h10l4 4v14H5V3z'],
  filter: ['M3 4h18M6 8h12M9 12h6m-3 0v8'],
  'follow-up': ['M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z'],
  history: ['M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z'],
  lock: ['M7 10V8a5 5 0 0110 0v2m-9 0h8a2 2 0 012 2v7H6v-7a2 2 0 012-2Z'],
  login: ['M5 12h13m0 0-4-4m4 4-4 4'],
  logout: ['M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1'],
  'mark-false-positive': ['M10 14l2-2m0 0l2-2m-2 2l-2-2m2 2l2 2m7-2a9 9 0 11-18 0 9 9 0 0118 0z'],
  'mark-positive': ['M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z'],
  menu: ['M4 6h16M4 12h16M4 18h16'],
  'more-actions': ['M5 12h.01M12 12h.01M19 12h.01'],
  'move-down': ['m6 9 6 6 6-6'],
  'move-up': ['m6 15 6-6 6 6'],
  next: ['M9 5l7 7-7 7'],
  pause: ['M6 4h4v16H6zM14 4h4v16h-4z'],
  play: ['M8 5v14l11-7z'],
  preview: ['M15 12a3 3 0 11-6 0 3 3 0 016 0z', 'M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z'],
  previous: ['M15 19l-7-7 7-7'],
  print: ['M17 17h2a2 2 0 002-2v-4a2 2 0 00-2-2H5a2 2 0 00-2 2v4a2 2 0 002 2h2m2 4h6a2 2 0 002-2v-4a2 2 0 00-2-2H9a2 2 0 00-2 2v4a2 2 0 002 2zm8-12V5a2 2 0 00-2-2H9a2 2 0 00-2 2v4h10z'],
  publish: ['M5 12l5 5L20 7'],
  refresh: ['M4 4v6h6M20 20v-6h-6M5.6 18.4A9 9 0 1 0 5.6 5.6L4 7'],
  reject: ['M6 18 18 6M6 6l12 12'],
  'reset-password': ['M15 7a2 2 0 012 2m-5-3a2 2 0 00-2 2v2a2 2 0 002 2h2a2 2 0 002-2V8a2 2 0 00-2-2h-2zm-6 3a2 2 0 11-4 0 2 2 0 014 0z'],
  resume: ['M8 5v14l11-7z'],
  retry: ['M4 4v6h6M20 20v-6h-6M5.6 18.4A9 9 0 1 0 5.6 5.6L4 7'],
  save: ['M5 4h12l2 2v14H5zM8 4v5h8V4M8 20v-6h8v6'],
  search: ['M21 21l-4.35-4.35m2.35-5.65a8 8 0 11-16 0 8 8 0 0116 0z'],
  'show-password': ['M15 12a3 3 0 11-6 0 3 3 0 016 0z', 'M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z'],
  sync: ['M4 7h11m0 0-3-3m3 3-3 3M20 17H9m0 0 3 3m-3-3 3-3'],
  'hide-password': ['M13.875 18.825A10.05 10.05 0 0112 19c-4.478 0-8.268-2.943-9.543-7a9.97 9.97 0 011.563-3.029m5.858.908a3 3 0 114.243 4.243M9.878 9.878l4.242 4.242M9.88 9.88l-3.29-3.29m7.532 7.532l3.29 3.29M3 3l3.59 3.59m0 0A9.953 9.953 0 0112 5c4.478 0 8.268 2.943 9.543 7a10.025 10.025 0 01-4.132 5.411m0 0L21 21'],
  unlock: ['M17 10V8a5 5 0 00-9.9-1M7 10h10a2 2 0 012 2v7H5v-7a2 2 0 012-2Z'],
  upload: ['M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-8-4-4m0 0L8 8m4-4v12'],
  validate: ['M5 12l4 4L19 6'],
  versions: ['M19 11H5m14 0a2 2 0 012 2v6a2 2 0 01-2 2H5a2 2 0 01-2-2v-6a2 2 0 012-2m14 0V9a2 2 0 00-2-2M5 11V9a2 2 0 012-2m0 0V5a2 2 0 012-2h6a2 2 0 012 2v2M7 7h10'],
  view: ['M15 12a3 3 0 11-6 0 3 3 0 016 0z', 'M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z']
};

@Component({
  selector: 'app-action-icon',
  standalone: true,
  template: `
    @if (loading) {
      <svg aria-hidden="true" focusable="false" viewBox="0 0 24 24" class="h-full w-full animate-spin">
        <circle class="opacity-25" cx="12" cy="12" r="10" fill="none" stroke="currentColor" stroke-width="4" />
        <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
      </svg>
    } @else {
      <svg aria-hidden="true" focusable="false" viewBox="0 0 24 24" class="h-full w-full" fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2">
        @for (path of paths; track path) { <path [attr.d]="path" /> }
      </svg>
    }
  `,
  host: { class: 'inline-flex h-[1em] w-[1em] shrink-0 items-center justify-center' },
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ActionIconComponent {
  @Input() action: ActionIconName = 'view';
  @Input() loading = false;

  get paths(): readonly string[] {
    return ACTION_ICON_PATHS[this.action] ?? ACTION_ICON_PATHS.view;
  }
}

/** Renderiza el mismo catálogo en superficies HTML externas a Angular (p. ej. SweetAlert). */
export function renderActionIconSvg(action: ActionIconName): string {
  const paths = ACTION_ICON_PATHS[action] ?? ACTION_ICON_PATHS.view;
  const markup = paths.map(path => `<path d="${path}" />`).join('');
  return `<svg aria-hidden="true" focusable="false" viewBox="0 0 24 24" style="width:20px;height:20px" fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2">${markup}</svg>`;
}
