using OficinaMecanicaFase1.Domain.OrdensServico;
using OficinaMecanicaFase1.Domain.Orcamentos;
using OficinaMecanicaFase1.Application.OrdensServico;

namespace OficinaMecanicaFase1.Api.Contracts;

public sealed record ItemNecessarioRequest(Guid ItemEstoqueId, decimal Quantidade);
public sealed record CriarOrdemServicoRequest(
    Guid ClienteId, Guid VeiculoId, Guid[] ServicosIds, ItemNecessarioRequest[]? ItensNecessarios);
public sealed record AdicionarServicoSolicitadoRequest(Guid ServicoCatalogoId);
public sealed record AdicionarItemNecessarioRequest(Guid ItemEstoqueId, decimal Quantidade);
public sealed record RegistrarDiagnosticoRequest(string Descricao);
public sealed record EmitirCodigoAcessoRequest(string? Motivo);
public sealed record CodigoAcessoResponse(long NumeroOrdemServico, string Codigo,
    int NumeroEmissao, DateTimeOffset EmitidoEmUtc);
public sealed record ConsultarOrcamentoClienteRequest(string Documento);
public sealed record DecidirOrcamentoClienteRequest(string Documento, string CodigoAcesso,
    bool Aprovar, string? Motivo);

public sealed record DiagnosticoResponse(DateTimeOffset? IniciadoEmUtc, Guid? IniciadoPorId,
    DateTimeOffset? RegistradoEmUtc, Guid? RegistradoPorId, string? Descricao);
public sealed record LinhaOrcamentoResponse(string Tipo, Guid ReferenciaId, string Descricao,
    string UnidadeMedida, decimal Quantidade, decimal PrecoUnitario, decimal ValorTotal);
public sealed record OrcamentoResponse(Guid Id, long NumeroOrdemServico, DateTimeOffset GeradoEmUtc,
    decimal TotalServicos, decimal TotalItens, decimal Total, string Decisao,
    DateTimeOffset? DecididoEmUtc, string? MotivoRejeicao, LinhaOrcamentoResponse[] Linhas)
{
    public static OrcamentoResponse De(Orcamento orcamento, long numero) => new(
        orcamento.Id, numero, orcamento.GeradoEmUtc,
        orcamento.TotalServicos, orcamento.TotalItens, orcamento.Total,
        orcamento.Decisao.ToString(), orcamento.DecididoEmUtc, orcamento.MotivoRejeicao,
        orcamento.Linhas.OrderBy(linha => linha.Tipo).ThenBy(linha => linha.Descricao)
            .Select(linha => new LinhaOrcamentoResponse(linha.Tipo.ToString(), linha.ReferenciaId,
                linha.Descricao, linha.UnidadeMedida, linha.Quantidade, linha.PrecoUnitario, linha.ValorTotal)).ToArray());
}

public sealed record ServicoSolicitadoResponse(Guid ServicoCatalogoId,
    DateTimeOffset? IniciadoEmUtc, Guid? IniciadoPorId,
    DateTimeOffset? ConcluidoEmUtc, Guid? ConcluidoPorId);
public sealed record ItemNecessarioResponse(
    Guid ItemEstoqueId, decimal Quantidade, decimal SaldoDisponivelConsultado);
public sealed record MarcoOrdemServicoResponse(
    string Status, string Descricao, DateTimeOffset RegistradoEmUtc);
public sealed record OrdemServicoResponse(
    Guid Id, long Numero, Guid ClienteId, Guid VeiculoId, string Status,
    DateTimeOffset CriadaEmUtc, DiagnosticoResponse Diagnostico, ServicoSolicitadoResponse[] ServicosSolicitados,
    ItemNecessarioResponse[] ItensNecessarios, MarcoOrdemServicoResponse[] Marcos,
    DateTimeOffset? OrcamentoAprovadoEmUtc, DateTimeOffset? EncerradaPorRejeicaoEmUtc,
    string? MotivoEncerramento, string SituacaoReserva, DateTimeOffset? ReservaConfirmadaEmUtc,
    DateTimeOffset? ReservaRecusadaEmUtc, bool PodeIniciarExecucao,
    DateTimeOffset? ExecucaoIniciadaEmUtc, Guid? ExecucaoIniciadaPorId)
{
    public static OrdemServicoResponse De(OrdemServico ordem) => new(
        ordem.Id, ordem.Numero, ordem.ClienteId, ordem.VeiculoId, ordem.Status.ToString(),
        ordem.CriadaEmUtc, new DiagnosticoResponse(ordem.DiagnosticoIniciadoEmUtc,
            ordem.DiagnosticoIniciadoPorId, ordem.DiagnosticoRegistradoEmUtc,
            ordem.DiagnosticoRegistradoPorId, ordem.DiagnosticoDescricao),
        ordem.ServicosSolicitados.Select(servico => new ServicoSolicitadoResponse(servico.ServicoCatalogoId,
            servico.IniciadoEmUtc, servico.IniciadoPorId, servico.ConcluidoEmUtc, servico.ConcluidoPorId)).ToArray(),
        ordem.ItensNecessarios.Select(item => new ItemNecessarioResponse(
            item.ItemEstoqueId, item.Quantidade, item.SaldoDisponivelConsultado)).ToArray(),
        ordem.Marcos.OrderBy(marco => marco.RegistradoEmUtc)
            .Select(marco => new MarcoOrdemServicoResponse(
                marco.Status.ToString(), marco.Descricao, marco.RegistradoEmUtc)).ToArray(),
        ordem.OrcamentoAprovadoEmUtc, ordem.EncerradaPorRejeicaoEmUtc, ordem.MotivoEncerramento,
        ordem.SituacaoReserva.ToString(), ordem.ReservaConfirmadaEmUtc,
        ordem.ReservaRecusadaEmUtc, ordem.PodeIniciarExecucao,
        ordem.ExecucaoIniciadaEmUtc, ordem.ExecucaoIniciadaPorId);
}

public sealed record ConsumoItemRequest(Guid ItemEstoqueId, decimal Quantidade);
public sealed record RegistrarConsumoRequest(ConsumoItemRequest[]? Consumos);

public sealed record ItemReservaInsuficienteResponse(Guid ItemEstoqueId, string Nome,
    decimal QuantidadeSolicitada, decimal SaldoDisponivel);
public sealed record TentativaReservaResponse(long NumeroOrdemServico, string Situacao,
    ItemReservaInsuficienteResponse[] ItensInsuficientes)
{
    public static TentativaReservaResponse De(ResultadoTentativaReserva resultado) => new(
        resultado.NumeroOrdemServico, resultado.Situacao.ToString(),
        resultado.ItensInsuficientes.Select(item => new ItemReservaInsuficienteResponse(
            item.ItemEstoqueId, item.Nome, item.QuantidadeSolicitada, item.SaldoDisponivel)).ToArray());
}
