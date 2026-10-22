namespace OficinaMecanicaFase1.Domain.Cadastros;

public sealed class Veiculo
{
    private Veiculo() { }

    public Guid Id { get; private set; }
    public Guid ClienteId { get; private set; }
    public string Placa { get; private set; } = null!;
    public string Marca { get; private set; } = null!;
    public string Modelo { get; private set; } = null!;
    public int Ano { get; private set; }

    public Veiculo(Guid clienteId, string placa, string marca, string modelo, int ano)
    {
        if (clienteId == Guid.Empty) throw new ArgumentException("Informe o cliente proprietário.", nameof(clienteId));
        Id = Guid.NewGuid();
        ClienteId = clienteId;
        Atualizar(placa, marca, modelo, ano);
    }

    public void Atualizar(string placa, string marca, string modelo, int ano)
    {
        var normalizada = PlacaVeiculo.Normalizar(placa);
        if (normalizada is null) throw new ArgumentException("Informe uma placa brasileira válida.", nameof(placa));
        if (string.IsNullOrWhiteSpace(marca) || marca.Trim().Length > 100)
            throw new ArgumentException("Informe uma marca com até 100 caracteres.", nameof(marca));
        if (string.IsNullOrWhiteSpace(modelo) || modelo.Trim().Length > 100)
            throw new ArgumentException("Informe um modelo com até 100 caracteres.", nameof(modelo));
        if (ano < 1886 || ano > DateTime.UtcNow.Year + 1)
            throw new ArgumentException("Informe um ano de fabricação válido.", nameof(ano));
        Placa = normalizada;
        Marca = marca.Trim();
        Modelo = modelo.Trim();
        Ano = ano;
    }
}
