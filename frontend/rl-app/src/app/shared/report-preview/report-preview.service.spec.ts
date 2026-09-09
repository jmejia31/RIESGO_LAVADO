import { DomSanitizer } from '@angular/platform-browser';
import { describe, expect, it, vi } from 'vitest';
import { ReportPreviewService } from './report-preview.service';

describe('ReportPreviewService', () => {
  it('revoca la URL PDF al cerrar la vista previa', () => {
    const create = vi.spyOn(URL, 'createObjectURL').mockReturnValue('blob:report-preview');
    const revoke = vi.spyOn(URL, 'revokeObjectURL').mockImplementation(() => undefined);
    const sanitizer = { bypassSecurityTrustResourceUrl: (value: string) => value } as unknown as DomSanitizer;
    const service = new ReportPreviewService(sanitizer);

    service.openPdf(new Blob(['pdf']), 'reporte.pdf', 'Vista previa PDF', 'Descripción');
    expect(service.state()?.kind).toBe('pdf');
    service.close();

    expect(create).toHaveBeenCalledOnce();
    expect(revoke).toHaveBeenCalledWith('blob:report-preview');
  });

  it('prepara Excel sin descargar hasta que el usuario lo solicita', async () => {
    const sanitizer = { bypassSecurityTrustResourceUrl: (value: string) => value } as unknown as DomSanitizer;
    const service = new ReportPreviewService(sanitizer);
    const workbook = { sheets: [{ name: 'Reporte', sheet: { data: [['Código'], ['R-1']] } }] };

    await service.openExcel(workbook, 'reporte.xlsx', 'Vista previa Excel', 'Descripción');
    expect(service.state()?.kind).toBe('excel');
    expect(service.state()?.sheets?.[0].rows).toEqual([['Código'], ['R-1']]);
  });
});
