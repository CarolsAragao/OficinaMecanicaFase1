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
using OficinaMecanicaFase1.Application.Catalogo;
using OficinaMecanicaFase1.Domain.Catalogo;
using OficinaMecanicaFase1.Domain.Estoque;
using OficinaMecanicaFase1.Infrastructure.Persistence;
using Testcontainers.MsSql;
using Xunit;

namespace OficinaMecanicaFase1.Infrastructure.Tests;

public sealed class CatalogoEstoqueTests
{
    [Fact]
    public void PecaExigeUnidadeEQuantidadeInteira()
    {
        Assert.Throws<ArgumentException>(() => new ItemEstoque("Filtro", null, TipoItemEstoque.Peca, "L", 20m));
        var item = new ItemEstoque("Filtro", null, TipoItemEstoque.Peca, "un", 20m);
        Assert.Throws<ArgumentException>(() => item.RegistrarEntrada(1.5m, DateTimeOffset.UtcNow, Guid.NewGuid()));
        Assert.Throws<ArgumentException>(() => item.RegistrarEntrada(0m, DateTimeOffset.UtcNow, Guid.NewGuid()));
        Assert.Equal(0m, item.SaldoFisico);
        var entrada = item.RegistrarEntrada(2m, DateTimeOffset.UtcNow, Guid.NewGuid());
        Assert.Equal(2m, entrada.Quantidade);
        Assert.Equal(2m, item.SaldoFisico);
        Assert.Equal(2m, item.SaldoDisponivel);
    }

    [Fact]
    public void InsumoAceitaFracaoERecusaQuantidadeSemPrecisaoSuportada()
    {
        var item = new ItemEstoque("Óleo", null, TipoItemEstoque.Insumo, "L", 15.50m);
        item.RegistrarEntrada(1.250m, DateTimeOffset.UtcNow, Guid.NewGuid());
        Assert.Equal(1.250m, item.SaldoFisico);
        Assert.Throws<ArgumentException>(() => item.RegistrarEntrada(0.0001m, DateTimeOffset.UtcNow, Guid.NewGuid()));
        Assert.Throws<ArgumentException>(() => item.RegistrarEntrada(-1m, DateTimeOffset.UtcNow, Guid.NewGuid()));
    }

    [Fact]
    public void ServicoInvalidoNaoPodeSerCadastrado()
    {
        Assert.Throws<ArgumentException>(() => new ServicoCatalogo("", null, 10m));
        Assert.Throws<ArgumentException>(() => new ServicoCatalogo("Alinhamento", null, -1m));
        Assert.Throws<ArgumentException>(() => new ServicoCatalogo("Alinhamento", null, 10.001m));
    }

