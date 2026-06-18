namespace RL.API.Models;

public class LoginSlide
{
    public int Id { get; set; }
    public string ImagenUrl { get; set; } = string.Empty;
    public string? Titulo { get; set; }
    public string? Descripcion { get; set; }
    public int Orden { get; set; }
    public bool Activo { get; set; }
    public string? ImagenIcono { get; set; }
    public DateTime FechaModif { get; set; }
}
