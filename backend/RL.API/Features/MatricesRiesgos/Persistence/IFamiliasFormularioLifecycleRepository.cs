using RL.API.Features.MatricesRiesgos.Contracts;

namespace RL.API.Features.MatricesRiesgos.Persistence;

public enum ResultadoCambioEstadoFamiliaFormulario
{
    Exito,
    NoExiste,
    YaEstabaEnEstado,
    EstaArchivada,
    TieneVersionVigente,
    EsPredeterminada
}

public enum ResultadoArchivadoFamiliaFormulario
{
    Exito,
    NoExiste,
    YaArchivada,
    FamiliaActiva,
    EsPredeterminada,
    TieneVersionVigente
}

public enum ResultadoRestauracionFamiliaFormulario
{
    Exito,
    NoExiste,
    NoEstaArchivada
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
    Task<ResultadoArchivadoFamiliaFormulario> ArchivarFamiliaFormularioAtomicoAsync(long famId, string motivo);
    Task<ResultadoRestauracionFamiliaFormulario> RestaurarFamiliaFormularioAtomicoAsync(long famId);
    Task<FamiliaFormularioPurgaPrecheckDto?> ObtenerPurgaPrecheckAsync(long famId);
}
