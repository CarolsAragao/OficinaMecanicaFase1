using OficinaMecanicaFase1.Infrastructure.Persistence;

namespace OficinaMecanicaFase1.Api.Security;

internal static class InicializacaoAplicacao
{
    public static async Task<bool> DeveIniciarServidorAsync(WebApplication app)
    {
        var modoRedefinicaoAdministrador = string.Equals(
            Environment.GetEnvironmentVariable("OFICINA_REDEFINIR_ADMIN"), "1", StringComparison.Ordinal);
        var modoLimpezaUsuariosTeste = string.Equals(
            Environment.GetEnvironmentVariable("OFICINA_LIMPAR_USUARIOS_TESTE"), "1", StringComparison.Ordinal);
        var modoManutencaoIdentidade = modoRedefinicaoAdministrador || modoLimpezaUsuariosTeste;
        var modoBanco = app.Configuration["Banco:Inicializacao"] ?? "Automatica";
        if (modoBanco is not ("Automatica" or "SomentePreparar" or "Externa"))
            throw new InvalidOperationException("Banco:Inicializacao deve ser Automatica, SomentePreparar ou Externa.");

        try
        {
            if (modoBanco != "Externa" || modoManutencaoIdentidade)
            {
                await using var scope = app.Services.CreateAsyncScope();
                await PreparacaoBanco.MigrarAsync(scope.ServiceProvider);
                await InicializadorIdentidade.InicializarAsync(scope.ServiceProvider, app.Configuration);
            }

            if (modoLimpezaUsuariosTeste)
            {
                await using var scope = app.Services.CreateAsyncScope();
                await LimpezaUsuariosTeste.ExecutarAsync(scope.ServiceProvider);
                await InicializadorIdentidade.InicializarAsync(scope.ServiceProvider, app.Configuration);
                await RedefinidorSenhaAdministrador.ExecutarAsync(scope.ServiceProvider);
                return false;
            }

            if (modoRedefinicaoAdministrador)
            {
                await using var scope = app.Services.CreateAsyncScope();
                await RedefinidorSenhaAdministrador.ExecutarAsync(scope.ServiceProvider);
                return false;
            }
        }
        catch (Exception exception) when (modoManutencaoIdentidade)
        {
            Console.Error.WriteLine("Falha ao preparar o banco ou redefinir a senha: " + exception.GetBaseException().Message);
            Environment.ExitCode = 1;
            return false;
        }

        return modoBanco != "SomentePreparar";
    }
}