    [Fact]
    public async Task ApiPreservaHistoricoAplicaPapeisERegrasDeQuantidade()
    {
        await using var sqlServer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-CU14-ubuntu-22.04").Build();
        await sqlServer.StartAsync();
        var conexao = new SqlConnectionStringBuilder(sqlServer.GetConnectionString())
        {
            InitialCatalog = $"OficinaCatalogo_{Guid.NewGuid():N}"
        };
        var configuracoes = new Dictionary<string, string?>
        {
            ["ConnectionStrings:Oficina"] = conexao.ConnectionString,
            ["Jwt:Issuer"] = "OficinaMecanicaFase1",
            ["Jwt:Audience"] = "OficinaMecanicaFase1.Interno",
            ["Jwt:SigningKey"] = Convert.ToBase64String(Encoding.UTF8.GetBytes("UmaChaveFixaDeTesteComMaisDeTrintaEDoisBytes!")),
            ["Bootstrap:AdminEmail"] = "admin@catalogo.test",
            ["Bootstrap:AdminPassword"] = "AdminDeTeste123!"
        };
        using var fabrica = new OficinaApiFactory(configuracoes);
        using var cliente = fabrica.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await cliente.GetAsync("/api/servicos")).StatusCode);
        await AutenticarAsync(cliente, "admin@catalogo.test", "AdminDeTeste123!");

        using (var documento = JsonDocument.Parse(await cliente.GetStringAsync("/openapi/v1.json")))
        {
            Assert.True(documento.RootElement.GetProperty("paths").GetProperty("/api/servicos")
                .GetProperty("post").TryGetProperty("security", out _));
            Assert.True(documento.RootElement.GetProperty("paths").GetProperty("/api/itens-estoque/{id}/entradas")
                .GetProperty("post").TryGetProperty("security", out _));
        }

        Assert.Equal(HttpStatusCode.BadRequest, (await cliente.PostAsJsonAsync("/api/servicos",
            new ServicoCatalogoRequest("Alinhamento", null, -1m))).StatusCode);
        var servicoCriado = await cliente.PostAsJsonAsync("/api/servicos",
            new ServicoCatalogoRequest("Alinhamento", "Eixos dianteiros", 120m));
        Assert.Equal(HttpStatusCode.Created, servicoCriado.StatusCode);
        var servico = await servicoCriado.Content.ReadFromJsonAsync<ServicoCatalogoResponse>();
        Assert.NotNull(servico);

        Assert.Equal(HttpStatusCode.BadRequest, (await cliente.PostAsJsonAsync("/api/itens-estoque",
            new CriarItemEstoqueRequest("Filtro", null, "Peca", "L", 20m))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await cliente.PostAsJsonAsync("/api/itens-estoque",
            new CriarItemEstoqueRequest("Filtro", null, "Outro", "un", 20m))).StatusCode);
        var pecaCriada = await cliente.PostAsJsonAsync("/api/itens-estoque",
            new CriarItemEstoqueRequest("Filtro", null, "Peca", "un", 20m));
        Assert.Equal(HttpStatusCode.Created, pecaCriada.StatusCode);
        var peca = await pecaCriada.Content.ReadFromJsonAsync<ItemEstoqueResponse>();
        Assert.NotNull(peca);
        var insumoCriado = await cliente.PostAsJsonAsync("/api/itens-estoque",
            new CriarItemEstoqueRequest("Óleo", null, "Insumo", "L", 15.50m));
        Assert.Equal(HttpStatusCode.Created, insumoCriado.StatusCode);
        var insumo = await insumoCriado.Content.ReadFromJsonAsync<ItemEstoqueResponse>();
        Assert.NotNull(insumo);

        Assert.Equal(HttpStatusCode.Forbidden, (await cliente.PostAsJsonAsync(
            $"/api/itens-estoque/{peca.Id}/entradas", new RegistrarEntradaEstoqueRequest(2m))).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await cliente.PostAsJsonAsync("/api/interno/usuarios", new
        {
            Email = "estoquista@catalogo.test", Senha = "EstoquistaDeTeste123!", Papel = "Estoquista"
        })).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await cliente.PostAsJsonAsync("/api/interno/usuarios", new
        {
            Email = "atendente@catalogo.test", Senha = "AtendenteDeTeste123!", Papel = "Atendente"
        })).StatusCode);

        await AutenticarAsync(cliente, "atendente@catalogo.test", "AtendenteDeTeste123!");
        Assert.Equal(HttpStatusCode.Forbidden, (await cliente.PostAsJsonAsync("/api/servicos",
            new ServicoCatalogoRequest("Troca de óleo", null, 80m))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync("/api/servicos")).StatusCode);

        await AutenticarAsync(cliente, "estoquista@catalogo.test", "EstoquistaDeTeste123!");
        Assert.Equal(HttpStatusCode.BadRequest, (await cliente.PostAsJsonAsync(
            $"/api/itens-estoque/{peca.Id}/entradas", new RegistrarEntradaEstoqueRequest(1.5m))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await cliente.PostAsJsonAsync(
            $"/api/itens-estoque/{insumo.Id}/entradas", new RegistrarEntradaEstoqueRequest(0m))).StatusCode);
        var entradaPeca = await cliente.PostAsJsonAsync(
            $"/api/itens-estoque/{peca.Id}/entradas", new RegistrarEntradaEstoqueRequest(2m));
        Assert.Equal(HttpStatusCode.Created, entradaPeca.StatusCode);
        var entradaPecaDados = await entradaPeca.Content.ReadFromJsonAsync<EntradaEstoqueResponse>();
        Assert.NotNull(entradaPecaDados);
        Assert.NotEqual(Guid.Empty, entradaPecaDados.UsuarioId);
        var entradaInsumo = await cliente.PostAsJsonAsync(
            $"/api/itens-estoque/{insumo.Id}/entradas", new RegistrarEntradaEstoqueRequest(1.25m));
        Assert.Equal(HttpStatusCode.Created, entradaInsumo.StatusCode);
        var saldoPeca = await cliente.GetFromJsonAsync<ItemEstoqueResponse>($"/api/itens-estoque/{peca.Id}");
        var saldoInsumo = await cliente.GetFromJsonAsync<ItemEstoqueResponse>($"/api/itens-estoque/{insumo.Id}");
        Assert.Equal(2m, saldoPeca!.SaldoFisico);
        Assert.Equal(0m, saldoPeca.SaldoReservado);
        Assert.Equal(2m, saldoPeca.SaldoDisponivel);
        Assert.Equal(1.25m, saldoInsumo!.SaldoFisico);

        await AutenticarAsync(cliente, "admin@catalogo.test", "AdminDeTeste123!");
        var servicoAtualizado = await cliente.PutAsJsonAsync($"/api/servicos/{servico.Id}",
            new ServicoCatalogoRequest("Alinhamento completo", null, 150m));
        Assert.Equal(HttpStatusCode.OK, servicoAtualizado.StatusCode);
        var itemAtualizado = await cliente.PutAsJsonAsync($"/api/itens-estoque/{peca.Id}",
            new AtualizarItemEstoqueRequest("Filtro premium", null, 25m));
        Assert.Equal(HttpStatusCode.OK, itemAtualizado.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await cliente.DeleteAsync($"/api/servicos/{servico.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await cliente.DeleteAsync($"/api/itens-estoque/{peca.Id}")).StatusCode);

        var ativosServicos = await cliente.GetFromJsonAsync<ServicoCatalogoResponse[]>("/api/servicos?somenteAtivos=true");
        var ativosItens = await cliente.GetFromJsonAsync<ItemEstoqueResponse[]>("/api/itens-estoque?somenteAtivos=true");
        Assert.DoesNotContain(ativosServicos!, atual => atual.Id == servico.Id);
        Assert.DoesNotContain(ativosItens!, atual => atual.Id == peca.Id);
        Assert.NotNull(await cliente.GetFromJsonAsync<ServicoCatalogoResponse>($"/api/servicos/{servico.Id}"));
        Assert.NotNull(await cliente.GetFromJsonAsync<ItemEstoqueResponse>($"/api/itens-estoque/{peca.Id}"));
        var historico = await cliente.GetFromJsonAsync<EntradaEstoqueResponse[]>(
            $"/api/itens-estoque/{peca.Id}/entradas");
        Assert.Single(historico!);
        Assert.Equal(entradaPecaDados.Id, historico![0].Id);

        using (var escopo = fabrica.Services.CreateScope())
        {
            var db = escopo.ServiceProvider.GetRequiredService<OficinaDbContext>();
            Assert.Equal(conexao.InitialCatalog, db.Database.GetDbConnection().Database);
            Assert.False(db.Database.HasPendingModelChanges());
            var catalogo = escopo.ServiceProvider.GetRequiredService<GerenciadorCatalogo>();
            Assert.Null(await catalogo.ObterServicoAtivoAsync(servico.Id, default));
            Assert.Null(await catalogo.ObterItemAtivoAsync(peca.Id, default));
            await Assert.ThrowsAsync<SqlException>(() => db.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE ItensEstoque SET SaldoFisico = 2.5 WHERE Id = {peca.Id}"));
        }

        var reativado = await cliente.PatchAsJsonAsync($"/api/itens-estoque/{peca.Id}/ativacao",
            new DefinirAtivacaoRequest(true));
        Assert.Equal(HttpStatusCode.OK, reativado.StatusCode);
        Assert.True((await reativado.Content.ReadFromJsonAsync<ItemEstoqueResponse>())!.Ativo);
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
