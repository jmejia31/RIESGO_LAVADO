using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RL.API.Features.MatricesRiesgos;
using RL.API.Features.MatricesRiesgos.Application;
using RL.API.Features.MatricesRiesgos.Contracts;
using RL.API.Shared.Results;
using RL.API.Tests.Support;
using Xunit;

namespace RL.API.Tests.Features.MatricesRiesgos;

public sealed class InstitutionalProjectionControllerTests
{
    [Fact]
    public async Task Endpoint_ReturnsOrdered82FieldProjection()
    {
        IInstitutionalProjectionService service = InterfaceStub.Create<IInstitutionalProjectionService>(out InterfaceStub stub);
        var fields = Enumerable.Range(1, 82).Select(number => new InstitutionalProjectionFieldDto(number, $"field{number}", $"Field {number}", "TEXT", number, "VALUE", "TEST")).ToArray();
        stub.On(nameof(IInstitutionalProjectionService.GetAsync), _ => Task.FromResult(ServiceResult<InstitutionalEvaluationProjectionDto>.Ok(new(50, "ROP-CUMP-59", fields))));
        var controller = new InstitutionalProjectionController(service);
        ConfigureUser(controller);

        IActionResult result = await controller.Get(50, CancellationToken.None);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        object projection = ok.Value!.GetType().GetProperty("datos")!.GetValue(ok.Value)!;
        var projectedFields = Assert.IsAssignableFrom<IReadOnlyList<InstitutionalProjectionFieldDto>>(
            projection.GetType().GetProperty("Fields")!.GetValue(projection));
        Assert.Equal(82, projectedFields.Count);
        Assert.Equal(Enumerable.Range(1, 82), projectedFields.Select(field => field.FieldNumber));
        string route = typeof(InstitutionalProjectionController).GetCustomAttribute<RouteAttribute>()!.Template!;
        string template = typeof(InstitutionalProjectionController).GetMethod(nameof(InstitutionalProjectionController.Get))!
            .GetCustomAttribute<HttpGetAttribute>()!.Template!;
        Assert.Equal("api/matrices-riesgos/evaluaciones", route);
        Assert.Equal("{evaluationId:long}/proyeccion-institucional", template);
    }

    [Fact]
    public async Task Endpoint_PropagatesNotFoundStatus()
    {
        IInstitutionalProjectionService service = InterfaceStub.Create<IInstitutionalProjectionService>(out InterfaceStub stub);
        stub.On(nameof(IInstitutionalProjectionService.GetAsync), _ => Task.FromResult(ServiceResult<InstitutionalEvaluationProjectionDto>.NotFound("Not found")));
        var controller = new InstitutionalProjectionController(service);
        ConfigureUser(controller);

        ObjectResult result = Assert.IsType<ObjectResult>(await controller.Get(999, CancellationToken.None));

        Assert.Equal(StatusCodes.Status404NotFound, result.StatusCode);
    }

    private static void ConfigureUser(ControllerBase controller)
    {
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "99")], "test"))
        };
        controller.ControllerContext = new ControllerContext { HttpContext = context };
    }
}
