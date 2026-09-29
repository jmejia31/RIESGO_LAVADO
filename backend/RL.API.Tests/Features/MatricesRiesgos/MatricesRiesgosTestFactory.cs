using RL.API.Features.Auditoria.Persistence;
using RL.API.Features.MatricesRiesgos.Application;
using RL.API.Features.MatricesRiesgos.Contracts;
using RL.API.Features.MatricesRiesgos.Domain;
using RL.API.Features.MatricesRiesgos.Persistence;
using RL.API.Tests.Support;

namespace RL.API.Tests.Features.MatricesRiesgos;

internal static class MatricesRiesgosTestFactory
{
    public static MatricesRiesgosAppService CreateAppService(
        IMatricesRiesgosRepository repository,
        IFormularioValidador validator,
        IMatricesRiesgoService calculator,
        IAuditoriaRepository audit)
    {
        return new MatricesRiesgosAppService(repository, validator, calculator, audit,
            CreateLegacyRuntime(), CreateControlsRepository());
    }

    public static MatricesRiesgosMitigacionService CreateMitigationService(IMatricesRiesgosMitigacionRepository repository)
    {
        IMatricesRiesgosRepository evaluations = InterfaceStub.Create<IMatricesRiesgosRepository>(out InterfaceStub stub);
        stub.On(nameof(IMatricesRiesgosRepository.ObtenerEvaluacionAsync), arguments =>
            Task.FromResult<EvaluacionRiesgoDto?>(new EvaluacionRiesgoDto
            {
                EvaId = (long)arguments[0]!, EvaVersionId = 1, EvaVersionRow = 1,
                EvaEstado = "BORRADOR", EvaDataJson = "{}", EvaDataCalcJson = "{}"
            }));
        stub.On(nameof(IMatricesRiesgosRepository.ObtenerVersionFormularioAsync), arguments =>
            Task.FromResult<VersionFormularioDto?>(new VersionFormularioDto
            {
                VerId = (long)arguments[0]!, VerJson = "{\"secciones\":[]}" 
            }));
        return new MatricesRiesgosMitigacionService(repository, evaluations, CreateLegacyRuntime());
    }

    public static VersionedCalculationRuntimeService CreateLegacyRuntime()
    {
        ICalculoConfiguracionRepository configuration = InterfaceStub.Create<ICalculoConfiguracionRepository>(out InterfaceStub stub);
        stub.On(nameof(ICalculoConfiguracionRepository.ListarFormulaBindingsPorVersionFormularioAsync), _ =>
            Task.FromResult<IReadOnlyList<FormulaBindingDto>>(Array.Empty<FormulaBindingDto>()));
        return new VersionedCalculationRuntimeService(configuration, new DbDrivenCalculationRuntimeFactory(configuration));
    }

    public static IMatricesRiesgosMitigacionRepository CreateControlsRepository()
    {
        IMatricesRiesgosMitigacionRepository controls = InterfaceStub.Create<IMatricesRiesgosMitigacionRepository>(out InterfaceStub stub);
        stub.On(nameof(IMatricesRiesgosMitigacionRepository.ListarControlesAsync), _ =>
            Task.FromResult<IReadOnlyList<ControlRiesgoDto>>(Array.Empty<ControlRiesgoDto>()));
        return controls;
    }
}
