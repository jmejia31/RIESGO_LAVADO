using Microsoft.AspNetCore.Http;

namespace RL.API.Exceptions;

/// <summary>
/// Excepción de dominio cuyo mensaje ha sido clasificado explícitamente como seguro para exposición pública.
/// Las excepciones técnicas o genéricas nunca deben transformarse en esta excepción reutilizando su Message.
/// </summary>
public sealed class PublicProblemException : Exception
{
    private PublicProblemException(int statusCode, string title, string type, string publicMessage)
        : base(ValidatePublicMessage(publicMessage))
    {
        StatusCode = statusCode;
        Title = title;
        Type = type;
    }

    public int StatusCode { get; }
    public string Title { get; }
    public string Type { get; }

    public static PublicProblemException BadRequest(string publicMessage) =>
        new(
            StatusCodes.Status400BadRequest,
            "Solicitud incorrecta",
            "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            publicMessage);

    public static PublicProblemException NotFound(string publicMessage) =>
        new(
            StatusCodes.Status404NotFound,
            "Recurso no encontrado",
            "https://tools.ietf.org/html/rfc7231#section-6.5.4",
            publicMessage);

    public static PublicProblemException Conflict(string publicMessage) =>
        new(
            StatusCodes.Status409Conflict,
            "Conflicto de estado",
            "https://tools.ietf.org/html/rfc7231#section-6.5.8",
            publicMessage);

    private static string ValidatePublicMessage(string publicMessage)
    {
        if (string.IsNullOrWhiteSpace(publicMessage))
            throw new ArgumentException("El mensaje público no puede estar vacío.", nameof(publicMessage));

        var normalized = publicMessage.Trim();
        if (normalized.Length > 300)
            throw new ArgumentException("El mensaje público excede la longitud máxima permitida.", nameof(publicMessage));

        if (normalized.Contains('\r') || normalized.Contains('\n'))
            throw new ArgumentException("El mensaje público debe ocupar una sola línea.", nameof(publicMessage));

        return normalized;
    }
}
