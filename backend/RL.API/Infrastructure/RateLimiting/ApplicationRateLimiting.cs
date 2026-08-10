using System.Globalization;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace RL.API.Infrastructure.RateLimiting;

/// <summary>
/// Nombres únicos de las políticas BE-04 aplicadas en los endpoints sensibles.
/// </summary>
public static class RateLimitPolicies
{
    public const string Login = "be04-login";
    public const string PasswordRecovery = "be04-password-recovery";
    public const string RefreshToken = "be04-refresh-token";
    public const string ReportExport = "be04-report-export";
    public const string EvidenceUpload = "be04-evidence-upload";
    public const string Unlimited = "be04-unlimited";
}

/// <summary>
/// Límites operativos configurables. Los valores se normalizan al construir cada limiter
/// para impedir configuraciones nulas, negativas o excesivas.
/// </summary>
public sealed class RateLimitingSettings
{
    public int LoginPermitLimit { get; set; } = 5;
    public int LoginWindowSeconds { get; set; } = 60;

    public int PasswordRecoveryPermitLimit { get; set; } = 3;
    public int PasswordRecoveryWindowSeconds { get; set; } = 900;

    public int RefreshTokenPermitLimit { get; set; } = 20;
    public int RefreshTokenWindowSeconds { get; set; } = 60;

    public int ReportExportPermitLimit { get; set; } = 6;
    public int ReportExportWindowSeconds { get; set; } = 60;

    public int EvidenceUploadPermitLimit { get; set; } = 10;
    public int EvidenceUploadWindowSeconds { get; set; } = 60;
}

/// <summary>
/// Construye particiones de rate limiting sin confiar en cabeceras reenviadas no validadas.
/// Operaciones anónimas se aíslan por RemoteIpAddress; operaciones autenticadas por usuario.
/// </summary>
public static class ApplicationRateLimitPartitions
{
    public static RateLimitPartition<string> ForRequest(HttpContext context, RateLimitingSettings settings)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        var method = context.Request.Method;

        if (HttpMethods.IsPost(method) && PathEquals(path, "/api/auth/login"))
            return Login(context, settings);

        if (HttpMethods.IsPost(method) && PathEquals(path, "/api/auth/recuperar-password"))
            return PasswordRecovery(context, settings);

        if (HttpMethods.IsPost(method) && PathEquals(path, "/api/auth/refresh"))
            return RefreshToken(context, settings);

        if (HttpMethods.IsGet(method) &&
            (PathEquals(path, "/api/matrices-riesgos/reportes/consolidado.xlsx") ||
             PathEquals(path, "/api/matrices-riesgos/reportes/consolidado.pdf")))
        {
            return ReportExport(context, settings);
        }

        if (HttpMethods.IsPost(method) && PathEquals(path, "/api/matrices-riesgos/evidencias/cargar"))
            return EvidenceUpload(context, settings);

