using System.Data;
using RL.API.Features.Auditoria.Persistence;
using RL.API.Features.MatricesRiesgos.Application;
using RL.API.Features.MatricesRiesgos.Contracts;
using RL.API.Features.MatricesRiesgos.Domain;
using RL.API.Features.MatricesRiesgos.Persistence;
using RL.API.Tests.Support;
using Xunit;

namespace RL.API.Tests.Features.MatricesRiesgos;

public sealed class GovernedControlMutationExecutorTests
{
    private const long EvaluationId = 44;
    private readonly GovernedControlMutationExecutor _executor = new();

    [Fact]
    public void RequiredDependencies_CannotBeNull()
    {
        IMatricesRiesgosRepository evaluations = InterfaceStub.Create<IMatricesRiesgosRepository>(out _);
        IMatricesRiesgosMitigacionRepository controls = InterfaceStub.Create<IMatricesRiesgosMitigacionRepository>(out _);
        IFormularioValidador validator = InterfaceStub.Create<IFormularioValidador>(out _);
        IMatricesRiesgoService calculator = InterfaceStub.Create<IMatricesRiesgoService>(out _);
        IAuditoriaRepository audit = InterfaceStub.Create<IAuditoriaRepository>(out _);
        VersionedCalculationRuntimeService runtime = MatricesRiesgosTestFactory.CreateLegacyRuntime();

        Assert.Throws<ArgumentNullException>(() => new MatricesRiesgosAppService(null!, validator, calculator, audit, runtime, controls));
        Assert.Throws<ArgumentNullException>(() => new MatricesRiesgosAppService(evaluations, validator, calculator, audit, null!, controls));
        Assert.Throws<ArgumentNullException>(() => new MatricesRiesgosAppService(evaluations, validator, calculator, audit, runtime, null!));
        Assert.Throws<ArgumentNullException>(() => new MatricesRiesgosMitigacionService(controls, null!, runtime));
        Assert.Throws<ArgumentNullException>(() => new MatricesRiesgosMitigacionService(controls, evaluations, null!));
    }

    [Fact]
    public async Task LegacySelection_IsBasedOnFormulaUsageAbsence()
    {
        GovernedCalculationResult result = await MatricesRiesgosTestFactory.CreateLegacyRuntime()
            .CalculateAsync(900, "{\"secciones\":[]}", "{}");

        Assert.False(result.IsGoverned);
        Assert.Null(result.Evaluation);
    }

    [Fact]
    public async Task GovernedCreate_Success_CommitsControlAndCalculation()
    {
        var store = Store.Create(row: 5, calculation: "A");

        long id = await CreateAsync(store, expectedRow: 5, calculation: "B");

        Assert.Equal(1, id);
        Assert.Single(store.Committed.Controls);
        Assert.Equal("B", store.Committed.CalculationJson);
        Assert.Equal(6, store.Committed.VersionRow);
        Assert.Equal(2, store.Committed.Audits.Count);
        Assert.Equal(1, store.CommitCount);
        Assert.Equal(0, store.RollbackCount);
    }

    [Fact]
    public Task GovernedCreate_CalculationFailure_RollsBackEverything() =>
        GovernedCreate_Failure_RollsBackEverything(MutationFault.Calculation);

    [Fact]
    public Task GovernedCreate_ControlAuditFailure_RollsBackEverything() =>
        GovernedCreate_Failure_RollsBackEverything(MutationFault.ControlAudit);

    [Fact]
    public Task GovernedCreate_EvaluationAuditFailure_RollsBackEverything() =>
        GovernedCreate_Failure_RollsBackEverything(MutationFault.EvaluationAudit);

    [Theory]
    [InlineData(MutationFault.Calculation)]
    [InlineData(MutationFault.ControlAudit)]
    [InlineData(MutationFault.EvaluationAudit)]
    public async Task GovernedCreate_Failure_RollsBackEverything(MutationFault fault)
    {
        var store = Store.Create(row: 5, calculation: "A", fault);

        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateAsync(store, 5, "B"));

