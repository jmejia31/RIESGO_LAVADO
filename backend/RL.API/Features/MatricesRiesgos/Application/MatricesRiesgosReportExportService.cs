using System.Globalization;
using System.IO.Compression;
using System.Security;
using System.Text;
using RL.API.Features.MatricesRiesgos.Contracts;

namespace RL.API.Features.MatricesRiesgos.Application;

public interface IMatricesRiesgosReportExportService
{
    ArchivoReporteDto CrearExcelConsolidado(IReadOnlyList<RiesgoReporteFilaDto> filas);
    ArchivoReporteDto CrearPdfConsolidado(IReadOnlyList<RiesgoReporteFilaDto> filas);
}

/// <summary>
/// Exportador sin dependencias de terceros. Genera un XLSX OpenXML mínimo y un
/// PDF 1.4 paginado, reduciendo superficie de dependencias en el módulo sensible.
/// </summary>
public sealed class MatricesRiesgosReportExportService : IMatricesRiesgosReportExportService
{
    private static readonly string[] Encabezados =
    {
        "Riesgo ID", "Evaluación ID", "Versión", "Código", "Área principal", "Dueño del riesgo",
        "VRI", "Nivel inherente", "VRR", "Nivel residual", "Respuesta", "Estado", "Fecha evaluación"
    };

    public ArchivoReporteDto CrearExcelConsolidado(IReadOnlyList<RiesgoReporteFilaDto> filas)
    {
        using var output = new MemoryStream();
        using (var zip = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            AgregarEntrada(zip, "[Content_Types].xml", """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
                  <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
                  <Default Extension="xml" ContentType="application/xml"/>
                  <Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>
                  <Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>
                  <Override PartName="/xl/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"/>
                </Types>
                """);
            AgregarEntrada(zip, "_rels/.rels", """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
                  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/>
                </Relationships>
                """);
            AgregarEntrada(zip, "xl/workbook.xml", """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
                  <sheets><sheet name="Matriz Consolidada" sheetId="1" r:id="rId1"/></sheets>
                </workbook>
                """);
            AgregarEntrada(zip, "xl/_rels/workbook.xml.rels", """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
                  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/>
                  <Relationship Id="rId2" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/>
                </Relationships>
                """);
            AgregarEntrada(zip, "xl/styles.xml", """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <styleSheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
                  <fonts count="2"><font><sz val="10"/><name val="Calibri"/></font><font><b/><sz val="10"/><name val="Calibri"/></font></fonts>
                  <fills count="2"><fill><patternFill patternType="none"/></fill><fill><patternFill patternType="gray125"/></fill></fills>
                  <borders count="1"><border><left/><right/><top/><bottom/><diagonal/></border></borders>
                  <cellStyleXfs count="1"><xf numFmtId="0" fontId="0" fillId="0" borderId="0"/></cellStyleXfs>
                  <cellXfs count="2"><xf numFmtId="0" fontId="0" fillId="0" borderId="0" xfId="0"/><xf numFmtId="0" fontId="1" fillId="0" borderId="0" xfId="0" applyFont="1"/></cellXfs>
                  <cellStyles count="1"><cellStyle name="Normal" xfId="0" builtinId="0"/></cellStyles>
                </styleSheet>
                """);

            var sheet = new StringBuilder();
            sheet.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
            sheet.Append("<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">");
            sheet.Append("<sheetViews><sheetView workbookViewId=\"0\"><pane ySplit=\"1\" topLeftCell=\"A2\" activePane=\"bottomLeft\" state=\"frozen\"/></sheetView></sheetViews>");
            sheet.Append("<cols><col min=\"1\" max=\"4\" width=\"14\" customWidth=\"1\"/><col min=\"5\" max=\"6\" width=\"26\" customWidth=\"1\"/><col min=\"7\" max=\"13\" width=\"18\" customWidth=\"1\"/></cols>");
            sheet.Append("<sheetData>");
            sheet.Append("<row r=\"1\">");
            for (int i = 0; i < Encabezados.Length; i++)
                sheet.Append(CeldaTexto(i + 1, 1, Encabezados[i], 1));
            sheet.Append("</row>");

            for (int index = 0; index < filas.Count; index++)
            {
                RiesgoReporteFilaDto fila = filas[index];
                int row = index + 2;
                sheet.Append($"<row r=\"{row}\">");
                sheet.Append(CeldaNumero(1, row, fila.RiesgoId));
                sheet.Append(CeldaNumero(2, row, fila.EvaluacionId));
                sheet.Append(CeldaNumero(3, row, fila.VersionFormularioId));
                sheet.Append(CeldaTexto(4, row, fila.CodigoRiesgo));
                sheet.Append(CeldaTexto(5, row, fila.AreaPrincipal));
                sheet.Append(CeldaTexto(6, row, fila.DuenoRiesgo));
                sheet.Append(CeldaNumero(7, row, fila.Vri));
                sheet.Append(CeldaTexto(8, row, fila.NivelInherente));
                sheet.Append(CeldaNumero(9, row, fila.Vrr));
                sheet.Append(CeldaTexto(10, row, fila.NivelResidual));
                sheet.Append(CeldaTexto(11, row, fila.RespuestaRiesgo));
                sheet.Append(CeldaTexto(12, row, fila.EstadoEvaluacion));
                sheet.Append(CeldaTexto(13, row, fila.FechaEvaluacion.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)));
                sheet.Append("</row>");
            }
            sheet.Append("</sheetData><autoFilter ref=\"A1:M1\"/></worksheet>");
            AgregarEntrada(zip, "xl/worksheets/sheet1.xml", sheet.ToString());
        }

        return new ArchivoReporteDto(
            output.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"Matriz_Riesgos_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx");
    }

