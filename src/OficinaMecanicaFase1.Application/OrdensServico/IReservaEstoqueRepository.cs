using OficinaMecanicaFase1.Domain.Estoque;
using OficinaMecanicaFase1.Domain.OrdensServico;

namespace OficinaMecanicaFase1.Application.OrdensServico;

public enum SituacaoTentativaReserva { Confirmada, Recusada, JaConfirmada, NaoEncontrada, NaoPermitida }

public sealed record ItemReservaInsuficiente(Guid ItemEstoqueId, string Nome,
    decimal QuantidadeSolicitada, decimal SaldoDisponivel);

public sealed record ResultadoTentativaReserva(long NumeroOrdemServico,
    SituacaoTentativaReserva Situacao, ItemReservaInsuficiente[] ItensInsuficientes);

public sealed record AvisoReservaEstoque(Guid Id, long NumeroOrdemServico, Guid ItemEstoqueId,
    string NomeItem, decimal QuantidadeSolicitada, decimal SaldoDisponivelObservado,
    DateTimeOffset RegistradoEmUtc, DateTimeOffset? ResolvidoEmUtc);

public interface IReservaEstoqueRepository
{
    Task<ResultadoTentativaReserva> ExecutarAsync(long numero,
        Func<IOperacoesReservaEstoque, OrdemServico, Task<ResultadoTentativaReserva>> acao,
        CancellationToken cancelamento);
    Task<IReadOnlyCollection<AvisoReservaEstoque>> ListarAvisosAsync(bool somentePendentes,
        CancellationToken cancelamento);
}

public interface IOperacoesReservaEstoque
{
    Task<bool> TemOrcamentoAprovadoAsync(Guid ordemId, CancellationToken cancelamento);
    Task<AvisoReservaRecusada[]> ObterAvisosPendentesAsync(Guid ordemId, CancellationToken cancelamento);
    Task<Dictionary<Guid, ItemEstoque>> BloquearItensAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancelamento);
    void Adicionar(AvisoReservaRecusada aviso);
    void Adicionar(ReservaItemOrdemServico reserva);
    void Adicionar(RegistroHistoricoOrdemServico marco);
}