        Assert.Empty(store.Committed.Controls);
        Assert.Equal("A", store.Committed.CalculationJson);
        Assert.Equal(5, store.Committed.VersionRow);
        Assert.Empty(store.Committed.Audits);
        Assert.Equal(0, store.CommitCount);
        Assert.Equal(1, store.RollbackCount);
    }

    [Fact]
    public async Task GovernedUpdate_Success_CommitsControlAndCalculation()
    {
        var store = Store.Create(row: 8, calculation: "A", control: ExistingControl("A", "MANUAL"));

        bool updated = await UpdateAsync(store, 8, "B", "AUTOMATICO", "B");

        Assert.True(updated);
        Assert.Equal("B", store.Committed.Controls.Single().ConDescripcion);
        Assert.Equal("AUTOMATICO", store.Committed.Controls.Single().ConAutomatizacion);
        Assert.Equal("B", store.Committed.CalculationJson);
        Assert.Equal(9, store.Committed.VersionRow);
        Assert.Equal(2, store.Committed.Audits.Count);
        Assert.Equal(1, store.CommitCount);
    }

    [Fact]
    public Task GovernedUpdate_CalculationFailure_RollsBackEverything() =>
        GovernedUpdate_Failure_RollsBackEverything(MutationFault.Calculation);

    [Fact]
    public Task GovernedUpdate_ControlAuditFailure_RollsBackEverything() =>
        GovernedUpdate_Failure_RollsBackEverything(MutationFault.ControlAudit);

    [Fact]
    public Task GovernedUpdate_EvaluationAuditFailure_RollsBackEverything() =>
        GovernedUpdate_Failure_RollsBackEverything(MutationFault.EvaluationAudit);

    [Theory]
    [InlineData(MutationFault.Calculation)]
    [InlineData(MutationFault.ControlAudit)]
    [InlineData(MutationFault.EvaluationAudit)]
    public async Task GovernedUpdate_Failure_RollsBackEverything(MutationFault fault)
    {
        var store = Store.Create(row: 8, calculation: "A", fault, ExistingControl("A", "MANUAL"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => UpdateAsync(store, 8, "B", "AUTOMATICO", "B"));

        ControlRiesgoDto control = store.Committed.Controls.Single();
        Assert.Equal("A", control.ConDescripcion);
        Assert.Equal("MANUAL", control.ConAutomatizacion);
        Assert.Equal("A", store.Committed.CalculationJson);
        Assert.Equal(8, store.Committed.VersionRow);
        Assert.Empty(store.Committed.Audits);
        Assert.Equal(0, store.CommitCount);
        Assert.Equal(1, store.RollbackCount);
    }

    [Fact]
    public async Task GovernedCreate_StaleVersionRow_ReturnsConflictAndPersistsNothing()
    {
        var store = Store.Create(row: 10, calculation: "A");
        await CreateAsync(store, 10, "B");

        await Assert.ThrowsAsync<DBConcurrencyException>(() => CreateAsync(store, 10, "C"));

        Assert.Single(store.Committed.Controls);
        Assert.Equal("B", store.Committed.CalculationJson);
        Assert.Equal(11, store.Committed.VersionRow);
        Assert.Equal(2, store.Committed.Audits.Count);
    }

    [Fact]
    public async Task GovernedUpdate_StaleVersionRow_ReturnsConflictAndPersistsNothing()
    {
        var store = Store.Create(row: 10, calculation: "A", control: ExistingControl("A", "MANUAL"));
        await UpdateAsync(store, 10, "B", "AUTOMATICO", "B");

        await Assert.ThrowsAsync<DBConcurrencyException>(() => UpdateAsync(store, 10, "C", "SEMIAUTOMATICO", "C"));

        Assert.Equal("B", store.Committed.Controls.Single().ConDescripcion);
        Assert.Equal("B", store.Committed.CalculationJson);
        Assert.Equal(11, store.Committed.VersionRow);
        Assert.Equal(2, store.Committed.Audits.Count);
    }

    [Fact]
    public async Task GovernedMutation_ApprovedEvaluation_IsRejected()
    {
        var store = Store.Create(row: 3, calculation: "A", state: "APROBADA", control: ExistingControl("A", "MANUAL"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateAsync(store, 3, "B"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => UpdateAsync(store, 3, "B", "AUTOMATICO", "B"));

        Assert.Equal("A", store.Committed.CalculationJson);
        Assert.Equal(3, store.Committed.VersionRow);
        Assert.Equal("A", store.Committed.Controls.Single().ConDescripcion);
    }

    [Fact]
    public async Task GovernedUpdate_Reparenting_IsRejected()
    {
        var store = Store.Create(row: 8, calculation: "A", control: ExistingControl("A", "MANUAL", evaluationId: 99));

        await Assert.ThrowsAsync<InvalidOperationException>(() => UpdateAsync(store, 8, "B", "AUTOMATICO", "B"));

        Assert.Equal(99, store.Committed.Controls.Single().ConEvaluacionId);
        Assert.Equal("A", store.Committed.CalculationJson);
    }

    [Fact]
    public async Task PrecalculatedResult_IsRejectedWhenVersionRowChanges()
    {
        var store = Store.Create(row: 12, calculation: "original");
        await CreateAsync(store, 12, "calculation-from-snapshot-12");

        await Assert.ThrowsAsync<DBConcurrencyException>(() =>
            CreateAsync(store, 12, "stale-calculation-from-snapshot-12"));

        Assert.Equal("calculation-from-snapshot-12", store.Committed.CalculationJson);
        Assert.Equal(13, store.Committed.VersionRow);
    }

    private Task<long> CreateAsync(Store store, int expectedRow, string calculation) =>
        _executor.CreateAsync(store, ValidControl(), expectedRow, calculation, 7, "127.0.0.1");

    private Task<bool> UpdateAsync(Store store, int expectedRow, string description, string automation, string calculation) =>
        _executor.UpdateAsync(store, 1, ValidControl(description, automation), expectedRow, calculation, 7, "127.0.0.1");

    private static ControlRiesgoGuardarDto ValidControl(string description = "Control", string automation = "MANUAL") => new()
    {
        ConEvaluacionId = EvaluationId, ConTipo = "PREVENTIVO", ConDescripcion = description,
        ConAutomatizacion = automation, ConEstado = "ACTIVO"
    };

    private static ControlRiesgoDto ExistingControl(string description, string automation, long evaluationId = EvaluationId) => new()
    {
        ConId = 1, ConEvaluacionId = evaluationId, ConTipo = "PREVENTIVO", ConDescripcion = description,
        ConAutomatizacion = automation, ConEstado = "ACTIVO"
    };

    public enum MutationFault { Calculation, ControlAudit, EvaluationAudit, Commit }

    private sealed class Store : IGovernedControlMutationStore
    {
        public State Committed { get; private set; } = new();
        public MutationFault? Fault { get; }
        public int CommitCount { get; private set; }
        public int RollbackCount { get; private set; }

        private Store(MutationFault? fault) => Fault = fault;

        public static Store Create(int row, string calculation, MutationFault? fault = null, ControlRiesgoDto? control = null, string state = "BORRADOR")
        {
            var store = new Store(fault);
            store.Committed = new State { VersionRow = row, CalculationJson = calculation, EvaluationState = state };
            if (control is not null) store.Committed.Controls.Add(Clone(control));
            return store;
        }

        public Task<IGovernedControlMutationSession> BeginAsync() =>
            Task.FromResult<IGovernedControlMutationSession>(new Session(this, Committed.Clone()));

        private sealed class Session(Store store, State working) : IGovernedControlMutationSession
        {
            private bool _completed;

            public Task LockDraftEvaluationAsync(long evaluationId, int expectedVersionRow)
            {
                if (evaluationId != EvaluationId) throw new KeyNotFoundException("Evaluation does not exist.");
                if (working.VersionRow != expectedVersionRow) throw new DBConcurrencyException("Stale EVA_VERSION_ROW.");
                if (!working.EvaluationState.Equals("BORRADOR", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Governed controls require BORRADOR.");
                return Task.CompletedTask;
            }

            public Task<long> InsertControlAsync(ControlRiesgoGuardarDto control)
            {
                long id = working.Controls.Count == 0 ? 1 : working.Controls.Max(item => item.ConId) + 1;
                working.Controls.Add(new ControlRiesgoDto
                {
                    ConId = id, ConEvaluacionId = control.ConEvaluacionId, ConTipo = control.ConTipo,
                    ConDescripcion = control.ConDescripcion, ConAutomatizacion = control.ConAutomatizacion, ConEstado = control.ConEstado
                });
                return Task.FromResult(id);
            }

            public Task<long?> LockControlParentAsync(long controlId) =>
                Task.FromResult(working.Controls.SingleOrDefault(control => control.ConId == controlId)?.ConEvaluacionId);

            public Task<bool> UpdateControlAsync(long controlId, ControlRiesgoGuardarDto control)
            {
                ControlRiesgoDto? existing = working.Controls.SingleOrDefault(item => item.ConId == controlId);
                if (existing is null || existing.ConEvaluacionId != control.ConEvaluacionId) return Task.FromResult(false);
                existing.ConTipo = control.ConTipo;
                existing.ConDescripcion = control.ConDescripcion;
                existing.ConAutomatizacion = control.ConAutomatizacion;
                existing.ConEstado = control.ConEstado;
                return Task.FromResult(true);
            }

            public Task UpdateCalculationAsync(long evaluationId, int expectedVersionRow, string calculatedJson)
            {
                ThrowIf(MutationFault.Calculation);
                if (working.VersionRow != expectedVersionRow) throw new DBConcurrencyException("Stale EVA_VERSION_ROW.");
                working.CalculationJson = calculatedJson;
                working.VersionRow++;
                return Task.CompletedTask;
            }

            public Task AuditControlAsync(long controlId, string action, ControlRiesgoGuardarDto control, long usuarioId, string? ip)
            {
                ThrowIf(MutationFault.ControlAudit);
                working.Audits.Add("CONTROL:" + action);
                return Task.CompletedTask;
            }

            public Task AuditEvaluationAsync(long evaluationId, string calculatedJson, long usuarioId, string? ip)
            {
                ThrowIf(MutationFault.EvaluationAudit);
                working.Audits.Add("EVALUACION:UPDATE");
                return Task.CompletedTask;
            }

            public Task CommitAsync()
            {
                ThrowIf(MutationFault.Commit);
                store.Committed = working.Clone();
                store.CommitCount++;
                _completed = true;
                return Task.CompletedTask;
            }

            public Task RollbackAsync()
            {
                if (!_completed)
                {
                    store.RollbackCount++;
                    _completed = true;
                }
                return Task.CompletedTask;
            }

            public ValueTask DisposeAsync() => ValueTask.CompletedTask;

            private void ThrowIf(MutationFault fault)
            {
                if (store.Fault == fault) throw new InvalidOperationException("Injected " + fault + " failure.");
            }
        }
    }

    private sealed class State
    {
        public int VersionRow { get; set; }
        public string CalculationJson { get; set; } = string.Empty;
        public string EvaluationState { get; set; } = "BORRADOR";
        public List<ControlRiesgoDto> Controls { get; } = [];
        public List<string> Audits { get; } = [];

        public State Clone()
        {
            var clone = new State { VersionRow = VersionRow, CalculationJson = CalculationJson, EvaluationState = EvaluationState };
            clone.Controls.AddRange(Controls.Select(control => GovernedControlMutationExecutorTests.Clone(control)));
            clone.Audits.AddRange(Audits);
            return clone;
        }
    }

    private static ControlRiesgoDto Clone(ControlRiesgoDto source) => new()
    {
        ConId = source.ConId, ConEvaluacionId = source.ConEvaluacionId, ConTipo = source.ConTipo,
        ConDescripcion = source.ConDescripcion, ConAutomatizacion = source.ConAutomatizacion, ConEstado = source.ConEstado
    };
}
