using System.Collections;
using System.Globalization;
using System.IO.Compression;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using RL.API.Features.MatricesRiesgos.Application;
using RL.API.Features.MatricesRiesgos.Contracts;
using Xunit;

namespace RL.API.Tests.Features.MatricesRiesgos;

public sealed class MatricesRiesgosReportRendererTests
{
    [Fact]
    public void GeneraArchivosOficialesEstructuralmenteValidos()
    {
        var reporte = CrearReporte();
        var pdf = InvocarRenderer("ConstruirPdfEjecutivo", reporte);
        var excel = InvocarRenderer("ConstruirExcel", reporte);
        var ficha = InvocarRenderer("ConstruirFicha", CrearDetalleMatriz());

        ValidarPdf(pdf, "Reporte ejecutivo");
        ValidarExcel(excel);
        ValidarPdf(ficha, "Ficha individual");

        var outputDir = Environment.GetEnvironmentVariable("FASE12_5_5_OUTPUT_DIR");
        if (!string.IsNullOrWhiteSpace(outputDir))
        {
            Directory.CreateDirectory(outputDir);
            Escribir(outputDir, "reporte_ejecutivo_matrices.pdf", pdf.Contenido);
            Escribir(outputDir, "reporte_matrices.xlsx", excel.Contenido);
            Escribir(outputDir, "ficha_individual_matriz.pdf", ficha.Contenido);

            var archivos = Directory.GetFiles(outputDir)
                .Where(path => !path.EndsWith("manifest.json", StringComparison.OrdinalIgnoreCase))
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .Select(path => new
                {
                    archivo = Path.GetFileName(path),
                    tamano_bytes = new FileInfo(path).Length,
                    sha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant()
                })
                .ToArray();

            File.WriteAllText(
                Path.Combine(outputDir, "manifest.json"),
                JsonSerializer.Serialize(new
                {
                    fase = "12.5.5",
                    origen = "MatricesRiesgosReportRenderer",
                    archivos
                }, new JsonSerializerOptions { WriteIndented = true }),
                Encoding.UTF8);
        }
    }

    private static MatricesRiesgoReporteDto CrearReporte()
    {
        var reporte = new MatricesRiesgoReporteDto
        {
            FechaGeneracion = new DateTime(2026, 7, 22, 14, 30, 0, DateTimeKind.Local),
            Filtro = new MatrizRiesgoReporteFiltroDto
            {
                Estado = "APROBADA",
                SujetoTipo = "PROVEEDOR",
                NivelResidual = "ALTO",
                FechaInicio = new DateTime(2026, 1, 1),
                FechaFin = new DateTime(2026, 7, 22),
                Responsable = "Unidad de Cumplimiento"
            },
            Totales = new MatricesRiesgoReporteTotalesDto
            {
                TotalMatrices = 4,
                TotalCalculadas = 3,
                TotalSinCalculo = 1,
                TotalCerradas = 1,
                TotalAltoCritico = 2,
                TotalPlanAccionRequerido = 2,
                TotalPlanesVencidos = 1
            },
            PorFactor =
            {
                new MatrizRiesgoFactorReporteDto
                {
                    FactorId = 1,
                    FactorCodigo = "PROV",
                    FactorNombre = "Proveedores",
                    TotalMatrices = 4,
                    PromedioInherente = 4.25m,
                    PromedioResidual = 3.10m,
                    TotalAltoCritico = 2,
                    TotalPlanAccionRequerido = 2
                }
            },
            MapaTransicion =
            {
                new MatrizRiesgoMapaTransicionDto
                {
                    NivelInherente = "CRÍTICO",
                    NivelResidual = "ALTO",
                    Total = 2,
                    PromedioInherente = 4.75m,
                    PromedioResidual = 3.50m
                },
                new MatrizRiesgoMapaTransicionDto
                {
                    NivelInherente = "SIN_CALCULO",
                    NivelResidual = "SIN_CALCULO",
                    Total = 1,
                    PromedioInherente = 0m,
                    PromedioResidual = 0m
                }
            },
            PlanesAccion =
            {
                new MatrizRiesgoPlanAccionReporteDto { Estado = "PENDIENTE", Total = 1, Vencidos = 1 },
                new MatrizRiesgoPlanAccionReporteDto { Estado = "EN_PROCESO", Total = 1, Vencidos = 0 }
            }
        };

        AddListItem(reporte, "MatricesFiltradas", new Dictionary<string, object?>
        {
            ["MatrizId"] = 101L,
            ["NombreSujeto"] = "Proveedor Estratégico Centroamericano, S. A.",
            ["Documento"] = "0801-1999-123456",
            ["SujetoTipo"] = "PROVEEDOR",
            ["Estado"] = "APROBADA",
            ["PuntajeInherente"] = 4.75m,
            ["NivelInherente"] = "CRÍTICO",
            ["PuntajeResidual"] = 3.50m,
            ["NivelResidual"] = "ALTO",
            ["RequierePlanAccion"] = true,
            ["FechaEvaluacion"] = new DateTime(2026, 7, 20)
        });
        AddListItem(reporte, "MatricesFiltradas", new Dictionary<string, object?>
        {
            ["MatrizId"] = 102L,
            ["NombreSujeto"] = "Proveedor de Servicios Tecnológicos",
            ["Documento"] = "0801-2001-654321",
            ["SujetoTipo"] = "PROVEEDOR",
            ["Estado"] = "CERRADA",
            ["PuntajeInherente"] = 3.90m,
            ["NivelInherente"] = "ALTO",
            ["PuntajeResidual"] = 2.20m,
            ["NivelResidual"] = "MEDIO",
            ["RequierePlanAccion"] = false,
            ["FechaEvaluacion"] = new DateTime(2026, 7, 18)
        });
        AddListItem(reporte, "MatricesCriticas", new Dictionary<string, object?>
        {
            ["MatrizId"] = 101L,
            ["NombreSujeto"] = "Proveedor Estratégico Centroamericano, S. A.",
            ["Documento"] = "0801-1999-123456",
            ["SujetoTipo"] = "PROVEEDOR",
            ["Estado"] = "APROBADA",
            ["PuntajeInherente"] = 4.75m,
            ["NivelInherente"] = "CRÍTICO",
            ["PuntajeResidual"] = 3.50m,
            ["NivelResidual"] = "ALTO",
            ["RequierePlanAccion"] = true,
            ["FechaEvaluacion"] = new DateTime(2026, 7, 20)
        });

        return reporte;
    }

