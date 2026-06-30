using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using RL.API.DTOs;
using RL.API.Security;
using RL.API.Services;

namespace RL.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IActivoDirectorioService _adService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, IActivoDirectorioService adService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _adService   = adService;
        _logger      = logger;
    }

    /// <summary>Iniciar sesión</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [AuditRequired("Login de usuario")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { success = false, errores = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)) });

        try
        {
            var ip       = HttpContext.Connection.RemoteIpAddress?.ToString();
            var response = await _authService.LoginAsync(dto, ip ?? "unknown");

            if (response == null)
                return Unauthorized(new { success = false, mensaje = "Credenciales inválidas" });

            return Ok(new { success = true, datos = response });
        }
        catch (System.InvalidOperationException ex)
        {
            return BadRequest(new { success = false, mensaje = ex.Message });
        }
    }

    /// <summary>Renovar Access Token con Refresh Token</summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequestDto dto)
    {
        var ip       = HttpContext.Connection.RemoteIpAddress?.ToString();
        var response = await _authService.RefreshTokenAsync(dto.RefreshToken, ip ?? "unknown");

        if (response == null)
            return Unauthorized(new { success = false, mensaje = "Refresh token inválido o expirado" });

        return Ok(new { success = true, datos = response });
    }

    /// <summary>Cerrar sesión</summary>
    [HttpPost("logout")]
    [Authorize]
    [AuditRequired("Logout de usuario")]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequestDto dto)
    {
        var claimUsr = User.FindFirst(ClaimTypes.NameIdentifier);
        if (claimUsr != null)
        {
            var usrId = Convert.ToInt64(claimUsr.Value);
            await _authService.LogoutAsync(usrId, dto.RefreshToken);
        }
        return Ok(new { success = true, mensaje = "Sesión cerrada correctamente" });
    }

    /// <summary>Cambiar contraseña</summary>
    [HttpPut("password")]
    [Authorize]
    [AuditRequired("Cambio de contrasena")]
    public async Task<IActionResult> CambiarPassword([FromBody] CambiarPasswordDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { success = false, errores = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)) });

        var usrId = Convert.ToInt64(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        bool ok;

        try
        {
            ok = await _authService.CambiarPasswordAsync(usrId, dto);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, mensaje = ex.Message });
        }

        if (!ok)
            return BadRequest(new { success = false, mensaje = "Contraseña actual incorrecta" });

        return Ok(new { success = true, mensaje = "Contraseña actualizada exitosamente" });
    }

    /// <summary>Obtener perfil del usuario autenticado</summary>
    [HttpGet("perfil")]
    [Authorize]
    public IActionResult Perfil()
    {
        return Ok(new
        {
            success = true,
            datos = new
            {
                id       = User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                uid      = User.FindFirst("uid")?.Value,
                nombre   = User.FindFirst(ClaimTypes.Name)?.Value,
                nombres  = User.FindFirst(ClaimTypes.GivenName)?.Value,
                apellido = User.FindFirst(ClaimTypes.Surname)?.Value,
                email    = User.FindFirst(ClaimTypes.Email)?.Value,
                rol      = User.FindFirst(ClaimTypes.Role)?.Value,
                rolId    = User.FindFirst("rol_id")?.Value,
                esUsuarioDominio = User.FindFirst("es_dom")?.Value,
                usuarioDominio = User.FindFirst("usr_dom")?.Value,
                dominio = User.FindFirst("dominio")?.Value,
                dominioId = User.FindFirst("dom_id")?.Value,
                modulosIds = (User.FindFirst("modulos")?.Value ?? string.Empty)
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(id => int.TryParse(id, out var parsed) ? parsed : (int?)null)
                    .Where(id => id.HasValue)
                    .Select(id => id!.Value)
                    .ToArray(),
                debeCambiarPassword = User.FindFirst("debe_cambiar_pass")?.Value == "1"
            }
        });
    }

    /// <summary>Crear nuevo usuario (solo Admin)</summary>
    [HttpPost("usuarios")]
    [Authorize(Roles = "ADMINISTRADOR")]
    [ModuloAuthorize(2)]
    [AuditRequired("Creacion de usuario")]
    public async Task<IActionResult> CrearUsuario([FromBody] CrearUsuarioDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { success = false, errores = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)) });

        try
        {
            var usrId  = Convert.ToInt64(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _authService.CrearUsuarioAsync(dto, usrId);

            return result == null
                ? BadRequest(new { success = false, mensaje = "Error al crear usuario" })
                : Ok(new { success = true, datos = result, mensaje = "Usuario creado exitosamente" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear usuario");
            return StatusCode(500, new { success = false, mensaje = "Error interno al crear el usuario." });
        }
    }

    /// <summary>Actualizar usuario existente (solo Admin)</summary>
    [HttpPut("usuarios/{uid}")]
    [Authorize(Roles = "ADMINISTRADOR")]
    [ModuloAuthorize(2)]
    [AuditRequired("Edicion de usuario")]
    public async Task<IActionResult> ActualizarUsuario(string uid, [FromBody] ActualizarUsuarioDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { success = false, errores = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)) });

        try
        {
            var ok = await _authService.ActualizarUsuarioAsync(uid, dto, ObtenerUsuarioId());
            return ok
                ? Ok(new { success = true, mensaje = "Usuario actualizado exitosamente" })
                : NotFound(new { success = false, mensaje = "No se encontró el usuario o el ID es inválido" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar usuario {Uid}", uid);
            return StatusCode(500, new { success = false, mensaje = "Error interno al actualizar el usuario." });
        }
    }

    /// <summary>Listar todos los usuarios (solo Admin)</summary>
    [HttpGet("usuarios")]
    [Authorize(Roles = "ADMINISTRADOR")]
    [ModuloAuthorize(2)]
    public async Task<IActionResult> ListarUsuarios()
    {
        var usuarios = await _authService.ListarUsuariosAsync();
        return Ok(new { success = true, datos = usuarios });
    }

    /// <summary>Activar o desactivar usuario (solo Admin)</summary>
    [HttpPut("usuarios/{uid}/estado")]
    [Authorize(Roles = "ADMINISTRADOR")]
    [ModuloAuthorize(2)]
    [AuditRequired("Cambio de estado de usuario")]
    public async Task<IActionResult> CambiarEstadoUsuario(string uid, [FromBody] EstadoUsuarioDto dto)
    {
        try
        {
            var ok = await _authService.ActualizarEstadoUsuarioAsync(uid, dto.Activo, ObtenerUsuarioId());
            return ok
                ? Ok(new { success = true, mensaje = $"Estado del usuario actualizado a {(dto.Activo ? "Activo" : "Inactivo")}" })
                : NotFound(new { success = false, mensaje = "Usuario no encontrado" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cambiar estado de usuario {Uid}", uid);
            return StatusCode(500, new { success = false, mensaje = "Error interno al actualizar el estado del usuario." });
        }
    }

    /// <summary>Validar usuario en Active Directory</summary>
    [HttpGet("validar-dominio")]
    [Authorize(Roles = "ADMINISTRADOR")]
    [ModuloAuthorize(2)]
    public async Task<IActionResult> ValidarDominio([FromQuery] string usuario, [FromQuery] string dominio = "")
    {
        if (string.IsNullOrWhiteSpace(usuario))
            return BadRequest(new { success = false, mensaje = "El parámetro 'usuario' es requerido." });

        try
        {
            var resultado = await _adService.ValidarUsuarioAsync(usuario.Trim(), dominio.Trim());
            return Ok(new { success = true, datos = resultado });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Error validando usuario AD '{Usuario}': {Msg}", usuario, ex.Message);
            return StatusCode(503, new { success = false, mensaje = ex.Message });
        }
    }

    /// <summary>Recuperar contraseña enviando clave provisional por correo</summary>
    [HttpPost("recuperar-password")]
    [AllowAnonymous]
    [AuditRequired("Solicitud de recuperacion de contrasena")]
    public async Task<IActionResult> RecuperarPassword([FromBody] SolicitudRecuperacionDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { success = false, errores = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)) });

        try
        {
            var ok = await _authService.RecuperarPasswordAsync(dto.Email);
            return ok 
                ? Ok(new { success = true, mensaje = "Se ha enviado una clave provisional a su correo electrónico." })
                : BadRequest(new { success = false, mensaje = "No se pudo generar la clave provisional." });
        }
        catch (System.InvalidOperationException ex)
        {
            return BadRequest(new { success = false, mensaje = ex.Message });
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error al recuperar contraseña para {Email}", dto.Email);
            return StatusCode(500, new { success = false, mensaje = "Ocurrió un error interno al procesar la solicitud." });
        }
    }
    private long ObtenerUsuarioId()
    {
        return Convert.ToInt64(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    }
}

public class EstadoUsuarioDto
{
    public bool Activo { get; set; }
}
