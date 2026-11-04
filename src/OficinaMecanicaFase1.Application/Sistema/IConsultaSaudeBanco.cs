namespace OficinaMecanicaFase1.Application.Sistema;

public interface IConsultaSaudeBanco
{
    Task<bool> PodeConectarAsync(CancellationToken cancelamento);
}
