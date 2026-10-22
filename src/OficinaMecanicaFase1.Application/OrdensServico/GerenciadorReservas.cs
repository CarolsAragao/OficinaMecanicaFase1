namespace OficinaMecanicaFase1.Application.OrdensServico;

public sealed class GerenciadorReservas(IRepositorioReservas repositorio, TimeProvider tempo)
{
    public Task<ResultadoTentativaReserva> TentarReservarAsync(long numero, CancellationToken cancelamento) =>
        repositorio.TentarReservarAsync(numero, tempo.GetUtcNow(), cancelamento);

    public Task<IReadOnlyCollection<AvisoReservaEstoque>> ListarAvisosAsync(
        bool somentePendentes, CancellationToken cancelamento) =>
        repositorio.ListarAvisosAsync(somentePendentes, cancelamento);
}
