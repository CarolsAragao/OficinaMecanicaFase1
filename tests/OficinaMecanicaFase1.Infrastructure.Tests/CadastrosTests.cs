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
using OficinaMecanicaFase1.Application.Cadastros;
using OficinaMecanicaFase1.Domain.Cadastros;
using OficinaMecanicaFase1.Infrastructure.Persistence;
using Testcontainers.MsSql;
using Xunit;

namespace OficinaMecanicaFase1.Infrastructure.Tests;

public sealed class CadastrosTests
{
    [Theory]
    [InlineData("529.982.247-25", "52998224725")]
    [InlineData("04.252.011/0001-10", "04252011000110")]
    public void DocumentoValidoENormalizado(string informado, string esperado) =>
        Assert.Equal(esperado, DocumentoCliente.Normalizar(informado));

    [Theory]
    [InlineData("529.982.247-26")]
    [InlineData("04.252.011/0001-11")]
    [InlineData("111.111.111-11")]
    [InlineData("5299822472A")]
    public void DocumentoInvalidoEhRecusado(string informado) =>
        Assert.Null(DocumentoCliente.Normalizar(informado));

    [Theory]
    [InlineData("abc-1234", "ABC1234")]
    [InlineData("abc1d23", "ABC1D23")]
    public void PlacaValidaENormalizada(string informada, string esperada) =>
        Assert.Equal(esperada, PlacaVeiculo.Normalizar(informada));

    [Theory]
    [InlineData("AB-1234")]
    [InlineData("ABC-12A4")]
    [InlineData("ABC1D2!")]
    public void PlacaInvalidaEhRecusada(string informada) =>
        Assert.Null(PlacaVeiculo.Normalizar(informada));

