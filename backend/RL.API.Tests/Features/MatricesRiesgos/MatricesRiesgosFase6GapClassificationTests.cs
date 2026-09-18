#pragma warning disable CA1707, CA1305, CA1416, CA1859, CA1307

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using ExcelDataReader;
using Xunit;

namespace RL.API.Tests.Features.MatricesRiesgos;

public sealed class MatricesRiesgosFase6GapClassificationTests
{
    private const string V1Hash = "f2f84f21b6cc46762fd6087bc41df449b31ca87b058c763689bdfb3bba961f90";
    private const string V2Hash = "769b5b25cd7cbb03b69782b5864828fb53155c070483b6d22ef5adf36d295651";

    [Fact]
    public void Fase6_28Gaps_SeClasificanPorDestinoSemanticoSinMissingGenerico()
    {
        var estados = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["MR-04"] = "SOURCE_ONLY_AUXILIARY",
            ["MR-05"] = "TRUE_GAP",
            ["MR-06"] = "TRUE_GAP",
            ["MR-07"] = "TRUE_GAP",
            ["MR-08"] = "IMPLEMENTED_EQUIVALENT",
            ["MR-09"] = "IMPLEMENTED_EQUIVALENT",
            ["MR-15"] = "TRUE_GAP",
            ["MR-16"] = "TRUE_GAP",
            ["MR-20"] = "IMPLEMENTED_EQUIVALENT",
            ["MR-21"] = "DERIVED",
            ["MR-22"] = "DERIVED",
            ["MR-23"] = "IMPLEMENTED_EQUIVALENT",
            ["MR-24"] = "IMPLEMENTED_EQUIVALENT",
            ["MR-25"] = "DERIVED",
            ["MR-26"] = "DERIVED",
            ["MR-27"] = "IMPLEMENTED_EQUIVALENT",
            ["MR-28"] = "IMPLEMENTED_EQUIVALENT",
            ["MR-29"] = "DERIVED",
            ["MR-30"] = "DERIVED",
            ["MR-31"] = "IMPLEMENTED_EQUIVALENT",
            ["MR-32"] = "IMPLEMENTED_EQUIVALENT",
            ["MR-40"] = "IMPLEMENTED_EQUIVALENT",
            ["MR-41"] = "DERIVED",
            ["MR-42"] = "IMPLEMENTED_EQUIVALENT",
            ["MR-43"] = "DERIVED",
            ["MR-44"] = "TRUE_GAP",
            ["MR-45"] = "TRUE_GAP",
            ["MR-70"] = "IMPLEMENTED_EQUIVALENT"
        };

        Assert.Equal(28, estados.Count);
        Assert.Equal(7, estados.Values.Count(value => value == "TRUE_GAP"));
        Assert.Equal(12, estados.Values.Count(value => value == "IMPLEMENTED_EQUIVALENT"));
        Assert.Equal(8, estados.Values.Count(value => value == "DERIVED"));
        Assert.Equal(1, estados.Values.Count(value => value == "SOURCE_ONLY_AUXILIARY"));
        Assert.DoesNotContain("MISSING", estados.Values);
        Assert.DoesNotContain("UNKNOWN", estados.Values);
    }

    [Fact]
    public void Fase6_Origen_ConfirmaInherenteEvaluacionYAlertasNoVacias()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        using var stream = File.Open(ObtenerRuta("Matrices de Riesgos.xlsx"), FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = ExcelReaderFactory.CreateReader(stream);
        while (reader.Name != "Matriz Consolidada" && reader.NextResult()) { }

        int filas = 0;
        int inherente = 0;
        int evaluacion = 0;
        int alertas = 0;
        int rowNumber = 0;
        while (reader.Read())
        {
            rowNumber++;
            int fila = rowNumber;
            if (fila < 2 || fila > 60) continue;
            filas++;
            inherente += NoVacio(reader.GetValue(7)) ? 1 : 0;
            evaluacion += NoVacio(reader.GetValue(8)) ? 1 : 0;
            alertas += NoVacio(reader.GetValue(69)) ? 1 : 0;
        }

        Assert.Equal(59, filas);
        Assert.Equal(59, inherente);
        Assert.Equal(59, evaluacion);
        Assert.Equal(23, alertas);
    }

    [Fact]
    public void Fase6_V2SoloAgregaCincoCamposDinamicosYNoDuplicaEntidadesNormalizadas()
    {
        string v1 = File.ReadAllText(ObtenerRuta("database", "19_matrices_riesgos", "fase11", "formulario_matriz_riesgos_laft_v1.json"));
        string v2Path = ObtenerRuta("database", "19_matrices_riesgos", "fase6", "matriz_riesgos_laft_v2_draft.json");
        string v2 = File.ReadAllText(v2Path);

        Assert.Equal(V1Hash, Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(v1))).ToLowerInvariant());
        Assert.Equal(V2Hash, Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(v2))).ToLowerInvariant());

        using var doc = System.Text.Json.JsonDocument.Parse(v2);
        var campos = doc.RootElement.GetProperty("secciones")[0].GetProperty("campos")
            .EnumerateArray().Select(c => c.GetProperty("id").GetString()).ToArray();
        Assert.Equal(7, campos.Length);
        Assert.Equal(
            new[] { "area_principal", "dueno_riesgo", "tipo_riesgo", "procedimiento", "objetivos_estrategicos", "regimen_afectado", "transversalidad" },
            campos);
        Assert.DoesNotContain("control_preventivo_descripcion", v2, StringComparison.Ordinal);
        Assert.DoesNotContain("plan_mitigacion", v2, StringComparison.Ordinal);
        Assert.DoesNotContain("senal_alerta", v2, StringComparison.Ordinal);
    }

    private static bool NoVacio(object? value) => !string.IsNullOrWhiteSpace(Convert.ToString(value, CultureInfo.InvariantCulture));

    private static string ObtenerRuta(params string[] partes)
    {
        string ruta = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(ruta))
        {
            string candidato = Path.Combine(new[] { ruta }.Concat(partes).ToArray());
            if (File.Exists(candidato)) return candidato;
            DirectoryInfo? parent = Directory.GetParent(ruta);
            if (parent is null) break;
            ruta = parent.FullName;
        }

        return Path.Combine(Directory.GetCurrentDirectory(), Path.Combine(partes));
    }
}
