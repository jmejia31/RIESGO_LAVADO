using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using RL.API.Features.MatricesRiesgos.Domain;
using Xunit;

namespace RL.API.Tests.Features.MatricesRiesgos;

public sealed class MatricesRiesgosPhase11Block1ContractTests
{
    private const string FamiliaCodigo = "MATRIZ_RIESGOS_LAFT";
    private const string HashEsperado = "f2f84f21b6cc46762fd6087bc41df449b31ca87b058c763689bdfb3bba961f90";

    [Fact]
    public async Task FormularioOficial_AceptaPayloadCompletoQueConsumeElBackend()
    {
        string definicion = LeerArchivoRepositorio(
            "database",
            "19_matrices_riesgos",
            "fase11",
            "formulario_matriz_riesgos_laft_v1.json");

        string respuestas = JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["area_principal"] = "Cumplimiento",
            ["dueno_riesgo"] = "Responsable institucional",
            ["frecuencia_inherente"] = "3",
            ["impacto_inherente"] = "4",
            ["nivel_inherente"] = "ALTO",
            ["controles_preventivo"] = 70,
            ["controles_detectivo"] = 15,
            ["controles_correctivo"] = 15,
            ["frecuencia_residual"] = "2",
            ["impacto_residual"] = "3",
            ["nivel_residual"] = "MODERADO",
            ["respuesta_riesgo"] = "MITIGAR"
        });

        var validador = new FormularioValidador();
        FormularioValidationResult resultado =
            await validador.ValidarRespuestasAsync(respuestas, definicion);

        Assert.True(
            resultado.Valido,
            string.Join(" | ", resultado.Errores.Select(error => $"{error.Campo}: {error.Mensaje}")));
    }

    [Fact]
    public void DefinicionOficial_ContieneCamposCatalogosYReglaQueConsumeElModulo()
    {
        string definicion = LeerArchivoRepositorio(
            "database",
            "19_matrices_riesgos",
            "fase11",
            "formulario_matriz_riesgos_laft_v1.json");

        using JsonDocument document = JsonDocument.Parse(definicion);
        JsonElement root = document.RootElement;
        Assert.Equal(FamiliaCodigo, root.GetProperty("codigoFormulario").GetString());

        string[] campos = root.GetProperty("secciones")
            .EnumerateArray()
            .SelectMany(seccion => seccion.GetProperty("campos").EnumerateArray())
            .Select(campo => campo.GetProperty("id").GetString()!)
            .ToArray();

        string[] esperados =
        {
            "area_principal",
            "dueno_riesgo",
            "frecuencia_inherente",
            "impacto_inherente",
            "nivel_inherente",
            "controles_preventivo",
            "controles_detectivo",
            "controles_correctivo",
            "frecuencia_residual",
            "impacto_residual",
            "nivel_residual",
            "respuesta_riesgo"
        };

        Assert.Equal(esperados.OrderBy(valor => valor), campos.OrderBy(valor => valor));
        Assert.Equal(campos.Length, campos.Distinct(StringComparer.OrdinalIgnoreCase).Count());

        Dictionary<string, int> catalogos = root.GetProperty("catalogos")
            .EnumerateArray()
            .ToDictionary(
                catalogo => catalogo.GetProperty("codigo").GetString()!,
                catalogo => catalogo.GetProperty("elementos").GetArrayLength(),
                StringComparer.OrdinalIgnoreCase);

        Assert.Equal(5, catalogos["MR_FRECUENCIA_1_5"]);
        Assert.Equal(5, catalogos["MR_IMPACTO_1_5"]);
        Assert.Equal(4, catalogos["MR_NIVEL_RIESGO"]);
        Assert.Equal(4, catalogos["MR_RESPUESTA_RIESGO"]);

        JsonElement regla = root.GetProperty("reglas").EnumerateArray().Single();
        Assert.Equal("CALCULO_VRI_VRR", regla.GetProperty("codigo").GetString());
        Assert.Equal("1.0", regla.GetProperty("version").GetString());
        Assert.Equal("MATRICES_VRI_ADITIVO_1_9", regla.GetProperty("algoritmoId").GetString());
    }

    [Fact]
    public void ScriptIdempotente_EmbebeJsonExactoHashRealYNoContieneOperacionesDestructivas()
    {
        string definicion = LeerArchivoRepositorio(
            "database",
            "19_matrices_riesgos",
            "fase11",
            "formulario_matriz_riesgos_laft_v1.json");
        string script = LeerArchivoRepositorio(
            "database",
            "19_matrices_riesgos",
            "fase11",
            "01_semillas_datos_iniciales_modelo_17_tablas.sql");

        string hash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(definicion)))
            .ToLowerInvariant();

        Assert.Equal(HashEsperado, hash);
        Assert.Contains(HashEsperado, script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(definicion, script, StringComparison.Ordinal);
        Assert.DoesNotContain("DROP TABLE", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("TRUNCATE", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DELETE FROM", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("B10_", script, StringComparison.OrdinalIgnoreCase);
    }

    private static string LeerArchivoRepositorio(params string[] segmentos)
    {
        DirectoryInfo? directorio = new(AppContext.BaseDirectory);
        while (directorio is not null)
        {
            string candidato = Path.Combine(new[] { directorio.FullName }.Concat(segmentos).ToArray());
            if (File.Exists(candidato))
            {
                return File.ReadAllText(candidato, new UTF8Encoding(false));
            }
            directorio = directorio.Parent;
        }

        throw new FileNotFoundException($"No se encontró el archivo de repositorio: {string.Join('/', segmentos)}");
    }
}
