namespace OficinaMecanicaFase1.Domain.OrdensServico;

public enum SituacaoNotificacaoStatusOrdemServico
{
    Pendente = 1,
    EmProcessamento = 2,
    Enviada = 3
}

public sealed class NotificacaoStatusOrdemServico
{
    private NotificacaoStatusOrdemServico() { }

    public Guid Id { get; private set; }
    public Guid MarcoHistoricoId { get; private set; }
    public Guid OrdemServicoId { get; private set; }
    public long NumeroOrdemServico { get; private set; }
    public StatusOrdemServico Status { get; private set; }
    public string Destinatario { get; private set; } = null!;
    public string Assunto { get; private set; } = null!;
    public string Corpo { get; private set; } = null!;
    public SituacaoNotificacaoStatusOrdemServico Situacao { get; private set; }
    public int Tentativas { get; private set; }
    public DateTimeOffset CriadaEmUtc { get; private set; }
    public DateTimeOffset ProximaTentativaEmUtc { get; private set; }
    public DateTimeOffset? ProcessandoAteUtc { get; private set; }
    public DateTimeOffset? EnviadaEmUtc { get; private set; }
    public string? UltimoErro { get; private set; }

    public static NotificacaoStatusOrdemServico CriarPendente(
        Guid marcoHistoricoId, Guid ordemServicoId, long numeroOrdemServico, StatusOrdemServico status,
        string destinatario, string assunto, string corpo, DateTimeOffset agora)
    {
        if (marcoHistoricoId == Guid.Empty) throw new ArgumentException("Informe o marco da OS.", nameof(marcoHistoricoId));
        if (ordemServicoId == Guid.Empty) throw new ArgumentException("Informe a ordem de serviço.", nameof(ordemServicoId));
        if (numeroOrdemServico <= 0) throw new ArgumentOutOfRangeException(nameof(numeroOrdemServico));
        if (string.IsNullOrWhiteSpace(destinatario) || destinatario.Trim().Length > 254)
            throw new ArgumentException("Informe o destinatário da notificação.", nameof(destinatario));
        if (string.IsNullOrWhiteSpace(assunto) || assunto.Trim().Length > 250)
            throw new ArgumentException("Informe o assunto da notificação.", nameof(assunto));
        if (string.IsNullOrWhiteSpace(corpo) || corpo.Trim().Length > 4000)
            throw new ArgumentException("Informe o corpo da notificação.", nameof(corpo));

        var dataUtc = agora.ToUniversalTime();
        return new NotificacaoStatusOrdemServico
        {
            Id = Guid.NewGuid(),
            MarcoHistoricoId = marcoHistoricoId,
            OrdemServicoId = ordemServicoId,
            NumeroOrdemServico = numeroOrdemServico,
            Status = status,
            Destinatario = destinatario.Trim(),
            Assunto = assunto.Trim(),
            Corpo = corpo.Trim(),
            Situacao = SituacaoNotificacaoStatusOrdemServico.Pendente,
            CriadaEmUtc = dataUtc,
            ProximaTentativaEmUtc = dataUtc
        };
    }
}
