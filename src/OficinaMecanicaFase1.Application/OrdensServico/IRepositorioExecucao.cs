using OficinaMecanicaFase1.Domain.OrdensServico;

namespace OficinaMecanicaFase1.Application.OrdensServico;

public sealed record ConsumoSolicitado(Guid ItemEstoqueId, decimal Quantidade);

public interface IRepositorioExecucao
{
    Task<ResultadoOrdemServico<OrdemServico>> IniciarExecucaoAsync(long numero, Guid mecanicoId,
        DateTimeOffset agora, CancellationToken cancelamento);
    Task<ResultadoOrdemServico<OrdemServico>> IniciarServicoAsync(long numero, Guid servicoId,
        Guid mecanicoId, DateTimeOffset agora, CancellationToken cancelamento);
    Task<ResultadoOrdemServico<OrdemServico>> ConcluirServicoAsync(long numero, Guid servicoId,
        Guid mecanicoId, DateTimeOffset agora, CancellationToken cancelamento);
    Task<ResultadoOrdemServico<OrdemServico>> RegistrarConsumoAsync(long numero, Guid mecanicoId,
        IReadOnlyCollection<ConsumoSolicitado> consumos, DateTimeOffset agora, CancellationToken cancelamento);
}
