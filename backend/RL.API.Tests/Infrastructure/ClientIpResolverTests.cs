using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using RL.API.Infrastructure.Http;
using Xunit;

namespace RL.API.Tests.Infrastructure;

public sealed class ClientIpResolverTests
{
    [Fact]
    public void Resolve_UsesRemoteIpv4_WhenNoTrustedProxyIsConfigured()
    {
        var context = Context("172.19.0.214");

        Assert.Equal("172.19.0.214", Resolver(context).Resolve(context));
    }

    [Fact]
    public void Resolve_NormalizesMappedIpv6()
    {
        var context = Context("::ffff:172.19.0.214");

        Assert.Equal("172.19.0.214", Resolver(context).Resolve(context));
    }

    [Fact]
    public void Resolve_NormalizesLoopbackWithoutInventingExternalAddress()
    {
        var context = Context("::1");

        Assert.Equal("127.0.0.1", Resolver(context).Resolve(context));
    }

    [Fact]
    public void Resolve_UsesForwardedClientWhenRemoteAddressIsTrustedProxy()
    {
        var context = Context("10.0.0.10");
        context.Request.Headers["X-Forwarded-For"] = "203.0.113.77, 10.0.0.11";

        Assert.Equal("203.0.113.77", Resolver(context, "10.0.0.10").Resolve(context));
    }

    [Fact]
    public void Resolve_RejectsForwardedHeaderFromUntrustedRemote()
    {
        var context = Context("10.0.0.11");
        context.Request.Headers["X-Forwarded-For"] = "203.0.113.77";
        context.Request.Headers["X-Real-IP"] = "198.51.100.25";

        Assert.Equal("10.0.0.11", Resolver(context, "10.0.0.10").Resolve(context));
    }

    private static DefaultHttpContext Context(string remoteIp)
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse(remoteIp);
        return context;
    }

    private static ClientIpResolver Resolver(DefaultHttpContext context, params string[] trustedProxies)
    {
        var accessor = new HttpContextAccessor { HttpContext = context };
        return new ClientIpResolver(accessor, Options.Create(new ClientIpOptions
        {
            TrustedProxies = trustedProxies
        }));
    }
}
