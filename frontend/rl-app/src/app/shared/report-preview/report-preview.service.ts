import { Injectable, signal } from '@angular/core';
import * as ExcelJS from 'exceljs';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import {
  createExcelPreview,
  createExcelPreviewFromRows,
  downloadBlob,
  ExcelPreviewSheet,
  WorkBook,
  workbookToBlob
} from '../../core/utils/excel-export.util';
import { getInstitutionalReportBranding } from '../../core/reporting/institutional-report-standard';

export type ReportPreviewKind = 'pdf' | 'excel';

export interface ReportPreviewState {
  kind: ReportPreviewKind;
  title: string;
  description: string;
  fileName: string;
  blob: Blob;
  pdfUrl?: SafeResourceUrl;
  workbook?: WorkBook;
  sheets?: ExcelPreviewSheet[];
  logoEmbedded?: boolean;
  logoUrl?: string;
  logoStatus?: 'sin-logo' | 'disponible' | 'invalido' | 'inaccesible';
  logoErrorMessage?: string;
}

@Injectable({ providedIn: 'root' })
export class ReportPreviewService {
  readonly state = signal<ReportPreviewState | null>(null);
  private pdfObjectUrl: string | null = null;
  private previewSequence = 0;

  constructor(private readonly sanitizer: DomSanitizer) {}

  openPdf(blob: Blob, fileName: string, title: string, description: string): void {
    this.close();
    this.pdfObjectUrl = URL.createObjectURL(blob);
    this.state.set({
      kind: 'pdf',
      title,
      description,
      fileName,
      blob,
      pdfUrl: this.sanitizer.bypassSecurityTrustResourceUrl(this.pdfObjectUrl)
    });
  }

  async openExcel(workbook: WorkBook, fileName: string, title: string, description: string): Promise<void> {
    this.close();
    const sequence = this.previewSequence;
    const blob = await workbookToBlob(workbook);
    if (sequence !== this.previewSequence) return;
    const branding = getInstitutionalReportBranding();
    const hasLogo = workbook.logoEmbedded === true && !!branding.logoUrl;
    this.state.set({
      kind: 'excel',
      title,
      description,
      fileName,
      blob,
      workbook,
      sheets: createExcelPreview(workbook),
      logoEmbedded: workbook.logoEmbedded === true,
      logoUrl: hasLogo ? branding.logoUrl : undefined,
      logoStatus: branding.logoStatus ?? (hasLogo ? 'disponible' : 'sin-logo'),
      logoErrorMessage: branding.logoErrorMessage
    });
  }

  async openExcelBlob(blob: Blob, fileName: string, title: string, description: string): Promise<void> {
    this.close();
    const sequence = this.previewSequence;
    let sheets: ExcelPreviewSheet[] = [];
    let logoEmbedded = false;
    let extractedLogoUrl: string | undefined;
    try {
      if (typeof blob.arrayBuffer !== 'function') throw new Error('El archivo Excel no expone un buffer legible.');
      const workbook = new ExcelJS.Workbook();
      await workbook.xlsx.load(await blob.arrayBuffer());
      for (const worksheet of workbook.worksheets) {
        const images = worksheet.getImages();
        if (images.length > 0) {
          logoEmbedded = true;
          if (!extractedLogoUrl) {
            const firstImage = images[0];
            const media = workbook.getImage(Number(firstImage.imageId));
            if (media && media.buffer) {
              const mime = media.extension === 'png' ? 'image/png' : 'image/jpeg';
              const bytes = new Uint8Array(media.buffer);
              let binary = '';
              for (let i = 0; i < bytes.byteLength; i++) {
                binary += String.fromCharCode(bytes[i]);
              }
              const b64 = typeof btoa === 'function' ? btoa(binary) : '';
              extractedLogoUrl = `data:${mime};base64,${b64}`;
            }
          }
        }
        const rows: unknown[][] = [];
        worksheet.eachRow({ includeEmpty: true }, row => {
          const values: unknown[] = [];
          for (let column = 1; column <= row.cellCount; column++) {
            values.push(this.normalizeCell(row.getCell(column).value));
          }
          rows.push(values);
        });
        sheets.push(createExcelPreviewFromRows(worksheet.name, rows));
      }
    } catch {
      sheets = [createExcelPreviewFromRows('Reporte', [])];
    }
    if (sequence === this.previewSequence) {
      const branding = getInstitutionalReportBranding();
      const finalLogoUrl = extractedLogoUrl || (logoEmbedded ? branding.logoUrl : undefined);
      this.state.set({
        kind: 'excel',
        title,
        description,
        fileName,
        blob,
        sheets,
        logoEmbedded,
        logoUrl: finalLogoUrl || undefined,
        logoStatus: finalLogoUrl ? 'disponible' : (branding.logoStatus ?? (logoEmbedded ? 'inaccesible' : 'sin-logo')),
        logoErrorMessage: branding.logoErrorMessage
      });
    }
  }

  private normalizeCell(value: unknown): unknown {
    if (value && typeof value === 'object' && 'text' in value) return (value as { text: string }).text;
    if (value && typeof value === 'object' && 'result' in value) return (value as { result: unknown }).result;
    return value;
  }

  async download(): Promise<boolean> {
    const current = this.state();
    if (!current) return false;
    // La descarga usa exactamente el artefacto que el usuario revisó.
    // Esto evita regenerar un workbook distinto al previewado.
    try {
      downloadBlob(current.blob, current.fileName);
    } catch {
      await this.notificarDescargaFallida(current);
      return false;
    }

    await this.notificarDescargaIniciada(current);
    return true;
  }

  private async notificarDescargaIniciada(current: ReportPreviewState): Promise<void> {
    const Swal = await import('sweetalert2');
    const contenido = current.title.replace(/^Vista previa (?:del|de la|de los|de las|de)\s+/i, '').trim();
    await Swal.default.fire({
      toast: true,
      position: 'top-end',
      icon: 'success',
      title: 'Descarga iniciada',
      text: contenido ? `Descarga iniciada: ${contenido}.` : 'Descarga iniciada correctamente.',
      showConfirmButton: false,
      timer: 3500,
      timerProgressBar: true
    });
  }

  private async notificarDescargaFallida(current: ReportPreviewState): Promise<void> {
    const Swal = await import('sweetalert2');
    const contenido = current.title.replace(/^Vista previa (?:del|de la|de los|de las|de)\s+/i, '').trim();
    await Swal.default.fire({
      toast: true,
      position: 'top-end',
      icon: 'error',
      title: 'No fue posible iniciar la descarga',
      text: contenido ? `No fue posible iniciar la descarga de ${contenido}.` : 'No fue posible iniciar la descarga del reporte.',
      showConfirmButton: false,
      timer: 4500,
      timerProgressBar: true
    });
  }

  close(): void {
    this.previewSequence += 1;
    if (this.pdfObjectUrl) {
      URL.revokeObjectURL(this.pdfObjectUrl);
      this.pdfObjectUrl = null;
    }
    this.state.set(null);
  }

  destroy(): void {
    this.close();
  }
}
