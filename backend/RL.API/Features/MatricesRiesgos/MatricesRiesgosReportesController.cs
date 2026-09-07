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
    [AuditRequired("Descarga de reporte consolidado Excel")]
    public async Task<IActionResult> DescargarExcel([FromQuery] FiltroReporteMatricesDto? filtro = null)
    {
        filtro ??= new FiltroReporteMatricesDto();
        var resultado = Request.Query.Count == 0
            ? await _matrices.ObtenerConsolidadoTipadoAsync()
            : await _matrices.ObtenerConsolidadoParaExportacionAsync(filtro);
        if (!resultado.Success || resultado.Data is null)
            return StatusCode(resultado.StatusCode, new { success = false, mensaje = resultado.Message });

        ArchivoReporteDto archivo = Request.Query.Count == 0
            ? _exportador.CrearExcelConsolidado(resultado.Data)
            : _exportador.CrearExcelConsolidado(resultado.Data, DescribirFiltros(filtro));
        return File(archivo.Contenido, archivo.ContentType, archivo.NombreArchivo);
    }

    [HttpGet("consolidado.pdf")]
    [Produces("application/pdf")]
    [AuditRequired("Descarga de reporte consolidado PDF")]
    public async Task<IActionResult> DescargarPdf([FromQuery] FiltroReporteMatricesDto? filtro = null)
    {
        filtro ??= new FiltroReporteMatricesDto();
        var resultado = Request.Query.Count == 0
            ? await _matrices.ObtenerConsolidadoTipadoAsync()
            : await _matrices.ObtenerConsolidadoParaExportacionAsync(filtro);
        if (!resultado.Success || resultado.Data is null)
            return StatusCode(resultado.StatusCode, new { success = false, mensaje = resultado.Message });

        ArchivoReporteDto archivo = Request.Query.Count == 0
            ? _exportador.CrearPdfConsolidado(resultado.Data)
            : _exportador.CrearPdfConsolidado(resultado.Data, DescribirFiltros(filtro));
        return File(archivo.Contenido, archivo.ContentType, archivo.NombreArchivo);
    }

    private static string DescribirFiltros(FiltroReporteMatricesDto filtro)
    {
        var valores = new List<string>();
        if (!string.IsNullOrWhiteSpace(filtro.Buscar)) valores.Add($"Buscar={filtro.Buscar.Trim()}");
        if (!string.IsNullOrWhiteSpace(filtro.Area)) valores.Add($"Área={filtro.Area.Trim()}");
        if (!string.IsNullOrWhiteSpace(filtro.DuenoRiesgo)) valores.Add($"Dueño={filtro.DuenoRiesgo.Trim()}");
        if (!string.IsNullOrWhiteSpace(filtro.EstadoEvaluacion)) valores.Add($"Estado={filtro.EstadoEvaluacion.Trim()}");
        if (!string.IsNullOrWhiteSpace(filtro.NivelInherente)) valores.Add($"Nivel inherente={filtro.NivelInherente.Trim()}");
        if (!string.IsNullOrWhiteSpace(filtro.NivelResidual)) valores.Add($"Nivel residual={filtro.NivelResidual.Trim()}");
        if (!string.IsNullOrWhiteSpace(filtro.RespuestaRiesgo)) valores.Add($"Respuesta={filtro.RespuestaRiesgo.Trim()}");
        if (filtro.FechaInicio.HasValue) valores.Add($"Desde={filtro.FechaInicio:yyyy-MM-dd}");
        if (filtro.FechaFin.HasValue) valores.Add($"Hasta={filtro.FechaFin:yyyy-MM-dd}");
        return valores.Count == 0 ? "Sin filtros" : string.Join(" | ", valores);
    }
}
