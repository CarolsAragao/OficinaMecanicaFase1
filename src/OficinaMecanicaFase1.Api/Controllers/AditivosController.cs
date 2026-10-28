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
[Route("api/ordens-servico/{numero:long}/aditivos")]
[Tags("Aditivos")]
[Authorize(Policy = "EquipeInterna")]
public sealed class AditivosController : OficinaControllerBase
{
    private readonly GerenciadorAditivos _gerenciadorAditivos;

    public AditivosController(GerenciadorAditivos gerenciadorAditivos)
    {
        _gerenciadorAditivos = gerenciadorAditivos;
    }

    [HttpPost("")]
    [Authorize(Policy = "Mecanicos")]
    [ProducesResponseType(typeof(AditivoResponse), 201)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 409)]
    public async Task<IActionResult> CriarAsync([FromRoute] long numero, [FromBody] CriarAditivoRequest entrada, CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirst("sub")?.Value, out var mecanicoId)) return Forbid();
        var resultado = await _gerenciadorAditivos.CriarAsync(numero, mecanicoId, entrada.Tipo, entrada.Descricao,
            entrada.ServicoCatalogoId, entrada.ItemEstoqueId, entrada.Quantidade,
            entrada.OriginalPodeProsseguir, ct);
        return resultado.Erro == ErroOrdemServico.Nenhum
            ? Created($"/api/ordens-servico/{numero}/aditivos/{resultado.Valor!.Id}",
                AditivoResponse.De(resultado.Valor, numero)) : Erro(resultado);
    }

    [HttpGet("{aditivoId:guid}")]
    [ProducesResponseType(typeof(AditivoResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ObterInternoAsync([FromRoute] long numero, [FromRoute] Guid aditivoId, CancellationToken ct)
    {
        var aditivo = await _gerenciadorAditivos.ConsultarInternoAsync(numero, aditivoId, ct);
        return aditivo is null ? NotFound() : Ok(AditivoResponse.De(aditivo, numero));
    }

    [HttpPost("{aditivoId:guid}/reservas/tentativa")]
    [Authorize(Policy = "Estoquistas")]
    [ProducesResponseType(typeof(AditivoResponse), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 409)]
    public async Task<IActionResult> TentarReservaAsync([FromRoute] long numero, [FromRoute] Guid aditivoId, CancellationToken ct)
    {
        var resultado = await _gerenciadorAditivos.TentarReservaAsync(numero, aditivoId, ct);
        return resultado.Erro == ErroOrdemServico.Nenhum
            ? Ok(AditivoResponse.De(resultado.Valor!, numero)) : Erro(resultado);
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
