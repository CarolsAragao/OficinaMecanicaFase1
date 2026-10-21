using Microsoft.AspNetCore.Identity;
using OficinaMecanicaFase1.Infrastructure.Identity;

namespace OficinaMecanicaFase1.Api.Security;

public static class LimpezaUsuariosTeste
{
    public static async Task ExecutarAsync(IServiceProvider services)
    {
        var usuarios = services.GetRequiredService<UserManager<UsuarioInterno>>();
        var removidos = 0;

        foreach (var (email, papel) in new[]
        {
            ("admin@teste.local", PapeisInternos.Administrador),
            ("atendente@teste.local", PapeisInternos.Atendente)
        })
        {
            var usuario = await usuarios.FindByEmailAsync(email);
            if (usuario is null || !await usuarios.IsInRoleAsync(usuario, papel))
            {
                continue;
            }

            var resultado = await usuarios.DeleteAsync(usuario);
            if (!resultado.Succeeded)
            {
                throw new InvalidOperationException($"Não foi possível remover a conta de teste {email}: " +
                    string.Join("; ", resultado.Errors.Select(erro => erro.Description)));
            }

            removidos++;
        }

        Console.WriteLine($"Contas de teste removidas: {removidos}.");
    }
}
