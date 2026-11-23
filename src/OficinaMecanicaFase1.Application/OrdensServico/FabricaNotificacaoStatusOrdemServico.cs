using OficinaMecanicaFase1.Domain.OrdensServico;

namespace OficinaMecanicaFase1.Application.OrdensServico;

public static class FabricaNotificacaoStatusOrdemServico
{
    public static NotificacaoStatusOrdemServico CriarPendente(
        OrdemServico ordem, RegistroHistoricoOrdemServico marco, string emailCliente, DateTimeOffset agora)
    {
        var mensagem = MensagemStatusOrdemServico.Criar(
            marco.Id, emailCliente, ordem.Numero, ordem.Status);

        return NotificacaoStatusOrdemServico.CriarPendente(
            marco.Id, ordem.Id, ordem.Numero, ordem.Status, mensagem.EmailCliente,
            mensagem.Assunto, mensagem.Corpo, agora);
    }
}
