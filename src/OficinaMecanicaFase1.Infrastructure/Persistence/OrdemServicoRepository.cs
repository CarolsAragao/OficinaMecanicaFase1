using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using OficinaMecanicaFase1.Application.Cadastros;
using OficinaMecanicaFase1.Application.OrdensServico;
using OficinaMecanicaFase1.Domain.Catalogo;
using OficinaMecanicaFase1.Domain.Estoque;
using OficinaMecanicaFase1.Domain.OrdensServico;
using OficinaMecanicaFase1.Domain.Orcamentos;

namespace OficinaMecanicaFase1.Infrastructure.Persistence;

public sealed class OrdemServicoRepository(
    OficinaDbContext db, DbContextOptions<OficinaDbContext> opcoes) : IOrdemServicoRepository
{
    private Guid? marcoConfirmacaoId;

    public async Task<ResultadoOrdemServico<T>> ExecutarComNotificacaoAsync<T>(
        Func<IOrdemServicoRepository, Task<ResultadoOrdemServico<T>>> operacao, CancellationToken cancelamento)
    {
        var confirmacao = new ConfirmacaoTransacao<T>();
        try
        {
            var estrategia = db.Database.CreateExecutionStrategy();
            return await estrategia.ExecuteAsync(confirmacao,
                async (_, estado, token) =>
                {
                    // Cada repetição refaz consultas e alterações, sem entidades já aceitas pelo contexto anterior.
                    estado.Resultado = null;
                    estado.MarcoId = null;
                    await using var tentativa = new OficinaDbContext(opcoes);
                    await using var transacao = await tentativa.Database.BeginTransactionAsync(token);
                    var repositorio = new OrdemServicoRepository(tentativa, opcoes);
                    var resultado = await operacao(repositorio);
                    if (resultado.Erro != ErroOrdemServico.Nenhum) return resultado;

                    estado.Resultado = resultado;
                    estado.MarcoId = repositorio.marcoConfirmacaoId;
                    await transacao.CommitAsync(token);
                    return resultado;
                },
                async (_, estado, token) =>
                {
                    // Se a conexão caiu durante o commit, o marco confirma que a transação já foi gravada.
                    await using var verificacao = new OficinaDbContext(opcoes);
                    var gravada = estado.MarcoId is { } marcoId && estado.Resultado is not null &&
                        await verificacao.Set<RegistroHistoricoOrdemServico>().AsNoTracking()
                            .AnyAsync(marco => marco.Id == marcoId, token);
                    return new ExecutionResult<ResultadoOrdemServico<T>>(gravada, estado.Resultado!);
                }, cancelamento);
        }
        catch (ConflitoPersistenciaException)
        {
            return ResultadoOrdemServico<T>.Falha(ErroOrdemServico.Conflito,
                "A ordem foi alterada ou um cadastro relacionado deixou de estar disponível. Consulte e tente novamente.");
        }
    }

    private sealed class ConfirmacaoTransacao<T>
    {
        public Guid? MarcoId { get; set; }
        public ResultadoOrdemServico<T>? Resultado { get; set; }
    }

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
            .Include(ordem => ordem.Historico)
            .FirstOrDefaultAsync(ordem => ordem.Numero == numero, cancelamento);

    public Task<OrdemServico?> ObterDoClienteAsync(long numero, string documento, CancellationToken cancelamento) =>
        db.OrdensServico.FirstOrDefaultAsync(ordem => ordem.Numero == numero &&
            db.Clientes.Any(cliente => cliente.Id == ordem.ClienteId && cliente.Documento == documento), cancelamento);

    public Task<OrdemServico?> ObterAndamentoDoClienteAsync(long numero, string documento, CancellationToken cancelamento) =>
        db.OrdensServico.AsNoTracking().Include(ordem => ordem.Historico)
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

    public Task<string?> ObterEmailClienteAsync(Guid clienteId, CancellationToken cancelamento) =>
        db.Clientes.AsNoTracking().Where(cliente => cliente.Id == clienteId)
            .Select(cliente => cliente.Email).SingleOrDefaultAsync(cancelamento);

    public void Adicionar(OrdemServico ordem)
    {
        db.OrdensServico.Add(ordem);
        marcoConfirmacaoId = ordem.Historico.Last().Id;
    }
    public void Adicionar(ServicoSolicitado servico) => db.Set<ServicoSolicitado>().Add(servico);
    public void Adicionar(ItemNecessario item) => db.Set<ItemNecessario>().Add(item);
    public void Adicionar(RegistroHistoricoOrdemServico marco)
    {
        db.Set<RegistroHistoricoOrdemServico>().Add(marco);
        marcoConfirmacaoId = marco.Id;
    }
    public void Adicionar(Orcamento orcamento) => db.Orcamentos.Add(orcamento);
    public void Adicionar(EmissaoCodigoAcesso emissao) => db.Set<EmissaoCodigoAcesso>().Add(emissao);
    public void Adicionar(NotificacaoStatusOrdemServico notificacao) => db.Set<NotificacaoStatusOrdemServico>().Add(notificacao);

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

    public async Task SalvarComNotificacaoAsync(Action registrarNotificacao, CancellationToken cancelamento)
    {
        if (db.Database.CurrentTransaction is null)
            throw new InvalidOperationException("Execute a alteração da OS e seu aviso na mesma transação.");

        await SalvarAsync(cancelamento); // A abertura recebe o número gerado pelo SQL Server.
        registrarNotificacao();
        await SalvarAsync(cancelamento);
    }
}
