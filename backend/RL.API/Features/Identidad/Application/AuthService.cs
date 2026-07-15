using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using RL.API.Features.Auditoria.Persistence;
using RL.API.Features.Configuracion.Persistence;
using RL.API.Features.Identidad.Contracts;
using RL.API.Features.Identidad.Domain;
using RL.API.Features.Identidad.Integrations.ActiveDirectory;
using RL.API.Features.Identidad.Integrations.Email;
using RL.API.Features.Identidad.Persistence;

namespace RL.API.Features.Identidad.Application;

public class AuthService : IAuthService
{
    private readonly IUsuarioRepository _usuarioRepo;
    private readonly IAuditoriaRepository _auditoriaRepo;
    private readonly IConfiguration _config;
    private readonly IActivoDirectorioService _adService;
    private readonly IConfiguracionRepository _configuracionRepo;
    private readonly IEmailService _emailService;

    public AuthService(IUsuarioRepository usuarioRepo, IAuditoriaRepository auditoriaRepo, IConfiguration config, IActivoDirectorioService adService, IConfiguracionRepository configuracionRepo, IEmailService emailService)
    {
        _usuarioRepo = usuarioRepo;
        _auditoriaRepo = auditoriaRepo;
        _config = config;
        _adService = adService;
        _configuracionRepo = configuracionRepo;
        _emailService = emailService;
    }

