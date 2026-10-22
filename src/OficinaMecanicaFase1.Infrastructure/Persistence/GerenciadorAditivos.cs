using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using OficinaMecanicaFase1.Application.OrdensServico;
using OficinaMecanicaFase1.Domain.Cadastros;
using OficinaMecanicaFase1.Domain.Estoque;
using OficinaMecanicaFase1.Domain.OrdensServico;
using OficinaMecanicaFase1.Domain.Orcamentos;

namespace OficinaMecanicaFase1.Infrastructure.Persistence;

public sealed class GerenciadorAditivos(DbContextOptions<OficinaDbContext> opcoes, TimeProvider tempo)
{
    public async Task<ResultadoOrdemServico<Aditivo>> CriarAsync(long numero, Guid mecanicoId,
        TipoAditivo tipo, string descricao, Guid? servicoId, Guid? itemId, decimal quantidade,
        bool originalPodeProsseguir, CancellationToken ct) => await ExecutarAsync(numero,
        async (db, ordem) =>
        {
            if (ordem.Status != StatusOrdemServico.EmExecucao)
                return Invalido("A OS precisa estar em execução.");
            if (await db.Aditivos.AnyAsync(a => a.OrdemServicoId == ordem.Id &&
                    a.Situacao == SituacaoAditivo.Pendente, ct))
                return Conflito("Decida o aditivo pendente antes de registrar outro.");
            if (tipo == TipoAditivo.ConsumoExcedente &&
                (itemId is null || !ordem.AutorizaItem(itemId.Value)))
                return Invalido("Consumo excedente exige um item já autorizado na OS.");
            if (tipo == TipoAditivo.ReparoAdicional && (servicoId is null ||
                ordem.ServicosSolicitados.Any(s => s.ServicoCatalogoId == servicoId)))
                return Invalido("Informe um serviço adicional ainda não incluído na OS.");
            var item = itemId is { } estoqueId ? await db.ItensEstoque.FindAsync([estoqueId], ct) : null;
            if (itemId is not null && (item is null || !item.Ativo || !item.QuantidadeValida(quantidade)))
                return Invalido("Item inativo, inexistente ou quantidade inválida.");
            if (itemId is null && (tipo != TipoAditivo.ReparoAdicional || quantidade != 0))
                return Invalido("Reparo sem item deve ter quantidade zero.");
            var servico = servicoId is { } id ? await db.ServicosCatalogo.FindAsync([id], ct) : null;
            if (servicoId is not null && (servico is null || !servico.Ativo))
                return Invalido("Serviço adicional inexistente ou inativo.");
            var aditivo = Aditivo.Criar(ordem.Id, tipo, descricao, servicoId, servico?.Nome,
                itemId, item?.Nome, quantidade, servico?.PrecoBase ?? 0, item?.PrecoUnitario ?? 0,
                originalPodeProsseguir,
                mecanicoId, tempo.GetUtcNow());
            db.Aditivos.Add(aditivo);
            db.Set<MarcoOrdemServico>().Add(ordem.RegistrarMarcoAditivo(
                tipo == TipoAditivo.ConsumoExcedente ? "Consumo excedente identificado; aprovação adicional solicitada" :
                "Reparo adicional identificado; aditivo disponibilizado", tempo.GetUtcNow()));
            return ResultadoOrdemServico<Aditivo>.Sucesso(aditivo);
        }, ct);

    public async Task<Aditivo?> ConsultarClienteAsync(long numero, Guid aditivoId, string? documento,
        CancellationToken ct)
    {
        var normalizado = DocumentoCliente.Normalizar(documento);
        if (normalizado is null) return null;
        await using var db = new OficinaDbContext(opcoes);
        return await (from a in db.Aditivos
            join o in db.OrdensServico on a.OrdemServicoId equals o.Id
            join c in db.Clientes on o.ClienteId equals c.Id
            where o.Numero == numero && a.Id == aditivoId && c.Documento == normalizado
            select a).SingleOrDefaultAsync(ct);
    }

