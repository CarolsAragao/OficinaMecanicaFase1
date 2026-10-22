namespace OficinaMecanicaFase1.Domain.OrdensServico;

public sealed class EmissaoCodigoAcesso
{
    private EmissaoCodigoAcesso() { }

    public Guid Id { get; private set; }
    public Guid OrdemServicoId { get; private set; }
    public int NumeroEmissao { get; private set; }
    public Guid AtendenteId { get; private set; }
    public string? Motivo { get; private set; }
    public DateTimeOffset EmitidoEmUtc { get; private set; }

    internal EmissaoCodigoAcesso(Guid ordemServicoId, int numeroEmissao,
        Guid atendenteId, string? motivo, DateTimeOffset agora)
    {
        Id = Guid.NewGuid();
        OrdemServicoId = ordemServicoId;
        NumeroEmissao = numeroEmissao;
        AtendenteId = atendenteId;
        Motivo = motivo;
        EmitidoEmUtc = agora.ToUniversalTime();
    }
}
