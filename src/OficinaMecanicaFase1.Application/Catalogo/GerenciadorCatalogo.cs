using OficinaMecanicaFase1.Application.Cadastros;
using OficinaMecanicaFase1.Domain.Catalogo;
using OficinaMecanicaFase1.Domain.Estoque;

namespace OficinaMecanicaFase1.Application.Catalogo;

public sealed class GerenciadorCatalogo(IRepositorioCatalogo repositorio, TimeProvider tempo)
{
    public Task<IReadOnlyList<ServicoCatalogo>> ListarServicosAsync(bool somenteAtivos, CancellationToken cancelamento) =>
        repositorio.ListarServicosAsync(somenteAtivos, cancelamento);

    public Task<ServicoCatalogo?> ObterServicoAsync(Guid id, CancellationToken cancelamento) =>
        repositorio.ObterServicoAsync(id, cancelamento);

    public Task<ServicoCatalogo?> ObterServicoAtivoAsync(Guid id, CancellationToken cancelamento) =>
        repositorio.ObterServicoAtivoAsync(id, cancelamento);

    public async Task<ResultadoCatalogo<ServicoCatalogo>> CriarServicoAsync(
        string nome, string? descricao, decimal precoBase, CancellationToken cancelamento)
    {
        ServicoCatalogo servico;
        try { servico = new ServicoCatalogo(nome, descricao, precoBase); }
        catch (ArgumentException erro) { return Invalido<ServicoCatalogo>(erro); }
        repositorio.Adicionar(servico);
        return await SalvarAsync(servico, cancelamento);
    }

    public async Task<ResultadoCatalogo<ServicoCatalogo>> AtualizarServicoAsync(
        Guid id, string nome, string? descricao, decimal precoBase, CancellationToken cancelamento)
    {
        var servico = await repositorio.ObterServicoAsync(id, cancelamento);
        if (servico is null) return NaoEncontrado<ServicoCatalogo>();
        try { servico.Atualizar(nome, descricao, precoBase); }
        catch (ArgumentException erro) { return Invalido<ServicoCatalogo>(erro); }
        return await SalvarAsync(servico, cancelamento);
    }

    public async Task<ResultadoCatalogo<ServicoCatalogo>> DefinirServicoAtivoAsync(
        Guid id, bool ativo, CancellationToken cancelamento)
    {
        var servico = await repositorio.ObterServicoAsync(id, cancelamento);
        if (servico is null) return NaoEncontrado<ServicoCatalogo>();
        servico.DefinirAtivo(ativo);
        return await SalvarAsync(servico, cancelamento);
    }

    public Task<IReadOnlyList<ItemEstoque>> ListarItensAsync(bool somenteAtivos, CancellationToken cancelamento) =>
        repositorio.ListarItensAsync(somenteAtivos, cancelamento);

    public Task<ItemEstoque?> ObterItemAsync(Guid id, CancellationToken cancelamento) =>
        repositorio.ObterItemAsync(id, cancelamento);

    public Task<ItemEstoque?> ObterItemAtivoAsync(Guid id, CancellationToken cancelamento) =>
        repositorio.ObterItemAtivoAsync(id, cancelamento);

    public async Task<ResultadoCatalogo<ItemEstoque>> CriarItemAsync(
        string nome, string? descricao, string tipo, string unidadeMedida, decimal precoUnitario, CancellationToken cancelamento)
    {
        if (!Enum.TryParse<TipoItemEstoque>(tipo, true, out var tipoConvertido) ||
            !Enum.IsDefined(tipoConvertido) || string.IsNullOrWhiteSpace(tipo) || char.IsDigit(tipo.Trim()[0]))
            return ResultadoCatalogo<ItemEstoque>.Falha(ErroCatalogo.Invalido, "Use Peca ou Insumo como tipo.", "tipo");

        ItemEstoque item;
        try { item = new ItemEstoque(nome, descricao, tipoConvertido, unidadeMedida, precoUnitario); }
        catch (ArgumentException erro) { return Invalido<ItemEstoque>(erro); }
        repositorio.Adicionar(item);
        return await SalvarAsync(item, cancelamento);
    }

    public async Task<ResultadoCatalogo<ItemEstoque>> AtualizarItemAsync(
        Guid id, string nome, string? descricao, decimal precoUnitario, CancellationToken cancelamento)
    {
        var item = await repositorio.ObterItemAsync(id, cancelamento);
        if (item is null) return NaoEncontrado<ItemEstoque>();
        try { item.Atualizar(nome, descricao, precoUnitario); }
        catch (ArgumentException erro) { return Invalido<ItemEstoque>(erro); }
        return await SalvarAsync(item, cancelamento);
    }

    public async Task<ResultadoCatalogo<ItemEstoque>> DefinirItemAtivoAsync(
        Guid id, bool ativo, CancellationToken cancelamento)
    {
        var item = await repositorio.ObterItemAsync(id, cancelamento);
        if (item is null) return NaoEncontrado<ItemEstoque>();
        item.DefinirAtivo(ativo);
        return await SalvarAsync(item, cancelamento);
    }

    public async Task<ResultadoCatalogo<EntradaEstoque>> RegistrarEntradaAsync(
        Guid itemId, decimal quantidade, Guid usuarioId, CancellationToken cancelamento)
    {
        var item = await repositorio.ObterItemAsync(itemId, cancelamento);
        if (item is null) return NaoEncontrado<EntradaEstoque>();
        EntradaEstoque entrada;
        try { entrada = item.RegistrarEntrada(quantidade, tempo.GetUtcNow(), usuarioId); }
        catch (ArgumentException erro) { return Invalido<EntradaEstoque>(erro); }
        repositorio.Adicionar(entrada);
        return await SalvarAsync(entrada, cancelamento);
    }

    public Task<IReadOnlyList<EntradaEstoque>> ListarEntradasAsync(Guid itemId, CancellationToken cancelamento) =>
        repositorio.ListarEntradasAsync(itemId, cancelamento);

    private async Task<ResultadoCatalogo<T>> SalvarAsync<T>(T valor, CancellationToken cancelamento)
    {
        try
        {
            await repositorio.SalvarAsync(cancelamento);
            return ResultadoCatalogo<T>.Sucesso(valor);
        }
        catch (ConflitoPersistenciaException)
        {
            return ResultadoCatalogo<T>.Falha(ErroCatalogo.Conflito,
                "O cadastro ou saldo foi alterado por outra operação. Consulte o valor atual e tente novamente.");
        }
    }

    private static ResultadoCatalogo<T> Invalido<T>(ArgumentException erro) =>
        ResultadoCatalogo<T>.Falha(ErroCatalogo.Invalido, erro.Message, erro.ParamName);

    private static ResultadoCatalogo<T> NaoEncontrado<T>() =>
        ResultadoCatalogo<T>.Falha(ErroCatalogo.NaoEncontrado, "Registro não encontrado.");
}
