namespace RL.API.Features.Identidad.Integrations.Email;

public interface IEmailService
{
    Task EnviarCorreoAsync(string destinatario, string asunto, string cuerpo, bool esHtml = true);
}