    private static object CrearDetalleMatriz()
    {
        var assembly = typeof(MatricesRiesgosAppService).Assembly;
        var type = assembly.GetType(
            "RL.API.Features.MatricesRiesgos.Contracts.MatrizRiesgoDetalleDto",
            throwOnError: true)!;
        var detalle = Activator.CreateInstance(type)!;

        SetProperties(detalle, new Dictionary<string, object?>
        {
            ["MatrizId"] = 101L,
            ["NombreSujeto"] = "Proveedor Estratégico Centroamericano, S. A.",
            ["SujetoTipo"] = "PROVEEDOR",
            ["Documento"] = "0801-1999-123456",
            ["SujetoIdExt"] = "PROV-101",
            ["Estado"] = "APROBADA",
            ["FechaEvaluacion"] = new DateTime(2026, 7, 20),
            ["ModeloVersion"] = "2026.1",
            ["OrigenDatos"] = "CAPTURA",
            ["PuntajeInherente"] = 4.75m,
            ["NivelInherente"] = "CRÍTICO",
            ["PuntajeResidual"] = 3.50m,
            ["NivelResidual"] = "ALTO",
            ["RequierePlanAccion"] = true
        });

        AddListItem(detalle, "Detalles", new Dictionary<string, object?>
        {
            ["FactorCodigo"] = "PROV",
            ["FactorNombre"] = "Proveedores",
            ["VariableCodigo"] = "PAIS",
            ["VariableNombre"] = "País de origen",
            ["ValorCapturado"] = "Honduras",
            ["Puntaje"] = 4m,
            ["PuntajePonderado"] = 1.20m,
            ["Justificacion"] = "Proveedor con operación regional y exposición transfronteriza controlada.",
            ["FuenteDato"] = "Expediente IHSS"
        });
        AddListItem(detalle, "Controles", new Dictionary<string, object?>
        {
            ["Nombre"] = "Validación documental trimestral",
            ["FactorCodigo"] = "PROV",
            ["EfectividadPct"] = 35m,
            ["Responsable"] = "Unidad de Cumplimiento",
            ["Estado"] = "ACTIVO",
            ["TieneEvidencia"] = true
        });
        AddListItem(detalle, "Resultados", new Dictionary<string, object?>
        {
            ["TipoResultado"] = "INSTITUCIONAL",
            ["VersionCalculo"] = 1,
            ["PuntajeInherente"] = 4.75m,
            ["NivelInherente"] = "CRÍTICO",
            ["MitigacionPct"] = 26.32m,
            ["PuntajeResidual"] = 3.50m,
            ["NivelResidual"] = "ALTO",
            ["RequierePlanAccion"] = true,
            ["FechaCalculo"] = new DateTime(2026, 7, 20),
            ["EsVigente"] = true
        });
        AddListItem(detalle, "PlanesAccion", new Dictionary<string, object?>
        {
            ["Actividad"] = "Actualizar debida diligencia ampliada",
            ["Responsable"] = "Unidad de Cumplimiento",
            ["Periodicidad"] = "Mensual",
            ["FechaInicio"] = new DateTime(2026, 7, 22),
            ["FechaFin"] = new DateTime(2026, 8, 22),
            ["Estado"] = "EN_PROCESO",
            ["Vencido"] = false
        });
        AddListItem(detalle, "Evidencias", new Dictionary<string, object?>
        {
            ["NombreOriginal"] = "informe_debida_diligencia.pdf",
            ["TipoMime"] = "application/pdf",
            ["TamanoBytes"] = 245760L,
            ["PlanId"] = 15L,
            ["ControlId"] = 9L,
            ["Activa"] = true,
            ["FechaCreacion"] = new DateTime(2026, 7, 21)
        });

