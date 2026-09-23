using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace RL.API.Features.MatricesRiesgos.Domain;

public static class FormularioBorradorPreparador
{
    public static string PrepararCamposAdministrables(string jsonConfig)
    {
        JsonNode? parsed = JsonNode.Parse(jsonConfig);
        if (parsed is not JsonObject root)
        {
            throw new JsonException("La definición del formulario debe ser un objeto JSON.");
        }

        JsonObject definicion = root["definicionFormulario"] as JsonObject ?? root;
        JsonArray? secciones = definicion["secciones"] as JsonArray;
        if (secciones is null)
        {
            return jsonConfig;
        }

        bool encontroAreaPrincipal = false;
        bool encontroDueno = false;

        foreach (JsonNode? seccionNode in secciones)
        {
            if (seccionNode is not JsonObject seccion || seccion["campos"] is not JsonArray campos) continue;

            foreach (JsonNode? campoNode in campos)
            {
                if (campoNode is not JsonObject campo) continue;
                string? clave = ObtenerTexto(campo, "clave")
                    ?? ObtenerTexto(campo, "rutaDatos")
                    ?? ObtenerTexto(campo, "identificador")
                    ?? ObtenerTexto(campo, "id");

                if (string.Equals(clave, "area_principal", StringComparison.OrdinalIgnoreCase))
                {
                    campo["tipo"] = "selector-catalogo";
                    campo["codigoCatalogo"] = "MR_AREA_PRINCIPAL";
                    campo["permiteValorManual"] = true;
                    encontroAreaPrincipal = true;
                }
                else if (string.Equals(clave, "dueno_riesgo", StringComparison.OrdinalIgnoreCase))
                {
                    campo["tipo"] = "selector-catalogo";
                    campo["codigoCatalogo"] = "MR_AREA_RESPONSABLE";
                    campo["permiteValorManual"] = true;
                    encontroDueno = true;
                }
            }
        }

        if (encontroAreaPrincipal)
        {
            AsegurarCatalogo(root, definicion, "MR_AREA_PRINCIPAL", "Áreas principales", Array.Empty<(string Codigo, string Valor)>());
        }

        if (encontroDueno)
        {
            AsegurarCatalogo(root, definicion, "MR_AREA_RESPONSABLE", "Áreas responsables", Array.Empty<(string Codigo, string Valor)>());
        }

        return root.ToJsonString();
    }

    private static void AsegurarCatalogo(
        JsonObject root,
        JsonObject definicion,
        string codigo,
        string nombre,
        IReadOnlyList<(string Codigo, string Valor)> elementosIniciales)
    {
        JsonObject contenedor = definicion;
        JsonNode? catalogosNode = definicion["catalogos"];
        if (catalogosNode is null && !ReferenceEquals(root, definicion) && root["catalogos"] is not null)
        {
            contenedor = root;
            catalogosNode = root["catalogos"];
        }

        JsonArray catalogos;
        if (catalogosNode is JsonArray existentes)
        {
            catalogos = existentes;
        }
        else if (catalogosNode is null)
        {
            catalogos = new JsonArray();
            contenedor["catalogos"] = catalogos;
        }
        else
        {
            return;
        }

        foreach (JsonNode? catalogoNode in catalogos)
        {
            if (catalogoNode is not JsonObject catalogo) continue;
            string? codigoExistente = ObtenerTexto(catalogo, "codigo") ?? ObtenerTexto(catalogo, "identificador");
            if (string.Equals(codigoExistente, codigo, StringComparison.OrdinalIgnoreCase)) return;
        }

        var elementos = new JsonArray();
        for (int i = 0; i < elementosIniciales.Count; i++)
        {
            (string codigoElemento, string valorElemento) = elementosIniciales[i];
            elementos.Add(new JsonObject
            {
                ["codigo"] = codigoElemento,
                ["valor"] = valorElemento,
                ["orden"] = i + 1
            });
        }

        catalogos.Add(new JsonObject
        {
            ["codigo"] = codigo,
            ["nombre"] = nombre,
            ["elementos"] = elementos
        });
    }

    private static string? ObtenerTexto(JsonObject objeto, string propiedad)
    {
        return objeto[propiedad] is JsonValue valor && valor.TryGetValue<string>(out string? texto)
            ? texto
            : null;
    }
}
