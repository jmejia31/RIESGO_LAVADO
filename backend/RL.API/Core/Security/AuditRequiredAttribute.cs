namespace RL.API.Core.Security;

/// <summary>
/// Marca operaciones cuya auditoría funcional es obligatoria.
/// El registro se realiza explícitamente dentro del servicio o repositorio para
/// conservarlo en la misma transacción que el cambio de negocio.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class AuditRequiredAttribute : Attribute
{
    public AuditRequiredAttribute(string motivo)
    {
        Motivo = motivo;
    }

    public string Motivo { get; }
}
