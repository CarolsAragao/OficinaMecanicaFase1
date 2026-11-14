using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using DotNet.Testcontainers.Builders;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OficinaMecanicaFase1.Api.Contracts;
using OficinaMecanicaFase1.Application.OrdensServico;
using OficinaMecanicaFase1.Domain.OrdensServico;
using OficinaMecanicaFase1.Infrastructure.DependencyInjection;
using Testcontainers.MsSql;
using Xunit;

namespace OficinaMecanicaFase1.Infrastructure.Tests;

public sealed class NotificadorStatusSmtpTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task AberturaDaOsSalvaStatusEEnviaEmailAoMailpit()
    {
        await using var sqlServer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-CU14-ubuntu-22.04").Build();
        await using var mailpit = new ContainerBuilder("axllent/mailpit:v1.32.0")
            .WithPortBinding(1025, true)
            .WithPortBinding(8025, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilExternalTcpPortIsAvailable(1025))
            .Build();
        await Task.WhenAll(sqlServer.StartAsync(), mailpit.StartAsync());

        var conexao = new SqlConnectionStringBuilder(sqlServer.GetConnectionString())
        {
            InitialCatalog = $"OficinaEmails_{Guid.NewGuid():N}"
        };
        var configuracoes = new Dictionary<string, string?>
        {
            ["ConnectionStrings:Oficina"] = conexao.ConnectionString,
            ["Jwt:Issuer"] = "OficinaMecanicaFase1",
            ["Jwt:Audience"] = "OficinaMecanicaFase1.Interno",
            ["Jwt:SigningKey"] = Convert.ToBase64String(Encoding.UTF8.GetBytes("UmaChaveFixaDeTesteComMaisDeTrintaEDoisBytes!")),
            ["Bootstrap:AdminEmail"] = "admin@emails.test",
            ["Bootstrap:AdminPassword"] = "AdminDeTeste123!",
            ["Smtp:Enabled"] = "true",
            ["Smtp:Host"] = mailpit.Hostname,
            ["Smtp:Port"] = mailpit.GetMappedPublicPort(1025).ToString(),
            ["Smtp:Security"] = "None",
            ["Smtp:FromEmail"] = "notificacoes@oficina.local"
        };
        using var fabrica = new OficinaApiFactory(configuracoes);
        using var cliente = fabrica.CreateClient();
        var login = await cliente.PostAsJsonAsync("/api/interno/autenticacao",
            new { Email = "admin@emails.test", Senha = "AdminDeTeste123!" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var acesso = (await login.Content.ReadFromJsonAsync<AutenticacaoInternaResponse>())!;
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", acesso.Token);

        var cadastro = await cliente.PostAsJsonAsync("/api/clientes",
            new ClienteRequest("Ana", "529.982.247-25", "ana@exemplo.com"));
        Assert.Equal(HttpStatusCode.Created, cadastro.StatusCode);
        var pessoa = (await cadastro.Content.ReadFromJsonAsync<ClienteResponse>())!;
        var veiculoCriado = await cliente.PostAsJsonAsync("/api/veiculos",
            new CriarVeiculoRequest(pessoa.Id, "ABC1234", "Fiat", "Uno", 2020));
        Assert.Equal(HttpStatusCode.Created, veiculoCriado.StatusCode);
        var veiculo = (await veiculoCriado.Content.ReadFromJsonAsync<VeiculoResponse>())!;
        var servicoCriado = await cliente.PostAsJsonAsync("/api/servicos",
            new ServicoCatalogoRequest("Revisão", null, 120m));
        Assert.Equal(HttpStatusCode.Created, servicoCriado.StatusCode);
        var servico = (await servicoCriado.Content.ReadFromJsonAsync<ServicoCatalogoResponse>())!;

        var abertura = await cliente.PostAsJsonAsync("/api/ordens-servico",
            new CriarOrdemServicoRequest(pessoa.Id, veiculo.Id, [servico.Id], []));
        Assert.Equal(HttpStatusCode.Created, abertura.StatusCode);
        var ordem = (await abertura.Content.ReadFromJsonAsync<OrdemServicoResponse>())!;
        Assert.Equal("Recebida", ordem.Status);
        Assert.Equal("Recebida", (await cliente.GetFromJsonAsync<OrdemServicoResponse>(
            $"/api/ordens-servico/{ordem.Numero}"))!.Status);

        using var http = new HttpClient
        {
            BaseAddress = new UriBuilder("http", mailpit.Hostname, mailpit.GetMappedPublicPort(8025)).Uri
        };
        var recebido = await http.GetFromJsonAsync<JsonElement>("/api/v1/message/latest");
        Assert.Contains($"OS nº {ordem.Numero}: Recebida", recebido.GetProperty("Subject").GetString());
        Assert.Contains($"OS nº {ordem.Numero}", recebido.GetProperty("Text").GetString());
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task EnviaMensagemDeStatusAoMailpit()
    {
        await using var mailpit = new ContainerBuilder("axllent/mailpit:v1.32.0")
            .WithPortBinding(1025, true)
            .WithPortBinding(8025, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilExternalTcpPortIsAvailable(1025))
            .Build();
        await mailpit.StartAsync();

        var configuracao = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Smtp:Enabled"] = "true",
            ["Smtp:Host"] = mailpit.Hostname,
            ["Smtp:Port"] = mailpit.GetMappedPublicPort(1025).ToString(),
            ["Smtp:Security"] = "None",
            ["Smtp:FromEmail"] = "notificacoes@oficina.local"
        }).Build();
        using var servicos = new ServiceCollection().AddLogging()
            .AddSmtpNotifications(configuracao).BuildServiceProvider();
        using var escopo = servicos.CreateScope();
        var notificador = escopo.ServiceProvider.GetRequiredService<INotificadorStatusOrdemServico>();
        var mensagem = MensagemStatusOrdemServico.Criar(
            Guid.NewGuid(), "cliente@exemplo.com", 123, StatusOrdemServico.Recebida);

        await notificador.EnviarAsync(mensagem, CancellationToken.None);

        using var http = new HttpClient
        {
            BaseAddress = new UriBuilder("http", mailpit.Hostname, mailpit.GetMappedPublicPort(8025)).Uri
        };
        var recebido = await http.GetFromJsonAsync<JsonElement>("/api/v1/message/latest");
        Assert.Equal(mensagem.Assunto, recebido.GetProperty("Subject").GetString());
        Assert.Contains("Novo status: Recebida.", recebido.GetProperty("Text").GetString());
    }

    [Fact]
    public void RecusaCredenciaisSemTls()
    {
        var configuracao = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Smtp:Enabled"] = "true",
            ["Smtp:Host"] = "smtp.exemplo.com",
            ["Smtp:Port"] = "587",
            ["Smtp:Security"] = "None",
            ["Smtp:FromEmail"] = "notificacoes@exemplo.com",
            ["Smtp:Username"] = "usuario",
            ["Smtp:Password"] = "senha-de-teste"
        }).Build();

        Assert.Throws<InvalidOperationException>(() =>
            new ServiceCollection().AddSmtpNotifications(configuracao));
    }

    private sealed class OficinaApiFactory(IReadOnlyDictionary<string, string?> configuracoes)
        : WebApplicationFactory<Program>
    {
        protected override IHost CreateHost(IHostBuilder builder)
        {
            builder.ConfigureHostConfiguration(configuration => configuration.AddInMemoryCollection(configuracoes));
            return base.CreateHost(builder);
        }
    }
}
