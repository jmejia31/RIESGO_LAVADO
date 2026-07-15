namespace RL.API.Shared.Results;

public sealed record ServiceResult(bool Success, string? Message = null, int StatusCode = 200)
{
    public static ServiceResult Ok(string? message = null) => new(true, message, 200);
    public static ServiceResult BadRequest(string message) => new(false, message, 400);
    public static ServiceResult NotFound(string message) => new(false, message, 404);
}

public sealed record ServiceResult<T>(bool Success, T? Data = default, string? Message = null, int StatusCode = 200)
{
    public static ServiceResult<T> Ok(T data, string? message = null) => new(true, data, message, 200);
    public static ServiceResult<T> BadRequest(string message) => new(false, default, message, 400);
    public static ServiceResult<T> NotFound(string message) => new(false, default, message, 404);
}
