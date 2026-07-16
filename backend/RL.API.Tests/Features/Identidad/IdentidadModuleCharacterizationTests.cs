using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using RL.API.Features.Auditoria.Contracts;
using RL.API.Features.Auditoria.Persistence;
using RL.API.Features.Configuracion.Contracts;
using RL.API.Features.Configuracion.Persistence;
using RL.API.Features.Identidad;
using RL.API.Features.Identidad.Application;
using RL.API.Features.Identidad.Contracts;
using RL.API.Features.Identidad.Domain;
using RL.API.Features.Identidad.Integrations.ActiveDirectory;
using RL.API.Features.Identidad.Integrations.Email;
using RL.API.Features.Identidad.Persistence;
using RL.API.Core.Security;
using Xunit;

namespace RL.API.Tests.Features.Identidad;

public sealed class IdentidadModuleCharacterizationTests
{
    [Fact]
    public void AuthController_ConservaRutaAnonimosYAdministracionModuloDos()
    {
        var type = typeof(AuthController);
        var route = Assert.Single(type.GetCustomAttributes(typeof(RouteAttribute), true).Cast<RouteAttribute>());
        var login = type.GetMethod(nameof(AuthController.Login))!;
        var refresh = type.GetMethod(nameof(AuthController.Refresh))!;
        var recuperar = type.GetMethod(nameof(AuthController.RecuperarPassword))!;
        var perfil = type.GetMethod(nameof(AuthController.Perfil))!;
        var crear = type.GetMethod(nameof(AuthController.CrearUsuario))!;

        Assert.Equal("api/[controller]", route.Template);
        Assert.NotNull(login.GetCustomAttributes(typeof(AllowAnonymousAttribute), true).SingleOrDefault());
        Assert.NotNull(refresh.GetCustomAttributes(typeof(AllowAnonymousAttribute), true).SingleOrDefault());
        Assert.NotNull(recuperar.GetCustomAttributes(typeof(AllowAnonymousAttribute), true).SingleOrDefault());
        Assert.NotNull(perfil.GetCustomAttributes(typeof(AuthorizeAttribute), true).SingleOrDefault());
        Assert.Equal("ADMINISTRADOR", Assert.Single(crear.GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>()).Roles);
        Assert.Equal(new[] { 2 }, ObtenerModulos(Assert.Single(crear.GetCustomAttributes(typeof(ModuloAuthorizeAttribute), true).Cast<ModuloAuthorizeAttribute>())));
    }

