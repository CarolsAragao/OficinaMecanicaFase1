using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OficinaMecanicaFase1.Api.Contracts;
using OficinaMecanicaFase1.Application.OrdensServico;
using OficinaMecanicaFase1.Domain.OrdensServico;

namespace OficinaMecanicaFase1.Api.Controllers;

[ApiController]
[Route("api/administracao/ordens-servico")]
[Tags("Gestão de ordens de serviço")]
[Authorize(Policy = "Administradores")]
public sealed class GestaoOrdensServicoController : OficinaControllerBase
{
    private readonly ConsultaGestaoOrdensServico _consultaGestaoOrdensServico;
    private readonly GerenciadorOrdensServico _gerenciadorOrdensServico;

    public GestaoOrdensServicoController(ConsultaGestaoOrdensServico consultaGestaoOrdensServico, GerenciadorOrdensServico gerenciadorOrdensServico)
    {
        _consultaGestaoOrdensServico = consultaGestaoOrdensServico;
        _gerenciadorOrdensServico = gerenciadorOrdensServico;
    }

    [HttpGet("", Name = "ListarOrdensServicoAdministrativas")]
    [EndpointSummary("Lista OS por status, da mais recente para a mais antiga, com paginação.")]
    [ProducesResponseType(typeof(PaginaOrdensServicoResponse), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    public async Task<IActionResult> ListarAsync([FromQuery] string? status, [FromQuery] int? pagina, [FromQuery] int? tamanhoPagina, CancellationToken cancelamento)
    {
        StatusOrdemServico? filtro = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<StatusOrdemServico>(status, true, out var convertido) ||
                !Enum.IsDefined(convertido))
                return RespostaValidacao(new Dictionary<string, string[]>
                {
                    ["status"] = ["Informe um dos seis status oficiais da OS."]
                });
            filtro = convertido;
        }

        if (pagina is < 1 || pagina is > 21474836 || tamanhoPagina is < 1 or > 100)
            return RespostaValidacao(new Dictionary<string, string[]>
            {
                ["paginacao"] = ["Página deve estar entre 1 e 21474836; tamanhoPagina entre 1 e 100."]
            });

        var resultado = await _consultaGestaoOrdensServico.ListarAsync(filtro, pagina ?? 1, tamanhoPagina ?? 20, cancelamento);
        return Ok(PaginaOrdensServicoResponse.De(resultado));
    }

    [HttpGet("indicadores/tempo-medio", Name = "ObterTempoMedioPorServico")]
    [EndpointSummary("Média do tempo decorrido por serviço concluído, incluindo pausas e impedimentos.")]
    [ProducesResponseType(typeof(IndicadorTempoMedioResponse), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> ObterTempoMedioAsync(CancellationToken cancelamento) =>
        Ok(IndicadorTempoMedioResponse.De(
            await _consultaGestaoOrdensServico.ObterIndicadorAsync(cancelamento)));

    [HttpGet("{numero:long}", Name = "ObterOrdemServicoAdministrativa")]
    [EndpointSummary("Detalha uma OS para o Administrador.")]
    [ProducesResponseType(typeof(OrdemServicoResponse), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> ObterAsync([FromRoute] long numero, CancellationToken cancelamento)
    {
        var ordem = await _gerenciadorOrdensServico.ObterAsync(numero, cancelamento);
        return ordem is null ? NotFound() : Ok(OrdemServicoResponse.De(ordem));
    }
}
