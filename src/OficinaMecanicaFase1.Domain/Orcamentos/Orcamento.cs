namespace OficinaMecanicaFase1.Domain.Orcamentos;

public sealed class Orcamento
{
    private readonly List<LinhaOrcamento> _linhas = [];
    private Orcamento() { }

    public Guid Id { get; private set; }
    public Guid OrdemServicoId { get; private set; }
    public DateTimeOffset GeradoEmUtc { get; private set; }
    public decimal TotalServicos { get; private set; }
    public decimal TotalItens { get; private set; }
    public decimal Total { get; private set; }
    public IReadOnlyCollection<LinhaOrcamento> Linhas => _linhas;

    public static Orcamento Gerar(Guid ordemServicoId, DateTimeOffset agora,
        IEnumerable<(Guid Id, string Nome, decimal Preco)> servicos,
        IEnumerable<(Guid Id, string Nome, string Unidade, decimal Quantidade, decimal Preco)> itens)
    {
        if (ordemServicoId == Guid.Empty) throw new ArgumentException("Informe a OS.", nameof(ordemServicoId));
        var orcamento = new Orcamento
        {
            Id = Guid.NewGuid(), OrdemServicoId = ordemServicoId, GeradoEmUtc = agora.ToUniversalTime()
        };
        foreach (var servico in servicos)
            orcamento._linhas.Add(new LinhaOrcamento(orcamento.Id, TipoLinhaOrcamento.Servico,
                servico.Id, servico.Nome, "serviço", 1, servico.Preco));
        foreach (var item in itens)
            orcamento._linhas.Add(new LinhaOrcamento(orcamento.Id, TipoLinhaOrcamento.Item,
                item.Id, item.Nome, item.Unidade, item.Quantidade, item.Preco));
        if (orcamento._linhas.Count == 0) throw new ArgumentException("O orçamento precisa de serviços ou itens.");
        orcamento.TotalServicos = checked(orcamento._linhas.Where(linha => linha.Tipo == TipoLinhaOrcamento.Servico)
            .Sum(linha => linha.ValorTotal));
        orcamento.TotalItens = checked(orcamento._linhas.Where(linha => linha.Tipo == TipoLinhaOrcamento.Item)
            .Sum(linha => linha.ValorTotal));
        orcamento.Total = checked(orcamento.TotalServicos + orcamento.TotalItens);
        if (orcamento.Total > 9_999_999_999_999_999.99m)
            throw new ArgumentException("O total excede o limite do orçamento.");
        return orcamento;
    }
}
