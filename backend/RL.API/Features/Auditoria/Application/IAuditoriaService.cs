using RL.API.Features.Auditoria.Contracts;

namespace RL.API.Features.Auditoria.Application;

public interface IAuditoriaService
{
    Task<(List<AuditoriaDto> Datos, int Total)> ObtenerBitacoraPaginadaAsync(int pagina, int limite, string? buscar, string? accion, string? modulo, string? tabla, DateTime? fechaInicio, DateTime? fechaFin);
    Task RegistrarExportacionAsync(RegistrarExportacionAuditoriaDto dto, long usuarioId, string? ip);
}
