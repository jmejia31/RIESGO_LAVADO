using Microsoft.AspNetCore.Mvc;
using RL.API.Features.MatricesRiesgos;
using RL.API.Features.MatricesRiesgos.Contracts;
using RL.API.Features.MatricesRiesgos.Domain;
using Xunit;

namespace RL.API.Tests.Features.MatricesRiesgos;

public sealed class CalculoConfiguracionValidationTests
{
    [Fact]
    public void NormalizeCode_TrimAndUppercase_CanonizesStableCode()
    {
        Assert.Equal("PESO_PREVENTIVO", CalculoConfiguracionValidation.NormalizeCode(" peso_preventivo ", "Código"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("with space")]
    [InlineData("1_START")]
    public void NormalizeCode_RejectsInvalidCode(string code)
    {
        Assert.Throws<InvalidOperationException>(() => CalculoConfiguracionValidation.NormalizeCode(code, "Código"));
    }

    [Fact]
    public void NativeFunction_RequiresHandlerAndRejectsDsl()
    {
        var withoutHandler = new CrearFuncionVersionDto
        {
            Tipo = "NATIVE",
            TipoResultado = "DECIMAL",
            MinArity = 1,
            Argumentos = [new FuncionArgumentoGuardarDto { Posicion = 1, Codigo = "VALUE", Nombre = "Value" }]
        };
        Assert.Throws<InvalidOperationException>(() => CalculoConfiguracionValidation.ValidateFunctionVersion(withoutHandler));

        var withDsl = new CrearFuncionVersionDto
        {
            Tipo = "NATIVE",
            TipoResultado = "DECIMAL",
            HandlerKey = "SAFE_V1",
            DefinicionDsl = "1",
            MinArity = 0
        };
        Assert.Throws<InvalidOperationException>(() => CalculoConfiguracionValidation.ValidateFunctionVersion(withDsl));
    }

    [Fact]
    public void CompositeFunction_RequiresDslAndDoesNotAcceptHandler()
    {
        var dto = new CrearFuncionVersionDto
        {
            Tipo = "COMPOSITE",
            TipoResultado = "DECIMAL",
            DefinicionDsl = "MAX(1, VALUE)",
            MinArity = 1,
            Argumentos = [new FuncionArgumentoGuardarDto { Posicion = 1, Codigo = "VALUE", Nombre = "Value" }]
        };

        var normalized = CalculoConfiguracionValidation.ValidateFunctionVersion(dto);

        Assert.Equal("COMPOSITE", normalized.Tipo);
        Assert.Null(normalized.Handler);
    }

    [Fact]
    public void FunctionArguments_RejectDuplicatePositionAndCode()
    {
        var dto = new CrearFuncionVersionDto
        {
            Tipo = "NATIVE",
            TipoResultado = "DECIMAL",
            HandlerKey = "SAFE_V1",
            MinArity = 2,
            Argumentos =
            [
                new FuncionArgumentoGuardarDto { Posicion = 1, Codigo = "VALUE", Nombre = "Value" },
                new FuncionArgumentoGuardarDto { Posicion = 1, Codigo = "OTHER", Nombre = "Other" }
            ]
        };

        Assert.Throws<InvalidOperationException>(() => CalculoConfiguracionValidation.ValidateFunctionVersion(dto));
    }

    [Theory]
    [InlineData("INTEGER", 10, null, null, null, null)]
    [InlineData("BOOLEAN", null, null, true, null, null)]
    [InlineData("TEXT", null, null, null, "texto", null)]
    public void ParameterVersion_AcceptsExactlyMatchingTypedValue(string type, int? integerValue, decimal? decimalValue, bool? booleanValue, string? textValue, DateTime? dateValue)
    {
        var dto = new CrearParametroVersionDto
        {
            Tipo = type,
            ValorEntero = integerValue,
            ValorDecimal = decimalValue,
            ValorBooleano = booleanValue,
            ValorTexto = textValue,
            ValorFecha = dateValue
        };

        Assert.Equal(type, CalculoConfiguracionValidation.ValidateParameterVersion(dto));
    }

    [Fact]
    public void ParameterVersion_AcceptsDecimalValue()
    {
        Assert.Equal("DECIMAL", CalculoConfiguracionValidation.ValidateParameterVersion(new CrearParametroVersionDto { Tipo = "DECIMAL", ValorDecimal = 10.5m }));
    }

    [Fact]
    public void ParameterVersion_RejectsIncoherentTypeAndValue()
    {
        var dto = new CrearParametroVersionDto { Tipo = "INTEGER", ValorDecimal = 1.2m };

        Assert.Throws<InvalidOperationException>(() => CalculoConfiguracionValidation.ValidateParameterVersion(dto));
    }

    [Fact]
    public void Hash_IsDeterministicSha256Hex()
    {
        string first = CalculoConfiguracionValidation.Hash("NATIVE|DECIMAL|ROUND_V1");
        string second = CalculoConfiguracionValidation.Hash("NATIVE|DECIMAL|ROUND_V1");

        Assert.Equal(first, second);
        Assert.Matches("^[0-9a-f]{64}$", first);
    }

    [Fact]
    public void Controller_UsesSingleCalculationConfigurationRoute()
    {
        var route = Assert.Single(typeof(CalculoConfiguracionController)
            .GetCustomAttributes(typeof(RouteAttribute), inherit: true)
            .Cast<RouteAttribute>());

        Assert.Equal("api/matrices-riesgos/configuracion-calculo", route.Template);
    }
}
