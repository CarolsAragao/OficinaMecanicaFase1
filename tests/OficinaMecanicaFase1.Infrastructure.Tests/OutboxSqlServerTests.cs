using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using OficinaMecanicaFase1.Application.OrdensServico;
using OficinaMecanicaFase1.Domain.Cadastros;
using OficinaMecanicaFase1.Domain.Catalogo;
using OficinaMecanicaFase1.Domain.OrdensServico;
using OficinaMecanicaFase1.Infrastructure.Notifications;
using OficinaMecanicaFase1.Infrastructure.Persistence;
using Testcontainers.MsSql;
using Xunit;

namespace OficinaMecanicaFase1.Infrastructure.Tests;

[Trait("Category", "Integration")]
public sealed class OutboxSqlServerTests(OutboxSqlServerFixture sql) : IClassFixture<OutboxSqlServerFixture>
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FalhaAoSalvarAvisoReverteTambemAOsESeuHistorico(bool ordemJaExiste)
    {
        var opcoes = await sql.CriarBancoAsync();
        var entrada = await PrepararAsync(opcoes, ordemJaExiste);
        var falha = new FalhaAoSalvarAviso(transitoria: false);
        var comFalha = new DbContextOptionsBuilder<OficinaDbContext>(opcoes).AddInterceptors(falha).Options;

        await Assert.ThrowsAsync<InvalidOperationException>(() => AlterarAsync(comFalha, entrada));

        await using var db = new OficinaDbContext(opcoes);
        Assert.Empty(await db.NotificacoesStatusOrdensServico.ToArrayAsync());
        if (ordemJaExiste)
        {
            var ordem = await db.OrdensServico.Include(os => os.Historico).SingleAsync();
            Assert.Equal(StatusOrdemServico.Recebida, ordem.Status);
            Assert.Single(ordem.Historico);
        }
        else
        {
            Assert.Empty(await db.OrdensServico.ToArrayAsync());
            Assert.Empty(await db.Set<RegistroHistoricoOrdemServico>().ToArrayAsync());
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FalhaTransitoriaRefazOperacaoComNovoContextoESalvaUmUnicoAviso(bool ordemJaExiste)
    {
        var opcoes = await sql.CriarBancoAsync();
        var entrada = await PrepararAsync(opcoes, ordemJaExiste);
        var falha = new FalhaAoSalvarAviso(transitoria: true);
        var comFalha = new DbContextOptionsBuilder<OficinaDbContext>(opcoes).AddInterceptors(falha).Options;

        var resultado = await AlterarAsync(comFalha, entrada);

        Assert.Equal(ErroOrdemServico.Nenhum, resultado.Erro);
        Assert.Equal(2, falha.Contextos.Count);
        Assert.Equal(2, falha.Contextos.Distinct().Count());
        await using var db = new OficinaDbContext(opcoes);
        var ordem = await db.OrdensServico.Include(os => os.Historico).SingleAsync();
        var aviso = await db.NotificacoesStatusOrdensServico.SingleAsync();
        Assert.Equal(ordemJaExiste ? StatusOrdemServico.EmDiagnostico : StatusOrdemServico.Recebida, ordem.Status);
        Assert.Equal(ordemJaExiste ? 2 : 1, ordem.Historico.Count);
        Assert.Equal(ordem.Id, aviso.OrdemServicoId);
        Assert.Equal(ordem.Numero, aviso.NumeroOrdemServico);
        Assert.Equal(ordem.Status, aviso.Status);
        Assert.Contains(ordem.Historico, marco => marco.Id == aviso.MarcoHistoricoId);
    }

    [Fact]
    public async Task ConfirmacaoPerdidaDepoisDoCommitNaoCriaOutraOs()
    {
        var opcoes = await sql.CriarBancoAsync();
        var entrada = await PrepararAsync(opcoes, ordemJaExiste: false);
        var falha = new FalhaAposCommit();
        var comFalha = new DbContextOptionsBuilder<OficinaDbContext>(opcoes).AddInterceptors(falha).Options;

        var resultado = await AlterarAsync(comFalha, entrada);

        Assert.Equal(ErroOrdemServico.Nenhum, resultado.Erro);
        Assert.True(falha.Disparada);
        await using var db = new OficinaDbContext(opcoes);
        var ordem = await db.OrdensServico.SingleAsync();
        Assert.Equal(ordem.Id, resultado.Valor!.Id);
        Assert.Single(await db.NotificacoesStatusOrdensServico.ToArrayAsync());
        Assert.Single(await db.Set<RegistroHistoricoOrdemServico>().ToArrayAsync());
    }

    [Fact]
    public async Task DisputaConcorrentePermiteUmaUnicaReservaDoAviso()
    {
        var opcoes = await sql.CriarBancoAsync();
        var avisoId = await PrepararAvisoAsync(opcoes);
        var agora = DateTimeOffset.UtcNow;
        await using var dbA = new OficinaDbContext(opcoes);
        await using var dbB = new OficinaDbContext(opcoes);
        var resultados = await Task.WhenAll(
            new NotificacaoStatusRepository(dbA).TentarAssumirAsync(avisoId, Guid.NewGuid(), agora,
                agora.AddMinutes(1), CancellationToken.None),
            new NotificacaoStatusRepository(dbB).TentarAssumirAsync(avisoId, Guid.NewGuid(), agora,
                agora.AddMinutes(1), CancellationToken.None));

        Assert.Single(resultados, assumida => assumida);
    }

    [Fact]
    public async Task TentativaAntigaNaoConcluiNemReagendaAvisoReassumido()
    {
        var opcoes = await sql.CriarBancoAsync();
        var avisoId = await PrepararAvisoAsync(opcoes);
        var agora = DateTimeOffset.UtcNow;
        var tentativaAntiga = Guid.NewGuid();
        var tentativaNova = Guid.NewGuid();
        await using var dbA = new OficinaDbContext(opcoes);
        await using var dbB = new OficinaDbContext(opcoes);
        var repoA = new NotificacaoStatusRepository(dbA);
        var repoB = new NotificacaoStatusRepository(dbB);
        Assert.True(await repoA.TentarAssumirAsync(avisoId, tentativaAntiga, agora,
            agora.AddSeconds(60), CancellationToken.None));
        Assert.True(await repoB.TentarAssumirAsync(avisoId, tentativaNova, agora.AddSeconds(61),
            agora.AddSeconds(121), CancellationToken.None));

        Assert.False(await repoA.MarcarEnviadaAsync(avisoId, tentativaAntiga,
            agora.AddSeconds(62), CancellationToken.None));
        Assert.False(await repoA.ReagendarAsync(avisoId, tentativaAntiga,
            agora.AddSeconds(92), "Falha da tentativa antiga", CancellationToken.None));
        var atual = await dbB.NotificacoesStatusOrdensServico.AsNoTracking().SingleAsync();
        Assert.Equal(SituacaoNotificacaoStatusOrdemServico.EmProcessamento, atual.Situacao);
        Assert.Equal(tentativaNova, atual.ProcessamentoId);
        Assert.Equal(0, atual.Tentativas);
        Assert.Null(atual.UltimoErro);
        Assert.True(await repoB.MarcarEnviadaAsync(avisoId, tentativaNova,
            agora.AddSeconds(63), CancellationToken.None));
    }

    [Fact]
    public async Task DoisProcessadoresNaoEnviamSimultaneamenteOMesmoAviso()
    {
        var opcoes = await sql.CriarBancoAsync();
        await PrepararAvisoAsync(opcoes);
        var notificador = new NotificadorBloqueado();
        await using var dbA = new OficinaDbContext(opcoes);
        await using var dbB = new OficinaDbContext(opcoes);
        using var limite = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var primeira = CriarProcessador(dbA, notificador, TimeProvider.System).ProcessarAsync(limite.Token);
        try
        {
            await notificador.Iniciou.Task.WaitAsync(limite.Token);
            await CriarProcessador(dbB, notificador, TimeProvider.System).ProcessarAsync(limite.Token);
            Assert.Equal(1, notificador.Envios);
        }
        finally
        {
            notificador.Liberar.TrySetResult();
            await primeira;
        }
        Assert.Equal(SituacaoNotificacaoStatusOrdemServico.Enviada,
            (await dbA.NotificacoesStatusOrdensServico.AsNoTracking().SingleAsync()).Situacao);
    }

    [Fact]
    public async Task FalhaSmtpPersisteReagendamentoEProximaTentativaEnviaSemAlterarOs()
    {
        var opcoes = await sql.CriarBancoAsync();
        await PrepararAvisoAsync(opcoes);
        var tempo = new RelogioControlado(DateTimeOffset.UtcNow.AddSeconds(1));
        await using (var db = new OficinaDbContext(opcoes))
            await CriarProcessador(db, new NotificadorFalha(), tempo).ProcessarAsync(CancellationToken.None);

        await using (var db = new OficinaDbContext(opcoes))
        {
            var aviso = await db.NotificacoesStatusOrdensServico.AsNoTracking().SingleAsync();
            Assert.Equal(SituacaoNotificacaoStatusOrdemServico.Pendente, aviso.Situacao);
            Assert.Equal(1, aviso.Tentativas);
            Assert.Equal("SMTP indisponível.", aviso.UltimoErro);
            Assert.Equal(tempo.GetUtcNow().AddSeconds(30), aviso.ProximaTentativaEmUtc);
            Assert.Null(aviso.ProcessamentoId);
            Assert.Null(aviso.EnviadaEmUtc);
            Assert.Equal(StatusOrdemServico.Recebida, (await db.OrdensServico.SingleAsync()).Status);
        }

        tempo.Avancar(TimeSpan.FromSeconds(31));
        var notificador = new NotificadorSucesso();
        await using (var db = new OficinaDbContext(opcoes))
        {
            var processador = CriarProcessador(db, notificador, tempo);
            await processador.ProcessarAsync(CancellationToken.None);
            await processador.ProcessarAsync(CancellationToken.None);
            var aviso = await db.NotificacoesStatusOrdensServico.AsNoTracking().SingleAsync();
            Assert.Equal(SituacaoNotificacaoStatusOrdemServico.Enviada, aviso.Situacao);
            Assert.NotNull(aviso.EnviadaEmUtc);
            Assert.Null(aviso.UltimoErro);
            Assert.Equal(1, aviso.Tentativas);
            Assert.Equal(1, notificador.Envios);
        }
    }

    [Fact]
    public async Task IndiceUnicoImpedeSegundoAvisoParaOMesmoMarco()
    {
        var opcoes = await sql.CriarBancoAsync();
        await PrepararAvisoAsync(opcoes);
        await using var db = new OficinaDbContext(opcoes);
        var ordem = await db.OrdensServico.Include(os => os.Historico).SingleAsync();
        db.NotificacoesStatusOrdensServico.Add(FabricaNotificacaoStatusOrdemServico.CriarPendente(
            ordem, ordem.Historico.Single(), "cliente@exemplo.com", DateTimeOffset.UtcNow));

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        await using var verificacao = new OficinaDbContext(opcoes);
        Assert.Single(await verificacao.NotificacoesStatusOrdensServico.ToArrayAsync());
        Assert.False(verificacao.Database.HasPendingModelChanges());
    }

    private static ProcessadorNotificacoesStatus CriarProcessador(OficinaDbContext db,
        INotificadorStatusOrdemServico notificador, TimeProvider tempo) =>
        new(new NotificacaoStatusRepository(db), notificador, tempo,
            new NotificacoesStatusConfiguracao(), NullLogger<ProcessadorNotificacoesStatus>.Instance);

    private static async Task<EntradaTeste> PrepararAsync(DbContextOptions<OficinaDbContext> opcoes, bool ordemJaExiste)
    {
        await using var db = new OficinaDbContext(opcoes);
        var cliente = new Cliente("Cliente de teste", "52998224725", "cliente@exemplo.com");
        var veiculo = new Veiculo(cliente.Id, "ABC1234", "Fiat", "Uno", 2020);
        var servico = new ServicoCatalogo("Revisão", null, 100m);
        db.AddRange(cliente, veiculo, servico);
        var ordem = ordemJaExiste
            ? OrdemServico.Criar(cliente.Id, veiculo.Id, [servico.Id], [], DateTimeOffset.UtcNow)
            : null;
        if (ordem is not null) db.OrdensServico.Add(ordem);
        await db.SaveChangesAsync();
        return new EntradaTeste(cliente.Id, veiculo.Id, servico.Id, ordem?.Numero);
    }

    private static async Task<ResultadoOrdemServico<OrdemServico>> AlterarAsync(
        DbContextOptions<OficinaDbContext> opcoes, EntradaTeste entrada)
    {
        await using var db = new OficinaDbContext(opcoes);
        var servico = new OrdemServicoService(new OrdemServicoRepository(db, opcoes), TimeProvider.System);
        return entrada.Numero is { } numero
            ? await servico.IniciarDiagnosticoAsync(numero, Guid.NewGuid(), CancellationToken.None)
            : await servico.CriarAsync(new AbrirOrdemServicoEntrada(
                entrada.ClienteId, entrada.VeiculoId, [entrada.ServicoId], []), CancellationToken.None);
    }

    private static async Task<Guid> PrepararAvisoAsync(DbContextOptions<OficinaDbContext> opcoes)
    {
        var entrada = await PrepararAsync(opcoes, ordemJaExiste: false);
        var resultado = await AlterarAsync(opcoes, entrada);
        Assert.Equal(ErroOrdemServico.Nenhum, resultado.Erro);
        await using var db = new OficinaDbContext(opcoes);
        return await db.NotificacoesStatusOrdensServico.Select(aviso => aviso.Id).SingleAsync();
    }

    private sealed record EntradaTeste(Guid ClienteId, Guid VeiculoId, Guid ServicoId, long? Numero);

    private sealed class FalhaAoSalvarAviso(bool transitoria) : SaveChangesInterceptor
    {
        public List<Guid> Contextos { get; } = [];
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (eventData.Context is { } db && db.ChangeTracker.Entries<NotificacaoStatusOrdemServico>()
                .Any(entrada => entrada.State == EntityState.Added))
            {
                Contextos.Add(db.ContextId.InstanceId);
                if (!transitoria) throw new InvalidOperationException("Falha simulada ao salvar aviso.");
                if (Contextos.Count == 1) throw new TimeoutException("Falha transitória simulada após salvar a OS.");
            }
            return ValueTask.FromResult(result);
        }
    }

    private sealed class FalhaAposCommit : DbTransactionInterceptor
    {
        public bool Disparada { get; private set; }
        public override Task TransactionCommittedAsync(DbTransaction transaction,
            TransactionEndEventData eventData, CancellationToken cancellationToken = default)
        {
            if (!Disparada && eventData.Context is { } db &&
                db.ChangeTracker.Entries<NotificacaoStatusOrdemServico>().Any())
            {
                Disparada = true;
                throw new TimeoutException("Confirmação perdida após o commit.");
            }
            return Task.CompletedTask;
        }
    }

    private sealed class RelogioControlado(DateTimeOffset agora) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => agora;
        public void Avancar(TimeSpan intervalo) => agora += intervalo;
    }

    private sealed class NotificadorFalha : INotificadorStatusOrdemServico
    {
        public Task EnviarAsync(MensagemStatusOrdemServico mensagem, CancellationToken cancelamento) =>
            Task.FromException(new InvalidOperationException("SMTP indisponível."));
    }

    private sealed class NotificadorSucesso : INotificadorStatusOrdemServico
    {
        public int Envios { get; private set; }
        public Task EnviarAsync(MensagemStatusOrdemServico mensagem, CancellationToken cancelamento)
        {
            Envios++;
            return Task.CompletedTask;
        }
    }

    private sealed class NotificadorBloqueado : INotificadorStatusOrdemServico
    {
        private int envios;
        public int Envios => envios;
        public TaskCompletionSource Iniciou { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Liberar { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public async Task EnviarAsync(MensagemStatusOrdemServico mensagem, CancellationToken cancelamento)
        {
            Interlocked.Increment(ref envios);
            Iniciou.TrySetResult();
            await Liberar.Task.WaitAsync(cancelamento);
        }
    }
}

public sealed class OutboxSqlServerFixture : IAsyncLifetime
{
    private readonly MsSqlContainer sql = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-CU14-ubuntu-22.04").Build();
    public Task InitializeAsync() => sql.StartAsync();
    public async Task DisposeAsync() => await sql.DisposeAsync();

    public async Task<DbContextOptions<OficinaDbContext>> CriarBancoAsync()
    {
        var conexao = new SqlConnectionStringBuilder(sql.GetConnectionString())
        {
            InitialCatalog = $"OficinaOutbox_{Guid.NewGuid():N}"
        };
        var opcoes = new DbContextOptionsBuilder<OficinaDbContext>()
            .UseSqlServer(conexao.ConnectionString, configuracao => configuracao.EnableRetryOnFailure())
            .Options;
        await using var db = new OficinaDbContext(opcoes);
        await db.Database.MigrateAsync();
        return opcoes;
    }
}
