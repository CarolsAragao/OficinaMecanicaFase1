using Microsoft.AspNetCore.Identity;
using OficinaMecanicaFase1.Api.Contracts;
using OficinaMecanicaFase1.Infrastructure.Identity;

namespace OficinaMecanicaFase1.Api.Security;

public enum FalhaCadastroInterno
{
    Nenhuma,
    EmailDuplicado,
    DadosInvalidos,
    AtribuicaoPapel
}

public sealed record ResultadoCadastroInterno(UsuarioInternoResponse? Usuario,
    FalhaCadastroInterno Falha, Dictionary<string, string[]>? Erros = null);

public sealed class AcessoInterno(
    UserManager<UsuarioInterno> usuarios,
    SignInManager<UsuarioInterno> login,
    EmissorJwt emissorJwt)
{
    public async Task<AutenticacaoInternaResponse?> AutenticarAsync(string email, string senha)
    {
        var usuario = await usuarios.FindByEmailAsync(email);
        if (usuario is null)
        {
            return null;
        }

        var resultado = await login.CheckPasswordSignInAsync(usuario, senha, lockoutOnFailure: true);
        if (!resultado.Succeeded)
        {
            return null;
        }

        var papeis = await usuarios.GetRolesAsync(usuario);
        if (!papeis.Any(papel => PapeisInternos.Todos.Contains(papel, StringComparer.Ordinal)))
        {
            return null;
        }

        var token = emissorJwt.Emitir(usuario, papeis);
        return new AutenticacaoInternaResponse(token.Valor, "Bearer", token.ExpiraEmUtc);
    }

    public async Task<ResultadoCadastroInterno> CadastrarAsync(string email, string senha, string papel)
    {
        if (await usuarios.FindByEmailAsync(email) is not null)
        {
            return new(null, FalhaCadastroInterno.EmailDuplicado);
        }

        var usuario = new UsuarioInterno
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            LockoutEnabled = true
        };

        var criacao = await usuarios.CreateAsync(usuario, senha);
        if (!criacao.Succeeded)
        {
            return new(null, FalhaCadastroInterno.DadosInvalidos, criacao.Errors
                .GroupBy(erro => erro.Code)
                .ToDictionary(grupo => grupo.Key, grupo => grupo.Select(erro => erro.Description).ToArray()));
        }

        var atribuicao = await usuarios.AddToRoleAsync(usuario, papel);
        if (!atribuicao.Succeeded)
        {
            await usuarios.DeleteAsync(usuario);
            return new(null, FalhaCadastroInterno.AtribuicaoPapel);
        }

        return new(new UsuarioInternoResponse(usuario.Id, usuario.Email!, papel), FalhaCadastroInterno.Nenhuma);
    }
}