    public ArchivoReporteDto CrearPdfConsolidado(IReadOnlyList<RiesgoReporteFilaDto> filas)
    {
        const int filasPorPagina = 28;
        int paginas = Math.Max(1, (int)Math.Ceiling(filas.Count / (double)filasPorPagina));
        var objetos = new SortedDictionary<int, string>();
        const int catalogoId = 1;
        const int paginasId = 2;
        const int fuenteId = 3;
        objetos[fuenteId] = "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>";

        var referenciasPaginas = new List<int>();
        for (int pagina = 0; pagina < paginas; pagina++)
        {
            int paginaId = 4 + pagina * 2;
            int contenidoId = paginaId + 1;
            referenciasPaginas.Add(paginaId);
            List<string> lineas = ConstruirLineasPdf(filas, pagina, filasPorPagina, paginas);
            string stream = ConstruirStreamPdf(lineas);
            int longitud = Encoding.ASCII.GetByteCount(stream);
            objetos[contenidoId] = $"<< /Length {longitud} >>\nstream\n{stream}\nendstream";
            objetos[paginaId] = $"<< /Type /Page /Parent {paginasId} 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 {fuenteId} 0 R >> >> /Contents {contenidoId} 0 R >>";
        }

        objetos[paginasId] = $"<< /Type /Pages /Kids [{string.Join(' ', referenciasPaginas.Select(id => $"{id} 0 R"))}] /Count {paginas} >>";
        objetos[catalogoId] = $"<< /Type /Catalog /Pages {paginasId} 0 R >>";

        byte[] pdf = EnsamblarPdf(objetos, catalogoId);
        return new ArchivoReporteDto(pdf, "application/pdf", $"Matriz_Riesgos_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf");
    }

    private static List<string> ConstruirLineasPdf(IReadOnlyList<RiesgoReporteFilaDto> filas, int pagina, int filasPorPagina, int totalPaginas)
    {
        var lineas = new List<string>
        {
            "SGRLA - IHSS | Matriz Consolidada de Riesgos LA/FT",
            $"Generado UTC: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} | Pagina {pagina + 1}/{totalPaginas}",
            "Codigo | Area | Responsable | VRI/Nivel | VRR/Nivel | Respuesta | Estado",
            new string('-', 105)
        };
        int inicio = pagina * filasPorPagina;
        int fin = Math.Min(filas.Count, inicio + filasPorPagina);
        for (int i = inicio; i < fin; i++)
        {
            RiesgoReporteFilaDto f = filas[i];
            string linea = $"{f.CodigoRiesgo} | {f.AreaPrincipal} | {f.DuenoRiesgo} | {f.Vri}/{f.NivelInherente} | {f.Vrr}/{f.NivelResidual} | {f.RespuestaRiesgo} | {f.EstadoEvaluacion}";
            lineas.Add(Acortar(linea, 110));
        }
        if (filas.Count == 0) lineas.Add("Sin registros para mostrar.");
        return lineas;
    }

