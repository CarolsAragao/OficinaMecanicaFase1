using System.Net.Mail;
using MailKit.Security;
using Microsoft.Extensions.Configuration;

namespace OficinaMecanicaFase1.Infrastructure.Notifications;

internal sealed class SmtpConfiguracao
{
    public bool Enabled { get; set; }
    public string? Host { get; set; }
    public int Port { get; set; } = 587;
    public SecureSocketOptions Security { get; set; } = SecureSocketOptions.StartTls;
    public string? FromEmail { get; set; }
    public string FromName { get; set; } = "Oficina Mecânica";
    public string? Username { get; set; }
    public string? Password { get; set; }

    public static SmtpConfiguracao Ler(IConfiguration configuration)
    {
        var smtp = configuration.GetSection("Smtp").Get<SmtpConfiguracao>() ?? new SmtpConfiguracao();
        if (!smtp.Enabled) return smtp;

        if (string.IsNullOrWhiteSpace(smtp.Host) || smtp.Port is < 1 or > 65535)
            throw new InvalidOperationException("Configure Smtp:Host e Smtp:Port para enviar avisos de status.");
        if (string.IsNullOrWhiteSpace(smtp.FromEmail) ||
            !MailAddress.TryCreate(smtp.FromEmail, out var remetente) ||
            !string.Equals(remetente.Address, smtp.FromEmail, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Configure Smtp:FromEmail com um endereço válido.");
        if (string.IsNullOrWhiteSpace(smtp.FromName))
            throw new InvalidOperationException("Configure Smtp:FromName.");
        if (string.IsNullOrWhiteSpace(smtp.Username) != string.IsNullOrWhiteSpace(smtp.Password))
            throw new InvalidOperationException("Configure Smtp:Username e Smtp:Password juntos.");
        if (!string.IsNullOrWhiteSpace(smtp.Username) &&
            smtp.Security is not (SecureSocketOptions.StartTls or SecureSocketOptions.SslOnConnect))
            throw new InvalidOperationException("Credenciais SMTP exigem StartTls ou SslOnConnect.");

        return smtp;
    }
}
