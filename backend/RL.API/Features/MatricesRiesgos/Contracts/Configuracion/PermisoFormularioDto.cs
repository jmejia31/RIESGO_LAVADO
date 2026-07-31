using System;

namespace RL.API.Features.MatricesRiesgos.Contracts;

public sealed class PermisoFormularioDto
{
    public long PerId { get; set; }
    public long PerVersionId { get; set; }
    public string PerRolId { get; set; } = string.Empty;
    public string PerAmbito { get; set; } = string.Empty; // 'FORMULARIO', 'SECCION', 'CAMPO'
    public string PerRefId { get; set; } = string.Empty; // Identificador del campo o sección
    public string PerTipoPermiso { get; set; } = string.Empty; // 'EDICION', 'LECTURA', 'OCULTO'
    public long PerUsrCreacion { get; set; }
    public DateTime PerFechaCreacion { get; set; }
}
