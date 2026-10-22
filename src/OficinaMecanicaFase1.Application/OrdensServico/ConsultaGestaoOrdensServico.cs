using OficinaMecanicaFase1.Domain.OrdensServico;

namespace OficinaMecanicaFase1.Application.OrdensServico;

public sealed record ResumoOrdemServico(long Numero, Guid ClienteId, Guid VeiculoId,
    StatusOrdemServico Status, DateTimeOffset CriadaEmUtc, DateTimeOffset? EntregueEmUtc,
    bool EncerradaPorRejeicao, int TotalServicos, int ServicosConcluidos);

public sealed record PaginaOrdensServico(int Pagina, int TamanhoPagina, int Total,
    IReadOnlyList<ResumoOrdemServico> Ordens);

public sealed record TempoServicoConcluido(Guid ServicoCatalogoId, string NomeServico,
    DateTimeOffset IniciadoEmUtc, DateTimeOffset ConcluidoEmUtc);

public sealed record MediaTempoServico(Guid ServicoCatalogoId, string NomeServico,
    int QuantidadeConcluida, double MediaSegundos);

public sealed record IndicadorTempoMedio(int QuantidadeTotalConcluida,
    double? MediaGeralSegundos, IReadOnlyList<MediaTempoServico> Servicos);

public interface IConsultaGestaoOrdensServico
{
    Task<PaginaOrdensServico> ListarAsync(StatusOrdemServico? status, int pagina,
        int tamanhoPagina, CancellationToken cancelamento);

    Task<IReadOnlyList<TempoServicoConcluido>> ObterTemposConcluidosAsync(
        CancellationToken cancelamento);
}

public sealed class ConsultaGestaoOrdensServico(IConsultaGestaoOrdensServico repositorio)
{
    public Task<PaginaOrdensServico> ListarAsync(StatusOrdemServico? status, int pagina,
        int tamanhoPagina, CancellationToken cancelamento) =>
        repositorio.ListarAsync(status, pagina, tamanhoPagina, cancelamento);

    public async Task<IndicadorTempoMedio> ObterIndicadorAsync(CancellationToken cancelamento)
    {
        var tempos = (await repositorio.ObterTemposConcluidosAsync(cancelamento))
            .Where(servico => servico.ConcluidoEmUtc >= servico.IniciadoEmUtc)
            .ToArray();
        var porServico = tempos.GroupBy(servico => new { servico.ServicoCatalogoId, servico.NomeServico })
            .Select(grupo => new MediaTempoServico(grupo.Key.ServicoCatalogoId,
                grupo.Key.NomeServico, grupo.Count(),
                Math.Round(grupo.Average(servico =>
                    (servico.ConcluidoEmUtc - servico.IniciadoEmUtc).TotalSeconds), 2)))
            .OrderBy(servico => servico.NomeServico).ThenBy(servico => servico.ServicoCatalogoId)
            .ToArray();

        return new IndicadorTempoMedio(tempos.Length,
            tempos.Length == 0 ? null : Math.Round(tempos.Average(servico =>
                (servico.ConcluidoEmUtc - servico.IniciadoEmUtc).TotalSeconds), 2), porServico);
    }
}
