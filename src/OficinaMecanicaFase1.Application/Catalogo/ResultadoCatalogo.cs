namespace OficinaMecanicaFase1.Application.Catalogo;

public enum ErroCatalogo { Nenhum, Invalido, NaoEncontrado, Conflito }

public sealed record ResultadoCatalogo<T>(T? Valor, ErroCatalogo Erro, string? Campo = null, string? Mensagem = null)
{
    public static ResultadoCatalogo<T> Sucesso(T valor) => new(valor, ErroCatalogo.Nenhum);
    public static ResultadoCatalogo<T> Falha(ErroCatalogo erro, string mensagem, string? campo = null) =>
        new(default, erro, campo, mensagem);
}
