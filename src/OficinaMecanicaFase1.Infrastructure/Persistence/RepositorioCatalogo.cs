using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using OficinaMecanicaFase1.Application.Cadastros;
using OficinaMecanicaFase1.Application.Catalogo;
using OficinaMecanicaFase1.Domain.Catalogo;
using OficinaMecanicaFase1.Domain.Estoque;

namespace OficinaMecanicaFase1.Infrastructure.Persistence;

public sealed class RepositorioCatalogo(OficinaDbContext db) : IRepositorioCatalogo
{
    public async Task<IReadOnlyList<ServicoCatalogo>> ListarServicosAsync(bool somenteAtivos, CancellationToken cancelamento) =>
        await db.ServicosCatalogo.AsNoTracking()
            .Where(servico => !somenteAtivos || servico.Ativo)
            .OrderBy(servico => servico.Nome)
            .ToListAsync(cancelamento);

    public Task<ServicoCatalogo?> ObterServicoAsync(Guid id, CancellationToken cancelamento) =>
        db.ServicosCatalogo.FirstOrDefaultAsync(servico => servico.Id == id, cancelamento);

    public Task<ServicoCatalogo?> ObterServicoAtivoAsync(Guid id, CancellationToken cancelamento) =>
        db.ServicosCatalogo.AsNoTracking().FirstOrDefaultAsync(servico => servico.Id == id && servico.Ativo, cancelamento);

    public async Task<IReadOnlyList<ItemEstoque>> ListarItensAsync(bool somenteAtivos, CancellationToken cancelamento) =>
        await db.ItensEstoque.AsNoTracking()
            .Where(item => !somenteAtivos || item.Ativo)
            .OrderBy(item => item.Nome)
            .ToListAsync(cancelamento);

    public Task<ItemEstoque?> ObterItemAsync(Guid id, CancellationToken cancelamento) =>
        db.ItensEstoque.FirstOrDefaultAsync(item => item.Id == id, cancelamento);

    public Task<ItemEstoque?> ObterItemAtivoAsync(Guid id, CancellationToken cancelamento) =>
        db.ItensEstoque.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id && item.Ativo, cancelamento);

    public async Task<IReadOnlyList<EntradaEstoque>> ListarEntradasAsync(Guid itemId, CancellationToken cancelamento) =>
        await db.EntradasEstoque.AsNoTracking()
            .Where(entrada => entrada.ItemEstoqueId == itemId)
            .OrderByDescending(entrada => entrada.RegistradaEmUtc)
            .ToListAsync(cancelamento);

    public void Adicionar(ServicoCatalogo servico) => db.ServicosCatalogo.Add(servico);
    public void Adicionar(ItemEstoque item) => db.ItensEstoque.Add(item);
    public void Adicionar(EntradaEstoque entrada) => db.EntradasEstoque.Add(entrada);

    public async Task SalvarAsync(CancellationToken cancelamento)
    {
        try { await db.SaveChangesAsync(cancelamento); }
        catch (DbUpdateConcurrencyException erro)
        {
            throw new ConflitoPersistenciaException(erro);
        }
        catch (DbUpdateException erro) when (erro.InnerException is SqlException sql &&
            sql.Number is 2601 or 2627 or 547)
        {
            throw new ConflitoPersistenciaException(erro);
        }
    }
}
