namespace OficinaMecanicaFase1.Domain.Cadastros;

public sealed class Cliente
{
    private Cliente() { }

    public Guid Id { get; private set; }
    public string Nome { get; private set; } = null!;
    public string Documento { get; private set; } = null!;

    public Cliente(string nome, string documento)
    {
        Id = Guid.NewGuid();
        Atualizar(nome, documento);
    }

    public void Atualizar(string nome, string documento)
    {
        if (string.IsNullOrWhiteSpace(nome) || nome.Trim().Length > 200)
            throw new ArgumentException("Informe um nome com até 200 caracteres.", nameof(nome));
        var normalizado = DocumentoCliente.Normalizar(documento);
        if (normalizado is null)
            throw new ArgumentException("Informe um CPF ou CNPJ válido.", nameof(documento));
        Nome = nome.Trim();
        Documento = normalizado;
    }
}
