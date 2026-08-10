using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace RL.API.Middleware;

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;
    private readonly IHostEnvironment? _env;

    public ErrorHandlingMiddleware(
        RequestDelegate next,
        ILogger<ErrorHandlingMiddleware> logger,
        IHostEnvironment? env = null)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción no controlada en la petición HTTP.");
            await HandleExceptionAsync(context, ex, _env);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception, IHostEnvironment? env)
    {
        context.Response.ContentType = "application/problem+json";

        int statusCode;
        string title;
        string type;
        string detail;

        switch (exception)
        {
            case ArgumentException or InvalidOperationException when exception.Message.Contains("no válido") || exception.Message.Contains("requerido"):
                statusCode = StatusCodes.Status400BadRequest;
                title = "Solicitud incorrecta";
                type = "https://tools.ietf.org/html/rfc7231#section-6.5.1";
                detail = exception.Message;
                break;
            case KeyNotFoundException:
                statusCode = StatusCodes.Status404NotFound;
                title = "Recurso no encontrado";
                type = "https://tools.ietf.org/html/rfc7231#section-6.5.4";
                detail = exception.Message;
                break;
            case UnauthorizedAccessException:
                statusCode = StatusCodes.Status403Forbidden;
                title = "Acceso no autorizado";
                type = "https://tools.ietf.org/html/rfc7231#section-6.5.3";
                detail = exception.Message;
                break;
            default:
                statusCode = StatusCodes.Status500InternalServerError;
                title = "Error interno del servidor";
                type = "https://tools.ietf.org/html/rfc7231#section-6.6.1";
                detail = (env != null && env.IsDevelopment())
                    ? exception.Message
                    : "Ocurrió un error interno en el servidor. Por favor intente más tarde.";
                break;
        }

        context.Response.StatusCode = statusCode;

        var problemDetails = new
        {
            type,
            title,
            status = statusCode,
            detail,
            mensaje = detail,
            instance = context.Request.Path.HasValue ? context.Request.Path.Value : null,
            traceId = context.TraceIdentifier
        };

        var json = JsonConvert.SerializeObject(problemDetails);
        return context.Response.WriteAsync(json);
    }
}

