using System;
using System.Linq;
using System.Reflection;
using Oracle.ManagedDataAccess.Client;
using RL.API.Features.Auditoria.Persistence;
using RL.API.Features.MatricesRiesgos.Persistence;
using RL.API.Infrastructure.Database;
using Xunit;

namespace RL.API.Tests.Features.MatricesRiesgos;

public sealed class MatricesRiesgosInstitutionalAuditTests
{
    [Fact]
    public void Repositorio_ExigeAuditoriaInstitucionalPorConstructor()
    {
        ConstructorInfo constructor = Assert.Single(typeof(MatricesRiesgosRepository).GetConstructors());
        Type[] parametros = constructor.GetParameters().Select(p => p.ParameterType).ToArray();

        Assert.Equal(new[] { typeof(OracleDbContext), typeof(IAuditoriaRepository) }, parametros);
    }

    [Fact]
    public void Repositorio_NoConservaMetodoDeAuditoriaLocal()
    {
        MethodInfo? metodoLocal = typeof(MatricesRiesgosRepository).GetMethod(
            "InsertarAuditoriaCampoAsync",
            BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);

        Assert.Null(metodoLocal);
    }

    [Fact]
    public void ContratoAuditoria_ExponeRegistroConConexionYTransaccionCompartidas()
    {
        MethodInfo metodo = Assert.Single(
            typeof(IAuditoriaRepository).GetMethods(),
            candidate =>
            {
                ParameterInfo[] parameters = candidate.GetParameters();
                return candidate.Name == nameof(IAuditoriaRepository.RegistrarAsync)
                    && parameters.Length == 11
                    && parameters[0].ParameterType == typeof(OracleConnection)
                    && parameters[1].ParameterType == typeof(OracleTransaction);
            });

        Assert.Equal(typeof(Task), metodo.ReturnType);
    }
}
