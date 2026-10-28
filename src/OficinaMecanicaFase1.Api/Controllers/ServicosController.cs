using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OficinaMecanicaFase1.Api.Contracts;
using OficinaMecanicaFase1.Application.Catalogo;
using System.Security.Claims;

namespace OficinaMecanicaFase1.Api.Controllers;

[ApiController]
[Route("api/servicos")]
[Tags("Serviços de catálogo")]
[Authorize(Policy = "EquipeInterna")]
public sealed class ServicosController : OficinaControllerBase
{
    private readonly GerenciadorCatalogo _gerenciadorCatalogo;

    public ServicosController(GerenciadorCatalogo gerenciadorCatalogo)
    {
        _gerenciadorCatalogo = gerenciadorCatalogo;
    }

    [HttpGet("", Name = "ListarServicosCatalogo")]
    [EndpointSummary("Lista serviços; somenteAtivos=true mostra os selecionáveis para uma nova OS.")]
    [ProducesResponseType(typeof(ServicoCatalogoResponse[]), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> ListarServicosAsync([FromQuery] bool? somenteAtivos, CancellationToken cancelamento) =>
        Ok((await _gerenciadorCatalogo.ListarServicosAsync(somenteAtivos ?? false, cancelamento))
            .Select(ServicoCatalogoResponse.De).ToArray());

    [HttpGet("{id:guid}", Name = "ObterServicoCatalogo")]
    [ProducesResponseType(typeof(ServicoCatalogoResponse), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> ObterServicoAsync([FromRoute] Guid id, CancellationToken cancelamento)
    {
        var servico = await _gerenciadorCatalogo.ObterServicoAsync(id, cancelamento);
        return servico is null ? NotFound() : Ok(ServicoCatalogoResponse.De(servico));
    }

    [HttpPost("", Name = "CriarServicoCatalogo")]
    [Authorize(Policy = "Administradores")]
    [ProducesResponseType(typeof(ServicoCatalogoResponse), 201)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 409)]
    public async Task<IActionResult> CriarServicoAsync([FromBody] ServicoCatalogoRequest entrada, CancellationToken cancelamento)
    {
        var resultado = await _gerenciadorCatalogo.CriarServicoAsync(entrada.Nome, entrada.Descricao, entrada.PrecoBase, cancelamento);
        return resultado.Erro == ErroCatalogo.Nenhum
            ? Created($"/api/servicos/{resultado.Valor!.Id}", ServicoCatalogoResponse.De(resultado.Valor))
            : ResponderErro(resultado);
    }

    [HttpPut("{id:guid}", Name = "AtualizarServicoCatalogo")]
    [Authorize(Policy = "Administradores")]
    [ProducesResponseType(typeof(ServicoCatalogoResponse), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 409)]
    public async Task<IActionResult> AtualizarServicoAsync([FromRoute] Guid id, [FromBody] ServicoCatalogoRequest entrada, CancellationToken cancelamento)
    {
        var resultado = await _gerenciadorCatalogo.AtualizarServicoAsync(id, entrada.Nome, entrada.Descricao, entrada.PrecoBase, cancelamento);
        return resultado.Erro == ErroCatalogo.Nenhum
            ? Ok(ServicoCatalogoResponse.De(resultado.Valor!)) : ResponderErro(resultado);
    }

    [HttpPatch("{id:guid}/ativacao", Name = "DefinirAtivacaoServicoCatalogo")]
    [Authorize(Policy = "Administradores")]
    [ProducesResponseType(typeof(ServicoCatalogoResponse), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(ProblemDetails), 409)]
    public async Task<IActionResult> DefinirServicoAtivoAsync([FromRoute] Guid id, [FromBody] DefinirAtivacaoRequest entrada, CancellationToken cancelamento)
    {
        var resultado = await _gerenciadorCatalogo.DefinirServicoAtivoAsync(id, entrada.Ativo, cancelamento);
        return resultado.Erro == ErroCatalogo.Nenhum
            ? Ok(ServicoCatalogoResponse.De(resultado.Valor!)) : ResponderErro(resultado);
    }

    [HttpDelete("{id:guid}", Name = "DesativarServicoCatalogo")]
    [EndpointSummary("Desativa o serviço sem apagar seu cadastro ou referências históricas.")]
    [Authorize(Policy = "Administradores")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(ProblemDetails), 409)]
    public async Task<IActionResult> DesativarServicoAsync([FromRoute] Guid id, CancellationToken cancelamento)
    {
        var resultado = await _gerenciadorCatalogo.DefinirServicoAtivoAsync(id, false, cancelamento);
        return resultado.Erro == ErroCatalogo.Nenhum ? NoContent() : ResponderErro(resultado);
    }

    private IActionResult ResponderErro<T>(ResultadoCatalogo<T> resultado) => resultado.Erro switch
    {
        ErroCatalogo.Invalido => RespostaValidacao(new Dictionary<string, string[]>
        {
            [resultado.Campo ?? "catalogo"] = [resultado.Mensagem ?? "Dados inválidos."]
        }),
        ErroCatalogo.NaoEncontrado => NotFound(),
        ErroCatalogo.Conflito => Problem(resultado.Mensagem, statusCode: 409),
        _ => Problem("Não foi possível processar a operação.", statusCode: 500)
    };
}
