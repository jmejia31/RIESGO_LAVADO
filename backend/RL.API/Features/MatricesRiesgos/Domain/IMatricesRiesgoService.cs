using RL.API.Shared.Results;

namespace RL.API.Features.MatricesRiesgos.Domain;

public interface IMatricesRiesgoService
{
    ServiceResult<MatrizCalculoResultadoDto> Calcular(MatrizCalculoRequestDto request);
}
