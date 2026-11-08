using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RL.API.Infrastructure.Database;

namespace RL.API.Infrastructure.Health;

/// <summary>
/// Diagnóstico local, explícitamente deshabilitado por defecto, para demostrar la
/// identidad Oracle de la misma instancia API que atiende la solicitud.
/// </summary>
[ApiController]
[AllowAnonymous]
public sealed class P06OracleIdentityController : ControllerBase
{
    private readonly OracleDbContext _db;
    private readonly ResolvedDatabaseConnection _resolvedConnection;
    private readonly IConfiguration _configuration;
    private readonly ILogger<P06OracleIdentityController> _logger;

    public P06OracleIdentityController(
        OracleDbContext db,
        ResolvedDatabaseConnection resolvedConnection,
        IConfiguration configuration,
        ILogger<P06OracleIdentityController> logger)
    {
        _db = db;
        _resolvedConnection = resolvedConnection;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpGet("/__p06/oracle-identity")]
    [Produces("application/json")]
    public async Task<ActionResult<P06OracleIdentityResponse>> GetIdentity(CancellationToken cancellationToken)
    {
        if (!_configuration.GetValue<bool>("P06:LocalIdentityProbeEnabled") || !IsLoopbackRequest())
            return NotFound();

        try
        {
            await using var connection = _db.CreateConnection();
            await connection.OpenAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT
                    SYS_CONTEXT('USERENV', 'SESSION_USER'),
                    SYS_CONTEXT('USERENV', 'CURRENT_SCHEMA'),
                    SYS_CONTEXT('USERENV', 'SERVICE_NAME'),
                    SYS_CONTEXT('USERENV', 'DB_UNIQUE_NAME'),
                    SYS_CONTEXT('USERENV', 'DB_NAME'),
                    SYS_CONTEXT('USERENV', 'INSTANCE_NAME')
                FROM DUAL
                """;
            command.CommandTimeout = 5;

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
                return StatusCode(StatusCodes.Status503ServiceUnavailable);

            return Ok(new P06OracleIdentityResponse(
                _resolvedConnection.ProfileName,
                Environment.ProcessId,
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetString(4),
                reader.GetString(5)));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogWarning("P06 local Oracle identity probe failed. ExceptionType={ExceptionType}", exception.GetType().Name);
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
    }

    private bool IsLoopbackRequest() =>
        Request.HttpContext.Connection.RemoteIpAddress is { } address && IPAddress.IsLoopback(address);
}

public sealed record P06OracleIdentityResponse(
    [property: JsonProperty("profile")] string Profile,
    [property: JsonProperty("processId")] int ProcessId,
    [property: JsonProperty("sessionUser")] string SessionUser,
    [property: JsonProperty("currentSchema")] string CurrentSchema,
    [property: JsonProperty("serviceName")] string ServiceName,
    [property: JsonProperty("dbUniqueName")] string DbUniqueName,
    [property: JsonProperty("dbName")] string DbName,
    [property: JsonProperty("instanceName")] string InstanceName);
