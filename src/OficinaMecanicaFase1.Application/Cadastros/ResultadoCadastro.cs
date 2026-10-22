namespace OficinaMecanicaFase1.Application.Cadastros;

public enum ErroCadastro { Nenhum, Invalido, NaoEncontrado, Conflito }

public sealed record ResultadoCadastro<T>(T? Valor, ErroCadastro Erro, string? Campo = null, string? Mensagem = null)
{
    public static ResultadoCadastro<T> Sucesso(T valor) => new(valor, ErroCadastro.Nenhum);
    public static ResultadoCadastro<T> Falha(ErroCadastro erro, string mensagem, string? campo = null) =>
        new(default, erro, campo, mensagem);
}
