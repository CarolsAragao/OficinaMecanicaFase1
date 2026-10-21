using Microsoft.AspNetCore.Identity;
using OficinaMecanicaFase1.Infrastructure.Identity;

namespace OficinaMecanicaFase1.Api.Security;

public static class InicializadorIdentidade
{
    public static async Task InicializarAsync(IServiceProvider services, IConfiguration configuration)
    {
        var papeis = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        foreach (var papel in PapeisInternos.Todos)
        {
            if (await papeis.RoleExistsAsync(papel))
            {
                continue;
            }

            var resultado = await papeis.CreateAsync(new IdentityRole<Guid>(papel) { Id = Guid.NewGuid() });
            Verificar(resultado);
        }

        var usuarios = services.GetRequiredService<UserManager<UsuarioInterno>>();
        var administradores = await usuarios.GetUsersInRoleAsync(PapeisInternos.Administrador);
        if (administradores.Count > 0)
        {
            return;
        }

        var email = configuration["Bootstrap:AdminEmail"];
        var senha = configuration["Bootstrap:AdminPassword"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(senha))
        {
            throw new InvalidOperationException("Defina Bootstrap:AdminEmail e Bootstrap:AdminPassword fora do repositório para criar o primeiro administrador.");
        }

        if (await usuarios.FindByEmailAsync(email) is not null)
        {
            throw new InvalidOperationException("O e-mail inicial já pertence a outra conta. Informe outro e-mail para o primeiro administrador.");
        }

        var administrador = new UsuarioInterno
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            LockoutEnabled = true
        };

        Verificar(await usuarios.CreateAsync(administrador, senha));
        var atribuicao = await usuarios.AddToRoleAsync(administrador, PapeisInternos.Administrador);
        if (!atribuicao.Succeeded)
        {
            await usuarios.DeleteAsync(administrador);
            Verificar(atribuicao);
        }
    }

    private static void Verificar(IdentityResult resultado)
    {
        if (!resultado.Succeeded)
        {
            throw new InvalidOperationException("Não foi possível preparar as contas internas: " +
                string.Join("; ", resultado.Errors.Select(erro => erro.Description)));
        }
    }
}