    [Fact]
    [Trait("Category", "Integration")]
    public async Task CadastrosRespeitamDocumentoPlacaProprietarioEAutorizacao()
    {
        await using var sqlServer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-CU14-ubuntu-22.04").Build();
        await sqlServer.StartAsync();

        var conexao = new SqlConnectionStringBuilder(sqlServer.GetConnectionString())
        {
            InitialCatalog = $"OficinaCadastros_{Guid.NewGuid():N}"
        };
        var configuracoes = new Dictionary<string, string?>
        {
            ["ConnectionStrings:Oficina"] = conexao.ConnectionString,
            ["Jwt:Issuer"] = "OficinaMecanicaFase1",
            ["Jwt:Audience"] = "OficinaMecanicaFase1.Interno",
            ["Jwt:SigningKey"] = Convert.ToBase64String(Encoding.UTF8.GetBytes("UmaChaveFixaDeTesteComMaisDeTrintaEDoisBytes!")),
            ["Bootstrap:AdminEmail"] = "admin@cadastros.test",
            ["Bootstrap:AdminPassword"] = "AdminDeTeste123!"
        };

        using var fabrica = new OficinaApiFactory(configuracoes);
        using var cliente = fabrica.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await cliente.GetAsync("/api/clientes")).StatusCode);

        await AutenticarAsync(cliente, "admin@cadastros.test", "AdminDeTeste123!");
        using (var openApi = JsonDocument.Parse(await cliente.GetStringAsync("/openapi/v1.json")))
        {
            Assert.True(openApi.RootElement.GetProperty("paths").GetProperty("/api/clientes")
                .GetProperty("post").TryGetProperty("security", out _));
            Assert.True(openApi.RootElement.GetProperty("paths").GetProperty("/api/veiculos")
                .GetProperty("post").TryGetProperty("security", out _));
        }
        var invalido = await cliente.PostAsJsonAsync("/api/clientes", new ClienteRequest("Ana", "111.111.111-11"));
        Assert.Equal(HttpStatusCode.BadRequest, invalido.StatusCode);

        var criacao = await cliente.PostAsJsonAsync("/api/clientes", new ClienteRequest("Ana", "529.982.247-25"));
        Assert.Equal(HttpStatusCode.Created, criacao.StatusCode);
        var ana = await criacao.Content.ReadFromJsonAsync<ClienteResponse>();
        Assert.NotNull(ana);
        Assert.Equal("52998224725", ana.Documento);

        var consulta = await cliente.GetFromJsonAsync<ClienteResponse[]>("/api/clientes?documento=529.982.247-25");
        Assert.Single(consulta!);
        var duplicado = await cliente.PostAsJsonAsync("/api/clientes", new ClienteRequest("Outra Ana", "52998224725"));
        Assert.Equal(HttpStatusCode.Conflict, duplicado.StatusCode);

        var naoEncontrado = await cliente.GetFromJsonAsync<ClienteResponse[]>(
            "/api/clientes?documento=04.252.011%2F0001-10");
        Assert.Empty(naoEncontrado!);

        var segundoCliente = await cliente.PostAsJsonAsync("/api/clientes", new ClienteRequest("Oficina", "04.252.011/0001-10"));
        Assert.Equal(HttpStatusCode.Created, segundoCliente.StatusCode);
        var oficina = await segundoCliente.Content.ReadFromJsonAsync<ClienteResponse>();
        Assert.NotNull(oficina);

        var semProprietario = await cliente.PostAsJsonAsync("/api/veiculos", new CriarVeiculoRequest(
            Guid.NewGuid(), "ABC1234", "Fiat", "Uno", 2020));
        Assert.Equal(HttpStatusCode.NotFound, semProprietario.StatusCode);
        var placaInvalida = await cliente.PostAsJsonAsync("/api/veiculos", new CriarVeiculoRequest(
            ana.Id, "ABC12A4", "Fiat", "Uno", 2020));
        Assert.Equal(HttpStatusCode.BadRequest, placaInvalida.StatusCode);

        var veiculoCriado = await cliente.PostAsJsonAsync("/api/veiculos", new CriarVeiculoRequest(
            ana.Id, "abc-1234", "Fiat", "Uno", 2020));
        Assert.Equal(HttpStatusCode.Created, veiculoCriado.StatusCode);
        var veiculo = await veiculoCriado.Content.ReadFromJsonAsync<VeiculoResponse>();
        Assert.NotNull(veiculo);
        Assert.Equal("ABC1234", veiculo.Placa);
        Assert.Equal(ana.Id, veiculo.ClienteId);

        var placaDuplicada = await cliente.PostAsJsonAsync("/api/veiculos", new CriarVeiculoRequest(
            oficina.Id, "ABC1234", "VW", "Gol", 2019));
        Assert.Equal(HttpStatusCode.Conflict, placaDuplicada.StatusCode);

        using (var escopo = fabrica.Services.CreateScope())
        {
            var db = escopo.ServiceProvider.GetRequiredService<OficinaDbContext>();
            Assert.Equal(conexao.InitialCatalog, db.Database.GetDbConnection().Database);
            Assert.False(db.Database.HasPendingModelChanges());
            var gerenciador = escopo.ServiceProvider.GetRequiredService<GerenciadorCadastros>();
            Assert.True(await gerenciador.VeiculoPertenceAoClienteAsync(veiculo.Id, ana.Id, default));
            Assert.False(await gerenciador.VeiculoPertenceAoClienteAsync(veiculo.Id, oficina.Id, default));
        }

        var exclusaoCliente = await cliente.DeleteAsync($"/api/clientes/{ana.Id}");
        Assert.Equal(HttpStatusCode.Conflict, exclusaoCliente.StatusCode);
        var atualizarCliente = await cliente.PutAsJsonAsync($"/api/clientes/{ana.Id}",
            new ClienteRequest("Ana Maria", "529.982.247-25"));
        Assert.Equal(HttpStatusCode.OK, atualizarCliente.StatusCode);
        Assert.Equal("Ana Maria", (await atualizarCliente.Content.ReadFromJsonAsync<ClienteResponse>())!.Nome);

        var novoAtendente = await cliente.PostAsJsonAsync("/api/interno/usuarios", new
        {
            Email = "atendente@cadastros.test", Senha = "AtendenteDeTeste123!", Papel = "Atendente"
        });
        Assert.Equal(HttpStatusCode.Created, novoAtendente.StatusCode);
        await AutenticarAsync(cliente, "atendente@cadastros.test", "AtendenteDeTeste123!");
        Assert.Equal(HttpStatusCode.Forbidden, (await cliente.DeleteAsync($"/api/veiculos/{veiculo.Id}")).StatusCode);
        var atualizado = await cliente.PutAsJsonAsync($"/api/veiculos/{veiculo.Id}",
            new AtualizarVeiculoRequest("ABC1D23", "Fiat", "Uno Mille", 2020));
        Assert.Equal(HttpStatusCode.OK, atualizado.StatusCode);
        var veiculoAtualizado = await atualizado.Content.ReadFromJsonAsync<VeiculoResponse>();
        Assert.Equal(ana.Id, veiculoAtualizado!.ClienteId);
        Assert.Equal("ABC1D23", veiculoAtualizado.Placa);

        await AutenticarAsync(cliente, "admin@cadastros.test", "AdminDeTeste123!");
        Assert.Equal(HttpStatusCode.NoContent, (await cliente.DeleteAsync($"/api/veiculos/{veiculo.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await cliente.GetAsync($"/api/veiculos/{veiculo.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await cliente.DeleteAsync($"/api/clientes/{ana.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await cliente.GetAsync($"/api/clientes/{ana.Id}")).StatusCode);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task IndiceUnicoDePlacaProtegeConcorrenciaDiretamenteNoBanco()
    {
        await using var sqlServer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-CU14-ubuntu-22.04").Build();
        await sqlServer.StartAsync();
        var conexao = new SqlConnectionStringBuilder(sqlServer.GetConnectionString())
        {
            InitialCatalog = $"OficinaPlaca_{Guid.NewGuid():N}"
        };
        var opcoes = new DbContextOptionsBuilder<OficinaDbContext>().UseSqlServer(conexao.ConnectionString).Options;
        await using var primeiro = new OficinaDbContext(opcoes);
        await primeiro.Database.MigrateAsync();
        var proprietario = new Cliente("Ana", "52998224725");
        primeiro.Clientes.Add(proprietario);
        primeiro.Veiculos.Add(new Veiculo(proprietario.Id, "ABC1234", "Fiat", "Uno", 2020));
        await primeiro.SaveChangesAsync();

        await using var segundo = new OficinaDbContext(opcoes);
        segundo.Veiculos.Add(new Veiculo(proprietario.Id, "ABC1234", "VW", "Gol", 2021));
        await Assert.ThrowsAsync<DbUpdateException>(() => segundo.SaveChangesAsync());
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
