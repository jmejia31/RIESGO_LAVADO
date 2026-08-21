using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RL.API.Features.MatricesRiesgos.Domain;

public sealed class FormularioValidador : IFormularioValidador
{
    public Task<FormularioValidationResult> ValidarRespuestasAsync(string jsonRespuestas, string jsonConfigFormulario)
    {
        var result = new FormularioValidationResult();

        if (string.IsNullOrWhiteSpace(jsonConfigFormulario))
        {
            result.Errores.Add(new FormularioValidationError("Configuracion", "La configuración del formulario es requerida."));
            return Task.FromResult(result);
        }

        if (string.IsNullOrWhiteSpace(jsonRespuestas))
        {
            jsonRespuestas = "{}";
        }

        try
        {
            using var configDoc = JsonDocument.Parse(jsonConfigFormulario);
            using var respuestasDoc = JsonDocument.Parse(jsonRespuestas);

            var camposDefinidos = ObtenerCamposDefinidos(configDoc.RootElement);
            var respuestasDict = ObtenerRespuestas(respuestasDoc.RootElement);

            // 1. Validar campos sucios (propiedades del payload de entrada no declaradas en la plantilla ni por su clave canónica ni aliases)
            foreach (var key in respuestasDict.Keys)
            {
                if (!camposDefinidos.ContainsKey(key))
                {
                    result.Errores.Add(new FormularioValidationError(key, $"La propiedad '{key}' no está definida en la estructura del formulario."));
                }
            }

            // 2. Validar cada campo definido en la plantilla
            foreach (var kvp in camposDefinidos)
            {
                string campoId = kvp.Key;
                var metadatos = kvp.Value;

                bool existe = BuscarValorEnRespuestas(respuestasDict, metadatos, out JsonElement valorElemento);
                bool esNuloOVacio = !existe || valorElemento.ValueKind == JsonValueKind.Null || 
                                    (valorElemento.ValueKind == JsonValueKind.String && string.IsNullOrWhiteSpace(valorElemento.GetString()));

                // A. Validar obligatoriedad
                if (metadatos.Obligatorio && esNuloOVacio)
                {
                    result.Errores.Add(new FormularioValidationError(campoId, $"El campo '{metadatos.Etiqueta}' es obligatorio."));
                    continue;
                }

                if (esNuloOVacio)
                {
                    continue;
                }

                // B. Validar tipo de datos y formato
                switch (metadatos.Tipo.ToLowerInvariant())
                {
                    case "numero":
                    case "numérico":
                        if (valorElemento.ValueKind != JsonValueKind.Number && 
                            !(valorElemento.ValueKind == JsonValueKind.String && double.TryParse(valorElemento.GetString(), out _)))
                        {
                            result.Errores.Add(new FormularioValidationError(campoId, $"El campo '{metadatos.Etiqueta}' debe ser un valor numérico."));
                        }
                        break;

                    case "texto":
                    case "texto-largo":
                        if (valorElemento.ValueKind != JsonValueKind.String)
                        {
                            result.Errores.Add(new FormularioValidationError(campoId, $"El campo '{metadatos.Etiqueta}' debe ser de tipo texto."));
                        }
                        break;

                    case "selector-catalogo":
                    case "catalogo":
                        if (valorElemento.ValueKind != JsonValueKind.String && valorElemento.ValueKind != JsonValueKind.Number)
                        {
                            result.Errores.Add(new FormularioValidationError(campoId, $"El campo '{metadatos.Etiqueta}' debe ser de tipo texto o número."));
                        }
                        else
                        {
                            string codigo = valorElemento.ValueKind == JsonValueKind.Number
                                ? valorElemento.GetRawText()
                                : valorElemento.GetString() ?? string.Empty;

                            if (metadatos.OpcionesCatalogo.Count > 0 && !metadatos.OpcionesCatalogo.Contains(codigo))
                            {
                                result.Errores.Add(new FormularioValidationError(campoId, $"El valor '{codigo}' no corresponde a un código válido del catálogo para el campo '{metadatos.Etiqueta}'."));
                            }
                        }
                        break;

                    case "catalogo-multiple":
                        if (valorElemento.ValueKind != JsonValueKind.Array)
                        {
                            result.Errores.Add(new FormularioValidationError(campoId, $"El campo '{metadatos.Etiqueta}' debe ser una lista de enteros (catálogo múltiple)."));
                        }
                        else
                        {
                            foreach (var element in valorElemento.EnumerateArray())
                            {
                                bool esEntero = element.ValueKind == JsonValueKind.Number ||
                                                (element.ValueKind == JsonValueKind.String && long.TryParse(element.GetString(), out _));

                                if (metadatos.OpcionesCatalogo.Count == 0 && !esEntero)
                                {
                                    result.Errores.Add(new FormularioValidationError(campoId, $"Todos los elementos del campo '{metadatos.Etiqueta}' deben ser números enteros."));
                                    break;
                                }

                                string codigo = element.ValueKind == JsonValueKind.Number ? element.GetRawText() : element.GetString() ?? string.Empty;
                                if (metadatos.OpcionesCatalogo.Count > 0 && !metadatos.OpcionesCatalogo.Contains(codigo))
                                {
                                    result.Errores.Add(new FormularioValidationError(campoId, $"El valor '{codigo}' en el campo '{metadatos.Etiqueta}' no corresponde a un código válido del catálogo."));
                                    break;
                                }
                            }
                        }
                        break;
                }

                // C. Validar Expresión Regular (si aplica)
                if (!string.IsNullOrWhiteSpace(metadatos.RegexValidacion) && valorElemento.ValueKind == JsonValueKind.String)
                {
                    string valorTexto = valorElemento.GetString() ?? string.Empty;
                    try
                    {
                        if (!Regex.IsMatch(valorTexto, metadatos.RegexValidacion, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase))
                        {
                            result.Errores.Add(new FormularioValidationError(campoId, $"El valor del campo '{metadatos.Etiqueta}' no cumple con el formato requerido."));
                        }
                    }
                    catch (ArgumentException)
                    {
                        result.Errores.Add(new FormularioValidationError(campoId, $"Expresión regular de validación inválida configurada para el campo '{metadatos.Etiqueta}'."));
                    }
                }
            }
        }
        catch (JsonException ex)
        {
            result.Errores.Add(new FormularioValidationError("JSON", $"Error en el formato de los datos JSON: {ex.Message}"));
        }

        return Task.FromResult(result);
    }

