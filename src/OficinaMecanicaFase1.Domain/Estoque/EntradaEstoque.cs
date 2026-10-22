namespace OficinaMecanicaFase1.Domain.Estoque;

public sealed class EntradaEstoque
{
    private EntradaEstoque() { }

    public Guid Id { get; private set; }
    public Guid ItemEstoqueId { get; private set; }
    public decimal Quantidade { get; private set; }
    public DateTimeOffset RegistradaEmUtc { get; private set; }
    public Guid UsuarioId { get; private set; }

    internal EntradaEstoque(Guid itemEstoqueId, decimal quantidade, DateTimeOffset registradaEmUtc, Guid usuarioId)
    {
        Id = Guid.NewGuid();
        ItemEstoqueId = itemEstoqueId;
        Quantidade = quantidade;
        RegistradaEmUtc = registradaEmUtc;
        UsuarioId = usuarioId;
    }
}
