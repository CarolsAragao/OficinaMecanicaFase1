namespace OficinaMecanicaFase1.Domain.Estoque;

public sealed class ConsumoItemOrdemServico
{
    private ConsumoItemOrdemServico() { }

    public Guid Id { get; private set; }
    public Guid OrdemServicoId { get; private set; }
    public Guid ItemEstoqueId { get; private set; }
    public Guid MecanicoId { get; private set; }
    public decimal Quantidade { get; private set; }
    public DateTimeOffset RegistradoEmUtc { get; private set; }

    public static ConsumoItemOrdemServico Criar(Guid ordemServicoId, Guid itemEstoqueId,
        Guid mecanicoId, decimal quantidade, DateTimeOffset agora)
    {
        if (ordemServicoId == Guid.Empty || itemEstoqueId == Guid.Empty || mecanicoId == Guid.Empty ||
            quantidade <= 0 || decimal.Round(quantidade, 3) != quantidade)
            throw new ArgumentException("Dados do consumo inválidos.");
        return new ConsumoItemOrdemServico
        {
            Id = Guid.NewGuid(), OrdemServicoId = ordemServicoId, ItemEstoqueId = itemEstoqueId,
            MecanicoId = mecanicoId, Quantidade = quantidade, RegistradoEmUtc = agora.ToUniversalTime()
        };
    }
}