        return detalle;
    }

    private static MatrizRiesgoExportacionDto InvocarRenderer(string methodName, object argument)
    {
        var renderer = typeof(MatricesRiesgosAppService).Assembly.GetType(
            "RL.API.Features.MatricesRiesgos.Application.MatricesRiesgosReportRenderer",
            throwOnError: true)!;
        var method = renderer.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)!;
        return (MatrizRiesgoExportacionDto)method.Invoke(null, new[] { argument })!;
    }

    private static void ValidarPdf(MatrizRiesgoExportacionDto archivo, string nombre)
    {
        Assert.Equal("application/pdf", archivo.ContentType);
        Assert.True(archivo.NombreArchivo.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase));
        Assert.True(archivo.Contenido.Length > 1500, $"{nombre} demasiado pequeño.");
        Assert.Equal("%PDF-", Encoding.ASCII.GetString(archivo.Contenido, 0, 5));
        var cola = Encoding.ASCII.GetString(archivo.Contenido, Math.Max(0, archivo.Contenido.Length - 256), Math.Min(256, archivo.Contenido.Length));
        Assert.Contains("%%EOF", cola);
    }

    private static void ValidarExcel(MatrizRiesgoExportacionDto archivo)
    {
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", archivo.ContentType);
        Assert.True(archivo.NombreArchivo.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase));
        Assert.True(archivo.Contenido.Length > 3000, "El libro Excel generado está vacío o incompleto.");

        using var stream = new MemoryStream(archivo.Contenido);
        using var zip = new ZipArchive(stream, ZipArchiveMode.Read);
        Assert.NotNull(zip.GetEntry("[Content_Types].xml"));
        Assert.NotNull(zip.GetEntry("xl/workbook.xml"));
        Assert.NotNull(zip.GetEntry("xl/styles.xml"));
        Assert.True(zip.Entries.Count(entry => entry.FullName.StartsWith("xl/worksheets/sheet", StringComparison.OrdinalIgnoreCase)) >= 6);

        using var reader = new StreamReader(zip.GetEntry("xl/workbook.xml")!.Open(), Encoding.UTF8);
        var workbookXml = reader.ReadToEnd();
        foreach (var hoja in new[] { "Resumen", "Matrices", "Factores", "Mapa transición", "Matrices críticas", "Planes" })
        {
            Assert.Contains(hoja, workbookXml);
        }

        foreach (var worksheet in zip.Entries.Where(entry =>
                     entry.FullName.StartsWith("xl/worksheets/sheet", StringComparison.OrdinalIgnoreCase)
                     && entry.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase)))
        {
            using var sheetReader = new StreamReader(worksheet.Open(), Encoding.UTF8);
            var sheetXml = sheetReader.ReadToEnd();
            Assert.Contains("<pageSetUpPr fitToPage=\"1\" autoPageBreaks=\"0\"/>", sheetXml);
            Assert.Contains("fitToWidth=\"1\"", sheetXml);

            var document = XDocument.Parse(sheetXml);
            var children = document.Root!.Elements().Select(element => element.Name.LocalName).ToList();
            var autoFilterIndex = children.IndexOf("autoFilter");
            var mergeCellsIndex = children.IndexOf("mergeCells");
            Assert.True(autoFilterIndex >= 0, $"{worksheet.FullName} no contiene autoFilter.");
            Assert.True(mergeCellsIndex >= 0, $"{worksheet.FullName} no contiene mergeCells.");
            Assert.True(autoFilterIndex < mergeCellsIndex,
                $"{worksheet.FullName} no respeta el orden OpenXML: autoFilter debe preceder a mergeCells.");
            Assert.NotEmpty(document.Root.Descendants().Where(element => element.Name.LocalName == "row"));
        }
    }

    private static void AddListItem(object owner, string propertyName, IReadOnlyDictionary<string, object?> values)
    {
        var property = owner.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        Assert.NotNull(property);
        var list = property!.GetValue(owner) as IList;
        Assert.NotNull(list);
        var elementType = property.PropertyType.GetGenericArguments().Single();
        var item = Activator.CreateInstance(elementType)!;
        SetProperties(item, values);
        list!.Add(item);
    }

    private static void SetProperties(object target, IReadOnlyDictionary<string, object?> values)
    {
        foreach (var (name, value) in values)
        {
            var property = target.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (property is null || !property.CanWrite) continue;
            property.SetValue(target, ConvertValue(value, property.PropertyType));
        }
    }

    private static object? ConvertValue(object? value, Type targetType)
    {
        if (value is null) return null;
        var effectiveType = Nullable.GetUnderlyingType(targetType) ?? targetType;
        if (effectiveType.IsInstanceOfType(value)) return value;
        if (effectiveType.IsEnum) return Enum.Parse(effectiveType, Convert.ToString(value, CultureInfo.InvariantCulture)!, true);
        return Convert.ChangeType(value, effectiveType, CultureInfo.InvariantCulture);
    }

    private static void Escribir(string outputDir, string fileName, byte[] content) =>
        File.WriteAllBytes(Path.Combine(outputDir, fileName), content);
}
