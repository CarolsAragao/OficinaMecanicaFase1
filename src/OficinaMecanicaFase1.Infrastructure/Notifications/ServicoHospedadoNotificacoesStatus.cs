using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace OficinaMecanicaFase1.Infrastructure.Notifications;

internal sealed class ServicoHospedadoNotificacoesStatus(
    IServiceScopeFactory escopos,
    NotificacoesStatusConfiguracao configuracao,
    ILogger<ServicoHospedadoNotificacoesStatus> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken parando)
    {
        while (!parando.IsCancellationRequested)
        {
            try
            {
                using var escopo = escopos.CreateScope();
                await escopo.ServiceProvider.GetRequiredService<ProcessadorNotificacoesStatus>()
                    .ProcessarAsync(parando);
            }
            catch (OperationCanceledException) when (parando.IsCancellationRequested)
            {
                break;
            }
            catch (Exception erro)
            {
                logger.LogError(erro, "Falha no processamento de avisos pendentes de status.");
            }

            await Task.Delay(TimeSpan.FromSeconds(configuracao.IntervaloSegundos), parando);
        }
    }
}
