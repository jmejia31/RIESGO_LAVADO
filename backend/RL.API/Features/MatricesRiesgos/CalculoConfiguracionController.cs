using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RL.API.Core.Security;
using RL.API.Features.MatricesRiesgos.Application;
using RL.API.Features.MatricesRiesgos.Contracts;
using RL.API.Shared.Results;

namespace RL.API.Features.MatricesRiesgos;

[ApiController]
[Authorize]
[ModuloAuthorize(10)]
[Route("api/matrices-riesgos/configuracion-calculo")]
[Produces("application/json")]
public sealed class CalculoConfiguracionController : ControllerBase
{
    private readonly ICalculoConfiguracionService _service;

    public CalculoConfiguracionController(ICalculoConfiguracionService service) => _service = service;

    [HttpGet("formulas")]
    public async Task<IActionResult> ListarFormulas([FromQuery] bool incluirInactivas = false) => Responder(await _service.ListarFormulasAsync(incluirInactivas));

    [HttpGet("formulas/{id:long}")]
    public async Task<IActionResult> ObtenerFormula(long id) => Responder(await _service.ObtenerFormulaAsync(id));

    [HttpPost("formulas")]
    [AuditRequired("Creación de fórmula administrativa")]
    public async Task<IActionResult> CrearFormula([FromBody] CrearFormulaDto dto) => Responder(await _service.CrearFormulaAsync(dto, UsuarioId(), Ip()));

    [HttpPost("formulas/{id:long}/versiones")]
    [AuditRequired("Creación de versión de fórmula")]
    public async Task<IActionResult> CrearFormulaVersion(long id, [FromBody] CrearFormulaVersionDto dto) => Responder(await _service.CrearFormulaVersionAsync(id, dto, UsuarioId(), Ip()));

    [HttpPut("formula-versiones/{id:long}")]
    [AuditRequired("Actualización de borrador de fórmula")]
    public async Task<IActionResult> ActualizarFormulaBorrador(long id, [FromBody] ActualizarFormulaBorradorDto dto) => Responder(await _service.ActualizarFormulaBorradorAsync(id, dto, UsuarioId(), Ip()));

    [HttpGet("formulas/{id:long}/versiones")]
    public async Task<IActionResult> ListarFormulaVersiones(long id) => Responder(await _service.ListarFormulaVersionesAsync(id));

    [HttpGet("formulas/{id:long}/usos")]
    public async Task<IActionResult> ListarFormulaUsages(long id) => Responder(await _service.ListarFormulaUsagesAsync(id));

    [HttpPost("formula-usos")]
    [AuditRequired("Vinculación de uso de fórmula")]
    public async Task<IActionResult> CrearFormulaUso([FromBody] CrearFormulaUsoDto dto) => Responder(await _service.CrearFormulaUsoAsync(dto, UsuarioId(), Ip()));

    [HttpPatch("formulas/{id:long}/estado")]
    [AuditRequired("Cambio de estado de fórmula")]
    public async Task<IActionResult> CambiarEstadoFormula(long id, [FromBody] CambiarEstadoConfiguracionDto dto) => Responder(await _service.CambiarEstadoFormulaAsync(id, dto, UsuarioId(), Ip()));

    [HttpGet("funciones")]
    public async Task<IActionResult> ListarFunciones([FromQuery] bool incluirInactivas = false) => Responder(await _service.ListarFuncionesAsync(incluirInactivas));

    [HttpGet("funciones/{id:long}")]
    public async Task<IActionResult> ObtenerFuncion(long id) => Responder(await _service.ObtenerFuncionAsync(id));

    [HttpPost("funciones")]
    [AuditRequired("Creación de función administrativa")]
    public async Task<IActionResult> CrearFuncion([FromBody] CrearFuncionDto dto) => Responder(await _service.CrearFuncionAsync(dto, UsuarioId(), Ip()));

    [HttpPost("funciones/{id:long}/versiones")]
    [AuditRequired("Creación de versión de función")]
    public async Task<IActionResult> CrearFuncionVersion(long id, [FromBody] CrearFuncionVersionDto dto) => Responder(await _service.CrearFuncionVersionAsync(id, dto, UsuarioId(), Ip()));

