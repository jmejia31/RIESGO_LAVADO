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

        // Si no hay respuestas y la configuración tiene campos obligatorios, validar nulidad total
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

            // 1. Validar campos sucios (propiedades del payload de entrada no declaradas en la plantilla)
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

                bool existe = respuestasDict.TryGetValue(campoId, out JsonElement valorElemento);
                bool esNuloOVacio = !existe || valorElemento.ValueKind == JsonValueKind.Null || 
                                    (valorElemento.ValueKind == JsonValueKind.String && string.IsNullOrWhiteSpace(valorElemento.GetString()));

                // A. Validar obligatoriedad
                if (metadatos.Obligatorio && esNuloOVacio)
                {
                    result.Errores.Add(new FormularioValidationError(campoId, $"El campo '{metadatos.Etiqueta}' es obligatorio."));
                    continue;
                }

                // Si no existe o es vacío y no es obligatorio, no se aplican validaciones adicionales de tipo/regex
                if (esNuloOVacio)
                {
                    continue;
                }

                // B. Validar tipo de datos y formato
                switch (metadatos.Tipo.ToLower())
                {
                    case "numero":
                    case "numérico":
                        if (valorElemento.ValueKind != JsonValueKind.Number && 
                            !(valorElemento.ValueKind == JsonValueKind.String && double.TryParse(valorElemento.GetString(), out _)))
                        {
                            result.Errores.Add(new FormularioValidationError(campoId, $"El campo '{metadatos.Etiqueta}' debe ser un valor numérico."));
                        }
                        break;

                    case "selector-catalogo":
                    case "texto":
                    case "texto-largo":
                        if (valorElemento.ValueKind != JsonValueKind.String)
                        {
                            result.Errores.Add(new FormularioValidationError(campoId, $"El campo '{metadatos.Etiqueta}' debe ser de tipo texto."));
                        }
                        break;

                    case "catalogo":
                        if (valorElemento.ValueKind != JsonValueKind.Number &&
                            !(valorElemento.ValueKind == JsonValueKind.String && long.TryParse(valorElemento.GetString(), out _)))
                        {
                            result.Errores.Add(new FormularioValidationError(campoId, $"El campo '{metadatos.Etiqueta}' debe ser un valor de catálogo (entero)."));
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
                                if (element.ValueKind != JsonValueKind.Number &&
                                    !(element.ValueKind == JsonValueKind.String && long.TryParse(element.GetString(), out _)))
                                {
                                    result.Errores.Add(new FormularioValidationError(campoId, $"Todos los elementos del campo '{metadatos.Etiqueta}' deben ser números enteros."));
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
                        // Expresión regular inválida definida en plantilla, se registra error de sistema
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

    private static Dictionary<string, CampoMetadatos> ObtenerCamposDefinidos(JsonElement rootElement)
    {
        var campos = new Dictionary<string, CampoMetadatos>(StringComparer.OrdinalIgnoreCase);

        if (rootElement.ValueKind != JsonValueKind.Object || !rootElement.TryGetProperty("secciones", out JsonElement seccionesElement) || seccionesElement.ValueKind != JsonValueKind.Array)
        {
            return campos;
        }

        foreach (var seccion in seccionesElement.EnumerateArray())
        {
            if (seccion.ValueKind == JsonValueKind.Object && seccion.TryGetProperty("campos", out JsonElement camposElement) && camposElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var campo in camposElement.EnumerateArray())
                {
                    if (campo.ValueKind == JsonValueKind.Object && campo.TryGetProperty("id", out JsonElement idProp) && idProp.ValueKind == JsonValueKind.String)
                    {
                        string id = idProp.GetString() ?? string.Empty;
                        if (string.IsNullOrWhiteSpace(id)) continue;

                        string etiqueta = campo.TryGetProperty("etiqueta", out JsonElement etProp) && etProp.ValueKind == JsonValueKind.String ? etProp.GetString() ?? id : id;
                        string tipo = campo.TryGetProperty("tipo", out JsonElement tipoProp) && tipoProp.ValueKind == JsonValueKind.String ? tipoProp.GetString() ?? "texto" : "texto";
                        
                        bool obligatorio = campo.TryGetProperty("obligatorio", out JsonElement oblProp) && oblProp.ValueKind == JsonValueKind.True;
                        
                        string regex = campo.TryGetProperty("regexValidacion", out JsonElement regProp) && regProp.ValueKind == JsonValueKind.String 
                            ? regProp.GetString() ?? string.Empty 
                            : (campo.TryGetProperty("expresionValidacion", out JsonElement expProp) && expProp.ValueKind == JsonValueKind.String ? expProp.GetString() ?? string.Empty : string.Empty);

                        campos[id] = new CampoMetadatos(id, etiqueta, tipo, obligatorio, regex);
                    }
                }
            }
        }

        return campos;
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

        public CampoMetadatos(string id, string etiqueta, string tipo, bool obligatorio, string regexValidacion)
        {
            Id = id;
            Etiqueta = etiqueta;
            Tipo = tipo;
            Obligatorio = obligatorio;
            RegexValidacion = regexValidacion;
        }
    }
}
