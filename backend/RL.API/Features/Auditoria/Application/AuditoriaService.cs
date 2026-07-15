using Newtonsoft.Json;
using RL.API.Features.Auditoria.Contracts;
using RL.API.Features.Auditoria.Persistence;

namespace RL.API.Features.Auditoria.Application;

public sealed class AuditoriaService : IAuditoriaService
{
    private readonly IAuditoriaRepository _repository;

    public AuditoriaService(IAuditoriaRepository repository)
    {
        _repository = repository;
    }

    public Task<(List<AuditoriaDto> Datos, int Total)> ObtenerBitacoraPaginadaAsync(
        int pagina,
        int limite,
        string? buscar,
        string? accion,
        string? modulo,
        string? tabla,
        DateTime? fechaInicio,
        DateTime? fechaFin) =>
        _repository.ObtenerBitacoraPaginadaAsync(pagina, limite, buscar, accion, modulo, tabla, fechaInicio, fechaFin);

    public Task RegistrarExportacionAsync(RegistrarExportacionAuditoriaDto dto, long usuarioId, string? ip)
    {
        var datos = JsonConvert.SerializeObject(new
        {
            Accion = ObtenerAccionDetalle(dto),
            dto.Detalle
        });

        return _repository.RegistrarAsync(dto.Tabla, dto.RegistroId, "VER", null, datos, usuarioId, null, ip, dto.Modulo);
    }

    private static string ObtenerAccionDetalle(RegistrarExportacionAuditoriaDto dto)
    {
        if (dto.Detalle.TryGetValue("accion", out var accion) && accion != null)
        {
            var accionTexto = accion.ToString();
            if (!string.IsNullOrWhiteSpace(accionTexto))
                return accionTexto;
        }

        if (dto.Detalle.TryGetValue("tipoReporte", out var tipoReporte) &&
            tipoReporte?.ToString()?.Contains("PDF", StringComparison.OrdinalIgnoreCase) == true)
            return "GENERACION_REPORTE_PDF";

        if (dto.Detalle.TryGetValue("archivo", out var archivo) &&
            archivo?.ToString()?.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) == true)
            return "EXPORTACION_PDF";

        return "EXPORTACION_EXCEL";
    }
}
