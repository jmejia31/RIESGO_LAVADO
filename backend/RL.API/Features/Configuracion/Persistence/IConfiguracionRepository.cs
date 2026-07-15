using RL.API.Features.Configuracion.Contracts;

namespace RL.API.Features.Configuracion.Persistence;

public interface IConfiguracionRepository
{
    Task<ConfigSistema?> ObtenerConfigSistemaAsync();
    Task<List<LoginSlide>> ObtenerSlidesAsync();
    Task<List<LoginSlide>> ObtenerTodosSlidesAsync();
    Task<bool> GuardarConfigSistemaAsync(ConfigSistema config);
    Task<bool> CrearSlideAsync(LoginSlide slide);
    Task<bool> ActualizarSlideAsync(LoginSlide slide);
    Task<bool> EliminarSlideAsync(int id);
}
