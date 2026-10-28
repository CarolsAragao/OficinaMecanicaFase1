using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OficinaMecanicaFase1.Api.OrdensServico;
using OficinaMecanicaFase1.Application.OrdensServico;
using OficinaMecanicaFase1.Domain.Orcamentos;
using OficinaMecanicaFase1.Infrastructure.Persistence;
using System.Security.Claims;

namespace OficinaMecanicaFase1.Api.Controllers;

[ApiController]
[Route("api/cliente/ordens-servico/{numero:long}/aditivos")]
[Tags("Cliente - aditivos")]
[AllowAnonymous]
public sealed class ClienteAditivosController : OficinaControllerBase
{
    private readonly GerenciadorAditivos _gerenciadorAditivos;

    public ClienteAditivosController(GerenciadorAditivos gerenciadorAditivos)
    {
        _gerenciadorAditivos = gerenciadorAditivos;
    }

    [HttpPost("{aditivoId:guid}/consulta")]
    [ProducesResponseType(typeof(AditivoResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ConsultarClienteAsync([FromRoute] long numero, [FromRoute] Guid aditivoId, [FromBody] ConsultarAditivoRequest entrada, CancellationToken ct)
    {
        HttpContext.Response.Headers.CacheControl = "no-store";
        var aditivo = await _gerenciadorAditivos.ConsultarClienteAsync(numero, aditivoId, entrada.Documento, ct);
        return aditivo is null ? NotFound() : Ok(AditivoResponse.De(aditivo, numero));
    }

    [HttpPost("{aditivoId:guid}/decisao")]
    [ProducesResponseType(typeof(AditivoResponse), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 409)]
    public async Task<IActionResult> DecidirAsync([FromRoute] long numero, [FromRoute] Guid aditivoId, [FromBody] DecidirAditivoRequest entrada, CancellationToken ct)
    {
        HttpContext.Response.Headers.CacheControl = "no-store";
        var resultado = await _gerenciadorAditivos.DecidirAsync(numero, aditivoId, entrada.Documento,
            entrada.CodigoAcesso, entrada.Aprovar, entrada.Motivo, ct);
        return resultado.Erro == ErroOrdemServico.Nenhum
            ? Ok(AditivoResponse.De((await _gerenciadorAditivos.ConsultarInternoAsync(numero, aditivoId, ct))!, numero))
            : Erro(resultado);
    }

    private IActionResult Erro(ResultadoOrdemServico<Aditivo> resultado) => resultado.Erro switch
    {
        ErroOrdemServico.NaoEncontrado => NotFound(),
        ErroOrdemServico.Invalido => RespostaValidacao(new Dictionary<string, string[]>
            { [resultado.Campo ?? "aditivo"] = [resultado.Mensagem ?? "Dados inválidos."] }),
        ErroOrdemServico.Conflito => Problem(resultado.Mensagem, statusCode: 409),
        _ => Problem(statusCode: 500)
    };
}
