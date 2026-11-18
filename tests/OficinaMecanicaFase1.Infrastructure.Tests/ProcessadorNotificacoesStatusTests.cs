using Microsoft.Extensions.Logging.Abstractions;
using OficinaMecanicaFase1.Application.OrdensServico;
using OficinaMecanicaFase1.Domain.OrdensServico;
using OficinaMecanicaFase1.Infrastructure.Notifications;
using Xunit;

namespace OficinaMecanicaFase1.Infrastructure.Tests;

public sealed class ProcessadorNotificacoesStatusTests
{
    [Fact]
    public async Task CadaAvisoRecebePrazoContadoAPartirDaSuaPropriaTentativa()
    {
        var tempo = new RelogioControlado();
        var repositorio = new RepositorioCaptura([NovoAviso(tempo.GetUtcNow()), NovoAviso(tempo.GetUtcNow())]);
        var notificador = new NotificadorAcao(_ => tempo.Avancar(TimeSpan.FromMinutes(2)));
        var processador = Criar(repositorio, notificador, tempo);

        await processador.ProcessarAsync(CancellationToken.None);

        Assert.Equal(2, repositorio.Reservas.Count);
        Assert.Equal(TimeSpan.FromMinutes(2), repositorio.Reservas[1].Inicio - repositorio.Reservas[0].Inicio);
        Assert.All(repositorio.Reservas, reserva => Assert.Equal(TimeSpan.FromSeconds(60), reserva.Fim - reserva.Inicio));
        Assert.Equal(2, repositorio.Reservas.Select(reserva => reserva.ProcessamentoId).Distinct().Count());
        Assert.Equal(repositorio.Reservas.Select(reserva => reserva.ProcessamentoId), repositorio.Conclusoes);
    }

    [Fact]
    public async Task NaoEnviaQuandoOutroProcessadorJaAssumiuOAviso()
    {
        var tempo = new RelogioControlado();
        var repositorio = new RepositorioCaptura([NovoAviso(tempo.GetUtcNow())]) { PodeAssumir = false };
        var notificador = new NotificadorAcao(_ => { });

        await Criar(repositorio, notificador, tempo).ProcessarAsync(CancellationToken.None);

        Assert.Equal(0, notificador.Envios);
        Assert.Empty(repositorio.Conclusoes);
    }

    [Fact]
    public async Task NaoIniciaEnvioSeAReservaJaExpirouAoRetornarDoBanco()
    {
        var tempo = new RelogioControlado();
        var repositorio = new RepositorioCaptura([NovoAviso(tempo.GetUtcNow())])
        {
            AoAssumir = () => tempo.Avancar(TimeSpan.FromMinutes(2))
        };
        var notificador = new NotificadorAcao(_ => { });

        await Criar(repositorio, notificador, tempo).ProcessarAsync(CancellationToken.None);

        Assert.Equal(0, notificador.Envios);
        Assert.Empty(repositorio.Conclusoes);
        Assert.Single(repositorio.Reagendamentos);
    }

    [Fact]
    public async Task EnvioQueExcedeOPrazoDaReservaEInterrompidoEReagendado()
    {
        var tempo = new RelogioControlado();
        var repositorio = new RepositorioCaptura([NovoAviso(tempo.GetUtcNow())]);
        var processador = Criar(repositorio, new NotificadorQueAguardaCancelamento(), tempo, 1);
        using var limiteDoTeste = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        await processador.ProcessarAsync(limiteDoTeste.Token);

        Assert.Empty(repositorio.Conclusoes);
        Assert.Equal(repositorio.Reservas.Single().ProcessamentoId, Assert.Single(repositorio.Reagendamentos));
    }

    private static ProcessadorNotificacoesStatus Criar(INotificacaoStatusRepository repositorio,
        INotificadorStatusOrdemServico notificador, TimeProvider tempo, int prazo = 60) =>
        new(repositorio, notificador, tempo,
            new NotificacoesStatusConfiguracao { ProcessamentoExpiraEmSegundos = prazo },
            NullLogger<ProcessadorNotificacoesStatus>.Instance);

    private static NotificacaoStatusOrdemServico NovoAviso(DateTimeOffset agora) =>
        NotificacaoStatusOrdemServico.CriarPendente(Guid.NewGuid(), Guid.NewGuid(), 1,
            StatusOrdemServico.Recebida, "cliente@exemplo.com", "Assunto", "Corpo", agora);

    private sealed class RelogioControlado : TimeProvider
    {
        private DateTimeOffset agora = new(2026, 11, 17, 12, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => agora;
        public void Avancar(TimeSpan intervalo) => agora += intervalo;
    }

    private sealed class NotificadorAcao(Action<MensagemStatusOrdemServico> acao) : INotificadorStatusOrdemServico
    {
        public int Envios { get; private set; }
        public Task EnviarAsync(MensagemStatusOrdemServico mensagem, CancellationToken cancelamento)
        {
            Envios++;
            acao(mensagem);
            return Task.CompletedTask;
        }
    }

    private sealed class NotificadorQueAguardaCancelamento : INotificadorStatusOrdemServico
    {
        public Task EnviarAsync(MensagemStatusOrdemServico mensagem, CancellationToken cancelamento) =>
            Task.Delay(Timeout.Infinite, cancelamento);
    }

    private sealed class RepositorioCaptura(IReadOnlyCollection<NotificacaoStatusOrdemServico> avisos)
        : INotificacaoStatusRepository
    {
        public bool PodeAssumir { get; init; } = true;
        public Action? AoAssumir { get; init; }
        public List<(Guid ProcessamentoId, DateTimeOffset Inicio, DateTimeOffset Fim)> Reservas { get; } = [];
        public List<Guid> Conclusoes { get; } = [];
        public List<Guid> Reagendamentos { get; } = [];
        public Task<IReadOnlyCollection<NotificacaoStatusOrdemServico>> ListarParaProcessamentoAsync(
            DateTimeOffset agora, int quantidadeMaxima, CancellationToken cancelamento) => Task.FromResult(avisos);
        public Task<bool> TentarAssumirAsync(Guid notificacaoId, Guid processamentoId, DateTimeOffset agora,
            DateTimeOffset processandoAteUtc, CancellationToken cancelamento)
        {
            Reservas.Add((processamentoId, agora, processandoAteUtc));
            AoAssumir?.Invoke();
            return Task.FromResult(PodeAssumir);
        }
        public Task<bool> MarcarEnviadaAsync(Guid notificacaoId, Guid processamentoId,
            DateTimeOffset enviadaEmUtc, CancellationToken cancelamento)
        {
            Conclusoes.Add(processamentoId);
            return Task.FromResult(true);
        }
        public Task<bool> ReagendarAsync(Guid notificacaoId, Guid processamentoId,
            DateTimeOffset proximaTentativaEmUtc, string erro, CancellationToken cancelamento)
        {
            Reagendamentos.Add(processamentoId);
            return Task.FromResult(true);
        }
    }
}
