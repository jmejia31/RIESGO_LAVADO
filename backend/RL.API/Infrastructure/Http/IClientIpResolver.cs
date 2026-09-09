using Microsoft.AspNetCore.Http;

namespace RL.API.Infrastructure.Http;

public interface IClientIpResolver
{
    string? Resolve(HttpContext? context = null, string? fallback = null);
}
