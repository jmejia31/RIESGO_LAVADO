namespace RL.API.Features.MatricesRiesgos.Persistence;

public enum ResultadoCambioEstadoFamiliaFormulario
{
    Exito,
    NoExiste,
    YaEstabaEnEstado,
    TieneVersionVigente
}

public enum ResultadoEliminacionFamiliaFormulario
{
    Exito,
    NoExiste,
    TieneVersiones
}

public interface IFamiliasFormularioLifecycleRepository
{
    Task<ResultadoCambioEstadoFamiliaFormulario> ActivarFamiliaFormularioAtomicoAsync(long famId);
    Task<ResultadoCambioEstadoFamiliaFormulario> DesactivarFamiliaFormularioAtomicoAsync(long famId);
    Task<ResultadoEliminacionFamiliaFormulario> EliminarFamiliaFormularioSeguraAsync(long famId);
}
