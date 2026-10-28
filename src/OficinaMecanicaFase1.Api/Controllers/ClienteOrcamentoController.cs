using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OficinaMecanicaFase1.Api.Contracts;
using OficinaMecanicaFase1.Application.OrdensServico;

namespace OficinaMecanicaFase1.Api.Controllers;

[ApiController]
[Route("api/cliente/ordens-servico")]
[Tags("Cliente - orçamento")]
[AllowAnonymous]
public sealed class ClienteOrcamentoController : OficinaControllerBase
{
    private readonly GerenciadorDecisaoOrcamento _gerenciadorDecisaoOrcamento;
    private readonly GerenciadorOrdensServico _gerenciadorOrdensServico;

    public ClienteOrcamentoController(GerenciadorDecisaoOrcamento gerenciadorDecisaoOrcamento, GerenciadorOrdensServico gerenciadorOrdensServico)
    {
        _gerenciadorDecisaoOrcamento = gerenciadorDecisaoOrcamento;
        _gerenciadorOrdensServico = gerenciadorOrdensServico;
    }

    [HttpPost("{numero:long}/orcamento/consulta", Name = "ConsultarOrcamentoCliente")]
    [EndpointSummary("Cliente consulta a proposta pelo número da OS e CPF/CNPJ.")]
    [ProducesResponseType(typeof(OrcamentoResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ConsultarAsync([FromRoute] long numero, [FromBody] ConsultarOrcamentoClienteRequest entrada, CancellationToken cancelamento)
    {
        HttpContext.Response.Headers.CacheControl = "no-store";
        var proposta = await _gerenciadorDecisaoOrcamento.ConsultarPropostaAsync(numero, entrada.Documento, cancelamento);
        return proposta is null ? NotFound() : Ok(OrcamentoResponse.De(proposta, numero));
    }

    [HttpPost("{numero:long}/orcamento/decisao", Name = "DecidirOrcamentoCliente")]
    [EndpointSummary("Cliente aprova ou rejeita com número da OS, CPF/CNPJ e código individual.")]
    [ProducesResponseType(typeof(OrcamentoResponse), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 409)]
    public async Task<IActionResult> DecidirAsync([FromRoute] long numero, [FromBody] DecidirOrcamentoClienteRequest entrada, CancellationToken cancelamento)
    {
        HttpContext.Response.Headers.CacheControl = "no-store";
        var resultado = await _gerenciadorDecisaoOrcamento.DecidirAsync(numero, entrada.Documento, entrada.CodigoAcesso,
            entrada.Aprovar, entrada.Motivo, cancelamento);
        return resultado.Erro switch
        {
            ErroOrdemServico.Nenhum => Ok(OrcamentoResponse.De(resultado.Valor!, numero)),
            ErroOrdemServico.NaoEncontrado => NotFound(),
            ErroOrdemServico.Invalido => RespostaValidacao(new Dictionary<string, string[]>
            {
                [resultado.Campo ?? "decisao"] = [resultado.Mensagem ?? "Dados inválidos."]
            }),
            ErroOrdemServico.Conflito => Problem(resultado.Mensagem, statusCode: 409),
            _ => Problem("Não foi possível registrar a decisão.", statusCode: 500)
        };
    }

    [HttpPost("{numero:long}/andamento/consulta", Name = "ConsultarAndamentoCliente")]
    [EndpointSummary("Consulta pública por número da OS e CPF/CNPJ, com número, status, última atualização e marcos.")]
    [ProducesResponseType(typeof(AndamentoOrdemServicoResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ConsultarAndamentoAsync([FromRoute] long numero, [FromBody] ConsultarOrcamentoClienteRequest entrada, CancellationToken cancelamento)
    {
        HttpContext.Response.Headers.CacheControl = "no-store";
        var ordem = await _gerenciadorOrdensServico.ObterAndamentoClienteAsync(numero, entrada.Documento, cancelamento);
        return ordem is null ? NotFound() : Ok(AndamentoOrdemServicoResponse.De(ordem));
    }
}
