using RL.API.Features.Configuracion.Contracts;

namespace RL.API.Features.Configuracion.Application;

public interface IConfiguracionService
{
    Task<ConfigSistema?> ObtenerConfigSistemaAsync();
    Task<List<LoginSlide>> ObtenerSlidesAsync();
    Task<List<LoginSlide>> ObtenerTodosSlidesAsync();
    Task<bool> GuardarConfigSistemaAsync(ConfigSistema config, long usuarioId, string? ip);
    Task<bool> CrearSlideAsync(LoginSlide slide, long usuarioId, string? ip);
    Task<bool> ActualizarSlideAsync(int id, LoginSlide slide, long usuarioId, string? ip);
    Task<bool> EliminarSlideAsync(int id, long usuarioId, string? ip);
    Task RegistrarCargaImagenAsync(string nombreOriginal, string nombreGuardado, string url, string tipoMime, long tamanioBytes, long usuarioId, string? ip);
}
