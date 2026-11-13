namespace OficinaMecanicaFase1.Application.OrdensServico;

public interface INotificadorStatusOrdemServico
{
    Task EnviarAsync(MensagemStatusOrdemServico mensagem, CancellationToken cancelamento);
}
