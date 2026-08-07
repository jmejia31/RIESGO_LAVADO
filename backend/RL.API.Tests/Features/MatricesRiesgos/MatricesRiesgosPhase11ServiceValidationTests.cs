using RL.API.Features.MatricesRiesgos.Application;
using RL.API.Features.MatricesRiesgos.Contracts;
using RL.API.Features.MatricesRiesgos.Persistence;
using Xunit;

namespace RL.API.Tests.Features.MatricesRiesgos;

public sealed class MatricesRiesgosPhase11ServiceValidationTests
{
    private const long UsuarioId = 7;
    private const string Ip = "127.0.0.1";

    [Fact]
    public async Task Gestion_ListarYObtener_CubreExitoBadRequestYNotFound()
    {
        var repo = new GestionRepoFake
        {
            Riesgos = new[] { new RiesgoDto { RieId = 9, RieCodigo = "R-009", RieNombre = "Riesgo" } },
            Riesgo = new RiesgoDto { RieId = 9, RieCodigo = "R-009", RieNombre = "Riesgo" }
        };
        var service = new MatricesRiesgosGestionService(repo);

        var lista = await service.ListarRiesgosAsync(false);
        var invalido = await service.ObtenerRiesgoAsync(0);
        var encontrado = await service.ObtenerRiesgoAsync(9);
        repo.Riesgo = null;
        var inexistente = await service.ObtenerRiesgoAsync(99);

        Assert.True(lista.Success);
        Assert.Single(lista.Data!);
        Assert.Equal(400, invalido.StatusCode);
        Assert.True(encontrado.Success);
        Assert.Equal(9, encontrado.Data!.RieId);
        Assert.Equal(404, inexistente.StatusCode);
    }

    [Fact]
    public async Task Gestion_Crear_RechazaCadaContratoInvalido()
    {
        var service = new MatricesRiesgosGestionService(new GestionRepoFake());

        Assert.Equal(400, (await service.CrearRiesgoAsync(null!, UsuarioId, Ip)).StatusCode);
        Assert.Equal(400, (await service.CrearRiesgoAsync(ValidoRiesgo(codigo: ""), UsuarioId, Ip)).StatusCode);
        Assert.Equal(400, (await service.CrearRiesgoAsync(ValidoRiesgo(codigo: new string('A', 31)), UsuarioId, Ip)).StatusCode);
        Assert.Equal(400, (await service.CrearRiesgoAsync(ValidoRiesgo(nombre: ""), UsuarioId, Ip)).StatusCode);
        Assert.Equal(400, (await service.CrearRiesgoAsync(ValidoRiesgo(nombre: new string('N', 251)), UsuarioId, Ip)).StatusCode);
        Assert.Equal(400, (await service.CrearRiesgoAsync(ValidoRiesgo(descripcion: new string('D', 2001)), UsuarioId, Ip)).StatusCode);
    }

    [Fact]
    public async Task Gestion_CrearYActualizar_CubreExitoNotFoundYCatch()
    {
        var repo = new GestionRepoFake { CreateId = 77, UpdateResult = true };
        var service = new MatricesRiesgosGestionService(repo);

        var creado = await service.CrearRiesgoAsync(ValidoRiesgo(), UsuarioId, Ip);
        var idInvalido = await service.ActualizarRiesgoAsync(0, ValidoRiesgo(), UsuarioId, Ip);
        var actualizado = await service.ActualizarRiesgoAsync(77, ValidoRiesgo(), UsuarioId, Ip);
        repo.UpdateResult = false;
        var noEncontrado = await service.ActualizarRiesgoAsync(77, ValidoRiesgo(), UsuarioId, Ip);
        repo.ThrowInvalidOperation = true;
        var errorCrear = await service.CrearRiesgoAsync(ValidoRiesgo(), UsuarioId, Ip);
        var errorActualizar = await service.ActualizarRiesgoAsync(77, ValidoRiesgo(), UsuarioId, Ip);

        Assert.True(creado.Success);
        Assert.Equal(77, creado.Data);
        Assert.Equal(400, idInvalido.StatusCode);
        Assert.True(actualizado.Success);
        Assert.Equal(404, noEncontrado.StatusCode);
        Assert.Equal(400, errorCrear.StatusCode);
        Assert.Equal("Fallo controlado", errorCrear.Message);
        Assert.Equal(400, errorActualizar.StatusCode);
    }

