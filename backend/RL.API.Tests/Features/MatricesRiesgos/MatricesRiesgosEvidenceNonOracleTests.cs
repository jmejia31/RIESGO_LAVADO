using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using RL.API.Features.MatricesRiesgos.Application;
using RL.API.Features.MatricesRiesgos.Contracts;
using RL.API.Features.MatricesRiesgos.Domain;
using RL.API.Features.MatricesRiesgos.Persistence;
using RL.API.Shared.Results;
using RL.API.Tests.Support;
using Xunit;

namespace RL.API.Tests.Features.MatricesRiesgos;

public sealed class MatricesRiesgosEvidenceNonOracleTests
{
    public static IEnumerable<object[]> DestinosPermitidos()
    {
        yield return new object[] { TipoEntidadEvidencia.Riesgo, "RL_MR_RIESGOS", "RIE_ID" };
        yield return new object[] { TipoEntidadEvidencia.Evaluacion, "RL_MR_EVALUACIONES_RIESGO", "EVA_ID" };
        yield return new object[] { TipoEntidadEvidencia.Control, "RL_MR_CONTROLES_RIESGO", "CON_ID" };
        yield return new object[] { TipoEntidadEvidencia.Plan, "RL_MR_PLANES", "PLA_ID" };
        yield return new object[] { TipoEntidadEvidencia.Actividad, "RL_MR_ACTIVIDADES", "ACT_ID" };
        yield return new object[] { TipoEntidadEvidencia.Alerta, "RL_MR_SENALES_ALERTA", "ALE_ID" };
        yield return new object[] { TipoEntidadEvidencia.Automonitoreo, "RL_MR_AUTOMONITOREO", "MON_ID" };
    }

    [Theory]
    [MemberData(nameof(DestinosPermitidos))]
    public void Repositorio_ResuelveCadaDestinoConSqlCerradoYParametrizado(
        TipoEntidadEvidencia tipo,
        string tabla,
        string columna)
    {
        MethodInfo metodo = ObtenerMetodoConsultaEntidad();

        string sql = Assert.IsType<string>(metodo.Invoke(null, new object[] { tipo }));

        Assert.Contains($"FROM {tabla}", sql, StringComparison.Ordinal);
        Assert.Contains($"SELECT {columna}", sql, StringComparison.Ordinal);
        Assert.Contains($"WHERE {columna} = :id", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void Repositorio_RechazaDestinoFueraDeLaListaCerrada()
    {
        MethodInfo metodo = ObtenerMetodoConsultaEntidad();

        TargetInvocationException error = Assert.Throws<TargetInvocationException>(() =>
            metodo.Invoke(null, new object[] { (TipoEntidadEvidencia)999 }));

        InvalidOperationException causa = Assert.IsType<InvalidOperationException>(error.InnerException);
        Assert.Contains("no está permitido", causa.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [MemberData(nameof(DestinosPermitidos))]
    public async Task AppService_DelegaLosSieteDestinosAlVinculoGenerico(
        TipoEntidadEvidencia tipo,
        string _,
        string __)
    {
        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repo);
        repo.On(nameof(IMatricesRiesgosRepository.VincularEvidenciaAsync), _ => Task.FromResult(true));
        var dto = new VincularEvidenciaDto
        {
            EvidenciaId = 8,
            EntidadId = 15,
            TipoEntidad = tipo
        };

        ServiceResult result = await service.VincularEvidenciaAsync(dto, 99, "127.0.0.1");

        Assert.True(result.Success);
        StubInvocation llamada = Assert.Single(repo.CallsTo(nameof(IMatricesRiesgosRepository.VincularEvidenciaAsync)));
        Assert.Same(dto, llamada.Arguments[0]);
        Assert.Equal(99L, llamada.Arguments[1]);
        Assert.Equal("127.0.0.1", llamada.Arguments[2]);
    }

    [Fact]
    public async Task AppService_CuandoRepositorioNoVincula_RetornaBadRequest()
    {
        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repo);
        repo.On(nameof(IMatricesRiesgosRepository.VincularEvidenciaAsync), _ => Task.FromResult(false));

        ServiceResult result = await service.VincularEvidenciaAsync(
            new VincularEvidenciaDto { EvidenciaId = 8, EntidadId = 15, TipoEntidad = TipoEntidadEvidencia.Evaluacion },
            99,
            null);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task AppService_CuandoNoExisteEvidenciaOEntidad_RetornaNotFound()
    {
        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repo);
        repo.On(nameof(IMatricesRiesgosRepository.VincularEvidenciaAsync), _ =>
            throw new KeyNotFoundException("No se encontró la entidad destino."));

        ServiceResult result = await service.VincularEvidenciaAsync(
            new VincularEvidenciaDto { EvidenciaId = 8, EntidadId = 15, TipoEntidad = TipoEntidadEvidencia.Plan },
            99,
            null);

        Assert.False(result.Success);
        Assert.Equal(404, result.StatusCode);
        Assert.Contains("entidad destino", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AppService_CuandoDestinoEsInvalido_RetornaBadRequest()
    {
        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repo);
        repo.On(nameof(IMatricesRiesgosRepository.VincularEvidenciaAsync), _ =>
            throw new InvalidOperationException("El tipo de entidad de evidencia no está permitido."));

        ServiceResult result = await service.VincularEvidenciaAsync(
            new VincularEvidenciaDto { EvidenciaId = 8, EntidadId = 15, TipoEntidad = (TipoEntidadEvidencia)999 },
            99,
            null);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.Contains("no está permitido", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static MethodInfo ObtenerMetodoConsultaEntidad() =>
        typeof(MatricesRiesgosRepository).GetMethod(
            "ObtenerConsultaEntidadEvidencia",
            BindingFlags.NonPublic | BindingFlags.Static)
        ?? throw new InvalidOperationException("No se encontró el resolvedor cerrado de entidades de evidencia.");

    private static MatricesRiesgosAppService CrearServicio(out InterfaceStub repoStub)
    {
        IMatricesRiesgosRepository repo = InterfaceStub.Create<IMatricesRiesgosRepository>(out repoStub);
        IFormularioValidador validador = InterfaceStub.Create<IFormularioValidador>(out _);
        IMatricesRiesgoService calculador = InterfaceStub.Create<IMatricesRiesgoService>(out _);
        IAuditoriaRepository auditoria = InterfaceStub.Create<IAuditoriaRepository>(out InterfaceStub auditoriaStub);
        auditoriaStub.On("RegistrarAsync", _ => Task.CompletedTask);
        return new MatricesRiesgosAppService(repo, validador, calculador, auditoria);
    }
}
