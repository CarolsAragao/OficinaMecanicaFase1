namespace OficinaMecanicaFase1.Domain.OrdensServico;

public sealed class ServicoSolicitado
{
    private ServicoSolicitado() { }

    public Guid Id { get; private set; }
    public Guid OrdemServicoId { get; private set; }
    public Guid ServicoCatalogoId { get; private set; }
    public DateTimeOffset? IniciadoEmUtc { get; private set; }
    public Guid? IniciadoPorId { get; private set; }
    public DateTimeOffset? ConcluidoEmUtc { get; private set; }
    public Guid? ConcluidoPorId { get; private set; }

    internal ServicoSolicitado(Guid ordemServicoId, Guid servicoCatalogoId)
    {
        if (ordemServicoId == Guid.Empty || servicoCatalogoId == Guid.Empty)
            throw new ArgumentException("Informe a ordem e o serviço solicitado.");
        Id = Guid.NewGuid();
        OrdemServicoId = ordemServicoId;
        ServicoCatalogoId = servicoCatalogoId;
    }

    internal void Iniciar(Guid mecanicoId, DateTimeOffset agora)
    {
        if (mecanicoId == Guid.Empty) throw new ArgumentException("Informe o mecânico.", nameof(mecanicoId));
        if (IniciadoEmUtc is not null) throw new InvalidOperationException("O serviço já foi iniciado.");
        IniciadoEmUtc = agora.ToUniversalTime();
        IniciadoPorId = mecanicoId;
    }

    internal void Concluir(Guid mecanicoId, DateTimeOffset agora)
    {
        if (mecanicoId == Guid.Empty) throw new ArgumentException("Informe o mecânico.", nameof(mecanicoId));
        if (IniciadoEmUtc is null) throw new InvalidOperationException("Inicie o serviço antes de concluí-lo.");
        if (ConcluidoEmUtc is not null) throw new InvalidOperationException("O serviço já foi concluído.");
        if (agora.ToUniversalTime() < IniciadoEmUtc.Value)
            throw new InvalidOperationException("A conclusão não pode ser anterior ao início do serviço.");
        ConcluidoEmUtc = agora.ToUniversalTime();
        ConcluidoPorId = mecanicoId;
    }
}