    [Fact]
    public async Task Mitigacion_Listados_CubrenIdsInvalidosYExito()
    {
        var service = new MatricesRiesgosMitigacionService(new MitigacionRepoFake());

        Assert.Equal(400, (await service.ListarControlesAsync(0)).StatusCode);
        Assert.True((await service.ListarControlesAsync(1)).Success);
        Assert.Equal(400, (await service.ListarEvaluacionesControlAsync(0)).StatusCode);
        Assert.True((await service.ListarEvaluacionesControlAsync(1)).Success);
        Assert.Equal(400, (await service.ListarPlanesAsync(0)).StatusCode);
        Assert.True((await service.ListarPlanesAsync(1)).Success);
        Assert.Equal(400, (await service.ListarActividadesAsync(0)).StatusCode);
        Assert.True((await service.ListarActividadesAsync(1)).Success);
    }

    [Fact]
    public async Task Mitigacion_Control_RechazaTodosLosDominiosInvalidos()
    {
        var service = new MatricesRiesgosMitigacionService(new MitigacionRepoFake());

        Assert.Equal(400, (await service.CrearControlAsync(ValidoControl(evaluacionId: 0), UsuarioId, Ip)).StatusCode);
        Assert.Equal(400, (await service.CrearControlAsync(ValidoControl(tipo: "OTRO"), UsuarioId, Ip)).StatusCode);
        Assert.Equal(400, (await service.CrearControlAsync(ValidoControl(automatizacion: "ROBOT"), UsuarioId, Ip)).StatusCode);
        Assert.Equal(400, (await service.CrearControlAsync(ValidoControl(descripcion: ""), UsuarioId, Ip)).StatusCode);
        Assert.Equal(400, (await service.CrearControlAsync(ValidoControl(descripcion: new string('D', 501)), UsuarioId, Ip)).StatusCode);
        Assert.Equal(400, (await service.CrearControlAsync(ValidoControl(estado: ""), UsuarioId, Ip)).StatusCode);
        Assert.Equal(400, (await service.CrearControlAsync(ValidoControl(estado: new string('E', 21)), UsuarioId, Ip)).StatusCode);
    }

    [Fact]
    public async Task Mitigacion_Control_CubreExitoNotFoundYCatch()
    {
        var repo = new MitigacionRepoFake { UpdateControlResult = true };
        var service = new MatricesRiesgosMitigacionService(repo);

        Assert.True((await service.CrearControlAsync(ValidoControl(), UsuarioId, Ip)).Success);
        Assert.Equal(400, (await service.ActualizarControlAsync(0, ValidoControl(), UsuarioId, Ip)).StatusCode);
        Assert.True((await service.ActualizarControlAsync(1, ValidoControl(), UsuarioId, Ip)).Success);
        repo.UpdateControlResult = false;
        Assert.Equal(404, (await service.ActualizarControlAsync(1, ValidoControl(), UsuarioId, Ip)).StatusCode);
        repo.ThrowInvalidOperation = true;
        Assert.Equal(400, (await service.CrearControlAsync(ValidoControl(), UsuarioId, Ip)).StatusCode);
        Assert.Equal(400, (await service.ActualizarControlAsync(1, ValidoControl(), UsuarioId, Ip)).StatusCode);
    }

