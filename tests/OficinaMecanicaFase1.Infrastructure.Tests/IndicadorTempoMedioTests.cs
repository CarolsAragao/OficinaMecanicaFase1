using OficinaMecanicaFase1.Application.OrdensServico;
using OficinaMecanicaFase1.Domain.OrdensServico;
using Xunit;

namespace OficinaMecanicaFase1.Infrastructure.Tests;

public sealed class IndicadorTempoMedioTests
{
    [Fact]
    public async Task AgrupaServicosConcluidosEIgnoraInstantesIncoerentes()
    {
        var freio = Guid.NewGuid();
        var revisao = Guid.NewGuid();
        var inicio = new DateTimeOffset(2026, 10, 22, 12, 0, 0, TimeSpan.Zero);
        var consulta = new ConsultaGestaoOrdensServico(new RepositorioEmMemoria(
        [
            new(freio, "Freio", inicio, inicio.AddSeconds(10)),
            new(freio, "Freio", inicio, inicio.AddSeconds(20)),
            new(revisao, "Revisão", inicio, inicio.AddSeconds(30)),
            new(revisao, "Revisão", inicio, inicio.AddSeconds(-1))
        ]));

        var indicador = await consulta.ObterIndicadorAsync(CancellationToken.None);

        Assert.Equal(3, indicador.QuantidadeTotalConcluida);
        Assert.Equal(20, indicador.MediaGeralSegundos);
        Assert.Equal(2, indicador.Servicos.Count);
        Assert.Contains(indicador.Servicos, servico => servico.ServicoCatalogoId == freio &&
            servico.QuantidadeConcluida == 2 && servico.MediaSegundos == 15);
        Assert.Contains(indicador.Servicos, servico => servico.ServicoCatalogoId == revisao &&
            servico.QuantidadeConcluida == 1 && servico.MediaSegundos == 30);
    }

    [Fact]
    public async Task SemServicosConcluidosRetornaMediaNula()
    {
        var consulta = new ConsultaGestaoOrdensServico(new RepositorioEmMemoria([]));

        var indicador = await consulta.ObterIndicadorAsync(CancellationToken.None);

        Assert.Equal(0, indicador.QuantidadeTotalConcluida);
        Assert.Null(indicador.MediaGeralSegundos);
        Assert.Empty(indicador.Servicos);
    }

    private sealed class RepositorioEmMemoria(IReadOnlyList<TempoServicoConcluido> tempos)
        : IConsultaGestaoOrdensServico
    {
        public Task<PaginaOrdensServico> ListarAsync(StatusOrdemServico? status,
            int pagina, int tamanhoPagina, CancellationToken cancelamento) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<TempoServicoConcluido>> ObterTemposConcluidosAsync(
            CancellationToken cancelamento) => Task.FromResult(tempos);
    }
}
