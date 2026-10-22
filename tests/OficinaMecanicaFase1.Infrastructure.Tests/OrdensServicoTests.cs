using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OficinaMecanicaFase1.Api.Contracts;
using OficinaMecanicaFase1.Domain.Cadastros;
using OficinaMecanicaFase1.Domain.Catalogo;
using OficinaMecanicaFase1.Domain.Estoque;
using OficinaMecanicaFase1.Domain.OrdensServico;
using OficinaMecanicaFase1.Infrastructure.Persistence;
using Testcontainers.MsSql;
using Xunit;

namespace OficinaMecanicaFase1.Infrastructure.Tests;

public sealed class OrdensServicoTests
{
    [Fact]
    public void CriacaoGeraStatusRecebidaEMarcoERecusaSolicitacaoSemServico()
    {
        var servicoId = Guid.NewGuid();
        var ordem = OrdemServico.Criar(Guid.NewGuid(), Guid.NewGuid(), [servicoId], [], DateTimeOffset.Now);
        Assert.Equal(StatusOrdemServico.Recebida, ordem.Status);
        Assert.Single(ordem.ServicosSolicitados);
        Assert.Single(ordem.Marcos);
        Assert.Equal("OS recebida", ordem.Marcos.Single().Descricao);
        Assert.Equal(TimeSpan.Zero, ordem.CriadaEmUtc.Offset);
        Assert.Throws<ArgumentException>(() => OrdemServico.Criar(Guid.NewGuid(), Guid.NewGuid(), [], [], DateTimeOffset.UtcNow));
    }

