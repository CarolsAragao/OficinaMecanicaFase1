using OficinaMecanicaFase1.Domain.Catalogo;
using OficinaMecanicaFase1.Domain.Estoque;
using OficinaMecanicaFase1.Domain.OrdensServico;

namespace OficinaMecanicaFase1.Application.OrdensServico;

public interface IRepositorioOrdensServico
{
    Task<bool> VeiculoPertenceAoClienteAsync(Guid clienteId, Guid veiculoId, CancellationToken cancelamento);
    Task<IReadOnlyDictionary<Guid, ServicoCatalogo>> ObterServicosAtivosAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancelamento);
    Task<IReadOnlyDictionary<Guid, ItemEstoque>> ObterItensAtivosAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancelamento);
    Task<OrdemServico?> ObterAsync(long numero, CancellationToken cancelamento);
    void Adicionar(OrdemServico ordem);
    void Adicionar(ServicoSolicitado servico);
    void Adicionar(ItemNecessario item);
    Task SalvarAsync(CancellationToken cancelamento);
}
