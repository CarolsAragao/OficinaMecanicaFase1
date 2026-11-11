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
public sealed class DiagnosticoOrcamentoController : OficinaControllerBase
{
    private readonly OrdemServicoService _ordemServicoService;
    private readonly DecisaoOrcamentoService _decisaoOrcamentoService;

    public DiagnosticoOrcamentoController(OrdemServicoService ordemServicoService, DecisaoOrcamentoService decisaoOrcamentoService)
    {
        _ordemServicoService = ordemServicoService;
        _decisaoOrcamentoService = decisaoOrcamentoService;
    }

    [HttpPost("{numero:long}/diagnostico/inicio", Name = "IniciarDiagnostico")]
    [EndpointSummary("Mecânico inicia o diagnóstico de uma OS recebida.")]
    [Authorize(Policy = "Mecanicos")]
    [ProducesResponseType(typeof(OrdemServicoResponse), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 409)]
    public async Task<IActionResult> IniciarDiagnosticoAsync([FromRoute] long numero, CancellationToken cancelamento)
    {
        if (!Guid.TryParse(User.FindFirst("sub")?.Value, out var mecanicoId)) return Forbid();
        var resultado = await _ordemServicoService.IniciarDiagnosticoAsync(numero, mecanicoId, cancelamento);
        return resultado.Erro == ErroOrdemServico.Nenhum
            ? Ok(OrdemServicoResponse.De(resultado.Valor!)) : ResponderErroOrdemServico(resultado);
    }

    [HttpPost("{numero:long}/diagnostico", Name = "RegistrarDiagnostico")]
    [EndpointSummary("Mecânico registra o diagnóstico iniciado.")]
    [Authorize(Policy = "Mecanicos")]
    [ProducesResponseType(typeof(OrdemServicoResponse), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 409)]
    public async Task<IActionResult> RegistrarDiagnosticoAsync([FromRoute] long numero, [FromBody] RegistrarDiagnosticoRequest entrada, CancellationToken cancelamento)
    {
        if (!Guid.TryParse(User.FindFirst("sub")?.Value, out var mecanicoId)) return Forbid();
        var resultado = await _ordemServicoService.RegistrarDiagnosticoAsync(numero, mecanicoId, entrada.Descricao, cancelamento);
        return resultado.Erro == ErroOrdemServico.Nenhum
            ? Ok(OrdemServicoResponse.De(resultado.Valor!)) : ResponderErroOrdemServico(resultado);
    }

    [HttpPost("{numero:long}/orcamento", Name = "GerarOrcamento")]
    [EndpointSummary("Sistema calcula serviços e itens após o diagnóstico e disponibiliza a proposta pela API.")]
    [Authorize(Policy = "Mecanicos")]
    [ProducesResponseType(typeof(OrcamentoResponse), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 409)]
    public async Task<IActionResult> GerarOrcamentoAsync([FromRoute] long numero, CancellationToken cancelamento)
    {
        var resultado = await _ordemServicoService.GerarOrcamentoAsync(numero, cancelamento);
        return resultado.Erro == ErroOrdemServico.Nenhum
            ? Ok(OrcamentoResponse.De(resultado.Valor!, numero)) : ResponderErroOrdemServico(resultado);
    }

    [HttpGet("{numero:long}/orcamento", Name = "ObterOrcamento")]
    [ProducesResponseType(typeof(OrcamentoResponse), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> ObterOrcamentoAsync([FromRoute] long numero, CancellationToken cancelamento)
    {
        var orcamento = await _ordemServicoService.ObterOrcamentoAsync(numero, cancelamento);
        return orcamento is null ? NotFound() : Ok(OrcamentoResponse.De(orcamento, numero));
    }

    [HttpPost("{numero:long}/codigo-acesso/emissao", Name = "EmitirCodigoAcessoOrdemServico")]
    [EndpointSummary("Atendente emite o código individual para decisão do cliente; reemissão exige motivo e invalida o código anterior.")]
    [Authorize(Policy = "Atendentes")]
    [ProducesResponseType(typeof(CodigoAcessoResponse), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 409)]
    public async Task<IActionResult> EmitirCodigoAcessoAsync([FromRoute] long numero, [FromBody] EmitirCodigoAcessoRequest entrada, CancellationToken cancelamento)
    {
        if (!Guid.TryParse(User.FindFirst("sub")?.Value, out var atendenteId)) return Forbid();
        HttpContext.Response.Headers.CacheControl = "no-store";
        var resultado = await _decisaoOrcamentoService.EmitirCodigoAsync(numero, atendenteId, entrada.Motivo, cancelamento);
        return resultado.Erro == ErroOrdemServico.Nenhum
            ? Ok(new CodigoAcessoResponse(numero, resultado.Valor!.Codigo,
                resultado.Valor.NumeroEmissao, resultado.Valor.EmitidoEmUtc))
            : ResponderErroOrdemServico(resultado);
    }

}
