using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OficinaMecanicaFase1.Api.Contracts;
using OficinaMecanicaFase1.Application.OrdensServico;
using System.Security.Claims;

namespace OficinaMecanicaFase1.Api.Controllers;

[ApiController]
[Route("api/ordens-servico")]
[Tags("Ordens de serviço")]
[Authorize(Policy = "EquipeInterna")]
public sealed class ExecucaoOrdensServicoController : OficinaControllerBase
{
    private readonly GerenciadorReservas _gerenciadorReservas;
    private readonly GerenciadorExecucao _gerenciadorExecucao;

    public ExecucaoOrdensServicoController(GerenciadorReservas gerenciadorReservas, GerenciadorExecucao gerenciadorExecucao)
    {
        _gerenciadorReservas = gerenciadorReservas;
        _gerenciadorExecucao = gerenciadorExecucao;
    }

    [HttpPost("{numero:long}/reservas/tentativa", Name = "TentarReservaOrdemServico")]
    [EndpointSummary("Estoquista tenta novamente a reserva integral após corrigir a disponibilidade.")]
    [Authorize(Policy = "Estoquistas")]
    [ProducesResponseType(typeof(TentativaReservaResponse), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(ProblemDetails), 409)]
    public async Task<IActionResult> TentarReservaAsync([FromRoute] long numero, CancellationToken cancelamento)
    {
        var resultado = await _gerenciadorReservas.TentarReservarAsync(numero, cancelamento);
        return resultado.Situacao switch
        {
            SituacaoTentativaReserva.NaoEncontrada => NotFound(),
            SituacaoTentativaReserva.NaoPermitida => Problem(
                "A OS não possui orçamento aprovado aguardando reserva.", statusCode: 409),
            _ => Ok(TentativaReservaResponse.De(resultado))
        };
    }

    [HttpPost("{numero:long}/execucao/inicio", Name = "IniciarExecucaoOrdemServico")]
    [EndpointSummary("Mecânico inicia a execução após orçamento aprovado e reserva confirmada.")]
    [Authorize(Policy = "Mecanicos")]
    [ProducesResponseType(typeof(OrdemServicoResponse), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(ProblemDetails), 409)]
    public async Task<IActionResult> IniciarExecucaoAsync([FromRoute] long numero, CancellationToken cancelamento)
    {
        if (!Guid.TryParse(User.FindFirst("sub")?.Value, out var mecanicoId)) return Forbid();
        var resultado = await _gerenciadorExecucao.IniciarExecucaoAsync(numero, mecanicoId, cancelamento);
        return resultado.Erro == ErroOrdemServico.Nenhum
            ? Ok(OrdemServicoResponse.De(resultado.Valor!)) : ResponderErro(resultado);
    }

    [HttpPost("{numero:long}/servicos/{servicoId:guid}/inicio", Name = "IniciarServicoOrdemServico")]
    [EndpointSummary("Mecânico inicia um serviço autorizado no orçamento aprovado.")]
    [Authorize(Policy = "Mecanicos")]
    [ProducesResponseType(typeof(OrdemServicoResponse), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(ProblemDetails), 409)]
    public async Task<IActionResult> IniciarServicoAsync([FromRoute] long numero, [FromRoute] Guid servicoId, CancellationToken cancelamento)
    {
        if (!Guid.TryParse(User.FindFirst("sub")?.Value, out var mecanicoId)) return Forbid();
        var resultado = await _gerenciadorExecucao.IniciarServicoAsync(numero, servicoId, mecanicoId, cancelamento);
        return resultado.Erro == ErroOrdemServico.Nenhum
            ? Ok(OrdemServicoResponse.De(resultado.Valor!)) : ResponderErro(resultado);
    }

    [HttpPost("{numero:long}/servicos/{servicoId:guid}/conclusao", Name = "ConcluirServicoOrdemServico")]
    [EndpointSummary("Mecânico conclui um serviço iniciado; o último serviço finaliza a OS e libera saldo não consumido.")]
    [Authorize(Policy = "Mecanicos")]
    [ProducesResponseType(typeof(OrdemServicoResponse), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(ProblemDetails), 409)]
    public async Task<IActionResult> ConcluirServicoAsync([FromRoute] long numero, [FromRoute] Guid servicoId, CancellationToken cancelamento)
    {
        if (!Guid.TryParse(User.FindFirst("sub")?.Value, out var mecanicoId)) return Forbid();
        var resultado = await _gerenciadorExecucao.ConcluirServicoAsync(numero, servicoId, mecanicoId, cancelamento);
        return resultado.Erro == ErroOrdemServico.Nenhum
            ? Ok(OrdemServicoResponse.De(resultado.Valor!)) : ResponderErro(resultado);
    }

    [HttpPost("{numero:long}/consumos", Name = "RegistrarConsumoRealOrdemServico")]
    [EndpointSummary("Mecânico informa consumo real autorizado, que baixa estoque e reserva na mesma transação.")]
    [Authorize(Policy = "Mecanicos")]
    [ProducesResponseType(typeof(OrdemServicoResponse), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 409)]
    public async Task<IActionResult> RegistrarConsumoAsync([FromRoute] long numero, [FromBody] RegistrarConsumoRequest entrada, CancellationToken cancelamento)
    {
        if (!Guid.TryParse(User.FindFirst("sub")?.Value, out var mecanicoId)) return Forbid();
        var consumos = entrada.Consumos ?? [];
        var resultado = await _gerenciadorExecucao.RegistrarConsumoAsync(numero, mecanicoId,
            consumos.Select(item => new ConsumoSolicitado(item.ItemEstoqueId, item.Quantidade)).ToArray(), cancelamento);
        return resultado.Erro == ErroOrdemServico.Nenhum
            ? Ok(OrdemServicoResponse.De(resultado.Valor!)) : ResponderErro(resultado);
    }

    private IActionResult ResponderErro<T>(ResultadoOrdemServico<T> resultado) => resultado.Erro switch
    {
        ErroOrdemServico.Invalido => RespostaValidacao(new Dictionary<string, string[]>
        {
            [resultado.Campo ?? "ordemServico"] = [resultado.Mensagem ?? "Dados inválidos."]
        }),
        ErroOrdemServico.NaoEncontrado => NotFound(),
        ErroOrdemServico.Conflito => Problem(resultado.Mensagem, statusCode: 409),
        _ => Problem("Não foi possível processar a ordem de serviço.", statusCode: 500)
    };
}
