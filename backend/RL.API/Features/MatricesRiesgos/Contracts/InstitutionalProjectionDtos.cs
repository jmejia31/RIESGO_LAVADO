namespace RL.API.Features.MatricesRiesgos.Contracts;

public sealed record InstitutionalProjectionFieldDto(
    int FieldNumber,
    string FieldKey,
    string FieldLabel,
    string ValueType,
    object? Value,
    string State,
    string Source);

public sealed record InstitutionalEvaluationProjectionDto(
    long EvaluationId,
    string RiskCode,
    IReadOnlyList<InstitutionalProjectionFieldDto> Fields)
{
    public long RiskId { get; init; }
}

public sealed record InstitutionalProjectionFieldDefinitionDto(
    int FieldNumber,
    string FieldKey,
    string FieldLabel,
    string ValueType,
    string Source);

public sealed record InstitutionalPlanProjectionValueDto(int PlanOrdinalIndex, object? Value, string State);
