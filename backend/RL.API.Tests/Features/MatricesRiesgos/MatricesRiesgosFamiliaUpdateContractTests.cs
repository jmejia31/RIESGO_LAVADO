using RL.API.Features.MatricesRiesgos.Contracts;
using Xunit;

namespace RL.API.Tests.Features.MatricesRiesgos;

public sealed class MatricesRiesgosFamiliaUpdateContractTests
{
    [Fact]
    public void CodigoNoEsEditableDesdeActualizarDto()
    {
        Assert.Null(typeof(ActualizarFamiliaFormularioDto).GetProperty("FamCodigo"));
    }

    [Fact]
    public void CrearDto_SiDefineCodigoInstitucional()
    {
        Assert.NotNull(typeof(CrearFamiliaFormularioDto).GetProperty(nameof(CrearFamiliaFormularioDto.FamCodigo)));
    }
}