    [Fact]
    public async Task Mitigacion_EvaluacionControl_CubreValidacionesExitoYCatch()
    {
        var repo = new MitigacionRepoFake();
        var service = new MatricesRiesgosMitigacionService(repo);

        Assert.Equal(400, (await service.RegistrarEvaluacionControlAsync(0, ValidoEvaluacionControl(), UsuarioId, Ip)).StatusCode);
        Assert.Equal(400, (await service.RegistrarEvaluacionControlAsync(1, ValidoEvaluacionControl(-1), UsuarioId, Ip)).StatusCode);
        Assert.Equal(400, (await service.RegistrarEvaluacionControlAsync(1, ValidoEvaluacionControl(101), UsuarioId, Ip)).StatusCode);
        Assert.Equal(400, (await service.RegistrarEvaluacionControlAsync(1, ValidoEvaluacionControl(comentario: new string('C', 501)), UsuarioId, Ip)).StatusCode);
        Assert.True((await service.RegistrarEvaluacionControlAsync(1, ValidoEvaluacionControl(80), UsuarioId, Ip)).Success);
        repo.ThrowInvalidOperation = true;
        Assert.Equal(400, (await service.RegistrarEvaluacionControlAsync(1, ValidoEvaluacionControl(80), UsuarioId, Ip)).StatusCode);
    }

    [Fact]
    public async Task Mitigacion_Plan_RechazaTodasLasInvariantes()
    {
        var service = new MatricesRiesgosMitigacionService(new MitigacionRepoFake());

        Assert.Equal(400, (await service.CrearPlanAsync(ValidoPlan(evaluacionId: 0), UsuarioId, Ip)).StatusCode);
        Assert.Equal(400, (await service.CrearPlanAsync(ValidoPlan(descripcion: ""), UsuarioId, Ip)).StatusCode);
        Assert.Equal(400, (await service.CrearPlanAsync(ValidoPlan(descripcion: new string('D', 501)), UsuarioId, Ip)).StatusCode);
        Assert.Equal(400, (await service.CrearPlanAsync(ValidoPlan(avance: -1), UsuarioId, Ip)).StatusCode);
        Assert.Equal(400, (await service.CrearPlanAsync(ValidoPlan(avance: 101), UsuarioId, Ip)).StatusCode);
        Assert.Equal(400, (await service.CrearPlanAsync(ValidoPlan(presupuesto: -1), UsuarioId, Ip)).StatusCode);
        Assert.Equal(400, (await service.CrearPlanAsync(ValidoPlan(finAnterior: true), UsuarioId, Ip)).StatusCode);
        Assert.Equal(400, (await service.CrearPlanAsync(ValidoPlan(estado: ""), UsuarioId, Ip)).StatusCode);
        Assert.Equal(400, (await service.CrearPlanAsync(ValidoPlan(estado: new string('E', 31)), UsuarioId, Ip)).StatusCode);
    }

    [Fact]
    public async Task Mitigacion_Plan_CubreExitoNotFoundYCatch()
    {
        var repo = new MitigacionRepoFake { UpdatePlanResult = true };
        var service = new MatricesRiesgosMitigacionService(repo);

        Assert.True((await service.CrearPlanAsync(ValidoPlan(), UsuarioId, Ip)).Success);
        Assert.Equal(400, (await service.ActualizarPlanAsync(0, ValidoPlan(), UsuarioId, Ip)).StatusCode);
        Assert.True((await service.ActualizarPlanAsync(1, ValidoPlan(), UsuarioId, Ip)).Success);
        repo.UpdatePlanResult = false;
        Assert.Equal(404, (await service.ActualizarPlanAsync(1, ValidoPlan(), UsuarioId, Ip)).StatusCode);
        repo.ThrowInvalidOperation = true;
        Assert.Equal(400, (await service.CrearPlanAsync(ValidoPlan(), UsuarioId, Ip)).StatusCode);
        Assert.Equal(400, (await service.ActualizarPlanAsync(1, ValidoPlan(), UsuarioId, Ip)).StatusCode);
    }

