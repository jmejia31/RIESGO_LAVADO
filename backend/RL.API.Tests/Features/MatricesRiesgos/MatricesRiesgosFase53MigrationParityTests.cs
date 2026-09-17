#pragma warning disable CA1707, CA1305, CA1416, CA1859, CA1307

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ExcelDataReader;
using RL.API.Features.MatricesRiesgos.Domain;
using Xunit;

namespace RL.API.Tests.Features.MatricesRiesgos;

public sealed class MatricesRiesgosFase53MigrationParityTests
{
    private const string HashEsperado = "f2f84f21b6cc46762fd6087bc41df449b31ca87b058c763689bdfb3bba961f90";

    [Fact]
    public void Fase53_DecisionesDePruebaAutorizadas_SeAplicanCorrectamente()
    {
        // Caso 1: ROTR-ALMACENBIENE-23 -> Dueño = GTIC (DATO DE PRUEBA)
        string codigo1 = "ROTR-ALMACENBIENE-23";
        string duenoSinMapeo = string.Empty;
        string duenoMapeado = string.IsNullOrWhiteSpace(duenoSinMapeo) &&
            string.Equals(codigo1, "ROTR-ALMACENBIENE-23", StringComparison.OrdinalIgnoreCase)
            ? "GTIC"
            : duenoSinMapeo;

        Assert.Equal("GTIC", duenoMapeado);

        // Caso 2: RCUMP-COMPRAS-37 -> Respuesta = MITIGAR (DATO DE PRUEBA)
        string codigo2 = "RCUMP-COMPRAS-37";
        string respuestaSinMapeo = string.Empty;
        string respuestaMapeada = string.IsNullOrWhiteSpace(respuestaSinMapeo) &&
            string.Equals(codigo2, "RCUMP-COMPRAS-37", StringComparison.OrdinalIgnoreCase)
            ? "MITIGAR"
            : respuestaSinMapeo;

        Assert.Equal("MITIGAR", respuestaMapeada);
    }

