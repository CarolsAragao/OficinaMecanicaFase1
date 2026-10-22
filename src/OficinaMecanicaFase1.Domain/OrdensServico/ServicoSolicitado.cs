namespace OficinaMecanicaFase1.Domain.OrdensServico;

public sealed class ServicoSolicitado
{
    private ServicoSolicitado() { }

    public Guid Id { get; private set; }
    public Guid OrdemServicoId { get; private set; }
    public Guid ServicoCatalogoId { get; private set; }

    internal ServicoSolicitado(Guid ordemServicoId, Guid servicoCatalogoId)
    {
        if (ordemServicoId == Guid.Empty || servicoCatalogoId == Guid.Empty)
            throw new ArgumentException("Informe a ordem e o serviço solicitado.");
        Id = Guid.NewGuid();
        OrdemServicoId = ordemServicoId;
        ServicoCatalogoId = servicoCatalogoId;
    }
}
