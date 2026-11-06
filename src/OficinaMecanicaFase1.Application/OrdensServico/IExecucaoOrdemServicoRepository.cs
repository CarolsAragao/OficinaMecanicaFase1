using OficinaMecanicaFase1.Domain.Estoque;
using OficinaMecanicaFase1.Domain.OrdensServico;
using OficinaMecanicaFase1.Domain.Orcamentos;

namespace OficinaMecanicaFase1.Application.OrdensServico;

public sealed record ConsumoSolicitado(Guid ItemEstoqueId, decimal Quantidade);

public interface IExecucaoOrdemServicoRepository
{
    Task<ResultadoOrdemServico<OrdemServico>> ExecutarAsync(long numero,
        Func<IOperacoesExecucao, OrdemServico, Orcamento?, Task<ResultadoOrdemServico<OrdemServico>>> acao,
        CancellationToken cancelamento);
}

public interface IOperacoesExecucao
{
    Task<Aditivo[]> ObterAditivosAsync(Guid ordemId, CancellationToken cancelamento);
    Task<ReservaItemOrdemServico[]> ObterReservasAtivasAsync(Guid ordemId, CancellationToken cancelamento);
    Task<Dictionary<Guid, ItemEstoque>> BloquearItensAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancelamento);
    void Adicionar(RegistroHistoricoOrdemServico marco);
    void Adicionar(ConsumoItemOrdemServico consumo);
}
