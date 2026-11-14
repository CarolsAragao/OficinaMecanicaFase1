using OficinaMecanicaFase1.Domain.OrdensServico;

namespace OficinaMecanicaFase1.Application.OrdensServico;

public interface INotificacaoStatusRepository
{
    Task<IReadOnlyCollection<NotificacaoStatusOrdemServico>> ListarParaProcessamentoAsync(
        DateTimeOffset agora, int quantidadeMaxima, CancellationToken cancelamento);
    Task<bool> TentarAssumirAsync(Guid notificacaoId, DateTimeOffset agora,
        DateTimeOffset processandoAteUtc, CancellationToken cancelamento);
    Task MarcarEnviadaAsync(Guid notificacaoId, DateTimeOffset enviadaEmUtc, CancellationToken cancelamento);
    Task ReagendarAsync(Guid notificacaoId, DateTimeOffset proximaTentativaEmUtc,
        string erro, CancellationToken cancelamento);
}
