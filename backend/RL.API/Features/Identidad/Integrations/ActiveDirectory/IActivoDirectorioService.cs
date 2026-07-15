namespace RL.API.Features.Identidad.Integrations.ActiveDirectory;

public interface IActivoDirectorioService
{
    Task<ResultadoValidacionAdDto> ValidarUsuarioAsync(string usuario, string dominio);
    Task<bool> AutenticarAsync(string usuario, string dominio, string password);
}
