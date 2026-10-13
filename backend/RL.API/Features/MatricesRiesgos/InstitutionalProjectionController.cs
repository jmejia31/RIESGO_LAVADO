using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RL.API.Core.Security;
using RL.API.Features.MatricesRiesgos.Application;

namespace RL.API.Features.MatricesRiesgos;

[ApiController]
[Authorize]
[ModuloAuthorize(10)]
[Route("api/matrices-riesgos/evaluaciones")]
[Produces("application/json")]
public sealed class InstitutionalProjectionController(IInstitutionalProjectionService service) : ControllerBase
{
    [HttpGet("{evaluationId:long}/proyeccion-institucional")]
    public async Task<IActionResult> Get(long evaluationId, CancellationToken cancellationToken)
    {
        long userId = long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out long parsed) ? parsed : 0;
        var result = await service.GetAsync(evaluationId, userId, cancellationToken);
        return result.Success
            ? Ok(new { success = true, datos = result.Data, mensaje = result.Message })
            : StatusCode(result.StatusCode, new { success = false, mensaje = result.Message });
    }
}