    [Fact]
    public async Task Mitigacion_Actividad_RechazaTodasLasInvariantes()
    {
        var service = new MatricesRiesgosMitigacionService(new MitigacionRepoFake());

        Assert.Equal(400, (await service.CrearActividadAsync(ValidoActividad(planId: 0), UsuarioId, Ip)).StatusCode);
        Assert.Equal(400, (await service.CrearActividadAsync(ValidoActividad(descripcion: ""), UsuarioId, Ip)).StatusCode);
        Assert.Equal(400, (await service.CrearActividadAsync(ValidoActividad(descripcion: new string('D', 501)), UsuarioId, Ip)).StatusCode);
        Assert.Equal(400, (await service.CrearActividadAsync(ValidoActividad(responsable: ""), UsuarioId, Ip)).StatusCode);
        Assert.Equal(400, (await service.CrearActividadAsync(ValidoActividad(responsable: new string('R', 151)), UsuarioId, Ip)).StatusCode);
        Assert.Equal(400, (await service.CrearActividadAsync(ValidoActividad(avance: -1), UsuarioId, Ip)).StatusCode);
        Assert.Equal(400, (await service.CrearActividadAsync(ValidoActividad(avance: 101), UsuarioId, Ip)).StatusCode);
        Assert.Equal(400, (await service.CrearActividadAsync(ValidoActividad(finAnterior: true), UsuarioId, Ip)).StatusCode);
        Assert.Equal(400, (await service.CrearActividadAsync(ValidoActividad(estado: ""), UsuarioId, Ip)).StatusCode);
        Assert.Equal(400, (await service.CrearActividadAsync(ValidoActividad(estado: new string('E', 31)), UsuarioId, Ip)).StatusCode);
    }

    [Fact]
    public async Task Mitigacion_Actividad_CubreExitoNotFoundYCatch()
    {
        var repo = new MitigacionRepoFake { UpdateActividadResult = true };
        var service = new MatricesRiesgosMitigacionService(repo);

        Assert.True((await service.CrearActividadAsync(ValidoActividad(), UsuarioId, Ip)).Success);
        Assert.Equal(400, (await service.ActualizarActividadAsync(0, ValidoActividad(), UsuarioId, Ip)).StatusCode);
        Assert.True((await service.ActualizarActividadAsync(1, ValidoActividad(), UsuarioId, Ip)).Success);
        repo.UpdateActividadResult = false;
        Assert.Equal(404, (await service.ActualizarActividadAsync(1, ValidoActividad(), UsuarioId, Ip)).StatusCode);
        repo.ThrowInvalidOperation = true;
        Assert.Equal(400, (await service.CrearActividadAsync(ValidoActividad(), UsuarioId, Ip)).StatusCode);
        Assert.Equal(400, (await service.ActualizarActividadAsync(1, ValidoActividad(), UsuarioId, Ip)).StatusCode);
    }

    [Fact]
    public async Task Monitoreo_ListadosYCambioEstado_CubrenValidacionesYResultados()
    {
        var repo = new MonitoreoRepoFake { UpdateAlertaResult = true };
        var service = new MatricesRiesgosMonitoreoService(repo);

        Assert.Equal(400, (await service.ListarAlertasAsync(0)).StatusCode);
        Assert.True((await service.ListarAlertasAsync(1)).Success);
        Assert.Equal(400, (await service.CambiarEstadoAlertaAsync(0, new SenalAlertaEstadoDto { AleEstado = "ACTIVO" }, UsuarioId, Ip)).StatusCode);
        Assert.Equal(400, (await service.CambiarEstadoAlertaAsync(1, new SenalAlertaEstadoDto { AleEstado = "OTRO" }, UsuarioId, Ip)).StatusCode);
        Assert.True((await service.CambiarEstadoAlertaAsync(1, new SenalAlertaEstadoDto { AleEstado = " activo " }, UsuarioId, Ip)).Success);
        repo.UpdateAlertaResult = false;
        Assert.Equal(404, (await service.CambiarEstadoAlertaAsync(1, new SenalAlertaEstadoDto { AleEstado = "INACTIVO" }, UsuarioId, Ip)).StatusCode);
        Assert.Equal(400, (await service.ListarAutomonitoreoAsync(0)).StatusCode);
        Assert.True((await service.ListarAutomonitoreoAsync(1)).Success);
        Assert.True((await service.ObtenerResumenOperativoAsync()).Success);
    }

