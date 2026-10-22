using OficinaMecanicaFase1.Domain.Cadastros;

namespace OficinaMecanicaFase1.Application.Cadastros;

public interface IRepositorioCadastros
{
    Task<IReadOnlyList<Cliente>> ListarClientesAsync(string? documento, CancellationToken cancelamento);
    Task<Cliente?> ObterClienteAsync(Guid id, CancellationToken cancelamento);
    Task<bool> ExisteDocumentoAsync(string documento, Guid? excetoId, CancellationToken cancelamento);
    Task<bool> ClientePossuiVeiculosAsync(Guid clienteId, CancellationToken cancelamento);
    Task<IReadOnlyList<Veiculo>> ListarVeiculosAsync(Guid? clienteId, CancellationToken cancelamento);
    Task<Veiculo?> ObterVeiculoAsync(Guid id, CancellationToken cancelamento);
    Task<bool> ExistePlacaAsync(string placa, Guid? excetoId, CancellationToken cancelamento);
    void Adicionar(Cliente cliente);
    void Adicionar(Veiculo veiculo);
    void Remover(Cliente cliente);
    void Remover(Veiculo veiculo);
    Task SalvarAsync(CancellationToken cancelamento);
}
