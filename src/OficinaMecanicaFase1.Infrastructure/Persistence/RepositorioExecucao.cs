using Microsoft.EntityFrameworkCore;
using OficinaMecanicaFase1.Application.OrdensServico;
using OficinaMecanicaFase1.Domain.Estoque;
using OficinaMecanicaFase1.Domain.OrdensServico;
using OficinaMecanicaFase1.Domain.Orcamentos;

namespace OficinaMecanicaFase1.Infrastructure.Persistence;

public sealed class RepositorioExecucao(DbContextOptions<OficinaDbContext> opcoes) : IRepositorioExecucao
{
    public Task<ResultadoOrdemServico<OrdemServico>> IniciarExecucaoAsync(
        long numero, Guid mecanicoId, DateTimeOffset agora, CancellationToken cancelamento) =>
        ExecutarAsync(numero, async (db, ordem, orcamento) =>
        {
            if (orcamento?.Decisao != DecisaoOrcamento.Aprovado ||
                !ordem.ServicosSolicitados.All(servico => orcamento.Linhas.Any(linha =>
                    linha.Tipo == TipoLinhaOrcamento.Servico && linha.ReferenciaId == servico.ServicoCatalogoId)))
                return Invalido(ordem, "A execução exige serviços autorizados em um orçamento aprovado.");

            var reservas = await db.ReservasItensOrdensServico.Where(reserva =>
                reserva.OrdemServicoId == ordem.Id && reserva.LiberadaEmUtc == null).ToArrayAsync(cancelamento);
            if (ordem.ItensNecessarios.Any(necessario => !orcamento.Linhas.Any(linha =>
                    linha.Tipo == TipoLinhaOrcamento.Item && linha.ReferenciaId == necessario.ItemEstoqueId) ||
                !reservas.Any(reserva => reserva.ItemEstoqueId == necessario.ItemEstoqueId &&
                    reserva.QuantidadeRestante >= necessario.Quantidade)))
                return Invalido(ordem, "A execução exige reserva integral de todos os itens autorizados.");

            db.Set<MarcoOrdemServico>().Add(ordem.IniciarExecucao(mecanicoId, agora));
            return ResultadoOrdemServico<OrdemServico>.Sucesso(ordem);
        }, cancelamento);

    public Task<ResultadoOrdemServico<OrdemServico>> IniciarServicoAsync(
        long numero, Guid servicoId, Guid mecanicoId, DateTimeOffset agora, CancellationToken cancelamento) =>
        ExecutarAsync(numero, (db, ordem, orcamento) =>
        {
            if (orcamento?.Decisao != DecisaoOrcamento.Aprovado || !orcamento.Linhas.Any(linha =>
                    linha.Tipo == TipoLinhaOrcamento.Servico && linha.ReferenciaId == servicoId))
                return Task.FromResult(Invalido(ordem, "O serviço não está autorizado pelo orçamento aprovado."));
            db.Set<MarcoOrdemServico>().Add(ordem.IniciarServico(servicoId, mecanicoId, agora));
            return Task.FromResult(ResultadoOrdemServico<OrdemServico>.Sucesso(ordem));
        }, cancelamento);

    public Task<ResultadoOrdemServico<OrdemServico>> ConcluirServicoAsync(
        long numero, Guid servicoId, Guid mecanicoId, DateTimeOffset agora, CancellationToken cancelamento) =>
        ExecutarAsync(numero, async (db, ordem, orcamento) =>
        {
            if (orcamento?.Decisao != DecisaoOrcamento.Aprovado || !orcamento.Linhas.Any(linha =>
                    linha.Tipo == TipoLinhaOrcamento.Servico && linha.ReferenciaId == servicoId))
                return Invalido(ordem, "O serviço não está autorizado pelo orçamento aprovado.");
            var ids = await db.ReservasItensOrdensServico.Where(reserva =>
                    reserva.OrdemServicoId == ordem.Id && reserva.LiberadaEmUtc == null)
                .Select(reserva => reserva.ItemEstoqueId).Distinct().OrderBy(id => id).ToArrayAsync(cancelamento);
            var itens = await BloquearItensAsync(db, ids, cancelamento);
            db.Set<MarcoOrdemServico>().Add(ordem.ConcluirServico(servicoId, mecanicoId, agora));
            if (ordem.Status == StatusOrdemServico.Finalizada)
            {
                var reservas = await db.ReservasItensOrdensServico.Where(reserva =>
                    reserva.OrdemServicoId == ordem.Id && reserva.LiberadaEmUtc == null).ToArrayAsync(cancelamento);
                foreach (var reserva in reservas)
                {
                    if (reserva.QuantidadeRestante > 0)
                        itens[reserva.ItemEstoqueId].LiberarReserva(reserva.QuantidadeRestante);
                    reserva.Liberar(agora);
                }
                db.Set<MarcoOrdemServico>().Add(ordem.Marcos.Last());
            }
            return ResultadoOrdemServico<OrdemServico>.Sucesso(ordem);
        }, cancelamento);

