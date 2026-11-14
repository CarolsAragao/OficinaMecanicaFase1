using Microsoft.Extensions.Configuration;

namespace OficinaMecanicaFase1.Infrastructure.Notifications;

public sealed class NotificacoesStatusConfiguracao
{
    public bool ProcessadorAtivo { get; set; }
    public int IntervaloSegundos { get; set; } = 5;
    public int QuantidadeMaxima { get; set; } = 20;
    public int AtrasoTentativaSegundos { get; set; } = 30;
    public int ProcessamentoExpiraEmSegundos { get; set; } = 60;

    public static NotificacoesStatusConfiguracao Ler(IConfiguration configuration)
    {
        var opcoes = configuration.GetSection("NotificacoesStatus").Get<NotificacoesStatusConfiguracao>()
            ?? new NotificacoesStatusConfiguracao();
        if (opcoes.IntervaloSegundos <= 0 || opcoes.QuantidadeMaxima <= 0 ||
            opcoes.AtrasoTentativaSegundos <= 0 || opcoes.ProcessamentoExpiraEmSegundos <= 0)
            throw new InvalidOperationException("Os intervalos de NotificacoesStatus devem ser maiores que zero.");
        return opcoes;
    }
}
