using OficinaMecanicaFase1.Domain.Cadastros;
using OficinaMecanicaFase1.Domain.Estoque;
using OficinaMecanicaFase1.Domain.Orcamentos;
using OficinaMecanicaFase1.Domain.OrdensServico;
using Xunit;

namespace OficinaMecanicaFase1.Infrastructure.Tests;

public sealed class RegrasDominioTests
{
    private static readonly DateTimeOffset Agora = new(2026, 10, 27, 12, 0, 0, TimeSpan.FromHours(-3));

    [Fact]
    public void CadastrosNormalizamIdentificadoresEPreservamProprietario()
    {
        var cliente = new Cliente(" Ana ", "529.982.247-25");
        var veiculo = new Veiculo(cliente.Id, "abc-1234", " Fiat ", " Uno ", 2020);
        var proprietario = veiculo.ClienteId;
        Assert.Equal("52998224725", cliente.Documento);
        Assert.Equal("ABC1234", veiculo.Placa);
        cliente.Atualizar(" Ana Maria ", "04.252.011/0001-10");
        veiculo.Atualizar("abc1d23", " Ford ", " Ka ", 2021);
        Assert.Equal("Ana Maria", cliente.Nome);
        Assert.Equal("04252011000110", cliente.Documento);
        Assert.Equal(proprietario, veiculo.ClienteId);
        Assert.Equal("ABC1D23", veiculo.Placa);
        Assert.Throws<ArgumentException>(() => cliente.Atualizar(" ", cliente.Documento));
        Assert.Throws<ArgumentException>(() => cliente.Atualizar("Ana", "00000000000"));
        Assert.Throws<ArgumentException>(() => new Veiculo(Guid.Empty, "ABC1234", "Fiat", "Uno", 2020));
        Assert.Throws<ArgumentException>(() => veiculo.Atualizar("INVALIDA", "Fiat", "Uno", 2020));
        Assert.Throws<ArgumentException>(() => veiculo.Atualizar("ABC1234", " ", "Uno", 2020));
        Assert.Throws<ArgumentException>(() => veiculo.Atualizar("ABC1234", "Fiat", " ", 2020));
        Assert.Throws<ArgumentException>(() => veiculo.Atualizar("ABC1234", "Fiat", "Uno", 1800));
    }

    [Fact]
    public void EstoqueImpedeReservaEConsumoAcimaDoSaldo()
    {
        var item = new ItemEstoque(" Óleo ", null, TipoItemEstoque.Insumo, " L ", 12.50m);
        item.RegistrarEntrada(2.5m, Agora, Guid.NewGuid());
        item.Reservar(2m);
        Assert.Equal(0.5m, item.SaldoDisponivel);
        Assert.Throws<InvalidOperationException>(() => item.Reservar(0.501m));
        Assert.Throws<InvalidOperationException>(() => item.RegistrarConsumoReservado(2.001m));
        item.RegistrarConsumoReservado(1.25m);
        Assert.Equal(1.25m, item.SaldoFisico);
        Assert.Equal(0.75m, item.SaldoReservado);
        item.LiberarReserva(0.75m);
        Assert.Equal(1.25m, item.SaldoDisponivel);
        Assert.Throws<InvalidOperationException>(() => item.LiberarReserva(0.001m));
        Assert.Throws<ArgumentException>(() => item.RegistrarEntrada(1m, Agora, Guid.Empty));
        item.DefinirAtivo(false);
        Assert.False(item.Ativo);
    }

    [Fact]
    public void ReservaDaOsControlaConsumoELiberacao()
    {
        var reserva = ReservaItemOrdemServico.Criar(Guid.NewGuid(), Guid.NewGuid(), 2m, Agora);
        reserva.RegistrarConsumo(0.5m);
        reserva.Acrescentar(1m);
        Assert.Equal(2.5m, reserva.QuantidadeRestante);
        Assert.Throws<InvalidOperationException>(() => reserva.RegistrarConsumo(2.501m));
        reserva.Liberar(Agora.AddHours(1));
        Assert.NotNull(reserva.LiberadaEmUtc);
        Assert.Throws<InvalidOperationException>(() => reserva.RegistrarConsumo(0.1m));
        Assert.Throws<InvalidOperationException>(() => reserva.Acrescentar(1m));
        Assert.Throws<InvalidOperationException>(() => reserva.Liberar(Agora.AddHours(2)));
        Assert.Throws<ArgumentException>(() => ReservaItemOrdemServico.Criar(Guid.Empty, Guid.NewGuid(), 1m, Agora));
    }

