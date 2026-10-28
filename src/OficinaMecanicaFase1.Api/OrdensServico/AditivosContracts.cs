using OficinaMecanicaFase1.Domain.Orcamentos;
using OficinaMecanicaFase1.Infrastructure.Persistence;

namespace OficinaMecanicaFase1.Api.OrdensServico;

public sealed record CriarAditivoRequest(TipoAditivo Tipo, string Descricao, Guid? ServicoCatalogoId,
    Guid? ItemEstoqueId, decimal Quantidade, bool OriginalPodeProsseguir);
public sealed record ConsultarAditivoRequest(string Documento);
public sealed record DecidirAditivoRequest(string Documento, string CodigoAcesso, bool Aprovar, string? Motivo);
public sealed record AditivoResponse(Guid Id, long NumeroOrdemServico, string Tipo, string Descricao,
    Guid? ServicoCatalogoId, string? NomeServico, Guid? ItemEstoqueId, string? NomeItem,
    decimal Quantidade, decimal PrecoServico,
    decimal PrecoItem, decimal Total, bool OriginalPodeProsseguir, string Situacao,
    string SituacaoReserva, DateTimeOffset CriadoEmUtc, DateTimeOffset? DecididoEmUtc,
    string? MotivoRejeicao)
{
    public static AditivoResponse De(Aditivo a, long numero) => new(a.Id, numero, a.Tipo.ToString(),
        a.Descricao, a.ServicoCatalogoId, a.NomeServico, a.ItemEstoqueId, a.NomeItem,
        a.Quantidade, a.PrecoServico,
        a.PrecoItem, a.Total, a.OriginalPodeProsseguir, a.Situacao.ToString(),
        a.SituacaoReserva.ToString(), a.CriadoEmUtc, a.DecididoEmUtc, a.MotivoRejeicao);
}
