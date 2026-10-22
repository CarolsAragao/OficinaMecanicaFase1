using System.Security.Cryptography;

namespace OficinaMecanicaFase1.Domain.OrdensServico;

public enum SituacaoReservaOrdemServico { Aguardando = 0, Confirmada = 1, Recusada = 2 }

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
    public DateTimeOffset? DiagnosticoIniciadoEmUtc { get; private set; }
    public Guid? DiagnosticoIniciadoPorId { get; private set; }
    public DateTimeOffset? DiagnosticoRegistradoEmUtc { get; private set; }
    public Guid? DiagnosticoRegistradoPorId { get; private set; }
    public string? DiagnosticoDescricao { get; private set; }
    public byte[]? CodigoAcessoHash { get; private set; }
    public int NumeroEmissoesCodigoAcesso { get; private set; }
    public DateTimeOffset? CodigoAcessoEmitidoEmUtc { get; private set; }
    public DateTimeOffset? OrcamentoAprovadoEmUtc { get; private set; }
    public DateTimeOffset? EncerradaPorRejeicaoEmUtc { get; private set; }
    public string? MotivoEncerramento { get; private set; }
    public SituacaoReservaOrdemServico SituacaoReserva { get; private set; }
    public DateTimeOffset? ReservaConfirmadaEmUtc { get; private set; }
    public DateTimeOffset? ReservaRecusadaEmUtc { get; private set; }
    public DateTimeOffset? ExecucaoIniciadaEmUtc { get; private set; }
    public Guid? ExecucaoIniciadaPorId { get; private set; }
    public DateTimeOffset? EntregueEmUtc { get; private set; }
    public Guid? EntreguePorId { get; private set; }
    public bool PodeIniciarExecucao => Status == StatusOrdemServico.AguardandoAprovacao &&
        OrcamentoAprovadoEmUtc is not null &&
        EncerradaPorRejeicaoEmUtc is null && SituacaoReserva == SituacaoReservaOrdemServico.Confirmada;
    public byte[] Versao { get; private set; } = [];
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

    public MarcoOrdemServico IniciarDiagnostico(Guid mecanicoId, DateTimeOffset agora)
    {
        if (Status != StatusOrdemServico.Recebida)
            throw new InvalidOperationException("O diagnóstico só pode ser iniciado em uma OS recebida.");
        if (mecanicoId == Guid.Empty) throw new ArgumentException("Informe o mecânico.", nameof(mecanicoId));
        Status = StatusOrdemServico.EmDiagnostico;
        DiagnosticoIniciadoPorId = mecanicoId;
        DiagnosticoIniciadoEmUtc = agora.ToUniversalTime();
        var marco = new MarcoOrdemServico(Id, Status, "Diagnóstico iniciado", agora);
        _marcos.Add(marco);
        return marco;
    }

    public MarcoOrdemServico RegistrarDiagnostico(Guid mecanicoId, string descricao, DateTimeOffset agora)
    {
        if (Status != StatusOrdemServico.EmDiagnostico || DiagnosticoIniciadoEmUtc is null || DiagnosticoRegistradoEmUtc is not null)
            throw new InvalidOperationException("Inicie o diagnóstico antes de registrá-lo; um diagnóstico já registrado não pode ser repetido.");
        if (mecanicoId == Guid.Empty) throw new ArgumentException("Informe o mecânico.", nameof(mecanicoId));
        if (string.IsNullOrWhiteSpace(descricao) || descricao.Trim().Length > 4000)
            throw new ArgumentException("Descreva o diagnóstico em até 4000 caracteres.", nameof(descricao));
        DiagnosticoDescricao = descricao.Trim();
        DiagnosticoRegistradoPorId = mecanicoId;
        DiagnosticoRegistradoEmUtc = agora.ToUniversalTime();
        var marco = new MarcoOrdemServico(Id, Status, "Diagnóstico registrado", agora);
        _marcos.Add(marco);
        return marco;
    }

    public MarcoOrdemServico DisponibilizarOrcamento(DateTimeOffset agora)
    {
        if (Status != StatusOrdemServico.EmDiagnostico || DiagnosticoRegistradoEmUtc is null)
            throw new InvalidOperationException("Registre o diagnóstico antes de gerar o orçamento.");
        Status = StatusOrdemServico.AguardandoAprovacao;
        var marco = new MarcoOrdemServico(Id, Status, "Orçamento disponibilizado", agora);
        _marcos.Add(marco);
        return marco;
    }

    public EmissaoCodigoAcesso EmitirCodigoAcesso(byte[] hash, Guid atendenteId,
        string? motivo, DateTimeOffset agora)
    {
        if ((Status != StatusOrdemServico.AguardandoAprovacao && Status != StatusOrdemServico.EmExecucao) ||
            EncerradaPorRejeicaoEmUtc is not null)
            throw new InvalidOperationException("O código só pode ser emitido para um orçamento pendente.");
        if (hash.Length != 32) throw new ArgumentException("Hash do código inválido.", nameof(hash));
        if (atendenteId == Guid.Empty) throw new ArgumentException("Informe o atendente.", nameof(atendenteId));
        if (NumeroEmissoesCodigoAcesso > 0 && string.IsNullOrWhiteSpace(motivo))
            throw new ArgumentException("Informe o motivo da reemissão.", nameof(motivo));
        if (motivo?.Trim().Length > 500)
            throw new ArgumentException("O motivo deve ter até 500 caracteres.", nameof(motivo));
        CodigoAcessoHash = hash.ToArray();
        CodigoAcessoEmitidoEmUtc = agora.ToUniversalTime();
        NumeroEmissoesCodigoAcesso++;
        return new EmissaoCodigoAcesso(Id, NumeroEmissoesCodigoAcesso, atendenteId,
            string.IsNullOrWhiteSpace(motivo) ? null : motivo.Trim(), agora);
    }

    public bool ValidarCodigoAcesso(byte[] hash) =>
        CodigoAcessoHash is { Length: 32 } && hash.Length == 32 &&
        CryptographicOperations.FixedTimeEquals(CodigoAcessoHash, hash);

    public MarcoOrdemServico RegistrarAprovacaoOrcamento(DateTimeOffset agora)
    {
        if (Status != StatusOrdemServico.AguardandoAprovacao ||
            OrcamentoAprovadoEmUtc is not null || EncerradaPorRejeicaoEmUtc is not null)
            throw new InvalidOperationException("A OS não aguarda decisão do orçamento.");
        OrcamentoAprovadoEmUtc = agora.ToUniversalTime();
        var marco = new MarcoOrdemServico(Id, Status, "Orçamento aprovado", agora);
        _marcos.Add(marco);
        return marco;
    }

    public (MarcoOrdemServico Rejeicao, MarcoOrdemServico Encerramento) EncerrarPorRejeicao(
        string motivo, DateTimeOffset agora)
    {
        if (Status != StatusOrdemServico.AguardandoAprovacao ||
            OrcamentoAprovadoEmUtc is not null || EncerradaPorRejeicaoEmUtc is not null)
            throw new InvalidOperationException("A OS não aguarda decisão do orçamento.");
        if (string.IsNullOrWhiteSpace(motivo) || motivo.Trim().Length > 1000)
            throw new ArgumentException("Informe o motivo da rejeição em até 1000 caracteres.", nameof(motivo));
        EncerradaPorRejeicaoEmUtc = agora.ToUniversalTime();
        MotivoEncerramento = motivo.Trim();
        var rejeicao = new MarcoOrdemServico(Id, Status, "Orçamento rejeitado", agora);
        var encerramento = new MarcoOrdemServico(Id, Status, "OS encerrada por rejeição", agora);
        _marcos.Add(rejeicao);
        _marcos.Add(encerramento);
        return (rejeicao, encerramento);
    }

    public MarcoOrdemServico ConfirmarReserva(DateTimeOffset agora)
    {
        ValidarTentativaReserva();
        SituacaoReserva = SituacaoReservaOrdemServico.Confirmada;
        ReservaConfirmadaEmUtc = agora.ToUniversalTime();
        ReservaRecusadaEmUtc = null;
        var marco = new MarcoOrdemServico(Id, Status, "Itens de estoque reservados para a OS", agora);
        _marcos.Add(marco);
        return marco;
    }

    public MarcoOrdemServico RecusarReserva(DateTimeOffset agora)
    {
        ValidarTentativaReserva();
        SituacaoReserva = SituacaoReservaOrdemServico.Recusada;
        ReservaRecusadaEmUtc = agora.ToUniversalTime();
        var marco = new MarcoOrdemServico(Id, Status, "Reserva recusada; Estoquista notificado", agora);
        _marcos.Add(marco);
        return marco;
    }

    private void ValidarTentativaReserva()
    {
        if (Status != StatusOrdemServico.AguardandoAprovacao ||
            OrcamentoAprovadoEmUtc is null || EncerradaPorRejeicaoEmUtc is not null ||
            SituacaoReserva == SituacaoReservaOrdemServico.Confirmada)
            throw new InvalidOperationException("A OS não permite uma tentativa de reserva.");
    }

    public MarcoOrdemServico IniciarExecucao(Guid mecanicoId, DateTimeOffset agora)
    {
        if (!PodeIniciarExecucao)
            throw new InvalidOperationException("A execução exige orçamento aprovado e reserva confirmada.");
        if (mecanicoId == Guid.Empty) throw new ArgumentException("Informe o mecânico.", nameof(mecanicoId));
        Status = StatusOrdemServico.EmExecucao;
        ExecucaoIniciadaEmUtc = agora.ToUniversalTime();
        ExecucaoIniciadaPorId = mecanicoId;
        var marco = new MarcoOrdemServico(Id, Status, "Execução iniciada", agora);
        _marcos.Add(marco);
        return marco;
    }

    public MarcoOrdemServico IniciarServico(Guid servicoId, Guid mecanicoId, DateTimeOffset agora)
    {
        if (Status != StatusOrdemServico.EmExecucao)
            throw new InvalidOperationException("A OS precisa estar em execução para iniciar um serviço.");
        var servico = _servicosSolicitados.SingleOrDefault(item => item.ServicoCatalogoId == servicoId);
        if (servico is null) throw new ArgumentException("O serviço não foi autorizado nesta OS.", nameof(servicoId));
        servico.Iniciar(mecanicoId, agora);
        var marco = new MarcoOrdemServico(Id, Status, "Serviço iniciado", agora);
        _marcos.Add(marco);
        return marco;
    }

    public MarcoOrdemServico ConcluirServico(Guid servicoId, Guid mecanicoId, DateTimeOffset agora)
    {
        if (Status != StatusOrdemServico.EmExecucao)
            throw new InvalidOperationException("A OS precisa estar em execução para concluir um serviço.");
        var servico = _servicosSolicitados.SingleOrDefault(item => item.ServicoCatalogoId == servicoId);
        if (servico is null) throw new ArgumentException("O serviço não foi autorizado nesta OS.", nameof(servicoId));
        servico.Concluir(mecanicoId, agora);
        var marco = new MarcoOrdemServico(Id, Status, "Serviço concluído", agora);
        _marcos.Add(marco);
        if (_servicosSolicitados.All(item => item.ConcluidoEmUtc is not null))
        {
            Status = StatusOrdemServico.Finalizada;
            _marcos.Add(new MarcoOrdemServico(Id, Status, "OS finalizada", agora));
        }
        return marco;
    }

    public bool AutorizaItem(Guid itemEstoqueId) =>
        _itensNecessarios.Any(item => item.ItemEstoqueId == itemEstoqueId);

    public MarcoOrdemServico RegistrarMarcoAditivo(string descricao, DateTimeOffset agora)
    {
        if (Status != StatusOrdemServico.EmExecucao)
            throw new InvalidOperationException("Aditivos só podem ser registrados durante a execução.");
        var marco = new MarcoOrdemServico(Id, Status, descricao, agora);
        _marcos.Add(marco);
        return marco;
    }

    public MarcoOrdemServico RegistrarEntrega(Guid atendenteId, DateTimeOffset agora)
    {
        if (Status != StatusOrdemServico.Finalizada)
            throw new InvalidOperationException("A OS só pode ser entregue depois de finalizada.");
        if (atendenteId == Guid.Empty) throw new ArgumentException("Informe o atendente.", nameof(atendenteId));
        Status = StatusOrdemServico.Entregue;
        EntregueEmUtc = agora.ToUniversalTime();
        EntreguePorId = atendenteId;
        var marco = new MarcoOrdemServico(Id, Status, "OS entregue ao cliente", agora);
        _marcos.Add(marco);
        return marco;
    }

    public (ServicoSolicitado? Servico, ItemNecessario? Item) AutorizarAditivo(
        Guid? servicoId, Guid? itemId, decimal quantidade)
    {
        if (Status != StatusOrdemServico.EmExecucao)
            throw new InvalidOperationException("A OS não está em execução.");
        ServicoSolicitado? novoServico = null;
        ItemNecessario? novoItem = null;
        if (servicoId is { } id && _servicosSolicitados.All(s => s.ServicoCatalogoId != id))
        {
            novoServico = new ServicoSolicitado(Id, id);
            _servicosSolicitados.Add(novoServico);
        }
        if (itemId is { } estoqueId && _itensNecessarios.All(i => i.ItemEstoqueId != estoqueId))
        {
            novoItem = new ItemNecessario(Id, estoqueId, quantidade, 0);
            _itensNecessarios.Add(novoItem);
        }
        return (novoServico, novoItem);
    }
}
