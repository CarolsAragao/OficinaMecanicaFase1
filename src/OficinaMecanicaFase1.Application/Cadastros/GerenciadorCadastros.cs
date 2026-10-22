using OficinaMecanicaFase1.Domain.Cadastros;

namespace OficinaMecanicaFase1.Application.Cadastros;

public sealed class GerenciadorCadastros(IRepositorioCadastros repositorio)
{
    public async Task<ResultadoCadastro<IReadOnlyList<Cliente>>> ListarClientesAsync(
        string? documento, CancellationToken cancelamento)
    {
        string? normalizado = null;
        if (documento is not null)
        {
            normalizado = DocumentoCliente.Normalizar(documento);
            if (normalizado is null)
                return ResultadoCadastro<IReadOnlyList<Cliente>>.Falha(ErroCadastro.Invalido, "Informe um CPF ou CNPJ válido.", "documento");
        }
        return ResultadoCadastro<IReadOnlyList<Cliente>>.Sucesso(
            await repositorio.ListarClientesAsync(normalizado, cancelamento));
    }

    public Task<Cliente?> ObterClienteAsync(Guid id, CancellationToken cancelamento) =>
        repositorio.ObterClienteAsync(id, cancelamento);

    public async Task<ResultadoCadastro<Cliente>> CriarClienteAsync(
        string nome, string documento, CancellationToken cancelamento)
    {
        Cliente cliente;
        try { cliente = new Cliente(nome, documento); }
        catch (ArgumentException erro) { return Invalido<Cliente>(erro); }

        if (await repositorio.ExisteDocumentoAsync(cliente.Documento, null, cancelamento))
            return ResultadoCadastro<Cliente>.Falha(ErroCadastro.Conflito, "Já existe um cliente com esse documento.");
        repositorio.Adicionar(cliente);
        return await SalvarAsync(cliente, "Já existe um cliente com esse documento.", cancelamento);
    }

    public async Task<ResultadoCadastro<Cliente>> AtualizarClienteAsync(
        Guid id, string nome, string documento, CancellationToken cancelamento)
    {
        var cliente = await repositorio.ObterClienteAsync(id, cancelamento);
        if (cliente is null) return NaoEncontrado<Cliente>();
        try { cliente.Atualizar(nome, documento); }
        catch (ArgumentException erro) { return Invalido<Cliente>(erro); }

        if (await repositorio.ExisteDocumentoAsync(cliente.Documento, id, cancelamento))
            return ResultadoCadastro<Cliente>.Falha(ErroCadastro.Conflito, "Já existe um cliente com esse documento.");
        return await SalvarAsync(cliente, "Já existe um cliente com esse documento.", cancelamento);
    }

    public async Task<ResultadoCadastro<bool>> RemoverClienteAsync(Guid id, CancellationToken cancelamento)
    {
        var cliente = await repositorio.ObterClienteAsync(id, cancelamento);
        if (cliente is null) return NaoEncontrado<bool>();
        if (await repositorio.ClientePossuiVeiculosAsync(id, cancelamento))
            return ResultadoCadastro<bool>.Falha(ErroCadastro.Conflito, "Remova os veículos do cliente antes de excluir o cadastro.");
        repositorio.Remover(cliente);
        return await SalvarAsync(true, "O cliente está em uso e não pode ser excluído.", cancelamento);
    }

    public Task<IReadOnlyList<Veiculo>> ListarVeiculosAsync(Guid? clienteId, CancellationToken cancelamento) =>
        repositorio.ListarVeiculosAsync(clienteId, cancelamento);

    public Task<Veiculo?> ObterVeiculoAsync(Guid id, CancellationToken cancelamento) =>
        repositorio.ObterVeiculoAsync(id, cancelamento);

    public async Task<bool> VeiculoPertenceAoClienteAsync(Guid veiculoId, Guid clienteId, CancellationToken cancelamento) =>
        (await repositorio.ObterVeiculoAsync(veiculoId, cancelamento))?.ClienteId == clienteId;

    public async Task<ResultadoCadastro<Veiculo>> CriarVeiculoAsync(
        Guid clienteId, string placa, string marca, string modelo, int ano, CancellationToken cancelamento)
    {
        if (await repositorio.ObterClienteAsync(clienteId, cancelamento) is null)
            return ResultadoCadastro<Veiculo>.Falha(ErroCadastro.NaoEncontrado, "Cliente proprietário não encontrado.");
        Veiculo veiculo;
        try { veiculo = new Veiculo(clienteId, placa, marca, modelo, ano); }
        catch (ArgumentException erro) { return Invalido<Veiculo>(erro); }

        if (await repositorio.ExistePlacaAsync(veiculo.Placa, null, cancelamento))
            return ResultadoCadastro<Veiculo>.Falha(ErroCadastro.Conflito, "Já existe um veículo com essa placa.");
        repositorio.Adicionar(veiculo);
        return await SalvarAsync(veiculo, "Já existe um veículo com essa placa.", cancelamento);
    }

    public async Task<ResultadoCadastro<Veiculo>> AtualizarVeiculoAsync(
        Guid id, string placa, string marca, string modelo, int ano, CancellationToken cancelamento)
    {
        var veiculo = await repositorio.ObterVeiculoAsync(id, cancelamento);
        if (veiculo is null) return NaoEncontrado<Veiculo>();
        try { veiculo.Atualizar(placa, marca, modelo, ano); }
        catch (ArgumentException erro) { return Invalido<Veiculo>(erro); }

        if (await repositorio.ExistePlacaAsync(veiculo.Placa, id, cancelamento))
            return ResultadoCadastro<Veiculo>.Falha(ErroCadastro.Conflito, "Já existe um veículo com essa placa.");
        return await SalvarAsync(veiculo, "Já existe um veículo com essa placa.", cancelamento);
    }

    public async Task<ResultadoCadastro<bool>> RemoverVeiculoAsync(Guid id, CancellationToken cancelamento)
    {
        var veiculo = await repositorio.ObterVeiculoAsync(id, cancelamento);
        if (veiculo is null) return NaoEncontrado<bool>();
        repositorio.Remover(veiculo);
        return await SalvarAsync(true, "O veículo está em uso e não pode ser excluído.", cancelamento);
    }

    private async Task<ResultadoCadastro<T>> SalvarAsync<T>(T valor, string mensagemConflito, CancellationToken cancelamento)
    {
        try
        {
            await repositorio.SalvarAsync(cancelamento);
            return ResultadoCadastro<T>.Sucesso(valor);
        }
        catch (ConflitoPersistenciaException)
        {
            return ResultadoCadastro<T>.Falha(ErroCadastro.Conflito, mensagemConflito);
        }
    }

    private static ResultadoCadastro<T> Invalido<T>(ArgumentException erro) =>
        ResultadoCadastro<T>.Falha(ErroCadastro.Invalido, erro.Message, erro.ParamName);

    private static ResultadoCadastro<T> NaoEncontrado<T>() =>
        ResultadoCadastro<T>.Falha(ErroCadastro.NaoEncontrado, "Cadastro não encontrado.");
}
