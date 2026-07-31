using RL.API.Features.MatricesRiesgos.Contracts;
using RL.API.Shared.Results;

namespace RL.API.Features.MatricesRiesgos.Domain;

public interface IMatricesRiesgoService
{
    /// <summary>
    /// Calcula VRI, ETP y VRR. Valida que el VRR matemático coincida con el VRR2 (de respuestas de formulario).
    /// </summary>
    ServiceResult<CalculoRiesgoResultadoDto> CalcularYValidarRiesgo(
        int frecuencia, 
        int impacto, 
        decimal preventivo, 
        decimal detectivo, 
        decimal correctivo, 
        int frecResidual, 
        int impResidual);
}