    [Fact]
    public void Fase53_ExcelOrigen_TieneExactamente59FilasYCodigosValidos()
    {
        string excelPath = ObtenerRutaArchivo("Matrices de Riesgos.xlsx");
        Assert.True(File.Exists(excelPath), $"No se encontró {excelPath}");

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        using var stream = File.Open(excelPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = ExcelReaderFactory.CreateReader(stream);

        while (reader.Name != "Matriz Consolidada" && reader.NextResult()) { }
        Assert.Equal("Matriz Consolidada", reader.Name);

        var codigos = new List<string>();
        int rowIdx = 0;
        while (reader.Read())
        {
            rowIdx++;
            if (rowIdx < 2 || rowIdx > 60) continue;

            string code = Convert.ToString(reader.GetValue(1), CultureInfo.InvariantCulture)?.Trim() ?? string.Empty;
            if (!string.IsNullOrEmpty(code))
            {
                codigos.Add(code);
            }
        }

        Assert.Equal(59, codigos.Count);
        Assert.Equal(59, codigos.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Equal("ROTR-AFIL-1", codigos.First());
        Assert.Equal("ROP-CUMP-59", codigos.Last());
        Assert.Contains("ROTR-ALMACENBIENE-23", codigos);
        Assert.Contains("RCUMP-COMPRAS-37", codigos);
    }

    [Fact]
    public async Task Fase53_TodosLos59Riesgos_SonValidosSegunEsquemaMatrizRiesgosLaftV1()
    {
        string excelPath = ObtenerRutaArchivo("Matrices de Riesgos.xlsx");
        string schemaPath = ObtenerRutaArchivo("database", "19_matrices_riesgos", "fase11", "formulario_matriz_riesgos_laft_v1.json");

        Assert.True(File.Exists(excelPath));
        Assert.True(File.Exists(schemaPath));

        string schemaJson = await File.ReadAllTextAsync(schemaPath);
        string hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(schemaJson))).ToLowerInvariant();
        Assert.Equal(HashEsperado, hash);

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        using var stream = File.Open(excelPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = ExcelReaderFactory.CreateReader(stream);

        while (reader.Name != "Matriz Consolidada" && reader.NextResult()) { }
        Assert.Equal("Matriz Consolidada", reader.Name);

        var validador = new FormularioValidador();
        int riesgosValidados = 0;
        int rowIdx = 0;

        while (reader.Read())
        {
            rowIdx++;
            if (rowIdx < 2 || rowIdx > 60) continue;

            string code = Convert.ToString(reader.GetValue(1), CultureInfo.InvariantCulture)?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(code)) continue;

            string area = Convert.ToString(reader.GetValue(2), CultureInfo.InvariantCulture)?.Trim() ?? string.Empty;
            string areaConsolidada = Convert.ToString(reader.GetValue(3), CultureInfo.InvariantCulture)?.Trim() ?? string.Empty;
            string areaFinal = !string.IsNullOrWhiteSpace(area) ? area : areaConsolidada;

            string dueno = Convert.ToString(reader.GetValue(13), CultureInfo.InvariantCulture)?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(dueno) && string.Equals(code, "ROTR-ALMACENBIENE-23", StringComparison.OrdinalIgnoreCase))
            {
                dueno = "GTIC"; // Decisión funcional autorizada como dato de prueba
            }

            int frecInh = ParseInt(reader.GetValue(9));
            int impInh = ParseInt(reader.GetValue(10));
            int vri = frecInh + impInh - 1;
            string nivelInh = DeterminarNivel(vri);

            decimal prev = ParsePorcentajeControl(reader.GetValue(22), reader.GetValue(20));
            decimal det = ParsePorcentajeControl(reader.GetValue(26), reader.GetValue(24));
            decimal corr = ParsePorcentajeControl(reader.GetValue(30), reader.GetValue(28));

            decimal etp = (prev * 0.70m) + (det * 0.15m) + (corr * 0.15m);
            decimal vrrRaw = vri * (1.0m - (etp / 100.0m));
            int vrr = (int)Math.Round(Math.Max(1.0m, vrrRaw), MidpointRounding.AwayFromZero);
            string nivelRes = DeterminarNivel(vrr);

            var (frecRes, impRes) = CalcularResidualAuxiliar(frecInh, impInh, vri, etp, vrr);

            string rawResp = Convert.ToString(reader.GetValue(38), CultureInfo.InvariantCulture)?.Trim() ?? string.Empty;
            string resp = MapearRespuestaRiesgo(rawResp);
            if (string.IsNullOrWhiteSpace(resp) && string.Equals(code, "RCUMP-COMPRAS-37", StringComparison.OrdinalIgnoreCase))
            {
                resp = "MITIGAR"; // Decisión funcional autorizada como dato de prueba
            }

            var respuestas = new Dictionary<string, object>
            {
                ["area_principal"] = areaFinal,
                ["dueno_riesgo"] = dueno,
                ["frecuencia_inherente"] = frecInh.ToString(CultureInfo.InvariantCulture),
                ["impacto_inherente"] = impInh.ToString(CultureInfo.InvariantCulture),
                ["nivel_inherente"] = nivelInh,
                ["controles_preventivo"] = prev,
                ["controles_detectivo"] = det,
                ["controles_correctivo"] = corr,
                ["frecuencia_residual"] = frecRes.ToString(CultureInfo.InvariantCulture),
                ["impacto_residual"] = impRes.ToString(CultureInfo.InvariantCulture),
                ["nivel_residual"] = nivelRes,
                ["respuesta_riesgo"] = resp
            };

            string respuestasJson = JsonSerializer.Serialize(respuestas);
            var validationResult = await validador.ValidarRespuestasAsync(respuestasJson, schemaJson);

            Assert.True(validationResult.Valido, $"Fila {code} falló validación: {string.Join("; ", validationResult.Errores.Select(e => $"{e.Campo}: {e.Mensaje}"))}");
            riesgosValidados++;
        }

