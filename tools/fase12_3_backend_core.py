from pathlib import Path
import re

root=Path(__file__).resolve().parents[1]
B=root/'backend/RL.API/Features/MatricesRiesgos'
F=root/'frontend/rl-app/src/app/features/admin/matrices-riesgos'

def read(p): return p.read_text(encoding='utf-8-sig')
def write(p,s): p.write_text(s,encoding='utf-8')
def rep(s,old,new,label):
    if old not in s:
        if new in s: return s
        raise RuntimeError(label)
    return s.replace(old,new,1)
def reg(s,pat,new,label):
    out,n=re.subn(pat,new,s,count=1,flags=re.S)
    if n!=1: raise RuntimeError(label)
    return out

p=B/'Contracts/Reporteria/ReporteriaDtos.cs'; s=read(p)
s=rep(s,'    public string? NivelResidual { get; set; }','    public string? NivelInherente { get; set; }\n    public string? NivelResidual { get; set; }','filter inherent')
old='''public sealed class MatricesRiesgoDashboardDto
{
    public int TotalMatrices { get; set; }
    public int TotalCalculadas { get; set; }
    public int TotalCerradas { get; set; }
    public int TotalConPlanAccion { get; set; }
    public List<MatrizRiesgoConteoDto> PorEstado { get; set; } = new();
    public List<MatrizRiesgoConteoDto> PorNivelResidual { get; set; } = new();
}
'''
new='''public sealed class MatricesRiesgoDashboardDto
{
    public DateTime FechaGeneracion { get; set; } = DateTime.Now;
    public MatrizRiesgoReporteFiltroDto Filtro { get; set; } = new();
    public int TotalMatrices { get; set; }
    public int TotalCalculadas { get; set; }
    public int TotalCerradas { get; set; }
    public int TotalConPlanAccion { get; set; }
    public int TotalAltoCritico { get; set; }
    public int TotalPlanesVencidos { get; set; }
    public List<MatrizRiesgoConteoDto> PorEstado { get; set; } = new();
    public List<MatrizRiesgoConteoDto> PorNivelInherente { get; set; } = new();
    public List<MatrizRiesgoConteoDto> PorNivelResidual { get; set; } = new();
    public List<MatrizRiesgoMapaTransicionDto> MapaTransicion { get; set; } = new();
    public List<MatrizRiesgoResumenDto> MatricesCriticas { get; set; } = new();
    public List<MatrizRiesgoResumenDto> MatricesFiltradas { get; set; } = new();
    public List<MatrizRiesgoPlanAccionReporteDto> PlanesAccion { get; set; } = new();
}

public sealed class MatrizRiesgoMapaTransicionDto
{
    public string NivelInherente { get; set; } = string.Empty;
    public string NivelResidual { get; set; } = string.Empty;
    public int Total { get; set; }
    public decimal PromedioInherente { get; set; }
    public decimal PromedioResidual { get; set; }
}
'''
s=rep(s,old,new,'dashboard dto')
write(p,s)

p=B/'Persistence/IMatricesRiesgosRepository.cs'; s=read(p); s=rep(s,'Task<MatricesRiesgoDashboardDto> ObtenerDashboardAsync();','Task<MatricesRiesgoDashboardDto> ObtenerDashboardAsync(MatrizRiesgoReporteFiltroDto filtro);','repo signature'); write(p,s)
p=B/'Application/IMatricesRiesgosAppService.cs'; s=read(p); s=rep(s,'Task<ServiceResult<MatricesRiesgoDashboardDto>> ObtenerDashboardAsync();','Task<ServiceResult<MatricesRiesgoDashboardDto>> ObtenerDashboardAsync(MatrizRiesgoReporteFiltroDto filtro);','app signature'); write(p,s)

p=B/'Application/MatricesRiesgosAppService.cs'; s=read(p)
old='''    public async Task<ServiceResult<MatricesRiesgoDashboardDto>> ObtenerDashboardAsync()
    {
        var dashboard = await _repo.ObtenerDashboardAsync();
        return ServiceResult<MatricesRiesgoDashboardDto>.Ok(dashboard);
    }
'''
new='''    public async Task<ServiceResult<MatricesRiesgoDashboardDto>> ObtenerDashboardAsync(MatrizRiesgoReporteFiltroDto filtro)
    {
        filtro ??= new MatrizRiesgoReporteFiltroDto();
        var errorFiltro = NormalizarFiltroReporte(filtro);
        if (errorFiltro != null)
            return ServiceResult<MatricesRiesgoDashboardDto>.BadRequest(errorFiltro);

        var dashboard = await _repo.ObtenerDashboardAsync(filtro);
        return ServiceResult<MatricesRiesgoDashboardDto>.Ok(dashboard);
    }
'''
s=rep(s,old,new,'app dashboard method')
s=rep(s,'        filtro.NivelResidual = filtro.NivelResidual?.Trim();','        filtro.NivelInherente = filtro.NivelInherente?.Trim();\n        filtro.NivelResidual = filtro.NivelResidual?.Trim();','normalize inherent')
s=rep(s,'            new[] { "Nivel residual", ValorFiltro(filtro.NivelResidual) },','            new[] { "Nivel inherente", ValorFiltro(filtro.NivelInherente) },\n            new[] { "Nivel residual", ValorFiltro(filtro.NivelResidual) },','filter summary')
write(p,s)

