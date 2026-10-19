using Microsoft.Extensions.Configuration;

namespace RL.API.Infrastructure.Database;

/// <summary>
/// Compatibilidad para herramientas antiguas; la selección canónica vive en DatabaseProfileResolver.
/// </summary>
public static class DatabaseEnvironmentGuard
{
    public static string ValidateAndResolveDevelopmentConnection(string baseConnectionString, IConfiguration configuration)
    {
        _ = baseConnectionString; // La clave genérica nunca es un fallback permitido.
        return DatabaseProfileResolver.Resolve(configuration).ConnectionString;
    }
}