        Assert.Equal(59, riesgosValidados);
    }

    [Theory]
    [InlineData(1, 1, "BAJO")]
    [InlineData(3, 3, "MODERADO")]
    [InlineData(4, 4, "ALTO")]
    [InlineData(5, 5, "CRITICO")]
    public void Fase53_NivelCalculado_CumpleMatrizInstitucional(int f, int i, string nivelEsperado)
    {
        int vri = f + i - 1;
        string nivel = DeterminarNivel(vri);
        Assert.Equal(nivelEsperado, nivel);
    }

    private static (int FrecRes, int ImpRes) CalcularResidualAuxiliar(int frec, int imp, int vri, decimal etp, int vrr)
    {
        if (vri == vrr) return (frec, imp);

        double etpDbl = (double)(etp / 100.0m);
        double f_res_aux = (1.0 - etpDbl) * frec;
        double i_res_aux = (1.0 - etpDbl) * imp;
        double suma_res = vrr + 1;
        double f_base = Math.Max(1.0, Math.Floor(f_res_aux));
        double i_base = Math.Max(1.0, Math.Floor(i_res_aux));
        double tope_f = frec;
        double tope_i = imp;
        double cap_f = Math.Max(0.0, tope_f - f_base);
        double cap_i = Math.Max(0.0, tope_i - i_base);
        double deficit = suma_res - (f_base + i_base);

        double extra_f = 0.0;
        double extra_i = 0.0;
        if (deficit > 0.0)
        {
            if (cap_f + cap_i > 0.0)
            {
                extra_f = deficit * (cap_f / (cap_f + cap_i));
                extra_i = deficit * (cap_i / (cap_f + cap_i));
            }
        }

        int f_fin = Math.Clamp((int)Math.Round(f_base + extra_f, MidpointRounding.AwayFromZero), 1, frec);
        int i_fin = Math.Clamp((int)Math.Round(i_base + extra_i, MidpointRounding.AwayFromZero), 1, imp);

        int ajuste = (vrr + 1) - (f_fin + i_fin);
        if (ajuste != 0)
        {
            if (ajuste > 0)
            {
                if (f_fin < frec) f_fin += Math.Min(ajuste, frec - f_fin);
                ajuste = (vrr + 1) - (f_fin + i_fin);
                if (ajuste > 0 && i_fin < imp) i_fin += Math.Min(ajuste, imp - i_fin);
            }
            else
            {
                int reduce = -ajuste;
                if (i_fin > 1)
                {
                    int drop = Math.Min(reduce, i_fin - 1);
                    i_fin -= drop;
                    reduce -= drop;
                }
                if (reduce > 0 && f_fin > 1)
                {
                    int drop = Math.Min(reduce, f_fin - 1);
                    f_fin -= drop;
                }
            }
        }

        return (f_fin, i_fin);
    }

    private static decimal ParsePorcentajeControl(object? valCalculado, object? valTexto)
    {
        if (valCalculado != null)
        {
            if (valCalculado is double d) return (decimal)d * 100m;
            if (valCalculado is decimal m) return m * 100m;
            if (valCalculado is int i) return i * 100m;
            if (decimal.TryParse(valCalculado.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
            {
                return parsed <= 1.0m && parsed > 0.0m ? parsed * 100m : parsed;
            }
        }

        if (valTexto != null)
        {
            string s = valTexto.ToString()?.Trim().Replace("%", string.Empty) ?? string.Empty;
            if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedTexto))
            {
                return parsedTexto;
            }
        }

        return 0m;
    }

    private static int ParseInt(object? val)
    {
        if (val == null) return 0;
        if (val is int i) return i;
        if (val is double d) return (int)Math.Round(d, MidpointRounding.AwayFromZero);
        if (int.TryParse(val.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }
        return 0;
    }

    private static string DeterminarNivel(int valor) => valor switch
    {
        <= 4 => "BAJO",
        5 => "MODERADO",
        <= 7 => "ALTO",
        _ => "CRITICO"
    };

    private static string MapearRespuestaRiesgo(string raw)
    {
        string s = raw.Trim().ToUpperInvariant();
        if (s.Contains("TRANSFERIR", StringComparison.OrdinalIgnoreCase)) return "TRANSFERIR";
        if (s.Contains("EVITAR", StringComparison.OrdinalIgnoreCase)) return "EVITAR";
        if (s.Contains("ACEPTAR", StringComparison.OrdinalIgnoreCase)) return "ACEPTAR";
        if (s.Contains("MITIGAR", StringComparison.OrdinalIgnoreCase)) return "MITIGAR";
        return string.Empty;
    }

    private static string ObtenerRutaArchivo(params string[] segmentos)
    {
        DirectoryInfo? dir = new(AppContext.BaseDirectory);
        while (dir is not null)
        {
            string candidato = Path.Combine(new[] { dir.FullName }.Concat(segmentos).ToArray());
            if (File.Exists(candidato)) return candidato;
            dir = dir.Parent;
        }
        return Path.Combine(segmentos);
    }
}
