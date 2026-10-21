using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using OficinaMecanicaFase1.Api.Contracts;
using OficinaMecanicaFase1.Infrastructure.Identity;

namespace OficinaMecanicaFase1.Api.Security;

public static class RotasAcessoInterno
{
    public static IEndpointRouteBuilder MapearAcessoInterno(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/api/interno").WithTags("Acesso interno");

        grupo.MapPost("/autenticacao", AutenticarAsync)
            .AllowAnonymous()
            .WithName("AutenticarUsuarioInterno")
            .WithSummary("Autentica uma conta interna e emite um JWT de 15 minutos.")
            .Produces<AutenticacaoInternaResponse>()
            .Produces(401);

        grupo.MapGet("/identidade", (ClaimsPrincipal usuario) =>
                Results.Ok(new IdentidadeInternaResponse(
                    usuario.Identity?.Name ?? string.Empty,
                    usuario.FindAll("role").Select(claim => claim.Value).ToArray())))
            .RequireAuthorization("EquipeInterna")
            .WithName("ConsultarIdentidadeInterna")
            .Produces<IdentidadeInternaResponse>()
            .Produces(401)
            .Produces(403);

        grupo.MapPost("/usuarios", CadastrarUsuarioAsync)
            .RequireAuthorization("Administradores")
            .WithName("CadastrarUsuarioInterno")
            .WithSummary("Cria uma conta interna em um dos quatro papéis definidos.")
            .Produces<UsuarioInternoResponse>(201)
            .ProducesValidationProblem()
            .Produces(401)
            .Produces(403)
            .ProducesProblem(409);

        return app;
    }

    private static async Task<IResult> AutenticarAsync(
        AutenticacaoInternaRequest entrada,
        UserManager<UsuarioInterno> usuarios,
        SignInManager<UsuarioInterno> autenticacao,
        EmissorJwt emissor)
    {
        if (string.IsNullOrWhiteSpace(entrada.Email) || string.IsNullOrWhiteSpace(entrada.Senha))
        {
            return Results.Unauthorized();
        }

        var usuario = await usuarios.FindByEmailAsync(entrada.Email);
        if (usuario is null)
        {
            return Results.Unauthorized();
        }

        var resultado = await autenticacao.CheckPasswordSignInAsync(usuario, entrada.Senha, lockoutOnFailure: true);
        if (!resultado.Succeeded)
        {
            return Results.Unauthorized();
        }

        var papeis = await usuarios.GetRolesAsync(usuario);
        if (!papeis.Any(papel => PapeisInternos.Todos.Contains(papel, StringComparer.Ordinal)))
        {
            return Results.Unauthorized();
        }

        var token = emissor.Emitir(usuario, papeis);
        return Results.Ok(new AutenticacaoInternaResponse(token.Valor, "Bearer", token.ExpiraEmUtc));
    }

    private static async Task<IResult> CadastrarUsuarioAsync(
        CadastrarUsuarioInternoRequest entrada,
        UserManager<UsuarioInterno> usuarios)
    {
        if (string.IsNullOrWhiteSpace(entrada.Email) || string.IsNullOrWhiteSpace(entrada.Senha) ||
            !PapeisInternos.Todos.Contains(entrada.Papel, StringComparer.Ordinal))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["usuario"] = ["Informe e-mail, senha e um papel interno válido."]
            });
        }

        if (await usuarios.FindByEmailAsync(entrada.Email) is not null)
        {
            return Results.Problem("Já existe uma conta com esse e-mail.", statusCode: 409);
        }

        var usuario = new UsuarioInterno
        {
            Id = Guid.NewGuid(),
            UserName = entrada.Email,
            Email = entrada.Email,
            LockoutEnabled = true
        };

        var criacao = await usuarios.CreateAsync(usuario, entrada.Senha);
        if (!criacao.Succeeded)
        {
            return Results.ValidationProblem(criacao.Errors
                .GroupBy(erro => erro.Code)
                .ToDictionary(grupo => grupo.Key, grupo => grupo.Select(erro => erro.Description).ToArray()));
        }

        var atribuicao = await usuarios.AddToRoleAsync(usuario, entrada.Papel!);
        if (!atribuicao.Succeeded)
        {
            await usuarios.DeleteAsync(usuario);
            return Results.Problem("Não foi possível atribuir o papel à conta.", statusCode: 500);
        }

        return Results.Json(new UsuarioInternoResponse(usuario.Id, usuario.Email!, entrada.Papel!), statusCode: 201);
    }
}
