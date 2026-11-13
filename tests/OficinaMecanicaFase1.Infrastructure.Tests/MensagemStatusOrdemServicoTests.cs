using OficinaMecanicaFase1.Application.OrdensServico;
using OficinaMecanicaFase1.Domain.OrdensServico;
using Xunit;

namespace OficinaMecanicaFase1.Infrastructure.Tests;

public sealed class MensagemStatusOrdemServicoTests
{
    [Theory]
    [InlineData(StatusOrdemServico.Recebida, "Recebida", "foi registrada")]
    [InlineData(StatusOrdemServico.EmDiagnostico, "Em diagnóstico", "iniciou a avaliação")]
    [InlineData(StatusOrdemServico.AguardandoAprovacao, "Aguardando aprovação", "orçamento está disponível")]
    [InlineData(StatusOrdemServico.EmExecucao, "Em execução", "serviços autorizados começaram")]
    [InlineData(StatusOrdemServico.Finalizada, "Finalizada", "serviços foram concluídos")]
    [InlineData(StatusOrdemServico.Entregue, "Entregue", "entrega do veículo foi registrada")]
    public void CadaStatusTemMensagemClara(StatusOrdemServico status, string nome, string explicacao)
    {
        var marcoId = Guid.NewGuid();

        var mensagem = MensagemStatusOrdemServico.Criar(marcoId, " cliente@exemplo.com ", 123, status);

        Assert.Equal(marcoId, mensagem.MarcoHistoricoId);
        Assert.Equal("cliente@exemplo.com", mensagem.EmailCliente);
        Assert.Equal(123, mensagem.NumeroOrdemServico);
        Assert.Equal(status, mensagem.Status);
        Assert.Contains($"OS nº 123: {nome}", mensagem.Assunto);
        Assert.Contains($"Novo status: {nome}.", mensagem.Corpo);
        Assert.Contains(explicacao, mensagem.Corpo);
    }

    [Fact]
    public void OrcamentoOrientaParaFormularioSemDivulgarCodigoOuDocumento()
    {
        var mensagem = MensagemStatusOrdemServico.Criar(
            Guid.NewGuid(), "cliente@exemplo.com", 123, StatusOrdemServico.AguardandoAprovacao);

        Assert.Contains("formulário de decisão", mensagem.Corpo);
        Assert.Contains("código fornecido pelo atendente", mensagem.Corpo);
        Assert.DoesNotContain("CPF", mensagem.Corpo);
        Assert.DoesNotContain("CNPJ", mensagem.Corpo);
        Assert.DoesNotContain("senha", mensagem.Corpo, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MensagemExigeMarcoNumeroEDestinatario()
    {
        Assert.Throws<ArgumentException>(() => MensagemStatusOrdemServico.Criar(
            Guid.Empty, "cliente@exemplo.com", 123, StatusOrdemServico.Recebida));
        Assert.Throws<ArgumentException>(() => MensagemStatusOrdemServico.Criar(
            Guid.NewGuid(), " ", 123, StatusOrdemServico.Recebida));
        Assert.Throws<ArgumentOutOfRangeException>(() => MensagemStatusOrdemServico.Criar(
            Guid.NewGuid(), "cliente@exemplo.com", 0, StatusOrdemServico.Recebida));
        Assert.Throws<ArgumentOutOfRangeException>(() => MensagemStatusOrdemServico.Criar(
            Guid.NewGuid(), "cliente@exemplo.com", 123, (StatusOrdemServico)999));
    }
}
