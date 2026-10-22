using OficinaMecanicaFase1.Domain.Catalogo;
using OficinaMecanicaFase1.Domain.Estoque;

namespace OficinaMecanicaFase1.Api.Contracts;

public sealed record ServicoCatalogoRequest(string Nome, string? Descricao, decimal PrecoBase);
public sealed record ServicoCatalogoResponse(Guid Id, string Nome, string? Descricao, decimal PrecoBase, bool Ativo)
{
    public static ServicoCatalogoResponse De(ServicoCatalogo servico) =>
        new(servico.Id, servico.Nome, servico.Descricao, servico.PrecoBase, servico.Ativo);
}

public sealed record CriarItemEstoqueRequest(
    string Nome, string? Descricao, string Tipo, string UnidadeMedida, decimal PrecoUnitario);
public sealed record AtualizarItemEstoqueRequest(string Nome, string? Descricao, decimal PrecoUnitario);
public sealed record ItemEstoqueResponse(
    Guid Id, string Nome, string? Descricao, string Tipo, string UnidadeMedida,
    decimal PrecoUnitario, decimal SaldoFisico, decimal SaldoReservado, decimal SaldoDisponivel, bool Ativo)
{
    public static ItemEstoqueResponse De(ItemEstoque item) =>
        new(item.Id, item.Nome, item.Descricao, item.Tipo.ToString(), item.UnidadeMedida,
            item.PrecoUnitario, item.SaldoFisico, item.SaldoReservado, item.SaldoDisponivel, item.Ativo);
}

public sealed record DefinirAtivacaoRequest(bool Ativo);
public sealed record RegistrarEntradaEstoqueRequest(decimal Quantidade);
public sealed record EntradaEstoqueResponse(
    Guid Id, Guid ItemEstoqueId, decimal Quantidade, DateTimeOffset RegistradaEmUtc, Guid UsuarioId)
{
    public static EntradaEstoqueResponse De(EntradaEstoque entrada) =>
        new(entrada.Id, entrada.ItemEstoqueId, entrada.Quantidade, entrada.RegistradaEmUtc, entrada.UsuarioId);
}
