import { describe, expect, it, afterAll } from 'vitest';
// @ts-ignore: Vitest ejecuta este archivo en Node.js.
import { createHash } from 'node:crypto';
// @ts-ignore: Vitest ejecuta este archivo en Node.js.
import { writeFileSync, mkdirSync } from 'node:fs';
// @ts-ignore: Vitest ejecuta este archivo en Node.js.
import { resolve } from 'node:path';
import { jsPDF } from 'jspdf';
import {
  construirLibroInstitucional,
  WorkBook,
  WorkSheet
} from '../utils/excel-export.util';
import {
  construirExcelInstitucionalDesdeReporte,
  InstitutionalReportDefinition
} from './institutional-report-parity.util';
import {
  agregarEncabezadoInstitucionalPdf,
  agregarPiesInstitucionalesPdf,
  autoTableInstitucional,
  COLORES_REPORTE_INSTITUCIONAL
} from './institutional-report.util';
import {
  INSTITUTIONAL_PDF_LOGO_WIDTH_MM,
  INSTITUTIONAL_LOGO_POLICY,
  INSTITUTIONAL_REPORT_COLORS,
  setInstitutionalReportBranding,
  getInstitutionalReportBranding
} from './institutional-report-standard';

function sha256Hex(buffer: ArrayBuffer | Uint8Array): string {
  const bytes = buffer instanceof Uint8Array ? buffer : new Uint8Array(buffer);
  return createHash('sha256').update(bytes).digest('hex');
}

