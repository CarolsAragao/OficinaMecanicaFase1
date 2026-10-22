namespace OficinaMecanicaFase1.Domain.Orcamentos;

public enum TipoAditivo { ReparoAdicional = 1, ConsumoExcedente = 2 }
public enum SituacaoAditivo { Pendente = 0, Aprovado = 1, Rejeitado = 2 }
public enum SituacaoReservaAditivo { Aguardando = 0, Confirmada = 1, Recusada = 2 }

public sealed class Aditivo
{
    private Aditivo() { }
    public Guid Id { get; private set; }
    public Guid OrdemServicoId { get; private set; }
    public TipoAditivo Tipo { get; private set; }
    public string Descricao { get; private set; } = null!;
    public Guid? ServicoCatalogoId { get; private set; }
    public string? NomeServico { get; private set; }
    public Guid? ItemEstoqueId { get; private set; }
    public string? NomeItem { get; private set; }
    public decimal Quantidade { get; private set; }
    public decimal PrecoServico { get; private set; }
    public decimal PrecoItem { get; private set; }
    public decimal Total { get; private set; }
    public bool OriginalPodeProsseguir { get; private set; }
    public Guid IdentificadoPorId { get; private set; }
    public DateTimeOffset CriadoEmUtc { get; private set; }
    public SituacaoAditivo Situacao { get; private set; }
    public DateTimeOffset? DecididoEmUtc { get; private set; }
    public string? MotivoRejeicao { get; private set; }
    public SituacaoReservaAditivo SituacaoReserva { get; private set; }
    public byte[] Versao { get; private set; } = [];

    public static Aditivo Criar(Guid ordemId, TipoAditivo tipo, string descricao, Guid? servicoId,
        string? nomeServico, Guid? itemId, string? nomeItem, decimal quantidade, decimal precoServico, decimal precoItem,
        bool originalPodeProsseguir, Guid mecanicoId, DateTimeOffset agora)
    {
        if (ordemId == Guid.Empty || mecanicoId == Guid.Empty ||
            !Enum.IsDefined(tipo) || (tipo == TipoAditivo.ReparoAdicional && servicoId is null) ||
            (tipo == TipoAditivo.ConsumoExcedente && (servicoId is not null || itemId is null)) ||
            string.IsNullOrWhiteSpace(descricao) || descricao.Trim().Length > 1000 ||
            (itemId is null && quantidade != 0) || (itemId is not null &&
                (itemId == Guid.Empty || quantidade <= 0 || decimal.Round(quantidade, 3) != quantidade)) ||
            precoServico < 0 || precoItem < 0)
            throw new ArgumentException("Dados do aditivo inválidos.");
        var total = checked(precoServico + decimal.Round(quantidade * precoItem, 2, MidpointRounding.AwayFromZero));
        return new Aditivo { Id = Guid.NewGuid(), OrdemServicoId = ordemId, Tipo = tipo,
            Descricao = descricao.Trim(), ServicoCatalogoId = servicoId, NomeServico = nomeServico,
            ItemEstoqueId = itemId, NomeItem = nomeItem,
            Quantidade = quantidade, PrecoServico = precoServico, PrecoItem = precoItem, Total = total,
            OriginalPodeProsseguir = originalPodeProsseguir, IdentificadoPorId = mecanicoId,
            CriadoEmUtc = agora.ToUniversalTime() };
    }

    public void Decidir(bool aprovar, string? motivo, DateTimeOffset agora)
    {
        if (Situacao != SituacaoAditivo.Pendente) throw new InvalidOperationException("O aditivo já foi decidido.");
        if (!aprovar && (string.IsNullOrWhiteSpace(motivo) || motivo.Trim().Length > 1000))
            throw new ArgumentException("Informe o motivo da rejeição em até 1000 caracteres.", nameof(motivo));
        Situacao = aprovar ? SituacaoAditivo.Aprovado : SituacaoAditivo.Rejeitado;
        MotivoRejeicao = aprovar ? null : motivo!.Trim();
        DecididoEmUtc = agora.ToUniversalTime();
    }

    public void RegistrarReserva(bool confirmada)
    {
        if (Situacao != SituacaoAditivo.Aprovado || SituacaoReserva == SituacaoReservaAditivo.Confirmada)
            throw new InvalidOperationException("O aditivo não aguarda reserva.");
        SituacaoReserva = confirmada ? SituacaoReservaAditivo.Confirmada : SituacaoReservaAditivo.Recusada;
    }
}
