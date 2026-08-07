namespace RL.API.Core.Security;

/// <summary>
/// Nombres canónicos de roles institucionales definidos en RL_ROLES.
/// Centraliza los valores usados por ASP.NET Core para evitar aliases o roles
/// inexistentes que provoquen denegaciones incorrectas.
/// </summary>
public static class SystemRoles
{
    public const string Administrador = "ADMINISTRADOR";
    public const string Supervisor = "SUPERVISOR";
    public const string Analista = "ANALISTA";
}
