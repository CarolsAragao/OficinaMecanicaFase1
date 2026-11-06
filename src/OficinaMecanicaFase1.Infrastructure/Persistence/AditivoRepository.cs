using Microsoft.EntityFrameworkCore;
using OficinaMecanicaFase1.Application.OrdensServico;
using OficinaMecanicaFase1.Domain.Cadastros;
using OficinaMecanicaFase1.Domain.Catalogo;
using OficinaMecanicaFase1.Domain.Estoque;
using OficinaMecanicaFase1.Domain.OrdensServico;
using OficinaMecanicaFase1.Domain.Orcamentos;

namespace OficinaMecanicaFase1.Infrastructure.Persistence;

internal sealed class AditivoRepository(DbContextOptions<OficinaDbContext> opcoes) : IAditivoRepository
{
    public async Task<Aditivo?> ConsultarClienteAsync(long numero, Guid aditivoId, string documento,
        CancellationToken ct)
    {
        await using var db = new OficinaDbContext(opcoes);
        return await (from a in db.Aditivos
            join o in db.OrdensServico on a.OrdemServicoId equals o.Id
            join c in db.Clientes on o.ClienteId equals c.Id
            where o.Numero == numero && a.Id == aditivoId && c.Documento == documento
            select a).SingleOrDefaultAsync(ct);
    }

    public async Task<Aditivo?> ConsultarInternoAsync(long numero, Guid aditivoId, CancellationToken ct)
    {
        await using var db = new OficinaDbContext(opcoes);
        return await (from a in db.Aditivos join o in db.OrdensServico on a.OrdemServicoId equals o.Id
            where o.Numero == numero && a.Id == aditivoId select a).SingleOrDefaultAsync(ct);
    }

    public async Task<ResultadoOrdemServico<Aditivo>> ExecutarAsync(long numero,
        Func<IOperacoesAditivos, OrdemServico, Task<ResultadoOrdemServico<Aditivo>>> acao,
        CancellationToken ct)
    {
        try
        {
            await using var contexto = new OficinaDbContext(opcoes);
            var estrategia = contexto.Database.CreateExecutionStrategy();
            return await estrategia.ExecuteAsync(async () =>
            {
                await using var db = new OficinaDbContext(opcoes);
                await using var transacao = await db.Database.BeginTransactionAsync(ct);
                var ordens = await db.OrdensServico.FromSqlInterpolated(
                    $"SELECT * FROM [OrdensServico] WITH (UPDLOCK, ROWLOCK) WHERE [Numero] = {numero}")
                    .ToListAsync(ct);
                var ordem = ordens.SingleOrDefault();
                if (ordem is null) return NaoEncontrado();
                await db.Entry(ordem).Collection(o => o.ServicosSolicitados).LoadAsync(ct);
                await db.Entry(ordem).Collection(o => o.ItensNecessarios).LoadAsync(ct);
                ResultadoOrdemServico<Aditivo> resposta;
                try { resposta = await acao(new OperacoesAditivos(db), ordem); }
                catch (ArgumentException erro) { return Invalido(erro.Message); }
                catch (InvalidOperationException erro) { return Conflito(erro.Message); }
                if (resposta.Erro != ErroOrdemServico.Nenhum) return resposta;
                await db.SaveChangesAsync(ct);
                await transacao.CommitAsync(ct);
                return resposta;
            });
        }
        catch (DbUpdateException) { return Conflito("A OS ou o estoque mudou; consulte e tente novamente."); }
    }

    private static ResultadoOrdemServico<Aditivo> NaoEncontrado() =>
        ResultadoOrdemServico<Aditivo>.Falha(ErroOrdemServico.NaoEncontrado, "Aditivo ou credenciais não encontrados.");
    private static ResultadoOrdemServico<Aditivo> Invalido(string mensagem) =>
        ResultadoOrdemServico<Aditivo>.Falha(ErroOrdemServico.Invalido, mensagem);
    private static ResultadoOrdemServico<Aditivo> Conflito(string mensagem) =>
        ResultadoOrdemServico<Aditivo>.Falha(ErroOrdemServico.Conflito, mensagem);

    private sealed class OperacoesAditivos(OficinaDbContext db) : IOperacoesAditivos
    {
        public Task<bool> TemPendenteAsync(Guid ordemId, CancellationToken ct) =>
            db.Aditivos.AnyAsync(a => a.OrdemServicoId == ordemId && a.Situacao == SituacaoAditivo.Pendente, ct);

        public Task<Aditivo?> ObterAditivoAsync(Guid ordemId, Guid aditivoId, CancellationToken ct) =>
            db.Aditivos.SingleOrDefaultAsync(a => a.Id == aditivoId && a.OrdemServicoId == ordemId, ct);

        public ValueTask<Cliente?> ObterClienteAsync(Guid id, CancellationToken ct) =>
            db.Clientes.FindAsync([id], ct);

        public ValueTask<ItemEstoque?> ObterItemAsync(Guid id, CancellationToken ct) =>
            db.ItensEstoque.FindAsync([id], ct);

        public ValueTask<ServicoCatalogo?> ObterServicoAsync(Guid id, CancellationToken ct) =>
            db.ServicosCatalogo.FindAsync([id], ct);

        public async Task<ItemEstoque?> BloquearItemAsync(Guid id, CancellationToken ct)
        {
            var itens = await db.ItensEstoque.FromSqlInterpolated(
                $"SELECT * FROM [ItensEstoque] WITH (UPDLOCK, ROWLOCK) WHERE [Id] = {id}")
                .ToListAsync(ct);
            return itens.SingleOrDefault();
        }

        public Task<ReservaItemOrdemServico?> ObterReservaAtivaAsync(Guid ordemId, Guid itemId, CancellationToken ct) =>
            db.ReservasItensOrdensServico.SingleOrDefaultAsync(r =>
                r.OrdemServicoId == ordemId && r.ItemEstoqueId == itemId && r.LiberadaEmUtc == null, ct);

        public void Adicionar(Aditivo aditivo) => db.Aditivos.Add(aditivo);
        public void Adicionar(RegistroHistoricoOrdemServico marco) => db.Set<RegistroHistoricoOrdemServico>().Add(marco);
        public void Adicionar(ServicoSolicitado servico) => db.Set<ServicoSolicitado>().Add(servico);
        public void Adicionar(ItemNecessario item) => db.Set<ItemNecessario>().Add(item);
        public void Adicionar(ReservaItemOrdemServico reserva) => db.ReservasItensOrdensServico.Add(reserva);
    }
}