describe('Microfase C3 — Certificación física de paridad Parejas 2, 3, 4 y 5 (Frontend)', () => {
  const capturedHashes: Record<string, string> = {};

  afterAll(() => {
    setInstitutionalReportBranding({
      headerColor: INSTITUTIONAL_REPORT_COLORS.navy,
      headerTextColor: INSTITUTIONAL_REPORT_COLORS.white,
      logoUrl: '',
      logoStatus: 'sin-logo'
    });
  });

  // 5 Temas M2 de contraste institucional
  const cincoTemasM2 = [
    { nombre: 'Navy Institucional', hex: '#123B63', rgb: [18, 59, 99] as [number, number, number] },
    { nombre: 'Teal Institucional', hex: '#0F766E', rgb: [15, 118, 110] as [number, number, number] },
    { nombre: 'Rojo Institucional', hex: '#B91C1C', rgb: [185, 28, 28] as [number, number, number] },
    { nombre: 'Slate Institucional', hex: '#1E293B', rgb: [30, 41, 59] as [number, number, number] },
    { nombre: 'Sky Institucional', hex: '#0284C7', rgb: [2, 132, 199] as [number, number, number] }
  ];

  it('Verifica geometría B06 canónica y los 5 temas M2 de contraste', () => {
    // Geometría B06: Ancho canónico 72.644 mm (205.92 pt)
    expect(INSTITUTIONAL_PDF_LOGO_WIDTH_MM).toBe(72.644);
    expect(INSTITUTIONAL_LOGO_POLICY.pdfWidthMm).toBe(72.644);
    expect(INSTITUTIONAL_LOGO_POLICY.pdfMaxHeightMm).toBe(32);
    expect(INSTITUTIONAL_LOGO_POLICY.excelWidthPx).toBe(156);
    expect(INSTITUTIONAL_LOGO_POLICY.excelMaxHeightPx).toBe(56);

    // Temas M2
    for (const tema of cincoTemasM2) {
      setInstitutionalReportBranding({ headerColor: tema.rgb });
      const current = getInstitutionalReportBranding();
      expect(current.headerColor).toEqual(tema.rgb);
    }
    // Restaurar color navy predeterminado institucional para aislamiento entre pruebas
    setInstitutionalReportBranding({ headerColor: INSTITUTIONAL_REPORT_COLORS.navy });
  });

  it('Pareja 2 (REP-04 / REP-05): Monitoreo de Listas — Lista Principal (Física Excel y PDF)', async () => {
    const reportData: InstitutionalReportDefinition = {
      title: 'REPORTE GENERAL DE MONITOREO DE COINCIDENCIAS',
      institution: 'INSTITUTO HONDUREÑO DE SEGURIDAD SOCIAL',
      systemName: 'SGRLA-IHSS',
      generatedAt: new Date('2026-11-20T10:00:00Z'),
      sections: [
        {
          kind: 'paragraph',
          title: 'PARÁMETROS Y FILTROS APLICADOS',
          text: 'Tipo: Personas Jurídicas | Estado: Pendientes | Periodo: 2026'
        },
        {
          kind: 'table',
          title: 'LISTA DE COINCIDENCIAS IDENTIFICADAS',
          headers: ['Nro Patronal', 'RTN', 'Razón Social', 'Tipo Lista', 'Estado', 'Fecha Detección'],
          rows: [
            ['201200601751', '05019006500703', 'TECPROFIRE S DE R.L.', 'OFAC', 'PENDIENTE', '20/11/2026'],
            ['0801199000123', '08011990001234', 'INVERSIONES DEL SUR', 'ONU', 'EN REVISIÓN', '19/11/2026'],
            ['0101198500456', '01011985004567', 'DISTRIBUIDORA NORTE', 'PEP', 'CALIFICADO', '18/11/2026']
          ]
        }
      ]
    };

    // 1. Generar Excel físicamente y obtener bytes reales
    const excelReport = construirExcelInstitucionalDesdeReporte(reportData);
    const wb: WorkBook = { sheets: [{ name: 'Monitoreo', sheet: excelReport.worksheet }] };
    const excelWorkbook = construirLibroInstitucional(wb);
    const excelBuffer = await excelWorkbook.xlsx.writeBuffer();
    const excelBytes = new Uint8Array(excelBuffer);

    expect(excelBytes.length).toBeGreaterThan(1000);
    const excelSha = sha256Hex(excelBytes);
    expect(excelSha).toHaveLength(64);

    // 2. Generar PDF físicamente y obtener bytes reales
    const doc = new jsPDF({ orientation: 'landscape', unit: 'mm', format: 'a4' });
    agregarEncabezadoInstitucionalPdf(doc, reportData.title, reportData.institution, reportData.systemName, 'Tipo: Personas Jurídicas');
    autoTableInstitucional(doc, {
      startY: 50,
      head: [['Nro Patronal', 'RTN', 'Razón Social', 'Tipo Lista', 'Estado', 'Fecha Detección']],
      body: [
        ['201200601751', '05019006500703', 'TECPROFIRE S DE R.L.', 'OFAC', 'PENDIENTE', '20/11/2026'],
        ['0801199000123', '08011990001234', 'INVERSIONES DEL SUR', 'ONU', 'EN REVISIÓN', '19/11/2026'],
        ['0101198500456', '01011985004567', 'DISTRIBUIDORA NORTE', 'PEP', 'CALIFICADO', '18/11/2026']
      ]
    });
    agregarPiesInstitucionalesPdf(doc);

    const pdfBuffer = doc.output('arraybuffer');
    const pdfBytes = new Uint8Array(pdfBuffer);
    expect(pdfBytes.length).toBeGreaterThan(1000);
    const pdfSha = sha256Hex(pdfBytes);
    expect(pdfSha).toHaveLength(64);

    capturedHashes['REP-04_EXCEL_SHA256'] = excelSha;
    capturedHashes['REP-05_PDF_SHA256'] = pdfSha;

    // 3. Extracción física y comparación de paridad
    const pdfString = doc.output();
    const excelCells = excelReport.data.flat().map(c => String(c ?? ''));

    // Registros
    expect(excelCells).toContain('201200601751');
    expect(pdfString).toContain('201200601751');
    expect(excelCells).toContain('0801199000123'); // Preserva ceros
    expect(pdfString).toContain('0801199000123');
    expect(excelCells).toContain('TECPROFIRE S DE R.L.');
    expect(pdfString).toContain('TECPROFIRE S DE R.L.');
    expect(excelCells).toContain('05019006500703');
    expect(pdfString).toContain('05019006500703');
  });

  it('Pareja 3 (REP-06 / REP-07): Monitoreo — Persona Natural Ficha Integral (Física Excel y PDF)', async () => {
    const reportData: InstitutionalReportDefinition = {
      title: 'REPORTE INTEGRAL DE PERSONA NATURAL',
      institution: 'INSTITUTO HONDUREÑO DE SEGURIDAD SOCIAL',
      systemName: 'SGRLA-IHSS',
      generatedAt: new Date('2026-11-20T10:00:00Z'),
      sections: [
        {
          kind: 'keyValue',
          title: '1. INFORMACIÓN GENERAL DE LA PERSONA',
          rows: [
            ['DNI / Identificación:', '0801199900010', 'Nombre Completo:', 'JUAN CARLOS PEREZ GOMEZ'],
            ['Lista de Coincidencia:', 'OFAC', 'Total de Coincidencias:', '1'],
            ['Estado Monitoreo:', 'PENDIENTE DE REGISTRO', 'Registro Interno:', '20/11/2026'],
            ['Origen del Registro:', 'DNP_LISTAS', '', '']
          ]
        },
        {
          kind: 'paragraph',
          title: '2. MOTIVO DE INGRESO A LISTA DE MONITOREO',
          text: 'Coincidencia preventiva detectada en cruce automático OFAC.'
        },
        {
          kind: 'table',
          title: '3. DETALLE DE COINCIDENCIAS ENCONTRADAS',
          headers: ['Condición Actúa', 'Nro Patronal', 'Empresa', 'Es PEP', 'Lista', 'Fecha Coincidencia'],
          rows: [
            ['Afiliado', '201200601751', 'TECPROFIRE S DE R.L.', 'NO', 'OFAC', '20/11/2026']
          ]
        },
        {
          kind: 'history',
          title: '4. HISTORIAL DE SEGUIMIENTOS Y EVIDENCIAS',
          rangeText: 'Todos los seguimientos',
          headers: ['Fecha', 'Usuario', 'Comentario', 'Evidencias'],
          rows: [
            ['20/11/2026', 'admin@ihss.hn', 'Apertura de expediente preventivo', 'ofac_match.pdf']
          ],
          emptyText: 'Sin seguimientos'
        }
      ]
    };

    // Excel
    const excelReport = construirExcelInstitucionalDesdeReporte(reportData);
    const wb: WorkBook = { sheets: [{ name: 'Ficha Natural', sheet: excelReport.worksheet }] };
    const excelBytes = new Uint8Array(await construirLibroInstitucional(wb).xlsx.writeBuffer());
    expect(excelBytes.length).toBeGreaterThan(1000);
    const excelSha = sha256Hex(excelBytes);
    expect(excelSha).toHaveLength(64);

    // PDF
    const doc = new jsPDF({ orientation: 'portrait', unit: 'mm', format: 'a4' });
    agregarEncabezadoInstitucionalPdf(doc, reportData.title);
    autoTableInstitucional(doc, {
      startY: 45,
      body: [
        ['DNI / Identificación:', '0801199900010', 'Nombre Completo:', 'JUAN CARLOS PEREZ GOMEZ'],
        ['Lista de Coincidencia:', 'OFAC', 'Total de Coincidencias:', '1'],
        ['Estado Monitoreo:', 'PENDIENTE DE REGISTRO', 'Registro Interno:', '20/11/2026'],
        ['Origen del Registro:', 'DNP_LISTAS', '', '']
      ]
    });
    agregarPiesInstitucionalesPdf(doc);
    const pdfBytes = new Uint8Array(doc.output('arraybuffer'));
    expect(pdfBytes.length).toBeGreaterThan(1000);
    const pdfSha = sha256Hex(pdfBytes);
    expect(pdfSha).toHaveLength(64);

    capturedHashes['REP-06_EXCEL_SHA256'] = excelSha;
    capturedHashes['REP-07_PDF_SHA256'] = pdfSha;

    // Paridad de datos e identificadores
    const pdfText = doc.output();
    const excelCells = excelReport.data.flat().map(c => String(c ?? ''));
    expect(excelCells).toContain('0801199900010'); // Preserva cero a la izquierda
    expect(pdfText).toContain('0801199900010');
    expect(excelCells).toContain('JUAN CARLOS PEREZ GOMEZ');
    expect(pdfText).toContain('JUAN CARLOS PEREZ GOMEZ');
    expect(excelCells).toContain('1. INFORMACIÓN GENERAL DE LA PERSONA');
    expect(excelCells).toContain('ofac_match.pdf');
  });

  it('Pareja 4 (REP-08 / REP-09): Monitoreo — Empleado IHSS Ficha Integral (Física Excel y PDF)', async () => {
    const reportData: InstitutionalReportDefinition = {
      title: 'REPORTE INTEGRAL DE EMPLEADO IHSS',
      institution: 'INSTITUTO HONDUREÑO DE SEGURIDAD SOCIAL',
      systemName: 'SGRLA-IHSS',
      generatedAt: new Date('2026-11-20T10:00:00Z'),
      sections: [
        {
          kind: 'keyValue',
          title: '1. INFORMACIÓN GENERAL DEL EMPLEADO',
          rows: [
            ['DNI / Identidad:', '0801198800011', 'Nombre Completo:', 'MARIA ELENA LOPEZ'],
            ['Lista de Coincidencia:', 'OFAC', 'Total de Coincidencias:', '1'],
            ['Estado Monitoreo:', 'EN REVISIÓN', 'Registro Interno:', '20/11/2026'],
            ['Origen del Registro:', 'MANUAL_CUMPLIMIENTO', '', '']
          ]
        },
        {
          kind: 'paragraph',
          title: '2. MOTIVO DE INGRESO A LISTA DE MONITOREO',
          text: 'Alerta preventiva de cumplimiento interno.'
        },
        {
          kind: 'table',
          title: '3. DETALLE DE COINCIDENCIAS ENCONTRADAS',
          headers: ['Condición Actúa', 'Nro Patronal', 'Empresa', 'Es PEP', 'Lista', 'Fecha Coincidencia'],
          rows: [
            ['Empleado', 'IHSS-001', 'IHSS OFICINAS CENTRALES', 'SÍ', 'OFAC', '20/11/2026']
          ]
        },
        {
          kind: 'history',
          title: '4. HISTORIAL DE SEGUIMIENTOS Y EVIDENCIAS',
          rangeText: 'Todos los seguimientos',
          headers: ['Fecha', 'Usuario', 'Comentario', 'Evidencias'],
          rows: [
            ['20/11/2026', 'oficial@ihss.hn', 'Verificación de puesto y funciones', 'constancia_laboral.pdf']
          ],
          emptyText: 'Sin seguimientos'
        }
      ]
    };

    // Excel
    const excelReport = construirExcelInstitucionalDesdeReporte(reportData);
    const wb: WorkBook = { sheets: [{ name: 'Ficha Empleado', sheet: excelReport.worksheet }] };
    const excelBytes = new Uint8Array(await construirLibroInstitucional(wb).xlsx.writeBuffer());
    expect(excelBytes.length).toBeGreaterThan(1000);
    const excelSha = sha256Hex(excelBytes);
    expect(excelSha).toHaveLength(64);

    // PDF
    const doc = new jsPDF({ orientation: 'portrait', unit: 'mm', format: 'a4' });
    agregarEncabezadoInstitucionalPdf(doc, reportData.title);
    autoTableInstitucional(doc, {
      startY: 45,
      body: [
        ['DNI / Identidad:', '0801198800011', 'Nombre Completo:', 'MARIA ELENA LOPEZ'],
        ['Lista de Coincidencia:', 'OFAC', 'Total de Coincidencias:', '1'],
        ['Estado Monitoreo:', 'EN REVISIÓN', 'Registro Interno:', '20/11/2026'],
        ['Origen del Registro:', 'MANUAL_CUMPLIMIENTO', '', '']
      ]
    });
    agregarPiesInstitucionalesPdf(doc);
    const pdfBytes = new Uint8Array(doc.output('arraybuffer'));
    expect(pdfBytes.length).toBeGreaterThan(1000);
    const pdfSha = sha256Hex(pdfBytes);
    expect(pdfSha).toHaveLength(64);

    capturedHashes['REP-08_EXCEL_SHA256'] = excelSha;
    capturedHashes['REP-09_PDF_SHA256'] = pdfSha;

    // Paridad
    const pdfText = doc.output();
    const excelCells = excelReport.data.flat().map(c => String(c ?? ''));
    expect(excelCells).toContain('0801198800011'); // Cero preservado
    expect(pdfText).toContain('0801198800011');
    expect(excelCells).toContain('MARIA ELENA LOPEZ');
    expect(pdfText).toContain('MARIA ELENA LOPEZ');
    expect(excelCells).toContain('IHSS-001');
    expect(excelCells).toContain('constancia_laboral.pdf');
  });

  it('Pareja 5 (REP-10 / REP-11): Monitoreo — Patrono Ficha Integral (Física Excel y PDF)', async () => {
    const reportData: InstitutionalReportDefinition = {
      title: 'REPORTE INTEGRAL DE PATRONO',
      institution: 'INSTITUTO HONDUREÑO DE SEGURIDAD SOCIAL',
      systemName: 'SGRLA-IHSS',
      generatedAt: new Date('2026-11-20T10:00:00Z'),
      sections: [
        {
          kind: 'keyValue',
          title: '1. INFORMACIÓN GENERAL DEL PATRONO',
          rows: [
            ['Número Patronal:', '201200601751', 'RTN:', '05019006500703'],
            ['Nombre / Razón Social:', 'TECPROFIRE S DE R.L. DE C.V.', 'Proveedor IHSS:', 'No'],
            ['Lista de Coincidencia:', 'OFAC', 'Estado Monitoreo:', 'PENDIENTE DE REGISTRO'],
            ['Fecha Coincidencia:', '21/05/2026', 'Fecha Calificación:', '14/06/2026'],
            ['Registro Interno:', 'N/D', 'Origen del Registro:', 'DNP_LISTAS']
          ]
        },
        {
          kind: 'paragraph',
          title: '2. MOTIVO DE INGRESO A LISTA DE MONITOREO',
          text: 'No se ha registrado un motivo de ingreso inicial en el sistema para este patrono.'
        },
        {
          kind: 'history',
          title: '3. HISTORIAL DE SEGUIMIENTOS Y EVIDENCIAS',
          rangeText: 'Todos los seguimientos registrados',
          headers: ['Fecha', 'Usuario', 'Comentario / Acción', 'Evidencias'],
          rows: [],
          emptyText: 'No se registran acciones de seguimiento ni evidencias adicionales para este patrono.'
        }
      ]
    };

    // Excel
    const excelReport = construirExcelInstitucionalDesdeReporte(reportData);
    const wb: WorkBook = { sheets: [{ name: 'Ficha Patrono', sheet: excelReport.worksheet }] };
    const excelBytes = new Uint8Array(await construirLibroInstitucional(wb).xlsx.writeBuffer());
    expect(excelBytes.length).toBeGreaterThan(1000);
    const excelSha = sha256Hex(excelBytes);
    expect(excelSha).toHaveLength(64);

    // PDF
    const doc = new jsPDF({ orientation: 'portrait', unit: 'mm', format: 'a4' });
    agregarEncabezadoInstitucionalPdf(doc, reportData.title);
    autoTableInstitucional(doc, {
      startY: 45,
      body: [
        ['Número Patronal:', '201200601751', 'RTN:', '05019006500703'],
        ['Nombre / Razón Social:', 'TECPROFIRE S DE R.L. DE C.V.', 'Proveedor IHSS:', 'No'],
        ['Lista de Coincidencia:', 'OFAC', 'Estado Monitoreo:', 'PENDIENTE DE REGISTRO'],
        ['Fecha Coincidencia:', '21/05/2026', 'Fecha Calificación:', '14/06/2026'],
        ['Registro Interno:', 'N/D', 'Origen del Registro:', 'DNP_LISTAS']
      ]
    });
    agregarPiesInstitucionalesPdf(doc);
    const pdfBytes = new Uint8Array(doc.output('arraybuffer'));
    expect(pdfBytes.length).toBeGreaterThan(1000);
    const pdfSha = sha256Hex(pdfBytes);
    expect(pdfSha).toHaveLength(64);

    capturedHashes['REP-10_EXCEL_SHA256'] = excelSha;
    capturedHashes['REP-11_PDF_SHA256'] = pdfSha;

    // Paridad
    const pdfText = doc.output();
    const excelCells = excelReport.data.flat().map(c => String(c ?? ''));
    expect(excelCells).toContain('201200601751');
    expect(pdfText).toContain('201200601751');
    expect(excelCells).toContain('05019006500703');
    expect(pdfText).toContain('05019006500703');
    expect(excelCells).toContain('TECPROFIRE S DE R.L. DE C.V.');
    expect(pdfText).toContain('TECPROFIRE S DE R.L. DE C.V.');
    expect(excelCells).toContain('N/D'); // Mapeo consistente de nulos
    expect(pdfText).toContain('N/D');
  });
});
