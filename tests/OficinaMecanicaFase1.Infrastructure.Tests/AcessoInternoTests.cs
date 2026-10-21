using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using OficinaMecanicaFase1.Api.Contracts;
using OficinaMecanicaFase1.Infrastructure.Identity;
using Testcontainers.MsSql;
using Xunit;

namespace OficinaMecanicaFase1.Infrastructure.Tests;

public sealed class AcessoInternoTests
{
    [Fact]
    public async Task LoginJwtEPoliticasExigemTokenValidoEPapelAdequado()
    {
        await using var sqlServer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-CU14-ubuntu-22.04").Build();
        await sqlServer.StartAsync();

        var conexao = new SqlConnectionStringBuilder(sqlServer.GetConnectionString())
        {
            InitialCatalog = $"OficinaAutorizacao_{Guid.NewGuid():N}"
        };
        var chave = Encoding.UTF8.GetBytes("UmaChaveFixaDeTesteComMaisDeTrintaEDoisBytes!");
        const string senhaAdmin = "AdminDeTeste123!";

        var configuracoes = new Dictionary<string, string?>
        {
            ["ConnectionStrings:Oficina"] = conexao.ConnectionString,
            ["Jwt:Issuer"] = "OficinaMecanicaFase1",
            ["Jwt:Audience"] = "OficinaMecanicaFase1.Interno",
            ["Jwt:SigningKey"] = Convert.ToBase64String(chave),
            ["Bootstrap:AdminEmail"] = "admin@teste.local",
            ["Bootstrap:AdminPassword"] = senhaAdmin
        };
        using var fabrica = new OficinaApiFactory(configuracoes);

        using var cliente = fabrica.CreateClient();
        using (var escopo = fabrica.Services.CreateScope())
        {
            var identidade = escopo.ServiceProvider.GetRequiredService<IdentidadeDbContext>();
            Assert.Equal(conexao.InitialCatalog, identidade.Database.GetDbConnection().Database);
        }

        using var documento = JsonDocument.Parse(await cliente.GetStringAsync("/openapi/v1.json"));
        Assert.True(documento.RootElement.GetProperty("components").GetProperty("securitySchemes").TryGetProperty("Bearer", out _));
        Assert.True(documento.RootElement.GetProperty("paths").GetProperty("/api/interno/usuarios")
            .GetProperty("post").TryGetProperty("security", out _));
        Assert.False(documento.RootElement.GetProperty("paths").GetProperty("/api/interno/autenticacao")
            .GetProperty("post").TryGetProperty("security", out _));

        Assert.Equal(HttpStatusCode.Unauthorized, (await cliente.GetAsync("/api/interno/identidade")).StatusCode);
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "token-invalido");
        Assert.Equal(HttpStatusCode.Unauthorized, (await cliente.GetAsync("/api/interno/identidade")).StatusCode);
        cliente.DefaultRequestHeaders.Authorization = null;

        var respostaInvalida = await cliente.PostAsJsonAsync("/api/interno/autenticacao",
            new { Email = "admin@teste.local", Senha = "senha-incorreta" });
        Assert.Equal(HttpStatusCode.Unauthorized, respostaInvalida.StatusCode);

        var login = await cliente.PostAsJsonAsync("/api/interno/autenticacao",
            new { Email = "admin@teste.local", Senha = senhaAdmin });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var acesso = await login.Content.ReadFromJsonAsync<AutenticacaoInternaResponse>();
        Assert.NotNull(acesso);
        Assert.Equal("Bearer", acesso.Tipo);
        Assert.Equal(3, acesso.Token.Split('.').Length);

        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", acesso.Token);
        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync("/api/interno/identidade")).StatusCode);

        var cadastro = await cliente.PostAsJsonAsync("/api/interno/usuarios", new
        {
            Email = "atendente@teste.local",
            Senha = "AtendenteDeTeste123!",
            Papel = "Atendente"
        });
        Assert.True(cadastro.StatusCode == HttpStatusCode.Created,
            $"Esperado 201; recebido {(int)cadastro.StatusCode}: {await cadastro.Content.ReadAsStringAsync()}");

        var duplicado = await cliente.PostAsJsonAsync("/api/interno/usuarios", new
        {
            Email = "atendente@teste.local", Senha = "AtendenteDeTeste123!", Papel = "Atendente"
        });
        Assert.Equal(HttpStatusCode.Conflict, duplicado.StatusCode);
        Assert.Equal("application/problem+json", duplicado.Content.Headers.ContentType?.MediaType);

        var senhaFraca = await cliente.PostAsJsonAsync("/api/interno/usuarios", new
        {
            Email = "fraco@teste.local", Senha = "fraca", Papel = "Atendente"
        });
        Assert.Equal(HttpStatusCode.BadRequest, senhaFraca.StatusCode);

        var papelInvalido = await cliente.PostAsJsonAsync("/api/interno/usuarios", new
        {
            Email = "invalido@teste.local", Senha = "SenhaValidaDeTeste123!", Papel = "Cliente"
        });
        Assert.Equal(HttpStatusCode.BadRequest, papelInvalido.StatusCode);

        var loginAtendente = await cliente.PostAsJsonAsync("/api/interno/autenticacao",
            new { Email = "atendente@teste.local", Senha = "AtendenteDeTeste123!" });
        Assert.Equal(HttpStatusCode.OK, loginAtendente.StatusCode);
        var acessoAtendente = await loginAtendente.Content.ReadFromJsonAsync<AutenticacaoInternaResponse>();
        Assert.NotNull(acessoAtendente);

        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", acessoAtendente.Token);
        Assert.Equal(HttpStatusCode.Forbidden, (await cliente.PostAsJsonAsync("/api/interno/usuarios", new
        {
            Email = "outro@teste.local",
            Senha = "OutraSenhaDeTeste123!",
            Papel = "Estoquista"
        })).StatusCode);

        var expirado = new JwtSecurityToken(
            issuer: "OficinaMecanicaFase1",
            audience: "OficinaMecanicaFase1.Interno",
            claims: [new Claim("role", "Administrador")],
            notBefore: DateTime.UtcNow.AddMinutes(-2),
            expires: DateTime.UtcNow.AddMinutes(-1),
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(chave), SecurityAlgorithms.HmacSha256));
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            new JwtSecurityTokenHandler().WriteToken(expirado));
        Assert.Equal(HttpStatusCode.Unauthorized, (await cliente.GetAsync("/api/interno/identidade")).StatusCode);

        cliente.DefaultRequestHeaders.Authorization = null;
        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync("/api/saude")).StatusCode);

        for (var tentativa = 0; tentativa < 5; tentativa++)
        {
            var falha = await cliente.PostAsJsonAsync("/api/interno/autenticacao",
                new { Email = "admin@teste.local", Senha = "senha-incorreta" });
            Assert.Equal(HttpStatusCode.Unauthorized, falha.StatusCode);
        }
        var bloqueado = await cliente.PostAsJsonAsync("/api/interno/autenticacao",
            new { Email = "admin@teste.local", Senha = senhaAdmin });
        Assert.Equal(HttpStatusCode.Unauthorized, bloqueado.StatusCode);

        var semBootstrap = new Dictionary<string, string?>(configuracoes)
        {
            ["Bootstrap:AdminEmail"] = string.Empty,
            ["Bootstrap:AdminPassword"] = string.Empty
        };
        using var fabricaReiniciada = new OficinaApiFactory(semBootstrap);
        using var clienteReiniciado = fabricaReiniciada.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await clienteReiniciado.GetAsync("/api/saude")).StatusCode);
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