p=B/'MatricesRiesgosController.cs'; s=read(p)
old='''    [HttpGet("dashboard")]
    public async Task<IActionResult> ObtenerDashboard()
    {
        try
        {
            var result = await _service.ObtenerDashboardAsync();
            return Responder(result);
        }
'''
new='''    [HttpGet("dashboard")]
    public async Task<IActionResult> ObtenerDashboard(
        [FromQuery] string? buscar = null,
        [FromQuery] string? estado = null,
        [FromQuery] string? sujetoTipo = null,
        [FromQuery] string? nivelInherente = null,
        [FromQuery] string? nivelResidual = null,
        [FromQuery] string? modeloVersion = null,
        [FromQuery] string? responsable = null,
        [FromQuery] DateTime? fechaInicio = null,
        [FromQuery] DateTime? fechaFin = null)
    {
        try
        {
            var result = await _service.ObtenerDashboardAsync(new MatrizRiesgoReporteFiltroDto
            {
                Buscar = buscar,
                Estado = estado,
                SujetoTipo = sujetoTipo,
                NivelInherente = nivelInherente,
                NivelResidual = nivelResidual,
                ModeloVersion = modeloVersion,
                Responsable = responsable,
                FechaInicio = fechaInicio,
                FechaFin = fechaFin
            });
            return Responder(result);
        }
'''
s=rep(s,old,new,'controller dashboard')
s=rep(s,'''    public async Task<IActionResult> ObtenerReporte(
        [FromQuery] string? buscar = null,
        [FromQuery] string? estado = null,
        [FromQuery] string? sujetoTipo = null,
        [FromQuery] string? nivelResidual = null,''','''    public async Task<IActionResult> ObtenerReporte(
        [FromQuery] string? buscar = null,
        [FromQuery] string? estado = null,
        [FromQuery] string? sujetoTipo = null,
        [FromQuery] string? nivelInherente = null,
        [FromQuery] string? nivelResidual = null,''','report inherent parameter')
s=rep(s,'''    public async Task<IActionResult> ExportarReporte(
        [FromQuery] string formato = "EXCEL",
        [FromQuery] string? buscar = null,
        [FromQuery] string? estado = null,
        [FromQuery] string? sujetoTipo = null,
        [FromQuery] string? nivelResidual = null,''','''    public async Task<IActionResult> ExportarReporte(
        [FromQuery] string formato = "EXCEL",
        [FromQuery] string? buscar = null,
        [FromQuery] string? estado = null,
        [FromQuery] string? sujetoTipo = null,
        [FromQuery] string? nivelInherente = null,
        [FromQuery] string? nivelResidual = null,''','export inherent parameter')
s=rep(s,'''            var result = await _service.ObtenerReporteAsync(new MatrizRiesgoReporteFiltroDto
            {
                Buscar = buscar,
                Estado = estado,
                SujetoTipo = sujetoTipo,
                NivelResidual = nivelResidual,''','''            var result = await _service.ObtenerReporteAsync(new MatrizRiesgoReporteFiltroDto
            {
                Buscar = buscar,
                Estado = estado,
                SujetoTipo = sujetoTipo,
                NivelInherente = nivelInherente,
                NivelResidual = nivelResidual,''','report inherent mapping')
s=rep(s,'''            var result = await _service.ExportarReporteAsync(new MatrizRiesgoReporteFiltroDto
            {
                Buscar = buscar,
                Estado = estado,
                SujetoTipo = sujetoTipo,
                NivelResidual = nivelResidual,''','''            var result = await _service.ExportarReporteAsync(new MatrizRiesgoReporteFiltroDto
            {
                Buscar = buscar,
                Estado = estado,
                SujetoTipo = sujetoTipo,
                NivelInherente = nivelInherente,
                NivelResidual = nivelResidual,''','export inherent mapping')
write(p,s)