    public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto dto, string ip)
    {
        // Proceso de autenticación: soporta usuario local y usuario de dominio,
        // controla intentos fallidos, valida clave provisional y audita cada resultado.
        // Obtener usuario (soporta email o login de dominio)
        var usuario = await _usuarioRepo.ObtenerPorLoginAsync(dto.Email);
        if (usuario == null)
        {
            await AuditarLoginAsync(dto.Email, null, ip, "FALLIDO", "USUARIO_NO_ENCONTRADO");
            return null;
        }

        // Verificar si está bloqueado temporalmente
        if (usuario.UsrFechaBloqueo != null)
        {
            var diff = DateTime.Now - usuario.UsrFechaBloqueo.Value;
            if (diff.TotalMinutes < 1)
            {
                var minutosRestantes = 1.0 - diff.TotalMinutes;
                var segundosRestantes = (int)(minutosRestantes * 60);
                await AuditarLoginAsync(dto.Email, usuario, ip, "FALLIDO", "USUARIO_BLOQUEADO");
                throw new InvalidOperationException($"Su cuenta está bloqueada temporalmente por demasiados intentos fallidos. Intente de nuevo en {segundosRestantes} segundos.");
            }
            else
            {
                // El bloqueo expiró, restablecer
                await _usuarioRepo.RestablecerIntentosAsync(usuario.UsrId);
                usuario.UsrIntentosFallidos = 0;
                usuario.UsrFechaBloqueo = null;
            }
        }

        bool loginValido = false;

        // Si es local, validar contra BCrypt hash
        if (usuario.EsUsuarioDominio == 0)
        {
            loginValido = BCrypt.Net.BCrypt.Verify(dto.Password, usuario.UsrPasswordHash);
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(usuario.UsuarioDominio) && !string.IsNullOrWhiteSpace(usuario.UsrDominio))
            {
                try
                {
                    loginValido = await _adService.AutenticarAsync(
                        usuario.UsuarioDominio, usuario.UsrDominio, dto.Password);
                }
                catch (InvalidOperationException)
                {
                    loginValido = false;
                }
            }
        }

        if (!loginValido)
        {
            // Cargar configuración de intentos máximos
            var configSistema = await _configuracionRepo.ObtenerConfigSistemaAsync();
            int maxIntentos = configSistema?.MaxIntentos ?? 5;

            int nuevosIntentos = usuario.UsrIntentosFallidos + 1;
            if (nuevosIntentos >= maxIntentos)
            {
                await _usuarioRepo.RegistrarIntentoFallidoAsync(usuario.UsrId, nuevosIntentos, DateTime.Now);
                await AuditarLoginAsync(dto.Email, usuario, ip, "FALLIDO", "MAXIMO_INTENTOS");
                throw new InvalidOperationException($"Ha superado el límite de {maxIntentos} intentos. Su cuenta ha sido bloqueada por 1 minuto.");
            }
            else
            {
                await _usuarioRepo.RegistrarIntentoFallidoAsync(usuario.UsrId, nuevosIntentos, null);
                await AuditarLoginAsync(dto.Email, usuario, ip, "FALLIDO", $"INTENTO_{nuevosIntentos}_DE_{maxIntentos}");
                throw new InvalidOperationException($"Credenciales inválidas. Intento fallido {nuevosIntentos} de {maxIntentos}.");
            }
        }

        // Si el login fue exitoso y tenía intentos fallidos acumulados, restablecerlos
        if (usuario.UsrIntentosFallidos > 0)
        {
            await _usuarioRepo.RestablecerIntentosAsync(usuario.UsrId);
        }

        // Verificar si la clave provisional ha expirado
        if (usuario.UsrDebeCambiarPass == 1 && usuario.UsrFechaClaveTemp != null)
        {
            var configSistema = await _configuracionRepo.ObtenerConfigSistemaAsync();
            int validezMinutos = configSistema?.ValidezClaveTemp ?? 15;
            var diff = DateTime.Now - usuario.UsrFechaClaveTemp.Value;
            if (diff.TotalMinutes > validezMinutos)
            {
                await AuditarLoginAsync(dto.Email, usuario, ip, "FALLIDO", "CLAVE_PROVISIONAL_EXPIRADA");
                throw new InvalidOperationException($"La clave provisional ha expirado (tiempo de validez: {validezMinutos} minutos). Por favor solicite una nueva.");
            }
        }

        // Generar Tokens
        var response = await GenerarTokensParaUsuarioAsync(usuario, ip);
        
        // Registrar Auditoría
        await AuditarLoginAsync(dto.Email, usuario, ip, "EXITOSO", null);

        return response;
    }



    public async Task LogoutAsync(long usrId, string refreshToken)
    {
        await _usuarioRepo.RevocarRefreshTokenAsync(refreshToken);
        await _auditoriaRepo.RegistrarAsync("RL_USUARIOS", usrId.ToString(), "LOGOUT", null, null, usrId, null, null, "Auth");
    }

    private Task AuditarLoginAsync(string identificador, Usuario? usuario, string ip, string resultado, string? motivo)
    {
        var datos = Newtonsoft.Json.JsonConvert.SerializeObject(new
        {
            Resultado = resultado,
            Motivo = motivo,
            Identificador = identificador
        });

        return _auditoriaRepo.RegistrarAsync(
            "RL_USUARIOS",
            usuario?.UsrId.ToString() ?? identificador,
            "LOGIN",
            null,
            datos,
            usuario?.UsrId,
            usuario?.UsrEmail ?? identificador,
            ip,
            "Auth");
    }

    public async Task<bool> CambiarPasswordAsync(long usrId, CambiarPasswordDto dto)
    {
        // Proceso de cambio de contraseña local: valida la clave actual, genera hash BCrypt
        // y registra auditoría del cambio cuando la actualización es exitosa.
        var usuario = await _usuarioRepo.ObtenerPorIdAsync(usrId);
        if (usuario == null) return false;

        if (usuario.EsUsuarioDominio == 1)
            throw new InvalidOperationException("Este usuario pertenece a Active Directory. La contraseña debe gestionarse con TI.");

        if (!BCrypt.Net.BCrypt.Verify(dto.PasswordActual, usuario.UsrPasswordHash))
            return false;

        string salt = BCrypt.Net.BCrypt.GenerateSalt();
        string hash = BCrypt.Net.BCrypt.HashPassword(dto.NuevoPassword, salt);

        bool ok = await _usuarioRepo.ActualizarPasswordAsync(usrId, hash, "BCRYPT");
        if (ok)
        {
            await _auditoriaRepo.RegistrarAsync("RL_USUARIOS", usrId.ToString(), "UPDATE", "Cambio contraseña", null, usrId, usuario.UsrEmail, null, "Auth");
        }
        return ok;
    }

    public async Task<UsuarioInfoDto?> CrearUsuarioAsync(CrearUsuarioDto dto, long creadoPor)
    {
        // Proceso de creación de usuario: valida duplicidad, genera contraseña provisional,
        // persiste el usuario, audita la operación y notifica credenciales iniciales si aplica.
        // Validar si el email ya existe
        var existente = await _usuarioRepo.ObtenerPorEmailAsync(dto.Email);
        if (existente != null)
            throw new InvalidOperationException("El correo electrónico ya se encuentra registrado por otro usuario.");

        // Password por defecto si viene vacío (genera una clave provisional aleatoria)
        string plainPw = string.IsNullOrEmpty(dto.Password) ? GenerarPasswordProvisional(10) : dto.Password;
        string salt = BCrypt.Net.BCrypt.GenerateSalt();
        string hash = BCrypt.Net.BCrypt.HashPassword(plainPw, salt);

        long newId = await _usuarioRepo.CrearAsync(dto, hash, "BCRYPT");
        if (newId <= 0) return null;

        // Registrar auditoría
        await _auditoriaRepo.RegistrarAsync("RL_USUARIOS", newId.ToString(), "INSERT", null, Newtonsoft.Json.JsonConvert.SerializeObject(AuditUsuarioDto(dto)), creadoPor, dto.Email, null, "AdminUsuarios");

        // Enviar correo si es usuario local
        if (dto.EsUsuarioDominio == 0)
        {
            try
            {
                string asunto = "Nueva Cuenta de Usuario - SGRLA";
                string cuerpo = $@"
                    <h3>Estimado/a {dto.Nombre} {dto.Apellido},</h3>
                    <p>Se ha creado una nueva cuenta para su usuario en el <strong>Sistema de Gestión de Riesgo de Lavado de Activos (SGRLA)</strong>.</p>
                    <p>Su contraseña provisional de acceso es: <strong style='font-size: 16px; background-color: #f3f4f6; padding: 4px 8px; border-radius: 4px;'>{plainPw}</strong></p>
                    <p style='color: #ef4444; font-weight: bold;'>IMPORTANTE: Al ingresar por primera vez al sistema con esta clave provisional, se le requerirá obligatoriamente que defina una nueva contraseña personal.</p>
                    <p>Atentamente,<br>Soporte de TI / SGRLA</p>";

                await _emailService.EnviarCorreoAsync(dto.Email, asunto, cuerpo, true);
            }
            catch (Exception ex)
            {
                // Registramos el error de envío de correo en los logs pero no impedimos la creación del usuario
                // ya que la transacción de base de datos fue exitosa.
                Serilog.Log.Warning(ex, "No se pudo enviar el correo de bienvenida al usuario {Email}", dto.Email);
            }
        }

        var creado = await _usuarioRepo.ObtenerPorIdAsync(newId);
        return creado != null ? MapToDto(creado) : null;
    }

    public async Task<bool> ActualizarUsuarioAsync(string uid, ActualizarUsuarioDto dto, long actualizadoPor)
    {
        long id = Helpers.HashIdHelper.DecodeId(uid);
        if (id <= 0) return false;

        var existente = await _usuarioRepo.ObtenerPorIdAsync(id);
        if (existente == null) return false;

        // Verificar si el email ya existe en otro usuario
        var otro = await _usuarioRepo.ObtenerPorEmailAsync(dto.Email);
        if (otro != null && otro.UsrId != id)
            throw new InvalidOperationException("El correo electrónico ya está registrado por otro usuario.");

        string? hash = null;
        string? salt = null;
        if (!string.IsNullOrEmpty(dto.Password))
        {
            salt = BCrypt.Net.BCrypt.GenerateSalt();
            hash = BCrypt.Net.BCrypt.HashPassword(dto.Password, salt);
        }

        bool ok = await _usuarioRepo.ActualizarAsync(id, dto, hash, salt);
        if (ok)
        {
            await _auditoriaRepo.RegistrarAsync("RL_USUARIOS", id.ToString(), "UPDATE", Newtonsoft.Json.JsonConvert.SerializeObject(AuditUsuario(existente)), Newtonsoft.Json.JsonConvert.SerializeObject(AuditUsuarioDto(dto)), actualizadoPor, dto.Email, null, "AdminUsuarios");
        }
        return ok;
    }

    public async Task<List<UsuarioInfoDto>> ListarUsuariosAsync()
    {
        return await _usuarioRepo.ListarAsync();
    }

    public async Task<bool> ActualizarEstadoUsuarioAsync(string uid, bool activo, long actualizadoPor)
    {
        long id = Helpers.HashIdHelper.DecodeId(uid);
        if (id <= 0) return false;

        var existente = await _usuarioRepo.ObtenerPorIdAsync(id);
        if (existente == null) return false;

        bool ok = await _usuarioRepo.ActualizarEstadoAsync(id, activo);
        if (ok)
        {
            await _auditoriaRepo.RegistrarAsync("RL_USUARIOS", id.ToString(), "UPDATE", $"Estado anterior: {(existente.UsrActivo ? 1 : 0)}", $"Nuevo estado: {(activo ? 1 : 0)}", actualizadoPor, existente.UsrEmail, null, "AdminUsuarios");
        }
        return ok;
    }

    // ─── Métodos Auxiliares de Autenticación ───────────────────────────

    private async Task<LoginResponseDto> GenerarTokensParaUsuarioAsync(Usuario usuario, string ip)
    {
        var secretKey = Encoding.UTF8.GetBytes(_config["Jwt:SecretKey"]!);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.UsrId.ToString()),
            new Claim(ClaimTypes.Name, $"{usuario.UsrNombre} {usuario.UsrApellido}"),
            new Claim(ClaimTypes.GivenName, usuario.UsrNombre),
            new Claim(ClaimTypes.Surname, usuario.UsrApellido),
            new Claim(ClaimTypes.Email, usuario.UsrEmail),
            new Claim(ClaimTypes.Role, usuario.Rol.RolNombre),
            new Claim("rol_id", usuario.UsrRolId.ToString()),
            new Claim("uid", Helpers.HashIdHelper.EncodeId(usuario.UsrId)),
            new Claim("es_dom", usuario.EsUsuarioDominio.ToString()),
            new Claim("dom_id", usuario.UsrDomId?.ToString() ?? ""),
            new Claim("dominio", usuario.UsrDominio ?? ""),
            new Claim("usr_dom", usuario.UsuarioDominio ?? ""),
            new Claim("modulos", string.Join(",", usuario.ModulosIds ?? new List<int>())),
            new Claim("debe_cambiar_pass", usuario.UsrDebeCambiarPass.ToString())
        };

        var expires = DateTime.UtcNow.AddMinutes(Convert.ToDouble(_config["Jwt:AccessTokenExpirationMinutes"] ?? "60"));
        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: expires,
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(secretKey), SecurityAlgorithms.HmacSha256)
        );

        string accessToken = new JwtSecurityTokenHandler().WriteToken(token);
        string refreshToken = GenerarRefreshToken();

        var refExp = DateTime.UtcNow.AddDays(Convert.ToDouble(_config["Jwt:RefreshTokenExpirationDays"] ?? "7"));
        await _usuarioRepo.GuardarRefreshTokenAsync(usuario.UsrId, refreshToken, refExp, ip);

        return new LoginResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = expires,
            Usuario = MapToDto(usuario)
        };
    }

    public async Task<LoginResponseDto?> RefreshTokenAsync(string refreshToken, string ip)
    {
        // La aplicación coordina la rotación; la localización y validación temporal del token pertenecen a persistencia.
        var usrId = await _usuarioRepo.BuscarUsuarioIdPorRefreshTokenAsync(refreshToken);
        if (!usrId.HasValue || usrId.Value <= 0) return null;

        var tokenDb = await _usuarioRepo.ObtenerRefreshTokenAsync(usrId.Value, refreshToken);
        if (tokenDb == null) return null;

        var usuario = await _usuarioRepo.ObtenerPorIdAsync(usrId.Value);
        if (usuario == null || !usuario.UsrActivo)
        {
            await _usuarioRepo.RevocarRefreshTokenAsync(refreshToken);
            return null;
        }

        // Revocar token actual
        await _usuarioRepo.RevocarRefreshTokenAsync(refreshToken);

        // Generar nuevos tokens
        return await GenerarTokensParaUsuarioAsync(usuario, ip);
    }

    private static string GenerarRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    public async Task<bool> RecuperarPasswordAsync(string email)
    {
        var usuario = await _usuarioRepo.ObtenerPorEmailAsync(email);
        if (usuario == null)
            throw new InvalidOperationException("El correo electrónico no corresponde a ningún usuario registrado.");

        if (usuario.EsUsuarioDominio == 1)
            throw new InvalidOperationException("Este usuario pertenece a Active Directory. Por favor, gestione su contraseña con el departamento de TI.");

        string provisionalPw = GenerarPasswordProvisional(10);
        string salt = BCrypt.Net.BCrypt.GenerateSalt();
        string hash = BCrypt.Net.BCrypt.HashPassword(provisionalPw, salt);

        bool ok = await _usuarioRepo.ForzarCambioPasswordAsync(usuario.UsrId, hash, "BCRYPT");
        if (ok)
        {
            string asunto = "Clave Provisional de Acceso - SGRLA";
            string cuerpo = $@"
                <h3>Estimado/a {usuario.UsrNombre} {usuario.UsrApellido},</h3>
                <p>Se ha solicitado una restauración de contraseña para su usuario en el <strong>Sistema de Gestión de Riesgo de Lavado de Activos (SGRLA)</strong>.</p>
                <p>Su contraseña provisional de acceso es: <strong style='font-size: 16px; background-color: #f3f4f6; padding: 4px 8px; border-radius: 4px;'>{provisionalPw}</strong></p>
                <p style='color: #ef4444; font-weight: bold;'>IMPORTANTE: Al ingresar por primera vez al sistema con esta clave provisional, se le requerirá obligatoriamente que defina una nueva contraseña personal.</p>
                <p>Atentamente,<br>Soporte de TI / SGRLA</p>";

            await _emailService.EnviarCorreoAsync(usuario.UsrEmail, asunto, cuerpo, true);
            await _auditoriaRepo.RegistrarAsync("RL_USUARIOS", usuario.UsrId.ToString(), "UPDATE", "Solicitud recuperación contraseña", null, usuario.UsrId, usuario.UsrEmail, null, "Auth");
        }

        return ok;
    }

    private static string GenerarPasswordProvisional(int longitud)
    {
        const string caracteresValidos = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!@#$";
        var res = new StringBuilder();
        using (var rng = RandomNumberGenerator.Create())
        {
            byte[] uintBuffer = new byte[4];
            while (res.Length < longitud)
            {
                rng.GetBytes(uintBuffer);
                uint num = BitConverter.ToUInt32(uintBuffer, 0);
                res.Append(caracteresValidos[(int)(num % (uint)caracteresValidos.Length)]);
            }
        }
        return res.ToString();
    }

    private static object AuditUsuarioDto(CrearUsuarioDto dto) => new
    {
        dto.Nombre,
        dto.Apellido,
        dto.Email,
        dto.RolId,
        dto.EmpleadoId,
        dto.EsUsuarioDominio,
        dto.UsuarioDominio,
        dto.Dominio,
        dto.DominioId,
        dto.Dni,
        dto.ModulosIds,
        PasswordInformado = !string.IsNullOrEmpty(dto.Password)
    };

    private static object AuditUsuarioDto(ActualizarUsuarioDto dto) => new
    {
        dto.Nombre,
        dto.Apellido,
        dto.Email,
        dto.RolId,
        dto.EmpleadoId,
        dto.EsUsuarioDominio,
        dto.UsuarioDominio,
        dto.Dominio,
        dto.DominioId,
        dto.Dni,
        dto.ModulosIds,
        PasswordInformado = !string.IsNullOrEmpty(dto.Password)
    };

    private static object AuditUsuario(Usuario u) => new
    {
        u.UsrId,
        Nombre = u.UsrNombre,
        Apellido = u.UsrApellido,
        Email = u.UsrEmail,
        RolId = u.UsrRolId,
        EmpleadoId = u.UsrEmpleadoId,
        Activo = u.UsrActivo,
        u.EsUsuarioDominio,
        u.UsuarioDominio,
        DominioId = u.UsrDomId,
        Dominio = u.UsrDominio,
        Dni = u.UsrDni,
        ModulosIds = u.ModulosIds ?? new List<int>(),
        DebeCambiarPassword = u.UsrDebeCambiarPass == 1
    };

    private static UsuarioInfoDto MapToDto(Usuario u) => new()
    {
        Id = u.UsrId,
        Nombre = u.UsrNombre,
        Apellido = u.UsrApellido,
        Email = u.UsrEmail,
        Rol = u.Rol.RolNombre,
        RolId = u.UsrRolId,
        EsUsuarioDominio = u.EsUsuarioDominio,
        UsuarioDominio = u.UsuarioDominio,
        Dominio = u.UsrDominio,
        DominioId = u.UsrDomId,
        Dni = u.UsrDni,
        ModulosIds = u.ModulosIds ?? new List<int>(),
        DebeCambiarPassword = u.UsrDebeCambiarPass == 1
    };
}
