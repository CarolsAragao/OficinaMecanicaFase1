using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using OficinaMecanicaFase1.Api.Controllers;
using OficinaMecanicaFase1.Api.Security;
using Xunit;

namespace OficinaMecanicaFase1.Infrastructure.Tests;

public sealed class ContratoHttpTests
{
    [Fact]
    public async Task OpenApiMantemAs56OperacoesEAsRotasPublicasEProtegidas()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Development"
        });
        builder.WebHost.UseTestServer();
        builder.Services.AddControllers(options =>
                options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true)
            .AddApplicationPart(typeof(ClientesController).Assembly);
        builder.Services.AddAuthorization();
        builder.Services.AddOpenApi(options => options.AddDocumentTransformer(
            (documento, _, _) => EsquemaDeSegurancaOpenApi.AdicionarAsync(documento)));

        await using var app = builder.Build();
        app.MapControllers();
        app.MapOpenApi();
        app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "Oficina Mecânica API v1"));
        await app.StartAsync();

        using var cliente = app.GetTestClient();
        using var saude = await cliente.GetAsync("/api/saude");
        Assert.True(saude.IsSuccessStatusCode);
        Assert.Equal("operacional", (await saude.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("status").GetString());
        using var swagger = await cliente.GetAsync("/swagger/index.html");
        Assert.True(swagger.IsSuccessStatusCode);
        using var documento = JsonDocument.Parse(await cliente.GetStringAsync("/openapi/v1.json"));
        var caminhos = documento.RootElement.GetProperty("paths");
        var operacoes = caminhos.EnumerateObject()
            .SelectMany(caminho => caminho.Value.EnumerateObject()
                .Where(operacao => operacao.Name is "get" or "post" or "put" or "patch" or "delete")
                .Select(operacao => $"{operacao.Name.ToUpperInvariant()} {caminho.Name}"))
            .ToHashSet(StringComparer.Ordinal);

        var esperadas = new[]
        {
            "DELETE /api/clientes/{id}",
            "DELETE /api/itens-estoque/{id}",
            "DELETE /api/servicos/{id}",
            "DELETE /api/veiculos/{id}",
            "GET /api/administracao/ordens-servico",
            "GET /api/administracao/ordens-servico/{numero}",
            "GET /api/administracao/ordens-servico/indicadores/tempo-medio",
            "GET /api/avisos-estoque/reservas",
            "GET /api/clientes",
            "GET /api/clientes/{id}",
            "GET /api/interno/identidade",
            "GET /api/itens-estoque",
            "GET /api/itens-estoque/{id}",
            "GET /api/itens-estoque/{id}/entradas",
            "GET /api/ordens-servico/{numero}",
            "GET /api/ordens-servico/{numero}/aditivos/{aditivoId}",
            "GET /api/ordens-servico/{numero}/orcamento",
            "GET /api/saude",
            "GET /api/saude/banco",
            "GET /api/servicos",
            "GET /api/servicos/{id}",
            "GET /api/veiculos",
            "GET /api/veiculos/{id}",
            "PATCH /api/itens-estoque/{id}/ativacao",
            "PATCH /api/servicos/{id}/ativacao",
            "POST /api/cliente/ordens-servico/{numero}/aditivos/{aditivoId}/consulta",
            "POST /api/cliente/ordens-servico/{numero}/aditivos/{aditivoId}/decisao",
            "POST /api/cliente/ordens-servico/{numero}/andamento/consulta",
            "POST /api/cliente/ordens-servico/{numero}/orcamento/consulta",
            "POST /api/cliente/ordens-servico/{numero}/orcamento/decisao",
            "POST /api/clientes",
            "POST /api/interno/autenticacao",
            "POST /api/interno/usuarios",
            "POST /api/itens-estoque",
            "POST /api/itens-estoque/{id}/entradas",
            "POST /api/ordens-servico",
            "POST /api/ordens-servico/{numero}/aditivos",
            "POST /api/ordens-servico/{numero}/aditivos/{aditivoId}/reservas/tentativa",
            "POST /api/ordens-servico/{numero}/codigo-acesso/emissao",
            "POST /api/ordens-servico/{numero}/consumos",
            "POST /api/ordens-servico/{numero}/diagnostico",
            "POST /api/ordens-servico/{numero}/diagnostico/inicio",
            "POST /api/ordens-servico/{numero}/entrega",
            "POST /api/ordens-servico/{numero}/execucao/inicio",
            "POST /api/ordens-servico/{numero}/itens",
            "POST /api/ordens-servico/{numero}/orcamento",
            "POST /api/ordens-servico/{numero}/reservas/tentativa",
            "POST /api/ordens-servico/{numero}/servicos",
            "POST /api/ordens-servico/{numero}/servicos/{servicoId}/conclusao",
            "POST /api/ordens-servico/{numero}/servicos/{servicoId}/inicio",
            "POST /api/servicos",
            "POST /api/veiculos",
            "PUT /api/clientes/{id}",
            "PUT /api/itens-estoque/{id}",
            "PUT /api/servicos/{id}",
            "PUT /api/veiculos/{id}"
        };
        Assert.Equal(56, operacoes.Count);
        Assert.Equal(esperadas.OrderBy(rota => rota, StringComparer.Ordinal),
            operacoes.OrderBy(rota => rota, StringComparer.Ordinal));

        var listarClientes = caminhos.GetProperty("/api/clientes").GetProperty("get");
        Assert.Equal("ListarClientes", listarClientes.GetProperty("operationId").GetString());
        Assert.Equal("Clientes", listarClientes.GetProperty("tags")[0].GetString());
        Assert.True(listarClientes.TryGetProperty("security", out _));
        Assert.True(caminhos.GetProperty("/api/administracao/ordens-servico")
            .GetProperty("get").TryGetProperty("security", out _));
        Assert.False(caminhos.GetProperty("/api/saude").GetProperty("get").TryGetProperty("security", out _));
        Assert.False(caminhos.GetProperty("/api/interno/autenticacao").GetProperty("post").TryGetProperty("security", out _));
        Assert.False(caminhos.GetProperty("/api/cliente/ordens-servico/{numero}/orcamento/consulta")
            .GetProperty("post").TryGetProperty("security", out _));

        var endpoints = app.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>().ToArray();
        RouteEndpoint Rota(string caminho, string verbo) => endpoints.Single(endpoint =>
            endpoint.RoutePattern.RawText?.TrimStart('/') == caminho &&
            endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods.Contains(verbo) == true);
        string[] Politicas(RouteEndpoint endpoint) => endpoint.Metadata
            .GetOrderedMetadata<IAuthorizeData>().Select(autorizacao => autorizacao.Policy)
            .OfType<string>().ToArray();

        Assert.Contains("Cadastros", Politicas(Rota("api/clientes", "GET")));
        Assert.Contains("Administradores", Politicas(Rota("api/clientes/{id:guid}", "DELETE")));
        Assert.Contains("Mecanicos", Politicas(Rota("api/ordens-servico/{numero:long}/diagnostico", "POST")));
        Assert.Contains("Estoquistas", Politicas(Rota("api/avisos-estoque/reservas", "GET")));
        Assert.NotNull(Rota("api/interno/autenticacao", "POST").Metadata.GetMetadata<IAllowAnonymous>());
        Assert.NotNull(Rota("api/cliente/ordens-servico/{numero:long}/orcamento/consulta", "POST")
            .Metadata.GetMetadata<IAllowAnonymous>());
    }
}
