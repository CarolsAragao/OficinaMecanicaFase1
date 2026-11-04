using Microsoft.EntityFrameworkCore;
using OficinaMecanicaFase1.Application.OrdensServico;
using OficinaMecanicaFase1.Domain.Estoque;
using OficinaMecanicaFase1.Domain.OrdensServico;
using OficinaMecanicaFase1.Domain.Orcamentos;

namespace OficinaMecanicaFase1.Infrastructure.Persistence;

public sealed class ReservaEstoqueRepository(DbContextOptions<OficinaDbContext> opcoes) : IReservaEstoqueRepository
{
    public async Task<ResultadoTentativaReserva> ExecutarAsync(long numero,
        Func<IOperacoesReservaEstoque, OrdemServico, Task<ResultadoTentativaReserva>> acao,
        CancellationToken cancelamento)
    {
        await using var contextoEstrategia = new OficinaDbContext(opcoes);
        var estrategia = contextoEstrategia.Database.CreateExecutionStrategy();
        return await estrategia.ExecuteAsync(async () =>
        {
            await using var db = new OficinaDbContext(opcoes);
            await using var transacao = await db.Database.BeginTransactionAsync(cancelamento);
            var ordens = await db.OrdensServico
                .FromSqlInterpolated($"SELECT * FROM [OrdensServico] WITH (UPDLOCK, ROWLOCK) WHERE [Numero] = {numero}")
                .ToListAsync(cancelamento);
            var ordem = ordens.SingleOrDefault();
            if (ordem is null)
                return new ResultadoTentativaReserva(numero, SituacaoTentativaReserva.NaoEncontrada, []);
            await db.Entry(ordem).Collection(o => o.ItensNecessarios).LoadAsync(cancelamento);
            var resultado = await acao(new OperacoesReservaEstoque(db), ordem);
            if (resultado.Situacao is SituacaoTentativaReserva.Confirmada or SituacaoTentativaReserva.Recusada)
            {
                await db.SaveChangesAsync(cancelamento);
                await transacao.CommitAsync(cancelamento);
            }
            return resultado;
        });
    }

    public async Task<IReadOnlyCollection<AvisoReservaEstoque>> ListarAvisosAsync(
        bool somentePendentes, CancellationToken cancelamento)
    {
        await using var db = new OficinaDbContext(opcoes);
        return await (from aviso in db.AvisosReservaRecusada.AsNoTracking()
            join ordem in db.OrdensServico.AsNoTracking() on aviso.OrdemServicoId equals ordem.Id
            join item in db.ItensEstoque.AsNoTracking() on aviso.ItemEstoqueId equals item.Id
            where !somentePendentes || aviso.ResolvidoEmUtc == null
            orderby aviso.RegistradoEmUtc descending
            select new AvisoReservaEstoque(aviso.Id, ordem.Numero, item.Id, item.Nome,
                aviso.QuantidadeSolicitada, aviso.SaldoDisponivelObservado,
                aviso.RegistradoEmUtc, aviso.ResolvidoEmUtc)).ToArrayAsync(cancelamento);
    }

    private sealed class OperacoesReservaEstoque(OficinaDbContext db) : IOperacoesReservaEstoque
    {
        public Task<bool> TemOrcamentoAprovadoAsync(Guid ordemId, CancellationToken cancelamento) =>
            db.Orcamentos.AsNoTracking().AnyAsync(orcamento =>
                orcamento.OrdemServicoId == ordemId && orcamento.Decisao == DecisaoOrcamento.Aprovado, cancelamento);

        public Task<AvisoReservaRecusada[]> ObterAvisosPendentesAsync(Guid ordemId, CancellationToken cancelamento) =>
            db.AvisosReservaRecusada.Where(aviso =>
                aviso.OrdemServicoId == ordemId && aviso.ResolvidoEmUtc == null).ToArrayAsync(cancelamento);

        public Task<Dictionary<Guid, ItemEstoque>> BloquearItensAsync(
            IReadOnlyCollection<Guid> ids, CancellationToken cancelamento) =>
            BloqueioEstoque.BloquearItensAsync(db, ids, cancelamento);

        public void Adicionar(AvisoReservaRecusada aviso) => db.AvisosReservaRecusada.Add(aviso);
        public void Adicionar(ReservaItemOrdemServico reserva) => db.ReservasItensOrdensServico.Add(reserva);
        public void Adicionar(MarcoOrdemServico marco) => db.Set<MarcoOrdemServico>().Add(marco);
    }
}
