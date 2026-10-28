using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OficinaMecanicaFase1.Api.Contracts;
using OficinaMecanicaFase1.Application.Catalogo;
using System.Security.Claims;

namespace OficinaMecanicaFase1.Api.Controllers;

[ApiController]
[Route("api/itens-estoque")]
[Tags("Peças e insumos")]
[Authorize(Policy = "EquipeInterna")]
public sealed class ItensEstoqueController : OficinaControllerBase
{
    private readonly GerenciadorCatalogo _gerenciadorCatalogo;

    public ItensEstoqueController(GerenciadorCatalogo gerenciadorCatalogo)
    {
        _gerenciadorCatalogo = gerenciadorCatalogo;
    }

    [HttpGet("", Name = "ListarItensEstoque")]
    [EndpointSummary("Lista peças e insumos; somenteAtivos=true mostra os selecionáveis para uma nova OS.")]
    [ProducesResponseType(typeof(ItemEstoqueResponse[]), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> ListarItensAsync([FromQuery] bool? somenteAtivos, CancellationToken cancelamento) =>
        Ok((await _gerenciadorCatalogo.ListarItensAsync(somenteAtivos ?? false, cancelamento))
            .Select(ItemEstoqueResponse.De).ToArray());

    [HttpGet("{id:guid}", Name = "ObterItemEstoque")]
    [ProducesResponseType(typeof(ItemEstoqueResponse), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> ObterItemAsync([FromRoute] Guid id, CancellationToken cancelamento)
    {
        var item = await _gerenciadorCatalogo.ObterItemAsync(id, cancelamento);
        return item is null ? NotFound() : Ok(ItemEstoqueResponse.De(item));
    }

    [HttpPost("", Name = "CriarItemEstoque")]
    [Authorize(Policy = "Administradores")]
    [ProducesResponseType(typeof(ItemEstoqueResponse), 201)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 409)]
    public async Task<IActionResult> CriarItemAsync([FromBody] CriarItemEstoqueRequest entrada, CancellationToken cancelamento)
    {
        var resultado = await _gerenciadorCatalogo.CriarItemAsync(entrada.Nome, entrada.Descricao, entrada.Tipo,
            entrada.UnidadeMedida, entrada.PrecoUnitario, cancelamento);
        return resultado.Erro == ErroCatalogo.Nenhum
            ? Created($"/api/itens-estoque/{resultado.Valor!.Id}", ItemEstoqueResponse.De(resultado.Valor))
            : ResponderErro(resultado);
    }

    [HttpPut("{id:guid}", Name = "AtualizarItemEstoque")]
    [Authorize(Policy = "Administradores")]
    [ProducesResponseType(typeof(ItemEstoqueResponse), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 409)]
    public async Task<IActionResult> AtualizarItemAsync([FromRoute] Guid id, [FromBody] AtualizarItemEstoqueRequest entrada, CancellationToken cancelamento)
    {
        var resultado = await _gerenciadorCatalogo.AtualizarItemAsync(id, entrada.Nome, entrada.Descricao,
            entrada.PrecoUnitario, cancelamento);
        return resultado.Erro == ErroCatalogo.Nenhum
            ? Ok(ItemEstoqueResponse.De(resultado.Valor!)) : ResponderErro(resultado);
    }

    [HttpPatch("{id:guid}/ativacao", Name = "DefinirAtivacaoItemEstoque")]
    [Authorize(Policy = "Administradores")]
    [ProducesResponseType(typeof(ItemEstoqueResponse), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(ProblemDetails), 409)]
    public async Task<IActionResult> DefinirItemAtivoAsync([FromRoute] Guid id, [FromBody] DefinirAtivacaoRequest entrada, CancellationToken cancelamento)
    {
        var resultado = await _gerenciadorCatalogo.DefinirItemAtivoAsync(id, entrada.Ativo, cancelamento);
        return resultado.Erro == ErroCatalogo.Nenhum
            ? Ok(ItemEstoqueResponse.De(resultado.Valor!)) : ResponderErro(resultado);
    }

    [HttpDelete("{id:guid}", Name = "DesativarItemEstoque")]
    [EndpointSummary("Desativa o item sem apagar seu cadastro nem as entradas registradas.")]
    [Authorize(Policy = "Administradores")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(ProblemDetails), 409)]
    public async Task<IActionResult> DesativarItemAsync([FromRoute] Guid id, CancellationToken cancelamento)
    {
        var resultado = await _gerenciadorCatalogo.DefinirItemAtivoAsync(id, false, cancelamento);
        return resultado.Erro == ErroCatalogo.Nenhum ? NoContent() : ResponderErro(resultado);
    }

    [HttpPost("{id:guid}/entradas", Name = "RegistrarEntradaEstoque")]
    [EndpointSummary("Estoquista registra entrada de peça inteira ou insumo fracionário.")]
    [Authorize(Policy = "Estoquistas")]
    [ProducesResponseType(typeof(EntradaEstoqueResponse), 201)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 409)]
    public async Task<IActionResult> RegistrarEntradaAsync([FromRoute] Guid id, [FromBody] RegistrarEntradaEstoqueRequest entrada, CancellationToken cancelamento)
    {
        if (!Guid.TryParse(User.FindFirst("sub")?.Value, out var usuarioId)) return Forbid();
        var resultado = await _gerenciadorCatalogo.RegistrarEntradaAsync(id, entrada.Quantidade, usuarioId, cancelamento);
        return resultado.Erro == ErroCatalogo.Nenhum
            ? Created($"/api/itens-estoque/{id}/entradas", EntradaEstoqueResponse.De(resultado.Valor!))
            : ResponderErro(resultado);
    }

    [HttpGet("{id:guid}/entradas", Name = "ListarEntradasEstoque")]
    [ProducesResponseType(typeof(EntradaEstoqueResponse[]), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> ListarEntradasAsync([FromRoute] Guid id, CancellationToken cancelamento)
    {
        if (await _gerenciadorCatalogo.ObterItemAsync(id, cancelamento) is null) return NotFound();
        return Ok((await _gerenciadorCatalogo.ListarEntradasAsync(id, cancelamento))
            .Select(EntradaEstoqueResponse.De).ToArray());
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
