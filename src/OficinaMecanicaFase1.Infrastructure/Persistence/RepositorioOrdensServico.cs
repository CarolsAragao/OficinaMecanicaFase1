using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using OficinaMecanicaFase1.Application.Cadastros;
using OficinaMecanicaFase1.Application.OrdensServico;
using OficinaMecanicaFase1.Domain.Catalogo;
using OficinaMecanicaFase1.Domain.Estoque;
using OficinaMecanicaFase1.Domain.OrdensServico;
using OficinaMecanicaFase1.Domain.Orcamentos;

namespace OficinaMecanicaFase1.Infrastructure.Persistence;

public sealed class RepositorioOrdensServico(OficinaDbContext db) : IRepositorioOrdensServico
{
    public Task<bool> VeiculoPertenceAoClienteAsync(Guid clienteId, Guid veiculoId, CancellationToken cancelamento) =>
        db.Veiculos.AsNoTracking().AnyAsync(veiculo => veiculo.Id == veiculoId && veiculo.ClienteId == clienteId, cancelamento);

    public async Task<IReadOnlyDictionary<Guid, ServicoCatalogo>> ObterServicosAtivosAsync(
        IReadOnlyCollection<Guid> ids, CancellationToken cancelamento) =>
        await db.ServicosCatalogo.AsNoTracking()
            .Where(servico => servico.Ativo && ids.Contains(servico.Id))
            .ToDictionaryAsync(servico => servico.Id, cancelamento);

    public async Task<IReadOnlyDictionary<Guid, ItemEstoque>> ObterItensAtivosAsync(
        IReadOnlyCollection<Guid> ids, CancellationToken cancelamento) =>
        await db.ItensEstoque.AsNoTracking()
            .Where(item => item.Ativo && ids.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, cancelamento);

    public async Task<IReadOnlyDictionary<Guid, ServicoCatalogo>> ObterServicosAsync(
        IReadOnlyCollection<Guid> ids, CancellationToken cancelamento) =>
        await db.ServicosCatalogo.AsNoTracking().Where(servico => ids.Contains(servico.Id))
            .ToDictionaryAsync(servico => servico.Id, cancelamento);

    public async Task<IReadOnlyDictionary<Guid, ItemEstoque>> ObterItensAsync(
        IReadOnlyCollection<Guid> ids, CancellationToken cancelamento) =>
        await db.ItensEstoque.AsNoTracking().Where(item => ids.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, cancelamento);

    public Task<OrdemServico?> ObterAsync(long numero, CancellationToken cancelamento) =>
        db.OrdensServico
            .Include(ordem => ordem.ServicosSolicitados)
            .Include(ordem => ordem.ItensNecessarios)
            .Include(ordem => ordem.Marcos)
            .FirstOrDefaultAsync(ordem => ordem.Numero == numero, cancelamento);

    public Task<OrdemServico?> ObterDoClienteAsync(long numero, string documento, CancellationToken cancelamento) =>
        db.OrdensServico.FirstOrDefaultAsync(ordem => ordem.Numero == numero &&
            db.Clientes.Any(cliente => cliente.Id == ordem.ClienteId && cliente.Documento == documento), cancelamento);

    public Task<OrdemServico?> ObterAndamentoDoClienteAsync(long numero, string documento, CancellationToken cancelamento) =>
        db.OrdensServico.AsNoTracking().Include(ordem => ordem.Marcos)
            .FirstOrDefaultAsync(ordem => ordem.Numero == numero &&
                db.Clientes.Any(cliente => cliente.Id == ordem.ClienteId && cliente.Documento == documento), cancelamento);

    public Task<Orcamento?> ObterOrcamentoAsync(Guid ordemServicoId, CancellationToken cancelamento) =>
        db.Orcamentos.Include(orcamento => orcamento.Linhas)
            .FirstOrDefaultAsync(orcamento => orcamento.OrdemServicoId == ordemServicoId, cancelamento);

    public async Task<IReadOnlyCollection<ReservaItemOrdemServico>> ObterReservasAtivasAsync(
        Guid ordemServicoId, CancellationToken cancelamento) =>
        await db.ReservasItensOrdensServico.Where(reserva => reserva.OrdemServicoId == ordemServicoId &&
            reserva.LiberadaEmUtc == null).ToArrayAsync(cancelamento);

    public async Task<IReadOnlyDictionary<Guid, ItemEstoque>> ObterItensParaReservaAsync(
        IReadOnlyCollection<Guid> ids, CancellationToken cancelamento) =>
        await db.ItensEstoque.Where(item => ids.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, cancelamento);

    public void Adicionar(OrdemServico ordem) => db.OrdensServico.Add(ordem);
    public void Adicionar(ServicoSolicitado servico) => db.Set<ServicoSolicitado>().Add(servico);
    public void Adicionar(ItemNecessario item) => db.Set<ItemNecessario>().Add(item);
    public void Adicionar(MarcoOrdemServico marco) => db.Set<MarcoOrdemServico>().Add(marco);
    public void Adicionar(Orcamento orcamento) => db.Orcamentos.Add(orcamento);
    public void Adicionar(EmissaoCodigoAcesso emissao) => db.Set<EmissaoCodigoAcesso>().Add(emissao);

    public async Task SalvarAsync(CancellationToken cancelamento)
    {
        try { await db.SaveChangesAsync(cancelamento); }
        catch (DbUpdateConcurrencyException erro) { throw new ConflitoPersistenciaException(erro); }
        catch (DbUpdateException erro) when (erro.InnerException is SqlException sql &&
            sql.Number is 2601 or 2627 or 547)
        {
            throw new ConflitoPersistenciaException(erro);
        }
    }
}
