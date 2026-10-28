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
public sealed class OrdensServicoController : OficinaControllerBase
{
    private readonly GerenciadorOrdensServico _gerenciadorOrdensServico;

    public OrdensServicoController(GerenciadorOrdensServico gerenciadorOrdensServico)
    {
        _gerenciadorOrdensServico = gerenciadorOrdensServico;
    }

    [HttpPost("", Name = "AbrirOrdemServico")]
    [EndpointSummary("Abre OS Recebida com veículo próprio, serviços ativos e itens necessários; consulta disponibilidade sem reservar.")]
    [Authorize(Policy = "Cadastros")]
    [ProducesResponseType(typeof(OrdemServicoResponse), 201)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 409)]
    public async Task<IActionResult> CriarAsync([FromBody] CriarOrdemServicoRequest entrada, CancellationToken cancelamento)
    {
        var servicos = entrada.ServicosIds ?? [];
        var itens = entrada.ItensNecessarios ?? [];
        var resultado = await _gerenciadorOrdensServico.CriarAsync(
            entrada.ClienteId, entrada.VeiculoId, servicos,
            itens.Select(item => (item.ItemEstoqueId, item.Quantidade)).ToArray(), cancelamento);
        return resultado.Erro == ErroOrdemServico.Nenhum
            ? Created($"/api/ordens-servico/{resultado.Valor!.Numero}", OrdemServicoResponse.De(resultado.Valor))
            : ResponderErro(resultado);
    }

    [HttpGet("{numero:long}", Name = "ObterOrdemServico")]
    [ProducesResponseType(typeof(OrdemServicoResponse), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> ObterAsync([FromRoute] long numero, CancellationToken cancelamento)
    {
        var ordem = await _gerenciadorOrdensServico.ObterAsync(numero, cancelamento);
        return ordem is null ? NotFound() : Ok(OrdemServicoResponse.De(ordem));
    }

    [HttpPost("{numero:long}/entrega", Name = "RegistrarEntregaOrdemServico")]
    [EndpointSummary("Atendente registra a entrega de uma OS finalizada.")]
    [Authorize(Policy = "Atendentes")]
    [ProducesResponseType(typeof(OrdemServicoResponse), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 409)]
    public async Task<IActionResult> EntregarAsync([FromRoute] long numero, CancellationToken cancelamento)
    {
        if (!Guid.TryParse(User.FindFirst("sub")?.Value, out var atendenteId)) return Forbid();
        var resultado = await _gerenciadorOrdensServico.EntregarAsync(numero, atendenteId, cancelamento);
        return resultado.Erro == ErroOrdemServico.Nenhum
            ? Ok(OrdemServicoResponse.De(resultado.Valor!)) : ResponderErro(resultado);
    }

    [HttpPost("{numero:long}/servicos", Name = "AdicionarServicoSolicitado")]
    [EndpointSummary("Adiciona um serviço ativo antes da aprovação do orçamento.")]
    [Authorize(Policy = "AtendentesEMecanicos")]
    [ProducesResponseType(typeof(OrdemServicoResponse), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 409)]
    public async Task<IActionResult> AdicionarServicoAsync([FromRoute] long numero, [FromBody] AdicionarServicoSolicitadoRequest entrada, CancellationToken cancelamento)
    {
        var resultado = await _gerenciadorOrdensServico.AdicionarServicoAsync(numero, entrada.ServicoCatalogoId, cancelamento);
        return resultado.Erro == ErroOrdemServico.Nenhum
            ? Ok(OrdemServicoResponse.De(resultado.Valor!)) : ResponderErro(resultado);
    }

    [HttpPost("{numero:long}/itens", Name = "AdicionarItemNecessario")]
    [EndpointSummary("Adiciona item ativo e consulta saldo disponível, sem reservar nem baixar estoque.")]
    [Authorize(Policy = "AtendentesEMecanicos")]
    [ProducesResponseType(typeof(OrdemServicoResponse), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 409)]
    public async Task<IActionResult> AdicionarItemAsync([FromRoute] long numero, [FromBody] AdicionarItemNecessarioRequest entrada, CancellationToken cancelamento)
    {
        var resultado = await _gerenciadorOrdensServico.AdicionarItemAsync(
            numero, entrada.ItemEstoqueId, entrada.Quantidade, cancelamento);
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
