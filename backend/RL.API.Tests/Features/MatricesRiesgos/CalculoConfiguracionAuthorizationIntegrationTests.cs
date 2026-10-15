using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.DependencyInjection;
using RL.API.Core.Security;
using RL.API.Features.MatricesRiesgos;
using RL.API.Features.MatricesRiesgos.Application;
using RL.API.Tests.Support;
using Xunit;

namespace RL.API.Tests.Features.MatricesRiesgos;

public sealed class CalculoConfiguracionAuthorizationIntegrationTests
{
    private const string Issuer = "calculo-auth-tests";
    private const string Audience = "calculo-auth-test-client";
    private const string SigningSecret = "local-test-signing-key-32-bytes-minimum";

    [Fact]
    public async Task EveryMutation_EnforcesAuthenticationAndModule10_WithoutRoleGate()
    {
        var controller = typeof(CalculoConfiguracionController);
        Assert.NotNull(controller.GetCustomAttribute<AuthorizeAttribute>());
        ModuloAuthorizeAttribute moduleGuard = Assert.IsType<ModuloAuthorizeAttribute>(
            controller.GetCustomAttribute<ModuloAuthorizeAttribute>());
        Assert.Equal(new[] { 10 }, ReadRequiredModules(moduleGuard));

        var endpoints = controller.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(method => (Method: method, Http: method.GetCustomAttribute<HttpMethodAttribute>()))
            .Where(item => item.Http is not null)
            .ToArray();
        Assert.Equal(25, endpoints.Length);
        var mutationEndpoints = endpoints.Where(item => item.Http!.HttpMethods.Any(method => method != "GET")).ToArray();
        Assert.Equal(14, mutationEndpoints.Length);
        Assert.Equal(11, endpoints.Count(item => item.Http!.HttpMethods.All(method => method == "GET")));
        Assert.Equal(6, mutationEndpoints.Count(item => item.Http!.Template!.StartsWith("formulas", StringComparison.Ordinal)
            || item.Http.Template.StartsWith("formula-", StringComparison.Ordinal)));
        Assert.Equal(4, mutationEndpoints.Count(item => item.Http!.Template!.StartsWith("funciones", StringComparison.Ordinal)
            || item.Http.Template.StartsWith("funcion-versiones", StringComparison.Ordinal)));
        Assert.Equal(4, mutationEndpoints.Count(item => item.Http!.Template!.StartsWith("parametros", StringComparison.Ordinal)
            || item.Http.Template.StartsWith("parametro-versiones", StringComparison.Ordinal)));
        Assert.All(endpoints, item =>
        {
            Assert.Null(item.Method.GetCustomAttribute<AllowAnonymousAttribute>());
            Assert.Null(item.Method.GetCustomAttribute<AuthorizeAttribute>()?.Roles);
        });

        var mutations = mutationEndpoints
            .SelectMany(item => item.Http!.HttpMethods.Select(httpMethod =>
                (item.Method, HttpMethod: httpMethod, Template: BuildRoute(item.Http.Template))))
            .ToArray();
        Assert.Equal(14, mutations.Length);

        await using WebApplication app = await StartTestApplicationAsync();
        using var client = new HttpClient { BaseAddress = GetAddress(app) };

        foreach (var mutation in mutations)
        {
            Assert.Equal(HttpStatusCode.Unauthorized,
                await SendAsync(client, mutation.HttpMethod, mutation.Template, token: null));

            Assert.Equal(HttpStatusCode.Forbidden,
                await SendAsync(client, mutation.HttpMethod, mutation.Template, CreateToken("ANALISTA", "9")));

            Assert.Equal(HttpStatusCode.Forbidden,
                await SendAsync(client, mutation.HttpMethod, mutation.Template, CreateToken("ANALISTA", modules: null)));

            foreach (string role in new[] { "ANALISTA", "SUPERVISOR", "ADMINISTRADOR" })
            {
                // Malformed JSON yields 400 only after both authentication and module authorization pass.
                Assert.Equal(HttpStatusCode.BadRequest,
                    await SendAsync(client, mutation.HttpMethod, mutation.Template, CreateToken(role, "10")));
            }

            Assert.Equal(HttpStatusCode.Forbidden,
                await SendAsync(client, mutation.HttpMethod, mutation.Template, CreateToken("ADMINISTRADOR", "inválido")));
        }
    }

    private static async Task<WebApplication> StartTestApplicationAsync()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Testing" });
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services
            .AddControllers()
            .AddApplicationPart(typeof(CalculoConfiguracionController).Assembly);
        builder.Services.AddSingleton<ICalculoConfiguracionService>(services =>
            InterfaceStub.Create<ICalculoConfiguracionService>(out InterfaceStub stub));
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options => options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = Issuer,
                ValidateAudience = true,
                ValidAudience = Audience,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningSecret)),
                ClockSkew = TimeSpan.Zero
            });
        builder.Services.AddAuthorization();

        WebApplication app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        await app.StartAsync();
        return app;
    }

    private static Uri GetAddress(WebApplication app)
    {
        var server = app.Services.GetRequiredService<IServer>();
        string address = Assert.Single(server.Features.Get<IServerAddressesFeature>()!.Addresses);
        return new Uri(address);
    }

    private static async Task<HttpStatusCode> SendAsync(HttpClient client, string method, string route, string? token)
    {
        using var request = new HttpRequestMessage(new HttpMethod(method), route);
        request.Content = new StringContent("{invalid-json", Encoding.UTF8, "application/json");
        if (token is not null)
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        using HttpResponseMessage response = await client.SendAsync(request);
        return response.StatusCode;
    }

    private static string CreateToken(string role, string? modules)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "27"),
            new(ClaimTypes.Role, role)
        };
        if (modules is not null)
            claims.Add(new Claim("modulos", modules));

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningSecret)),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(Issuer, Audience, claims, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddMinutes(5), credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static int[] ReadRequiredModules(ModuloAuthorizeAttribute attribute)
    {
        var field = typeof(ModuloAuthorizeAttribute).GetField("_moduloIds", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        return Assert.IsType<HashSet<int>>(field!.GetValue(attribute)).Order().ToArray();
    }

    private static string BuildRoute(string? actionTemplate)
    {
        string controllerTemplate = typeof(CalculoConfiguracionController).GetCustomAttribute<RouteAttribute>()!.Template!;
        string action = System.Text.RegularExpressions.Regex.Replace(actionTemplate ?? string.Empty, "\\{[^}]+\\}", "1");
        return $"/{controllerTemplate}/{action}";
    }
}
