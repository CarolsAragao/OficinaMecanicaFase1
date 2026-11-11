using System.Text.Json;
using OficinaMecanicaFase1.Api.Contracts;
using OficinaMecanicaFase1.Domain.OrdensServico;
using Xunit;

namespace OficinaMecanicaFase1.Infrastructure.Tests;

public sealed class ContratoHistoricoTests
{
    [Fact]
    public void RespostasDeOrdemServicoUsamHistoricoNoJson()
    {
        var ordem = OrdemServico.Criar(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [],
            new DateTimeOffset(2026, 11, 10, 12, 0, 0, TimeSpan.Zero));
        var opcoes = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        var andamento = JsonSerializer.SerializeToElement(AndamentoOrdemServicoResponse.De(ordem), opcoes);
        Assert.Equal(["historico", "numero", "status", "ultimaAtualizacaoEmUtc"],
            andamento.EnumerateObject().Select(propriedade => propriedade.Name).OrderBy(nome => nome));
        Assert.Equal("OS recebida", andamento.GetProperty("historico")[0].GetProperty("descricao").GetString());

        var detalhe = JsonSerializer.SerializeToElement(OrdemServicoResponse.De(ordem), opcoes);
        Assert.Equal("OS recebida", detalhe.GetProperty("historico")[0].GetProperty("descricao").GetString());
        Assert.False(detalhe.TryGetProperty("marcos", out _));
    }
}
