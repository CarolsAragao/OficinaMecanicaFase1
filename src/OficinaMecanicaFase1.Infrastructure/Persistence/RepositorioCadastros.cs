using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using OficinaMecanicaFase1.Application.Cadastros;
using OficinaMecanicaFase1.Domain.Cadastros;

namespace OficinaMecanicaFase1.Infrastructure.Persistence;

public sealed class RepositorioCadastros(OficinaDbContext db) : IRepositorioCadastros
{
    public async Task<IReadOnlyList<Cliente>> ListarClientesAsync(string? documento, CancellationToken cancelamento) =>
        await db.Clientes.AsNoTracking()
            .Where(cliente => documento == null || cliente.Documento == documento)
            .OrderBy(cliente => cliente.Nome)
            .ToListAsync(cancelamento);

    public Task<Cliente?> ObterClienteAsync(Guid id, CancellationToken cancelamento) =>
        db.Clientes.FirstOrDefaultAsync(cliente => cliente.Id == id, cancelamento);

    public Task<bool> ExisteDocumentoAsync(string documento, Guid? excetoId, CancellationToken cancelamento) =>
        db.Clientes.AnyAsync(cliente => cliente.Documento == documento &&
            (!excetoId.HasValue || cliente.Id != excetoId.Value), cancelamento);

    public Task<bool> ClientePossuiVeiculosAsync(Guid clienteId, CancellationToken cancelamento) =>
        db.Veiculos.AnyAsync(veiculo => veiculo.ClienteId == clienteId, cancelamento);

    public async Task<IReadOnlyList<Veiculo>> ListarVeiculosAsync(Guid? clienteId, CancellationToken cancelamento) =>
        await db.Veiculos.AsNoTracking()
            .Where(veiculo => !clienteId.HasValue || veiculo.ClienteId == clienteId.Value)
            .OrderBy(veiculo => veiculo.Placa)
            .ToListAsync(cancelamento);

    public Task<Veiculo?> ObterVeiculoAsync(Guid id, CancellationToken cancelamento) =>
        db.Veiculos.FirstOrDefaultAsync(veiculo => veiculo.Id == id, cancelamento);

    public Task<bool> ExistePlacaAsync(string placa, Guid? excetoId, CancellationToken cancelamento) =>
        db.Veiculos.AnyAsync(veiculo => veiculo.Placa == placa &&
            (!excetoId.HasValue || veiculo.Id != excetoId.Value), cancelamento);

    public void Adicionar(Cliente cliente) => db.Clientes.Add(cliente);
    public void Adicionar(Veiculo veiculo) => db.Veiculos.Add(veiculo);
    public void Remover(Cliente cliente) => db.Clientes.Remove(cliente);
    public void Remover(Veiculo veiculo) => db.Veiculos.Remove(veiculo);

    public async Task SalvarAsync(CancellationToken cancelamento)
    {
        try { await db.SaveChangesAsync(cancelamento); }
        catch (DbUpdateException erro) when (erro.InnerException is SqlException sql &&
            sql.Number is 2601 or 2627 or 547)
        {
            throw new ConflitoPersistenciaException(erro);
        }
    }
}
