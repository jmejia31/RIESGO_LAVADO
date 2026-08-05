using System;
using Microsoft.AspNetCore.Http;

namespace RL.API.Features.MatricesRiesgos.Contracts;

public sealed class EvidenciaDto
{
    public long EviId { get; set; }
    public string EviNombreArchivo { get; set; } = string.Empty;
    public string EviExtension { get; set; } = string.Empty;
    public long EviTamano { get; set; }
    public string EviHash { get; set; } = string.Empty;
    public string EviRuta { get; set; } = string.Empty;
    public long EviUsrCreacion { get; set; }
    public DateTime EviFechaCreacion { get; set; }
}

public sealed class EvidenciaRegistroDto
{
    public string EviNombreArchivo { get; set; } = string.Empty;
    public string EviExtension { get; set; } = string.Empty;
    public long EviTamano { get; set; }
    public string EviHash { get; set; } = string.Empty;
    public string EviRuta { get; set; } = string.Empty;
    public long EviUsrCreacion { get; set; }
}

public sealed class EvidenciaDescargaDto
{
    public string NombreArchivo { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
    public byte[] Contenido { get; set; } = Array.Empty<byte>();
}

public sealed class EvidenciaUploadFormDto
{
    public IFormFile? Archivo { get; set; }
    public long UsrId { get; set; }
}

public enum TipoEntidadEvidencia
{
    Riesgo, Evaluacion, Control, Plan, Actividad, Alerta, Automonitoreo
}

public sealed class VincularEvidenciaDto
{
    public long EvidenciaId { get; set; }
    public TipoEntidadEvidencia TipoEntidad { get; set; }
    public long EntidadId { get; set; }
}

/// <summary>
/// Contrato temporal exclusivo de la prueba Oracle pendiente. No se expone por API.
/// </summary>
public sealed class AsociarEvidenciaAprobacionDto
{
    public long EvapAprobacionId { get; set; }
    public long EvapEvidenciaId { get; set; }
    public long UsrId { get; set; }
}
