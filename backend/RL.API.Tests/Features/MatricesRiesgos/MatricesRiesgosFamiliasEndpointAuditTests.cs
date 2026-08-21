using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using RL.API.Core.Security;
using RL.API.Features.MatricesRiesgos;
using Xunit;

namespace RL.API.Tests.Features.MatricesRiesgos;

public sealed class MatricesRiesgosFamiliasEndpointAuditTests
{
    [Fact]
    public void Activar_TieneAuditoriaObligatoria()
    {
        MethodInfo method = typeof(FamiliasFormularioLifecycleController).GetMethod(nameof(FamiliasFormularioLifecycleController.ActivarFamiliaFormulario))!;
        Assert.NotNull(method.GetCustomAttribute<AuditRequiredAttribute>());
        Assert.Equal(SystemRoles.Administrador, method.GetCustomAttribute<AuthorizeAttribute>()?.Roles);
    }

    [Fact]
    public void Eliminar_TieneAuditoriaObligatoria()
    {
        MethodInfo method = typeof(FamiliasFormularioLifecycleController).GetMethod(nameof(FamiliasFormularioLifecycleController.EliminarFamiliaFormulario))!;
        Assert.NotNull(method.GetCustomAttribute<AuditRequiredAttribute>());
        Assert.Equal(SystemRoles.Administrador, method.GetCustomAttribute<AuthorizeAttribute>()?.Roles);
    }
}
