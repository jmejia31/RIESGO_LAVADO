namespace RL.API.Features.Identidad.Integrations.ActiveDirectory;

public class ResultadoValidacionAdDto
{
    public bool Existe { get; set; }
    public bool Bloqueado { get; set; }
    public bool Activo { get; set; }
    public string? NombreCompleto { get; set; }
    public string? Mensaje { get; set; }
}

public class ConfigDominio
{
    public string Servidor { get; set; } = string.Empty;
    public string Container { get; set; } = string.Empty;
    public string? Usuario { get; set; }
    public string? Password { get; set; }
}
