using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RL.API.Exceptions;

namespace RL.API.Middleware;

public class ErrorHandlingMiddleware
{
    private const string DefaultBadRequestMessage = "La solicitud contiene parámetros no válidos o incompletos.";
    private const string DefaultForbiddenMessage = "No tiene privilegios suficientes para realizar esta acción.";
    private const string DefaultNotFoundMessage = "El recurso solicitado no existe o no se encuentra disponible.";
    private const string DefaultInternalErrorMessage = "Ocurrió un error interno en el servidor. Por favor intente más tarde.";

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
            _logger.LogError(ex, "Excepción no controlada en la petición HTTP. TraceId: {TraceId}", context.TraceIdentifier);
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
            // Única vía para exponer un mensaje de excepción al cliente: debe ser declarado
            // explícitamente como mensaje público por la capa de dominio/aplicación.
            case PublicProblemException publicProblem:
                statusCode = publicProblem.StatusCode;
                title = publicProblem.Title;
                type = publicProblem.Type;
                detail = publicProblem.Message;
                break;

            // Excepciones genéricas conservan la semántica HTTP, pero jamás publican exception.Message.
            case ArgumentException:
                statusCode = StatusCodes.Status400BadRequest;
                title = "Solicitud incorrecta";
                type = "https://tools.ietf.org/html/rfc7231#section-6.5.1";
                detail = DefaultBadRequestMessage;
                break;

            case KeyNotFoundException:
                statusCode = StatusCodes.Status404NotFound;
                title = "Recurso no encontrado";
                type = "https://tools.ietf.org/html/rfc7231#section-6.5.4";
                detail = DefaultNotFoundMessage;
                break;

            case UnauthorizedAccessException:
                statusCode = StatusCodes.Status403Forbidden;
                title = "Acceso no autorizado";
                type = "https://tools.ietf.org/html/rfc7231#section-6.5.3";
                detail = DefaultForbiddenMessage;
                break;

            // InvalidOperationException y cualquier excepción técnica no clasificada son 500.
            // En Development se conserva el detalle para diagnóstico local; Producción/Staging reciben fallback fijo.
            default:
                statusCode = StatusCodes.Status500InternalServerError;
                title = "Error interno del servidor";
                type = "https://tools.ietf.org/html/rfc7231#section-6.6.1";
                detail = env != null && env.IsDevelopment()
                    ? exception.Message
                    : DefaultInternalErrorMessage;
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
