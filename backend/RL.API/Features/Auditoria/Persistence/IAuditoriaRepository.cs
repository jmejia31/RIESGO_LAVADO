using Oracle.ManagedDataAccess.Client;
using RL.API.Features.Auditoria.Contracts;

namespace RL.API.Features.Auditoria.Persistence;

public interface IAuditoriaRepository
{
    Task RegistrarAsync(string tabla, string registroId, string accion, string? datosAnt, string? datosNvo, long? usrId, string? email, string? ip, string? modulo);
    Task RegistrarAsync(OracleConnection connection, OracleTransaction? transaction, string tabla, string registroId, string accion, string? datosAnt, string? datosNvo, long? usrId, string? email, string? ip, string? modulo)
        => throw new NotSupportedException("El repositorio de auditoría inyectado no admite una transacción compartida.");
    Task<(List<AuditoriaDto> Datos, int Total)> ObtenerBitacoraPaginadaAsync(int pagina, int limite, string? buscar, string? accion, string? modulo, string? tabla, DateTime? fechaInicio, DateTime? fechaFin);
}