    [Fact]
    public async Task Monitoreo_Alerta_RechazaCadaContratoInvalido()
    {
        var service = new MatricesRiesgosMonitoreoService(new MonitoreoRepoFake());

        Assert.Equal(400, (await service.CrearAlertaAsync(ValidaAlerta(evaluacionId: 0), UsuarioId, Ip)).StatusCode);
        Assert.Equal(400, (await service.CrearAlertaAsync(ValidaAlerta(codigo: ""), UsuarioId, Ip)).StatusCode);
        Assert.Equal(400, (await service.CrearAlertaAsync(ValidaAlerta(codigo: new string('A', 51)), UsuarioId, Ip)).StatusCode);
        Assert.Equal(400, (await service.CrearAlertaAsync(ValidaAlerta(indicador: ""), UsuarioId, Ip)).StatusCode);
        Assert.Equal(400, (await service.CrearAlertaAsync(ValidaAlerta(indicador: new string('I', 151)), UsuarioId, Ip)).StatusCode);
        Assert.Equal(400, (await service.CrearAlertaAsync(ValidaAlerta(estado: "DESCONOCIDO"), UsuarioId, Ip)).StatusCode);
    }

    [Fact]
    public async Task Monitoreo_Alerta_CubreExitoYCatch()
    {
        var repo = new MonitoreoRepoFake { CreateAlertaId = 44 };
        var service = new MatricesRiesgosMonitoreoService(repo);

        var creado = await service.CrearAlertaAsync(ValidaAlerta(), UsuarioId, Ip);
        repo.ThrowInvalidOperation = true;
        var error = await service.CrearAlertaAsync(ValidaAlerta(), UsuarioId, Ip);

        Assert.True(creado.Success);
        Assert.Equal(44, creado.Data);
        Assert.Equal(400, error.StatusCode);
        Assert.Equal("Fallo controlado", error.Message);
    }

    [Fact]
    public async Task Monitoreo_Automonitoreo_RechazaCadaContratoInvalidoYCubreExitoYCatch()
    {
        var repo = new MonitoreoRepoFake { CreateAutomonitoreoId = 55 };
        var service = new MatricesRiesgosMonitoreoService(repo);

        Assert.Equal(400, (await service.RegistrarAutomonitoreoAsync(ValidoAutomonitoreo(evaluacionId: 0), UsuarioId, Ip)).StatusCode);
        Assert.Equal(400, (await service.RegistrarAutomonitoreoAsync(ValidoAutomonitoreo(estadoRiesgo: ""), UsuarioId, Ip)).StatusCode);
        Assert.Equal(400, (await service.RegistrarAutomonitoreoAsync(ValidoAutomonitoreo(estadoRiesgo: new string('R', 31)), UsuarioId, Ip)).StatusCode);
        Assert.Equal(400, (await service.RegistrarAutomonitoreoAsync(ValidoAutomonitoreo(estadoControl: ""), UsuarioId, Ip)).StatusCode);
        Assert.Equal(400, (await service.RegistrarAutomonitoreoAsync(ValidoAutomonitoreo(estadoControl: new string('C', 31)), UsuarioId, Ip)).StatusCode);
        Assert.Equal(400, (await service.RegistrarAutomonitoreoAsync(ValidoAutomonitoreo(resultado: ""), UsuarioId, Ip)).StatusCode);
        Assert.Equal(400, (await service.RegistrarAutomonitoreoAsync(ValidoAutomonitoreo(resultado: new string('X', 1001)), UsuarioId, Ip)).StatusCode);

        var creado = await service.RegistrarAutomonitoreoAsync(ValidoAutomonitoreo(), UsuarioId, Ip);
        Assert.True(creado.Success);
        Assert.Equal(55, creado.Data);

        repo.ThrowInvalidOperation = true;
        Assert.Equal(400, (await service.RegistrarAutomonitoreoAsync(ValidoAutomonitoreo(), UsuarioId, Ip)).StatusCode);
    }

    private static RiesgoGuardarDto ValidoRiesgo(string codigo = "R-001", string nombre = "Riesgo institucional", string? descripcion = "Descripción") => new()
    {
        RieCodigo = codigo,
        RieNombre = nombre,
        RieDescripcion = descripcion,
        RieActivo = true
    };

    private static ControlRiesgoGuardarDto ValidoControl(long evaluacionId = 1, string tipo = "PREVENTIVO", string descripcion = "Control", string automatizacion = "MANUAL", string estado = "ACTIVO") => new()
    {
        ConEvaluacionId = evaluacionId,
        ConTipo = tipo,
        ConDescripcion = descripcion,
        ConAutomatizacion = automatizacion,
        ConEstado = estado
    };

