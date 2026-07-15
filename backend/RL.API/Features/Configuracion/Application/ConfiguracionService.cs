using Newtonsoft.Json;
using RL.API.Features.Auditoria.Persistence;
using RL.API.Features.Configuracion.Contracts;
using RL.API.Features.Configuracion.Persistence;

namespace RL.API.Features.Configuracion.Application;

public class ConfiguracionService : IConfiguracionService
{
    private readonly IConfiguracionRepository _repository;
    private readonly IAuditoriaRepository _auditoriaRepository;

    public ConfiguracionService(IConfiguracionRepository repository, IAuditoriaRepository auditoriaRepository)
    {
        _repository = repository;
        _auditoriaRepository = auditoriaRepository;
    }

    public Task<ConfigSistema?> ObtenerConfigSistemaAsync() =>
        _repository.ObtenerConfigSistemaAsync();

    public Task<List<LoginSlide>> ObtenerSlidesAsync() =>
        _repository.ObtenerSlidesAsync();

    public Task<List<LoginSlide>> ObtenerTodosSlidesAsync() =>
        _repository.ObtenerTodosSlidesAsync();

    public async Task<bool> GuardarConfigSistemaAsync(ConfigSistema config, long usuarioId, string? ip)
    {
        var anterior = await _repository.ObtenerConfigSistemaAsync();
        var guardado = await _repository.GuardarConfigSistemaAsync(config);
        if (!guardado)
        {
            return false;
        }

        await _auditoriaRepository.RegistrarAsync(
            "RL_CONFIG_SISTEMA",
            "1",
            "UPDATE",
            JsonConvert.SerializeObject(anterior),
            JsonConvert.SerializeObject(config),
            usuarioId,
            null,
            ip,
            "Configuracion");

        return true;
    }

    public async Task<bool> CrearSlideAsync(LoginSlide slide, long usuarioId, string? ip)
    {
        var creado = await _repository.CrearSlideAsync(slide);
        if (!creado)
        {
            return false;
        }

        await RegistrarAuditoriaSlideAsync("INSERT", slide.Id.ToString(), null, slide, usuarioId, ip);
        return true;
    }

    public async Task<bool> ActualizarSlideAsync(int id, LoginSlide slide, long usuarioId, string? ip)
    {
        var anterior = (await _repository.ObtenerTodosSlidesAsync()).FirstOrDefault(item => item.Id == id);
        slide.Id = id;

        var actualizado = await _repository.ActualizarSlideAsync(slide);
        if (!actualizado)
        {
            return false;
        }

        await RegistrarAuditoriaSlideAsync("UPDATE", id.ToString(), anterior, slide, usuarioId, ip);
        return true;
    }

    public async Task<bool> EliminarSlideAsync(int id, long usuarioId, string? ip)
    {
        var anterior = (await _repository.ObtenerTodosSlidesAsync()).FirstOrDefault(item => item.Id == id);
        var eliminado = await _repository.EliminarSlideAsync(id);
        if (!eliminado)
        {
            return false;
        }

        await RegistrarAuditoriaSlideAsync("DELETE", id.ToString(), anterior, null, usuarioId, ip);
        return true;
    }

    public Task RegistrarCargaImagenAsync(string nombreOriginal, string nombreGuardado, string url, string tipoMime, long tamanioBytes, long usuarioId, string? ip) =>
        _auditoriaRepository.RegistrarAsync(
            "RL_LOGIN_SLIDES",
            nombreGuardado,
            "UPLOAD",
            null,
            JsonConvert.SerializeObject(new
            {
                NombreOriginal = nombreOriginal,
                NombreGuardado = nombreGuardado,
                Url = url,
                TipoMime = tipoMime,
                TamanioBytes = tamanioBytes
            }),
            usuarioId,
            null,
            ip,
            "Configuracion");

    private Task RegistrarAuditoriaSlideAsync(string accion, string registroId, LoginSlide? anterior, LoginSlide? nuevo, long usuarioId, string? ip) =>
        _auditoriaRepository.RegistrarAsync(
            "RL_LOGIN_SLIDES",
            registroId,
            accion,
            anterior == null ? null : JsonConvert.SerializeObject(anterior),
            nuevo == null ? null : JsonConvert.SerializeObject(nuevo),
            usuarioId,
            null,
            ip,
            "Configuracion");
}
