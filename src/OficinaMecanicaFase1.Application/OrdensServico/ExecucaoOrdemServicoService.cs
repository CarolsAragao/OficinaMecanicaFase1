using OficinaMecanicaFase1.Domain.Estoque;
using OficinaMecanicaFase1.Domain.OrdensServico;
using OficinaMecanicaFase1.Domain.Orcamentos;

namespace OficinaMecanicaFase1.Application.OrdensServico;

public sealed class ExecucaoOrdemServicoService(IExecucaoOrdemServicoRepository repositorio, TimeProvider tempo)
{
    public Task<ResultadoOrdemServico<OrdemServico>> IniciarExecucaoAsync(
        long numero, Guid mecanicoId, CancellationToken cancelamento)
    {
        var agora = tempo.GetUtcNow();
        return repositorio.ExecutarAsync(numero, async (operacoes, ordem, orcamento) =>
        {
            if (orcamento?.Decisao != DecisaoOrcamento.Aprovado ||
                !ordem.ServicosSolicitados.All(servico => orcamento.Linhas.Any(linha =>
                    linha.Tipo == TipoLinhaOrcamento.Servico && linha.ReferenciaId == servico.ServicoCatalogoId)))
                return Invalido("A execução exige serviços autorizados em um orçamento aprovado.");

            var reservas = await operacoes.ObterReservasAtivasAsync(ordem.Id, cancelamento);
            if (ordem.ItensNecessarios.Any(necessario => !orcamento.Linhas.Any(linha =>
                    linha.Tipo == TipoLinhaOrcamento.Item && linha.ReferenciaId == necessario.ItemEstoqueId) ||
                !reservas.Any(reserva => reserva.ItemEstoqueId == necessario.ItemEstoqueId &&
                    reserva.QuantidadeRestante >= necessario.Quantidade)))
                return Invalido("A execução exige reserva integral de todos os itens autorizados.");

            operacoes.Adicionar(ordem.IniciarExecucao(mecanicoId, agora));
            return ResultadoOrdemServico<OrdemServico>.Sucesso(ordem);
        }, cancelamento);
    }

    public Task<ResultadoOrdemServico<OrdemServico>> IniciarServicoAsync(
        long numero, Guid servicoId, Guid mecanicoId, CancellationToken cancelamento)
    {
        var agora = tempo.GetUtcNow();
        return repositorio.ExecutarAsync(numero, async (operacoes, ordem, orcamento) =>
        {
            var aditivos = await operacoes.ObterAditivosAsync(ordem.Id, cancelamento);
            if (orcamento?.Decisao != DecisaoOrcamento.Aprovado ||
                (!orcamento.Linhas.Any(linha => linha.Tipo == TipoLinhaOrcamento.Servico && linha.ReferenciaId == servicoId) &&
                 !aditivos.Any(a => a.ServicoCatalogoId == servicoId &&
                     a.Situacao == SituacaoAditivo.Aprovado && a.SituacaoReserva == SituacaoReservaAditivo.Confirmada)))
                return Invalido("O serviço não está autorizado pelo orçamento aprovado.");
            if (aditivos.Any(a => !a.OriginalPodeProsseguir &&
                (a.Situacao == SituacaoAditivo.Pendente || a.Situacao == SituacaoAditivo.Rejeitado ||
                 (a.Situacao == SituacaoAditivo.Aprovado && a.SituacaoReserva != SituacaoReservaAditivo.Confirmada))))
                return Invalido("A execução está impedida por um aditivo não autorizado.");
            operacoes.Adicionar(ordem.IniciarServico(servicoId, mecanicoId, agora));
            return ResultadoOrdemServico<OrdemServico>.Sucesso(ordem);
        }, cancelamento);
    }

