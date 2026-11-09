using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace RL.API.Tests.Infrastructure;

public sealed class AntiforgeryRegistrationContractTests
{
    [Fact]
    public void MvcRegistration_ResolvesTheAntiforgeryAuthorizationFilter()
    {
        var services = new ServiceCollection();
        services.AddControllersWithViews();

        using var provider = services.BuildServiceProvider();

        var filterType = typeof(ValidateAntiForgeryTokenAttribute).Assembly.GetType(
            "Microsoft.AspNetCore.Mvc.ViewFeatures.Filters.ValidateAntiforgeryTokenAuthorizationFilter",
            throwOnError: true)!;

        Assert.NotNull(provider.GetService(filterType));
    }

    [Fact]
    public void Application_UsesMvcRegistrationRequiredByValidateAntiForgeryToken()
    {
        var source = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "backend", "RL.API", "Program.cs"));

        Assert.Contains("builder.Services.AddControllersWithViews()", source, StringComparison.Ordinal);
        Assert.DoesNotContain("builder.Services.AddControllers()", source, StringComparison.Ordinal);
        Assert.Contains("builder.Services.AddAntiforgery", source, StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new DirectoryNotFoundException("No se encontró la raíz del repositorio.");
    }
}
