using System;
using System.Linq;
using System.Reflection;
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
}
