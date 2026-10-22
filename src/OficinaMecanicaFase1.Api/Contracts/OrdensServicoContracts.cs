using OficinaMecanicaFase1.Domain.OrdensServico;

namespace OficinaMecanicaFase1.Api.Contracts;

public sealed record ItemNecessarioRequest(Guid ItemEstoqueId, decimal Quantidade);
public sealed record CriarOrdemServicoRequest(
    Guid ClienteId, Guid VeiculoId, Guid[] ServicosIds, ItemNecessarioRequest[]? ItensNecessarios);
public sealed record AdicionarServicoSolicitadoRequest(Guid ServicoCatalogoId);
public sealed record AdicionarItemNecessarioRequest(Guid ItemEstoqueId, decimal Quantidade);

public sealed record ServicoSolicitadoResponse(Guid ServicoCatalogoId);
public sealed record ItemNecessarioResponse(
    Guid ItemEstoqueId, decimal Quantidade, decimal SaldoDisponivelConsultado);
public sealed record MarcoOrdemServicoResponse(
    string Status, string Descricao, DateTimeOffset RegistradoEmUtc);
public sealed record OrdemServicoResponse(
    Guid Id, long Numero, Guid ClienteId, Guid VeiculoId, string Status,
    DateTimeOffset CriadaEmUtc, ServicoSolicitadoResponse[] ServicosSolicitados,
    ItemNecessarioResponse[] ItensNecessarios, MarcoOrdemServicoResponse[] Marcos)
{
    public static OrdemServicoResponse De(OrdemServico ordem) => new(
        ordem.Id, ordem.Numero, ordem.ClienteId, ordem.VeiculoId, ordem.Status.ToString(),
        ordem.CriadaEmUtc,
        ordem.ServicosSolicitados.Select(servico => new ServicoSolicitadoResponse(servico.ServicoCatalogoId)).ToArray(),
        ordem.ItensNecessarios.Select(item => new ItemNecessarioResponse(
            item.ItemEstoqueId, item.Quantidade, item.SaldoDisponivelConsultado)).ToArray(),
        ordem.Marcos.OrderBy(marco => marco.RegistradoEmUtc)
            .Select(marco => new MarcoOrdemServicoResponse(
                marco.Status.ToString(), marco.Descricao, marco.RegistradoEmUtc)).ToArray());
}
