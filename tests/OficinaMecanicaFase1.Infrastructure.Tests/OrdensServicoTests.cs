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
using OficinaMecanicaFase1.Api.OrdensServico;
using OficinaMecanicaFase1.Application.OrdensServico;
using OficinaMecanicaFase1.Domain.Cadastros;
using OficinaMecanicaFase1.Domain.Catalogo;
using OficinaMecanicaFase1.Domain.Estoque;
using OficinaMecanicaFase1.Domain.OrdensServico;
using OficinaMecanicaFase1.Domain.Orcamentos;
using OficinaMecanicaFase1.Infrastructure.Persistence;
using Testcontainers.MsSql;
using Xunit;

namespace OficinaMecanicaFase1.Infrastructure.Tests;

public sealed class OrdensServicoTests
{
    [Fact]
    public async Task ReservasSaoAtomicasAvisamEstoquistaEPreservamUltimaUnidadeSobConcorrencia()
    {
        await using var sqlServer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-CU14-ubuntu-22.04").Build();
        await sqlServer.StartAsync();
        var conexao = new SqlConnectionStringBuilder(sqlServer.GetConnectionString())
        {
            InitialCatalog = $"OficinaReservas_{Guid.NewGuid():N}"
        };
        var configuracoes = new Dictionary<string, string?>
        {
            ["ConnectionStrings:Oficina"] = conexao.ConnectionString,
            ["Jwt:Issuer"] = "OficinaMecanicaFase1",
            ["Jwt:Audience"] = "OficinaMecanicaFase1.Interno",
            ["Jwt:SigningKey"] = Convert.ToBase64String(Encoding.UTF8.GetBytes("UmaChaveFixaDeTesteComMaisDeTrintaEDoisBytes!")),
            ["Bootstrap:AdminEmail"] = "admin@reservas.test",
            ["Bootstrap:AdminPassword"] = "AdminDeTeste123!"
        };
        using var fabrica = new OficinaApiFactory(configuracoes);
        using var interno = fabrica.CreateClient();
        using var publico = fabrica.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await interno.GetAsync("/api/avisos-estoque/reservas")).StatusCode);
        await AutenticarAsync(interno, "admin@reservas.test", "AdminDeTeste123!");
        var pessoa = await CriarClienteAsync(interno, "Ana", "529.982.247-25");
        var veiculo = await CriarVeiculoAsync(interno, pessoa.Id, "ABC1234");
        var servico = await CriarServicoAsync(interno, "Revisão", 120m);
        var peca = await CriarItemAsync(interno, "Filtro", "Peca", "un");
        var insumo = await CriarItemAsync(interno, "Óleo", "Insumo", "L");
        var ultimaPeca = await CriarItemAsync(interno, "Última peça", "Peca", "un");
        foreach (var (email, senha, papel) in new[]
        {
            ("estoquista@reservas.test", "EstoquistaDeTeste123!", "Estoquista"),
            ("mecanico@reservas.test", "MecanicoDeTeste123!", "Mecânico"),
            ("atendente@reservas.test", "AtendenteDeTeste123!", "Atendente")
        })
            Assert.Equal(HttpStatusCode.Created, (await interno.PostAsJsonAsync("/api/interno/usuarios",
                new { Email = email, Senha = senha, Papel = papel })).StatusCode);

        await AutenticarAsync(interno, "estoquista@reservas.test", "EstoquistaDeTeste123!");
        await ReporAsync(interno, peca.Id, 1m);
        await ReporAsync(interno, ultimaPeca.Id, 1m);
        await AutenticarAsync(interno, "admin@reservas.test", "AdminDeTeste123!");
        var numeros = new List<long>();
        foreach (var itens in new[]
        {
            new[] { new ItemNecessarioRequest(peca.Id, 1m), new ItemNecessarioRequest(insumo.Id, 0.5m) },
            new[] { new ItemNecessarioRequest(ultimaPeca.Id, 1m) },
            new[] { new ItemNecessarioRequest(ultimaPeca.Id, 1m) }
        })
        {
            var resposta = await interno.PostAsJsonAsync("/api/ordens-servico",
                new CriarOrdemServicoRequest(pessoa.Id, veiculo.Id, [servico.Id], itens));
            Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
            numeros.Add((await resposta.Content.ReadFromJsonAsync<OrdemServicoResponse>())!.Numero);
        }
        await AutenticarAsync(interno, "mecanico@reservas.test", "MecanicoDeTeste123!");
        foreach (var numero in numeros)
        {
            var rota = $"/api/ordens-servico/{numero}";
            Assert.Equal(HttpStatusCode.OK, (await interno.PostAsync(rota + "/diagnostico/inicio", null)).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await interno.PostAsJsonAsync(rota + "/diagnostico",
                new RegistrarDiagnosticoRequest("Serviço necessário"))).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await interno.PostAsync(rota + "/orcamento", null)).StatusCode);
        }
        await AutenticarAsync(interno, "atendente@reservas.test", "AtendenteDeTeste123!");
        var codigos = new List<string>();
        foreach (var numero in numeros)
        {
            var resposta = await interno.PostAsJsonAsync(
                $"/api/ordens-servico/{numero}/codigo-acesso/emissao", new EmitirCodigoAcessoRequest(null));
            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            codigos.Add((await resposta.Content.ReadFromJsonAsync<CodigoAcessoResponse>())!.Codigo);
        }
        Assert.Equal(HttpStatusCode.Forbidden,
            (await interno.GetAsync("/api/avisos-estoque/reservas")).StatusCode);
        var decisao = new DecidirOrcamentoClienteRequest("529.982.247-25", codigos[0], true, null);
        var aprovacao = await publico.PostAsJsonAsync(
            $"/api/cliente/ordens-servico/{numeros[0]}/orcamento/decisao", decisao);
        Assert.Equal(HttpStatusCode.OK, aprovacao.StatusCode);
        var semReserva = (await interno.GetFromJsonAsync<OrdemServicoResponse>(
            $"/api/ordens-servico/{numeros[0]}"))!;
        Assert.Equal("AguardandoAprovacao", semReserva.Status);
        Assert.Equal("Recusada", semReserva.SituacaoReserva);
        Assert.False(semReserva.PodeIniciarExecucao);
        var pecaSemReserva = (await interno.GetFromJsonAsync<ItemEstoqueResponse>(
            $"/api/itens-estoque/{peca.Id}"))!;
        Assert.Equal(1m, pecaSemReserva.SaldoFisico);
        Assert.Equal(0m, pecaSemReserva.SaldoReservado);
        await AutenticarAsync(interno, "estoquista@reservas.test", "EstoquistaDeTeste123!");
        var avisos = (await interno.GetFromJsonAsync<AvisoReservaEstoque[]>(
            "/api/avisos-estoque/reservas"))!;
        Assert.Single(avisos);
        Assert.Equal(insumo.Id, avisos[0].ItemEstoqueId);
        await ReporAsync(interno, insumo.Id, 0.5m);
        var novaTentativa = await interno.PostAsync(
            $"/api/ordens-servico/{numeros[0]}/reservas/tentativa", null);
        Assert.Equal(HttpStatusCode.OK, novaTentativa.StatusCode);
        Assert.Equal("Confirmada", (await novaTentativa.Content.ReadFromJsonAsync<TentativaReservaResponse>())!.Situacao);
        Assert.Empty((await interno.GetFromJsonAsync<AvisoReservaEstoque[]>(
            "/api/avisos-estoque/reservas"))!);
        var confirmada = (await interno.GetFromJsonAsync<OrdemServicoResponse>(
            $"/api/ordens-servico/{numeros[0]}"))!;
        Assert.True(confirmada.PodeIniciarExecucao);
        Assert.Equal("AguardandoAprovacao", confirmada.Status);
        Assert.Equal(1m, (await interno.GetFromJsonAsync<ItemEstoqueResponse>(
            $"/api/itens-estoque/{peca.Id}"))!.SaldoReservado);
        var insumoReservado = (await interno.GetFromJsonAsync<ItemEstoqueResponse>(
            $"/api/itens-estoque/{insumo.Id}"))!;
        Assert.Equal(0.5m, insumoReservado.SaldoFisico);
        Assert.Equal(0.5m, insumoReservado.SaldoReservado);
        Assert.Equal(HttpStatusCode.OK, (await interno.PostAsync(
            $"/api/ordens-servico/{numeros[0]}/reservas/tentativa", null)).StatusCode);

        var aprovacoesConcorrentes = await Task.WhenAll(numeros.Skip(1).Select((numero, indice) =>
            publico.PostAsJsonAsync($"/api/cliente/ordens-servico/{numero}/orcamento/decisao",
                new DecidirOrcamentoClienteRequest("529.982.247-25", codigos[indice + 1], true, null))));
        Assert.All(aprovacoesConcorrentes, resposta => Assert.Equal(HttpStatusCode.OK, resposta.StatusCode));
        var disputantes = await Task.WhenAll(numeros.Skip(1).Select(numero =>
            interno.GetFromJsonAsync<OrdemServicoResponse>($"/api/ordens-servico/{numero}")));
        Assert.Single(disputantes, ordem => ordem!.SituacaoReserva == "Confirmada");
        Assert.Single(disputantes, ordem => ordem!.SituacaoReserva == "Recusada");
        Assert.All(disputantes, ordem => Assert.Equal("AguardandoAprovacao", ordem!.Status));
        var ultima = (await interno.GetFromJsonAsync<ItemEstoqueResponse>(
            $"/api/itens-estoque/{ultimaPeca.Id}"))!;
        Assert.Equal(1m, ultima.SaldoFisico);
        Assert.Equal(1m, ultima.SaldoReservado);
        using var escopo = fabrica.Services.CreateScope();
        var db = escopo.ServiceProvider.GetRequiredService<OficinaDbContext>();
        Assert.False(db.Database.HasPendingModelChanges());
        Assert.Equal(1, await db.ReservasItensOrdensServico.CountAsync(reserva =>
            reserva.ItemEstoqueId == ultimaPeca.Id && reserva.LiberadaEmUtc == null));
        Assert.Single((await interno.GetFromJsonAsync<AvisoReservaEstoque[]>(
            "/api/avisos-estoque/reservas"))!);
    }

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
    public void DiagnosticoPrecedeOrcamentoEValoresSaoCongelados()
    {
        var ordem = OrdemServico.Criar(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [], DateTimeOffset.UtcNow);
        Assert.Throws<InvalidOperationException>(() => ordem.DisponibilizarOrcamento(DateTimeOffset.UtcNow));
        Assert.Throws<InvalidOperationException>(() => ordem.RegistrarDiagnostico(Guid.NewGuid(), "Ruído", DateTimeOffset.UtcNow));
        var mecanico = Guid.NewGuid();
        ordem.IniciarDiagnostico(mecanico, DateTimeOffset.UtcNow);
        Assert.Equal(StatusOrdemServico.EmDiagnostico, ordem.Status);
        ordem.RegistrarDiagnostico(mecanico, "Pastilhas desgastadas", DateTimeOffset.UtcNow);
        Assert.Throws<InvalidOperationException>(() => ordem.RegistrarDiagnostico(mecanico, "Outro", DateTimeOffset.UtcNow));
        var orcamento = Orcamento.Gerar(ordem.Id, DateTimeOffset.UtcNow,
            [(Guid.NewGuid(), "Freio", 100m)], [(Guid.NewGuid(), "Pastilha", "un", 2m, 12.50m)]);
        ordem.DisponibilizarOrcamento(DateTimeOffset.UtcNow);
        Assert.Equal(StatusOrdemServico.AguardandoAprovacao, ordem.Status);
        Assert.Equal(100m, orcamento.TotalServicos);
        Assert.Equal(25m, orcamento.TotalItens);
        Assert.Equal(125m, orcamento.Total);
        Assert.Throws<InvalidOperationException>(() => ordem.AdicionarServicoSolicitado(Guid.NewGuid()));
    }

    [Fact]
    public async Task ApiExigeMecanicoEPublicaOrcamentoAposDiagnostico()
    {
        await using var sqlServer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-CU14-ubuntu-22.04").Build();
        await sqlServer.StartAsync();
        var conexao = new SqlConnectionStringBuilder(sqlServer.GetConnectionString())
        {
            InitialCatalog = $"OficinaOrcamentos_{Guid.NewGuid():N}"
        };
        var configuracoes = new Dictionary<string, string?>
        {
            ["ConnectionStrings:Oficina"] = conexao.ConnectionString,
            ["Jwt:Issuer"] = "OficinaMecanicaFase1",
            ["Jwt:Audience"] = "OficinaMecanicaFase1.Interno",
            ["Jwt:SigningKey"] = Convert.ToBase64String(Encoding.UTF8.GetBytes("UmaChaveFixaDeTesteComMaisDeTrintaEDoisBytes!")),
            ["Bootstrap:AdminEmail"] = "admin@orcamentos.test",
            ["Bootstrap:AdminPassword"] = "AdminDeTeste123!"
        };
        using var fabrica = new OficinaApiFactory(configuracoes);
        using var cliente = fabrica.CreateClient();
        await AutenticarAsync(cliente, "admin@orcamentos.test", "AdminDeTeste123!");
        var pessoa = await CriarClienteAsync(cliente, "Ana", "529.982.247-25");
        var veiculo = await CriarVeiculoAsync(cliente, pessoa.Id, "ABC1234");
        var servico = await CriarServicoAsync(cliente, "Revisão", 120m);
        var item = await CriarItemAsync(cliente, "Óleo", "Insumo", "L");
        var criado = await cliente.PostAsJsonAsync("/api/ordens-servico", new
        {
            clienteId = pessoa.Id, veiculoId = veiculo.Id, servicosIds = new[] { servico.Id },
            itensNecessarios = new[] { new { itemEstoqueId = item.Id, quantidade = 0.5m } }
        });
        Assert.Equal(HttpStatusCode.Created, criado.StatusCode);
        var ordem = (await criado.Content.ReadFromJsonAsync<OrdemServicoResponse>())!;
        var rota = $"/api/ordens-servico/{ordem.Numero}";
        Assert.Equal(HttpStatusCode.Forbidden, (await cliente.PostAsync(rota + "/orcamento", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await cliente.PostAsync(rota + "/diagnostico/inicio", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await cliente.GetAsync(rota + "/orcamento")).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await cliente.PostAsJsonAsync("/api/interno/usuarios", new
        {
            Email = "mecanico@orcamentos.test", Senha = "MecanicoDeTeste123!", Papel = "Mecânico"
        })).StatusCode);
        await AutenticarAsync(cliente, "mecanico@orcamentos.test", "MecanicoDeTeste123!");
        Assert.Equal(HttpStatusCode.BadRequest, (await cliente.PostAsync(rota + "/orcamento", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await cliente.PostAsync(rota + "/diagnostico/inicio", null)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await cliente.PostAsync(rota + "/orcamento", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await cliente.PostAsJsonAsync(rota + "/diagnostico",
            new RegistrarDiagnosticoRequest("Troca de óleo necessária"))).StatusCode);
        var gerado = await cliente.PostAsync(rota + "/orcamento", null);
        Assert.True(gerado.StatusCode == HttpStatusCode.OK,
            $"{gerado.StatusCode}: {await gerado.Content.ReadAsStringAsync()}");
        var proposta = (await gerado.Content.ReadFromJsonAsync<OrcamentoResponse>())!;
        Assert.Equal(120m, proposta.TotalServicos);
        Assert.Equal(10m, proposta.TotalItens);
        Assert.Equal(130m, proposta.Total);
        Assert.Equal(2, proposta.Linhas.Length);
        Assert.Equal(HttpStatusCode.BadRequest, (await cliente.PostAsync(rota + "/orcamento", null)).StatusCode);
        var consultada = (await cliente.GetFromJsonAsync<OrcamentoResponse>(rota + "/orcamento"))!;
        Assert.Equal(proposta.Id, consultada.Id);
        Assert.Equal(proposta.Total, consultada.Total);
        var osAtualizada = (await cliente.GetFromJsonAsync<OrdemServicoResponse>(rota))!;
        Assert.Equal("AguardandoAprovacao", osAtualizada.Status);
        Assert.Equal("Troca de óleo necessária", osAtualizada.Diagnostico.Descricao);
        Assert.Equal(4, osAtualizada.Marcos.Length);
        await AutenticarAsync(cliente, "admin@orcamentos.test", "AdminDeTeste123!");
        Assert.Equal(HttpStatusCode.OK, (await cliente.PutAsJsonAsync($"/api/servicos/{servico.Id}",
            new ServicoCatalogoRequest("Revisão", null, 300m))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await cliente.PutAsJsonAsync($"/api/itens-estoque/{item.Id}",
            new AtualizarItemEstoqueRequest("Óleo", null, 50m))).StatusCode);
        var propostaHistorica = (await cliente.GetFromJsonAsync<OrcamentoResponse>(rota + "/orcamento"))!;
        Assert.Equal(130m, propostaHistorica.Total);
        Assert.Contains(propostaHistorica.Linhas, linha => linha.Tipo == "Servico" && linha.PrecoUnitario == 120m);
        Assert.Contains(propostaHistorica.Linhas, linha => linha.Tipo == "Item" && linha.PrecoUnitario == 20m);
        using var escopo = fabrica.Services.CreateScope();
        var db = escopo.ServiceProvider.GetRequiredService<OficinaDbContext>();
        Assert.False(db.Database.HasPendingModelChanges());
        Assert.Equal(0m, await db.ItensEstoque.Where(i => i.Id == item.Id).Select(i => i.SaldoReservado).SingleAsync());
    }

    [Fact]
    public async Task ClienteConsultaEDecideComCodigoReemitidoERejeicaoLiberaReservas()
    {
        await using var sqlServer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-CU14-ubuntu-22.04").Build();
        await sqlServer.StartAsync();
        var conexao = new SqlConnectionStringBuilder(sqlServer.GetConnectionString())
        {
            InitialCatalog = $"OficinaDecisoes_{Guid.NewGuid():N}"
        };
        var configuracoes = new Dictionary<string, string?>
        {
            ["ConnectionStrings:Oficina"] = conexao.ConnectionString,
            ["Jwt:Issuer"] = "OficinaMecanicaFase1",
            ["Jwt:Audience"] = "OficinaMecanicaFase1.Interno",
            ["Jwt:SigningKey"] = Convert.ToBase64String(Encoding.UTF8.GetBytes("UmaChaveFixaDeTesteComMaisDeTrintaEDoisBytes!")),
            ["Bootstrap:AdminEmail"] = "admin@decisoes.test",
            ["Bootstrap:AdminPassword"] = "AdminDeTeste123!"
        };
        using var fabrica = new OficinaApiFactory(configuracoes);
        using var interno = fabrica.CreateClient();
        using var publico = fabrica.CreateClient();
        await AutenticarAsync(interno, "admin@decisoes.test", "AdminDeTeste123!");
        var pessoa = await CriarClienteAsync(interno, "Ana", "529.982.247-25");
        var veiculo = await CriarVeiculoAsync(interno, pessoa.Id, "ABC1234");
        var servico = await CriarServicoAsync(interno, "Revisão", 120m);
        var item = await CriarItemAsync(interno, "Filtro", "Peca", "un");
        Assert.Equal(HttpStatusCode.Created, (await interno.PostAsJsonAsync("/api/interno/usuarios", new
        {
            Email = "atendente@decisoes.test", Senha = "AtendenteDeTeste123!", Papel = "Atendente"
        })).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await interno.PostAsJsonAsync("/api/interno/usuarios", new
        {
            Email = "mecanico@decisoes.test", Senha = "MecanicoDeTeste123!", Papel = "Mecânico"
        })).StatusCode);

        var numeros = new List<long>();
        for (var indice = 0; indice < 3; indice++)
        {
            var criada = await interno.PostAsJsonAsync("/api/ordens-servico", new
            {
                clienteId = pessoa.Id, veiculoId = veiculo.Id, servicosIds = new[] { servico.Id },
                itensNecessarios = new[] { new { itemEstoqueId = item.Id, quantidade = 1m } }
            });
            Assert.Equal(HttpStatusCode.Created, criada.StatusCode);
            numeros.Add((await criada.Content.ReadFromJsonAsync<OrdemServicoResponse>())!.Numero);
        }
        await AutenticarAsync(interno, "mecanico@decisoes.test", "MecanicoDeTeste123!");
        foreach (var numero in numeros)
        {
            var rota = $"/api/ordens-servico/{numero}";
            Assert.Equal(HttpStatusCode.OK, (await interno.PostAsync(rota + "/diagnostico/inicio", null)).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await interno.PostAsJsonAsync(rota + "/diagnostico",
                new RegistrarDiagnosticoRequest("Serviço necessário"))).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await interno.PostAsync(rota + "/orcamento", null)).StatusCode);
        }
        var primeiraRota = $"/api/cliente/ordens-servico/{numeros[0]}/orcamento";
        Assert.Equal(HttpStatusCode.NotFound, (await publico.PostAsJsonAsync(primeiraRota + "/consulta",
            new ConsultarOrcamentoClienteRequest("04.252.011/0001-10"))).StatusCode);
        var consulta = await publico.PostAsJsonAsync(primeiraRota + "/consulta",
            new ConsultarOrcamentoClienteRequest("529.982.247-25"));
        Assert.Equal(HttpStatusCode.OK, consulta.StatusCode);
        Assert.Equal("no-store", consulta.Headers.CacheControl?.ToString());
        Assert.Equal(HttpStatusCode.Unauthorized, (await publico.GetAsync(
            $"/api/ordens-servico/{numeros[0]}/orcamento")).StatusCode);

        await AutenticarAsync(interno, "admin@decisoes.test", "AdminDeTeste123!");
        Assert.Equal(HttpStatusCode.Forbidden, (await interno.PostAsJsonAsync(
            $"/api/ordens-servico/{numeros[0]}/codigo-acesso/emissao",
            new EmitirCodigoAcessoRequest(null))).StatusCode);
        await AutenticarAsync(interno, "atendente@decisoes.test", "AtendenteDeTeste123!");
        var emissaoRota = $"/api/ordens-servico/{numeros[0]}/codigo-acesso/emissao";
        var primeiraEmissao = await interno.PostAsJsonAsync(emissaoRota, new EmitirCodigoAcessoRequest(null));
        Assert.Equal(HttpStatusCode.OK, primeiraEmissao.StatusCode);
        var primeiroCodigo = (await primeiraEmissao.Content.ReadFromJsonAsync<CodigoAcessoResponse>())!;
        Assert.Equal(1, primeiroCodigo.NumeroEmissao);
        Assert.DoesNotContain(primeiroCodigo.Codigo, await consulta.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.BadRequest, (await interno.PostAsJsonAsync(emissaoRota,
            new EmitirCodigoAcessoRequest(null))).StatusCode);
        var segundaEmissao = await interno.PostAsJsonAsync(emissaoRota,
            new EmitirCodigoAcessoRequest("Cliente perdeu o código anterior"));
        Assert.Equal(HttpStatusCode.OK, segundaEmissao.StatusCode);
        var segundoCodigo = (await segundaEmissao.Content.ReadFromJsonAsync<CodigoAcessoResponse>())!;
        Assert.Equal(2, segundoCodigo.NumeroEmissao);
        Assert.NotEqual(primeiroCodigo.Codigo, segundoCodigo.Codigo);
        Assert.Equal(HttpStatusCode.NotFound, (await publico.PostAsJsonAsync(primeiraRota + "/decisao",
            new DecidirOrcamentoClienteRequest("529.982.247-25", primeiroCodigo.Codigo, true, null))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await publico.PostAsJsonAsync(primeiraRota + "/decisao",
            new DecidirOrcamentoClienteRequest("04.252.011/0001-10", segundoCodigo.Codigo, true, null))).StatusCode);
        var aprovada = await publico.PostAsJsonAsync(primeiraRota + "/decisao",
            new DecidirOrcamentoClienteRequest("529.982.247-25", segundoCodigo.Codigo, true, null));
        Assert.Equal(HttpStatusCode.OK, aprovada.StatusCode);
        Assert.Equal("Aprovado", (await aprovada.Content.ReadFromJsonAsync<OrcamentoResponse>())!.Decisao);
        Assert.Equal(HttpStatusCode.Conflict, (await publico.PostAsJsonAsync(primeiraRota + "/decisao",
            new DecidirOrcamentoClienteRequest("529.982.247-25", segundoCodigo.Codigo, false, "Mudei de ideia"))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await interno.PostAsJsonAsync(emissaoRota,
            new EmitirCodigoAcessoRequest("Nova solicitação"))).StatusCode);

        using (var escopo = fabrica.Services.CreateScope())
        {
            var db = escopo.ServiceProvider.GetRequiredService<OficinaDbContext>();
            var itemPersistido = await db.ItensEstoque.SingleAsync(estoque => estoque.Id == item.Id);
            db.EntradasEstoque.Add(itemPersistido.RegistrarEntrada(2m, DateTimeOffset.UtcNow, Guid.NewGuid()));
            itemPersistido.Reservar(1m);
            var segundaOrdem = await db.OrdensServico.SingleAsync(ordem => ordem.Numero == numeros[1]);
            db.ReservasItensOrdensServico.Add(ReservaItemOrdemServico.Criar(
                segundaOrdem.Id, item.Id, 1m, DateTimeOffset.UtcNow));
            await db.SaveChangesAsync();
        }
        var segundaEmissaoRota = $"/api/ordens-servico/{numeros[1]}/codigo-acesso/emissao";
        var codigoRejeicaoResposta = await interno.PostAsJsonAsync(segundaEmissaoRota,
            new EmitirCodigoAcessoRequest(null));
        Assert.Equal(HttpStatusCode.OK, codigoRejeicaoResposta.StatusCode);
        var codigoRejeicao = (await codigoRejeicaoResposta.Content.ReadFromJsonAsync<CodigoAcessoResponse>())!.Codigo;
        var segundaRota = $"/api/cliente/ordens-servico/{numeros[1]}/orcamento";
        Assert.Equal(HttpStatusCode.BadRequest, (await publico.PostAsJsonAsync(segundaRota + "/decisao",
            new DecidirOrcamentoClienteRequest("529.982.247-25", codigoRejeicao, false, null))).StatusCode);
        var rejeitada = await publico.PostAsJsonAsync(segundaRota + "/decisao",
            new DecidirOrcamentoClienteRequest("529.982.247-25", codigoRejeicao, false, "Cliente desistiu"));
        Assert.True(rejeitada.StatusCode == HttpStatusCode.OK,
            $"{rejeitada.StatusCode}: {await rejeitada.Content.ReadAsStringAsync()}");
        Assert.Equal("Rejeitado", (await rejeitada.Content.ReadFromJsonAsync<OrcamentoResponse>())!.Decisao);
        var encerrada = (await interno.GetFromJsonAsync<OrdemServicoResponse>(
            $"/api/ordens-servico/{numeros[1]}"))!;
        Assert.Equal("AguardandoAprovacao", encerrada.Status);
        Assert.NotNull(encerrada.EncerradaPorRejeicaoEmUtc);
        Assert.Equal("Cliente desistiu", encerrada.MotivoEncerramento);
        Assert.Contains(encerrada.Marcos, marco => marco.Descricao == "Orçamento rejeitado");
        Assert.Contains(encerrada.Marcos, marco => marco.Descricao == "OS encerrada por rejeição");
        using (var escopo = fabrica.Services.CreateScope())
        {
            var db = escopo.ServiceProvider.GetRequiredService<OficinaDbContext>();
            Assert.False(db.Database.HasPendingModelChanges());
            Assert.Equal(0m, await db.ItensEstoque.Where(estoque => estoque.Id == item.Id)
                .Select(estoque => estoque.SaldoReservado).SingleAsync());
            Assert.Equal(2m, await db.ItensEstoque.Where(estoque => estoque.Id == item.Id)
                .Select(estoque => estoque.SaldoFisico).SingleAsync());
            Assert.NotNull((await db.ReservasItensOrdensServico.SingleAsync()).LiberadaEmUtc);
            Assert.Equal(2, await db.Set<EmissaoCodigoAcesso>().CountAsync(
                emissao => emissao.OrdemServicoId ==
                    db.OrdensServico.Where(ordem => ordem.Numero == numeros[0]).Select(ordem => ordem.Id).Single()));
        }

        var terceiraEmissaoResposta = await interno.PostAsJsonAsync(
            $"/api/ordens-servico/{numeros[2]}/codigo-acesso/emissao", new EmitirCodigoAcessoRequest(null));
        Assert.Equal(HttpStatusCode.OK, terceiraEmissaoResposta.StatusCode);
        var codigoConcorrente = (await terceiraEmissaoResposta.Content.ReadFromJsonAsync<CodigoAcessoResponse>())!.Codigo;
        var terceiraRota = $"/api/cliente/ordens-servico/{numeros[2]}/orcamento";
        using var publicoConcorrente = fabrica.CreateClient();
        var aprovar = publico.PostAsJsonAsync(terceiraRota + "/decisao",
            new DecidirOrcamentoClienteRequest("529.982.247-25", codigoConcorrente, true, null));
        var rejeitar = publicoConcorrente.PostAsJsonAsync(terceiraRota + "/decisao",
            new DecidirOrcamentoClienteRequest("529.982.247-25", codigoConcorrente, false, "Não aprovado"));
        var respostas = await Task.WhenAll(aprovar, rejeitar);
        Assert.Single(respostas, resposta => resposta.StatusCode == HttpStatusCode.OK);
        Assert.Single(respostas, resposta => resposta.StatusCode == HttpStatusCode.Conflict);
        var resultadoFinal = (await (await publico.PostAsJsonAsync(terceiraRota + "/consulta",
            new ConsultarOrcamentoClienteRequest("529.982.247-25")))
            .Content.ReadFromJsonAsync<OrcamentoResponse>())!;
        Assert.NotEqual("Pendente", resultadoFinal.Decisao);
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

    [Fact]
    public async Task ExecucaoRegistraServicosConsumoRealEDevolveReservaNaoUsada()
    {
        await using var sqlServer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-CU14-ubuntu-22.04").Build();
        await sqlServer.StartAsync();
        var conexao = new SqlConnectionStringBuilder(sqlServer.GetConnectionString())
        {
            InitialCatalog = $"OficinaExecucao_{Guid.NewGuid():N}"
        };
        var configuracoes = new Dictionary<string, string?>
        {
            ["ConnectionStrings:Oficina"] = conexao.ConnectionString,
            ["Jwt:Issuer"] = "OficinaMecanicaFase1",
            ["Jwt:Audience"] = "OficinaMecanicaFase1.Interno",
            ["Jwt:SigningKey"] = Convert.ToBase64String(Encoding.UTF8.GetBytes("UmaChaveFixaDeTesteComMaisDeTrintaEDoisBytes!")),
            ["Bootstrap:AdminEmail"] = "admin@execucao.test",
            ["Bootstrap:AdminPassword"] = "AdminDeTeste123!"
        };
        using var fabrica = new OficinaApiFactory(configuracoes);
        using var interno = fabrica.CreateClient();
        using var publico = fabrica.CreateClient();
        await AutenticarAsync(interno, "admin@execucao.test", "AdminDeTeste123!");
        var pessoa = await CriarClienteAsync(interno, "Ana", "529.982.247-25");
        var veiculo = await CriarVeiculoAsync(interno, pessoa.Id, "ABC1234");
        var servicoFreio = await CriarServicoAsync(interno, "Freio", 150m);
        var servicoRevisao = await CriarServicoAsync(interno, "Revisão", 90m);
        var item = await CriarItemAsync(interno, "Pastilha", "Peca", "un");
        var insumo = await CriarItemAsync(interno, "Óleo", "Insumo", "L");
        foreach (var (email, senha, papel) in new[]
        {
            ("estoquista@execucao.test", "EstoquistaDeTeste123!", "Estoquista"),
            ("mecanico@execucao.test", "MecanicoDeTeste123!", "Mecânico"),
            ("atendente@execucao.test", "AtendenteDeTeste123!", "Atendente")
        })
            Assert.Equal(HttpStatusCode.Created, (await interno.PostAsJsonAsync("/api/interno/usuarios",
                new { Email = email, Senha = senha, Papel = papel })).StatusCode);

        await AutenticarAsync(interno, "estoquista@execucao.test", "EstoquistaDeTeste123!");
        await ReporAsync(interno, item.Id, 2m);
        await ReporAsync(interno, insumo.Id, 1.25m);
        await AutenticarAsync(interno, "admin@execucao.test", "AdminDeTeste123!");
        var criada = await interno.PostAsJsonAsync("/api/ordens-servico", new
        {
            clienteId = pessoa.Id, veiculoId = veiculo.Id,
            servicosIds = new[] { servicoFreio.Id, servicoRevisao.Id },
            itensNecessarios = new[]
            {
                new { itemEstoqueId = item.Id, quantidade = 2m },
                new { itemEstoqueId = insumo.Id, quantidade = 1.25m }
            }
        });
        Assert.Equal(HttpStatusCode.Created, criada.StatusCode);
        var numero = (await criada.Content.ReadFromJsonAsync<OrdemServicoResponse>())!.Numero;
        var rota = $"/api/ordens-servico/{numero}";
        await AutenticarAsync(interno, "mecanico@execucao.test", "MecanicoDeTeste123!");
        Assert.Equal(HttpStatusCode.OK, (await interno.PostAsync(rota + "/diagnostico/inicio", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await interno.PostAsJsonAsync(rota + "/diagnostico",
            new RegistrarDiagnosticoRequest("Pastilhas e óleo precisam de troca"))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await interno.PostAsync(rota + "/orcamento", null)).StatusCode);
        await AutenticarAsync(interno, "atendente@execucao.test", "AtendenteDeTeste123!");
        var emissao = await interno.PostAsJsonAsync(rota + "/codigo-acesso/emissao", new EmitirCodigoAcessoRequest(null));
        var codigo = (await emissao.Content.ReadFromJsonAsync<CodigoAcessoResponse>())!.Codigo;
        Assert.Equal(HttpStatusCode.OK, (await publico.PostAsJsonAsync(
            $"/api/cliente/ordens-servico/{numero}/orcamento/decisao",
            new DecidirOrcamentoClienteRequest("529.982.247-25", codigo, true, null))).StatusCode);

        Assert.Equal(HttpStatusCode.BadRequest, (await interno.PostAsync(rota + "/entrega", null)).StatusCode);

        var estoqueReservado = (await interno.GetFromJsonAsync<ItemEstoqueResponse>($"/api/itens-estoque/{item.Id}"))!;
        Assert.Equal(2m, estoqueReservado.SaldoFisico);
        Assert.Equal(2m, estoqueReservado.SaldoReservado);
        Assert.Equal(0m, estoqueReservado.SaldoDisponivel);
        Assert.Equal(HttpStatusCode.Forbidden, (await interno.PostAsync(rota + "/execucao/inicio", null)).StatusCode);
        await AutenticarAsync(interno, "mecanico@execucao.test", "MecanicoDeTeste123!");
        var inicio = await interno.PostAsync(rota + "/execucao/inicio", null);
        Assert.Equal(HttpStatusCode.OK, inicio.StatusCode);
        Assert.Equal("EmExecucao", (await inicio.Content.ReadFromJsonAsync<OrdemServicoResponse>())!.Status);
        Assert.Equal(HttpStatusCode.BadRequest, (await interno.PostAsync(
            $"{rota}/servicos/{Guid.NewGuid()}/inicio", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await interno.PostAsync(
            $"{rota}/servicos/{servicoFreio.Id}/inicio", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await interno.PostAsync(
            $"{rota}/servicos/{servicoRevisao.Id}/inicio", null)).StatusCode);

        var consumo = await interno.PostAsJsonAsync(rota + "/consumos", new RegistrarConsumoRequest(
        [new ConsumoItemRequest(item.Id, 1m), new ConsumoItemRequest(insumo.Id, 0.25m)]));
        Assert.Equal(HttpStatusCode.OK, consumo.StatusCode);
        var consumoExcedente = await interno.PostAsJsonAsync(rota + "/consumos",
            new RegistrarConsumoRequest([new ConsumoItemRequest(item.Id, 2m)]));
        Assert.Equal(HttpStatusCode.BadRequest, consumoExcedente.StatusCode);
        var itemParcial = (await interno.GetFromJsonAsync<ItemEstoqueResponse>($"/api/itens-estoque/{item.Id}"))!;
        Assert.Equal(1m, itemParcial.SaldoFisico);
        Assert.Equal(1m, itemParcial.SaldoReservado);
        Assert.Equal(0m, itemParcial.SaldoDisponivel);
        Assert.Equal(HttpStatusCode.OK, (await interno.PostAsync(
            $"{rota}/servicos/{servicoFreio.Id}/conclusao", null)).StatusCode);
        var finalizada = await interno.PostAsync($"{rota}/servicos/{servicoRevisao.Id}/conclusao", null);
        Assert.Equal(HttpStatusCode.OK, finalizada.StatusCode);
        var ordemFinalizada = (await finalizada.Content.ReadFromJsonAsync<OrdemServicoResponse>())!;
        Assert.Equal("Finalizada", ordemFinalizada.Status);
        Assert.All(ordemFinalizada.ServicosSolicitados, servico =>
        {
            Assert.NotNull(servico.IniciadoEmUtc);
            Assert.NotNull(servico.ConcluidoEmUtc);
            Assert.NotNull(servico.IniciadoPorId);
            Assert.NotNull(servico.ConcluidoPorId);
        });
        var itemFinal = (await interno.GetFromJsonAsync<ItemEstoqueResponse>($"/api/itens-estoque/{item.Id}"))!;
        Assert.Equal(1m, itemFinal.SaldoFisico);
        Assert.Equal(0m, itemFinal.SaldoReservado);
        Assert.Equal(1m, itemFinal.SaldoDisponivel);
        var insumoFinal = (await interno.GetFromJsonAsync<ItemEstoqueResponse>($"/api/itens-estoque/{insumo.Id}"))!;
        Assert.Equal(1m, insumoFinal.SaldoFisico);
        Assert.Equal(0m, insumoFinal.SaldoReservado);
        var consultaPublica = $"/api/cliente/ordens-servico/{numero}/andamento/consulta";
        Assert.Equal(HttpStatusCode.NotFound, (await publico.PostAsJsonAsync(consultaPublica,
            new ConsultarOrcamentoClienteRequest("04.252.011/0001-10"))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await interno.PostAsync(rota + "/entrega", null)).StatusCode);
        await AutenticarAsync(interno, "atendente@execucao.test", "AtendenteDeTeste123!");
        var entrega = await interno.PostAsync(rota + "/entrega", null);
        Assert.Equal(HttpStatusCode.OK, entrega.StatusCode);
        var entregue = (await entrega.Content.ReadFromJsonAsync<OrdemServicoResponse>())!;
        Assert.Equal("Entregue", entregue.Status);
        Assert.NotNull(entregue.EntregueEmUtc);
        Assert.NotNull(entregue.EntreguePorId);
        Assert.Equal(HttpStatusCode.BadRequest, (await interno.PostAsync(rota + "/entrega", null)).StatusCode);
        var andamento = await publico.PostAsJsonAsync(consultaPublica,
            new ConsultarOrcamentoClienteRequest("529.982.247-25"));
        Assert.Equal(HttpStatusCode.OK, andamento.StatusCode);
        using (var json = JsonDocument.Parse(await andamento.Content.ReadAsStringAsync()))
        {
            var raiz = json.RootElement;
            Assert.Equal(new[] { "marcos", "numero", "status", "ultimaAtualizacaoEmUtc" },
                raiz.EnumerateObject().Select(propriedade => propriedade.Name).OrderBy(nome => nome));
            Assert.Equal("Entregue", raiz.GetProperty("status").GetString());
            Assert.Equal(entregue.EntregueEmUtc!.Value,
                raiz.GetProperty("ultimaAtualizacaoEmUtc").GetDateTimeOffset());
            Assert.Contains(raiz.GetProperty("marcos").EnumerateArray(), marco =>
                marco.GetProperty("descricao").GetString() == "OS entregue ao cliente");
        }
        using var escopo = fabrica.Services.CreateScope();
        var db = escopo.ServiceProvider.GetRequiredService<OficinaDbContext>();
        Assert.False(db.Database.HasPendingModelChanges());
        Assert.Equal(2, await db.ConsumosItensOrdensServico.CountAsync(consumoItem =>
            consumoItem.OrdemServicoId == db.OrdensServico.Where(ordem => ordem.Numero == numero)
                .Select(ordem => ordem.Id).Single()));
        Assert.All(await db.ReservasItensOrdensServico.Where(reserva => reserva.LiberadaEmUtc != null).ToArrayAsync(),
            reserva => Assert.Equal(reserva.Quantidade, reserva.QuantidadeConsumida +
                (reserva.ItemEstoqueId == item.Id ? 1m : 1m)));
    }

    [Fact]
    public async Task AditivoExigeDecisaoEReservaERejeicaoInseguraImpedeConclusao()
    {
        await using var sqlServer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-CU14-ubuntu-22.04").Build();
        await sqlServer.StartAsync();
        var conexao = new SqlConnectionStringBuilder(sqlServer.GetConnectionString())
        {
            InitialCatalog = $"OficinaAditivos_{Guid.NewGuid():N}"
        };
        var configuracoes = new Dictionary<string, string?>
        {
            ["ConnectionStrings:Oficina"] = conexao.ConnectionString,
            ["Jwt:Issuer"] = "OficinaMecanicaFase1",
            ["Jwt:Audience"] = "OficinaMecanicaFase1.Interno",
            ["Jwt:SigningKey"] = Convert.ToBase64String(Encoding.UTF8.GetBytes("UmaChaveFixaDeTesteComMaisDeTrintaEDoisBytes!")),
            ["Bootstrap:AdminEmail"] = "admin@aditivos.test",
            ["Bootstrap:AdminPassword"] = "AdminDeTeste123!"
        };
        using var fabrica = new OficinaApiFactory(configuracoes);
        using var interno = fabrica.CreateClient();
        using var publico = fabrica.CreateClient();
        await AutenticarAsync(interno, "admin@aditivos.test", "AdminDeTeste123!");
        var pessoa = await CriarClienteAsync(interno, "Ana", "529.982.247-25");
        var veiculo = await CriarVeiculoAsync(interno, pessoa.Id, "XYZ1A23");
        var servico = await CriarServicoAsync(interno, "Revisão", 120m);
        var extra = await CriarServicoAsync(interno, "Reparo adicional", 50m);
        var extraRecusado = await CriarServicoAsync(interno, "Reparo recusado", 40m);
        var item = await CriarItemAsync(interno, "Filtro", "Peca", "un");
        foreach (var (email, senha, papel) in new[]
        {
            ("estoquista@aditivos.test", "EstoquistaDeTeste123!", "Estoquista"),
            ("mecanico@aditivos.test", "MecanicoDeTeste123!", "Mecânico"),
            ("atendente@aditivos.test", "AtendenteDeTeste123!", "Atendente")
        })
            Assert.Equal(HttpStatusCode.Created, (await interno.PostAsJsonAsync("/api/interno/usuarios",
                new { Email = email, Senha = senha, Papel = papel })).StatusCode);
        await AutenticarAsync(interno, "estoquista@aditivos.test", "EstoquistaDeTeste123!");
        await ReporAsync(interno, item.Id, 2m);
        await AutenticarAsync(interno, "admin@aditivos.test", "AdminDeTeste123!");
        var abertura = await interno.PostAsJsonAsync("/api/ordens-servico",
            new CriarOrdemServicoRequest(pessoa.Id, veiculo.Id, [servico.Id],
                [new ItemNecessarioRequest(item.Id, 1m)]));
        Assert.Equal(HttpStatusCode.Created, abertura.StatusCode);
        var numero = (await abertura.Content.ReadFromJsonAsync<OrdemServicoResponse>())!.Numero;
        var rota = $"/api/ordens-servico/{numero}";
        await AutenticarAsync(interno, "mecanico@aditivos.test", "MecanicoDeTeste123!");
        Assert.Equal(HttpStatusCode.OK, (await interno.PostAsync(rota + "/diagnostico/inicio", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await interno.PostAsJsonAsync(rota + "/diagnostico",
            new RegistrarDiagnosticoRequest("Filtro desgastado"))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await interno.PostAsync(rota + "/orcamento", null)).StatusCode);
        await AutenticarAsync(interno, "atendente@aditivos.test", "AtendenteDeTeste123!");
        var emissao = await interno.PostAsJsonAsync(rota + "/codigo-acesso/emissao", new EmitirCodigoAcessoRequest(null));
        var codigo = (await emissao.Content.ReadFromJsonAsync<CodigoAcessoResponse>())!.Codigo;
        Assert.Equal(HttpStatusCode.OK, (await publico.PostAsJsonAsync(
            $"/api/cliente/ordens-servico/{numero}/orcamento/decisao",
            new DecidirOrcamentoClienteRequest("529.982.247-25", codigo, true, null))).StatusCode);
        await AutenticarAsync(interno, "mecanico@aditivos.test", "MecanicoDeTeste123!");
        Assert.Equal(HttpStatusCode.OK, (await interno.PostAsync(rota + "/execucao/inicio", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await interno.PostAsync(rota + $"/servicos/{servico.Id}/inicio", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await interno.PostAsJsonAsync(rota + "/consumos",
            new RegistrarConsumoRequest([new ConsumoItemRequest(item.Id, 1m)]))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await interno.PostAsJsonAsync(rota + "/consumos",
            new RegistrarConsumoRequest([new ConsumoItemRequest(item.Id, 1m)]))).StatusCode);

        var pedido = await interno.PostAsJsonAsync(rota + "/aditivos",
            new CriarAditivoRequest(TipoAditivo.ConsumoExcedente, "Mais um filtro necessário",
                null, item.Id, 1m, true));
        Assert.Equal(HttpStatusCode.Created, pedido.StatusCode);
        var aditivo = (await pedido.Content.ReadFromJsonAsync<AditivoResponse>())!;
        var publica = $"/api/cliente/ordens-servico/{numero}/aditivos/{aditivo.Id}";
        Assert.Equal(HttpStatusCode.NotFound, (await publico.PostAsJsonAsync(publica + "/consulta",
            new ConsultarAditivoRequest("04.252.011/0001-10"))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await publico.PostAsJsonAsync(publica + "/consulta",
            new ConsultarAditivoRequest("529.982.247-25"))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await interno.PostAsJsonAsync(rota + "/consumos",
            new RegistrarConsumoRequest([new ConsumoItemRequest(item.Id, 1m)]))).StatusCode);
        var decisao = await publico.PostAsJsonAsync(publica + "/decisao",
            new DecidirAditivoRequest("529.982.247-25", codigo, true, null));
        Assert.Equal(HttpStatusCode.OK, decisao.StatusCode);
        Assert.Equal("Confirmada", (await decisao.Content.ReadFromJsonAsync<AditivoResponse>())!.SituacaoReserva);
        Assert.Equal(HttpStatusCode.Conflict, (await publico.PostAsJsonAsync(publica + "/decisao",
            new DecidirAditivoRequest("529.982.247-25", codigo, false, "Mudei de ideia"))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await interno.PostAsJsonAsync(rota + "/consumos",
            new RegistrarConsumoRequest([new ConsumoItemRequest(item.Id, 1m)]))).StatusCode);

        var reparoAprovado = await interno.PostAsJsonAsync(rota + "/aditivos",
            new CriarAditivoRequest(TipoAditivo.ReparoAdicional, "Reparo sem peça",
                extra.Id, null, 0m, true));
        Assert.Equal(HttpStatusCode.Created, reparoAprovado.StatusCode);
        var reparoAprovadoId = (await reparoAprovado.Content.ReadFromJsonAsync<AditivoResponse>())!.Id;
        var rotaAprovacaoReparo = $"/api/cliente/ordens-servico/{numero}/aditivos/{reparoAprovadoId}/decisao";
        var aprovacaoReparo = await publico.PostAsJsonAsync(rotaAprovacaoReparo,
            new DecidirAditivoRequest("529.982.247-25", codigo, true, null));
        Assert.Equal(HttpStatusCode.OK, aprovacaoReparo.StatusCode);
        Assert.Equal("Confirmada", (await aprovacaoReparo.Content.ReadFromJsonAsync<AditivoResponse>())!.SituacaoReserva);
        Assert.Equal(HttpStatusCode.OK,
            (await interno.PostAsync(rota + $"/servicos/{extra.Id}/inicio", null)).StatusCode);

        var semEstoque = await interno.PostAsJsonAsync(rota + "/aditivos",
            new CriarAditivoRequest(TipoAditivo.ConsumoExcedente, "Terceiro filtro",
                null, item.Id, 1m, true));
        Assert.Equal(HttpStatusCode.Created, semEstoque.StatusCode);
        var semEstoqueId = (await semEstoque.Content.ReadFromJsonAsync<AditivoResponse>())!.Id;
        var rotaSemEstoque = $"/api/cliente/ordens-servico/{numero}/aditivos/{semEstoqueId}/decisao";
        var aprovacaoSemEstoque = await publico.PostAsJsonAsync(rotaSemEstoque,
            new DecidirAditivoRequest("529.982.247-25", codigo, true, null));
        Assert.Equal(HttpStatusCode.OK, aprovacaoSemEstoque.StatusCode);
        Assert.Equal("Recusada", (await aprovacaoSemEstoque.Content.ReadFromJsonAsync<AditivoResponse>())!.SituacaoReserva);
        Assert.Equal(HttpStatusCode.BadRequest, (await interno.PostAsJsonAsync(rota + "/consumos",
            new RegistrarConsumoRequest([new ConsumoItemRequest(item.Id, 1m)]))).StatusCode);
        await AutenticarAsync(interno, "estoquista@aditivos.test", "EstoquistaDeTeste123!");
        await ReporAsync(interno, item.Id, 1m);
        var novaReserva = await interno.PostAsync(rota + $"/aditivos/{semEstoqueId}/reservas/tentativa", null);
        Assert.Equal(HttpStatusCode.OK, novaReserva.StatusCode);
        Assert.Equal("Confirmada", (await novaReserva.Content.ReadFromJsonAsync<AditivoResponse>())!.SituacaoReserva);
        await AutenticarAsync(interno, "mecanico@aditivos.test", "MecanicoDeTeste123!");
        Assert.Equal(HttpStatusCode.OK, (await interno.PostAsJsonAsync(rota + "/consumos",
            new RegistrarConsumoRequest([new ConsumoItemRequest(item.Id, 1m)]))).StatusCode);

        var reparo = await interno.PostAsJsonAsync(rota + "/aditivos",
            new CriarAditivoRequest(TipoAditivo.ReparoAdicional, "Reparo inseguro sem aprovação",
                extraRecusado.Id, null, 0m, false));
        Assert.Equal(HttpStatusCode.Created, reparo.StatusCode);
        var reparoId = (await reparo.Content.ReadFromJsonAsync<AditivoResponse>())!.Id;
        var rotaReparo = $"/api/cliente/ordens-servico/{numero}/aditivos/{reparoId}/decisao";
        Assert.Equal(HttpStatusCode.OK, (await publico.PostAsJsonAsync(rotaReparo,
            new DecidirAditivoRequest("529.982.247-25", codigo, false, "Não autorizo"))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await interno.PostAsync(rota + $"/servicos/{servico.Id}/conclusao", null)).StatusCode);
        Assert.Equal("EmExecucao", (await interno.GetFromJsonAsync<OrdemServicoResponse>(rota))!.Status);
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
