namespace OficinaMecanicaFase1.Application.OrdensServico;

public enum ErroOrdemServico { Nenhum, Invalido, NaoEncontrado, Conflito }

public sealed record ResultadoOrdemServico<T>(
    ErroOrdemServico Erro, T? Valor = default, string? Mensagem = null, string? Campo = null)
{
    public static ResultadoOrdemServico<T> Sucesso(T valor) => new(ErroOrdemServico.Nenhum, valor);
    public static ResultadoOrdemServico<T> Falha(ErroOrdemServico erro, string mensagem, string? campo = null) =>
        new(erro, default, mensagem, campo);
}
