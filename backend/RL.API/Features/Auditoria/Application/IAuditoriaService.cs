using RL.API.Features.Auditoria.Contracts;

namespace RL.API.Features.Auditoria.Application;

public interface IAuditoriaService
{
    Task<(List<AuditoriaDto> Datos, int Total)> ObtenerBitacoraPaginadaAsync(int pagina, int limite, string? buscar, string? accion, string? modulo, string? tabla, DateTime? fechaInicio, DateTime? fechaFin);
    Task<(List<AuditoriaDto> Datos, int Total)> ObtenerPorEntidadPaginadaAsync(string tabla, string registroId, string? registroAlternativo, int pagina, int limite);
    Task<(List<AuditoriaDto> Datos, int Total)> ObtenerPorFamiliaPaginadaAsync(long familiaId, int pagina, int limite);
    Task RegistrarExportacionAsync(RegistrarExportacionAuditoriaDto dto, long usuarioId, string? ip);
}
