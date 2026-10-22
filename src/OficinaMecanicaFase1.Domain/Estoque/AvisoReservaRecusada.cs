namespace OficinaMecanicaFase1.Domain.Estoque;

public sealed class AvisoReservaRecusada
{
    private AvisoReservaRecusada() { }

    public Guid Id { get; private set; }
    public Guid OrdemServicoId { get; private set; }
    public Guid ItemEstoqueId { get; private set; }
    public decimal QuantidadeSolicitada { get; private set; }
    public decimal SaldoDisponivelObservado { get; private set; }
    public DateTimeOffset RegistradoEmUtc { get; private set; }
    public DateTimeOffset? ResolvidoEmUtc { get; private set; }

    public AvisoReservaRecusada(Guid ordemServicoId, Guid itemEstoqueId,
        decimal quantidadeSolicitada, decimal saldoDisponivelObservado, DateTimeOffset agora)
    {
        if (ordemServicoId == Guid.Empty || itemEstoqueId == Guid.Empty ||
            quantidadeSolicitada <= 0 || decimal.Round(quantidadeSolicitada, 3) != quantidadeSolicitada ||
            saldoDisponivelObservado < 0)
            throw new ArgumentException("Dados do aviso de reserva inválidos.");
        Id = Guid.NewGuid();
        OrdemServicoId = ordemServicoId;
        ItemEstoqueId = itemEstoqueId;
        QuantidadeSolicitada = quantidadeSolicitada;
        SaldoDisponivelObservado = saldoDisponivelObservado;
        RegistradoEmUtc = agora.ToUniversalTime();
    }

    public void Resolver(DateTimeOffset agora)
    {
        if (ResolvidoEmUtc is not null) throw new InvalidOperationException("O aviso já foi resolvido.");
        ResolvidoEmUtc = agora.ToUniversalTime();
    }
}
