namespace OficinaMecanicaFase1.Application.OrdensServico;

public enum SituacaoTentativaReserva { Confirmada, Recusada, JaConfirmada, NaoEncontrada, NaoPermitida }

public sealed record ItemReservaInsuficiente(Guid ItemEstoqueId, string Nome,
    decimal QuantidadeSolicitada, decimal SaldoDisponivel);

public sealed record ResultadoTentativaReserva(long NumeroOrdemServico,
    SituacaoTentativaReserva Situacao, ItemReservaInsuficiente[] ItensInsuficientes);

public sealed record AvisoReservaEstoque(Guid Id, long NumeroOrdemServico, Guid ItemEstoqueId,
    string NomeItem, decimal QuantidadeSolicitada, decimal SaldoDisponivelObservado,
    DateTimeOffset RegistradoEmUtc, DateTimeOffset? ResolvidoEmUtc);

public interface IRepositorioReservas
{
    Task<ResultadoTentativaReserva> TentarReservarAsync(long numero, DateTimeOffset agora,
        CancellationToken cancelamento);
    Task<IReadOnlyCollection<AvisoReservaEstoque>> ListarAvisosAsync(bool somentePendentes,
        CancellationToken cancelamento);
}
