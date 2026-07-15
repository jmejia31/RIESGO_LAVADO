using System.DirectoryServices.AccountManagement;
using System.Runtime.Versioning;

namespace RL.API.Features.Identidad.Integrations.ActiveDirectory;

[SupportedOSPlatform("windows")]
public class ActiveDirectorioService : IActivoDirectorioService
{
    private readonly IConfiguration _config;
    private readonly ILogger<ActiveDirectorioService> _logger;

    public ActiveDirectorioService(IConfiguration config, ILogger<ActiveDirectorioService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task<ResultadoValidacionAdDto> ValidarUsuarioAsync(string usuario, string dominio)
    {
        int timeoutMs = _config.GetValue<int>("ActiveDirectory:TimeoutSegundos", 15) * 1000;
        using var cts = new CancellationTokenSource(timeoutMs);
        var task = Task.Run(() => ConsultarAd(usuario, dominio.ToUpper().Trim()), cts.Token);
        if (await Task.WhenAny(task, Task.Delay(timeoutMs)) != task)
        {
            _logger.LogWarning("Timeout al consultar AD para usuario '{Usuario}' en dominio '{Dominio}'", usuario, dominio);
            throw new InvalidOperationException($"La consulta al servidor de Active Directory tomó más de {timeoutMs / 1000}s. Intente nuevamente.");
        }
        return await task;
    }

    public async Task<bool> AutenticarAsync(string usuario, string dominio, string password)
    {
        int timeoutMs = _config.GetValue<int>("ActiveDirectory:TimeoutSegundos", 15) * 1000;
        using var cts = new CancellationTokenSource(timeoutMs);
        var task = Task.Run(() => ValidarCredencialesAd(usuario, dominio.ToUpper().Trim(), password), cts.Token);
        if (await Task.WhenAny(task, Task.Delay(timeoutMs)) != task)
        {
            _logger.LogWarning("Timeout autenticando usuario '{Usuario}' en dominio '{Dominio}'", usuario, dominio);
            throw new InvalidOperationException($"El servidor de Active Directory no respondió en {timeoutMs / 1000}s. Intente nuevamente.");
        }
        return await task;
    }

    private bool ValidarCredencialesAd(string usuario, string codigoDominio, string password)
    {
        var cfg = ObtenerConfigDominio(codigoDominio);
        if (cfg == null)
            throw new InvalidOperationException($"El dominio '{codigoDominio}' no está configurado en el sistema.");

        try
        {
            using var context = (!string.IsNullOrWhiteSpace(cfg.Usuario) && !string.IsNullOrWhiteSpace(cfg.Password))
                ? new PrincipalContext(ContextType.Domain, cfg.Servidor, cfg.Container,
                    ContextOptions.Negotiate, cfg.Usuario, cfg.Password)
                : new PrincipalContext(ContextType.Domain, cfg.Servidor, cfg.Container);

            return context.ValidateCredentials(usuario, password, ContextOptions.Negotiate);
        }
        catch (PrincipalServerDownException ex)
        {
            _logger.LogError(ex, "No se pudo conectar al DC '{Servidor}' del dominio '{Dominio}'", cfg.Servidor, codigoDominio);
            throw new InvalidOperationException(
                $"No se pudo conectar al servidor de Active Directory del dominio {codigoDominio}. Verifique la configuración.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error autenticando usuario '{Usuario}' en dominio '{Dominio}'", usuario, codigoDominio);
            throw new InvalidOperationException($"Error al autenticar contra Active Directory: {ex.Message}");
        }
    }

    private ResultadoValidacionAdDto ConsultarAd(string usuario, string codigoDominio)
    {
        var cfg = ObtenerConfigDominio(codigoDominio);

        if (cfg == null)
        {
            return new ResultadoValidacionAdDto
            {
                Existe   = false,
                Bloqueado = false,
                Activo   = false,
                Mensaje  = $"El dominio '{codigoDominio}' no está configurado en el sistema."
            };
        }

        try
        {
            using var context = (!string.IsNullOrWhiteSpace(cfg.Usuario) && !string.IsNullOrWhiteSpace(cfg.Password))
                ? new PrincipalContext(ContextType.Domain, cfg.Servidor, cfg.Container,
                    ContextOptions.Negotiate, cfg.Usuario, cfg.Password)
                : new PrincipalContext(ContextType.Domain, cfg.Servidor, cfg.Container);

            var userPrincipal = UserPrincipal.FindByIdentity(context, IdentityType.SamAccountName, usuario);

            if (userPrincipal == null)
            {
                return new ResultadoValidacionAdDto
                {
                    Existe    = false,
                    Bloqueado = false,
                    Activo    = false,
                    Mensaje   = $"El usuario '{usuario}' no existe en el dominio {codigoDominio}."
                };
            }

            bool bloqueado = userPrincipal.IsAccountLockedOut();
            bool activo    = userPrincipal.Enabled ?? false;

            return new ResultadoValidacionAdDto
            {
                Existe        = true,
                Bloqueado     = bloqueado,
                Activo        = activo,
                NombreCompleto = userPrincipal.DisplayName ?? userPrincipal.Name,
                Mensaje       = bloqueado
                    ? $"La cuenta '{usuario}' está bloqueada en el dominio {codigoDominio}."
                    : !activo
                        ? $"La cuenta '{usuario}' está deshabilitada en el dominio {codigoDominio}."
                        : $"Usuario '{usuario}' verificado correctamente en dominio {codigoDominio}."
            };
        }
        catch (PrincipalServerDownException ex)
        {
            _logger.LogError(ex, "No se pudo conectar al DC '{Servidor}' del dominio '{Dominio}'", cfg.Servidor, codigoDominio);
            throw new InvalidOperationException(
                $"No se pudo conectar al servidor de Active Directory del dominio {codigoDominio}. Verifique la configuración.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error consultando AD para usuario '{Usuario}' en dominio '{Dominio}'", usuario, codigoDominio);
            throw new InvalidOperationException($"Error al consultar Active Directory: {ex.Message}");
        }
    }

    private ConfigDominio? ObtenerConfigDominio(string codigoDominio)
    {
        var seccion = _config.GetSection($"ActiveDirectory:Dominios:{codigoDominio}");
        if (!seccion.Exists()) return null;

        return new ConfigDominio
        {
            Servidor  = seccion["Servidor"]  ?? string.Empty,
            Container = seccion["Container"] ?? string.Empty,
            Usuario   = seccion["Usuario"],
            Password  = seccion["Password"]
        };
    }
}
