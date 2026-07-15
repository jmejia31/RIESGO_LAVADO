using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace RL.API.Features.Identidad.Contracts;

public class LoginRequestDto
{
    [Required(ErrorMessage = "El email/usuario es requerido")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es requerida")]
    public string Password { get; set; } = string.Empty;
}

public class LoginResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public UsuarioInfoDto Usuario { get; set; } = null!;
}

public class RefreshTokenRequestDto
{
    [Required(ErrorMessage = "El refresh token es requerido")]
    public string RefreshToken { get; set; } = string.Empty;
}

public class UsuarioInfoDto
{
    [JsonProperty("id")]
    public long Id { get; set; }

    [JsonProperty("uid")]
    public string Uid => RL.API.Helpers.HashIdHelper.EncodeId(Id);

    [JsonProperty("nombre")]
    public string Nombre { get; set; } = string.Empty;

    [JsonProperty("apellido")]
    public string Apellido { get; set; } = string.Empty;

    [JsonProperty("email")]
    public string Email { get; set; } = string.Empty;

    [JsonProperty("rol")]
    public string Rol { get; set; } = string.Empty;

    [JsonProperty("rolId")]
    public int RolId { get; set; }

    [JsonProperty("esUsuarioDominio")]
    public int EsUsuarioDominio { get; set; }

    [JsonProperty("usuarioDominio")]
    public string? UsuarioDominio { get; set; }

    [JsonProperty("dominio")]
    public string? Dominio { get; set; }

    [JsonProperty("dominioId")]
    public int? DominioId { get; set; }

    [JsonProperty("dni")]
    public string? Dni { get; set; }

    [JsonProperty("modulosIds")]
    public List<int> ModulosIds { get; set; } = new();

    [JsonProperty("debeCambiarPassword")]
    public bool DebeCambiarPassword { get; set; }
}

public class CrearUsuarioDto
{
    [Required(ErrorMessage = "El nombre es requerido")]
    [MaxLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El apellido es requerido")]
    [MaxLength(100)]
    public string Apellido { get; set; } = string.Empty;

    [Required(ErrorMessage = "El email es requerido")]
    [EmailAddress(ErrorMessage = "Email inválido")]
    [MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Password { get; set; }

    [Required(ErrorMessage = "El rol es requerido")]
    public int RolId { get; set; }

    public string? EmpleadoId { get; set; }

    public int EsUsuarioDominio { get; set; }

    public string? UsuarioDominio { get; set; }

    public string? Dominio { get; set; }

    public int? DominioId { get; set; }

    public string? Dni { get; set; }
    public List<int> ModulosIds { get; set; } = new();
}

public class ActualizarUsuarioDto
{
    [Required(ErrorMessage = "El nombre es requerido")]
    [MaxLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El apellido es requerido")]
    [MaxLength(100)]
    public string Apellido { get; set; } = string.Empty;

    [Required(ErrorMessage = "El email es requerido")]
    [EmailAddress(ErrorMessage = "Email inválido")]
    [MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Password { get; set; }

    [Required(ErrorMessage = "El rol es requerido")]
    public int RolId { get; set; }

    public string? EmpleadoId { get; set; }

    public int EsUsuarioDominio { get; set; }

    public string? UsuarioDominio { get; set; }

    public string? Dominio { get; set; }

    public int? DominioId { get; set; }

    public string? Dni { get; set; }
    public List<int> ModulosIds { get; set; } = new();
}

public class CambiarPasswordDto
{
    [Required(ErrorMessage = "La contraseña actual es requerida")]
    public string PasswordActual { get; set; } = string.Empty;

    [Required(ErrorMessage = "La nueva contraseña es requerida")]
    [MinLength(8, ErrorMessage = "La contraseña debe tener mínimo 8 caracteres")]
    public string NuevoPassword { get; set; } = string.Empty;
}

public class SolicitudRecuperacionDto
{
    [Required(ErrorMessage = "El email es requerido")]
    [EmailAddress(ErrorMessage = "Email inválido")]
    public string Email { get; set; } = string.Empty;
}

public class ResetPasswordDto
{
    [Required(ErrorMessage = "El token es requerido")]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "La nueva contraseña es requerida")]
    [MinLength(8, ErrorMessage = "La contraseña debe tener mínimo 8 caracteres")]
    public string Password { get; set; } = string.Empty;
}

public class ModuloDto
{
    public int ModId { get; set; }
    public string ModNombre { get; set; } = string.Empty;
    public string? ModDescripcion { get; set; }
    public string ModRuta { get; set; } = string.Empty;
    public string ModIcono { get; set; } = string.Empty;
    public string ModSeccion { get; set; } = string.Empty;
}
