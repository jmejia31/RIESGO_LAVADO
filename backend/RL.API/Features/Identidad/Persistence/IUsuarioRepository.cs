using RL.API.Features.Identidad.Contracts;
using RL.API.Features.Identidad.Domain;

namespace RL.API.Features.Identidad.Persistence;

public interface IUsuarioRepository
{
    Task<Usuario?> ObtenerPorEmailAsync(string email);
    Task<Usuario?> ObtenerPorLoginAsync(string identifier);
    Task<Usuario?> ObtenerPorIdAsync(long id);
    Task<long> CrearAsync(CrearUsuarioDto dto, string hash, string salt);
    Task<bool> ActualizarPasswordAsync(long usrId, string hash, string salt);
    Task<bool> ForzarCambioPasswordAsync(long usrId, string hash, string salt);
    Task<long?> BuscarUsuarioIdPorRefreshTokenAsync(string token);
    Task<string?> ObtenerRefreshTokenAsync(long usrId, string token);
    Task GuardarRefreshTokenAsync(long usrId, string token, DateTime expira, string? ip);
    Task RevocarRefreshTokenAsync(string token);
    Task RevocarTodosTokensAsync(long usrId);
    Task<List<UsuarioInfoDto>> ListarAsync();
    Task<bool> ActualizarAsync(long id, ActualizarUsuarioDto dto, string? hash, string? salt);
    Task<bool> ActualizarEstadoAsync(long id, bool activo);
    Task<List<int>> ObtenerModulosIdsPorUsuarioAsync(long usrId);
    Task RegistrarIntentoFallidoAsync(long usrId, int nuevosIntentos, DateTime? fechaBloqueo);
    Task RestablecerIntentosAsync(long usrId);
}
