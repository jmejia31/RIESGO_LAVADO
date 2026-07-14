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
            _ => throw new InvalidOperationException(detalleSensible),
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
        Assert.DoesNotContain(detalleSensible, body, StringComparison.Ordinal);
        Assert.Contains("trace-prueba-123", body, StringComparison.Ordinal);
    }
}