    public Task<ResultadoOrdemServico<OrdemServico>> ConcluirServicoAsync(
        long numero, Guid servicoId, Guid mecanicoId, CancellationToken cancelamento)
    {
        var agora = tempo.GetUtcNow();
        return repositorio.ExecutarAsync(numero, async (operacoes, ordem, orcamento) =>
        {
            var aditivos = await operacoes.ObterAditivosAsync(ordem.Id, cancelamento);
            if (orcamento?.Decisao != DecisaoOrcamento.Aprovado ||
                (!orcamento.Linhas.Any(linha => linha.Tipo == TipoLinhaOrcamento.Servico && linha.ReferenciaId == servicoId) &&
                 !aditivos.Any(a => a.ServicoCatalogoId == servicoId &&
                     a.Situacao == SituacaoAditivo.Aprovado && a.SituacaoReserva == SituacaoReservaAditivo.Confirmada)))
                return Invalido("O serviço não está autorizado pelo orçamento aprovado.");
            var ultimoServico = ordem.ServicosSolicitados.Count(s => s.ConcluidoEmUtc is null) == 1;
            if (aditivos.Any(a => ((a.Situacao == SituacaoAditivo.Pendente ||
                  (a.Situacao == SituacaoAditivo.Aprovado && a.SituacaoReserva != SituacaoReservaAditivo.Confirmada)) &&
                  (!a.OriginalPodeProsseguir || ultimoServico)) ||
                 (a.Situacao == SituacaoAditivo.Rejeitado && !a.OriginalPodeProsseguir)))
                return Invalido("Há aditivo pendente, sem reserva ou impedimento registrado.");
            var reservas = await operacoes.ObterReservasAtivasAsync(ordem.Id, cancelamento);
            var ids = reservas.Select(reserva => reserva.ItemEstoqueId).Distinct().OrderBy(id => id).ToArray();
            var itens = await operacoes.BloquearItensAsync(ids, cancelamento);
            operacoes.Adicionar(ordem.ConcluirServico(servicoId, mecanicoId, agora));
            if (ordem.Status == StatusOrdemServico.Finalizada)
            {
                foreach (var reserva in reservas)
                {
                    if (reserva.QuantidadeRestante > 0)
                        itens[reserva.ItemEstoqueId].LiberarReserva(reserva.QuantidadeRestante);
                    reserva.Liberar(agora);
                }
                operacoes.Adicionar(ordem.Historico.Last());
            }
            return ResultadoOrdemServico<OrdemServico>.Sucesso(ordem);
        }, cancelamento);
    }

    public Task<ResultadoOrdemServico<OrdemServico>> RegistrarConsumoAsync(
        long numero, Guid mecanicoId, IReadOnlyCollection<ConsumoSolicitado> consumos,
        CancellationToken cancelamento)
    {
        var agora = tempo.GetUtcNow();
        return repositorio.ExecutarAsync(numero, async (operacoes, ordem, orcamento) =>
        {
            var aditivos = await operacoes.ObterAditivosAsync(ordem.Id, cancelamento);
            if (ordem.Status != StatusOrdemServico.EmExecucao)
                return Invalido("O consumo só pode ser registrado durante a execução.");
            if (aditivos.Any(a => !a.OriginalPodeProsseguir &&
                (a.Situacao == SituacaoAditivo.Pendente || a.Situacao == SituacaoAditivo.Rejeitado ||
                 (a.Situacao == SituacaoAditivo.Aprovado && a.SituacaoReserva != SituacaoReservaAditivo.Confirmada))))
                return Invalido("O consumo está impedido por um aditivo não autorizado.");
            if (consumos.Count == 0 || consumos.Any(consumo => consumo.ItemEstoqueId == Guid.Empty) ||
                consumos.Select(consumo => consumo.ItemEstoqueId).Distinct().Count() != consumos.Count)
                return Invalido("Informe itens de consumo válidos, sem repetições.");
            if (orcamento?.Decisao != DecisaoOrcamento.Aprovado)
                return Invalido("O consumo exige orçamento aprovado.");
            foreach (var consumo in consumos)
                if (!ordem.AutorizaItem(consumo.ItemEstoqueId) ||
                    (!orcamento.Linhas.Any(linha => linha.Tipo == TipoLinhaOrcamento.Item &&
                        linha.ReferenciaId == consumo.ItemEstoqueId) &&
                     !aditivos.Any(a => a.ItemEstoqueId == consumo.ItemEstoqueId && a.Situacao == SituacaoAditivo.Aprovado &&
                         a.SituacaoReserva == SituacaoReservaAditivo.Confirmada)))
                    return Invalido("O item não está autorizado pelo orçamento aprovado.");

            var itens = await operacoes.BloquearItensAsync(consumos.Select(consumo => consumo.ItemEstoqueId)
                .OrderBy(id => id).ToArray(), cancelamento);
            var reservas = await operacoes.ObterReservasAtivasAsync(ordem.Id, cancelamento);
            foreach (var consumo in consumos)
            {
                if (!itens.TryGetValue(consumo.ItemEstoqueId, out var item) ||
                    reservas.SingleOrDefault(reserva => reserva.ItemEstoqueId == consumo.ItemEstoqueId) is not { } reserva)
                    return Invalido("Não existe reserva ativa para um dos itens informados.");
                if (!item.QuantidadeValida(consumo.Quantidade))
                    return Invalido("A quantidade consumida não é válida para o tipo do item.");
                reserva.RegistrarConsumo(consumo.Quantidade);
                item.RegistrarConsumoReservado(consumo.Quantidade);
                operacoes.Adicionar(ConsumoItemOrdemServico.Criar(
                    ordem.Id, item.Id, mecanicoId, consumo.Quantidade, agora));
            }
            return ResultadoOrdemServico<OrdemServico>.Sucesso(ordem);
        }, cancelamento);
    }

    private static ResultadoOrdemServico<OrdemServico> Invalido(string mensagem) =>
        ResultadoOrdemServico<OrdemServico>.Falha(ErroOrdemServico.Invalido, mensagem, "ordemServico");
}
