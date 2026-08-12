using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RL.API.Core.Security;
using RL.API.Features.MatricesRiesgos;
using Xunit;

namespace RL.API.Tests.Features.MatricesRiesgos;

public sealed class MatricesRiesgosPhase13UatContractTests
{
    private static readonly Type[] Controllers =
    {
        typeof(MatricesRiesgosController),
        typeof(MatricesRiesgosGestionController),
        typeof(MatricesRiesgosMitigacionController),
        typeof(MatricesRiesgosMonitoreoController),
        typeof(MatricesRiesgosReportesController)
    };

    [Fact]
    public void TodosLosControllers_ConservanAutenticacionYModulo10()
    {
        foreach (Type controller in Controllers)
        {
            Assert.NotNull(controller.GetCustomAttribute<AuthorizeAttribute>());
            Assert.NotNull(controller.GetCustomAttribute<ModuloAuthorizeAttribute>());
            Assert.Null(controller.GetCustomAttribute<AllowAnonymousAttribute>());
        }
    }

    [Fact]
    public void TodasLasMutaciones_ExigenAuditoria()
    {
        foreach (Type controller in Controllers)
        {
            MethodInfo[] actions = controller.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
            foreach (MethodInfo action in actions.Where(EsMutacionHttp))
            {
                Assert.Contains(action.GetCustomAttributes(), a => a.GetType().Name == "AuditRequiredAttribute");
            }
        }
    }

    [Fact]
    public void DescargasDeReportes_ExigenAuditoriaExplicita()
    {
        foreach (string methodName in new[] { nameof(MatricesRiesgosReportesController.DescargarExcel), nameof(MatricesRiesgosReportesController.DescargarPdf) })
        {
            MethodInfo method = typeof(MatricesRiesgosReportesController).GetMethod(methodName)
                ?? throw new InvalidOperationException($"No existe {methodName}.");
            Assert.Contains(method.GetCustomAttributes(), a => a.GetType().Name == "AuditRequiredAttribute");
        }
    }

    [Fact]
    public void SuperficieUat_ConservaOperacionesCriticas()
    {
        AssertMetodos(typeof(MatricesRiesgosController),
            nameof(MatricesRiesgosController.ObtenerVersionVigenteFormulario),
            nameof(MatricesRiesgosController.ListarHistorialVersionesFormulario),
            nameof(MatricesRiesgosController.CrearBorradorFormulario),
            nameof(MatricesRiesgosController.ClonarVersionFormulario),
            nameof(MatricesRiesgosController.ActualizarBorradorFormulario),
            nameof(MatricesRiesgosController.PublicarVersionFormulario),
            nameof(MatricesRiesgosController.CambiarEstadoVigenciaFormulario),
            nameof(MatricesRiesgosController.ListarEvaluacionesPaginadas),
            nameof(MatricesRiesgosController.ObtenerEvaluacion),
            nameof(MatricesRiesgosController.CrearEvaluacion),
            nameof(MatricesRiesgosController.ActualizarEvaluacion),
            nameof(MatricesRiesgosController.TransicionarEstadoEvaluacion),
            nameof(MatricesRiesgosController.ObtenerFlujosEvaluacion),
            nameof(MatricesRiesgosController.CargarEvidencia),
            nameof(MatricesRiesgosController.VincularEvidencia),
            nameof(MatricesRiesgosController.EliminarEvidencia),
            nameof(MatricesRiesgosController.ObtenerConsolidado),
            nameof(MatricesRiesgosController.ObtenerMetodologiaVigente));

        AssertMetodos(typeof(MatricesRiesgosGestionController), "Listar", "Obtener", "Crear", "Actualizar");
        AssertMetodos(typeof(MatricesRiesgosMitigacionController),
            "ListarControles", "CrearControl", "ActualizarControl", "ListarEvaluacionesControl", "EvaluarControl",
            "ListarPlanes", "CrearPlan", "ActualizarPlan", "ListarActividades", "CrearActividad", "ActualizarActividad");
        AssertMetodos(typeof(MatricesRiesgosMonitoreoController),
            "ListarAlertas", "CrearAlerta", "CambiarEstadoAlerta", "ListarAutomonitoreo", "RegistrarAutomonitoreo", "ObtenerResumen");
        AssertMetodos(typeof(MatricesRiesgosReportesController), "DescargarExcel", "DescargarPdf");
    }

    [Fact]
    public void Plantillas_SiguenRestringidasAlAdministradorInstitucional()
    {
        foreach (string methodName in new[]
        {
            nameof(MatricesRiesgosController.CrearBorradorFormulario),
            nameof(MatricesRiesgosController.ClonarVersionFormulario),
            nameof(MatricesRiesgosController.ActualizarBorradorFormulario),
            nameof(MatricesRiesgosController.PublicarVersionFormulario),
            nameof(MatricesRiesgosController.CambiarEstadoVigenciaFormulario)
        })
        {
            MethodInfo method = typeof(MatricesRiesgosController).GetMethod(methodName)!;
            AuthorizeAttribute authorize = Assert.Single(method.GetCustomAttributes<AuthorizeAttribute>());
            Assert.Equal(SystemRoles.Administrador, authorize.Roles);
        }
    }

    [Fact]
    public void Plantillas_AceptanDocumentoJsonEnLugarDeTextoPlano()
    {
        foreach (string methodName in new[]
        {
            nameof(MatricesRiesgosController.CrearBorradorFormulario),
            nameof(MatricesRiesgosController.ActualizarBorradorFormulario)
        })
        {
            MethodInfo method = typeof(MatricesRiesgosController).GetMethod(methodName)!;
            ParameterInfo body = Assert.Single(method.GetParameters().Where(parameter => parameter.Name == "jsonConfig"));

            Assert.Equal(typeof(JsonElement), body.ParameterType);
            Assert.NotNull(body.GetCustomAttribute<FromBodyAttribute>());
        }
    }

    private static bool EsMutacionHttp(MethodInfo method) =>
        method.GetCustomAttribute<HttpPostAttribute>() is not null ||
        method.GetCustomAttribute<HttpPutAttribute>() is not null ||
        method.GetCustomAttribute<HttpDeleteAttribute>() is not null ||
        method.GetCustomAttribute<HttpPatchAttribute>() is not null;

    private static void AssertMetodos(Type controller, params string[] methodNames)
    {
        foreach (string methodName in methodNames)
        {
            Assert.NotNull(controller.GetMethod(methodName));
        }
    }
}