    [Fact]
    public void AvisoRecusadoExigeResolucaoUnica()
    {
        var aviso = new AvisoReservaRecusada(Guid.NewGuid(), Guid.NewGuid(), 2m, 0.5m, Agora);
        Assert.Equal(2m, aviso.QuantidadeSolicitada);
        aviso.Resolver(Agora.AddMinutes(5));
        Assert.NotNull(aviso.ResolvidoEmUtc);
        Assert.Throws<InvalidOperationException>(() => aviso.Resolver(Agora.AddMinutes(6)));
        Assert.Throws<ArgumentException>(() => new AvisoReservaRecusada(Guid.NewGuid(), Guid.NewGuid(), 1m, -1m, Agora));
    }

    [Fact]
    public void AditivoRejeitadoNaoReservaEAprovadoDependeDeEstoque()
    {
        var ordemId = Guid.NewGuid();
        var mecanicoId = Guid.NewGuid();
        var servicoId = Guid.NewGuid();
        var rejeitado = Aditivo.Criar(ordemId, TipoAditivo.ReparoAdicional, " Pastilhas ", servicoId,
            "Freio", null, null, 0m, 100m, 0m, false, mecanicoId, Agora);
        Assert.Throws<ArgumentException>(() => rejeitado.Decidir(false, " ", Agora));
        rejeitado.Decidir(false, " Sem autorização ", Agora.AddMinutes(1));
        Assert.Equal(SituacaoAditivo.Rejeitado, rejeitado.Situacao);
        Assert.Equal("Sem autorização", rejeitado.MotivoRejeicao);
        Assert.Throws<InvalidOperationException>(() => rejeitado.RegistrarReserva(true));
        Assert.Throws<InvalidOperationException>(() => rejeitado.Decidir(true, null, Agora));
        var aprovado = Aditivo.Criar(ordemId, TipoAditivo.ConsumoExcedente, "Mais óleo", null, null,
            Guid.NewGuid(), "Óleo", 0.25m, 0m, 12.50m, true, mecanicoId, Agora);
        Assert.Equal(3.13m, aprovado.Total);
        aprovado.Decidir(true, null, Agora.AddMinutes(1));
        aprovado.RegistrarReserva(false);
        Assert.Equal(SituacaoReservaAditivo.Recusada, aprovado.SituacaoReserva);
        aprovado.RegistrarReserva(true);
        Assert.Equal(SituacaoReservaAditivo.Confirmada, aprovado.SituacaoReserva);
        Assert.Throws<InvalidOperationException>(() => aprovado.RegistrarReserva(true));
        Assert.Throws<ArgumentException>(() => Aditivo.Criar(ordemId, TipoAditivo.ConsumoExcedente,
            "Incorreto", servicoId, "Freio", Guid.NewGuid(), "Óleo", 1m, 1m, 1m, true, mecanicoId, Agora));
    }

    [Fact]
    public void ReemissaoDoCodigoInvalidaOAnteriorEExigeMotivo()
    {
        var ordem = OrdemComOrcamento();
        var atendente = Guid.NewGuid();
        var primeiro = Enumerable.Repeat((byte)1, 32).ToArray();
        var segundo = Enumerable.Repeat((byte)2, 32).ToArray();
        Assert.False(ordem.ValidarCodigoAcesso(primeiro));
        var emissao = ordem.EmitirCodigoAcesso(primeiro, atendente, null, Agora.AddMinutes(3));
        Assert.Equal(1, emissao.NumeroEmissao);
        Assert.True(ordem.ValidarCodigoAcesso(primeiro));
        Assert.Throws<ArgumentException>(() => ordem.EmitirCodigoAcesso(segundo, atendente, null, Agora.AddMinutes(4)));
        ordem.EmitirCodigoAcesso(segundo, atendente, " Código perdido ", Agora.AddMinutes(4));
        Assert.False(ordem.ValidarCodigoAcesso(primeiro));
        Assert.True(ordem.ValidarCodigoAcesso(segundo));
        Assert.Equal(2, ordem.NumeroEmissoesCodigoAcesso);
        Assert.Throws<ArgumentException>(() => ordem.EmitirCodigoAcesso([1], atendente, "Outro", Agora.AddMinutes(5)));
    }

    [Fact]
    public void OrcamentoRejeitadoNaoPodeReceberSegundaDecisao()
    {
        var orcamento = Orcamento.Gerar(Guid.NewGuid(), Agora,
            [(Guid.NewGuid(), "Alinhamento", 100m)], [(Guid.NewGuid(), "Óleo", "L", 0.25m, 12.50m)]);
        Assert.Equal(103.13m, orcamento.Total);
        Assert.Throws<ArgumentException>(() => orcamento.Rejeitar(" ", Agora));
        orcamento.Rejeitar("Cliente recusou", Agora.AddMinutes(1));
        Assert.Equal(DecisaoOrcamento.Rejeitado, orcamento.Decisao);
        Assert.Throws<InvalidOperationException>(() => orcamento.Aprovar(Agora.AddMinutes(2)));
        Assert.Throws<ArgumentException>(() => Orcamento.Gerar(Guid.Empty, Agora, [], []));
        Assert.Throws<ArgumentException>(() => Orcamento.Gerar(Guid.NewGuid(), Agora, [], []));
    }

