using Microsoft.AspNetCore.Identity;
using OficinaMecanicaFase1.Application.AcessoInterno;

namespace OficinaMecanicaFase1.Infrastructure.Identity;

public sealed class IdentidadeInternaAdapter(
    UserManager<UsuarioInterno> usuarios, SignInManager<UsuarioInterno> login) : IIdentidadeInterna
{
    public async Task<UsuarioInternoDados?> AutenticarAsync(string email, string senha)
    {
        var usuario = await usuarios.FindByEmailAsync(email);
        if (usuario is null || !(await login.CheckPasswordSignInAsync(
            usuario, senha, lockoutOnFailure: true)).Succeeded) return null;

        var papeis = await usuarios.GetRolesAsync(usuario);
        return new UsuarioInternoDados(usuario.Id, usuario.UserName ?? string.Empty,
            usuario.Email ?? string.Empty, papeis.ToArray());
    }

    public async Task<bool> EmailEmUsoAsync(string email) =>
        await usuarios.FindByEmailAsync(email) is not null;

    public async Task<ResultadoCadastroInterno> CadastrarAsync(string email, string senha, string papel)
    {
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
            if (criacao.Errors.Any(erro => erro.Code is "DuplicateEmail" or "DuplicateUserName"))
                return new(null, FalhaCadastroInterno.EmailDuplicado);

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

        return new(new UsuarioInternoDados(usuario.Id, usuario.UserName!, usuario.Email!, [papel]),
            FalhaCadastroInterno.Nenhuma);
    }
}
