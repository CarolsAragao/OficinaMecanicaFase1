namespace OficinaMecanicaFase1.Domain.OrdensServico;

public sealed class RegistroHistoricoOrdemServico
{
    private RegistroHistoricoOrdemServico() { }

    public Guid Id { get; private set; }
    public Guid OrdemServicoId { get; private set; }
    public StatusOrdemServico Status { get; private set; }
    public string Descricao { get; private set; } = null!;
    public DateTimeOffset RegistradoEmUtc { get; private set; }

    internal RegistroHistoricoOrdemServico(Guid ordemServicoId, StatusOrdemServico status, string descricao, DateTimeOffset registradoEmUtc)
    {
        Id = Guid.NewGuid();
        OrdemServicoId = ordemServicoId;
        Status = status;
        Descricao = descricao;
        RegistradoEmUtc = registradoEmUtc.ToUniversalTime();
    }
}
