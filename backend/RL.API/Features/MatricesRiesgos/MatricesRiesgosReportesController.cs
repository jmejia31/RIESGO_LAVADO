using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RL.API.Core.Security;
using RL.API.Features.MatricesRiesgos.Application;
using RL.API.Features.MatricesRiesgos.Contracts;

namespace RL.API.Features.MatricesRiesgos;

[ApiController]
[Authorize]
[ModuloAuthorize(10)]
[Route("api/matrices-riesgos/reportes")]
public sealed class MatricesRiesgosReportesController : ControllerBase
{
    private readonly IMatricesRiesgosAppService _matrices;
    private readonly IMatricesRiesgosReportExportService _exportador;

    public MatricesRiesgosReportesController(
        IMatricesRiesgosAppService matrices,
        IMatricesRiesgosReportExportService exportador)
    {
        _matrices = matrices;
        _exportador = exportador;
    }

    [HttpGet("consolidado.xlsx")]
    [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    public async Task<IActionResult> DescargarExcel()
    {
        var resultado = await _matrices.ObtenerConsolidadoTipadoAsync();
        if (!resultado.Success || resultado.Data is null)
            return StatusCode(resultado.StatusCode, new { success = false, mensaje = resultado.Message });

        ArchivoReporteDto archivo = _exportador.CrearExcelConsolidado(resultado.Data);
        return File(archivo.Contenido, archivo.ContentType, archivo.NombreArchivo);
    }

    [HttpGet("consolidado.pdf")]
    [Produces("application/pdf")]
    public async Task<IActionResult> DescargarPdf()
    {
        var resultado = await _matrices.ObtenerConsolidadoTipadoAsync();
        if (!resultado.Success || resultado.Data is null)
            return StatusCode(resultado.StatusCode, new { success = false, mensaje = resultado.Message });

        ArchivoReporteDto archivo = _exportador.CrearPdfConsolidado(resultado.Data);
        return File(archivo.Contenido, archivo.ContentType, archivo.NombreArchivo);
    }
}
