using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OficinaMecanicaFase1.Api.Contracts;
using OficinaMecanicaFase1.Api.Security;
using OficinaMecanicaFase1.Infrastructure.Identity;
using System.Security.Claims;

namespace OficinaMecanicaFase1.Api.Controllers;

[ApiController]
[Route("api/interno")]
[Tags("Acesso interno")]
public sealed class AcessoInternoController : OficinaControllerBase
{
    private readonly UserManager<UsuarioInterno> _userManagerUsuarioInterno;
    private readonly SignInManager<UsuarioInterno> _signInManagerUsuarioInterno;
    private readonly EmissorJwt _emissorJwt;

    public AcessoInternoController(UserManager<UsuarioInterno> userManagerUsuarioInterno, SignInManager<UsuarioInterno> signInManagerUsuarioInterno, EmissorJwt emissorJwt)
    {
        _userManagerUsuarioInterno = userManagerUsuarioInterno;
        _signInManagerUsuarioInterno = signInManagerUsuarioInterno;
        _emissorJwt = emissorJwt;
    }

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

        var usuario = await _userManagerUsuarioInterno.FindByEmailAsync(entrada.Email);
        if (usuario is null)
        {
            return Unauthorized();
        }

        var resultado = await _signInManagerUsuarioInterno.CheckPasswordSignInAsync(usuario, entrada.Senha, lockoutOnFailure: true);
        if (!resultado.Succeeded)
        {
            return Unauthorized();
        }

        var papeis = await _userManagerUsuarioInterno.GetRolesAsync(usuario);
        if (!papeis.Any(papel => PapeisInternos.Todos.Contains(papel, StringComparer.Ordinal)))
        {
            return Unauthorized();
        }

        var token = _emissorJwt.Emitir(usuario, papeis);
        return Ok(new AutenticacaoInternaResponse(token.Valor, "Bearer", token.ExpiraEmUtc));
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

        if (await _userManagerUsuarioInterno.FindByEmailAsync(entrada.Email) is not null)
        {
            return Problem("Já existe uma conta com esse e-mail.", statusCode: 409);
        }

        var usuario = new UsuarioInterno
        {
            Id = Guid.NewGuid(),
            UserName = entrada.Email,
            Email = entrada.Email,
            LockoutEnabled = true
        };

        var criacao = await _userManagerUsuarioInterno.CreateAsync(usuario, entrada.Senha);
        if (!criacao.Succeeded)
        {
            return RespostaValidacao(criacao.Errors
                .GroupBy(erro => erro.Code)
                .ToDictionary(grupo => grupo.Key, grupo => grupo.Select(erro => erro.Description).ToArray()));
        }

        var atribuicao = await _userManagerUsuarioInterno.AddToRoleAsync(usuario, entrada.Papel!);
        if (!atribuicao.Succeeded)
        {
            await _userManagerUsuarioInterno.DeleteAsync(usuario);
            return Problem("Não foi possível atribuir o papel à conta.", statusCode: 500);
        }

        return new JsonResult(new UsuarioInternoResponse(usuario.Id, usuario.Email!, entrada.Papel!)) { StatusCode = 201 };
    }
}
