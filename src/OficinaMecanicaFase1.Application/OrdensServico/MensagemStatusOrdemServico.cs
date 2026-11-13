using OficinaMecanicaFase1.Domain.OrdensServico;

namespace OficinaMecanicaFase1.Application.OrdensServico;

public sealed record MensagemStatusOrdemServico(
    Guid MarcoHistoricoId, string EmailCliente, long NumeroOrdemServico,
    StatusOrdemServico Status, string Assunto, string Corpo)
{
    public static MensagemStatusOrdemServico Criar(
        Guid marcoHistoricoId, string emailCliente, long numeroOrdemServico, StatusOrdemServico status)
    {
        if (marcoHistoricoId == Guid.Empty)
            throw new ArgumentException("Informe o marco da mudança de status.", nameof(marcoHistoricoId));
        if (string.IsNullOrWhiteSpace(emailCliente))
            throw new ArgumentException("Informe o e-mail do cliente.", nameof(emailCliente));
        if (numeroOrdemServico <= 0)
            throw new ArgumentOutOfRangeException(nameof(numeroOrdemServico));

        var (nome, explicacao) = status switch
        {
            StatusOrdemServico.Recebida => ("Recebida", "Sua ordem de serviço foi registrada. A equipe iniciará a avaliação."),
            StatusOrdemServico.EmDiagnostico => ("Em diagnóstico", "A equipe iniciou a avaliação do veículo."),
            StatusOrdemServico.AguardandoAprovacao => ("Aguardando aprovação",
                "O orçamento está disponível. Para aprovar ou recusar, use o formulário de decisão e o código fornecido pelo atendente. Se ainda não recebeu as instruções de acesso, entre em contato com a oficina."),
            StatusOrdemServico.EmExecucao => ("Em execução", "Os serviços autorizados começaram."),
            StatusOrdemServico.Finalizada => ("Finalizada", "Os serviços foram concluídos. Aguarde as orientações da oficina para a retirada."),
            StatusOrdemServico.Entregue => ("Entregue", "A entrega do veículo foi registrada."),
            _ => throw new ArgumentOutOfRangeException(nameof(status))
        };

        return new MensagemStatusOrdemServico(
            marcoHistoricoId, emailCliente.Trim(), numeroOrdemServico, status,
            $"Atualização da OS nº {numeroOrdemServico}: {nome}",
            $"Olá,\n\nA OS nº {numeroOrdemServico} mudou de status.\nNovo status: {nome}.\n{explicacao}\n\nOficina Mecânica");
    }
}