    [HttpPut("funcion-versiones/{id:long}")]
    [AuditRequired("Actualización de borrador de función")]
    public async Task<IActionResult> ActualizarFuncionBorrador(long id, [FromBody] ActualizarFuncionBorradorDto dto) => Responder(await _service.ActualizarFuncionBorradorAsync(id, dto, UsuarioId(), Ip()));

    [HttpGet("funciones/{id:long}/versiones")]
    public async Task<IActionResult> ListarFuncionVersiones(long id) => Responder(await _service.ListarFuncionVersionesAsync(id));

    [HttpGet("funcion-versiones/{id:long}/argumentos")]
    public async Task<IActionResult> ListarFuncionArgumentos(long id) => Responder(await _service.ListarFuncionArgumentosAsync(id));

    [HttpPatch("funcion-versiones/{id:long}/estado")]
    [AuditRequired("Cambio de estado de versión de función")]
    public async Task<IActionResult> CambiarEstadoFuncionVersion(long id, [FromBody] CambiarEstadoConfiguracionDto dto) => Responder(await _service.CambiarEstadoFuncionVersionAsync(id, dto, UsuarioId(), Ip()));

    [HttpGet("parametros")]
    public async Task<IActionResult> ListarParametros([FromQuery] bool incluirInactivos = false) => Responder(await _service.ListarParametrosAsync(incluirInactivos));

    [HttpGet("parametros/{id:long}")]
    public async Task<IActionResult> ObtenerParametro(long id) => Responder(await _service.ObtenerParametroAsync(id));

    [HttpPost("parametros")]
    [AuditRequired("Creación de parámetro de cálculo")]
    public async Task<IActionResult> CrearParametro([FromBody] CrearParametroDto dto) => Responder(await _service.CrearParametroAsync(dto, UsuarioId(), Ip()));

    [HttpPost("parametros/{id:long}/versiones")]
    [AuditRequired("Creación de versión de parámetro")]
    public async Task<IActionResult> CrearParametroVersion(long id, [FromBody] CrearParametroVersionDto dto) => Responder(await _service.CrearParametroVersionAsync(id, dto, UsuarioId(), Ip()));

    [HttpPut("parametro-versiones/{id:long}")]
    [AuditRequired("Actualización de borrador de parámetro")]
    public async Task<IActionResult> ActualizarParametroBorrador(long id, [FromBody] ActualizarParametroBorradorDto dto) => Responder(await _service.ActualizarParametroBorradorAsync(id, dto, UsuarioId(), Ip()));

    [HttpGet("parametros/{id:long}/versiones")]
    public async Task<IActionResult> ListarParametroVersiones(long id) => Responder(await _service.ListarParametroVersionesAsync(id));

    [HttpPatch("parametro-versiones/{id:long}/estado")]
    [AuditRequired("Cambio de estado de versión de parámetro")]
    public async Task<IActionResult> CambiarEstadoParametroVersion(long id, [FromBody] CambiarEstadoConfiguracionDto dto) => Responder(await _service.CambiarEstadoParametroVersionAsync(id, dto, UsuarioId(), Ip()));

    private long UsuarioId() => Convert.ToInt64(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
    private string? Ip()
    {
        string? forwarded = Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwarded)) return forwarded.Split(',')[0].Trim();
        string? real = Request.Headers["X-Real-IP"].FirstOrDefault();
        return !string.IsNullOrWhiteSpace(real) ? real.Trim() : HttpContext.Connection.RemoteIpAddress?.ToString();
    }

    private IActionResult Responder(ServiceResult result) => result.Success
        ? Ok(new { success = true, mensaje = result.Message })
        : StatusCode(result.StatusCode, new { success = false, mensaje = result.Message });

    private IActionResult Responder<T>(ServiceResult<T> result) => result.Success
        ? Ok(new { success = true, datos = result.Data, mensaje = result.Message })
        : StatusCode(result.StatusCode, new { success = false, mensaje = result.Message });
}
