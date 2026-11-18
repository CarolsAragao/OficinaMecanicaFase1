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
            agora = tempo.GetUtcNow();
            var processamentoId = Guid.NewGuid();
            var processandoAte = agora.AddSeconds(configuracao.ProcessamentoExpiraEmSegundos);
            var assumida = await repositorio.TentarAssumirAsync(notificacao.Id, processamentoId, agora,
                processandoAte, cancelamento);
            if (!assumida) continue;

            var mensagem = new MensagemStatusOrdemServico(
                notificacao.MarcoHistoricoId, notificacao.Destinatario, notificacao.NumeroOrdemServico,
                notificacao.Status, notificacao.Assunto, notificacao.Corpo);
            try
            {
                var restante = processandoAte - tempo.GetUtcNow();
                if (restante <= TimeSpan.Zero)
                    throw new TimeoutException("A reserva do aviso expirou antes do envio.");
                using var prazoEnvio = CancellationTokenSource.CreateLinkedTokenSource(cancelamento);
                prazoEnvio.CancelAfter(restante);
                await notificador.EnviarAsync(mensagem, prazoEnvio.Token);
                var enviada = await repositorio.MarcarEnviadaAsync(
                    notificacao.Id, processamentoId, tempo.GetUtcNow(), cancelamento);
                if (!enviada)
                    logger.LogWarning("A tentativa perdeu a reserva do aviso {NotificacaoId}; não alterou o registro.",
                        notificacao.Id);
            }
            catch (OperationCanceledException) when (cancelamento.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception erro)
            {
                await repositorio.ReagendarAsync(notificacao.Id, processamentoId,
                    tempo.GetUtcNow().AddSeconds(configuracao.AtrasoTentativaSegundos), erro.Message, cancelamento);
                logger.LogWarning(erro,
                    "Falha ao enviar aviso de status da OS {Numero}. Marco {MarcoId}; nova tentativa será agendada.",
                    notificacao.NumeroOrdemServico, notificacao.MarcoHistoricoId);
            }
        }
    }
}
