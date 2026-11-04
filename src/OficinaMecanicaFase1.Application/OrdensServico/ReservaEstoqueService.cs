using OficinaMecanicaFase1.Domain.Estoque;
using OficinaMecanicaFase1.Domain.OrdensServico;

namespace OficinaMecanicaFase1.Application.OrdensServico;

public sealed class ReservaEstoqueService(IReservaEstoqueRepository repositorio, TimeProvider tempo) : ITentativaReservaEstoque
{
    public Task<ResultadoTentativaReserva> TentarReservarAsync(long numero, CancellationToken cancelamento)
    {
        var agora = tempo.GetUtcNow();
        return repositorio.ExecutarAsync(numero, async (operacoes, ordem) =>
        {
            if (ordem.SituacaoReserva == SituacaoReservaOrdemServico.Confirmada)
                return new ResultadoTentativaReserva(numero, SituacaoTentativaReserva.JaConfirmada, []);

            var aprovado = await operacoes.TemOrcamentoAprovadoAsync(ordem.Id, cancelamento);
            if (!aprovado || ordem.Status != StatusOrdemServico.AguardandoAprovacao ||
                ordem.OrcamentoAprovadoEmUtc is null || ordem.EncerradaPorRejeicaoEmUtc is not null)
                return new ResultadoTentativaReserva(numero, SituacaoTentativaReserva.NaoPermitida, []);

            var ids = ordem.ItensNecessarios.Select(item => item.ItemEstoqueId).OrderBy(id => id).ToArray();
            var itens = await operacoes.BloquearItensAsync(ids, cancelamento);

            var faltantes = ordem.ItensNecessarios
                .Where(necessario => !itens.TryGetValue(necessario.ItemEstoqueId, out var item) ||
                    !item.Ativo || !item.QuantidadeValida(necessario.Quantidade) ||
                    item.SaldoDisponivel < necessario.Quantidade)
                .Select(necessario =>
                {
                    itens.TryGetValue(necessario.ItemEstoqueId, out var item);
                    return new ItemReservaInsuficiente(necessario.ItemEstoqueId,
                        item?.Nome ?? "Item indisponível", necessario.Quantidade, item?.SaldoDisponivel ?? 0m);
                }).ToArray();

            var pendentes = await operacoes.ObterAvisosPendentesAsync(ordem.Id, cancelamento);
            foreach (var aviso in pendentes) aviso.Resolver(agora);

            SituacaoTentativaReserva situacao;
            if (faltantes.Length > 0)
            {
                foreach (var faltante in faltantes)
                    operacoes.Adicionar(new AvisoReservaRecusada(ordem.Id,
                        faltante.ItemEstoqueId, faltante.QuantidadeSolicitada,
                        faltante.SaldoDisponivel, agora));
                operacoes.Adicionar(ordem.RecusarReserva(agora));
                situacao = SituacaoTentativaReserva.Recusada;
            }
            else
            {
                foreach (var necessario in ordem.ItensNecessarios)
                {
                    itens[necessario.ItemEstoqueId].Reservar(necessario.Quantidade);
                    operacoes.Adicionar(ReservaItemOrdemServico.Criar(
                        ordem.Id, necessario.ItemEstoqueId, necessario.Quantidade, agora));
                }
                operacoes.Adicionar(ordem.ConfirmarReserva(agora));
                situacao = SituacaoTentativaReserva.Confirmada;
            }

            return new ResultadoTentativaReserva(numero, situacao, faltantes);
        }, cancelamento);
    }

    public Task<IReadOnlyCollection<AvisoReservaEstoque>> ListarAvisosAsync(
        bool somentePendentes, CancellationToken cancelamento) =>
        repositorio.ListarAvisosAsync(somentePendentes, cancelamento);
}
