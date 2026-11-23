using OficinaMecanicaFase1.Domain.Cadastros;

namespace OficinaMecanicaFase1.Api.Contracts;

public sealed record ClienteRequest(string Nome, string Documento, string? Email = null);
public sealed record ClienteResponse(Guid Id, string Nome, string Documento, string? Email)
{
    public static ClienteResponse De(Cliente cliente) => new(cliente.Id, cliente.Nome, cliente.Documento, cliente.Email);
}

public sealed record CriarVeiculoRequest(Guid ClienteId, string Placa, string Marca, string Modelo, int Ano);
public sealed record AtualizarVeiculoRequest(string Placa, string Marca, string Modelo, int Ano);
public sealed record VeiculoResponse(Guid Id, Guid ClienteId, string Placa, string Marca, string Modelo, int Ano)
{
    public static VeiculoResponse De(Veiculo veiculo) =>
        new(veiculo.Id, veiculo.ClienteId, veiculo.Placa, veiculo.Marca, veiculo.Modelo, veiculo.Ano);
}
