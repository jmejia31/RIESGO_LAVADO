using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using RL.API.Middleware;
using System.Runtime.Versioning;
using Xunit;

namespace RL.API.Tests.Middleware;

[SupportedOSPlatform("windows")]
public sealed class ErrorHandlingMiddlewareTests
{
    [Fact]
    public async Task Invoke_ExcepcionNoControlada_OcultaDetalleYDevuelveTraceId()
    {
        const string detalleSensible = "ORA-00942: tabla RL_SECRETA no existe";
        var middleware = new ErrorHandlingMiddleware(
            _ => throw new Exception(detalleSensible),
            NullLogger<ErrorHandlingMiddleware>.Instance);
        var context = new DefaultHttpContext
        {
            TraceIdentifier = "trace-prueba-123",
            Response = { Body = new MemoryStream() }
        };

        await middleware.Invoke(context);

        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        var body = await reader.ReadToEndAsync();

        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.Equal("application/problem+json", context.Response.ContentType);
        Assert.DoesNotContain(detalleSensible, body, StringComparison.Ordinal);
        Assert.Contains("trace-prueba-123", body, StringComparison.Ordinal);
        Assert.Contains("application/problem+json", context.Response.ContentType, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Invoke_ValidacionFallida_Retorna400BadRequestProblemDetails()
    {
        var middleware = new ErrorHandlingMiddleware(
            _ => throw new ArgumentException("El campo nombre es requerido"),
            NullLogger<ErrorHandlingMiddleware>.Instance);
        var context = new DefaultHttpContext
        {
            TraceIdentifier = "trace-400-test",
            Response = { Body = new MemoryStream() }
        };

        await middleware.Invoke(context);

        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        var body = await reader.ReadToEndAsync();

        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        Assert.Contains("Solicitud incorrecta", body, StringComparison.Ordinal);
        Assert.Contains("El campo nombre es requerido", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Invoke_RecursoNoEncontrado_Retorna404NotFoundProblemDetails()
    {
        var middleware = new ErrorHandlingMiddleware(
            _ => throw new KeyNotFoundException("No se encontró la matriz especificada"),
            NullLogger<ErrorHandlingMiddleware>.Instance);
        var context = new DefaultHttpContext
        {
            TraceIdentifier = "trace-404-test",
            Response = { Body = new MemoryStream() }
        };

        await middleware.Invoke(context);

        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        var body = await reader.ReadToEndAsync();

        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
        Assert.Contains("Recurso no encontrado", body, StringComparison.Ordinal);
        Assert.Contains("No se encontró la matriz especificada", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Invoke_ExcepcionConSqlSensible_SanitizaMensajeYDevuelveFallback()
    {
        var middleware = new ErrorHandlingMiddleware(
            _ => throw new ArgumentException("Error en consulta ORA-00942: SELECT * FROM RL_MR_MODELOS"),
            NullLogger<ErrorHandlingMiddleware>.Instance);
        var context = new DefaultHttpContext
        {
            TraceIdentifier = "trace-sanitizar-test",
            Response = { Body = new MemoryStream() }
        };

        await middleware.Invoke(context);

        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        var body = await reader.ReadToEndAsync();

        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        Assert.DoesNotContain("ORA-00942", body, StringComparison.Ordinal);
        Assert.DoesNotContain("SELECT", body, StringComparison.Ordinal);
        Assert.Contains("La solicitud contiene parámetros no válidos o incompletos.", body, StringComparison.Ordinal);
    }
}