    public Task<ResultadoOrdemServico<OrdemServico>> RegistrarConsumoAsync(
        long numero, Guid mecanicoId, IReadOnlyCollection<ConsumoSolicitado> consumos,
        DateTimeOffset agora, CancellationToken cancelamento) =>
        ExecutarAsync(numero, async (db, ordem, orcamento) =>
        {
            if (ordem.Status != StatusOrdemServico.EmExecucao)
                return Invalido(ordem, "O consumo só pode ser registrado durante a execução.");
            if (consumos.Count == 0 || consumos.Any(consumo => consumo.ItemEstoqueId == Guid.Empty) ||
                consumos.Select(consumo => consumo.ItemEstoqueId).Distinct().Count() != consumos.Count)
                return Invalido(ordem, "Informe itens de consumo válidos, sem repetições.");
            if (orcamento?.Decisao != DecisaoOrcamento.Aprovado)
                return Invalido(ordem, "O consumo exige orçamento aprovado.");
            foreach (var consumo in consumos)
                if (!ordem.AutorizaItem(consumo.ItemEstoqueId) || !orcamento.Linhas.Any(linha =>
                        linha.Tipo == TipoLinhaOrcamento.Item && linha.ReferenciaId == consumo.ItemEstoqueId))
                    return Invalido(ordem, "O item não está autorizado pelo orçamento aprovado.");

            var itens = await BloquearItensAsync(db, consumos.Select(consumo => consumo.ItemEstoqueId)
                .OrderBy(id => id).ToArray(), cancelamento);
            var reservas = await db.ReservasItensOrdensServico.Where(reserva =>
                reserva.OrdemServicoId == ordem.Id && reserva.LiberadaEmUtc == null).ToArrayAsync(cancelamento);
            foreach (var consumo in consumos)
            {
                if (!itens.TryGetValue(consumo.ItemEstoqueId, out var item) ||
                    reservas.SingleOrDefault(reserva => reserva.ItemEstoqueId == consumo.ItemEstoqueId) is not { } reserva)
                    return Invalido(ordem, "Não existe reserva ativa para um dos itens informados.");
                if (!item.QuantidadeValida(consumo.Quantidade))
                    return Invalido(ordem, "A quantidade consumida não é válida para o tipo do item.");
                reserva.RegistrarConsumo(consumo.Quantidade);
                item.RegistrarConsumoReservado(consumo.Quantidade);
                db.Set<ConsumoItemOrdemServico>().Add(ConsumoItemOrdemServico.Criar(
                    ordem.Id, item.Id, mecanicoId, consumo.Quantidade, agora));
            }
            return ResultadoOrdemServico<OrdemServico>.Sucesso(ordem);
        }, cancelamento);

    private async Task<ResultadoOrdemServico<OrdemServico>> ExecutarAsync(long numero,
        Func<OficinaDbContext, OrdemServico, Orcamento?, Task<ResultadoOrdemServico<OrdemServico>>> acao,
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
                await db.Entry(ordem).Collection(item => item.Marcos).LoadAsync(cancelamento);
                var orcamento = await db.Orcamentos.Include(item => item.Linhas)
                    .FirstOrDefaultAsync(item => item.OrdemServicoId == ordem.Id, cancelamento);
                ResultadoOrdemServico<OrdemServico> resultado;
                try { resultado = await acao(db, ordem, orcamento); }
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

    private static async Task<Dictionary<Guid, ItemEstoque>> BloquearItensAsync(
        OficinaDbContext db, IReadOnlyCollection<Guid> ids, CancellationToken cancelamento)
    {
        var itens = new Dictionary<Guid, ItemEstoque>();
        foreach (var id in ids)
        {
            var encontrados = await db.ItensEstoque.FromSqlInterpolated(
                $"SELECT * FROM [ItensEstoque] WITH (UPDLOCK, ROWLOCK) WHERE [Id] = {id}")
                .ToListAsync(cancelamento);
            if (encontrados.SingleOrDefault() is { } item) itens.Add(id, item);
        }
        return itens;
    }

    private static ResultadoOrdemServico<OrdemServico> Invalido(OrdemServico ordem, string mensagem) =>
        ResultadoOrdemServico<OrdemServico>.Falha(ErroOrdemServico.Invalido, mensagem, "ordemServico");
}
