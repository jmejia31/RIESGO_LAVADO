using System.ComponentModel.DataAnnotations;

namespace RL.API.Features.MatricesRiesgos.Contracts;

public sealed class FormulaDto
{
    public long Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string Estado { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; }
    public int VersionRow { get; set; }
}

public sealed class FormulaVersionDto
{
    public long Id { get; set; }
    public long FormulaId { get; set; }
    public int Version { get; set; }
    public string Expresion { get; set; } = string.Empty;
    public string TipoResultado { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public string Hash { get; set; } = string.Empty;
    public DateTime? FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    public DateTime FechaCreacion { get; set; }
    public int VersionRow { get; set; }
}

public sealed class FormulaUsageDto
{
    public long Id { get; set; }
    public long VersionFormularioId { get; set; }
    public string CampoClave { get; set; } = string.Empty;
    public long FormulaVersionId { get; set; }
    public int FormulaVersion { get; set; }
    public string FormulaCodigo { get; set; } = string.Empty;
}

public sealed class CrearFormulaDto
{
    [Required, StringLength(80)] public string Codigo { get; set; } = string.Empty;
    [Required, StringLength(150)] public string Nombre { get; set; } = string.Empty;
    [StringLength(1000)] public string? Descripcion { get; set; }
    [Required] public CrearFormulaVersionDto VersionInicial { get; set; } = new();
}

public sealed class CrearFormulaVersionDto
{
    [Required] public string Expresion { get; set; } = string.Empty;
    [Required, StringLength(20)] public string TipoResultado { get; set; } = "DECIMAL";
}

public sealed class ActualizarFormulaBorradorDto
{
    [Required] public string Expresion { get; set; } = string.Empty;
    [Required, StringLength(20)] public string TipoResultado { get; set; } = "DECIMAL";
    public int VersionRow { get; set; }
}

public sealed class CrearFormulaUsoDto
{
    [Required] public long VersionFormularioId { get; set; }
    [Required, StringLength(150)] public string CampoClave { get; set; } = string.Empty;
    [Required] public long FormulaVersionId { get; set; }
}

public sealed class ReemplazarFormulaUsosDto
{
    public List<CrearFormulaUsoDto> Usos { get; set; } = new();
}

public sealed class CambiarEstadoConfiguracionDto
{
    [Required, StringLength(20)] public string Estado { get; set; } = string.Empty;
    public int VersionRow { get; set; }
}

public sealed class FuncionDto
{
    public long Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string Categoria { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public int VersionRow { get; set; }
}

public sealed class FuncionVersionDto
{
    public long Id { get; set; }
    public long FuncionId { get; set; }
    public int Version { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string TipoResultado { get; set; } = string.Empty;
    public string? SignatureJson { get; set; }
    public string? DefinicionDsl { get; set; }
    public string? HandlerKey { get; set; }
    public int MinArity { get; set; }
    public int? MaxArity { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string Hash { get; set; } = string.Empty;
    public int VersionRow { get; set; }
}

public sealed class FuncionArgumentoDto
{
    public long Id { get; set; }
    public long FuncionVersionId { get; set; }
    public int Posicion { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public bool Requerido { get; set; }
    public bool Variadic { get; set; }
    public string? ValorDefaultJson { get; set; }
    public string? Descripcion { get; set; }
}

public sealed class CrearFuncionDto
{
    [Required, StringLength(80)] public string Codigo { get; set; } = string.Empty;
    [Required, StringLength(150)] public string Nombre { get; set; } = string.Empty;
    [StringLength(1000)] public string? Descripcion { get; set; }
    [StringLength(50)] public string Categoria { get; set; } = "CALCULO";
    [Required] public CrearFuncionVersionDto VersionInicial { get; set; } = new();
}

public class CrearFuncionVersionDto
{
    [Required, StringLength(12)] public string Tipo { get; set; } = "NATIVE";
    [Required, StringLength(20)] public string TipoResultado { get; set; } = "DECIMAL";
    public string? SignatureJson { get; set; }
    public string? DefinicionDsl { get; set; }
    public string? HandlerKey { get; set; }
    [Range(0, 999)] public int MinArity { get; set; }
    [Range(0, 999)] public int? MaxArity { get; set; }
    public List<FuncionArgumentoGuardarDto> Argumentos { get; set; } = new();
}

public sealed class ActualizarFuncionBorradorDto : CrearFuncionVersionDto
{
    public int VersionRow { get; set; }
}

public sealed class FuncionArgumentoGuardarDto
{
    [Range(1, 999)] public int Posicion { get; set; }
    [Required, StringLength(80)] public string Codigo { get; set; } = string.Empty;
    [Required, StringLength(150)] public string Nombre { get; set; } = string.Empty;
    [Required, StringLength(20)] public string Tipo { get; set; } = "DECIMAL";
    public bool Requerido { get; set; } = true;
    public bool Variadic { get; set; }
    public string? ValorDefaultJson { get; set; }
    [StringLength(500)] public string? Descripcion { get; set; }
}

public sealed class CrearParametroDto
{
    [Required, StringLength(80)] public string Codigo { get; set; } = string.Empty;
    [Required, StringLength(150)] public string Nombre { get; set; } = string.Empty;
    [StringLength(1000)] public string? Descripcion { get; set; }
    [Required] public CrearParametroVersionDto VersionInicial { get; set; } = new();
}

public sealed class ParametroDto
{
    public long Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public int VersionRow { get; set; }
}

public sealed class ParametroVersionDto
{
    public long Id { get; set; }
    public long ParametroId { get; set; }
    public int Version { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public int? ValorEntero { get; set; }
    public decimal? ValorDecimal { get; set; }
    public bool? ValorBooleano { get; set; }
    public string? ValorTexto { get; set; }
    public DateTime? ValorFecha { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string Hash { get; set; } = string.Empty;
    public int VersionRow { get; set; }
}

public class CrearParametroVersionDto
{
    [Required, StringLength(20)] public string Tipo { get; set; } = "DECIMAL";
    public int? ValorEntero { get; set; }
    public decimal? ValorDecimal { get; set; }
    public bool? ValorBooleano { get; set; }
    [StringLength(2000)] public string? ValorTexto { get; set; }
    public DateTime? ValorFecha { get; set; }
}

public sealed class ActualizarParametroBorradorDto : CrearParametroVersionDto
{
    public int VersionRow { get; set; }
}
