namespace RL.API.Security;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class AuditRequiredAttribute : Attribute
{
    public AuditRequiredAttribute(string motivo)
    {
        Motivo = motivo;
    }

    public string Motivo { get; }
}