    private static EvaluacionControlGuardarDto ValidoEvaluacionControl(decimal efectividad = 80, string? comentario = "Correcto") => new()
    {
        EcoEfectividad = efectividad,
        EcoComentario = comentario
    };

    private static PlanMitigacionGuardarDto ValidoPlan(long evaluacionId = 1, string descripcion = "Plan", decimal avance = 10, decimal presupuesto = 100, bool finAnterior = false, string estado = "ABIERTO")
    {
        DateTime inicio = new(2026, 8, 7);
        return new PlanMitigacionGuardarDto
        {
            PlaEvaluacionId = evaluacionId,
            PlaDescripcion = descripcion,
            PlaAvance = avance,
            PlaPresupuesto = presupuesto,
            PlaFechaInicio = inicio,
            PlaFechaFin = finAnterior ? inicio.AddDays(-1) : inicio.AddDays(30),
            PlaEstado = estado
        };
    }

    private static ActividadPlanGuardarDto ValidoActividad(long planId = 1, string descripcion = "Actividad", string responsable = "Responsable", decimal avance = 10, bool finAnterior = false, string estado = "PENDIENTE")
    {
        DateTime inicio = new(2026, 8, 7);
        return new ActividadPlanGuardarDto
        {
            ActPlanId = planId,
            ActDescripcion = descripcion,
            ActResponsable = responsable,
            ActAvance = avance,
            ActFechaInicio = inicio,
            ActFechaFin = finAnterior ? inicio.AddDays(-1) : inicio.AddDays(15),
            ActEstado = estado
        };
    }

    private static SenalAlertaGuardarDto ValidaAlerta(long evaluacionId = 1, string codigo = "ALE-001", string indicador = "Indicador", string estado = "ACTIVO") => new()
    {
        AleEvaluacionId = evaluacionId,
        AleCodigo = codigo,
        AleIndicador = indicador,
        AleEstado = estado
    };

    private static AutomonitoreoGuardarDto ValidoAutomonitoreo(long evaluacionId = 1, string estadoRiesgo = "ALTO", string estadoControl = "EN_SEGUIMIENTO", string resultado = "Sin novedades") => new()
    {
        MonEvaluacionId = evaluacionId,
        MonEstadoRiesgo = estadoRiesgo,
        MonEstadoContr = estadoControl,
        MonResultado = resultado
    };

    private sealed class GestionRepoFake : IMatricesRiesgosGestionRepository
    {
        public IReadOnlyList<RiesgoDto> Riesgos { get; set; } = Array.Empty<RiesgoDto>();
        public RiesgoDto? Riesgo { get; set; }
        public long CreateId { get; set; } = 1;
        public bool UpdateResult { get; set; } = true;
        public bool ThrowInvalidOperation { get; set; }

        public Task<IReadOnlyList<RiesgoDto>> ListarRiesgosAsync(bool incluirInactivos) => Task.FromResult(Riesgos);
        public Task<RiesgoDto?> ObtenerRiesgoAsync(long riesgoId) => Task.FromResult(Riesgo);
        public Task<long> CrearRiesgoAsync(RiesgoGuardarDto dto, long usuarioId, string? ip) =>
            ThrowInvalidOperation ? Task.FromException<long>(new InvalidOperationException("Fallo controlado")) : Task.FromResult(CreateId);
        public Task<bool> ActualizarRiesgoAsync(long riesgoId, RiesgoGuardarDto dto, long usuarioId, string? ip) =>
            ThrowInvalidOperation ? Task.FromException<bool>(new InvalidOperationException("Fallo controlado")) : Task.FromResult(UpdateResult);
    }

    private sealed class MitigacionRepoFake : IMatricesRiesgosMitigacionRepository
    {
        public bool ThrowInvalidOperation { get; set; }
        public bool UpdateControlResult { get; set; } = true;
        public bool UpdatePlanResult { get; set; } = true;
        public bool UpdateActividadResult { get; set; } = true;

