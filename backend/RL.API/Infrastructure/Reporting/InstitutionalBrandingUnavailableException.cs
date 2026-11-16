namespace RL.API.Infrastructure.Reporting;

public sealed class InstitutionalBrandingUnavailableException : InvalidOperationException
{
    public InstitutionalBrandingUnavailableException(string code, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        Code = code;
    }

    public string Code { get; }
}
