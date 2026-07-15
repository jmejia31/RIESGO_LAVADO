namespace RL.API.Features.Configuracion.Contracts;

public class ConfigSistema
{
    public int SfsId { get; set; }
    public string NombreInstitucion { get; set; } = string.Empty;
    public string NombreSistema { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string? IconoUrl { get; set; }
    public string? ColorPrimario { get; set; }
    public string? ColorSecundario { get; set; }
    public int TimeoutSesion { get; set; }
    public string? AcuerdoLegal { get; set; }
    public int MaxIntentos { get; set; } = 5;
    public int ValidezClaveTemp { get; set; } = 15;
    public DateTime UltimaActualizacion { get; set; }
}