    public async Task<Aditivo?> ConsultarInternoAsync(long numero, Guid aditivoId, CancellationToken ct)
    {
        await using var db = new OficinaDbContext(opcoes);
        return await (from a in db.Aditivos join o in db.OrdensServico on a.OrdemServicoId equals o.Id
            where o.Numero == numero && a.Id == aditivoId select a).SingleOrDefaultAsync(ct);
    }

    public async Task<ResultadoOrdemServico<Aditivo>> DecidirAsync(long numero, Guid aditivoId,
        string? documento, string? codigo, bool aprovar, string? motivo, CancellationToken ct)
    {
        var normalizado = DocumentoCliente.Normalizar(documento);
        var limpo = codigo?.Trim().Replace("-", "", StringComparison.Ordinal).ToUpperInvariant();
        if (normalizado is null || limpo is null || limpo.Length != 24 || !limpo.All(Uri.IsHexDigit))
            return NaoEncontrado();
        var hash = SHA256.HashData(Encoding.ASCII.GetBytes(limpo));
        return await ExecutarAsync(numero, async (db, ordem) =>
        {
            var cliente = await db.Clientes.FindAsync([ordem.ClienteId], ct);
            if (cliente?.Documento != normalizado || !ordem.ValidarCodigoAcesso(hash)) return NaoEncontrado();
            var aditivo = await db.Aditivos.SingleOrDefaultAsync(a =>
                a.Id == aditivoId && a.OrdemServicoId == ordem.Id, ct);
            if (aditivo is null) return NaoEncontrado();
            if (ordem.Status != StatusOrdemServico.EmExecucao || aditivo.Situacao != SituacaoAditivo.Pendente)
                return Conflito("O aditivo não aguarda decisão.");
            aditivo.Decidir(aprovar, motivo, tempo.GetUtcNow());
            db.Set<MarcoOrdemServico>().Add(ordem.RegistrarMarcoAditivo(aprovar ?
                "Aditivo aprovado; reserva adicional solicitada" :
                aditivo.OriginalPodeProsseguir ? "Aditivo rejeitado; escopo original pode prosseguir" :
                "Aditivo rejeitado; impedimento do escopo original registrado", tempo.GetUtcNow()));
            return ResultadoOrdemServico<Aditivo>.Sucesso(aditivo);
        }, ct, async resultado =>
        {
            if (aprovar && resultado.Erro == ErroOrdemServico.Nenhum)
                await TentarReservaAsync(numero, aditivoId, ct);
        });
    }

