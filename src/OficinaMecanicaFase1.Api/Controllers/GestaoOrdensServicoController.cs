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
    private readonly GestaoOrdemServicoService _consultaGestaoOrdensServico;
    private readonly OrdemServicoService _ordemServicoService;

    public GestaoOrdensServicoController(GestaoOrdemServicoService consultaGestaoOrdensServico, OrdemServicoService ordemServicoService)
    {
        _consultaGestaoOrdensServico = consultaGestaoOrdensServico;
        _ordemServicoService = ordemServicoService;
    }

    [HttpGet("", Name = "ListarOrdensServicoAdministrativas")]
    [EndpointSummary("Lista a fila operacional de ordens de serviço")]
    [EndpointDescription(
        "A fila prioriza EmExecucao, AguardandoAprovacao, EmDiagnostico e Recebida, nessa ordem. " +
        "Dentro de cada status, as OS mais antigas aparecem primeiro. Em caso de empate, o menor número da OS vem primeiro.\n\n" +
        "Use `status` para filtrar a fila. A paginação começa em 1, com 20 registros por padrão e até 100 por página. " +
        "O total considera os filtros antes da paginação.\n\n" +
        "OS Finalizada e Entregue ficam no histórico e podem ser consultadas em " +
        "`GET /api/administracao/ordens-servico/{numero}`. Filtrar a fila por um desses status retorna uma lista vazia.")]
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
        var ordem = await _ordemServicoService.ObterAsync(numero, cancelamento);
        return ordem is null ? NotFound() : Ok(OrdemServicoResponse.De(ordem));
    }
}
