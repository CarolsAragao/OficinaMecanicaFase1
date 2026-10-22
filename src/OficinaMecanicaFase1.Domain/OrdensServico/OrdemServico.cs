namespace OficinaMecanicaFase1.Domain.OrdensServico;

public sealed class OrdemServico
{
    private readonly List<ServicoSolicitado> _servicosSolicitados = [];
    private readonly List<ItemNecessario> _itensNecessarios = [];
    private readonly List<MarcoOrdemServico> _marcos = [];
    private OrdemServico() { }

    public Guid Id { get; private set; }
    public long Numero { get; private set; }
    public Guid ClienteId { get; private set; }
    public Guid VeiculoId { get; private set; }
    public StatusOrdemServico Status { get; private set; }
    public DateTimeOffset CriadaEmUtc { get; private set; }
    public IReadOnlyCollection<ServicoSolicitado> ServicosSolicitados => _servicosSolicitados;
    public IReadOnlyCollection<ItemNecessario> ItensNecessarios => _itensNecessarios;
    public IReadOnlyCollection<MarcoOrdemServico> Marcos => _marcos;

    public static OrdemServico Criar(
        Guid clienteId, Guid veiculoId, IEnumerable<Guid> servicosIds,
        IEnumerable<(Guid ItemEstoqueId, decimal Quantidade, decimal SaldoDisponivel)> itens,
        DateTimeOffset criadaEmUtc)
    {
        if (clienteId == Guid.Empty) throw new ArgumentException("Informe o cliente da ordem de serviço.", nameof(clienteId));
        if (veiculoId == Guid.Empty) throw new ArgumentException("Informe o veículo da ordem de serviço.", nameof(veiculoId));
        var servicos = servicosIds.ToArray();
        if (servicos.Length == 0 || servicos.Any(id => id == Guid.Empty) || servicos.Distinct().Count() != servicos.Length)
            throw new ArgumentException("Informe ao menos um serviço solicitado, sem repetições.", nameof(servicosIds));
        var itensNecessarios = itens.ToArray();
        if (itensNecessarios.Select(item => item.ItemEstoqueId).Distinct().Count() != itensNecessarios.Length)
            throw new ArgumentException("Os itens necessários não podem se repetir.", nameof(itens));

        var dataUtc = criadaEmUtc.ToUniversalTime();
        var ordem = new OrdemServico
        {
            Id = Guid.NewGuid(),
            ClienteId = clienteId,
            VeiculoId = veiculoId,
            Status = StatusOrdemServico.Recebida,
            CriadaEmUtc = dataUtc
        };
        foreach (var servicoId in servicos)
            ordem._servicosSolicitados.Add(new ServicoSolicitado(ordem.Id, servicoId));
        foreach (var item in itensNecessarios)
            ordem._itensNecessarios.Add(new ItemNecessario(ordem.Id, item.ItemEstoqueId, item.Quantidade, item.SaldoDisponivel));
        ordem._marcos.Add(new MarcoOrdemServico(ordem.Id, ordem.Status, "OS recebida", dataUtc));
        return ordem;
    }

    public ServicoSolicitado AdicionarServicoSolicitado(Guid servicoId)
    {
        if (Status is not (StatusOrdemServico.Recebida or StatusOrdemServico.EmDiagnostico))
            throw new InvalidOperationException("Serviços só podem ser adicionados antes da aprovação do orçamento.");
        if (servicoId == Guid.Empty || _servicosSolicitados.Any(servico => servico.ServicoCatalogoId == servicoId))
            throw new ArgumentException("O serviço informado é inválido ou já foi solicitado nesta OS.", nameof(servicoId));
        var servico = new ServicoSolicitado(Id, servicoId);
        _servicosSolicitados.Add(servico);
        return servico;
    }

    public ItemNecessario AdicionarItemNecessario(Guid itemEstoqueId, decimal quantidade, decimal saldoDisponivel)
    {
        if (Status is not (StatusOrdemServico.Recebida or StatusOrdemServico.EmDiagnostico))
            throw new InvalidOperationException("Itens necessários só podem ser adicionados antes da aprovação do orçamento.");
        if (itemEstoqueId == Guid.Empty || _itensNecessarios.Any(item => item.ItemEstoqueId == itemEstoqueId))
            throw new ArgumentException("O item informado é inválido ou já foi adicionado nesta OS.", nameof(itemEstoqueId));
        var item = new ItemNecessario(Id, itemEstoqueId, quantidade, saldoDisponivel);
        _itensNecessarios.Add(item);
        return item;
    }
}
