namespace OficinaMecanicaFase1.Application.AcessoInterno;

public sealed class AcessoInternoService(IIdentidadeInterna identidade, IEmissorTokenInterno emissorToken)
{
    public async Task<TokenInterno?> AutenticarAsync(string? email, string? senha)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(senha)) return null;

        var usuario = await identidade.AutenticarAsync(email, senha);
        return usuario is not null && usuario.Papeis.Any(papel =>
            PapeisInternos.Todos.Contains(papel, StringComparer.Ordinal))
            ? emissorToken.Emitir(usuario) : null;
    }

    public async Task<ResultadoCadastroInterno> CadastrarAsync(string? email, string? senha, string? papel)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(senha) ||
            !PapeisInternos.Todos.Contains(papel, StringComparer.Ordinal))
        {
            return new(null, FalhaCadastroInterno.DadosInvalidos, new Dictionary<string, string[]>
            {
                ["usuario"] = ["Informe e-mail, senha e um papel interno válido."]
            });
        }

        if (await identidade.EmailEmUsoAsync(email))
            return new(null, FalhaCadastroInterno.EmailDuplicado);

        return await identidade.CadastrarAsync(email, senha, papel!);
    }
}
