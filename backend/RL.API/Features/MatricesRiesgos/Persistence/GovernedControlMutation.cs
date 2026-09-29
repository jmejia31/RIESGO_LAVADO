using RL.API.Features.MatricesRiesgos.Contracts;

namespace RL.API.Features.MatricesRiesgos.Persistence;

/// <summary>
/// Orquesta la mutación de controles que participa en el cálculo gobernado.
/// La implementación Oracle y los dobles de prueba comparten este flujo para
/// que cada paso se confirme o revierta como una única unidad de trabajo.
/// </summary>
internal sealed class GovernedControlMutationExecutor
{
    public async Task<long> CreateAsync(
        IGovernedControlMutationStore store,
        ControlRiesgoGuardarDto control,
        int expectedEvaluationVersionRow,
        string calculatedJson,
        long usuarioId,
        string? ip)
    {
        await using IGovernedControlMutationSession session = await store.BeginAsync();
        try
        {
            await session.LockDraftEvaluationAsync(control.ConEvaluacionId, expectedEvaluationVersionRow);
            long controlId = await session.InsertControlAsync(control);
            await session.UpdateCalculationAsync(control.ConEvaluacionId, expectedEvaluationVersionRow, calculatedJson);
            await session.AuditControlAsync(controlId, "INSERT", control, usuarioId, ip);
            await session.AuditEvaluationAsync(control.ConEvaluacionId, calculatedJson, usuarioId, ip);
            await session.CommitAsync();
            return controlId;
        }
        catch
        {
            await session.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> UpdateAsync(
        IGovernedControlMutationStore store,
        long controlId,
        ControlRiesgoGuardarDto control,
        int expectedEvaluationVersionRow,
        string calculatedJson,
        long usuarioId,
        string? ip)
    {
        await using IGovernedControlMutationSession session = await store.BeginAsync();
        try
        {
            await session.LockDraftEvaluationAsync(control.ConEvaluacionId, expectedEvaluationVersionRow);
            long? parentEvaluationId = await session.LockControlParentAsync(controlId);
            if (!parentEvaluationId.HasValue)
            {
                await session.RollbackAsync();
                return false;
            }

            if (parentEvaluationId.Value != control.ConEvaluacionId)
                throw new InvalidOperationException("No se permite cambiar el padre de un control gobernado.");

            if (!await session.UpdateControlAsync(controlId, control))
            {
                await session.RollbackAsync();
                return false;
            }

            await session.UpdateCalculationAsync(control.ConEvaluacionId, expectedEvaluationVersionRow, calculatedJson);
            await session.AuditControlAsync(controlId, "UPDATE", control, usuarioId, ip);
            await session.AuditEvaluationAsync(control.ConEvaluacionId, calculatedJson, usuarioId, ip);
            await session.CommitAsync();
            return true;
        }
        catch
        {
            await session.RollbackAsync();
            throw;
        }
    }
}

internal interface IGovernedControlMutationStore
{
    Task<IGovernedControlMutationSession> BeginAsync();
}

internal interface IGovernedControlMutationSession : IAsyncDisposable
{
    Task LockDraftEvaluationAsync(long evaluationId, int expectedVersionRow);
    Task<long> InsertControlAsync(ControlRiesgoGuardarDto control);
    Task<long?> LockControlParentAsync(long controlId);
    Task<bool> UpdateControlAsync(long controlId, ControlRiesgoGuardarDto control);
    Task UpdateCalculationAsync(long evaluationId, int expectedVersionRow, string calculatedJson);
    Task AuditControlAsync(long controlId, string action, ControlRiesgoGuardarDto control, long usuarioId, string? ip);
    Task AuditEvaluationAsync(long evaluationId, string calculatedJson, long usuarioId, string? ip);
    Task CommitAsync();
    Task RollbackAsync();
}
