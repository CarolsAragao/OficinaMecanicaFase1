using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using MimeKit;
using OficinaMecanicaFase1.Application.OrdensServico;

namespace OficinaMecanicaFase1.Infrastructure.Notifications;

internal sealed class NotificadorStatusSmtp(
    SmtpConfiguracao configuracao, ILogger<NotificadorStatusSmtp> logger)
    : INotificadorStatusOrdemServico
{
    public async Task EnviarAsync(MensagemStatusOrdemServico mensagem, CancellationToken cancelamento)
    {
        if (!configuracao.Enabled)
        {
            logger.LogDebug("SMTP desabilitado; aviso da OS {Numero} não enviado. Marco {MarcoId}.",
                mensagem.NumeroOrdemServico, mensagem.MarcoHistoricoId);
            return;
        }

        var email = new MimeMessage();
        email.From.Add(new MailboxAddress(configuracao.FromName, configuracao.FromEmail!));
        email.To.Add(MailboxAddress.Parse(mensagem.EmailCliente));
        email.Subject = mensagem.Assunto;
        email.Body = new TextPart("plain") { Text = mensagem.Corpo };
        email.Headers.Add("X-Oficina-Marco-Id", mensagem.MarcoHistoricoId.ToString("D"));

        using var smtp = new SmtpClient { Timeout = 10_000 };
        await smtp.ConnectAsync(configuracao.Host!, configuracao.Port, configuracao.Security, cancelamento);
        if (!string.IsNullOrWhiteSpace(configuracao.Username))
            await smtp.AuthenticateAsync(configuracao.Username, configuracao.Password!, cancelamento);
        await smtp.SendAsync(email, cancelamento);
        logger.LogInformation("SMTP aceitou o aviso da OS {Numero}, status {Status}, marco {MarcoId}.",
            mensagem.NumeroOrdemServico, mensagem.Status, mensagem.MarcoHistoricoId);
        try { await smtp.DisconnectAsync(true, cancelamento); }
        catch (Exception erro)
        {
            // O SMTP já aceitou a mensagem; falha ao encerrar a conexão não indica falha de envio.
            logger.LogDebug(erro, "Falha ao encerrar conexão SMTP após o envio do marco {MarcoId}.",
                mensagem.MarcoHistoricoId);
        }
    }
}
