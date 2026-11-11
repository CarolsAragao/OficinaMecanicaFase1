namespace OficinaMecanicaFase1.Application.AcessoInterno;

public sealed record UsuarioInternoDados(Guid Id, string Nome, string Email,
    IReadOnlyCollection<string> Papeis);

public enum FalhaCadastroInterno
{
    Nenhuma,
    EmailDuplicado,
    DadosInvalidos,
    AtribuicaoPapel
}

public sealed record ResultadoCadastroInterno(UsuarioInternoDados? Usuario,
    FalhaCadastroInterno Falha, Dictionary<string, string[]>? Erros = null);

public sealed record TokenInterno(string Valor, DateTimeOffset ExpiraEmUtc);

public interface IIdentidadeInterna
{
    Task<UsuarioInternoDados?> AutenticarAsync(string email, string senha);
    Task<bool> EmailEmUsoAsync(string email);
    Task<ResultadoCadastroInterno> CadastrarAsync(string email, string senha, string papel);
}

public interface IEmissorTokenInterno
{
    TokenInterno Emitir(UsuarioInternoDados usuario);
}
