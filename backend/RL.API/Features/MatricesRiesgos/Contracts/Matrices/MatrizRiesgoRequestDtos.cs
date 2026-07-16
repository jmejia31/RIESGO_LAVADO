namespace RL.API.Features.MatricesRiesgos.Contracts;

public sealed class MatrizRiesgoCrearRequestDto
{
    public string SujetoTipo { get; set; } = string.Empty;
    public string? SujetoIdExt { get; set; }
    public string? Documento { get; set; }
    public string NombreSujeto { get; set; } = string.Empty;
    public string OrigenDatos { get; set; } = "CAPTURA";
    public List<MatrizRiesgoDetalleRequestDto> Detalles { get; set; } = new();
    public List<MatrizRiesgoControlRequestDto> Controles { get; set; } = new();
}

public sealed class MatrizRiesgoDetalleRequestDto
{
    public long VariableId { get; set; }
    public string? ValorCapturado { get; set; }
    public decimal Puntaje { get; set; }
    public string? Justificacion { get; set; }
    public string? FuenteDato { get; set; }
}

public sealed class MatrizRiesgoControlRequestDto
{
    public long? FactorId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string? Periodicidad { get; set; }
    public string? Oportunidad { get; set; }
    public string? Automatizacion { get; set; }
    public string? Procedimientos { get; set; }
    public string? Calidad { get; set; }
    public decimal EfectividadPct { get; set; }
    public string? Responsable { get; set; }
    public bool EvidenciaObligatoria { get; set; }
}

public sealed class MatrizRiesgoCalcularRequestDto
{
    public string TipoCalculo { get; set; } = "GLOBAL";
    public string? MotivoCalculo { get; set; }
}

public sealed class MatrizRiesgoCambiarEstadoRequestDto
{
    public string Estado { get; set; } = string.Empty;
    public string Motivo { get; set; } = string.Empty;
}

public sealed class MatrizRiesgoInactivarRequestDto
{
    public string Motivo { get; set; } = string.Empty;
}
