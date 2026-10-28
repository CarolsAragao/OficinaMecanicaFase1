using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OficinaMecanicaFase1.Api.Contracts;
using OficinaMecanicaFase1.Application.Cadastros;

namespace OficinaMecanicaFase1.Api.Controllers;

[ApiController]
[Route("api/clientes")]
[Tags("Clientes")]
[Authorize(Policy = "Cadastros")]
public sealed class ClientesController : OficinaControllerBase
{
    private readonly GerenciadorCadastros _gerenciadorCadastros;

    public ClientesController(GerenciadorCadastros gerenciadorCadastros)
    {
        _gerenciadorCadastros = gerenciadorCadastros;
    }

    [HttpGet("", Name = "ListarClientes")]
    [EndpointSummary("Lista clientes; documento opcional busca CPF/CNPJ exato após validação.")]
    [ProducesResponseType(typeof(ClienteResponse[]), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    public async Task<IActionResult> ListarClientesAsync([FromQuery] string? documento, CancellationToken cancelamento)
    {
        var resultado = await _gerenciadorCadastros.ListarClientesAsync(documento, cancelamento);
        return resultado.Erro == ErroCadastro.Nenhum
            ? Ok(resultado.Valor!.Select(ClienteResponse.De).ToArray())
            : ResponderErro(resultado);
    }

    [HttpGet("{id:guid}", Name = "ObterCliente")]
    [ProducesResponseType(typeof(ClienteResponse), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> ObterClienteAsync([FromRoute] Guid id, CancellationToken cancelamento)
    {
        var cliente = await _gerenciadorCadastros.ObterClienteAsync(id, cancelamento);
        return cliente is null ? NotFound() : Ok(ClienteResponse.De(cliente));
    }

    [HttpPost("", Name = "CriarCliente")]
    [ProducesResponseType(typeof(ClienteResponse), 201)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 409)]
    public async Task<IActionResult> CriarClienteAsync([FromBody] ClienteRequest entrada, CancellationToken cancelamento)
    {
        var resultado = await _gerenciadorCadastros.CriarClienteAsync(entrada.Nome, entrada.Documento, cancelamento);
        return resultado.Erro == ErroCadastro.Nenhum
            ? Created($"/api/clientes/{resultado.Valor!.Id}", ClienteResponse.De(resultado.Valor))
            : ResponderErro(resultado);
    }

    [HttpPut("{id:guid}", Name = "AtualizarCliente")]
    [ProducesResponseType(typeof(ClienteResponse), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 409)]
    public async Task<IActionResult> AtualizarClienteAsync([FromRoute] Guid id, [FromBody] ClienteRequest entrada, CancellationToken cancelamento)
    {
        var resultado = await _gerenciadorCadastros.AtualizarClienteAsync(id, entrada.Nome, entrada.Documento, cancelamento);
        return resultado.Erro == ErroCadastro.Nenhum
            ? Ok(ClienteResponse.De(resultado.Valor!))
            : ResponderErro(resultado);
    }

    [HttpDelete("{id:guid}", Name = "RemoverCliente")]
    [Authorize(Policy = "Administradores")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(ProblemDetails), 409)]
    public async Task<IActionResult> RemoverClienteAsync([FromRoute] Guid id, CancellationToken cancelamento)
    {
        var resultado = await _gerenciadorCadastros.RemoverClienteAsync(id, cancelamento);
        return resultado.Erro == ErroCadastro.Nenhum ? NoContent() : ResponderErro(resultado);
    }

    private IActionResult ResponderErro<T>(ResultadoCadastro<T> resultado) => resultado.Erro switch
    {
        ErroCadastro.Invalido => RespostaValidacao(new Dictionary<string, string[]>
        {
            [resultado.Campo ?? "cadastro"] = [resultado.Mensagem ?? "Dados inválidos."]
        }),
        ErroCadastro.NaoEncontrado => NotFound(),
        ErroCadastro.Conflito => Problem(resultado.Mensagem, statusCode: 409),
        _ => Problem("Não foi possível processar o cadastro.", statusCode: 500)
    };
}