    private static string ConstruirStreamPdf(IEnumerable<string> lineas)
    {
        var sb = new StringBuilder();
        sb.Append("BT\n/F1 8 Tf\n36 806 Td\n11 TL\n");
        foreach (string linea in lineas)
            sb.Append('(').Append(EscapePdf(NormalizarAscii(linea))).Append(") Tj\nT*\n");
        sb.Append("ET");
        return sb.ToString();
    }

    private static byte[] EnsamblarPdf(SortedDictionary<int, string> objetos, int catalogoId)
    {
        using var ms = new MemoryStream();
        using var writer = new StreamWriter(ms, Encoding.ASCII, 1024, leaveOpen: true) { NewLine = "\n" };
        writer.Write("%PDF-1.4\n");
        writer.Flush();
        int maxId = objetos.Keys.Max();
        var offsets = new long[maxId + 1];
        foreach ((int id, string contenido) in objetos)
        {
            writer.Flush();
            offsets[id] = ms.Position;
            writer.Write($"{id} 0 obj\n{contenido}\nendobj\n");
        }
        writer.Flush();
        long xref = ms.Position;
        writer.Write($"xref\n0 {maxId + 1}\n");
        writer.Write("0000000000 65535 f \n");
        for (int id = 1; id <= maxId; id++) writer.Write($"{offsets[id]:D10} 00000 n \n");
        writer.Write($"trailer\n<< /Size {maxId + 1} /Root {catalogoId} 0 R >>\nstartxref\n{xref}\n%%EOF\n");
        writer.Flush();
        return ms.ToArray();
    }

    private static void AgregarEntrada(ZipArchive zip, string ruta, string contenido)
    {
        ZipArchiveEntry entry = zip.CreateEntry(ruta, CompressionLevel.Fastest);
        using Stream stream = entry.Open();
        using var writer = new StreamWriter(stream, new UTF8Encoding(false));
        writer.Write(contenido.Trim());
    }

    private static string CeldaTexto(int columna, int fila, string? valor, int estilo = 0) =>
        $"<c r=\"{ReferenciaCelda(columna, fila)}\" t=\"inlineStr\" s=\"{estilo}\"><is><t xml:space=\"preserve\">{Xml(valor ?? string.Empty)}</t></is></c>";

    private static string CeldaNumero(int columna, int fila, long valor) =>
        $"<c r=\"{ReferenciaCelda(columna, fila)}\"><v>{valor.ToString(CultureInfo.InvariantCulture)}</v></c>";

    private static string ReferenciaCelda(int columna, int fila)
    {
        var nombre = new StringBuilder();
        int valor = columna;
        while (valor > 0)
        {
            valor--;
            nombre.Insert(0, (char)('A' + valor % 26));
            valor /= 26;
        }
        return nombre.Append(fila).ToString();
    }

    private static string Xml(string valor) => SecurityElement.Escape(valor) ?? string.Empty;
    private static string EscapePdf(string valor) => valor.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
    private static string Acortar(string valor, int maximo) => valor.Length <= maximo ? valor : valor[..(maximo - 3)] + "...";

    private static string NormalizarAscii(string valor)
    {
        string normalizado = valor.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalizado.Length);
        foreach (char c in normalizado)
        {
            var categoria = CharUnicodeInfo.GetUnicodeCategory(c);
            if (categoria == UnicodeCategory.NonSpacingMark) continue;
            sb.Append(c <= 127 ? c : '?');
        }
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}
