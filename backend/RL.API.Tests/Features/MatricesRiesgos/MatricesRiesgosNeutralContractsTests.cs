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
        MethodInfo repositorio = Assert.Single(
            typeof(IMatricesRiesgosRepository).GetMethods(),
            metodo => metodo.Name == nameof(IMatricesRiesgosRepository.ObtenerConsolidadoTipadoAsync));
        MethodInfo aplicacion = Assert.Single(
            typeof(IMatricesRiesgosAppService).GetMethods(),
            metodo => metodo.Name == nameof(IMatricesRiesgosAppService.ObtenerConsolidadoTipadoAsync));

        Assert.Equal(typeof(Task<IReadOnlyList<RiesgoReporteFilaDto>>), repositorio.ReturnType);
        Assert.Equal(typeof(Task<ServiceResult<IReadOnlyList<RiesgoReporteFilaDto>>>), aplicacion.ReturnType);
    }

    [Fact]
    public void MetodologiaVigente_ExponeContratoNeutroEnRepositorioYAplicacion()
    {
        MethodInfo repositorio = Assert.Single(
            typeof(IMatricesRiesgosRepository).GetMethods(),
            metodo => metodo.Name == nameof(IMatricesRiesgosRepository.ObtenerMetodologiaDinamicaVigenteAsync));
        MethodInfo aplicacion = Assert.Single(
            typeof(IMatricesRiesgosAppService).GetMethods(),
            metodo => metodo.Name == nameof(IMatricesRiesgosAppService.ObtenerMetodologiaDinamicaVigenteAsync));

        Assert.Equal(typeof(Task<MetodologiaFormularioDto>), repositorio.ReturnType);
        Assert.Equal(typeof(Task<ServiceResult<MetodologiaFormularioDto>>), aplicacion.ReturnType);
    }

    [Fact]
    public void InterfacesPublicas_NoExponenMetodosDelContratoAnterior()
    {
        string[] nombresRetirados =
        {
            "ObtenerConsolidadoMatricesAsync",
            "ObtenerMetodologiaVigenteAsync",
            "ObtenerDashboardAsync",
            "ObtenerReporteAsync"
        };

        string[] repositorio = typeof(IMatricesRiesgosRepository).GetMethods().Select(m => m.Name).ToArray();
        string[] aplicacion = typeof(IMatricesRiesgosAppService).GetMethods().Select(m => m.Name).ToArray();

        Assert.DoesNotContain(repositorio, nombre => nombresRetirados.Contains(nombre, StringComparer.Ordinal));
        Assert.DoesNotContain(aplicacion, nombre => nombresRetirados.Contains(nombre, StringComparer.Ordinal));
    }
}
