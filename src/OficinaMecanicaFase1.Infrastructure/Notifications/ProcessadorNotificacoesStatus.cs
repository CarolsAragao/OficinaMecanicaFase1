using Microsoft.Extensions.Logging;
using OficinaMecanicaFase1.Application.OrdensServico;

namespace OficinaMecanicaFase1.Infrastructure.Notifications;

public sealed class ProcessadorNotificacoesStatus(
    INotificacaoStatusRepository repositorio,
    INotificadorStatusOrdemServico notificador,
    TimeProvider tempo,
    NotificacoesStatusConfiguracao configuracao,
    ILogger<ProcessadorNotificacoesStatus> logger)
{
    public async Task ProcessarAsync(CancellationToken cancelamento)
    {
        var agora = tempo.GetUtcNow();
        var notificacoes = await repositorio.ListarParaProcessamentoAsync(
            agora, configuracao.QuantidadeMaxima, cancelamento);

        foreach (var notificacao in notificacoes)
        {
            var assumida = await repositorio.TentarAssumirAsync(notificacao.Id, agora,
                agora.AddSeconds(configuracao.ProcessamentoExpiraEmSegundos), cancelamento);
            if (!assumida) continue;

            var mensagem = new MensagemStatusOrdemServico(
                notificacao.MarcoHistoricoId, notificacao.Destinatario, notificacao.NumeroOrdemServico,
                notificacao.Status, notificacao.Assunto, notificacao.Corpo);
            try
            {
                await notificador.EnviarAsync(mensagem, cancelamento);
                await repositorio.MarcarEnviadaAsync(notificacao.Id, tempo.GetUtcNow(), cancelamento);
            }
            catch (OperationCanceledException) when (cancelamento.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception erro)
            {
                await repositorio.ReagendarAsync(notificacao.Id,
                    tempo.GetUtcNow().AddSeconds(configuracao.AtrasoTentativaSegundos), erro.Message, cancelamento);
                logger.LogWarning(erro,
                    "Falha ao enviar aviso de status da OS {Numero}. Marco {MarcoId}; nova tentativa será agendada.",
                    notificacao.NumeroOrdemServico, notificacao.MarcoHistoricoId);
            }
        }
    }
}
