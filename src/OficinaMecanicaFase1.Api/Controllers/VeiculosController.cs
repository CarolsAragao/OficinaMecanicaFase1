using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OficinaMecanicaFase1.Api.Contracts;
using OficinaMecanicaFase1.Application.Cadastros;

namespace OficinaMecanicaFase1.Api.Controllers;

[ApiController]
[Route("api/veiculos")]
[Tags("Veículos")]
[Authorize(Policy = "Cadastros")]
public sealed class VeiculosController : OficinaControllerBase
{
    private readonly GerenciadorCadastros _gerenciadorCadastros;

    public VeiculosController(GerenciadorCadastros gerenciadorCadastros)
    {
        _gerenciadorCadastros = gerenciadorCadastros;
    }

    [HttpGet("", Name = "ListarVeiculos")]
    [EndpointSummary("Lista veículos; clienteId opcional filtra pelo proprietário.")]
    [ProducesResponseType(typeof(VeiculoResponse[]), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> ListarVeiculosAsync([FromQuery] Guid? clienteId, CancellationToken cancelamento) =>
        Ok((await _gerenciadorCadastros.ListarVeiculosAsync(clienteId, cancelamento))
            .Select(VeiculoResponse.De).ToArray());

    [HttpGet("{id:guid}", Name = "ObterVeiculo")]
    [ProducesResponseType(typeof(VeiculoResponse), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> ObterVeiculoAsync([FromRoute] Guid id, CancellationToken cancelamento)
    {
        var veiculo = await _gerenciadorCadastros.ObterVeiculoAsync(id, cancelamento);
        return veiculo is null ? NotFound() : Ok(VeiculoResponse.De(veiculo));
    }

    [HttpPost("", Name = "CriarVeiculo")]
    [ProducesResponseType(typeof(VeiculoResponse), 201)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 409)]
    public async Task<IActionResult> CriarVeiculoAsync([FromBody] CriarVeiculoRequest entrada, CancellationToken cancelamento)
    {
        var resultado = await _gerenciadorCadastros.CriarVeiculoAsync(
            entrada.ClienteId, entrada.Placa, entrada.Marca, entrada.Modelo, entrada.Ano, cancelamento);
        return resultado.Erro == ErroCadastro.Nenhum
            ? Created($"/api/veiculos/{resultado.Valor!.Id}", VeiculoResponse.De(resultado.Valor))
            : ResponderErro(resultado);
    }

    [HttpPut("{id:guid}", Name = "AtualizarVeiculo")]
    [EndpointSummary("Atualiza os dados do veículo sem alterar o cliente proprietário.")]
    [ProducesResponseType(typeof(VeiculoResponse), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 409)]
    public async Task<IActionResult> AtualizarVeiculoAsync([FromRoute] Guid id, [FromBody] AtualizarVeiculoRequest entrada, CancellationToken cancelamento)
    {
        var resultado = await _gerenciadorCadastros.AtualizarVeiculoAsync(
            id, entrada.Placa, entrada.Marca, entrada.Modelo, entrada.Ano, cancelamento);
        return resultado.Erro == ErroCadastro.Nenhum
            ? Ok(VeiculoResponse.De(resultado.Valor!))
            : ResponderErro(resultado);
    }

    [HttpDelete("{id:guid}", Name = "RemoverVeiculo")]
    [Authorize(Policy = "Administradores")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(ProblemDetails), 409)]
    public async Task<IActionResult> RemoverVeiculoAsync([FromRoute] Guid id, CancellationToken cancelamento)
    {
        var resultado = await _gerenciadorCadastros.RemoverVeiculoAsync(id, cancelamento);
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
