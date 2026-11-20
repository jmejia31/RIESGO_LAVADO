import { DomSanitizer } from '@angular/platform-browser';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ReportPreviewService } from './report-preview.service';

describe('ReportPreviewService', () => {
  const notification = { notify: vi.fn().mockResolvedValue(undefined) } as any;

  beforeEach(() => {
    vi.restoreAllMocks();
    vi.clearAllMocks();
  });

  it('revoca la URL PDF al cerrar la vista previa', () => {
    const create = vi.spyOn(URL, 'createObjectURL').mockReturnValue('blob:report-preview');
    const revoke = vi.spyOn(URL, 'revokeObjectURL').mockImplementation(() => undefined);
    const sanitizer = { bypassSecurityTrustResourceUrl: (value: string) => value } as unknown as DomSanitizer;
    const service = new ReportPreviewService(sanitizer, notification);

    service.openPdf(new Blob(['pdf']), 'reporte.pdf', 'Vista previa PDF', 'Descripción');
    expect(service.state()?.kind).toBe('pdf');
    service.close();

    expect(create).toHaveBeenCalledOnce();
    expect(revoke).toHaveBeenCalledWith('blob:report-preview');
  });

  it('prepara Excel sin descargar hasta que el usuario lo solicita', async () => {
    const sanitizer = { bypassSecurityTrustResourceUrl: (value: string) => value } as unknown as DomSanitizer;
    const service = new ReportPreviewService(sanitizer, notification);
    const workbook = { sheets: [{ name: 'Reporte', sheet: { data: [['Código'], ['R-1']] } }] };

    await service.openExcel(workbook, 'reporte.xlsx', 'Vista previa Excel', 'Descripción');
    expect(service.state()?.kind).toBe('excel');
    expect(service.state()?.sheets?.[0].rows).toEqual([['Código'], ['R-1']]);
  });
  it('abre PDF y Excel sin notificar una descarga prematura', async () => {
    const sanitizer = { bypassSecurityTrustResourceUrl: (value: string) => value } as unknown as DomSanitizer;
    const service = new ReportPreviewService(sanitizer, notification);

    await service.openExcel({ sheets: [{ name: 'Reporte', sheet: { data: [['Codigo'], ['R-1']] } }] }, 'reporte.xlsx', 'Vista previa del reporte', 'Revise el reporte.');
    service.openPdf(new Blob(['pdf'], { type: 'application/pdf' }), 'reporte.pdf', 'Vista previa del reporte PDF', 'Revise el reporte.');

    expect(notification.notify).not.toHaveBeenCalled();
  });

  it('notifica la descarga iniciada solamente después de descargar', async () => {
    const sanitizer = { bypassSecurityTrustResourceUrl: (value: string) => value } as unknown as DomSanitizer;
    const service = new ReportPreviewService(sanitizer, notification);
    const click = vi.fn();
    const link = { href: '', download: '', click };
    vi.stubGlobal('document', {
      createElement: () => link,
      body: { appendChild: vi.fn(), removeChild: vi.fn() }
    });
    vi.spyOn(URL, 'createObjectURL').mockReturnValue('blob:download');
    vi.spyOn(URL, 'revokeObjectURL').mockImplementation(() => undefined);

    service.openPdf(new Blob(['pdf'], { type: 'application/pdf' }), 'reporte.pdf', 'Vista previa del reporte PDF', 'Revise el reporte.');
    expect(notification.notify).not.toHaveBeenCalled();
    await expect(service.download()).resolves.toBe(true);
    expect(click).toHaveBeenCalledOnce();
    expect(notification.notify).toHaveBeenCalledWith(expect.objectContaining({ icon: 'success', title: 'Descarga iniciada' }));

    vi.unstubAllGlobals();
    vi.restoreAllMocks();
  });

  it('informa el error sin anunciar éxito cuando falla el inicio de descarga', async () => {
    const sanitizer = { bypassSecurityTrustResourceUrl: (value: string) => value } as unknown as DomSanitizer;
    const service = new ReportPreviewService(sanitizer, notification);
    vi.spyOn(URL, 'createObjectURL').mockImplementation(() => { throw new Error('fallo'); });

    service.state.set({
      kind: 'pdf',
      title: 'Vista previa del reporte PDF',
      description: 'Revise el reporte.',
      fileName: 'reporte.pdf',
      blob: new Blob(['pdf'], { type: 'application/pdf' })
    });
    await expect(service.download()).resolves.toBe(false);
    expect(notification.notify).toHaveBeenCalledWith(expect.objectContaining({ icon: 'error' }));
    expect(notification.notify).not.toHaveBeenCalledWith(expect.objectContaining({ title: 'Descarga iniciada' }));
    vi.restoreAllMocks();
  });
});