    private static bool BuscarValorEnRespuestas(
        Dictionary<string, JsonElement> respuestas,
        CampoMetadatos metadatos,
        out JsonElement valorElemento)
    {
        if (respuestas.TryGetValue(metadatos.Id, out valorElemento)) return true;
        foreach (string alias in metadatos.Aliases)
        {
            if (respuestas.TryGetValue(alias, out valorElemento)) return true;
        }
        valorElemento = default;
        return false;
    }

    private static Dictionary<string, CampoMetadatos> ObtenerCamposDefinidos(JsonElement rootElement)
    {
        var campos = new Dictionary<string, CampoMetadatos>(StringComparer.OrdinalIgnoreCase);

        if (rootElement.ValueKind != JsonValueKind.Object || !rootElement.TryGetProperty("secciones", out JsonElement seccionesElement) || seccionesElement.ValueKind != JsonValueKind.Array)
        {
            return campos;
        }

        var catalogosRaiz = ExtraerCatalogosRaiz(rootElement);

        foreach (var seccion in seccionesElement.EnumerateArray())
        {
            if (seccion.ValueKind == JsonValueKind.Object && seccion.TryGetProperty("campos", out JsonElement camposElement) && camposElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var campo in camposElement.EnumerateArray())
                {
                    if (campo.ValueKind != JsonValueKind.Object) continue;

                    string id = ObtenerPropiedadString(campo, "clave") ??
                               ObtenerPropiedadString(campo, "rutaDatos") ??
                               ObtenerPropiedadString(campo, "identificador") ??
                               ObtenerPropiedadString(campo, "id") ?? string.Empty;

                    if (string.IsNullOrWhiteSpace(id)) continue;

                    var aliases = new List<string>();
                    AgregarAliasSiDifiere(aliases, id, ObtenerPropiedadString(campo, "clave"));
                    AgregarAliasSiDifiere(aliases, id, ObtenerPropiedadString(campo, "rutaDatos"));
                    AgregarAliasSiDifiere(aliases, id, ObtenerPropiedadString(campo, "identificador"));
                    AgregarAliasSiDifiere(aliases, id, ObtenerPropiedadString(campo, "id"));

                    string etiqueta = ObtenerPropiedadString(campo, "etiqueta") ?? id;
                    string tipo = ObtenerPropiedadString(campo, "tipo") ?? "texto";
                    bool obligatorio = campo.TryGetProperty("obligatorio", out JsonElement oblProp) && oblProp.ValueKind == JsonValueKind.True;
                    string regex = ObtenerPropiedadString(campo, "regexValidacion") ?? ObtenerPropiedadString(campo, "expresionValidacion") ?? string.Empty;

                    var opciones = ExtraerCodicesOpciones(campo, catalogosRaiz);

                    var metadatos = new CampoMetadatos(id, etiqueta, tipo, obligatorio, regex, aliases, opciones);
                    campos[id] = metadatos;
                    foreach (string alias in aliases)
                    {
                        campos[alias] = metadatos;
                    }
                }
            }
        }

