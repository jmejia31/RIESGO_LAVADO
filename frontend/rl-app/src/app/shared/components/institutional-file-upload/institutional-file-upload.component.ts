import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output, signal } from '@angular/core';

import { ActionIconComponent } from '../action-icon/action-icon.component';
import { INSTITUTIONAL_EVIDENCE_FILE_POLICY, InstitutionalFileUploadPolicy } from './institutional-file-upload-policy';

@Component({
  selector: 'app-institutional-file-upload',
  standalone: true,
  imports: [ActionIconComponent, CommonModule],
  templateUrl: './institutional-file-upload.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class InstitutionalFileUploadComponent {
  private static nextId = 0;

  @Input() label = 'Archivos Adjuntos';
  @Input() policy: InstitutionalFileUploadPolicy = INSTITUTIONAL_EVIDENCE_FILE_POLICY;
  @Input() multiple = true;
  @Input() disabled = false;
  @Input() required = false;
  @Input() uploading = false;
  @Input() helperText = '';
  @Input() selectedFiles: File[] = [];
  @Output() readonly filesChange = new EventEmitter<File[]>();
  @Output() readonly fileRejected = new EventEmitter<string>();

  readonly inputId = `institutional-file-upload-${++InstitutionalFileUploadComponent.nextId}`;
  readonly dragging = signal(false);
  readonly rejectionMessage = signal('');

  get accept(): string {
    return this.policy.allowedExtensions.map(extension => `.${extension.replace(/^\./, '')}`).join(',');
  }

  get disabledState(): boolean {
    return this.disabled || this.uploading;
  }

  get sizeText(): string {
    if (!this.policy.maxFileSizeBytes) return this.policy.allowedTypesText;
    const megabytes = this.policy.maxFileSizeBytes / (1024 * 1024);
    const max = `${megabytes}`;
    return `${this.policy.allowedTypesText} (Máx. ${max}MB${this.multiple ? ' c/u' : ''})`;
  }

  formatFileSize(bytes: number): string {
    if (bytes < 1024) return `${bytes} B`;
    const kilobytes = bytes / 1024;
    if (kilobytes < 1024) return `${kilobytes < 10 ? kilobytes.toFixed(1) : Math.round(kilobytes)} KB`;
    return `${(kilobytes / 1024).toFixed(2)} MB`;
  }

  openPicker(input: HTMLInputElement): void {
    if (!this.disabledState) input.click();
  }

  onKeydown(event: KeyboardEvent, input: HTMLInputElement): void {
    if (event.key !== 'Enter' && event.key !== ' ') return;
    event.preventDefault();
    this.openPicker(input);
  }

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    if (!this.disabledState) this.dragging.set(true);
  }

  onDragLeave(event: DragEvent): void {
    event.preventDefault();
    this.dragging.set(false);
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    this.dragging.set(false);
    if (this.disabledState) return;
    this.addFiles(Array.from(event.dataTransfer?.files ?? []));
  }

  onSelection(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.addFiles(Array.from(input.files ?? []));
    input.value = '';
  }

  removeFile(index: number): void {
    if (this.disabledState || index < 0 || index >= this.selectedFiles.length) return;
    this.publish(this.selectedFiles.filter((_, current) => current !== index));
  }

  private addFiles(incoming: File[]): void {
    if (this.disabledState || incoming.length === 0) return;
    this.rejectionMessage.set('');
    const next = this.multiple ? [...this.selectedFiles] : [];
    const candidates = this.multiple ? incoming : incoming.slice(0, 1);
    for (const file of candidates) {
      const rejection = this.validate(file, next);
      if (rejection) {
        this.reject(rejection);
        continue;
      }
      next.push(file);
    }
    if (!this.multiple && incoming.length > 1) this.reject('Solo puede seleccionar un archivo.');
    this.publish(next);
  }

  private validate(file: File, current: File[]): string | null {
    if (file.size === 0) return `El archivo ${file.name} está vacío.`;
    const extension = file.name.split('.').pop()?.toLowerCase();
    const allowed = this.policy.allowedExtensions.map(value => value.replace(/^\./, '').toLowerCase());
    if (!extension || !allowed.includes(extension)) return `El archivo ${file.name} no tiene un formato permitido.`;
    if (this.policy.maxFileSizeBytes && file.size > this.policy.maxFileSizeBytes) {
      const maximum = this.policy.maxFileSizeBytes / (1024 * 1024);
      return `El archivo ${file.name} supera el límite de ${maximum} MB.`;
    }
    if (current.some(existing => this.isSameFile(existing, file))) return `El archivo ${file.name} ya está seleccionado.`;
    if (this.policy.maxFiles && current.length >= this.policy.maxFiles) return `Solo puede seleccionar hasta ${this.policy.maxFiles} archivos.`;
    return null;
  }

  private isSameFile(left: File, right: File): boolean {
    return left.name === right.name && left.size === right.size && left.lastModified === right.lastModified;
  }

  private reject(message: string): void {
    this.rejectionMessage.set(message);
    this.fileRejected.emit(message);
  }

  private publish(files: File[]): void {
    this.selectedFiles = files;
    this.filesChange.emit(files);
  }
}
