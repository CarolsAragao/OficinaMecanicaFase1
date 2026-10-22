using OficinaMecanicaFase1.Domain.Catalogo;
using OficinaMecanicaFase1.Domain.Estoque;

namespace OficinaMecanicaFase1.Application.Catalogo;

public interface IRepositorioCatalogo
{
    Task<IReadOnlyList<ServicoCatalogo>> ListarServicosAsync(bool somenteAtivos, CancellationToken cancelamento);
    Task<ServicoCatalogo?> ObterServicoAsync(Guid id, CancellationToken cancelamento);
    Task<ServicoCatalogo?> ObterServicoAtivoAsync(Guid id, CancellationToken cancelamento);
    Task<IReadOnlyList<ItemEstoque>> ListarItensAsync(bool somenteAtivos, CancellationToken cancelamento);
    Task<ItemEstoque?> ObterItemAsync(Guid id, CancellationToken cancelamento);
    Task<ItemEstoque?> ObterItemAtivoAsync(Guid id, CancellationToken cancelamento);
    Task<IReadOnlyList<EntradaEstoque>> ListarEntradasAsync(Guid itemId, CancellationToken cancelamento);
    void Adicionar(ServicoCatalogo servico);
    void Adicionar(ItemEstoque item);
    void Adicionar(EntradaEstoque entrada);
    Task SalvarAsync(CancellationToken cancelamento);
}
