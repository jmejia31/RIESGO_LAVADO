using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using RL.API.Exceptions;
using RL.API.Middleware;
using System.Runtime.Versioning;
using Xunit;

namespace RL.API.Tests.Middleware;

[SupportedOSPlatform("windows")]
public sealed class ErrorHandlingMiddlewareTests
{
    [Theory]
    [InlineData("Development")]
    [InlineData("Production")]
    public async Task Invoke_ExcepcionTecnicaEnCualquierEntorno_DevuelveContratoSeguroYConservaLog(string environmentName)
    {
        const string detalleSensible = "ORA-00942 | SELECT * FROM SECRET_TABLE | C:\\internal\\secret.config | Server=internal-db | Password=FakeSecretForTest | Internal.Namespace.SecretRepository";
        const string traceId = "trace-safe-error-500";
        var exception = new InvalidOperationException(detalleSensible);
        var logger = new CapturingLogger<ErrorHandlingMiddleware>();
        var environment = new TestHostEnvironment(environmentName);

        var (context, body) = await EjecutarAsync(exception, traceId, environment, logger);
        using var response = System.Text.Json.JsonDocument.Parse(body);

        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.Equal("application/problem+json", context.Response.ContentType);
        Assert.Equal(StatusCodes.Status500InternalServerError, response.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("Ocurrió un error interno en el servidor. Por favor intente más tarde.", response.RootElement.GetProperty("detail").GetString());
        Assert.Equal(traceId, response.RootElement.GetProperty("traceId").GetString());
        Assert.DoesNotContain(detalleSensible, body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ORA-00942", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SELECT * FROM SECRET_TABLE", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("C:\\internal\\secret.config", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Password=FakeSecretForTest", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Internal.Namespace.SecretRepository", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("StackTrace", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("InnerException", body, StringComparison.OrdinalIgnoreCase);
        Assert.Same(exception, logger.Exception);
        Assert.Contains(traceId, logger.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Invoke_ExcepcionNoControlada_OcultaDetalleYDevuelveTraceId()
    {
        const string detalleSensible = "ORA-00942: tabla RL_SECRETA no existe";
        var (context, body) = await EjecutarAsync(new Exception(detalleSensible), "trace-prueba-123");

        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.Equal("application/problem+json", context.Response.ContentType);
        Assert.DoesNotContain(detalleSensible, body, StringComparison.Ordinal);
        Assert.Contains("trace-prueba-123", body, StringComparison.Ordinal);
        Assert.Contains("Ocurrió un error interno en el servidor", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Invoke_ArgumentException_Retorna400SinExponerMensajeInterno()
    {
        const string mensajeOriginal = "El campo nombre es requerido";
        var (context, body) = await EjecutarAsync(new ArgumentException(mensajeOriginal), "trace-400-test");

        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        Assert.Contains("Solicitud incorrecta", body, StringComparison.Ordinal);
        Assert.Contains("La solicitud contiene parámetros no válidos o incompletos.", body, StringComparison.Ordinal);
        Assert.DoesNotContain(mensajeOriginal, body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Invoke_KeyNotFoundException_Retorna404SinExponerMensajeInterno()
    {
        const string mensajeOriginal = "No se encontró la matriz especificada";
        var (context, body) = await EjecutarAsync(new KeyNotFoundException(mensajeOriginal), "trace-404-test");

        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
        Assert.Contains("Recurso no encontrado", body, StringComparison.Ordinal);
        Assert.Contains("El recurso solicitado no existe o no se encuentra disponible.", body, StringComparison.Ordinal);
        Assert.DoesNotContain(mensajeOriginal, body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Invoke_PublicProblemException_ExponeSoloMensajeDeclaradoPublico()
    {
        const string mensajePublico = "El nombre de la matriz es requerido.";
        var (context, body) = await EjecutarAsync(PublicProblemException.BadRequest(mensajePublico), "trace-publico-test");

        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        Assert.Contains("Solicitud incorrecta", body, StringComparison.Ordinal);
        Assert.Contains(mensajePublico, body, StringComparison.Ordinal);
        Assert.Contains("trace-publico-test", body, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Error en consulta ORA-00942: SELECT * FROM RL_MR_MODELOS")]
    [InlineData("SELECT PASSWORD FROM USUARIOS")]
    [InlineData("select password from usuarios")]
    [InlineData("Connection timeout database server")]
    [InlineData("Tabla USUARIOS no existe")]
    [InlineData("Execute procedure usuarios")]
    public async Task Invoke_ArgumentExceptionConTextoTecnico_SiempreUsaFallback(string mensajeSensible)
    {
        var (context, body) = await EjecutarAsync(new ArgumentException(mensajeSensible), "trace-sanitizar-test");

        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        Assert.Contains("La solicitud contiene parámetros no válidos o incompletos.", body, StringComparison.Ordinal);
        Assert.DoesNotContain(mensajeSensible, body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Invoke_InvalidOperationException_Retorna500YNo400()
    {
        const string mensajeTecnico = "Connection pool unavailable";
        var (context, body) = await EjecutarAsync(new InvalidOperationException(mensajeTecnico), "trace-invalid-operation");

        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.DoesNotContain(mensajeTecnico, body, StringComparison.Ordinal);
        Assert.Contains("Ocurrió un error interno en el servidor", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Invoke_UnauthorizedAccessException_Retorna403ConMensajeFijo()
    {
        const string mensajeInterno = "Rol ADMINISTRADOR requerido para RL_MR_PLANTILLAS";
        var (context, body) = await EjecutarAsync(new UnauthorizedAccessException(mensajeInterno), "trace-403-test");

        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
        Assert.Contains("No tiene privilegios suficientes para realizar esta acción.", body, StringComparison.Ordinal);
        Assert.DoesNotContain(mensajeInterno, body, StringComparison.Ordinal);
    }

    private static async Task<(DefaultHttpContext Context, string Body)> EjecutarAsync(
        Exception exception,
        string traceId,
        IHostEnvironment? environment = null,
        ILogger<ErrorHandlingMiddleware>? logger = null)
    {
        var middleware = new ErrorHandlingMiddleware(
            _ => throw exception,
            logger ?? NullLogger<ErrorHandlingMiddleware>.Instance,
            environment);
        var context = new DefaultHttpContext
        {
            TraceIdentifier = traceId,
            Response = { Body = new MemoryStream() }
        };

        await middleware.Invoke(context);

        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        var body = await reader.ReadToEndAsync();
        return (context, body);
    }

    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = nameof(ErrorHandlingMiddlewareTests);
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public Exception? Exception { get; private set; }
        public string? Message { get; private set; }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => NullLogger<T>.Instance.BeginScope(state);

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Exception = exception;
            Message = formatter(state, exception);
        }
    }
}
