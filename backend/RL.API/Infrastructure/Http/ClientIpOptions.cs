namespace RL.API.Infrastructure.Http;

public sealed class ClientIpOptions
{
    public const string SectionName = "ClientIp";

    public string[] TrustedProxies { get; init; } = Array.Empty<string>();
}
