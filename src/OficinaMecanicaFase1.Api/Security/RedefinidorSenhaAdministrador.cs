using System.Text;
using Microsoft.AspNetCore.Identity;
using OficinaMecanicaFase1.Infrastructure.Identity;

namespace OficinaMecanicaFase1.Api.Security;

public static class RedefinidorSenhaAdministrador
{
    public static async Task ExecutarAsync(IServiceProvider services)
    {
        Console.Write("E-mail da conta Administrador: ");
        var email = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new InvalidOperationException("Informe o e-mail da conta Administrador.");
        }

        var usuarios = services.GetRequiredService<UserManager<UsuarioInterno>>();
        var usuario = await usuarios.FindByEmailAsync(email);
        if (usuario is null || !await usuarios.IsInRoleAsync(usuario, PapeisInternos.Administrador))
        {
            var administradores = await usuarios.GetUsersInRoleAsync(PapeisInternos.Administrador);
            if (administradores.Count == 0)
            {
                throw new InvalidOperationException("O banco conectado não possui conta Administrador. Confira a conexão e as configurações de bootstrap antes de redefinir a senha.");
            }

            throw new InvalidOperationException("O e-mail informado não pertence a uma conta Administrador neste banco. Contas disponíveis: " +
                string.Join(", ", administradores.Select(administrador => administrador.Email)));
        }

        var senha = LerSenha("Nova senha: ");
        var confirmacao = LerSenha("Confirme a nova senha: ");
        if (!string.Equals(senha, confirmacao, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("As senhas não coincidem.");
        }

        var token = await usuarios.GeneratePasswordResetTokenAsync(usuario);
        var redefinicao = await usuarios.ResetPasswordAsync(usuario, token, senha);
        if (!redefinicao.Succeeded)
        {
            throw new InvalidOperationException("A senha não atende às regras do Identity: " +
                string.Join("; ", redefinicao.Errors.Select(erro => erro.Description)));
        }

        await usuarios.ResetAccessFailedCountAsync(usuario);
        await usuarios.SetLockoutEndDateAsync(usuario, null);
        Console.WriteLine("Senha do Administrador redefinida. Faça login no Swagger com a nova senha.");
    }

    private static string LerSenha(string rotulo)
    {
        Console.Write(rotulo);
        var senha = new StringBuilder();
        while (true)
        {
            var tecla = Console.ReadKey(intercept: true);
            if (tecla.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                return senha.ToString();
            }

            if (tecla.Key == ConsoleKey.Backspace)
            {
                if (senha.Length > 0)
                {
                    senha.Length--;
                    Console.Write("\b \b");
                }
                continue;
            }

            if (!char.IsControl(tecla.KeyChar))
            {
                senha.Append(tecla.KeyChar);
                Console.Write('*');
            }
        }
    }
}
