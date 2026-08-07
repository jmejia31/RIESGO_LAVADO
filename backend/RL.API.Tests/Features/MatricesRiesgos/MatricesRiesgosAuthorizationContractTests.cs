using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using RL.API.Core.Security;
using RL.API.Features.MatricesRiesgos;
using Xunit;

namespace RL.API.Tests.Features.MatricesRiesgos;

public sealed class MatricesRiesgosAuthorizationContractTests
{
    private static readonly string[] AccionesAdministrativasFormulario =
    {
        nameof(MatricesRiesgosController.CrearBorradorFormulario),
        nameof(MatricesRiesgosController.ClonarVersionFormulario),
        nameof(MatricesRiesgosController.ActualizarBorradorFormulario),
        nameof(MatricesRiesgosController.PublicarVersionFormulario),
        nameof(MatricesRiesgosController.CambiarEstadoVigenciaFormulario)
    };

    [Fact]
    public void RolesInstitucionales_UsanNombresCanonicosDeRlRoles()
    {
        Assert.Equal("ADMINISTRADOR", SystemRoles.Administrador);
        Assert.Equal("SUPERVISOR", SystemRoles.Supervisor);
        Assert.Equal("ANALISTA", SystemRoles.Analista);
    }

    [Fact]
    public void ModuloMatrices_NoAutenticado_Devuelve401()
    {
        ModuloAuthorizeAttribute attribute = ObtenerAtributoModuloDelController();
        AuthorizationFilterContext context = CrearContexto(new ClaimsPrincipal(new ClaimsIdentity()));

        attribute.OnAuthorization(context);

        UnauthorizedObjectResult result = Assert.IsType<UnauthorizedObjectResult>(context.Result);
        Assert.Equal(StatusCodes.Status401Unauthorized, result.StatusCode);
    }

    [Fact]
    public void ModuloMatrices_AutenticadoSinClaimModulos_Devuelve403()
    {
        ModuloAuthorizeAttribute attribute = ObtenerAtributoModuloDelController();
        AuthorizationFilterContext context = CrearContexto(CrearUsuarioAutenticado(SystemRoles.Administrador, null));

        attribute.OnAuthorization(context);

        ObjectResult result = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, result.StatusCode);
    }

    [Fact]
    public void ModuloMatrices_AutenticadoSinModulo10_Devuelve403()
    {
        ModuloAuthorizeAttribute attribute = ObtenerAtributoModuloDelController();
        AuthorizationFilterContext context = CrearContexto(CrearUsuarioAutenticado(SystemRoles.Administrador, "1,2,9"));

        attribute.OnAuthorization(context);

        ObjectResult result = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, result.StatusCode);
    }

    [Fact]
    public void ModuloMatrices_AdministradorConModulo10_PasaFiltroDeModulo()
    {
        ModuloAuthorizeAttribute attribute = ObtenerAtributoModuloDelController();
        AuthorizationFilterContext context = CrearContexto(CrearUsuarioAutenticado(SystemRoles.Administrador, "1,10,12"));

        attribute.OnAuthorization(context);

        Assert.Null(context.Result);
        Assert.True(context.HttpContext.User.IsInRole(SystemRoles.Administrador));
    }

    [Fact]
    public void OperacionesAdministrativasFormulario_ExigenAdministradorCanonico()
    {
        foreach (string methodName in AccionesAdministrativasFormulario)
        {
            MethodInfo method = typeof(MatricesRiesgosController).GetMethod(methodName)
                ?? throw new InvalidOperationException($"No existe el método {methodName}.");
            AuthorizeAttribute authorize = Assert.Single(method.GetCustomAttributes<AuthorizeAttribute>());
            Assert.Equal(SystemRoles.Administrador, authorize.Roles);
        }
    }

    [Fact]
    public void ControllerMatrices_MantieneAuthorizeYModuloAuthorize()
    {
        Assert.NotNull(typeof(MatricesRiesgosController).GetCustomAttribute<AuthorizeAttribute>());
        Assert.NotNull(typeof(MatricesRiesgosController).GetCustomAttribute<ModuloAuthorizeAttribute>());
    }

    [Fact]
    public void RolesAliasesHeredados_NoFormanParteDelContratoDePlantillas()
    {
        foreach (string methodName in AccionesAdministrativasFormulario)
        {
            MethodInfo method = typeof(MatricesRiesgosController).GetMethod(methodName)!;
            string roles = method.GetCustomAttribute<AuthorizeAttribute>()?.Roles ?? string.Empty;
            Assert.DoesNotContain("ADMIN, DBA, RIESGOS_ADMIN", roles, StringComparison.Ordinal);
            Assert.DoesNotContain("DBA", roles, StringComparison.Ordinal);
            Assert.DoesNotContain("RIESGOS_ADMIN", roles, StringComparison.Ordinal);
        }
    }

    private static ModuloAuthorizeAttribute ObtenerAtributoModuloDelController() =>
        typeof(MatricesRiesgosController).GetCustomAttribute<ModuloAuthorizeAttribute>()
        ?? throw new InvalidOperationException("MatricesRiesgosController debe conservar ModuloAuthorize.");

    private static ClaimsPrincipal CrearUsuarioAutenticado(string role, string? modulos)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "27"),
            new(ClaimTypes.Role, role)
        };
        if (modulos is not null)
        {
            claims.Add(new Claim("modulos", modulos));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "Test"));
    }

    private static AuthorizationFilterContext CrearContexto(ClaimsPrincipal user)
    {
        var httpContext = new DefaultHttpContext { User = user };
        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor(),
            new ModelStateDictionary());
        return new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());
    }
}
