using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using RL.API.Features.MatricesRiesgos.Application;
using RL.API.Features.MatricesRiesgos.Contracts;
using RL.API.Features.MatricesRiesgos.Persistence;
using RL.API.Shared.Results;
using Xunit;

namespace RL.API.Tests.Features.MatricesRiesgos;

public sealed class MatricesRiesgosNeutralContractsTests
{
    [Fact]
    public void MetodologiaDinamica_ConservaVersionSeccionesCamposCatalogosYReglas()
    {
        Type contrato = typeof(MetodologiaFormularioDto);

        Assert.NotNull(contrato.GetProperty(nameof(MetodologiaFormularioDto.VersionFormularioId)));
        Assert.NotNull(contrato.GetProperty(nameof(MetodologiaFormularioDto.Codigo)));
        Assert.NotNull(contrato.GetProperty(nameof(MetodologiaFormularioDto.Version)));
        Assert.Equal(typeof(IReadOnlyList<SeccionFormularioDto>), contrato.GetProperty(nameof(MetodologiaFormularioDto.Secciones))!.PropertyType);
        Assert.Equal(typeof(IReadOnlyList<CatalogoMatricesDto>), contrato.GetProperty(nameof(MetodologiaFormularioDto.Catalogos))!.PropertyType);
        Assert.Equal(typeof(IReadOnlyList<ReglaCalculoMatricesDto>), contrato.GetProperty(nameof(MetodologiaFormularioDto.Reglas))!.PropertyType);
        Assert.Equal(typeof(IReadOnlyList<CampoFormularioDto>), typeof(SeccionFormularioDto).GetProperty(nameof(SeccionFormularioDto.Campos))!.PropertyType);
    }

    [Fact]
    public void ReporteConsolidado_ExponeFilaTipadaEnRepositorioYAplicacion()
    {
        MethodInfo? repositorio = typeof(IMatricesRiesgosRepository)
            .GetMethod(nameof(IMatricesRiesgosRepository.ObtenerConsolidadoTipadoAsync));
        MethodInfo? aplicacion = typeof(IMatricesRiesgosAppService)
            .GetMethod(nameof(IMatricesRiesgosAppService.ObtenerConsolidadoTipadoAsync));

        Assert.NotNull(repositorio);
        Assert.NotNull(aplicacion);
        Assert.Equal(typeof(Task<IReadOnlyList<RiesgoReporteFilaDto>>), repositorio!.ReturnType);
        Assert.Equal(typeof(Task<ServiceResult<IReadOnlyList<RiesgoReporteFilaDto>>>), aplicacion!.ReturnType);
    }

    [Fact]
    public void MetodologiaVigente_ExponeContratoNeutroEnRepositorioYAplicacion()
    {
        MethodInfo? repositorio = typeof(IMatricesRiesgosRepository)
            .GetMethod(nameof(IMatricesRiesgosRepository.ObtenerMetodologiaDinamicaVigenteAsync));
        MethodInfo? aplicacion = typeof(IMatricesRiesgosAppService)
            .GetMethod(nameof(IMatricesRiesgosAppService.ObtenerMetodologiaDinamicaVigenteAsync));

        Assert.NotNull(repositorio);
        Assert.NotNull(aplicacion);
        Assert.Equal(typeof(Task<MetodologiaFormularioDto>), Desanular(repositorio!.ReturnType));
        Assert.Equal(typeof(Task<ServiceResult<MetodologiaFormularioDto>>), aplicacion!.ReturnType);
    }

    private static Type Desanular(Type type)
    {
        // La nulabilidad de referencia no cambia el Type de reflexión en tiempo de ejecución.
        return type;
    }
}
