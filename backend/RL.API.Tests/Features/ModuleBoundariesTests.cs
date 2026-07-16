using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RL.API.Features.Listas;
using RL.API.Features.MatricesRiesgos;
using RL.API.Core.Security;
using Xunit;

namespace RL.API.Tests.Features;

public sealed class ModuleBoundariesTests
{
    [Fact]
    public void ListasController_ConservaRutaYAutorizacion()
    {
        var controllerType = typeof(ListasController);
        var route = Assert.Single(controllerType.GetCustomAttributes(typeof(RouteAttribute), inherit: true).Cast<RouteAttribute>());

        Assert.Equal("api/[controller]", route.Template);
        Assert.NotNull(controllerType.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true).SingleOrDefault());
        Assert.Equal("RL.API.Features.Listas", controllerType.Namespace);
    }

    [Fact]
    public void MatricesRiesgosController_ConservaRutaYModuloAutorizado()
    {
        var controllerType = typeof(MatricesRiesgosController);
        var route = Assert.Single(controllerType.GetCustomAttributes(typeof(RouteAttribute), inherit: true).Cast<RouteAttribute>());

        Assert.Equal("api/matrices-riesgos", route.Template);
        Assert.NotNull(controllerType.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true).SingleOrDefault());
        Assert.NotNull(controllerType.GetCustomAttributes(typeof(ModuloAuthorizeAttribute), inherit: true).SingleOrDefault());
        Assert.Equal("RL.API.Features.MatricesRiesgos", controllerType.Namespace);
    }
}
