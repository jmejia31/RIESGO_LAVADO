using System;
using Microsoft.AspNetCore.Http;

namespace RL.API.Features.MatricesRiesgos.Contracts;

// ============================================================
// 1. DTO CENTRAL DE METADATOS DE EVIDENCIAS (RL_MR_EVIDENCIAS)
// ============================================================
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

// ============================================================
// 2. DTOS ESPECÍFICOS PARA LAS 9 TABLAS PUENTE DE EVIDENCIAS
// ============================================================

/// <summary>
/// Mapea la asociación en RL_MR_EVI_RIESGO
/// </summary>
public sealed class AsociarEvidenciaRiesgoDto
{
    public long EvrRiesgoId { get; set; }
    public long EvrEvidenciaId { get; set; }
    public long UsrId { get; set; }
}

/// <summary>
/// Mapea la asociación en RL_MR_EVI_EVALUACION
/// </summary>
public sealed class AsociarEvidenciaEvaluacionDto
{
    public long EveEvaluacionId { get; set; }
    public long EveEvidenciaId { get; set; }
    public long UsrId { get; set; }
}

/// <summary>
/// Mapea la asociación en RL_MR_EVI_CONTROL
/// </summary>
public sealed class AsociarEvidenciaControlDto
{
    public long EvcControlId { get; set; }
    public long EvcEvidenciaId { get; set; }
    public long UsrId { get; set; }
}

/// <summary>
/// Mapea la asociación en RL_MR_EVI_PLAN
/// </summary>
public sealed class AsociarEvidenciaPlanDto
{
    public long EvpPlanId { get; set; }
    public long EvpEvidenciaId { get; set; }
    public long UsrId { get; set; }
}

/// <summary>
/// Mapea la asociación en RL_MR_EVI_ACTIVIDAD
/// </summary>
public sealed class AsociarEvidenciaActividadDto
{
    public long EvaActividadId { get; set; }
    public long EvaEvidenciaId { get; set; }
    public long UsrId { get; set; }
}

/// <summary>
/// Mapea la asociación en RL_MR_EVI_ALERTA
/// </summary>
public sealed class AsociarEvidenciaAlertaDto
{
    public long EvaAlertaId { get; set; }
    public long EvaEvidenciaId { get; set; }
    public long UsrId { get; set; }
}

/// <summary>
/// Mapea la asociación en RL_MR_EVI_AUTOMONITOREO
/// </summary>
public sealed class AsociarEvidenciaAutomonitoreoDto
{
    public long EvmMonitoreoId { get; set; }
    public long EvmEvidenciaId { get; set; }
    public long UsrId { get; set; }
}

/// <summary>
/// Mapea la asociación en RL_MR_EVI_REVISION
/// </summary>
public sealed class AsociarEvidenciaRevisionDto
{
    public long EvvRevisionId { get; set; }
    public long EvvEvidenciaId { get; set; }
    public long UsrId { get; set; }
}

/// <summary>
/// Mapea la asociación en RL_MR_EVI_APROBACION
/// </summary>
public sealed class AsociarEvidenciaAprobacionDto
{
    public long EvapAprobacionId { get; set; }
    public long EvapEvidenciaId { get; set; }
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
