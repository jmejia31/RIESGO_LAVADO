using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RL.API.Core.Security;
using RL.API.Features.MatricesRiesgos;
using RL.API.Features.MatricesRiesgos.Application;
using RL.API.Features.MatricesRiesgos.Persistence;
using RL.API.Infrastructure.Caching;
using RL.API.Shared.Results;
using RL.API.Tests.Support;
using Xunit;

namespace RL.API.Tests.Features.MatricesRiesgos;

public sealed class MatricesRiesgosFamiliasF65Fam1Tests
{
    [Fact]
    public async Task Activar_IdInvalido_Retorna400SinPersistir()
    {
        FamiliasFormularioLifecycleService service = CrearService(out InterfaceStub repo, out _);

        ServiceResult result = await service.ActivarFamiliaFormularioAsync(0);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.Empty(repo.Invocations);
    }

    [Fact]
    public async Task Activar_FamiliaInactiva_RetornaOkEInvalidaCache()
    {
        FamiliasFormularioLifecycleService service = CrearService(out InterfaceStub repo, out InterfaceStub cache);
        repo.On(nameof(IFamiliasFormularioLifecycleRepository.ActivarFamiliaFormularioAtomicoAsync), _ =>
            Task.FromResult(ResultadoCambioEstadoFamiliaFormulario.Exito));

        ServiceResult result = await service.ActivarFamiliaFormularioAsync(12);

        Assert.True(result.Success);
        Assert.Contains("activada", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Single(cache.CallsTo(nameof(IApplicationCache.Invalidate)));
    }

    [Fact]
    public async Task Activar_FamiliaYaActiva_EsIdempotenteSinInvalidacion()
    {
        FamiliasFormularioLifecycleService service = CrearService(out InterfaceStub repo, out InterfaceStub cache);
        repo.On(nameof(IFamiliasFormularioLifecycleRepository.ActivarFamiliaFormularioAtomicoAsync), _ =>
            Task.FromResult(ResultadoCambioEstadoFamiliaFormulario.YaEstabaEnEstado));

        ServiceResult result = await service.ActivarFamiliaFormularioAsync(12);

        Assert.True(result.Success);
        Assert.Contains("ya se encuentra activa", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(cache.CallsTo(nameof(IApplicationCache.Invalidate)));
    }

    [Fact]
    public async Task Activar_FamiliaInexistente_Retorna404()
    {
        FamiliasFormularioLifecycleService service = CrearService(out InterfaceStub repo, out _);
        repo.On(nameof(IFamiliasFormularioLifecycleRepository.ActivarFamiliaFormularioAtomicoAsync), _ =>
            Task.FromResult(ResultadoCambioEstadoFamiliaFormulario.NoExiste));

        ServiceResult result = await service.ActivarFamiliaFormularioAsync(777);

        Assert.False(result.Success);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task Eliminar_IdInvalido_Retorna400SinPersistir()
    {
        FamiliasFormularioLifecycleService service = CrearService(out InterfaceStub repo, out _);

        ServiceResult result = await service.EliminarFamiliaFormularioAsync(-1);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.Empty(repo.Invocations);
    }

    [Fact]
    public async Task Eliminar_FamiliaVacia_RetornaOkEInvalidaCache()
    {
        FamiliasFormularioLifecycleService service = CrearService(out InterfaceStub repo, out InterfaceStub cache);
        repo.On(nameof(IFamiliasFormularioLifecycleRepository.EliminarFamiliaFormularioSeguraAsync), _ =>
            Task.FromResult(ResultadoEliminacionFamiliaFormulario.Exito));

        ServiceResult result = await service.EliminarFamiliaFormularioAsync(31);

        Assert.True(result.Success);
        Assert.Contains("eliminada", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Single(cache.CallsTo(nameof(IApplicationCache.Invalidate)));
    }

    [Fact]
    public async Task Eliminar_FamiliaInexistente_Retorna404()
    {
        FamiliasFormularioLifecycleService service = CrearService(out InterfaceStub repo, out _);
        repo.On(nameof(IFamiliasFormularioLifecycleRepository.EliminarFamiliaFormularioSeguraAsync), _ =>
            Task.FromResult(ResultadoEliminacionFamiliaFormulario.NoExiste));

        ServiceResult result = await service.EliminarFamiliaFormularioAsync(404);

        Assert.False(result.Success);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task Eliminar_FamiliaConVersiones_Retorna400YPreservaTrazabilidad()
    {
        FamiliasFormularioLifecycleService service = CrearService(out InterfaceStub repo, out _);
        repo.On(nameof(IFamiliasFormularioLifecycleRepository.EliminarFamiliaFormularioSeguraAsync), _ =>
            Task.FromResult(ResultadoEliminacionFamiliaFormulario.TieneVersiones));

        ServiceResult result = await service.EliminarFamiliaFormularioAsync(8);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.Contains("versiones asociadas", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("trazabilidad histórica", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LifecycleController_ConservaAuthorizeYModulo10()
    {
        Type controller = typeof(FamiliasFormularioLifecycleController);

        Assert.NotNull(controller.GetCustomAttribute<AuthorizeAttribute>());
        ModuloAuthorizeAttribute modulo = controller.GetCustomAttribute<ModuloAuthorizeAttribute>()
            ?? throw new InvalidOperationException("El controller de familias debe conservar ModuloAuthorize(10).");
        Assert.NotNull(modulo);
    }

    [Theory]
    [InlineData(nameof(FamiliasFormularioLifecycleController.ActivarFamiliaFormulario))]
    [InlineData(nameof(FamiliasFormularioLifecycleController.EliminarFamiliaFormulario))]
    public void LifecycleMutations_ExigenAdministradorCanonico(string methodName)
    {
        MethodInfo method = typeof(FamiliasFormularioLifecycleController).GetMethod(methodName)
            ?? throw new InvalidOperationException($"No existe {methodName}.");
        AuthorizeAttribute authorize = Assert.Single(method.GetCustomAttributes<AuthorizeAttribute>());

        Assert.Equal(SystemRoles.Administrador, authorize.Roles);
    }

    [Theory]
    [InlineData(nameof(FamiliasFormularioLifecycleController.ActivarFamiliaFormulario))]
    [InlineData(nameof(FamiliasFormularioLifecycleController.EliminarFamiliaFormulario))]
    public void LifecycleMutations_ExigenAuditRequired(string methodName)
    {
        MethodInfo method = typeof(FamiliasFormularioLifecycleController).GetMethod(methodName)
            ?? throw new InvalidOperationException($"No existe {methodName}.");

        Assert.NotNull(method.GetCustomAttribute<AuditRequiredAttribute>());
    }

    [Fact]
    public void Activar_ExponeContratoPutExplicito()
    {
        MethodInfo method = typeof(FamiliasFormularioLifecycleController)
            .GetMethod(nameof(FamiliasFormularioLifecycleController.ActivarFamiliaFormulario))!;
        HttpPutAttribute route = Assert.Single(method.GetCustomAttributes<HttpPutAttribute>());

        Assert.Equal("{id:long}/activar", route.Template);
    }

    [Fact]
    public void Eliminar_ExponeContratoDeleteExplicito()
    {
        MethodInfo method = typeof(FamiliasFormularioLifecycleController)
            .GetMethod(nameof(FamiliasFormularioLifecycleController.EliminarFamiliaFormulario))!;
        HttpDeleteAttribute route = Assert.Single(method.GetCustomAttributes<HttpDeleteAttribute>());

        Assert.Equal("{id:long}", route.Template);
    }

    [Theory]
    [InlineData(nameof(MatricesRiesgosController.CrearFamiliaFormulario))]
    [InlineData(nameof(MatricesRiesgosController.ActualizarFamiliaFormulario))]
    [InlineData(nameof(MatricesRiesgosController.DesactivarFamiliaFormulario))]
    public void MutacionesFamiliaExistentes_ExigenAdministradorYAuditoria(string methodName)
    {
        MethodInfo method = typeof(MatricesRiesgosController).GetMethod(methodName)
            ?? throw new InvalidOperationException($"No existe {methodName}.");
        AuthorizeAttribute authorize = Assert.Single(method.GetCustomAttributes<AuthorizeAttribute>());

        Assert.Equal(SystemRoles.Administrador, authorize.Roles);
        Assert.NotNull(method.GetCustomAttribute<AuditRequiredAttribute>());
    }

    [Fact]
    public void PersistenciaLifecycle_SerializaOperacionesConLockDeFamilia()
    {
        string source = LeerArchivo(
            "backend", "RL.API", "Features", "MatricesRiesgos", "Persistence",
            "FamiliasFormularioLifecycleRepository.cs");

        Assert.Contains("FROM RL_MR_FAMILIAS_FORMULARIO", source, StringComparison.Ordinal);
        Assert.Contains("FOR UPDATE", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Desactivar_ExigePublishedVigenteAntesDeCambiarEstado()
    {
        string source = ObtenerMetodoFuente(
            LeerLifecycleRepository(),
            "DesactivarFamiliaFormularioAtomicoAsync",
            "EliminarFamiliaFormularioSeguraAsync");

        Assert.Contains("VER_ESTADO = 'PUBLISHED'", source, StringComparison.Ordinal);
        Assert.Contains("VER_VIGENTE = 1", source, StringComparison.Ordinal);
        Assert.Contains("TieneVersionVigente", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Eliminar_BloqueaCualquierVersionSinImportarEstado()
    {
        string source = ObtenerMetodoFuente(
            LeerLifecycleRepository(),
            "EliminarFamiliaFormularioSeguraAsync",
            "ObtenerFamiliaBloqueadaAsync");

        Assert.Contains("FROM RL_MR_VERSIONES_FORMULARIO", source, StringComparison.Ordinal);
        Assert.DoesNotContain("VER_ESTADO", source, StringComparison.Ordinal);
        Assert.DoesNotContain("VER_VIGENTE", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Eliminar_UsaDeleteDefensivoNotExists()
    {
        string source = ObtenerMetodoFuente(
            LeerLifecycleRepository(),
            "EliminarFamiliaFormularioSeguraAsync",
            "ObtenerFamiliaBloqueadaAsync");

        Assert.Contains("DELETE FROM RL_MR_FAMILIAS_FORMULARIO", source, StringComparison.Ordinal);
        Assert.Contains("NOT EXISTS", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Eliminar_NoIncluyeCascadeNiBorradoDeVersiones()
    {
        string source = ObtenerMetodoFuente(
            LeerLifecycleRepository(),
            "EliminarFamiliaFormularioSeguraAsync",
            "ObtenerFamiliaBloqueadaAsync");

        Assert.DoesNotContain("CASCADE", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DELETE FROM RL_MR_VERSIONES_FORMULARIO", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Eliminar_ConvierteViolacionFkConcurrenteEnBloqueoFuncional()
    {
        string source = ObtenerMetodoFuente(
            LeerLifecycleRepository(),
            "EliminarFamiliaFormularioSeguraAsync",
            "ObtenerFamiliaBloqueadaAsync");

        Assert.Contains("ex.Number == 2292", source, StringComparison.Ordinal);
        Assert.Contains("TieneVersiones", source, StringComparison.Ordinal);
    }

    [Fact]
    public void ActualizarFamilia_SoloActualizaMetadatosYNoEstado()
    {
        string source = ObtenerMetodoFuente(
            LeerArchivo(
                "backend", "RL.API", "Features", "MatricesRiesgos", "Persistence",
                "SafeMatricesRiesgosRepository.cs"),
            "ActualizarFamiliaFormularioAsync",
            "DesactivarFamiliaFormularioAtomicoAsync");

        Assert.Contains("SET FAM_NOMBRE", source, StringComparison.Ordinal);
        Assert.Contains("FAM_DESCRIPCION", source, StringComparison.Ordinal);
        Assert.DoesNotContain("SET FAM_ACTIVO", source, StringComparison.Ordinal);
    }

    [Fact]
    public void CodigoFamilia_NoFormaParteDelUpdateDeMetadatos()
    {
        string source = ObtenerMetodoFuente(
            LeerArchivo(
                "backend", "RL.API", "Features", "MatricesRiesgos", "Persistence",
                "SafeMatricesRiesgosRepository.cs"),
            "ActualizarFamiliaFormularioAsync",
            "DesactivarFamiliaFormularioAtomicoAsync");

        Assert.DoesNotContain("SET FAM_CODIGO", source, StringComparison.Ordinal);
    }

    [Fact]
    public void MutacionesFamilia_RegistranAuditoriaTransaccional()
    {
        string safeSource = LeerArchivo(
            "backend", "RL.API", "Features", "MatricesRiesgos", "Persistence",
            "SafeMatricesRiesgosRepository.cs");
        string lifecycleSource = LeerLifecycleRepository();

        Assert.Contains("_auditoria.RegistrarAsync", safeSource, StringComparison.Ordinal);
        Assert.Contains("\"INSERT\"", safeSource, StringComparison.Ordinal);
        Assert.Contains("\"UPDATE\"", safeSource, StringComparison.Ordinal);
        Assert.Contains("_auditoria.RegistrarAsync", lifecycleSource, StringComparison.Ordinal);
        Assert.Contains("\"DELETE\"", lifecycleSource, StringComparison.Ordinal);
    }

    [Fact]
    public void Program_UsaDecoradorSeguroYLifecycleRepository()
    {
        string source = LeerArchivo("backend", "RL.API", "Program.cs");

        Assert.Contains("AddScoped<MatricesRiesgosRepository>()", source, StringComparison.Ordinal);
        Assert.Contains("AddScoped<IMatricesRiesgosRepository, SafeMatricesRiesgosRepository>()", source, StringComparison.Ordinal);
        Assert.Contains("AddScoped<IFamiliasFormularioLifecycleRepository, FamiliasFormularioLifecycleRepository>()", source, StringComparison.Ordinal);
        Assert.Contains("AddScoped<FamiliasFormularioLifecycleService>()", source, StringComparison.Ordinal);
    }

    private static FamiliasFormularioLifecycleService CrearService(
        out InterfaceStub repositoryStub,
        out InterfaceStub cacheStub)
    {
        IFamiliasFormularioLifecycleRepository repository =
            InterfaceStub.Create<IFamiliasFormularioLifecycleRepository>(out repositoryStub);
        IApplicationCache cache = InterfaceStub.Create<IApplicationCache>(out cacheStub);
        cacheStub.On(nameof(IApplicationCache.Invalidate), _ => null);
        return new FamiliasFormularioLifecycleService(repository, cache);
    }

    private static string LeerLifecycleRepository() => LeerArchivo(
        "backend", "RL.API", "Features", "MatricesRiesgos", "Persistence",
        "FamiliasFormularioLifecycleRepository.cs");

    private static string ObtenerMetodoFuente(string source, string inicio, string siguiente)
    {
        int start = source.IndexOf(inicio, StringComparison.Ordinal);
        Assert.True(start >= 0, $"No se encontró el método {inicio}.");

        int searchFrom = start + inicio.Length;
        int end = -1;
        while (searchFrom < source.Length)
        {
            int candidate = source.IndexOf(siguiente, searchFrom, StringComparison.Ordinal);
            if (candidate < 0)
            {
                break;
            }

            int lineStart = source.LastIndexOf('\n', candidate) + 1;
            string declarationPrefix = source[lineStart..candidate].TrimStart();
            if (declarationPrefix.StartsWith("public ", StringComparison.Ordinal)
                || declarationPrefix.StartsWith("private ", StringComparison.Ordinal)
                || declarationPrefix.StartsWith("protected ", StringComparison.Ordinal)
                || declarationPrefix.StartsWith("internal ", StringComparison.Ordinal))
            {
                end = candidate;
                break;
            }

            searchFrom = candidate + siguiente.Length;
        }

        Assert.True(end > start, $"No se encontró el límite posterior {siguiente}.");
        return source[start..end];
    }

    private static string LeerArchivo(params string[] segments)
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "RIESGO_LAVADO.sln")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        string path = Path.Combine(new[] { directory!.FullName }.Concat(segments).ToArray());
        Assert.True(File.Exists(path), $"No se encontró el archivo {path}.");
        return File.ReadAllText(path);
    }
}
