namespace OficinaMecanicaFase1.Domain.OrdensServico;

public sealed class ItemNecessario
{
    private ItemNecessario() { }

    public Guid Id { get; private set; }
    public Guid OrdemServicoId { get; private set; }
    public Guid ItemEstoqueId { get; private set; }
    public decimal Quantidade { get; private set; }
    public decimal SaldoDisponivelConsultado { get; private set; }

    internal ItemNecessario(Guid ordemServicoId, Guid itemEstoqueId, decimal quantidade, decimal saldoDisponivelConsultado)
    {
        if (ordemServicoId == Guid.Empty || itemEstoqueId == Guid.Empty)
            throw new ArgumentException("Informe a ordem e o item necessário.");
        if (quantidade <= 0 || decimal.Round(quantidade, 3) != quantidade)
            throw new ArgumentException("Informe uma quantidade positiva com até três casas decimais.", nameof(quantidade));
        if (saldoDisponivelConsultado < 0)
            throw new ArgumentException("O saldo disponível consultado não pode ser negativo.", nameof(saldoDisponivelConsultado));
        Id = Guid.NewGuid();
        OrdemServicoId = ordemServicoId;
        ItemEstoqueId = itemEstoqueId;
        Quantidade = quantidade;
        SaldoDisponivelConsultado = saldoDisponivelConsultado;
    }
}
