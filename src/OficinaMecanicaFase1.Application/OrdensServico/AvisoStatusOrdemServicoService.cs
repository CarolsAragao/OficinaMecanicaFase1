using Microsoft.Extensions.Logging;
using OficinaMecanicaFase1.Application.Cadastros;
using OficinaMecanicaFase1.Domain.OrdensServico;

namespace OficinaMecanicaFase1.Application.OrdensServico;

public sealed class AvisoStatusOrdemServicoService(
    ICadastroRepository cadastros, INotificadorStatusOrdemServico notificador,
    ILogger<AvisoStatusOrdemServicoService> logger)
{
    public async Task AvisarAsync(OrdemServico ordem, RegistroHistoricoOrdemServico marco)
    {
        try
        {
            var cliente = await cadastros.ObterClienteAsync(ordem.ClienteId, CancellationToken.None);
            if (string.IsNullOrWhiteSpace(cliente?.Email))
            {
                logger.LogInformation("Aviso da OS {Numero} não enviado: cliente sem e-mail. Marco {MarcoId}.",
                    ordem.Numero, marco.Id);
                return;
            }

            var mensagem = MensagemStatusOrdemServico.Criar(marco.Id, cliente.Email, ordem.Numero, ordem.Status);
            await notificador.EnviarAsync(mensagem, CancellationToken.None);
        }
        catch (Exception erro)
        {
            // A OS já foi salva; a falha do canal externo não altera o resultado da operação.
            logger.LogWarning(erro, "Falha ao avisar o status {Status} da OS {Numero}. Marco {MarcoId}.",
                ordem.Status, ordem.Numero, marco.Id);
        }
    }
}
