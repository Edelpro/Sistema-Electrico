using System.Net;
using System.Net.Mail;

namespace SistemaElectrico.Api.Services;

public sealed class SmtpOptions
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = "SGSE";
}

public interface IEmailService
{
    Task<bool> EnviarRecuperacionAsync(string destinatario, string nombre, string enlace, CancellationToken cancellationToken);
    Task<bool> EnviarInvitacionAsync(string destinatario, string nombre, string enlace, CancellationToken cancellationToken);
}

public sealed class SmtpEmailService(IConfiguration configuration, ILogger<SmtpEmailService> logger)
    : IEmailService
{
    public async Task<bool> EnviarRecuperacionAsync(
        string destinatario,
        string nombre,
        string enlace,
        CancellationToken cancellationToken)
    {
        var opciones = configuration.GetSection("Smtp").Get<SmtpOptions>() ?? new SmtpOptions();
        if (string.IsNullOrWhiteSpace(opciones.Host) || string.IsNullOrWhiteSpace(opciones.Username) ||
            string.IsNullOrWhiteSpace(opciones.Password) || string.IsNullOrWhiteSpace(opciones.FromAddress))
        {
            logger.LogWarning("Correo de recuperación omitido: SMTP no está configurado.");
            return false;
        }

        using var mensaje = new MailMessage
        {
            From = new MailAddress(opciones.FromAddress, opciones.FromName),
            Subject = "Restablece tu contraseña de SGSE",
            Body = $"""
                Hola {WebUtility.HtmlEncode(nombre)},

                Recibimos una solicitud para restablecer tu contraseña. Usa este enlace, válido durante 30 minutos:

                {enlace}

                Si no realizaste la solicitud, ignora este mensaje. Tu contraseña actual seguirá funcionando.
                """,
            IsBodyHtml = false
        };
        mensaje.To.Add(destinatario);

        using var cliente = new SmtpClient(opciones.Host, opciones.Port)
        {
            EnableSsl = opciones.EnableSsl,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(opciones.Username, opciones.Password)
        };

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            await cliente.SendMailAsync(mensaje, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "No se pudo enviar el correo de recuperación.");
            return false;
        }
    }

    public Task<bool> EnviarInvitacionAsync(
        string destinatario,
        string nombre,
        string enlace,
        CancellationToken cancellationToken)
    {
        return EnviarAsync(
            destinatario,
            "Tu cuenta de SGSE está activa",
            $"""
                Hola {WebUtility.HtmlEncode(nombre)},

                Tu cuenta de acceso a SGSE está activa. Usa este enlace, válido durante 30 minutos, para crear tu contraseña privada:

                {enlace}

                Por seguridad, SGSE nunca envía contraseñas por correo. Si no esperabas esta invitación, comunícate con el administrador.
                """,
            "invitación de acceso",
            cancellationToken);
    }

    private async Task<bool> EnviarAsync(
        string destinatario,
        string asunto,
        string cuerpo,
        string tipo,
        CancellationToken cancellationToken)
    {
        var opciones = configuration.GetSection("Smtp").Get<SmtpOptions>() ?? new SmtpOptions();
        if (string.IsNullOrWhiteSpace(opciones.Host) || string.IsNullOrWhiteSpace(opciones.Username) ||
            string.IsNullOrWhiteSpace(opciones.Password) || string.IsNullOrWhiteSpace(opciones.FromAddress))
        {
            logger.LogWarning("Correo de {Tipo} omitido: SMTP no está configurado.", tipo);
            return false;
        }

        using var mensaje = new MailMessage
        {
            From = new MailAddress(opciones.FromAddress, opciones.FromName),
            Subject = asunto,
            Body = cuerpo,
            IsBodyHtml = false
        };
        mensaje.To.Add(destinatario);

        using var cliente = new SmtpClient(opciones.Host, opciones.Port)
        {
            EnableSsl = opciones.EnableSsl,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(opciones.Username, opciones.Password)
        };

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            await cliente.SendMailAsync(mensaje, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "No se pudo enviar el correo de {Tipo}.", tipo);
            return false;
        }
    }
}
