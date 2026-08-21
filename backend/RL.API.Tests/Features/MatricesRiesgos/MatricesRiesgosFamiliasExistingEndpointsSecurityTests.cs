using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using RL.API.Core.Security;
using RL.API.Features.MatricesRiesgos;
using Xunit;

namespace RL.API.Tests.Features.MatricesRiesgos;

public sealed class MatricesRiesgosFamiliasExistingEndpointsSecurityTests
{
    [Theory]
    [InlineData(nameof(MatricesRiesgosController.CrearFamiliaFormulario))]
    [InlineData(nameof(MatricesRiesgosController.ActualizarFamiliaFormulario))]
    [InlineData(nameof(MatricesRiesgosController.DesactivarFamiliaFormulario))]
    public void EndpointsFamiliaExistentes_ConservanAdminYAudit(string methodName)
    {
        MethodInfo method = typeof(MatricesRiesgosController).GetMethod(methodName)
            ?? throw new InvalidOperationException($"No existe {methodName}.");
        AuthorizeAttribute authorize = Assert.Single(method.GetCustomAttributes<AuthorizeAttribute>());

        Assert.Equal(SystemRoles.Administrador, authorize.Roles);
        Assert.NotNull(method.GetCustomAttribute<AuditRequiredAttribute>());
    }
}
