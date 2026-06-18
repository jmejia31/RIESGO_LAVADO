using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using RL.API.Models;

namespace RL.API.Services;

public interface IEmailService
{
    Task EnviarCorreoAsync(string destinatario, string asunto, string cuerpo, bool esHtml = true);
}

public class EmailService : IEmailService
{
    private readonly SmtpSettings _smtpSettings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<SmtpSettings> smtpSettings, ILogger<EmailService> logger)
    {
        _smtpSettings = smtpSettings.Value;
        _logger = logger;
    }

    public async Task EnviarCorreoAsync(string destinatario, string asunto, string cuerpo, bool esHtml = true)
    {
        var mensaje = new MimeMessage();
        
        if (MailboxAddress.TryParse(_smtpSettings.From, out var fromAddress))
        {
            mensaje.From.Add(fromAddress);
        }
        else
        {
            mensaje.From.Add(new MailboxAddress("Sistema de Mensajeria", _smtpSettings.User));
        }

        mensaje.To.Add(MailboxAddress.Parse(destinatario));
        mensaje.Subject = asunto;

        var bodyBuilder = new BodyBuilder();
        if (esHtml)
        {
            bodyBuilder.HtmlBody = cuerpo;
        }
        else
        {
            bodyBuilder.TextBody = cuerpo;
        }
        mensaje.Body = bodyBuilder.ToMessageBody();

        using var cliente = new SmtpClient();
        try
        {
            var socketOptions = _smtpSettings.Secure 
                ? (_smtpSettings.Port == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls) 
                : SecureSocketOptions.None;

            await cliente.ConnectAsync(_smtpSettings.Host, _smtpSettings.Port, socketOptions);
            
            if (!string.IsNullOrEmpty(_smtpSettings.User) && !string.IsNullOrEmpty(_smtpSettings.Password))
            {
                await cliente.AuthenticateAsync(_smtpSettings.User, _smtpSettings.Password);
            }

            await cliente.SendAsync(mensaje);
            _logger.LogInformation("Correo enviado exitosamente a {Destinatario}", destinatario);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enviando correo a {Destinatario}", destinatario);
            throw;
        }
        finally
        {
            await cliente.DisconnectAsync(true);
        }
    }
}
