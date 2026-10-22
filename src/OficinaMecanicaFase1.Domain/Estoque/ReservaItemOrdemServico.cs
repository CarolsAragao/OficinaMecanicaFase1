namespace OficinaMecanicaFase1.Domain.Estoque;

public sealed class ReservaItemOrdemServico
{
    private ReservaItemOrdemServico() { }

    public Guid Id { get; private set; }
    public Guid OrdemServicoId { get; private set; }
    public Guid ItemEstoqueId { get; private set; }
    public decimal Quantidade { get; private set; }
    public decimal QuantidadeConsumida { get; private set; }
    public decimal QuantidadeRestante => Quantidade - QuantidadeConsumida;
    public DateTimeOffset ReservadaEmUtc { get; private set; }
    public DateTimeOffset? LiberadaEmUtc { get; private set; }

    public static ReservaItemOrdemServico Criar(Guid ordemServicoId, Guid itemEstoqueId,
        decimal quantidade, DateTimeOffset agora)
    {
        if (ordemServicoId == Guid.Empty || itemEstoqueId == Guid.Empty ||
            quantidade <= 0 || decimal.Round(quantidade, 3) != quantidade)
            throw new ArgumentException("Dados da reserva inválidos.");
        return new ReservaItemOrdemServico
        {
            Id = Guid.NewGuid(), OrdemServicoId = ordemServicoId,
            ItemEstoqueId = itemEstoqueId, Quantidade = quantidade,
            ReservadaEmUtc = agora.ToUniversalTime()
        };
    }

    public void Liberar(DateTimeOffset agora)
    {
        if (LiberadaEmUtc is not null) throw new InvalidOperationException("A reserva já foi liberada.");
        LiberadaEmUtc = agora.ToUniversalTime();
    }

    public void RegistrarConsumo(decimal quantidade)
    {
        if (LiberadaEmUtc is not null || quantidade <= 0 || decimal.Round(quantidade, 3) != quantidade ||
            quantidade > QuantidadeRestante)
            throw new InvalidOperationException("O consumo excede a quantidade ainda reservada para a OS.");
        QuantidadeConsumida += quantidade;
    }
}