        public Task<IReadOnlyList<ControlRiesgoDto>> ListarControlesAsync(long evaluacionId) => Task.FromResult<IReadOnlyList<ControlRiesgoDto>>(Array.Empty<ControlRiesgoDto>());
        public Task<long> CrearControlAsync(ControlRiesgoGuardarDto dto, long usuarioId, string? ip) => LongResult(10);
        public Task<bool> ActualizarControlAsync(long controlId, ControlRiesgoGuardarDto dto, long usuarioId, string? ip) => BoolResult(UpdateControlResult);
        public Task<IReadOnlyList<EvaluacionControlDto>> ListarEvaluacionesControlAsync(long controlId) => Task.FromResult<IReadOnlyList<EvaluacionControlDto>>(Array.Empty<EvaluacionControlDto>());
        public Task<long> RegistrarEvaluacionControlAsync(long controlId, EvaluacionControlGuardarDto dto, long usuarioId, string? ip) => LongResult(11);
        public Task<IReadOnlyList<PlanMitigacionDto>> ListarPlanesAsync(long evaluacionId) => Task.FromResult<IReadOnlyList<PlanMitigacionDto>>(Array.Empty<PlanMitigacionDto>());
        public Task<long> CrearPlanAsync(PlanMitigacionGuardarDto dto, long usuarioId, string? ip) => LongResult(12);
        public Task<bool> ActualizarPlanAsync(long planId, PlanMitigacionGuardarDto dto, long usuarioId, string? ip) => BoolResult(UpdatePlanResult);
        public Task<IReadOnlyList<ActividadPlanDto>> ListarActividadesAsync(long planId) => Task.FromResult<IReadOnlyList<ActividadPlanDto>>(Array.Empty<ActividadPlanDto>());
        public Task<long> CrearActividadAsync(ActividadPlanGuardarDto dto, long usuarioId, string? ip) => LongResult(13);
        public Task<bool> ActualizarActividadAsync(long actividadId, ActividadPlanGuardarDto dto, long usuarioId, string? ip) => BoolResult(UpdateActividadResult);

        private Task<long> LongResult(long value) => ThrowInvalidOperation
            ? Task.FromException<long>(new InvalidOperationException("Fallo controlado"))
            : Task.FromResult(value);
        private Task<bool> BoolResult(bool value) => ThrowInvalidOperation
            ? Task.FromException<bool>(new InvalidOperationException("Fallo controlado"))
            : Task.FromResult(value);
    }

    private sealed class MonitoreoRepoFake : IMatricesRiesgosMonitoreoRepository
    {
        public bool ThrowInvalidOperation { get; set; }
        public bool UpdateAlertaResult { get; set; } = true;
        public long CreateAlertaId { get; set; } = 1;
        public long CreateAutomonitoreoId { get; set; } = 2;

        public Task<IReadOnlyList<SenalAlertaDto>> ListarAlertasAsync(long evaluacionId) => Task.FromResult<IReadOnlyList<SenalAlertaDto>>(Array.Empty<SenalAlertaDto>());
        public Task<long> CrearAlertaAsync(SenalAlertaGuardarDto dto, long usuarioId, string? ip) => LongResult(CreateAlertaId);
        public Task<bool> CambiarEstadoAlertaAsync(long alertaId, string estado, long usuarioId, string? ip) => Task.FromResult(UpdateAlertaResult);
        public Task<IReadOnlyList<AutomonitoreoDto>> ListarAutomonitoreoAsync(long evaluacionId) => Task.FromResult<IReadOnlyList<AutomonitoreoDto>>(Array.Empty<AutomonitoreoDto>());
        public Task<long> RegistrarAutomonitoreoAsync(AutomonitoreoGuardarDto dto, long usuarioId, string? ip) => LongResult(CreateAutomonitoreoId);
        public Task<ResumenMatricesOperativoDto> ObtenerResumenOperativoAsync() => Task.FromResult(new ResumenMatricesOperativoDto());

        private Task<long> LongResult(long value) => ThrowInvalidOperation
            ? Task.FromException<long>(new InvalidOperationException("Fallo controlado"))
            : Task.FromResult(value);
    }
}
