import { Injectable, signal } from '@angular/core';
import * as ExcelJS from 'exceljs';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import {
  createExcelPreview,
  downloadBlob,
  ExcelPreviewSheet,
  WorkBook,
  workbookToBlob,
  writeFile
} from '../../core/utils/excel-export.util';

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
    this.state.set({
      kind: 'excel',
      title,
      description,
      fileName,
      blob,
      workbook,
      sheets: createExcelPreview(workbook)
    });
  }

  async openExcelBlob(blob: Blob, fileName: string, title: string, description: string): Promise<void> {
    this.close();
    const sequence = this.previewSequence;
    let sheets: ExcelPreviewSheet[] = [];
    try {
      if (typeof blob.arrayBuffer !== 'function') throw new Error('El archivo Excel no expone un buffer legible.');
      const workbook = new ExcelJS.Workbook();
      await workbook.xlsx.load(await blob.arrayBuffer());
      sheets = workbook.worksheets.map(worksheet => {
        const rows: unknown[][] = [];
        worksheet.eachRow({ includeEmpty: true }, row => {
          const values: unknown[] = [];
          for (let column = 1; column <= row.cellCount; column++) {
            values.push(this.normalizeCell(row.getCell(column).value));
          }
          rows.push(values);
        });
        return { name: worksheet.name, rows, totalRows: rows.length };
      });
    } catch {
      sheets = [{ name: 'Reporte', rows: [], totalRows: 0 }];
    }
    if (sequence === this.previewSequence) {
      this.state.set({ kind: 'excel', title, description, fileName, blob, sheets });
    }
  }

  private normalizeCell(value: unknown): unknown {
    if (value && typeof value === 'object' && 'text' in value) return (value as { text: string }).text;
    if (value && typeof value === 'object' && 'result' in value) return (value as { result: unknown }).result;
    return value;
  }

  download(): void {
    const current = this.state();
    if (!current) return;
    if (current.kind === 'excel' && current.workbook) {
      void writeFile(current.workbook, current.fileName);
      return;
    }
    downloadBlob(current.blob, current.fileName);
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
