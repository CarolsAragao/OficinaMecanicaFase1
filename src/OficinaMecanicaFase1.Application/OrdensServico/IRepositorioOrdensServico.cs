using OficinaMecanicaFase1.Domain.Catalogo;
using OficinaMecanicaFase1.Domain.Estoque;
using OficinaMecanicaFase1.Domain.OrdensServico;
using OficinaMecanicaFase1.Domain.Orcamentos;

namespace OficinaMecanicaFase1.Application.OrdensServico;

public interface IRepositorioOrdensServico
{
    Task<bool> VeiculoPertenceAoClienteAsync(Guid clienteId, Guid veiculoId, CancellationToken cancelamento);
    Task<IReadOnlyDictionary<Guid, ServicoCatalogo>> ObterServicosAtivosAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancelamento);
    Task<IReadOnlyDictionary<Guid, ItemEstoque>> ObterItensAtivosAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancelamento);
    Task<IReadOnlyDictionary<Guid, ServicoCatalogo>> ObterServicosAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancelamento);
    Task<IReadOnlyDictionary<Guid, ItemEstoque>> ObterItensAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancelamento);
    Task<OrdemServico?> ObterAsync(long numero, CancellationToken cancelamento);
    Task<Orcamento?> ObterOrcamentoAsync(Guid ordemServicoId, CancellationToken cancelamento);
    void Adicionar(OrdemServico ordem);
    void Adicionar(ServicoSolicitado servico);
    void Adicionar(ItemNecessario item);
    void Adicionar(MarcoOrdemServico marco);
    void Adicionar(Orcamento orcamento);
    Task SalvarAsync(CancellationToken cancelamento);
}
