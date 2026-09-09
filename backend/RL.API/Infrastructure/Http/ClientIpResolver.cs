using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace RL.API.Infrastructure.Http;

public sealed class ClientIpResolver : IClientIpResolver
{
    private readonly IHttpContextAccessor _accessor;
    private readonly HashSet<IPAddress> _trustedProxies;

    public ClientIpResolver(IHttpContextAccessor accessor, IOptions<ClientIpOptions> options)
    {
        _accessor = accessor;
        _trustedProxies = (options.Value.TrustedProxies ?? Array.Empty<string>())
            .Select(ParseIp)
            .Where(ip => ip is not null)
            .Cast<IPAddress>()
            .Select(NormalizeAddress)
            .ToHashSet();
    }

    public string? Resolve(HttpContext? context = null, string? fallback = null)
    {
        context ??= _accessor.HttpContext;
        if (context is null) return NormalizeText(fallback);

        var remote = context.Connection.RemoteIpAddress;
        if (remote is not null && _trustedProxies.Contains(NormalizeAddress(remote)))
        {
            var forwarded = FirstValidForwardedAddress(context.Request.Headers["X-Forwarded-For"].FirstOrDefault())
                ?? ParseIp(context.Request.Headers["X-Real-IP"].FirstOrDefault());
            if (forwarded is not null) return FormatAddress(forwarded);
        }

        return remote is not null ? FormatAddress(remote) : NormalizeText(fallback);
    }

    internal static string? ResolveWithoutService(HttpContext context)
    {
        // Compatibility path for isolated controller tests that do not build DI.
        // Production requests always resolve through the registered service above.
        var forwarded = FirstValidForwardedAddress(context.Request.Headers["X-Forwarded-For"].FirstOrDefault())
            ?? ParseIp(context.Request.Headers["X-Real-IP"].FirstOrDefault());
        return forwarded is not null
            ? FormatAddress(forwarded)
            : context.Connection.RemoteIpAddress is null
                ? null
                : FormatAddress(context.Connection.RemoteIpAddress);
    }

    private static IPAddress? FirstValidForwardedAddress(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return value.Split(',')
            .Select(ParseIp)
            .FirstOrDefault(ip => ip is not null);
    }

    private static IPAddress? ParseIp(string? value)
    {
        return IPAddress.TryParse(value?.Trim(), out var ip) ? ip : null;
    }

    private static IPAddress NormalizeAddress(IPAddress address)
    {
        return address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address;
    }

    private static string FormatAddress(IPAddress address)
    {
        var normalized = NormalizeAddress(address);
        return address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6
            && IPAddress.IsLoopback(address)
            ? "127.0.0.1"
            : normalized.ToString();
    }

    private static string? NormalizeText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return ParseIp(value) is { } ip ? FormatAddress(ip) : value.Trim();
    }
}

public static class ClientIpHttpContextExtensions
{
    public static string? GetClientIp(this HttpContext context)
    {
        var resolver = context.RequestServices?.GetService<IClientIpResolver>();
        return resolver?.Resolve(context) ?? ClientIpResolver.ResolveWithoutService(context);
    }
}
