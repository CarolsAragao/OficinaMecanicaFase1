namespace OficinaMecanicaFase1.Application.OrdensServico;

public sealed record AbrirOrdemServicoEntrada(
    Guid ClienteId,
    Guid VeiculoId,
    IReadOnlyCollection<Guid> ServicosIds,
    IReadOnlyCollection<ItemAberturaOrdemServico> Itens);

public sealed record ItemAberturaOrdemServico(Guid ItemEstoqueId, decimal Quantidade);
