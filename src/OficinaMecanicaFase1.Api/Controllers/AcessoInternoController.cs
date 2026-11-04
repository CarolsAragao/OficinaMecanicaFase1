using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OficinaMecanicaFase1.Api.Contracts;
using OficinaMecanicaFase1.Api.Security;
using System.Security.Claims;

namespace OficinaMecanicaFase1.Api.Controllers;

[ApiController]
[Route("api/interno")]
[Tags("Acesso interno")]
public sealed class AcessoInternoController(AcessoInterno acesso) : OficinaControllerBase
{
    [HttpPost("autenticacao", Name = "AutenticarUsuarioInterno")]
    [EndpointSummary("Autentica uma conta interna e emite um JWT de 15 minutos.")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AutenticacaoInternaResponse), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> AutenticarAsync([FromBody] AutenticacaoInternaRequest entrada)
    {
        if (string.IsNullOrWhiteSpace(entrada.Email) || string.IsNullOrWhiteSpace(entrada.Senha))
        {
            return Unauthorized();
        }

        var resposta = await acesso.AutenticarAsync(entrada.Email, entrada.Senha);
        return resposta is null ? Unauthorized() : Ok(resposta);
    }

    [HttpGet("identidade", Name = "ConsultarIdentidadeInterna")]
    [Authorize(Policy = "EquipeInterna")]
    [ProducesResponseType(typeof(IdentidadeInternaResponse), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public IActionResult ConsultarIdentidade() => Ok(new IdentidadeInternaResponse(
        User.Identity?.Name ?? string.Empty,
        User.FindAll("role").Select(claim => claim.Value).ToArray()));

    [HttpPost("usuarios", Name = "CadastrarUsuarioInterno")]
    [EndpointSummary("Cria uma conta interna em um dos quatro papéis definidos.")]
    [Authorize(Policy = "Administradores")]
    [ProducesResponseType(typeof(UsuarioInternoResponse), 201)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 409)]
    public async Task<IActionResult> CadastrarUsuarioAsync([FromBody] CadastrarUsuarioInternoRequest entrada)
    {
        if (string.IsNullOrWhiteSpace(entrada.Email) || string.IsNullOrWhiteSpace(entrada.Senha) ||
            !PapeisInternos.Todos.Contains(entrada.Papel, StringComparer.Ordinal))
        {
            return RespostaValidacao(new Dictionary<string, string[]>
            {
                ["usuario"] = ["Informe e-mail, senha e um papel interno válido."]
            });
        }

        var resultado = await acesso.CadastrarAsync(entrada.Email, entrada.Senha, entrada.Papel!);
        return resultado.Falha switch
        {
            FalhaCadastroInterno.EmailDuplicado => Problem("Já existe uma conta com esse e-mail.", statusCode: 409),
            FalhaCadastroInterno.DadosInvalidos => RespostaValidacao(resultado.Erros!),
            FalhaCadastroInterno.AtribuicaoPapel => Problem("Não foi possível atribuir o papel à conta.", statusCode: 500),
            _ => new JsonResult(resultado.Usuario) { StatusCode = 201 }
        };
    }
}
