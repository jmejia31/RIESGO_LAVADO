namespace RL.API.Models;

public class Usuario
{
    public long UsrId { get; set; }
    public string UsrNombre { get; set; } = string.Empty;
    public string UsrApellido { get; set; } = string.Empty;
    public string UsrEmail { get; set; } = string.Empty;
    public string UsrPasswordHash { get; set; } = string.Empty;
    public string UsrPasswordSalt { get; set; } = string.Empty;
    public int UsrRolId { get; set; }
    public string? UsrEmpleadoId { get; set; }
    public bool UsrActivo { get; set; }
    public DateTime UsrFechaCreacion { get; set; }
    public DateTime? UsrFechaModificacion { get; set; }
    public long? UsrCreadoPor { get; set; }
    public int EsUsuarioDominio { get; set; }
    public int? UsrDomId { get; set; }
    public string? UsrDominio { get; set; } // Nombre del dominio obtenido mediante JOIN
    public string? UsuarioDominio { get; set; }
    public string? UsrDni { get; set; }
    public int UsrIntentosFallidos { get; set; }
    public DateTime? UsrFechaBloqueo { get; set; }
    public int UsrDebeCambiarPass { get; set; }
    public DateTime? UsrFechaClaveTemp { get; set; }
    public List<int> ModulosIds { get; set; } = new();

    // Propiedades adicionales de navegación
    public Rol Rol { get; set; } = null!;
}