    [Fact]
    public void Perfil_ConservaClaimsPublicosYModulosNumericos()
    {
        var controller = CrearController(new AuthServiceFake(), new ActivoDirectorioServiceFake());
        controller.HttpContext.User = CrearUsuarioClaims();

        var result = Assert.IsType<OkObjectResult>(controller.Perfil());
        var json = JsonSerializer.Serialize(result.Value);

        Assert.Contains("\"uid\":\"uid-seguro\"", json, StringComparison.Ordinal);
        Assert.Contains("\"rol\":\"ADMINISTRADOR\"", json, StringComparison.Ordinal);
        Assert.Contains("\"modulosIds\":[2,5,9]", json, StringComparison.Ordinal);
        Assert.Contains("\"debeCambiarPassword\":true", json, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AuthService_LoginLocal_ConservaClaimsYVigenciaJwt()
    {
        var usuario = CrearUsuarioLocal("ClaveSegura123!");
        var repository = new UsuarioRepositoryFake { UsuarioPorLogin = usuario };
        var auditoria = new AuditoriaRepositoryFake();
        var service = CrearServicio(repository, auditoria, new ConfiguracionRepositoryFake());
        var inicio = DateTime.UtcNow;

        var response = await service.LoginAsync(new LoginRequestDto { Email = usuario.UsrEmail, Password = "ClaveSegura123!" }, "127.0.0.8");

        Assert.NotNull(response);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(response.AccessToken);
        Assert.Equal("sgrla-tests", token.Issuer);
        Assert.Contains("sgrla-client", token.Audiences);
        Assert.Equal("2,5,9", token.Claims.Single(c => c.Type == "modulos").Value);
        Assert.Equal("1", token.Claims.Single(c => c.Type == "debe_cambiar_pass").Value);
        Assert.InRange(response.ExpiresAt, inicio.AddMinutes(29), inicio.AddMinutes(31));
        Assert.Equal(response.RefreshToken, repository.RefreshTokenGuardado);
        Assert.InRange(repository.RefreshTokenExpira!.Value, inicio.AddDays(4.9), inicio.AddDays(5.1));
        Assert.Contains(auditoria.Registros, item => item.Accion == "LOGIN" && item.DatosNuevos!.Contains("EXITOSO", StringComparison.Ordinal));
    }

    [Fact]
    public async Task AuthService_LoginInvalido_IncrementaIntentosYAuditaSinEmitirToken()
    {
        var usuario = CrearUsuarioLocal("ClaveCorrecta123!");
        usuario.UsrIntentosFallidos = 1;
        var repository = new UsuarioRepositoryFake { UsuarioPorLogin = usuario };
        var auditoria = new AuditoriaRepositoryFake();
        var configuracion = new ConfiguracionRepositoryFake { Configuracion = new ConfigSistema { MaxIntentos = 5 } };
        var service = CrearServicio(repository, auditoria, configuracion);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.LoginAsync(new LoginRequestDto { Email = usuario.UsrEmail, Password = "Incorrecta" }, "127.0.0.9"));

        Assert.Contains("2 de 5", error.Message, StringComparison.Ordinal);
        Assert.Equal((2, null), repository.UltimoIntentoFallido);
        Assert.Null(repository.RefreshTokenGuardado);
        Assert.Contains(auditoria.Registros, item => item.Accion == "LOGIN" && item.DatosNuevos!.Contains("INTENTO_2_DE_5", StringComparison.Ordinal));
    }

    [Fact]
    public async Task AuthService_RefreshValido_RevocaYRotaToken()
    {
        const string tokenAnterior = "refresh-token-anterior";
        var usuario = CrearUsuarioLocal("ClaveSegura123!");
        var repository = new UsuarioRepositoryFake
        {
            UsuarioIdPorRefreshToken = usuario.UsrId,
            RefreshTokenValido = tokenAnterior,
            UsuarioPorId = usuario
        };
        var service = CrearServicio(repository, new AuditoriaRepositoryFake(), new ConfiguracionRepositoryFake());

        var response = await service.RefreshTokenAsync(tokenAnterior, "127.0.0.10");

        Assert.NotNull(response);
        Assert.Equal(new[] { tokenAnterior }, repository.TokensRevocados);
        Assert.Equal(response.RefreshToken, repository.RefreshTokenGuardado);
        Assert.NotEqual(tokenAnterior, response.RefreshToken);
        Assert.Equal(usuario.UsrId, response.Usuario.Id);
    }

    [Fact]
    public async Task AuthService_RefreshInexistente_NoRevocaNiGeneraToken()
    {
        var repository = new UsuarioRepositoryFake();
        var service = CrearServicio(repository, new AuditoriaRepositoryFake(), new ConfiguracionRepositoryFake());

        var response = await service.RefreshTokenAsync("token-inexistente", "127.0.0.11");

        Assert.Null(response);
        Assert.Empty(repository.TokensRevocados);
        Assert.Null(repository.RefreshTokenGuardado);
    }

    [Fact]
    public async Task AuthService_RefreshDeUsuarioInactivo_RevocaTokenSinRotarlo()
    {
        const string tokenAnterior = "refresh-token-inactivo";
        var usuario = CrearUsuarioLocal("ClaveSegura123!");
        usuario.UsrActivo = false;
        var repository = new UsuarioRepositoryFake
        {
            UsuarioIdPorRefreshToken = usuario.UsrId,
            RefreshTokenValido = tokenAnterior,
            UsuarioPorId = usuario
        };
        var service = CrearServicio(repository, new AuditoriaRepositoryFake(), new ConfiguracionRepositoryFake());

        var response = await service.RefreshTokenAsync(tokenAnterior, "127.0.0.12");

        Assert.Null(response);
        Assert.Equal(new[] { tokenAnterior }, repository.TokensRevocados);
        Assert.Null(repository.RefreshTokenGuardado);
    }

    private static AuthController CrearController(IAuthService auth, IActivoDirectorioService ad)
    {
        var controller = new AuthController(auth, ad, NullLogger<AuthController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        return controller;
    }

    private static AuthService CrearServicio(IUsuarioRepository usuarios, IAuditoriaRepository auditoria, IConfiguracionRepository configuracion)
    {
        var values = new Dictionary<string, string?>
        {
            ["Jwt:SecretKey"] = "clave-de-prueba-segura-con-32-caracteres-minimo-2026",
            ["Jwt:Issuer"] = "sgrla-tests",
            ["Jwt:Audience"] = "sgrla-client",
            ["Jwt:AccessTokenExpirationMinutes"] = "30",
            ["Jwt:RefreshTokenExpirationDays"] = "5"
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        return new AuthService(usuarios, auditoria, configuration, new ActivoDirectorioServiceFake(), configuracion, new EmailServiceFake());
    }

    private static ClaimsPrincipal CrearUsuarioClaims()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "27"),
            new Claim("uid", "uid-seguro"),
            new Claim(ClaimTypes.Name, "Ana Pérez"),
            new Claim(ClaimTypes.GivenName, "Ana"),
            new Claim(ClaimTypes.Surname, "Pérez"),
            new Claim(ClaimTypes.Email, "ana@ihss.hn"),
            new Claim(ClaimTypes.Role, "ADMINISTRADOR"),
            new Claim("rol_id", "1"),
            new Claim("es_dom", "0"),
            new Claim("modulos", "2, 5, invalido, 9"),
            new Claim("debe_cambiar_pass", "1")
        };
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
    }

