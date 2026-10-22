using OficinaMecanicaFase1.Domain.OrdensServico;

namespace OficinaMecanicaFase1.Application.OrdensServico;

public sealed class GerenciadorExecucao(IRepositorioExecucao repositorio, TimeProvider tempo)
{
    public Task<ResultadoOrdemServico<OrdemServico>> IniciarExecucaoAsync(long numero, Guid mecanicoId,
        CancellationToken cancelamento) => repositorio.IniciarExecucaoAsync(
            numero, mecanicoId, tempo.GetUtcNow(), cancelamento);

    public Task<ResultadoOrdemServico<OrdemServico>> IniciarServicoAsync(long numero, Guid servicoId,
        Guid mecanicoId, CancellationToken cancelamento) => repositorio.IniciarServicoAsync(
            numero, servicoId, mecanicoId, tempo.GetUtcNow(), cancelamento);

    public Task<ResultadoOrdemServico<OrdemServico>> ConcluirServicoAsync(long numero, Guid servicoId,
        Guid mecanicoId, CancellationToken cancelamento) => repositorio.ConcluirServicoAsync(
            numero, servicoId, mecanicoId, tempo.GetUtcNow(), cancelamento);

    public Task<ResultadoOrdemServico<OrdemServico>> RegistrarConsumoAsync(long numero, Guid mecanicoId,
        IReadOnlyCollection<ConsumoSolicitado> consumos, CancellationToken cancelamento) =>
        repositorio.RegistrarConsumoAsync(numero, mecanicoId, consumos, tempo.GetUtcNow(), cancelamento);
}
