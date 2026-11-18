using OficinaMecanicaFase1.Domain.Catalogo;
using OficinaMecanicaFase1.Domain.Estoque;
using OficinaMecanicaFase1.Domain.OrdensServico;
using OficinaMecanicaFase1.Domain.Orcamentos;

namespace OficinaMecanicaFase1.Application.OrdensServico;

public interface IOrdemServicoRepository
{
    Task<ResultadoOrdemServico<T>> ExecutarComNotificacaoAsync<T>(
        Func<IOrdemServicoRepository, Task<ResultadoOrdemServico<T>>> operacao, CancellationToken cancelamento);
    Task<bool> VeiculoPertenceAoClienteAsync(Guid clienteId, Guid veiculoId, CancellationToken cancelamento);
    Task<IReadOnlyDictionary<Guid, ServicoCatalogo>> ObterServicosAtivosAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancelamento);
    Task<IReadOnlyDictionary<Guid, ItemEstoque>> ObterItensAtivosAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancelamento);
    Task<IReadOnlyDictionary<Guid, ServicoCatalogo>> ObterServicosAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancelamento);
    Task<IReadOnlyDictionary<Guid, ItemEstoque>> ObterItensAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancelamento);
    Task<OrdemServico?> ObterAsync(long numero, CancellationToken cancelamento);
    Task<OrdemServico?> ObterDoClienteAsync(long numero, string documento, CancellationToken cancelamento);
    Task<OrdemServico?> ObterAndamentoDoClienteAsync(long numero, string documento, CancellationToken cancelamento);
    Task<Orcamento?> ObterOrcamentoAsync(Guid ordemServicoId, CancellationToken cancelamento);
    Task<IReadOnlyCollection<ReservaItemOrdemServico>> ObterReservasAtivasAsync(Guid ordemServicoId, CancellationToken cancelamento);
    Task<IReadOnlyDictionary<Guid, ItemEstoque>> ObterItensParaReservaAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancelamento);
    void Adicionar(OrdemServico ordem);
    void Adicionar(ServicoSolicitado servico);
    void Adicionar(ItemNecessario item);
    void Adicionar(RegistroHistoricoOrdemServico marco);
    void Adicionar(Orcamento orcamento);
    void Adicionar(EmissaoCodigoAcesso emissao);
    void Adicionar(NotificacaoStatusOrdemServico notificacao);
    Task<string?> ObterEmailClienteAsync(Guid clienteId, CancellationToken cancelamento);
    Task SalvarAsync(CancellationToken cancelamento);
    Task SalvarComNotificacaoAsync(Action registrarNotificacao, CancellationToken cancelamento);
}
