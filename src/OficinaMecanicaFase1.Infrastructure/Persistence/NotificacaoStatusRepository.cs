using Microsoft.EntityFrameworkCore;
using OficinaMecanicaFase1.Application.OrdensServico;
using OficinaMecanicaFase1.Domain.OrdensServico;

namespace OficinaMecanicaFase1.Infrastructure.Persistence;

public sealed class NotificacaoStatusRepository(OficinaDbContext db) : INotificacaoStatusRepository
{
    public async Task<IReadOnlyCollection<NotificacaoStatusOrdemServico>> ListarParaProcessamentoAsync(
        DateTimeOffset agora, int quantidadeMaxima, CancellationToken cancelamento) =>
        await db.NotificacoesStatusOrdensServico.AsNoTracking()
            .Where(notificacao =>
                (notificacao.Situacao == SituacaoNotificacaoStatusOrdemServico.Pendente &&
                 notificacao.ProximaTentativaEmUtc <= agora) ||
                (notificacao.Situacao == SituacaoNotificacaoStatusOrdemServico.EmProcessamento &&
                 notificacao.ProcessandoAteUtc <= agora))
            .OrderBy(notificacao => notificacao.CriadaEmUtc)
            .Take(quantidadeMaxima)
            .ToArrayAsync(cancelamento);

    public async Task<bool> TentarAssumirAsync(Guid notificacaoId, DateTimeOffset agora,
        DateTimeOffset processandoAteUtc, CancellationToken cancelamento)
    {
        var atualizadas = await db.NotificacoesStatusOrdensServico
            .Where(notificacao => notificacao.Id == notificacaoId &&
                ((notificacao.Situacao == SituacaoNotificacaoStatusOrdemServico.Pendente &&
                  notificacao.ProximaTentativaEmUtc <= agora) ||
                 (notificacao.Situacao == SituacaoNotificacaoStatusOrdemServico.EmProcessamento &&
                  notificacao.ProcessandoAteUtc <= agora)))
            .ExecuteUpdateAsync(atualiza => atualiza
                .SetProperty(notificacao => notificacao.Situacao, SituacaoNotificacaoStatusOrdemServico.EmProcessamento)
                .SetProperty(notificacao => notificacao.ProcessandoAteUtc, processandoAteUtc), cancelamento);
        return atualizadas == 1;
    }

    public Task MarcarEnviadaAsync(Guid notificacaoId, DateTimeOffset enviadaEmUtc, CancellationToken cancelamento) =>
        db.NotificacoesStatusOrdensServico
            .Where(notificacao => notificacao.Id == notificacaoId &&
                notificacao.Situacao == SituacaoNotificacaoStatusOrdemServico.EmProcessamento)
            .ExecuteUpdateAsync(atualiza => atualiza
                .SetProperty(notificacao => notificacao.Situacao, SituacaoNotificacaoStatusOrdemServico.Enviada)
                .SetProperty(notificacao => notificacao.EnviadaEmUtc, enviadaEmUtc)
                .SetProperty(notificacao => notificacao.ProcessandoAteUtc, (DateTimeOffset?)null)
                .SetProperty(notificacao => notificacao.UltimoErro, (string?)null), cancelamento);

    public Task ReagendarAsync(Guid notificacaoId, DateTimeOffset proximaTentativaEmUtc,
        string erro, CancellationToken cancelamento) =>
        db.NotificacoesStatusOrdensServico
            .Where(notificacao => notificacao.Id == notificacaoId &&
                notificacao.Situacao == SituacaoNotificacaoStatusOrdemServico.EmProcessamento)
            .ExecuteUpdateAsync(atualiza => atualiza
                .SetProperty(notificacao => notificacao.Situacao, SituacaoNotificacaoStatusOrdemServico.Pendente)
                .SetProperty(notificacao => notificacao.Tentativas, notificacao => notificacao.Tentativas + 1)
                .SetProperty(notificacao => notificacao.ProximaTentativaEmUtc, proximaTentativaEmUtc)
                .SetProperty(notificacao => notificacao.ProcessandoAteUtc, (DateTimeOffset?)null)
                .SetProperty(notificacao => notificacao.UltimoErro,
                    erro.Length <= 1000 ? erro : erro[..1000]), cancelamento);
}
