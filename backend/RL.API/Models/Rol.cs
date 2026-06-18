namespace RL.API.Models;

public class Rol
{
    public int RolId { get; set; }
    public string RolNombre { get; set; } = string.Empty;
    public string? RolDescripcion { get; set; }
    public bool RolActivo { get; set; }
    public DateTime RolFechaCreacion { get; set; }
}