    private static Usuario CrearUsuarioLocal(string password) => new()
    {
        UsrId = 27,
        UsrNombre = "Ana",
        UsrApellido = "Pérez",
        UsrEmail = "ana@ihss.hn",
        UsrPasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
        UsrPasswordSalt = "BCRYPT",
        UsrRolId = 1,
        UsrActivo = true,
        EsUsuarioDominio = 0,
        UsrDebeCambiarPass = 1,
        ModulosIds = new List<int> { 2, 5, 9 },
        Rol = new Rol { RolId = 1, RolNombre = "ADMINISTRADOR", RolActivo = true }
    };

    private static int[] ObtenerModulos(ModuloAuthorizeAttribute attribute)
    {
        var field = typeof(ModuloAuthorizeAttribute).GetField("_moduloIds", BindingFlags.Instance | BindingFlags.NonPublic)!;
        return Assert.IsType<HashSet<int>>(field.GetValue(attribute)).OrderBy(id => id).ToArray();
    }

    private sealed class AuthServiceFake : IAuthService
    {
        public Task<LoginResponseDto?> LoginAsync(LoginRequestDto dto, string ip) => Task.FromResult<LoginResponseDto?>(null);
        public Task<LoginResponseDto?> RefreshTokenAsync(string refreshToken, string ip) => Task.FromResult<LoginResponseDto?>(null);
        public Task LogoutAsync(long usrId, string refreshToken) => Task.CompletedTask;
        public Task<bool> CambiarPasswordAsync(long usrId, CambiarPasswordDto dto) => Task.FromResult(false);
        public Task<UsuarioInfoDto?> CrearUsuarioAsync(CrearUsuarioDto dto, long creadoPor) => Task.FromResult<UsuarioInfoDto?>(null);
        public Task<bool> ActualizarUsuarioAsync(string uid, ActualizarUsuarioDto dto, long actualizadoPor) => Task.FromResult(false);
        public Task<List<UsuarioInfoDto>> ListarUsuariosAsync() => Task.FromResult(new List<UsuarioInfoDto>());
        public Task<bool> ActualizarEstadoUsuarioAsync(string uid, bool activo, long actualizadoPor) => Task.FromResult(false);
        public Task<bool> RecuperarPasswordAsync(string email) => Task.FromResult(false);
    }

    private sealed class UsuarioRepositoryFake : IUsuarioRepository
    {
        public Usuario? UsuarioPorLogin { get; init; }
        public Usuario? UsuarioPorId { get; init; }
        public long? UsuarioIdPorRefreshToken { get; init; }
        public string? RefreshTokenValido { get; init; }
        public string? RefreshTokenGuardado { get; private set; }
        public DateTime? RefreshTokenExpira { get; private set; }
        public (int Intentos, DateTime? Bloqueo)? UltimoIntentoFallido { get; private set; }
        public List<string> TokensRevocados { get; } = new();

