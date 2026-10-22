using OficinaMecanicaFase1.Domain.OrdensServico;
using Xunit;

namespace OficinaMecanicaFase1.Infrastructure.Tests;

public sealed class InvariantesDominioTests
{
    private static readonly DateTimeOffset Inicio = new(2026, 10, 22, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void RejeicaoEncerraSemCriarStatusExtraEImpedeReservaEExecucao()
    {
        var ordem = CriarComOrcamentoDisponivel([Guid.NewGuid()]);

        Assert.Throws<ArgumentException>(() => ordem.EncerrarPorRejeicao(" ", Inicio.AddMinutes(3)));
        ordem.EncerrarPorRejeicao("Cliente recusou", Inicio.AddMinutes(3));

        Assert.Equal(StatusOrdemServico.AguardandoAprovacao, ordem.Status);
        Assert.Equal("Cliente recusou", ordem.MotivoEncerramento);
        Assert.NotNull(ordem.EncerradaPorRejeicaoEmUtc);
        Assert.False(ordem.PodeIniciarExecucao);
        Assert.Throws<InvalidOperationException>(() => ordem.RegistrarAprovacaoOrcamento(Inicio.AddMinutes(4)));
        Assert.Throws<InvalidOperationException>(() => ordem.ConfirmarReserva(Inicio.AddMinutes(4)));
        Assert.Throws<InvalidOperationException>(() => ordem.IniciarExecucao(Guid.NewGuid(), Inicio.AddMinutes(4)));
        Assert.Contains(ordem.Marcos, marco => marco.Descricao == "OS encerrada por rejeição");
    }

    [Fact]
    public void ReservaConfirmadaPermiteExecucaoEEntregaExigeTodosOsServicosConcluidos()
    {
        var servicos = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var ordem = CriarComOrcamentoDisponivel(servicos);
        var mecanico = Guid.NewGuid();
        var atendente = Guid.NewGuid();

        Assert.Throws<InvalidOperationException>(() => ordem.ConfirmarReserva(Inicio.AddMinutes(3)));
        ordem.RegistrarAprovacaoOrcamento(Inicio.AddMinutes(3));
        ordem.RecusarReserva(Inicio.AddMinutes(4));
        Assert.False(ordem.PodeIniciarExecucao);
        ordem.ConfirmarReserva(Inicio.AddMinutes(5));
        Assert.True(ordem.PodeIniciarExecucao);
        ordem.IniciarExecucao(mecanico, Inicio.AddMinutes(6));
        Assert.Throws<InvalidOperationException>(() => ordem.RegistrarEntrega(atendente, Inicio.AddMinutes(7)));

        ordem.IniciarServico(servicos[0], mecanico, Inicio.AddMinutes(7));
        ordem.IniciarServico(servicos[1], mecanico, Inicio.AddMinutes(7));
        Assert.Throws<InvalidOperationException>(() => ordem.ConcluirServico(servicos[0], mecanico, Inicio.AddMinutes(6)));
        ordem.ConcluirServico(servicos[0], mecanico, Inicio.AddMinutes(8));
        Assert.Equal(StatusOrdemServico.EmExecucao, ordem.Status);
        ordem.ConcluirServico(servicos[1], mecanico, Inicio.AddMinutes(9));
        Assert.Equal(StatusOrdemServico.Finalizada, ordem.Status);
        ordem.RegistrarEntrega(atendente, Inicio.AddMinutes(10));
        Assert.Equal(StatusOrdemServico.Entregue, ordem.Status);
        Assert.Throws<InvalidOperationException>(() => ordem.RegistrarEntrega(atendente, Inicio.AddMinutes(11)));
    }

    [Fact]
    public void ItemNecessarioRecusaQuantidadeOuSaldoInvalido()
    {
        foreach (var (quantidade, saldo) in new[]
        {
            (0m, 0m), (-1m, 0m), (1.0001m, 0m), (1m, -1m)
        })
            Assert.Throws<ArgumentException>(() => OrdemServico.Criar(Guid.NewGuid(), Guid.NewGuid(),
                [Guid.NewGuid()], [(Guid.NewGuid(), quantidade, saldo)], Inicio));
    }

    private static OrdemServico CriarComOrcamentoDisponivel(Guid[] servicos)
    {
        var ordem = OrdemServico.Criar(Guid.NewGuid(), Guid.NewGuid(), servicos, [], Inicio);
        var mecanico = Guid.NewGuid();
        ordem.IniciarDiagnostico(mecanico, Inicio.AddMinutes(1));
        ordem.RegistrarDiagnostico(mecanico, "Serviço necessário", Inicio.AddMinutes(2));
        ordem.DisponibilizarOrcamento(Inicio.AddMinutes(3));
        return ordem;
    }
}
