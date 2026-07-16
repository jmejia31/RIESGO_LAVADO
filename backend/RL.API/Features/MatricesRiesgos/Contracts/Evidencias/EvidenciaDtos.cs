using Newtonsoft.Json;
using Microsoft.AspNetCore.Http;

namespace RL.API.Features.MatricesRiesgos.Contracts;

public sealed class MatrizRiesgoEvidenciaDto
{
    public long EvidenciaId { get; set; }
    public long MatrizId { get; set; }
    public long? ControlId { get; set; }
    public long? PlanId { get; set; }
    public string NombreOriginal { get; set; } = string.Empty;
    public string NombreFisico { get; set; } = string.Empty;
    public string? TipoMime { get; set; }
    public string? Extension { get; set; }
    public long TamanoBytes { get; set; }
    [JsonIgnore]
    public string RutaFisica { get; set; } = string.Empty;
    public string? HashSha256 { get; set; }
    public bool Activa { get; set; }
    public string? MotivoInactivo { get; set; }
    public DateTime FechaCreacion { get; set; }
}

public sealed class MatrizRiesgoEvidenciaRegistroDto
{
    public long MatrizId { get; set; }
    public long? ControlId { get; set; }
    public long? PlanId { get; set; }
    public string NombreOriginal { get; set; } = string.Empty;
    public string NombreFisico { get; set; } = string.Empty;
    public string? TipoMime { get; set; }
    public string? Extension { get; set; }
    public long TamanoBytes { get; set; }
    public string RutaFisica { get; set; } = string.Empty;
    public string? HashSha256 { get; set; }
}

public sealed class MatrizRiesgoEvidenciaDescargaDto
{
    public string NombreArchivo { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
    public byte[] Contenido { get; set; } = Array.Empty<byte>();
}

public sealed class MatrizRiesgoEvidenciaUploadFormDto
{
    public long? ControlId { get; set; }
    public long? PlanId { get; set; }
    public IFormFile? Archivo { get; set; }
}