        public Task<Usuario?> ObtenerPorEmailAsync(string email) => Task.FromResult<Usuario?>(null);
        public Task<Usuario?> ObtenerPorLoginAsync(string identifier) => Task.FromResult(UsuarioPorLogin);
        public Task<Usuario?> ObtenerPorIdAsync(long id) => Task.FromResult(UsuarioPorId);
        public Task<long> CrearAsync(CrearUsuarioDto dto, string hash, string salt) => Task.FromResult(0L);
        public Task<bool> ActualizarPasswordAsync(long usrId, string hash, string salt) => Task.FromResult(false);
        public Task<bool> ForzarCambioPasswordAsync(long usrId, string hash, string salt) => Task.FromResult(false);
        public Task<long?> BuscarUsuarioIdPorRefreshTokenAsync(string token) => Task.FromResult(UsuarioIdPorRefreshToken);
        public Task<string?> ObtenerRefreshTokenAsync(long usrId, string token) => Task.FromResult(token == RefreshTokenValido ? token : null);
        public Task GuardarRefreshTokenAsync(long usrId, string token, DateTime expira, string? ip) { RefreshTokenGuardado = token; RefreshTokenExpira = expira; return Task.CompletedTask; }
        public Task RevocarRefreshTokenAsync(string token) { TokensRevocados.Add(token); return Task.CompletedTask; }
        public Task RevocarTodosTokensAsync(long usrId) => Task.CompletedTask;
        public Task<List<UsuarioInfoDto>> ListarAsync() => Task.FromResult(new List<UsuarioInfoDto>());
        public Task<bool> ActualizarAsync(long id, ActualizarUsuarioDto dto, string? hash, string? salt) => Task.FromResult(false);
        public Task<bool> ActualizarEstadoAsync(long id, bool activo) => Task.FromResult(false);
        public Task<List<int>> ObtenerModulosIdsPorUsuarioAsync(long usrId) => Task.FromResult(new List<int>());
        public Task RegistrarIntentoFallidoAsync(long usrId, int nuevosIntentos, DateTime? fechaBloqueo) { UltimoIntentoFallido = (nuevosIntentos, fechaBloqueo); return Task.CompletedTask; }
        public Task RestablecerIntentosAsync(long usrId) => Task.CompletedTask;
    }

    private sealed class AuditoriaRepositoryFake : IAuditoriaRepository
    {
        public List<AuditoriaRegistro> Registros { get; } = new();
        public Task RegistrarAsync(string tabla, string registroId, string accion, string? datosAnt, string? datosNvo, long? usrId, string? email, string? ip, string? modulo) { Registros.Add(new AuditoriaRegistro(accion, datosNvo)); return Task.CompletedTask; }
        public Task<(List<AuditoriaDto> Datos, int Total)> ObtenerBitacoraPaginadaAsync(int pagina, int limite, string? buscar, string? accion, string? modulo, string? tabla, DateTime? fechaInicio, DateTime? fechaFin) => Task.FromResult((new List<AuditoriaDto>(), 0));
    }

    private sealed class ConfiguracionRepositoryFake : IConfiguracionRepository
    {
        public ConfigSistema? Configuracion { get; init; }
        public Task<ConfigSistema?> ObtenerConfigSistemaAsync() => Task.FromResult(Configuracion);
        public Task<List<LoginSlide>> ObtenerSlidesAsync() => Task.FromResult(new List<LoginSlide>());
        public Task<List<LoginSlide>> ObtenerTodosSlidesAsync() => Task.FromResult(new List<LoginSlide>());
        public Task<bool> GuardarConfigSistemaAsync(ConfigSistema config) => Task.FromResult(false);
        public Task<bool> CrearSlideAsync(LoginSlide slide) => Task.FromResult(false);
        public Task<bool> ActualizarSlideAsync(LoginSlide slide) => Task.FromResult(false);
        public Task<bool> EliminarSlideAsync(int id) => Task.FromResult(false);
    }

    private sealed class ActivoDirectorioServiceFake : IActivoDirectorioService
    {
        public Task<ResultadoValidacionAdDto> ValidarUsuarioAsync(string usuario, string dominio) => Task.FromResult(new ResultadoValidacionAdDto());
        public Task<bool> AutenticarAsync(string usuario, string dominio, string password) => Task.FromResult(false);
    }

    private sealed class EmailServiceFake : IEmailService
    {
        public Task EnviarCorreoAsync(string destinatario, string asunto, string cuerpo, bool esHtml = true) => Task.CompletedTask;
    }

    private sealed record AuditoriaRegistro(string Accion, string? DatosNuevos);
}
