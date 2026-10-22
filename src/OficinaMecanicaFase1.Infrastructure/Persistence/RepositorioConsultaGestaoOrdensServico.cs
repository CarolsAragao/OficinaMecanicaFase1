using Microsoft.EntityFrameworkCore;
using OficinaMecanicaFase1.Application.OrdensServico;
using OficinaMecanicaFase1.Domain.OrdensServico;

namespace OficinaMecanicaFase1.Infrastructure.Persistence;

public sealed class RepositorioConsultaGestaoOrdensServico(OficinaDbContext db)
    : IConsultaGestaoOrdensServico
{
    public async Task<PaginaOrdensServico> ListarAsync(StatusOrdemServico? status,
        int pagina, int tamanhoPagina, CancellationToken cancelamento)
    {
        var consulta = db.OrdensServico.AsNoTracking();
        if (status is not null) consulta = consulta.Where(ordem => ordem.Status == status);

        var total = await consulta.CountAsync(cancelamento);
        var ordens = await consulta.OrderByDescending(ordem => ordem.CriadaEmUtc)
            .ThenByDescending(ordem => ordem.Numero)
            .Skip((pagina - 1) * tamanhoPagina).Take(tamanhoPagina)
            .Select(ordem => new ResumoOrdemServico(ordem.Numero, ordem.ClienteId,
                ordem.VeiculoId, ordem.Status, ordem.CriadaEmUtc, ordem.EntregueEmUtc,
                ordem.EncerradaPorRejeicaoEmUtc != null, ordem.ServicosSolicitados.Count,
                ordem.ServicosSolicitados.Count(servico => servico.ConcluidoEmUtc != null)))
            .ToArrayAsync(cancelamento);
        return new PaginaOrdensServico(pagina, tamanhoPagina, total, ordens);
    }

    public async Task<IReadOnlyList<TempoServicoConcluido>> ObterTemposConcluidosAsync(
        CancellationToken cancelamento) =>
        await (from servico in db.Set<ServicoSolicitado>().AsNoTracking()
               join catalogo in db.ServicosCatalogo.AsNoTracking()
                   on servico.ServicoCatalogoId equals catalogo.Id
               where servico.IniciadoEmUtc != null && servico.ConcluidoEmUtc != null
               select new TempoServicoConcluido(servico.ServicoCatalogoId, catalogo.Nome,
                   servico.IniciadoEmUtc!.Value, servico.ConcluidoEmUtc!.Value))
            .ToArrayAsync(cancelamento);
}
