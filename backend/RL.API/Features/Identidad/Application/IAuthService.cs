using RL.API.Features.Identidad.Contracts;

namespace RL.API.Features.Identidad.Application;

public interface IAuthService
{
    Task<LoginResponseDto?> LoginAsync(LoginRequestDto dto, string ip);
    Task<LoginResponseDto?> RefreshTokenAsync(string refreshToken, string ip);
    Task LogoutAsync(long usrId, string refreshToken);
    Task<bool> CambiarPasswordAsync(long usrId, CambiarPasswordDto dto);
    Task<UsuarioInfoDto?> CrearUsuarioAsync(CrearUsuarioDto dto, long creadoPor);
    Task<bool> ActualizarUsuarioAsync(string uid, ActualizarUsuarioDto dto, long actualizadoPor);
    Task<List<UsuarioInfoDto>> ListarUsuariosAsync();
    Task<bool> ActualizarEstadoUsuarioAsync(string uid, bool activo, long actualizadoPor);
    Task<bool> RecuperarPasswordAsync(string email);
}
