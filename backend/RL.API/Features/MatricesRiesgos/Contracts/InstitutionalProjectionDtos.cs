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
    IReadOnlyList<InstitutionalProjectionFieldDto> Fields);