    [Fact]
    public void ConsumoRegistraResponsavelEQuantidadeValida()
    {
        var ordemId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var mecanicoId = Guid.NewGuid();
        var consumo = ConsumoItemOrdemServico.Criar(ordemId, itemId, mecanicoId, 0.25m, Agora);
        Assert.Equal(mecanicoId, consumo.MecanicoId);
        Assert.Equal(0.25m, consumo.Quantidade);
        Assert.Equal(TimeSpan.Zero, consumo.RegistradoEmUtc.Offset);
        Assert.Throws<ArgumentException>(() => ConsumoItemOrdemServico.Criar(ordemId, itemId,
            mecanicoId, 0.0001m, Agora));
    }

    [Fact]
    public void CatalogoNaoAceitaPrecoInvalidoEPreservaDefinicaoDaOs()
    {
        var catalogo = new OficinaMecanicaFase1.Domain.Catalogo.ServicoCatalogo(" Revisão ", " Completa ", 120m);
        var ordem = OrdemServico.Criar(Guid.NewGuid(), Guid.NewGuid(), [catalogo.Id], [], Agora);
        Assert.Equal("Revisão", catalogo.Nome);
        Assert.Equal("Completa", catalogo.Descricao);
        catalogo.Atualizar("Revisão", null, 180m);
        catalogo.DefinirAtivo(false);
        Assert.False(catalogo.Ativo);
        Assert.Equal(catalogo.Id, ordem.ServicosSolicitados.Single().ServicoCatalogoId);
        Assert.Throws<ArgumentException>(() => catalogo.Atualizar(" ", null, 120m));
        Assert.Throws<ArgumentException>(() => catalogo.Atualizar("Revisão", null, -1m));
        Assert.Throws<ArgumentException>(() => catalogo.Atualizar("Revisão", null, 1.001m));
        Assert.Throws<ArgumentException>(() => catalogo.Atualizar("Revisão", new string('x', 501), 120m));
    }

    [Fact]
    public void EscopoDaOsPodeSerAmpliadoAntesDoOrcamentoEAditivoDuranteExecucao()
    {
        var primeiro = Guid.NewGuid();
        var segundo = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var ordem = OrdemServico.Criar(Guid.NewGuid(), Guid.NewGuid(), [primeiro], [], Agora);
        ordem.AdicionarServicoSolicitado(segundo);
        ordem.AdicionarItemNecessario(itemId, 1m, 2m);
        Assert.True(ordem.AutorizaItem(itemId));
        Assert.Throws<ArgumentException>(() => ordem.AdicionarServicoSolicitado(segundo));
        Assert.Throws<ArgumentException>(() => ordem.AdicionarItemNecessario(itemId, 1m, 2m));
        var mecanico = Guid.NewGuid();
        ordem.IniciarDiagnostico(mecanico, Agora.AddMinutes(1));
        ordem.RegistrarDiagnostico(mecanico, "Serviço necessário", Agora.AddMinutes(2));
        ordem.DisponibilizarOrcamento(Agora.AddMinutes(3));
        Assert.Throws<InvalidOperationException>(() => ordem.AdicionarServicoSolicitado(Guid.NewGuid()));
        ordem.RegistrarAprovacaoOrcamento(Agora.AddMinutes(4));
        ordem.ConfirmarReserva(Agora.AddMinutes(5));
        ordem.IniciarExecucao(mecanico, Agora.AddMinutes(6));
        var adicional = Guid.NewGuid();
        var novoItem = Guid.NewGuid();
        var autorizados = ordem.AutorizarAditivo(adicional, novoItem, 0.5m);
        Assert.Equal(adicional, autorizados.Servico!.ServicoCatalogoId);
        Assert.Equal(novoItem, autorizados.Item!.ItemEstoqueId);
        Assert.True(ordem.AutorizaItem(novoItem));
        ordem.RegistrarMarcoAditivo("Reparo adicional autorizado", Agora.AddMinutes(7));
        Assert.Contains(ordem.Marcos, marco => marco.Descricao == "Reparo adicional autorizado");
        var repetidos = ordem.AutorizarAditivo(adicional, novoItem, 0.5m);
        Assert.Null(repetidos.Servico);
        Assert.Null(repetidos.Item);
    }
    private static OrdemServico OrdemComOrcamento()
    {
        var ordem = OrdemServico.Criar(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [], Agora);
        var mecanico = Guid.NewGuid();
        ordem.IniciarDiagnostico(mecanico, Agora.AddMinutes(1));
        ordem.RegistrarDiagnostico(mecanico, "Inspeção concluída", Agora.AddMinutes(2));
        ordem.DisponibilizarOrcamento(Agora.AddMinutes(3));
        return ordem;
    }
}
