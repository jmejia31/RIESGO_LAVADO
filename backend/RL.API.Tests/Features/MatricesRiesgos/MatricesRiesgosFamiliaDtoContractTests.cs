using System.Reflection;
using RL.API.Features.MatricesRiesgos.Contracts;
using Xunit;

namespace RL.API.Tests.Features.MatricesRiesgos;

public sealed class MatricesRiesgosFamiliaDtoContractTests
{
    [Fact]
    public void FamiliaDto_ExponeDatosAutoritativosParaElGestor()
    {
        Type dto = typeof(FamiliaFormularioDto);

        Assert.NotNull(dto.GetProperty(nameof(FamiliaFormularioDto.FamId)));
        Assert.NotNull(dto.GetProperty(nameof(FamiliaFormularioDto.FamCodigo)));
        Assert.NotNull(dto.GetProperty(nameof(FamiliaFormularioDto.FamNombre)));
        Assert.NotNull(dto.GetProperty(nameof(FamiliaFormularioDto.FamDescripcion)));
        Assert.NotNull(dto.GetProperty(nameof(FamiliaFormularioDto.FamActivo)));
        Assert.NotNull(dto.GetProperty(nameof(FamiliaFormularioDto.FamFechaCreacion)));
        Assert.NotNull(dto.GetProperty(nameof(FamiliaFormularioDto.TotalVersiones)));
        Assert.NotNull(dto.GetProperty(nameof(FamiliaFormularioDto.TieneVersionVigente)));
    }

    [Fact]
    public void UpdateDto_NoExponeCodigoMutable()
    {
        Assert.Null(typeof(ActualizarFamiliaFormularioDto).GetProperty("FamCodigo"));
    }
}
