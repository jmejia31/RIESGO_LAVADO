using System;

namespace RL.API.Features.MatricesRiesgos.Contracts;

public sealed class VersionFormularioDto
{
    public long VerId { get; set; }
    public long VerFamiliaId { get; set; }
    public string VerCodigo { get; set; } = string.Empty;
    public int VerVersion { get; set; }
    public string VerJson { get; set; } = string.Empty;
    public string VerHash { get; set; } = string.Empty;
    public string VerEstado { get; set; } = string.Empty; // 'DRAFT', 'IN_REVIEW', 'APPROVED', 'PUBLISHED', 'RETIRED', 'ARCHIVED'
    public bool VerVigente { get; set; }
    public DateTime? VerFechaInicio { get; set; }
    public DateTime? VerFechaFin { get; set; }
    public DateTime VerFechaCreacion { get; set; }
    public long VerUsrCreacion { get; set; }
}