        return RateLimitPartition.GetNoLimiter<string>(RateLimitPolicies.Unlimited);
    }

    public static RateLimitPartition<string> Login(HttpContext context, RateLimitingSettings settings) =>
        FixedWindow(
            $"{RateLimitPolicies.Login}:{AnonymousPartitionKey(context)}",
            settings.LoginPermitLimit,
            settings.LoginWindowSeconds,
            maxPermitLimit: 30,
            maxWindowSeconds: 3600);

    public static RateLimitPartition<string> PasswordRecovery(HttpContext context, RateLimitingSettings settings) =>
        FixedWindow(
            $"{RateLimitPolicies.PasswordRecovery}:{AnonymousPartitionKey(context)}",
            settings.PasswordRecoveryPermitLimit,
            settings.PasswordRecoveryWindowSeconds,
            maxPermitLimit: 20,
            maxWindowSeconds: 3600);

    public static RateLimitPartition<string> RefreshToken(HttpContext context, RateLimitingSettings settings) =>
        FixedWindow(
            $"{RateLimitPolicies.RefreshToken}:{AnonymousPartitionKey(context)}",
            settings.RefreshTokenPermitLimit,
            settings.RefreshTokenWindowSeconds,
            maxPermitLimit: 100,
            maxWindowSeconds: 3600);

    public static RateLimitPartition<string> ReportExport(HttpContext context, RateLimitingSettings settings) =>
        FixedWindow(
            $"{RateLimitPolicies.ReportExport}:{AuthenticatedPartitionKey(context)}",
            settings.ReportExportPermitLimit,
            settings.ReportExportWindowSeconds,
            maxPermitLimit: 60,
            maxWindowSeconds: 3600);

    public static RateLimitPartition<string> EvidenceUpload(HttpContext context, RateLimitingSettings settings) =>
        FixedWindow(
            $"{RateLimitPolicies.EvidenceUpload}:{AuthenticatedPartitionKey(context)}",
            settings.EvidenceUploadPermitLimit,
            settings.EvidenceUploadWindowSeconds,
            maxPermitLimit: 60,
            maxWindowSeconds: 3600);

    public static string AnonymousPartitionKey(HttpContext context) =>
        $"ip:{NormalizeIp(context.Connection.RemoteIpAddress)}";

    public static string AuthenticatedPartitionKey(HttpContext context)
    {
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return !string.IsNullOrWhiteSpace(userId)
            ? $"user:{userId.Trim()}"
            : AnonymousPartitionKey(context);
    }

    private static bool PathEquals(string actual, string expected) =>
        string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);

    private static string NormalizeIp(System.Net.IPAddress? address)
    {
        if (address is null)
            return "unknown";

        if (address.IsIPv4MappedToIPv6)
            address = address.MapToIPv4();

        return address.ToString();
    }

    private static RateLimitPartition<string> FixedWindow(
        string partitionKey,
        int configuredPermitLimit,
        int configuredWindowSeconds,
        int maxPermitLimit,
        int maxWindowSeconds)
    {
        var permitLimit = Math.Clamp(configuredPermitLimit, 1, maxPermitLimit);
        var windowSeconds = Math.Clamp(configuredWindowSeconds, 1, maxWindowSeconds);

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromSeconds(windowSeconds),
                QueueLimit = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                AutoReplenishment = true
            });
    }
}

public static class ApplicationRateLimitingServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var settings = configuration.GetSection("RateLimiting").Get<RateLimitingSettings>()
            ?? new RateLimitingSettings();

        services.AddSingleton(settings);
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
                context => ApplicationRateLimitPartitions.ForRequest(context, settings));

            options.AddPolicy(
                RateLimitPolicies.Login,
                context => ApplicationRateLimitPartitions.Login(context, settings));
            options.AddPolicy(
                RateLimitPolicies.PasswordRecovery,
                context => ApplicationRateLimitPartitions.PasswordRecovery(context, settings));
            options.AddPolicy(
                RateLimitPolicies.RefreshToken,
                context => ApplicationRateLimitPartitions.RefreshToken(context, settings));
            options.AddPolicy(
                RateLimitPolicies.ReportExport,
                context => ApplicationRateLimitPartitions.ReportExport(context, settings));
            options.AddPolicy(
                RateLimitPolicies.EvidenceUpload,
                context => ApplicationRateLimitPartitions.EvidenceUpload(context, settings));

            options.OnRejected = async (rejectionContext, cancellationToken) =>
            {
                var response = rejectionContext.HttpContext.Response;
                response.StatusCode = StatusCodes.Status429TooManyRequests;
                response.ContentType = "application/problem+json";

                if (rejectionContext.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    var seconds = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds));
                    response.Headers.RetryAfter = seconds.ToString(CultureInfo.InvariantCulture);
                }

                var problem = new ProblemDetails
                {
                    Type = "https://www.rfc-editor.org/rfc/rfc6585#section-4",
                    Title = "Demasiadas solicitudes",
                    Status = StatusCodes.Status429TooManyRequests,
                    Detail = "Se alcanzó temporalmente el límite de solicitudes para esta operación. Intente nuevamente más tarde.",
                    Instance = rejectionContext.HttpContext.Request.Path
                };
                problem.Extensions["traceId"] = rejectionContext.HttpContext.TraceIdentifier;

                await response.WriteAsJsonAsync(problem, cancellationToken: cancellationToken);
            };
        });

        return services;
    }
}
