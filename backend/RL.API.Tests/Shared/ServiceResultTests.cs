using Xunit;
using RL.API.Shared.Results;

namespace RL.API.Tests.Shared;

public sealed class ServiceResultTests
{
    [Fact]
    public void Ok_ServiceResult_RetornaValoresCorrectos()
    {
        var result = ServiceResult.Ok("Operación exitosa");
        
        Assert.True(result.Success);
        Assert.Equal("Operación exitosa", result.Message);
        Assert.Equal(200, result.StatusCode);
    }

    [Fact]
    public void BadRequest_ServiceResult_RetornaValoresCorrectos()
    {
        var result = ServiceResult.BadRequest("Datos inválidos");
        
        Assert.False(result.Success);
        Assert.Equal("Datos inválidos", result.Message);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public void NotFound_ServiceResult_RetornaValoresCorrectos()
    {
        var result = ServiceResult.NotFound("Recurso no encontrado");
        
        Assert.False(result.Success);
        Assert.Equal("Recurso no encontrado", result.Message);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public void Ok_ServiceResultGeneric_RetornaValoresCorrectos()
    {
        var result = ServiceResult<string>.Ok("Datos de prueba", "Operación exitosa");
        
        Assert.True(result.Success);
        Assert.Equal("Datos de prueba", result.Data);
        Assert.Equal("Operación exitosa", result.Message);
        Assert.Equal(200, result.StatusCode);
    }

    [Fact]
    public void BadRequest_ServiceResultGeneric_RetornaValoresCorrectos()
    {
        var result = ServiceResult<string>.BadRequest("Fallo de validación");
        
        Assert.False(result.Success);
        Assert.Null(result.Data);
        Assert.Equal("Fallo de validación", result.Message);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public void NotFound_ServiceResultGeneric_RetornaValoresCorrectos()
    {
        var result = ServiceResult<string>.NotFound("No existe");
        
        Assert.False(result.Success);
        Assert.Null(result.Data);
        Assert.Equal("No existe", result.Message);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public void Constructor_ServiceResult_EstableceValores()
    {
        var result = new ServiceResult(false, "Error 500", 500);
        
        Assert.False(result.Success);
        Assert.Equal("Error 500", result.Message);
        Assert.Equal(500, result.StatusCode);
    }

    [Fact]
    public void Constructor_ServiceResultGeneric_EstableceValores()
    {
        var result = new ServiceResult<int>(false, 0, "Conflicto", 409);
        
        Assert.False(result.Success);
        Assert.Equal(0, result.Data);
        Assert.Equal("Conflicto", result.Message);
        Assert.Equal(409, result.StatusCode);
    }
}
