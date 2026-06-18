namespace RL.API.Models;

public class Modulo
{
    public int ModId { get; set; }
    public string ModNombre { get; set; } = string.Empty;
    public string? ModDescripcion { get; set; }
    public string ModRuta { get; set; } = string.Empty;
    public string ModIcono { get; set; } = string.Empty;
    public string ModSeccion { get; set; } = string.Empty;
    public int ModActivo { get; set; }
}
