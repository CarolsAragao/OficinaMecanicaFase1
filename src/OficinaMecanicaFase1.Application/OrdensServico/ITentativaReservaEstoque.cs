namespace OficinaMecanicaFase1.Application.OrdensServico;

public interface ITentativaReservaEstoque
{
    Task<ResultadoTentativaReserva> TentarReservarAsync(long numero, CancellationToken cancelamento);
}
