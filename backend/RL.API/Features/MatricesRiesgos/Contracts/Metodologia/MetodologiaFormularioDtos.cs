using System.Text.Json;

namespace RL.API.Features.MatricesRiesgos.Contracts;

/// <summary>
/// Contrato neutro de la metodología dinámica asociada a una versión publicada y vigente.
/// No reutiliza conceptos del modelo retirado de modelos, factores, variables, escalas o criterios.
/// </summary>
public sealed class MetodologiaFormularioDto
{
    public long VersionFormularioId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public int Version { get; set; }
    public IReadOnlyList<SeccionFormularioDto> Secciones { get; set; } = Array.Empty<SeccionFormularioDto>();
    public IReadOnlyList<CatalogoMatricesDto> Catalogos { get; set; } = Array.Empty<CatalogoMatricesDto>();
    public IReadOnlyList<ReglaCalculoMatricesDto> Reglas { get; set; } = Array.Empty<ReglaCalculoMatricesDto>();
}

public sealed class SeccionFormularioDto
{
    public string Clave { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public int Orden { get; set; }
    public IReadOnlyList<CampoFormularioDto> Campos { get; set; } = Array.Empty<CampoFormularioDto>();
}

public sealed class CampoFormularioDto
{
    public long? CampoCanonicoId { get; set; }
    public string Clave { get; set; } = string.Empty;
    public string Etiqueta { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string? CodigoCatalogo { get; set; }
    public bool Obligatorio { get; set; }
    public bool SoloLectura { get; set; }
}

public sealed class CatalogoMatricesDto
{
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public IReadOnlyList<ElementoCatalogoMatricesDto> Elementos { get; set; } = Array.Empty<ElementoCatalogoMatricesDto>();
}

public sealed class ElementoCatalogoMatricesDto
{
    public string Codigo { get; set; } = string.Empty;
    public string Valor { get; set; } = string.Empty;
    public int Orden { get; set; }
}

public sealed class ReglaCalculoMatricesDto
{
    public string Codigo { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string AlgoritmoId { get; set; } = string.Empty;
    public JsonElement? Parametros { get; set; }
}