    public async Task<ResultadoOrdemServico<Aditivo>> TentarReservaAsync(long numero, Guid aditivoId,
        CancellationToken ct) => await ExecutarAsync(numero, async (db, ordem) =>
    {
        var aditivo = await db.Aditivos.SingleOrDefaultAsync(a =>
            a.Id == aditivoId && a.OrdemServicoId == ordem.Id, ct);
        if (aditivo is null) return NaoEncontrado();
        if (ordem.Status != StatusOrdemServico.EmExecucao || aditivo.Situacao != SituacaoAditivo.Aprovado ||
            aditivo.SituacaoReserva == SituacaoReservaAditivo.Confirmada)
            return Conflito("O aditivo não aguarda reserva.");
        if (aditivo.ItemEstoqueId is null)
        {
            var novos = ordem.AutorizarAditivo(aditivo.ServicoCatalogoId, null, 0);
            if (novos.Servico is not null) db.Set<ServicoSolicitado>().Add(novos.Servico);
            aditivo.RegistrarReserva(true);
            db.Set<MarcoOrdemServico>().Add(ordem.RegistrarMarcoAditivo(
                "Reparo adicional sem item autorizado", tempo.GetUtcNow()));
            return ResultadoOrdemServico<Aditivo>.Sucesso(aditivo);
        }
        var itens = await db.ItensEstoque.FromSqlInterpolated(
            $"SELECT * FROM [ItensEstoque] WITH (UPDLOCK, ROWLOCK) WHERE [Id] = {aditivo.ItemEstoqueId}")
            .ToListAsync(ct);
        var item = itens.SingleOrDefault();
        if (item is null) return Conflito("Item do aditivo não encontrado.");
        if (item.SaldoDisponivel < aditivo.Quantidade)
        {
            aditivo.RegistrarReserva(false);
            db.Set<MarcoOrdemServico>().Add(ordem.RegistrarMarcoAditivo(
                "Reserva do aditivo recusada; aguarda reposição", tempo.GetUtcNow()));
            return ResultadoOrdemServico<Aditivo>.Sucesso(aditivo);
        }
        item.Reservar(aditivo.Quantidade);
        var reserva = await db.ReservasItensOrdensServico.SingleOrDefaultAsync(r =>
            r.OrdemServicoId == ordem.Id && r.ItemEstoqueId == item.Id && r.LiberadaEmUtc == null, ct);
        if (reserva is null)
            db.ReservasItensOrdensServico.Add(ReservaItemOrdemServico.Criar(
                ordem.Id, item.Id, aditivo.Quantidade, tempo.GetUtcNow()));
        else reserva.Acrescentar(aditivo.Quantidade);
        var autorizados = ordem.AutorizarAditivo(aditivo.ServicoCatalogoId, item.Id, aditivo.Quantidade);
        if (autorizados.Servico is not null) db.Set<ServicoSolicitado>().Add(autorizados.Servico);
        if (autorizados.Item is not null) db.Set<ItemNecessario>().Add(autorizados.Item);
        aditivo.RegistrarReserva(true);
        db.Set<MarcoOrdemServico>().Add(ordem.RegistrarMarcoAditivo(
            "Itens adicionais reservados; aditivo autorizado", tempo.GetUtcNow()));
        return ResultadoOrdemServico<Aditivo>.Sucesso(aditivo);
    }, ct);

    private async Task<ResultadoOrdemServico<Aditivo>> ExecutarAsync(long numero,
        Func<OficinaDbContext, OrdemServico, Task<ResultadoOrdemServico<Aditivo>>> acao,
        CancellationToken ct, Func<ResultadoOrdemServico<Aditivo>, Task>? depois = null)
    {
        try
        {
            await using var contexto = new OficinaDbContext(opcoes);
            var estrategia = contexto.Database.CreateExecutionStrategy();
            var resultado = await estrategia.ExecuteAsync(async () =>
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
                try { resposta = await acao(db, ordem); }
                catch (ArgumentException erro) { return Invalido(erro.Message); }
                catch (InvalidOperationException erro) { return Conflito(erro.Message); }
                if (resposta.Erro != ErroOrdemServico.Nenhum) return resposta;
                await db.SaveChangesAsync(ct);
                await transacao.CommitAsync(ct);
                return resposta;
            });
            if (depois is not null) await depois(resultado);
            return resultado;
        }
        catch (DbUpdateException) { return Conflito("A OS ou o estoque mudou; consulte e tente novamente."); }
    }

    private static ResultadoOrdemServico<Aditivo> NaoEncontrado() =>
        ResultadoOrdemServico<Aditivo>.Falha(ErroOrdemServico.NaoEncontrado, "Aditivo ou credenciais não encontrados.");
    private static ResultadoOrdemServico<Aditivo> Invalido(string mensagem) =>
        ResultadoOrdemServico<Aditivo>.Falha(ErroOrdemServico.Invalido, mensagem);
    private static ResultadoOrdemServico<Aditivo> Conflito(string mensagem) =>
        ResultadoOrdemServico<Aditivo>.Falha(ErroOrdemServico.Conflito, mensagem);
}
