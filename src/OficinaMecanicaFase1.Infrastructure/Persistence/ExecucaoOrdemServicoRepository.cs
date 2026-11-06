using Microsoft.EntityFrameworkCore;
using OficinaMecanicaFase1.Application.OrdensServico;
using OficinaMecanicaFase1.Domain.Estoque;
using OficinaMecanicaFase1.Domain.OrdensServico;
using OficinaMecanicaFase1.Domain.Orcamentos;

namespace OficinaMecanicaFase1.Infrastructure.Persistence;

public sealed class ExecucaoOrdemServicoRepository(DbContextOptions<OficinaDbContext> opcoes) : IExecucaoOrdemServicoRepository
{
    public async Task<ResultadoOrdemServico<OrdemServico>> ExecutarAsync(long numero,
        Func<IOperacoesExecucao, OrdemServico, Orcamento?, Task<ResultadoOrdemServico<OrdemServico>>> acao,
        CancellationToken cancelamento)
    {
        try
        {
            await using var contextoEstrategia = new OficinaDbContext(opcoes);
            var estrategia = contextoEstrategia.Database.CreateExecutionStrategy();
            return await estrategia.ExecuteAsync(async () =>
            {
                await using var db = new OficinaDbContext(opcoes);
                await using var transacao = await db.Database.BeginTransactionAsync(cancelamento);
                var ordens = await db.OrdensServico.FromSqlInterpolated(
                    $"SELECT * FROM [OrdensServico] WITH (UPDLOCK, ROWLOCK) WHERE [Numero] = {numero}")
                    .ToListAsync(cancelamento);
                var ordem = ordens.SingleOrDefault();
                if (ordem is null)
                    return ResultadoOrdemServico<OrdemServico>.Falha(
                        ErroOrdemServico.NaoEncontrado, "Ordem de serviço não encontrada.");
                await db.Entry(ordem).Collection(item => item.ServicosSolicitados).LoadAsync(cancelamento);
                await db.Entry(ordem).Collection(item => item.ItensNecessarios).LoadAsync(cancelamento);
                await db.Entry(ordem).Collection(item => item.Historico).LoadAsync(cancelamento);
                var orcamento = await db.Orcamentos.Include(item => item.Linhas)
                    .FirstOrDefaultAsync(item => item.OrdemServicoId == ordem.Id, cancelamento);
                ResultadoOrdemServico<OrdemServico> resultado;
                try { resultado = await acao(new OperacoesExecucao(db), ordem, orcamento); }
                catch (ArgumentException erro)
                {
                    return ResultadoOrdemServico<OrdemServico>.Falha(ErroOrdemServico.Invalido, erro.Message, erro.ParamName);
                }
                catch (InvalidOperationException erro)
                {
                    return ResultadoOrdemServico<OrdemServico>.Falha(ErroOrdemServico.Invalido, erro.Message, "ordemServico");
                }
                if (resultado.Erro != ErroOrdemServico.Nenhum) return resultado;
                await db.SaveChangesAsync(cancelamento);
                await transacao.CommitAsync(cancelamento);
                return resultado;
            });
        }
        catch (DbUpdateConcurrencyException)
        {
            return ResultadoOrdemServico<OrdemServico>.Falha(
                ErroOrdemServico.Conflito, "A OS ou o estoque foi alterado. Consulte e tente novamente.");
        }
        catch (DbUpdateException)
        {
            return ResultadoOrdemServico<OrdemServico>.Falha(
                ErroOrdemServico.Conflito, "A OS ou o estoque foi alterado. Consulte e tente novamente.");
        }
    }

    private sealed class OperacoesExecucao(OficinaDbContext db) : IOperacoesExecucao
    {
        public Task<Aditivo[]> ObterAditivosAsync(Guid ordemId, CancellationToken cancelamento) =>
            db.Aditivos.AsNoTracking().Where(aditivo => aditivo.OrdemServicoId == ordemId).ToArrayAsync(cancelamento);

        public Task<ReservaItemOrdemServico[]> ObterReservasAtivasAsync(Guid ordemId, CancellationToken cancelamento) =>
            db.ReservasItensOrdensServico.Where(reserva =>
                reserva.OrdemServicoId == ordemId && reserva.LiberadaEmUtc == null).ToArrayAsync(cancelamento);

        public Task<Dictionary<Guid, ItemEstoque>> BloquearItensAsync(
            IReadOnlyCollection<Guid> ids, CancellationToken cancelamento) =>
            BloqueioEstoque.BloquearItensAsync(db, ids, cancelamento);

        public void Adicionar(RegistroHistoricoOrdemServico marco) => db.Set<RegistroHistoricoOrdemServico>().Add(marco);
        public void Adicionar(ConsumoItemOrdemServico consumo) => db.Set<ConsumoItemOrdemServico>().Add(consumo);
    }
}
