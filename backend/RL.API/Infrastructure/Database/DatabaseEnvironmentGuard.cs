using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;

namespace RL.API.Infrastructure.Database;

/// <summary>
/// Protección fail-closed estricta para impedir que el entorno Development se conecte
/// accidental o silenciosamente a bases de datos productivas o externas no autorizadas.
/// Implementa una política positiva (allowlist estricto de host local y service name XE).
/// </summary>
public static class DatabaseEnvironmentGuard
{
    private static readonly HashSet<string> AllowedDevelopmentHosts = new(StringComparer.OrdinalIgnoreCase)
    {
        "localhost",
        "127.0.0.1",
        "::1"
    };

    private static readonly HashSet<string> AllowedDevelopmentServices = new(StringComparer.OrdinalIgnoreCase)
    {
        "XE"
    };

    public static string ValidateAndResolveDevelopmentConnection(string connectionString, IConfiguration configuration)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("STARTUP_REFUSED: ConnectionStrings:OracleDB no está configurada para el entorno Development.");
        }

        // 1. Extraer HOST
        var hostMatch = Regex.Match(connectionString, @"(?i)HOST\s*=\s*([^\)\s;]+)");
        if (!hostMatch.Success || string.IsNullOrWhiteSpace(hostMatch.Groups[1].Value))
        {
            throw new InvalidOperationException("STARTUP_REFUSED: Cadena de conexión Oracle malformada o sin parámetro HOST requerido.");
        }
        var host = hostMatch.Groups[1].Value.Trim();

        // 2. Extraer SERVICE_NAME
        var serviceMatch = Regex.Match(connectionString, @"(?i)SERVICE_NAME\s*=\s*([^\)\s;]+)");
        if (!serviceMatch.Success || string.IsNullOrWhiteSpace(serviceMatch.Groups[1].Value))
        {
            throw new InvalidOperationException("STARTUP_REFUSED: Cadena de conexión Oracle malformada o sin parámetro SERVICE_NAME requerido.");
        }
        var serviceName = serviceMatch.Groups[1].Value.Trim();

        // 3. Bloqueo explícito de producción histórica
        if (serviceName.Contains("hpprod", StringComparison.OrdinalIgnoreCase) ||
            serviceName.Equals("prod", StringComparison.OrdinalIgnoreCase) ||
            host.Equals("10.1.19.112", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("STARTUP_REFUSED: El entorno Development no tiene permitido conectarse a la base de datos de producción (target productivo detectado).");
        }

        // 4. Política positiva: Allowlist estricto de Service Name en Development
        if (!AllowedDevelopmentServices.Contains(serviceName))
        {
            throw new InvalidOperationException($"STARTUP_REFUSED: El servicio Oracle '{serviceName}' no está autorizado para Development. Solo se permite el servicio local XE.");
        }

        // 5. Política positiva: Allowlist estricto de Host en Development (solo localhost / 127.0.0.1)
        if (!AllowedDevelopmentHosts.Contains(host))
        {
            throw new InvalidOperationException($"STARTUP_REFUSED: El host '{host}' no está en la lista de destinos de desarrollo permitidos (localhost, 127.0.0.1). El entorno Development solo puede conectarse a una base de datos local.");
        }

        // 6. Resolución segura de password si no viene en el connection string
        var resolved = connectionString;
        if (!resolved.Contains("Password=", StringComparison.OrdinalIgnoreCase))
        {
            var pwd = configuration["RL_ORACLE_PASSWORD"]
                   ?? configuration["OraclePassword"]
                   ?? configuration["ConnectionStrings:OraclePassword"];
            if (!string.IsNullOrWhiteSpace(pwd))
            {
                resolved = resolved.TrimEnd(';') + $";Password={pwd};";
            }
        }

        return resolved;
    }
}
