using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RL.API.Core.Security;
using RL.API.Features.MatricesRiesgos;
using Xunit;

namespace RL.API.Tests.Features.MatricesRiesgos;

public sealed class MatricesRiesgosFamiliasControllerSecurityTests
{
    [Fact]
    public void LifecycleController_UsaRutaCanonicaFamilias()
    {
        RouteAttribute route = typeof(FamiliasFormularioLifecycleController)
            .GetCustomAttribute<RouteAttribute>()
            ?? throw new InvalidOperationException("Falta RouteAttribute.");

        Assert.Equal("api/matrices-riesgos/familias", route.Template);
    }

    [Fact]
    public void LifecycleController_ExigeAutenticacion()
    {
        Assert.NotNull(typeof(FamiliasFormularioLifecycleController).GetCustomAttribute<AuthorizeAttribute>());
    }

    [Theory]
    [InlineData(nameof(FamiliasFormularioLifecycleController.ActivarFamiliaFormulario), "{id:long}/activar")]
    public void PutLifecycle_TieneRutaEsperada(string methodName, string template)
    {
        MethodInfo method = typeof(FamiliasFormularioLifecycleController).GetMethod(methodName)!;
        HttpPutAttribute attribute = Assert.Single(method.GetCustomAttributes<HttpPutAttribute>());
        Assert.Equal(template, attribute.Template);
    }

    [Theory]
    [InlineData(nameof(FamiliasFormularioLifecycleController.ActivarFamiliaFormulario))]
    [InlineData(nameof(FamiliasFormularioLifecycleController.EliminarFamiliaFormulario))]
    public void Lifecycle_NoUsaAliasesDeRolesHeredados(string methodName)
    {
        MethodInfo method = typeof(FamiliasFormularioLifecycleController).GetMethod(methodName)!;
        string roles = method.GetCustomAttribute<AuthorizeAttribute>()?.Roles ?? string.Empty;

        Assert.Equal(SystemRoles.Administrador, roles);
        Assert.DoesNotContain("DBA", roles, StringComparison.Ordinal);
        Assert.DoesNotContain("RIESGOS_ADMIN", roles, StringComparison.Ordinal);
    }
}
