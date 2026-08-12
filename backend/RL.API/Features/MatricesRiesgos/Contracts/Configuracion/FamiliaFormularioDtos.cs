using System;
using System.ComponentModel.DataAnnotations;

namespace RL.API.Features.MatricesRiesgos.Contracts;

public sealed class FamiliaFormularioDto
{
    public long FamId { get; set; }
    public string FamCodigo { get; set; } = string.Empty;
    public string FamNombre { get; set; } = string.Empty;
    public string? FamDescripcion { get; set; }
    public bool FamActivo { get; set; }
    public DateTime FamFechaCreacion { get; set; }
    public int TotalVersiones { get; set; }
    public bool TieneVersionVigente { get; set; }
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
