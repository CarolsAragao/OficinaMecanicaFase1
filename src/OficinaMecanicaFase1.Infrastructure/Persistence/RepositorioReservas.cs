using Microsoft.EntityFrameworkCore;
using OficinaMecanicaFase1.Application.OrdensServico;
using OficinaMecanicaFase1.Domain.Estoque;
using OficinaMecanicaFase1.Domain.OrdensServico;
using OficinaMecanicaFase1.Domain.Orcamentos;

namespace OficinaMecanicaFase1.Infrastructure.Persistence;

public sealed class RepositorioReservas(DbContextOptions<OficinaDbContext> opcoes) : IRepositorioReservas
{
    public async Task<ResultadoTentativaReserva> TentarReservarAsync(
        long numero, DateTimeOffset agora, CancellationToken cancelamento)
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
            if (ordem.SituacaoReserva == SituacaoReservaOrdemServico.Confirmada)
                return new ResultadoTentativaReserva(numero, SituacaoTentativaReserva.JaConfirmada, []);

            var aprovado = await db.Orcamentos.AsNoTracking().AnyAsync(
                orcamento => orcamento.OrdemServicoId == ordem.Id &&
                    orcamento.Decisao == DecisaoOrcamento.Aprovado, cancelamento);
            if (!aprovado || ordem.Status != StatusOrdemServico.AguardandoAprovacao ||
                ordem.OrcamentoAprovadoEmUtc is null || ordem.EncerradaPorRejeicaoEmUtc is not null)
                return new ResultadoTentativaReserva(numero, SituacaoTentativaReserva.NaoPermitida, []);

            await db.Entry(ordem).Collection(o => o.ItensNecessarios).LoadAsync(cancelamento);
            var itens = new Dictionary<Guid, ItemEstoque>();
            foreach (var id in ordem.ItensNecessarios.Select(item => item.ItemEstoqueId).OrderBy(id => id))
            {
                var encontrados = await db.ItensEstoque
                    .FromSqlInterpolated($"SELECT * FROM [ItensEstoque] WITH (UPDLOCK, ROWLOCK) WHERE [Id] = {id}")
                    .ToListAsync(cancelamento);
                var item = encontrados.SingleOrDefault();
                if (item is not null) itens.Add(id, item);
            }

            var faltantes = ordem.ItensNecessarios
                .Where(necessario => !itens.TryGetValue(necessario.ItemEstoqueId, out var item) ||
                    !item.Ativo || !item.QuantidadeValida(necessario.Quantidade) ||
                    item.SaldoDisponivel < necessario.Quantidade)
                .Select(necessario =>
                {
                    itens.TryGetValue(necessario.ItemEstoqueId, out var item);
                    return new ItemReservaInsuficiente(necessario.ItemEstoqueId,
                        item?.Nome ?? "Item indisponível", necessario.Quantidade, item?.SaldoDisponivel ?? 0m);
                }).ToArray();

            var pendentes = await db.AvisosReservaRecusada.Where(aviso =>
                aviso.OrdemServicoId == ordem.Id && aviso.ResolvidoEmUtc == null).ToArrayAsync(cancelamento);
            foreach (var aviso in pendentes) aviso.Resolver(agora);

            SituacaoTentativaReserva situacao;
            if (faltantes.Length > 0)
            {
                foreach (var faltante in faltantes)
                    db.AvisosReservaRecusada.Add(new AvisoReservaRecusada(ordem.Id,
                        faltante.ItemEstoqueId, faltante.QuantidadeSolicitada,
                        faltante.SaldoDisponivel, agora));
                db.Set<MarcoOrdemServico>().Add(ordem.RecusarReserva(agora));
                situacao = SituacaoTentativaReserva.Recusada;
            }
            else
            {
                foreach (var necessario in ordem.ItensNecessarios)
                {
                    itens[necessario.ItemEstoqueId].Reservar(necessario.Quantidade);
                    db.ReservasItensOrdensServico.Add(ReservaItemOrdemServico.Criar(
                        ordem.Id, necessario.ItemEstoqueId, necessario.Quantidade, agora));
                }
                db.Set<MarcoOrdemServico>().Add(ordem.ConfirmarReserva(agora));
                situacao = SituacaoTentativaReserva.Confirmada;
            }

            await db.SaveChangesAsync(cancelamento);
            await transacao.CommitAsync(cancelamento);
            return new ResultadoTentativaReserva(numero, situacao, faltantes);
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
}
