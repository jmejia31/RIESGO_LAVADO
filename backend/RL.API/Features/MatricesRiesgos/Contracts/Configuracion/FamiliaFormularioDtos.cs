using System;
using System.ComponentModel.DataAnnotations;
using RL.API.Shared.Results;

namespace RL.API.Features.MatricesRiesgos.Contracts;

public sealed class FamiliaFormularioDto
{
    public long FamId { get; set; }
    public string FamCodigo { get; set; } = string.Empty;
    public string FamNombre { get; set; } = string.Empty;
    public string? FamDescripcion { get; set; }
    public bool FamActivo { get; set; }
    public bool FamPredeterminada { get; set; }
    public bool FamArchivada { get; set; }
    public DateTime? FamFechaArchivado { get; set; }
    public long? FamUsuarioArchivado { get; set; }
    public DateTime FamFechaCreacion { get; set; }
    public int TotalVersiones { get; set; }
    public bool TieneVersionVigente { get; set; }
}

public sealed class FamiliaPredeterminadaDto
{
    public bool Configurada { get; set; }
    public long? FamiliaId { get; set; }
    public string? FamiliaCodigo { get; set; }
    public string? FamiliaNombre { get; set; }
    public bool TieneVersionVigente { get; set; }
    public long? VersionVigenteId { get; set; }
    public string? VersionCodigo { get; set; }
    public int? Version { get; set; }
}

public sealed class ConsultaFamiliasFormularioPaginadaDto
{
    public int Pagina { get; set; } = 1;
    public int TamanoPagina { get; set; } = 10;
    public string? Buscar { get; set; }
    public string Estado { get; set; } = "TODAS";
    public string Archivado { get; set; } = "NO_ARCHIVADAS";
    public string Vigencia { get; set; } = "TODAS";
}

public sealed class FamiliasFormularioTotalesDto
{
    public int TotalFamilias { get; init; }
    public int Activas { get; init; }
    public int Inactivas { get; init; }
    public int Archivadas { get; init; }
    public int TotalVersiones { get; init; }
}

public sealed class FamiliasFormularioPaginadasDto : PaginadoDto<FamiliaFormularioDto>
{
    public FamiliasFormularioTotalesDto Totales { get; init; } = new();
}

public sealed class CrearFamiliaFormularioDto
{
    [Required(ErrorMessage = "El código de la familia es obligatorio.")]
    [StringLength(50, ErrorMessage = "El código no puede exceder los 50 caracteres.")]
    public string FamCodigo { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre de la familia es obligatorio.")]
    [StringLength(150, ErrorMessage = "El nombre no puede exceder los 150 caracteres.")]
    public string FamNombre { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "La descripción no puede exceder los 500 caracteres.")]
    public string? FamDescripcion { get; set; }
}

public sealed class ActualizarFamiliaFormularioDto
{
    [Required(ErrorMessage = "El nombre de la familia es obligatorio.")]
    [StringLength(150, ErrorMessage = "El nombre no puede exceder los 150 caracteres.")]
    public string FamNombre { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "La descripción no puede exceder los 500 caracteres.")]
    public string? FamDescripcion { get; set; }

    public bool FamActivo { get; set; } = true;
}

public sealed class FamiliaFormularioPurgaPrecheckDto
{
    public long FamiliaId { get; init; }
    public string FamiliaCodigo { get; init; } = string.Empty;
    public bool EsDatoDePrueba { get; init; }
    public bool FamiliaActiva { get; init; }
    public bool FamiliaArchivada { get; init; }
    public bool FamiliaPredeterminada { get; init; }
    public int TotalVersiones { get; init; }
    public int VersionesPublicadas { get; init; }
    public int VersionesPublicadasVigentes { get; init; }
    public int TotalEvaluaciones { get; init; }
    public int TotalEvidenciasVinculadas { get; init; }
    public int TotalUsosFormula { get; init; }
    public int TotalReferenciasAuditoria { get; init; }
    public bool DependenciasCompletamenteAuditadas { get; init; }
    public bool PurgaElegible { get; init; }
    public IReadOnlyList<string> MotivosBloqueo { get; init; } = Array.Empty<string>();
}
