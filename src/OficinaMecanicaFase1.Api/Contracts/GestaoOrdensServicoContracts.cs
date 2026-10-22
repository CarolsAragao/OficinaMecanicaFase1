using OficinaMecanicaFase1.Application.OrdensServico;

namespace OficinaMecanicaFase1.Api.Contracts;

public sealed record ResumoOrdemServicoResponse(long Numero, Guid ClienteId, Guid VeiculoId,
    string Status, DateTimeOffset CriadaEmUtc, DateTimeOffset? EntregueEmUtc,
    bool EncerradaPorRejeicao, int TotalServicos, int ServicosConcluidos)
{
    public static ResumoOrdemServicoResponse De(ResumoOrdemServico ordem) => new(
        ordem.Numero, ordem.ClienteId, ordem.VeiculoId, ordem.Status.ToString(),
        ordem.CriadaEmUtc, ordem.EntregueEmUtc, ordem.EncerradaPorRejeicao,
        ordem.TotalServicos, ordem.ServicosConcluidos);
}

public sealed record PaginaOrdensServicoResponse(int Pagina, int TamanhoPagina, int Total,
    ResumoOrdemServicoResponse[] Ordens)
{
    public static PaginaOrdensServicoResponse De(PaginaOrdensServico pagina) => new(
        pagina.Pagina, pagina.TamanhoPagina, pagina.Total,
        pagina.Ordens.Select(ResumoOrdemServicoResponse.De).ToArray());
}

public sealed record IndicadorTempoMedioResponse(string Definicao, bool IncluiPausasEImpedimentos,
    string Unidade, int QuantidadeTotalConcluida, double? MediaGeralSegundos,
    MediaTempoServico[] Servicos)
{
    public static IndicadorTempoMedioResponse De(IndicadorTempoMedio indicador) => new(
        "Tempo decorrido entre início e conclusão de cada serviço concluído; serviços incompletos não entram na média.",
        true, "segundos", indicador.QuantidadeTotalConcluida,
        indicador.MediaGeralSegundos, indicador.Servicos.ToArray());
}
