namespace RL.API.Features.MatricesRiesgos.Contracts;

public sealed record TransicionesEvaluacionDto(string EstadoActual, IReadOnlyList<string> DestinosPermitidos);
