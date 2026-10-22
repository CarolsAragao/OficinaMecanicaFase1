namespace OficinaMecanicaFase1.Domain.Orcamentos;

public enum TipoLinhaOrcamento { Servico = 1, Item = 2 }

public sealed class LinhaOrcamento
{
    private LinhaOrcamento() { }

    public Guid Id { get; private set; }
    public Guid OrcamentoId { get; private set; }
    public TipoLinhaOrcamento Tipo { get; private set; }
    public Guid ReferenciaId { get; private set; }
    public string Descricao { get; private set; } = null!;
    public string UnidadeMedida { get; private set; } = null!;
    public decimal Quantidade { get; private set; }
    public decimal PrecoUnitario { get; private set; }
    public decimal ValorTotal { get; private set; }

    internal LinhaOrcamento(Guid orcamentoId, TipoLinhaOrcamento tipo, Guid referenciaId,
        string descricao, string unidadeMedida, decimal quantidade, decimal precoUnitario)
    {
        if (orcamentoId == Guid.Empty || referenciaId == Guid.Empty || string.IsNullOrWhiteSpace(descricao) ||
            string.IsNullOrWhiteSpace(unidadeMedida) || quantidade <= 0 || precoUnitario < 0)
            throw new ArgumentException("Dados da linha de orçamento inválidos.");
        var valor = decimal.Round(checked(quantidade * precoUnitario), 2, MidpointRounding.AwayFromZero);
        if (valor > 9_999_999_999_999_999.99m)
            throw new ArgumentException("O valor da linha excede o limite do orçamento.");
        Id = Guid.NewGuid();
        OrcamentoId = orcamentoId;
        Tipo = tipo;
        ReferenciaId = referenciaId;
        Descricao = descricao;
        UnidadeMedida = unidadeMedida;
        Quantidade = quantidade;
        PrecoUnitario = precoUnitario;
        ValorTotal = valor;
    }
}
