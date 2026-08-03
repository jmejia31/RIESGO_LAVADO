using System;
using System.Linq;
using Oracle.ManagedDataAccess.Client;
using RL.API.Features.Auditoria.Persistence;
using Xunit;

namespace RL.API.Tests.Features.Auditoria;

public sealed class AuditoriaTransaccionalContractTests
{
    [Fact]
    public void Contrato_ExponeRegistroConConexionYTransaccionCompartidas()
    {
        var parametros = new[]
        {
            typeof(OracleConnection), typeof(OracleTransaction), typeof(string), typeof(string),
            typeof(string), typeof(string), typeof(string), typeof(long?), typeof(string),
            typeof(string), typeof(string)
        };

        var contrato = typeof(IAuditoriaRepository).GetMethod("RegistrarAsync", parametros);
        var implementacion = typeof(AuditoriaRepository).GetMethod("RegistrarAsync", parametros);

        Assert.NotNull(contrato);
        Assert.NotNull(implementacion);
    }

    [Fact]
    public void Contrato_ConservaElRegistroNoTransaccionalParaOtrosModulos()
    {
        var sobrecargas = typeof(IAuditoriaRepository)
            .GetMethods()
            .Where(m => m.Name == "RegistrarAsync")
            .ToArray();

        Assert.Equal(2, sobrecargas.Length);
        Assert.Contains(sobrecargas, metodo => metodo.GetParameters().Length == 9);
        Assert.Contains(sobrecargas, metodo => metodo.GetParameters().Length == 11);
    }
}
