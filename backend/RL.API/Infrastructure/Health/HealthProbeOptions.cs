namespace RL.API.Infrastructure.Health;

/// <summary>
/// Configuración operativa de los probes de salud. Los límites se acotan para evitar
/// que una dependencia no disponible bloquee durante demasiado tiempo el endpoint de readiness.
/// </summary>
public sealed class HealthProbeOptions
{
    public const int DefaultOracleTimeoutSeconds = 3;
    public const int MinOracleTimeoutSeconds = 1;
    public const int MaxOracleTimeoutSeconds = 10;

    public int OracleTimeoutSeconds { get; set; } = DefaultOracleTimeoutSeconds;

    public int GetEffectiveOracleTimeoutSeconds() =>
        Math.Clamp(OracleTimeoutSeconds, MinOracleTimeoutSeconds, MaxOracleTimeoutSeconds);
}
