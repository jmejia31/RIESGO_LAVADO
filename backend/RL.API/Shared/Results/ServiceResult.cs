namespace RL.API.Shared.Results;

public sealed record ServiceResult(bool Success, string? Message = null, int StatusCode = 200)
{
    public static ServiceResult Ok(string? message = null) => new(true, message, 200);
    public static ServiceResult BadRequest(string message) => new(false, message, 400);
    public static ServiceResult NotFound(string message) => new(false, message, 404);
    public static ServiceResult Conflict(string message) => new(false, message, 409);
}

public sealed record ServiceResult<T>(bool Success, T? Data = default, string? Message = null, int StatusCode = 200)
{
    /// <summary>
    /// Construye un resultado fallido sin datos cuando T es un tipo valor.
    /// El parámetro sinDatos solo representa la ausencia deliberada de contenido.
    /// </summary>
    public ServiceResult(bool success, object? sinDatos, string? message, int statusCode)
        : this(success, default, message, statusCode)
    {
        if (success || sinDatos is not null)
        {
            throw new ArgumentException(
                "Este constructor se reserva para resultados fallidos sin datos.",
                nameof(sinDatos));
        }
    }

    public static ServiceResult<T> Ok(T data, string? message = null) => new(true, data, message, 200);
    public static ServiceResult<T> BadRequest(string message) => new(false, default, message, 400);
    public static ServiceResult<T> NotFound(string message) => new(false, default, message, 404);
    public static ServiceResult<T> Conflict(string message) => new(false, default, message, 409);
}