    [Fact]
    public async Task ApiAbreOsComReferenciasValidasEConsultaEstoqueSemReservar()
    {
        await using var sqlServer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-CU14-ubuntu-22.04").Build();
        await sqlServer.StartAsync();
        var conexao = new SqlConnectionStringBuilder(sqlServer.GetConnectionString())
        {
            InitialCatalog = $"OficinaOrdens_{Guid.NewGuid():N}"
        };
        var configuracoes = new Dictionary<string, string?>
        {
            ["ConnectionStrings:Oficina"] = conexao.ConnectionString,
            ["Jwt:Issuer"] = "OficinaMecanicaFase1",
            ["Jwt:Audience"] = "OficinaMecanicaFase1.Interno",
            ["Jwt:SigningKey"] = Convert.ToBase64String(Encoding.UTF8.GetBytes("UmaChaveFixaDeTesteComMaisDeTrintaEDoisBytes!")),
            ["Bootstrap:AdminEmail"] = "admin@ordens.test",
            ["Bootstrap:AdminPassword"] = "AdminDeTeste123!"
        };
        using var fabrica = new OficinaApiFactory(configuracoes);
        using var cliente = fabrica.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await cliente.PostAsJsonAsync("/api/ordens-servico", new { })).StatusCode);
        await AutenticarAsync(cliente, "admin@ordens.test", "AdminDeTeste123!");

        using (var documento = JsonDocument.Parse(await cliente.GetStringAsync("/openapi/v1.json")))
        {
            Assert.True(documento.RootElement.GetProperty("paths").GetProperty("/api/ordens-servico")
                .GetProperty("post").TryGetProperty("security", out _));
        }

        var clienteAna = await CriarClienteAsync(cliente, "Ana", "529.982.247-25");
        var clienteOutra = await CriarClienteAsync(cliente, "Bia", "04.252.011/0001-10");
        var veiculoAna = await CriarVeiculoAsync(cliente, clienteAna.Id, "ABC1234");
        var veiculoBia = await CriarVeiculoAsync(cliente, clienteOutra.Id, "XYZ5678");
        var servico = await CriarServicoAsync(cliente, "Revisão", 250m);
        var servicoAdicional = await CriarServicoAsync(cliente, "Alinhamento", 90m);
        var peca = await CriarItemAsync(cliente, "Filtro", "Peca", "un");
        var insumo = await CriarItemAsync(cliente, "Óleo", "Insumo", "L");
        var vela = await CriarItemAsync(cliente, "Vela", "Peca", "un");
        Assert.Equal(HttpStatusCode.Created, (await cliente.PostAsJsonAsync("/api/interno/usuarios", new
        {
            Email = "estoquista@ordens.test", Senha = "EstoquistaDeTeste123!", Papel = "Estoquista"
        })).StatusCode);
        await AutenticarAsync(cliente, "estoquista@ordens.test", "EstoquistaDeTeste123!");
        await ReporAsync(cliente, peca.Id, 2m);
        await ReporAsync(cliente, insumo.Id, 1.25m);
        await ReporAsync(cliente, vela.Id, 4m);
        await AutenticarAsync(cliente, "admin@ordens.test", "AdminDeTeste123!");
        Assert.Equal(HttpStatusCode.Created, (await cliente.PostAsJsonAsync("/api/interno/usuarios", new
        {
            Email = "atendente@ordens.test", Senha = "AtendenteDeTeste123!", Papel = "Atendente"
        })).StatusCode);
        await AutenticarAsync(cliente, "atendente@ordens.test", "AtendenteDeTeste123!");

        var semServico = await cliente.PostAsJsonAsync("/api/ordens-servico", new
        {
            clienteId = clienteAna.Id, veiculoId = veiculoAna.Id, servicosIds = Array.Empty<Guid>()
        });
        Assert.Equal(HttpStatusCode.BadRequest, semServico.StatusCode);
        var veiculoDeOutro = await cliente.PostAsJsonAsync("/api/ordens-servico", new
        {
            clienteId = clienteAna.Id, veiculoId = veiculoBia.Id, servicosIds = new[] { servico.Id }
        });
        Assert.Equal(HttpStatusCode.BadRequest, veiculoDeOutro.StatusCode);
        var quantidadeFracionariaDePeca = await cliente.PostAsJsonAsync("/api/ordens-servico", new
        {
            clienteId = clienteAna.Id, veiculoId = veiculoAna.Id, servicosIds = new[] { servico.Id },
            itensNecessarios = new[] { new { itemEstoqueId = peca.Id, quantidade = 1.5m } }
        });
        Assert.Equal(HttpStatusCode.BadRequest, quantidadeFracionariaDePeca.StatusCode);

        var criado = await cliente.PostAsJsonAsync("/api/ordens-servico", new
        {
            clienteId = clienteAna.Id, veiculoId = veiculoAna.Id, servicosIds = new[] { servico.Id },
            itensNecessarios = new[]
            {
                new { itemEstoqueId = peca.Id, quantidade = 1m },
                new { itemEstoqueId = insumo.Id, quantidade = 0.5m }
            }
        });
        Assert.Equal(HttpStatusCode.Created, criado.StatusCode);
        var ordem = await criado.Content.ReadFromJsonAsync<OrdemServicoResponse>();
        Assert.NotNull(ordem);
        Assert.True(ordem.Numero > 0);
        Assert.Equal(clienteAna.Id, ordem.ClienteId);
        Assert.Equal(veiculoAna.Id, ordem.VeiculoId);
        Assert.Equal("Recebida", ordem.Status);
        Assert.Single(ordem.Marcos);
        Assert.Equal("OS recebida", ordem.Marcos[0].Descricao);
        Assert.Equal(2m, Assert.Single(ordem.ItensNecessarios, item => item.ItemEstoqueId == peca.Id).SaldoDisponivelConsultado);
        Assert.Equal(1.25m, Assert.Single(ordem.ItensNecessarios, item => item.ItemEstoqueId == insumo.Id).SaldoDisponivelConsultado);

        var servicoNovo = await cliente.PostAsJsonAsync($"/api/ordens-servico/{ordem.Numero}/servicos",
            new AdicionarServicoSolicitadoRequest(servicoAdicional.Id));
        Assert.True(servicoNovo.StatusCode == HttpStatusCode.OK,
            $"{servicoNovo.StatusCode}: {await servicoNovo.Content.ReadAsStringAsync()}");
        var comServicoNovo = await servicoNovo.Content.ReadFromJsonAsync<OrdemServicoResponse>();
        Assert.Equal(2, comServicoNovo!.ServicosSolicitados.Length);
        Assert.Equal(HttpStatusCode.BadRequest, (await cliente.PostAsJsonAsync(
            $"/api/ordens-servico/{ordem.Numero}/servicos", new AdicionarServicoSolicitadoRequest(servico.Id))).StatusCode);

        var saldoPecaAntes = await cliente.GetFromJsonAsync<ItemEstoqueResponse>($"/api/itens-estoque/{peca.Id}");
        var adicionarInsumo = await cliente.PostAsJsonAsync($"/api/ordens-servico/{ordem.Numero}/itens",
            new AdicionarItemNecessarioRequest(insumo.Id, 0.25m));
        Assert.Equal(HttpStatusCode.BadRequest, adicionarInsumo.StatusCode);
        var adicionarOutraPeca = await cliente.PostAsJsonAsync($"/api/ordens-servico/{ordem.Numero}/itens",
            new AdicionarItemNecessarioRequest(Guid.NewGuid(), 1m));
        Assert.Equal(HttpStatusCode.BadRequest, adicionarOutraPeca.StatusCode);
        var adicionarVela = await cliente.PostAsJsonAsync($"/api/ordens-servico/{ordem.Numero}/itens",
            new AdicionarItemNecessarioRequest(vela.Id, 5m));
        Assert.True(adicionarVela.StatusCode == HttpStatusCode.OK,
            $"{adicionarVela.StatusCode}: {await adicionarVela.Content.ReadAsStringAsync()}");
        var comVela = await adicionarVela.Content.ReadFromJsonAsync<OrdemServicoResponse>();
        Assert.Equal(4m, Assert.Single(comVela!.ItensNecessarios, item => item.ItemEstoqueId == vela.Id)
            .SaldoDisponivelConsultado);
        var saldoPecaDepois = await cliente.GetFromJsonAsync<ItemEstoqueResponse>($"/api/itens-estoque/{peca.Id}");
        Assert.Equal(saldoPecaAntes!.SaldoFisico, saldoPecaDepois!.SaldoFisico);
        Assert.Equal(saldoPecaAntes.SaldoReservado, saldoPecaDepois.SaldoReservado);
        Assert.Equal(saldoPecaAntes.SaldoDisponivel, saldoPecaDepois.SaldoDisponivel);
        var saldoVela = await cliente.GetFromJsonAsync<ItemEstoqueResponse>($"/api/itens-estoque/{vela.Id}");
        Assert.Equal(4m, saldoVela!.SaldoFisico);
        Assert.Equal(0m, saldoVela.SaldoReservado);

        var consultada = await cliente.GetFromJsonAsync<OrdemServicoResponse>($"/api/ordens-servico/{ordem.Numero}");
        Assert.Equal("Recebida", consultada!.Status);
        Assert.Equal(2, consultada.ServicosSolicitados.Length);
        Assert.Equal(3, consultada.ItensNecessarios.Length);
        using (var escopo = fabrica.Services.CreateScope())
        {
            var db = escopo.ServiceProvider.GetRequiredService<OficinaDbContext>();
            Assert.False(db.Database.HasPendingModelChanges());
            Assert.Equal(1, await db.OrdensServico.CountAsync());
            Assert.Equal(0m, await db.ItensEstoque.Where(item => item.Id == peca.Id).Select(item => item.SaldoReservado).SingleAsync());
        }

        await AutenticarAsync(cliente, "admin@ordens.test", "AdminDeTeste123!");
        Assert.Equal(HttpStatusCode.NoContent, (await cliente.DeleteAsync($"/api/servicos/{servico.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await cliente.DeleteAsync($"/api/itens-estoque/{peca.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await cliente.PostAsJsonAsync("/api/ordens-servico", new
        {
            clienteId = clienteAna.Id, veiculoId = veiculoAna.Id, servicosIds = new[] { servico.Id }
        })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await cliente.PostAsJsonAsync("/api/ordens-servico", new
        {
            clienteId = clienteAna.Id, veiculoId = veiculoAna.Id, servicosIds = new[] { servicoAdicional.Id },
            itensNecessarios = new[] { new { itemEstoqueId = peca.Id, quantidade = 1m } }
        })).StatusCode);
        var historica = await cliente.GetFromJsonAsync<OrdemServicoResponse>($"/api/ordens-servico/{ordem.Numero}");
        Assert.Equal(3, historica!.ItensNecessarios.Length);
        Assert.Equal(2, historica.ServicosSolicitados.Length);
    }

    private static async Task<ClienteResponse> CriarClienteAsync(HttpClient cliente, string nome, string documento)
    {
        var resposta = await cliente.PostAsJsonAsync("/api/clientes", new ClienteRequest(nome, documento));
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        return (await resposta.Content.ReadFromJsonAsync<ClienteResponse>())!;
    }

    private static async Task<VeiculoResponse> CriarVeiculoAsync(HttpClient cliente, Guid clienteId, string placa)
    {
        var resposta = await cliente.PostAsJsonAsync("/api/veiculos",
            new CriarVeiculoRequest(clienteId, placa, "Fiat", "Uno", 2020));
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        return (await resposta.Content.ReadFromJsonAsync<VeiculoResponse>())!;
    }

    private static async Task<ServicoCatalogoResponse> CriarServicoAsync(HttpClient cliente, string nome, decimal preco)
    {
        var resposta = await cliente.PostAsJsonAsync("/api/servicos", new ServicoCatalogoRequest(nome, null, preco));
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        return (await resposta.Content.ReadFromJsonAsync<ServicoCatalogoResponse>())!;
    }

    private static async Task<ItemEstoqueResponse> CriarItemAsync(HttpClient cliente, string nome, string tipo, string unidade)
    {
        var resposta = await cliente.PostAsJsonAsync("/api/itens-estoque",
            new CriarItemEstoqueRequest(nome, null, tipo, unidade, 20m));
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        return (await resposta.Content.ReadFromJsonAsync<ItemEstoqueResponse>())!;
    }

    private static async Task ReporAsync(HttpClient cliente, Guid itemId, decimal quantidade)
    {
        Assert.Equal(HttpStatusCode.Created, (await cliente.PostAsJsonAsync(
            $"/api/itens-estoque/{itemId}/entradas", new RegistrarEntradaEstoqueRequest(quantidade))).StatusCode);
    }

    private static async Task AutenticarAsync(HttpClient cliente, string email, string senha)
    {
        var resposta = await cliente.PostAsJsonAsync("/api/interno/autenticacao", new { Email = email, Senha = senha });
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var acesso = await resposta.Content.ReadFromJsonAsync<AutenticacaoInternaResponse>();
        Assert.NotNull(acesso);
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", acesso.Token);
    }

    private sealed class OficinaApiFactory(IReadOnlyDictionary<string, string?> configuracoes) : WebApplicationFactory<Program>
    {
        protected override IHost CreateHost(IHostBuilder builder)
        {
            builder.ConfigureHostConfiguration(configuration => configuration.AddInMemoryCollection(configuracoes));
            return base.CreateHost(builder);
        }
    }
}
