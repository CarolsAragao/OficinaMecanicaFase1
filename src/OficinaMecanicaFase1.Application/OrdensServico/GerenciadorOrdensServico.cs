using OficinaMecanicaFase1.Application.Cadastros;
using OficinaMecanicaFase1.Domain.OrdensServico;
using OficinaMecanicaFase1.Domain.Orcamentos;

namespace OficinaMecanicaFase1.Application.OrdensServico;

public sealed class GerenciadorOrdensServico(
    IRepositorioOrdensServico repositorio, TimeProvider tempo)
{
    public async Task<ResultadoOrdemServico<OrdemServico>> CriarAsync(
        Guid clienteId, Guid veiculoId, IReadOnlyCollection<Guid> servicosIds,
        IReadOnlyCollection<(Guid ItemEstoqueId, decimal Quantidade)> itens,
        CancellationToken cancelamento)
    {
        if (clienteId == Guid.Empty || veiculoId == Guid.Empty)
            return Invalido<OrdemServico>("Informe cliente e veículo.", "clienteId");
        if (!await repositorio.VeiculoPertenceAoClienteAsync(clienteId, veiculoId, cancelamento))
            return Invalido<OrdemServico>("O veículo informado não pertence ao cliente selecionado.", "veiculoId");
        if (servicosIds.Count == 0 || servicosIds.Any(id => id == Guid.Empty) || servicosIds.Distinct().Count() != servicosIds.Count)
            return Invalido<OrdemServico>("Informe ao menos um serviço solicitado, sem repetições.", "servicosIds");

        var servicos = await repositorio.ObterServicosAtivosAsync(servicosIds, cancelamento);
        if (servicos.Count != servicosIds.Count)
            return Invalido<OrdemServico>("Um ou mais serviços não existem ou estão inativos.", "servicosIds");

        if (itens.Any(item => item.ItemEstoqueId == Guid.Empty) ||
            itens.Select(item => item.ItemEstoqueId).Distinct().Count() != itens.Count)
            return Invalido<OrdemServico>("Os itens devem ser válidos e não podem se repetir.", "itensNecessarios");
        var itensCatalogo = await repositorio.ObterItensAtivosAsync(itens.Select(item => item.ItemEstoqueId).ToArray(), cancelamento);
        if (itensCatalogo.Count != itens.Count)
            return Invalido<OrdemServico>("Um ou mais itens não existem ou estão inativos.", "itensNecessarios");

        var itensComDisponibilidade = new List<(Guid ItemEstoqueId, decimal Quantidade, decimal SaldoDisponivel)>();
        foreach (var requisicao in itens)
        {
            var item = itensCatalogo[requisicao.ItemEstoqueId];
            if (!item.QuantidadeValida(requisicao.Quantidade))
                return Invalido<OrdemServico>(item.Tipo == Domain.Estoque.TipoItemEstoque.Peca
                    ? "Peças exigem quantidade inteira e positiva."
                    : "Insumos exigem quantidade positiva com até três casas decimais.", "quantidade");
            itensComDisponibilidade.Add((item.Id, requisicao.Quantidade, item.SaldoDisponivel));
        }

        OrdemServico ordem;
        try
        {
            ordem = OrdemServico.Criar(clienteId, veiculoId, servicosIds, itensComDisponibilidade, tempo.GetUtcNow());
        }
        catch (ArgumentException erro) { return Invalido<OrdemServico>(erro.Message, erro.ParamName); }
        repositorio.Adicionar(ordem);
        return await SalvarAsync(ordem, cancelamento);
    }

    public Task<OrdemServico?> ObterAsync(long numero, CancellationToken cancelamento) =>
        repositorio.ObterAsync(numero, cancelamento);

    public async Task<ResultadoOrdemServico<OrdemServico>> AdicionarServicoAsync(
        long numero, Guid servicoId, CancellationToken cancelamento)
    {
        var ordem = await repositorio.ObterAsync(numero, cancelamento);
        if (ordem is null) return NaoEncontrado<OrdemServico>();
        var servicos = await repositorio.ObterServicosAtivosAsync([servicoId], cancelamento);
        if (!servicos.ContainsKey(servicoId)) return Invalido<OrdemServico>("O serviço não existe ou está inativo.", "servicoId");
        try { repositorio.Adicionar(ordem.AdicionarServicoSolicitado(servicoId)); }
        catch (ArgumentException erro) { return Invalido<OrdemServico>(erro.Message, erro.ParamName); }
        catch (InvalidOperationException erro) { return Invalido<OrdemServico>(erro.Message, "status"); }
        return await SalvarAsync(ordem, cancelamento);
    }

    public async Task<ResultadoOrdemServico<OrdemServico>> AdicionarItemAsync(
        long numero, Guid itemId, decimal quantidade, CancellationToken cancelamento)
    {
        var ordem = await repositorio.ObterAsync(numero, cancelamento);
        if (ordem is null) return NaoEncontrado<OrdemServico>();
        var itens = await repositorio.ObterItensAtivosAsync([itemId], cancelamento);
        if (!itens.TryGetValue(itemId, out var item)) return Invalido<OrdemServico>("O item não existe ou está inativo.", "itemEstoqueId");
        if (!item.QuantidadeValida(quantidade))
            return Invalido<OrdemServico>(item.Tipo == Domain.Estoque.TipoItemEstoque.Peca
                ? "Peças exigem quantidade inteira e positiva."
                : "Insumos exigem quantidade positiva com até três casas decimais.", "quantidade");
        try { repositorio.Adicionar(ordem.AdicionarItemNecessario(item.Id, quantidade, item.SaldoDisponivel)); }
        catch (ArgumentException erro) { return Invalido<OrdemServico>(erro.Message, erro.ParamName); }
        catch (InvalidOperationException erro) { return Invalido<OrdemServico>(erro.Message, "status"); }
        return await SalvarAsync(ordem, cancelamento);
    }

    public async Task<ResultadoOrdemServico<OrdemServico>> IniciarDiagnosticoAsync(
        long numero, Guid mecanicoId, CancellationToken cancelamento)
    {
        var ordem = await repositorio.ObterAsync(numero, cancelamento);
        if (ordem is null) return NaoEncontrado<OrdemServico>();
        try { repositorio.Adicionar(ordem.IniciarDiagnostico(mecanicoId, tempo.GetUtcNow())); }
        catch (ArgumentException erro) { return Invalido<OrdemServico>(erro.Message, erro.ParamName); }
        catch (InvalidOperationException erro) { return Invalido<OrdemServico>(erro.Message, "status"); }
        return await SalvarAsync(ordem, cancelamento);
    }

    public async Task<ResultadoOrdemServico<OrdemServico>> RegistrarDiagnosticoAsync(
        long numero, Guid mecanicoId, string descricao, CancellationToken cancelamento)
    {
        var ordem = await repositorio.ObterAsync(numero, cancelamento);
        if (ordem is null) return NaoEncontrado<OrdemServico>();
        try { repositorio.Adicionar(ordem.RegistrarDiagnostico(mecanicoId, descricao, tempo.GetUtcNow())); }
        catch (ArgumentException erro) { return Invalido<OrdemServico>(erro.Message, erro.ParamName); }
        catch (InvalidOperationException erro) { return Invalido<OrdemServico>(erro.Message, "status"); }
        return await SalvarAsync(ordem, cancelamento);
    }

    public async Task<ResultadoOrdemServico<Orcamento>> GerarOrcamentoAsync(
        long numero, CancellationToken cancelamento)
    {
        var ordem = await repositorio.ObterAsync(numero, cancelamento);
        if (ordem is null) return NaoEncontrado<Orcamento>();
        if (ordem.Status != StatusOrdemServico.EmDiagnostico || ordem.DiagnosticoRegistradoEmUtc is null)
            return Invalido<Orcamento>("Registre o diagnóstico antes de gerar o orçamento.", "status");
        if (await repositorio.ObterOrcamentoAsync(ordem.Id, cancelamento) is not null)
            return Invalido<Orcamento>("Esta OS já possui orçamento.", "status");

        var servicosIds = ordem.ServicosSolicitados.Select(servico => servico.ServicoCatalogoId).ToArray();
        var itensIds = ordem.ItensNecessarios.Select(item => item.ItemEstoqueId).ToArray();
        var servicos = await repositorio.ObterServicosAsync(servicosIds, cancelamento);
        var itens = await repositorio.ObterItensAsync(itensIds, cancelamento);
        if (servicos.Count != servicosIds.Length || itens.Count != itensIds.Length)
            return Invalido<Orcamento>("Um serviço ou item da OS não está mais disponível.", "ordemServico");

        Orcamento orcamento;
        try
        {
            var agora = tempo.GetUtcNow();
            orcamento = Orcamento.Gerar(ordem.Id, agora,
                servicosIds.Select(id => (id, servicos[id].Nome, servicos[id].PrecoBase)),
                ordem.ItensNecessarios.Select(item => (item.ItemEstoqueId, itens[item.ItemEstoqueId].Nome,
                    itens[item.ItemEstoqueId].UnidadeMedida, item.Quantidade, itens[item.ItemEstoqueId].PrecoUnitario)));
            repositorio.Adicionar(ordem.DisponibilizarOrcamento(agora));
        }
        catch (ArgumentException erro) { return Invalido<Orcamento>(erro.Message, erro.ParamName); }
        catch (OverflowException) { return Invalido<Orcamento>("O total do orçamento excede o limite permitido.", "valor"); }
        catch (InvalidOperationException erro) { return Invalido<Orcamento>(erro.Message, "status"); }
        repositorio.Adicionar(orcamento);
        return await SalvarAsync(orcamento, cancelamento);
    }

    public async Task<Orcamento?> ObterOrcamentoAsync(long numero, CancellationToken cancelamento)
    {
        var ordem = await repositorio.ObterAsync(numero, cancelamento);
        return ordem is null ? null : await repositorio.ObterOrcamentoAsync(ordem.Id, cancelamento);
    }

    private async Task<ResultadoOrdemServico<T>> SalvarAsync<T>(T valor, CancellationToken cancelamento)
    {
        try
        {
            await repositorio.SalvarAsync(cancelamento);
            return ResultadoOrdemServico<T>.Sucesso(valor);
        }
        catch (ConflitoPersistenciaException)
        {
            return ResultadoOrdemServico<T>.Falha(ErroOrdemServico.Conflito,
                "A ordem foi alterada ou um cadastro relacionado deixou de estar disponível. Consulte e tente novamente.");
        }
    }

    private static ResultadoOrdemServico<T> Invalido<T>(string mensagem, string? campo = null) =>
        ResultadoOrdemServico<T>.Falha(ErroOrdemServico.Invalido, mensagem, campo);

    private static ResultadoOrdemServico<T> NaoEncontrado<T>() =>
        ResultadoOrdemServico<T>.Falha(ErroOrdemServico.NaoEncontrado, "Ordem de serviço não encontrada.");
}