        return campos;
    }

    private static Dictionary<string, HashSet<string>> ExtraerCatalogosRaiz(JsonElement rootElement)
    {
        var catalogosMap = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        if (rootElement.TryGetProperty("catalogos", out JsonElement catalogosProp) && catalogosProp.ValueKind == JsonValueKind.Array)
        {
            foreach (var cat in catalogosProp.EnumerateArray())
            {
                if (cat.ValueKind != JsonValueKind.Object) continue;

                string? codigoCat = ObtenerPropiedadString(cat, "codigo") ?? ObtenerPropiedadString(cat, "id");
                if (string.IsNullOrWhiteSpace(codigoCat)) continue;

                var codigos = new HashSet<string>(StringComparer.Ordinal);

                if (cat.TryGetProperty("elementos", out JsonElement elemProp) && elemProp.ValueKind == JsonValueKind.Array)
                {
                    foreach (var elem in elemProp.EnumerateArray())
                    {
                        if (elem.ValueKind == JsonValueKind.Object)
                        {
                            string? codigo = ObtenerPropiedadString(elem, "codigo") ?? ObtenerPropiedadString(elem, "value") ?? ObtenerPropiedadString(elem, "id");
                            if (codigo is not null)
                            {
                                codigos.Add(codigo);
                            }
                        }
                        else if (elem.ValueKind == JsonValueKind.String)
                        {
                            codigos.Add(elem.GetString()!);
                        }
                        else if (elem.ValueKind == JsonValueKind.Number)
                        {
                            codigos.Add(elem.GetRawText());
                        }
                    }
                }
                else if (cat.TryGetProperty("opciones", out JsonElement opProp) && opProp.ValueKind == JsonValueKind.Array)
                {
                    foreach (var op in opProp.EnumerateArray())
                    {
                        if (op.ValueKind == JsonValueKind.Object)
                        {
                            string? codigo = ObtenerPropiedadString(op, "codigo") ?? ObtenerPropiedadString(op, "value") ?? ObtenerPropiedadString(op, "id");
                            if (codigo is not null)
                            {
                                codigos.Add(codigo);
                            }
                        }
                        else if (op.ValueKind == JsonValueKind.String)
                        {
                            codigos.Add(op.GetString()!);
                        }
                        else if (op.ValueKind == JsonValueKind.Number)
                        {
                            codigos.Add(op.GetRawText());
                        }
                    }
                }

                catalogosMap[codigoCat] = codigos;
            }
        }

        return catalogosMap;
    }

    private static string? ObtenerPropiedadString(JsonElement element, string propName)
    {
        if (element.TryGetProperty(propName, out JsonElement prop) && prop.ValueKind == JsonValueKind.String)
        {
            return prop.GetString();
        }
        return null;
    }

    private static void AgregarAliasSiDifiere(List<string> aliases, string idPrincipal, string? aliasCandidato)
    {
        if (!string.IsNullOrWhiteSpace(aliasCandidato) &&
            !string.Equals(idPrincipal, aliasCandidato, StringComparison.OrdinalIgnoreCase) &&
            !aliases.Contains(aliasCandidato, StringComparer.OrdinalIgnoreCase))
        {
            aliases.Add(aliasCandidato);
        }
    }

    private static HashSet<string> ExtraerCodicesOpciones(JsonElement campo, Dictionary<string, HashSet<string>> catalogosRaiz)
    {
        var codigos = new HashSet<string>(StringComparer.Ordinal);
        if (campo.TryGetProperty("opciones", out JsonElement opcionesProp) && opcionesProp.ValueKind == JsonValueKind.Array)
        {
            foreach (var op in opcionesProp.EnumerateArray())
            {
                if (op.ValueKind == JsonValueKind.Object)
                {
                    string? codigo = ObtenerPropiedadString(op, "codigo") ?? ObtenerPropiedadString(op, "value") ?? ObtenerPropiedadString(op, "id");
                    if (codigo is not null)
                    {
                        codigos.Add(codigo);
                    }
                }
                else if (op.ValueKind == JsonValueKind.String)
                {
                    codigos.Add(op.GetString()!);
                }
                else if (op.ValueKind == JsonValueKind.Number)
                {
                    codigos.Add(op.GetRawText());
                }
            }
        }

        string? codigoCatalogoRef = ObtenerPropiedadString(campo, "codigoCatalogo") ?? ObtenerPropiedadString(campo, "catalogoId");
        if (!string.IsNullOrWhiteSpace(codigoCatalogoRef) && catalogosRaiz.TryGetValue(codigoCatalogoRef, out var opcionesReferenciadas))
        {
            foreach (var cod in opcionesReferenciadas)
            {
                codigos.Add(cod);
            }
        }

        return codigos;
    }

    private static Dictionary<string, JsonElement> ObtenerRespuestas(JsonElement rootElement)
    {
        var respuestas = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);

        if (rootElement.ValueKind != JsonValueKind.Object)
        {
            return respuestas;
        }

        foreach (var prop in rootElement.EnumerateObject())
        {
            respuestas[prop.Name] = prop.Value;
        }

        return respuestas;
    }

    private sealed class CampoMetadatos
    {
        public string Id { get; }
        public string Etiqueta { get; }
        public string Tipo { get; }
        public bool Obligatorio { get; }
        public string RegexValidacion { get; }
        public List<string> Aliases { get; }
        public HashSet<string> OpcionesCatalogo { get; }

        public CampoMetadatos(
            string id,
            string etiqueta,
            string tipo,
            bool obligatorio,
            string regexValidacion,
            List<string> aliases,
            HashSet<string> opcionesCatalogo)
        {
            Id = id;
            Etiqueta = etiqueta;
            Tipo = tipo;
            Obligatorio = obligatorio;
            RegexValidacion = regexValidacion;
            Aliases = aliases;
            OpcionesCatalogo = opcionesCatalogo;
        }
    }
}
