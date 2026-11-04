using OficinaMecanicaFase1.Domain.OrdensServico;

namespace OficinaMecanicaFase1.Application.OrdensServico;

public interface IConsultaGestaoOrdemServicoRepository
{
    Task<PaginaOrdensServico> ListarAsync(StatusOrdemServico? status, int pagina,
        int tamanhoPagina, CancellationToken cancelamento);

    Task<IReadOnlyList<TempoServicoConcluido>> ObterTemposConcluidosAsync(
        CancellationToken cancelamento);
}
